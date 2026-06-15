using System;
using System.Collections.Generic;
using Microsoft.Office.Tools;
using Office = Microsoft.Office.Core;
using Excel = Microsoft.Office.Interop.Excel;
using SistemaPSM.AddIn.Laudo;

namespace SistemaPSM.AddIn
{
    public partial class ThisAddIn
    {
        // Objeto de automação exposto ao VBA (Application.COMAddIns("SistemaPSM.AddIn").Object).
        private LaudoAutomation _automacao;

        protected override object RequestComAddInAutomationService()
        {
            if (_automacao == null) _automacao = new LaudoAutomation();
            return _automacao;
        }

        // Um CustomTaskPane por janela (Excel 2013+ é SDI: cada pasta = uma janela).
        // Chave = Hwnd da janela (estável).
        private readonly Dictionary<int, CustomTaskPane> _paineis = new Dictionary<int, CustomTaskPane>();
        private RibbonSistema _ribbon;

        private void ThisAddIn_Startup(object sender, EventArgs e)
        {
            this.Application.WorkbookActivate += App_WorkbookActivate;
            this.Application.WorkbookDeactivate += App_WorkbookDeactivate;
            this.Application.WorkbookBeforeClose += App_WorkbookBeforeClose;

            try
            {
                var awb = this.Application.ActiveWorkbook;
                if (awb != null && EhSistemaPSM(awb))
                    MostrarPainel(awb);
            }
            catch { /* inicialização tolerante a falhas */ }
        }

        private void ThisAddIn_Shutdown(object sender, EventArgs e) { }

        protected override Office.IRibbonExtensibility CreateRibbonExtensibilityObject()
        {
            _ribbon = new RibbonSistema();
            return _ribbon;
        }

        /// <summary>Identifica a pasta do Sistema PSM de forma robusta a renomeação.</summary>
        internal bool EhSistemaPSM(Excel.Workbook wb)
        {
            if (wb == null) return false;

            // 1) Marcador CustomDocumentProperty "SistemaPSM"
            try
            {
                var props = (Office.DocumentProperties)wb.CustomDocumentProperties;
                foreach (Office.DocumentProperty p in props)
                {
                    if (string.Equals(p.Name, "SistemaPSM", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }

            // 2) Fallback: nome do arquivo
            try
            {
                if (wb.Name != null && wb.Name.StartsWith("Bioimpedancia", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }

            return false;
        }

        private int? HwndDe(Excel.Workbook wb)
        {
            try { return wb.Windows[1].Hwnd; }
            catch { return null; }
        }

        internal void MostrarPainel(Excel.Workbook wb)
        {
            var hwnd = HwndDe(wb);
            if (hwnd == null) return;

            CustomTaskPane ctp;
            if (!_paineis.TryGetValue(hwnd.Value, out ctp) || ctp == null)
            {
                Excel.Window win = wb.Windows[1];
                // Título em branco: a barra nativa fica sem texto (o cabeçalho vive no banner do painel).
                ctp = this.CustomTaskPanes.Add(new PainelSistema(), " ", win);
                ctp.DockPosition = Office.MsoCTPDockPosition.msoCTPDockPositionLeft;
                ctp.Width = 220;
                ctp.VisibleChanged += (s, ev) => InvalidarRibbon();
                _paineis[hwnd.Value] = ctp;
            }
            ctp.Visible = true;
            InvalidarRibbon();
        }

        internal void AlternarPainel()
        {
            var wb = this.Application.ActiveWorkbook;
            if (wb == null || !EhSistemaPSM(wb)) return;

            var hwnd = HwndDe(wb);
            if (hwnd == null) return;

            CustomTaskPane ctp;
            if (_paineis.TryGetValue(hwnd.Value, out ctp) && ctp != null)
                ctp.Visible = !ctp.Visible;
            else
                MostrarPainel(wb);
        }

        internal bool PainelVisivel()
        {
            var wb = this.Application.ActiveWorkbook;
            if (wb == null) return false;
            var hwnd = HwndDe(wb);
            if (hwnd == null) return false;
            CustomTaskPane ctp;
            return _paineis.TryGetValue(hwnd.Value, out ctp) && ctp != null && ctp.Visible;
        }

        internal bool PainelDisponivel()
        {
            var wb = this.Application.ActiveWorkbook;
            return wb != null && EhSistemaPSM(wb);
        }

        private void InvalidarRibbon()
        {
            try { if (_ribbon != null) _ribbon.Invalidate(); }
            catch { }
        }

        private void App_WorkbookActivate(Excel.Workbook wb)
        {
            if (EhSistemaPSM(wb)) MostrarPainel(wb);
            InvalidarRibbon();
        }

        private void App_WorkbookDeactivate(Excel.Workbook wb)
        {
            InvalidarRibbon();
        }

        private void App_WorkbookBeforeClose(Excel.Workbook wb, ref bool cancel)
        {
            var hwnd = HwndDe(wb);
            if (hwnd == null) return;
            CustomTaskPane ctp;
            if (_paineis.TryGetValue(hwnd.Value, out ctp))
            {
                _paineis.Remove(hwnd.Value);
                try { this.CustomTaskPanes.Remove(ctp); } catch { }
            }
        }

        #region VSTO generated code
        private void InternalStartup()
        {
            this.Startup += new EventHandler(ThisAddIn_Startup);
            this.Shutdown += new EventHandler(ThisAddIn_Shutdown);
        }
        #endregion
    }
}
