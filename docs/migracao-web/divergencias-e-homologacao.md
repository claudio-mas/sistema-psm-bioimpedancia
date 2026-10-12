# Divergências e critérios de homologação

Data: 2026-09-27. Resultado de leitura estática, sem execução de macros ou alteração do sistema atual.

Revisão de 2026-09-27: cada divergência foi conferida contra a extração VBA, o manifesto de controles (`manifest.json`), o XML do XLSM e o C# atual. As referências `Arquivo:linha` são da extração atual. “Confirmado estático” significa que o código determina o resultado sem depender de estado externo; ainda assim, os casos marcados devem ser executados em cópia controlada antes da homologação.

Revisão de 2026-10-04: a pessoa decidiu DIV-04, DIV-07 (provisoriamente, a validar com o médico responsável), DIV-13 (anamnese) e DIV-15 (anamnese). O Plano 2 foi liberado (§5).

Revisão de 2026-10-08: a pessoa decidiu DIV-01, 02, 03, 10, 14, 15 e 16 (as clínicas provisoriamente, pelo método das premissas) e executou os casos H no Excel. O Plano 3 foi liberado (§5).

Revisão de 2026-10-11: a pessoa conferiu no Excel as legendas das avaliações (cópia do arquivo analisado, SHA-256 `c1f34ada…`) e decidiu DIV-05, DIV-11, DIV-13 (avaliações) e a parte de formulário da DIV-14. Foi registrada a DIV-17 (pontuação da C). O Plano 4 foi liberado (§5).

## 1. Política proposta

Documentar primeiro o comportamento observado. Para cada divergência, decidir se a aplicação preservará o comportamento ou adotará uma regra corrigida, com resultado esperado aprovado. Uma correção aprovada deve ser identificada na versão das regras; PDFs e revisões anteriores devem continuar reproduzíveis.

Recomenda-se corrigir defeitos confirmados em vez de incorporá-los como regra nova. Essa recomendação não é aprovação de qualquer mudança clínica específica.

## 2. Divergências encontradas

