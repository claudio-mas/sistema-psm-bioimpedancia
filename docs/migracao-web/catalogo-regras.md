# Catálogo de regras — migração web

Data: 2026-09-27. Status: levantamento estático para revisão; não é homologação clínica ou de execução.

Fonte de referência: XLSM identificado em [evidências](evidencias.md), VBA extraído dele e código C# atual. A consolidação de produto está aprovada; decisões de tratamento das divergências deste levantamento permanecem abertas.

## Como ler

- **Observado:** expressão ou fluxo encontrado no código atual, sem executar macros.
- **Proposto:** comportamento sugerido para a aplicação web, ainda sujeito à especificação final.
- **Divergência:** comportamento que não pode ser portado ou corrigido silenciosamente; ver [registro de divergências](divergencias-e-homologacao.md).
- As referências `Módulo:linha` são da extração atual, não das linhas do olevba.txt antigo. As rotinas de cálculo e classificação estão transcritas no [anexo de fontes](anexo-fontes-regras.md).
- Unidades e campos de origem estão no [dicionário](dicionario-dados.md). Não inferir uma regra clínica pelo nome de uma variável ou comentário.

## 1. Identificação, cadastro e persistência

| ID | Entradas | Comportamento observado / saída | Fonte |
|---|---|---|---|
| CAD-01 | Nome, sexo, nascimento | Salvar exige nome não vazio, sexo diferente de vazio/Selecione e nascimento não vazio/máscara. O nome é normalizado com WorksheetFunction.Trim. Data inválida pode ser gravada vazia no trecho de conversão; validação web deve ser decidida explicitamente. | FrmCadastro_C.frm:184, 241, 249 |
| CAD-02 | Nascimento; relógio atual | Age usa diferença entre anos e subtrai um se o aniversário ainda não ocorreu; IsNull retorna 0. Usa Now/Date, não a data da avaliação. É chamada só ao salvar o cadastro (grava Clientes!E); avaliações e anamneses novas copiam essa coluna, sem recálculo. DIV-15. | Idade.bas:2; FrmCadastro_C:552; FrmCliente:281/331/379/782 |
| CAD-03 | Idade inteira | Crianças <11; adolescentes <20; adultos <60; idosos >59. Faixa é gravada junto da avaliação. | SalvaAvaliacaoA/B/C; exemplo AvaliacaoC.bas:29 |
| CAD-04 | Identificador legado | Cadastro existente é localizado por Seq na coluna A; altera a linha encontrada. Novo registro incrementa Seq. A2. Avaliações usam B2; anamnese usa C2. | FrmCadastro_C.BtnSalvar_Click; FrmAvaliacaoA/B/C.CommandButton179_Click; FrmAnamnese.BtnSalvar_Click |
| CAD-05 | CPF, RG, CEP, telefone, endereço | Há máscaras e filtros de tecla; isso não comprova validação de dígito verificador ou de duplicidade. RG/CPF e partes do endereço podem ser convertidos por CDbl quando IsNumeric. | FrmCadastro_C.frm:297–345, eventos KeyPress |
| CAD-06 | Foto e anexos | Campos armazenam caminhos de arquivos associados à pasta do paciente; a migração deve mapear referências e existência dos arquivos, se sua inclusão for aprovada. | FrmCadastro_C.BtnSalvar_Click; Clientes.CriaPasta_Nome |
| DAT-01 | Data da avaliação | Grava B=data, C=mês em maiúsculas, D=ano, O=data de período. A coluna DD armazena hora no fluxo C. Não há política de fuso horário explícita. | FrmAvaliacaoA/B/C.CommandButton179_Click; AvaliacaoC.bas:31 |
| DAT-02 | Medidas opcionais | Várias rotinas gravam 0 quando o campo não é numérico ou é zero; algumas deixam a coluna sem gravação. Leitores C# também convertem ausente em 0. | SalvaAvaliacaoA/B/C; LaudoRepositorio.Num |

**Proposta de persistência web:** representar ausente com nulo e manter origem/estado do campo; salvar revisão com versão de regra e resultado. Requisitos mínimos para finalizar uma avaliação devem ser definidos por protocolo, sem transformar zeros legados em prova de medição.

## 2. Matriz dos protocolos

