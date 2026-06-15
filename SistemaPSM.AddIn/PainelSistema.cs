using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SistemaPSM.AddIn.Laudo;

namespace SistemaPSM.AddIn
{
    /// <summary>
    /// Painel lateral (CustomTaskPane) que substitui a ribbon "MENU SISTEMA".
    /// Cada botão dispara a macro VBA correspondente via Application.Run.
    /// </summary>
    public class PainelSistema : UserControl
    {
        // Paleta de identidade do Sistema PSM (linha "Botticelli / saúde integral").
        private static readonly Color CorPrimaria  = Color.FromArgb(0xC5, 0xA0, 0x59); // Ouro envelhecido
        private static readonly Color CorPrimariaSuave = Color.FromArgb(0xEF, 0xE6, 0xD2); // Ouro claro (hover)
        private static readonly Color CorNavegacao = Color.FromArgb(0x8A, 0x9A, 0x70); // Verde oliva
        private static readonly Color CorAcento    = Color.FromArgb(0x7A, 0x2B, 0x22); // Terracota
        private static readonly Color CorFundo     = Color.FromArgb(0xFD, 0xFB, 0xF7); // Alabastro
        private static readonly Color CorTexto     = Color.FromArgb(0x1F, 0x1A, 0x17); // Grafite quente
        private static readonly Color CorBorda     = Color.FromArgb(0xE2, 0xDC, 0xD3); // Areia claro

        public PainelSistema()
        {
            MontarUi();
        }

        private void MontarUi()
        {
            SuspendLayout();
            BackColor = CorFundo;
            AutoScroll = false;
            Padding = new Padding(0);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(180, 0);

            // Área de conteúdo rolável (mantém o banner fixo no topo).
            var conteudo = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = CorFundo,
                Padding = new Padding(8)
            };

            var raiz = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Width = 196
            };

            raiz.Controls.Add(Secao("Tela Inicial"));
            raiz.Controls.Add(Botao("Início", "Inicio", "UI_Inicio"));

            raiz.Controls.Add(Secao("Meu Cadastro"));
            raiz.Controls.Add(Botao("Meu Cadastro", "MeuCadastro", "UI_MeuCadastro"));

            raiz.Controls.Add(Secao("Controle de Clientes"));
            raiz.Controls.Add(Botao("Clientes", "Clientes", "UI_Clientes"));

            raiz.Controls.Add(Secao("Controle de Relatórios"));
            raiz.Controls.Add(Botao("Anamneses", "Anamneses", "UI_Anamneses"));
            raiz.Controls.Add(Botao("Avaliações", "Avaliacoes", "UI_Avaliacoes"));

            raiz.Controls.Add(Secao("Ferramentas"));
            raiz.Controls.Add(Botao("Logo", "Logo", "UI_Logo"));
            raiz.Controls.Add(Botao("Backup", "Backup", "UI_Backup"));
            raiz.Controls.Add(Botao("Informações", "Informacoes", "UI_Informacoes"));
            raiz.Controls.Add(Botao("Tutoriais", "Tutoriais", "UI_Tutoriais"));
            raiz.Controls.Add(Botao("Suporte", "Suporte", "UI_Suporte"));

            conteudo.Controls.Add(raiz);

            // Ordem importa para o docking: conteúdo (Fill) primeiro, banner (Top) depois,
            // para o banner reservar o topo e o conteúdo preencher o restante.
            Controls.Add(conteudo);
            Controls.Add(Banner());

            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>Cabeçalho colorido do painel (substitui o título da barra nativa, que fica em branco).</summary>
        private Panel Banner()
        {
            var banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = CorNavegacao
            };

            var titulo = new Label
            {
                Text = "Menu Sistema",
                Dock = DockStyle.Fill,
                ForeColor = CorFundo,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };

            var acento = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                BackColor = CorPrimaria
            };

            banner.Controls.Add(titulo);
            banner.Controls.Add(acento);
            return banner;
        }

        private Panel Secao(string texto)
        {
            var painel = new Panel
            {
                Width = 192,
                Height = 22,
                Margin = new Padding(0, 10, 0, 2)
            };

            var rotulo = new Label
            {
                Text = texto.ToUpperInvariant(),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = CorAcento
            };

            var divisoria = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = CorBorda
            };

            painel.Controls.Add(rotulo);
            painel.Controls.Add(divisoria);
            return painel;
        }

        private Button Botao(string texto, string icone, string macro)
        {
            return Botao(texto, icone, () => Executar(macro));
        }

        private Button Botao(string texto, string icone, Action acao)
        {
            var b = new Button
            {
                Text = "   " + texto,
                Image = Img(icone),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                FlatStyle = FlatStyle.Flat,
                Height = 38,
                Width = 192,
                Margin = new Padding(0, 2, 0, 2),
                Cursor = Cursors.Hand,
                BackColor = CorFundo,
                ForeColor = CorTexto,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = CorBorda;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = CorPrimariaSuave;
            b.FlatAppearance.MouseDownBackColor = CorPrimaria;
            // A borda não muda sozinha no hover; realça em ouro ao passar o mouse.
            b.MouseEnter += (s, e) => ((Button)s).FlatAppearance.BorderColor = CorPrimaria;
            b.MouseLeave += (s, e) => ((Button)s).FlatAppearance.BorderColor = CorBorda;
            b.Click += (s, e) => acao();
            return b;
        }

        private void Executar(string macro)
        {
            try
            {
                Globals.ThisAddIn.Application.Run(macro,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possível executar a ação (" + macro + ").\n\n" + ex.Message,
                    "Sistema PSM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Carrega um ícone embutido (Icons\&lt;nome&gt;.png).</summary>
        internal static Image Img(string nome)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream("SistemaPSM.AddIn.Icons." + nome + ".png"))
                {
                    if (s == null) return null;
                    using (var tmp = Image.FromStream(s))
                        return new Bitmap(tmp); // cópia desacoplada do stream
                }
            }
            catch { return null; }
        }
    }
}
