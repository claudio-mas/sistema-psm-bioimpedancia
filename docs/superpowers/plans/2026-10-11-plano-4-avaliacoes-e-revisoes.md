# Plano 4 — Avaliações e revisões (psm-web) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar a gravação das avaliações A, B/R e C na web:

- rascunho e finalização, com a versão das regras e o resultado do cálculo gravados;
- telas por protocolo, com a prévia do Plano 3;
- classificações manuais da B e da C, com todas as opções de cada grupo;
- todos os campos que cada protocolo grava na aba Avaliações;
- concorrência com versão do registro;
- revisões das avaliações finalizadas, com justificativa, autor, data e campos alterados.

**Architecture:** A configuração `avaliacao-v2` repete os cálculos da v1, que é imutável. Ela acrescenta quatro coisas:

- as opções das classificações manuais;
- os limites dos campos novos;
- os critérios de finalização;
- as legendas do legado nos métodos de TMB.

A validação (`assessment-normalize.ts`) passa a cobrir todos os campos de cada protocolo. Uma função pura (`evaluateAssessment`) valida, calcula e separa três tipos de erro: os de campo, que impedem o rascunho; o da TMB, que só impede a finalização; e os de finalização. Cada linha da tabela `assessments` é uma revisão. Uma série junta as revisões da mesma avaliação. As entradas do protocolo, as classificações e o resultado do cálculo ficam em três colunas JSONB, gravadas a cada salvamento, e o resultado gravado é o oficial. A concorrência usa versão otimista (`updateMany` com versão e status) e responde 409 com o estado atual. A criação de revisão é serializada pela trava `pg_advisory_xact_lock(7302004)`. A web mostra um formulário por protocolo, gerado a partir de listas de campos, e segue o padrão da anamnese.

**Tech Stack:** a mesma dos Planos 1 a 3, sem dependência nova:

- Node 24, NestJS 11, Prisma 6, PostgreSQL 16 (Docker, porta 5433; banco `psm_test` nos testes), Jest 29 + Supertest;
- React 19, Vite 6, React Router 7, TanStack Query 5, Tailwind 4, Vitest 3 + Testing Library.

**Spec:**
- Divergências e homologação: [divergencias-e-homologacao.md](../../migracao-web/divergencias-e-homologacao.md):
  - DIV-05, DIV-11, DIV-13 (avaliações) e DIV-14;
  - casos H-16, H-19 e H-20;
  - §5, portões por plano.
- Regras: [catalogo-regras.md](../../migracao-web/catalogo-regras.md):
  - CL-08 e AV-07;
  - "Proposta de persistência web" (fim da seção 1).
- Código legado: [anexo-fontes-regras.md](../../migracao-web/anexo-fontes-regras.md), `FrmBioimpC.CommandButton179_Click`.
- Campos: [dicionario-dados.md](../../migracao-web/dicionario-dados.md):
  - "Avaliações — 108 posições A:DD", colunas por protocolo;
  - "Dados necessários na web que não possuem coluna equivalente".
- Concorrência, permissões e revisões: [consolidação](../specs/2026-09-27-migracao-web-brainstorming.md), §4, §5 e §9 (navegação).
- Decisões da pessoa de 2026-10-11: seção "Decisões incorporadas", abaixo.

Os caminhos de código abaixo são relativos a `C:\Sistema\PSM\psm-web`. Os comandos são em PowerShell e rodam nessa pasta, salvo indicação. Este plano foi escrito sobre o código real da branch `master`, no commit `44fa921` (Planos 1, 2 e 3 concluídos). Base conferida em 2026-10-11:

- API com 46 suítes, 631 testes e 1 instantâneo;
- web com 19 arquivos e 349 testes.

Os valores esperados dos cálculos foram conferidos rodando o código real do Plano 3.

## Decisões incorporadas (2026-10-11)

Todas as decisões abaixo são da pessoa. As clínicas são **provisórias** e ficam a validar com o médico responsável, pelo método das premissas. O executor não as reabre. Se algo contradisser uma delas, registre em "Decisões e bloqueios" e pare.

1. **P4-1, escopo.** Este plano cobre:
   - rascunho e finalização das avaliações A, B/R e C, com `rulesVersion`;
   - telas por protocolo, com a prévia;
   - classificações manuais da B e da C;
   - concorrência;
   - revisões das avaliações finalizadas.

   Ficam fora: o vínculo das revisões com os PDFs (Plano 5), o OCR (Plano 6) e as revisões da anamnese (depois, reaproveitando o mecanismo).
2. **P4-2, finalização (provisória, premissa PC-14).**
   - Todos os protocolos exigem protocolo, data, peso e altura, e nenhum erro de campo, inclusive o da TMB calculada não válida.
   - A e B exigem também a gordura (%).
   - C exige também a gordura (kg).
   - B exige também o IMC do aparelho.

   O resto é opcional. O rascunho aceita qualquer subconjunto válido; exige só protocolo e data, porque a idade depende da data.
3. **P4-3, protocolo (DIV-11).** O protocolo é escolhido ao criar e não muda. Para outro protocolo, cria-se outra avaliação. O corpo do PUT não tem o campo `protocol`.
4. **P4-4 e P4-10, classificações manuais (CL-08, DIV-05, DIV-13, H-16).**
   - Cada grupo da B e da C tem as opções mapeadas explicitamente, com as legendas exatas conferidas no Excel.
   - A gravação guarda o texto escolhido. A reabertura marca a opção pelo texto.
   - A B tem 4 grupos, com as legendas do FrmBioimpR: IMC com as faixas da OMS; gordura e músculo esquelético com `Baixo (-)`, `Normal (0)`, `Alto (+)` e `Muito Alto (++)`; gordura visceral com `0 (Normal)`, `(+) Alto` e `(++) Muito Alto`. Não há texto livre.
   - A C tem 13 grupos e 45 opções, inclusive as 4 que o legado não transfere (`Opt4Alto`, `Opt5Excel`, `Opt6Alto` e `Opt7Alto`) e a `Opt3Alto`.
5. **P4-5, o que se grava.** Grava-se cada vez que se salva ou finaliza:
   - as entradas validadas;
   - `rulesVersion`;
   - o resultado do cálculo.

   Na avaliação finalizada, o resultado gravado é o oficial e nunca é recalculado na leitura.
6. **P4-6, revisões (H-20).**
   - A avaliação finalizada é somente leitura.
   - "Nova revisão" exige `assessments.finalize` e uma justificativa. Ela cria um rascunho da revisão seguinte, copiado da versão vigente.
   - Ao ser finalizada, a revisão passa a ser a vigente. A anterior fica marcada como substituída, com autor e data, e continua consultável.
   - A revisão grava os campos alterados em relação à anterior e usa a versão de regras corrente (cada salvamento recalcula com ela e grava qual foi).
7. **P4-7, exclusão.**
   - Um rascunho (revisão 1) pode ser excluído por quem tem `assessments.draft` ou `assessments.finalize`, com auditoria.
   - A finalizada e as revisões finalizadas nunca são excluídas na v1. A retenção fica para o Plano 7.
8. **P4-8, concorrência (H-19).** Como na anamnese: versão do registro no salvamento. Uma versão desatualizada recebe 409 com o estado atual, sem perder o que o outro gravou.
9. **P4-9, P4-11 e P4-12, campos (dicionario-dados).** Cada protocolo grava todas as colunas da aba Avaliações que preenche. Os campos do aparelho são digitados à mão até o OCR (Plano 6).
   - **Todos:** peso, altura e as 14 circunferências (pescoço, ombros, peitoral, braços, antebraços, abdômen, cintura, quadril, coxas e panturrilhas), além de gordura-alvo e IMC-alvo, método de TMB, TMB do aparelho, fator de atividade, observação e conclusão.
   - **A:** gordura (%) e diâmetros do punho e do fêmur, em mm.
   - **B:** IMC do aparelho, gordura (%), músculo esquelético (%), nível de gordura visceral, idade corporal e as 4 classificações.
   - **C:** IMC do aparelho e os kg de gordura, massa óssea, proteína, água, massa muscular e músculo esquelético. Também a massa livre de gordura informada, o nível de gordura visceral, a gordura subcutânea (%), a idade corporal, a pontuação e a hora da medição. Entram ainda a gordura e o músculo por segmento (% do padrão e kg, cinco segmentos), as impedâncias Z20 e Z100 (cinco segmentos) e as 13 classificações.
10. **Pontuação (achado de 2026-10-11, nova DIV).** O FrmAvaliacaoC não tem o controle `TxtPontuacao`. No legado, a pontuação digitada provavelmente nunca chega a Avaliações!CI. A web grava a pontuação da C (P4-9), como inteiro de 0 a 999 (DIV-14, PC-13).
11. **Respostas de 2026-10-11 a este plano.**
    - **a) Métodos de TMB:** as legendas do legado (FrmNecessidadeEnergetica), na `avaliacao-v2`:
      - `Harris-Benedict (1919) - 15 aos 74 anos`;
      - `OMS (Organização Mundial da Saúde) 1985`;
      - `Comitê DRI - Crianças e Adolescentes com Peso Normal ou Sobrepeso/Obesidade.`;
      - `Comitê DRI - Adultos com Peso Normal ou Sobrepeso/Obesidade.`;
      - `Calculado pela Bioimpedância`.

      A web não bloqueia o Harris-Benedict fora de 15 a 74 anos.
    - **b) `history.read`:** controla as revisões anteriores. Ver uma revisão substituída e a lista de revisões, com os campos alterados, exige também `history.read`. A avaliação vigente e os rascunhos seguem a regra geral: `patients.read` e mais `assessments.draft` ou `assessments.finalize`.
    - **c) Revisão em rascunho:** editar, finalizar e descartar exigem `assessments.finalize`. O descarte é auditado, e a versão vigente não muda.
    - **d) Limites técnicos dos campos novos.** Não são regra clínica; todos aceitam até 2 casas, salvo os inteiros:
      - idade corporal: maior que 0 e até 150 anos;
      - % segmentar (do padrão): maior que 0 e até 1000;
      - impedância: inteiro, maior que 0 e até 2000 Ω;
      - hora: de 00:00 a 23:59;
      - kg segmentares e massa livre da C: como os kg de componente (maior que 0 e até 500);
      - gordura subcutânea: como os percentuais (maior que 0 e menor que 100);
      - circunferências novas: como as atuais (maior que 0 e até 300 cm);
      - observação e conclusão: até 4000 caracteres;
      - justificativa da revisão: até 2000 caracteres.

### Decisões técnicas deste plano

Elas decorrem das regras do próprio código ou da especificação. O executor também não as reabre.

- **`avaliacao-v2`.** A v1 é imutável, e o instantâneo dela nunca é regravado. Opções, limites novos, critérios de finalização e legendas entram numa versão nova, com os mesmos cálculos; um teste prova que só esses itens mudam. A v2 tem instantâneo próprio, que também nunca é regravado. Os testes do Plano 3 que leem a v1 diretamente continuam iguais.
- **Pendência técnica do Plano 3.** `avaliacaoRulesForVersion` e `rulesForVersion` (anamnese) passam a aceitar só chaves próprias (`Object.hasOwn`). A visão da avaliação chama a função com o `rulesVersion` lido do banco. `toString`, `constructor`, `__proto__` e `hasOwnProperty` viram "versão desconhecida".
- **Modelo de dados.**
  - Cada linha de `assessments` é uma revisão. `seriesId` junta as revisões; a revisão 1 tem `seriesId = id`.
  - "Vigente" é a finalizada sem `supersededAt`.
  - Cada série tem no máximo uma revisão em rascunho.
  - Entradas, classificações e cálculo ficam em JSONB:
    - `inputs` e `classifications` guardam só as chaves do protocolo (DIV-11: o que não se aplica nem aparece; `null` é "não informado");
    - `calculation` é o `AssessmentCalculation` do momento do salvamento.
- **TMB calculada não válida.** O rascunho é gravado assim mesmo; a prévia mostra o erro e a finalização o recusa (P4-2).
- **Nova revisão.** Copia literalmente a vigente: entradas, classificações, cálculo, versão das regras, sexo e idade. O primeiro salvamento ou a finalização recalcula com a versão corrente e o cadastro atual. Uma cópia literal nunca falha nem perde campos, mesmo que o cadastro tenha mudado depois.
- **Classificações da C na tela.** Os grupos seguem a ordem das colunas da aba Avaliações (AE, AH, AJ, AL, AQ, AS, AU, AW, AY, BB, BE, BM, BT). As opções vão do menor para o maior: Baixo, Saudável ou Médio, Excelente, Alto. Cada opção guarda o controle do legado (`legacyControl`) para rastreabilidade; ele não vai para a web.
- **Conflitos de revisão.** Nenhum deles devolve a revisão substituída a quem não tem `history.read`. Salvar, finalizar, excluir ou revisar uma revisão substituída responde 409 `'Esta avaliação já foi substituída por uma revisão mais recente.'`, com a vigente em `current`.

## Global Constraints

- Continuam valendo as restrições dos Planos 1 a 3:
  - dados de teste sempre sintéticos;
  - campo ausente é `null`, nunca `0` nem `''`;
  - interface e mensagens em português do Brasil;
  - fuso `America/Sao_Paulo`;
  - datas civis como `date`, instantes como `timestamptz`;
  - a pasta `zanoni` é só leitura durante a execução;
  - o `Bioimpedancia.xlsm` não é alterado.
- Regras clínicas só em `apps/api/src/clinical-rules/`, como funções puras. Faixas, textos, opções, limites técnicos e critérios de finalização ficam só em `avaliacao-rules.ts`. A web não calcula nem classifica.
- Versões publicadas são imutáveis: `AVALIACAO_RULES_V1` e, depois da Task 1, `AVALIACAO_RULES_V2`. **Nunca rode `jest -u`.** Os instantâneos `avaliacao-rules.spec.ts.snap` e `avaliacao-rules-v2.spec.ts.snap` nunca são regravados; o segundo é gerado uma única vez, na Task 1.
- Toda gravação usa `CURRENT_AVALIACAO_RULES` (`avaliacao-v2`). A leitura nunca recalcula: mostra o `calculation` gravado, e as legendas vêm da versão da própria linha (`avaliacaoRulesForVersion(row.rulesVersion)`).
- A avaliação grava cópias do sexo e da idade. Elas são refeitas a partir do cadastro a cada salvamento de rascunho e congeladas na finalização.
- Premissa nova: PC-14 (critérios de finalização). Ela tem a marcação `// TODO(PREMISSA-CLINICA PC-14): validar com o médico responsável ...` e uma linha em `docs/premissas-clinicas.md`, e `premissas.spec.ts` passa a esperar PC-01 a PC-14.
- Testes à prova de mutação:
  - cada fronteira testada dos dois lados (aceito e recusado), inclusive datas (hoje e amanhã, 1900-01-01, dia do nascimento e véspera);
  - intervalos e referências com mais de um sexo e idade;
  - campo vazio com outros preenchidos (cálculo parcial sem exceção);
  - campo de outro protocolo e fora do limite;
  - protocolo inválido com números;
  - cada permissão isolada na combinação que de fato a isola, e 401 sem sessão;
  - valores com 2 casas cujo produto por 100 não é exato em ponto flutuante (40,02; 0,07);
  - esperados sempre literais, nunca lidos da configuração.
- Commits sempre com a lista explícita de arquivos. **Nunca use `git add -A`, `git add .` nem `git commit -a`**, porque `AGENTS.md` e `CLAUDE.md` não são versionados.
- Permissões das rotas:

  | Rota | Exige |
  |---|---|
  | `GET /api/assessments/rules` | `assessments.draft` ou `assessments.finalize` |
  | prévia, lista, leitura, criação, PUT e DELETE | `patients.read` e mais `assessments.draft` ou `assessments.finalize` |
  | `POST /api/assessments/:id/finalize` e `POST /api/assessments/:id/revisions` | `patients.read` e `assessments.finalize` |
  | `GET /api/assessments/:id/revisions` | `patients.read`, `history.read` e mais `assessments.draft` ou `assessments.finalize` |
  | `GET /api/assessments/:id` de uma revisão substituída | também `history.read` (checado no serviço) |
  | PUT, finalização e DELETE de revisão maior que 1 | `assessments.finalize` (checado no serviço) |

  As permissões de anamnese não valem para avaliações.
- Concorrência: `updateMany`/`deleteMany` com `where: { id, version, status: 'RASCUNHO' }` e 409 com `current`. A criação de revisão usa `pg_advisory_xact_lock(7302004)`; as chaves 7302001 a 7302004 ficam em uso e a próxima livre, 7302005, fica reservada.
- Auditoria na mesma transação da escrita (`entityType: 'assessment'`):
  - `assessment.create`, com `{ patientId, seriesId, revision, protocol }`;
  - `assessment.update` e `assessment.finalize`, com `{ patientId, seriesId, revision, changes }`; na finalização de revisão, também `supersededId`;
  - `assessment.delete`, com `{ patientId, seriesId, revision, protocol, assessedOn }`;
  - `assessment.revision.create`, com `{ patientId, seriesId, revision, fromId, reason }`.

  `changes` vem de `assessmentChanges` (Task 4). Ela compara status, protocolo, data, versão das regras, sexo, idade, `inputs.*` e `classifications.*`, sem o resultado do cálculo.
- Mensagens públicas:
  - 400 só com `'Dados inválidos.'` e `errors` por campo, formato que `errorMessage` da web já trata como público. Nenhuma mensagem 400 nova é criada. O 400 genérico do `ValidationPipe` (campo desconhecido, protocolo no PUT, texto longo) cai no texto genérico da web.
  - 409 com estes textos:
    - `'Esta avaliação foi alterada por outra pessoa. Recarregue para ver a versão atual.'`;
    - `'Esta avaliação já foi finalizada e não pode ser alterada.'`;
    - `'Avaliação finalizada não pode ser excluída.'`;
    - `'Só é possível revisar uma avaliação finalizada.'`;
    - `'Esta avaliação já foi substituída por uma revisão mais recente.'`;
    - `'Já existe uma revisão em andamento desta avaliação.'`.
- Web: efeitos sobre cache, sessão, navegação ou estado vão só nos callbacks síncronos das **opções** de `useMutation` (REGRA da Task 9 do Plano 2). Não use `mutate(vars, callbacks)`, `mutateAsync`, `then` nem efeito depois de `await`. `errorMessage` só mostra mensagens públicas.

## Review Focus

Estes pontos não aparecem explicitamente na especificação, mas são onde a pessoa mais provavelmente seria prejudicada. Cada um tem teste na tarefa indicada.

1. **Classificação manual que não chega ao registro (DIV-05, H-16).**
   - Cada uma das 45 opções da C e das 15 da B é gravada e relida exatamente.
   - Ao trocar de opção, a nova substitui a anterior. No legado, as 4 opções sem ramo mantinham o texto anterior.
   - A reabertura marca a opção gravada.
   - Uma legenda da C num grupo da B, ou da B num grupo da C, é recusada.

   Testes: Tasks 2, 6, 7 e 11.
2. **Valor herdado de outro protocolo (DIV-11, P4-3).**
   - O PUT não aceita `protocol`.
   - Campo de outro protocolo é recusado no campo, mesmo fora do limite.
   - O registro só tem as chaves do protocolo.
   - A revisão herda o protocolo da vigente.

   Testes: Tasks 2, 4, 5, 6 e 7.
3. **Duas pessoas na mesma avaliação (H-19).** Cada caso é testado:
   - dois salvamentos da mesma versão;
   - salvamento tardio enquanto outra pessoa finaliza;
   - exclusão com versão antiga;
   - duas "nova revisão" ao mesmo tempo, que esbarram na trava 7302004.

   Na web, "Carregar versão atual" aparece depois de um 409.

   Testes: Tasks 5, 8, 9 e 12.
4. **Histórico que muda depois de emitido (H-20, P4-5).**
   - A finalizada não muda, mesmo com o cadastro do paciente alterado.
   - A substituída guarda os dados e a versão de regras dela.
   - A leitura não recalcula.
   - Uma linha `avaliacao-v1` mostra a legenda curta; uma `avaliacao-v2` mostra a do legado.
   - Uma versão desconhecida no banco (`toString`) é erro, e não um objeto herdado.

   Testes: Tasks 1, 4, 8 e 9.
5. **Permissão retirada ou parcial.**
   - Só as permissões de anamnese não bastam.
   - Sem `patients.read`, o 403 não traz dado.
   - Sem `history.read`, nem a substituída nem a lista de revisões aparecem, e a vigente continua.
   - O assistente sem `assessments.finalize` não edita, não finaliza e não descarta revisão.
   - Na web, um 403 nas avaliações não derruba o cadastro do paciente.

   Testes: Tasks 5, 8, 9, 10 e 13.

---

## Mapa de arquivos

```
psm-web/
  docs/premissas-clinicas.md                       + PC-14 (critérios de finalização)
  README.md                                        entrega e verificação do Plano 4
  apps/api/
    prisma/schema.prisma                           + enums AssessmentStatus e AssessmentProtocol; modelo Assessment
    prisma/migrations/<carimbo>_assessments/       migração gerada pelo Prisma
    src/clinical-rules/
      avaliacao-rules.ts                           + avaliacao-v2 (opções, limites novos, finalização, legendas); Object.hasOwn
      avaliacao-rules.spec.ts                      versão corrente v2; chaves herdadas
      avaliacao-rules-v2.spec.ts                   conteúdo da v2 e instantâneo próprio
      __snapshots__/avaliacao-rules-v2.spec.ts.snap  gerado uma vez pelo Jest (Task 1)
      anamnese-rules.ts / anamnese-rules.spec.ts   Object.hasOwn e chaves herdadas
      premissas.spec.ts                            + PC-14
    src/assessments/
      assessment-normalize.ts                      todos os campos por protocolo, classificações, hora, inteiros
      assessment-evaluation.ts                     valida + calcula; erros de campo, da TMB e de finalização
      assessment-record.ts                         registro do protocolo (JSONB) e campos alterados
      assessment.view.ts                           visão, lista (cabeças das séries), série, item de revisão
      assessment-rules.view.ts                     + opções, limites novos e finalização
      dto/assessment.dto.ts                        campos, corpo, atualização, exclusão, revisão (substitui assessment-calculation.dto.ts)
      assessments.service.ts                       prévia, lista, leitura, rascunho, finalização, exclusão, revisões
      assessments.controller.ts                    rotas
    test/assessments-calculation.e2e-spec.ts       versão v2 nas regras e na prévia
    test/assessments.e2e-spec.ts                   rascunho da A, 409, permissões
    test/assessments-b.e2e-spec.ts                 gravação da B e classificações
    test/assessments-c.e2e-spec.ts                 gravação da C, 13 classificações (H-16), pontuação
    test/assessments-finalize.e2e-spec.ts          finalização, congelamento, exclusão, concorrência
    test/assessments-revisions.e2e-spec.ts         revisões, history.read, trava 7302004
  apps/web/src/
    lib/query-client.ts                            403 em cascata: pacientes → avaliações
    assessments/
      types.ts                                     tipos da API, protocolos, permissões
      fields.ts                                    campos por protocolo e seção, resultados, rótulos de alterações
      PatientAssessmentsSection.tsx                lista na página do paciente
      ProtocolPickerPage.tsx                       escolha do protocolo (P4-3)
      ClassificationField.tsx                      grupo de opções com "Limpar classificação"
      AssessmentFormPage.tsx                       formulário, prévia, rascunho, finalização, exclusão, revisões
      RevisionsPanel.tsx                           histórico de revisões (history.read)
    patients/PatientFormPage.tsx                   mostra a seção de avaliações
    router.tsx                                     rotas das avaliações
    test/assessment-fixtures.ts                    dados sintéticos de avaliação
```

---

## Antes da Task 1

- [ ] **Conferir a base**

```powershell
Set-Location C:\Sistema\PSM\psm-web
git status --short
git switch -c plano-4-avaliacoes
npm run db:up
npm test
```

Expected:
- só `AGENTS.md` e `CLAUDE.md` aparecem como não rastreados;
- a branch é criada a partir de `master` em `44fa921`;
- a API passa com 46 suítes (631 testes, 1 instantâneo);
- a web passa com 19 arquivos (349 testes).

- [ ] **Apontar as instruções dos agentes para este plano**

Em `CLAUDE.md` e `AGENTS.md`, que não são rastreados e continuam assim, troque a linha `Plano:` por:

```
Plano: C:/Sistema/PSM/Projeto 14/zanoni/docs/superpowers/plans/2026-10-11-plano-4-avaliacoes-e-revisoes.md
```

---

### Task 1: `avaliacao-v2`, chaves herdadas e visão das regras

**Files:**
- Modify: `apps/api/src/clinical-rules/avaliacao-rules.ts`
- Modify: `apps/api/src/clinical-rules/anamnese-rules.ts` (só `rulesForVersion`)
- Modify: `apps/api/src/clinical-rules/avaliacao-rules.spec.ts`, `apps/api/src/clinical-rules/anamnese-rules.spec.ts`, `apps/api/src/clinical-rules/premissas.spec.ts`
- Create: `apps/api/src/clinical-rules/avaliacao-rules-v2.spec.ts` (o Jest gera `__snapshots__/avaliacao-rules-v2.spec.ts.snap`)
- Modify: `apps/api/src/assessments/assessment-rules.view.ts`
- Modify: `apps/api/test/assessments-calculation.e2e-spec.ts`
- Modify: `docs/premissas-clinicas.md`

**Interfaces:**
- Consumes: `AVALIACAO_RULES_V1`, `AvaliacaoRules`, `Range`, `LimitKey`, `deepFreeze` e `range` de `avaliacao-rules.ts` (Plano 3).
- Produces, em `avaliacao-rules.ts`:

```ts
export type RecordLimitKey = 'bodyAgeYears' | 'segmentReferencePct' | 'impedanceOhm';
export type ClassificationProtocol = 'B' | 'C';
export type ClassificationField =
  | 'bmiClassification' | 'fatClassification' | 'musclePctClassification' | 'muscleKgClassification'
  | 'skeletalMuscleClassification' | 'visceralFatClassification' | 'subcutaneousFatClassification' | 'waterClassification'
  | 'proteinClassification' | 'boneClassification' | 'bodyAgeClassification' | 'bmrClassification' | 'totalWeightClassification';
export interface ClassificationOption { text: string; hint: string | null; legacyControl: string | null }
export interface ClassificationGroup { field: ClassificationField; label: string; source: string | null; options: readonly ClassificationOption[] }
export type FinalizeRequiredField = 'weightKg' | 'heightCm' | 'fatPct' | 'fatKg' | 'informedBmi';
export interface AvaliacaoRecordRules extends AvaliacaoRules {
  recordLimits: Readonly<Record<RecordLimitKey, Range>>;
  manualClassifications: Readonly<Record<ClassificationProtocol, readonly ClassificationGroup[]>>;
  finalizeRequired: Readonly<Record<AssessmentProtocol, readonly FinalizeRequiredField[]>>;
}
export const AVALIACAO_RULES_V2: AvaliacaoRecordRules;
export const CURRENT_AVALIACAO_RULES: AvaliacaoRecordRules; // = AVALIACAO_RULES_V2
export function avaliacaoRulesForVersion(version: string): AvaliacaoRules; // só chaves próprias
```

- Produces, em `assessment-rules.view.ts`:
  - `ClassificationGroupView = { field; label; source; options: { text; hint }[] }`;
  - `AssessmentRulesView`, que ganha `limits: Record<LimitKey | RecordLimitKey, Range>`, `manualClassifications: Record<ClassificationProtocol, ClassificationGroupView[]>` e `finalizeRequired: Record<AssessmentProtocol, FinalizeRequiredField[]>`;
  - `toAssessmentRulesView(rules: AvaliacaoRecordRules)`.

- [ ] **Step 1: Escrever os testes**

Em `apps/api/src/clinical-rules/avaliacao-rules.spec.ts`, troque o primeiro teste (`'a versão corrente é a avaliacao-v1, provisória'`) e o import por:

```ts
import {
  AVALIACAO_RULES_BY_VERSION, AVALIACAO_RULES_V1, AVALIACAO_RULES_V2, avaliacaoRulesForVersion, CURRENT_AVALIACAO_RULES,
} from './avaliacao-rules';

describe('regras das avaliações versionadas', () => {
  it('a versão corrente é a avaliacao-v2; a v1 continua registrada e provisória', () => {
    expect(CURRENT_AVALIACAO_RULES).toBe(AVALIACAO_RULES_V2);
    expect(avaliacaoRulesForVersion('avaliacao-v1')).toBe(AVALIACAO_RULES_V1);
    expect(avaliacaoRulesForVersion('avaliacao-v2')).toBe(AVALIACAO_RULES_V2);
    expect(Object.keys(AVALIACAO_RULES_BY_VERSION)).toEqual(['avaliacao-v1', 'avaliacao-v2']);
    expect(AVALIACAO_RULES_V1).toMatchObject({ version: 'avaliacao-v1', effectiveFrom: '2026-10-08', status: 'PROVISORIA' });
  });

  it.each(['toString', 'constructor', '__proto__', 'hasOwnProperty'])('chave herdada %s é versão desconhecida', (key) => {
    expect(() => avaliacaoRulesForVersion(key)).toThrow(`Versão de regras de avaliação desconhecida: ${key}`);
  });
```

Os demais testes do arquivo, inclusive o instantâneo da v1, não mudam.

Em `apps/api/src/clinical-rules/anamnese-rules.spec.ts`, acrescente logo depois de `'versão desconhecida é erro'`:

```ts
  it.each(['toString', 'constructor', '__proto__', 'hasOwnProperty'])('chave herdada %s é versão desconhecida', (key) => {
    expect(() => rulesForVersion(key)).toThrow(`Versão de regras de anamnese desconhecida: ${key}`);
  });
```

Em `apps/api/src/clinical-rules/premissas.spec.ts`, a lista esperada passa a terminar em `'PC-13', 'PC-14',`.

Crie `apps/api/src/clinical-rules/avaliacao-rules-v2.spec.ts`:

```ts
import {
  AVALIACAO_RULES_V1 as V1, AVALIACAO_RULES_V2 as V2, avaliacaoRulesForVersion, CURRENT_AVALIACAO_RULES,
} from './avaliacao-rules';

const c = (text: string, legacyControl: string) => ({ text, hint: null, legacyControl });
const plain = (text: string, hint: string | null = null) => ({ text, hint, legacyControl: null });
const OMRON = '*Fonte: Omron Healthcare';
const LEVELS = [plain('Baixo (-)'), plain('Normal (0)'), plain('Alto (+)'), plain('Muito Alto (++)')];

describe('avaliacao-v2', () => {
  it('é a versão corrente, provisória, desde 2026-10-11', () => {
    expect(CURRENT_AVALIACAO_RULES).toBe(V2);
    expect(avaliacaoRulesForVersion('avaliacao-v2')).toBe(V2);
    expect(V2).toMatchObject({ version: 'avaliacao-v2', effectiveFrom: '2026-10-11', status: 'PROVISORIA' });
  });

  it('mesmos cálculos da v1: só mudam versão, data, legendas dos métodos e o que é novo', () => {
    const { version, effectiveFrom, energy, recordLimits, manualClassifications, finalizeRequired, ...rest } = V2;
    const { version: v1Version, effectiveFrom: v1EffectiveFrom, energy: v1Energy, ...v1Rest } = V1;
    expect([version, effectiveFrom, v1Version, v1EffectiveFrom]).toEqual(['avaliacao-v2', '2026-10-11', 'avaliacao-v1', '2026-10-08']);
    expect(rest).toEqual(v1Rest);
    expect({ ...energy, methods: [] }).toEqual({ ...v1Energy, methods: [] });
    expect(energy.methods.map((m) => m.id)).toEqual(v1Energy.methods.map((m) => m.id));
    expect([recordLimits, manualClassifications, finalizeRequired].every(Boolean)).toBe(true);
  });

  it('métodos de TMB com as legendas do FrmNecessidadeEnergetica (conferidas no Excel)', () => {
    expect(V2.energy.methods).toEqual([
      { id: 'HARRIS_BENEDICT', label: 'Harris-Benedict (1919) - 15 aos 74 anos' },
      { id: 'OMS', label: 'OMS (Organização Mundial da Saúde) 1985' },
      { id: 'DRI_ADOLESCENTES', label: 'Comitê DRI - Crianças e Adolescentes com Peso Normal ou Sobrepeso/Obesidade.' },
      { id: 'DRI_ADULTOS', label: 'Comitê DRI - Adultos com Peso Normal ou Sobrepeso/Obesidade.' },
      { id: 'BIOIMPEDANCIA', label: 'Calculado pela Bioimpedância' },
    ]);
  });

  it('limites técnicos dos campos novos', () => {
    expect(V2.recordLimits).toEqual({
      bodyAgeYears: { min: 0, minInclusive: false, max: 150, maxInclusive: true, integer: false },
      segmentReferencePct: { min: 0, minInclusive: false, max: 1000, maxInclusive: true, integer: false },
      impedanceOhm: { min: 0, minInclusive: false, max: 2000, maxInclusive: true, integer: true },
    });
  });

  it('PC-14: o que cada protocolo exige para finalizar, além de protocolo e data', () => {
    expect(V2.finalizeRequired).toEqual({
      A: ['weightKg', 'heightCm', 'fatPct'],
      B: ['weightKg', 'heightCm', 'fatPct', 'informedBmi'],
      C: ['weightKg', 'heightCm', 'fatKg'],
    });
  });

  it('B: as quatro classificações com as legendas do FrmBioimpR (P4-10)', () => {
    expect(V2.manualClassifications.B).toEqual([
      {
        field: 'bmiClassification', label: 'IMC', source: '*Valores propostos pela OMS, a Organização Mundial da Saúde.',
        options: [
          plain('- (Abaixo do peso)', 'Menos de 18,5'), plain('0 (Normal)', '18,5 ou mais e menos de 25'),
          plain('+ (Sobrepeso)', '25 ou mais e menos de 30'), plain('++ (Obeso)', '30 ou mais'),
        ],
      },
      { field: 'fatClassification', label: 'Gordura corporal', source: OMRON, options: LEVELS },
      { field: 'skeletalMuscleClassification', label: 'Músculo esquelético', source: OMRON, options: LEVELS },
      {
        field: 'visceralFatClassification', label: 'Gordura visceral', source: OMRON,
        options: [plain('0 (Normal)', 'gordura visceral <= 9'), plain('(+) Alto', 'gordura visceral <= 14'), plain('(++) Muito Alto', 'gordura visceral >= 15')],
      },
    ]);
  });

  it('C: os 13 grupos do FrmBioimpC, cada controle no seu grupo (DIV-05, H-16)', () => {
    expect(V2.manualClassifications.C).toEqual([
      { field: 'bmiClassification', label: 'IMC', source: null, options: [c('Baixo peso', 'OptionButton13'), c('Saudável', 'OptionButton14'), c('Médio', 'OptionButton16'), c('Alto ou Muito Alto', 'OptionButton15')] },
      { field: 'fatClassification', label: 'Gordura corporal', source: null, options: [c('Baixo', 'OptionButton1'), c('Saudável', 'OptionButton2'), c('Excelente', 'OptionButton3'), c('Alto', 'OptionButton4')] },
      { field: 'musclePctClassification', label: 'Taxa muscular', source: null, options: [c('Baixo', 'OptionButton17'), c('Médio', 'OptionButton18'), c('Excelente', 'OptionButton20')] },
      { field: 'muscleKgClassification', label: 'Massa muscular', source: null, options: [c('Baixo', 'OptionButton25'), c('Saudável', 'OptionButton26'), c('Excelente', 'OptionButton28'), c('Alto', 'Opt6Alto')] },
      { field: 'skeletalMuscleClassification', label: 'Músculo esquelético', source: null, options: [c('Baixo', 'OptionButton5'), c('Saudável', 'OptionButton6'), c('Excelente', 'OptionButton8'), c('Alto', 'Opt7Alto')] },
      { field: 'visceralFatClassification', label: 'Gordura visceral', source: null, options: [c('Baixo Risco', 'OptionButton9'), c('Médio Risco', 'OptionButton10'), c('Alto Risco', 'OptionButton12')] },
      { field: 'subcutaneousFatClassification', label: 'Gordura subcutânea', source: null, options: [c('Baixo', 'OptionButton34'), c('Médio', 'OptionButton35'), c('Sobrepeso', 'OptionButton33')] },
      { field: 'waterClassification', label: 'Água corporal', source: null, options: [c('Baixo', 'OptionButton22'), c('Saudável', 'OptionButton23'), c('Excelente', 'Opt5Excel'), c('Alto', 'OptionButton24')] },
      { field: 'proteinClassification', label: 'Proteína', source: null, options: [c('Baixo', 'OptionButton40'), c('Saudável', 'OptionButton41'), c('Excelente', 'OptionButton39'), c('Alto', 'Opt4Alto')] },
      { field: 'boneClassification', label: 'Massa óssea', source: null, options: [c('Baixo', 'OptionButton37'), c('Saudável', 'OptionButton38'), c('Excelente', 'OptionButton36'), c('Alto', 'Opt3Alto')] },
      { field: 'bodyAgeClassification', label: 'Idade corporal', source: null, options: [c('Excelente', 'OptionButton44'), c('Sobrepeso', 'OptionButton45')] },
      { field: 'bmrClassification', label: 'TMB', source: null, options: [c('Baixo', 'OptionButton42'), c('Excelente', 'OptionButton43')] },
      { field: 'totalWeightClassification', label: 'Peso total', source: null, options: [c('Baixo', 'OptionButton29'), c('Saudável', 'OptionButton30'), c('Excelente', 'OptionButton31'), c('Alto', 'OptionButton32')] },
    ]);
  });

  it('C: as 45 opções do código, uma vez cada, inclusive Opt3Alto e as 4 sem ramo no legado', () => {
    const controls = V2.manualClassifications.C.flatMap((group) => group.options.map((option) => option.legacyControl as string));
    expect(controls).toHaveLength(45);
    expect(new Set(controls).size).toBe(45);
    expect(controls.filter((name) => name.startsWith('Opt') && !name.startsWith('OptionButton')).sort()).toEqual([
      'Opt3Alto', 'Opt4Alto', 'Opt5Excel', 'Opt6Alto', 'Opt7Alto',
    ]);
    expect(controls.filter((name) => name.startsWith('OptionButton')).map((name) => Number(name.slice(12))).sort((a, b) => a - b)).toEqual([
      1, 2, 3, 4, 5, 6, 8, 9, 10, 12, 13, 14, 15, 16, 17, 18, 20, 22, 23, 24, 25, 26, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45,
    ]);
  });

  it('legendas únicas em cada grupo e grupos sem repetição', () => {
    for (const groups of [V2.manualClassifications.B, V2.manualClassifications.C]) {
      expect(new Set(groups.map((g) => g.field)).size).toBe(groups.length);
      for (const group of groups) expect(new Set(group.options.map((o) => o.text)).size).toBe(group.options.length);
    }
  });

  it('a versão publicada não muda em execução', () => {
    expect(Object.isFrozen(V2)).toBe(true);
    expect(Object.isFrozen(V2.manualClassifications.C[9].options[3])).toBe(true);
    expect(Object.isFrozen(V2.recordLimits.impedanceOhm)).toBe(true);
    expect(Object.isFrozen(V2.finalizeRequired.B)).toBe(true);
    expect(Object.isFrozen(V2.energy.methods[0])).toBe(true);
  });

  // Instantâneo da versão publicada. Se falhar, NÃO rode jest -u: crie a avaliacao-v3.
  it('avaliacao-v2: conteúdo publicado', () => {
    expect(JSON.stringify(V2)).toMatchSnapshot();
  });
});
```

Em `apps/api/test/assessments-calculation.e2e-spec.ts`:

- no teste `'prévia da A com a idade na data da avaliação, sem gravar nem auditar'`, troque `rulesVersion: 'avaliacao-v1'` por `rulesVersion: 'avaliacao-v2'`;
- troque o teste `'regras para o formulário do Plano 4'` por:

```ts
  it('regras para o formulário (avaliacao-v2): métodos, limites, opções e finalização, sem o controle do legado', async () => {
    const { agent } = await loginAs(app, 'ASSISTENTE');

    const res = await agent.get('/api/assessments/rules');

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({
      version: 'avaliacao-v2', status: 'PROVISORIA', protocols: ['A', 'B', 'C'], defaultActivityFactor: 1, suggestedTargetBmi: 22.5,
    });
    expect(res.body.energyMethods.map((m: { id: string }) => m.id)).toEqual(['HARRIS_BENEDICT', 'OMS', 'DRI_ADOLESCENTES', 'DRI_ADULTOS', 'BIOIMPEDANCIA']);
    expect(res.body.energyMethods[4]).toEqual({ id: 'BIOIMPEDANCIA', label: 'Calculado pela Bioimpedância' });
    expect(res.body.activityFactors).toHaveLength(21);
    expect(res.body.limits.bodyScore).toEqual({ min: 0, minInclusive: true, max: 999, maxInclusive: true, integer: true });
    expect(res.body.limits.visceralFatLevel.max).toBeNull();
    expect(res.body.limits.impedanceOhm).toEqual({ min: 0, minInclusive: false, max: 2000, maxInclusive: true, integer: true });
    expect(res.body.manualClassifications.B.map((g: { field: string }) => g.field)).toEqual([
      'bmiClassification', 'fatClassification', 'skeletalMuscleClassification', 'visceralFatClassification',
    ]);
    expect(res.body.manualClassifications.B[0].options[0]).toEqual({ text: '- (Abaixo do peso)', hint: 'Menos de 18,5' });
    expect(res.body.manualClassifications.C).toHaveLength(13);
    expect(res.body.manualClassifications.C[9]).toEqual({
      field: 'boneClassification', label: 'Massa óssea', source: null,
      options: [{ text: 'Baixo', hint: null }, { text: 'Saudável', hint: null }, { text: 'Excelente', hint: null }, { text: 'Alto', hint: null }],
    });
    expect(res.body.finalizeRequired).toEqual({ A: ['weightKg', 'heightCm', 'fatPct'], B: ['weightKg', 'heightCm', 'fatPct', 'informedBmi'], C: ['weightKg', 'heightCm', 'fatKg'] });
    expect(JSON.stringify(res.body)).not.toContain('legacyControl');
  });
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `Set-Location apps/api; npx jest src/clinical-rules test/assessments-calculation --runInBand; Set-Location ../..`
Expected: FAIL. `AVALIACAO_RULES_V2` não existe. As chaves herdadas `toString`, `constructor`, `__proto__` e `hasOwnProperty` não lançam erro nas duas funções. `premissas.spec.ts` não encontra PC-14. A rota de regras ainda responde `avaliacao-v1`.

- [ ] **Step 3: Implementar**

`apps/api/src/clinical-rules/anamnese-rules.ts`, só a função:

```ts
/** Só versões registradas: chaves herdadas do Object (toString, constructor, __proto__) não são versão. */
export function rulesForVersion(version: string): AnamneseRules {
  if (!Object.hasOwn(ANAMNESE_RULES_BY_VERSION, version)) throw new Error(`Versão de regras de anamnese desconhecida: ${version}`);
  return ANAMNESE_RULES_BY_VERSION[version];
}
```

`apps/api/src/clinical-rules/avaliacao-rules.ts`:

1. Logo depois de `export type LimitKey = ...;`, acrescente os tipos de "Produces" (de `RecordLimitKey` até `AvaliacaoRecordRules`), com estes comentários:
   - `RecordLimitKey`: `/** Limites dos campos que só a gravação usa (desde a avaliacao-v2; não entram no cálculo). */`
   - `ClassificationField`: `/** Classificações manuais, pela coluna da aba Avaliações (dicionario-dados.md). */`
   - `ClassificationOption`: `/** Legenda exata do legado; legacyControl liga a opção ao controle do FrmBioimpC (rastreabilidade, DIV-05). */`
   - `FinalizeRequiredField`: `/** Campos que a P4-2 exige para finalizar, além de protocolo e data. */`
   - `AvaliacaoRecordRules`: `/** Regras da gravação (Plano 4), desde a avaliacao-v2: as de cálculo e mais estas. */`
2. Não toque em `AVALIACAO_RULES_V1`. Depois dela, substitua `AVALIACAO_RULES_BY_VERSION`, `CURRENT_AVALIACAO_RULES` e `avaliacaoRulesForVersion` por:

```ts
const option = (text: string, legacyControl: string | null = null, hint: string | null = null): ClassificationOption => ({ text, hint, legacyControl });
const OMRON = '*Fonte: Omron Healthcare';
const OMRON_LEVELS: readonly ClassificationOption[] = ['Baixo (-)', 'Normal (0)', 'Alto (+)', 'Muito Alto (++)'].map((text) => option(text));

/**
 * Versão publicada em 2026-10-11 (Plano 4). Os cálculos são os da avaliacao-v1. Mudam só as legendas dos métodos de TMB
 * (as do FrmNecessidadeEnergetica, conferidas no Excel). Entram os limites dos campos novos, as opções das classificações
 * manuais da B e da C e os critérios de finalização. NUNCA altere esta constante: para mudar, crie a avaliacao-v3.
 */
export const AVALIACAO_RULES_V2: AvaliacaoRecordRules = deepFreeze({
  ...AVALIACAO_RULES_V1,
  version: 'avaliacao-v2',
  effectiveFrom: '2026-10-11',
  energy: {
    ...AVALIACAO_RULES_V1.energy,
    methods: [
      { id: 'HARRIS_BENEDICT', label: 'Harris-Benedict (1919) - 15 aos 74 anos' },
      { id: 'OMS', label: 'OMS (Organização Mundial da Saúde) 1985' },
      { id: 'DRI_ADOLESCENTES', label: 'Comitê DRI - Crianças e Adolescentes com Peso Normal ou Sobrepeso/Obesidade.' },
      { id: 'DRI_ADULTOS', label: 'Comitê DRI - Adultos com Peso Normal ou Sobrepeso/Obesidade.' },
      { id: 'BIOIMPEDANCIA', label: 'Calculado pela Bioimpedância' },
    ],
  },
  recordLimits: {
    bodyAgeYears: range(0, false, 150, true),
    segmentReferencePct: range(0, false, 1000, true),
    impedanceOhm: range(0, false, 2000, true, true),
  },
  manualClassifications: {
    // FrmBioimpR (conferido no Excel em 2026-10-11): o Aplicar copia o texto da opção para o FrmAvaliacaoB (P4-10).
    B: [
      {
        field: 'bmiClassification',
        label: 'IMC',
        source: '*Valores propostos pela OMS, a Organização Mundial da Saúde.',
        options: [
          option('- (Abaixo do peso)', null, 'Menos de 18,5'),
          option('0 (Normal)', null, '18,5 ou mais e menos de 25'),
          option('+ (Sobrepeso)', null, '25 ou mais e menos de 30'),
          option('++ (Obeso)', null, '30 ou mais'),
        ],
      },
      { field: 'fatClassification', label: 'Gordura corporal', source: OMRON, options: OMRON_LEVELS },
      { field: 'skeletalMuscleClassification', label: 'Músculo esquelético', source: OMRON, options: OMRON_LEVELS },
      {
        field: 'visceralFatClassification',
        label: 'Gordura visceral',
        source: OMRON,
        options: [
          option('0 (Normal)', null, 'gordura visceral <= 9'),
          option('(+) Alto', null, 'gordura visceral <= 14'),
          option('(++) Muito Alto', null, 'gordura visceral >= 15'),
        ],
      },
    ],
    // FrmBioimpC.CommandButton179_Click (DIV-05): todas as opções de cada grupo, inclusive Opt4Alto, Opt5Excel, Opt6Alto e
    // Opt7Alto, que o legado não transfere. Grupos na ordem das colunas da aba Avaliações; opções do menor para o maior.
    C: [
      { field: 'bmiClassification', label: 'IMC', source: null, options: [option('Baixo peso', 'OptionButton13'), option('Saudável', 'OptionButton14'), option('Médio', 'OptionButton16'), option('Alto ou Muito Alto', 'OptionButton15')] },
      { field: 'fatClassification', label: 'Gordura corporal', source: null, options: [option('Baixo', 'OptionButton1'), option('Saudável', 'OptionButton2'), option('Excelente', 'OptionButton3'), option('Alto', 'OptionButton4')] },
      { field: 'musclePctClassification', label: 'Taxa muscular', source: null, options: [option('Baixo', 'OptionButton17'), option('Médio', 'OptionButton18'), option('Excelente', 'OptionButton20')] },
      { field: 'muscleKgClassification', label: 'Massa muscular', source: null, options: [option('Baixo', 'OptionButton25'), option('Saudável', 'OptionButton26'), option('Excelente', 'OptionButton28'), option('Alto', 'Opt6Alto')] },
      { field: 'skeletalMuscleClassification', label: 'Músculo esquelético', source: null, options: [option('Baixo', 'OptionButton5'), option('Saudável', 'OptionButton6'), option('Excelente', 'OptionButton8'), option('Alto', 'Opt7Alto')] },
      { field: 'visceralFatClassification', label: 'Gordura visceral', source: null, options: [option('Baixo Risco', 'OptionButton9'), option('Médio Risco', 'OptionButton10'), option('Alto Risco', 'OptionButton12')] },
      { field: 'subcutaneousFatClassification', label: 'Gordura subcutânea', source: null, options: [option('Baixo', 'OptionButton34'), option('Médio', 'OptionButton35'), option('Sobrepeso', 'OptionButton33')] },
      { field: 'waterClassification', label: 'Água corporal', source: null, options: [option('Baixo', 'OptionButton22'), option('Saudável', 'OptionButton23'), option('Excelente', 'Opt5Excel'), option('Alto', 'OptionButton24')] },
      { field: 'proteinClassification', label: 'Proteína', source: null, options: [option('Baixo', 'OptionButton40'), option('Saudável', 'OptionButton41'), option('Excelente', 'OptionButton39'), option('Alto', 'Opt4Alto')] },
      { field: 'boneClassification', label: 'Massa óssea', source: null, options: [option('Baixo', 'OptionButton37'), option('Saudável', 'OptionButton38'), option('Excelente', 'OptionButton36'), option('Alto', 'Opt3Alto')] },
      { field: 'bodyAgeClassification', label: 'Idade corporal', source: null, options: [option('Excelente', 'OptionButton44'), option('Sobrepeso', 'OptionButton45')] },
      { field: 'bmrClassification', label: 'TMB', source: null, options: [option('Baixo', 'OptionButton42'), option('Excelente', 'OptionButton43')] },
      { field: 'totalWeightClassification', label: 'Peso total', source: null, options: [option('Baixo', 'OptionButton29'), option('Saudável', 'OptionButton30'), option('Excelente', 'OptionButton31'), option('Alto', 'OptionButton32')] },
    ],
  },
  // TODO(PREMISSA-CLINICA PC-14): validar com o médico responsável os critérios mínimos para finalizar a avaliação (P4-2).
  finalizeRequired: {
    A: ['weightKg', 'heightCm', 'fatPct'],
    B: ['weightKg', 'heightCm', 'fatPct', 'informedBmi'],
    C: ['weightKg', 'heightCm', 'fatKg'],
  },
});

export const AVALIACAO_RULES_BY_VERSION: Readonly<Record<string, AvaliacaoRules>> = Object.freeze({
  [AVALIACAO_RULES_V1.version]: AVALIACAO_RULES_V1,
  [AVALIACAO_RULES_V2.version]: AVALIACAO_RULES_V2,
});

/** Versão usada na prévia e em toda gravação. */
export const CURRENT_AVALIACAO_RULES: AvaliacaoRecordRules = AVALIACAO_RULES_V2;

/** Só versões registradas: chaves herdadas do Object (toString, constructor, __proto__) não são versão. */
export function avaliacaoRulesForVersion(version: string): AvaliacaoRules {
  if (!Object.hasOwn(AVALIACAO_RULES_BY_VERSION, version)) throw new Error(`Versão de regras de avaliação desconhecida: ${version}`);
  return AVALIACAO_RULES_BY_VERSION[version];
}
```

`apps/api/src/assessments/assessment-rules.view.ts` (arquivo inteiro):

```ts
import type {
  AssessmentProtocol, AvaliacaoRecordRules, ClassificationField, ClassificationProtocol, EnergyMethod, FinalizeRequiredField,
  LimitKey, Range, RecordLimitKey,
} from '../clinical-rules/avaliacao-rules';

export interface ClassificationGroupView {
  field: ClassificationField;
  label: string;
  source: string | null;
  options: { text: string; hint: string | null }[];
}

/** O que o formulário da avaliação precisa das regras correntes (o controle do legado fica só na API). */
export interface AssessmentRulesView {
  version: string;
  status: AvaliacaoRecordRules['status'];
  protocols: AssessmentProtocol[];
  energyMethods: { id: EnergyMethod; label: string }[];
  activityFactors: number[];
  defaultActivityFactor: number;
  suggestedTargetBmi: number;
  limits: Record<LimitKey | RecordLimitKey, Range>;
  manualClassifications: Record<ClassificationProtocol, ClassificationGroupView[]>;
  finalizeRequired: Record<AssessmentProtocol, FinalizeRequiredField[]>;
}

export function toAssessmentRulesView(rules: AvaliacaoRecordRules): AssessmentRulesView {
  const groups = (protocol: ClassificationProtocol): ClassificationGroupView[] =>
    rules.manualClassifications[protocol].map(({ field, label, source, options }) => ({
      field, label, source, options: options.map(({ text, hint }) => ({ text, hint })),
    }));
  return {
    version: rules.version,
    status: rules.status,
    protocols: ['A', 'B', 'C'],
    energyMethods: rules.energy.methods.map(({ id, label }) => ({ id, label })),
    activityFactors: [...rules.energy.activityFactors],
    defaultActivityFactor: rules.energy.defaultActivityFactor,
    suggestedTargetBmi: rules.targets.suggestedBmi,
    limits: Object.fromEntries(
      Object.entries({ ...rules.limits, ...rules.recordLimits }).map(([key, range]) => [key, { ...range }]),
    ) as Record<LimitKey | RecordLimitKey, Range>,
    manualClassifications: { B: groups('B'), C: groups('C') },
    finalizeRequired: { A: [...rules.finalizeRequired.A], B: [...rules.finalizeRequired.B], C: [...rules.finalizeRequired.C] },
  };
}
```

`docs/premissas-clinicas.md`, nova linha ao fim da tabela:

```
| PC-14 | Critérios mínimos para finalizar a avaliação: todos os protocolos exigem protocolo, data, peso e altura e nenhum erro de campo (inclusive a TMB calculada não válida); A e B exigem gordura (%); C exige gordura (kg); B exige o IMC do aparelho. O restante é opcional. | Decisão da pessoa (P4-2, Plano 4) | 2026-10-11 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (finalizeRequired) |
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api`
Expected: PASS com 47 suítes e 2 instantâneos. O Jest escreve `__snapshots__/avaliacao-rules-v2.spec.ts.snap` ("1 snapshot written"). O instantâneo da v1 continua passando sem ser reescrito. Confira com `git status --short apps/api/src/clinical-rules/__snapshots__`: só aparece o arquivo novo `avaliacao-rules-v2.spec.ts.snap`, e `avaliacao-rules.spec.ts.snap` não está modificado.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/clinical-rules/avaliacao-rules.ts apps/api/src/clinical-rules/anamnese-rules.ts apps/api/src/clinical-rules/avaliacao-rules.spec.ts apps/api/src/clinical-rules/anamnese-rules.spec.ts apps/api/src/clinical-rules/avaliacao-rules-v2.spec.ts apps/api/src/clinical-rules/__snapshots__/avaliacao-rules-v2.spec.ts.snap apps/api/src/clinical-rules/premissas.spec.ts apps/api/src/assessments/assessment-rules.view.ts apps/api/test/assessments-calculation.e2e-spec.ts docs/premissas-clinicas.md
git commit -m "feat(api): avaliacao-v2 com opções das classificações, limites novos e finalização; versões só por chave própria"
```

---

### Task 2: Validação — circunferências, textos, classificações e DTOs

**Files:**
- Modify: `apps/api/src/assessments/assessment-normalize.ts`, `apps/api/src/assessments/assessment-normalize.spec.ts`
- Create: `apps/api/src/assessments/dto/assessment.dto.ts`, `apps/api/src/assessments/dto/assessment.dto.spec.ts`
- Delete: `apps/api/src/assessments/dto/assessment-calculation.dto.ts` (`git rm`)
- Modify: `apps/api/src/assessments/assessments.controller.ts` (a prévia passa a usar `AssessmentBodyDto`)

**Interfaces:**
- Consumes: `CURRENT_AVALIACAO_RULES`, `AvaliacaoRecordRules`, `ClassificationField` e `RecordLimitKey` (Task 1); `cleanText` e `cleanMultiline` de `common/text.ts`.
- Produces, em `assessment-normalize.ts`:
  - `NUMERIC_FIELDS`, agora com `limit: LimitKey | RecordLimitKey` e as 12 circunferências novas;
  - `TEXT_FIELDS = ['observation', 'conclusion']` e `type TextField`;
  - `CLASSIFICATION_FIELDS`, com as 13 colunas;
  - `AssessmentInput`, que ganha `Partial<Record<TextField, string | null>>` e `Partial<Record<ClassificationField, string | null>>`;
  - `AssessmentValues`, que ganha `texts: Record<TextField, string | null>` e `classifications: Record<ClassificationField, string | null>`;
  - `normalizeAssessment(input, patient, rules: AvaliacaoRecordRules, today)`;
  - `toCalculationInput(values, sex)`, que só passa os 20 campos das regras.
- Produces, em `dto/assessment.dto.ts`:
  - `AssessmentFieldsDto`, com todos os campos menos o protocolo;
  - `AssessmentBodyDto`, que estende `AssessmentFieldsDto` com `protocol`;
  - `UpdateAssessmentDto`, que estende `AssessmentFieldsDto` com `version: number`.

  Cada task seguinte que acrescentar campo a `NUMERIC_FIELDS` acrescenta também a linha do DTO; o teste do DTO garante isso.

- [ ] **Step 1: Escrever os testes**

Em `apps/api/src/assessments/assessment-normalize.spec.ts`, troque o import das regras por `import { CURRENT_AVALIACAO_RULES as RULES, type Range } from '../clinical-rules/avaliacao-rules';` e o import da normalização por `import { CLASSIFICATION_FIELDS, NUMERIC_FIELDS, normalizeAssessment, rangeMessage, toCalculationInput, type AssessmentInput } from './assessment-normalize';`. Os testes existentes ficam como estão: os limites da v2 são os da v1. Acrescente ao fim:

```ts
describe('campos comuns da gravação (P4-12)', () => {
  const CIRCUMFERENCES = [
    ['neckCm', 'A circunferência do pescoço'],
    ['shouldersCm', 'A circunferência dos ombros'],
    ['chestCm', 'A circunferência do peitoral'],
    ['armRightCm', 'A circunferência do braço direito'],
    ['armLeftCm', 'A circunferência do braço esquerdo'],
    ['forearmRightCm', 'A circunferência do antebraço direito'],
    ['forearmLeftCm', 'A circunferência do antebraço esquerdo'],
    ['abdomenCm', 'A circunferência do abdômen'],
    ['thighRightCm', 'A circunferência da coxa direita'],
    ['thighLeftCm', 'A circunferência da coxa esquerda'],
    ['calfRightCm', 'A circunferência da panturrilha direita'],
    ['calfLeftCm', 'A circunferência da panturrilha esquerda'],
  ] as const;

  it.each(CIRCUMFERENCES)('%s: aceita 0,01 e 300 nos três protocolos; recusa 0 e 300,01', (field, label) => {
    for (const protocol of ['A', 'B', 'C']) {
      expect(run({ protocol, assessedOn: '2026-05-09', [field]: 0.01 }).errors).toEqual({});
      expect(run({ protocol, assessedOn: '2026-05-09', [field]: 300 }).values.numbers[field]).toBe(300);
    }
    const message = `${label} deve ser maior que 0 e no máximo 300 cm, com até 2 casas decimais.`;
    expect(run({ protocol: 'A', assessedOn: '2026-05-09', [field]: 0 }).errors).toEqual({ [field]: message });
    expect(run({ protocol: 'C', assessedOn: '2026-05-09', [field]: 300.01 }).errors).toEqual({ [field]: message });
  });

  it('circunferência com 2 casas cujo produto por 100 não é exato', () => {
    const { values, errors } = run({ protocol: 'B', assessedOn: '2026-05-09', neckCm: 40.02, calfLeftCm: 0.07 });
    expect(errors).toEqual({});
    expect(values.numbers).toMatchObject({ neckCm: 40.02, calfLeftCm: 0.07 });
  });

  it('observação e conclusão: qualquer protocolo; sem espaços nas pontas; CRLF vira LF; vazio é null', () => {
    const { values, errors } = run({ protocol: 'C', assessedOn: '2026-05-09', observation: '  linha 1\r\nlinha 2  ', conclusion: '   ' });
    expect(errors).toEqual({});
    expect(values.texts).toEqual({ observation: 'linha 1\nlinha 2', conclusion: null });
    expect(run({ protocol: 'A', assessedOn: '2026-05-09' }).values.texts).toEqual({ observation: null, conclusion: null });
  });
});

describe('classificações manuais (CL-08, DIV-05, P4-4, P4-10)', () => {
  const B_OPTIONS = [
    ['bmiClassification', '- (Abaixo do peso)'], ['bmiClassification', '0 (Normal)'], ['bmiClassification', '+ (Sobrepeso)'], ['bmiClassification', '++ (Obeso)'],
    ['fatClassification', 'Baixo (-)'], ['fatClassification', 'Normal (0)'], ['fatClassification', 'Alto (+)'], ['fatClassification', 'Muito Alto (++)'],
    ['skeletalMuscleClassification', 'Baixo (-)'], ['skeletalMuscleClassification', 'Normal (0)'], ['skeletalMuscleClassification', 'Alto (+)'], ['skeletalMuscleClassification', 'Muito Alto (++)'],
    ['visceralFatClassification', '0 (Normal)'], ['visceralFatClassification', '(+) Alto'], ['visceralFatClassification', '(++) Muito Alto'],
  ] as const;
  // Uma opção de cada grupo da C, com as 5 que o legado não transferia (★) ou que só a Opt3Alto tinha.
  const C_OPTIONS = [
    ['bmiClassification', 'Alto ou Muito Alto'], ['fatClassification', 'Excelente'], ['musclePctClassification', 'Médio'],
    ['muscleKgClassification', 'Alto'], ['skeletalMuscleClassification', 'Alto'], ['visceralFatClassification', 'Médio Risco'],
    ['subcutaneousFatClassification', 'Sobrepeso'], ['waterClassification', 'Excelente'], ['proteinClassification', 'Alto'],
    ['boneClassification', 'Alto'], ['bodyAgeClassification', 'Sobrepeso'], ['bmrClassification', 'Baixo'], ['totalWeightClassification', 'Excelente'],
  ] as const;

  it.each(B_OPTIONS)('B aceita %s = %s', (field, text) => {
    const { values, errors } = run({ protocol: 'B', assessedOn: '2026-05-09', [field]: text });
    expect(errors).toEqual({});
    expect(values.classifications[field]).toBe(text);
  });

  it.each(C_OPTIONS)('C aceita %s = %s', (field, text) => {
    const { values, errors } = run({ protocol: 'C', assessedOn: '2026-05-09', [field]: text });
    expect(errors).toEqual({});
    expect(values.classifications[field]).toBe(text);
  });

  it('A não tem classificação manual: qualquer uma é recusada no campo', () => {
    const { values, errors } = run({ protocol: 'A', assessedOn: '2026-05-09', bmiClassification: 'Saudável', waterClassification: 'Alto' });
    expect(errors).toEqual({
      bmiClassification: 'Este campo não se aplica ao protocolo A.',
      waterClassification: 'Este campo não se aplica ao protocolo A.',
    });
    expect(values.classifications).toMatchObject({ bmiClassification: null, waterClassification: null });
  });

  it('grupo só da C na B: não se aplica', () => {
    expect(run({ protocol: 'B', assessedOn: '2026-05-09', waterClassification: 'Alto', bodyAgeClassification: 'Excelente' }).errors).toEqual({
      waterClassification: 'Este campo não se aplica ao protocolo B.',
      bodyAgeClassification: 'Este campo não se aplica ao protocolo B.',
    });
  });

  it('legenda da C num grupo da B e da B num grupo da C: opção desconhecida', () => {
    expect(run({ protocol: 'B', assessedOn: '2026-05-09', bmiClassification: 'Saudável' }).errors).toEqual({ bmiClassification: 'Opção de classificação desconhecida.' });
    expect(run({ protocol: 'C', assessedOn: '2026-05-09', visceralFatClassification: '0 (Normal)' }).errors).toEqual({
      visceralFatClassification: 'Opção de classificação desconhecida.',
    });
  });

  it('maiúsculas diferentes são outra opção; espaços nas pontas são ignorados; vazio é null', () => {
    expect(run({ protocol: 'C', assessedOn: '2026-05-09', boneClassification: 'alto' }).errors).toEqual({ boneClassification: 'Opção de classificação desconhecida.' });
    const { values, errors } = run({ protocol: 'C', assessedOn: '2026-05-09', boneClassification: '  Alto ', waterClassification: '  ' });
    expect(errors).toEqual({});
    expect(values.classifications).toMatchObject({ boneClassification: 'Alto', waterClassification: null });
  });

  it('protocolo inválido com classificação e números: só o erro do protocolo; a classificação fica vazia', () => {
    const { values, errors } = run({ protocol: 'D', assessedOn: '2026-05-09', boneClassification: 'Alto', neckCm: 30 });
    expect(errors).toEqual({ protocol: 'Escolha o protocolo A, B ou C.' });
    expect(values.classifications.boneClassification).toBeNull();
    expect(values.numbers.neckCm).toBe(30);
  });

  it('as 13 colunas são exatamente os grupos da C; os 4 da B estão entre elas', () => {
    expect([...CLASSIFICATION_FIELDS].sort()).toEqual(RULES.manualClassifications.C.map((g) => g.field).sort());
    expect(RULES.manualClassifications.B.every((g) => (CLASSIFICATION_FIELDS as readonly string[]).includes(g.field))).toBe(true);
  });
});

describe('toCalculationInput — só os campos das regras', () => {
  it('circunferências, textos e classificações não entram no cálculo', () => {
    const { values } = run({ protocol: 'B', assessedOn: '2026-05-09', weightKg: 80, neckCm: 35, observation: 'x', bmiClassification: '0 (Normal)' });
    expect(Object.keys(toCalculationInput(values, 'FEMININO') ?? {}).sort()).toEqual([
      'activityFactor', 'ageYears', 'boneKg', 'energyMethod', 'fatKg', 'fatPct', 'femurDiameterMm', 'heightCm', 'hipCm', 'informedBmi',
      'informedBmrKcal', 'muscleKg', 'proteinKg', 'protocol', 'sex', 'skeletalMuscleKg', 'skeletalMusclePct', 'targetBmi', 'targetFatPct',
      'visceralFatLevel', 'waistCm', 'waterKg', 'weightKg', 'wristDiameterMm',
    ]);
  });

  it('NUMERIC_FIELDS não repete campo', () => {
    expect(new Set(NUMERIC_FIELDS.map((f) => f.field)).size).toBe(NUMERIC_FIELDS.length);
  });
});
```

Crie `apps/api/src/assessments/dto/assessment.dto.spec.ts`:

```ts
import { plainToInstance } from 'class-transformer';
import { validateSync } from 'class-validator';
import { CLASSIFICATION_FIELDS, NUMERIC_FIELDS, TEXT_FIELDS } from '../assessment-normalize';
import { AssessmentBodyDto, UpdateAssessmentDto } from './assessment.dto';

/** As mesmas opções do ValidationPipe global (app.setup.ts). */
const OPTIONS = { whitelist: true, forbidNonWhitelisted: true };
const errorsOf = (cls: new () => object, body: Record<string, unknown>) => validateSync(plainToInstance(cls, body), OPTIONS);

describe('DTOs da avaliação', () => {
  it.each(NUMERIC_FIELDS.map((f) => f.field))('número %s é aceito no corpo e na atualização', (field) => {
    expect(errorsOf(AssessmentBodyDto, { [field]: 1 })).toEqual([]);
    expect(errorsOf(UpdateAssessmentDto, { [field]: null, version: 1 })).toEqual([]);
    expect(errorsOf(AssessmentBodyDto, { [field]: 'um' })).toHaveLength(1);
  });

  it.each([...CLASSIFICATION_FIELDS, ...TEXT_FIELDS])('texto %s é aceito', (field) => {
    expect(errorsOf(AssessmentBodyDto, { [field]: 'Alto' })).toEqual([]);
    expect(errorsOf(AssessmentBodyDto, { [field]: 10 })).toHaveLength(1);
  });

  it('classificação até 40 caracteres; observação e conclusão até 4000', () => {
    expect(errorsOf(AssessmentBodyDto, { boneClassification: 'x'.repeat(40) })).toEqual([]);
    expect(errorsOf(AssessmentBodyDto, { boneClassification: 'x'.repeat(41) })).toHaveLength(1);
    expect(errorsOf(AssessmentBodyDto, { conclusion: 'x'.repeat(4000) })).toEqual([]);
    expect(errorsOf(AssessmentBodyDto, { conclusion: 'x'.repeat(4001) })).toHaveLength(1);
  });

  it('campo desconhecido é recusado', () => {
    expect(errorsOf(AssessmentBodyDto, { pontuacao: 10 })).toHaveLength(1);
  });

  it('atualização: exige a versão e não aceita o protocolo (P4-3)', () => {
    expect(errorsOf(UpdateAssessmentDto, { version: 1 })).toEqual([]);
    expect(errorsOf(UpdateAssessmentDto, {})).toHaveLength(1);
    expect(errorsOf(UpdateAssessmentDto, { version: 0 })).toHaveLength(1);
    expect(errorsOf(UpdateAssessmentDto, { version: 1, protocol: 'A' })).toHaveLength(1);
    expect(errorsOf(AssessmentBodyDto, { protocol: 'AB' })).toHaveLength(1);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `Set-Location apps/api; npx jest src/assessments --runInBand; Set-Location ../..`
Expected: FAIL. `dto/assessment.dto` não existe; `CLASSIFICATION_FIELDS`, `values.texts` e `values.classifications` não existem; as circunferências novas não são campos.

- [ ] **Step 3: Implementar**

`apps/api/src/assessments/assessment-normalize.ts`:

1. Imports: `AvaliacaoRecordRules`, `ClassificationField` e `RecordLimitKey` de `../clinical-rules/avaliacao-rules` (sai `AvaliacaoRules`) e `cleanMultiline` de `../common/text`.
2. Em `NUMERIC_FIELDS`, o `satisfies` passa a `limit: LimitKey | RecordLimitKey`. Acrescente ao fim da lista:

```ts
  // Circunferências: os três formulários gravam P a AC da aba Avaliações (P4-12).
  { field: 'neckCm', label: 'A circunferência do pescoço', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'shouldersCm', label: 'A circunferência dos ombros', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'chestCm', label: 'A circunferência do peitoral', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'armRightCm', label: 'A circunferência do braço direito', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'armLeftCm', label: 'A circunferência do braço esquerdo', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'forearmRightCm', label: 'A circunferência do antebraço direito', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'forearmLeftCm', label: 'A circunferência do antebraço esquerdo', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'abdomenCm', label: 'A circunferência do abdômen', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'thighRightCm', label: 'A circunferência da coxa direita', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'thighLeftCm', label: 'A circunferência da coxa esquerda', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'calfRightCm', label: 'A circunferência da panturrilha direita', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'calfLeftCm', label: 'A circunferência da panturrilha esquerda', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
```

3. Depois de `export type NumericField = ...`, acrescente:

```ts
/** Campos que entram no cálculo (AssessmentCalculationInput); os demais só são gravados. */
const CALCULATION_FIELDS = [
  'weightKg', 'heightCm', 'waistCm', 'hipCm', 'fatPct', 'wristDiameterMm', 'femurDiameterMm', 'informedBmi', 'skeletalMusclePct',
  'visceralFatLevel', 'fatKg', 'boneKg', 'proteinKg', 'waterKg', 'muscleKg', 'skeletalMuscleKg', 'targetFatPct', 'targetBmi',
  'informedBmrKcal', 'activityFactor',
] as const satisfies readonly NumericField[];

/** Textos livres de todos os protocolos (colunas N e CH). */
export const TEXT_FIELDS = ['observation', 'conclusion'] as const;
export type TextField = (typeof TEXT_FIELDS)[number];

/** As 13 classificações manuais; cada protocolo só aceita as dos seus grupos (avaliacao-rules.manualClassifications). */
export const CLASSIFICATION_FIELDS = [
  'bmiClassification', 'fatClassification', 'musclePctClassification', 'muscleKgClassification', 'skeletalMuscleClassification',
  'visceralFatClassification', 'subcutaneousFatClassification', 'waterClassification', 'proteinClassification', 'boneClassification',
  'bodyAgeClassification', 'bmrClassification', 'totalWeightClassification',
] as const satisfies readonly ClassificationField[];
```

4. Tipos:

```ts
export type AssessmentInput = { protocol?: string | null; assessedOn?: string | null; energyMethod?: string | null } & Partial<
  Record<NumericField, number | null>
> &
  Partial<Record<TextField, string | null>> &
  Partial<Record<ClassificationField, string | null>>;

export interface AssessmentValues {
  protocol: AssessmentProtocol | null;
  assessedOn: string | null;
  ageYears: number | null;
  energyMethod: EnergyMethod | null;
  numbers: Record<NumericField, number | null>;
  texts: Record<TextField, string | null>;
  classifications: Record<ClassificationField, string | null>;
}
```

5. Em `normalizeAssessment`:
   - o parâmetro `rules` passa a `AvaliacaoRecordRules`;
   - no laço numérico, `rules.limits[limit]` vira `rangeOf(rules, limit)` (duas vezes);
   - a mensagem de não aplicável passa a usar `notApplicable(protocol)`;
   - antes do `return`, monte:

```ts
  const texts = Object.fromEntries(TEXT_FIELDS.map((field) => [field, cleanMultiline(input[field])])) as Record<TextField, string | null>;
  const classifications = {} as Record<ClassificationField, string | null>;
  for (const field of CLASSIFICATION_FIELDS) classifications[field] = validClassification(field, input[field], protocol, rules, errors);
```

   e acrescente `texts` e `classifications` ao objeto `values`.

6. `toCalculationInput` passa a escolher só os campos das regras:

```ts
/** Entrada do cálculo, com o sexo do cadastro; sem protocolo ou sem idade, não há cálculo. Só os campos das regras. */
export function toCalculationInput(values: AssessmentValues, sex: Sex): AssessmentCalculationInput | null {
  if (values.protocol === null || values.ageYears === null) return null;
  const numbers = Object.fromEntries(CALCULATION_FIELDS.map((field) => [field, values.numbers[field]])) as Pick<
    AssessmentCalculationInput,
    (typeof CALCULATION_FIELDS)[number]
  >;
  return { protocol: values.protocol, sex, ageYears: values.ageYears, energyMethod: values.energyMethod, ...numbers };
}
```

7. Funções auxiliares novas (no fim do arquivo):

```ts
const notApplicable = (protocol: AssessmentProtocol) => `Este campo não se aplica ao protocolo ${protocol}.`;

function rangeOf(rules: AvaliacaoRecordRules, key: LimitKey | RecordLimitKey): Range {
  return Object.hasOwn(rules.recordLimits, key) ? rules.recordLimits[key as RecordLimitKey] : rules.limits[key as LimitKey];
}

/** Texto exato de uma opção do grupo do protocolo (P4-4); sem protocolo válido não há como conferir, e fica vazio. */
function validClassification(
  field: ClassificationField,
  value: string | null | undefined,
  protocol: AssessmentProtocol | null,
  rules: AvaliacaoRecordRules,
  errors: AssessmentFieldErrors,
): string | null {
  const text = cleanText(value);
  if (text === null || protocol === null) return null;
  const group = protocol === 'A' ? undefined : rules.manualClassifications[protocol].find((g) => g.field === field);
  if (!group) {
    errors[field] = notApplicable(protocol);
    return null;
  }
  if (!group.options.some((o) => o.text === text)) {
    errors[field] = 'Opção de classificação desconhecida.';
    return null;
  }
  return text;
}
```

Crie `apps/api/src/assessments/dto/assessment.dto.ts` e apague `dto/assessment-calculation.dto.ts` com `git rm`:

```ts
import { IsInt, IsNumber, IsOptional, IsString, MaxLength, Min } from 'class-validator';

const FINITE = { allowNaN: false, allowInfinity: false };

/** Campos da avaliação sem o protocolo, que não muda depois de criado (P4-3). Números na ordem de NUMERIC_FIELDS. */
export class AssessmentFieldsDto {
  @IsOptional() @IsString() @MaxLength(10) assessedOn?: string | null;
  @IsOptional() @IsString() @MaxLength(40) energyMethod?: string | null;
  @IsOptional() @IsString() @MaxLength(4000) observation?: string | null;
  @IsOptional() @IsString() @MaxLength(4000) conclusion?: string | null;
  @IsOptional() @IsNumber(FINITE) weightKg?: number | null;
  @IsOptional() @IsNumber(FINITE) heightCm?: number | null;
  @IsOptional() @IsNumber(FINITE) waistCm?: number | null;
  @IsOptional() @IsNumber(FINITE) hipCm?: number | null;
  @IsOptional() @IsNumber(FINITE) fatPct?: number | null;
  @IsOptional() @IsNumber(FINITE) wristDiameterMm?: number | null;
  @IsOptional() @IsNumber(FINITE) femurDiameterMm?: number | null;
  @IsOptional() @IsNumber(FINITE) informedBmi?: number | null;
  @IsOptional() @IsNumber(FINITE) skeletalMusclePct?: number | null;
  @IsOptional() @IsNumber(FINITE) visceralFatLevel?: number | null;
  @IsOptional() @IsNumber(FINITE) fatKg?: number | null;
  @IsOptional() @IsNumber(FINITE) boneKg?: number | null;
  @IsOptional() @IsNumber(FINITE) proteinKg?: number | null;
  @IsOptional() @IsNumber(FINITE) waterKg?: number | null;
  @IsOptional() @IsNumber(FINITE) muscleKg?: number | null;
  @IsOptional() @IsNumber(FINITE) skeletalMuscleKg?: number | null;
  @IsOptional() @IsNumber(FINITE) targetFatPct?: number | null;
  @IsOptional() @IsNumber(FINITE) targetBmi?: number | null;
  @IsOptional() @IsNumber(FINITE) informedBmrKcal?: number | null;
  @IsOptional() @IsNumber(FINITE) activityFactor?: number | null;
  @IsOptional() @IsNumber(FINITE) neckCm?: number | null;
  @IsOptional() @IsNumber(FINITE) shouldersCm?: number | null;
  @IsOptional() @IsNumber(FINITE) chestCm?: number | null;
  @IsOptional() @IsNumber(FINITE) armRightCm?: number | null;
  @IsOptional() @IsNumber(FINITE) armLeftCm?: number | null;
  @IsOptional() @IsNumber(FINITE) forearmRightCm?: number | null;
  @IsOptional() @IsNumber(FINITE) forearmLeftCm?: number | null;
  @IsOptional() @IsNumber(FINITE) abdomenCm?: number | null;
  @IsOptional() @IsNumber(FINITE) thighRightCm?: number | null;
  @IsOptional() @IsNumber(FINITE) thighLeftCm?: number | null;
  @IsOptional() @IsNumber(FINITE) calfRightCm?: number | null;
  @IsOptional() @IsNumber(FINITE) calfLeftCm?: number | null;
  @IsOptional() @IsString() @MaxLength(40) bmiClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) fatClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) musclePctClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) muscleKgClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) skeletalMuscleClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) visceralFatClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) subcutaneousFatClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) waterClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) proteinClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) boneClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) bodyAgeClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) bmrClassification?: string | null;
  @IsOptional() @IsString() @MaxLength(40) totalWeightClassification?: string | null;
}

/** Criação e prévia: com o protocolo. */
export class AssessmentBodyDto extends AssessmentFieldsDto {
  @IsOptional() @IsString() @MaxLength(1) protocol?: string | null;
}

/** Rascunho e finalização: sem o protocolo e com a versão lida. */
export class UpdateAssessmentDto extends AssessmentFieldsDto {
  @IsInt() @Min(1) version!: number;
}
```

Em `assessments.controller.ts`, o import `AssessmentCalculationDto` vira `import { AssessmentBodyDto } from './dto/assessment.dto';`, e o corpo da prévia passa a `@Body() body: AssessmentBodyDto`.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS com 48 suítes, inclusive as e2e da prévia e da homologação do Plano 3, que não mudam. `typecheck` sem erros.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/assessments/assessment-normalize.ts apps/api/src/assessments/assessment-normalize.spec.ts apps/api/src/assessments/dto/assessment.dto.ts apps/api/src/assessments/dto/assessment.dto.spec.ts apps/api/src/assessments/dto/assessment-calculation.dto.ts apps/api/src/assessments/assessments.controller.ts
git commit -m "feat(api): validação das circunferências, textos e classificações manuais por protocolo"
```

(`git add` do arquivo apagado registra a remoção feita com `git rm`.)

---

### Task 3: Avaliação, erro da TMB e critérios de finalização

**Files:**
- Create: `apps/api/src/assessments/assessment-evaluation.ts`, `apps/api/src/assessments/assessment-evaluation.spec.ts`
- Modify: `apps/api/src/assessments/assessments.service.ts` (a prévia passa a usar `evaluateAssessment`)

**Interfaces:**
- Consumes: `normalizeAssessment`, `toCalculationInput`, `AssessmentValues`, `AssessmentFieldErrors` e `PatientFacts` (Task 2); `calculateAssessment` e `bmrInputsComplete` (Plano 3); `AvaliacaoRecordRules.finalizeRequired` (Task 1).
- Produces:

```ts
export const INVALID_CALCULATED_BMR = 'A TMB calculada não é válida; confira o método escolhido e os dados.';
export interface AssessmentEvaluation {
  values: AssessmentValues;
  /** Validação: o rascunho não grava com eles. */
  fieldErrors: AssessmentFieldErrors;
  /** TMB calculada não válida: aparece na prévia e impede só a finalização. */
  calculationErrors: AssessmentFieldErrors;
  calculation: AssessmentCalculation | null;
}
export function evaluateAssessment(input: AssessmentInput, patient: PatientFacts, rules: AvaliacaoRecordRules, today: string): AssessmentEvaluation;
export function previewErrors(evaluation: AssessmentEvaluation): AssessmentFieldErrors;
export function finalizationErrors(evaluation: AssessmentEvaluation, rules: AvaliacaoRecordRules): AssessmentFieldErrors;
```

- [ ] **Step 1: Escrever os testes**

`apps/api/src/assessments/assessment-evaluation.spec.ts`:

```ts
import { CURRENT_AVALIACAO_RULES as RULES } from '../clinical-rules/avaliacao-rules';
import { evaluateAssessment, finalizationErrors, INVALID_CALCULATED_BMR, previewErrors } from './assessment-evaluation';
import type { AssessmentInput, PatientFacts } from './assessment-normalize';

const woman: PatientFacts = { sex: 'FEMININO', birthDate: '1986-05-09' };
const elder: PatientFacts = { sex: 'MASCULINO', birthDate: '1929-05-09' };
const evaluate = (input: AssessmentInput, patient: PatientFacts = woman) => evaluateAssessment(input, patient, RULES, '2026-10-11');
const base = { assessedOn: '2026-05-09', weightKg: 80, heightCm: 170 };
const COMPLETE: Record<'A' | 'B' | 'C', AssessmentInput> = {
  A: { protocol: 'A', ...base, fatPct: 20 },
  B: { protocol: 'B', ...base, fatPct: 20, informedBmi: 27.7 },
  C: { protocol: 'C', ...base, fatKg: 16 },
};

describe('evaluateAssessment', () => {
  it('valida, calcula com a versão corrente e separa os erros de campo do erro da TMB', () => {
    const e = evaluate(COMPLETE.A);
    expect(e.fieldErrors).toEqual({});
    expect(e.calculationErrors).toEqual({});
    expect(e.calculation).toMatchObject({ rulesVersion: 'avaliacao-v2', ageYears: 40, bmi: 27.68, fatKg: 16 });
  });

  it('TMB calculada não válida: só em calculationErrors; a prévia mostra', () => {
    const e = evaluate({ protocol: 'A', assessedOn: '2026-05-09', weightKg: 51, heightCm: 170, energyMethod: 'DRI_ADOLESCENTES' }, elder);
    expect(e.fieldErrors).toEqual({});
    expect(e.calculationErrors).toEqual({ energyMethod: INVALID_CALCULATED_BMR });
    expect(previewErrors(e)).toEqual({ energyMethod: INVALID_CALCULATED_BMR });
    expect(e.calculation).toMatchObject({ ageYears: 97, bmrKcal: null, expenditureKcal: null });
  });

  it('método desconhecido: vale o erro do campo, sem o da TMB', () => {
    const e = evaluate({ ...COMPLETE.A, energyMethod: 'KATCH' });
    expect(e.fieldErrors).toEqual({ energyMethod: 'Método de TMB desconhecido.' });
    expect(e.calculationErrors).toEqual({});
  });

  it('C só com a gordura em kg: cálculo parcial, sem exceção', () => {
    const e = evaluate({ protocol: 'C', assessedOn: '2026-05-09', fatKg: 14 });
    expect(e.fieldErrors).toEqual({});
    expect(e.calculation).toMatchObject({ protocol: 'C', fatKg: 14, fatPct: null, totalWeightKg: null, bmrKcal: null });
  });

  it('sem protocolo ou sem data: sem cálculo', () => {
    expect(evaluate({ assessedOn: '2026-05-09', weightKg: 80 }).calculation).toBeNull();
    expect(evaluate({ protocol: 'A', weightKg: 80 }).calculation).toBeNull();
  });
});

describe('finalizationErrors (P4-2, PC-14)', () => {
  it.each(['A', 'B', 'C'] as const)('%s completa: nenhum erro', (protocol) => {
    expect(finalizationErrors(evaluate(COMPLETE[protocol]), RULES)).toEqual({});
  });

  it.each([
    ['A', 'weightKg', 'Informe o peso para finalizar.'],
    ['A', 'heightCm', 'Informe a altura para finalizar.'],
    ['A', 'fatPct', 'Informe o percentual de gordura para finalizar.'],
    ['B', 'weightKg', 'Informe o peso para finalizar.'],
    ['B', 'heightCm', 'Informe a altura para finalizar.'],
    ['B', 'fatPct', 'Informe o percentual de gordura para finalizar.'],
    ['B', 'informedBmi', 'Informe o IMC do aparelho para finalizar.'],
    ['C', 'weightKg', 'Informe o peso para finalizar.'],
    ['C', 'heightCm', 'Informe a altura para finalizar.'],
    ['C', 'fatKg', 'Informe o peso de gordura para finalizar.'],
  ] as const)('%s sem %s: %s', (protocol, field, message) => {
    expect(finalizationErrors(evaluate({ ...COMPLETE[protocol], [field]: null }), RULES)).toEqual({ [field]: message });
  });

  it('o que o protocolo não exige não é cobrado', () => {
    expect(finalizationErrors(evaluate({ protocol: 'A', ...base, fatPct: 20, energyMethod: null, waistCm: null }), RULES)).toEqual({});
    expect(finalizationErrors(evaluate({ protocol: 'C', ...base, fatKg: 16, boneKg: null }), RULES)).toEqual({});
  });

  it('campo inválido: vale a mensagem do limite, e não a de obrigatório', () => {
    expect(finalizationErrors(evaluate({ ...COMPLETE.A, weightKg: 0 }), RULES)).toEqual({
      weightKg: 'O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
    });
  });

  it('a TMB calculada não válida impede a finalização', () => {
    const input = { protocol: 'A', assessedOn: '2026-05-09', weightKg: 51, heightCm: 170, fatPct: 20, energyMethod: 'DRI_ADOLESCENTES' };
    expect(finalizationErrors(evaluate(input, elder), RULES)).toEqual({ energyMethod: INVALID_CALCULATED_BMR });
  });

  it('sem data: o erro da data e os obrigatórios ausentes', () => {
    expect(finalizationErrors(evaluate({ protocol: 'A' }), RULES)).toEqual({
      assessedOn: 'Informe a data da avaliação.',
      weightKg: 'Informe o peso para finalizar.',
      heightCm: 'Informe a altura para finalizar.',
      fatPct: 'Informe o percentual de gordura para finalizar.',
    });
  });

  it('protocolo inválido: só o erro do protocolo', () => {
    expect(finalizationErrors(evaluate({ protocol: 'D', ...base }), RULES)).toEqual({ protocol: 'Escolha o protocolo A, B ou C.' });
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `Set-Location apps/api; npx jest src/assessments/assessment-evaluation --runInBand; Set-Location ../..`
Expected: FAIL com "Cannot find module './assessment-evaluation'".

- [ ] **Step 3: Implementar**

`apps/api/src/assessments/assessment-evaluation.ts`:

```ts
import { calculateAssessment, type AssessmentCalculation } from '../clinical-rules/assessment-calculation';
import type { AvaliacaoRecordRules, FinalizeRequiredField } from '../clinical-rules/avaliacao-rules';
import { bmrInputsComplete } from '../clinical-rules/energy';
import {
  normalizeAssessment, toCalculationInput, type AssessmentFieldErrors, type AssessmentInput, type AssessmentValues, type PatientFacts,
} from './assessment-normalize';

export const INVALID_CALCULATED_BMR = 'A TMB calculada não é válida; confira o método escolhido e os dados.';

const REQUIRED_MESSAGES: Readonly<Record<FinalizeRequiredField, string>> = {
  weightKg: 'Informe o peso para finalizar.',
  heightCm: 'Informe a altura para finalizar.',
  fatPct: 'Informe o percentual de gordura para finalizar.',
  fatKg: 'Informe o peso de gordura para finalizar.',
  informedBmi: 'Informe o IMC do aparelho para finalizar.',
};

export interface AssessmentEvaluation { /* como em Interfaces */ }

/** Valida contra o cadastro (idade na data, DIV-15), calcula o que os campos válidos permitem e separa o erro da TMB. */
export function evaluateAssessment(input: AssessmentInput, patient: PatientFacts, rules: AvaliacaoRecordRules, today: string): AssessmentEvaluation {
  const { values, errors } = normalizeAssessment(input, patient, rules, today);
  const calculationInput = toCalculationInput(values, patient.sex);
  const calculation = calculationInput === null ? null : calculateAssessment(rules, calculationInput);
  const calculationErrors: AssessmentFieldErrors =
    calculation !== null && invalidCalculatedBmr(values, calculation, errors) ? { energyMethod: INVALID_CALCULATED_BMR } : {};
  return { values, fieldErrors: errors, calculationErrors, calculation };
}

/** Prévia: tudo o que a pessoa precisa ver enquanto digita. */
export function previewErrors(evaluation: AssessmentEvaluation): AssessmentFieldErrors {
  return { ...evaluation.fieldErrors, ...evaluation.calculationErrors };
}

/** P4-2 (PC-14): obrigatórios do protocolo ausentes, erros de campo e o da TMB. O erro do limite vence o de ausente. */
export function finalizationErrors(evaluation: AssessmentEvaluation, rules: AvaliacaoRecordRules): AssessmentFieldErrors {
  const missing: AssessmentFieldErrors = {};
  const { protocol, numbers } = evaluation.values;
  if (protocol !== null) {
    for (const field of rules.finalizeRequired[protocol]) if (numbers[field] === null) missing[field] = REQUIRED_MESSAGES[field];
  }
  return { ...missing, ...evaluation.fieldErrors, ...evaluation.calculationErrors };
}
```

Mova para este arquivo a função `invalidCalculatedBmr` de `assessments.service.ts`, sem mudar o corpo nem o comentário (decisão (a) da Task 6 do Plano 3).

Em `assessments.service.ts`, `calculate` passa a:

```ts
  /** Prévia sem gravação: valida contra o cadastro (idade na data, DIV-15) e calcula o que os campos válidos permitem. */
  async calculate(patientId: string, input: AssessmentInput): Promise<AssessmentCalculationResult> {
    const patient = await this.prisma.patient.findUnique({ where: { id: patientId } });
    if (!patient) throw new NotFoundException('Paciente não encontrado.');
    const evaluation = evaluateAssessment(
      input,
      { sex: patient.sex, birthDate: formatIsoDate(patient.birthDate) },
      CURRENT_AVALIACAO_RULES,
      todayInTimeZone(this.config.clinicTimeZone),
    );
    return { calculation: evaluation.calculation, errors: previewErrors(evaluation) };
  }
```

Remova do serviço `INVALID_CALCULATED_BMR`, `invalidCalculatedBmr` e os imports que deixarem de ser usados (`calculateAssessment`, `bmrInputsComplete`, `normalizeAssessment`, `toCalculationInput`, `AssessmentValues`).

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS com 49 suítes. As e2e da prévia (`assessments-calculation`), inclusive as da TMB calculada não válida, continuam verdes sem mudança.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/assessments/assessment-evaluation.ts apps/api/src/assessments/assessment-evaluation.spec.ts apps/api/src/assessments/assessments.service.ts
git commit -m "feat(api): avaliação com erros de campo, da TMB e critérios de finalização por protocolo"
```

---

### Task 4: Modelo de dados, registro do protocolo e visão

**Files:**
- Modify: `apps/api/prisma/schema.prisma`
- Create: `apps/api/prisma/migrations/<carimbo>_assessments/migration.sql` (gerada pelo Prisma)
- Create: `apps/api/src/assessments/assessment-record.ts`, `apps/api/src/assessments/assessment-record.spec.ts`
- Create: `apps/api/src/assessments/assessment.view.ts`, `apps/api/src/assessments/assessment.view.spec.ts`

**Interfaces:**
- Consumes: `AssessmentEvaluation` (Task 3); `NUMERIC_FIELDS`, `TEXT_FIELDS`, `NumericField` e `TextField` (Task 2); `avaliacaoRulesForVersion` (Task 1); `diffFields` e `FieldChanges` de `audit/diff.ts`.
- Produces, em `assessment-record.ts`:

```ts
export type AssessmentInputs = { energyMethod: EnergyMethod | null } & Partial<Record<NumericField, number | null>> & Record<TextField, string | null>;
export type AssessmentClassifications = Partial<Record<ClassificationField, string | null>>;
export interface AssessmentRecordData {
  protocol: AssessmentProtocol; assessedOn: Date; rulesVersion: string; sex: Sex; ageYears: number;
  inputs: AssessmentInputs; classifications: AssessmentClassifications; calculation: AssessmentCalculation;
}
export function toRecordData(evaluation: AssessmentEvaluation, sex: Sex, rules: AvaliacaoRecordRules): AssessmentRecordData;
export function toPrismaData(data: AssessmentRecordData): Pick<Prisma.AssessmentUncheckedCreateInput,
  'protocol' | 'assessedOn' | 'rulesVersion' | 'sex' | 'ageYears' | 'inputs' | 'classifications' | 'calculation'>;
export function assessmentChanges(before: Assessment, after: Assessment): FieldChanges;
```

- Produces, em `assessment.view.ts`:

```ts
export interface SeriesState { currentRevisionId: string | null; openRevisionId: string | null }
export interface AssessmentView {
  id: string; patientId: string; seriesId: string; revision: number; status: AssessmentStatus; protocol: AssessmentProtocol;
  assessedOn: string; rulesVersion: string; sex: Sex; ageYears: number;
  inputs: AssessmentInputs; energyMethodLabel: string | null; classifications: AssessmentClassifications; calculation: AssessmentCalculation;
  revisionReason: string | null; revisionChanges: FieldChanges | null;
  supersededAt: string | null; currentRevisionId: string | null; openRevisionId: string | null;
  version: number; createdAt: string; updatedAt: string; finalizedAt: string | null;
}
export interface AssessmentListItem {
  id: string; seriesId: string; revision: number; protocol: AssessmentProtocol; assessedOn: string; status: AssessmentStatus;
  finalizedAt: string | null; updatedAt: string; openRevisionId: string | null;
}
export function seriesState(rows: readonly Pick<Assessment, 'id' | 'revision' | 'status' | 'supersededAt'>[]): SeriesState;
export function toAssessmentView(row: Assessment, series: SeriesState): AssessmentView;
export function toAssessmentList(rows: readonly Assessment[]): AssessmentListItem[];
```

- [ ] **Step 1: Escrever os testes**

`apps/api/src/assessments/assessment-record.spec.ts`:

```ts
import type { Assessment } from '@prisma/client';
import { CURRENT_AVALIACAO_RULES as RULES } from '../clinical-rules/avaliacao-rules';
import { evaluateAssessment } from './assessment-evaluation';
import type { AssessmentInput } from './assessment-normalize';
import { assessmentChanges, toRecordData } from './assessment-record';

const woman = { sex: 'FEMININO' as const, birthDate: '1986-05-09' };
const record = (input: AssessmentInput) => toRecordData(evaluateAssessment(input, woman, RULES, '2026-10-11'), 'FEMININO', RULES);

describe('toRecordData — o que cada protocolo grava (DIV-11, P4-5, P4-12)', () => {
  it('A: só as chaves da A, a versão das regras e o cálculo do momento', () => {
    const data = record({ protocol: 'A', assessedOn: '2026-05-09', weightKg: 80, heightCm: 170, fatPct: 20, neckCm: 34, observation: 'Obs.' });

    expect(data).toMatchObject({ protocol: 'A', rulesVersion: 'avaliacao-v2', sex: 'FEMININO', ageYears: 40, classifications: {} });
    expect(data.assessedOn).toEqual(new Date('2026-05-09T00:00:00.000Z'));
    expect(data.calculation).toMatchObject({ rulesVersion: 'avaliacao-v2', bmi: 27.68, fatKg: 16 });
    expect(data.inputs).toEqual({
      energyMethod: null, weightKg: 80, heightCm: 170, waistCm: null, hipCm: null, fatPct: 20, wristDiameterMm: null, femurDiameterMm: null,
      targetFatPct: null, targetBmi: null, informedBmrKcal: null, activityFactor: null, neckCm: 34, shouldersCm: null, chestCm: null,
      armRightCm: null, armLeftCm: null, forearmRightCm: null, forearmLeftCm: null, abdomenCm: null, thighRightCm: null, thighLeftCm: null,
      calfRightCm: null, calfLeftCm: null, observation: 'Obs.', conclusion: null,
    });
  });

  it('B: chaves da B e as 4 classificações; nada da A nem da C', () => {
    const data = record({ protocol: 'B', assessedOn: '2026-05-09', informedBmi: 25.7, fatClassification: 'Alto (+)' });
    expect(data.inputs).toMatchObject({ informedBmi: 25.7, fatPct: null, skeletalMusclePct: null, visceralFatLevel: null });
    expect(data.inputs).not.toHaveProperty('wristDiameterMm');
    expect(data.inputs).not.toHaveProperty('fatKg');
    expect(data.classifications).toEqual({
      bmiClassification: null, fatClassification: 'Alto (+)', skeletalMuscleClassification: null, visceralFatClassification: null,
    });
  });

  it('C: kg da C e as 13 classificações; sem a gordura em % (que é calculada)', () => {
    const data = record({ protocol: 'C', assessedOn: '2026-05-09', weightKg: 70, fatKg: 14, boneClassification: 'Alto' });
    expect(data.inputs).toMatchObject({ weightKg: 70, fatKg: 14, boneKg: null, informedBmi: null });
    expect(data.inputs).not.toHaveProperty('fatPct');
    expect(Object.keys(data.classifications)).toHaveLength(13);
    expect(data.classifications).toMatchObject({ boneClassification: 'Alto', waterClassification: null });
    expect(data.calculation).toMatchObject({ fatPct: 20 });
  });

  it('sem protocolo ou sem cálculo é erro de programação (o serviço confere os erros antes)', () => {
    expect(() => record({ assessedOn: '2026-05-09' })).toThrow('Avaliação sem protocolo, data ou cálculo.');
  });
});

describe('assessmentChanges — dados comparados, sem o cálculo', () => {
  const base = {
    id: 'a1', patientId: 'p1', seriesId: 'a1', revision: 1, status: 'RASCUNHO', protocol: 'C', assessedOn: new Date('2026-05-09T00:00:00.000Z'),
    rulesVersion: 'avaliacao-v2', sex: 'FEMININO', ageYears: 40, inputs: { weightKg: 80, neckCm: null }, classifications: { boneClassification: 'Baixo' },
    calculation: { bmi: 27.68 }, revisionReason: null, revisionChanges: null, supersededAt: null, supersededById: null, version: 1,
    createdAt: new Date(), updatedAt: new Date(), createdById: 'u1', updatedById: 'u1', finalizedAt: null, finalizedById: null,
  } as Assessment;

  it('status, data, entradas e classificações; o cálculo e a versão do registro não entram', () => {
    const after = {
      ...base, status: 'FINALIZADA', assessedOn: new Date('2026-05-10T00:00:00.000Z'), version: 2,
      inputs: { weightKg: 81, neckCm: 35 }, classifications: { boneClassification: 'Alto' }, calculation: { bmi: 28.03 },
    } as Assessment;
    expect(assessmentChanges(base, after)).toEqual({
      status: { from: 'RASCUNHO', to: 'FINALIZADA' },
      assessedOn: { from: '2026-05-09', to: '2026-05-10' },
      'inputs.weightKg': { from: 80, to: 81 },
      'inputs.neckCm': { from: null, to: 35 },
      'classifications.boneClassification': { from: 'Baixo', to: 'Alto' },
    });
  });

  it('chave que só existe de um lado conta como null; versão de regras, sexo e idade entram', () => {
    const after = { ...base, rulesVersion: 'avaliacao-v3', sex: 'MASCULINO', ageYears: 41, inputs: { weightKg: 80, neckCm: null, calfLeftCm: 30 } } as Assessment;
    expect(assessmentChanges(base, after)).toEqual({
      rulesVersion: { from: 'avaliacao-v2', to: 'avaliacao-v3' },
      sex: { from: 'FEMININO', to: 'MASCULINO' },
      ageYears: { from: 40, to: 41 },
      'inputs.calfLeftCm': { from: null, to: 30 },
    });
  });

  it('nada mudou: vazio', () => {
    expect(assessmentChanges(base, { ...base, version: 5, calculation: { bmi: 1 } } as Assessment)).toEqual({});
  });
});
```

`apps/api/src/assessments/assessment.view.spec.ts`:

```ts
import type { Assessment } from '@prisma/client';
import { seriesState, toAssessmentList, toAssessmentView } from './assessment.view';

function row(overrides: Partial<Assessment> = {}): Assessment {
  return {
    id: 'a1', patientId: 'p1', seriesId: 'a1', revision: 1, status: 'RASCUNHO', protocol: 'A',
    assessedOn: new Date('2026-05-09T00:00:00.000Z'), rulesVersion: 'avaliacao-v2', sex: 'FEMININO', ageYears: 40,
    inputs: { energyMethod: 'HARRIS_BENEDICT', weightKg: 80 }, classifications: {}, calculation: { rulesVersion: 'avaliacao-v2', bmi: 27.68 },
    revisionReason: null, revisionChanges: null, supersededAt: null, supersededById: null, version: 1,
    createdAt: new Date('2026-05-09T13:00:00.000Z'), updatedAt: new Date('2026-05-09T14:00:00.000Z'), createdById: 'u1', updatedById: 'u1',
    finalizedAt: null, finalizedById: null, ...overrides,
  } as Assessment;
}
const SUPERSEDED = new Date('2026-05-10T12:00:00.000Z');

describe('seriesState', () => {
  it('vigente é a finalizada não substituída; aberta é o rascunho de revisão maior que 1', () => {
    expect(seriesState([row()])).toEqual({ currentRevisionId: null, openRevisionId: null });
    expect(seriesState([row({ status: 'FINALIZADA' })])).toEqual({ currentRevisionId: 'a1', openRevisionId: null });
    expect(
      seriesState([row({ status: 'FINALIZADA', supersededAt: SUPERSEDED }), row({ id: 'a2', revision: 2, status: 'FINALIZADA' }), row({ id: 'a3', revision: 3 })]),
    ).toEqual({ currentRevisionId: 'a2', openRevisionId: 'a3' });
  });
});

describe('toAssessmentList — uma linha por série, na ordem recebida', () => {
  it('rascunho da revisão 1, vigente e revisão em andamento', () => {
    const rows = [
      row({ id: 'b1', seriesId: 'b1', assessedOn: new Date('2026-06-01T00:00:00.000Z') }),
      row({ id: 'a1', status: 'FINALIZADA', supersededAt: SUPERSEDED, finalizedAt: SUPERSEDED }),
      row({ id: 'a2', revision: 2, status: 'FINALIZADA', finalizedAt: SUPERSEDED }),
      row({ id: 'a3', revision: 3 }),
    ];
    expect(toAssessmentList(rows)).toEqual([
      { id: 'b1', seriesId: 'b1', revision: 1, protocol: 'A', assessedOn: '2026-06-01', status: 'RASCUNHO', finalizedAt: null, updatedAt: '2026-05-09T14:00:00.000Z', openRevisionId: null },
      { id: 'a2', seriesId: 'a1', revision: 2, protocol: 'A', assessedOn: '2026-05-09', status: 'FINALIZADA', finalizedAt: '2026-05-10T12:00:00.000Z', updatedAt: '2026-05-09T14:00:00.000Z', openRevisionId: 'a3' },
    ]);
  });
});

describe('toAssessmentView — o que foi gravado, com a legenda da versão da própria linha', () => {
  it('rascunho da v2: legenda do legado, datas ISO e série', () => {
    expect(toAssessmentView(row(), { currentRevisionId: null, openRevisionId: null })).toEqual({
      id: 'a1', patientId: 'p1', seriesId: 'a1', revision: 1, status: 'RASCUNHO', protocol: 'A', assessedOn: '2026-05-09',
      rulesVersion: 'avaliacao-v2', sex: 'FEMININO', ageYears: 40, inputs: { energyMethod: 'HARRIS_BENEDICT', weightKg: 80 },
      energyMethodLabel: 'Harris-Benedict (1919) - 15 aos 74 anos', classifications: {}, calculation: { rulesVersion: 'avaliacao-v2', bmi: 27.68 },
      revisionReason: null, revisionChanges: null, supersededAt: null, currentRevisionId: null, openRevisionId: null, version: 1,
      createdAt: '2026-05-09T13:00:00.000Z', updatedAt: '2026-05-09T14:00:00.000Z', finalizedAt: null,
    });
  });

  it('linha gravada com a v1 mostra a legenda curta da v1; sem método, null', () => {
    const state = { currentRevisionId: null, openRevisionId: null };
    expect(toAssessmentView(row({ rulesVersion: 'avaliacao-v1' }), state).energyMethodLabel).toBe('Harris-Benedict');
    expect(toAssessmentView(row({ inputs: { energyMethod: null } }), state).energyMethodLabel).toBeNull();
  });

  it('versão desconhecida no banco é erro, inclusive chave herdada', () => {
    const state = { currentRevisionId: null, openRevisionId: null };
    expect(() => toAssessmentView(row({ rulesVersion: 'toString' }), state)).toThrow('Versão de regras de avaliação desconhecida: toString');
    expect(() => toAssessmentView(row({ rulesVersion: '__proto__', inputs: { energyMethod: null } }), state)).toThrow(
      'Versão de regras de avaliação desconhecida: __proto__',
    );
  });

  it('revisão substituída: justificativa, campos alterados e data da substituição', () => {
    const view = toAssessmentView(
      row({ revision: 2, status: 'FINALIZADA', supersededAt: SUPERSEDED, revisionReason: 'Peso errado.', revisionChanges: { 'inputs.weightKg': { from: 80, to: 81 } } }),
      { currentRevisionId: 'a3', openRevisionId: null },
    );
    expect(view).toMatchObject({
      revision: 2, revisionReason: 'Peso errado.', revisionChanges: { 'inputs.weightKg': { from: 80, to: 81 } },
      supersededAt: '2026-05-10T12:00:00.000Z', currentRevisionId: 'a3',
    });
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `Set-Location apps/api; npx jest src/assessments/assessment-record src/assessments/assessment.view --runInBand; Set-Location ../..`
Expected: FAIL. Os módulos não existem, e o tipo `Assessment` não existe no Prisma Client.

- [ ] **Step 3: Implementar o modelo e gerar a migração**

Em `apps/api/prisma/schema.prisma`:

- no modelo `Patient`, depois de `anamneses Anamnesis[]`, acrescente `assessments Assessment[]`;
- ao fim do arquivo:

```prisma
enum AssessmentStatus {
  RASCUNHO
  FINALIZADA
}

enum AssessmentProtocol {
  A
  B
  C
}

/// Avaliação (aba Avaliações do legado). Cada linha é uma revisão; seriesId junta as revisões (a 1 tem seriesId = id).
/// inputs e classifications guardam só as chaves do protocolo (DIV-11); calculation é o resultado gravado (P4-5).
model Assessment {
  id              String             @id @default(uuid()) @db.Uuid
  patientId       String             @map("patient_id") @db.Uuid
  patient         Patient            @relation(fields: [patientId], references: [id], onDelete: Restrict)
  seriesId        String             @map("series_id") @db.Uuid
  revision        Int
  status          AssessmentStatus   @default(RASCUNHO)
  protocol        AssessmentProtocol
  assessedOn      DateTime           @map("assessed_on") @db.Date
  rulesVersion    String             @map("rules_version")
  sex             Sex
  ageYears        Int                @map("age_years")
  inputs          Json
  classifications Json
  calculation     Json
  revisionReason  String?            @map("revision_reason")
  revisionChanges Json?              @map("revision_changes")
  supersededAt    DateTime?          @map("superseded_at") @db.Timestamptz(3)
  supersededById  String?            @map("superseded_by_id") @db.Uuid
  version         Int                @default(1)
  createdAt       DateTime           @default(now()) @map("created_at") @db.Timestamptz(3)
  updatedAt       DateTime           @updatedAt @map("updated_at") @db.Timestamptz(3)
  createdById     String             @map("created_by_id") @db.Uuid
  updatedById     String             @map("updated_by_id") @db.Uuid
  finalizedAt     DateTime?          @map("finalized_at") @db.Timestamptz(3)
  finalizedById   String?            @map("finalized_by_id") @db.Uuid

  @@unique([seriesId, revision])
  @@index([patientId, assessedOn])
  @@map("assessments")
}
```

Gere a migração no banco de desenvolvimento:

```powershell
Set-Location apps/api; npx prisma migrate dev --name assessments; Set-Location ../..
```

Expected: cria `apps/api/prisma/migrations/<carimbo>_assessments/migration.sql`. Ela tem os dois `CREATE TYPE`, o `CREATE TABLE "assessments"` (com `inputs`, `classifications` e `calculation` em `JSONB`), o índice único `assessments_series_id_revision_key`, o índice de `patient_id, assessed_on` e a chave estrangeira com `ON DELETE RESTRICT`. O Prisma Client é regenerado.

- [ ] **Step 4: Implementar o registro e a visão**

`apps/api/src/assessments/assessment-record.ts`:

```ts
import type { Assessment, Prisma, Sex } from '@prisma/client';
import { diffFields, type FieldChanges } from '../audit/diff';
import type { AssessmentCalculation } from '../clinical-rules/assessment-calculation';
import type { AssessmentProtocol, AvaliacaoRecordRules, ClassificationField, EnergyMethod } from '../clinical-rules/avaliacao-rules';
import { formatIsoDate, parseIsoDate } from '../common/civil-date';
import type { AssessmentEvaluation } from './assessment-evaluation';
import { NUMERIC_FIELDS, type NumericField, type TextField } from './assessment-normalize';

// (tipos AssessmentInputs, AssessmentClassifications e AssessmentRecordData como em Interfaces)

/** O que o protocolo grava (DIV-11, P4-12): só as chaves dele; o que não se aplica nem aparece; null é "não informado". */
export function toRecordData(evaluation: AssessmentEvaluation, sex: Sex, rules: AvaliacaoRecordRules): AssessmentRecordData {
  const { values, calculation } = evaluation;
  if (values.protocol === null || values.assessedOn === null || calculation === null) {
    throw new Error('Avaliação sem protocolo, data ou cálculo.');
  }
  const protocol = values.protocol;
  const numbers = NUMERIC_FIELDS.filter((f) => (f.protocols as readonly AssessmentProtocol[]).includes(protocol)).map(
    ({ field }) => [field, values.numbers[field]] as const,
  );
  const groups = protocol === 'A' ? [] : rules.manualClassifications[protocol];
  return {
    protocol,
    assessedOn: parseIsoDate(values.assessedOn) as Date,
    rulesVersion: calculation.rulesVersion,
    sex,
    ageYears: calculation.ageYears,
    inputs: { energyMethod: values.energyMethod, ...Object.fromEntries(numbers), ...values.texts },
    classifications: Object.fromEntries(groups.map(({ field }) => [field, values.classifications[field]])),
    calculation,
  };
}

export function toPrismaData(data: AssessmentRecordData) {
  return {
    protocol: data.protocol,
    assessedOn: data.assessedOn,
    rulesVersion: data.rulesVersion,
    sex: data.sex,
    ageYears: data.ageYears,
    inputs: data.inputs as unknown as Prisma.InputJsonObject,
    classifications: data.classifications as unknown as Prisma.InputJsonObject,
    calculation: data.calculation as unknown as Prisma.InputJsonObject,
  };
}

/** Registro achatado para comparar: dados da avaliação com prefixo do bloco JSONB; o cálculo fica de fora. */
function flat(row: Assessment): Record<string, unknown> {
  const prefixed = (prefix: string, value: Prisma.JsonValue) =>
    Object.entries((value ?? {}) as Record<string, unknown>).map(([key, v]) => [`${prefix}.${key}`, v]);
  return {
    status: row.status,
    protocol: row.protocol,
    assessedOn: formatIsoDate(row.assessedOn),
    rulesVersion: row.rulesVersion,
    sex: row.sex,
    ageYears: row.ageYears,
    ...Object.fromEntries(prefixed('inputs', row.inputs)),
    ...Object.fromEntries(prefixed('classifications', row.classifications)),
  };
}

/** Campos alterados (auditoria e revisões, P4-6): chave que só existe de um lado conta como null. */
export function assessmentChanges(before: Assessment, after: Assessment): FieldChanges {
  const a = flat(before);
  const b = flat(after);
  return diffFields(a, b, [...new Set([...Object.keys(a), ...Object.keys(b)])]);
}
```

`apps/api/src/assessments/assessment.view.ts`:

```ts
import type { Assessment } from '@prisma/client';
import type { FieldChanges } from '../audit/diff';
import type { AssessmentCalculation } from '../clinical-rules/assessment-calculation';
import { avaliacaoRulesForVersion } from '../clinical-rules/avaliacao-rules';
import { formatIsoDate } from '../common/civil-date';
import type { AssessmentClassifications, AssessmentInputs } from './assessment-record';

// (interfaces SeriesState, AssessmentView e AssessmentListItem como em Interfaces; AssessmentStatus/AssessmentProtocol de @prisma/client)

type SeriesRow = Pick<Assessment, 'id' | 'revision' | 'status' | 'supersededAt'>;

const isCurrent = (r: SeriesRow) => r.status === 'FINALIZADA' && r.supersededAt === null;
const isOpenRevision = (r: SeriesRow) => r.status === 'RASCUNHO' && r.revision > 1;

/** Vigente: a finalizada não substituída. Aberta: o rascunho de uma revisão maior que 1 (no máximo um por série). */
export function seriesState(rows: readonly SeriesRow[]): SeriesState {
  return { currentRevisionId: rows.find(isCurrent)?.id ?? null, openRevisionId: rows.find(isOpenRevision)?.id ?? null };
}

/** O que foi gravado; a leitura nunca recalcula (P4-5). A legenda do método vem da versão de regras da própria linha. */
export function toAssessmentView(row: Assessment, series: SeriesState): AssessmentView {
  const inputs = row.inputs as unknown as AssessmentInputs;
  // Toda leitura confere a versão gravada, com ou sem método (chave herdada também é erro).
  const rules = avaliacaoRulesForVersion(row.rulesVersion);
  const method = inputs.energyMethod ? rules.energy.methods.find((m) => m.id === inputs.energyMethod) : undefined;
  return {
    id: row.id, patientId: row.patientId, seriesId: row.seriesId, revision: row.revision, status: row.status, protocol: row.protocol,
    assessedOn: formatIsoDate(row.assessedOn), rulesVersion: row.rulesVersion, sex: row.sex, ageYears: row.ageYears,
    inputs, energyMethodLabel: method?.label ?? null,
    classifications: row.classifications as unknown as AssessmentClassifications,
    calculation: row.calculation as unknown as AssessmentCalculation,
    revisionReason: row.revisionReason, revisionChanges: row.revisionChanges as unknown as FieldChanges | null,
    supersededAt: row.supersededAt?.toISOString() ?? null, ...series,
    version: row.version, createdAt: row.createdAt.toISOString(), updatedAt: row.updatedAt.toISOString(),
    finalizedAt: row.finalizedAt?.toISOString() ?? null,
  };
}

/** Lista do paciente: uma linha por série (o rascunho da revisão 1 ou a vigente), na ordem recebida. */
export function toAssessmentList(rows: readonly Assessment[]): AssessmentListItem[] {
  return rows
    .filter((r) => isCurrent(r) || (r.status === 'RASCUNHO' && r.revision === 1))
    .map((r) => ({
      id: r.id, seriesId: r.seriesId, revision: r.revision, protocol: r.protocol, assessedOn: formatIsoDate(r.assessedOn), status: r.status,
      finalizedAt: r.finalizedAt?.toISOString() ?? null, updatedAt: r.updatedAt.toISOString(),
      openRevisionId: rows.find((o) => o.seriesId === r.seriesId && isOpenRevision(o))?.id ?? null,
    }));
}
```

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS com 51 suítes. O `global-setup` aplica a migração nova no `psm_test` (`prisma migrate reset`). `typecheck` sem erros.

- [ ] **Step 6: Commit**

```powershell
git add apps/api/prisma/schema.prisma apps/api/prisma/migrations apps/api/src/assessments/assessment-record.ts apps/api/src/assessments/assessment-record.spec.ts apps/api/src/assessments/assessment.view.ts apps/api/src/assessments/assessment.view.spec.ts
git commit -m "feat(api): tabela de avaliações por revisão, registro do protocolo em JSONB e visão com a versão da linha"
```

---
### Task 5: API — criar, ler, listar e salvar o rascunho (avaliação A)

**Files:**
- Modify: `apps/api/src/assessments/assessments.service.ts`, `apps/api/src/assessments/assessments.controller.ts`
- Create: `apps/api/test/assessments.e2e-spec.ts`

**Interfaces:**
- Consumes:
  - `evaluateAssessment` e `previewErrors` (Task 3);
  - `toRecordData`, `toPrismaData` e `assessmentChanges` (Task 4);
  - `seriesState`, `toAssessmentView`, `toAssessmentList`, `AssessmentView` e `AssessmentListItem` (Task 4);
  - `AssessmentBodyDto` e `UpdateAssessmentDto` (Task 2);
  - `AuditService` (global);
  - `AuthContext` e `CurrentAuth`.
- Produces, em `AssessmentsService`:
  - `list(patientId: string): Promise<AssessmentListItem[]>`;
  - `get(id: string): Promise<AssessmentView>`;
  - `create(patientId: string, body: AssessmentInput, actorId: string): Promise<AssessmentView>`;
  - `update(id: string, body: UpdateAssessmentDto, auth: AuthContext): Promise<AssessmentView>`;
  - os privados `save`, `view`, `conflict`, `lostRace`, `patientOrThrow`, `evaluate` e `throwIfErrors`, que as Tasks 8 e 9 estendem.
- Produces, as rotas:
  - `GET /api/patients/:patientId/assessments`;
  - `POST /api/patients/:patientId/assessments` (201);
  - `GET /api/assessments/:id`;
  - `PUT /api/assessments/:id`.

- [ ] **Step 1: Escrever os testes**

Crie `apps/api/test/assessments.e2e-spec.ts`:

```ts
import { randomUUID } from 'node:crypto';
import { INestApplication } from '@nestjs/common';
import request from 'supertest';
import { todayInTimeZone } from '../src/common/civil-date';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';
import { runPaused } from './helpers/run-paused';

const mulher = { fullName: 'Paciente Sintética Avaliação', sex: 'FEMININO', birthDate: '1986-05-09' };
const avaliacaoA = {
  protocol: 'A', assessedOn: '2026-05-09', weightKg: 80, heightCm: 170, waistCm: 80, hipCm: 100, fatPct: 20, wristDiameterMm: 50,
  femurDiameterMm: 90, energyMethod: 'HARRIS_BENEDICT', activityFactor: 1.55, neckCm: 34, shouldersCm: 105, chestCm: 92, armRightCm: 30,
  armLeftCm: 29.5, forearmRightCm: 25, forearmLeftCm: 24.5, abdomenCm: 85, thighRightCm: 55, thighLeftCm: 54.5, calfRightCm: 36,
  calfLeftCm: 35.5, observation: 'Observação sintética.', conclusion: 'Conclusão sintética.',
};
const { protocol: _protocol, ...rascunhoA } = avaliacaoA;
const { assessedOn: _assessedOn, ...camposA } = rascunhoA;
/** Tudo o que a A grava (P4-12): o que não foi enviado aparece como null. */
const inputsA = { ...camposA, targetFatPct: null, targetBmi: null, informedBmrKcal: null };
const CONFLICT = 'Esta avaliação foi alterada por outra pessoa. Recarregue para ver a versão atual.';

describe('Avaliações — rascunho (HTTP)', () => {
  let app: INestApplication;
  let prisma: PrismaService;

  beforeAll(async () => {
    app = await createTestApp();
    prisma = app.get(PrismaService);
  });

  beforeEach(() => resetDb(prisma));

  afterAll(async () => {
    await app.close();
  });

  async function patientFor(agent: Agent, body: Record<string, unknown> = mulher): Promise<string> {
    const res = await agent.post('/api/patients').send(body);
    expect(res.status).toBe(201);
    return res.body.id as string;
  }

  async function draftFor(agent: Agent, body: Record<string, unknown> = avaliacaoA) {
    const patientId = await patientFor(agent);
    const res = await agent.post(`/api/patients/${patientId}/assessments`).send(body);
    expect(res.status).toBe(201);
    return { patientId, id: res.body.id as string };
  }

  async function setAssistant(admin: Agent, changes: { permission: string; granted: boolean }[]) {
    expect((await admin.put('/api/permissions/assistant').send({ changes })).status).toBe(200);
  }

  const versionOf = async (id: string) => (await prisma.assessment.findUniqueOrThrow({ where: { id } })).version;

  it('assistente cria o rascunho da A: entradas do protocolo, versão das regras, cálculo e auditoria', async () => {
    const { agent, user } = await loginAs(app, 'ASSISTENTE');
    const patientId = await patientFor(agent);

    const res = await agent.post(`/api/patients/${patientId}/assessments`).send(avaliacaoA);

    expect(res.status).toBe(201);
    expect(res.body).toMatchObject({
      patientId, seriesId: res.body.id, revision: 1, status: 'RASCUNHO', protocol: 'A', assessedOn: '2026-05-09', rulesVersion: 'avaliacao-v2',
      sex: 'FEMININO', ageYears: 40, classifications: {}, energyMethodLabel: 'Harris-Benedict (1919) - 15 aos 74 anos', version: 1,
      revisionReason: null, revisionChanges: null, supersededAt: null, currentRevisionId: null, openRevisionId: null, finalizedAt: null,
    });
    expect(res.body.inputs).toEqual(inputsA);
    expect(res.body.calculation).toMatchObject({
      rulesVersion: 'avaliacao-v2', ageBand: 'Adultos', bmi: 27.68, bmiClassification: 'Sobrepeso', fatKg: 16, fatClassification: 'Bom',
      fatFreeKg: 64, fatFreePct: 80, residualKg: 16.72, residualPct: 20.9, boneKg: 9.77, bonePct: 12.21, muscleKg: 37.51, musclePct: 46.89,
      waistHipRatio: 0.8, waistHipClassification: 'Risco Alto', targetFatPct: 25, targetWeightByFatKg: 85.33, weightControlByFatKg: 5.33,
      targetWeightByBmiKg: 65.03, weightControlByBmiKg: -14.97, bmrKcal: 1548, expenditureKcal: 2399.4,
    });
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'assessment.create' } });
    expect(event).toMatchObject({ actorId: user.id, entityType: 'assessment', entityId: res.body.id });
    expect(event.data).toEqual({ patientId, seriesId: res.body.id, revision: 1, protocol: 'A' });
  });

  it('lê, lista e salva o rascunho com a versão; recalcula e audita os campos alterados', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { patientId, id } = await draftFor(agent);

    const read = await agent.get(`/api/assessments/${id}`);
    const list = await agent.get(`/api/patients/${patientId}/assessments`);
    const saved = await agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, weightKg: 81, neckCm: 35, version: 1 });

    expect(read.status).toBe(200);
    expect(read.body.inputs).toEqual(inputsA);
    expect(list.body).toEqual([
      { id, seriesId: id, revision: 1, protocol: 'A', assessedOn: '2026-05-09', status: 'RASCUNHO', finalizedAt: null, updatedAt: expect.any(String), openRevisionId: null },
    ]);
    expect(saved.status).toBe(200);
    expect(saved.body).toMatchObject({ version: 2, inputs: { weightKg: 81, neckCm: 35 } });
    expect(saved.body.calculation).toMatchObject({ bmi: 28.03, fatKg: 16.2, muscleKg: 38.1, bmrKcal: 1557, expenditureKcal: 2413.35 });
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'assessment.update' } });
    expect(event.data).toEqual({
      patientId, seriesId: id, revision: 1, changes: { 'inputs.weightKg': { from: 80, to: 81 }, 'inputs.neckCm': { from: 34, to: 35 } },
    });
  });

  it('PUT substitui o formulário inteiro: campo que não vem fica null', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { id } = await draftFor(agent);

    const res = await agent.put(`/api/assessments/${id}`).send({ assessedOn: '2026-05-09', weightKg: 80, version: 1 });

    expect(res.status).toBe(200);
    expect(res.body.inputs).toMatchObject({ weightKg: 80, heightCm: null, neckCm: null, observation: null, energyMethod: null });
    expect(res.body.calculation).toMatchObject({ bmi: null, bmrKcal: null });
  });

  it('a lista vem da data mais recente para a mais antiga e só traz as do paciente', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { patientId, id: older } = await draftFor(agent);
    const newer = await agent.post(`/api/patients/${patientId}/assessments`).send({ protocol: 'B', assessedOn: '2026-06-01' });
    const other = await patientFor(agent, { ...mulher, fullName: 'Outra Paciente Sintética' });
    await agent.post(`/api/patients/${other}/assessments`).send({ protocol: 'C', assessedOn: '2026-07-01' });

    const res = await agent.get(`/api/patients/${patientId}/assessments`);

    expect(res.body.map((item: { id: string; protocol: string }) => [item.id, item.protocol])).toEqual([[newer.body.id, 'B'], [older, 'A']]);
  });

  it('o rascunho exige só protocolo e data e aceita a TMB calculada não válida (só a finalização recusa)', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);
    const idoso = await patientFor(agent, { fullName: 'Paciente Sintético Idoso', sex: 'MASCULINO', birthDate: '1929-05-09' });

    const minimal = await agent.post(`/api/patients/${patientId}/assessments`).send({ protocol: 'C', assessedOn: '2026-05-09' });
    const badBmr = await agent
      .post(`/api/patients/${idoso}/assessments`)
      .send({ protocol: 'A', assessedOn: '2026-05-09', weightKg: 51, heightCm: 170, energyMethod: 'DRI_ADOLESCENTES' });

    expect(minimal.status).toBe(201);
    expect(minimal.body.inputs).toMatchObject({ weightKg: null, fatKg: null, energyMethod: null });
    expect(Object.keys(minimal.body.classifications)).toHaveLength(13);
    expect(badBmr.status).toBe(201);
    expect(badBmr.body.calculation).toMatchObject({ ageYears: 97, energyMethod: 'DRI_ADOLESCENTES', bmrKcal: null });
  });

  it('erros por campo: 400 sem gravar nem auditar; campo de outro protocolo é recusado (DIV-11)', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { patientId, id } = await draftFor(agent);

    const badCreate = await agent.post(`/api/patients/${patientId}/assessments`).send({ protocol: 'A', assessedOn: '2026-05-09', weightKg: 0, informedBmi: 25 });
    const noProtocol = await agent.post(`/api/patients/${patientId}/assessments`).send({ assessedOn: '2026-05-09' });
    const badPut = await agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, fatKg: 10, boneClassification: 'Alto', version: 1 });

    expect(badCreate.status).toBe(400);
    expect(badCreate.body.message).toBe('Dados inválidos.');
    expect(badCreate.body.errors).toEqual({
      weightKg: 'O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
      informedBmi: 'Este campo não se aplica ao protocolo A.',
    });
    expect(noProtocol.body.errors).toEqual({ protocol: 'Escolha o protocolo A, B ou C.' });
    expect(badPut.status).toBe(400);
    expect(badPut.body.errors).toEqual({
      fatKg: 'Este campo não se aplica ao protocolo A.',
      boneClassification: 'Este campo não se aplica ao protocolo A.',
    });
    expect(await prisma.assessment.count()).toBe(1);
    expect(await versionOf(id)).toBe(1);
    expect(await prisma.auditEvent.count({ where: { action: { startsWith: 'assessment.' } } })).toBe(1);
  });

  it('o protocolo não muda depois de criado (P4-3): PUT com protocolo, mesmo o mesmo, é 400 e nada muda', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { id } = await draftFor(agent);

    const other = await agent.put(`/api/assessments/${id}`).send({ ...avaliacaoA, protocol: 'B', version: 1 });
    const same = await agent.put(`/api/assessments/${id}`).send({ ...avaliacaoA, version: 1 });

    expect([other.status, same.status]).toEqual([400, 400]);
    expect(await prisma.assessment.findUniqueOrThrow({ where: { id } })).toMatchObject({ protocol: 'A', version: 1 });
  });

  it('data: hoje, o dia do nascimento e 1900-01-01 são aceitos; amanhã, a véspera do nascimento e 1899-12-31, recusados', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);
    const centenarian = await patientFor(agent, { fullName: 'Paciente Sintético 1900', sex: 'MASCULINO', birthDate: '1900-01-01' });
    const today = todayInTimeZone('America/Sao_Paulo');
    const tomorrow = new Date(Date.parse(`${today}T00:00:00Z`) + 86_400_000).toISOString().slice(0, 10);
    const create = (assessedOn: string, patient = patientId) => agent.post(`/api/patients/${patient}/assessments`).send({ protocol: 'A', assessedOn });

    const onToday = await create(today);
    const onBirthday = await create('1986-05-09');
    const on1900 = await create('1900-01-01', centenarian);
    const onTomorrow = await create(tomorrow);
    const dayBeforeBirth = await create('1986-05-08');
    const before1900 = await create('1899-12-31', centenarian);

    expect(onToday.status).toBe(201);
    expect(onBirthday.body).toMatchObject({ assessedOn: '1986-05-09', ageYears: 0 });
    expect(on1900.body).toMatchObject({ assessedOn: '1900-01-01', ageYears: 0 });
    expect(onTomorrow.body.errors).toEqual({ assessedOn: 'A data da avaliação não pode estar no futuro.' });
    expect(dayBeforeBirth.body.errors).toEqual({ assessedOn: 'A data da avaliação não pode ser anterior ao nascimento do paciente.' });
    expect(before1900.body.errors).toEqual({ assessedOn: 'A data da avaliação deve ser a partir de 1900.' });
  });

  it('versão desatualizada: 409 com a avaliação atual, sem gravar', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { id } = await draftFor(agent);
    expect((await agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, weightKg: 81, version: 1 })).status).toBe(200);

    const stale = await agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, neckCm: 40, version: 1 });

    expect(stale.status).toBe(409);
    expect(stale.body.message).toBe(CONFLICT);
    expect(stale.body.current).toMatchObject({ id, version: 2, inputs: { weightKg: 81, neckCm: 34 } });
    expect(await versionOf(id)).toBe(2);
  });

  it('dois rascunhos da versão 1 ao mesmo tempo: um grava e o outro recebe 409 com a versão atual (H-19)', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { id } = await draftFor(agent);

    const [first, second] = await runPaused(
      app,
      (input) => input.action === 'assessment.update',
      () => agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, weightKg: 81, version: 1 }),
      () => agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, neckCm: 40, version: 1 }),
    );

    expect([first.status, second.status]).toEqual([200, 409]);
    expect(second.body.message).toBe(CONFLICT);
    expect(second.body.current).toMatchObject({ version: 2, inputs: { weightKg: 81, neckCm: 34 } });
    expect(await prisma.assessment.findUniqueOrThrow({ where: { id } })).toMatchObject({ version: 2, inputs: expect.objectContaining({ weightKg: 81, neckCm: 34 }) });
  });

  it('404 e 400: avaliação e paciente inexistentes, id inválido, campo desconhecido', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { patientId } = await draftFor(agent);

    const missing = await agent.get(`/api/assessments/${randomUUID()}`);
    expect(missing.status).toBe(404);
    expect(missing.body.message).toBe('Avaliação não encontrada.');
    expect((await agent.put(`/api/assessments/${randomUUID()}`).send({ ...rascunhoA, version: 1 })).status).toBe(404);
    const noPatient = await agent.post(`/api/patients/${randomUUID()}/assessments`).send(avaliacaoA);
    expect(noPatient.status).toBe(404);
    expect(noPatient.body.message).toBe('Paciente não encontrado.');
    expect((await agent.get(`/api/patients/${randomUUID()}/assessments`)).status).toBe(404);
    expect((await agent.get('/api/assessments/abc')).status).toBe(400);
    expect((await agent.post(`/api/patients/${patientId}/assessments`).send({ ...avaliacaoA, pontuacao: 10 })).status).toBe(400);
  });

  it('sem sessão: 401 em todas as rotas', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { patientId, id } = await draftFor(agent);
    const anonymous = request(app.getHttpServer());

    const statuses = [
      (await anonymous.get(`/api/patients/${patientId}/assessments`)).status,
      (await anonymous.post(`/api/patients/${patientId}/assessments`).send(avaliacaoA)).status,
      (await anonymous.get(`/api/assessments/${id}`)).status,
      (await anonymous.put(`/api/assessments/${id}`).send({ ...rascunhoA, version: 1 })).status,
    ];

    expect(statuses).toEqual([401, 401, 401, 401]);
  });

  it('permissões: cada combinação isola uma permissão', async () => {
    const professional = await loginAs(app, 'PROFISSIONAL');
    const { patientId, id } = await draftFor(professional.agent);
    const assistant = await loginAs(app, 'ASSISTENTE');
    const admin = await loginAs(app, 'ADMIN');
    const statuses = async () => [
      (await assistant.agent.get(`/api/patients/${patientId}/assessments`)).status,
      (await assistant.agent.get(`/api/assessments/${id}`)).status,
      (await assistant.agent.post(`/api/patients/${patientId}/assessments`).send({ protocol: 'A', assessedOn: '2026-05-09' })).status,
      (await assistant.agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, version: await versionOf(id) })).status,
    ];

    // Padrão do assistente: patients.read + assessments.draft.
    expect(await statuses()).toEqual([200, 200, 201, 200]);

    // Sem assessments.draft e sem finalize: as permissões de anamnese (que o assistente mantém) não bastam.
    await setAssistant(admin.agent, [{ permission: 'assessments.draft', granted: false }]);
    expect(await statuses()).toEqual([403, 403, 403, 403]);

    // Só assessments.finalize (delegação): basta uma das duas.
    await setAssistant(admin.agent, [{ permission: 'assessments.finalize', granted: true }]);
    expect(await statuses()).toEqual([200, 200, 201, 200]);

    // Sem patients.read: tudo 403, e o 403 não traz dado da avaliação.
    await setAssistant(admin.agent, [
      { permission: 'assessments.finalize', granted: false },
      { permission: 'assessments.draft', granted: true },
      { permission: 'patients.read', granted: false },
    ]);
    const denied = await assistant.agent.get(`/api/assessments/${id}`);
    expect(await statuses()).toEqual([403, 403, 403, 403]);
    expect(denied.body).not.toHaveProperty('inputs');
    expect(JSON.stringify(denied.body)).not.toContain('Observação sintética');
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `Set-Location apps/api; npx jest test/assessments.e2e-spec --runInBand; Set-Location ../..`
Expected: FAIL. As rotas não existem (404 em `POST /api/patients/:id/assessments`).

- [ ] **Step 3: Implementar o serviço**

`apps/api/src/assessments/assessments.service.ts` passa a ser:

```ts
import { randomUUID } from 'node:crypto';
import { BadRequestException, ConflictException, Inject, Injectable, NotFoundException } from '@nestjs/common';
import type { Assessment, Patient, Prisma } from '@prisma/client';
import { AuditService } from '../audit/audit.service';
import type { AuthContext } from '../auth/auth-context';
import type { AssessmentCalculation } from '../clinical-rules/assessment-calculation';
import { CURRENT_AVALIACAO_RULES } from '../clinical-rules/avaliacao-rules';
import { formatIsoDate, todayInTimeZone } from '../common/civil-date';
import { APP_CONFIG, AppConfig } from '../config';
import { PrismaService } from '../prisma/prisma.service';
import { evaluateAssessment, previewErrors, type AssessmentEvaluation } from './assessment-evaluation';
import type { AssessmentFieldErrors, AssessmentInput } from './assessment-normalize';
import { assessmentChanges, toPrismaData, toRecordData } from './assessment-record';
import { toAssessmentRulesView, type AssessmentRulesView } from './assessment-rules.view';
import { seriesState, toAssessmentList, toAssessmentView, type AssessmentListItem, type AssessmentView } from './assessment.view';
import type { UpdateAssessmentDto } from './dto/assessment.dto';

export interface AssessmentCalculationResult {
  calculation: AssessmentCalculation | null;
  errors: AssessmentFieldErrors;
}

const NOT_FOUND = 'Avaliação não encontrada.';
const PATIENT_NOT_FOUND = 'Paciente não encontrado.';
const CONFLICT_MESSAGE = 'Esta avaliação foi alterada por outra pessoa. Recarregue para ver a versão atual.';
const FINALIZED_MESSAGE = 'Esta avaliação já foi finalizada e não pode ser alterada.';

@Injectable()
export class AssessmentsService {
  constructor(
    private readonly prisma: PrismaService,
    private readonly audit: AuditService,
    @Inject(APP_CONFIG) private readonly config: AppConfig,
  ) {}

  rules(): AssessmentRulesView {
    return toAssessmentRulesView(CURRENT_AVALIACAO_RULES);
  }

  /** Prévia sem gravação: valida contra o cadastro (idade na data, DIV-15) e calcula o que os campos válidos permitem. */
  async calculate(patientId: string, input: AssessmentInput): Promise<AssessmentCalculationResult> {
    const evaluation = this.evaluate(input, await this.patientOrThrow(this.prisma, patientId));
    return { calculation: evaluation.calculation, errors: previewErrors(evaluation) };
  }

  /** Uma linha por série (rascunho da revisão 1 ou vigente), da data mais recente para a mais antiga. */
  async list(patientId: string): Promise<AssessmentListItem[]> {
    await this.patientOrThrow(this.prisma, patientId);
    const rows = await this.prisma.assessment.findMany({
      where: { patientId },
      orderBy: [{ assessedOn: 'desc' }, { createdAt: 'desc' }, { id: 'asc' }],
    });
    return toAssessmentList(rows);
  }

  async get(id: string): Promise<AssessmentView> {
    const row = await this.prisma.assessment.findUnique({ where: { id } });
    if (!row) throw new NotFoundException(NOT_FOUND);
    return this.view(this.prisma, row);
  }

  /** Revisão 1 de uma série nova (seriesId = id), com o protocolo escolhido (P4-3) e a versão corrente das regras. */
  async create(patientId: string, body: AssessmentInput, actorId: string): Promise<AssessmentView> {
    return this.prisma.$transaction(async (tx) => {
      const patient = await this.patientOrThrow(tx, patientId);
      const evaluation = this.evaluate(body, patient);
      this.throwIfErrors(evaluation.fieldErrors);
      const id = randomUUID();
      const row = await tx.assessment.create({
        data: {
          id, seriesId: id, revision: 1, patientId, ...toPrismaData(toRecordData(evaluation, patient.sex, CURRENT_AVALIACAO_RULES)),
          createdById: actorId, updatedById: actorId,
        },
      });
      await this.audit.record(
        { actorId, action: 'assessment.create', entityType: 'assessment', entityId: id, data: { patientId, seriesId: id, revision: 1, protocol: row.protocol } },
        tx,
      );
      return toAssessmentView(row, seriesState([row]));
    });
  }

  update(id: string, body: UpdateAssessmentDto, auth: AuthContext): Promise<AssessmentView> {
    return this.save(id, body, auth);
  }

  /**
   * Grava o rascunho lido na versão `body.version`. O protocolo é o da linha (P4-3). Sexo, idade e cálculo vêm do cadastro
   * atual e da versão corrente das regras (P4-5). A finalizada nunca muda: o updateMany só casa com RASCUNHO na versão lida.
   */
  private save(id: string, body: UpdateAssessmentDto, auth: AuthContext): Promise<AssessmentView> {
    return this.prisma.$transaction(async (tx) => {
      const before = await tx.assessment.findUnique({ where: { id } });
      if (!before) throw new NotFoundException(NOT_FOUND);
      if (before.status === 'FINALIZADA') throw await this.conflict(tx, FINALIZED_MESSAGE, before);
      if (before.version !== body.version) throw await this.conflict(tx, CONFLICT_MESSAGE, before);
      const patient = await this.patientOrThrow(tx, before.patientId);
      const evaluation = this.evaluate({ ...body, protocol: before.protocol }, patient);
      this.throwIfErrors(evaluation.fieldErrors);
      const data = toRecordData(evaluation, patient.sex, CURRENT_AVALIACAO_RULES);

      const result = await tx.assessment.updateMany({
        where: { id, version: body.version, status: 'RASCUNHO' },
        data: { ...toPrismaData(data), updatedById: auth.user.id, version: { increment: 1 } },
      });
      if (result.count === 0) throw await this.lostRace(tx, id, FINALIZED_MESSAGE);
      const after = await tx.assessment.findUniqueOrThrow({ where: { id } });
      await this.audit.record(
        {
          actorId: auth.user.id, action: 'assessment.update', entityType: 'assessment', entityId: id,
          data: { patientId: before.patientId, seriesId: before.seriesId, revision: before.revision, changes: assessmentChanges(before, after) },
        },
        tx,
      );
      return this.view(tx, after);
    });
  }

  /** A escrita condicional não casou: outra transação gravou, finalizou ou excluiu antes. */
  private async lostRace(tx: Prisma.TransactionClient, id: string, finalizedMessage: string): Promise<Error> {
    const current = await tx.assessment.findUnique({ where: { id } });
    if (!current) return new NotFoundException(NOT_FOUND);
    return this.conflict(tx, current.status === 'FINALIZADA' ? finalizedMessage : CONFLICT_MESSAGE, current);
  }

  private async conflict(db: Prisma.TransactionClient, message: string, current: Assessment): Promise<ConflictException> {
    return new ConflictException({ message, current: await this.view(db, current) });
  }

  private async view(db: Prisma.TransactionClient, row: Assessment): Promise<AssessmentView> {
    const series = await db.assessment.findMany({
      where: { seriesId: row.seriesId },
      select: { id: true, revision: true, status: true, supersededAt: true },
    });
    return toAssessmentView(row, seriesState(series));
  }

  private async patientOrThrow(db: Prisma.TransactionClient, id: string): Promise<Patient> {
    const patient = await db.patient.findUnique({ where: { id } });
    if (!patient) throw new NotFoundException(PATIENT_NOT_FOUND);
    return patient;
  }

  private evaluate(input: AssessmentInput, patient: Patient): AssessmentEvaluation {
    return evaluateAssessment(
      input,
      { sex: patient.sex, birthDate: formatIsoDate(patient.birthDate) },
      CURRENT_AVALIACAO_RULES,
      todayInTimeZone(this.config.clinicTimeZone),
    );
  }

  private throwIfErrors(errors: AssessmentFieldErrors): void {
    if (Object.keys(errors).length > 0) throw new BadRequestException({ message: 'Dados inválidos.', errors });
  }
}
```

- [ ] **Step 4: Implementar as rotas**

Em `apps/api/src/assessments/assessments.controller.ts`:

- imports: `Put` de `@nestjs/common`; `type AuthContext` de `../auth/auth-context`; `CurrentAuth` de `../auth/current-auth.decorator`; `type AssessmentListItem` e `type AssessmentView` de `./assessment.view`; `AssessmentBodyDto` e `UpdateAssessmentDto` de `./dto/assessment.dto`;
- o comentário de `DRAFT_OR_FINALIZE` passa a: `/** Basta uma das duas; as permissões de anamnese não valem aqui. Rotas com dados do paciente exigem também patients.read (inclusive o 409). */`;
- depois de `calculate`, mantendo `rules` como a primeira rota (`assessments/rules` precisa casar antes de `assessments/:id`), acrescente:

```ts
  @Get('patients/:patientId/assessments')
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  list(@Param('patientId', new ParseUUIDPipe()) patientId: string): Promise<AssessmentListItem[]> {
    return this.assessments.list(patientId);
  }

  @Post('patients/:patientId/assessments')
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  create(
    @Param('patientId', new ParseUUIDPipe()) patientId: string,
    @Body() body: AssessmentBodyDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<AssessmentView> {
    return this.assessments.create(patientId, body, auth.user.id);
  }

  @Get('assessments/:id')
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  get(@Param('id', new ParseUUIDPipe()) id: string): Promise<AssessmentView> {
    return this.assessments.get(id);
  }

  @Put('assessments/:id')
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  update(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Body() body: UpdateAssessmentDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<AssessmentView> {
    return this.assessments.update(id, body, auth);
  }
```

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS com 52 suítes; `typecheck` sem erros.

- [ ] **Step 6: Commit**

```powershell
git add apps/api/src/assessments/assessments.service.ts apps/api/src/assessments/assessments.controller.ts apps/api/test/assessments.e2e-spec.ts
git commit -m "feat(api): rascunho das avaliações com versão das regras, cálculo gravado, 409 e permissões"
```

- [ ] **Step 7: PONTO DE CONTROLE DA PESSOA — gravação da A**

Mostre à pessoa:

1. A saída de `npx jest test/assessments.e2e-spec --verbose` (em `apps/api`).
2. A tabela abaixo, que liga cada coluna da aba Avaliações que a A preenche ao que a web grava. `inputs` são as entradas; `calculation`, o resultado gravado. Ela confere contra o primeiro teste (`inputsA` e o `calculation` esperado).

| Coluna (dicionario-dados) | Gravado na web |
|---|---|
| B Data, O Data Período, C Mês, D Ano | `assessedOn` (mês e ano saem da data) |
| E Id, F Nome, G Sexo, H Idade | `patientId`, `sex`, `ageYears` (idade na data, DIV-15) |
| I Faixa etária | `calculation.ageBand` |
| J Peso, K Altura | `inputs.weightKg`, `inputs.heightCm` |
| M Protocolo | `protocol` |
| N Observação | `inputs.observation` |
| P a AC (pescoço … panturrilha E) | `inputs.neckCm` … `inputs.calfLeftCm` (inclusive cintura e quadril) |
| AD IMC, AE Classificação IMC | `calculation.bmi`, `calculation.bmiClassification` |
| AF % Gordura, AG Kg Peso Gordo, AH Classificação Gordura | `inputs.fatPct`, `calculation.fatKg`, `calculation.fatClassification` |
| AI Taxa Muscular, AK Peso Muscular | `calculation.musclePct`, `calculation.muscleKg` |
| AM % MLG, AN Kg MLG | `calculation.fatFreePct`, `calculation.fatFreeKg` |
| AZ % Óssea, BA Peso Ósseo | `calculation.bonePct`, `calculation.boneKg` |
| BC Peso Residual, CE % Residual | `calculation.residualKg`, `calculation.residualPct` |
| CF Punho, CG Fêmur (mm) | `inputs.wristDiameterMm`, `inputs.femurDiameterMm` |
| BF RCQ, BG Classificação RCQ | `calculation.waistHipRatio`, `calculation.waistHipClassification` |
| BH % Gordura Alvo, BI IMC Alvo | `inputs.targetFatPct` e `inputs.targetBmi` (informados) e `calculation.targetFatPct` e `calculation.targetBmi` (efetivos, com as sugestões) |
| BJ Peso Alvo, BK Controle Peso | `calculation.targetWeightByFatKg` e `weightControlByFatKg`; `targetWeightByBmiKg` e `weightControlByBmiKg` |
| BL TMB, BN GET | `calculation.bmrKcal`, `calculation.expenditureKcal`; método e fator em `inputs.energyMethod` e `inputs.activityFactor` |
| CH Conclusão | `inputs.conclusion` |
| (sem coluna) | `rulesVersion`, `status`, `version`, autor e datas de criação e alteração |

A pessoa anota OK ou a divergência. Só siga para a Task 6 com o "ok".

---

### Task 6: Avaliação B — idade corporal e as quatro classificações

**Files:**
- Modify: `apps/api/src/assessments/assessment-normalize.ts`, `apps/api/src/assessments/assessment-normalize.spec.ts`
- Modify: `apps/api/src/assessments/dto/assessment.dto.ts`
- Create: `apps/api/test/assessments-b.e2e-spec.ts`

**Interfaces:**
- Consumes: as rotas da Task 5 e as opções da B (Task 1).
- Produces: o campo `bodyAgeYears` (B e C; limite `bodyAgeYears` da v2; mensagem `'A idade corporal deve ser maior que 0 e no máximo 150 anos, com até 2 casas decimais.'`).

- [ ] **Step 1: Escrever os testes**

Em `apps/api/src/assessments/assessment-normalize.spec.ts`, acrescente:

```ts
describe('idade corporal (B e C, P4-11)', () => {
  const MESSAGE = 'A idade corporal deve ser maior que 0 e no máximo 150 anos, com até 2 casas decimais.';

  it('aceita 0,01, 40,02 e 150; recusa 0 e 150,01', () => {
    for (const value of [0.01, 40.02, 150]) {
      expect(run({ protocol: 'B', assessedOn: '2026-05-09', bodyAgeYears: value }).values.numbers.bodyAgeYears).toBe(value);
      expect(run({ protocol: 'C', assessedOn: '2026-05-09', bodyAgeYears: value }).errors).toEqual({});
    }
    for (const value of [0, 150.01]) expect(run({ protocol: 'B', assessedOn: '2026-05-09', bodyAgeYears: value }).errors).toEqual({ bodyAgeYears: MESSAGE });
  });

  it('na A não se aplica, mesmo fora do limite', () => {
    expect(run({ protocol: 'A', assessedOn: '2026-05-09', bodyAgeYears: 0 }).errors).toEqual({ bodyAgeYears: 'Este campo não se aplica ao protocolo A.' });
  });
});
```

Crie `apps/api/test/assessments-b.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';

const mulher40 = { fullName: 'Paciente Sintética B', sex: 'FEMININO', birthDate: '1986-05-09' };
const homem35 = { fullName: 'Paciente Sintético B', sex: 'MASCULINO', birthDate: '1991-05-09' };
const avaliacaoB = {
  protocol: 'B', assessedOn: '2026-05-09', weightKg: 70, heightCm: 165, waistCm: 75, hipCm: 98, informedBmi: 25.7, fatPct: 30,
  skeletalMusclePct: 28, visceralFatLevel: 6, bodyAgeYears: 42, informedBmrKcal: 1350, energyMethod: 'BIOIMPEDANCIA', activityFactor: 1.2,
  targetFatPct: 28, targetBmi: 22.5, neckCm: 33, shouldersCm: 100, chestCm: 90, armRightCm: 28, armLeftCm: 27.5, forearmRightCm: 23,
  forearmLeftCm: 22.5, abdomenCm: 82, thighRightCm: 56, thighLeftCm: 55.5, calfRightCm: 35, calfLeftCm: 34.5,
  observation: 'Observação sintética da B.', conclusion: 'Conclusão sintética da B.',
};
const classificacoesB = {
  bmiClassification: '+ (Sobrepeso)', fatClassification: 'Normal (0)', skeletalMuscleClassification: 'Normal (0)', visceralFatClassification: '0 (Normal)',
};
const { protocol: _protocol, ...rascunhoB } = avaliacaoB;
const { assessedOn: _assessedOn, ...inputsB } = rascunhoB;
/** Grupos da B com as legendas do FrmBioimpR (P4-10), na ordem da tela. */
const B_GROUPS = [
  ['bmiClassification', ['- (Abaixo do peso)', '0 (Normal)', '+ (Sobrepeso)', '++ (Obeso)']],
  ['fatClassification', ['Baixo (-)', 'Normal (0)', 'Alto (+)', 'Muito Alto (++)']],
  ['skeletalMuscleClassification', ['Baixo (-)', 'Normal (0)', 'Alto (+)', 'Muito Alto (++)']],
  ['visceralFatClassification', ['0 (Normal)', '(+) Alto', '(++) Muito Alto']],
] as const;

describe('Avaliação B — gravação (HTTP)', () => {
  let app: INestApplication;
  let prisma: PrismaService;
  let agent: Agent;

  beforeAll(async () => {
    app = await createTestApp();
    prisma = app.get(PrismaService);
  });

  beforeEach(async () => {
    await resetDb(prisma);
    agent = (await loginAs(app, 'PROFISSIONAL')).agent;
  });

  afterAll(async () => {
    await app.close();
  });

  async function draftB(patient: Record<string, unknown> = mulher40, extra: Record<string, unknown> = classificacoesB) {
    const patientId = (await agent.post('/api/patients').send(patient)).body.id;
    const res = await agent.post(`/api/patients/${patientId}/assessments`).send({ ...avaliacaoB, ...extra });
    expect(res.status).toBe(201);
    return res.body;
  }

  it('grava todos os campos da B (P4-11, P4-12), as quatro classificações e os intervalos de referência', async () => {
    const created = await draftB();
    const read = (await agent.get(`/api/assessments/${created.id}`)).body;

    expect(read.inputs).toEqual(inputsB);
    expect(read.classifications).toEqual(classificacoesB);
    expect(read.energyMethodLabel).toBe('Calculado pela Bioimpedância');
    expect(read.calculation).toMatchObject({
      protocol: 'B', ageYears: 40, bmi: 25.7, bmiCalculated: 25.71, bmiClassification: null, bmiReference: '18,5 - 24,9', fatKg: 21,
      fatClassification: null, fatReference: '23 - 33,9', fatFreeKg: 49, fatFreePct: 70, skeletalMuscleKg: 19.6,
      skeletalMuscleReference: '24,1 - 30,1', visceralFatReference: '<= 9', waistHipRatio: 0.77, waistHipClassification: 'Risco Moderado',
      waistHipReference: '0,73 - 0,79', targetWeightByFatKg: 68.06, weightControlByFatKg: -1.94, targetWeightByBmiKg: 61.26,
      weightControlByBmiKg: -8.74, energyMethod: 'BIOIMPEDANCIA', bmrKcal: 1350, expenditureKcal: 1620,
    });
  });

  it.each(B_GROUPS)('cada opção de %s substitui a anterior, é gravada e reaberta', async (field, texts) => {
    const created = await draftB(mulher40, { ...classificacoesB, [field]: texts[texts.length - 1] });
    let version = created.version;

    for (const text of texts) {
      const saved = await agent.put(`/api/assessments/${created.id}`).send({ ...rascunhoB, ...classificacoesB, [field]: text, version });
      expect(saved.status).toBe(200);
      version = saved.body.version;
      expect((await agent.get(`/api/assessments/${created.id}`)).body.classifications[field]).toBe(text);
    }
  });

  it('limpar a opção grava null; legenda da C e grupo só da C são recusados no campo', async () => {
    const created = await draftB();

    const cleared = await agent.put(`/api/assessments/${created.id}`).send({ ...rascunhoB, ...classificacoesB, fatClassification: null, version: 1 });
    const wrong = await agent
      .put(`/api/assessments/${created.id}`)
      .send({ ...rascunhoB, bmiClassification: 'Saudável', waterClassification: 'Alto', version: 2 });

    expect(cleared.body.classifications).toEqual({ ...classificacoesB, fatClassification: null });
    expect(wrong.status).toBe(400);
    expect(wrong.body.errors).toEqual({
      bmiClassification: 'Opção de classificação desconhecida.',
      waterClassification: 'Este campo não se aplica ao protocolo B.',
    });
  });

  it('os intervalos variam com sexo e idade: masculino de 35 anos', async () => {
    const created = await draftB(homem35);

    expect(created.calculation).toMatchObject({
      ageYears: 35, fatReference: '8 - 19,9', skeletalMuscleReference: '33,3 - 39,3', waistHipRatio: 0.77,
      waistHipClassification: 'Risco Baixo', waistHipReference: '0,84 - 0,91', bmiReference: '18,5 - 24,9', visceralFatReference: '<= 9',
    });
  });

  it('campos da A e da C na B são recusados no campo; idade corporal fora do limite também', async () => {
    const patientId = (await agent.post('/api/patients').send(mulher40)).body.id;

    const res = await agent
      .post(`/api/patients/${patientId}/assessments`)
      .send({ ...avaliacaoB, wristDiameterMm: 50, fatKg: 10, bodyAgeYears: 150.01 });

    expect(res.status).toBe(400);
    expect(res.body.errors).toEqual({
      wristDiameterMm: 'Este campo não se aplica ao protocolo B.',
      fatKg: 'Este campo não se aplica ao protocolo B.',
      bodyAgeYears: 'A idade corporal deve ser maior que 0 e no máximo 150 anos, com até 2 casas decimais.',
    });
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `Set-Location apps/api; npx jest src/assessments test/assessments-b.e2e-spec --runInBand; Set-Location ../..`
Expected: FAIL. `bodyAgeYears` não é campo (400 do `ValidationPipe` no e2e; `values.numbers.bodyAgeYears` indefinido no teste da normalização). O teste do DTO ainda passa, porque `NUMERIC_FIELDS` ainda não tem o campo.

- [ ] **Step 3: Implementar**

Em `NUMERIC_FIELDS`, depois das circunferências:

```ts
  // B e C: idade corporal do aparelho (FrmBioimpR e FrmBioimpC, coluna BD).
  { field: 'bodyAgeYears', label: 'A idade corporal', limit: 'bodyAgeYears', unit: ' anos', protocols: ['B', 'C'] },
```

Em `AssessmentFieldsDto`, depois de `calfLeftCm`: `@IsOptional() @IsNumber(FINITE) bodyAgeYears?: number | null;`.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api`
Expected: PASS com 53 suítes.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/assessments/assessment-normalize.ts apps/api/src/assessments/assessment-normalize.spec.ts apps/api/src/assessments/dto/assessment.dto.ts apps/api/test/assessments-b.e2e-spec.ts
git commit -m "feat(api): gravação da avaliação B com idade corporal e as classificações do FrmBioimpR"
```

- [ ] **Step 6: PONTO DE CONTROLE DA PESSOA — gravação da B**

Mostre à pessoa a saída de `npx jest test/assessments-b.e2e-spec --verbose` e a tabela abaixo. Ela confere contra o primeiro teste (`inputsB`, `classificacoesB` e o `calculation` esperado).

| Coluna (dicionario-dados, gravação B/R) ou campo do FrmBioimpR | Gravado na web |
|---|---|
| B, O, C, D (datas); E a H (paciente, sexo, idade); I (faixa) | `assessedOn`; `patientId`, `sex`, `ageYears`; `calculation.ageBand` |
| J Peso, K Altura, M Protocolo, N Observação, CH Conclusão | `inputs.weightKg`, `inputs.heightCm`, `protocol`, `inputs.observation`, `inputs.conclusion` |
| P a AC (circunferências) | `inputs.neckCm` … `inputs.calfLeftCm` |
| AD IMC (informado no FrmBioimpR) | `inputs.informedBmi` (= `calculation.bmi`); o calculado ao lado em `calculation.bmiCalculated` |
| AE Classificação IMC | `classifications.bmiClassification` (legendas do FrmBioimpR) |
| AF % Gordura, AG Kg Peso Gordo, AH Classificação Gordura | `inputs.fatPct`, `calculation.fatKg`, `classifications.fatClassification` |
| AM % MLG, AN Kg MLG | `calculation.fatFreePct`, `calculation.fatFreeKg` |
| AO % Músculo Esquelético, AP Peso, AQ Classificação | `inputs.skeletalMusclePct`, `calculation.skeletalMuscleKg`, `classifications.skeletalMuscleClassification` |
| AR Nível Gordura Visceral, AS Classificação | `inputs.visceralFatLevel`, `classifications.visceralFatClassification` |
| BD Idade Corporal (FrmBioimpR "Idade corporal") | `inputs.bodyAgeYears` |
| Metabolismo basal do FrmBioimpR ("TBM") | `inputs.informedBmrKcal`; com o método "Calculado pela Bioimpedância", vira `calculation.bmrKcal` (coluna BL) |
| BF RCQ, BG Classificação RCQ | `calculation.waistHipRatio`, `calculation.waistHipClassification` |
| BH a BK (metas), BN GET | como na A |
| BO a BS (intervalos de IMC, gordura, músculo, visceral e RCQ) | `calculation.bmiReference`, `fatReference`, `skeletalMuscleReference`, `visceralFatReference`, `waistHipReference` |

A pessoa anota OK ou a divergência. Só siga para a Task 7 com o "ok".

---

### Task 7: Avaliação C — campos do FrmBioimpC, pontuação, hora e as 13 classificações (H-16)

**Files:**
- Modify: `apps/api/src/assessments/assessment-normalize.ts`, `apps/api/src/assessments/assessment-normalize.spec.ts`
- Modify: `apps/api/src/assessments/assessment-record.ts` (hora da medição no registro da C)
- Modify: `apps/api/src/assessments/dto/assessment.dto.ts`
- Create: `apps/api/test/assessments-c.e2e-spec.ts`

**Interfaces:**
- Consumes: Tasks 1 a 6.
- Produces:
  - campos da C em `NUMERIC_FIELDS`:
    - `fatFreeKg`, `subcutaneousFatPct` e `bodyScore`;
    - `fat{ArmRight|ArmLeft|Trunk|LegRight|LegLeft}RefPct` e `…Kg`;
    - `z20{…}Ohm` e `z100{…}Ohm`;
    - `muscle{…}RefPct` e `…Kg`;
  - `visceralFatLevel`, que passa a valer em B e C;
  - `measuredTime` (só C; `HH:MM`), em `AssessmentInput`, `AssessmentValues` e `AssessmentInputs`;
  - `rangeMessage` com o fim `', sem casas decimais.'` para limites inteiros.

- [ ] **Step 1: Escrever os testes**

Em `apps/api/src/assessments/assessment-normalize.spec.ts`, acrescente:

```ts
describe('campos da C (P4-9)', () => {
  const at = (input: AssessmentInput) => run({ protocol: 'C', assessedOn: '2026-05-09', ...input });

  it('pontuação: inteiro de 0 a 999, sem teto clínico (DIV-14, PC-13)', () => {
    for (const value of [0, 120, 999]) expect(at({ bodyScore: value }).values.numbers.bodyScore).toBe(value);
    for (const value of [-1, 1000, 120.5]) {
      expect(at({ bodyScore: value }).errors).toEqual({ bodyScore: 'A pontuação deve estar entre 0 e 999, sem casas decimais.' });
    }
  });

  it('impedâncias: inteiro maior que 0 e até 2000 Ω', () => {
    for (const value of [1, 2000]) expect(at({ z20ArmRightOhm: value, z100LegLeftOhm: value }).errors).toEqual({});
    for (const value of [0, 2001, 450.5]) {
      expect(at({ z20ArmRightOhm: value, z100LegLeftOhm: value }).errors).toEqual({
        z20ArmRightOhm: 'A impedância Z20 do braço direito deve ser maior que 0 e no máximo 2000 Ω, sem casas decimais.',
        z100LegLeftOhm: 'A impedância Z100 da perna esquerda deve ser maior que 0 e no máximo 2000 Ω, sem casas decimais.',
      });
    }
  });

  it('% do padrão por segmento: maior que 0 e até 1000 (passa de 100)', () => {
    for (const value of [0.07, 110.4, 1000]) expect(at({ fatTrunkRefPct: value, muscleLegLeftRefPct: value }).errors).toEqual({});
    for (const value of [0, 1000.01]) {
      expect(at({ fatTrunkRefPct: value, muscleLegLeftRefPct: value }).errors).toEqual({
        fatTrunkRefPct: 'O percentual de gordura do tronco deve ser maior que 0 e no máximo 1000, com até 2 casas decimais.',
        muscleLegLeftRefPct: 'O percentual de músculo da perna esquerda deve ser maior que 0 e no máximo 1000, com até 2 casas decimais.',
      });
    }
  });

  it('kg por segmento e massa livre: maior que 0 e até 500 kg', () => {
    for (const value of [0.07, 500]) expect(at({ fatArmLeftKg: value, muscleTrunkKg: value, fatFreeKg: value }).errors).toEqual({});
    expect(at({ fatArmLeftKg: 500.01, muscleTrunkKg: 0, fatFreeKg: 500.01 }).errors).toEqual({
      fatArmLeftKg: 'A gordura do braço esquerdo deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
      muscleTrunkKg: 'O músculo do tronco deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
      fatFreeKg: 'A massa livre de gordura deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
    });
  });

  it('gordura subcutânea: maior que 0 e menor que 100', () => {
    expect(at({ subcutaneousFatPct: 99.99 }).errors).toEqual({});
    expect(at({ subcutaneousFatPct: 100 }).errors).toEqual({
      subcutaneousFatPct: 'O percentual de gordura subcutânea deve ser maior que 0 e menor que 100, com até 2 casas decimais.',
    });
  });

  it('nível de gordura visceral também na C; na A não se aplica', () => {
    expect(at({ visceralFatLevel: 7 }).values.numbers.visceralFatLevel).toBe(7);
    expect(run({ protocol: 'A', assessedOn: '2026-05-09', visceralFatLevel: 7 }).errors).toEqual({
      visceralFatLevel: 'Este campo não se aplica ao protocolo A.',
    });
  });

  it('hora da medição: só na C, HH:MM de 00:00 a 23:59; vazio é null', () => {
    for (const time of ['00:00', '08:30', '23:59']) expect(at({ measuredTime: time }).values.measuredTime).toBe(time);
    for (const time of ['24:00', '7:30', '08:60', '0830']) {
      expect(at({ measuredTime: time }).errors).toEqual({ measuredTime: 'Informe a hora no formato HH:MM, de 00:00 a 23:59.' });
    }
    expect(at({ measuredTime: '  ' }).values.measuredTime).toBeNull();
    expect(run({ protocol: 'B', assessedOn: '2026-05-09', measuredTime: '08:30' }).errors).toEqual({ measuredTime: 'Este campo não se aplica ao protocolo B.' });
  });

  it('campos da C na A e na B não se aplicam, mesmo fora do limite', () => {
    expect(run({ protocol: 'B', assessedOn: '2026-05-09', bodyScore: 120.5, fatFreeKg: 50 }).errors).toEqual({
      bodyScore: 'Este campo não se aplica ao protocolo B.',
      fatFreeKg: 'Este campo não se aplica ao protocolo B.',
    });
    expect(run({ protocol: 'A', assessedOn: '2026-05-09', z20ArmRightOhm: 0 }).errors).toEqual({ z20ArmRightOhm: 'Este campo não se aplica ao protocolo A.' });
  });

  it('protocolo inválido com campos da C: só o erro do protocolo; os números válidos ficam', () => {
    const { values, errors } = run({ protocol: 'D', assessedOn: '2026-05-09', bodyScore: 120, z20TrunkOhm: 25, measuredTime: '08:30' });
    expect(errors).toEqual({ protocol: 'Escolha o protocolo A, B ou C.' });
    expect(values.numbers).toMatchObject({ bodyScore: 120, z20TrunkOhm: 25 });
  });

  it('rangeMessage de limites inteiros', () => {
    expect(rangeMessage('A pontuação', RULES.limits.bodyScore, '')).toBe('A pontuação deve estar entre 0 e 999, sem casas decimais.');
    expect(rangeMessage('A impedância Z20 do tronco', RULES.recordLimits.impedanceOhm, ' Ω')).toBe(
      'A impedância Z20 do tronco deve ser maior que 0 e no máximo 2000 Ω, sem casas decimais.',
    );
  });
});
```

Crie `apps/api/test/assessments-c.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';

const homem35 = { fullName: 'Paciente Sintético C', sex: 'MASCULINO', birthDate: '1991-05-09' };
const avaliacaoC = {
  protocol: 'C', assessedOn: '2026-05-09', measuredTime: '08:30', weightKg: 70, heightCm: 175, waistCm: 82, hipCm: 96, informedBmi: 22.9,
  fatKg: 14, fatFreeKg: 56, muscleKg: 52.9, skeletalMuscleKg: 30.2, boneKg: 3.1, waterKg: 41.3, proteinKg: 11.7, visceralFatLevel: 7,
  subcutaneousFatPct: 15.4, bodyAgeYears: 33, bodyScore: 120, informedBmrKcal: 1610, energyMethod: 'BIOIMPEDANCIA', activityFactor: 1.55,
  targetFatPct: 19, targetBmi: 22.5, neckCm: 38, shouldersCm: 118, chestCm: 100, armRightCm: 33, armLeftCm: 32.5, forearmRightCm: 28,
  forearmLeftCm: 27.5, abdomenCm: 86, thighRightCm: 57, thighLeftCm: 56.5, calfRightCm: 38, calfLeftCm: 37.5,
  fatArmRightRefPct: 95.5, fatArmRightKg: 0.8, fatArmLeftRefPct: 97.2, fatArmLeftKg: 0.82, fatTrunkRefPct: 110.4, fatTrunkKg: 7.1,
  fatLegRightRefPct: 88.3, fatLegRightKg: 2.2, fatLegLeftRefPct: 89.1, fatLegLeftKg: 2.21,
  z20ArmRightOhm: 310, z20ArmLeftOhm: 315, z20TrunkOhm: 25, z20LegRightOhm: 240, z20LegLeftOhm: 245,
  z100ArmRightOhm: 280, z100ArmLeftOhm: 284, z100TrunkOhm: 21, z100LegRightOhm: 215, z100LegLeftOhm: 219,
  muscleArmRightRefPct: 102.3, muscleArmRightKg: 3.1, muscleArmLeftRefPct: 101.8, muscleArmLeftKg: 3.05, muscleTrunkRefPct: 104.6,
  muscleTrunkKg: 25.4, muscleLegRightRefPct: 99.7, muscleLegRightKg: 9.3, muscleLegLeftRefPct: 99.1, muscleLegLeftKg: 9.25,
  observation: 'Observação sintética da C.', conclusion: 'Conclusão sintética da C.',
};
/** As 13 classificações, com as 5 opções que o legado não transfere ou só a Opt3Alto tinha (Alto e Excelente). */
const classificacoesC = {
  bmiClassification: 'Saudável', fatClassification: 'Saudável', musclePctClassification: 'Excelente', muscleKgClassification: 'Alto',
  skeletalMuscleClassification: 'Alto', visceralFatClassification: 'Baixo Risco', subcutaneousFatClassification: 'Médio',
  waterClassification: 'Excelente', proteinClassification: 'Alto', boneClassification: 'Alto', bodyAgeClassification: 'Excelente',
  bmrClassification: 'Excelente', totalWeightClassification: 'Saudável',
};
const { protocol: _protocol, ...rascunhoC } = avaliacaoC;
const { assessedOn: _assessedOn, ...inputsC } = rascunhoC;
/** H-16: todas as 45 opções do FrmBioimpC, por grupo, com as legendas conferidas no Excel. */
const C_GROUPS = [
  ['bmiClassification', ['Baixo peso', 'Saudável', 'Médio', 'Alto ou Muito Alto']],
  ['fatClassification', ['Baixo', 'Saudável', 'Excelente', 'Alto']],
  ['musclePctClassification', ['Baixo', 'Médio', 'Excelente']],
  ['muscleKgClassification', ['Baixo', 'Saudável', 'Excelente', 'Alto']],
  ['skeletalMuscleClassification', ['Baixo', 'Saudável', 'Excelente', 'Alto']],
  ['visceralFatClassification', ['Baixo Risco', 'Médio Risco', 'Alto Risco']],
  ['subcutaneousFatClassification', ['Baixo', 'Médio', 'Sobrepeso']],
  ['waterClassification', ['Baixo', 'Saudável', 'Excelente', 'Alto']],
  ['proteinClassification', ['Baixo', 'Saudável', 'Excelente', 'Alto']],
  ['boneClassification', ['Baixo', 'Saudável', 'Excelente', 'Alto']],
  ['bodyAgeClassification', ['Excelente', 'Sobrepeso']],
  ['bmrClassification', ['Baixo', 'Excelente']],
  ['totalWeightClassification', ['Baixo', 'Saudável', 'Excelente', 'Alto']],
] as const;

describe('Avaliação C — gravação (HTTP)', () => {
  let app: INestApplication;
  let prisma: PrismaService;
  let agent: Agent;
  let patientId: string;

  beforeAll(async () => {
    app = await createTestApp();
    prisma = app.get(PrismaService);
  });

  beforeEach(async () => {
    await resetDb(prisma);
    agent = (await loginAs(app, 'PROFISSIONAL')).agent;
    patientId = (await agent.post('/api/patients').send(homem35)).body.id;
  });

  afterAll(async () => {
    await app.close();
  });

  const create = (body: Record<string, unknown>) => agent.post(`/api/patients/${patientId}/assessments`).send(body);

  it('grava tudo o que o FrmBioimpC transfere (P4-9), a pontuação e as 13 classificações', async () => {
    const created = await create({ ...avaliacaoC, ...classificacoesC });
    const read = (await agent.get(`/api/assessments/${created.body.id}`)).body;

    expect(created.status).toBe(201);
    expect(read.inputs).toEqual(inputsC);
    expect(read.classifications).toEqual(classificacoesC);
    expect(read.calculation).toMatchObject({
      protocol: 'C', ageYears: 35, totalWeightKg: 70, totalWeightPct: 100, bmi: 22.9, bmiCalculated: 22.86, fatKg: 14, fatPct: 20,
      boneKg: 3.1, bonePct: 4.43, proteinKg: 11.7, proteinPct: 16.71, waterKg: 41.3, waterPct: 59, muscleKg: 52.9, musclePct: 75.57,
      skeletalMuscleKg: 30.2, skeletalMusclePct: 43.14, fatFreeKg: null, waistHipRatio: 0.85, waistHipClassification: 'Risco Moderado',
      targetWeightByFatKg: 69.14, weightControlByFatKg: -0.86, targetWeightByBmiKg: 68.91, weightControlByBmiKg: -1.09, bmrKcal: 1610,
      expenditureKcal: 2495.5,
    });
  });

  it.each(C_GROUPS)('H-16: cada opção de %s substitui a anterior, é gravada e reaberta', async (field, texts) => {
    const created = await create({ ...avaliacaoC, ...classificacoesC, [field]: texts[texts.length - 1] });
    let version = created.body.version;

    for (const text of texts) {
      const saved = await agent.put(`/api/assessments/${created.body.id}`).send({ ...rascunhoC, ...classificacoesC, [field]: text, version });
      expect(saved.status).toBe(200);
      version = saved.body.version;
      expect((await agent.get(`/api/assessments/${created.body.id}`)).body.classifications[field]).toBe(text);
    }
  });

  it('pontuação acima de 100 é aceita (DIV-14); fora de 0 a 999, com decimais, hora inválida e legenda da B são recusadas', async () => {
    const res = await create({ ...avaliacaoC, bodyScore: 1000, z20TrunkOhm: 25.5, measuredTime: '24:00', visceralFatClassification: '0 (Normal)' });

    expect(res.status).toBe(400);
    expect(res.body.errors).toEqual({
      bodyScore: 'A pontuação deve estar entre 0 e 999, sem casas decimais.',
      z20TrunkOhm: 'A impedância Z20 do tronco deve ser maior que 0 e no máximo 2000 Ω, sem casas decimais.',
      measuredTime: 'Informe a hora no formato HH:MM, de 00:00 a 23:59.',
      visceralFatClassification: 'Opção de classificação desconhecida.',
    });
  });

  it('a C não aceita campos da A nem da B (DIV-11)', async () => {
    const res = await create({ ...avaliacaoC, fatPct: 20, skeletalMusclePct: 30, wristDiameterMm: 50 });

    expect(res.status).toBe(400);
    expect(res.body.errors).toEqual({
      fatPct: 'Este campo não se aplica ao protocolo C.',
      skeletalMusclePct: 'Este campo não se aplica ao protocolo C.',
      wristDiameterMm: 'Este campo não se aplica ao protocolo C.',
    });
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `Set-Location apps/api; npx jest src/assessments test/assessments-c.e2e-spec --runInBand; Set-Location ../..`
Expected: FAIL. Os campos da C não existem (400 do `ValidationPipe`). `values.measuredTime` é indefinido. A mensagem de inteiro ainda termina em "com até 2 casas decimais".

- [ ] **Step 3: Implementar**

Em `assessment-normalize.ts`:

1. `visceralFatLevel` passa a `protocols: ['B', 'C']`, com o comentário `// B e C (a C grava AR, P4-9).` na linha anterior.
2. Acrescente ao fim de `NUMERIC_FIELDS`:

```ts
  // C: o que o FrmBioimpC transfere (P4-9), digitado à mão até o OCR (Plano 6).
  { field: 'fatFreeKg', label: 'A massa livre de gordura', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'subcutaneousFatPct', label: 'O percentual de gordura subcutânea', limit: 'percent', unit: '', protocols: ['C'] },
  { field: 'bodyScore', label: 'A pontuação', limit: 'bodyScore', unit: '', protocols: ['C'] },
  // Gordura por segmento (BU a CD): % do padrão, não do peso (PDF-10), e kg.
  { field: 'fatArmRightRefPct', label: 'O percentual de gordura do braço direito', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'fatArmRightKg', label: 'A gordura do braço direito', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'fatArmLeftRefPct', label: 'O percentual de gordura do braço esquerdo', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'fatArmLeftKg', label: 'A gordura do braço esquerdo', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'fatTrunkRefPct', label: 'O percentual de gordura do tronco', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'fatTrunkKg', label: 'A gordura do tronco', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'fatLegRightRefPct', label: 'O percentual de gordura da perna direita', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'fatLegRightKg', label: 'A gordura da perna direita', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'fatLegLeftRefPct', label: 'O percentual de gordura da perna esquerda', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'fatLegLeftKg', label: 'A gordura da perna esquerda', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  // Impedâncias (CJ a CS): inteiros, como o Format "#,##0" do FrmBioimpC.
  { field: 'z20ArmRightOhm', label: 'A impedância Z20 do braço direito', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  { field: 'z20ArmLeftOhm', label: 'A impedância Z20 do braço esquerdo', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  { field: 'z20TrunkOhm', label: 'A impedância Z20 do tronco', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  { field: 'z20LegRightOhm', label: 'A impedância Z20 da perna direita', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  { field: 'z20LegLeftOhm', label: 'A impedância Z20 da perna esquerda', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  { field: 'z100ArmRightOhm', label: 'A impedância Z100 do braço direito', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  { field: 'z100ArmLeftOhm', label: 'A impedância Z100 do braço esquerdo', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  { field: 'z100TrunkOhm', label: 'A impedância Z100 do tronco', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  { field: 'z100LegRightOhm', label: 'A impedância Z100 da perna direita', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  { field: 'z100LegLeftOhm', label: 'A impedância Z100 da perna esquerda', limit: 'impedanceOhm', unit: ' Ω', protocols: ['C'] },
  // Músculo por segmento (CT a DC): % do padrão e kg.
  { field: 'muscleArmRightRefPct', label: 'O percentual de músculo do braço direito', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'muscleArmRightKg', label: 'O músculo do braço direito', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'muscleArmLeftRefPct', label: 'O percentual de músculo do braço esquerdo', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'muscleArmLeftKg', label: 'O músculo do braço esquerdo', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'muscleTrunkRefPct', label: 'O percentual de músculo do tronco', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'muscleTrunkKg', label: 'O músculo do tronco', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'muscleLegRightRefPct', label: 'O percentual de músculo da perna direita', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'muscleLegRightKg', label: 'O músculo da perna direita', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'muscleLegLeftRefPct', label: 'O percentual de músculo da perna esquerda', limit: 'segmentReferencePct', unit: '', protocols: ['C'] },
  { field: 'muscleLegLeftKg', label: 'O músculo da perna esquerda', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
```

3. `AssessmentInput` ganha `measuredTime?: string | null` (no primeiro objeto do tipo). `AssessmentValues` ganha `measuredTime: string | null`. Em `normalizeAssessment`, `values` recebe `measuredTime: validTime(input.measuredTime, protocol, errors)`.
4. No laço numérico, a condição `!withinRange(...) || !hasUpToTwoDecimals(value)` vira `!validNumber(value, rangeOf(rules, limit))`. Acrescente:

```ts
/** Inteiro onde o limite exige (pontuação, impedâncias); nos demais, até 2 casas. */
function validNumber(value: number, range: Range): boolean {
  return withinRange(value, range) && (range.integer ? Number.isInteger(value) : hasUpToTwoDecimals(value));
}

const TIME = /^([01]\d|2[0-3]):[0-5]\d$/;

/** Hora da medição (coluna DD), só na C. Sem protocolo válido, confere só o formato. */
function validTime(value: string | null | undefined, protocol: AssessmentProtocol | null, errors: AssessmentFieldErrors): string | null {
  const text = cleanText(value);
  if (text === null) return null;
  if (protocol !== null && protocol !== 'C') {
    errors.measuredTime = notApplicable(protocol);
    return null;
  }
  if (!TIME.test(text)) {
    errors.measuredTime = 'Informe a hora no formato HH:MM, de 00:00 a 23:59.';
    return null;
  }
  return text;
}
```

5. A última linha de `rangeMessage` vira `return \`${label} ${rule}, ${range.integer ? 'sem casas decimais' : 'com até 2 casas decimais'}.\`;`.

Em `assessment-record.ts`:

- `AssessmentInputs` ganha `measuredTime?: string | null`;
- em `toRecordData`, `inputs` passa a `{ energyMethod: values.energyMethod, ...(protocol === 'C' ? { measuredTime: values.measuredTime } : {}), ...Object.fromEntries(numbers), ...values.texts }`.

Em `AssessmentFieldsDto`:

- depois de `conclusion`, acrescente `@IsOptional() @IsString() @MaxLength(5) measuredTime?: string | null;`;
- depois de `bodyAgeYears`, acrescente uma linha `@IsOptional() @IsNumber(FINITE) <campo>?: number | null;` para cada um dos 33 campos novos, na ordem de `NUMERIC_FIELDS`: `fatFreeKg`, `subcutaneousFatPct`, `bodyScore`, os 10 de gordura, os 10 de impedância e os 10 de músculo.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS com 54 suítes. O teste do DTO cobre os 33 campos novos. `typecheck` sem erros.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/assessments/assessment-normalize.ts apps/api/src/assessments/assessment-normalize.spec.ts apps/api/src/assessments/assessment-record.ts apps/api/src/assessments/dto/assessment.dto.ts apps/api/test/assessments-c.e2e-spec.ts
git commit -m "feat(api): gravação da avaliação C com campos do FrmBioimpC, pontuação, hora e as 13 classificações (H-16)"
```

- [ ] **Step 6: PONTO DE CONTROLE DA PESSOA — gravação da C**

Mostre à pessoa a saída de `npx jest test/assessments-c.e2e-spec --verbose`, com os 13 testes H-16, e a tabela abaixo. Ela confere contra o primeiro teste (`inputsC`, `classificacoesC` e o `calculation` esperado).

| Coluna (dicionario-dados, gravação C) | Gravado na web |
|---|---|
| B, O, C, D (datas); DD (hora, sem cabeçalho) | `assessedOn`; `inputs.measuredTime` |
| E a I (paciente, sexo, idade, faixa) | `patientId`, `sex`, `ageYears`, `calculation.ageBand` |
| J Peso, K Altura, M, N, CH; P a AC | como na A |
| AD IMC; AE Classificação | `inputs.informedBmi` (calculado em `calculation.bmiCalculated`); `classifications.bmiClassification` |
| AF % Gordura; AG Kg; AH Classificação | `calculation.fatPct` (derivado do kg); `inputs.fatKg`; `classifications.fatClassification` |
| AI Taxa Muscular; AJ Classificação | `calculation.musclePct`; `classifications.musclePctClassification` |
| AK Peso Muscular; AL Classificação | `inputs.muscleKg`; `classifications.muscleKgClassification` |
| AN Kg MLG (informado no FrmBioimpC) | `inputs.fatFreeKg` |
| AO % Músc. Esq.; AP Peso; AQ Classificação | `calculation.skeletalMusclePct`; `inputs.skeletalMuscleKg`; `classifications.skeletalMuscleClassification` |
| AR Nível Visceral; AS Classificação | `inputs.visceralFatLevel`; `classifications.visceralFatClassification` |
| AT % Subcutânea; AU Classificação | `inputs.subcutaneousFatPct`; `classifications.subcutaneousFatClassification` |
| AV Água (%); AW Classificação | `calculation.waterPct` (o kg informado em `inputs.waterKg`, DIV-10); `classifications.waterClassification` |
| AX Proteína (%); AY Classificação | `calculation.proteinPct` (o kg em `inputs.proteinKg`); `classifications.proteinClassification` |
| AZ % Óssea; BA Peso Ósseo; BB Classificação | `calculation.bonePct`; `inputs.boneKg`; `classifications.boneClassification` |
| BD Idade Corporal; BE Classificação | `inputs.bodyAgeYears`; `classifications.bodyAgeClassification` |
| BF RCQ, BG Classificação; BH a BK metas; BN GET | como na A |
| BL TMB; BM Classificação TMB | `calculation.bmrKcal` (a TMB do aparelho em `inputs.informedBmrKcal`); `classifications.bmrClassification` |
| BT Peso Total Classificação | `classifications.totalWeightClassification` (peso total 100% em `calculation.totalWeightPct`) |
| BU a CD (gordura por segmento, % do padrão e kg) | `inputs.fatArmRightRefPct` … `inputs.fatLegLeftKg` |
| CI Pontuação | `inputs.bodyScore` (no legado provavelmente nunca chega à CI; ver a nova DIV) |
| CJ a CS (Z20 e Z100) | `inputs.z20ArmRightOhm` … `inputs.z100LegLeftOhm` |
| CT a DC (músculo por segmento) | `inputs.muscleArmRightRefPct` … `inputs.muscleLegLeftKg` |

A pessoa anota OK ou a divergência. Só siga para a Task 8 com o "ok".

---

### Task 8: Finalização, congelamento e exclusão do rascunho

**Files:**
- Modify: `apps/api/src/assessments/assessments.service.ts`, `apps/api/src/assessments/assessments.controller.ts`, `apps/api/src/assessments/dto/assessment.dto.ts`
- Create: `apps/api/test/assessments-finalize.e2e-spec.ts`

**Interfaces:**
- Consumes: `finalizationErrors` (Task 3) e o serviço da Task 5.
- Produces:
  - `finalize(id: string, body: UpdateAssessmentDto, auth: AuthContext): Promise<AssessmentView>`;
  - `remove(id: string, version: number, auth: AuthContext): Promise<void>`;
  - `save(id, body, auth, mode: 'update' | 'finalize')`;
  - `DeleteAssessmentQueryDto { version: number }`;
  - as rotas `POST /api/assessments/:id/finalize` (200) e `DELETE /api/assessments/:id?version=N` (204).

- [ ] **Step 1: Escrever os testes**

Crie `apps/api/test/assessments-finalize.e2e-spec.ts`:

```ts
import { randomUUID } from 'node:crypto';
import { INestApplication } from '@nestjs/common';
import request from 'supertest';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';
import { runPaused } from './helpers/run-paused';

const mulher = { fullName: 'Paciente Sintética Finalização', sex: 'FEMININO', birthDate: '1986-05-09' };
const avaliacaoA = {
  protocol: 'A', assessedOn: '2026-05-09', weightKg: 80, heightCm: 170, waistCm: 80, hipCm: 100, fatPct: 20, wristDiameterMm: 50,
  femurDiameterMm: 90, energyMethod: 'HARRIS_BENEDICT', activityFactor: 1.55, neckCm: 34,
};
const { protocol: _protocol, ...rascunhoA } = avaliacaoA;
const FINALIZED = 'Esta avaliação já foi finalizada e não pode ser alterada.';
const FINALIZED_DELETE = 'Avaliação finalizada não pode ser excluída.';
const CONFLICT = 'Esta avaliação foi alterada por outra pessoa. Recarregue para ver a versão atual.';
const INVALID_BMR = 'A TMB calculada não é válida; confira o método escolhido e os dados.';

describe('Avaliações — finalização e exclusão (HTTP)', () => {
  let app: INestApplication;
  let prisma: PrismaService;

  beforeAll(async () => {
    app = await createTestApp();
    prisma = app.get(PrismaService);
  });

  beforeEach(() => resetDb(prisma));

  afterAll(async () => {
    await app.close();
  });

  async function draftFor(agent: Agent, body: Record<string, unknown> = avaliacaoA, patient: Record<string, unknown> = mulher) {
    const patientId = (await agent.post('/api/patients').send(patient)).body.id as string;
    const res = await agent.post(`/api/patients/${patientId}/assessments`).send(body);
    expect(res.status).toBe(201);
    return { patientId, id: res.body.id as string };
  }

  const rowOf = (id: string) => prisma.assessment.findUniqueOrThrow({ where: { id } });

  it('assistente sem assessments.finalize recebe 403 e nada muda', async () => {
    const assistant = await loginAs(app, 'ASSISTENTE');
    const { id } = await draftFor(assistant.agent);

    const res = await assistant.agent.post(`/api/assessments/${id}/finalize`).send({ ...rascunhoA, version: 1 });

    expect(res.status).toBe(403);
    expect(await rowOf(id)).toMatchObject({ status: 'RASCUNHO', version: 1, finalizedAt: null });
  });

  it('profissional finaliza: grava o que enviou, congela e audita', async () => {
    const { agent, user } = await loginAs(app, 'PROFISSIONAL');
    const { patientId, id } = await draftFor(agent);

    const res = await agent.post(`/api/assessments/${id}/finalize`).send({ ...rascunhoA, neckCm: 35, version: 1 });

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({ status: 'FINALIZADA', version: 2, revision: 1, currentRevisionId: id, inputs: { neckCm: 35 } });
    expect(res.body.calculation).toMatchObject({ rulesVersion: 'avaliacao-v2', bmi: 27.68, bmrKcal: 1548 });
    expect(res.body.finalizedAt).toEqual(expect.any(String));
    expect(await rowOf(id)).toMatchObject({ finalizedById: user.id });
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'assessment.finalize' } });
    expect(event).toMatchObject({ actorId: user.id, entityType: 'assessment', entityId: id });
    expect(event.data).toEqual({
      patientId, seriesId: id, revision: 1,
      changes: { status: { from: 'RASCUNHO', to: 'FINALIZADA' }, 'inputs.neckCm': { from: 34, to: 35 } },
    });
  });

  it.each([
    ['A', { ...avaliacaoA, fatPct: null }, { fatPct: 'Informe o percentual de gordura para finalizar.' }],
    ['B', { protocol: 'B', assessedOn: '2026-05-09', weightKg: 70, heightCm: 165, fatPct: 30 }, { informedBmi: 'Informe o IMC do aparelho para finalizar.' }],
    ['C', { protocol: 'C', assessedOn: '2026-05-09', weightKg: 70, heightCm: 175 }, { fatKg: 'Informe o peso de gordura para finalizar.' }],
    ['A sem peso e altura', { ...avaliacaoA, weightKg: null, heightCm: null }, { weightKg: 'Informe o peso para finalizar.', heightCm: 'Informe a altura para finalizar.' }],
  ])('P4-2 (%s): falta obrigatório → 400 no campo e continua rascunho', async (_label, body, errors) => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { id } = await draftFor(agent, body);
    const { protocol: _p, ...fields } = body;

    const res = await agent.post(`/api/assessments/${id}/finalize`).send({ ...fields, version: 1 });

    expect(res.status).toBe(400);
    expect(res.body.message).toBe('Dados inválidos.');
    expect(res.body.errors).toEqual(errors);
    expect(await rowOf(id)).toMatchObject({ status: 'RASCUNHO', version: 1 });
  });

  it('a TMB calculada não válida: o rascunho grava, a finalização recusa', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const body = { protocol: 'A', assessedOn: '2026-05-09', weightKg: 51, heightCm: 170, fatPct: 20, energyMethod: 'DRI_ADOLESCENTES' };
    const { id } = await draftFor(agent, body, { fullName: 'Paciente Sintético Idoso', sex: 'MASCULINO', birthDate: '1929-05-09' });
    const { protocol: _p, ...fields } = body;

    const draft = await agent.put(`/api/assessments/${id}`).send({ ...fields, version: 1 });
    const finalize = await agent.post(`/api/assessments/${id}/finalize`).send({ ...fields, version: 2 });

    expect(draft.status).toBe(200);
    expect(finalize.status).toBe(400);
    expect(finalize.body.errors).toEqual({ energyMethod: INVALID_BMR });
    expect(await rowOf(id)).toMatchObject({ status: 'RASCUNHO', version: 2 });
  });

  it('finalizada: PUT, nova finalização e exclusão recebem 409 com a avaliação atual e não gravam nada', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { id } = await draftFor(agent);
    expect((await agent.post(`/api/assessments/${id}/finalize`).send({ ...rascunhoA, version: 1 })).status).toBe(200);

    const responses = [
      await agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, neckCm: 40, version: 2 }),
      await agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, neckCm: 40, version: 1 }),
      await agent.post(`/api/assessments/${id}/finalize`).send({ ...rascunhoA, version: 2 }),
    ];
    const deleted = await agent.delete(`/api/assessments/${id}?version=2`);

    for (const res of responses) {
      expect(res.status).toBe(409);
      expect(res.body.message).toBe(FINALIZED);
      expect(res.body.current).toMatchObject({ status: 'FINALIZADA', version: 2, inputs: { neckCm: 34 } });
    }
    expect(deleted.status).toBe(409);
    expect(deleted.body.message).toBe(FINALIZED_DELETE);
    expect(await prisma.auditEvent.count({ where: { action: { in: ['assessment.update', 'assessment.delete'] } } })).toBe(0);
    expect(await prisma.auditEvent.count({ where: { action: 'assessment.finalize' } })).toBe(1);
  });

  it('idade e sexo são refeitos a cada rascunho e congelados ao finalizar (H-20)', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { patientId, id } = await draftFor(agent);
    expect((await agent.put(`/api/patients/${patientId}`).send({ ...mulher, birthDate: '1990-05-10', version: 1 })).status).toBe(200);

    const redraft = await agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, version: 1 });
    const done = await agent.post(`/api/assessments/${id}/finalize`).send({ ...rascunhoA, version: 2 });
    expect((await agent.put(`/api/patients/${patientId}`).send({ ...mulher, sex: 'MASCULINO', birthDate: '2000-01-01', version: 2 })).status).toBe(200);

    expect(redraft.body).toMatchObject({ ageYears: 35, version: 2 });
    expect(done.status).toBe(200);
    expect((await agent.get(`/api/assessments/${id}`)).body).toMatchObject({ ageYears: 35, sex: 'FEMININO', status: 'FINALIZADA', version: 3 });
  });

  it('delegação: assistente com assessments.finalize concedida finaliza', async () => {
    const assistant = await loginAs(app, 'ASSISTENTE');
    const { id } = await draftFor(assistant.agent);
    const admin = await loginAs(app, 'ADMIN');
    await admin.agent.put('/api/permissions/assistant').send({ changes: [{ permission: 'assessments.finalize', granted: true }] });

    const res = await assistant.agent.post(`/api/assessments/${id}/finalize`).send({ ...rascunhoA, version: 1 });

    expect(res.status).toBe(200);
    expect(res.body.status).toBe('FINALIZADA');
  });

  it('rascunho salvo enquanto outra pessoa finaliza: 409 de finalizada, sem gravar (H-19)', async () => {
    const professional = await loginAs(app, 'PROFISSIONAL');
    const { id } = await draftFor(professional.agent);
    const assistant = await loginAs(app, 'ASSISTENTE');

    const [finalized, late] = await runPaused(
      app,
      (input) => input.action === 'assessment.finalize',
      () => professional.agent.post(`/api/assessments/${id}/finalize`).send({ ...rascunhoA, version: 1 }),
      () => assistant.agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, neckCm: 40, version: 1 }),
    );

    expect([finalized.status, late.status]).toEqual([200, 409]);
    expect(late.body.message).toBe(FINALIZED);
    expect(late.body.current).toMatchObject({ status: 'FINALIZADA', version: 2, inputs: { neckCm: 34 } });
    expect(await prisma.auditEvent.count({ where: { action: 'assessment.update' } })).toBe(0);
  });

  describe('exclusão do rascunho (P4-7)', () => {
    it('assistente exclui o rascunho com a versão lida; a exclusão é auditada', async () => {
      const { agent, user } = await loginAs(app, 'ASSISTENTE');
      const { patientId, id } = await draftFor(agent);

      const res = await agent.delete(`/api/assessments/${id}?version=1`);

      expect(res.status).toBe(204);
      expect(await prisma.assessment.findUnique({ where: { id } })).toBeNull();
      expect((await agent.get(`/api/patients/${patientId}/assessments`)).body).toEqual([]);
      const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'assessment.delete' } });
      expect(event).toMatchObject({ actorId: user.id, entityType: 'assessment', entityId: id });
      expect(event.data).toEqual({ patientId, seriesId: id, revision: 1, protocol: 'A', assessedOn: '2026-05-09' });
    });

    it('versão antiga: 409 com a atual, sem excluir; sem versão ou versão inválida: 400; inexistente: 404', async () => {
      const { agent } = await loginAs(app, 'PROFISSIONAL');
      const { id } = await draftFor(agent);
      expect((await agent.put(`/api/assessments/${id}`).send({ ...rascunhoA, neckCm: 40, version: 1 })).status).toBe(200);

      const stale = await agent.delete(`/api/assessments/${id}?version=1`);

      expect(stale.status).toBe(409);
      expect(stale.body.message).toBe(CONFLICT);
      expect(stale.body.current).toMatchObject({ version: 2, inputs: { neckCm: 40 } });
      expect((await agent.delete(`/api/assessments/${id}`)).status).toBe(400);
      expect((await agent.delete(`/api/assessments/${id}?version=abc`)).status).toBe(400);
      expect((await agent.delete(`/api/assessments/${randomUUID()}?version=1`)).status).toBe(404);
      expect(await prisma.assessment.count()).toBe(1);
    });

    it('permissões: sem sessão 401; sem draft e sem finalize 403; só finalize basta', async () => {
      const professional = await loginAs(app, 'PROFISSIONAL');
      const { id } = await draftFor(professional.agent);
      const { id: other } = await draftFor(professional.agent);
      const assistant = await loginAs(app, 'ASSISTENTE');
      const admin = await loginAs(app, 'ADMIN');

      expect((await request(app.getHttpServer()).delete(`/api/assessments/${id}?version=1`)).status).toBe(401);
      await admin.agent.put('/api/permissions/assistant').send({ changes: [{ permission: 'assessments.draft', granted: false }] });
      expect((await assistant.agent.delete(`/api/assessments/${id}?version=1`)).status).toBe(403);
      await admin.agent.put('/api/permissions/assistant').send({ changes: [{ permission: 'assessments.finalize', granted: true }] });
      expect((await assistant.agent.delete(`/api/assessments/${other}?version=1`)).status).toBe(204);
      expect(await prisma.assessment.count()).toBe(1);
    });
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `Set-Location apps/api; npx jest test/assessments-finalize.e2e-spec --runInBand; Set-Location ../..`
Expected: FAIL. As rotas `POST /api/assessments/:id/finalize` e `DELETE /api/assessments/:id` não existem (404).

- [ ] **Step 3: Implementar**

Em `dto/assessment.dto.ts` (import `Type` de `class-transformer`):

```ts
/** Exclusão do rascunho: a versão lida vai na query (?version=N). */
export class DeleteAssessmentQueryDto {
  @Type(() => Number) @IsInt() @Min(1) version!: number;
}
```

Em `assessments.service.ts`:

1. Constante nova: `const FINALIZED_DELETE_MESSAGE = 'Avaliação finalizada não pode ser excluída.';`. Import de `finalizationErrors`.
2. `update` passa a chamar `this.save(id, body, auth, 'update')`. Acrescente `finalize(id, body, auth)`, que chama `this.save(id, body, auth, 'finalize')`.
3. `save` ganha o parâmetro `mode: 'update' | 'finalize'`, e o comentário passa a dizer "Grava o rascunho lido na versão `body.version`, ou grava e finaliza." No corpo:
   - a validação vira `this.throwIfErrors(mode === 'finalize' ? finalizationErrors(evaluation, CURRENT_AVALIACAO_RULES) : evaluation.fieldErrors);`;
   - antes do `updateMany`: `const finalizing = mode === 'finalize' ? { status: 'FINALIZADA' as const, finalizedAt: new Date(), finalizedById: auth.user.id } : {};`;
   - o `data` do `updateMany` passa a `{ ...toPrismaData(data), ...finalizing, updatedById: auth.user.id, version: { increment: 1 } }`;
   - a ação da auditoria passa a `` `assessment.${mode}` ``.
4. Acrescente:

```ts
  /** Exclui o rascunho lido na versão `version` (P4-7); a finalizada nunca é excluída. */
  async remove(id: string, version: number, auth: AuthContext): Promise<void> {
    await this.prisma.$transaction(async (tx) => {
      const row = await tx.assessment.findUnique({ where: { id } });
      if (!row) throw new NotFoundException(NOT_FOUND);
      if (row.status === 'FINALIZADA') throw await this.conflict(tx, FINALIZED_DELETE_MESSAGE, row);
      if (row.version !== version) throw await this.conflict(tx, CONFLICT_MESSAGE, row);
      const result = await tx.assessment.deleteMany({ where: { id, version, status: 'RASCUNHO' } });
      if (result.count === 0) throw await this.lostRace(tx, id, FINALIZED_DELETE_MESSAGE);
      await this.audit.record(
        {
          actorId: auth.user.id, action: 'assessment.delete', entityType: 'assessment', entityId: id,
          data: { patientId: row.patientId, seriesId: row.seriesId, revision: row.revision, protocol: row.protocol, assessedOn: formatIsoDate(row.assessedOn) },
        },
        tx,
      );
    });
  }
```

Em `assessments.controller.ts` (imports `Delete` e `Query` de `@nestjs/common`, e `DeleteAssessmentQueryDto`), depois de `update`:

```ts
  @Post('assessments/:id/finalize')
  @HttpCode(200)
  @RequirePermissions('patients.read', 'assessments.finalize')
  finalize(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Body() body: UpdateAssessmentDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<AssessmentView> {
    return this.assessments.finalize(id, body, auth);
  }

  @Delete('assessments/:id')
  @HttpCode(204)
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  remove(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Query() query: DeleteAssessmentQueryDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<void> {
    return this.assessments.remove(id, query.version, auth);
  }
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS com 55 suítes.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/assessments/assessments.service.ts apps/api/src/assessments/assessments.controller.ts apps/api/src/assessments/dto/assessment.dto.ts apps/api/test/assessments-finalize.e2e-spec.ts
git commit -m "feat(api): finalização das avaliações com critérios por protocolo e exclusão auditada do rascunho"
```

---

### Task 9: Revisões das avaliações finalizadas

**Files:**
- Modify: `apps/api/src/assessments/assessments.service.ts`, `apps/api/src/assessments/assessments.controller.ts`, `apps/api/src/assessments/dto/assessment.dto.ts`
- Modify: `apps/api/src/assessments/assessment.view.ts`, `apps/api/src/assessments/assessment.view.spec.ts`
- Create: `apps/api/test/assessments-revisions.e2e-spec.ts`

**Interfaces:**
- Consumes: o serviço das Tasks 5 e 8; `assessmentChanges` (Task 4); `cleanMultiline`.
- Produces, em `assessment.view.ts`:

```ts
export interface AssessmentRevisionItem {
  id: string; revision: number; status: AssessmentStatus; assessedOn: string; rulesVersion: string;
  revisionReason: string | null; revisionChanges: FieldChanges | null;
  createdAt: string; createdByName: string | null; finalizedAt: string | null; finalizedByName: string | null; supersededAt: string | null;
}
export function toRevisionItem(row: Assessment, names: ReadonlyMap<string, string>): AssessmentRevisionItem;
```

- Produces, em `AssessmentsService`:
  - `get(id: string, auth: AuthContext)`: a substituída exige `history.read`;
  - `createRevision(id: string, reason: string, auth: AuthContext): Promise<AssessmentView>`;
  - `revisions(id: string): Promise<AssessmentRevisionItem[]>`;
  - os privados `assertCanEdit`, `finalizedConflict` e `supersede`.
- Produces, em `dto/assessment.dto.ts`: `CreateRevisionDto { reason: string }`, até 2000 caracteres.
- Produces, as rotas:
  - `POST /api/assessments/:id/revisions` (201), que exige `patients.read` e `assessments.finalize`;
  - `GET /api/assessments/:id/revisions`, que exige `patients.read`, `history.read` e mais `assessments.draft` ou `assessments.finalize`.

- [ ] **Step 1: Escrever os testes**

Em `apps/api/src/assessments/assessment.view.spec.ts`, acrescente o import de `toRevisionItem` e:

```ts
describe('toRevisionItem', () => {
  it('autores pelo nome; usuário sem nome conhecido fica null', () => {
    const names = new Map([['u1', 'Pessoa profissional']]);
    expect(
      toRevisionItem(
        row({ revision: 2, status: 'FINALIZADA', revisionReason: 'Peso errado.', revisionChanges: { 'inputs.weightKg': { from: 80, to: 81 } }, finalizedAt: SUPERSEDED, finalizedById: 'u9' }),
        names,
      ),
    ).toEqual({
      id: 'a1', revision: 2, status: 'FINALIZADA', assessedOn: '2026-05-09', rulesVersion: 'avaliacao-v2', revisionReason: 'Peso errado.',
      revisionChanges: { 'inputs.weightKg': { from: 80, to: 81 } }, createdAt: '2026-05-09T13:00:00.000Z', createdByName: 'Pessoa profissional',
      finalizedAt: '2026-05-10T12:00:00.000Z', finalizedByName: null, supersededAt: null,
    });
  });
});
```

Crie `apps/api/test/assessments-revisions.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';
import { runPaused } from './helpers/run-paused';

const mulher = { fullName: 'Paciente Sintética Revisão', sex: 'FEMININO', birthDate: '1986-05-09' };
const avaliacaoA = {
  protocol: 'A', assessedOn: '2026-05-09', weightKg: 80, heightCm: 170, waistCm: 80, hipCm: 100, fatPct: 20, wristDiameterMm: 50,
  femurDiameterMm: 90, energyMethod: 'HARRIS_BENEDICT', activityFactor: 1.55,
};
const { protocol: _protocol, ...rascunhoA } = avaliacaoA;
const REASON = 'Peso digitado errado.';
const NOT_FINALIZED = 'Só é possível revisar uma avaliação finalizada.';
const SUPERSEDED = 'Esta avaliação já foi substituída por uma revisão mais recente.';
const OPEN_REVISION = 'Já existe uma revisão em andamento desta avaliação.';

describe('Avaliações — revisões (HTTP)', () => {
  let app: INestApplication;
  let prisma: PrismaService;
  let professional: Agent;

  beforeAll(async () => {
    app = await createTestApp();
    prisma = app.get(PrismaService);
  });

  beforeEach(async () => {
    await resetDb(prisma);
    professional = (await loginAs(app, 'PROFISSIONAL')).agent;
  });

  afterAll(async () => {
    await app.close();
  });

  /** Avaliação A finalizada (revisão 1, versão 2). */
  async function finalizedA() {
    const patientId = (await professional.post('/api/patients').send(mulher)).body.id as string;
    const id = (await professional.post(`/api/patients/${patientId}/assessments`).send(avaliacaoA)).body.id as string;
    expect((await professional.post(`/api/assessments/${id}/finalize`).send({ ...rascunhoA, version: 1 })).status).toBe(200);
    return { patientId, id };
  }

  const revise = (agent: Agent, id: string, reason: unknown = REASON) => agent.post(`/api/assessments/${id}/revisions`).send({ reason });

  /** Revisão 2 com o peso corrigido, finalizada. */
  async function finalizedRevision(id: string): Promise<string> {
    const revisionId = (await revise(professional, id)).body.id as string;
    expect((await professional.post(`/api/assessments/${revisionId}/finalize`).send({ ...rascunhoA, weightKg: 81, version: 1 })).status).toBe(200);
    return revisionId;
  }

  it('nova revisão: copia a vigente como rascunho da revisão 2, com a justificativa sem espaços nas pontas, e audita', async () => {
    const { patientId, id } = await finalizedA();

    const res = await revise(professional, id, `  ${REASON}  `);

    expect(res.status).toBe(201);
    expect(res.body).toMatchObject({
      patientId, seriesId: id, revision: 2, status: 'RASCUNHO', protocol: 'A', assessedOn: '2026-05-09', rulesVersion: 'avaliacao-v2',
      revisionReason: REASON, revisionChanges: null, currentRevisionId: id, openRevisionId: res.body.id, version: 1, finalizedAt: null,
      inputs: { weightKg: 80, fatPct: 20 }, calculation: { bmi: 27.68 },
    });
    expect((await professional.get(`/api/assessments/${id}`)).body).toMatchObject({ status: 'FINALIZADA', currentRevisionId: id, openRevisionId: res.body.id });
    expect((await professional.get(`/api/patients/${patientId}/assessments`)).body).toEqual([
      expect.objectContaining({ id, revision: 1, status: 'FINALIZADA', openRevisionId: res.body.id }),
    ]);
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'assessment.revision.create' } });
    expect(event.data).toEqual({ patientId, seriesId: id, revision: 2, fromId: id, reason: REASON });
  });

  it('justificativa: obrigatória e até 2000 caracteres', async () => {
    const { id } = await finalizedA();

    const blank = await revise(professional, id, '   ');
    const tooLong = await revise(professional, id, 'x'.repeat(2001));
    const missing = await professional.post(`/api/assessments/${id}/revisions`).send({});
    const atLimit = await revise(professional, id, 'x'.repeat(2000));

    expect(blank.status).toBe(400);
    expect(blank.body.message).toBe('Dados inválidos.');
    expect(blank.body.errors).toEqual({ reason: 'Informe a justificativa da revisão.' });
    expect([tooLong.status, missing.status]).toEqual([400, 400]);
    expect(atLimit.status).toBe(201);
    expect(atLimit.body.revisionReason).toBe('x'.repeat(2000));
    expect(await prisma.assessment.count()).toBe(2);
  });

  it('só quem finaliza cria a revisão; quem só preenche rascunhos lê, mas não altera, finaliza nem descarta', async () => {
    const { id } = await finalizedA();
    const assistant = await loginAs(app, 'ASSISTENTE');
    expect((await revise(assistant.agent, id)).status).toBe(403);
    const revisionId = (await revise(professional, id)).body.id as string;

    const read = await assistant.agent.get(`/api/assessments/${revisionId}`);
    const put = await assistant.agent.put(`/api/assessments/${revisionId}`).send({ ...rascunhoA, weightKg: 81, version: 1 });
    const finalize = await assistant.agent.post(`/api/assessments/${revisionId}/finalize`).send({ ...rascunhoA, version: 1 });
    const discard = await assistant.agent.delete(`/api/assessments/${revisionId}?version=1`);

    expect([read.status, put.status, finalize.status, discard.status]).toEqual([200, 403, 403, 403]);
    expect(await prisma.assessment.findUniqueOrThrow({ where: { id: revisionId } })).toMatchObject({ status: 'RASCUNHO', version: 1 });
  });

  it('finalizar a revisão: ela vira a vigente, grava os campos alterados e a anterior fica substituída', async () => {
    const { patientId, id } = await finalizedA();
    const revisionId = (await revise(professional, id)).body.id as string;
    expect((await professional.put(`/api/assessments/${revisionId}`).send({ ...rascunhoA, weightKg: 81, version: 1 })).body.calculation).toMatchObject({ bmi: 28.03 });

    const res = await professional.post(`/api/assessments/${revisionId}/finalize`).send({ ...rascunhoA, weightKg: 81, version: 2 });

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({
      status: 'FINALIZADA', revision: 2, version: 3, currentRevisionId: revisionId, openRevisionId: null,
      revisionChanges: { 'inputs.weightKg': { from: 80, to: 81 } }, calculation: { bmi: 28.03, bmrKcal: 1557 },
    });
    const previous = await prisma.assessment.findUniqueOrThrow({ where: { id } });
    expect(previous).toMatchObject({ status: 'FINALIZADA', version: 3, supersededById: expect.any(String) });
    expect(previous.supersededAt).toBeInstanceOf(Date);
    expect(previous.inputs).toMatchObject({ weightKg: 80 });
    expect((await professional.get(`/api/assessments/${id}`)).body).toMatchObject({ supersededAt: expect.any(String), currentRevisionId: revisionId });
    expect((await professional.get(`/api/patients/${patientId}/assessments`)).body).toEqual([
      expect.objectContaining({ id: revisionId, seriesId: id, revision: 2, status: 'FINALIZADA', openRevisionId: null }),
    ]);
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'assessment.finalize', entityId: revisionId } });
    expect(event.data).toEqual({
      patientId, seriesId: id, revision: 2, changes: { status: { from: 'RASCUNHO', to: 'FINALIZADA' } }, supersededId: id,
    });
  });

  it('regras da nova revisão: rascunho não se revisa, uma revisão aberta por vez, a substituída leva à vigente', async () => {
    const { id } = await finalizedA();
    const patientId = (await prisma.assessment.findUniqueOrThrow({ where: { id } })).patientId;
    const draftId = (await professional.post(`/api/patients/${patientId}/assessments`).send(avaliacaoA)).body.id as string;

    const fromDraft = await revise(professional, draftId);
    const openId = (await revise(professional, id)).body.id as string;
    const second = await revise(professional, id);

    expect(fromDraft.status).toBe(409);
    expect(fromDraft.body).toMatchObject({ message: NOT_FINALIZED, current: { id: draftId, status: 'RASCUNHO' } });
    expect(second.status).toBe(409);
    expect(second.body).toMatchObject({ message: OPEN_REVISION, current: { id: openId, revision: 2, status: 'RASCUNHO' } });

    expect((await professional.post(`/api/assessments/${openId}/finalize`).send({ ...rascunhoA, weightKg: 81, version: 1 })).status).toBe(200);
    const fromSuperseded = await revise(professional, id);
    const third = await revise(professional, openId);

    expect(fromSuperseded.status).toBe(409);
    expect(fromSuperseded.body).toMatchObject({ message: SUPERSEDED, current: { id: openId, revision: 2 } });
    expect(third.status).toBe(201);
    expect(third.body).toMatchObject({ revision: 3, inputs: { weightKg: 81 } });
  });

  it('PUT, finalização e exclusão da substituída: 409 com a vigente, sem expor a substituída', async () => {
    const { id } = await finalizedA();
    const revisionId = await finalizedRevision(id);

    const responses = [
      await professional.put(`/api/assessments/${id}`).send({ ...rascunhoA, version: 3 }),
      await professional.post(`/api/assessments/${id}/finalize`).send({ ...rascunhoA, version: 3 }),
      await professional.delete(`/api/assessments/${id}?version=3`),
    ];

    for (const res of responses) {
      expect(res.status).toBe(409);
      expect(res.body.message).toBe(SUPERSEDED);
      expect(res.body.current).toMatchObject({ id: revisionId, supersededAt: null, inputs: { weightKg: 81 } });
    }
  });

  it('histórico (history.read): revisões da série, com autores, justificativa e campos alterados', async () => {
    const { id } = await finalizedA();
    const revisionId = await finalizedRevision(id);

    const res = await professional.get(`/api/assessments/${id}/revisions`);

    expect(res.status).toBe(200);
    expect(res.body).toEqual([
      {
        id: revisionId, revision: 2, status: 'FINALIZADA', assessedOn: '2026-05-09', rulesVersion: 'avaliacao-v2', revisionReason: REASON,
        revisionChanges: { 'inputs.weightKg': { from: 80, to: 81 } }, createdAt: expect.any(String), createdByName: 'Pessoa profissional',
        finalizedAt: expect.any(String), finalizedByName: 'Pessoa profissional', supersededAt: null,
      },
      {
        id, revision: 1, status: 'FINALIZADA', assessedOn: '2026-05-09', rulesVersion: 'avaliacao-v2', revisionReason: null, revisionChanges: null,
        createdAt: expect.any(String), createdByName: 'Pessoa profissional', finalizedAt: expect.any(String), finalizedByName: 'Pessoa profissional',
        supersededAt: expect.any(String),
      },
    ]);
  });

  it('history.read isolada: sem ela, nem a substituída nem as revisões; a vigente continua', async () => {
    const { id } = await finalizedA();
    const revisionId = await finalizedRevision(id);
    const assistant = await loginAs(app, 'ASSISTENTE');
    const admin = await loginAs(app, 'ADMIN');
    const statuses = async () => [
      (await assistant.agent.get(`/api/assessments/${id}`)).status,
      (await assistant.agent.get(`/api/assessments/${id}/revisions`)).status,
      (await assistant.agent.get(`/api/assessments/${revisionId}`)).status,
    ];

    // Padrão do assistente: patients.read, assessments.draft e history.read.
    expect(await statuses()).toEqual([200, 200, 200]);

    await admin.agent.put('/api/permissions/assistant').send({ changes: [{ permission: 'history.read', granted: false }] });
    const denied = await assistant.agent.get(`/api/assessments/${id}`);
    expect(await statuses()).toEqual([403, 403, 200]);
    expect(denied.body).not.toHaveProperty('inputs');

    // history.read sozinha não basta: sem permissão de avaliação, tudo 403.
    await admin.agent.put('/api/permissions/assistant').send({
      changes: [{ permission: 'history.read', granted: true }, { permission: 'assessments.draft', granted: false }],
    });
    expect(await statuses()).toEqual([403, 403, 403]);
  });

  it('descartar a revisão em rascunho: a vigente não muda; o descarte é auditado', async () => {
    const { patientId, id } = await finalizedA();
    const revisionId = (await revise(professional, id)).body.id as string;

    const res = await professional.delete(`/api/assessments/${revisionId}?version=1`);

    expect(res.status).toBe(204);
    expect(await prisma.assessment.findUnique({ where: { id: revisionId } })).toBeNull();
    expect(await prisma.assessment.findUniqueOrThrow({ where: { id } })).toMatchObject({ status: 'FINALIZADA', version: 2, supersededAt: null });
    expect((await professional.get(`/api/patients/${patientId}/assessments`)).body).toEqual([expect.objectContaining({ id, openRevisionId: null })]);
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'assessment.delete' } });
    expect(event.data).toEqual({ patientId, seriesId: id, revision: 2, protocol: 'A', assessedOn: '2026-05-09' });
  });

  it('duas novas revisões ao mesmo tempo: uma cria, a outra recebe 409 com a aberta (trava 7302004, H-19)', async () => {
    const { id } = await finalizedA();
    const other = (await loginAs(app, 'ADMIN')).agent;

    const [first, second] = await runPaused(
      app,
      (input) => input.action === 'assessment.revision.create',
      () => revise(professional, id),
      () => revise(other, id, 'Outra correção.'),
    );

    expect([first.status, second.status]).toEqual([201, 409]);
    expect(second.body).toMatchObject({ message: OPEN_REVISION, current: { id: first.body.id, revision: 2 } });
    expect(await prisma.assessment.count({ where: { seriesId: id } })).toBe(2);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `Set-Location apps/api; npx jest src/assessments/assessment.view test/assessments-revisions.e2e-spec --runInBand; Set-Location ../..`
Expected: FAIL. `toRevisionItem` não existe e as rotas de revisão respondem 404.

- [ ] **Step 3: Implementar a visão e o DTO**

Em `assessment.view.ts`, acrescente a interface `AssessmentRevisionItem` (de "Produces") e:

```ts
/** Linha do histórico de revisões (history.read): autores pelo nome de exibição. */
export function toRevisionItem(row: Assessment, names: ReadonlyMap<string, string>): AssessmentRevisionItem {
  return {
    id: row.id, revision: row.revision, status: row.status, assessedOn: formatIsoDate(row.assessedOn), rulesVersion: row.rulesVersion,
    revisionReason: row.revisionReason, revisionChanges: row.revisionChanges as unknown as FieldChanges | null,
    createdAt: row.createdAt.toISOString(), createdByName: names.get(row.createdById) ?? null,
    finalizedAt: row.finalizedAt?.toISOString() ?? null, finalizedByName: row.finalizedById ? (names.get(row.finalizedById) ?? null) : null,
    supersededAt: row.supersededAt?.toISOString() ?? null,
  };
}
```

Em `dto/assessment.dto.ts`:

```ts
/** Justificativa da nova revisão (P4-6). */
export class CreateRevisionDto {
  @IsString() @MaxLength(2000) reason!: string;
}
```

- [ ] **Step 4: Implementar o serviço e as rotas**

Em `assessments.service.ts`:

1. Imports: `ForbiddenException`; `cleanMultiline` de `../common/text`; `toRevisionItem` e `type AssessmentRevisionItem` de `./assessment.view`.
2. Constantes novas:

```ts
const FORBIDDEN_MESSAGE = 'Você não tem permissão para esta ação.';
const NOT_FINALIZED_MESSAGE = 'Só é possível revisar uma avaliação finalizada.';
const SUPERSEDED_MESSAGE = 'Esta avaliação já foi substituída por uma revisão mais recente.';
const OPEN_REVISION_MESSAGE = 'Já existe uma revisão em andamento desta avaliação.';
/** Chave fixa do pg_advisory_xact_lock da criação de revisões (7302001 a 7302003 já em uso; a próxima livre é 7302005). */
const REVISIONS_LOCK = 7_302_004;
```

3. `get` passa a `get(id: string, auth: AuthContext)`. Antes do `return`: `if (row.supersededAt !== null && !auth.permissions.includes('history.read')) throw new ForbiddenException(FORBIDDEN_MESSAGE);`.
4. Em `save`, logo depois do 404:
   - `this.assertCanEdit(before, auth);`;
   - a linha da finalizada passa a `if (before.status === 'FINALIZADA') throw await this.finalizedConflict(tx, before, FINALIZED_MESSAGE);`;
   - depois de ler `after`, troque `const after` por `let after` e acrescente:

```ts
      const superseded = mode === 'finalize' && before.revision > 1 ? await this.supersede(tx, after, auth.user.id) : null;
      if (superseded) {
        after = await tx.assessment.update({
          where: { id },
          data: { revisionChanges: assessmentChanges(superseded, after) as unknown as Prisma.InputJsonObject },
        });
      }
```

   - o `data` da auditoria passa a `{ patientId: before.patientId, seriesId: before.seriesId, revision: before.revision, changes: assessmentChanges(before, after), ...(superseded ? { supersededId: superseded.id } : {}) }`.
5. Em `remove`, logo depois do 404: `this.assertCanEdit(row, auth);`. A linha da finalizada passa a `throw await this.finalizedConflict(tx, row, FINALIZED_DELETE_MESSAGE)`.
6. Em `lostRace`, o caso finalizado passa a `return this.finalizedConflict(tx, current, finalizedMessage);` (o caso de rascunho continua `CONFLICT_MESSAGE`).
7. Novos métodos:

```ts
  /**
   * P4-6: rascunho da revisão seguinte, cópia literal da vigente (entradas, classificações, cálculo, versão das regras,
   * sexo e idade). O primeiro salvamento recalcula com a versão corrente. A trava garante uma revisão aberta por série.
   */
  async createRevision(id: string, reason: string, auth: AuthContext): Promise<AssessmentView> {
    const justification = cleanMultiline(reason);
    if (justification === null) {
      throw new BadRequestException({ message: 'Dados inválidos.', errors: { reason: 'Informe a justificativa da revisão.' } });
    }
    return this.prisma.$transaction(async (tx) => {
      await tx.$executeRaw`SELECT pg_advisory_xact_lock(${REVISIONS_LOCK})`;
      const from = await tx.assessment.findUnique({ where: { id } });
      if (!from) throw new NotFoundException(NOT_FOUND);
      if (from.status !== 'FINALIZADA') throw await this.conflict(tx, NOT_FINALIZED_MESSAGE, from);
      if (from.supersededAt !== null) throw await this.finalizedConflict(tx, from, SUPERSEDED_MESSAGE);
      const series = await tx.assessment.findMany({ where: { seriesId: from.seriesId }, orderBy: { revision: 'desc' } });
      const open = series.find((r) => r.status === 'RASCUNHO');
      if (open) throw await this.conflict(tx, OPEN_REVISION_MESSAGE, open);

      const revision = series[0].revision + 1;
      const created = await tx.assessment.create({
        data: {
          id: randomUUID(), patientId: from.patientId, seriesId: from.seriesId, revision, protocol: from.protocol, assessedOn: from.assessedOn,
          rulesVersion: from.rulesVersion, sex: from.sex, ageYears: from.ageYears,
          inputs: from.inputs as unknown as Prisma.InputJsonObject,
          classifications: from.classifications as unknown as Prisma.InputJsonObject,
          calculation: from.calculation as unknown as Prisma.InputJsonObject,
          revisionReason: justification,
          createdById: auth.user.id, updatedById: auth.user.id,
        },
      });
      await this.audit.record(
        {
          actorId: auth.user.id, action: 'assessment.revision.create', entityType: 'assessment', entityId: created.id,
          data: { patientId: from.patientId, seriesId: from.seriesId, revision, fromId: from.id, reason: justification },
        },
        tx,
      );
      return this.view(tx, created);
    });
  }

  /** Revisões da série, da mais nova para a mais antiga (history.read). */
  async revisions(id: string): Promise<AssessmentRevisionItem[]> {
    const row = await this.prisma.assessment.findUnique({ where: { id } });
    if (!row) throw new NotFoundException(NOT_FOUND);
    const rows = await this.prisma.assessment.findMany({ where: { seriesId: row.seriesId }, orderBy: { revision: 'desc' } });
    const ids = [...new Set(rows.flatMap((r) => [r.createdById, r.finalizedById]).filter((v): v is string => v !== null))];
    const users = await this.prisma.user.findMany({ where: { id: { in: ids } }, select: { id: true, displayName: true } });
    const names = new Map(users.map((u) => [u.id, u.displayName]));
    return rows.map((r) => toRevisionItem(r, names));
  }

  /** Revisão maior que 1: só quem finaliza a edita, finaliza ou descarta (resposta c de 2026-10-11). */
  private assertCanEdit(row: Assessment, auth: AuthContext): void {
    if (row.revision > 1 && !auth.permissions.includes('assessments.finalize')) throw new ForbiddenException(FORBIDDEN_MESSAGE);
  }

  /** Finalizada: 409 com ela; substituída: 409 com a vigente, para não expor a substituída a quem não tem history.read. */
  private async finalizedConflict(db: Prisma.TransactionClient, row: Assessment, message: string): Promise<ConflictException> {
    if (row.supersededAt === null) return this.conflict(db, message, row);
    const current = await db.assessment.findFirst({ where: { seriesId: row.seriesId, status: 'FINALIZADA', supersededAt: null } });
    return this.conflict(db, SUPERSEDED_MESSAGE, current ?? row);
  }

  /** A revisão finalizada vira a vigente; a anterior fica substituída, com autor e data. Devolve a anterior como era. */
  private async supersede(tx: Prisma.TransactionClient, revision: Assessment, actorId: string): Promise<Assessment | null> {
    const previous = await tx.assessment.findFirst({
      where: { seriesId: revision.seriesId, status: 'FINALIZADA', supersededAt: null, NOT: { id: revision.id } },
    });
    if (!previous) return null;
    await tx.assessment.update({
      where: { id: previous.id },
      data: { supersededAt: new Date(), supersededById: actorId, version: { increment: 1 } },
    });
    return previous;
  }
```

Em `assessments.controller.ts`:

- `get` recebe `@CurrentAuth() auth: AuthContext` e chama `this.assessments.get(id, auth)`;
- imports de `CreateRevisionDto` e `type AssessmentRevisionItem`;
- acrescente, depois de `remove`:

```ts
  @Post('assessments/:id/revisions')
  @RequirePermissions('patients.read', 'assessments.finalize')
  createRevision(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Body() body: CreateRevisionDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<AssessmentView> {
    return this.assessments.createRevision(id, body.reason, auth);
  }

  @Get('assessments/:id/revisions')
  @RequirePermissions('patients.read', 'history.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  revisions(@Param('id', new ParseUUIDPipe()) id: string): Promise<AssessmentRevisionItem[]> {
    return this.assessments.revisions(id);
  }
```

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS com 56 suítes. As e2e das Tasks 5 a 8 continuam verdes.

- [ ] **Step 6: Commit**

```powershell
git add apps/api/src/assessments/assessments.service.ts apps/api/src/assessments/assessments.controller.ts apps/api/src/assessments/dto/assessment.dto.ts apps/api/src/assessments/assessment.view.ts apps/api/src/assessments/assessment.view.spec.ts apps/api/test/assessments-revisions.e2e-spec.ts
git commit -m "feat(api): revisões das avaliações finalizadas com justificativa, campos alterados, history.read e trava 7302004"
```

- [ ] **Step 7: PONTO DE CONTROLE DA PESSOA — revisões**

Mostre à pessoa a saída de `npx jest test/assessments-revisions.e2e-spec test/assessments-finalize.e2e-spec --verbose` e confirme com ela, item por item:

1. Só quem tem "Finalizar avaliações" cria, altera, finaliza ou descarta uma revisão. A justificativa é obrigatória.
2. A revisão nasce como cópia da vigente. Ao ser finalizada, passa a valer, e a anterior fica substituída e consultável, com autor e data.
3. O histórico mostra, por revisão: autor, data, justificativa e campos alterados (ex.: `inputs.weightKg: 80 → 81`).
4. Sem "Consultar histórico", a pessoa vê a vigente, mas não as substituídas nem o histórico.
5. A finalizada e as revisões finalizadas nunca são excluídas. A revisão em rascunho pode ser descartada.

A pessoa anota OK ou a divergência. Só siga para a Task 10 com o "ok".

---
### Task 10: Web — tipos, avaliações na página do paciente, escolha do protocolo e 403 em cascata

**Files:**
- Create: `apps/web/src/assessments/types.ts`, `apps/web/src/assessments/PatientAssessmentsSection.tsx`, `apps/web/src/assessments/ProtocolPickerPage.tsx`
- Create: `apps/web/src/test/assessment-fixtures.ts`
- Modify: `apps/web/src/patients/PatientFormPage.tsx`, `apps/web/src/router.tsx`, `apps/web/src/lib/query-client.ts`
- Test: `apps/web/src/assessments/assessments-section.test.tsx`

**Interfaces:**
- Consumes: `GET /api/patients/:patientId/assessments` (Task 5); `api`, `errorMessage`, `formatDateBr`, `can`, `canAny`, `useCurrentUser` e os componentes de `components/ui`.
- Produces, em `types.ts`:
  - `ASSESSMENT_PERMISSIONS`, `Protocol`, `AssessmentStatus`, `FieldChanges`;
  - `PROTOCOLS`, `PROTOCOL_LABELS`, `PROTOCOL_DESCRIPTIONS`, `STATUS_LABELS` e `isProtocol`;
  - os tipos da API: `AssessmentCalculation`, `Assessment`, `AssessmentListItem`, `AssessmentRevisionItem`, `AssessmentCalculationResult`, `ClassificationGroup`, `Range` e `AssessmentRules`.
- Produces:
  - `PatientAssessmentsSection({ patientId })`, que mostra o aviso `state.assessmentNotice` da navegação;
  - `situation(item)`;
  - `ProtocolPickerRoute`, na rota `/pacientes/:patientId/avaliacoes/nova`;
  - em `test/assessment-fixtures.ts`: `patientWoman` (id `p1`), `assessmentList` e `mockAssessmentApi(routes)`, que a Task 11 amplia.

- [ ] **Step 1: Escrever os testes**

Crie `apps/web/src/test/assessment-fixtures.ts`:

```ts
import type { AssessmentListItem } from '../assessments/types';
import type { Patient } from '../patients/types';
import { professionalMe } from './fixtures';
import { mockApi } from './mock-api';

export const patientWoman: Patient = {
  id: 'p1', legacyId: null, fullName: 'Paciente Sintética Avaliação', sex: 'FEMININO', birthDate: '1986-05-09', profession: null, phone: null,
  mobile: null, email: null, notes: null, postalCode: null, street: null, streetNumber: null, addressComplement: null, district: null,
  city: null, state: null, rg: null, cpf: null, otherDocument: null, version: 1,
  createdAt: '2026-05-01T12:00:00.000Z', updatedAt: '2026-05-01T12:00:00.000Z',
};

export const assessmentList: AssessmentListItem[] = [
  { id: 'v3', seriesId: 'v3', revision: 1, protocol: 'C', assessedOn: '2026-06-01', status: 'RASCUNHO', finalizedAt: null, updatedAt: '2026-06-01T12:00:00.000Z', openRevisionId: null },
  { id: 'v2', seriesId: 'v1', revision: 2, protocol: 'A', assessedOn: '2026-05-09', status: 'FINALIZADA', finalizedAt: '2026-05-10T12:00:00.000Z', updatedAt: '2026-05-10T12:00:00.000Z', openRevisionId: null },
  { id: 'v4', seriesId: 'v4', revision: 1, protocol: 'B', assessedOn: '2026-04-01', status: 'FINALIZADA', finalizedAt: '2026-04-01T15:00:00.000Z', updatedAt: '2026-04-01T15:00:00.000Z', openRevisionId: 'v5' },
];

/** API mínima das telas de avaliação (profissional, paciente p1); `routes` sobrepõe. A Task 11 acrescenta regras e prévia. */
export function mockAssessmentApi(routes: Parameters<typeof mockApi>[0] = {}) {
  return mockApi({
    'GET /auth/me': { body: professionalMe },
    'GET /patients/p1': { body: patientWoman },
    'GET /patients/p1/anamneses': { body: [] },
    'GET /patients/p1/assessments': { body: [] },
    ...routes,
  });
}
```

Crie `apps/web/src/assessments/assessments-section.test.tsx`:

```tsx
import { act, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { Me } from '../auth/me';
import { assessmentList, mockAssessmentApi, patientWoman } from '../test/assessment-fixtures';
import { assistantMe } from '../test/fixtures';
import type { MockResponse } from '../test/mock-api';
import { renderApp } from '../test/render';
import { situation } from './PatientAssessmentsSection';

const FORBIDDEN: MockResponse = { status: 403, body: { statusCode: 403, message: 'Você não tem permissão para esta ação.' } };
const draftAssistantMe: Me = { ...assistantMe, permissions: ['patients.read', 'patients.write', 'assessments.draft'] };

describe('Avaliações na página do paciente', () => {
  it('lista na ordem da API, com data, protocolo, situação, link e Nova avaliação', async () => {
    mockAssessmentApi({ 'GET /patients/p1/assessments': { body: assessmentList } });
    renderApp('/pacientes/p1');

    const section = await screen.findByRole('region', { name: 'Avaliações' });
    expect(await within(section).findByRole('link', { name: '09/05/2026' })).toHaveAttribute('href', '/pacientes/p1/avaliacoes/v2');
    expect(within(section).getAllByRole('row').slice(1).map((row) => row.textContent)).toEqual([
      '01/06/2026Protocolo CRascunho',
      '09/05/2026Protocolo AFinalizada (revisão 2)',
      '01/04/2026Protocolo B/RFinalizada — revisão em andamento',
    ]);
    expect(within(section).getByRole('link', { name: 'Nova avaliação' })).toHaveAttribute('href', '/pacientes/p1/avaliacoes/nova');
  });

  it('situation: rascunho, finalizada, revisão vigente e revisão em andamento', () => {
    expect(situation({ status: 'RASCUNHO', revision: 1, openRevisionId: null })).toBe('Rascunho');
    expect(situation({ status: 'FINALIZADA', revision: 1, openRevisionId: null })).toBe('Finalizada');
    expect(situation({ status: 'FINALIZADA', revision: 3, openRevisionId: null })).toBe('Finalizada (revisão 3)');
    expect(situation({ status: 'FINALIZADA', revision: 3, openRevisionId: 'x' })).toBe('Finalizada (revisão 3) — revisão em andamento');
  });

  it('assistente com assessments.draft vê a seção vazia', async () => {
    mockAssessmentApi({ 'GET /auth/me': { body: draftAssistantMe } });
    renderApp('/pacientes/p1');

    const section = await screen.findByRole('region', { name: 'Avaliações' });
    expect(await within(section).findByText('Nenhuma avaliação registrada.')).toBeInTheDocument();
  });

  it('sem assessments.draft nem assessments.finalize: a seção não aparece e nada é buscado', async () => {
    const { calls } = mockAssessmentApi({ 'GET /auth/me': { body: assistantMe } });
    renderApp('/pacientes/p1');

    expect(await screen.findByDisplayValue('Paciente Sintética Avaliação')).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Avaliações' })).not.toBeInTheDocument();
    expect(calls.filter((c) => c.path.includes('/assessments'))).toEqual([]);
  });

  it('403 nas avaliações tira as avaliações do cache e mantém o cadastro do paciente', async () => {
    let forbidden = false;
    mockAssessmentApi({ 'GET /patients/p1/assessments': () => (forbidden ? FORBIDDEN : { body: assessmentList }) });
    const { queryClient } = renderApp('/pacientes/p1');
    expect(await screen.findByRole('link', { name: '09/05/2026' })).toBeInTheDocument();

    forbidden = true;
    await act(() => queryClient.invalidateQueries({ queryKey: ['assessments'] }));

    expect(await screen.findByText('Você não tem permissão para esta ação.')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: '09/05/2026' })).not.toBeInTheDocument();
    expect(screen.getByDisplayValue('Paciente Sintética Avaliação')).toBeInTheDocument();
    expect(queryClient.getQueryData(['patient', 'p1'])).toEqual(patientWoman);
    expect(queryClient.getQueryData(['assessments', 'p1'])).toBeUndefined();
  });

  it('403 no cadastro do paciente tira do cache também as avaliações', async () => {
    let forbidden = false;
    mockAssessmentApi({
      'GET /patients/p1': () => (forbidden ? FORBIDDEN : { body: patientWoman }),
      'GET /patients/p1/assessments': { body: assessmentList },
    });
    const { queryClient } = renderApp('/pacientes/p1');
    expect(await screen.findByRole('link', { name: '09/05/2026' })).toBeInTheDocument();

    forbidden = true;
    await act(() => queryClient.invalidateQueries({ queryKey: ['patient'] }));

    expect(await screen.findByText('Você não tem permissão para esta ação.')).toBeInTheDocument();
    expect(queryClient.getQueryData(['assessments', 'p1'])).toBeUndefined();
  });

  it('Nova avaliação: escolhe o protocolo, que leva ao formulário dele (P4-3)', async () => {
    mockAssessmentApi();
    renderApp('/pacientes/p1/avaliacoes/nova');

    expect(await screen.findByRole('heading', { name: 'Nova avaliação' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Paciente Sintética Avaliação' })).toHaveAttribute('href', '/pacientes/p1');
    expect(screen.getByRole('link', { name: 'Protocolo A' })).toHaveAttribute('href', '/pacientes/p1/avaliacoes/nova/A');
    expect(screen.getByRole('link', { name: 'Protocolo B/R' })).toHaveAttribute('href', '/pacientes/p1/avaliacoes/nova/B');
    expect(screen.getByRole('link', { name: 'Protocolo C' })).toHaveAttribute('href', '/pacientes/p1/avaliacoes/nova/C');
    expect(screen.getByText('Escolha o protocolo. Ele não muda depois do primeiro salvamento; para outro protocolo, crie outra avaliação.')).toBeInTheDocument();
  });

  it('escolha do protocolo sem permissão de avaliação: mensagem, sem buscar o paciente', async () => {
    const { calls } = mockAssessmentApi({ 'GET /auth/me': { body: assistantMe } });
    renderApp('/pacientes/p1/avaliacoes/nova');

    expect(await screen.findByText('Você não tem permissão para acessar esta página.')).toBeInTheDocument();
    expect(calls.filter((c) => c.path === '/patients/p1')).toEqual([]);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/web -- assessments-section`
Expected: FAIL. `./PatientAssessmentsSection` e `../assessments/types` não existem.

- [ ] **Step 3: Implementar**

`apps/web/src/assessments/types.ts`:

```ts
import type { Sex } from '../patients/types';

/** Basta uma delas para ver, criar e editar rascunhos (a API também exige patients.read). */
export const ASSESSMENT_PERMISSIONS = ['assessments.draft', 'assessments.finalize'] as const;

export type Protocol = 'A' | 'B' | 'C';
export type AssessmentStatus = 'RASCUNHO' | 'FINALIZADA';
export type FieldChanges = Record<string, { from: string | number | boolean | null; to: string | number | boolean | null }>;

export const PROTOCOLS: readonly Protocol[] = ['A', 'B', 'C'];
export const PROTOCOL_LABELS: Record<Protocol, string> = { A: 'Protocolo A', B: 'Protocolo B/R', C: 'Protocolo C' };
export const PROTOCOL_DESCRIPTIONS: Record<Protocol, string> = {
  A: 'IMC calculado pela altura, gordura (%) e composição pelos diâmetros do punho e do fêmur.',
  B: 'IMC, gordura (%), músculo esquelético e gordura visceral do aparelho, com intervalos de referência e 4 classificações.',
  C: 'Composição em kg do aparelho, análise segmentar, impedâncias, pontuação e 13 classificações.',
};
export const STATUS_LABELS: Record<AssessmentStatus, string> = { RASCUNHO: 'Rascunho', FINALIZADA: 'Finalizada' };

export function isProtocol(value: unknown): value is Protocol {
  return value === 'A' || value === 'B' || value === 'C';
}

/** Resultado calculado pela API (prévia ou gravado), como AssessmentCalculation da API. */
export interface AssessmentCalculation {
  rulesVersion: string;
  protocol: Protocol;
  ageYears: number;
  ageBand: string;
  totalWeightKg: number | null;
  totalWeightPct: number | null;
  bmi: number | null;
  bmiCalculated: number | null;
  bmiClassification: string | null;
  bmiReference: string | null;
  fatPct: number | null;
  fatKg: number | null;
  fatPctOfWeight: number | null;
  fatClassification: string | null;
  fatReference: string | null;
  fatFreeKg: number | null;
  fatFreePct: number | null;
  skeletalMusclePct: number | null;
  skeletalMuscleKg: number | null;
  skeletalMuscleReference: string | null;
  muscleKg: number | null;
  musclePct: number | null;
  boneKg: number | null;
  bonePct: number | null;
  residualKg: number | null;
  residualPct: number | null;
  proteinKg: number | null;
  proteinPct: number | null;
  waterKg: number | null;
  waterPct: number | null;
  visceralFatReference: string | null;
  waistHipRatio: number | null;
  waistHipClassification: string | null;
  waistHipReference: string | null;
  suggestedTargetBmi: number;
  suggestedTargetFatPct: number | null;
  targetFatPct: number | null;
  targetBmi: number;
  targetWeightByFatKg: number | null;
  weightControlByFatKg: number | null;
  targetWeightByBmiKg: number | null;
  weightControlByBmiKg: number | null;
  energyMethod: string | null;
  bmrKcal: number | null;
  activityFactor: number;
  expenditureKcal: number | null;
}

export interface Assessment {
  id: string;
  patientId: string;
  seriesId: string;
  revision: number;
  status: AssessmentStatus;
  protocol: Protocol;
  assessedOn: string;
  rulesVersion: string;
  sex: Sex;
  ageYears: number;
  inputs: Record<string, number | string | null>;
  energyMethodLabel: string | null;
  classifications: Record<string, string | null>;
  calculation: AssessmentCalculation;
  revisionReason: string | null;
  revisionChanges: FieldChanges | null;
  supersededAt: string | null;
  currentRevisionId: string | null;
  openRevisionId: string | null;
  version: number;
  createdAt: string;
  updatedAt: string;
  finalizedAt: string | null;
}

export interface AssessmentListItem {
  id: string;
  seriesId: string;
  revision: number;
  protocol: Protocol;
  assessedOn: string;
  status: AssessmentStatus;
  finalizedAt: string | null;
  updatedAt: string;
  openRevisionId: string | null;
}

export interface AssessmentRevisionItem {
  id: string;
  revision: number;
  status: AssessmentStatus;
  assessedOn: string;
  rulesVersion: string;
  revisionReason: string | null;
  revisionChanges: FieldChanges | null;
  createdAt: string;
  createdByName: string | null;
  finalizedAt: string | null;
  finalizedByName: string | null;
  supersededAt: string | null;
}

export interface AssessmentCalculationResult {
  calculation: AssessmentCalculation | null;
  errors: Record<string, string>;
}

export interface ClassificationGroup {
  field: string;
  label: string;
  source: string | null;
  options: { text: string; hint: string | null }[];
}

export interface Range {
  min: number;
  minInclusive: boolean;
  max: number | null;
  maxInclusive: boolean;
  integer: boolean;
}

export interface AssessmentRules {
  version: string;
  status: 'PROVISORIA' | 'VALIDADA';
  protocols: Protocol[];
  energyMethods: { id: string; label: string }[];
  activityFactors: number[];
  defaultActivityFactor: number;
  suggestedTargetBmi: number;
  limits: Record<string, Range>;
  manualClassifications: Record<'B' | 'C', ClassificationGroup[]>;
  finalizeRequired: Record<Protocol, string[]>;
}
```

`apps/web/src/assessments/PatientAssessmentsSection.tsx`: segue `PatientAnamnesesSection`, com estas diferenças:
- título `Avaliações` (id `avaliacoes-titulo`), botão `Nova avaliação` para `/pacientes/${patientId}/avaliacoes/nova` e texto vazio `Nenhuma avaliação registrada.`;
- consulta `['assessments', patientId]` em `/patients/${patientId}/assessments`;
- colunas `Data` (link para `/pacientes/${patientId}/avaliacoes/${item.id}`), `Protocolo` (`PROTOCOL_LABELS[item.protocol]`) e `Situação` (`situation(item)`);
- logo depois do cabeçalho, `{notice ? <Alert tone="success">{notice}</Alert> : null}`, com `const notice = (useLocation().state as { assessmentNotice?: string } | null)?.assessmentNotice ?? null;`.

```tsx
/** Situação na lista: a revisão vigente e a revisão em andamento aparecem no texto. */
export function situation(item: Pick<AssessmentListItem, 'status' | 'revision' | 'openRevisionId'>): string {
  const base = item.status === 'RASCUNHO' ? 'Rascunho' : item.revision > 1 ? `Finalizada (revisão ${item.revision})` : 'Finalizada';
  return item.openRevisionId ? `${base} — revisão em andamento` : base;
}
```

`apps/web/src/assessments/ProtocolPickerPage.tsx`:

```tsx
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router';
import { useCurrentUser } from '../auth/current-user';
import { can, canAny } from '../auth/me';
import { Alert } from '../components/ui/Alert';
import { PageTitle } from '../components/ui/PageTitle';
import { api } from '../lib/api';
import { errorMessage } from '../lib/error-message';
import type { Patient } from '../patients/types';
import { ASSESSMENT_PERMISSIONS, PROTOCOL_DESCRIPTIONS, PROTOCOL_LABELS, PROTOCOLS } from './types';

/** Entrada única da nova avaliação: o protocolo é escolhido aqui e não muda depois (P4-3). */
export function ProtocolPickerRoute() {
  const { patientId } = useParams();
  const me = useCurrentUser();
  if (!patientId || !can(me, 'patients.read') || !canAny(me, ASSESSMENT_PERMISSIONS)) {
    return <Alert>Você não tem permissão para acessar esta página.</Alert>;
  }
  return <ProtocolPicker patientId={patientId} />;
}

function ProtocolPicker({ patientId }: { patientId: string }) {
  const patient = useQuery({
    queryKey: ['patient', patientId],
    queryFn: () => api<Patient>(`/patients/${patientId}`),
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });
  if (patient.error) return <Alert>{errorMessage(patient.error)}</Alert>;
  if (!patient.data) return <p className="text-sm">Carregando…</p>;
  return (
    <section className="space-y-4">
      <PageTitle>Nova avaliação</PageTitle>
      <p className="text-sm">
        {'Paciente: '}
        <Link to={`/pacientes/${patientId}`} className="text-terracota underline">
          {patient.data.fullName}
        </Link>
      </p>
      <p className="text-sm">Escolha o protocolo. Ele não muda depois do primeiro salvamento; para outro protocolo, crie outra avaliação.</p>
      <ul className="grid gap-3 sm:grid-cols-3">
        {PROTOCOLS.map((protocol) => (
          <li key={protocol} className="rounded-lg border border-areia bg-white p-4">
            <Link to={`/pacientes/${patientId}/avaliacoes/nova/${protocol}`} className="font-display text-lg text-terracota underline">
              {PROTOCOL_LABELS[protocol]}
            </Link>
            <p className="mt-2 text-sm">{PROTOCOL_DESCRIPTIONS[protocol]}</p>
          </li>
        ))}
      </ul>
    </section>
  );
}
```

`apps/web/src/patients/PatientFormPage.tsx`:
- imports de `PatientAssessmentsSection` e `ASSESSMENT_PERMISSIONS`;
- logo depois da linha da seção de anamneses:

```tsx
      {!isNew && id && canAny(me, ASSESSMENT_PERMISSIONS) ? <PatientAssessmentsSection patientId={id} /> : null}
```

`apps/web/src/router.tsx`: depois das rotas da anamnese, acrescente o comentário `// As páginas conferem patients.read e assessments.draft ou assessments.finalize.` e a rota `{ path: 'pacientes/:patientId/avaliacoes/nova', element: <ProtocolPickerRoute /> }`.

`apps/web/src/lib/query-client.ts`:

```ts
/** Raízes das consultas de avaliação; 'assessment-revisions' (history.read) fica fora do grupo. */
const ASSESSMENT_ROOTS = ['assessments', 'assessment', 'assessment-calculation', 'assessment-revisions'] as const;
```

`FORBIDDEN_CASCADE` passa a:

```ts
const FORBIDDEN_CASCADE: Readonly<Record<string, readonly string[]>> = {
  patients: ['patients', 'patient', ...ANAMNESIS_ROOTS, ...ASSESSMENT_ROOTS],
  patient: ['patients', 'patient', ...ANAMNESIS_ROOTS, ...ASSESSMENT_ROOTS],
  anamneses: ANAMNESIS_ROOTS,
  anamnesis: ANAMNESIS_ROOTS,
  'anamnesis-calculation': ANAMNESIS_ROOTS,
  assessments: ASSESSMENT_ROOTS,
  assessment: ASSESSMENT_ROOTS,
  'assessment-calculation': ASSESSMENT_ROOTS,
  // Sem history.read, o histórico cai sozinho: a avaliação aberta continua.
  'assessment-revisions': ['assessment-revisions'],
};
```

O comentário de `FORBIDDEN_CASCADE` passa a citar as avaliações: elas caem junto com pacientes; um 403 nelas não derruba o cadastro; um 403 no histórico de revisões não derruba a avaliação; as regras (`assessment-rules`) não têm dado de paciente.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/web; npm run typecheck -w @psm/web`
Expected: PASS com 20 arquivos. `assessments-section.test.tsx` passa nos 8 testes; `typecheck` sem erros.

- [ ] **Step 5: Commit**

```powershell
git add apps/web/src/assessments/types.ts apps/web/src/assessments/PatientAssessmentsSection.tsx apps/web/src/assessments/ProtocolPickerPage.tsx apps/web/src/assessments/assessments-section.test.tsx apps/web/src/test/assessment-fixtures.ts apps/web/src/patients/PatientFormPage.tsx apps/web/src/router.tsx apps/web/src/lib/query-client.ts
git commit -m "feat(web): avaliações na página do paciente, escolha do protocolo e 403 em cascata"
```

---

### Task 11: Web — formulário por protocolo, prévia, classificações e rascunho

**Files:**
- Create: `apps/web/src/assessments/fields.ts`, `apps/web/src/assessments/ClassificationField.tsx`, `apps/web/src/assessments/AssessmentFormPage.tsx`
- Modify: `apps/web/src/router.tsx`, `apps/web/src/test/assessment-fixtures.ts`
- Test: `apps/web/src/assessments/assessment-form.test.tsx`

**Interfaces:**
- Consumes:
  - as rotas das Tasks 1, 5, 6 e 7: regras, prévia, criação, leitura e PUT;
  - `useDebouncedValue` (de `anamneses/use-debounced-value`), `formatDecimalBr`, `formatDateBr` e `todayInClinic`.
- Produces, em `fields.ts`:

```ts
export type SectionId = 'medidas' | 'circunferencias' | 'composicao' | 'segmentar' | 'impedancias' | 'metas' | 'energia';
export interface NumberFieldDef { field: string; label: string; protocols: readonly Protocol[]; section: SectionId; integer?: boolean }
export const NUMBER_FIELDS: readonly NumberFieldDef[];   // os campos numéricos da API, sem activityFactor (que é lista)
export const SECTION_TITLES: Record<SectionId, string>;
export interface ResultDef { key: keyof AssessmentCalculation; label: string; protocols: readonly Protocol[] }
export const RESULTS: readonly ResultDef[];
export function numberFieldsOf(protocol: Protocol): NumberFieldDef[];
export function groupsOf(rules: AssessmentRules, protocol: Protocol): ClassificationGroup[];
export function formatResult(value: unknown): string;   // número com 2 casas em pt-BR; texto como veio; vazio "—"
export function formatFactor(value: number): string;    // 1.55 → "1,55"
export function changeLabel(key: string, groups: readonly ClassificationGroup[]): string;
export function formatChangeValue(value: string | number | boolean | null): string;
```

- Produces:
  - `ClassificationField({ group, value, onChange, clearable, error })`;
  - `AssessmentFormRoute`, nas rotas `/pacientes/:patientId/avaliacoes/nova/:protocol` e `/pacientes/:patientId/avaliacoes/:assessmentId`.
- Produces, em `test/assessment-fixtures.ts`: `rulesV2`, `calcEmpty`, `calcA`, `calcB`, `calcC`, `draftA`, `draftB` e `draftC`. `mockAssessmentApi` passa a incluir regras e prévia.

- [ ] **Step 1: Ampliar os dados de teste**

Em `apps/web/src/test/assessment-fixtures.ts`, acrescente (imports de `NUMBER_FIELDS` de `../assessments/fields` e dos tipos):

```ts
const OMRON = '*Fonte: Omron Healthcare';
const opts = (...texts: string[]) => texts.map((text) => ({ text, hint: null }));
const LEVELS = opts('Baixo (-)', 'Normal (0)', 'Alto (+)', 'Muito Alto (++)');
const range = (min: number, minInclusive: boolean, max: number | null, maxInclusive: boolean, integer = false) => ({ min, minInclusive, max, maxInclusive, integer });

/** Espelha GET /api/assessments/rules da avaliacao-v2. */
export const rulesV2: AssessmentRules = {
  version: 'avaliacao-v2',
  status: 'PROVISORIA',
  protocols: ['A', 'B', 'C'],
  energyMethods: [
    { id: 'HARRIS_BENEDICT', label: 'Harris-Benedict (1919) - 15 aos 74 anos' },
    { id: 'OMS', label: 'OMS (Organização Mundial da Saúde) 1985' },
    { id: 'DRI_ADOLESCENTES', label: 'Comitê DRI - Crianças e Adolescentes com Peso Normal ou Sobrepeso/Obesidade.' },
    { id: 'DRI_ADULTOS', label: 'Comitê DRI - Adultos com Peso Normal ou Sobrepeso/Obesidade.' },
    { id: 'BIOIMPEDANCIA', label: 'Calculado pela Bioimpedância' },
  ],
  activityFactors: [1, 1.2, 1.3, 1.4, 1.5, 1.55, 1.56, 1.6, 1.64, 1.78, 1.8, 1.82, 1.9, 2.1, 2.2, 2.5, 3, 3.5, 4, 5, 6],
  defaultActivityFactor: 1,
  suggestedTargetBmi: 22.5,
  limits: {
    weightKg: range(0, false, 500, true), heightCm: range(30, true, 250, true), circumferenceCm: range(0, false, 300, true),
    diameterMm: range(0, false, 200, true), percent: range(0, false, 100, false), componentKg: range(0, false, 500, true),
    bmi: range(0, false, 100, true), visceralFatLevel: range(0, false, null, false), bmrKcal: range(0, false, 10000, true),
    activityFactor: range(0, false, 10, true), bodyScore: range(0, true, 999, true, true), bodyAgeYears: range(0, false, 150, true),
    segmentReferencePct: range(0, false, 1000, true), impedanceOhm: range(0, false, 2000, true, true),
  },
  manualClassifications: {
    B: [
      {
        field: 'bmiClassification', label: 'IMC', source: '*Valores propostos pela OMS, a Organização Mundial da Saúde.',
        options: [
          { text: '- (Abaixo do peso)', hint: 'Menos de 18,5' }, { text: '0 (Normal)', hint: '18,5 ou mais e menos de 25' },
          { text: '+ (Sobrepeso)', hint: '25 ou mais e menos de 30' }, { text: '++ (Obeso)', hint: '30 ou mais' },
        ],
      },
      { field: 'fatClassification', label: 'Gordura corporal', source: OMRON, options: LEVELS },
      { field: 'skeletalMuscleClassification', label: 'Músculo esquelético', source: OMRON, options: LEVELS },
      {
        field: 'visceralFatClassification', label: 'Gordura visceral', source: OMRON,
        options: [{ text: '0 (Normal)', hint: 'gordura visceral <= 9' }, { text: '(+) Alto', hint: 'gordura visceral <= 14' }, { text: '(++) Muito Alto', hint: 'gordura visceral >= 15' }],
      },
    ],
    C: [
      { field: 'bmiClassification', label: 'IMC', source: null, options: opts('Baixo peso', 'Saudável', 'Médio', 'Alto ou Muito Alto') },
      { field: 'fatClassification', label: 'Gordura corporal', source: null, options: opts('Baixo', 'Saudável', 'Excelente', 'Alto') },
      { field: 'musclePctClassification', label: 'Taxa muscular', source: null, options: opts('Baixo', 'Médio', 'Excelente') },
      { field: 'muscleKgClassification', label: 'Massa muscular', source: null, options: opts('Baixo', 'Saudável', 'Excelente', 'Alto') },
      { field: 'skeletalMuscleClassification', label: 'Músculo esquelético', source: null, options: opts('Baixo', 'Saudável', 'Excelente', 'Alto') },
      { field: 'visceralFatClassification', label: 'Gordura visceral', source: null, options: opts('Baixo Risco', 'Médio Risco', 'Alto Risco') },
      { field: 'subcutaneousFatClassification', label: 'Gordura subcutânea', source: null, options: opts('Baixo', 'Médio', 'Sobrepeso') },
      { field: 'waterClassification', label: 'Água corporal', source: null, options: opts('Baixo', 'Saudável', 'Excelente', 'Alto') },
      { field: 'proteinClassification', label: 'Proteína', source: null, options: opts('Baixo', 'Saudável', 'Excelente', 'Alto') },
      { field: 'boneClassification', label: 'Massa óssea', source: null, options: opts('Baixo', 'Saudável', 'Excelente', 'Alto') },
      { field: 'bodyAgeClassification', label: 'Idade corporal', source: null, options: opts('Excelente', 'Sobrepeso') },
      { field: 'bmrClassification', label: 'TMB', source: null, options: opts('Baixo', 'Excelente') },
      { field: 'totalWeightClassification', label: 'Peso total', source: null, options: opts('Baixo', 'Saudável', 'Excelente', 'Alto') },
    ],
  },
  finalizeRequired: { A: ['weightKg', 'heightCm', 'fatPct'], B: ['weightKg', 'heightCm', 'fatPct', 'informedBmi'], C: ['weightKg', 'heightCm', 'fatKg'] },
};

export const calcEmpty: AssessmentCalculation = {
  rulesVersion: 'avaliacao-v2', protocol: 'A', ageYears: 40, ageBand: 'Adultos', totalWeightKg: null, totalWeightPct: null, bmi: null,
  bmiCalculated: null, bmiClassification: null, bmiReference: null, fatPct: null, fatKg: null, fatPctOfWeight: null, fatClassification: null,
  fatReference: null, fatFreeKg: null, fatFreePct: null, skeletalMusclePct: null, skeletalMuscleKg: null, skeletalMuscleReference: null,
  muscleKg: null, musclePct: null, boneKg: null, bonePct: null, residualKg: null, residualPct: null, proteinKg: null, proteinPct: null,
  waterKg: null, waterPct: null, visceralFatReference: null, waistHipRatio: null, waistHipClassification: null, waistHipReference: null,
  suggestedTargetBmi: 22.5, suggestedTargetFatPct: null, targetFatPct: null, targetBmi: 22.5, targetWeightByFatKg: null,
  weightControlByFatKg: null, targetWeightByBmiKg: null, weightControlByBmiKg: null, energyMethod: null, bmrKcal: null, activityFactor: 1,
  expenditureKcal: null,
};

/** Valores da API para os casos sintéticos das Tasks 5 a 7 (conferidos com o código do Plano 3). */
export const calcA: AssessmentCalculation = {
  ...calcEmpty, totalWeightKg: 80, totalWeightPct: 100, bmi: 27.68, bmiCalculated: 27.68, bmiClassification: 'Sobrepeso', fatPct: 20, fatKg: 16,
  fatPctOfWeight: 20, fatClassification: 'Bom', fatFreeKg: 64, fatFreePct: 80, muscleKg: 37.51, musclePct: 46.89, boneKg: 9.77, bonePct: 12.21,
  residualKg: 16.72, residualPct: 20.9, waistHipRatio: 0.8, waistHipClassification: 'Risco Alto', suggestedTargetFatPct: 25, targetFatPct: 25,
  targetWeightByFatKg: 85.33, weightControlByFatKg: 5.33, targetWeightByBmiKg: 65.03, weightControlByBmiKg: -14.97, energyMethod: 'HARRIS_BENEDICT',
  bmrKcal: 1548, activityFactor: 1.55, expenditureKcal: 2399.4,
};
export const calcB: AssessmentCalculation = {
  ...calcEmpty, protocol: 'B', totalWeightKg: 70, totalWeightPct: 100, bmi: 25.7, bmiCalculated: 25.71, bmiReference: '18,5 - 24,9', fatPct: 30,
  fatKg: 21, fatPctOfWeight: 30, fatReference: '23 - 33,9', fatFreeKg: 49, fatFreePct: 70, skeletalMusclePct: 28, skeletalMuscleKg: 19.6,
  skeletalMuscleReference: '24,1 - 30,1', visceralFatReference: '<= 9', waistHipRatio: 0.77, waistHipClassification: 'Risco Moderado',
  waistHipReference: '0,73 - 0,79', suggestedTargetFatPct: 28, targetFatPct: 28, targetWeightByFatKg: 68.06, weightControlByFatKg: -1.94,
  targetWeightByBmiKg: 61.26, weightControlByBmiKg: -8.74, energyMethod: 'BIOIMPEDANCIA', bmrKcal: 1350, activityFactor: 1.2, expenditureKcal: 1620,
};
export const calcC: AssessmentCalculation = {
  ...calcEmpty, protocol: 'C', totalWeightKg: 70, totalWeightPct: 100, bmi: 22.9, bmiCalculated: 22.86, fatPct: 20, fatKg: 14,
  skeletalMusclePct: 43.14, skeletalMuscleKg: 30.2, muscleKg: 52.9, musclePct: 75.57, boneKg: 3.1, bonePct: 4.43, proteinKg: 11.7,
  proteinPct: 16.71, waterKg: 41.3, waterPct: 59, waistHipRatio: 0.85, waistHipClassification: 'Risco Moderado', suggestedTargetFatPct: 19,
  targetFatPct: 19, targetWeightByFatKg: 69.14, weightControlByFatKg: -0.86, targetWeightByBmiKg: 68.91, weightControlByBmiKg: -1.09,
  energyMethod: 'BIOIMPEDANCIA', bmrKcal: 1610, activityFactor: 1.55, expenditureKcal: 2495.5,
};

/** Entradas vazias do protocolo (dado de teste; os esperados dos testes são literais). */
const emptyInputs = (protocol: Protocol) => Object.fromEntries(NUMBER_FIELDS.filter((f) => f.protocols.includes(protocol)).map((f) => [f.field, null]));

const VIEW = {
  patientId: 'p1', revision: 1, status: 'RASCUNHO', assessedOn: '2026-05-09', rulesVersion: 'avaliacao-v2', sex: 'FEMININO', ageYears: 40,
  revisionReason: null, revisionChanges: null, supersededAt: null, currentRevisionId: null, openRevisionId: null, version: 1,
  createdAt: '2026-05-09T13:00:00.000Z', updatedAt: '2026-05-09T13:00:00.000Z', finalizedAt: null,
} as const;

export const draftA: Assessment = {
  ...VIEW, id: 'v1', seriesId: 'v1', protocol: 'A', energyMethodLabel: 'Harris-Benedict (1919) - 15 aos 74 anos', classifications: {}, calculation: calcA,
  inputs: {
    ...emptyInputs('A'), weightKg: 80, heightCm: 170, waistCm: 80, hipCm: 100, fatPct: 20, wristDiameterMm: 50, femurDiameterMm: 90, neckCm: 34,
    energyMethod: 'HARRIS_BENEDICT', activityFactor: 1.55, observation: 'Observação sintética.', conclusion: null,
  },
};

export const draftB: Assessment = {
  ...VIEW, id: 'b1', seriesId: 'b1', protocol: 'B', energyMethodLabel: 'Calculado pela Bioimpedância', calculation: calcB,
  inputs: {
    ...emptyInputs('B'), weightKg: 70, heightCm: 165, waistCm: 75, hipCm: 98, informedBmi: 25.7, fatPct: 30, skeletalMusclePct: 28,
    visceralFatLevel: 6, bodyAgeYears: 42, informedBmrKcal: 1350, energyMethod: 'BIOIMPEDANCIA', activityFactor: 1.2, observation: null, conclusion: null,
  },
  classifications: { bmiClassification: '+ (Sobrepeso)', fatClassification: 'Normal (0)', skeletalMuscleClassification: null, visceralFatClassification: '0 (Normal)' },
};

export const draftC: Assessment = {
  ...VIEW, id: 'c1', seriesId: 'c1', protocol: 'C', energyMethodLabel: 'Calculado pela Bioimpedância', calculation: calcC,
  inputs: {
    ...emptyInputs('C'), measuredTime: '08:30', weightKg: 70, heightCm: 175, informedBmi: 22.9, fatKg: 14, boneKg: 3.1, proteinKg: 11.7,
    waterKg: 41.3, muscleKg: 52.9, skeletalMuscleKg: 30.2, bodyScore: 120, z20ArmRightOhm: 310, energyMethod: 'BIOIMPEDANCIA',
    activityFactor: 1.55, observation: null, conclusion: null,
  },
  classifications: {
    bmiClassification: 'Saudável', fatClassification: 'Saudável', musclePctClassification: 'Excelente', muscleKgClassification: 'Alto',
    skeletalMuscleClassification: 'Alto', visceralFatClassification: 'Baixo Risco', subcutaneousFatClassification: 'Médio',
    waterClassification: 'Excelente', proteinClassification: 'Alto', boneClassification: 'Alto', bodyAgeClassification: 'Excelente',
    bmrClassification: 'Excelente', totalWeightClassification: 'Saudável',
  },
};
```

Em `mockAssessmentApi`, acrescente às rotas padrão `'GET /assessments/rules': { body: rulesV2 }` e `'POST /patients/p1/assessments/calculation': { body: { calculation: calcEmpty, errors: {} } }`.

- [ ] **Step 2: Escrever os testes**

`apps/web/src/assessments/assessment-form.test.tsx`:

```tsx
import { act, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { todayInClinic } from '../lib/format';
import { calcA, calcB, calcC, draftA, draftB, draftC, mockAssessmentApi } from '../test/assessment-fixtures';
import type { MockRequest } from '../test/mock-api';
import { renderApp } from '../test/render';
import { NUMBER_FIELDS } from './fields';
import type { Protocol } from './types';

const putBody = (calls: MockRequest[]) => calls.find((c) => c.method === 'PUT')?.body as Record<string, unknown> | undefined;

describe('Formulário da avaliação', () => {
  it('campos de cada protocolo iguais aos da API (assessment-normalize), sem o fator de atividade, que é lista', () => {
    const of = (protocol: Protocol) => NUMBER_FIELDS.filter((f) => f.protocols.includes(protocol)).map((f) => f.field).sort();
    expect(of('A')).toEqual([
      'abdomenCm', 'armLeftCm', 'armRightCm', 'calfLeftCm', 'calfRightCm', 'chestCm', 'fatPct', 'femurDiameterMm', 'forearmLeftCm', 'forearmRightCm',
      'heightCm', 'hipCm', 'informedBmrKcal', 'neckCm', 'shouldersCm', 'targetBmi', 'targetFatPct', 'thighLeftCm', 'thighRightCm', 'waistCm',
      'weightKg', 'wristDiameterMm',
    ]);
    expect(of('B')).toEqual([
      'abdomenCm', 'armLeftCm', 'armRightCm', 'bodyAgeYears', 'calfLeftCm', 'calfRightCm', 'chestCm', 'fatPct', 'forearmLeftCm', 'forearmRightCm',
      'heightCm', 'hipCm', 'informedBmi', 'informedBmrKcal', 'neckCm', 'shouldersCm', 'skeletalMusclePct', 'targetBmi', 'targetFatPct',
      'thighLeftCm', 'thighRightCm', 'visceralFatLevel', 'waistCm', 'weightKg',
    ]);
    expect(of('C')).toHaveLength(61);
    expect(of('C')).toEqual(expect.arrayContaining(['fatFreeKg', 'subcutaneousFatPct', 'bodyScore', 'fatTrunkRefPct', 'z100LegLeftOhm', 'muscleLegLeftKg']));
    expect(new Set(NUMBER_FIELDS.map((f) => f.field)).size).toBe(NUMBER_FIELDS.length);
    expect(new Set(NUMBER_FIELDS.map((f) => f.label)).size).toBe(NUMBER_FIELDS.length);
  });

  it('nova A: só os campos da A e a data de hoje; salvar envia o protocolo e os campos da A (vazios como null) e abre o rascunho', async () => {
    const { calls } = mockAssessmentApi({ 'POST /patients/p1/assessments': { status: 201, body: draftA } });
    const user = userEvent.setup();
    const { router } = renderApp('/pacientes/p1/avaliacoes/nova/A');

    expect(await screen.findByRole('heading', { name: 'Nova avaliação — Protocolo A' })).toBeInTheDocument();
    expect(screen.getByLabelText('Data da avaliação *')).toHaveValue(todayInClinic());
    expect(screen.getByLabelText('Diâmetro do punho (mm)')).toBeInTheDocument();
    for (const label of ['IMC do aparelho', 'Massa óssea (kg)', 'Pontuação', 'Hora da medição', 'Idade corporal (anos)']) {
      expect(screen.queryByLabelText(label)).not.toBeInTheDocument();
    }
    expect(screen.queryByRole('region', { name: 'Classificações' })).not.toBeInTheDocument();
    expect(screen.getByText('Para finalizar, preencha: Peso (kg), Altura (cm), Gordura (%).')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Salvar rascunho' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/pacientes/p1/avaliacoes/v1'));
    expect(calls.find((c) => c.method === 'POST' && c.path === '/patients/p1/assessments')?.body).toEqual({
      protocol: 'A', assessedOn: todayInClinic(), energyMethod: null, activityFactor: null, observation: null, conclusion: null,
      weightKg: null, heightCm: null, neckCm: null, shouldersCm: null, chestCm: null, armRightCm: null, armLeftCm: null, forearmRightCm: null,
      forearmLeftCm: null, abdomenCm: null, waistCm: null, hipCm: null, thighRightCm: null, thighLeftCm: null, calfRightCm: null,
      calfLeftCm: null, fatPct: null, wristDiameterMm: null, femurDiameterMm: null, targetFatPct: null, targetBmi: null, informedBmrKcal: null,
    });
    expect(await screen.findByText('Rascunho salvo.')).toBeInTheDocument();
  });

  it('prévia: envia o formulário com o protocolo; mostra os resultados e os erros por campo da API', async () => {
    const previews: MockRequest[] = [];
    mockAssessmentApi({
      'POST /patients/p1/assessments/calculation': (request) => {
        previews.push(request);
        return { body: { calculation: calcA, errors: { heightCm: 'A altura deve estar entre 30 e 250 cm, com até 2 casas decimais.' } } };
      },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/avaliacoes/nova/A');

    await user.type(await screen.findByLabelText('Peso (kg)'), '80');

    await waitFor(() => expect(previews.at(-1)?.body).toMatchObject({ protocol: 'A', weightKg: 80, heightCm: null }));
    expect(await screen.findByText('A altura deve estar entre 30 e 250 cm, com até 2 casas decimais.')).toBeInTheDocument();
    expect(screen.getByLabelText('IMC')).toHaveTextContent('27,68');
    expect(screen.getByLabelText('Classificação do IMC')).toHaveTextContent('Sobrepeso');
    expect(screen.getByLabelText('Peso residual (kg)')).toHaveTextContent('16,72');
    expect(screen.getByLabelText('TMB (kcal/dia)')).toHaveTextContent('1.548,00');
    expect(screen.getByLabelText('Idade na data')).toHaveTextContent('40 anos');
  });

  it('B: as 4 classificações com as legendas do FrmBioimpR; reabre marcada; escolher e limpar vão no PUT, sem o protocolo', async () => {
    const { calls } = mockAssessmentApi({
      'GET /assessments/b1': { body: draftB },
      'PUT /assessments/b1': { body: draftB },
      'POST /patients/p1/assessments/calculation': { body: { calculation: calcB, errors: {} } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/avaliacoes/b1');

    const bmi = await screen.findByRole('group', { name: 'IMC' });
    expect(within(bmi).getAllByRole('radio').map((radio) => radio.closest('label')?.textContent)).toEqual([
      '- (Abaixo do peso)', '0 (Normal)', '+ (Sobrepeso)', '++ (Obeso)',
    ]);
    expect(within(bmi).getByRole('radio', { name: '+ (Sobrepeso)' })).toBeChecked();
    expect(within(bmi).getByText('Menos de 18,5')).toBeInTheDocument();
    expect(within(bmi).getByText('*Valores propostos pela OMS, a Organização Mundial da Saúde.')).toBeInTheDocument();
    const muscle = screen.getByRole('group', { name: 'Músculo esquelético' });
    expect(within(muscle).getAllByRole('radio').filter((radio) => (radio as HTMLInputElement).checked)).toEqual([]);
    await waitFor(() => expect(screen.getByLabelText('Intervalo da gordura')).toHaveTextContent('23 - 33,9'));

    await user.click(within(screen.getByRole('group', { name: 'Gordura corporal' })).getByRole('radio', { name: 'Alto (+)' }));
    await user.click(within(screen.getByRole('group', { name: 'Gordura visceral' })).getByRole('button', { name: 'Limpar classificação' }));
    await user.click(screen.getByRole('button', { name: 'Salvar rascunho' }));

    await waitFor(() =>
      expect(putBody(calls)).toMatchObject({
        version: 1, bmiClassification: '+ (Sobrepeso)', fatClassification: 'Alto (+)', skeletalMuscleClassification: null,
        visceralFatClassification: null, bodyAgeYears: 42, informedBmi: 25.7,
      }),
    );
    expect(putBody(calls)).not.toHaveProperty('protocol');
    expect(putBody(calls)).not.toHaveProperty('waterClassification');
    expect(putBody(calls)).not.toHaveProperty('wristDiameterMm');
  });

  it('C: os 13 grupos, com as opções que o legado não transferia marcadas ao reabrir (H-16); hora e campos da C no PUT', async () => {
    const { calls } = mockAssessmentApi({
      'GET /assessments/c1': { body: draftC },
      'PUT /assessments/c1': { body: draftC },
      'POST /patients/p1/assessments/calculation': { body: { calculation: calcC, errors: {} } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/avaliacoes/c1');

    const region = await screen.findByRole('region', { name: 'Classificações' });
    expect(within(region).getAllByRole('group').map((group) => group.querySelector('legend')?.textContent)).toEqual([
      'IMC', 'Gordura corporal', 'Taxa muscular', 'Massa muscular', 'Músculo esquelético', 'Gordura visceral', 'Gordura subcutânea',
      'Água corporal', 'Proteína', 'Massa óssea', 'Idade corporal', 'TMB', 'Peso total',
    ]);
    for (const [group, text] of [['Massa muscular', 'Alto'], ['Músculo esquelético', 'Alto'], ['Água corporal', 'Excelente'], ['Proteína', 'Alto'], ['Massa óssea', 'Alto']]) {
      expect(within(screen.getByRole('group', { name: group })).getByRole('radio', { name: text })).toBeChecked();
    }
    expect(screen.getByLabelText('Hora da medição')).toHaveValue('08:30');
    expect(screen.getByLabelText('Pontuação')).toHaveValue(120);
    await waitFor(() => expect(screen.getByLabelText('Proteína (%)')).toHaveTextContent('16,71'));
    expect(screen.queryByLabelText('Diâmetro do punho (mm)')).not.toBeInTheDocument();

    await user.click(within(screen.getByRole('group', { name: 'Proteína' })).getByRole('radio', { name: 'Baixo' }));
    await user.click(screen.getByRole('button', { name: 'Salvar rascunho' }));

    await waitFor(() =>
      expect(putBody(calls)).toMatchObject({
        version: 1, measuredTime: '08:30', bodyScore: 120, z20ArmRightOhm: 310, fatKg: 14, proteinClassification: 'Baixo', boneClassification: 'Alto',
      }),
    );
    expect(putBody(calls)).not.toHaveProperty('fatPct');
  });

  it('métodos de TMB com as legendas do legado; fator de atividade com vírgula; vazio é o padrão', async () => {
    mockAssessmentApi({ 'GET /assessments/v1': { body: draftA } });
    renderApp('/pacientes/p1/avaliacoes/v1');

    const method = await screen.findByLabelText('Método de TMB');
    expect(within(method).getAllByRole('option').map((option) => option.textContent)).toEqual([
      'Selecione', 'Harris-Benedict (1919) - 15 aos 74 anos', 'OMS (Organização Mundial da Saúde) 1985',
      'Comitê DRI - Crianças e Adolescentes com Peso Normal ou Sobrepeso/Obesidade.', 'Comitê DRI - Adultos com Peso Normal ou Sobrepeso/Obesidade.',
      'Calculado pela Bioimpedância',
    ]);
    expect(method).toHaveValue('HARRIS_BENEDICT');
    const factor = screen.getByLabelText('Fator de atividade');
    expect(factor).toHaveValue('1.55');
    expect(within(factor).getAllByRole('option').slice(0, 7).map((option) => option.textContent)).toEqual(['Padrão (1)', '1', '1,2', '1,3', '1,4', '1,5', '1,55']);
    expect(within(factor).getAllByRole('option')).toHaveLength(22);
    expect(screen.getByRole('heading', { name: 'Avaliação de 09/05/2026' })).toBeInTheDocument();
    expect(screen.getByText('Protocolo A. O protocolo não muda depois de criado.')).toBeInTheDocument();
  });

  it('erro do salvamento aparece no campo e no aviso geral (400 "Dados inválidos.")', async () => {
    const message = 'A circunferência do pescoço deve ser maior que 0 e no máximo 300 cm, com até 2 casas decimais.';
    mockAssessmentApi({
      'GET /assessments/v1': { body: draftA },
      'PUT /assessments/v1': { status: 400, body: { message: 'Dados inválidos.', errors: { neckCm: message } } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/avaliacoes/v1');

    await user.click(await screen.findByRole('button', { name: 'Salvar rascunho' }));

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByText('Dados inválidos.')).toBeInTheDocument();
  });

  it('avaliação de outro paciente: "Avaliação não encontrada."; protocolo desconhecido na URL: aviso', async () => {
    mockAssessmentApi({ 'GET /assessments/v9': { body: { ...draftA, id: 'v9', patientId: 'p2' } } });
    const { router } = renderApp('/pacientes/p1/avaliacoes/v9');
    expect(await screen.findByText('Avaliação não encontrada.')).toBeInTheDocument();

    await act(() => router.navigate('/pacientes/p1/avaliacoes/nova/D'));
    expect(await screen.findByText('Protocolo desconhecido.')).toBeInTheDocument();
  });
});
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `npm test -w @psm/web -- assessment-form`
Expected: FAIL. `./fields` não existe, e as rotas do formulário levam a `/`.

- [ ] **Step 4: Implementar `fields.ts` e `ClassificationField.tsx`**

`apps/web/src/assessments/fields.ts`:

```ts
import { formatDecimalBr } from '../lib/format';
import type { AssessmentCalculation, AssessmentRules, ClassificationGroup, Protocol } from './types';

export type SectionId = 'medidas' | 'circunferencias' | 'composicao' | 'segmentar' | 'impedancias' | 'metas' | 'energia';

export interface NumberFieldDef {
  field: string;
  label: string;
  protocols: readonly Protocol[];
  section: SectionId;
  integer?: boolean;
}

const ALL: readonly Protocol[] = ['A', 'B', 'C'];
const C: readonly Protocol[] = ['C'];
const cm = (field: string, label: string): NumberFieldDef => ({ field, label: `${label} (cm)`, protocols: ALL, section: 'circunferencias' });
const SEGMENTS = [['ArmRight', 'braço direito'], ['ArmLeft', 'braço esquerdo'], ['Trunk', 'tronco'], ['LegRight', 'perna direita'], ['LegLeft', 'perna esquerda']] as const;

/** Campos numéricos da API (assessment-normalize, NUMERIC_FIELDS), sem activityFactor, que é uma lista. Unidade no rótulo. */
export const NUMBER_FIELDS: readonly NumberFieldDef[] = [
  { field: 'weightKg', label: 'Peso (kg)', protocols: ALL, section: 'medidas' },
  { field: 'heightCm', label: 'Altura (cm)', protocols: ALL, section: 'medidas' },
  cm('neckCm', 'Pescoço'), cm('shouldersCm', 'Ombros'), cm('chestCm', 'Peitoral'), cm('armRightCm', 'Braço direito'), cm('armLeftCm', 'Braço esquerdo'),
  cm('forearmRightCm', 'Antebraço direito'), cm('forearmLeftCm', 'Antebraço esquerdo'), cm('abdomenCm', 'Abdômen'), cm('waistCm', 'Cintura'),
  cm('hipCm', 'Quadril'), cm('thighRightCm', 'Coxa direita'), cm('thighLeftCm', 'Coxa esquerda'), cm('calfRightCm', 'Panturrilha direita'),
  cm('calfLeftCm', 'Panturrilha esquerda'),
  { field: 'fatPct', label: 'Gordura (%)', protocols: ['A', 'B'], section: 'composicao' },
  { field: 'wristDiameterMm', label: 'Diâmetro do punho (mm)', protocols: ['A'], section: 'composicao' },
  { field: 'femurDiameterMm', label: 'Diâmetro do fêmur (mm)', protocols: ['A'], section: 'composicao' },
  { field: 'informedBmi', label: 'IMC do aparelho', protocols: ['B', 'C'], section: 'composicao' },
  { field: 'skeletalMusclePct', label: 'Músculo esquelético (%)', protocols: ['B'], section: 'composicao' },
  { field: 'fatKg', label: 'Gordura (kg)', protocols: C, section: 'composicao' },
  { field: 'fatFreeKg', label: 'Massa livre de gordura (kg)', protocols: C, section: 'composicao' },
  { field: 'muscleKg', label: 'Massa muscular (kg)', protocols: C, section: 'composicao' },
  { field: 'skeletalMuscleKg', label: 'Músculo esquelético (kg)', protocols: C, section: 'composicao' },
  { field: 'boneKg', label: 'Massa óssea (kg)', protocols: C, section: 'composicao' },
  { field: 'waterKg', label: 'Água corporal (kg)', protocols: C, section: 'composicao' },
  { field: 'proteinKg', label: 'Proteína (kg)', protocols: C, section: 'composicao' },
  { field: 'visceralFatLevel', label: 'Nível de gordura visceral', protocols: ['B', 'C'], section: 'composicao' },
  { field: 'subcutaneousFatPct', label: 'Gordura subcutânea (%)', protocols: C, section: 'composicao' },
  { field: 'bodyAgeYears', label: 'Idade corporal (anos)', protocols: ['B', 'C'], section: 'composicao' },
  { field: 'bodyScore', label: 'Pontuação', protocols: C, section: 'composicao', integer: true },
  ...SEGMENTS.flatMap(([key, name]): NumberFieldDef[] => [
    { field: `fat${key}RefPct`, label: `Gordura — ${name} (% do padrão)`, protocols: C, section: 'segmentar' },
    { field: `fat${key}Kg`, label: `Gordura — ${name} (kg)`, protocols: C, section: 'segmentar' },
  ]),
  ...SEGMENTS.flatMap(([key, name]): NumberFieldDef[] => [
    { field: `muscle${key}RefPct`, label: `Músculo — ${name} (% do padrão)`, protocols: C, section: 'segmentar' },
    { field: `muscle${key}Kg`, label: `Músculo — ${name} (kg)`, protocols: C, section: 'segmentar' },
  ]),
  ...(['20', '100'] as const).flatMap((frequency) =>
    SEGMENTS.map(([key, name]): NumberFieldDef => ({
      field: `z${frequency}${key}Ohm`, label: `Z${frequency} — ${name} (Ω)`, protocols: C, section: 'impedancias', integer: true,
    })),
  ),
  { field: 'targetFatPct', label: 'Gordura-alvo (%)', protocols: ALL, section: 'metas' },
  { field: 'targetBmi', label: 'IMC-alvo', protocols: ALL, section: 'metas' },
  { field: 'informedBmrKcal', label: 'TMB do aparelho (kcal/dia)', protocols: ALL, section: 'energia' },
];

export const SECTION_TITLES: Record<SectionId, string> = {
  medidas: 'Medidas', circunferencias: 'Circunferências', composicao: 'Composição corporal', segmentar: 'Análise segmentar',
  impedancias: 'Impedâncias', metas: 'Metas', energia: 'Energia',
};

export interface ResultDef {
  key: keyof AssessmentCalculation;
  label: string;
  protocols: readonly Protocol[];
}

/** O que a API calcula, por protocolo; a unidade vai no rótulo. */
export const RESULTS: readonly ResultDef[] = [
  { key: 'ageBand', label: 'Faixa etária', protocols: ALL },
  { key: 'bmi', label: 'IMC', protocols: ['A'] },
  { key: 'bmiClassification', label: 'Classificação do IMC', protocols: ['A'] },
  { key: 'bmiCalculated', label: 'IMC calculado (peso/altura²)', protocols: ['B', 'C'] },
  { key: 'bmiReference', label: 'Intervalo do IMC', protocols: ['B'] },
  { key: 'totalWeightPct', label: 'Peso total (%)', protocols: C },
  { key: 'fatKg', label: 'Gordura (kg)', protocols: ['A', 'B'] },
  { key: 'fatPct', label: 'Gordura (%)', protocols: C },
  { key: 'fatClassification', label: 'Classificação da gordura', protocols: ['A'] },
  { key: 'fatReference', label: 'Intervalo da gordura', protocols: ['B'] },
  { key: 'fatFreeKg', label: 'Massa livre de gordura (kg)', protocols: ['A', 'B'] },
  { key: 'fatFreePct', label: 'Massa livre de gordura (%)', protocols: ['A', 'B'] },
  { key: 'skeletalMuscleKg', label: 'Músculo esquelético (kg)', protocols: ['B'] },
  { key: 'skeletalMusclePct', label: 'Músculo esquelético (%)', protocols: C },
  { key: 'skeletalMuscleReference', label: 'Intervalo do músculo esquelético', protocols: ['B'] },
  { key: 'visceralFatReference', label: 'Intervalo da gordura visceral', protocols: ['B'] },
  { key: 'residualKg', label: 'Peso residual (kg)', protocols: ['A'] },
  { key: 'residualPct', label: 'Peso residual (%)', protocols: ['A'] },
  { key: 'boneKg', label: 'Peso ósseo (kg)', protocols: ['A'] },
  { key: 'bonePct', label: 'Peso ósseo (%)', protocols: ['A'] },
  { key: 'muscleKg', label: 'Peso muscular (kg)', protocols: ['A'] },
  { key: 'musclePct', label: 'Peso muscular (%)', protocols: ['A'] },
  { key: 'musclePct', label: 'Taxa muscular (%)', protocols: C },
  { key: 'bonePct', label: 'Massa óssea (%)', protocols: C },
  { key: 'waterPct', label: 'Água corporal (%)', protocols: C },
  { key: 'proteinPct', label: 'Proteína (%)', protocols: C },
  { key: 'waistHipRatio', label: 'RCQ', protocols: ALL },
  { key: 'waistHipClassification', label: 'Classificação do RCQ', protocols: ALL },
  { key: 'waistHipReference', label: 'Intervalo do RCQ', protocols: ['B'] },
  { key: 'suggestedTargetFatPct', label: 'Gordura-alvo sugerida (%)', protocols: ALL },
  { key: 'suggestedTargetBmi', label: 'IMC-alvo sugerido', protocols: ALL },
  { key: 'targetWeightByFatKg', label: 'Peso-alvo pela gordura (kg)', protocols: ALL },
  { key: 'weightControlByFatKg', label: 'Controle de peso pela gordura (kg)', protocols: ALL },
  { key: 'targetWeightByBmiKg', label: 'Peso-alvo pelo IMC (kg)', protocols: ALL },
  { key: 'weightControlByBmiKg', label: 'Controle de peso pelo IMC (kg)', protocols: ALL },
  { key: 'bmrKcal', label: 'TMB (kcal/dia)', protocols: ALL },
  { key: 'expenditureKcal', label: 'GET (kcal/dia)', protocols: ALL },
];

export function numberFieldsOf(protocol: Protocol): NumberFieldDef[] {
  return NUMBER_FIELDS.filter((def) => def.protocols.includes(protocol));
}

export function groupsOf(rules: AssessmentRules, protocol: Protocol): ClassificationGroup[] {
  return protocol === 'A' ? [] : rules.manualClassifications[protocol];
}

export function formatResult(value: unknown): string {
  if (value === null || value === undefined) return '—';
  return typeof value === 'number' ? formatDecimalBr(value, 2) : String(value);
}

export function formatFactor(value: number): string {
  return String(value).replace('.', ',');
}

const FIXED_LABELS: Readonly<Record<string, string>> = {
  status: 'Situação', protocol: 'Protocolo', assessedOn: 'Data da avaliação', rulesVersion: 'Versão das regras', sex: 'Sexo',
  ageYears: 'Idade (anos)', 'inputs.energyMethod': 'Método de TMB', 'inputs.activityFactor': 'Fator de atividade',
  'inputs.measuredTime': 'Hora da medição', 'inputs.observation': 'Observação', 'inputs.conclusion': 'Conclusão',
};

/** Rótulo de um campo alterado numa revisão (chaves de assessmentChanges da API). Só chaves próprias contam. */
export function changeLabel(key: string, groups: readonly ClassificationGroup[]): string {
  if (Object.hasOwn(FIXED_LABELS, key)) return FIXED_LABELS[key];
  if (key.startsWith('inputs.')) return NUMBER_FIELDS.find((def) => def.field === key.slice('inputs.'.length))?.label ?? key;
  if (key.startsWith('classifications.')) {
    const group = groups.find((g) => g.field === key.slice('classifications.'.length));
    return group ? `Classificação — ${group.label}` : key;
  }
  return key;
}

export function formatChangeValue(value: string | number | boolean | null): string {
  if (value === null) return '—';
  return typeof value === 'number' ? formatFactor(value) : String(value);
}
```

`apps/web/src/assessments/ClassificationField.tsx`:

```tsx
import type { ClassificationGroup } from './types';

interface ClassificationFieldProps {
  group: ClassificationGroup;
  value: string | null;
  onChange: (value: string | null) => void;
  /** Mostra "Limpar classificação" quando há opção marcada (falso na somente leitura). */
  clearable: boolean;
  error?: string;
}

/** Grupo de classificação manual (CL-08): a opção é o texto exato da legenda do legado; vazio é null. */
export function ClassificationField({ group, value, onChange, clearable, error }: ClassificationFieldProps) {
  return (
    <fieldset className="space-y-1">
      <legend className="text-sm font-semibold">{group.label}</legend>
      <div className="grid gap-1">
        {group.options.map((option) => (
          <div key={option.text} className="flex flex-wrap items-center gap-2 text-sm">
            <label className="flex items-center gap-2">
              <input type="radio" name={group.field} checked={value === option.text} onChange={() => onChange(option.text)} />
              {option.text}
            </label>
            {option.hint ? <span className="text-xs text-grafite/70">{option.hint}</span> : null}
          </div>
        ))}
      </div>
      {group.source ? <p className="text-xs text-grafite/70">{group.source}</p> : null}
      {error ? <p className="text-sm text-terracota">{error}</p> : null}
      {clearable && value !== null ? (
        <button type="button" className="text-sm text-terracota underline" onClick={() => onChange(null)}>
          Limpar classificação
        </button>
      ) : null}
    </fieldset>
  );
}
```

- [ ] **Step 5: Implementar o formulário e as rotas**

`apps/web/src/assessments/AssessmentFormPage.tsx` segue `AnamnesisFormPage.tsx`. Ele reaproveita `FormSection` e `Result` copiados de lá, com o mesmo markup. As partes que mudam:

```tsx
const TEXT_MAX_LENGTH = 4000;
const PREVIEW_DELAY_MS = 300;
const BEFORE_CLASSIFICATIONS: readonly SectionId[] = ['medidas', 'circunferencias', 'composicao', 'segmentar', 'impedancias'];

interface FormState {
  assessedOn: string;
  measuredTime: string;
  energyMethod: string;
  activityFactor: string;
  numbers: Record<string, string>;
  classifications: Record<string, string | null>;
  observation: string;
  conclusion: string;
}

function emptyForm(protocol: Protocol, rules: AssessmentRules): FormState {
  return {
    assessedOn: todayInClinic(), measuredTime: '', energyMethod: '', activityFactor: '',
    numbers: Object.fromEntries(numberFieldsOf(protocol).map((def) => [def.field, ''])),
    classifications: Object.fromEntries(groupsOf(rules, protocol).map((group) => [group.field, null])),
    observation: '', conclusion: '',
  };
}

function toForm(a: Assessment, rules: AssessmentRules): FormState {
  const text = (value: unknown) => (value === null || value === undefined ? '' : String(value));
  return {
    assessedOn: a.assessedOn, measuredTime: text(a.inputs.measuredTime), energyMethod: text(a.inputs.energyMethod), activityFactor: text(a.inputs.activityFactor),
    numbers: Object.fromEntries(numberFieldsOf(a.protocol).map((def) => [def.field, text(a.inputs[def.field])])),
    classifications: Object.fromEntries(groupsOf(rules, a.protocol).map((group) => [group.field, a.classifications[group.field] ?? null])),
    observation: text(a.inputs.observation), conclusion: text(a.inputs.conclusion),
  };
}

/** Corpo do POST/PUT e da prévia: só os campos do protocolo (DIV-11); vazio vai como null. O protocolo vai só na criação e na prévia. */
function toPayload(form: FormState, protocol: Protocol) {
  const number = (value: string) => (value.trim() === '' ? null : Number(value));
  const text = (value: string) => (value.trim() === '' ? null : value.trim());
  return {
    assessedOn: form.assessedOn,
    ...(protocol === 'C' ? { measuredTime: text(form.measuredTime) } : {}),
    energyMethod: form.energyMethod === '' ? null : form.energyMethod,
    activityFactor: number(form.activityFactor),
    ...Object.fromEntries(Object.entries(form.numbers).map(([field, value]) => [field, number(value)])),
    ...form.classifications,
    observation: text(form.observation),
    conclusion: text(form.conclusion),
  };
}
```

A rota e o carregamento:

```tsx
/** Remonta o formulário quando muda a avaliação (ou de "nova" para a salva). */
export function AssessmentFormRoute() {
  const { patientId, assessmentId, protocol } = useParams();
  const me = useCurrentUser();
  if (!patientId || !can(me, 'patients.read') || !canAny(me, ASSESSMENT_PERMISSIONS)) {
    return <Alert>Você não tem permissão para acessar esta página.</Alert>;
  }
  if (assessmentId === undefined && !isProtocol(protocol)) return <Alert>Protocolo desconhecido.</Alert>;
  return (
    <AssessmentPage
      key={`${patientId}/${assessmentId ?? `nova-${protocol}`}`}
      patientId={patientId}
      assessmentId={assessmentId}
      newProtocol={isProtocol(protocol) ? protocol : null}
    />
  );
}
```

`AssessmentPage` é igual a `AnamnesisPage`, com estas diferenças:

- regras em `['assessment-rules']` (`/assessments/rules`);
- avaliação em `['assessment', assessmentId]`;
- mensagem `Avaliação não encontrada.` quando o `patientId` não confere;
- `protocol = assessment.data?.protocol ?? newProtocol`.

`AssessmentForm({ patient, rules, assessment, protocol })` segue `AnamnesisForm`: estado, efeito de recarga, `keepConflict`, `update` e `loadCurrentVersion`. Diferenças:

- **Prévia:**

```tsx
  const previewBody = useDebouncedValue(
    editable && form.assessedOn !== '' ? JSON.stringify({ protocol, ...toPayload(form, protocol) }) : null,
    PREVIEW_DELAY_MS,
  );
  const preview = useQuery({
    queryKey: ['assessment-calculation', patient.id, previewBody],
    queryFn: () =>
      api<AssessmentCalculationResult>(`/patients/${patient.id}/assessments/calculation`, { method: 'POST', json: JSON.parse(previewBody as string) }),
    enabled: previewBody !== null,
    placeholderData: keepPreviousData,
  });
  // Rascunho: o que a API calculou para o formulário atual. Finalizada: o que foi gravado (P4-5).
  const results: AssessmentCalculation | null = editable ? (preview.data?.calculation ?? null) : (assessment?.calculation ?? null);
```

- **Salvar:** a criação faz `POST /patients/${patient.id}/assessments` com `{ protocol, ...toPayload(form, protocol) }`. O rascunho faz `PUT /assessments/${assessment.id}` com `{ ...toPayload(form, protocol), version }`. Os efeitos de `onSuccess` e `onError` são os da anamnese, com as chaves `['assessments', patient.id]` e `['assessment', result.id]`. A navegação vai para `/pacientes/${patient.id}/avaliacoes/${result.id}`.
- **`loadCurrentVersion`:** grava `conflict` em `['assessment', conflict.id]`. Se `conflict.id !== assessment?.id`, navega para `/pacientes/${patient.id}/avaliacoes/${conflict.id}`; isso acontece quando o 409 traz a vigente ou a revisão aberta. Depois limpa o conflito e faz `reset` das mutações.
- **Cabeçalho**, nesta ordem:
  - título `Nova avaliação — ${PROTOCOL_LABELS[protocol]}` ou `Avaliação de ${formatDateBr(assessment.assessedOn)}`;
  - linha do paciente;
  - `${PROTOCOL_LABELS[protocol]}. O protocolo não muda depois de criado.`;
  - linha das regras, igual à da anamnese;
  - no rascunho, `Para finalizar, preencha: ${labels}.`, com `labels` = rótulos de `rules.finalizeRequired[protocol]` achados em `numberFieldsOf(protocol)` e unidos por `', '`;
  - avisos, conflito e erro geral, como na anamnese.
- **Seções**, dentro de `<fieldset disabled={!editable || busy}>`:
  1. `Avaliação`: `Data da avaliação` (obrigatória, `type="date"`); só na C, `Hora da medição` (`type="time"`, `name="measuredTime"`); e o resultado `Idade na data` (`${results.ageYears} anos` ou `—`).
  2. Uma `FormSection` por seção de `BEFORE_CLASSIFICATIONS` que tenha campos no protocolo, com o título `SECTION_TITLES[section]`. Cada campo é `<Field label={def.label} error={errors[def.field]}><Input type="number" name={def.field} step={def.integer ? 1 : 0.01} value={form.numbers[def.field] ?? ''} onChange={...} /></Field>`.
  3. `Classificações`, só se `groupsOf(rules, protocol)` não estiver vazio: um `ClassificationField` por grupo, com `clearable={editable}` e `error={errors[group.field]}`.
  4. `Metas`: os campos da seção `metas`.
  5. `Energia`:
     - `Método de TMB`: no rascunho, `Select` com `Selecione` e `rules.energyMethods`; na finalizada, `Result` com `assessment.energyMethodLabel`;
     - os campos da seção `energia`;
     - `Fator de atividade`: `Select` com a primeira opção `value=""` e o texto `` `Padrão (${formatFactor(rules.defaultActivityFactor)})` ``, e depois `rules.activityFactors`, com `value={String(f)}` e o texto `formatFactor(f)`.
  6. `Observação e conclusão`: `Textarea` `Observação` e `Conclusão`, com `maxLength={TEXT_MAX_LENGTH}`.
  7. `Resultados`: `RESULTS.filter((r) => r.protocols.includes(protocol))`, cada um como `<Result key={r.label} label={r.label} value={results ? formatResult(results[r.key]) : '—'} />`.
- **Botões:** no rascunho, só `Salvar rascunho` (`Salvando…` durante o envio).

`apps/web/src/router.tsx`: depois da rota da escolha do protocolo, acrescente `{ path: 'pacientes/:patientId/avaliacoes/nova/:protocol', element: <AssessmentFormRoute /> }` e `{ path: 'pacientes/:patientId/avaliacoes/:assessmentId', element: <AssessmentFormRoute /> }`.

- [ ] **Step 6: Rodar e ver passar**

Run: `npm test -w @psm/web; npm run typecheck -w @psm/web`
Expected: PASS com 21 arquivos. `assessment-form.test.tsx` passa nos 8 testes; `typecheck` sem erros.

- [ ] **Step 7: Commit**

```powershell
git add apps/web/src/assessments/fields.ts apps/web/src/assessments/ClassificationField.tsx apps/web/src/assessments/AssessmentFormPage.tsx apps/web/src/assessments/assessment-form.test.tsx apps/web/src/test/assessment-fixtures.ts apps/web/src/router.tsx
git commit -m "feat(web): formulário das avaliações por protocolo com prévia, classificações manuais e rascunho"
```

---

### Task 12: Web — finalização, somente leitura, exclusão do rascunho e 409

**Files:**
- Modify: `apps/web/src/assessments/AssessmentFormPage.tsx`, `apps/web/src/test/assessment-fixtures.ts`
- Test: `apps/web/src/assessments/assessment-finalize.test.tsx`

**Interfaces:**
- Consumes: `POST /api/assessments/:id/finalize` e `DELETE /api/assessments/:id?version=N` (Task 8).
- Produces:
  - as mutações `finalize` e `remove` em `AssessmentForm`, com o estado `confirming: 'finalize' | 'delete' | null`;
  - o fixture `finalizedA`.

- [ ] **Step 1: Escrever os testes**

Em `test/assessment-fixtures.ts`:

```ts
export const finalizedA: Assessment = {
  ...draftA, status: 'FINALIZADA', version: 2, currentRevisionId: 'v1', updatedAt: '2026-05-09T15:30:00.000Z', finalizedAt: '2026-05-09T15:30:00.000Z',
};
```

`apps/web/src/assessments/assessment-finalize.test.tsx`:

```tsx
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import type { Me } from '../auth/me';
import { draftA, finalizedA, mockAssessmentApi } from '../test/assessment-fixtures';
import { assistantMe } from '../test/fixtures';
import { renderApp } from '../test/render';

const draftAssistantMe: Me = { ...assistantMe, permissions: ['patients.read', 'patients.write', 'assessments.draft'] };
const FINALIZED_TEXT = /Avaliação finalizada em 09\/05\/2026.*12:30\. Não pode ser alterada\./;
const ALREADY_FINALIZED = 'Esta avaliação já foi finalizada e não pode ser alterada.';
const CONFLICT = 'Esta avaliação foi alterada por outra pessoa. Recarregue para ver a versão atual.';

describe('Finalização e exclusão da avaliação', () => {
  it('profissional finaliza: confirma, envia o formulário com a versão e a avaliação fica somente leitura', async () => {
    const { calls } = mockAssessmentApi({ 'GET /assessments/v1': { body: draftA }, 'POST /assessments/v1/finalize': { body: finalizedA } });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/avaliacoes/v1');

    await user.click(await screen.findByRole('button', { name: 'Finalizar' }));
    expect(screen.getByText('Depois de finalizada, a avaliação não pode ser alterada; uma correção vira nova revisão.')).toBeInTheDocument();
    expect(calls.some((c) => c.path.endsWith('/finalize'))).toBe(false);
    await user.click(screen.getByRole('button', { name: 'Confirmar finalização' }));

    expect(await screen.findByText('Avaliação finalizada.')).toBeInTheDocument();
    expect(calls.find((c) => c.path === '/assessments/v1/finalize')?.body).toMatchObject({
      version: 1, assessedOn: '2026-05-09', weightKg: 80, fatPct: 20, energyMethod: 'HARRIS_BENEDICT', activityFactor: 1.55,
    });
    expect(calls.find((c) => c.path === '/assessments/v1/finalize')?.body).not.toHaveProperty('protocol');
    expect(screen.getByText(FINALIZED_TEXT)).toBeInTheDocument();
    for (const name of ['Salvar rascunho', 'Finalizar', 'Excluir rascunho']) expect(screen.queryByRole('button', { name })).not.toBeInTheDocument();
    expect(screen.getByLabelText('Peso (kg)')).toBeDisabled();
  });

  it('cancelar a confirmação não finaliza', async () => {
    const { calls } = mockAssessmentApi({ 'GET /assessments/v1': { body: draftA } });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/avaliacoes/v1');

    await user.click(await screen.findByRole('button', { name: 'Finalizar' }));
    await user.click(screen.getByRole('button', { name: 'Cancelar' }));

    expect(screen.queryByRole('button', { name: 'Confirmar finalização' })).not.toBeInTheDocument();
    expect(calls.some((c) => c.path.endsWith('/finalize'))).toBe(false);
  });

  it('finalizar com obrigatório vazio (P4-2): a mensagem aparece no campo e continua rascunho', async () => {
    mockAssessmentApi({
      'GET /assessments/v1': { body: draftA },
      'POST /assessments/v1/finalize': { status: 400, body: { message: 'Dados inválidos.', errors: { fatPct: 'Informe o percentual de gordura para finalizar.' } } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/avaliacoes/v1');

    await user.click(await screen.findByRole('button', { name: 'Finalizar' }));
    await user.click(screen.getByRole('button', { name: 'Confirmar finalização' }));

    expect(await screen.findByText('Informe o percentual de gordura para finalizar.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Salvar rascunho' })).toBeEnabled();
  });

  it('assistente sem assessments.finalize salva e exclui o rascunho, mas não vê Finalizar', async () => {
    mockAssessmentApi({ 'GET /auth/me': { body: draftAssistantMe }, 'GET /assessments/v1': { body: draftA } });
    renderApp('/pacientes/p1/avaliacoes/v1');

    expect(await screen.findByRole('button', { name: 'Salvar rascunho' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Excluir rascunho' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Finalizar' })).not.toBeInTheDocument();
  });

  it('nova avaliação não oferece Finalizar nem Excluir antes do primeiro salvamento', async () => {
    mockAssessmentApi();
    renderApp('/pacientes/p1/avaliacoes/nova/B');

    expect(await screen.findByRole('button', { name: 'Salvar rascunho' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Finalizar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Excluir rascunho' })).not.toBeInTheDocument();
  });

  it('finalizada abre somente leitura, com os valores gravados, a legenda gravada do método e sem prévia', async () => {
    const { calls } = mockAssessmentApi({ 'GET /assessments/v1': { body: finalizedA } });
    renderApp('/pacientes/p1/avaliacoes/v1');

    expect(await screen.findByText(FINALIZED_TEXT)).toBeInTheDocument();
    expect(screen.getByLabelText('IMC')).toHaveTextContent('27,68');
    expect(screen.getByLabelText('Método de TMB')).toHaveTextContent('Harris-Benedict (1919) - 15 aos 74 anos');
    expect(screen.getByText('Regras avaliacao-v2.')).toBeInTheDocument();
    expect(screen.queryByText(/Para finalizar, preencha/)).not.toBeInTheDocument();
    expect(calls.filter((c) => c.path.includes('/calculation'))).toEqual([]);
  });

  it('finalizada com a avaliacao-v1 mostra a versão e a legenda gravadas, sem recalcular (H-20)', async () => {
    mockAssessmentApi({ 'GET /assessments/v1': { body: { ...finalizedA, rulesVersion: 'avaliacao-v1', energyMethodLabel: 'Harris-Benedict' } } });
    renderApp('/pacientes/p1/avaliacoes/v1');

    expect(await screen.findByText('Regras avaliacao-v1.')).toBeInTheDocument();
    expect(screen.getByLabelText('Método de TMB')).toHaveTextContent(/^Harris-Benedict$/);
  });

  it('salvar uma avaliação que outra pessoa finalizou: 409 e Carregar versão atual mostra a finalizada', async () => {
    mockAssessmentApi({
      'GET /assessments/v1': { body: draftA },
      'PUT /assessments/v1': { status: 409, body: { message: ALREADY_FINALIZED, current: finalizedA } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/avaliacoes/v1');

    await user.click(await screen.findByRole('button', { name: 'Salvar rascunho' }));
    expect(await screen.findByText(ALREADY_FINALIZED)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Carregar versão atual' }));

    expect(await screen.findByText(FINALIZED_TEXT)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Salvar rascunho' })).not.toBeInTheDocument();
  });

  it('excluir o rascunho: confirma, envia a versão e volta ao paciente com aviso; cancelar não exclui', async () => {
    const { calls } = mockAssessmentApi({ 'GET /assessments/v1': { body: draftA }, 'DELETE /assessments/v1': { status: 204 } });
    const user = userEvent.setup();
    const { router } = renderApp('/pacientes/p1/avaliacoes/v1');

    await user.click(await screen.findByRole('button', { name: 'Excluir rascunho' }));
    await user.click(screen.getByRole('button', { name: 'Cancelar' }));
    expect(calls.some((c) => c.method === 'DELETE')).toBe(false);

    await user.click(screen.getByRole('button', { name: 'Excluir rascunho' }));
    expect(screen.getByText('O rascunho será excluído. Esta ação não pode ser desfeita.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Confirmar exclusão' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/pacientes/p1'));
    expect(calls.find((c) => c.method === 'DELETE')?.path).toBe('/assessments/v1?version=1');
    expect(await screen.findByText('Rascunho excluído.')).toBeInTheDocument();
  });

  it('excluir com versão antiga: 409 e Carregar versão atual', async () => {
    mockAssessmentApi({
      'GET /assessments/v1': { body: draftA },
      'DELETE /assessments/v1': { status: 409, body: { message: CONFLICT, current: { ...draftA, version: 2 } } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/avaliacoes/v1');

    await user.click(await screen.findByRole('button', { name: 'Excluir rascunho' }));
    await user.click(screen.getByRole('button', { name: 'Confirmar exclusão' }));

    expect(await screen.findByText(CONFLICT)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Carregar versão atual' })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/web -- assessment-finalize`
Expected: FAIL. Não há botões "Finalizar" nem "Excluir rascunho", e o aviso de finalizada não aparece.

- [ ] **Step 3: Implementar**

Em `AssessmentForm`, no padrão da Task 9 do Plano 2:

```tsx
  const me = useCurrentUser();
  const [confirming, setConfirming] = useState<'finalize' | 'delete' | null>(null);
  const canFinalize = !isNew && editable && can(me, 'assessments.finalize');
  const canDelete = !isNew && editable && canAny(me, ASSESSMENT_PERMISSIONS);

  const finalize = useMutation({
    mutationFn: () => api<Assessment>(`/assessments/${assessment?.id}/finalize`, { method: 'POST', json: { ...toPayload(form, protocol), version } }),
    onSuccess: (result) => {
      setConflict(null);
      setConfirming(null);
      void queryClient.invalidateQueries({ queryKey: ['assessments', patient.id] });
      queryClient.setQueryData(['assessment', result.id], result);
      setNotice('Avaliação finalizada.');
    },
    onError: (error) => {
      setNotice(null);
      setConfirming(null);
      keepConflict(error);
    },
  });

  const remove = useMutation({
    mutationFn: () => api<void>(`/assessments/${assessment?.id}?version=${version}`, { method: 'DELETE' }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['assessments', patient.id] });
      navigate(`/pacientes/${patient.id}`, { replace: true, state: { assessmentNotice: 'Rascunho excluído.' } });
    },
    onError: (error) => {
      setNotice(null);
      setConfirming(null);
      keepConflict(error);
    },
  });
```

Ajustes:

- `lastError = save.error ?? finalize.error ?? remove.error`;
- `busy` inclui `finalize.isPending` e `remove.isPending`;
- `loadCurrentVersion` faz `reset` das três mutações;
- o envio do formulário faz `finalize.reset(); remove.reset();` antes de `save.mutate()`.

Logo depois do título, mostre o aviso da finalizada:

```tsx
      {assessment?.status === 'FINALIZADA' && assessment.finalizedAt ? (
        <Alert tone="info">{`Avaliação finalizada em ${formatDateTimeBr(assessment.finalizedAt)}. Não pode ser alterada.`}</Alert>
      ) : null}
```

Botões do rascunho, depois de `Salvar rascunho`:

- `Finalizar` (`variant="secondary"`), se `canFinalize && confirming === null`;
- `Excluir rascunho` (`variant="danger"`), se `canDelete && confirming === null`;
- confirmação da finalização (`Alert tone="info"`): texto `Depois de finalizada, a avaliação não pode ser alterada; uma correção vira nova revisão.`, botões `Confirmar finalização` (`Finalizando…` durante o envio; faz `save.reset(); remove.reset(); finalize.mutate();`) e `Cancelar`;
- confirmação da exclusão (`Alert tone="info"`): texto `O rascunho será excluído. Esta ação não pode ser desfeita.`, botões `Confirmar exclusão` (`variant="danger"`; `Excluindo…` durante o envio; faz `save.reset(); finalize.reset(); remove.mutate();`) e `Cancelar`.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/web; npm run typecheck -w @psm/web`
Expected: PASS com 22 arquivos. `assessment-finalize.test.tsx` passa nos 10 testes, e `assessments-section.test.tsx` continua verde.

- [ ] **Step 5: Commit**

```powershell
git add apps/web/src/assessments/AssessmentFormPage.tsx apps/web/src/assessments/assessment-finalize.test.tsx apps/web/src/test/assessment-fixtures.ts
git commit -m "feat(web): finalização com confirmação, leitura da finalizada e exclusão do rascunho"
```

---

### Task 13: Web — revisões, histórico e somente leitura da revisão

**Files:**
- Create: `apps/web/src/assessments/RevisionsPanel.tsx`
- Modify: `apps/web/src/assessments/AssessmentFormPage.tsx`, `apps/web/src/test/assessment-fixtures.ts`
- Test: `apps/web/src/assessments/assessment-revisions.test.tsx`

**Interfaces:**
- Consumes:
  - `POST /api/assessments/:id/revisions` e `GET /api/assessments/:id/revisions` (Task 9);
  - `changeLabel` e `formatChangeValue` (Task 11);
  - `formatDateTimeBr`.
- Produces:
  - `RevisionsPanel({ assessment, patientId, groups })`, com a consulta `['assessment-revisions', assessment.seriesId]`;
  - a mutação `createRevision` em `AssessmentForm`;
  - os fixtures `revisionDraft`, `finalizedWithOpen`, `supersededA`, `revisionFinal` e `revisionItems`.

- [ ] **Step 1: Escrever os testes**

Em `test/assessment-fixtures.ts`:

```ts
export const revisionDraft: Assessment = {
  ...draftA, id: 'v2', seriesId: 'v1', revision: 2, revisionReason: 'Peso digitado errado.', currentRevisionId: 'v1', openRevisionId: 'v2',
};
export const finalizedWithOpen: Assessment = { ...finalizedA, openRevisionId: 'v2' };
export const supersededA: Assessment = { ...finalizedA, version: 3, supersededAt: '2026-05-10T12:00:00.000Z', currentRevisionId: 'v2' };
export const revisionFinal: Assessment = {
  ...revisionDraft, status: 'FINALIZADA', version: 3, finalizedAt: '2026-05-10T12:00:00.000Z', currentRevisionId: 'v2', openRevisionId: null,
  inputs: { ...draftA.inputs, weightKg: 81, neckCm: 35 }, revisionChanges: { 'inputs.weightKg': { from: 80, to: 81 }, 'inputs.neckCm': { from: 34, to: 35 } },
};
export const revisionItems: AssessmentRevisionItem[] = [
  {
    id: 'v2', revision: 2, status: 'FINALIZADA', assessedOn: '2026-05-09', rulesVersion: 'avaliacao-v2', revisionReason: 'Peso digitado errado.',
    revisionChanges: { 'inputs.weightKg': { from: 80, to: 81 }, 'inputs.neckCm': { from: null, to: 35 } }, createdAt: '2026-05-10T11:00:00.000Z',
    createdByName: 'Paula Prof', finalizedAt: '2026-05-10T12:00:00.000Z', finalizedByName: 'Paula Prof', supersededAt: null,
  },
  {
    id: 'v1', revision: 1, status: 'FINALIZADA', assessedOn: '2026-05-09', rulesVersion: 'avaliacao-v2', revisionReason: null, revisionChanges: null,
    createdAt: '2026-05-09T13:00:00.000Z', createdByName: 'Artur Assistente', finalizedAt: '2026-05-09T15:30:00.000Z', finalizedByName: 'Paula Prof',
    supersededAt: '2026-05-10T12:00:00.000Z',
  },
];
```

`apps/web/src/assessments/assessment-revisions.test.tsx`:

```tsx
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import type { Me } from '../auth/me';
import {
  finalizedA, finalizedWithOpen, mockAssessmentApi, revisionDraft, revisionFinal, revisionItems, supersededA,
} from '../test/assessment-fixtures';
import { assistantMe, professionalMe } from '../test/fixtures';
import type { MockResponse } from '../test/mock-api';
import { renderApp } from '../test/render';

const FORBIDDEN: MockResponse = { status: 403, body: { statusCode: 403, message: 'Você não tem permissão para esta ação.' } };
const draftAssistantMe: Me = { ...assistantMe, permissions: ['patients.read', 'patients.write', 'assessments.draft', 'history.read'] };
const noHistoryMe: Me = { ...professionalMe, permissions: professionalMe.permissions.filter((p) => p !== 'history.read') };
const OPEN_REVISION = 'Já existe uma revisão em andamento desta avaliação.';

describe('Revisões da avaliação', () => {
  it('Nova revisão: exige justificativa; cria o rascunho da revisão e abre-o com aviso', async () => {
    const { calls } = mockAssessmentApi({
      'GET /assessments/v1': { body: finalizedA },
      'POST /assessments/v1/revisions': { status: 201, body: revisionDraft },
      'GET /assessments/v2/revisions': { body: revisionItems },
    });
    const user = userEvent.setup();
    const { router } = renderApp('/pacientes/p1/avaliacoes/v1');

    await user.click(await screen.findByRole('button', { name: 'Nova revisão' }));
    const create = screen.getByRole('button', { name: 'Criar revisão' });
    expect(create).toBeDisabled();
    await user.type(screen.getByLabelText('Justificativa da revisão *'), '   ');
    expect(create).toBeDisabled();
    await user.type(screen.getByLabelText('Justificativa da revisão *'), 'Peso digitado errado.');
    await user.click(create);

    await waitFor(() => expect(router.state.location.pathname).toBe('/pacientes/p1/avaliacoes/v2'));
    expect(calls.find((c) => c.path === '/assessments/v1/revisions')?.body).toEqual({ reason: '   Peso digitado errado.' });
    expect(await screen.findByText('Revisão criada. Altere os dados e finalize para que ela passe a valer.')).toBeInTheDocument();
    expect(screen.getByText('Revisão 2. Justificativa: Peso digitado errado.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Salvar rascunho' })).toBeEnabled();
  });

  it('quem não finaliza não vê Nova revisão', async () => {
    mockAssessmentApi({ 'GET /auth/me': { body: draftAssistantMe }, 'GET /assessments/v1': { body: finalizedA } });
    renderApp('/pacientes/p1/avaliacoes/v1');

    expect(await screen.findByText(/Avaliação finalizada em/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Nova revisão' })).not.toBeInTheDocument();
  });

  it('revisão em rascunho para quem só preenche rascunhos: somente leitura, sem prévia, Salvar, Finalizar nem Descartar', async () => {
    const { calls } = mockAssessmentApi({
      'GET /auth/me': { body: draftAssistantMe },
      'GET /assessments/v2': { body: revisionDraft },
      'GET /assessments/v2/revisions': { body: revisionItems },
    });
    renderApp('/pacientes/p1/avaliacoes/v2');

    expect(await screen.findByText('Somente quem finaliza avaliações pode alterar esta revisão.')).toBeInTheDocument();
    expect(screen.getByLabelText('Peso (kg)')).toBeDisabled();
    for (const name of ['Salvar rascunho', 'Finalizar', 'Descartar revisão', 'Excluir rascunho']) {
      expect(screen.queryByRole('button', { name })).not.toBeInTheDocument();
    }
    expect(calls.filter((c) => c.path.includes('/calculation'))).toEqual([]);
  });

  it('descartar a revisão em rascunho: confirma e volta à versão vigente com aviso', async () => {
    const { calls } = mockAssessmentApi({
      'GET /assessments/v2': { body: revisionDraft },
      'GET /assessments/v2/revisions': { body: revisionItems },
      'DELETE /assessments/v2': { status: 204 },
      'GET /assessments/v1': { body: finalizedA },
    });
    const user = userEvent.setup();
    const { router } = renderApp('/pacientes/p1/avaliacoes/v2');

    await user.click(await screen.findByRole('button', { name: 'Descartar revisão' }));
    expect(screen.getByText('A revisão em rascunho será descartada; a versão vigente continua valendo.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Confirmar exclusão' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/pacientes/p1/avaliacoes/v1'));
    expect(calls.find((c) => c.method === 'DELETE')?.path).toBe('/assessments/v2?version=1');
    expect(await screen.findByText('Revisão descartada.')).toBeInTheDocument();
  });

  it('vigente com revisão em andamento: aviso com link, sem Nova revisão', async () => {
    mockAssessmentApi({ 'GET /assessments/v1': { body: finalizedWithOpen }, 'GET /assessments/v1/revisions': { body: revisionItems } });
    renderApp('/pacientes/p1/avaliacoes/v1');

    expect(await screen.findByText('Há uma revisão em andamento desta avaliação.')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Abrir a revisão em andamento' })).toHaveAttribute('href', '/pacientes/p1/avaliacoes/v2');
    expect(screen.queryByRole('button', { name: 'Nova revisão' })).not.toBeInTheDocument();
  });

  it('revisão substituída: aviso, link para a vigente e sem Nova revisão', async () => {
    mockAssessmentApi({ 'GET /assessments/v1': { body: supersededA }, 'GET /assessments/v1/revisions': { body: revisionItems } });
    renderApp('/pacientes/p1/avaliacoes/v1');

    expect(await screen.findByText(/Esta versão foi substituída em 10\/05\/2026.*09:00\. Ela não vale mais\./)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Abrir a versão vigente' })).toHaveAttribute('href', '/pacientes/p1/avaliacoes/v2');
    expect(screen.queryByRole('button', { name: 'Nova revisão' })).not.toBeInTheDocument();
  });

  it('histórico (history.read): revisões, autores, justificativa e campos alterados com rótulos', async () => {
    mockAssessmentApi({ 'GET /assessments/v2': { body: revisionFinal }, 'GET /assessments/v2/revisions': { body: revisionItems } });
    renderApp('/pacientes/p1/avaliacoes/v2');

    const panel = await screen.findByRole('region', { name: 'Revisões' });
    await within(panel).findByText('Justificativa: Peso digitado errado.');
    const [second, first] = Array.from(panel.querySelectorAll<HTMLElement>('ol > li'));
    expect(second).toHaveTextContent(/Revisão 2 — finalizada em 10\/05\/2026.*09:00 por Paula Prof; vigente/);
    expect(second).toHaveTextContent('Justificativa: Peso digitado errado.');
    expect(within(second).getAllByRole('listitem').map((li) => li.textContent)).toEqual(['Peso (kg): 80 → 81', 'Pescoço (cm): — → 35']);
    expect(within(first).getByRole('link', { name: 'Revisão 1' })).toHaveAttribute('href', '/pacientes/p1/avaliacoes/v1');
    expect(first).toHaveTextContent(/substituída em 10\/05\/2026/);
  });

  it('sem history.read: sem histórico e nada buscado', async () => {
    const { calls } = mockAssessmentApi({ 'GET /auth/me': { body: noHistoryMe }, 'GET /assessments/v2': { body: revisionFinal } });
    renderApp('/pacientes/p1/avaliacoes/v2');

    expect(await screen.findByText(/Avaliação finalizada em/)).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Revisões' })).not.toBeInTheDocument();
    expect(calls.filter((c) => c.path.endsWith('/revisions'))).toEqual([]);
  });

  it('403 no histórico não derruba a avaliação aberta', async () => {
    mockAssessmentApi({ 'GET /assessments/v2': { body: revisionFinal }, 'GET /assessments/v2/revisions': FORBIDDEN });
    renderApp('/pacientes/p1/avaliacoes/v2');

    const panel = await screen.findByRole('region', { name: 'Revisões' });
    expect(await within(panel).findByText('Você não tem permissão para esta ação.')).toBeInTheDocument();
    expect(screen.getByLabelText('IMC')).toHaveTextContent('27,68');
  });

  it('nova revisão com outra em andamento: 409 e Carregar versão atual abre a revisão aberta', async () => {
    mockAssessmentApi({
      'GET /assessments/v1': { body: finalizedA },
      'POST /assessments/v1/revisions': { status: 409, body: { message: OPEN_REVISION, current: revisionDraft } },
      'GET /assessments/v2/revisions': { body: revisionItems },
    });
    const user = userEvent.setup();
    const { router } = renderApp('/pacientes/p1/avaliacoes/v1');

    await user.click(await screen.findByRole('button', { name: 'Nova revisão' }));
    await user.type(screen.getByLabelText('Justificativa da revisão *'), 'Outra correção.');
    await user.click(screen.getByRole('button', { name: 'Criar revisão' }));
    expect(await screen.findByText(OPEN_REVISION)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Carregar versão atual' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/pacientes/p1/avaliacoes/v2'));
  });
});
```

No teste do histórico, as duas linhas da `<ol>` vêm na ordem da API: a revisão 2 (`second`) e depois a 1 (`first`).

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/web -- assessment-revisions`
Expected: FAIL. Não há "Nova revisão", nem avisos de revisão, nem o painel "Revisões".

- [ ] **Step 3: Implementar o painel**

`apps/web/src/assessments/RevisionsPanel.tsx`:

```tsx
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { Alert } from '../components/ui/Alert';
import { api } from '../lib/api';
import { errorMessage } from '../lib/error-message';
import { formatDateTimeBr } from '../lib/format';
import { changeLabel, formatChangeValue } from './fields';
import type { Assessment, AssessmentRevisionItem, ClassificationGroup } from './types';

function revisionStatus(item: AssessmentRevisionItem): string {
  if (item.status === 'RASCUNHO') return `em rascunho, aberta por ${item.createdByName ?? '—'}`;
  const finalized = `finalizada em ${formatDateTimeBr(item.finalizedAt as string)} por ${item.finalizedByName ?? '—'}`;
  return item.supersededAt ? `${finalized}; substituída em ${formatDateTimeBr(item.supersededAt)}` : `${finalized}; vigente`;
}

/** Histórico de revisões da série (history.read): autor, data, justificativa e campos alterados (P4-6). */
export function RevisionsPanel({ assessment, patientId, groups }: { assessment: Assessment; patientId: string; groups: readonly ClassificationGroup[] }) {
  const revisions = useQuery({
    queryKey: ['assessment-revisions', assessment.seriesId],
    queryFn: () => api<AssessmentRevisionItem[]>(`/assessments/${assessment.id}/revisions`),
  });
  const items = revisions.error ? undefined : revisions.data;

  return (
    <section aria-labelledby="revisoes-titulo" className="space-y-3 rounded-lg border border-areia bg-white p-6">
      <h2 id="revisoes-titulo" className="font-display text-xl text-terracota">
        Revisões
      </h2>
      {revisions.error ? <Alert>{errorMessage(revisions.error)}</Alert> : null}
      {revisions.isPending ? <p className="text-sm">Carregando…</p> : null}
      {items ? (
        <ol className="space-y-3">
          {items.map((item) => (
            <li key={item.id} className="border-t border-areia pt-3 text-sm">
              <p>
                {item.id === assessment.id ? (
                  <strong>{`Revisão ${item.revision}`}</strong>
                ) : (
                  <Link to={`/pacientes/${patientId}/avaliacoes/${item.id}`} className="text-terracota underline">
                    {`Revisão ${item.revision}`}
                  </Link>
                )}
                {` — ${revisionStatus(item)}`}
              </p>
              {item.revisionReason ? <p>{`Justificativa: ${item.revisionReason}`}</p> : null}
              {item.revisionChanges && Object.keys(item.revisionChanges).length > 0 ? (
                <ul className="list-disc pl-5">
                  {Object.entries(item.revisionChanges).map(([key, change]) => (
                    <li key={key}>{`${changeLabel(key, groups)}: ${formatChangeValue(change.from)} → ${formatChangeValue(change.to)}`}</li>
                  ))}
                </ul>
              ) : null}
            </li>
          ))}
        </ol>
      ) : null}
    </section>
  );
}
```

- [ ] **Step 4: Implementar as revisões no formulário**

Em `AssessmentForm`:

1. **Quem edita** (resposta c):

```tsx
  const isRevision = assessment !== null && assessment.revision > 1;
  const finalized = assessment?.status === 'FINALIZADA';
  // Revisão maior que 1: só quem finaliza edita, finaliza ou descarta.
  const editable = !finalized && (!isRevision || can(me, 'assessments.finalize'));
```

   `me` passa para o topo do componente. `canDelete` passa a `!isNew && editable`: a regra de revisão já está em `editable`, e quem chega ao formulário tem `assessments.draft` ou `assessments.finalize`. `canFinalize` continua `!isNew && editable && can(me, 'assessments.finalize')`.
2. **Avisos**, logo depois do aviso da finalizada (que passa a testar `finalized`):
   - se `isRevision`: `<p className="text-sm">{\`Revisão ${assessment.revision}. Justificativa: ${assessment.revisionReason}\`}</p>`;
   - se `isRevision && !finalized && !editable`: `<Alert tone="info">Somente quem finaliza avaliações pode alterar esta revisão.</Alert>`;
   - se `assessment?.supersededAt`: `<Alert tone="info">` com o texto `` `Esta versão foi substituída em ${formatDateTimeBr(assessment.supersededAt)}. Ela não vale mais.` `` e o link `Abrir a versão vigente` para `/pacientes/${patient.id}/avaliacoes/${assessment.currentRevisionId}`;
   - se `finalized && !assessment.supersededAt && assessment.openRevisionId`: `<Alert tone="info">` com `Há uma revisão em andamento desta avaliação.` e o link `Abrir a revisão em andamento` para `/pacientes/${patient.id}/avaliacoes/${assessment.openRevisionId}`.
3. **Exclusão da revisão:**
   - com `isRevision`, o botão se chama `Descartar revisão`, e a confirmação diz `A revisão em rascunho será descartada; a versão vigente continua valendo.`;
   - o `onSuccess` de `remove` passa a:

```tsx
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['assessments', patient.id] });
      if (assessment && assessment.revision > 1 && assessment.currentRevisionId) {
        void queryClient.invalidateQueries({ queryKey: ['assessment', assessment.currentRevisionId] });
        void queryClient.invalidateQueries({ queryKey: ['assessment-revisions', assessment.seriesId] });
        navigate(`/pacientes/${patient.id}/avaliacoes/${assessment.currentRevisionId}`, { replace: true, state: { notice: 'Revisão descartada.' } });
      } else {
        navigate(`/pacientes/${patient.id}`, { replace: true, state: { assessmentNotice: 'Rascunho excluído.' } });
      }
    },
```

4. **Finalização de revisão:** no `onSuccess` de `finalize`, quando `assessment.revision > 1`, acrescente `invalidateQueries` de `['assessment', assessment.currentRevisionId]` (a anterior passou a substituída) e de `['assessment-revisions', assessment.seriesId]`.
5. **Nova revisão** (P4-6), só quando `finalized && !assessment.supersededAt && !assessment.openRevisionId && can(me, 'assessments.finalize')`:

```tsx
  const [reason, setReason] = useState<string | null>(null);
  const createRevision = useMutation({
    mutationFn: () => api<Assessment>(`/assessments/${assessment?.id}/revisions`, { method: 'POST', json: { reason } }),
    onSuccess: (result) => {
      setConflict(null);
      setReason(null);
      void queryClient.invalidateQueries({ queryKey: ['assessments', patient.id] });
      void queryClient.invalidateQueries({ queryKey: ['assessment', assessment?.id] });
      void queryClient.invalidateQueries({ queryKey: ['assessment-revisions', result.seriesId] });
      queryClient.setQueryData(['assessment', result.id], result);
      navigate(`/pacientes/${patient.id}/avaliacoes/${result.id}`, {
        state: { notice: 'Revisão criada. Altere os dados e finalize para que ela passe a valer.' },
      });
    },
    onError: (error) => {
      setNotice(null);
      keepConflict(error);
    },
  });
```

   A interface:
   - com `reason === null`, mostra o botão `Nova revisão` (`variant="secondary"`), que faz `setReason('')`;
   - senão, mostra um bloco (`Alert tone="info"`) com:
     - `<Field label="Justificativa da revisão" required error={fieldErrors(createRevision.error).reason}>`, com um `Textarea` (`name="reason"`, `rows={3}`, `maxLength={2000}`);
     - o botão `Criar revisão`, desabilitado com `reason.trim() === ''` ou durante o envio (`Criando…`);
     - o botão `Cancelar` (`setReason(null)`).

   O texto vai como foi digitado; a API tira os espaços das pontas.

   `lastError` passa a incluir `createRevision.error`, e `loadCurrentVersion` faz `reset` dela também.
6. **Painel:** depois do `</form>`, `{!isNew && can(me, 'history.read') && (isRevision || assessment.supersededAt || assessment.openRevisionId) ? <RevisionsPanel assessment={assessment} patientId={patient.id} groups={groupsOf(rules, protocol)} /> : null}`. A primeira avaliação, sem revisões, não busca o histórico.

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/web; npm run typecheck -w @psm/web; npm run build -w @psm/web`
Expected: PASS com 23 arquivos. `assessment-revisions.test.tsx` passa nos 10 testes, e os das Tasks 10 a 12 continuam verdes. `typecheck` sem erros; o `build` gera `apps/web/dist/`.

- [ ] **Step 6: Commit**

```powershell
git add apps/web/src/assessments/RevisionsPanel.tsx apps/web/src/assessments/AssessmentFormPage.tsx apps/web/src/assessments/assessment-revisions.test.tsx apps/web/src/test/assessment-fixtures.ts
git commit -m "feat(web): revisões das avaliações com justificativa, histórico e leitura da substituída"
```

---

### Task 14: Verificação ponta a ponta e README

**Files:**
- Modify: `README.md`

**Interfaces:**
- Consumes: tudo o que as Tasks 1 a 13 produziram.
- Produces: o `README.md` com a entrega e a verificação manual do Plano 4.

- [ ] **Step 1: Rodar toda a verificação automática**

Run: `npm test; npm run typecheck; npm run build`
Expected:

- API com 56 suítes verdes e 2 instantâneos (`avaliacao-rules.spec.ts.snap` sem modificação).
- Web com 23 arquivos de teste verdes.
- `typecheck` sem erros.
- `apps/api/dist/` e `apps/web/dist/` gerados.
- `git grep -n "TODO(PREMISSA-CLINICA"` lista PC-01 a PC-14, nos arquivos indicados em `docs/premissas-clinicas.md`.
- `git diff master --stat -- apps/api/src/clinical-rules/__snapshots__/avaliacao-rules.spec.ts.snap` vazio.

- [ ] **Step 2: Subir o ambiente**

```powershell
npm run db:up
Set-Location apps/api; npx prisma migrate deploy; npx prisma generate; Set-Location ../..
```

Em dois terminais, rode `npm run dev -w @psm/api` e `npm run dev -w @psm/web`, e abra `http://localhost:5173`. Os usuários `admin`, `prof` e `assist` dos roteiros anteriores continuam valendo; se não existirem, crie-os em Usuários.

- [ ] **Step 3: PONTO DE CONTROLE DA PESSOA — roteiro manual**

A pessoa executa o roteiro e anota OK ou a divergência em cada item. Use só dados fictícios.

1. Como `admin`, cadastre "Paciente Teste Avaliação" (Feminino, 09/05/1986) e "Paciente Teste C" (Masculino, 09/05/1991).
2. Como `assist`, abra "Paciente Teste Avaliação".
   - Esperado: seção "Avaliações" com "Nenhuma avaliação registrada.".
   - "Nova avaliação" mostra os três protocolos, com a frase de que o protocolo não muda.
3. Escolha o Protocolo A e preencha:
   - data 09/05/2026, peso 80, altura 170, cintura 80, quadril 100, gordura 20, punho 50, fêmur 90;
   - método "Harris-Benedict (1919) - 15 aos 74 anos", fator 1,55.

   Esperado:
   - IMC 27,68 e "Sobrepeso";
   - gordura 16,00 kg e "Bom";
   - peso residual 16,72; peso ósseo 9,77; peso muscular 37,51;
   - RCQ 0,80 e "Risco Alto";
   - TMB 1.548,00 e GET 2.399,40.

   Salve.
   - Esperado: "Rascunho salvo.", e nenhum botão "Finalizar" para o assistente.
   - Recarregue (F5). Esperado: tudo continua preenchido.
4. Mude o peso para 0. Esperado: "O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.". Volte para 80 e salve.
5. Crie uma avaliação "Protocolo B/R" para a mesma paciente e preencha:
   - data 09/05/2026, peso 70, altura 165, cintura 75, quadril 98;
   - IMC do aparelho 25,7, gordura 30, músculo esquelético 28, gordura visceral 6, idade corporal 42;
   - TMB do aparelho 1350, método "Calculado pela Bioimpedância", fator 1,2.

   Esperado:
   - intervalos "18,5 - 24,9", "23 - 33,9", "24,1 - 30,1", "<= 9" e "0,73 - 0,79";
   - TMB 1.350,00 e GET 1.620,00.

   Nas classificações, confira as legendas do FrmBioimpR em cada grupo. Marque IMC "+ (Sobrepeso)" e gordura "Normal (0)" e salve.
   - Recarregue. Esperado: as duas opções continuam marcadas.
   - Clique em "Limpar classificação" na gordura, salve e recarregue. Esperado: a gordura fica sem opção.
6. Em "Paciente Teste C", crie uma avaliação "Protocolo C" e preencha:
   - data 09/05/2026, hora 08:30, peso 70, altura 175;
   - IMC do aparelho 22,9; gordura 14 kg, massa óssea 3,1, proteína 11,7, água 41,3, massa muscular 52,9, músculo esquelético 30,2;
   - pontuação 120.

   Esperado: gordura 20,00%; massa óssea 4,43%; proteína 16,71%; água 59,00%; taxa muscular 75,57%; músculo esquelético 43,14%; peso total 100,00%.

   Mude a pontuação para 1000. Esperado: "A pontuação deve estar entre 0 e 999, sem casas decimais.". Volte para 120.
7. H-16: na mesma C, marque e salve:
   - Massa muscular "Alto";
   - Músculo esquelético "Alto";
   - Água corporal "Excelente";
   - Proteína "Alto";
   - Massa óssea "Alto".

   Recarregue. Esperado: as cinco continuam marcadas. Troque Proteína para "Baixo", salve e recarregue. Esperado: "Baixo".
8. Crie um rascunho A qualquer e clique em "Excluir rascunho" e depois em "Confirmar exclusão".
   - Esperado: volta ao paciente com "Rascunho excluído.", e a avaliação some da lista.
9. Concorrência:
   - em outra janela anônima, entre como `prof`, abra a avaliação A do item 3 e finalize-a;
   - volte à janela do `assist` (sem recarregar), mude algo e salve.

   Esperado: "Esta avaliação já foi finalizada e não pode ser alterada." e, em "Carregar versão atual", a avaliação somente leitura, com "Avaliação finalizada em …".
10. Como `prof`, tente finalizar a B do item 5 sem o IMC do aparelho. Esperado: "Informe o IMC do aparelho para finalizar." no campo, e a avaliação continua rascunho.
11. Revisão:
    - como `prof`, na A finalizada, clique em "Nova revisão", escreva a justificativa "Peso digitado errado." e crie;
    - mude o peso para 81, salve e finalize.

    Esperado:
    - a lista do paciente mostra "Finalizada (revisão 2)";
    - o painel "Revisões" mostra "Peso (kg): 80 → 81", autor e data;
    - em "Revisão 1", o aviso "Esta versão foi substituída em …" e o link para a vigente.
12. Crie outra revisão na vigente e, como `assist`, abra essa revisão em rascunho.
    - Esperado: "Somente quem finaliza avaliações pode alterar esta revisão.", sem botões de salvar ou descartar.
    - Como `prof`, descarte-a. Esperado: "Revisão descartada." e a revisão 2 continua vigente.
13. Como `admin`, em Permissões, desmarque "Consultar histórico" para o assistente. Como `assist`:
    - abra a revisão 2: o painel "Revisões" não aparece;
    - abra a revisão 1 pelo endereço: "Você não tem permissão para esta ação.".

    Marque a permissão de novo.
14. Como `admin`, desmarque "Preencher rascunhos de avaliação". Como `assist`, a seção "Avaliações" não aparece. Marque de novo.
15. Como `admin`, abra Auditoria. Esperado: aparecem, com autor e horário de Brasília:
    - `assessment.create`, `assessment.update`, `assessment.finalize`, `assessment.delete` e `assessment.revision.create`;
    - a finalização da revisão com `supersededId`.

Só siga para o Step 4 com os 15 itens OK ou com as divergências aceitas pela pessoa.

- [ ] **Step 4: Atualizar o README**

Em `README.md`, na seção do Plano 3, troque a linha `Fora deste plano: gravação das avaliações, …` por `Fora deste plano: gravação das avaliações, telas e revisões (entregues no Plano 4).`. Acrescente ao fim:

```markdown
## O que o Plano 4 entrega

Avaliações A, B/R e C por paciente:

- rascunho (`assessments.draft` ou `assessments.finalize`) e finalização (`assessments.finalize`), com critérios por protocolo (premissa PC-14);
- cada salvamento grava as entradas do protocolo, a versão das regras (`avaliacao-v2`) e o resultado do cálculo; a leitura nunca recalcula;
- telas por protocolo com a prévia, todos os campos que cada protocolo grava na aba Avaliações e as classificações manuais da B (FrmBioimpR) e da C (13 grupos, inclusive as opções que o legado não transferia);
- exclusão auditada do rascunho; a finalizada nunca é excluída;
- edição concorrente com versão do registro (409 com o estado atual);
- revisões das finalizadas, com justificativa, autor, data e campos alterados; as anteriores ficam consultáveis com "Consultar histórico".

Fora deste plano: vínculo das revisões com os PDFs (Plano 5), OCR (Plano 6), revisões da anamnese e retenção (Plano 7).

## Verificação do Plano 4

Data: <AAAA-MM-DD>. Roteiro manual da Task 14, itens 1–15: <OK ou divergências aceitas>.
```

Preencha a data e o resultado com o que a pessoa anotou no Step 3.

- [ ] **Step 5: Commit**

```powershell
git add README.md
git commit -m "docs: entrega e verificação do Plano 4 (avaliações e revisões)"
```

---

## Fora do escopo deste plano

- Vínculo de cada revisão com o PDF emitido e com a versão do template (H-20, segunda metade): Plano 5. A pontuação gravada (nova DIV) e a origem manual dos campos do aparelho entram no laudo do Plano 5.
- OCR do Relaxmedic (Plano 6). Os campos da C são digitados à mão até lá, e o OCR não pode sobrescrever em silêncio o que foi digitado.
- Revisões da anamnese: depois, reaproveitando o mecanismo de séries deste plano.
- Política de retenção e exclusão de finalizadas: Plano 7.
- Validação clínica da PC-14 (critérios de finalização) e das PC-05 a PC-13: feita pelo médico responsável, fora do código. Uma correção vira `avaliacao-v3`.
- Origem por campo (manual, OCR, cálculo) proposta no dicionário: entra com o OCR, quando houver mais de uma origem.
- Consulta à planilha antiga para o histórico anterior à web: fora da aplicação, conforme a consolidação.
