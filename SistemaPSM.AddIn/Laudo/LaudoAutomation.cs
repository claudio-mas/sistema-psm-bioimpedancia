using System;
using System.Runtime.InteropServices;

namespace SistemaPSM.AddIn.Laudo
{
    /// <summary>
    /// Objeto de automação exposto ao VBA pelo add-in (via
    /// <see cref="ThisAddIn.RequestComAddInAutomationService"/>). Permite ao formulário
    /// <c>FrmRelatorioB</c> emitir o laudo:
    /// <code>Application.COMAddIns("SistemaPSM.AddIn").Object.EmitirLaudoPorSeq Seq</code>
    /// O assembly é ComVisible(false), então a visibilidade COM é declarada aqui explicitamente.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class LaudoAutomation
    {
        /// <summary>Emite o laudo de EVOLUÇÃO da avaliação cujo Seq (coluna A) é informado. (botão "Comparações")</summary>
        public void EmitirLaudoPorSeq(int seq)
        {
            LaudoService.GerarLaudoPorSeq(seq);
        }

        /// <summary>Emite o laudo DETALHADO de avaliação única (estilo Relaxmedic). (botão "Avaliação")</summary>
        public void EmitirLaudoAvaliacaoPorSeq(int seq)
        {
            LaudoService.GerarLaudoAvaliacaoPorSeq(seq);
        }

        /// <summary>
        /// Lê um laudo Relaxmedic (PDF-imagem) por OCR e devolve os valores para o VBA
        /// pré-preencher FrmBioimpC. Retorno: linhas "Campo=valor" (uma por linha); campos de
        /// baixa confiança listados em "#BAIXA=..."; em erro, uma linha "#ERRO=mensagem".
        /// Uso no VBA:
        /// <code>r = Application.COMAddIns("SistemaPSM.AddIn").Object.ImportarBioimpedanciaPdf(caminho)</code>
        /// </summary>
        public string ImportarBioimpedanciaPdf(string caminhoPdf)
        {
            return Importacao.BioimpedanciaOcrService.ImportarBioimpedanciaPdf(caminhoPdf);
        }
    }
}
