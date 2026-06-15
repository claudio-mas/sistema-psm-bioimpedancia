using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SistemaPSM.AddIn.Laudo
{
    /// <summary>
    /// Recursos do laudo. O wkhtmltopdf.exe é distribuído como arquivo ao lado do assembly
    /// (Content do ClickOnce) — não embutido na DLL, para o add-in carregar rápido na abertura
    /// do Excel. As fontes continuam embutidas e são extraídas (uma vez) para
    /// %LOCALAPPDATA%\SistemaPSM\Laudo, pois o wkhtmltopdf precisa de caminhos file:/// estáveis.
    /// </summary>
    public static class RecursosLaudo
    {
        private const string ResPlayfair = "SistemaPSM.AddIn.Fonts.PlayfairDisplay.ttf";
        private const string ResInter = "SistemaPSM.AddIn.Fonts.Inter.ttf";

        public static string PastaBase
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SistemaPSM", "Laudo");
            }
        }

        /// <summary>
        /// Pastas candidatas onde o exe pode estar, em ordem de preferência. No ClickOnce/VSTO o
        /// .NET carrega a DLL de um *shadow cache* (…\assembly\dl3\…) que contém só os assemblies —
        /// os arquivos de Content (o wkhtmltopdf) ficam na pasta real de implantação (…\Apps\2.0\…),
        /// para onde aponta o <c>CodeBase</c>, e NÃO o <c>Location</c>. Por isso sondamos ambos.
        /// </summary>
        private static IEnumerable<string> DirsCandidatos()
        {
            var asm = Assembly.GetExecutingAssembly();

            string codeBaseDir = null;
            try { codeBaseDir = Path.GetDirectoryName(new Uri(asm.CodeBase).LocalPath); } catch { }
            if (!string.IsNullOrEmpty(codeBaseDir)) yield return codeBaseDir;

            string locDir = null;
            try { locDir = string.IsNullOrEmpty(asm.Location) ? null : Path.GetDirectoryName(asm.Location); } catch { }
            if (!string.IsNullOrEmpty(locDir)) yield return locDir;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir)) yield return baseDir;

            yield return PastaBase; // último recurso
        }

        /// <summary>wkhtmltopdf.exe ao lado da DLL — sonda as pastas candidatas e usa a 1ª que o contém.</summary>
        public static string CaminhoExe
        {
            get
            {
                string primeiro = null;
                foreach (string dir in DirsCandidatos())
                {
                    string exe = Path.Combine(dir, "wkhtmltopdf", "wkhtmltopdf.exe");
                    if (primeiro == null) primeiro = exe;
                    if (File.Exists(exe)) return exe;
                }
                return primeiro; // não achou em lugar nenhum → devolve o 1º p/ mensagem de erro coerente
            }
        }

        public static string CaminhoPlayfair { get { return Path.Combine(PastaBase, "fonts", "PlayfairDisplay.ttf"); } }
        public static string CaminhoInter { get { return Path.Combine(PastaBase, "fonts", "Inter.ttf"); } }

        public static string UriPlayfair { get { return ParaUri(CaminhoPlayfair); } }
        public static string UriInter { get { return ParaUri(CaminhoInter); } }

        /// <summary>Garante que as fontes estejam extraídas em disco (o exe já vem como arquivo).</summary>
        public static void Garantir()
        {
            Directory.CreateDirectory(Path.Combine(PastaBase, "fonts"));
            Extrair(ResPlayfair, CaminhoPlayfair);
            Extrair(ResInter, CaminhoInter);
        }

        private static void Extrair(string recurso, string destino)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(recurso))
            {
                if (s == null) throw new InvalidOperationException("Recurso embutido não encontrado: " + recurso);

                // Idempotente: re-extrai apenas se ausente ou de tamanho diferente.
                if (File.Exists(destino) && new FileInfo(destino).Length == s.Length) return;

                using (var f = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None))
                    s.CopyTo(f);
            }
        }

        private static string ParaUri(string caminho)
        {
            return new Uri(caminho).AbsoluteUri; // gera file:/// com escaping correto (espaços, acentos)
        }
    }
}
