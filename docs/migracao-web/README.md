# Migração web — catálogo e dicionário para revisão

Data: 2026-09-27. Etapa: especificação, após aprovação da consolidação de brainstorming. Nenhuma aplicação web foi implementada e o XLSM não foi alterado.

## Leitura recomendada

1. [Catálogo de regras](catalogo-regras.md): comportamento atual, diferenças entre os três protocolos, cálculos, classificações, anamnese, OCR e documentos.
2. [Dicionário de dados](dicionario-dados.md): 25 campos de pacientes, 108 posições de avaliações, 48 campos de anamnese e inventário de 43 campos de Meu Cadastro. Inclui expressões de gravação e nomes/tipos propostos para a web.
3. [Divergências e homologação](divergencias-e-homologacao.md): 16 divergências conferidas no código, situação e recomendação de cada uma, 23 casos de referência e portões por plano de implementação.
4. [Evidências](evidencias.md): identificação do XLSM, inventário VBA, 21 fórmulas de célula resolvidas e 44 zonas OCR.
5. [Anexo de fontes](anexo-fontes-regras.md): transcrição das rotinas de cálculo/classificação para conferência dos detalhes e limites.

Base de produto: [consolidação aprovada](../superpowers/specs/2026-09-27-migracao-web-brainstorming.md).

## O que foi verificado

- Extração atual de 89 módulos e 651 procedimentos VBA, sem executar macros.
- Estrutura XML do XLSM e campos das quatro abas principais; dados pessoais não foram reproduzidos nos documentos.
- Regras C# e mapeamento do OCR atual, incluindo comportamentos que não estavam suficientemente representados na documentação histórica.
- Fórmula compartilhada G7 resolvida como `=1-F7`; hora da medição identificada em DD apesar de ausência de cabeçalho.

## Limites desta entrega

“Observado no código” não significa “homologado em execução”. A extração das legendas dos controles apresentou falhas e não foi usada como fonte confiável de texto. A classificação clínica e as correções de divergências continuam sujeitas à revisão do responsável. Os casos de referência ainda precisam ser executados na cópia de validação da planilha.

O catálogo permite revisar os dados e decisões antes do plano de implementação. As propostas de nomes de campos, nulabilidade e modelo relacional não são migrations nem código de produção.

## Próxima decisão prioritária

A implementação será dividida em sete planos por subsistema ([portões](divergencias-e-homologacao.md#5-portões-por-plano-de-implementação)). O Plano 1 (Fundação: acesso, permissões, auditoria, pacientes e importação de cadastros) não depende de decisão clínica. Os padrões foram aceitos em 2026-09-27 e o plano está em [2026-09-27-plano-1-fundacao.md](../superpowers/plans/2026-09-27-plano-1-fundacao.md); o código fica no repositório separado `C:\Sistema\PSM\psm-web`.

Para liberar o motor de regras (Plano 3), a clínica precisa decidir DIV-01, 02, 03, 15 e 16, e os casos de referência precisam ser executados na cópia de validação. Recomenda-se corrigir os defeitos confirmados, com validação individual e registro da diferença em relação ao legado. Nenhuma correção foi aplicada à planilha.

DIV-05 (classificações sem transferência) e DIV-15 (idade copiada do cadastro) também afetam o sistema atual. Corrigi-los na planilha é uma decisão separada da migração.