| ID | Evidência | Consequência | Tratamento necessário |
|---|---|---|---|
| DIV-01 | Energia.bas:64 e 99: IMC interno das DRI = peso/alturaCm²; os termos de altura usam alturaCm/100. O operador escolhe o método (FrmNecessidadeEnergetica:78 adolescentes, :82 adultos); a idade não seleciona a função. | Com qualquer entrada realista, o IMC interno fica abaixo de 0,01 (90 kg/173 cm → 0,003). O legado usa **sempre** a equação de peso normal (≤21,8 / ≤24,99); os ramos de sobrepeso nunca executam. Um adulto com IMC 30 recebe a equação de peso normal. | Resultado legado determinado; não precisa ser medido no Excel. Decidir entre preservar e corrigir para IMC em kg/m². A correção ativa ramos nunca exercitados: conferir os coeficientes com a publicação de origem (as expressões coincidem com as equações de gasto energético basal do IOM/DRI 2005; o ramo masculino adolescente de sobrepeso usa 35,5×idade, e a referência aparenta usar 33,5) e o limiar 21,8 dos adolescentes, sem referência identificada. Ver H-23. **Decidido em 2026-10-08 (Plano 3):** IMC interno em kg/m², com comparação exata; limiares do legado (21,8 e 24,99). Coeficiente da idade no sobrepeso masculino adolescente: 33,5. A fonte é secundária (revisão sistemática PMC8685418, Tabela 3, com as equações do IOM); o legado usa 35,5. As premissas PC-05 e PC-06 ficam a validar com o médico. |
| DIV-02 | FrmCalcPeso:68 lê `FrmAvaliacaoB.TxtAltura`. Além disso, `OptionButton1_Click` e `OptionButton2_Click` (FrmCalcPeso:134–166), cuja escolha é obrigatória para calcular (:19), **sobrescrevem** peso atual, gordura atual, IMC atual e gordura-alvo com os valores de FrmAvaliacaoB e zeram os campos-alvo de B, qualquer que seja o protocolo. Os formulários A (FrmAvaliacaoA:373–379) e C (FrmAvaliacaoC:221–227) preenchem a calculadora corretamente; o clique na opção desfaz esse preenchimento. `FrmAvaliacaoB.UserForm_Initialize` inicia a altura com 0,00 (:780). | Em A e C, se B não estiver carregado, a referência a FrmAvaliacaoB cria uma instância vazia: peso, gordura e IMC viram 0 e o cálculo pede a gordura ou o IMC atual. Mesmo com redigitação, o método por IMC-alvo retorna peso-alvo 0,00 (altura de B = 0) e controle de peso = −peso atual. Se houver uma instância de B em memória, os dados usados são os dela. | Não há comportamento legado a preservar em A/C. Proposta: a calculadora usa somente os dados da avaliação corrente. Confirmar a predição com uma execução em cópia de teste. Ver H-07 e H-08. **Decidido em 2026-10-08 (Plano 3):** só os dados da avaliação corrente, nos três protocolos. Pela gordura, exige peso, gordura atual e alvo; pelo IMC, IMC-alvo e altura. H-08 confirmado no Excel: legado 0,00 kg; web 63,58 kg. |
| DIV-03 | FrmAvaliacaoA:296 e :607 atribuem `ImcDesc` à classificação de IMC para 5–19 anos. `ImcDesc` (Public String, IMC.bas:3) só é atribuída dentro de `Imc_CA_CAM`. Nenhum dos 89 módulos chama `Imc_CA_CAM`, e nenhuma fórmula de célula chama função VBA (só a aba Analise C tem fórmulas). | A classificação de IMC de 5 a 19 anos no fluxo A é **sempre vazia**. O valor do IMC é calculado e gravado normalmente. | Decidir se a v1 classifica 5–19 anos. A tabela de `Imc_CA_CAM` (percentis por sexo e idade inteira de 5 a 19 anos) existe, mas nunca foi usada em produção; ativá-la exige validação clínica. Alternativa: exibir “não classificado” explicitamente. **Decidido em 2026-10-08 (Plano 3):** "Não classificado (5 a 19 anos)". Imc_CA_CAM fica para uma versão futura (PC-07). |
| DIV-04 | FrmUrina:19 testa `OptionButton3.Value Or OptionButton4.Value = True`. Pela precedência do VBA, isso equivale a `Op3 Or (Op4 = True)`, e o ramo da opção 4 (:22) fica inalcançável. `UserForm_Activate` (:68–71) testa a escala 7 duas vezes e nunca a 8. | Selecionar 4 grava escala 3; o texto continua “Levemente Desidratado”. Se a escala 8 estiver carregada ao abrir o formulário (FrmAnamnese:194), nenhuma opção fica marcada. | Proposta: correspondência direta entre opção e escala, inclusive na reabertura. Confirmar o resultado esperado. Ver H-12. **Decidido em 2026-10-04:** correspondência direta opção = escala, inclusive na reabertura; legendas 1–2 Bem Hidratado, 3–4 Levemente, 5–6 Moderadamente e 7–8 Severamente Desidratado. |
| DIV-05 | `FrmBioimpC.CommandButton179_Click` (:90–189) transfere as classificações pela Caption, sem `Else`. Quatro controles não têm ramo; o grupo de cada um foi identificado pelo frame no manifesto: `Opt7Alto` → músculo esquelético (AQ); `Opt5Excel` → água (AW); `Opt6Alto` → massa muscular (AL); `Opt4Alto` → proteína (AY). `Opt3Alto` (óssea, BB) tem ramo (:141). A recarga (:738–895) também não reseleciona essas quatro opções. | Selecionar uma dessas opções mantém a classificação que já estava em FrmAvaliacaoC: vazia em avaliação nova, a anterior em edição. O laudo imprime esse texto em 4 dos 7 grupos do quadro de composição. **Defeito ativo no sistema atual**, não só na migração. | Na web, enumerar as opções de cada grupo e mapear todas explicitamente. Os demais grupos têm 2 ou 3 opções no código; conferir no Excel se faltam controles. As legendas extraídas são truncadas; confirmar os textos no Excel. Corrigir a planilha atual é uma decisão separada da migração. **Decidido em 2026-10-11 (Plano 4):** a web mapeia explicitamente as 45 opções dos 13 grupos da C. Isso inclui `Opt4Alto`, `Opt5Excel`, `Opt6Alto` e `Opt7Alto`, além da `Opt3Alto`. A web grava o texto da opção escolhida e reabre o formulário com ela marcada (H-16). Legendas conferidas no Excel, por controle: <ul><li>IMC: 13 Baixo peso, 14 Saudável, 15 Alto ou Muito Alto, 16 Médio;</li><li>gordura: 1 Baixo, 2 Saudável, 3 Excelente, 4 Alto;</li><li>músculo esquelético: 5 Baixo, 6 Saudável, 8 Excelente, Opt7Alto Alto;</li><li>gordura visceral: 9 Baixo Risco, 10 Médio Risco, 12 Alto Risco;</li><li>peso total: 29 Baixo, 30 Saudável, 31 Excelente, 32 Alto;</li><li>massa óssea: 36 Excelente, 37 Baixo, 38 Saudável, Opt3Alto Alto;</li><li>taxa muscular: 17 Baixo, 18 Médio, 20 Excelente;</li><li>gordura subcutânea: 33 Sobrepeso, 34 Baixo, 35 Médio;</li><li>água: 22 Baixo, 23 Saudável, 24 Alto, Opt5Excel Excelente;</li><li>massa muscular: 25 Baixo, 26 Saudável, 28 Excelente, Opt6Alto Alto;</li><li>proteína: 39 Excelente, 40 Baixo, 41 Saudável, Opt4Alto Alto;</li><li>TMB: 42 Baixo, 43 Excelente;</li><li>idade corporal: 44 Excelente, 45 Sobrepeso.</li></ul>A B usa as opções do FrmBioimpR (DIV-13), e não texto livre. A planilha atual não é corrigida. |
| DIV-06 | Leitura antiga do XML documentava Analise C!G7 como `=`. A fórmula é compartilhada com G6 e resolve para `=1-F7`. | Um catálogo baseado no PRD antigo levaria uma regra incorreta para a migração. | Resolvido no novo catálogo por interpretação da fórmula compartilhada. Nenhuma alteração na planilha. |
| DIV-07 | FrmAnamnese.CommandButton184_Click (:118–148): cadeia de `ElseIf` com `AND` em cada faixa. A guarda `If sistolica And diastolica <> 0` equivale a `sistolica And (diastolica <> 0)` e, na prática, exige as duas pressões diferentes de zero. | Resultados legados determinados: 150/80 → Hipertensão Estágio 1 (o ramo Sistólica Isolada é inalcançável para sistólica de 140 a 179); 190/105 e 170/115 → Não Classificado (o Estágio 3 exige ≥180 **e** ≥110); 85/70 → Ótima (Baixa exige <90 e <60). | As faixas coincidem com a classificação das VI Diretrizes Brasileiras de Hipertensão (2010); conferir. A clínica deve escolher a tabela de referência (existe diretriz mais recente) e a regra para pressões em categorias diferentes. Não derivar uma tabela nova automaticamente. Ver H-22. **Decidido provisoriamente em 2026-10-04, a validar com o médico responsável.** Tabela: Diretriz Brasileira de Hipertensão Arterial – 2025 (SBC, SBH, SBN), Arq Bras Cardiol. 2025;122(9):e20250624, Quadro 3.2, p. 36 (consultório, a partir de 18 anos). Regras: <ul><li>categorias diferentes: vale a mais alta;</li><li>hipertensão primeiro (PAS ≥ 140 ou PAD ≥ 90);</li><li>depois, Pressão Baixa / Hipotensão (PAS < 90 ou PAD < 60), premissa fora da diretriz;</li><li>por último, PA normal ou Pré-hipertensão;</li><li>HA sistólica isolada (PAS ≥ 140 e PAD < 90) e diastólica isolada (PAS < 140 e PAD ≥ 90), com estágio pela pressão alterada;</li><li>sem PAS ou PAD: dados incompletos;</li><li>menores de 18 anos: sem classificação.</li></ul>Os nomes do legado deixam de existir. Premissas PC-01 e PC-02 em `psm-web/docs/premissas-clinicas.md`. |
| DIV-08 | TipoCorpo usa limites de gordura 21/33 ou 8/20, mas imprime 18/28 no eixo. O comentário em LaudoAvaliacaoHtmlBuilder.cs:249–252 indica eixo fixo intencional, igual para os dois sexos. | Marcação visual e limiares efetivos podem comunicar coisas diferentes. | Separar apresentação dos limites da regra; decidir se o eixo varia por sexo e validar em documento de referência. |
| DIV-09 | Avaliações!DD1 sem cabeçalho; VBA e C# tratam DD como hora. | Mapeamento somente por cabeçalho perde a hora. | Resolvido no dicionário pela dupla evidência. Validar formato/hora local na especificação web. |
| DIV-10 | Formulário recebe kg de água/proteína, grava percentual com duas casas, C# reconstrói kg e arredonda. | Ida e volta kg→%→kg pode alterar o valor original. | Proposta: preservar kg original e percentual derivado com regra explícita; validar diferença em relação ao PDF atual antes de aprovar. **Decidido em 2026-10-08 (Plano 3):** o kg informado é a entrada; o percentual é derivado (kg ÷ peso × 100), com 2 casas, meio para cima. H-09 (arquivo analisado): água 41,3 kg com 70 kg dá 59,00%; proteína 11,7 kg dá 16,71% (a ida e volta daria 11,697 kg). |
| DIV-11 | SalvaAvaliacaoC não grava MLG%/residual/intervalos em colunas que os leitores podem consultar; protocolos gravam subconjuntos distintos da mesma linha. | Ausência ou reaproveitamento de valores de outro fluxo pode afetar saída. | Modelar campos aplicáveis por protocolo e proibir herança silenciosa ao trocar protocolo; verificar se troca existe na operação atual. **Decidido em 2026-10-08 (cálculo) e 2026-10-11 (gravação):** <ul><li>campo de outro protocolo é recusado no campo;</li><li>o protocolo é escolhido ao criar e não muda; para outro protocolo, cria-se outra avaliação (P4-3);</li><li>cada protocolo grava só os campos dele e todas as colunas da aba Avaliações que preenche (P4-9, P4-11, P4-12); o que não se aplica nem aparece no registro.</li></ul> |
| DIV-12 | LaudoRepositorio.MontarSerie (:269–290) inclui avaliações do mesmo Id com data menor ou igual à selecionada e ordena só por data. O `OrderBy` do LINQ é estável: em empate, prevalece a ordem física das linhas da aba. | Avaliações do mesmo dia gravadas depois da selecionada entram na série e aparecem à direita dela. | Definir desempate por data, hora e identificador; excluir as posteriores à selecionada no mesmo dia (pela hora, quando houver). |
| DIV-13 | Extração de controles retornou captions truncadas/deslocadas, inclusive caracteres de controle. | Rótulos poderiam ser documentados incorretamente. | Limitação do extrator: usar nomes apenas como indício; conferir legendas/propriedades no Excel antes de fechar opções e obrigatoriedades. **Anamnese conferida no Excel pela pessoa em 2026-10-04:** <ul><li>22 perguntas Sim/Não, sem resposta pré-marcada (null = não respondida), e 11 textos; legendas e grupos no Plano 2;</li><li>obrigatórios: paciente e data;</li><li>no legado, o controle 70 do dispositivo de saúde é Sim e o 69 é Não, invertido em relação às demais perguntas.</li></ul>**Avaliações conferidas no Excel pela pessoa em 2026-10-11:** <ul><li>legendas das 13 classificações da C por controle (ver DIV-05);</li><li>B: no FrmAvaliacaoB as classificações são digitadas; o FrmBioimpR tem opções, e o botão Aplicar copia o texto delas. As legendas são estas: IMC '- (Abaixo do peso)', '0 (Normal)', '+ (Sobrepeso)' e '++ (Obeso)', com as faixas da OMS; gordura e músculo esquelético 'Baixo (-)', 'Normal (0)', 'Alto (+)' e 'Muito Alto (++)'; gordura visceral '0 (Normal)', '(+) Alto' e '(++) Muito Alto'. A web usa essas opções, e não texto livre (P4-10);</li><li>textos '18,5 - 24,9' e '<= 9' da B conferidos;</li><li>diâmetros da A em mm conferidos;</li><li>métodos de TMB com as legendas do FrmNecessidadeEnergetica, que a web adota na `avaliacao-v2`: 'Harris-Benedict (1919) - 15 aos 74 anos', 'OMS (Organização Mundial da Saúde) 1985', 'Comitê DRI - Crianças e Adolescentes com Peso Normal ou Sobrepeso/Obesidade.', 'Comitê DRI - Adultos com Peso Normal ou Sobrepeso/Obesidade.' e 'Calculado pela Bioimpedância'.</li></ul>Obrigatórios para finalizar a avaliação (P4-2, provisório, PC-14): <ul><li>todos os protocolos: protocolo, data, peso e altura, sem erro de campo;</li><li>A e B: gordura (%);</li><li>C: gordura (kg);</li><li>B: IMC do aparelho.</li></ul> |
| DIV-14 | Comentários de pontuação dizem 0–100, enquanto texto emitido admite >100. | Restrição arbitrária na web pode rejeitar valores aceitos hoje. | Definir domínio com a clínica e amostras do aparelho; não impor teto com base no comentário. **Decidido em 2026-10-08 (Plano 3):** sem teto clínico. Limite técnico: inteiro de 0 a 999, até a definição com amostras do aparelho (PC-13). **Formulário (Plano 4, 2026-10-11):** a C grava a pontuação com esse limite; no Excel, 120 foi aceito sem erro. Ver DIV-17. |
| DIV-15 | `Age` (Idade.bas:2) só é chamada nos formulários de cadastro (FrmCadastro_C:552/555, FrmCadastroPL_C:1016/1019) e grava a idade em Clientes!E. Avaliações e anamneses novas copiam essa coluna (FrmCliente:281/331/379/782, lista carregada com os valores brutos da aba por FiltrosLTB.FiltroClientes). Nada recalcula Clientes!E ao abrir a pasta. `Age` usa `Now`; em ano não bissexto, nascidos em 29/02 fazem aniversário em 01/03. | A idade da avaliação é a do dia em que o cadastro foi salvo pela última vez, não a da data da avaliação. Um paciente cadastrado há dois anos, sem edição do cadastro, é avaliado com a idade de dois anos atrás. Faixa etária e todas as regras por idade (IMC, gordura, RCQ, energia, FC máxima) herdam o erro. **Defeito ativo no sistema atual.** | Proposta: calcular a idade na data da avaliação a partir do nascimento; preservar a idade gravada nos registros históricos. Definir o aniversário de 29/02. Ver H-10. **Decidido para a anamnese em 2026-10-04:** idade na data do atendimento, a partir do nascimento; quem nasceu em 29/02 faz aniversário em 01/03 nos anos não bissextos, como `Age`. O Plano 3 confirma a mesma regra para as avaliações. **Decidido também para as avaliações em 2026-10-08:** idade na data da avaliação, pelo mesmo módulo da anamnese. |
| DIV-16 | `IMCidoso` (IMC.bas:4) rotula IMC >27 como “Obesidade”. O IMC-alvo padrão é fixo em 22,5 (FrmAvaliacaoA:377, FrmAvaliacaoB:226, FrmAvaliacaoC:225, FrmCalcPeso:148/166). A gordura-alvo sugerida vem de `TabelaGorduraAlvo`, chamada ao abrir a calculadora (ex.: FrmAvaliacaoC:224) e nos cliques de opção (FrmCalcPeso:147/165). | Textos e padrões clínicos sem referência registrada. A classificação de Lipschitz, comum para idosos, usa “sobrepeso” acima de 27; conferir a fonte adotada. | Preservar os textos até aceite. Registrar a referência bibliográfica de cada regra na versão das regras. Confirmar se o IMC-alvo 22,5 e a gordura-alvo sugerida continuam na web como sugestões editáveis. **Decidido em 2026-10-08 (Plano 3):** textos e limites do legado preservados como premissas (PC-08 a PC-12), inclusive IMC idoso > 27 = "Obesidade". IMC-alvo 22,5 e gordura-alvo (com as lacunas do legado) são sugestões editáveis. |
| DIV-17 | `FrmBioimpC.CommandButton179_Click`:36 copia a pontuação para `FrmAvaliacaoC.TxtPontuacao`, sob `On Error Resume Next`. `AvaliacaoC.bas`:410 grava esse controle em Avaliações!CI. No arquivo analisado, o FrmAvaliacaoC **não tem** o controle `TxtPontuacao`: a pessoa conferiu a lista de controles da janela Propriedades em 2026-10-11. | A pontuação digitada no FrmBioimpC provavelmente nunca chega a Avaliações!CI. O laudo (PDF-12, `LaudoAvaliacaoHtmlBuilder`:199–201) lê a CI. **Provável defeito ativo no sistema atual** (conferido estaticamente e na lista de controles; não executado). | **Decidido em 2026-10-11 (Plano 4, P4-9):** a web grava a pontuação da C (inteiro de 0 a 999, DIV-14). Considerar no Plano 5 a origem e a exibição da pontuação no laudo. Corrigir a planilha atual é decisão separada da migração. |

