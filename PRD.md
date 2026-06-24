# PRD — Micro-SaaS de Bioimpedância Sistema PSM

Documento gerado por engenharia reversa baseada em evidências extraídas de `Bioimpedancia.xlsm`, VBA (`_prd_evidence/olevba.txt`), XML da planilha (`_prd_evidence/xml_*.json`), documentação (`CLAUDE.md`, `README.md`, `DESIGN.md`) e código C# do AddIn.

## Evidência central

- A extração XML encontrou 25 abas; apenas `Analise C` contém fórmulas de célula (21).
- A lógica de negócio está majoritariamente no VBA extraído por `python -m oletools.olevba`.
- O AddIn C# atua como UI/geração de PDF e lê valores já calculados da aba `Avaliações`.

## Produto

Micro-SaaS para cadastro de clientes, anamneses, avaliações de bioimpedância R/A/C, cálculo de composição corporal, metas, necessidade energética, histórico comparativo e emissão de laudos PDF com identidade visual.

## Personas

1. Profissional de saúde/nutrição.
2. Assistente administrativo.
3. Cliente/paciente.
4. Administrador do SaaS.

## Glossário

- **Bioimpedância R**: fluxo resumido/manual (`FrmAvaliacaoB`, `FrmBioimpR`).
- **Bioimpedância A**: fluxo automático (`FrmAvaliacaoA`).
- **Bioimpedância C**: fluxo completo (`FrmAvaliacaoC`, aba `Analise C`).
- **IMC**: índice de massa corporal.
- **RCQ**: relação cintura/quadril.
- **TMB/GEB**: taxa/gasto energético basal.
- **GET**: gasto energético total.
- **MLG**: massa livre de gordura.

## Estrutura da planilha

Fonte: `_prd_evidence/xml_summary.json`.

| Aba | Dimensão | Fórmulas XML | Propósito evidenciado |
|---|---:|---:|---|
| Início | A1 | 0 | Tela inicial/entrada visual. |
| Seq. | A1:C2 | 0 | Sequências/IDs. |
| Painel R | A1:AZ99 | 0 | Tela/painel do fluxo resumido. |
| Painel C | A1:AZ99 | 0 | Tela/painel do fluxo completo. |
| Painel A | A1:AZ99 | 0 | Tela/painel do fluxo automático. |
| Painel A2 | A1:AZ99 | 0 | Tela auxiliar do fluxo automático. |
| Anamnese | A1:AV1 | 0 | Base tabular de anamnese. |
| RelatorioAn | A1:L105 | 0 | Modelo de relatório de anamnese. |
| Avaliações | A1:DC4 | 0 | Base tabular principal das avaliações. |
| RelatorioBh | A1:L103 | 0 | Laudo masculino/variante H. |
| RelatorioBm | A1:L103 | 0 | Laudo feminino/variante M. |
| RelatorioBh_ | A1:L106 | 0 | Modelo alternativo de laudo. |
| RelatorioBm_ | A1:XFD106 | 0 | Modelo alternativo de laudo. |
| RelatorioBh3 | A1:L105 | 0 | Modelo alternativo de laudo. |
| RelatorioBm3 | A1:L105 | 0 | Modelo alternativo de laudo. |
| RelatorioBhm | A1:L95 | 0 | Modelo de laudo combinado/alternativo. |
| Comparações | A1:XFD98 | 0 | Relatório comparativo/evolução. |
| Comparações_ | A1:XFD99 | 0 | Modelo comparativo alternativo. |
| Comparações3_ | A1:XFD99 | 0 | Modelo comparativo alternativo. |
| Comparações4 | A1:XFD99 | 0 | Modelo comparativo alternativo. |
| Meu Cadastro | A1:AQ2 | 0 | Cadastro do profissional/empresa. |
| Clientes | A1:Y2 | 0 | Cadastro de clientes. |
| Ficha Cadastral | A2:J39 | 0 | Modelo de ficha cadastral. |
| Ficha Cadastral__ | A2:J39 | 0 | Modelo alternativo de ficha. |
| Analise C | A1:Q44 | 21 | Cálculos/apoio do fluxo completo. |

## Modelo de dados

### Cliente

Fonte: VBA `FrmClientes` e aba `Clientes`.

Campos evidenciados: id, nome, sexo, data de nascimento/idade, foto, telefone, celular, CEP, endereço, número, complemento, UF, RG, CPF, anexos de RG/CPF/outros. Validações de máscara aparecem em eventos `KeyPress` para telefone, celular, CEP, CPF, RG e datas.

### AvaliacaoBioimpedancia

Fonte principal: `SalvaAvaliacaoB`, `_prd_evidence/olevba.txt:6333-6570`.

