# Coluna "Avaliação" — Quadro "Análise da composição corporal"

A coluna **"Avaliação"** do quadro é apenas **texto lido** da aba "Avaliações" da planilha `Bioimpedancia.xlsm`. O código C# (`LaudoAvaliacaoHtmlBuilder`) **não calcula** nada — só transcreve o texto, convertendo `N/A` e `Não Calculado` em `-`. Toda a lógica vive no **VBA** da planilha.

O texto é gravado pela macro de salvamento do formulário usado no cadastro. **A classificação depende do fluxo de cadastro** (há 3): **A** = `FrmAvaliacaoA` · **B** = `FrmAvaliacaoB`/`FrmBioimpR` · **C** = `FrmAvaliacaoC`/`FrmBioimpC`. Offsets de coluna com A = 0.

## Origem por componente e fluxo

| Componente | Coluna | Fluxo A | Fluxo B | Fluxo C |
|---|---|---|---|---|
| Peso | BT (71) | `-` | `-` | manual: Baixo · Saudável · Alto · Excelente |
| **Gordura corporal** | AH (33) | **fórmula** `TabelaGordura` | manual: Baixo · Médio · Obesidade · Sobrepeso | manual: Baixo · Saudável · Alto · Excelente |
| Sal inorgânico (ósseo) | BB (53) | `-` | `-` | manual: Baixo · Saudável · Alto · Excelente |
| Proteína | AY (50) | `-` | `-` | manual: Baixo · Saudável · Alto · Excelente |
| Água corporal | AW (48) | `-` | `-` | manual: Baixo · Saudável · Alto · Excelente |
| Músculo | AL (37) | `-` | `-` | manual: Baixo · Saudável · Alto · Excelente |
| **Músculo esquelético** | AQ (42) | `-` | manual: Baixo (-) · Normal (0) · Alto (+) · Muito Alto (++) | manual: Baixo · Saudável · Alto · Excelente |

> As palavras das colunas de fluxo B/C são as **legendas (`Caption`) reais** dos radio buttons (extraídas via `Designer.Controls`). O operador marca uma; ela vira o texto da avaliação.
>
> **Fluxo C (a partir de 2026-06-24):** os 7 componentes acima foram padronizados para o vocabulário único **Baixo · Saudável · Alto · Excelente**. O C# continua transcrevendo verbatim (sem alteração). Ver `docs/superpowers/specs/2026-06-24-rotulos-avaliacao-laudo-c-design.md`.

## `TabelaGordura` — a fórmula (somente fluxo A)

`TabelaGordura(% gordura, sexo, idade)` em `GORDURA.vba`:

- **< 18 anos** (Houtkooper 1996; ambos os sexos): `<5` Muito Baixo · `≤10` Baixo · `≤20` Ótimo · `≤25` Moderadamente Alto · `≤31` Alto · `>31` Muito Alto
- **Adultos** (Pollock 1993; faixas etárias `≤25/≤35/≤45/≤55/≤65`): Excelente · Bom · Acima da Média · Média · Abaixo da Média · Ruim · Muito Ruim
- **> 65 anos:** `N/A` → `-`

## Notas

- **A classificação deste quadro depende do fluxo.** Só no **fluxo A** a Gordura é calculada por fórmula; nos fluxos **B** e **C** todas as classificações aqui são **seleção manual**. Os fluxos A/B não gravam Ósseo/Proteína/Água/Músculo nem o Peso Total → saem `-`.
- No **fluxo C**, a única classificação por **fórmula** de toda a avaliação é o **RCQ** (`TabelaRCQ`, em `RCQ.vba`), que pertence a outro quadro do laudo, não a este.
- O `-` aparece quando a célula está vazia ou contém `N/A` / `Não Calculado` (função `Cls()` do C#).
- Offsets confirmados em `LaudoRepositorio.cs`; gravação em `SalvaAvaliacaoA/B/C` (`AvaliacaoA.vba` / `AvaliacaoB.vba` / `AvaliacaoC.vba`) e nos formulários `FrmBioimpR` / `FrmBioimpC`.