## 3. Situação e recomendação

| ID | Status | Recomendação | Bloqueia |
|---|---|---|---|
| DIV-01 | Decidido provisoriamente (2026-10-08) | IMC em kg/m²; 33,5 da fonte secundária; validar com o médico (PC-05, PC-06) | — |
| DIV-02 | Decidido (2026-10-08); H-08 confirmado no Excel | Dados da avaliação corrente | — |
| DIV-03 | Decidido provisoriamente (2026-10-08) | “Não classificado (5 a 19 anos)” (PC-07) | — |
| DIV-04 | Decidido (2026-10-04) | Correspondência direta opção = escala | — |
| DIV-05 | Decidido (2026-10-11); legendas conferidas no Excel | Mapear as 45 opções da C; gravar o texto e reabrir marcada (H-16) | — |
| DIV-06 | Resolvido | — | — |
| DIV-07 | Decidido provisoriamente (2026-10-04) | Diretriz 2025, Quadro 3.2; validar com o médico responsável (PC-01, PC-02) | — |
| DIV-08 | Confirmado estático | Separar apresentação e regra; decidir eixo por sexo | Plano 5 |
| DIV-09 | Resolvido | — | — |
| DIV-10 | Decidido (2026-10-08) | kg original; percentual derivado com 2 casas, meio para cima | — |
| DIV-11 | Decidido (2026-10-08 e 2026-10-11) | Campos por protocolo; protocolo imutável depois de criado | — |
| DIV-12 | Confirmado estático | Corrigir o desempate | Plano 5 |
| DIV-13 | Decidido (anamnese em 2026-10-04; avaliações em 2026-10-11) | Legendas do legado conferidas; opções do FrmBioimpR na B | — |
| DIV-14 | Decidido provisoriamente (2026-10-08; formulário em 2026-10-11) | Sem teto clínico; limite técnico 0–999 (PC-13) | — |
| DIV-15 | Decidido (anamnese em 2026-10-04; avaliações em 2026-10-08) | Idade na data do atendimento; 29/02 → 01/03 | — |
| DIV-16 | Decidido provisoriamente (2026-10-08) | Textos do legado como premissas (PC-08 a PC-12); alvos como sugestões editáveis | — |
| DIV-17 | Decidido (2026-10-11); defeito provável do legado | A web grava a pontuação da C | Plano 5 (laudo) |