| Campo | Unidade/tipo | Origem evidenciada |
|---|---|---|
| IdA | inteiro | `ActiveCell.Value = CInt(FrmAvaliacaoB.TxtCodA)` |
| IdC | inteiro | offset 4 |
| Nome | texto | offset 5 |
| Sexo | texto | offset 6 |
| Idade | anos | offset 7 |
| Faixa etária | texto | `<11 Crianças`, `<20 Adolescentes`, `<60 Adultos`, `>59 Idosos` |
| Peso | kg | offset 9 |
| Altura | cm | offset 10 |
| Tipo | texto | offset 11 = `Bioimpedância` |
| Protocolo | texto | offset 12 |
| Observação | texto | offset 13 |
| Circunferências | cm | pescoço, ombros, peitoral, braços, antebraços, abdômen, cintura, quadril, coxas, panturrilhas; offsets 15–28 |
| IMC | kg/m² | offset 29 |
| Classificação IMC | texto | offset 30 |
| Gordura atual | % | offset 31 |
| Peso gordo | kg | offset 32 |
| Classificação gordura | texto | offset 33 |
| Massa livre de gordura | % e kg | offsets 38 e 39 |
| Músculo esquelético | % e kg | offsets 40 e 41 |
| Classificação músculo | texto | offset 42 |
| Gordura visceral | nível | offset 43 |
| Classificação visceral | texto | offset 44 |
| Idade corporal/metabólica | anos | offset 55 |
| RCQ | razão | offset 57 |
| Classificação RCQ | texto | offset 58 |
| Gordura alvo | % | offset 59 |
| IMC alvo | kg/m² | offset 60 |
| Peso alvo | kg | offset 61 |
| Controle de peso | kg | offset 62 |
| GEB/TMB | kcal/dia | offset 63 |
| GET | kcal/dia | offset 65 |
| Intervalos normativos | texto | offsets 66–70 usados no laudo |

## Catálogo de cálculos evidenciados

| Cálculo | Entradas | Fórmula/regra | Unidade | Fonte |
|---|---|---|---|---|
| Idade | data nascimento | `DateDiff("yyyy", nascimento, Now)` com ajuste se aniversário ainda não ocorreu | anos | `Age`, `_prd_evidence/olevba.txt:4191-4202` |
| Faixa etária | idade | `<11 Crianças`; `<20 Adolescentes`; `<60 Adultos`; `>59 Idosos` | texto | `SalvaAvaliacaoB`, 6341–6350 |
| RCQ | cintura, quadril | `cintura / quadril`, formatado `#,##0.00` | razão | `FrmAvaliacaoB`, 4242–4244 |
| Classificação RCQ | sexo, idade, RCQ | `TabelaRCQ(sexo, idade, rcq)` | texto | 15386–15609 |
| Peso gordo | % gordura, peso | `(%gordura / 100) * peso` | kg | 4844–4847 |
| MLG kg | peso, peso gordo | `peso - pesoGordo` | kg | 4850–4852 |
| MLG % | MLG, peso | `(MLG / peso) * 100` | % | 4851–4852 |
| Peso muscular esquelético | % músculo, peso | `(%músculo / 100) * peso` | kg | 4879–4883 |
| TMB Harris-Benedict feminino | sexo, peso, altura, idade | `Round(655.1 + 9.563*peso + 1.85*altura - 4.676*idade)` | kcal/dia | 11550–11566 |
| TMB Harris-Benedict masculino | sexo, peso, altura, idade | `Round(66.5 + 13.75*peso + 5.003*altura - 6.755*idade)` | kcal/dia | 11550–11566 |
| Peso alvo por gordura | peso atual, gordura atual, gordura alvo | `pesoMagroAtual = pesoAtual - pesoAtual*(gorduraAtual/100)`; `pesoAlvo = (gorduraAlvo/100)*pesoMagroAtual/(1-(gorduraAlvo/100)) + pesoMagroAtual` | kg | 6069–6073 |
| Controle de peso | peso alvo, peso atual | `pesoAlvo - pesoAtual` | kg | 6072–6073 e 6097–6098 |
| Peso alvo por IMC | IMC alvo, altura cm | `IMCAlvo * (altura/100)^2` | kg | 6091–6098 |
| Classificação IMC | IMC | `<18.5 Abaixo do Peso`; `<25 Peso Normal`; `<30 Sobrepeso`; `<35 Obesidade Classe I`; `<40 Obesidade Classe II`; `>=40 Obesidade Classe III` | texto | `TabelaIMC`, 11977–11998 |
| Classificação gordura | % gordura, sexo, idade | `TabelaGordura` com faixas por sexo/idade | texto | 14483–14907 |
| Gordura alvo | sexo, idade, % gordura | `TabelaGorduraAlvo` retorna alvo numérico por sexo/idade/faixa | % | 14913–15242 |
| TMB OMS | sexo, peso, idade | `tmbOMS(...)` | kcal/dia | chamada em 5453–5455; fórmula interna **A CONFIRMAR** |
| DRI adolescentes | sexo, peso, altura, idade | `DRIS_Adolescentes(...)` | kcal/dia | chamada em 5458–5460; fórmula interna **A CONFIRMAR** |
| DRI adultos | sexo, peso, altura, idade | `DRIS_Adultos(...)` | kcal/dia | chamada em 5462–5464; fórmula interna **A CONFIRMAR** |
| IMC no fluxo R | valor informado/importado | `FrmBioimpR` copia `TxtIMC` para `FrmAvaliacaoB.TxtIMC`; fórmula automática não evidenciada nesse trecho | kg/m² | 5106–5113; **A CONFIRMAR** |