| Aspecto | A | B/R | C |
|---|---|---|---|
| Formulário principal | FrmAvaliacaoA | FrmAvaliacaoB | FrmAvaliacaoC |
| Entrada auxiliar | FrmBioimpedancia e calculadoras | FrmBioimpR | FrmBioimpC + OCR |
| IMC | Calcula de peso e altura; classificação por idade | Informado no auxiliar | Informado/importado no auxiliar |
| Gordura | Percentual informado; massa e classificação derivadas | Percentual e classificação informados; massa derivada | Kg informado/importado; percentual calculado; classificação selecionada |
| Massa livre | Peso − massa gorda | Peso − massa gorda | Valor informado/importado; fallback adicional no laudo |
| Óssea/residual/muscular | Cálculos condicionais por idade e diâmetros | Sem gravação equivalente nas rotinas inspecionadas | Óssea/muscular informadas; percentuais calculados; residual sem gravação na rotina inspecionada |
| RCQ | Calculado e classificado | Calculado e classificado | Calculado e classificado |
| Energia/metas | Calculadoras compartilhadas | Calculadoras compartilhadas | Calculadoras compartilhadas |
| Segmentar/impedância/pontuação/hora | Sem gravação nas rotinas inspecionadas | Sem gravação nas rotinas inspecionadas | Campos específicos |

Esta matriz descreve os caminhos encontrados. Não autoriza a importação automática dos resultados de um protocolo para outro nem presume que todos os campos da aba sejam usados nos três.

## 3. Cálculos de avaliação

| ID | Entradas e unidades | Regra observada | Guardas, precisão e fonte |
|---|---|---|---|
| AV-01 | Peso kg; altura cm; idade | No fluxo A, IMC = peso / (altura/100)². Para idade <5, grava 0 e N/A. | Peso e altura não vazios/não zero; Format 2 casas. FrmAvaliacaoA.CommandButton207_Click:260 |
| AV-02 | IMC; idade | A: 5–19 usa variável global ImcDesc; 20–59 usa TabelaIMC; >=60 usa IMCidoso. | ImcDesc só é atribuída por Imc_CA_CAM, que não tem chamadores: a classificação de 5–19 anos sai sempre vazia; DIV-03. FrmAvaliacaoA:296/607 |
| AV-03 | Peso kg; gordura % | A/B: gordura kg = gordura% × peso/100; massa livre kg = peso − gordura kg; massa livre% = massa livre kg/peso×100. | Eventos aplicam Format de 2 casas em etapas, podendo realimentar valores arredondados. FrmAvaliacaoA:749; FrmAvaliacaoB:642 |
| AV-04 | Peso kg; músculo esquelético % | B: músculo esquelético kg = percentual × peso/100. | Format 2 casas. FrmAvaliacaoB.TxtPorcentagemMuscEsqueceletico_Change:677 |
| AV-05 | Cintura e quadril cm | RCQ = cintura/quadril; texto de classificação por sexo e idade. | Campos não zero; razão formatada em 2 casas antes da classificação. CommandButton114_Click dos três formulários |
| AV-06 | Peso total; kg de gordura, osso, proteína, água, músculo e músculo esquelético | C: percentual de cada componente = kg/peso×100; percentual do peso = 100. | Só recalcula com peso numérico >0 e componente numérico. Format 2 casas. FrmBioimpC.RecalcComposicao:12 |
| AV-07 | Valores e seleções do auxiliar C | Aplicar copia gordura kg explicitamente, massa livre informada, percentuais de água/proteína e os pares de óssea/músculo; classificações vêm de Caption. | A ordem de eventos pode afetar campos derivados. Não equiparar texto selecionado a fórmula automática. FrmBioimpC.CommandButton179_Click:27 |
| AV-08 | Idade; sexo; peso; altura; punho e fêmur | A: para 20–59 anos com ambos os diâmetros não zero, calcula residual, óssea e muscular. Fora dessas condições atribui zero. | FrmAvaliacaoA.CommandButton114_Click:44 |
| AV-09 | Sexo; peso kg | Residual masculino = Round(peso×0,241;2); demais = Round(peso×0,209;2). | Ramo Else do legado; não é validação de domínio. Equações_Avaliações.PesoResidual:297 |
| AV-10 | Altura cm; diâmetros de punho/fêmur | Óssea = Round(3,02×((altura/100)²×(punho/1000)×(fêmur/1000)×400)^0,712;2). | Conversões indicam diâmetros em mm; unidade visível do controle precisa ser conferida. Equações_Avaliações.PesoOsseo:285 |
| AV-11 | Peso, gordura, óssea e residual kg | Muscular = peso − (gordura + óssea + residual). | Função sem Round; chamador formata. Equações_Avaliações.PesoMuscular:292 |
| AV-12 | Peso atual, gordura atual%, gordura alvo% | Massa livre = peso×(1−gordura atual/100); peso alvo efetivo = massa livre/(1−gordura alvo/100); controle = peso alvo formatado − peso atual. | Exige gordura atual/alvo não zero; denominador zero não é tratado explicitamente. Correto no fluxo B; em A/C, os cliques de opção recarregam as entradas de FrmAvaliacaoB; DIV-02. FrmCalcPeso.BtnCalcular_Click:10 |
| AV-13 | IMC alvo; altura | Peso alvo = IMC alvo×(altura/100)². | Lê diretamente FrmAvaliacaoB.TxtAltura, mesmo sendo calculadora compartilhada; os cliques de opção também recarregam peso, gordura e IMC de B. Em A/C sem B carregado, resulta 0,00; DIV-02. FrmCalcPeso:68, 134–166 |
| AV-14 | Sexo, idade, gordura atual % | Ao abrir a calculadora de peso: IMC alvo = 22,5 e gordura alvo = TabelaGorduraAlvo(sexo, idade, gordura atual). Os cliques de opção repetem o preenchimento com dados de FrmAvaliacaoB. | Valores sugeridos e editáveis no formulário. FrmAvaliacaoA:377; FrmAvaliacaoB:226; FrmAvaliacaoC:224–225; FrmCalcPeso:147–148, 165–166; DIV-16 |