## 4. Casos de referência propostos

Os casos abaixo são especificações de verificação. Não foram executados no VBA nem na aplicação web. Valores aritméticos simples foram conferidos de forma independente; limites de classificação e efeitos de eventos precisam de execução no Excel em cópia controlada, com dados sintéticos.

| Caso | Entrada | Resultado/critério esperado | Regras |
|---|---|---|---|
| H-01 | Peso 80 kg, gordura 20% | Gordura 16 kg; massa livre 64 kg e 80%, observando a sequência de arredondamentos. **Observado no Excel em 2026-10-08:** 16,00 kg; massa livre 64,00 kg e 80,00%. | AV-03 |
| H-02 | Peso 80 kg, altura 200 cm, adulto | IMC 20,00 no fluxo A. B/C devem preservar o valor informado/importado conforme protocolo. **Observado no Excel em 2026-10-08:** IMC 20,00 na A. | AV-01/02 |
| H-03 | Cintura 80 cm, quadril 100 cm | RCQ 0,80. Testar quadril zero e ausente separadamente. **Observado no Excel em 2026-10-08:** 0,80, "Risco Alto" (Feminino, 40 anos). Quadril 0 ou vazio: o legado pede o quadril e mantém o RCQ anterior; a web deixa RCQ e classificação vazios. Bloco L: o legado arredonda o RCQ a 2 casas, meio para cima, e classifica o valor arredondado (Feminino 25, quadril 100: cintura 82,4 → 0,82 "Risco Alto"; 82,5 → 0,83 "Muito Alto"). | AV-05 |
| H-04 | C: peso 80 kg; componente 16 kg | Percentual global 20,00%. Não aplicar essa fórmula ao percentual segmentar relativo ao padrão. **Observado no Excel em 2026-10-08:** 20,00%; peso total 100,00%. | AV-06/PDF-10 |
| H-05 | Feminino, 60 kg, 165 cm, 40 anos, HB | Expressão 1347,09; função retorna 1347; apresentação pode mostrar 1347,00. **Observado no Excel em 2026-10-08:** TMB 1347. | EN-01 |
| H-06 | Feminino, 60 kg, 40 anos, OMS, fator 1,55 | TMB 1351,00; GET 2094,05. **Observado no Excel em 2026-10-08:** TMB 1351,00; GET 2094,05. | EN-02/04 |
| H-07 | Fluxo B: peso 80 kg, gordura 20%, alvo 15% | Peso alvo apresentado 75,29 kg; controle −4,71 kg a partir do valor formatado. Em A/C, o legado depende de redigitação (DIV-02); a proposta usa os dados da avaliação corrente. **Observado no Excel em 2026-10-08:** 75,29 kg; controle −4,71 kg. | AV-12/DIV-02 |
| H-08 | IMC alvo 22, altura atual 170 cm; fluxo A ou C sem FrmAvaliacaoB carregado | Legado previsto: peso alvo 0,00 kg (altura de B = 0). Proposta corrigida: 63,58 kg para A/B/C. Confirmar a predição antes da decisão DIV-02. **Observado no Excel em 2026-10-08:** confirmado: o legado dá 0,00 kg em A sem a B carregada; a web dá 63,58 kg (DIV-02). | AV-13/DIV-02 |
| H-09 | Composição kg com dízimas percentuais | Registrar kg original, percentual arredondado e kg reconstruído; documentar diferença. **Observado (arquivo analisado):** água 41,3 kg com 70 kg → 59,00%; proteína 11,7 kg → 16,71% (16,71% de 70 = 11,697 kg; perde 0,003 kg na volta). A web guarda o kg (DIV-10). | DIV-10 |
| H-10 | Idade imediatamente antes/depois de 5, 20 e 60; aniversário próximo; nascido em 29/02; cadastro salvo há mais de um ano | Classificador correto e data-base explícita. Legado: usa a idade gravada em Clientes!E no último salvamento do cadastro. **Observado no Excel em 2026-10-08:** IMC por idade (4 → 0 e N/A; 5 e 19 → vazio; 20 e 59 → TabelaIMC; 60 → IMCidoso), RCQ por idade (N/A abaixo de 20 e acima de 69), TMB OMS por faixa (17/18, 29/30, 59/60) e nascido em 29/02/2000 → 26 anos: tudo OK. A web troca o vazio de 5 a 19 anos por “Não classificado (5 a 19 anos)” (DIV-03). | CAD-02/AV-02/DIV-15 |
| H-11 | Cada limite das tabelas e valores imediatamente abaixo/acima | Igualdade inclusiva/exclusiva deve coincidir com a versão aprovada da regra, sem perda por arredondamento. **Observado no Excel em 2026-10-08:** limites de gordura (Feminino 40 e Masculino 17, inclusive a grafia “ABaixo da Média”), RCQ (inclusive Masculino 50–59 com “< 0,90”), TabelaIMC e IMCidoso: tudo OK. | CL-01 a CL-07 |
| H-12 | Escala de urina: opção 4; reabertura com escala 8 | Legado: grava 3 (Levemente Desidratado); ao reabrir com 8, nenhuma opção marcada. Web (DIV-04 decidida): a opção 4 grava a escala 4 (Levemente Desidratado); ao reabrir com 8, a opção 8 vem marcada. | AN-04 |
| H-13 | FC máxima 180, repouso 70 | Reserva 110. Casos de FC máxima usam o método selecionado e arredondamento próprio. | AN-01/02 |
| H-14 | Peso 60 kg; 35 mL/kg | Água ideal 2,10 L. A web recebe o fator em mL/kg e mostra litros (peso × fator ÷ 1000), decisão de 2026-10-04. O legado usa 0,035, rotulado "Ml x Kg.", e também mostra 2,10; o valor "2100,00 mL" registrado antes desta revisão estava errado. Peso ou fator zero: a web não calcula. | AN-03 |
| H-15 | Oito avaliações em datas distintas | Seleção legado [1,4,5,6,7,8]. Para datas iguais, resultado só após definir DIV-12. | PDF-01 |
| H-16 | Cada opção manual dos grupos C, incluindo `Opt4Alto`, `Opt5Excel`, `Opt6Alto` e `Opt7Alto` | Texto correto transferido, salvo, recarregado e emitido; nenhum valor anterior permanece por falta de ramo. Legado: essas quatro opções mantêm a classificação anterior. | CL-08/DIV-05 |
| H-17 | PDF Relaxmedic com campo não reconhecido, zero e valor segmentar | Revisão obrigatória, distinção de estados, nenhuma confirmação automática; conferir as 44 zonas. | OCR-01 a OCR-06 |
| H-18 | CPF/CEP com zero inicial e cadastro importado novamente | Proposta: preservar texto e impedir duplicação da mesma origem/ID; divergências vão para revisão. | CAD-04/05 |
| H-19 | Dois usuários editando a mesma revisão | Detectar conflito ao salvar; não perder alterações silenciosamente. | Proposta de concorrência |
| H-20 | Correção após emissão | Nova revisão e novo PDF; documento anterior acessível com dados/regras/template de origem. | Decisão de revisões |
| H-21 | Quatro documentos, nomes longos e campos vazios | Conteúdo completo, paginação legível, tabelas sem cortes, fontes e cores aprovadas. | PDF-09/11 |
| H-22 | PA 150/80, 190/105, 170/115 e 85/70; cada limite da tabela | Legado: Estágio 1; Não Classificado; Não Classificado; Ótima. Web (DIV-07, provisória): HA Sistólica Isolada – Estágio 1; HA Estágio 3; HA Estágio 3; Pressão Baixa / Hipotensão. Os limites estão nos testes do Plano 2. | AN-05/DIV-07 |
| H-23 | DRI adultos, masculino, 40 anos, 90 kg, 173 cm (IMC 30,07) | Legado: 1875 (ramo de peso normal; expressão 1875,465). Corrigido: 1841 (ramo de sobrepeso; expressão 1841,372). A web adota o corrigido (DIV-01): 1841. | EN-03/DIV-01 |