## Regras de negócio

- Cadastro valida nome, sexo e data; máscaras para telefone, celular, CEP, CPF, RG e datas.
- Campos numéricos usam `bloquear`/`KeyPress` para restringir caracteres.
- RCQ exige cintura e quadril diferentes de zero.
- Fluxo R copia classificações de `OptionButton.Caption` para IMC, gordura, músculo e visceral (`FrmBioimpR`, 5115–5154), portanto há seleção manual.
- Necessidade energética permite Bioimpedância, Harris-Benedict, OMS, DRI adolescentes e DRI adultos; fórmulas internas de OMS/DRI ficam **A CONFIRMAR**.
- Laudo usa offsets da aba `Avaliações`; mudanças de layout quebram leitura C#/VBA.

## Fluxos de usuário

1. Cadastro de cliente → cálculo de idade → criação de pasta `CLIENTES/<Id Nome>`.
2. Avaliação R/A/C → entrada/importação de medidas → cálculos derivados → persistência em `Avaliações`.
3. Seleção de avaliação → preenchimento de `RelatorioBM/BH` ou HTML C# → PDF.
4. Comparações/evolução por histórico de avaliações do mesmo cliente.

## Telas sugeridas

Login/tenant, dashboard, cadastro profissional, clientes, anamnese, nova avaliação R/A/C, medidas corporais, calculadoras RCQ/TMB/peso alvo, revisão de classificações, preview de laudo, histórico comparativo, exportação PDF e administração.

## Estrutura do laudo

Fonte: `RelatorioBM_Preenche`/`RelatorioBH_Preenche`, `_prd_evidence/olevba.txt:6915-7180`, e `SistemaPSM.AddIn/Laudo`.

Seções: cabeçalho; composição corporal; indicadores IMC/gordura/músculo/visceral/RCQ com classificação e intervalo; TMB/GET; metas; medidas corporais; detalhamento; observações.

## Requisitos não funcionais

- LGPD: criptografia, controle de acesso, auditoria e retenção.
- Rastreabilidade de regras e versão da fonte.
- Preservação de arredondamentos/formatos VBA (`Format(..., "#,##0.00")`, `Round`).
- PDF fiel ao modelo visual e identidade configurável.
- Anexos privados com validação de tipo/tamanho.
- Backup automático, multiusuário e logs de geração/cálculo.

## Rastreabilidade resumida

| Item | Fonte |
|---|---|
| Estrutura de abas | `_prd_evidence/xml_summary.json` |
| Fórmulas de célula | `_prd_evidence/xml_formulas.json` |
| Macros VBA | `_prd_evidence/olevba.txt` |
| Idade | `Age`, 4191–4202 |
| RCQ | `FrmAvaliacaoB`, 4242–4244 |
| Peso gordo/MLG | `TxtPGorduraAtual_Change`, 4844–4858 |
| Peso muscular | `TxtPorcentagemMuscEsqueceletico_Change`, 4879–4886 |
| Fluxo resumido/manual | `FrmBioimpR`, 5106–5154 |
| TMB Harris-Benedict | `Harris_Benedict`, 11550–11566 |
| Peso alvo | `FrmCalcPeso`, 6069–6098 |
| Persistência avaliação | `SalvaAvaliacaoB`, 6333–6570 |
| Laudo Excel | `RelatorioBM_Preenche`/`RelatorioBH_Preenche`, 6915–7180 |
| Classificação IMC | `TabelaIMC`, 11977–11998 |
| Classificações gordura/RCQ/músculo | funções `TabelaGordura*`, `TabelaRCQ*`, 14483–15690 |
| AddIn como camada de UI/PDF | `CLAUDE.md`, `README.md`, `SistemaPSM.AddIn/Laudo/*.cs` |

## A CONFIRMAR

1. Transcrição integral das faixas numéricas de `TabelaGordura`, `TabelaGorduraAlvo`, `TabelaRCQ`, `TabelaRCQ_INTERVALO`, `TabelaGorduraOMR_INTERVALO` e `TabelaMusculoEsqOMR_INTERVALO`.
2. Fórmulas internas de `tmbOMS`, `DRIS_Adolescentes` e `DRIS_Adultos`.
3. Fórmulas completas da aba `Analise C` e relação com `FrmAvaliacaoC`.
4. Captions completas dos `OptionButton` dos formulários.
5. Diferenças exatas entre modelos `RelatorioBh`, `RelatorioBm`, versões `_`, `3` e `Bhm`.
6. Fórmula automática de IMC no fluxo R, pois o trecho evidenciado copia `TxtIMC`.
7. Regras completas de GET/fatores de atividade.

## Evidências geradas

