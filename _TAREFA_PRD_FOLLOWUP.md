# FOLLOW-UP: Completar o PRD com os itens "A CONFIRMAR" extraíveis do VBA

O PRD em `C:\Sistema\PSM\Projeto 14\zanoni\PRD.md` ficou bom, mas vários itens marcados como "A CONFIRMAR" NÃO são realmente desconhecidos — eles existem no VBA que você já extraiu em `C:\Sistema\PSM\Projeto 14\zanoni\_prd_evidence\olevba.txt`. Transcreva o conteúdo real desses trechos e complete o PRD com precisão (sem inventar).

## TAREFAS (extraia do `olevba.txt` e demais evidências já geradas)

1. **Tabelas de classificação completas** — transcreva integralmente as faixas numéricas e os textos de classificação de:
   - `TabelaGordura` (linhas ~14483-14907)
   - `TabelaGorduraAlvo` (~14913-15242)
   - `TabelaRCQ` (~15386-15609)
   - `TabelaRCQ_INTERVALO`, `TabelaGorduraOMR_INTERVALO`, `TabelaMusculoEsqOMR_INTERVALO`, `TabelaIMC` (~11977-11998)
   Apresente cada tabela como matriz por sexo/faixa etária/faixa de valor -> classificação, fielmente ao VBA.

2. **Fórmulas internas** — transcreva o corpo real das funções:
   - `tmbOMS`
   - `DRIS_Adolescentes`
   - `DRIS_Adultos`
   Documente as fórmulas/coeficientes exatos por sexo/faixa etária.

3. **Aba `Analise C`** — transcreva as 21 fórmulas de célula extraídas em `_prd_evidence/xml_formulas.json` e descreva o que cada uma calcula e como se conecta a `FrmAvaliacaoC`.

4. **IMC no fluxo R** — confirme se existe cálculo automático de IMC em algum ponto do VBA (procure por `TxtIMC =`, `peso / `, `^ 2`, `Altura`). Se houver fórmula, documente; se for realmente só cópia de valor digitado, deixe claro.

5. **GET / fatores de atividade** — procure no VBA como o GET é calculado a partir da TMB (fatores de atividade, multiplicadores). Documente as regras encontradas.

## COMO ENTREGAR
- ATUALIZE o arquivo `C:\Sistema\PSM\Projeto 14\zanoni\PRD.md` substituindo/expandindo as seções correspondentes e reduzindo a lista "A CONFIRMAR" apenas aos itens que genuinamente não puderam ser extraídos.
- Para CADA item completado, mantenha a rastreabilidade (função + faixa de linhas no `olevba.txt`).
- NÃO invente valores. Se um trecho específico não existir no VBA extraído, mantenha como "A CONFIRMAR" e explique por quê.

## RELATÓRIO FINAL
Reporte: quais itens "A CONFIRMAR" foram resolvidos, quais permaneceram e por quê, e o novo tamanho/seções do PRD.
