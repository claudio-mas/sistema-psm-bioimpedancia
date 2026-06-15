using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SistemaPSM.AddIn.Laudo
{
    /// <summary>Invoca o wkhtmltopdf.exe (processo externo) para converter HTML em PDF.</summary>
    public static class WkHtmlToPdf
    {
        /// <summary>
        /// Gera o PDF a partir do arquivo HTML. Lança exceção com o stderr do wkhtmltopdf em falha.
        /// </summary>
        public static void GerarPdf(string exePath, string htmlPath, string pdfPath, int timeoutMs = 90000)
        {
            if (!File.Exists(exePath)) throw new FileNotFoundException("wkhtmltopdf.exe não encontrado.", exePath);

            var args = new StringBuilder();
            args.Append("--enable-local-file-access --quiet --encoding utf-8 ");
            args.Append("--page-size A4 ");
            args.Append("--margin-top 8mm --margin-bottom 8mm --margin-left 9mm --margin-right 9mm ");
            args.Append('"').Append(htmlPath).Append("\" \"").Append(pdfPath).Append('"');

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory
            };

            using (var p = new Process())
            {
                p.StartInfo = psi;
                var err = new StringBuilder();
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) err.AppendLine(e.Data); };
                p.OutputDataReceived += (s, e) => { };

                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();

                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { }
                    throw new TimeoutException("A geração do PDF excedeu o tempo limite.");
                }

                // wkhtmltopdf pode retornar código != 0 só com avisos; o critério real é o PDF existir.
                if (!File.Exists(pdfPath) || new FileInfo(pdfPath).Length == 0)
                    throw new Exception("Falha ao gerar o PDF.\n" + err.ToString().Trim());
            }
        }
    }
}