## 5. Portões por plano de implementação

Cada plano precisa produzir software testável com valores esperados exatos. Por isso a implementação é dividida em planos por subsistema, e cada plano só é escrito quando suas dependências estão aceitas.

| Plano | Escopo | Pode ser escrito agora? | Depende de |
|---|---|---|---|
| 1. Fundação | Estrutura do projeto, NestJS + Prisma + PostgreSQL, React + Vite, login, perfis e permissões, auditoria, pacientes, importação de cadastros | Sim | Aceite dos padrões propostos abaixo |
| 2. Anamnese | Formulário, FC máxima e de reserva, água ideal, escala de urina, pressão arterial | Sim (liberado em 2026-10-04) | DIV-04, DIV-07 (provisória) e DIV-13 (anamnese), decididas em 2026-10-04 |
| 3. Motor de regras A, B/R e C | Cálculos, classificações, energia, metas, versão das regras | Sim (liberado em 2026-10-08) | DIV-01, 02, 03, 10, 14, 15 e 16 decididas e casos H executados no Excel em 2026-10-08 |
| 4. Avaliações e revisões | Telas por protocolo, rascunho e finalização, revisões, concorrência | Sim (liberado em 2026-10-11) | Critérios de finalização (provisórios, PC-14), DIV-05, DIV-11, DIV-13 (avaliações) e exclusão do rascunho, decididos em 2026-10-11; retenção no Plano 7 |
| 5. Documentos PDF | Os quatro documentos com Chromium, templates versionados | Não | Inventário das variantes H/M, 2, 3 e BHM; modelos de anamnese e ficha; DIV-08; DIV-12 |
| 6. OCR | Modelo atual Relaxmedic | Não | Escolha do motor após teste com PDFs representativos; limites de arquivo e retenção |
| 7. Implantação | Hospedagem, backup, transição | Não | Provedor, região, orçamento, política de internet, metas de backup e restauração |

