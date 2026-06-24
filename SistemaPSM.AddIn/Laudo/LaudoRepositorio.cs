using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Excel = Microsoft.Office.Interop.Excel;

namespace SistemaPSM.AddIn.Laudo
{
    /// <summary>
    /// Lê os dados do laudo a partir da linha selecionada na aba "Avaliações".
    /// Offsets confirmados contra AvaliacaoB.bas (coluna = offset + 1; A = offset 0).
    /// </summary>
    public static class LaudoRepositorio
    {
        // Offsets (0-based a partir de A) — espelham SalvaAvaliacaoB / RelatorioBH_Preenche.
        private const int OFF_DATA = 1, OFF_ID = 4, OFF_NOME = 5, OFF_SEXO = 6, OFF_IDADE = 7;
        private const int OFF_PESO = 9, OFF_ALTURA = 10;
        private const int OFF_PEITORAL = 17, OFF_ABDOMEN = 22, OFF_QUADRIL = 24;
        private const int OFF_IMC = 29, OFF_IMC_CL = 30;
        private const int OFF_GORD_PCT = 31, OFF_GORD_KG = 32, OFF_GORD_CL = 33;
        private const int OFF_MLG_KG = 39;
        private const int OFF_MUSC_PCT = 40, OFF_MUSC_KG = 41, OFF_MUSC_CL = 42;
        private const int OFF_VISC = 43, OFF_VISC_CL = 44;
        private const int OFF_IDCORP = 55;
        private const int OFF_RCQ = 57, OFF_RCQ_CL = 58;
        private const int OFF_TMB = 63, OFF_GET = 65;
        private const int OFF_INT_IMC = 66, OFF_INT_GORD = 67, OFF_INT_MUSC = 68, OFF_INT_VISC = 69, OFF_INT_RCQ = 70;
        private const int OFF_CONCLUSAO = 85; // CH — texto livre da conclusão da avaliação
        private const int COL_MAX = OFF_CONCLUSAO + 1; // lê até CH (offset 85) -> col 86, p/ incluir a Conclusão

        // Offsets extras (laudo de avaliação única). Confirmados contra o cabeçalho real (A–CG).
        private const int OFF_TAXA_MUSC = 34, OFF_TAXA_MUSC_CL = 35, OFF_PESO_MUSC = 36, OFF_PESO_MUSC_CL = 37;
        private const int OFF_MLG_PCT = 38;
        private const int OFF_SUBCUT_PCT = 45, OFF_SUBCUT_CL = 46;
        private const int OFF_AGUA = 47, OFF_AGUA_CL = 48, OFF_PROT = 49, OFF_PROT_CL = 50;
        private const int OFF_OSSEA_PCT = 51, OFF_PESO_OSSEO = 52, OFF_OSSEA_CL = 53, OFF_PESO_RESID = 54;
        private const int OFF_IDCORP_CL = 56;
        private const int OFF_GORD_ALVO = 59, OFF_IMC_ALVO = 60, OFF_PESO_ALVO = 61, OFF_CTRL_PESO = 62;
        private const int OFF_TMB_CL = 64, OFF_PESO_TOTAL_CL = 71;
        private const int OFF_BRACO_D_PCT = 72, OFF_BRACO_D_KG = 73, OFF_BRACO_E_PCT = 74, OFF_BRACO_E_KG = 75;
        private const int OFF_ABS_PCT = 76, OFF_ABS_KG = 77;
        private const int OFF_PERNA_D_PCT = 78, OFF_PERNA_D_KG = 79, OFF_PERNA_E_PCT = 80, OFF_PERNA_E_KG = 81;
        private const int OFF_RESID_PCT = 82;
        private const int OFF_PONTUACAO = 86; // CI — pontuação corporal (0–100)
        // Impedância Z (Ω): 20 kHz (CJ–CN) e 100 kHz (CO–CS), ordem Braço D/E, Tronco, Perna D/E.
        private const int OFF_Z20_BRD = 87, OFF_Z20_BRE = 88, OFF_Z20_TR = 89, OFF_Z20_PD = 90, OFF_Z20_PE = 91;
        private const int OFF_Z100_BRD = 92, OFF_Z100_BRE = 93, OFF_Z100_TR = 94, OFF_Z100_PD = 95, OFF_Z100_PE = 96;
        // Músculo segmentar (% + kg), espelha BU–CD. CT–DC (97–106).
        private const int OFF_MUSC_BRD_PCT = 97, OFF_MUSC_BRD_KG = 98, OFF_MUSC_BRE_PCT = 99, OFF_MUSC_BRE_KG = 100;
        private const int OFF_MUSC_ABS_PCT = 101, OFF_MUSC_ABS_KG = 102;
        private const int OFF_MUSC_PD_PCT = 103, OFF_MUSC_PD_KG = 104, OFF_MUSC_PE_PCT = 105, OFF_MUSC_PE_KG = 106;
        private const int OFF_HORA = 107; // DD — hora da medição ("HH:mm")
        private const int COL_MAX_FULL = OFF_HORA + 1; // lê até DD (offset 107) -> col 108

