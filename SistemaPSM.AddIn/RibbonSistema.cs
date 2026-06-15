using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Office = Microsoft.Office.Core;

namespace SistemaPSM.AddIn
{
    /// <summary>
    /// Ribbon do suplemento: apenas um botão de alternância para mostrar/ocultar o painel.
    /// </summary>
    [ComVisible(true)]
    public class RibbonSistema : Office.IRibbonExtensibility
    {
        private Office.IRibbonUI _ribbon;

        public string GetCustomUI(string ribbonID)
        {
            return LerRecursoTexto("SistemaPSM.AddIn.RibbonSistema.xml");
        }

        public void OnLoad(Office.IRibbonUI ribbonUI)
        {
            _ribbon = ribbonUI;
        }

        public void Invalidate()
        {
            try { if (_ribbon != null) _ribbon.Invalidate(); }
            catch { }
        }

        // Visibilidade da aba: só quando a pasta do Sistema PSM está ativa.
        public bool Tab_GetVisible(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.PainelDisponivel();
        }

        // Toggle do painel.
        public void Btn_OnAction(Office.IRibbonControl control, bool pressed)
        {
            Globals.ThisAddIn.AlternarPainel();
        }

        public bool Btn_GetPressed(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.PainelVisivel();
        }

        public stdole.IPictureDisp Btn_GetImage(Office.IRibbonControl control)
        {
            return ConversorImagem.ParaPictureDisp(PainelSistema.Img("MeuCadastro"));
        }

        private static string LerRecursoTexto(string nomeRecurso)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream stream = asm.GetManifestResourceStream(nomeRecurso))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }

    /// <summary>Converte System.Drawing.Image em stdole.IPictureDisp para a ribbon.</summary>
    internal sealed class ConversorImagem : AxHost
    {
        private ConversorImagem() : base("00000000-0000-0000-0000-000000000000") { }

        public static stdole.IPictureDisp ParaPictureDisp(System.Drawing.Image imagem)
        {
            if (imagem == null) return null;
            return (stdole.IPictureDisp)GetIPictureDispFromPicture(imagem);
        }
    }
}