- `_prd_evidence/xml_summary.json`: abas/dimensões/fórmulas por aba.
- `_prd_evidence/xml_formulas.json`: fórmulas de célula extraídas do XML.
- `_prd_evidence/xml_samples.json`: amostras de células/headers.
- `_prd_evidence/olevba.txt`: extração textual completa do VBA via `olevba`.
- `_prd_evidence/vba_core.txt`: trechos centrais de cálculo/persistência/laudo.
- `_prd_evidence/matches.txt`: índice de ocorrências relevantes.

---

# Follow-up — Itens A CONFIRMAR resolvidos a partir do VBA

Esta seção complementa e, quando houver conflito, substitui as marcações anteriores de **A CONFIRMAR** para os itens abaixo. Fonte principal: `_prd_evidence/olevba.txt` e `_prd_evidence/followup_core.txt`.

## Fórmulas internas de TMB/necessidade energética

### GET e fatores de atividade

Fonte: `FrmNecessidadeEnergetica`, `_prd_evidence/olevba.txt:5396-5411` e `5564-5590`.

- Validação: TMB (`TxtTmB`) deve estar preenchida e diferente de zero; fator (`CbFa`) deve estar preenchido.
- Fórmula: `GET = CbFa * TMB`, formatado como `#,##0.00`.
- Fatores disponíveis no combo `CbFa`: `1`, `1.2`, `1.3`, `1.4`, `1.5`, `1.55`, `1.56`, `1.6`, `1.64`, `1.78`, `1.8`, `1.82`, `1.9`, `2.1`, `2.2`, `2.5`, `3`, `3.5`, `4`, `5`, `6`. Valor inicial: `1`.

### tmbOMS

Fonte: `Public Function tmbOMS`, `_prd_evidence/olevba.txt:11513-11548`.

| Sexo | Idade | Fórmula VBA |
|---|---:|---|
| Feminino | `< 3` | `Round((61 * PesoKg) - 51, 2)` |
| Feminino | `< 10` | `Round((22.5 * PesoKg) + 499, 2)` |
| Feminino | `< 18` | `Round((12.2 * PesoKg) + 746, 2)` |
| Feminino | `< 30` | `Round((14.7 * PesoKg) + 496, 2)` |
| Feminino | `< 60` | `Round((8.7 * PesoKg) + 829, 2)` |
| Feminino | `>= 60` | `Round((10.5 * PesoKg) + 596, 2)` |
| Masculino/outros | `< 3` | `Round((60.9 * PesoKg) - 54, 2)` |
| Masculino/outros | `< 10` | `Round((22.7 * PesoKg) + 495, 2)` |
| Masculino/outros | `< 18` | `Round((17.5 * PesoKg) + 651, 2)` |
| Masculino/outros | `< 30` | `Round((15.3 * PesoKg) + 679, 2)` |
| Masculino/outros | `< 60` | `Round((11.6 * PesoKg) + 879, 2)` |
| Masculino/outros | `>= 60` | `Round((13.5 * PesoKg) + 487, 2)` |

### DRIS_Adolescentes

Fonte: `Public Function DRIS_Adolescentes`, `_prd_evidence/olevba.txt:11568-11602`.

Observação fiel ao VBA: o IMC interno é calculado como `PesoKg / (AlturaCm ^ 2)`, sem converter altura para metros nessa linha; já os termos de altura das fórmulas usam `(AlturaCm / 100)`.

| Sexo | Condição IMC | Fórmula VBA |
|---|---:|---|
| Feminino | `IMC <= 21.8` | `Round(189 - (17.6 * IdadeAnos) + (625 * (AlturaCm / 100)) + (7.9 * PesoKg))` |
| Feminino | `IMC > 21.8` | `Round(515.8 - (26.8 * IdadeAnos) + (347 * (AlturaCm / 100)) + (12.4 * PesoKg))` |
| Masculino/outros | `IMC <= 21.8` | `Round(68 - (43.3 * IdadeAnos) + (712 * (AlturaCm / 100)) + (19.2 * PesoKg))` |
| Masculino/outros | `IMC > 21.8` | `Round(419.9 - (35.5 * IdadeAnos) + (418.9 * (AlturaCm / 100)) + (16.7 * PesoKg))` |

### DRIS_Adultos

Fonte: `Public Function DRIS_Adultos`, `_prd_evidence/olevba.txt:11603-11639`.

Observação fiel ao VBA: o IMC interno é calculado como `PesoKg / (AlturaCm ^ 2)`, sem converter altura para metros nessa linha; os termos de altura das fórmulas usam `(AlturaCm / 100)`.

| Sexo | Condição IMC | Fórmula VBA |
|---|---:|---|
| Feminino | `IMC <= 24.99` | `Round(255 - (2.35 * IdadeAnos) + (361.6 * (AlturaCm / 100)) + (9.39 * PesoKg))` |
| Feminino | `IMC > 24.99` | `Round(247 - (2.67 * IdadeAnos) + (401.5 * (AlturaCm / 100)) + (8.6 * PesoKg))` |
| Masculino/outros | `IMC <= 24.99` | `Round(204 - (4 * IdadeAnos) + (450.5 * (AlturaCm / 100)) + (11.69 * PesoKg))` |
| Masculino/outros | `IMC > 24.99` | `Round(293 - (3.8 * IdadeAnos) + (456.4 * (AlturaCm / 100)) + (10.12 * PesoKg))` |