        /// <summary>
        /// Coleta a avaliação da linha ativa (+ avaliação anterior do mesmo paciente).
        /// Retorna null e preenche <paramref name="erro"/> se o contexto for inválido.
        /// </summary>
        public static AvaliacaoLaudo LerSelecionada(out string erro)
        {
            erro = null;
            var app = Globals.ThisAddIn.Application;

            var aba = app.ActiveSheet as Excel.Worksheet;
            if (aba == null || !aba.Name.StartsWith("Avalia", StringComparison.OrdinalIgnoreCase))
            {
                erro = "Selecione a linha de uma avaliação na aba \"Avaliações\" antes de emitir o laudo.";
                return null;
            }

            var sel = app.ActiveCell as Excel.Range;
            if (sel == null) { erro = "Nenhuma célula selecionada."; return null; }

            int row = sel.Row;
            if (row < 2) { erro = "Selecione a linha de uma avaliação (não o cabeçalho)."; return null; }

            int last = ((Excel.Range)aba.Cells[aba.Rows.Count, 1]).End[Excel.XlDirection.xlUp].Row;
            if (last < 2) { erro = "Não há avaliações cadastradas."; return null; }
            if (row > last) { erro = "Selecione a linha de uma avaliação válida."; return null; }

            // Lê o bloco inteiro de dados de uma só vez (uma chamada COM).
            var dados = LerBloco(aba, last);
            if (dados == null) { erro = "Não foi possível ler os dados da avaliação."; return null; }

            int idxAtual = row - 1; // array Value2 é 1-based; linha 2 da planilha = índice 1.
            var atual = Montar(dados, last, idxAtual);
            if (atual == null)
            {
                erro = "A linha selecionada não contém uma avaliação.";
                return null;
            }
            return atual;
        }

        /// <summary>
        /// Coleta a avaliação cujo <c>Seq</c> (coluna A) corresponde ao valor informado, na aba
        /// "Avaliações" da pasta ativa. Usado pelo formulário VBA (botão "Comparações"), que passa o
        /// Seq do item selecionado no ListBox. Independe de célula/aba ativa.
        /// </summary>
        public static AvaliacaoLaudo LerPorSeq(int seq, out string erro)
        {
            erro = null;
            var app = Globals.ThisAddIn.Application;

            Excel.Worksheet aba = AbaAvaliacoes(app);
            if (aba == null) { erro = "Não foi possível localizar a aba \"Avaliações\"."; return null; }

            int last = ((Excel.Range)aba.Cells[aba.Rows.Count, 1]).End[Excel.XlDirection.xlUp].Row;
            if (last < 2) { erro = "Não há avaliações cadastradas."; return null; }

            var dados = LerBloco(aba, last);
            if (dados == null) { erro = "Não foi possível ler os dados da avaliação."; return null; }

            // Procura o índice cuja coluna A (Seq, offset 0) == seq.
            int idxAtual = -1;
            for (int idx = 1; idx <= last - 1; idx++)
            {
                if ((int)Math.Round(Num(dados, idx, 0)) == seq) { idxAtual = idx; break; }
            }
            if (idxAtual < 0) { erro = "Avaliação não encontrada (Seq " + seq + ")."; return null; }

            var atual = Montar(dados, last, idxAtual);
            if (atual == null) { erro = "A avaliação selecionada está incompleta."; return null; }
            return atual;
        }

