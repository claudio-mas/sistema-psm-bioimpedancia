# Divergências e critérios de homologação

Data: 2026-09-27. Resultado de leitura estática, sem execução de macros ou alteração do sistema atual.

Revisão de 2026-09-27: cada divergência foi conferida contra a extração VBA, o manifesto de controles (`manifest.json`), o XML do XLSM e o C# atual. As referências `Arquivo:linha` são da extração atual. “Confirmado estático” significa que o código determina o resultado sem depender de estado externo; ainda assim, os casos marcados devem ser executados em cópia controlada antes da homologação.

## 1. Política proposta

Documentar primeiro o comportamento observado. Para cada divergência, decidir se a aplicação preservará o comportamento ou adotará uma regra corrigida, com resultado esperado aprovado. Uma correção aprovada deve ser identificada na versão das regras; PDFs e revisões anteriores devem continuar reproduzíveis.

Recomenda-se corrigir defeitos confirmados em vez de incorporá-los como regra nova. Essa recomendação não é aprovação de qualquer mudança clínica específica.

## 2. Divergências encontradas

| ID | Evidência | Consequência | Tratamento necessário |
|---|---|---|---|
| DIV-01 | Energia.bas:64 e 99: IMC interno das DRI = peso/alturaCm²; os termos de altura usam alturaCm/100. O operador escolhe o método (FrmNecessidadeEnergetica:78 adolescentes, :82 adultos); a idade não seleciona a função. | Com qualquer entrada realista, o IMC interno fica abaixo de 0,01 (90 kg/173 cm → 0,003). O legado usa **sempre** a equação de peso normal (≤21,8 / ≤24,99); os ramos de sobrepeso nunca executam. Um adulto com IMC 30 recebe a equação de peso normal. | Resultado legado determinado; não precisa ser medido no Excel. Decidir entre preservar e corrigir para IMC em kg/m². A correção ativa ramos nunca exercitados: conferir os coeficientes com a publicação de origem (as expressões coincidem com as equações de gasto energético basal do IOM/DRI 2005; o ramo masculino adolescente de sobrepeso usa 35,5×idade, e a referência aparenta usar 33,5) e o limiar 21,8 dos adolescentes, sem referência identificada. Ver H-23. |
| DIV-02 | FrmCalcPeso:68 lê `FrmAvaliacaoB.TxtAltura`. Além disso, `OptionButton1_Click` e `OptionButton2_Click` (FrmCalcPeso:134–166), cuja escolha é obrigatória para calcular (:19), **sobrescrevem** peso atual, gordura atual, IMC atual e gordura-alvo com os valores de FrmAvaliacaoB e zeram os campos-alvo de B, qualquer que seja o protocolo. Os formulários A (FrmAvaliacaoA:373–379) e C (FrmAvaliacaoC:221–227) preenchem a calculadora corretamente; o clique na opção desfaz esse preenchimento. `FrmAvaliacaoB.UserForm_Initialize` inicia a altura com 0,00 (:780). | Em A e C, se B não estiver carregado, a referência a FrmAvaliacaoB cria uma instância vazia: peso, gordura e IMC viram 0 e o cálculo pede a gordura ou o IMC atual. Mesmo com redigitação, o método por IMC-alvo retorna peso-alvo 0,00 (altura de B = 0) e controle de peso = −peso atual. Se houver uma instância de B em memória, os dados usados são os dela. | Não há comportamento legado a preservar em A/C. Proposta: a calculadora usa somente os dados da avaliação corrente. Confirmar a predição com uma execução em cópia de teste. Ver H-07 e H-08. |
| DIV-03 | FrmAvaliacaoA:296 e :607 atribuem `ImcDesc` à classificação de IMC para 5–19 anos. `ImcDesc` (Public String, IMC.bas:3) só é atribuída dentro de `Imc_CA_CAM`. Nenhum dos 89 módulos chama `Imc_CA_CAM`, e nenhuma fórmula de célula chama função VBA (só a aba Analise C tem fórmulas). | A classificação de IMC de 5 a 19 anos no fluxo A é **sempre vazia**. O valor do IMC é calculado e gravado normalmente. | Decidir se a v1 classifica 5–19 anos. A tabela de `Imc_CA_CAM` (percentis por sexo e idade inteira de 5 a 19 anos) existe, mas nunca foi usada em produção; ativá-la exige validação clínica. Alternativa: exibir “não classificado” explicitamente. |
| DIV-04 | FrmUrina:19 testa `OptionButton3.Value Or OptionButton4.Value = True`. Pela precedência do VBA, isso equivale a `Op3 Or (Op4 = True)`, e o ramo da opção 4 (:22) fica inalcançável. `UserForm_Activate` (:68–71) testa a escala 7 duas vezes e nunca a 8. | Selecionar 4 grava escala 3; o texto continua “Levemente Desidratado”. Se a escala 8 estiver carregada ao abrir o formulário (FrmAnamnese:194), nenhuma opção fica marcada. | Proposta: correspondência direta entre opção e escala, inclusive na reabertura. Confirmar o resultado esperado. Ver H-12. |
| DIV-05 | `FrmBioimpC.CommandButton179_Click` (:90–189) transfere as classificações pela Caption, sem `Else`. Quatro controles não têm ramo; o grupo de cada um foi identificado pelo frame no manifesto: `Opt7Alto` → músculo esquelético (AQ); `Opt5Excel` → água (AW); `Opt6Alto` → massa muscular (AL); `Opt4Alto` → proteína (AY). `Opt3Alto` (óssea, BB) tem ramo (:141). A recarga (:738–895) também não reseleciona essas quatro opções. | Selecionar uma dessas opções mantém a classificação que já estava em FrmAvaliacaoC: vazia em avaliação nova, a anterior em edição. O laudo imprime esse texto em 4 dos 7 grupos do quadro de composição. **Defeito ativo no sistema atual**, não só na migração. | Na web, enumerar as opções de cada grupo e mapear todas explicitamente. Os demais grupos têm 2 ou 3 opções no código; conferir no Excel se faltam controles. As legendas extraídas são truncadas; confirmar os textos no Excel. Corrigir a planilha atual é uma decisão separada da migração. |
| DIV-06 | Leitura antiga do XML documentava Analise C!G7 como `=`. A fórmula é compartilhada com G6 e resolve para `=1-F7`. | Um catálogo baseado no PRD antigo levaria uma regra incorreta para a migração. | Resolvido no novo catálogo por interpretação da fórmula compartilhada. Nenhuma alteração na planilha. |
| DIV-07 | FrmAnamnese.CommandButton184_Click (:118–148): cadeia de `ElseIf` com `AND` em cada faixa. A guarda `If sistolica And diastolica <> 0` equivale a `sistolica And (diastolica <> 0)` e, na prática, exige as duas pressões diferentes de zero. | Resultados legados determinados: 150/80 → Hipertensão Estágio 1 (o ramo Sistólica Isolada é inalcançável para sistólica de 140 a 179); 190/105 e 170/115 → Não Classificado (o Estágio 3 exige ≥180 **e** ≥110); 85/70 → Ótima (Baixa exige <90 e <60). | As faixas coincidem com a classificação das VI Diretrizes Brasileiras de Hipertensão (2010); conferir. A clínica deve escolher a tabela de referência (existe diretriz mais recente) e a regra para pressões em categorias diferentes. Não derivar uma tabela nova automaticamente. Ver H-22. |
| DIV-08 | TipoCorpo usa limites de gordura 21/33 ou 8/20, mas imprime 18/28 no eixo. O comentário em LaudoAvaliacaoHtmlBuilder.cs:249–252 indica eixo fixo intencional, igual para os dois sexos. | Marcação visual e limiares efetivos podem comunicar coisas diferentes. | Separar apresentação dos limites da regra; decidir se o eixo varia por sexo e validar em documento de referência. |
| DIV-09 | Avaliações!DD1 sem cabeçalho; VBA e C# tratam DD como hora. | Mapeamento somente por cabeçalho perde a hora. | Resolvido no dicionário pela dupla evidência. Validar formato/hora local na especificação web. |
| DIV-10 | Formulário recebe kg de água/proteína, grava percentual com duas casas, C# reconstrói kg e arredonda. | Ida e volta kg→%→kg pode alterar o valor original. | Proposta: preservar kg original e percentual derivado com regra explícita; validar diferença em relação ao PDF atual antes de aprovar. |
| DIV-11 | SalvaAvaliacaoC não grava MLG%/residual/intervalos em colunas que os leitores podem consultar; protocolos gravam subconjuntos distintos da mesma linha. | Ausência ou reaproveitamento de valores de outro fluxo pode afetar saída. | Modelar campos aplicáveis por protocolo e proibir herança silenciosa ao trocar protocolo; verificar se troca existe na operação atual. |
| DIV-12 | LaudoRepositorio.MontarSerie (:269–290) inclui avaliações do mesmo Id com data menor ou igual à selecionada e ordena só por data. O `OrderBy` do LINQ é estável: em empate, prevalece a ordem física das linhas da aba. | Avaliações do mesmo dia gravadas depois da selecionada entram na série e aparecem à direita dela. | Definir desempate por data, hora e identificador; excluir as posteriores à selecionada no mesmo dia (pela hora, quando houver). |
| DIV-13 | Extração de controles retornou captions truncadas/deslocadas, inclusive caracteres de controle. | Rótulos poderiam ser documentados incorretamente. | Limitação do extrator: usar nomes apenas como indício; conferir legendas/propriedades no Excel antes de fechar opções e obrigatoriedades. |
| DIV-14 | Comentários de pontuação dizem 0–100, enquanto texto emitido admite >100. | Restrição arbitrária na web pode rejeitar valores aceitos hoje. | Definir domínio com a clínica e amostras do aparelho; não impor teto com base no comentário. |
| DIV-15 | `Age` (Idade.bas:2) só é chamada nos formulários de cadastro (FrmCadastro_C:552/555, FrmCadastroPL_C:1016/1019) e grava a idade em Clientes!E. Avaliações e anamneses novas copiam essa coluna (FrmCliente:281/331/379/782, lista carregada com os valores brutos da aba por FiltrosLTB.FiltroClientes). Nada recalcula Clientes!E ao abrir a pasta. `Age` usa `Now`; em ano não bissexto, nascidos em 29/02 fazem aniversário em 01/03. | A idade da avaliação é a do dia em que o cadastro foi salvo pela última vez, não a da data da avaliação. Um paciente cadastrado há dois anos, sem edição do cadastro, é avaliado com a idade de dois anos atrás. Faixa etária e todas as regras por idade (IMC, gordura, RCQ, energia, FC máxima) herdam o erro. **Defeito ativo no sistema atual.** | Proposta: calcular a idade na data da avaliação a partir do nascimento; preservar a idade gravada nos registros históricos. Definir o aniversário de 29/02. Ver H-10. |
| DIV-16 | `IMCidoso` (IMC.bas:4) rotula IMC >27 como “Obesidade”. O IMC-alvo padrão é fixo em 22,5 (FrmAvaliacaoA:377, FrmAvaliacaoB:226, FrmAvaliacaoC:225, FrmCalcPeso:148/166). A gordura-alvo sugerida vem de `TabelaGorduraAlvo`, chamada ao abrir a calculadora (ex.: FrmAvaliacaoC:224) e nos cliques de opção (FrmCalcPeso:147/165). | Textos e padrões clínicos sem referência registrada. A classificação de Lipschitz, comum para idosos, usa “sobrepeso” acima de 27; conferir a fonte adotada. | Preservar os textos até aceite. Registrar a referência bibliográfica de cada regra na versão das regras. Confirmar se o IMC-alvo 22,5 e a gordura-alvo sugerida continuam na web como sugestões editáveis. |

