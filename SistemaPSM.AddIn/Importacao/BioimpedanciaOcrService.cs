using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SistemaPSM.AddIn.Importacao
{
    /// <summary>
    /// Extrai os valores do laudo Relaxmedic (PDF-imagem) por OCR ZONAL e devolve pares
    /// <c>Campo=valor</c> (um por linha) para o VBA pré-preencher <c>FrmBioimpC</c>.
    ///
    /// Pipeline (espelha o padrão do wkhtmltopdf: processo externo):
    ///   1. Lê o PDF e extrai o JPEG de página inteira (varredura de marcadores FFD8..FFD9).
    ///   2. Rasteriza em 2x (System.Drawing) para PNG temporário — melhora o OCR.
    ///   3. OCR via <c>Windows.Media.Ocr</c> num processo <c>powershell.exe</c>
    ///      (<c>-EncodedCommand</c>, offline, sem dependência nova, sem arquivo .ps1 no deploy).
    ///   4. Casa cada palavra (com sua caixa) às zonas de <see cref="RelatorioRelaxmedicLayout"/>
    ///      e formata os números em pt-BR.
    ///
    /// Retorno: linhas "Campo=valor". Linha "#BAIXA=c1,c2,..." lista campos de baixa confiança.
    /// Em erro: uma única linha "#ERRO=mensagem".
    /// </summary>
    internal static class BioimpedanciaOcrService
    {
        private static readonly Regex RxNumero = new Regex(@"-?\d+(?:[.,]\d+)?", RegexOptions.Compiled);

        public static string ImportarBioimpedanciaPdf(string caminhoPdf)
        {
            string tmpPng = null;
            string tmpTsv = null;
            try
            {
                if (string.IsNullOrWhiteSpace(caminhoPdf) || !File.Exists(caminhoPdf))
                    return "#ERRO=Arquivo PDF não encontrado: " + caminhoPdf;

                byte[] jpeg = ExtrairJpegDoPdf(File.ReadAllBytes(caminhoPdf));
                if (jpeg == null)
                    return "#ERRO=Não foi possível localizar a imagem do laudo no PDF.";

                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SistemaPSM", "Import");
                Directory.CreateDirectory(dir);
                string stamp = Guid.NewGuid().ToString("N");
                tmpPng = Path.Combine(dir, stamp + ".png");
                tmpTsv = Path.Combine(dir, stamp + ".tsv");

                RasterizarEmDobro(jpeg, tmpPng);

                string tsv = RodarOcr(tmpPng, tmpTsv);
                if (tsv == null)
                    return "#ERRO=Falha ao executar o OCR (Windows.Media.Ocr).";
                if (tsv.StartsWith("#ERR"))
                    return "#ERRO=Motor de OCR indisponível no Windows (idioma não instalado?).";

                int larg, alt;
                List<Palavra> palavras = LerTsv(tsv, out larg, out alt);
                if (palavras.Count == 0 || larg == 0 || alt == 0)
                    return "#ERRO=OCR não retornou texto.";

                return Mapear(palavras, larg, alt);
            }
            catch (Exception ex)
            {
                return "#ERRO=" + ex.Message.Replace("\r", " ").Replace("\n", " ");
            }
            finally
            {
                TentarApagar(tmpPng);
                TentarApagar(tmpTsv);
            }
        }

        // ---------------------------------------------------------------- PDF → JPEG

        /// <summary>
        /// Extrai o maior bloco JPEG (SOI 0xFFD8 … EOI 0xFFD9) do PDF. Os laudos Relaxmedic
        /// embutem a página como um único JPEG (DCTDecode), então o maior bloco é a imagem.
        /// </summary>
        private static byte[] ExtrairJpegDoPdf(byte[] pdf)
        {
            int melhorIni = -1, melhorFim = -1;
            for (int i = 0; i + 1 < pdf.Length; i++)
            {
                if (pdf[i] != 0xFF || pdf[i + 1] != 0xD8) continue;
                // achou SOI; procura o EOI correspondente
                for (int j = i + 2; j + 1 < pdf.Length; j++)
                {
                    if (pdf[j] == 0xFF && pdf[j + 1] == 0xD9)
                    {
                        int tam = j + 2 - i;
                        if (tam > melhorFim - melhorIni)
                        {
                            melhorIni = i; melhorFim = j + 2;
                        }
                        i = j; // continua a varredura após este EOI
                        break;
                    }
                }
            }
            if (melhorIni < 0 || melhorFim <= melhorIni) return null;
            byte[] jpeg = new byte[melhorFim - melhorIni];
            Array.Copy(pdf, melhorIni, jpeg, 0, jpeg.Length);
            return jpeg;
        }

        // ---------------------------------------------------------------- rasterização 2x

        private static void RasterizarEmDobro(byte[] jpeg, string destinoPng)
        {
            using (var ms = new MemoryStream(jpeg))
            using (var orig = new Bitmap(ms))
            using (var grande = new Bitmap(orig.Width * 2, orig.Height * 2, PixelFormat.Format24bppRgb))
            {
                using (var g = Graphics.FromImage(grande))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(orig, 0, 0, grande.Width, grande.Height);
                }
                grande.Save(destinoPng, ImageFormat.Png);
            }
        }

        // ---------------------------------------------------------------- OCR (PowerShell)

        // Script Windows.Media.Ocr. Só aspas simples + [char]9 (tab) para embutir sem escaping.
        // Lê a imagem em $env:OCR_IN, grava o TSV (palavra<TAB>x<TAB>y<TAB>w<TAB>h) em $env:OCR_OUT.
        private const string ScriptOcr = @"
$ErrorActionPreference='Stop'
$TAB=[char]9
Add-Type -AssemblyName System.Runtime.WindowsRuntime | Out-Null
$asTaskGeneric=([System.WindowsRuntimeSystemExtensions].GetMethods()|Where-Object{$_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'})[0]
function Await($op,$t){$m=$asTaskGeneric.MakeGenericMethod($t);$k=$m.Invoke($null,@($op));$k.Wait(-1)|Out-Null;$k.Result}
[Windows.Storage.StorageFile,Windows.Foundation,ContentType=WindowsRuntime]|Out-Null
[Windows.Graphics.Imaging.BitmapDecoder,Windows.Foundation,ContentType=WindowsRuntime]|Out-Null
[Windows.Graphics.Imaging.SoftwareBitmap,Windows.Foundation,ContentType=WindowsRuntime]|Out-Null
[Windows.Media.Ocr.OcrEngine,Windows.Foundation,ContentType=WindowsRuntime]|Out-Null
$in=$env:OCR_IN;$out=$env:OCR_OUT
$file=Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($in)) ([Windows.Storage.StorageFile])
$stream=Await ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
$decoder=Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
$bmp=Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
$eng=[Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
if(-not $eng){[IO.File]::WriteAllText($out,'#ERR'+$TAB+'NO_OCR_ENGINE',(New-Object System.Text.UTF8Encoding($false)));exit 3}
$res=Await ($eng.RecognizeAsync($bmp)) ([Windows.Media.Ocr.OcrResult])
$sb=New-Object System.Text.StringBuilder
[void]$sb.AppendLine('#'+$TAB+[int]$decoder.PixelWidth+$TAB+[int]$decoder.PixelHeight)
foreach($ln in $res.Lines){foreach($wd in $ln.Words){$r=$wd.BoundingRect;[void]$sb.AppendLine($wd.Text+$TAB+[int]$r.X+$TAB+[int]$r.Y+$TAB+[int]$r.Width+$TAB+[int]$r.Height)}}
[IO.File]::WriteAllText($out,$sb.ToString(),(New-Object System.Text.UTF8Encoding($false)))
";

        private static string RodarOcr(string pngIn, string tsvOut)
        {
            string b64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(ScriptOcr));
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + b64)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            psi.EnvironmentVariables["OCR_IN"] = pngIn;
            psi.EnvironmentVariables["OCR_OUT"] = tsvOut;

            using (var p = Process.Start(psi))
            {
                // esvazia os pipes para não travar; a saída útil vai para o arquivo TSV
                p.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                if (!p.WaitForExit(60000))
                {
                    try { p.Kill(); } catch { }
                    return null;
                }
            }
            if (!File.Exists(tsvOut)) return null;
            return File.ReadAllText(tsvOut, Encoding.UTF8);
        }

        // ---------------------------------------------------------------- TSV → palavras

        private struct Palavra
        {
            public string Texto;
            public double Cx, Cy; // centro em per-mil (0..1000)
        }

        private static List<Palavra> LerTsv(string tsv, out int larg, out int alt)
        {
            larg = 0; alt = 0;
            var lista = new List<Palavra>();
            var linhas = tsv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var linha in linhas)
            {
                var c = linha.Split('\t');
                if (c.Length >= 3 && c[0] == "#")
                {
                    int.TryParse(c[1], out larg);
                    int.TryParse(c[2], out alt);
                    continue;
                }
                if (c.Length < 5) continue;
                double x, y, w, h;
                if (!double.TryParse(c[1], NumberStyles.Any, CultureInfo.InvariantCulture, out x)) continue;
                if (!double.TryParse(c[2], NumberStyles.Any, CultureInfo.InvariantCulture, out y)) continue;
                if (!double.TryParse(c[3], NumberStyles.Any, CultureInfo.InvariantCulture, out w)) continue;
                if (!double.TryParse(c[4], NumberStyles.Any, CultureInfo.InvariantCulture, out h)) continue;
                lista.Add(new Palavra { Texto = c[0], Cx = x + w / 2, Cy = y + h / 2 });
            }
            if (larg > 0 && alt > 0)
            {
                for (int i = 0; i < lista.Count; i++)
                {
                    var p = lista[i];
                    p.Cx = p.Cx / larg * 1000.0;
                    p.Cy = p.Cy / alt * 1000.0;
                    lista[i] = p;
                }
            }
            return lista;
        }

        // ---------------------------------------------------------------- zonas → valores

        private static string Mapear(List<Palavra> palavras, int larg, int alt)
        {
            var sb = new StringBuilder();
            var baixa = new List<string>();

            foreach (var z in RelatorioRelaxmedicLayout.Zonas)
            {
                // junta as palavras cujo centro cai na zona, da esquerda p/ direita
                var dentro = new List<Palavra>();
                foreach (var p in palavras)
                    if (p.Cx >= z.X0 && p.Cx <= z.X1 && p.Cy >= z.Y0 && p.Cy <= z.Y1)
                        dentro.Add(p);
                if (dentro.Count == 0) continue;
                dentro.Sort((a, b) => a.Cx.CompareTo(b.Cx));

                var texto = new StringBuilder();
                foreach (var p in dentro) { if (texto.Length > 0) texto.Append(' '); texto.Append(p.Texto); }

                var m = RxNumero.Match(texto.ToString());
                if (!m.Success) continue;

                double valor;
                string bruto = m.Value.Replace(',', '.');
                if (!double.TryParse(bruto, NumberStyles.Any, CultureInfo.InvariantCulture, out valor)) continue;
                if (valor == 0) continue;

                sb.Append(z.Campo).Append('=').Append(Formatar(valor, z.Formato)).Append("\r\n");
                if (z.BaixaConfianca) baixa.Add(z.Campo);
            }

            if (baixa.Count > 0)
                sb.Append("#BAIXA=").Append(string.Join(",", baixa)).Append("\r\n");

            return sb.ToString();
        }

        private static string Formatar(double valor, RelatorioRelaxmedicLayout.Fmt fmt)
        {
            string s;
            switch (fmt)
            {
                case RelatorioRelaxmedicLayout.Fmt.Int:
                    s = Math.Round(valor).ToString("0", CultureInfo.InvariantCulture);
                    break;
                case RelatorioRelaxmedicLayout.Fmt.Dec1:
                    s = valor.ToString("0.#", CultureInfo.InvariantCulture);
                    break;
                default: // Dec2
                    s = valor.ToString("0.00", CultureInfo.InvariantCulture);
                    break;
            }
            return s.Replace('.', ','); // pt-BR
        }

        private static void TentarApagar(string caminho)
        {
            if (caminho == null) return;
            try { if (File.Exists(caminho)) File.Delete(caminho); } catch { }
        }
    }
}