        /// <summary>
        /// Coleta a avaliação cujo <c>Seq</c> (coluna A) corresponde ao valor informado, lendo TODAS
        /// as colunas (A–CG) para o laudo detalhado de avaliação única (estilo Relaxmedic).
        /// Não monta <c>Serie</c> (laudo de uma única avaliação).
        /// </summary>
        public static AvaliacaoLaudo LerCompletaPorSeq(int seq, out string erro)
        {
            erro = null;
            var app = Globals.ThisAddIn.Application;

            Excel.Worksheet aba = AbaAvaliacoes(app);
            if (aba == null) { erro = "Não foi possível localizar a aba \"Avaliações\"."; return null; }

            int last = ((Excel.Range)aba.Cells[aba.Rows.Count, 1]).End[Excel.XlDirection.xlUp].Row;
            if (last < 2) { erro = "Não há avaliações cadastradas."; return null; }

            var rng = aba.Range[(Excel.Range)aba.Cells[2, 1], (Excel.Range)aba.Cells[last, COL_MAX_FULL]];
            var dados = rng.Value2 as object[,];
            if (dados == null) { erro = "Não foi possível ler os dados da avaliação."; return null; }

            int idxAtual = -1;
            for (int idx = 1; idx <= last - 1; idx++)
            {
                if ((int)Math.Round(Num(dados, idx, 0)) == seq) { idxAtual = idx; break; }
            }
            if (idxAtual < 0) { erro = "Avaliação não encontrada (Seq " + seq + ")."; return null; }

            var a = LerLinhaCompleta(dados, idxAtual);
            if (a == null || string.IsNullOrWhiteSpace(a.Nome)) { erro = "A avaliação selecionada está incompleta."; return null; }
            return a;
        }

        /// <summary>Linha completa: base (<see cref="LerLinha"/>) + campos extras do laudo único.</summary>
        private static AvaliacaoLaudo LerLinhaCompleta(object[,] dados, int idx)
        {
            var a = LerLinha(dados, idx);
            if (a == null) return null;

            a.TaxaMuscular = Num(dados, idx, OFF_TAXA_MUSC);
            a.TaxaMuscularClassif = Txt(dados, idx, OFF_TAXA_MUSC_CL);
            a.PesoMuscular = Num(dados, idx, OFF_PESO_MUSC);
            a.PesoMuscularClassif = Txt(dados, idx, OFF_PESO_MUSC_CL);
            a.MassaLivrePct = Num(dados, idx, OFF_MLG_PCT);
            a.GorduraSubcutaneaPct = Num(dados, idx, OFF_SUBCUT_PCT);
            a.SubcutaneaClassif = Txt(dados, idx, OFF_SUBCUT_CL);
            a.AguaKg = Num(dados, idx, OFF_AGUA);
            a.AguaClassif = Txt(dados, idx, OFF_AGUA_CL);
            a.ProteinaKg = Num(dados, idx, OFF_PROT);
            a.ProteinaClassif = Txt(dados, idx, OFF_PROT_CL);
            a.OsseaPct = Num(dados, idx, OFF_OSSEA_PCT);
            a.PesoOsseo = Num(dados, idx, OFF_PESO_OSSEO);
            a.OsseaClassif = Txt(dados, idx, OFF_OSSEA_CL);
            a.PesoResidual = Num(dados, idx, OFF_PESO_RESID);
            a.ResidualPct = Num(dados, idx, OFF_RESID_PCT);
            a.IdadeCorporalClassif = Txt(dados, idx, OFF_IDCORP_CL);
            a.GorduraAlvoPct = Num(dados, idx, OFF_GORD_ALVO);
            a.ImcAlvo = Num(dados, idx, OFF_IMC_ALVO);
            a.PesoAlvo = Num(dados, idx, OFF_PESO_ALVO);
            a.ControlePeso = Num(dados, idx, OFF_CTRL_PESO);
            a.TmbClassif = Txt(dados, idx, OFF_TMB_CL);
            a.PesoTotalClassif = Txt(dados, idx, OFF_PESO_TOTAL_CL);

            a.BracoDirPct = Num(dados, idx, OFF_BRACO_D_PCT);
            a.BracoDirKg = Num(dados, idx, OFF_BRACO_D_KG);
            a.BracoEsqPct = Num(dados, idx, OFF_BRACO_E_PCT);
            a.BracoEsqKg = Num(dados, idx, OFF_BRACO_E_KG);
            a.AbsPct = Num(dados, idx, OFF_ABS_PCT);
            a.AbsKg = Num(dados, idx, OFF_ABS_KG);
            a.PernaDirPct = Num(dados, idx, OFF_PERNA_D_PCT);
            a.PernaDirKg = Num(dados, idx, OFF_PERNA_D_KG);
            a.PernaEsqPct = Num(dados, idx, OFF_PERNA_E_PCT);
            a.PernaEsqKg = Num(dados, idx, OFF_PERNA_E_KG);

            a.Pontuacao = Num(dados, idx, OFF_PONTUACAO);

            a.Z20BracoD = Num(dados, idx, OFF_Z20_BRD);
            a.Z20BracoE = Num(dados, idx, OFF_Z20_BRE);
            a.Z20Tronco = Num(dados, idx, OFF_Z20_TR);
            a.Z20PernaD = Num(dados, idx, OFF_Z20_PD);
            a.Z20PernaE = Num(dados, idx, OFF_Z20_PE);
            a.Z100BracoD = Num(dados, idx, OFF_Z100_BRD);
            a.Z100BracoE = Num(dados, idx, OFF_Z100_BRE);
            a.Z100Tronco = Num(dados, idx, OFF_Z100_TR);
            a.Z100PernaD = Num(dados, idx, OFF_Z100_PD);
            a.Z100PernaE = Num(dados, idx, OFF_Z100_PE);

            a.MuscBracoDirPct = Num(dados, idx, OFF_MUSC_BRD_PCT);
            a.MuscBracoDirKg = Num(dados, idx, OFF_MUSC_BRD_KG);
            a.MuscBracoEsqPct = Num(dados, idx, OFF_MUSC_BRE_PCT);
            a.MuscBracoEsqKg = Num(dados, idx, OFF_MUSC_BRE_KG);
            a.MuscAbsPct = Num(dados, idx, OFF_MUSC_ABS_PCT);
            a.MuscAbsKg = Num(dados, idx, OFF_MUSC_ABS_KG);
            a.MuscPernaDirPct = Num(dados, idx, OFF_MUSC_PD_PCT);
            a.MuscPernaDirKg = Num(dados, idx, OFF_MUSC_PD_KG);
            a.MuscPernaEsqPct = Num(dados, idx, OFF_MUSC_PE_PCT);
            a.MuscPernaEsqKg = Num(dados, idx, OFF_MUSC_PE_KG);

            a.Hora = HoraDe(dados, idx, OFF_HORA);

            return a;
        }