## 3. Situação e recomendação

| ID | Status | Recomendação | Bloqueia |
|---|---|---|---|
| DIV-01 | Confirmado estático | Corrigir para IMC em kg/m² após conferir coeficientes e limiar | Plano 3 |
| DIV-02 | Confirmado estático; executar no Excel para confirmar a predição | Corrigir: dados da avaliação corrente | Plano 3 |
| DIV-03 | Confirmado estático | Decisão clínica: classificar 5–19 anos ou exibir “não classificado” | Plano 3 |
| DIV-04 | Confirmado estático | Corrigir: opção igual à escala | Plano 2 |
| DIV-05 | Confirmado estático; conferir legendas no Excel | Corrigir: mapear todas as opções por grupo | Plano 4 |
| DIV-06 | Resolvido | — | — |
| DIV-07 | Confirmado estático | Decisão clínica: tabela de PA e regra para categorias diferentes | Plano 2 |
| DIV-08 | Confirmado estático | Separar apresentação e regra; decidir eixo por sexo | Plano 5 |
| DIV-09 | Resolvido | — | — |
| DIV-10 | Proposta | Preservar kg original; percentual derivado | Plano 3 |
| DIV-11 | Proposta | Campos aplicáveis por protocolo | Planos 3 e 4 |
| DIV-12 | Confirmado estático | Corrigir o desempate | Plano 5 |
| DIV-13 | Limitação do extrator | Conferir legendas no Excel | Planos 2 e 4 |
| DIV-14 | Decisão clínica | Definir domínio com amostras do aparelho | Plano 3 |
| DIV-15 | Confirmado estático | Corrigir: idade na data da avaliação | Plano 3 |
| DIV-16 | Decisão clínica | Preservar textos até aceite; registrar referência | Plano 3 |

