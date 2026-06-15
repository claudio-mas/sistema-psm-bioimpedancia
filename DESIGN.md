# Documento de Design de Interface (DESIGN.md)
## Projeto: Planilha Bioimpedância — Flávia Zanoni (Saúde Integral da Mulher)

Este documento estabelece as diretrizes de design, a paleta de cores e os padrões visuais para a emissão do laudo. O conceito visual é fundamentado no manual de branding da marca, inspirado na premissa **"A mulher como obra de arte"** e referências clássicas como *Sandro Botticelli* (O Nascimento de Vênus).

---

## 🎨 1. Paleta de Cores (Especificação Técnica)

Para garantir uma interface sofisticada, limpa e acessível, as cores foram mapeadas seguindo a regra **60-30-10** (60% neutros/fundo, 30% estrutura/textos e 10% destaque).

### 1.1. Cores de Identidade e Destaque
* **Ouro Botticelli (Primary / Accent):** `#C5A059`
    * *Descrição:* Tom refinado extraído dos elementos clássicos e traços de *Line Art* do manual.
    * *Uso:* Botões de ação principal (*Call to Action*), ícones destacados, estados ativos de menus e links importantes.
* **Terracota Profundo (Semantic / Accent Secundário):** `#7A2B22`
    * *Descrição:* Tom quente e orgânico que traz contraste e profundidade à interface.
    * *Uso:* Alertas sutis, badges de status, elementos de validação e destaques secundários.

### 1.2. Cores Estruturais e Apoio
* **Verde Oliva Suave (Secondary / Structure):** `#8A9A70`
    * *Descrição:* Representa a saúde integral, o equilíbrio e o bem-estar orgânico.
    * *Uso:* Cabeçalhos de tabelas, barras laterais de navegação secundárias, fundos de cards específicos e divisores.

### 1.3. Tons Neutros (Fundo e Tipografia)
* **Alabastro / Off-White Quente (Canvas Background):** `#FDFBF7`
    * *Descrição:* Substitui o branco puro para evitar a fatigue visual e dar um aspecto de tela de pintura ou papel de alta gramatura.
    * *Uso:* Fundo principal da aplicação.
* **Grafite Quente (Text Primary):** `#1F1A17`
    * *Descrição:* Um tom quase preto, suavizado para garantir excelente leitura sem o contraste agressivo do preto puro.
    * *Uso:* Textos logos, títulos (`<h1>` a `<h3>`) e tabelas de dados.
* **Areia Claro (Borders & Component Background):** `#E2DCD3`
    * *Descrição:* Neutro de suporte para separação física de elementos.
    * *Uso:* Linhas divisórias, bordas de campos de formulário (`input`) e fundos de elementos desabilitados.

---

## 📐 2. Tipografia e Escala Visual

Para manter o alinhamento com a sofisticação da marca, a tipografia deve priorizar fontes serifadas elegantes para títulos e fontes sem serifa altamente legíveis para o corpo do sistema.

* **Títulos (`<h1>`, `<h2>`):** Playfair Display ou Georgia (Serif) — Transmite o conceito de arte e exclusividade.
* **Corpo do Texto e Tabelas:** Inter, DM Sans ou Arial (Sans-serif) — Garante clareza na leitura de dados clínicos ou relatórios.

### Escala de Tamanhos Recomendada:
* `h1` (Títulos de Páginas): `24pt` / `32px`
* `h2` (Seções de Cards): `18pt` / `24px`
* `h3` (Subtítulos): `14pt` / `18px`
* `body` (Textos gerais / Tabelas): `11pt` / `15px`
* `small` (Legendas / Metadados): `9pt` / `12px`

---

## 🧩 3. Componentes e Estilo de Interface

O conceito gráfico baseado nas curvas da *Vênus de Milo* dita as regras para os componentes front-end:

1.  **Arredondamento Suave (Border Radius):** Todos os componentes estruturais (cards, botões, modais) devem utilizar cantos arredondados moderados (`border-radius: 8px` a `12px`). Evite cantos totalmente retos para manter a suavidade biológica e feminina do ecossistema visual.
2.  **Ícones em Linha (Outline Icons):** Utilize bibliotecas de ícones com traços finos e vazados (como *Lucide Icons* ou *Heroicons Outline*), configurados com `stroke-width: 1.5`. Ícones pesados ou totalmente preenchidos quebram a harmonia sutil da marca.
3.  **Sombras Sutis (Elevation):** A profundidade deve ser aplicada de forma quase imperceptível. Use sombras suaves e difusas para destacar cards sobre o fundo Alabastro.

---

## 💻 4. Exemplo de Configuração de Tema (Tailwind CSS)

Para acelerar o desenvolvimento front-end, utilize o seguinte mapeamento no arquivo `tailwind.config.js`:

```javascript
module.exports = {
  theme: {
    extend: {
      colors: {
        brand: {
          gold: '#C5A059',
          terracotta: '#7A2B22',
          olive: '#8A9A70',
        },
        neutral: {
          canvas: '#FDFBF7',
          ink: '#1F1A17',
          sand: '#E2DCD3',
        }
      },
      fontFamily: {
        display: ['Playfair Display', 'Georgia', 'serif'],
        sans: ['Inter', 'sans-serif'],
      },
      borderRadius: {
        'brand': '10px',
      }
    },
  },
}

## ♿ 5. Acessibilidade e Contraste (WCAG)
Texto Principal (#1F1A17) sobre Fundo (#FDFBF7): Proporção de contraste superior a 12:1, superando com folga o padrão AAA da WCAG para leitura confortável.

Botões Ouro (#C5A059): Devem utilizar texto interno na cor Grafite Quente (#1F1A17) ou Branco puro para garantir o contraste mínimo de leitura em elementos interativos.