        /// <summary>Worksheet cujo nome começa com "Avalia" na pasta ativa (ou null).</summary>
        private static Excel.Worksheet AbaAvaliacoes(Excel.Application app)
        {
            try
            {
                var wb = app.ActiveWorkbook;
                if (wb == null) return null;
                foreach (Excel.Worksheet ws in wb.Worksheets)
                    if (ws.Name.StartsWith("Avalia", StringComparison.OrdinalIgnoreCase)) return ws;
            }
            catch { }
            return null;
        }

        /// <summary>Lê o bloco A2:COL_MAX da aba numa única chamada COM (Value2, array 1-based).</summary>
        private static object[,] LerBloco(Excel.Worksheet aba, int last)
        {
            var rng = aba.Range[(Excel.Range)aba.Cells[2, 1], (Excel.Range)aba.Cells[last, COL_MAX]];
            return rng.Value2 as object[,];
        }

        /// <summary>Monta a avaliação do índice <paramref name="idxAtual"/> + sua série de evolução.</summary>
        private static AvaliacaoLaudo Montar(object[,] dados, int last, int idxAtual)
        {
            var atual = LerLinha(dados, idxAtual);
            if (atual == null || string.IsNullOrWhiteSpace(atual.Nome)) return null;
            atual.Conclusao = Txt(dados, idxAtual, OFF_CONCLUSAO);
            atual.Serie = MontarSerie(dados, last, atual, idxAtual);
            return atual;
        }

        /// <summary>
        /// Série de evolução: avaliações do mesmo Id com data ≤ a da selecionada, em ordem
        /// cronológica. Se houver mais de 6, mantém a primeira + as 5 mais recentes (a última = atual).
        /// </summary>
        private static List<AvaliacaoLaudo> MontarSerie(object[,] dados, int last, AvaliacaoLaudo atual, int idxAtual)
        {
            if (atual.Data == null) return new List<AvaliacaoLaudo> { atual };

            var todas = new List<AvaliacaoLaudo>();
            for (int idx = 1; idx <= last - 1; idx++)
            {
                int id = (int)Math.Round(Num(dados, idx, OFF_ID));
                if (id != atual.Id) continue;
                DateTime? d = DataDe(dados, idx, OFF_DATA);
                if (d == null || d.Value > atual.Data.Value) continue;
                todas.Add(idx == idxAtual ? atual : LerLinha(dados, idx));
            }

            todas = todas.OrderBy(x => x.Data ?? DateTime.MinValue).ToList();
            if (todas.Count <= 6) return todas;

            // Primeira + as 5 mais recentes (6 colunas; pode haver salto entre a 1ª e a 2ª).
            var sel = new List<AvaliacaoLaudo> { todas[0] };
            sel.AddRange(todas.Skip(todas.Count - 5));
            return sel;
        }

