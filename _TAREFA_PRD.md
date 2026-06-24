# TAREFA: Engenharia reversa de Bioimpedancia.xlsm + geração de PRD

## OBJETIVO
Fazer engenharia reversa COMPLETA da planilha `Bioimpedancia.xlsm` para extrair toda a lógica e regras de negócio, e gerar um PRD (Product Requirements Document) detalhado que será usado para desenvolver uma aplicação web micro-SaaS.

## ARQUIVOS E PASTAS RELEVANTES (diretório do projeto: `C:\Sistema\PSM\Projeto 14\zanoni`)
- `Bioimpedancia.xlsm` — planilha principal (5.3MB, com macros VBA)
- `Bioimpedancia_extracted/` — pasta já extraída da planilha (inspecione primeiro: `xl/`, `vbaProject.bin`, worksheets, sharedStrings, etc.)
- `SistemaPSM.AddIn/` — código C# do AddIn VSTO (contém regras de negócio relevantes)
- `CLAUDE.md`, `DESIGN.md`, `README.md` — documentação existente do projeto (LEIA primeiro)
- `BIA.pdf`, `APR_FLAVIA_AGO_2.0.pdf` — laudos/relatórios de exemplo (referência de output esperado)
- `modelos/`, `CLIENTES/`, `_LaudoPreview/` — modelos e exemplos

## ESCOPO DA ANÁLISE (extraia TUDO)
1. **Estrutura**: todas as abas/planilhas, seu propósito e como se relacionam.
2. **Campos de entrada**: dados que o usuário informa (peso, altura, idade, sexo, medidas de bioimpedância, dobras, etc).
3. **Fórmulas e cálculos**: TODAS as fórmulas de células, conversões, índices (%gordura, massa magra, TMB, IMC, água corporal, etc). Documente cada fórmula com sua lógica matemática.
4. **Macros VBA**: extraia e analise todo o código VBA (`vbaProject.bin` / `Bioimpedancia_extracted`). Documente funções, fluxos, validações e automações.
5. **Código do AddIn VSTO (C#)**: regras de negócio implementadas em código.
6. **Tabelas de referência/lookup**: faixas, classificações, valores normativos por idade/sexo, escalas.
7. **Regras de negócio**: validações, condições, faixas de classificação, decisões (if/then), formatação condicional.
8. **Outputs/Laudos**: estrutura dos relatórios/laudos gerados (use os PDFs e `_LaudoPreview` como referência).

## ENTREGÁVEL
Gere um arquivo `PRD.md` na raiz do projeto (`C:\Sistema\PSM\Projeto 14\zanoni\PRD.md`) contendo:
- Visão geral do produto e proposta de valor do micro-SaaS
- Personas / usuários-alvo
- Glossário de termos de bioimpedância
- Modelo de dados (entidades, campos, tipos, unidades)
- Catálogo COMPLETO de cálculos e fórmulas (nome, entradas, fórmula, unidade, fonte na planilha)
- Catálogo de regras de negócio e classificações (faixas/tabelas de referência)
- Fluxos de usuário (entrada de dados -> cálculo -> laudo)
- Especificação de telas/funcionalidades sugeridas
- Estrutura do laudo/relatório de saída
- Requisitos não-funcionais relevantes
- Mapeamento de rastreabilidade: cada regra/fórmula -> origem na planilha/código
- Itens ambíguos ou incertos claramente sinalizados como "A CONFIRMAR"

## REGRAS IMPORTANTES
- NÃO invente fórmulas ou regras. Se não conseguir extrair algo, marque como incerto.
- Baseie tudo em evidência extraída dos arquivos.
- Para extrair VBA/xlsm use ferramentas disponíveis (python openpyxl, olevba/oletools, unzip do xlsm, ou inspeção da pasta `_extracted`).
- Documente a fonte (arquivo + aba/célula/função) de cada item.

## RELATÓRIO FINAL
Ao terminar, reporte: resumo do que foi extraído, caminho absoluto do `PRD.md`, e liste explicitamente quaisquer lacunas/itens A CONFIRMAR.