### Classificações e intervalos

| ID | Função | Dimensões / contrato observado |
|---|---|---|
| CL-01 | TabelaIMC | Limites sequenciais <18,5; <25; <30; <35; <40; >=40. Textos: Abaixo do Peso, Peso Normal, Sobrepeso, Obesidade Classe I/II/III. |
| CL-02 | IMCidoso | <22 Desnutrição; <=27 Eutrofia - Peso Normal; >27 Obesidade. |
| CL-03 | TabelaGordura | Gordura%, sexo e idade; tabelas extensas com condições inclusivas/exclusivas. O corpo atual está preservado no anexo. Usada no fluxo A. |
| CL-04 | TabelaGorduraAlvo | Sexo, idade, percentual atual; faixas de metas no código. Preservar as tabelas completas do anexo. Chamadores: abertura da calculadora de peso em A/B/C (ex.: FrmAvaliacaoC:224) e cliques de opção de FrmCalcPeso (:147/165, com dados de B). O resultado é sugestão editável; ver AV-14. |
| CL-05 | TabelaRCQ | Sexo, idade, RCQ; faixas por década entre 20 e 69 anos e N/A fora do intervalo, conforme ramificações. Preservar o caso masculino 50–59 com comparação `<0,90`, distinto de `<=` em outras faixas. |
| CL-06 | TabelaRCQ_INTERVALO, TabelaGorduraOMR_INTERVALO, TabelaMusculoEsqOMR_INTERVALO | Retornam textos de intervalo. São diferentes das funções de classificação; não calcular limites a partir de rótulos de gráficos. |
| CL-07 | Imc_CA_CAM | Tabela pediátrica presente em IMC.bas, devolve percentil e altera ImcDesc. Não foi encontrada chamada textual nos módulos extraídos; manter como função declarada com integração não demonstrada. |
| CL-08 | Classificações manuais B/C | Texto de Caption selecionada; documento histórico define sete grupos C com Baixo/Saudável/Alto/Excelente. Quatro opções não têm ramo de transferência e mantêm a classificação anterior: Opt4Alto (proteína), Opt5Excel (água), Opt6Alto (massa muscular) e Opt7Alto (músculo esquelético); DIV-05. Legendas a conferir no Excel. |

As fórmulas/classificações acima são descrição do software existente. Sua adequação clínica e eventuais atualizações exigem validação do responsável pela clínica.

## 4. Energia

Entradas: sexo, peso em kg, altura em cm, idade e método. A calculadora exige peso e altura não vazios/não zero, inclusive para métodos cuja fórmula não usa altura. A opção Bioimpedância copia o valor do formulário correspondente ao protocolo. Fonte: FrmNecessidadeEnergetica.CommandButton2_Click:33.

| ID | Método | Regra |
|---|---|---|
| EN-01 | Harris-Benedict | Feminino: Round(655,1+9,563×peso+1,85×altura−4,676×idade). Else: Round(66,5+13,75×peso+5,003×altura−6,755×idade). Arredonda a inteiro; apresentação com 2 casas. Energia.bas:41. |
| EN-02 | OMS | Por sexo e seis faixas de idade; coeficientes abaixo. Round a 2 casas. Energia.bas:4. |
| EN-03 | DRI adolescentes/adultos | Seleciona equação por sexo e IMC interno. Expressões completas no anexo. O IMC interno usa altura em cm ao quadrado, enquanto os termos de altura usam metros. Resultado: o ramo de sobrepeso nunca executa, e o legado aplica sempre a equação de peso normal; DIV-01. Energia.bas:59 e 94. |
| EN-04 | GET | GET = fator × TMB; Format 2 casas. Exige TMB não vazia/não zero e fator preenchido. FrmNecessidadeEnergetica:15. |

