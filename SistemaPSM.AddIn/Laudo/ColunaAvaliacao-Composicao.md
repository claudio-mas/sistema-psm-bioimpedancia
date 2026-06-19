# Coluna "Avaliação" — Quadro "Análise da composição corporal"

A coluna **"Avaliação"** do quadro é apenas **texto lido** da aba "Avaliações" da planilha `Bioimpedancia.xlsm`. O código C# (`LaudoAvaliacaoHtmlBuilder`) **não calcula** nada — só transcreve o texto, convertendo `N/A` e `Não Calculado` em `-`. Toda a lógica de classificação vive no **VBA** da planilha.

Quem grava o texto são as macros de salvamento dos formulários de avaliação, em offsets de coluna (A = 0).

## Origem e valores possíveis por linha

| Linha (componente) | Coluna | Como a "Avaliação" é obtida | Valores possíveis |
|---|---|---|---|
| Peso | BT (71) | Nunca gravada por nenhum formulário | *(sempre `-`)* |
| **Gordura corporal** | AH (33) | **Fórmula automática** `TabelaGordura(% gordura, sexo, idade)` | **<18 anos:** Muito Baixo · Baixo · Ótimo · Moderadamente Alto · Alto · Muito Alto<br>**Adultos (Pollock):** Excelente · Bom · Acima da Média · Média · Abaixo da Média · Ruim · Muito Ruim<br>**>65 anos:** N/A → `-` |
| Sal inorgânico (ósseo) | BB (53) | Fluxo C, seleção manual | Baixo · Médio · Excelente · *(ou `-`)* |
| Proteína | AY (50) | Fluxo C, seleção manual | Insuficiente · Médio · Excelente · *(ou `-`)* |
| Água corporal | AW (48) | Fluxo C, seleção manual | Desidratado · Hidratado · Hiperhidratado · *(ou `-`)* |
| Músculo | AL (37) | Fluxo C, seleção manual | Baixo · Médio · Excelente · *(ou `-`)* |
| **Músculo esquelético** | AQ (42) | Seleção manual | **Fluxo B:** Baixo (-) · Normal (0) · Alto (+) · Muito Alto (++)<br>**Fluxo C:** Muito baixo · Baixo · Excelente<br>*(ou `-`)* |

## Fluxos de cadastro

| Fluxo | Formulários | Grava Ósseo/Proteína/Água/Músculo? |
|---|---|---|
| **A** | `FrmAvaliacaoA` | Não → essas linhas saem `-` |
| **B** | `FrmAvaliacaoB` / `FrmBioimpR` | Não → essas linhas saem `-` (mas grava Músculo esquelético) |
| **C** | `FrmAvaliacaoC` / `FrmBioimpC` | Sim → preenche por seleção manual |

## Notas

- **Gordura corporal** é a única linha com classificação **calculada por fórmula** (`TabelaGordura` em `GORDURA.vba`: Houtkooper 1996 para menores de 18; Pollock 1993 por faixa etária ≤25/≤35/≤45/≤55/≤65 para adultos).
- As demais linhas com texto vêm de **seleção manual** do operador: a legenda (`Caption`) do radio button marcado vira o texto da avaliação. Não há cálculo.
- O `-` aparece quando a célula está vazia ou contém `N/A` / `Não Calculado` (conversão feita pela função `Cls()` do C#).
- Os offsets de coluna são confirmados em `LaudoRepositorio.cs`; a lógica de gravação está nas macros `SalvaAvaliacaoB` (`AvaliacaoB.vba`) e equivalentes em `AvaliacaoC.vba` / `AvaliacaoA.vba`.