Padrões do Plano 1, aceitos pelo usuário em 2026-09-27 (código em repositório separado, `C:\Sistema\PSM\psm-web`; plano em [2026-09-27-plano-1-fundacao.md](../superpowers/plans/2026-09-27-plano-1-fundacao.md)):

- Sessão mantida no servidor, com cookie httpOnly.
- Duplicidade na importação detectada pelo identificador legado, com alerta quando CPF ou nome + nascimento coincidirem.
- Fotos e anexos cadastrais fora da carga inicial; o anexo de anamnese não entra, conforme a carga aprovada.
- Permissões do assistente conforme a consolidação (seção 5), com finalização e emissão reservadas ao profissional e ao administrador; a delegação fica como opção configurável desligada.

Decisões do Plano 2, tomadas pela pessoa em 2026-10-04 (plano em [2026-10-04-plano-2-anamnese.md](../superpowers/plans/2026-10-04-plano-2-anamnese.md)):

- Anamnese com status rascunho ou finalizada.
  - `anamneses.draft` ou `anamneses.finalize` cria e edita rascunhos; só `anamneses.finalize` finaliza.
  - A finalizada fica somente leitura. Correções ficam para as revisões com histórico, num plano posterior.
- Várias anamneses por paciente, da mais recente para a mais antiga. A leitura exige `patients.read` e mais `anamneses.draft` ou `anamneses.finalize`.
- FC máxima:
  - só pelo método, sem digitação, com os 9 métodos limitados por sexo como no legado;
  - arredondamento para cima, premissa PC-04;
  - referências das fórmulas a validar, premissa PC-03.