OMS — saída `a × peso + b`:

| Idade | Feminino a; b | Else a; b |
|---|---|---|
| <3 | 61; −51 | 60,9; −54 |
| <10 | 22,5; 499 | 22,7; 495 |
| <18 | 12,2; 746 | 17,5; 651 |
| <30 | 14,7; 496 | 15,3; 679 |
| <60 | 8,7; 829 | 11,6; 879 |
| >=60 | 10,5; 596 | 13,5; 487 |

Lista de fatores oferecida pelo formulário: 1; 1,2; 1,3; 1,4; 1,5; 1,55; 1,56; 1,6; 1,64; 1,78; 1,8; 1,82; 1,9; 2,1; 2,2; 2,5; 3; 3,5; 4; 5; 6. Padrão 1. Isso comprova os itens oferecidos, não a rejeição de qualquer valor digitado fora da lista.

## 5. Anamnese

| ID | Entradas | Regra observada | Fonte |
|---|---|---|---|
| AN-01 | Idade; sexo; método | FC máxima: fórmulas abaixo, RoundUp a inteiro, depois Format 2 casas. | FrmFcMax.CommandButton179_Click:11 |
| AN-02 | FC máxima; FC repouso | FC reserva = máxima − repouso. O fluxo exige repouso não zero. | FrmAnamnese.CommandButton185_Click:158 |
| AN-03 | Peso kg; mL por kg | Água ideal = peso×mL/kg; Format 2 casas. Mensagens de campos vazios não encerram explicitamente a rotina. | FrmAgua.CommandButton1_Click:14 |
| AN-04 | Opção de escala de urina 1–8 | 1/2 Bem Hidratado; 3/4 Levemente; 5/6 Moderadamente; 7/8 Severamente Desidratado. O código captura opção 4 no ramo que grava escala 3, e a reabertura não reconhece a escala 8; DIV-04. | FrmUrina.CommandButton1_Click:10 |
| AN-05 | Pressão sistólica/diastólica | Classificação por cadeia ordenada de comparações com AND; fonte completa no anexo. O Estágio 3 exige as duas pressões no limite (≥180 e ≥110) e a Sistólica Isolada é inalcançável para sistólica de 140 a 179; DIV-07. | FrmAnamnese.CommandButton184_Click:108 |
| AN-06 | Hábitos, condições e descrições | Grava respostas selecionadas (Caption) e textos separados. Valores não respondidos não equivalem necessariamente a “não”. | Anamnese.SalvaAnamnese:2; 48 campos no dicionário |

FC máxima — métodos conforme seletores do código:

| Seleção | Abrangência do código | Expressão |
|---|---|---|
| Obtn1 | Feminino/Masculino | 220 − idade |
| Obtn2 | Feminino/Masculino | 220 − 0,65×idade |
| Obtn3 | Feminino/Masculino | 208 − 0,7×idade |
| Obtn4 | Masculino | 201 − 0,6×idade |
| Obtn5 | Masculino | 205 − 0,41×idade |
| Obtn6 | Masculino | 198 − 0,41×idade |
| Obtn7 | Masculino | 205 − 0,7×idade |
| Obtn8 | Feminino | 192 − 0,7×idade |
| Obtn9 | Feminino | 206 − 0,597×idade |

## 6. Regras adicionais do AddIn e dos documentos