## 4. Casos de referência propostos

Os casos abaixo são especificações de verificação. Não foram executados no VBA nem na aplicação web. Valores aritméticos simples foram conferidos de forma independente; limites de classificação e efeitos de eventos precisam de execução no Excel em cópia controlada, com dados sintéticos.

| Caso | Entrada | Resultado/critério esperado | Regras |
|---|---|---|---|
| H-01 | Peso 80 kg, gordura 20% | Gordura 16 kg; massa livre 64 kg e 80%, observando a sequência de arredondamentos. | AV-03 |
| H-02 | Peso 80 kg, altura 200 cm, adulto | IMC 20,00 no fluxo A. B/C devem preservar o valor informado/importado conforme protocolo. | AV-01/02 |
| H-03 | Cintura 80 cm, quadril 100 cm | RCQ 0,80. Testar quadril zero e ausente separadamente. | AV-05 |
| H-04 | C: peso 80 kg; componente 16 kg | Percentual global 20,00%. Não aplicar essa fórmula ao percentual segmentar relativo ao padrão. | AV-06/PDF-10 |
| H-05 | Feminino, 60 kg, 165 cm, 40 anos, HB | Expressão 1347,09; função retorna 1347; apresentação pode mostrar 1347,00. | EN-01 |
| H-06 | Feminino, 60 kg, 40 anos, OMS, fator 1,55 | TMB 1351,00; GET 2094,05. | EN-02/04 |
| H-07 | Fluxo B: peso 80 kg, gordura 20%, alvo 15% | Peso alvo apresentado 75,29 kg; controle −4,71 kg a partir do valor formatado. Em A/C, o legado depende de redigitação (DIV-02); a proposta usa os dados da avaliação corrente. | AV-12/DIV-02 |
| H-08 | IMC alvo 22, altura atual 170 cm; fluxo A ou C sem FrmAvaliacaoB carregado | Legado previsto: peso alvo 0,00 kg (altura de B = 0). Proposta corrigida: 63,58 kg para A/B/C. Confirmar a predição antes da decisão DIV-02. | AV-13/DIV-02 |
| H-09 | Composição kg com dízimas percentuais | Registrar kg original, percentual arredondado e kg reconstruído; documentar diferença. | DIV-10 |
| H-10 | Idade imediatamente antes/depois de 5, 20 e 60; aniversário próximo; nascido em 29/02; cadastro salvo há mais de um ano | Classificador correto e data-base explícita. Legado: usa a idade gravada em Clientes!E no último salvamento do cadastro. | CAD-02/AV-02/DIV-15 |
| H-11 | Cada limite das tabelas e valores imediatamente abaixo/acima | Igualdade inclusiva/exclusiva deve coincidir com a versão aprovada da regra, sem perda por arredondamento. | CL-01 a CL-07 |
| H-12 | Escala de urina: opção 4; reabertura com escala 8 | Legado: grava 3 (Levemente Desidratado); ao reabrir com 8, nenhuma opção marcada. Saída corrigida depende do aceite DIV-04. | AN-04 |
| H-13 | FC máxima 180, repouso 70 | Reserva 110. Casos de FC máxima usam o método selecionado e arredondamento próprio. | AN-01/02 |
| H-14 | Peso 60 kg; 35 mL/kg | Água ideal 2100,00 mL. | AN-03 |
| H-15 | Oito avaliações em datas distintas | Seleção legado [1,4,5,6,7,8]. Para datas iguais, resultado só após definir DIV-12. | PDF-01 |
| H-16 | Cada opção manual dos grupos C, incluindo `Opt4Alto`, `Opt5Excel`, `Opt6Alto` e `Opt7Alto` | Texto correto transferido, salvo, recarregado e emitido; nenhum valor anterior permanece por falta de ramo. Legado: essas quatro opções mantêm a classificação anterior. | CL-08/DIV-05 |
| H-17 | PDF Relaxmedic com campo não reconhecido, zero e valor segmentar | Revisão obrigatória, distinção de estados, nenhuma confirmação automática; conferir as 44 zonas. | OCR-01 a OCR-06 |
| H-18 | CPF/CEP com zero inicial e cadastro importado novamente | Proposta: preservar texto e impedir duplicação da mesma origem/ID; divergências vão para revisão. | CAD-04/05 |
| H-19 | Dois usuários editando a mesma revisão | Detectar conflito ao salvar; não perder alterações silenciosamente. | Proposta de concorrência |
| H-20 | Correção após emissão | Nova revisão e novo PDF; documento anterior acessível com dados/regras/template de origem. | Decisão de revisões |
| H-21 | Quatro documentos, nomes longos e campos vazios | Conteúdo completo, paginação legível, tabelas sem cortes, fontes e cores aprovadas. | PDF-09/11 |
| H-22 | PA 150/80, 190/105, 170/115 e 85/70; cada limite da tabela | Legado: Estágio 1; Não Classificado; Não Classificado; Ótima. A saída web depende da tabela escolhida em DIV-07. | AN-05/DIV-07 |
| H-23 | DRI adultos, masculino, 40 anos, 90 kg, 173 cm (IMC 30,07) | Legado: 1875 (ramo de peso normal; expressão 1875,465). Corrigido: 1841 (ramo de sobrepeso; expressão 1841,372). | EN-03/DIV-01 |

