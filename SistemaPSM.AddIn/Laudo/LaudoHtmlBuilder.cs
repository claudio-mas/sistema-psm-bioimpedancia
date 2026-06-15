using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SistemaPSM.AddIn.Laudo
{
    /// <summary>
    /// Monta o HTML do laudo (modelo completo) a partir do Template.html embutido + dados.
    /// Posições CSS usam ponto decimal (InvariantCulture); valores exibidos usam vírgula (pt-BR).
    /// </summary>
    public static class LaudoHtmlBuilder
    {
        private static readonly CultureInfo BR = new CultureInfo("pt-BR");
        private static readonly CultureInfo INV = CultureInfo.InvariantCulture;
        private const string TemplateRes = "SistemaPSM.AddIn.Laudo.Template.html";
        private const string LogoRes = "SistemaPSM.AddIn.Laudo.logo.jpg";

        /// <param name="playfairUri">URI file:/// da fonte Playfair Display extraída.</param>
        /// <param name="interUri">URI file:/// da fonte Inter extraída.</param>
        public static string MontarHtml(AvaliacaoLaudo a, string playfairUri, string interUri, string logoDataUri)
        {
            string template = LerRecurso(TemplateRes);

            string fonts =
                "@font-face{font-family:'Playfair Display';src:url('" + playfairUri + "');font-weight:400 900;font-style:normal;}\n" +
                "@font-face{font-family:'Inter';src:url('" + interUri + "');font-weight:100 900;font-style:normal;}";

            var c = new StringBuilder();
            Cabecalho(c, a, logoDataUri);
            Circunferencias(c, a);
            Composicao(c, a);
            Evolucao(c, a);
            Conclusao(c, a);
            Informacao(c);

            return template
                .Replace("{{FONTS}}", fonts)
                .Replace("{{CONTEUDO}}", c.ToString());
        }

        // ---------- Seções ----------

        private static void Cabecalho(StringBuilder c, AvaliacaoLaudo a, string logoDataUri)
        {
            string data = a.Data.HasValue ? a.Data.Value.ToString("dd/MM/yyyy", BR) : "—";
            string impressao = DateTime.Now.ToString("dd/MM/yyyy", BR);

            c.Append("<div class=\"marca\">");
            string logo = !string.IsNullOrEmpty(logoDataUri) ? logoDataUri : LogoDataUri();
            if (logo.Length > 0) c.Append("<img class=\"marca-logo\" src=\"").Append(logo).Append("\" />");
            c.Append("<div class=\"marca-topo\">Laudo de Avaliação Física &middot; Composição Corporal</div>");
            c.Append("</div>");

            c.Append("<div class=\"cab\"><div class=\"cab-tabela\">");
            c.Append("<div class=\"cab-cel cab-nome\"><div class=\"rotulo\">Nome</div><div class=\"nome\">")
             .Append(Esc(Maiusc(a.Nome))).Append("</div></div>");
            CabMeta(c, "Idade", a.Idade > 0 ? a.Idade + " anos" : "—");
            CabMeta(c, "Altura", a.AlturaCm > 0 ? Fmt(a.AlturaCm, 0) + " cm" : "—");
            CabMeta(c, "Sexo", string.IsNullOrEmpty(a.Sexo) ? "—" : a.Sexo);
            CabMeta(c, "Data da Avaliação", data);
            CabMeta(c, "Impressão", impressao);
            c.Append("</div></div>");
        }

        private static void CabMeta(StringBuilder c, string rotulo, string valor)
        {
            c.Append("<div class=\"cab-cel cab-meta\"><div class=\"rotulo\">").Append(Esc(rotulo))
             .Append("</div><div class=\"v\">").Append(Esc(valor)).Append("</div></div>");
        }

        private static void Circunferencias(StringBuilder c, AvaliacaoLaudo a)
        {
            var serie = a.Serie;
            if (serie == null || serie.Count == 0) return;
            if (!serie.Any(x => x.CircPeitoral > 0 || x.CircAbdomen > 0 || x.CircQuadril > 0)) return;

            var linhas = new[]
            {
                new DefLinha("Peito", "cm", 1, x => x.CircPeitoral),
                new DefLinha("Barriga", "cm", 1, x => x.CircAbdomen),
                new DefLinha("Quadril", "cm", 1, x => x.CircQuadril),
            };
            GraficoSerie(c, "Circunferência", serie, linhas);
        }

        private static void Composicao(StringBuilder c, AvaliacaoLaudo a)
        {
            AbreSecao(c, "Composição Corporal");

            Barra(c, "Peso", "kg", a.Peso, 0, 150, 1, null, null);
            if (a.GorduraKg > 0) Barra(c, "Gordura corporal", "kg", a.GorduraKg, 0, 50, 1, null, null);
            if (a.MassaMagraKg > 0) Barra(c, "Massa magra", "kg", a.MassaMagraKg, 0, 100, 1, null, null);
            if (a.MusculoEsqKg > 0) Barra(c, "Músculo esquelético", "kg", a.MusculoEsqKg, 0, 50, 1, null, a.MusculoClassif);
            if (a.Tmb > 0) Barra(c, "TMB (metabolismo basal)", "kcal", a.Tmb, 1000, 3000, 0, null, null);
            if (a.GorduraPct > 0) Barra(c, "Gordura corporal", "%", a.GorduraPct, 0, 60, 1, a.GorduraIntervalo, a.GorduraClassif);
            if (a.Imc > 0) Barra(c, "IMC", "kg/m²", a.Imc, 10, 60, 1, a.ImcIntervalo, a.ImcClassif);
            if (a.Rcq > 0) Barra(c, "RCQ (cintura/quadril)", "", a.Rcq, 0.6, 1.2, 2, a.RcqIntervalo, a.RcqClassif);
            if (a.GorduraVisceral > 0) Barra(c, "Gordura visceral", "nível", a.GorduraVisceral, 1, 30, 0, a.VisceralIntervalo, a.VisceralClassif);
            if (a.Smi > 0) Barra(c, "SMI (índice músculo-esquelético)", "kg/m²", a.Smi, 4, 12, 1, null, null);
            if (a.IdadeCorporal > 0) Barra(c, "Idade corporal", "anos", a.IdadeCorporal, 18, 80, 0, null, null);

            FechaSecao(c);
        }

        private static void Evolucao(StringBuilder c, AvaliacaoLaudo a)
        {
            var serie = a.Serie;
            if (serie == null || serie.Count < 2) return; // sem evolução com menos de 2 avaliações

            var linhas = new[]
            {
                new DefLinha("Peso", "kg", 1, x => x.Peso),
                new DefLinha("Gordura corporal", "kg", 1, x => x.GorduraKg),
                new DefLinha("Gordura corporal", "%", 1, x => x.GorduraPct),
                new DefLinha("Massa magra", "kg", 1, x => x.MassaMagraKg),
                new DefLinha("IMC", "kg/m²", 1, x => x.Imc),
                new DefLinha("TMB", "kcal", 0, x => x.Tmb),
                new DefLinha("RCQ", "", 2, x => x.Rcq),
            };
            GraficoSerie(c, "Evolução", serie, linhas);
        }

        private static void Conclusao(StringBuilder c, AvaliacaoLaudo a)
        {
            if (string.IsNullOrWhiteSpace(a.Conclusao)) return; // sem texto -> sem seção
            AbreSecao(c, "Conclusão");
            c.Append("<div class=\"info\">");
            foreach (var par in a.Conclusao.Replace("\r\n", "\n").Split('\n'))
                if (par.Trim().Length > 0)
                    c.Append("<p>").Append(Esc(par.Trim())).Append("</p>");
            c.Append("</div>");
            FechaSecao(c);
        }

        private static void Informacao(StringBuilder c)
        {
            AbreSecao(c, "Informação Técnica");
            c.Append("<div class=\"info\">");
            c.Append("<p>A <b>percentagem de gordura corporal (%)</b> indica a proporção de gordura armazenada em relação ao peso e é inversamente proporcional à massa muscular. Este valor é usado, em particular, para avaliar o treino e o estado nutricional.</p>");
            c.Append("<p>O <b>IMC</b> avalia o peso em relação ao tamanho do corpo e é utilizado para classificar o excesso de peso.</p>");
            c.Append("<p>A <b>RCQ</b> (relação cintura/quadril) avalia a distribuição de gordura abdominal. Uma proporção elevada de gordura abdominal significa maior risco de doenças metabólicas.</p>");
            c.Append("<p>A <b>gordura visceral</b> acumula-se na cavidade abdominal, envolvendo os órgãos internos (fígado, pâncreas, intestinos); níveis elevados aumentam o risco cardiometabólico.</p>");
            c.Append("</div>");
            FechaSecao(c);
        }

        // ---------- Componentes ----------

        private static void AbreSecao(StringBuilder c, string titulo)
        {
            c.Append("<div class=\"secao\"><h2>").Append(Esc(titulo)).Append("</h2><div class=\"linha-ouro\"></div><div class=\"card\">");
        }

        private static void FechaSecao(StringBuilder c) { c.Append("</div></div>"); }

        /// <summary>Definição de uma linha (métrica) da tabela de série.</summary>
        private class DefLinha
        {
            public readonly string Rotulo;
            public readonly string Unidade;
            public readonly int Casas;
            public readonly Func<AvaliacaoLaudo, double> Sel;
            public DefLinha(string rotulo, string unidade, int casas, Func<AvaliacaoLaudo, double> sel)
            {
                Rotulo = rotulo; Unidade = unidade; Casas = casas; Sel = sel;
            }
        }

        // Geometria dos gráficos (coordenadas em px reais — sem escala/distorção no wkhtmltopdf).
        private const int SVG_W = 560, PAD_L = 36, PAD_R = 36;

        /// <summary>Seção em gráficos: régua de datas + uma sparkline (mini-linha) por métrica.</summary>
        private static void GraficoSerie(StringBuilder c, string titulo, List<AvaliacaoLaudo> serie, DefLinha[] linhas)
        {
            AbreSecao(c, titulo);
            c.Append("<div class=\"gs\">");
            c.Append("<div class=\"gs-row\"><div class=\"gs-lbl\"></div><div class=\"gs-plot\">")
             .Append(ReguaDatas(serie)).Append("</div></div>");

            foreach (var ln in linhas)
            {
                if (!serie.Any(x => ln.Sel(x) > 0)) continue; // oculta métrica sem valor em nenhuma coluna
                string lbl = ln.Unidade.Length > 0 ? ln.Rotulo + " (" + ln.Unidade + ")" : ln.Rotulo;
                c.Append("<div class=\"gs-row\"><div class=\"gs-lbl\">").Append(Esc(lbl)).Append("</div>")
                 .Append("<div class=\"gs-plot\">").Append(Sparkline(serie, ln)).Append("</div></div>");
            }
            c.Append("</div>");
            FechaSecao(c);
        }

        private static double EixoX(int i, int n)
        {
            if (n <= 1) return SVG_W / 2.0;
            return PAD_L + i * (double)(SVG_W - PAD_L - PAD_R) / (n - 1);
        }

        private static string ReguaDatas(List<AvaliacaoLaudo> serie)
        {
            int n = serie.Count;
            var sb = new StringBuilder();
            sb.Append("<svg width=\"").Append(SVG_W).Append("\" height=\"20\" style=\"font-family:'Inter','Segoe UI',Arial,sans-serif\">");
            for (int i = 0; i < n; i++)
            {
                bool atual = (i == n - 1);
                string d = serie[i].Data.HasValue ? serie[i].Data.Value.ToString("dd/MM/yy", BR) : "—";
                sb.Append("<text x=\"").Append(EixoX(i, n).ToString("0.#", INV))
                  .Append("\" y=\"14\" text-anchor=\"middle\" font-size=\"13\" font-weight=\"").Append(atual ? "700" : "600")
                  .Append("\" fill=\"").Append(atual ? "#7A2B22" : "#8A9A70").Append("\">").Append(Esc(d)).Append("</text>");
            }
            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>Mini-gráfico de linha de uma métrica, na sua própria escala, com valor em cada ponto.</summary>
        private static string Sparkline(List<AvaliacaoLaudo> serie, DefLinha def)
        {
            int n = serie.Count;
            const int H = 40, top = 17, bottom = 5;

            var vals = new double[n];
            double min = double.MaxValue, max = double.MinValue;
            for (int i = 0; i < n; i++)
            {
                vals[i] = def.Sel(serie[i]);
                if (vals[i] > 0) { if (vals[i] < min) min = vals[i]; if (vals[i] > max) max = vals[i]; }
            }
            if (min == double.MaxValue) return ""; // sem dados
            if (max <= min) { double d0 = (min == 0 ? 1 : Math.Abs(min) * 0.05); min -= d0; max += d0; }

            Func<int, double> y = i => top + (1 - (vals[i] - min) / (max - min)) * (H - top - bottom);

            var sb = new StringBuilder();
            sb.Append("<svg width=\"").Append(SVG_W).Append("\" height=\"").Append(H)
              .Append("\" style=\"font-family:'Inter','Segoe UI',Arial,sans-serif\">");

            var pts = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                if (vals[i] <= 0) continue;
                if (pts.Length > 0) pts.Append(' ');
                pts.Append(EixoX(i, n).ToString("0.#", INV)).Append(',').Append(y(i).ToString("0.#", INV));
            }
            sb.Append("<polyline fill=\"none\" stroke=\"#C5A059\" stroke-width=\"1.5\" stroke-linejoin=\"round\" stroke-linecap=\"round\" points=\"")
              .Append(pts).Append("\" />");

            for (int i = 0; i < n; i++)
            {
                if (vals[i] <= 0) continue;
                bool atual = (i == n - 1);
                double x = EixoX(i, n), yi = y(i);
                sb.Append("<circle cx=\"").Append(x.ToString("0.#", INV)).Append("\" cy=\"").Append(yi.ToString("0.#", INV))
                  .Append("\" r=\"").Append(atual ? "2.6" : "2.1").Append("\" fill=\"").Append(atual ? "#7A2B22" : "#1F1A17").Append("\" />");
                double ty = yi - 5; if (ty < 11) ty = 11;
                sb.Append("<text x=\"").Append(x.ToString("0.#", INV)).Append("\" y=\"").Append(ty.ToString("0.#", INV))
                  .Append("\" text-anchor=\"middle\" font-size=\"13\" font-weight=\"").Append(atual ? "700" : "600")
                  .Append("\" fill=\"").Append(atual ? "#7A2B22" : "#1F1A17").Append("\">").Append(Fmt(vals[i], def.Casas)).Append("</text>");
            }
            sb.Append("</svg>");
            return sb.ToString();
        }

        private static void Barra(StringBuilder c, string rotulo, string unidade, double valor,
                                  double escMin, double escMax, int casas, string intervalo, string classif)
        {
            c.Append("<div class=\"barra\"><div class=\"b-lbl\">").Append(Esc(rotulo));
            if (!string.IsNullOrEmpty(unidade)) c.Append(" <span class=\"un\">").Append(Esc(unidade)).Append("</span>");
            if (ClassifValida(classif)) c.Append("<div class=\"b-cls\">").Append(Esc(classif)).Append("</div>");
            c.Append("</div>");

            c.Append("<div class=\"b-trilho-wrap\"><div class=\"b-trilho\">");
            double fMin, fMax;
            if (ParseIntervalo(intervalo, escMin, escMax, out fMin, out fMax))
            {
                double l = Pct(fMin, escMin, escMax);
                double w = Pct(fMax, escMin, escMax) - l;
                if (w > 0)
                    c.Append("<div class=\"b-faixa\" style=\"left:")
                     .Append(l.ToString("0.#", INV)).Append("%;width:").Append(w.ToString("0.#", INV)).Append("%\"></div>");
            }
            double p = Clamp(Pct(valor, escMin, escMax), 1, 99);
            c.Append("<div class=\"b-marca\" style=\"left:").Append(p.ToString("0.#", INV)).Append("%\"></div>");
            c.Append("</div>");
            c.Append("<div class=\"b-escala\"><span>").Append(FmtEscala(escMin, escMax))
             .Append("</span><span class=\"fim\">").Append(FmtEscala(escMax, escMax)).Append("</span></div>");
            c.Append("</div>");

            c.Append("<div class=\"b-val\">").Append(Fmt(valor, casas));
            if (!string.IsNullOrEmpty(unidade)) c.Append("<span class=\"vu\">").Append(Esc(unidade)).Append("</span>");
            c.Append("</div></div>");
        }

        // ---------- Helpers ----------

        private static bool ParseIntervalo(string txt, double escMin, double escMax, out double fMin, out double fMax)
        {
            fMin = 0; fMax = 0;
            if (string.IsNullOrWhiteSpace(txt)) return false;
            string t = txt.Trim();
            if (t.IndexOf("N/A", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            var nums = new System.Collections.Generic.List<double>();
            foreach (Match m in Regex.Matches(t, @"\d+(?:[.,]\d+)?"))
            {
                double d;
                if (double.TryParse(m.Value.Replace(',', '.'), NumberStyles.Any, INV, out d)) nums.Add(d);
            }
            if (nums.Count == 0) return false;

            bool menor = t.Contains("<") || t.IndexOf("até", StringComparison.OrdinalIgnoreCase) >= 0;
            bool maior = t.Contains(">");

            if (t.Contains("-") && nums.Count >= 2) { fMin = nums[0]; fMax = nums[1]; }
            else if (menor) { fMin = escMin; fMax = nums[0]; }
            else if (maior) { fMin = nums[0]; fMax = escMax; }
            else if (nums.Count >= 2) { fMin = nums[0]; fMax = nums[1]; }
            else return false;

            if (fMax <= fMin) return false;
            return true;
        }

        private static double Pct(double valor, double min, double max)
        {
            if (max <= min) return 0;
            return Clamp((valor - min) / (max - min) * 100.0, 0, 100);
        }

        private static double Clamp(double v, double lo, double hi) { return v < lo ? lo : (v > hi ? hi : v); }

        private static string FmtEscala(double v, double escMax) { return Fmt(v, escMax <= 3 ? 1 : 0); }

        private static string Fmt(double v, int casas) { return v.ToString("N" + casas, BR); }

        private static bool ClassifValida(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            return !s.Equals("Não Calculado", StringComparison.OrdinalIgnoreCase)
                && !s.Equals("N/A", StringComparison.OrdinalIgnoreCase);
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
                using (var r = new StreamReader(s, Encoding.UTF8))
                    return r.ReadToEnd();
            }
        }

        /// <summary>Logo da clínica embutida, como data URI base64 (vazio se ausente).</summary>
        private static string LogoDataUri()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream(LogoRes))
                {
                    if (s == null) return "";
                    using (var ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        return "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch { return ""; }
        }
    }
}
