using System.Collections.Generic;

namespace SistemaPSM.AddIn.Importacao
{
    /// <summary>
    /// Layout ZONAL do laudo Relaxmedic (template fixo). Cada <see cref="Zona"/> é um retângulo
    /// em coordenadas NORMALIZADAS por-mil (0..1000) — x sobre a largura, y sobre a altura da
    /// imagem — de onde o OCR extrai UM campo, e o nome do controle correspondente em
    /// <c>FrmBioimpC</c> a ser pré-preenchido.
    ///
    /// As coordenadas foram calibradas contra <c>BIA.pdf</c> (imagem 904x1280 → OCR em 2x) e
    /// validadas campo a campo. Por serem normalizadas, resistem a variação de escala/DPI; se o
    /// Relaxmedic mudar o TEMPLATE, as zonas precisam ser recalibradas.
    ///
    /// NÃO inclui Sexo/Idade/Altura/Nome/Data: esses vêm do cadastro do cliente em
    /// <c>FrmAvaliacaoC</c>, não de FrmBioimpC.
    /// </summary>
    internal static class RelatorioRelaxmedicLayout
    {
        /// <summary>Formato do valor emitido (decimal com vírgula pt-BR).</summary>
        internal enum Fmt { Dec2, Int, Dec1 }

        internal sealed class Zona
        {
            public string Campo;   // nome do controle em FrmBioimpC
            public double X0, Y0, X1, Y1; // per-mil
            public Fmt Formato;
            public bool BaixaConfianca; // segmentar sobre a silhueta → conferir

            public Zona(string campo, double x0, double y0, double x1, double y1, Fmt fmt, bool baixa = false)
            {
                Campo = campo; X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; Formato = fmt; BaixaConfianca = baixa;
            }
        }

        /// <summary>Todas as zonas mapeadas para controles de FrmBioimpC.</summary>
        internal static readonly Zona[] Zonas = new[]
        {
            // ---- Tabela "Análise da composição corporal" (coluna kg) ----
            new Zona("TxtPeso",            150, 136, 250, 153, Fmt.Dec2),
            new Zona("TxtGorduraAtualKg",  150, 157, 250, 173, Fmt.Dec2), // Gordura corporal (kg)
            new Zona("TxtMassaOssea",      150, 177, 250, 193, Fmt.Dec2), // Sal inorgânico (kg)
            new Zona("TxtProteinaKg",      150, 197, 250, 213, Fmt.Dec2), // Proteína (kg)
            new Zona("TxtAguaCorporalKg",  150, 217, 250, 233, Fmt.Dec2), // Água corporal (kg)
            new Zona("TxtMassaMuscular",   150, 238, 250, 254, Fmt.Dec2), // Músculo (kg)
            new Zona("TxtPesoMuscularEsq", 150, 258, 250, 275, Fmt.Dec2), // Músculo esquelético (kg)

            // O % de cada componente é calculado por FrmBioimpC (kg/peso*100); o OCR só lê os kg.

            // ---- Bloco direito ----
            new Zona("TxtPontuacao",       600, 122, 690, 150, Fmt.Int),
            new Zona("TxtImc",             780, 360, 865, 379, Fmt.Dec2),

            // ---- "Outros indicadores" (rodapé direito) ----
            new Zona("TxtGorduraVisceral",   860, 853, 965, 869, Fmt.Int),
            new Zona("TxtTmB",               860, 872, 965, 888, Fmt.Int),  // TMB kcal
            new Zona("TxtMlg",               860, 893, 965, 907, Fmt.Dec2), // Peso livre de gordura
            new Zona("TxtGorduraSubcutanea", 860, 911, 965, 927, Fmt.Dec2),
            new Zona("TxtIdadeCorpo",        860, 951, 965, 966, Fmt.Int),

            // ---- Impedância bioelétrica 20 kHz ----
            new Zona("TxtZ20BracoD1", 168, 915, 238, 931, Fmt.Dec1),
            new Zona("TxtZ20BracoE",  248, 915, 320, 931, Fmt.Dec1),
            new Zona("TxtZ20Tronco",  333, 915, 392, 931, Fmt.Dec1),
            new Zona("TxtZ20PernaD",  408, 915, 472, 931, Fmt.Dec1),
            new Zona("TxtZ20PernaE",  488, 915, 548, 931, Fmt.Dec1),
            // ---- Impedância bioelétrica 100 kHz ----
            new Zona("TxtZ100BracoD1",168, 941, 238, 957, Fmt.Dec1),
            new Zona("TxtZ100BracoE", 248, 941, 320, 957, Fmt.Dec1),
            new Zona("TxtZ100Tronco", 333, 941, 392, 957, Fmt.Dec1),
            new Zona("TxtZ100PernaD", 408, 941, 472, 957, Fmt.Dec1),
            new Zona("TxtZ100PernaE", 488, 941, 548, 957, Fmt.Dec1),

            // ---- "Análise de gordura segmentar" (silhueta ESQUERDA do laudo, kg + %) ----
            //      L (esquerdo) = coluna x~51 ; R (direito) = coluna x~240. BAIXA CONFIANÇA (sobre a silhueta).
            new Zona("TxtPesoBracoEsquerdo",        35, 605, 130, 620, Fmt.Dec2, true),
            new Zona("TxtPorcentagemBracoEsquerdo", 35, 621, 130, 637, Fmt.Dec2, true),
            new Zona("TxtPesoBracoDireito",        205, 605, 295, 620, Fmt.Dec2, true),
            new Zona("TxtPorcentagemBracoDireito", 205, 621, 295, 637, Fmt.Dec2, true),
            new Zona("TxtPesoAbdominal",            35, 659, 130, 674, Fmt.Dec2, true), // Tronco
            new Zona("TxtPorcentagemAbdominal",     35, 675, 130, 691, Fmt.Dec2, true),
            new Zona("TxtPesoPernaEsquerda",        35, 724, 130, 740, Fmt.Dec2, true),
            new Zona("TxtPorcentagemPernaEsquerda", 35, 740, 130, 756, Fmt.Dec2, true),
            new Zona("TxtPesoPernaDireita",        205, 724, 295, 740, Fmt.Dec2, true),
            new Zona("TxtPorcentagemPernaDireita", 205, 740, 295, 756, Fmt.Dec2, true),

            // ---- "Equilíbrio muscular" (silhueta DIREITA do laudo, kg + %) → controles TxtMusc* ----
            new Zona("TxtMuscBracoEsqKg",  315, 605, 400, 620, Fmt.Dec2, true),
            new Zona("TxtMuscBracoEsqPct", 315, 621, 400, 637, Fmt.Dec2, true),
            new Zona("TxtMuscBracoDirKg",  510, 605, 560, 620, Fmt.Dec2, true),
            new Zona("TxtMuscBracoDirPct", 500, 621, 560, 637, Fmt.Dec2, true),
            new Zona("TxtMuscTroncoKg",    315, 659, 400, 674, Fmt.Dec2, true),
            new Zona("TxtMuscTroncoPct",   315, 675, 400, 691, Fmt.Dec2, true),
            new Zona("TxtMuscPernaEsqKg",  315, 724, 400, 740, Fmt.Dec2, true),
            new Zona("TxtMuscPernaEsqPct", 315, 740, 400, 756, Fmt.Dec2, true),
            new Zona("TxtMuscPernaDirKg",  510, 724, 560, 740, Fmt.Dec2, true),
            new Zona("TxtMuscPernaDirPct", 500, 740, 560, 756, Fmt.Dec2, true),
        };
    }
}