## Tabelas de classificação e intervalos

### TabelaIMC

Fonte: `Public Function TabelaIMC`, `_prd_evidence/olevba.txt:11977-11998`.

| Condição IMC | Classificação |
|---:|---|
| `< 18.5` | Abaixo do Peso |
| `< 25` | Peso Normal |
| `< 30` | Sobrepeso |
| `< 35` | Obesidade Classe I |
| `< 40` | Obesidade Classe II |
| `>= 40` | Obesidade Classe III |

### TabelaRCQ

Fonte: `Public Function TabelaRCQ`, `_prd_evidence/olevba.txt:15386-15617`.

| Sexo | Idade | Risco Baixo | Risco Moderado | Risco Alto | Muito Alto |
|---|---:|---:|---:|---:|---:|
| Masculino | `<20` | N/A | N/A | N/A | N/A |
| Masculino | `20-29` | `<=0.83` | `>0.83 e <=0.88` | `>0.88 e <=0.94` | `>0.94` |
| Masculino | `30-39` | `<=0.84` | `>0.84 e <=0.91` | `>0.91 e <=0.96` | `>0.96` |
| Masculino | `40-49` | `<=0.88` | `>0.88 e <=0.95` | `>0.95 e <=1.00` | `>1.00` |
| Masculino | `50-59` | `<0.90` | `>=0.90 e <=0.96` | `>0.96 e <=1.02` | `>1.02` |
| Masculino | `60-69` | `<=0.91` | `>0.91 e <=0.98` | `>0.98 e <=1.03` | `>1.03` |
| Masculino | `>69` | N/A | N/A | N/A | N/A |
| Feminino | `<20` | N/A | N/A | N/A | N/A |
| Feminino | `20-29` | `<=0.71` | `>0.71 e <=0.77` | `>0.77 e <=0.82` | `>0.82` |
| Feminino | `30-39` | `<=0.72` | `>0.72 e <=0.78` | `>0.78 e <=0.84` | `>0.84` |
| Feminino | `40-49` | `<=0.73` | `>0.73 e <=0.79` | `>0.79 e <=0.87` | `>0.87` |
| Feminino | `50-59` | `<=0.74` | `>0.74 e <=0.81` | `>0.81 e <=0.88` | `>0.88` |
| Feminino | `60-69` | `<=0.76` | `>0.76 e <=0.83` | `>0.83 e <=0.90` | `>0.90` |
| Feminino | `>69` | N/A | N/A | N/A | N/A |

### TabelaRCQ_INTERVALO

Fonte: `Public Function TabelaRCQ_INTERVALO`, `_prd_evidence/olevba.txt:15619-15690`.

| Sexo | Idade | Intervalo retornado |
|---|---:|---|
| Masculino | `<20` | N/A |
| Masculino | `20-29` | `0,83 - 0,88` |
| Masculino | `30-39` | `0,84 - 0,91` |
| Masculino | `40-49` | `0,88 - 0,95` |
| Masculino | `50-59` | `0,90 - 0,96` |
| Masculino | `60-69` | `0,91 - 0,98` |
| Masculino | `>69` | N/A |
| Feminino | `<20` | N/A |
| Feminino | `20-29` | `0,71 - 0,77` |
| Feminino | `30-39` | `0,72 - 0,78` |
| Feminino | `40-49` | `0,73 - 0,79` |
| Feminino | `50-59` | `0,74 - 0,81` |
| Feminino | `60-69` | `0,76 - 0,83` |
| Feminino | `>69` | N/A |

### TabelaGorduraOMR_INTERVALO

Fonte: `Public Function TabelaGorduraOMR_INTERVALO`, `_prd_evidence/olevba.txt:15251-15310`.

| Sexo | Idade | Intervalo retornado |
|---|---:|---|
| Masculino | `<20` | N/A |
| Masculino | `20-39` | `8 - 19,9` |
| Masculino | `40-59` | `11 - 21,9` |
| Masculino | `60-79` | `13 - 24,9` |
| Masculino | `>79` | N/A |
| Feminino | `<20` | N/A |
| Feminino | `20-39` | `21 - 32,9` |
| Feminino | `40-59` | `23 - 33,9` |
| Feminino | `60-79` | `24 - 35,9` |
| Feminino | `>79` | N/A |

### TabelaMusculoEsqOMR_INTERVALO

Fonte: `Public Function TabelaMusculoEsqOMR_INTERVALO`, `_prd_evidence/olevba.txt:15320-15380`.