| ID | Regra observada | Fonte |
|---|---|---|
| PDF-01 | Evolução filtra mesmo Id e data <= avaliação selecionada; ordena por data; até seis registros mantém todos, acima disso primeira + cinco mais recentes. A ordem por hora/Seq não é explícita em empates. | LaudoRepositorio.MontarSerie:269 |
| PDF-02 | Água/proteína kg = Round(percentual/100×peso;2) quando percentual e peso >0. | LaudoRepositorio.LerLinhaCompleta:172–180 |
| PDF-03 | Massa livre kg ausente/<=0 recebe fallback Round(peso−gordura kg;2), com peso e gordura positivos. | LaudoRepositorio.LerLinha:335 |
| PDF-04 | SMI = músculo esquelético kg / (altura cm/100)²; sem entradas positivas retorna zero. | AvaliacaoLaudo.Smi:131 |
| PDF-05 | Tabela de composição deriva proporção kg/peso×100; para músculo e músculo esquelético prioriza percentual existente quando >0. | LaudoAvaliacaoHtmlBuilder.Composicao:70 |
| PDF-06 | Controle de peso usa valor armazenado não zero, senão peso alvo−peso atual. Controle de gordura = peso alvo×gordura alvo%/100−gordura atual kg. Controle muscular = controle de peso−controle de gordura. | LaudoAvaliacaoHtmlBuilder.PontuacaoControle:197 |
| PDF-07 | Obesidade apresentada como peso atual/peso alvo×100. | LaudoAvaliacaoHtmlBuilder.ObesidadeBarras:223 |
| PDF-08 | Tipo corporal cruza faixas de IMC (16, 18,5, 25) e gordura (feminino 21/33; demais 8/20), com células mescladas. Rótulos visuais do eixo mostram 18/28; DIV-08. | LaudoAvaliacaoHtmlBuilder.TipoCorpo:253 |
| PDF-09 | Vazios, N/A e Não Calculado são exibidos como traço. Helpers numéricos também ocultam valores <=0 em vários quadros. Controle de peso usa formatação com sinal. | LaudoAvaliacaoHtmlBuilder.Cls:347, Dn:341, SignKg:343 |
| PDF-10 | Percentuais segmentares são apresentados diretamente; não são recomputados como kg/peso. | LaudoAvaliacaoHtmlBuilder.SegLbl:189 |
| PDF-11 | Avaliação e evolução usam builders C#; anamnese/ficha usam modelos Excel. Variantes legadas H/M, 2, 3 e BHM precisam de inventário visual antes da equivalência final. | LaudoService; AvaliacaoA/B/C; Anamnese.RelatorioAn_PDF; Clientes.FichaCadastral_PDF |
| PDF-12 | Pontuação é informada/importada; o texto do laudo admite valor >100, embora comentários do modelo indiquem 0–100. Não introduzir limite 100 sem decisão. | AvaliacaoLaudo:80; LaudoAvaliacaoHtmlBuilder:199–201 |

## 7. OCR

| ID | Regra observada / proposta aprovada |
|---|---|
| OCR-01 | C# procura o maior bloco JPEG dentro do PDF, amplia a imagem 2× e executa Windows.Media.Ocr. É uma estratégia específica do formato atual. |
| OCR-02 | O layout atual possui 44 zonas, sendo 20 segmentares com marca estática de baixa confiança. A contagem histórica de 39 não representa o código atual. Retângulos e controles estão em evidências. |
| OCR-03 | Composição corporal extrai kg; porcentagens globais são calculadas no formulário. Segmentar extrai kg e %. Impedâncias têm dez campos (cinco segmentos × duas frequências). |
| OCR-04 | Não extrai nome, sexo, idade, altura ou data. Vinculação correta ao paciente/avaliação deve ser confirmada pelo operador. |
| OCR-05 | O mapeador ignora zeros e campos sem número reconhecido; devolve Campo=valor com vírgula decimal. Esse comportamento não prova que o campo do documento esteja ausente. |
| OCR-06 | Revisão antes de aplicar já é solicitada no VBA. Na web, revisão será etapa obrigatória; campos importados e corrigidos devem preservar origem. Apenas o modelo atual Relaxmedic faz parte da v1. |

## 8. Funções declaradas com uso não demonstrado

O módulo Equações_Avaliações contém Pollock3/7, Slaughter, Weltman, Petroski, Visser, Guedes, Chumlea88P, Chumlea85A e Osterkamp95P. A busca nos módulos extraídos não encontrou chamadas textuais dessas funções nos formulários inspecionados. As funções PesoOsseo, PesoMuscular e PesoResidual têm chamadas confirmadas no fluxo A.

As funções declaradas estão preservadas no anexo, mas não devem ampliar automaticamente o escopo dos três protocolos. Referências dinâmicas, propriedades de controles e comportamento em execução precisam ser considerados antes de classificá-las definitivamente como não utilizadas.

## 9. Regras novas já decididas para a web

Estas regras vêm do brainstorming aprovado, não da planilha: três perfis; permissões parametrizáveis do assistente; revisões com histórico; preservação de documentos emitidos; templates versionados; importação somente de cadastros; uma clínica na nuvem; até cinco usuários simultâneos.

Não foi definido ainda: conjunto de permissões delegáveis, política de exclusão/retenção, critérios mínimos de finalização por protocolo, limites de arquivo, política de duplicidade e seleção de método/fornecedor de OCR. Essas decisões não devem ser preenchidas como se fossem regras existentes no VBA.
