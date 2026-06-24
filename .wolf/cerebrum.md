# Cerebrum

> OpenWolf's learning memory. Updated automatically as the AI learns from interactions.
> Do not edit manually unless correcting an error.
> Last updated: 2026-06-24

## User Preferences

<!-- How the user likes things done. Code style, tools, patterns, communication. -->

## Key Learnings

- **Project:** zanoni
- **Description:** Suplemento do Microsoft Excel (VSTO / C#, .NET Framework 4.7.2) que adiciona um
- **Coluna "Avaliação" do laudo = transcrição verbatim.** O quadro *Análise da composição
  corporal* lê 7 células de texto da aba "Avaliações" e imprime sem calcular. `LaudoAvaliacaoHtmlBuilder.Cls()`
  só converte `N/A` / `Não Calculado` / vazio em `-`; qualquer outro texto passa intacto.
  Não há whitelist nem normalização de rótulos.
- **Mapeamento dos 7 componentes (offset A=0):** Peso→`PesoTotalClassif`/BT(71),
  Gordura→`GorduraClassif`/AH(33), Sal inorgânico→`OsseaClassif`/BB(53),
  Proteína→`ProteinaClassif`/AY(50), Água→`AguaClassif`/AW(48),
  Músculo→`PesoMuscularClassif`/AL(37), Músculo esquelético→`MusculoClassif`/AQ(42).
  Definidos em `LaudoRepositorio.cs` (OFF_*) e usados em `Composicao()`.
- **Fluxo C (2026-06-24): rótulos padronizados** para Baixo · Saudável · Alto · Excelente nos
  7 grupos do `FrmBioimpC`. **Mudança é 100% VBA/formulário — o C# não muda nem recompila**,
  porque grava nas mesmas colunas e o C# transcreve verbatim.
- **Gotcha VBA:** mudar a `Caption` do radio no form só basta se o `SalvaAvaliacaoC` gravar
  `optSelecionado.Caption`. Se gravar literais fixos (`If optBaixo Then ... = "Baixo"`), os
  literais também precisam ser atualizados.
- **Acento importa:** "Saudável" deve ser gravado com acento (planilha e laudo são Unicode/UTF-8).

## Do-Not-Repeat

<!-- Mistakes made and corrected. Each entry prevents the same mistake recurring. -->
<!-- Format: [YYYY-MM-DD] Description of what went wrong and what to do instead. -->

## Decision Log

<!-- Significant technical decisions with rationale. Why X was chosen over Y. -->

- **2026-06-24 — Rótulos de avaliação como texto simples (não cor por nível).** Escolhido para
  manter a mudança em zero alteração de C# (só VBA/form). Cor por nível exigiria
  `LaudoAvaliacaoHtmlBuilder` + CSS + recompilar/republicar — descartado.