        private static AvaliacaoLaudo LerLinha(object[,] dados, int idx)
        {
            if (idx < dados.GetLowerBound(0) || idx > dados.GetUpperBound(0)) return null;

            var a = new AvaliacaoLaudo
            {
                Id = (int)Math.Round(Num(dados, idx, OFF_ID)),
                Nome = Txt(dados, idx, OFF_NOME),
                Sexo = Txt(dados, idx, OFF_SEXO),
                Idade = (int)Math.Round(Num(dados, idx, OFF_IDADE)),
                AlturaCm = Num(dados, idx, OFF_ALTURA),
                Data = DataDe(dados, idx, OFF_DATA),

                Peso = Num(dados, idx, OFF_PESO),
                Imc = Num(dados, idx, OFF_IMC),
                GorduraPct = Num(dados, idx, OFF_GORD_PCT),
                GorduraKg = Num(dados, idx, OFF_GORD_KG),
                MassaMagraKg = Num(dados, idx, OFF_MLG_KG),
                MusculoEsqPct = Num(dados, idx, OFF_MUSC_PCT),
                MusculoEsqKg = Num(dados, idx, OFF_MUSC_KG),
                GorduraVisceral = Num(dados, idx, OFF_VISC),
                IdadeCorporal = Num(dados, idx, OFF_IDCORP),
                Rcq = Num(dados, idx, OFF_RCQ),
                Tmb = Num(dados, idx, OFF_TMB),
                Get = Num(dados, idx, OFF_GET),

                ImcClassif = Txt(dados, idx, OFF_IMC_CL),
                GorduraClassif = Txt(dados, idx, OFF_GORD_CL),
                MusculoClassif = Txt(dados, idx, OFF_MUSC_CL),
                VisceralClassif = Txt(dados, idx, OFF_VISC_CL),
                RcqClassif = Txt(dados, idx, OFF_RCQ_CL),

                ImcIntervalo = Txt(dados, idx, OFF_INT_IMC),
                GorduraIntervalo = Txt(dados, idx, OFF_INT_GORD),
                MusculoIntervalo = Txt(dados, idx, OFF_INT_MUSC),
                VisceralIntervalo = Txt(dados, idx, OFF_INT_VISC),
                RcqIntervalo = Txt(dados, idx, OFF_INT_RCQ),

                CircPeitoral = Num(dados, idx, OFF_PEITORAL),
                CircAbdomen = Num(dados, idx, OFF_ABDOMEN),
                CircQuadril = Num(dados, idx, OFF_QUADRIL),
            };

            // Massa magra = Peso - Massa gorda (definição). Deriva quando a coluna vier vazia/zerada.
            if (a.MassaMagraKg <= 0 && a.Peso > 0 && a.GorduraKg > 0)
                a.MassaMagraKg = Math.Round(a.Peso - a.GorduraKg, 2);

            return a;
        }

        // --- Conversores tolerantes (Value2 vem como double p/ números e datas) ---

        private static double Num(object[,] a, int r, int c)
        {
            object v = a[r, c + 1];
            if (v == null) return 0;
            if (v is double) return (double)v;
            double d;
            if (double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            if (double.TryParse(v.ToString(), NumberStyles.Any, new CultureInfo("pt-BR"), out d)) return d;
            return 0;
        }

        private static string Txt(object[,] a, int r, int c)
        {
            object v = a[r, c + 1];
            return v == null ? "" : v.ToString().Trim();
        }

        /// <summary>Hora "HH:mm": texto direto, ou fração de dia (OADate) se a célula vier como hora.</summary>
        private static string HoraDe(object[,] a, int r, int c)
        {
            object v = a[r, c + 1];
            if (v == null) return "";
            if (v is double)
            {
                try { return DateTime.FromOADate((double)v).ToString("HH:mm"); } catch { return ""; }
            }
            return v.ToString().Trim();
        }

        private static DateTime? DataDe(object[,] a, int r, int c)
        {
            object v = a[r, c + 1];
            if (v == null) return null;
            if (v is double) { try { return DateTime.FromOADate((double)v); } catch { return null; } }
            DateTime dt;
            return DateTime.TryParse(v.ToString(), out dt) ? dt : (DateTime?)null;
        }
    }
}