| Sexo | Idade | Intervalo retornado |
|---|---:|---|
| Masculino | `<18` | N/A |
| Masculino | `18-39` | `33,3 - 39,3` |
| Masculino | `40-59` | `33,1 - 39,1` |
| Masculino | `60-80` | `32,9 - 38,9` |
| Masculino | `>80` | N/A |
| Feminino | `<18` | N/A |
| Feminino | `18-39` | `24,3 - 30,3` |
| Feminino | `40-59` | `24,1 - 30,1` |
| Feminino | `60-80` | `23,9 - 29,9` |
| Feminino | `>80` | N/A |

### TabelaGordura

Fonte: `Public Function TabelaGordura`, `_prd_evidence/olevba.txt:14483-14912`.

#### Masculino

| Idade | Faixa de % gordura | Classificação |
|---:|---:|---|
| `<18` | `<5` | Muito Baixo |
| `<18` | `>=5 e <=10` | Baixo |
| `<18` | `>10 e <=20` | Ótimo |
| `<18` | `>20 e <=25` | Moderadamente Alto |
| `<18` | `>25 e <=31` | Alto |
| `<18` | `>31` | Muito Alto |
| `<=25` | `<7` | Excelente |
| `<=25` | `>=7 e <11` | Bom |
| `<=25` | `>=11 e <13.5` | Acima da Média |
| `<=25` | `>=13.5 e <16.5` | Média |
| `<=25` | `>=16.5 e <20` | ABaixo da Média |
| `<=25` | `>=20 e <25` | Ruim |
| `<=25` | `>=25` | Muito Ruim |
| `<=35` | `<11.5` | Excelente |
| `<=35` | `>=11.5 e <15.5` | Bom |
| `<=35` | `>=15.5 e <18` | Acima da Média |
| `<=35` | `>=18 e <21` | Média |
| `<=35` | `>=21 e <24.5` | ABaixo da Média |
| `<=35` | `>=24.5 e <28` | Ruim |
| `<=35` | `>=28` | Muito Ruim |
| `<=45` | `<15` | Excelente |
| `<=45` | `>=15 e <18.5` | Bom |
| `<=45` | `>=18.5 e <21` | Acima da Média |
| `<=45` | `>=21 e <23.5` | Média |
| `<=45` | `>=23.5 e <26` | ABaixo da Média |
| `<=45` | `>=26 e <29.5` | Ruim |
| `<=45` | `>=29.5` | Muito Ruim |
| `<=55` | `<17` | Excelente |
| `<=55` | `>=17 e <20.5` | Bom |
| `<=55` | `>=20.5 e <23.5` | Acima da Média |
| `<=55` | `>=23.5 e <25.5` | Média |
| `<=55` | `>=25.5 e <27.5` | ABaixo da Média |
| `<=55` | `>=27.5 e <31` | Ruim |
| `<=55` | `>=31` | Muito Ruim |
| `<=65` | `<19` | Excelente |
| `<=65` | `>=19 e <21.5` | Bom |
| `<=65` | `>=21.5 e <23.5` | Acima da Média |
| `<=65` | `>=23.5 e <25.5` | Média |
| `<=65` | `>=25.5 e <27.5` | ABaixo da Média |
| `<=65` | `>=27.5 e <31` | Ruim |
| `<=65` | `>=31` | Muito Ruim |
| `>65` | qualquer | N/A |

#### Feminino

| Idade | Faixa de % gordura | Classificação |
|---:|---:|---|
| `<18` | `<12` | Muito Baixo |
| `<18` | `>=12 e <=15` | Baixo |
| `<18` | `>15 e <=25` | Ótimo |
| `<18` | `>25 e <=30` | Moderadamente Alto |
| `<18` | `>30 e <=36` | Alto |
| `<18` | `>36` | Muito Alto |
| `<=25` | `<16.5` | Excelente |
| `<=25` | `>=16.5 e <19.5` | Bom |
| `<=25` | `>=19.5 e <22.5` | Acima da Média |
| `<=25` | `>=22.5 e <25.5` | Média |
| `<=25` | `>=25.5 e <28.5` | ABaixo da Média |
| `<=25` | `>=28.5 e <32` | Ruim |
| `<=25` | `>=32` | Muito Ruim |
| `<=35` | `<17` | Excelente |
| `<=35` | `>=17 e <20.5` | Bom |
| `<=35` | `>=20.5 e <23.5` | Acima da Média |
| `<=35` | `>=23.5 e <26` | Média |
| `<=35` | `>=26 e <30` | ABaixo da Média |
| `<=35` | `>=30 e <34.5` | Ruim |
| `<=35` | `>=34.5` | Muito Ruim |
| `<=45` | `<19.5` | Excelente |
| `<=45` | `>=19.5 e <23.5` | Bom |
| `<=45` | `>=23.5 e <26.5` | Acima da Média |
| `<=45` | `>=26.5 e <29.5` | Média |
| `<=45` | `>=29.5 e <32.5` | ABaixo da Média |
| `<=45` | `>=32.5 e <37` | Ruim |
| `<=45` | `>=37` | Muito Ruim |
| `<=55` | `<22` | Excelente |
| `<=55` | `>=22 e <25.5` | Bom |
| `<=55` | `>=25.5 e <28.5` | Acima da Média |
| `<=55` | `>=28.5 e <31.5` | Média |
| `<=55` | `>=31.5 e <34.5` | ABaixo da Média |
| `<=55` | `>=34.5 e <38.5` | Ruim |
| `<=55` | `>=38.5` | Muito Ruim |
| `<=65` | `<23` | Excelente |
| `<=65` | `>=23 e <26.5` | Bom |
| `<=65` | `>=26.5 e <29.5` | Acima da Média |
| `<=65` | `>=29.5 e <32.5` | Média |
| `<=65` | `>=32.5 e <35.5` | ABaixo da Média |
| `<=65` | `>=35.5 e <38.5` | Ruim |
| `<=65` | `>=38.5` | Muito Ruim |
| `>65` | qualquer | N/A |