- Água ideal: peso e fator precisam ser maiores que zero, com fator padrão de 35 mL/kg.
- Limites técnicos de digitação:
  - PAS e PAD: inteiros de 1 a 300 mmHg;
  - FC em repouso: de 1 a 250 bpm;
  - peso: até 500 kg; fator: até 200 mL/kg, os dois com 2 casas;
  - data: de 1900 até hoje, sem ser anterior ao nascimento.
- Regras clínicas em módulos puros, com configuração versionada (`anamnese-v1`) gravada em cada anamnese. As premissas provisórias ficam marcadas no código e no backlog `psm-web/docs/premissas-clinicas.md`.

Decisões do Plano 3, tomadas pela pessoa em 2026-10-08 (plano em [2026-10-08-plano-3-motor-de-regras.md](../superpowers/plans/2026-10-08-plano-3-motor-de-regras.md)):

- **Escopo.** O Plano 3 entrega as regras puras, a configuração versionada `avaliacao-v1` e a prévia de cálculo pela API, sem gravação. O Plano 4 cuida da gravação com a versão das regras, das telas e das classificações manuais da B e da C.
- **Arredondamento.** Meio para cima em todas as regras, sobre o valor decimal (inclusive onde o legado usava `Round()`, que leva o empate para o par). O IMC e o RCQ são classificados depois de arredondados.
- **RCQ sem quadril.** Fica vazio.
- **Peso total.** É o peso da avaliação, com 100%, nos três protocolos.
- **Peso-alvo pela gordura.** Não exige altura nem IMC.
- **Prévia.** Recalcula a cada mudança.
- **IMC na B e na C.** É o informado, mostrado ao lado do calculado, sem alerta.
- **Peso.** Peso ≤ 0 é recusado; sem peso, o que depende dele fica vazio.
- **Zeros do legado.** Viram vazio, mantendo "N/A" onde o legado escreve.
- **Limites técnicos.** Cada um aceita até 2 casas:
  - peso: até 500 kg;
  - altura: de 30 a 250 cm;
  - circunferências: até 300 cm;
  - diâmetros: até 200 mm;
  - percentuais: abaixo de 100;
  - kg de componente: até 500;
  - IMC: até 100;
  - fator de atividade: até 10;
  - TMB: até 10000;
  - pontuação: inteiro de 0 a 999.