## 5. Portões por plano de implementação

Cada plano precisa produzir software testável com valores esperados exatos. Por isso a implementação é dividida em planos por subsistema, e cada plano só é escrito quando suas dependências estão aceitas.

| Plano | Escopo | Pode ser escrito agora? | Depende de |
|---|---|---|---|
| 1. Fundação | Estrutura do projeto, NestJS + Prisma + PostgreSQL, React + Vite, login, perfis e permissões, auditoria, pacientes, importação de cadastros | Sim | Aceite dos padrões propostos abaixo |
| 2. Anamnese | Formulário, FC máxima e de reserva, água ideal, escala de urina, pressão arterial | Parcial | DIV-04; DIV-07 (tabela de PA escolhida); DIV-13 |
| 3. Motor de regras A, B/R e C | Cálculos, classificações, energia, metas, versão das regras | Não | DIV-01, 02, 03, 10, 14, 15 e 16; execução dos casos H no Excel |
| 4. Avaliações e revisões | Telas por protocolo, rascunho e finalização, revisões, concorrência | Depois do Plano 3 | Critérios mínimos de finalização por protocolo; DIV-05; DIV-11; política de exclusão e retenção |
| 5. Documentos PDF | Os quatro documentos com Chromium, templates versionados | Não | Inventário das variantes H/M, 2, 3 e BHM; modelos de anamnese e ficha; DIV-08; DIV-12 |
| 6. OCR | Modelo atual Relaxmedic | Não | Escolha do motor após teste com PDFs representativos; limites de arquivo e retenção |
| 7. Implantação | Hospedagem, backup, transição | Não | Provedor, região, orçamento, política de internet, metas de backup e restauração |

Padrões do Plano 1, aceitos pelo usuário em 2026-09-27 (código em repositório separado, `C:\Sistema\PSM\psm-web`; plano em [2026-09-27-plano-1-fundacao.md](../superpowers/plans/2026-09-27-plano-1-fundacao.md)):

- Sessão mantida no servidor, com cookie httpOnly.
- Duplicidade na importação detectada pelo identificador legado, com alerta quando CPF ou nome + nascimento coincidirem.
- Fotos e anexos cadastrais fora da carga inicial; o anexo de anamnese não entra, conforme a carga aprovada.
- Permissões do assistente conforme a consolidação (seção 5), com finalização e emissão reservadas ao profissional e ao administrador; a delegação fica como opção configurável desligada.

Nenhum desses itens invalida as decisões de produto já aprovadas. Eles delimitam o trabalho necessário antes de cada plano.