### TabelaGorduraAlvo

Fonte: `Public Function TabelaGorduraAlvo`, `_prd_evidence/olevba.txt:14913-15249`.

A função retorna um percentual alvo numérico. Observação: foram preservadas condições aparentemente redundantes ou inalcançáveis do VBA, como `ElseIf Pgordura < 23.5` repetido no bloco masculino `<=55` e `ElseIf Pgordura < 18.5` após `<19.5` no bloco feminino `<=45`.

| Sexo | Idade | Condição % gordura | Alvo |
|---|---:|---:|---:|
| Masculino | `<=25` | `<7` | 5 |
| Masculino | `<=25` | `<11` | 9 |
| Masculino | `<=25` | `<14` | 12.5 |
| Masculino | `<=25` | `<=17` | 15 |
| Masculino | `<=25` | `<20` | 15 |
| Masculino | `<=25` | `<25` | 15 |
| Masculino | `<=25` | `>25` | 15 |
| Masculino | `<=35` | `<11.5` | 9.5 |
| Masculino | `<=35` | `<15.5` | 13.5 |
| Masculino | `<=35` | `<18` | 17 |
| Masculino | `<=35` | `<21` | 19 |
| Masculino | `<=35` | `<24` | 19 |
| Masculino | `<=35` | `<27.5` | 19 |
| Masculino | `<=35` | `>27.5` | 19 |
| Masculino | `<=45` | `<15` | 12 |
| Masculino | `<=45` | `<18.5` | 17 |
| Masculino | `<=45` | `<21` | 20 |
| Masculino | `<=45` | `<23.5` | 22 |
| Masculino | `<=45` | `<26` | 22 |
| Masculino | `<=45` | `<29.5` | 22 |
| Masculino | `<=45` | `>29.5` | 22 |
| Masculino | `<=55` | `<17` | 14 |
| Masculino | `<=55` | `<20.5` | 19 |
| Masculino | `<=55` | `<23.5` | 22 |
| Masculino | `<=55` | `<23.5` | 25 |
| Masculino | `<=55` | `<25.5` | 25 |
| Masculino | `<=55` | `<27.5` | 25 |
| Masculino | `<=55` | `>31` | 25 |
| Masculino | `<=65` | `<19` | 16 |
| Masculino | `<=65` | `<21.5` | 20.5 |
| Masculino | `<=65` | `<23.5` | 22.5 |
| Masculino | `<=65` | `<25.5` | 24.5 |
| Masculino | `<=65` | `<27.5` | 24.5 |
| Masculino | `<=65` | `<31` | 24.5 |
| Masculino | `<=65` | `>31` | 24.5 |
| Feminino | `<=25` | `<16.5` | 14.5 |
| Feminino | `<=25` | `<19.5` | 18 |
| Feminino | `<=25` | `<22.5` | 21 |
| Feminino | `<=25` | `<=25.5` | 24 |
| Feminino | `<=25` | `<28.5` | 24 |
| Feminino | `<=25` | `<32` | 24 |
| Feminino | `<=25` | `>32` | 24 |
| Feminino | `<=35` | `<17` | 15 |
| Feminino | `<=35` | `<20.5` | 19 |
| Feminino | `<=35` | `<23.5` | 22 |
| Feminino | `<=35` | `<26` | 24.5 |
| Feminino | `<=35` | `<30` | 24.5 |
| Feminino | `<=35` | `<34.5` | 24.5 |
| Feminino | `<=35` | `>34.5` | 24.5 |
| Feminino | `<=45` | `<19.5` | 17.5 |
| Feminino | `<=45` | `<18.5` | 21.5 |
| Feminino | `<=45` | `<26.5` | 25 |
| Feminino | `<=45` | `<29.5` | 28 |
| Feminino | `<=45` | `<32.5` | 28 |
| Feminino | `<=45` | `<37` | 28 |
| Feminino | `<=45` | `>37` | 28 |
| Feminino | `<=55` | `<22` | 19 |
| Feminino | `<=55` | `<25.5` | 24 |
| Feminino | `<=55` | `<28.5` | 27 |
| Feminino | `<=55` | `<31.5` | 30 |
| Feminino | `<=55` | `<34.5` | 30 |
| Feminino | `<=55` | `<38.5` | 30 |
| Feminino | `<=55` | `>38.5` | 30 |
| Feminino | `<=65` | `<23` | 20 |
| Feminino | `<=65` | `<26.5` | 25 |
| Feminino | `<=65` | `<29.5` | 28 |
| Feminino | `<=65` | `<32.5` | 31 |
| Feminino | `<=65` | `<35.5` | 31 |
| Feminino | `<=65` | `<38.5` | 31 |
| Feminino | `<=65` | `>38.5` | 31 |

