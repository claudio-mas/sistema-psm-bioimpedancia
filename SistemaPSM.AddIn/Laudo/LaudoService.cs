using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace SistemaPSM.AddIn.Laudo
{
    /// <summary>
    /// Orquestra a emissão do laudo: coleta a avaliação selecionada, gera o HTML, converte em
    /// PDF (wkhtmltopdf), salva em CLIENTES\&lt;Id Nome&gt; e abre o arquivo.
    /// </summary>
    public static class LaudoService
    {
        private const string Titulo = "Sistema PSM — Laudo";

        /// <summary>Emite o laudo de evolução da linha ativa na aba "Avaliações" (gatilho legado).</summary>
        public static void GerarLaudoLinhaSelecionada()
        {
            string erro;
            AvaliacaoLaudo a = LaudoRepositorio.LerSelecionada(out erro);
            if (a == null)
            {
                MessageBox.Show(erro, Titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            GerarEvolucao(a);
        }

        /// <summary>
        /// Emite o laudo de EVOLUÇÃO da avaliação cujo <c>Seq</c> (coluna A) é informado.
        /// Gatilho do formulário VBA <c>FrmRelatorioB</c> (botão "Comparações"), via automação COM.
        /// </summary>
        public static void GerarLaudoPorSeq(int seq)
        {
            string erro;
            AvaliacaoLaudo a = LaudoRepositorio.LerPorSeq(seq, out erro);
            if (a == null)
            {
                MessageBox.Show(erro, Titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            GerarEvolucao(a);
        }

        /// <summary>
        /// Emite o laudo DETALHADO de avaliação única (estilo Relaxmedic) da avaliação cujo
        /// <c>Seq</c> (coluna A) é informado. Gatilho do botão "Avaliação" do <c>FrmRelatorioB</c>.
        /// </summary>
        public static void GerarLaudoAvaliacaoPorSeq(int seq)
        {
            string erro;
            AvaliacaoLaudo a = LaudoRepositorio.LerCompletaPorSeq(seq, out erro);
            if (a == null)
            {
                MessageBox.Show(erro, Titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            RecursosLaudo.Garantir();
            string html = LaudoAvaliacaoHtmlBuilder.MontarHtml(a, RecursosLaudo.UriPlayfair, RecursosLaudo.UriInter, LogoArquivoDataUri());
            Gerar(a, html, "Avaliacao");
        }

        /// <summary>Monta o HTML do laudo de evolução e gera o PDF.</summary>
        private static void GerarEvolucao(AvaliacaoLaudo a)
        {
            RecursosLaudo.Garantir();
            string html = LaudoHtmlBuilder.MontarHtml(a, RecursosLaudo.UriPlayfair, RecursosLaudo.UriInter, LogoArquivoDataUri());
            Gerar(a, html, "Laudo");
        }

        /// <summary>Grava o HTML, converte em PDF (wkhtmltopdf), salva e abre o arquivo.</summary>
        private static void Gerar(AvaliacaoLaudo a, string html, string prefixoArquivo)
        {
            string htmlTemp = null;
            try
            {
                htmlTemp = Path.Combine(Path.GetTempPath(), "laudo_" + Guid.NewGuid().ToString("N") + ".html");
                File.WriteAllText(htmlTemp, html, new UTF8Encoding(false));

                string pdf = CaminhoDestino(a, prefixoArquivo);

                try
                {
                    WkHtmlToPdf.GerarPdf(RecursosLaudo.CaminhoExe, htmlTemp, pdf);
                }
                catch (Exception exGer)
                {
                    MessageBox.Show(
                        "Não foi possível gerar o PDF. Verifique se o laudo não está aberto em outro programa e tente novamente.\n\n"
                        + exGer.Message, Titulo, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AbrirArquivo(pdf);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao emitir o laudo.\n\n" + ex.Message, Titulo,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (htmlTemp != null) { try { File.Delete(htmlTemp); } catch { } }
            }
        }

        /// <summary>Define o caminho do PDF: &lt;pasta do .xlsm&gt;\CLIENTES\&lt;Id Nome&gt;\&lt;prefixo&gt; &lt;data&gt;.pdf.</summary>
        private static string CaminhoDestino(AvaliacaoLaudo a, string prefixoArquivo)
        {
            string raiz;
            try
            {
                Excel.Workbook wb = Globals.ThisAddIn.Application.ActiveWorkbook;
                raiz = (wb != null && !string.IsNullOrEmpty(wb.Path)) ? wb.Path : null;
            }
            catch { raiz = null; }

            if (string.IsNullOrEmpty(raiz))
                raiz = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SistemaPSM");

            string nomePasta = ((a.Id > 0 ? a.Id + " " : "") + a.Nome).Trim();
            string pastaCliente = Path.Combine(raiz, "CLIENTES", Sanitizar(nomePasta));
            Directory.CreateDirectory(pastaCliente);

            string data = (a.Data ?? DateTime.Now).ToString("dd-MM-yyyy");
            string prefixo = string.IsNullOrEmpty(prefixoArquivo) ? "Laudo" : prefixoArquivo;
            return Path.Combine(pastaCliente, Sanitizar(prefixo + " " + data) + ".pdf");
        }

        private static void AbrirArquivo(string caminho)
        {
            try { Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true }); }
            catch { /* PDF gerado; falha apenas ao abrir o visualizador */ }
        }

        /// <summary>
        /// Logo do laudo: lê um arquivo de imagem ("logo.jpeg"/"logo.jpg"/"logo.png") na pasta da
        /// planilha (a "raiz do projeto"), em vez da logo embutida no .xlsm. Assim nada pesado fica
        /// dentro do .xlsm (não atrasa a abertura) e a logo pode ser trocada só substituindo o
        /// arquivo. Retorna um data URI, ou null (→ o builder usa a logo embutida como fallback).
        /// </summary>
        private static string LogoArquivoDataUri()
        {
            try
            {
                Excel.Workbook wb = Globals.ThisAddIn.Application.ActiveWorkbook;
                string pasta = (wb != null) ? wb.Path : null;
                if (string.IsNullOrEmpty(pasta)) return null;

                // .png primeiro: suporta transparência (fundo creme do laudo aparece através da logo)
                string[] nomes = { "logo.png", "logo.jpeg", "logo.jpg" };
                foreach (string nome in nomes)
                {
                    string caminho = Path.Combine(pasta, nome);
                    if (!File.Exists(caminho)) continue;

                    byte[] bytes = File.ReadAllBytes(caminho);
                    string mime = nome.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
                    return "data:" + mime + ";base64," + Convert.ToBase64String(bytes);
                }
            }
            catch { /* qualquer falha → fallback */ }
            return null;
        }

        private static string Sanitizar(string nome)
        {
            if (string.IsNullOrEmpty(nome)) return "Laudo";
            foreach (char ch in Path.GetInvalidFileNameChars())
                nome = nome.Replace(ch, ' ');
            nome = nome.Trim();
            return nome.Length == 0 ? "Laudo" : nome;
        }
    }
}
