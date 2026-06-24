using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace SistemaPSM.AddIn.Laudo
{
    /// <summary>
    /// Monta o HTML do laudo de avaliação única seguindo o modelo "laudo-2" (portado para CSS
    /// estático compatível com wkhtmltopdf). Campos sem dado na planilha aparecem como "-".
    /// </summary>
    public static class LaudoAvaliacaoHtmlBuilder
    {
        private static readonly CultureInfo BR = new CultureInfo("pt-BR");
        private static readonly CultureInfo INV = CultureInfo.InvariantCulture;
        private const string Dash = "-";
        private const string TemplateRes = "SistemaPSM.AddIn.Laudo.TemplateAvaliacao.html";
        private const string LogoRes = "SistemaPSM.AddIn.Laudo.logo.jpg";
        private const string Seg1Res = "SistemaPSM.AddIn.Laudo.seg1.png";
        private const string Seg2Res = "SistemaPSM.AddIn.Laudo.seg2.png";

        public static string MontarHtml(AvaliacaoLaudo a, string playfairUri, string interUri, string logoDataUri)
        {
            string template = LerRecurso(TemplateRes);
            string fonts =
                "@font-face{font-family:'Playfair Display';src:url('" + playfairUri + "');font-weight:400 900;font-style:normal;}\n" +
                "@font-face{font-family:'Inter';src:url('" + interUri + "');font-weight:100 900;font-style:normal;}";

            var c = new StringBuilder();
            Topo(c, a, logoDataUri);

            c.Append("<div class=\"main\"><div class=\"left\">");
            Composicao(c, a);
            GorduraMuscular(c, a);
            ObesidadeMedidores(c, a);
            Segmentar(c, a);
            c.Append("</div><div class=\"right\">");
            PontuacaoControle(c, a);
            ObesidadeBarras(c, a);
            TipoCorpo(c, a);
            c.Append("</div></div>");

            Rodape(c, a);

            return template.Replace("{{FONTS}}", fonts).Replace("{{CONTEUDO}}", c.ToString());
        }

        // ---------- Cabeçalho ----------
        private static void Topo(StringBuilder c, AvaliacaoLaudo a, string logoDataUri)
        {
            string logo = !string.IsNullOrEmpty(logoDataUri) ? logoDataUri : ImgDataUri(LogoRes, "image/jpeg");
            c.Append("<div class=\"topo\">");
            if (logo.Length > 0) c.Append("<img class=\"logo\" src=\"").Append(logo).Append("\" />");
            c.Append("<h1>Relatório de análise de composição corporal</h1></div>");

            string data = a.Data.HasValue ? a.Data.Value.ToString("dd/MM/yyyy", BR) : Dash;
            c.Append("<div class=\"ident\">");
            c.Append("<div class=\"c\">").Append(Esc(Maiusc(a.Nome))).Append("</div>");
            c.Append("<div class=\"c\">Sexo: <span>").Append(Esc(string.IsNullOrEmpty(a.Sexo) ? Dash : a.Sexo)).Append("</span></div>");
            c.Append("<div class=\"c\">Idade: <span>").Append(a.Idade > 0 ? a.Idade + " anos" : Dash).Append("</span></div>");
            c.Append("<div class=\"c\">Altura: <span>").Append(a.AlturaCm > 0 ? Fmt(a.AlturaCm, 0) + " cm" : Dash).Append("</span></div>");
            string hora = string.IsNullOrEmpty(a.Hora) ? "" : " " + Esc(a.Hora);
            c.Append("<div class=\"c\">Avaliação: <span>").Append(data).Append(hora).Append("</span></div>");
            c.Append("</div>");
        }

        // ---------- Composição ----------
        private static void Composicao(StringBuilder c, AvaliacaoLaudo a)
        {
            c.Append("<div class=\"sec\"><h2>Análise da composição corporal</h2>");
            c.Append("<table class=\"t\"><thead><tr><th>Componente</th><th class=\"ctr\">Medição (kg)</th><th class=\"ctr\">Proporção de peso (%)</th><th class=\"ctr\">Avaliação</th></tr></thead><tbody>");
            int i = 0;
            LinhaComp(c, ref i, "Peso", a.Peso, 100, a.PesoTotalClassif);
            LinhaComp(c, ref i, "Gordura corporal", a.GorduraKg, Prop(a.GorduraKg, a.Peso), a.GorduraClassif);
            LinhaComp(c, ref i, "Sal inorgânico", a.PesoOsseo, Prop(a.PesoOsseo, a.Peso), a.OsseaClassif);
            LinhaComp(c, ref i, "Proteína", a.ProteinaKg, Prop(a.ProteinaKg, a.Peso), a.ProteinaClassif);
            LinhaComp(c, ref i, "Água corporal", a.AguaKg, Prop(a.AguaKg, a.Peso), a.AguaClassif);
            LinhaComp(c, ref i, "Músculo", a.PesoMuscular, a.TaxaMuscular > 0 ? a.TaxaMuscular : Prop(a.PesoMuscular, a.Peso), a.PesoMuscularClassif);
            LinhaComp(c, ref i, "Músculo esquelético", a.MusculoEsqKg, a.MusculoEsqPct > 0 ? a.MusculoEsqPct : Prop(a.MusculoEsqKg, a.Peso), a.MusculoClassif);
            c.Append("</tbody></table></div>");
        }

        private static void LinhaComp(StringBuilder c, ref int i, string nome, double kg, double prop, string classif)
        {
            c.Append("<tr").Append(i % 2 == 0 ? " class=\"alt\"" : "").Append("><td class=\"nome\">").Append(Esc(nome)).Append("</td>");
            c.Append("<td class=\"ctr\">").Append(Dn(kg, 2)).Append("</td>");
            c.Append("<td class=\"ctr\">").Append(prop > 0 ? Fmt(prop, 2) : Dash).Append("</td>");
            c.Append("<td class=\"ctr\">").Append(Cls(classif)).Append("</td></tr>");
            i++;
        }

        private static double Prop(double kg, double peso) { return (kg > 0 && peso > 0) ? kg / peso * 100.0 : 0; }

        // ---------- Medidores (barras com marcador) ----------
        private class Bar
        {
            public string Lbl; public double V, Min, Max; public int Casas; public string Classif;
            public Bar(string l, double v, double min, double max, int casas, string classif)
            { Lbl = l; V = v; Min = min; Max = max; Casas = casas; Classif = classif; }
        }

        private static void GorduraMuscular(StringBuilder c, AvaliacaoLaudo a)
        {
            Medidores(c, "Análise de gordura muscular",
                new[] { "Baixo", "Saudável", "Alto" }, new[] { 25, 25, 50 }, 1,
                new[]
                {
                    new Bar("Peso (kg)", a.Peso, 0, 150, 1, null),
                    new Bar("Músculo esquelético (kg)", a.MusculoEsqKg, 0, 50, 1, null),
                    new Bar("Massa gorda (kg)", a.GorduraKg, 0, 50, 1, null),
                });
        }

        private static void ObesidadeMedidores(StringBuilder c, AvaliacaoLaudo a)
        {
            Medidores(c, "Análise de obesidade",
                new[] { "Abaixo", "Saudável", "Alto", "Alto risco" }, new[] { 25, 25, 25, 25 }, 1,
                new[]
                {
                    new Bar("IMC (kg/m²)", a.Imc, 10, 45, 1, a.ImcClassif),
                    new Bar("Taxa de gordura corporal (%)", a.GorduraPct, 0, 60, 1, a.GorduraClassif),
                });
        }

        private static void Medidores(StringBuilder c, string titulo, string[] zonaLbl, int[] zonaW, int healthy, Bar[] bars)
        {
            c.Append("<div class=\"sec\"><h2>").Append(Esc(titulo)).Append("</h2><table class=\"mf\">");
            // legenda de zonas
            c.Append("<tr><td class=\"mf-lbl\"></td><td><table class=\"zones\"><tr>");
            for (int z = 0; z < zonaLbl.Length; z++)
                c.Append("<td class=\"").Append(z == healthy ? "z2" : "z1").Append("\" width=\"").Append(zonaW[z]).Append("%\">").Append(Esc(zonaLbl[z])).Append("</td>");
            c.Append("</tr></table></td><td class=\"mf-val\"></td></tr>");
            // dividers acumulados
            var divs = new List<int>(); int cum = 0;
            for (int z = 0; z < zonaW.Length - 1; z++) { cum += zonaW[z]; divs.Add(cum); }

            foreach (var b in bars)
            {
                c.Append("<tr><td class=\"mf-lbl\">").Append(Esc(b.Lbl)).Append("</td><td><div class=\"track\">");
                foreach (int d in divs) c.Append("<div class=\"div\" style=\"left:").Append(d).Append("%\"></div>");
                if (b.V > 0)
                {
                    double p = Clamp((b.V - b.Min) / (b.Max - b.Min), 0, 1) * 100.0;
                    c.Append("<div class=\"marker\" style=\"left:").Append(p.ToString("0.#", INV)).Append("%\"></div>");
                }
                c.Append("</div></td><td class=\"mf-val\">");
                if (b.V > 0)
                {
                    c.Append(Fmt(b.V, b.Casas));
                    string cl = Cls(b.Classif);
                    if (cl != Dash) c.Append(" (").Append(cl).Append(")");
                }
                else c.Append(Dash);
                c.Append("</td></tr>");
            }
            c.Append("</table></div>");
        }

        // ---------- Segmentar ----------
        private static void Segmentar(StringBuilder c, AvaliacaoLaudo a)
        {
            c.Append("<table class=\"segwrap\"><tr><td>");
            FiguraSeg(c, "Análise de gordura segmentar", Seg1Res,
                a.BracoDirKg, a.BracoDirPct, a.BracoEsqKg, a.BracoEsqPct, a.AbsKg, a.AbsPct, a.PernaDirKg, a.PernaDirPct, a.PernaEsqKg, a.PernaEsqPct);
            c.Append("</td><td>");
            FiguraSeg(c, "Equilíbrio muscular", Seg2Res,
                a.MuscBracoDirKg, a.MuscBracoDirPct, a.MuscBracoEsqKg, a.MuscBracoEsqPct, a.MuscAbsKg, a.MuscAbsPct, a.MuscPernaDirKg, a.MuscPernaDirPct, a.MuscPernaEsqKg, a.MuscPernaEsqPct);
            c.Append("</td></tr></table>");
        }

        private static void FiguraSeg(StringBuilder c, string titulo, string imgRes,
            double bdk, double bdp, double bek, double bep, double trk, double trp,
            double pdk, double pdp, double pek, double pep)
        {
            c.Append("<div class=\"sec\"><h2>").Append(Esc(titulo)).Append("</h2>");
            c.Append("<table class=\"fig\"><tr><td>");
            SegLbl(c, "Braço D", bdk, bdp, false);
            SegLbl(c, "Tronco", trk, trp, false);
            SegLbl(c, "Perna D", pdk, pdp, false);
            c.Append("</td><td class=\"img\"><img src=\"").Append(ImgDataUri(imgRes, "image/png")).Append("\" /></td><td>");
            SegLbl(c, "Braço E", bek, bep, true);
            SegLbl(c, "Perna E", pek, pep, true);
            c.Append("</td></tr></table>");
            c.Append("<div class=\"nota\">Intervalo padrão 80%~160% (gordura) / 90%~110% (músculo). Valor segmentar é inferido.</div></div>");
        }

        private static void SegLbl(StringBuilder c, string lbl, double kg, double pct, bool dir)
        {
            c.Append("<div class=\"seglbl").Append(dir ? " r" : "").Append("\">").Append(Esc(lbl))
             .Append("<br/><b>").Append(Dn(kg, 1, "kg")).Append("</b><br/>").Append(pct > 0 ? Fmt(pct, 1) + "%" : Dash).Append("</div>");
        }

        // ---------- Pontuação + controle ----------
        private static void PontuacaoControle(StringBuilder c, AvaliacaoLaudo a)
        {
            c.Append("<div class=\"sec box\"><h2>Pontuação corporal</h2>");
            c.Append("<div class=\"score\"><div class=\"big\">").Append(a.Pontuacao > 0 ? Fmt(a.Pontuacao, 0) : Dash).Append("<small> /100 Pontos</small></div>");
            c.Append("<div class=\"obs\">* A pontuação total reflete o valor avaliado da composição corporal. Uma pessoa musculosa pode obter mais de 100 pontos.</div></div>");
            c.Append("<div class=\"subh\">Controle de peso</div><table class=\"kv\">");
            Kv(c, "Peso alvo", Dn(a.PesoAlvo, 1, "kg"), false);

            // Controle de peso (kg): valor armazenado ou, na falta, Peso alvo − Peso atual.
            bool temPesoCtrl = a.ControlePeso != 0 || (a.PesoAlvo > 0 && a.Peso > 0);
            double pesoCtrl = a.ControlePeso != 0 ? a.ControlePeso
                            : ((a.PesoAlvo > 0 && a.Peso > 0) ? a.PesoAlvo - a.Peso : 0);
            Kv(c, "Controle de peso", temPesoCtrl ? SignKg(pesoCtrl) : Dash, false);

            // Controle de gordura (kg) = (Peso alvo × % gordura alvo) − gordura atual.
            bool temGord = a.PesoAlvo > 0 && a.GorduraAlvoPct > 0 && a.GorduraKg > 0;
            double ctrlGord = temGord ? a.PesoAlvo * a.GorduraAlvoPct / 100.0 - a.GorduraKg : 0;
            Kv(c, "Controle de gordura", temGord ? SignKg(ctrlGord) : Dash, false);

            // Controle muscular (kg) = Controle de peso − Controle de gordura (identidade InBody/Relaxmedic).
            bool temMusc = temGord && temPesoCtrl;
            Kv(c, "Controle muscular", temMusc ? SignKg(pesoCtrl - ctrlGord) : Dash, false);
            c.Append("</table></div>");
        }

        // ---------- Avaliação da obesidade (barras preenchidas) ----------
        private static void ObesidadeBarras(StringBuilder c, AvaliacaoLaudo a)
        {
            c.Append("<div class=\"sec\"><h2>Avaliação da obesidade</h2>");
            ObBar(c, "IMC", a.Imc, 52, new[] { "Abaixo", "Saudável", "Alto", "Alto risco" });
            ObBar(c, "Taxa de gordura corporal", a.GorduraPct, 68, new[] { "Abaixo", "Saudável", "Alto", "Alto risco" });

            double obes = (a.PesoAlvo > 0 && a.Peso > 0) ? a.Peso / a.PesoAlvo * 100.0 : 0;
            c.Append("<div class=\"ob\"><div class=\"ob-top\"><div class=\"a\">Obesidade (peso atual/peso alvo)</div><div class=\"b\">")
             .Append(obes > 0 ? Fmt(obes, 0) + "%" : Dash).Append("</div></div>");
            c.Append("<table class=\"z3\"><tr><td class=\"a\">Baixo</td><td class=\"b\">Normal</td><td class=\"c\">Alto</td></tr></table></div>");
            c.Append("</div>");
        }

        private static void ObBar(StringBuilder c, string lbl, double v, double escMax, string[] zonas)
        {
            c.Append("<div class=\"ob\"><div class=\"ob-top\"><div class=\"a\">").Append(Esc(lbl)).Append("</div><div class=\"b\">")
             .Append(v > 0 ? Fmt(v, 1) : Dash).Append("</div></div>");
            double w = v > 0 ? Clamp(v / escMax, 0, 1) * 100.0 : 0;
            c.Append("<div class=\"ob-track\"><div class=\"ob-fill\" style=\"width:").Append(w.ToString("0.#", INV)).Append("%\"></div></div>");
            c.Append("<div class=\"ob-zones\">");
            for (int i = 0; i < zonas.Length; i++)
                c.Append("<span").Append(i == zonas.Length - 1 ? " class=\"r\"" : "").Append(">").Append(Esc(zonas[i])).Append("</span>");
            c.Append("</div></div>");
        }

        // ---------- Tipo de corpo ----------
        // Treemap estilo Relaxmedic: eixo Y = IMC (4 faixas: >25 / 18.5–25 / 16–18.5 / <16),
        // eixo X = % de gordura (3 faixas, limites por sexo). "Obesidade invisível" ocupa as
        // duas faixas de IMC baixo na coluna de gordura alta (rowspan=2). Marcadores numéricos
        // só para referência visual (Y: 18.5/25 · X: 18/28).
        private static void TipoCorpo(StringBuilder c, AvaliacaoLaudo a)
        {
            int ri = -1, ci = -1;
            if (a.Imc > 0) ri = a.Imc >= 25 ? 0 : (a.Imc >= 18.5 ? 1 : (a.Imc >= 16 ? 2 : 3));
            if (a.GorduraPct > 0)
            {
                string sx = (a.Sexo ?? "").ToLowerInvariant();
                bool fem = sx.Contains("mulh") || sx.Contains("femin") || sx.StartsWith("f");
                double g = a.GorduraPct;
                if (fem) ci = g < 21 ? 0 : (g <= 33 ? 1 : 2);
                else ci = g < 8 ? 0 : (g <= 20 ? 1 : 2);
            }
            // gordura alta + IMC muito baixo cai na célula mesclada "Obesidade invisível"
            int onR = ri, onC = ci;
            if (ri == 3 && ci == 2) { onR = 2; onC = 2; }

            c.Append("<div class=\"sec\"><h2>Avaliação do tipo de corpo</h2>");
            c.Append("<table class=\"tc-outer\"><tr>");
            // eixo Y (IMC) — números no topo da faixa correspondente
            c.Append("<td class=\"tc-yax\"><table class=\"tc-y\">");
            c.Append("<tr style=\"height:38px\"><td>&nbsp;</td></tr>");
            c.Append("<tr style=\"height:38px\"><td>25</td></tr>");
            c.Append("<tr style=\"height:30px\"><td>18.5</td></tr>");
            c.Append("<tr style=\"height:30px\"><td>&nbsp;</td></tr>");
            c.Append("</table></td>");
            // grade do tipo de corpo
            c.Append("<td class=\"tc-main\"><table class=\"tc\">");
            c.Append("<tr style=\"height:38px\">")
             .Append("<td").Append(OnCls(onR, onC, 0, 0)).Append(">Atletas</td>")
             .Append("<td").Append(OnCls(onR, onC, 0, 1)).Append(">Ligeiramente obeso</td>")
             .Append("<td").Append(OnCls(onR, onC, 0, 2)).Append(">Obesidade</td></tr>");
            c.Append("<tr style=\"height:38px\">")
             .Append("<td").Append(OnCls(onR, onC, 1, 0)).Append(">Músculo</td>")
             .Append("<td").Append(OnCls(onR, onC, 1, 1)).Append(">Saudável</td>")
             .Append("<td").Append(OnCls(onR, onC, 1, 2)).Append(">Sobrepeso</td></tr>");
            c.Append("<tr style=\"height:30px\">")
             .Append("<td").Append(OnCls(onR, onC, 2, 0)).Append(">Muscular magro</td>")
             .Append("<td").Append(OnCls(onR, onC, 2, 1)).Append(">Magro</td>")
             .Append("<td rowspan=\"2\"").Append(OnCls(onR, onC, 2, 2)).Append(">Obesidade invisível</td></tr>");
            c.Append("<tr style=\"height:30px\">")
             .Append("<td").Append(OnCls(onR, onC, 3, 0)).Append(">Baixo peso severo</td>")
             .Append("<td").Append(OnCls(onR, onC, 3, 1)).Append(">Abaixo do peso</td></tr>");
            c.Append("</table>");
            // eixo X (% de gordura) — números nas divisórias das colunas
            c.Append("<table class=\"tc-x\"><tr><td>18</td><td>28</td><td></td></tr></table>");
            c.Append("</td></tr></table>");
            c.Append("<div class=\"tc-cap\">eixo X: % de gordura corporal · eixo Y: IMC (kg/m²)</div></div>");
        }

        /// <summary>Classe CSS da célula realçada do treemap de tipo de corpo (vazia se não for a célula ativa).</summary>
        private static string OnCls(int onR, int onC, int r, int col)
            => (r == onR && col == onC) ? " class=\"on\"" : "";

        // ---------- Rodapé ----------
        private static void Rodape(StringBuilder c, AvaliacaoLaudo a)
        {
            c.Append("<div class=\"foot\"><div class=\"fc\">");
            c.Append("<div class=\"sec\"><h2>Impedância bioelétrica</h2>");
            c.Append("<table class=\"t imp\"><thead><tr><th>Impedância<br>(Ω)</th><th class=\"ctr\">Braço<br>direito</th><th class=\"ctr\">Braço<br>esquerdo</th><th class=\"ctr\">Tronco<br>&nbsp;</th><th class=\"ctr\">Perna<br>direita</th><th class=\"ctr\">Perna<br>esquerda</th></tr></thead><tbody>");
            LinhaZ(c, "20 kHz", true, a.Z20BracoD, a.Z20BracoE, a.Z20Tronco, a.Z20PernaD, a.Z20PernaE);
            LinhaZ(c, "100 kHz", false, a.Z100BracoD, a.Z100BracoE, a.Z100Tronco, a.Z100PernaD, a.Z100PernaE);
            c.Append("</tbody></table></div>");
            c.Append("</div><div class=\"fc\">");
            c.Append("<div class=\"sec\"><h2>Outros indicadores</h2><table class=\"kv\">");
            Kv(c, "Grau de gordura visceral", Dn(a.GorduraVisceral, 0), false);
            Kv(c, "Taxa metabólica basal", Dn(a.Tmb, 0, "kcal"), false);
            Kv(c, "Peso corporal livre de gordura", Dn(a.MassaMagraKg, 1, "kg"), false);
            Kv(c, "Gordura subcutânea", a.GorduraSubcutaneaPct > 0 ? Fmt(a.GorduraSubcutaneaPct, 1) + "%" : Dash, false);
            Kv(c, "SMI", a.Smi > 0 ? Fmt(a.Smi, 1) + " kg/m²" : Dash, false);
            Kv(c, "Idade do corpo", Dn(a.IdadeCorporal, 0, "anos"), false);
            c.Append("</table></div></div></div>");
        }

        // ---------- Helpers ----------
        private static void Kv(StringBuilder c, string k, string v, bool ac)
        {
            c.Append("<tr><td>").Append(Esc(k)).Append("</td><td class=\"v").Append(ac ? " ac" : "").Append("\">").Append(v).Append("</td></tr>");
        }

        /// <summary>Linha da tabela de impedância (frequência + 5 segmentos Z em Ω; "-" se ausente).</summary>
        private static void LinhaZ(StringBuilder c, string freq, bool alt, double brd, double bre, double tr, double pd, double pe)
        {
            c.Append("<tr").Append(alt ? " class=\"alt\"" : "").Append("><td class=\"nome\">").Append(freq).Append("</td>");
            foreach (double v in new[] { brd, bre, tr, pd, pe })
                c.Append("<td class=\"ctr\">").Append(v > 0 ? Fmt(v, 0) : Dash).Append("</td>");
            c.Append("</tr>");
        }

        private static string Dn(double v, int casas) { return v > 0 ? Fmt(v, casas) : Dash; }
        private static string Dn(double v, int casas, string un) { return v > 0 ? Fmt(v, casas) + " " + un : Dash; }
        private static string SignKg(double v) { return (v > 0 ? "+" : "") + Fmt(v, 1) + " kg"; }
        private static double Clamp(double v, double lo, double hi) { return v < lo ? lo : (v > hi ? hi : v); }
        private static string Fmt(double v, int casas) { return v.ToString("N" + casas, BR); }

        private static string Cls(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Dash;
            s = s.Trim();
            if (s.Equals("Não Calculado", StringComparison.OrdinalIgnoreCase) || s.Equals("N/A", StringComparison.OrdinalIgnoreCase)) return Dash;
            return Esc(s);
        }

        private static string Maiusc(string s) { return string.IsNullOrEmpty(s) ? s : s.ToUpper(BR); }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string LerRecurso(string nome)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(nome))
            {
                if (s == null) throw new InvalidOperationException("Recurso não encontrado: " + nome);
                using (var r = new StreamReader(s, Encoding.UTF8)) return r.ReadToEnd();
            }
        }

        private static string ImgDataUri(string res, string mime)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream(res))
                {
                    if (s == null) return "";
                    using (var ms = new MemoryStream()) { s.CopyTo(ms); return "data:" + mime + ";base64," + Convert.ToBase64String(ms.ToArray()); }
                }
            }
            catch { return ""; }
        }
    }
}
