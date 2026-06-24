# Novos rótulos de avaliação no laudo (fluxo C) — Design

**Data:** 2026-06-24
**Status:** Aprovado (orientação; implementação do form/VBA é do usuário)

## Objetivo

Padronizar os rótulos dos 7 componentes do quadro circundado no `FrmBioimpC`
(Peso, Gordura corporal, Massa Óssea, Proteína, Água Corporal, Massa Muscular,
Massa Musc. Esquelética) para o vocabulário único:

> **Baixo · Saudável · Alto · Excelente**

E garantir que o rótulo selecionado apareça na coluna **"Avaliação"** do quadro
*Análise da composição corporal* do laudo de avaliação única.

Apresentação no laudo: **texto simples** (sem cor/ícone).

## Descoberta-chave

O texto da coluna "Avaliação" é **transcrito verbatim** da aba "Avaliações". O C#
(`LaudoAvaliacaoHtmlBuilder.Cls()`) não calcula nada: só converte `N/A` /
`Não Calculado` / vazio em `-`; qualquer outro texto passa intacto. Não há
whitelist nem normalização dos rótulos.

**Consequência:** se o VBA gravar o novo rótulo nas mesmas 7 colunas, o laudo
imprime correto **sem nenhuma alteração no C# e sem recompilar/republicar**.

## Contrato planilha ↔ C# (offsets imutáveis)

Os 7 componentes já estão integralmente mapeados em `LaudoRepositorio.cs` e
`LaudoAvaliacaoHtmlBuilder.Composicao()`. Gravar o rótulo selecionado nestas
colunas (não alterar os offsets):

| Linha no laudo | Campo C# | Coluna | Offset (A=0) | Grupo no FrmBioimpC |
|---|---|---|---|---|
| Peso | `PesoTotalClassif` | BT | 71 | Peso |
| Gordura corporal | `GorduraClassif` | AH | 33 | Gordura corporal |
| Sal inorgânico | `OsseaClassif` | BB | 53 | Massa Óssea |
| Proteína | `ProteinaClassif` | AY | 50 | Proteína |
| Água corporal | `AguaClassif` | AW | 48 | Água Corporal |
| Músculo | `PesoMuscularClassif` | AL | 37 | Massa Muscular |
| Músculo esquelético | `MusculoClassif` | AQ | 42 | Massa Musc. Esquelética |

## Mudanças necessárias (lado VBA/formulário — responsabilidade do usuário)

1. **Formulário:** trocar as `Caption` dos radio buttons dos 7 grupos para
   `Baixo` · `Saudável` · `Alto` · `Excelente`.
2. **Save (`SalvaAvaliacaoC` / lógica do `FrmBioimpC`):** verificar o padrão de
   gravação:
   - **Padrão A** — grava o caption (`Cells(...) = optSelecionado.Caption`):
     trocar as captions já resolve.
   - **Padrão B** — grava literal fixo (`If optBaixo Then Cells(...) = "Baixo"`):
     atualizar os literais para o novo vocabulário, senão o laudo continua
     mostrando o texto antigo.
3. **Acento:** gravar `Saudável` com acento (planilha e laudo são Unicode/UTF-8).

## Mudanças no C#

**Nenhuma.** A transcrição verbatim já cobre o novo vocabulário.

## Verificação (sem recompilar)

1. Ajustar form + VBA.
2. Abrir uma avaliação fluxo C, marcar os 7 grupos, salvar.
3. Conferir nas colunas BT/AH/BB/AY/AW/AL/AQ da aba "Avaliações" se o texto
   gravado é o novo rótulo (com acento em Saudável).
4. Emitir o laudo de avaliação → coluna "Avaliação" reflete os 7.

## Notas

- Avaliações antigas mantêm os rótulos antigos; apenas novos salvamentos usam o
  novo vocabulário.
- Fluxos A e B não são afetados (continuam não gravando Ósseo/Proteína/Água/
  Músculo nem o Peso Total → `-` no laudo).
- Atualizar `SistemaPSM.AddIn/Laudo/ColunaAvaliacao-Composicao.md` para refletir o
  novo vocabulário do fluxo C.