- **Campo de outro protocolo.** É recusado (DIV-11).
- **Ressalva sobre os casos H.** Exceto o H-09, os casos foram executados numa cópia próxima, mas não idêntica, ao arquivo analisado (SHA-256 diferente de `c1f34ada…`). A pessoa decidiu não refazer, e todos bateram com o código transcrito.

Decisões do Plano 4, tomadas pela pessoa em 2026-10-11 (plano em [2026-10-11-plano-4-avaliacoes-e-revisoes.md](../superpowers/plans/2026-10-11-plano-4-avaliacoes-e-revisoes.md)):

- **Escopo.** Rascunho e finalização das avaliações A, B/R e C, com a versão das regras; telas por protocolo com a prévia; classificações manuais da B e da C; concorrência; revisões das finalizadas. O vínculo das revisões com os PDFs fica no Plano 5, e as revisões da anamnese ficam para depois.
- **Finalização (provisória, PC-14).** Todos os protocolos exigem protocolo, data, peso e altura, sem erro de campo, inclusive o da TMB. A e B exigem gordura (%); C, gordura (kg); B, IMC do aparelho. O rascunho aceita qualquer subconjunto válido.
- **Protocolo.** É escolhido ao criar e não muda (DIV-11).
- **Gravação.** Cada salvamento grava as entradas do protocolo, a versão das regras (`avaliacao-v2`, com os cálculos da v1) e o resultado do cálculo; na finalizada, o resultado gravado é o oficial. Cada protocolo grava todas as colunas da aba Avaliações que preenche. Os campos do aparelho são digitados até o OCR, inclusive a pontuação (DIV-17).
- **Revisões (H-20).** A finalizada fica somente leitura. "Nova revisão" exige `assessments.finalize` e justificativa e registra autor, data e campos alterados. Ao finalizar, a revisão passa a vigente, e as anteriores ficam consultáveis com `history.read`. Editar, finalizar e descartar uma revisão em rascunho também exigem `assessments.finalize`.
- **Exclusão e retenção.** O rascunho pode ser excluído por quem tem `assessments.draft` ou `assessments.finalize`, com auditoria. A finalizada e as revisões finalizadas nunca são excluídas na v1. A retenção fica no Plano 7.
- **Concorrência (H-19).** Versão do registro; uma versão desatualizada recebe 409 com o estado atual.
- **Limites técnicos novos:**
  - idade corporal: maior que 0 e até 150 anos;
  - % segmentar do padrão: maior que 0 e até 1000;
  - impedância: inteiro, maior que 0 e até 2000 Ω;
  - hora: de 00:00 a 23:59;
  - justificativa: até 2000 caracteres.

Nenhum desses itens invalida as decisões de produto já aprovadas. Eles delimitam o trabalho necessário antes de cada plano.