## Aba Analise C — fórmulas de célula

Fonte: `_prd_evidence/xml_formulas.json`, extraído do XML da planilha. A conexão com `FrmAvaliacaoC` é evidenciada pelo fluxo `Bioimpedância C` e pela existência da aba `Analise C`; o significado clínico de cada bloco deve ser preservado conforme rótulos da própria aba ao implementar.

| Célula | Fórmula | Valor cache extraído | Descrição operacional |
|---|---|---:|---|
| G5 | `=1-F5` | 0.77100000000000002 | Complemento percentual de F5. |
| G6 | `=1-F6` | 1 | Complemento percentual de F6. |
| G7 | `=` | 1 | Fórmula vazia/placeholder no XML com valor cache 1. |
| G8 | `=1-F8` | 0 | Complemento percentual de F8. |
| H11 | `=$D$12` | 17.100000000000001 | Espelha D12. |
| O11 | `=IF($G$2="Masculino",P11,Q11)` | 0.19 | Seleciona coeficiente masculino/feminino conforme G2. |
| O12 | `=IF($G$2="Masculino",P12,Q12)` | 0.08 | Seleciona coeficiente masculino/feminino conforme G2. |
| H13 | `=SUM($L$11:$L$15)-$H$11-$H$12` | 101.4 | Diferença entre soma L11:L15 e H11/H12. |
| O13 | `=IF($G$2="Masculino",P13,Q13)` | 0.03 | Seleciona coeficiente masculino/feminino conforme G2. |
| O14 | `=IF($G$2="Masculino",P14,Q14)` | 0.3 | Seleciona coeficiente masculino/feminino conforme G2. |
| L15 | `=SUM(L11:L14)` | 60 | Soma L11:L14. |
| O15 | `=SUM(O11:O14)` | 0.60000000000000009 | Soma O11:O14. |
| H16 | `=$F$5` | 0.22899999999999998 | Espelha F5. |
| H18 | `=SUM($O$11:$O$15)-$H$16-$H$17` | 0.95600000000000018 | Diferença entre soma O11:O15 e H16/H17. |
| H22 | `=$F$5` | 0.22899999999999998 | Espelha F5. |
| H24 | `=SUM($O$27:$O$30)-$H$22-$H$23` | 0.95600000000000018 | Diferença entre soma O27:O30 e H22/H23. |
| O27 | `=IF($G$2="Masculino",P27,Q27)` | 0.22500000000000001 | Seleciona coeficiente masculino/feminino conforme G2. |
| O28 | `=IF($G$2="Masculino",P28,Q28)` | 0.11 | Seleciona coeficiente masculino/feminino conforme G2. |
| O29 | `=IF($G$2="Masculino",P29,Q29)` | 0.26500000000000001 | Seleciona coeficiente masculino/feminino conforme G2. |
| E30 | `=1-D30` | 0.8 | Complemento percentual de D30. |
| O30 | `=SUM(O27:O29)` | 0.60000000000000009 | Soma O27:O29. |

## IMC no fluxo R

Fonte: buscas em `_prd_evidence/followup_matches.txt` e trechos `_prd_evidence/olevba.txt:5106-5113`, `5253-5256`, `5294-5307`, `6095`, `11573`, `11608`.

- No fluxo R (`FrmBioimpR`), o VBA copia `TxtIMC` para `FrmAvaliacaoB.TxtIMC`: `FrmAvaliacaoB.TxtIMC = Format(TxtIMC, "#,##0.00")`.
- Foram encontrados cálculos com `^ 2` para peso alvo por IMC e para IMC interno das funções DRI, mas não foi encontrado, no fluxo R extraído, cálculo automático do tipo `TxtIMC = peso / altura²`.
- Portanto, para o fluxo R, o PRD deve tratar IMC como valor informado/importado no formulário resumido, não como cálculo automático evidenciado.

## Itens que permanecem A CONFIRMAR após follow-up

1. Captions completas dos `OptionButton` dos formulários VBA, porque o trecho textual evidencia a cópia das captions, mas não lista todas as propriedades visuais dos controles.
2. Diferenças visuais exatas entre modelos `RelatorioBh`, `RelatorioBm`, versões `_`, `3` e `Bhm`, além dos campos já mapeados.
3. Significado clínico/semântico detalhado de cada célula da aba `Analise C` além das fórmulas XML e rótulos disponíveis; as fórmulas foram extraídas, mas a nomenclatura final deve ser validada contra a interface do fluxo C.