# Plano 2 — Anamnese (psm-web) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar a anamnese na web, com estes itens:

- formulário com 22 perguntas Sim/Não, todas começando sem resposta, e 11 textos livres;
- FC máxima e FC de reserva, água ideal, escala de urina e classificação da pressão arterial, todas calculadas pela API com regras versionadas;
- rascunho e finalização, cada um com sua permissão;
- histórico de anamneses por paciente;
- detecção de edição concorrente.

**Architecture:** As regras clínicas ficam em módulos puros (`apps/api/src/clinical-rules/`), sem banco e sem interface. Esses módulos leem uma configuração versionada (`anamnese-v1`), que reúne faixas, fatores, nomes e fontes. O novo módulo `anamneses` da API valida os dados contra o cadastro do paciente, calcula os valores, grava em cada anamnese a versão das regras usada e congela tudo na finalização. A web não faz cálculo clínico: ela exibe a prévia que vem de um endpoint de cálculo, que não grava nada, e envia o formulário. O conflito de edição usa versão otimista, igual ao cadastro de pacientes: `updateMany` condicionado à versão e ao status, com 409 e `current`.

**Tech Stack:** a mesma do Plano 1, sem dependência nova: Node 24, npm ≥ 10, NestJS 11, Prisma 6, PostgreSQL 16 (Docker, porta 5433), Jest 29 + Supertest, React 19, Vite 6, React Router 7, TanStack Query 5, Tailwind 4, Vitest 3 + Testing Library.

**Spec:**
- Divergências e homologação: [divergencias-e-homologacao.md](../../migracao-web/divergencias-e-homologacao.md). Divergências DIV-04, DIV-07, DIV-13 e DIV-15; casos H-12, H-13, H-14 e H-22; §5, portões por plano.
- Regras: [catalogo-regras.md](../../migracao-web/catalogo-regras.md), §5 Anamnese (AN-01 a AN-06).
- Campos: [dicionario-dados.md](../../migracao-web/dicionario-dados.md), aba Anamnese (48 colunas).
- Permissões: [consolidação](../specs/2026-09-27-migracao-web-brainstorming.md), §5.
- Decisões da pessoa de 2026-10-04: seção "Decisões incorporadas", abaixo.

Os caminhos de código abaixo são relativos a `C:\Sistema\PSM\psm-web`. Os comandos são em PowerShell e rodam nessa pasta, salvo indicação. Este plano foi escrito sobre o código real do psm-web na branch `master`, no commit `d33e30e`, e não sobre o texto do Plano 1.

## Decisões incorporadas (2026-10-04)

Todas foram tomadas pela pessoa. O executor não as reabre. Se encontrar algo que contradiga uma delas, registre em "Decisões e bloqueios" e pare.

1. **Escala de urina (DIV-04, AN-04, H-12).** A opção marcada é a escala gravada, inclusive na reabertura. Legendas: 1 e 2, Bem Hidratado; 3 e 4, Levemente Desidratado; 5 e 6, Moderadamente Desidratado; 7 e 8, Severamente Desidratado.
2. **Pressão arterial (DIV-07, AN-05, H-22). Decisão provisória, a validar com o médico responsável.** A fonte é a Diretriz Brasileira de Hipertensão Arterial – 2025 (SBC, SBH, SBN), Arq Bras Cardiol. 2025;122(9):e20250624, Quadro 3.2, p. 36 (medida em consultório, a partir de 18 anos). Regras:
   - quando PAS e PAD caem em categorias diferentes, vale a mais alta;
   - a ordem de decisão é: hipertensão (PAS ≥ 140 ou PAD ≥ 90); depois pressão baixa (PAS < 90 ou PAD < 60), que é premissa provisória fora da diretriz; por último, PA normal ou pré-hipertensão;
   - HA sistólica isolada é PAS ≥ 140 e PAD < 90, com estágio pela PAS; HA diastólica isolada é PAS < 140 e PAD ≥ 90, com estágio pela PAD;
   - sem PAS ou sem PAD, o status é `DADOS_INCOMPLETOS`;
   - abaixo de 18 anos, o status é `FORA_DA_FAIXA_ETARIA`, com a idade calculada na data da anamnese.

   Os nomes do legado (Ótima, Normal, Limítrofe, Não Classificado) deixam de existir.
3. **Formulário (DIV-13).** As 22 perguntas Sim/Não, os 11 textos e as legendas foram conferidos no Excel pela pessoa. As listas estão na Task 4 (campos) e na Task 8 (legendas).
   - Toda pergunta começa sem resposta. Pergunta sem resposta é `null`, e nenhuma vem pré-marcada.
   - Para salvar, só são exigidos o paciente e a data.
4. **FC máxima e de reserva (AN-01, AN-02).**
   - São 9 métodos, limitados por sexo como no legado.
   - O resultado é arredondado para cima até o inteiro.
   - A FC máxima só vem do método escolhido, sem digitação manual.
   - FC de reserva = FC máxima − FC em repouso. Ela só existe quando há FC máxima e FC em repouso.
5. **Água ideal (AN-03, H-14).**
   - O fator é informado em mL/kg; o padrão é 35. O resultado é dado em litros: peso × fator ÷ 1000 (70 kg dá 2,45 L; 60 kg dá 2,10 L).
   - Peso e fator precisam ser maiores que zero. Se faltar qualquer um dos dois, não há resultado.
6. **Rascunho e finalização.**
   - Status `RASCUNHO` ou `FINALIZADA`.
   - Quem tem `anamneses.draft` ou `anamneses.finalize` cria e edita rascunhos. Só quem tem `anamneses.finalize` finaliza.
   - Para finalizar, bastam o paciente e a data.
   - A anamnese finalizada fica somente leitura. Este plano não oferece reabertura nem edição dela; as correções ficam para as revisões com histórico, num plano posterior.
7. **Histórico.**
   - Um paciente pode ter várias anamneses, listadas da mais recente para a mais antiga.
   - Para ler, é preciso `patients.read` e também `anamneses.draft` ou `anamneses.finalize`.
   - `history.read` não entra neste plano.
8. **Idade (DIV-15).** É a idade em anos completos na data da anamnese, calculada a partir do nascimento. Quem nasceu em 29/02 faz aniversário em 01/03 nos anos não bissextos, como na função `Age` do legado.
9. **Limites técnicos (não são regra clínica).**
   - PAS e PAD: inteiros de 1 a 300 mmHg.
   - FC em repouso: inteiro de 1 a 250 bpm.
   - Escala de urina: de 1 a 8.
   - Peso: maior que 0 e até 500 kg, com até 2 casas.
   - Fator: maior que 0 e até 200 mL/kg, com até 2 casas.
   - Data: de 01/01/1900 até hoje, e não anterior ao nascimento.
   - Textos: até 4000 caracteres.
10. **Premissas provisórias.**
    - Cada regra clínica fica num módulo puro.
    - Faixas, fatores e nomes ficam na configuração versionada.
    - Cada anamnese grava a versão das regras.
    - O código leva a marcação `// TODO(PREMISSA-CLINICA PC-xx): validar com o médico responsável ...`.
    - O backlog fica em `docs/premissas-clinicas.md`, com PC-01 a PC-04.
    - Fica fora deste plano uma tabela de faixas editável no banco.

## Global Constraints

- Continuam valendo todas as restrições do Plano 1:
  - dados de teste sempre sintéticos;
  - campo ausente é `null`, nunca `0` nem `''`;
  - interface em português do Brasil;
  - identidade visual (alabastro, grafite, terracota, ouro, oliva, areia; Playfair Display nos títulos, Inter no restante);
  - fuso da clínica `America/Sao_Paulo`;
  - datas civis como `date`, instantes como `timestamptz`.
- A pasta `zanoni` é só leitura durante a execução. Este plano não altera o `Bioimpedancia.xlsm`.
- Regras clínicas ficam só na API, em `apps/api/src/clinical-rules/`: funções puras, sem Prisma Client, sem Nest e sem I/O. A importação de **tipos** de `@prisma/client` é permitida. A web não calcula valor clínico.
- Faixas, coeficientes, fatores, legendas, nomes e fontes ficam só em `apps/api/src/clinical-rules/anamnese-rules.ts`.
- Versão publicada é imutável: mudar uma regra significa criar `anamnese-v2`. Cada anamnese grava `rulesVersion`.
- A anamnese grava cópias do sexo e da idade (`sex`, `ageYears`). Elas são recalculadas a partir do cadastro a cada gravação de rascunho e congeladas na finalização.
- A idade é calculada em anos completos na data da anamnese. Quem nasceu em 29/02 faz aniversário em 01/03 nos anos não bissextos.
- Toda premissa clínica provisória tem a marcação `// TODO(PREMISSA-CLINICA PC-xx): validar com o médico responsável ...` e uma linha em `docs/premissas-clinicas.md`. Um teste confere que as duas listas coincidem.
- Permissões:
  - regras: `anamneses.draft` ou `anamneses.finalize`;
  - listar, calcular, criar, ler e salvar rascunho: `patients.read` e mais `anamneses.draft` ou `anamneses.finalize`;
  - finalizar: `patients.read` e `anamneses.finalize`;
  - o PUT e a finalização exigem leitura porque o 409 devolve a anamnese em `current`.
- A anamnese finalizada é somente leitura: nenhuma rota a altera, reabre ou exclui. Não existe exclusão de anamnese, porque a política de retenção ainda não foi definida.
- Concorrência: versão otimista (`updateMany` com `where: { id, version, status: 'RASCUNHO' }`) e 409 com `current`. Este plano não cria trava `pg_advisory_xact_lock`, porque não há regra de unicidade entre linhas. A próxima chave livre, 7302004, continua reservada.
- Auditoria na mesma transação da escrita:
  - `anamnesis.create` com `{ patientId }`;
  - `anamnesis.update` e `anamnesis.finalize` com `{ patientId, changes }`, gerado por `diffFields`.
- Web:
  - Efeitos sobre cache, sessão, navegação ou estado de dados vão só nos callbacks síncronos das **opções** de `useMutation`. Não use `mutate(vars, callbacks)`, `mutateAsync`, `then`, nem efeito depois de `await`.
  - A API da anamnese só emite 400 com a mensagem `'Dados inválidos.'` e `errors` por campo, um formato que `errorMessage` já trata como público. Este plano não cria mensagem 400 nova. Se alguma tarefa criar, ela entra em `PUBLIC_400_MESSAGES` com teste.
  - Os 409 (`'Esta anamnese foi alterada por outra pessoa…'` e `'Esta anamnese já foi finalizada…'`) passam porque 409 é status público.
- Web: o `maxLength` dos textos é 4000, como no DTO. Campo vazio é enviado como `null`.

## Review Focus

Estes pontos não aparecem explicitamente na especificação, mas é onde a pessoa mais provavelmente seria prejudicada. Cada um tem teste na tarefa indicada.

1. **Conta de ponto flutuante perto de um inteiro.**
   - 208 − 0,7 × 30 tem de dar FC máxima 187, e não 188.
   - 48,9 kg × 50 mL/kg tem de dar 2,45 L, e não 2,44.
   - Testes: Task 2 (`heart-rate.spec.ts`, `water-intake.spec.ts`).
2. **Idade na fronteira.**
   - Véspera e dia do aniversário, nascido em 29/02 e o limite de 18 anos da PA (17 anos não classifica; 18 classifica).
   - Data da anamnese anterior ao nascimento: recusada com mensagem, nunca idade negativa.
   - Testes: Task 1 (`age.spec.ts`), Task 3 (`blood-pressure.spec.ts`), Task 4 (`anamnesis-normalize.spec.ts`).
3. **Cadastro do paciente alterado depois da anamnese.**
   - O rascunho recalcula idade e sexo ao ser salvo de novo.
   - A finalizada não muda, mesmo que o nascimento ou o sexo mudem depois.
   - Teste: Task 6 (`anamneses-finalize.e2e-spec.ts`).
4. **Aba aberta com rascunho antigo enquanto outra pessoa finaliza.**
   - O salvamento tardio recebe 409 `'Esta anamnese já foi finalizada e não pode ser alterada.'` e não grava nada.
   - "Carregar versão atual" mostra a anamnese finalizada, somente leitura.
   - Testes: Task 6 (concorrência determinística) e Task 9 (tela).
5. **Permissão retirada no meio da sessão.**
   - Sem `anamneses.draft` ou sem `patients.read`, a API responde 403 sem devolver dado nenhum, nem no 409.
   - A web tira as anamneses do cache. Quando o 403 vem das anamneses, o cadastro do paciente continua na tela; quando vem do paciente, as anamneses saem junto.
   - Testes: Task 5 (`anamneses.e2e-spec.ts`) e Task 7 (`anamneses-section.test.tsx`).

---

## Mapa de arquivos

```
psm-web/
  docs/premissas-clinicas.md                     backlog das premissas clínicas provisórias (PC-01 a PC-04)
  README.md                                      entrega e verificação do Plano 2
  apps/api/
    prisma/schema.prisma                         + enums AnamnesisStatus e BloodPressureStatus; modelo Anamnesis
    prisma/migrations/<carimbo>_anamneses/       migração gerada pelo Prisma
    src/clinical-rules/                          regras clínicas puras (sem banco nem interface)
      anamnese-rules.ts                          configuração versionada anamnese-v1 (faixas, fatores, nomes, fontes)
      age.ts                                     idade na data (DIV-15; 29/02 → 01/03)
      heart-rate.ts                              HeartRateCalculator: FC máxima e de reserva
      water-intake.ts                            WaterIntakeCalculator: água ideal
      urine-scale.ts                             UrineScale: escala 1–8 → classificação
      blood-pressure.ts                          BloodPressureClassifier
      anamnese-calculation.ts                    junta as regras e registra a versão
      premissas.spec.ts                          backlog × marcações no código
    src/auth/require-permissions.decorator.ts    + RequireAnyPermission
    src/auth/auth.guard.ts                       confere também "basta uma das permissões"
    src/anamneses/
      anamnesis-normalize.ts                     campos, validação e limpeza contra o cadastro
      anamnesis.view.ts                          AnamnesisView, item da lista, visão das regras
      dto/anamnesis.dto.ts                       corpo, atualização e consulta do cálculo
      anamneses.service.ts                       listar, calcular, criar, ler, salvar rascunho, finalizar
      anamneses.controller.ts                    rotas /api/anamneses e /api/patients/:id/anamneses
      anamneses.module.ts
    src/app.module.ts                            + AnamnesesModule
    test/helpers/run-paused.ts                   roteiro determinístico de concorrência
    test/anamneses.e2e-spec.ts                   rascunho, lista, cálculo, regras, permissões
    test/anamneses-finalize.e2e-spec.ts          finalização, congelamento e concorrência
  apps/web/
    src/lib/query-client.ts                      403 em cascata: pacientes → anamneses
    src/lib/format.ts                            todayInClinic, formatDecimalBr
    src/anamneses/
      types.ts                                   tipos da API, campos e permissões
      questions.ts                               grupos, perguntas e legendas (DIV-13)
      YesNoQuestion.tsx                          pergunta Sim/Não anulável
      use-debounced-value.ts                     atraso da prévia
      PatientAnamnesesSection.tsx                lista na página do paciente
      AnamnesisFormPage.tsx                      formulário, prévia, rascunho, finalização
    src/patients/PatientFormPage.tsx             mostra a seção de anamneses
    src/router.tsx                               rotas da anamnese
    src/test/anamnesis-fixtures.ts               dados sintéticos de anamnese
```

---

## Antes da Task 1

- [ ] **Conferir a base**

```powershell
Set-Location C:\Sistema\PSM\psm-web
git status --short
git switch -c plano-2-anamnese
npm run db:up
npm test
```

Expected: só `AGENTS.md` e `CLAUDE.md` aparecem como não rastreados. A branch nova é criada a partir de `master`. A API passa com 19 suítes e a web com 16 arquivos de teste.

- [ ] **Apontar as instruções dos agentes para este plano**

Em `CLAUDE.md` e `AGENTS.md`, que não são rastreados e continuam assim, troque a linha `Plano:` por:

```
Plano: C:/Sistema/PSM/Projeto 14/zanoni/docs/superpowers/plans/2026-10-04-plano-2-anamnese.md
```

e remova a linha `Na Task 1, acrescente a linha .claude/settings.local.json ao .gitignore`, que era do Plano 1.

---

### Task 1: Configuração versionada das regras, idade na data e backlog de premissas

**Files:**
- Create: `apps/api/src/clinical-rules/anamnese-rules.ts`, `apps/api/src/clinical-rules/age.ts`, `docs/premissas-clinicas.md`
- Test: `apps/api/src/clinical-rules/age.spec.ts`, `apps/api/src/clinical-rules/anamnese-rules.spec.ts`, `apps/api/src/clinical-rules/premissas.spec.ts`

**Interfaces:**
- Consumes: `parseIsoDate(s: string): Date | null` (`src/common/civil-date.ts`).
- Produces:
  - `ageOnDate(birthDate: string, onDate: string): number`;
  - os tipos `HeartRateMethodRule { id; label; sexes: readonly Sex[]; intercept: number; slopeThousandths: number }`, `HypertensionStage = 1 | 2 | 3`, `HypertensionStageRule { stage; systolicFrom; diastolicFrom }`, `UrineScale = 1 | … | 8` e `AnamneseRules`;
  - as constantes `ANAMNESE_RULES_V1`, `ANAMNESE_RULES_BY_VERSION` e `CURRENT_ANAMNESE_RULES`;
  - `rulesForVersion(version: string): AnamneseRules`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/age.spec.ts`:

```ts
import { ageOnDate } from './age';

describe('ageOnDate (DIV-15: idade na data do atendimento)', () => {
  it.each([
    ['1980-05-10', '2026-05-09', 45],
    ['1980-05-10', '2026-05-10', 46],
    ['1980-05-10', '1980-05-10', 0],
    ['2008-03-15', '2026-03-14', 17],
    ['2008-03-15', '2026-03-15', 18],
    ['1999-12-31', '2026-01-01', 26],
  ])('nascido em %s, na data %s → %i anos', (birth, on, age) => {
    expect(ageOnDate(birth, on)).toBe(age);
  });

  it.each([
    ['2025-02-28', 24],
    ['2025-03-01', 25],
    ['2024-02-28', 23],
    ['2024-02-29', 24],
  ])('nascido em 29/02/2000, na data %s → %i anos (01/03 nos anos não bissextos)', (on, age) => {
    expect(ageOnDate('2000-02-29', on)).toBe(age);
  });

  it('recusa data inválida ou anterior ao nascimento', () => {
    expect(() => ageOnDate('1980-05-10', '1980-05-09')).toThrow('A data é anterior ao nascimento.');
    expect(() => ageOnDate('1980-02-30', '2026-01-01')).toThrow('Data inválida para calcular a idade.');
  });
});
```

`apps/api/src/clinical-rules/anamnese-rules.spec.ts`:

```ts
import { ANAMNESE_RULES_BY_VERSION, ANAMNESE_RULES_V1, CURRENT_ANAMNESE_RULES, rulesForVersion } from './anamnese-rules';

describe('regras da anamnese versionadas', () => {
  it('a versão corrente é a anamnese-v1, provisória', () => {
    expect(CURRENT_ANAMNESE_RULES).toBe(ANAMNESE_RULES_V1);
    expect(rulesForVersion('anamnese-v1')).toBe(ANAMNESE_RULES_V1);
    expect(Object.keys(ANAMNESE_RULES_BY_VERSION)).toEqual(['anamnese-v1']);
    expect(ANAMNESE_RULES_V1).toMatchObject({ version: 'anamnese-v1', effectiveFrom: '2026-10-04', status: 'PROVISORIA' });
  });

  it('versão desconhecida é erro', () => {
    expect(() => rulesForVersion('anamnese-v0')).toThrow('Versão de regras de anamnese desconhecida: anamnese-v0');
  });

  it('a versão publicada não muda em execução', () => {
    expect(Object.isFrozen(ANAMNESE_RULES_V1)).toBe(true);
    expect(Object.isFrozen(ANAMNESE_RULES_V1.heartRate.methods)).toBe(true);
    expect(Object.isFrozen(ANAMNESE_RULES_V1.heartRate.methods[0])).toBe(true);
    expect(Object.isFrozen(ANAMNESE_RULES_V1.bloodPressure.stages[2])).toBe(true);
    expect(Object.isFrozen(ANAMNESE_RULES_V1.urine.labels)).toBe(true);
  });

  // Conteúdo publicado da anamnese-v1. Se um destes testes falhar, NÃO ajuste o esperado: crie a anamnese-v2.
  it('anamnese-v1: métodos de FC máxima', () => {
    expect(ANAMNESE_RULES_V1.heartRate.methods.map((m) => [m.id, m.label, m.sexes.join('/'), m.intercept, m.slopeThousandths])).toEqual([
      ['karvonen-1957', 'Karvonen et al., 1957 — clássica: 220 − idade', 'FEMININO/MASCULINO', 220, 1000],
      ['jones-1975', 'Jones et al., 1975: 220 − 0,65 × idade', 'FEMININO/MASCULINO', 220, 650],
      ['tanaka-2001', 'Tanaka et al., 2001: 208 − 0,7 × idade', 'FEMININO/MASCULINO', 208, 700],
      ['calvert-1977-homens', 'Calvert et al., 1977: 201 − 0,6 × idade', 'MASCULINO', 201, 600],
      ['sheffield-1965-sedentarios', 'Sheffield et al., 1965 (sedentários): 205 − 0,41 × idade', 'MASCULINO', 205, 410],
      ['sheffield-1965-ativos', 'Sheffield et al., 1965 (ativos): 198 − 0,41 × idade', 'MASCULINO', 198, 410],
      ['branco-2004', 'Branco et al., 2004 (corredores de fundo): 205 − 0,7 × idade', 'MASCULINO', 205, 700],
      ['calvert-1977-mulheres', 'Calvert et al., 1977: 192 − 0,7 × idade', 'FEMININO', 192, 700],
      ['hossack-1981', 'Hossack et al., 1981: 206 − 0,597 × idade', 'FEMININO', 206, 597],
    ]);
  });

  it('anamnese-v1: pressão arterial', () => {
    const { source, ...bloodPressure } = ANAMNESE_RULES_V1.bloodPressure;
    expect(source).toBe(
      'Diretriz Brasileira de Hipertensão Arterial – 2025 (SBC, SBH, SBN). Arq Bras Cardiol. 2025;122(9):e20250624. Quadro 3.2, p. 36 (consultório, a partir de 18 anos).',
    );
    expect(bloodPressure).toEqual({
      minAgeYears: 18,
      stages: [
        { stage: 1, systolicFrom: 140, diastolicFrom: 90 },
        { stage: 2, systolicFrom: 160, diastolicFrom: 100 },
        { stage: 3, systolicFrom: 180, diastolicFrom: 110 },
      ],
      preHypertension: { systolicFrom: 120, diastolicFrom: 80 },
      hypotension: { systolicBelow: 90, diastolicBelow: 60 },
      labels: {
        normal: 'PA normal',
        preHypertension: 'Pré-hipertensão',
        hypotension: 'Pressão Baixa / Hipotensão',
        stage: 'HA Estágio {n}',
        isolatedSystolic: 'HA Sistólica Isolada – Estágio {n}',
        isolatedDiastolic: 'HA Diastólica Isolada – Estágio {n}',
      },
    });
  });

  it('anamnese-v1: escala de urina e água', () => {
    expect(ANAMNESE_RULES_V1.urine.labels).toEqual({
      1: 'Bem Hidratado',
      2: 'Bem Hidratado',
      3: 'Levemente Desidratado',
      4: 'Levemente Desidratado',
      5: 'Moderadamente Desidratado',
      6: 'Moderadamente Desidratado',
      7: 'Severamente Desidratado',
      8: 'Severamente Desidratado',
    });
    expect(ANAMNESE_RULES_V1.water.defaultMlPerKg).toBe(35);
  });
});
```

`apps/api/src/clinical-rules/premissas.spec.ts`:

```ts
import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const REPO = join(__dirname, '..', '..', '..', '..');
const SOURCES = [join(REPO, 'apps', 'api', 'src'), join(REPO, 'apps', 'web', 'src')];
const MARKER = /TODO\(PREMISSA-CLINICA (PC-\d{2})\): validar com o médico responsável/g;
const PROVISIONAL_ROW = /^\| (PC-\d{2}) \|.*\| Provisória \|/gm;

function sourceFiles(dir: string): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) return sourceFiles(path);
    return /\.tsx?$/.test(entry.name) && !/\.(spec|test)\.tsx?$/.test(entry.name) ? [path] : [];
  });
}

describe('premissas clínicas provisórias', () => {
  it('o backlog e as marcações no código listam as mesmas premissas', () => {
    const backlog = readFileSync(join(REPO, 'docs', 'premissas-clinicas.md'), 'utf8');
    const documented = [...new Set([...backlog.matchAll(PROVISIONAL_ROW)].map((m) => m[1]))].sort();
    const marked = [
      ...new Set(SOURCES.flatMap(sourceFiles).flatMap((file) => [...readFileSync(file, 'utf8').matchAll(MARKER)].map((m) => m[1]))),
    ].sort();

    expect(documented).toEqual(['PC-01', 'PC-02', 'PC-03', 'PC-04']);
    expect(marked).toEqual(documented);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: FAIL com `Cannot find module './age'` e `'./anamnese-rules'`; `premissas.spec` falha com `ENOENT` em `docs/premissas-clinicas.md`.

- [ ] **Step 3: Implementar a idade na data**

`apps/api/src/clinical-rules/age.ts`:

```ts
import { parseIsoDate } from '../common/civil-date';

/**
 * Idade em anos completos na data `onDate` (DIV-15: calculada na data do atendimento, não a gravada no cadastro).
 * Nascidos em 29/02 fazem aniversário em 01/03 nos anos não bissextos, como a função Age do legado (decisão de
 * 2026-10-04). Datas em AAAA-MM-DD; data anterior ao nascimento é erro de quem chama.
 */
export function ageOnDate(birthDate: string, onDate: string): number {
  const birth = parseIsoDate(birthDate);
  const on = parseIsoDate(onDate);
  if (!birth || !on) throw new Error('Data inválida para calcular a idade.');
  if (onDate < birthDate) throw new Error('A data é anterior ao nascimento.');

  const year = on.getUTCFullYear();
  let month = birth.getUTCMonth() + 1;
  let day = birth.getUTCDate();
  if (month === 2 && day === 29 && !isLeapYear(year)) {
    month = 3;
    day = 1;
  }
  const onMonth = on.getUTCMonth() + 1;
  const beforeBirthday = onMonth < month || (onMonth === month && on.getUTCDate() < day);
  return year - birth.getUTCFullYear() - (beforeBirthday ? 1 : 0);
}

function isLeapYear(year: number): boolean {
  return (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;
}
```

- [ ] **Step 4: Implementar a configuração versionada**

`apps/api/src/clinical-rules/anamnese-rules.ts`:

```ts
import type { Sex } from '@prisma/client';

/** FC máxima = intercept − slopeThousandths/1000 × idade, arredondada para cima ao inteiro (bpm). */
export interface HeartRateMethodRule {
  id: string;
  label: string;
  sexes: readonly Sex[];
  /** bpm */
  intercept: number;
  /** Coeficiente da idade em milésimos (0,597 → 597): a conta fica inteira e exata. */
  slopeThousandths: number;
}

export type HypertensionStage = 1 | 2 | 3;

/** Limites inferiores inclusivos, em mmHg. */
export interface HypertensionStageRule {
  stage: HypertensionStage;
  systolicFrom: number;
  diastolicFrom: number;
}

export type UrineScale = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;

export interface AnamneseRules {
  version: string;
  effectiveFrom: string;
  status: 'PROVISORIA' | 'VALIDADA';
  heartRate: { source: string; methods: readonly HeartRateMethodRule[] };
  bloodPressure: {
    source: string;
    minAgeYears: number;
    /** Em ordem crescente de estágio. */
    stages: readonly HypertensionStageRule[];
    preHypertension: { systolicFrom: number; diastolicFrom: number };
    hypotension: { systolicBelow: number; diastolicBelow: number };
    /** `{n}` é substituído pelo estágio. */
    labels: {
      normal: string;
      preHypertension: string;
      hypotension: string;
      stage: string;
      isolatedSystolic: string;
      isolatedDiastolic: string;
    };
  };
  urine: { source: string; labels: Readonly<Record<UrineScale, string>> };
  water: { source: string; defaultMlPerKg: number };
}

function deepFreeze<T>(value: T): T {
  if (typeof value === 'object' && value !== null && !Object.isFrozen(value)) {
    Object.freeze(value);
    for (const child of Object.values(value)) deepFreeze(child);
  }
  return value;
}

/**
 * Versão publicada: NUNCA altere valores, nomes ou limites desta constante. Anamneses gravadas com 'anamnese-v1'
 * precisam continuar reproduzíveis. Para mudar uma regra, crie 'anamnese-v2', registre-a em ANAMNESE_RULES_BY_VERSION,
 * aponte CURRENT_ANAMNESE_RULES para ela e atualize docs/premissas-clinicas.md.
 */
export const ANAMNESE_RULES_V1: AnamneseRules = deepFreeze({
  version: 'anamnese-v1',
  effectiveFrom: '2026-10-04',
  status: 'PROVISORIA',
  heartRate: {
    // TODO(PREMISSA-CLINICA PC-03): validar com o médico responsável as referências e os anos das fórmulas (em especial 220 − idade atribuída a Karvonen et al., 1957).
    // TODO(PREMISSA-CLINICA PC-04): validar com o médico responsável o arredondamento para cima ao inteiro (RoundUp do legado).
    source: 'FrmFcMax do legado (AN-01); legendas conferidas no Excel em 2026-10-04 (DIV-13).',
    methods: [
      { id: 'karvonen-1957', label: 'Karvonen et al., 1957 — clássica: 220 − idade', sexes: ['FEMININO', 'MASCULINO'], intercept: 220, slopeThousandths: 1000 },
      { id: 'jones-1975', label: 'Jones et al., 1975: 220 − 0,65 × idade', sexes: ['FEMININO', 'MASCULINO'], intercept: 220, slopeThousandths: 650 },
      { id: 'tanaka-2001', label: 'Tanaka et al., 2001: 208 − 0,7 × idade', sexes: ['FEMININO', 'MASCULINO'], intercept: 208, slopeThousandths: 700 },
      { id: 'calvert-1977-homens', label: 'Calvert et al., 1977: 201 − 0,6 × idade', sexes: ['MASCULINO'], intercept: 201, slopeThousandths: 600 },
      { id: 'sheffield-1965-sedentarios', label: 'Sheffield et al., 1965 (sedentários): 205 − 0,41 × idade', sexes: ['MASCULINO'], intercept: 205, slopeThousandths: 410 },
      { id: 'sheffield-1965-ativos', label: 'Sheffield et al., 1965 (ativos): 198 − 0,41 × idade', sexes: ['MASCULINO'], intercept: 198, slopeThousandths: 410 },
      { id: 'branco-2004', label: 'Branco et al., 2004 (corredores de fundo): 205 − 0,7 × idade', sexes: ['MASCULINO'], intercept: 205, slopeThousandths: 700 },
      { id: 'calvert-1977-mulheres', label: 'Calvert et al., 1977: 192 − 0,7 × idade', sexes: ['FEMININO'], intercept: 192, slopeThousandths: 700 },
      { id: 'hossack-1981', label: 'Hossack et al., 1981: 206 − 0,597 × idade', sexes: ['FEMININO'], intercept: 206, slopeThousandths: 597 },
    ],
  },
  bloodPressure: {
    // TODO(PREMISSA-CLINICA PC-01): validar com o médico responsável a tabela, a regra da categoria mais alta, a HA isolada, os nomes e a idade mínima (DIV-07).
    source:
      'Diretriz Brasileira de Hipertensão Arterial – 2025 (SBC, SBH, SBN). Arq Bras Cardiol. 2025;122(9):e20250624. Quadro 3.2, p. 36 (consultório, a partir de 18 anos).',
    minAgeYears: 18,
    stages: [
      { stage: 1, systolicFrom: 140, diastolicFrom: 90 },
      { stage: 2, systolicFrom: 160, diastolicFrom: 100 },
      { stage: 3, systolicFrom: 180, diastolicFrom: 110 },
    ],
    preHypertension: { systolicFrom: 120, diastolicFrom: 80 },
    // TODO(PREMISSA-CLINICA PC-02): validar com o médico responsável o limite de pressão baixa (PAS < 90 ou PAD < 60), que não consta da diretriz, e sua precedência sobre PA normal e pré-hipertensão.
    hypotension: { systolicBelow: 90, diastolicBelow: 60 },
    labels: {
      normal: 'PA normal',
      preHypertension: 'Pré-hipertensão',
      hypotension: 'Pressão Baixa / Hipotensão',
      stage: 'HA Estágio {n}',
      isolatedSystolic: 'HA Sistólica Isolada – Estágio {n}',
      isolatedDiastolic: 'HA Diastólica Isolada – Estágio {n}',
    },
  },
  urine: {
    source: 'Legendas do FrmUrina (AN-04); opção = escala, inclusive na reabertura (DIV-04, 2026-10-04).',
    labels: {
      1: 'Bem Hidratado',
      2: 'Bem Hidratado',
      3: 'Levemente Desidratado',
      4: 'Levemente Desidratado',
      5: 'Moderadamente Desidratado',
      6: 'Moderadamente Desidratado',
      7: 'Severamente Desidratado',
      8: 'Severamente Desidratado',
    },
  },
  water: {
    source: 'FrmAgua do legado (AN-03); fator em mL/kg e resultado em litros (decisão de 2026-10-04).',
    defaultMlPerKg: 35,
  },
});

export const ANAMNESE_RULES_BY_VERSION: Readonly<Record<string, AnamneseRules>> = Object.freeze({
  [ANAMNESE_RULES_V1.version]: ANAMNESE_RULES_V1,
});

/** Versão usada em toda gravação de rascunho e na finalização. */
export const CURRENT_ANAMNESE_RULES: AnamneseRules = ANAMNESE_RULES_V1;

export function rulesForVersion(version: string): AnamneseRules {
  const rules = ANAMNESE_RULES_BY_VERSION[version];
  if (!rules) throw new Error(`Versão de regras de anamnese desconhecida: ${version}`);
  return rules;
}
```

- [ ] **Step 5: Criar o backlog de premissas**

`docs/premissas-clinicas.md`:

```markdown
# Premissas clínicas provisórias

Regras adotadas provisoriamente pela pessoa responsável pelo projeto, até a validação do médico responsável. Cada
premissa provisória tem marcação no código (`TODO(PREMISSA-CLINICA PC-xx): validar com o médico responsável ...`). O
teste `apps/api/src/clinical-rules/premissas.spec.ts` confere que as linhas com status "Provisória" e as marcações
coincidem.

Como validar:

1. O médico responsável confirma a premissa ou indica a correção.
2. Uma correção vira nova versão das regras (`anamnese-v2` em `apps/api/src/clinical-rules/anamnese-rules.ts`). A
   versão publicada nunca é alterada: as anamneses gravadas guardam a versão usada e continuam reproduzíveis.
3. Atualize a linha abaixo: status "Validada" (ou "Substituída por anamnese-vN"), data e quem validou. Depois remova
   as marcações do código.

| ID | Decisão provisória | Fonte | Data | Responsável pela validação | Status | Onde no código |
|---|---|---|---|---|---|---|
| PC-01 | Classificação da PA (consultório, a partir de 18 anos): PA normal (PAS < 120 e PAD < 80); Pré-hipertensão (PAS 120–139 e/ou PAD 80–89); HA Estágio 1 (140–159 e/ou 90–99), 2 (160–179 e/ou 100–109) e 3 (≥ 180 e/ou ≥ 110). Categorias diferentes: vale a mais alta. HA Sistólica Isolada (PAS ≥ 140 e PAD < 90) e HA Diastólica Isolada (PAS < 140 e PAD ≥ 90), estágio pela pressão alterada. PAS e PAD obrigatórias para classificar. Menores de 18 anos não são classificados. Nomes provisórios. | Diretriz Brasileira de Hipertensão Arterial – 2025 (SBC, SBH, SBN), Arq Bras Cardiol. 2025;122(9):e20250624, Quadro 3.2, p. 36; DIV-07 | 2026-10-04 | Médico responsável pela clínica (nome a registrar) | Provisória | `anamnese-rules.ts` (bloodPressure); `blood-pressure.ts` |
| PC-02 | Pressão Baixa / Hipotensão quando PAS < 90 ou PAD < 60, sem hipertensão; precede PA normal e Pré-hipertensão (135/55 e 125/55 → Pressão Baixa). | Não consta da diretriz; decisão da pessoa (DIV-07) | 2026-10-04 | Médico responsável pela clínica (nome a registrar) | Provisória | `anamnese-rules.ts` (hypotension); `blood-pressure.ts` |
| PC-03 | Referências e anos das 9 fórmulas de FC máxima, em especial 220 − idade atribuída a Karvonen et al., 1957 (Karvonen é mais conhecido pelo método da FC de reserva); restrição por sexo como no legado. | Legendas do FrmFcMax conferidas no Excel (DIV-13); AN-01 | 2026-10-04 | Médico responsável pela clínica (nome a registrar) | Provisória | `anamnese-rules.ts` (heartRate) |
| PC-04 | FC máxima arredondada para cima ao inteiro (RoundUp do legado). | FrmFcMax.CommandButton179_Click (AN-01) | 2026-10-04 | Médico responsável pela clínica (nome a registrar) | Provisória | `anamnese-rules.ts` (heartRate); `heart-rate.ts` |
```

- [ ] **Step 6: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: PASS — `age.spec` (11), `anamnese-rules.spec` (6), `premissas.spec` (1).

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat(api): regras da anamnese versionadas, idade na data e backlog de premissas clínicas"
```

---

### Task 2: FC máxima e de reserva, água ideal e escala de urina

**Files:**
- Create: `apps/api/src/clinical-rules/heart-rate.ts`, `apps/api/src/clinical-rules/water-intake.ts`, `apps/api/src/clinical-rules/urine-scale.ts`
- Test: `apps/api/src/clinical-rules/heart-rate.spec.ts`, `apps/api/src/clinical-rules/water-intake.spec.ts`, `apps/api/src/clinical-rules/urine-scale.spec.ts`

**Interfaces:**
- Consumes: `AnamneseRules`, `HeartRateMethodRule`, `UrineScale`, `ANAMNESE_RULES_V1` (Task 1).
- Produces:
  - `heartRateMethodsFor(rules: AnamneseRules, sex: Sex): HeartRateMethodRule[]`;
  - `findHeartRateMethod(rules: AnamneseRules, id: string): HeartRateMethodRule | undefined`;
  - `maxHeartRate(method: HeartRateMethodRule, ageYears: number): number`;
  - `reserveHeartRate(max: number | null, resting: number | null): number | null`;
  - `idealWaterLiters(weightKg: number | null, mlPerKg: number | null): number | null`;
  - `URINE_SCALES: readonly UrineScale[]`;
  - `isUrineScale(value: number): value is UrineScale`;
  - `urineClassification(rules: AnamneseRules, scale: number | null): string | null`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/heart-rate.spec.ts`:

```ts
import { ANAMNESE_RULES_V1 as RULES } from './anamnese-rules';
import { findHeartRateMethod, heartRateMethodsFor, maxHeartRate, reserveHeartRate } from './heart-rate';

function method(id: string) {
  const found = findHeartRateMethod(RULES, id);
  if (!found) throw new Error(`Método ausente: ${id}`);
  return found;
}

describe('FC máxima (AN-01)', () => {
  it.each([
    ['karvonen-1957', 180],
    ['jones-1975', 194],
    ['tanaka-2001', 180],
    ['calvert-1977-homens', 177],
    ['sheffield-1965-sedentarios', 189],
    ['sheffield-1965-ativos', 182],
    ['branco-2004', 177],
    ['calvert-1977-mulheres', 164],
    ['hossack-1981', 183],
  ])('%s aos 40 anos → %i bpm', (id, bpm) => {
    expect(maxHeartRate(method(id), 40)).toBe(bpm);
  });

  it('arredonda para cima: Tanaka aos 45 (176,5) → 177; Hossack aos 41 (181,523) → 182', () => {
    expect(maxHeartRate(method('tanaka-2001'), 45)).toBe(177);
    expect(maxHeartRate(method('hossack-1981'), 41)).toBe(182);
  });

  it('resultado inteiro exato não sobe por erro de ponto flutuante', () => {
    // Em ponto flutuante, 208 − 0,7 × 30 = 187,00000000000003, e o arredondamento para cima daria 188.
    expect(maxHeartRate(method('tanaka-2001'), 30)).toBe(187);
    expect(maxHeartRate(method('calvert-1977-mulheres'), 30)).toBe(171);
  });

  it('recusa idade negativa ou fracionária', () => {
    expect(() => maxHeartRate(method('tanaka-2001'), -1)).toThrow('Idade inválida.');
    expect(() => maxHeartRate(method('tanaka-2001'), 40.5)).toThrow('Idade inválida.');
  });
});

describe('métodos de FC máxima por sexo', () => {
  it('masculino: os 3 comuns e os 4 masculinos, na ordem do formulário', () => {
    expect(heartRateMethodsFor(RULES, 'MASCULINO').map((m) => m.id)).toEqual([
      'karvonen-1957', 'jones-1975', 'tanaka-2001', 'calvert-1977-homens', 'sheffield-1965-sedentarios', 'sheffield-1965-ativos', 'branco-2004',
    ]);
  });

  it('feminino: os 3 comuns e os 2 femininos', () => {
    expect(heartRateMethodsFor(RULES, 'FEMININO').map((m) => m.id)).toEqual([
      'karvonen-1957', 'jones-1975', 'tanaka-2001', 'calvert-1977-mulheres', 'hossack-1981',
    ]);
  });

  it('método desconhecido', () => {
    expect(findHeartRateMethod(RULES, 'inexistente')).toBeUndefined();
  });
});

describe('FC de reserva (AN-02)', () => {
  it('H-13: máxima 180 e repouso 70 → 110', () => {
    expect(reserveHeartRate(180, 70)).toBe(110);
  });

  it('sem máxima, sem repouso ou com repouso zero, não há reserva', () => {
    expect(reserveHeartRate(null, 70)).toBeNull();
    expect(reserveHeartRate(180, null)).toBeNull();
    expect(reserveHeartRate(180, 0)).toBeNull();
  });
});
```

`apps/api/src/clinical-rules/water-intake.spec.ts`:

```ts
import { idealWaterLiters } from './water-intake';

describe('água ideal (AN-03)', () => {
  it.each([
    [70, 35, 2.45],
    [60, 35, 2.1],
    [48.9, 50, 2.45],
    [72.5, 33.33, 2.42],
    [0.01, 0.01, 0],
    [500, 200, 100],
  ])('%p kg × %p mL/kg → %p L', (weight, factor, liters) => {
    expect(idealWaterLiters(weight, factor)).toBe(liters);
  });

  it('sem peso ou sem fator, ou com zero ou negativo, não há resultado', () => {
    expect(idealWaterLiters(null, 35)).toBeNull();
    expect(idealWaterLiters(70, null)).toBeNull();
    expect(idealWaterLiters(0, 35)).toBeNull();
    expect(idealWaterLiters(70, 0)).toBeNull();
    expect(idealWaterLiters(-70, 35)).toBeNull();
  });
});
```

`apps/api/src/clinical-rules/urine-scale.spec.ts`:

```ts
import { ANAMNESE_RULES_V1 as RULES } from './anamnese-rules';
import { isUrineScale, URINE_SCALES, urineClassification } from './urine-scale';

describe('escala de urina (AN-04, DIV-04)', () => {
  it.each([
    [1, 'Bem Hidratado'],
    [2, 'Bem Hidratado'],
    [3, 'Levemente Desidratado'],
    [4, 'Levemente Desidratado'],
    [5, 'Moderadamente Desidratado'],
    [6, 'Moderadamente Desidratado'],
    [7, 'Severamente Desidratado'],
    [8, 'Severamente Desidratado'],
  ])('escala %i → %s', (scale, label) => {
    expect(urineClassification(RULES, scale)).toBe(label);
  });

  it('as oito escalas, em ordem', () => {
    expect(URINE_SCALES).toEqual([1, 2, 3, 4, 5, 6, 7, 8]);
  });

  it('sem escala, sem classificação', () => {
    expect(urineClassification(RULES, null)).toBeNull();
  });

  it('fora de 1 a 8 é erro de quem chama', () => {
    expect(isUrineScale(0)).toBe(false);
    expect(isUrineScale(3.5)).toBe(false);
    expect(() => urineClassification(RULES, 9)).toThrow('Escala de urina fora de 1 a 8.');
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: FAIL com `Cannot find module './heart-rate'`, `'./water-intake'` e `'./urine-scale'`.

- [ ] **Step 3: Implementar `heart-rate.ts`**

Escreva as quatro funções da interface.

- `heartRateMethodsFor` filtra `rules.heartRate.methods` por `sexes.includes(sex)`, mantendo a ordem.
- `findHeartRateMethod` procura pelo `id` em todos os métodos.
- `reserveHeartRate` devolve `max − resting`, ou `null` quando um dos dois é `null` ou quando `resting` é 0.
- `maxHeartRate` usa conta inteira em milésimos e põe a marcação no ponto do arredondamento:

```ts
/** FC máxima em bpm inteiros. A conta é em milésimos de bpm, inteira, para 208 − 0,7 × 30 dar 187 e não 188. */
export function maxHeartRate(method: HeartRateMethodRule, ageYears: number): number {
  if (!Number.isInteger(ageYears) || ageYears < 0) throw new Error('Idade inválida.');
  const thousandths = method.intercept * 1000 - method.slopeThousandths * ageYears;
  // TODO(PREMISSA-CLINICA PC-04): validar com o médico responsável o arredondamento para cima ao inteiro (RoundUp do legado).
  return Math.ceil(thousandths / 1000);
}
```

- [ ] **Step 4: Implementar `water-intake.ts`**

```ts
/**
 * AN-03: água ideal em litros = peso (kg) × fator (mL/kg) ÷ 1000, com 2 casas, meio para cima. Peso e fator chegam com
 * até 2 casas (a validação garante); a conta é inteira, em centésimos, para 48,9 × 50 dar 2,45 e não 2,44. Sem um
 * deles, ou com zero ou negativo, não há resultado (decisão de 2026-10-04).
 */
export function idealWaterLiters(weightKg: number | null, mlPerKg: number | null): number | null {
  if (weightKg === null || mlPerKg === null || weightKg <= 0 || mlPerKg <= 0) return null;
  const product = Math.round(weightKg * 100) * Math.round(mlPerKg * 100); // mL × 10⁴
  const centiliters = Math.floor((product + 50_000) / 100_000);
  return centiliters / 100;
}
```

- [ ] **Step 5: Implementar `urine-scale.ts`**

`URINE_SCALES = [1, 2, 3, 4, 5, 6, 7, 8] as const`, tipado como `readonly UrineScale[]`.

- `isUrineScale` aceita só inteiros de 1 a 8.
- `urineClassification` devolve `null` para `null`. Para valor fora da escala, lança `Error('Escala de urina fora de 1 a 8.')`. Nos demais casos, devolve `rules.urine.labels[scale]`.

- [ ] **Step 6: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: PASS — `heart-rate.spec` (17), `water-intake.spec` (7), `urine-scale.spec` (11), além das suítes da Task 1.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat(api): FC máxima e de reserva, água ideal e escala de urina como regras puras"
```

---

### Task 3: Classificação da pressão arterial e cálculo da anamnese

**Files:**
- Create: `apps/api/src/clinical-rules/blood-pressure.ts`, `apps/api/src/clinical-rules/anamnese-calculation.ts`
- Test: `apps/api/src/clinical-rules/blood-pressure.spec.ts`, `apps/api/src/clinical-rules/anamnese-calculation.spec.ts`

**Interfaces:**
- Consumes: Tasks 1 e 2.
- Produces:
  - `type BloodPressureStatus = 'CLASSIFICADA' | 'DADOS_INCOMPLETOS' | 'FORA_DA_FAIXA_ETARIA'`;
  - `interface BloodPressureResult { status: BloodPressureStatus; classification: string | null }`;
  - `classifyBloodPressure(rules: AnamneseRules, input: { systolic: number | null; diastolic: number | null; ageYears: number }): BloodPressureResult`;
  - `interface AnamnesisCalculationInput { sex: Sex; ageYears: number; restingHeartRate: number | null; heartRateMethod: string | null; systolicPressure: number | null; diastolicPressure: number | null; urineScale: number | null; waterWeightKg: number | null; waterMlPerKg: number | null }`;
  - `interface AnamnesisCalculation { rulesVersion: string; ageYears: number; maxHeartRate: number | null; reserveHeartRate: number | null; bloodPressureStatus: BloodPressureStatus; bloodPressureClassification: string | null; urineClassification: string | null; idealWaterLiters: number | null }`;
  - `calculateAnamnesis(rules: AnamneseRules, input: AnamnesisCalculationInput): AnamnesisCalculation`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/blood-pressure.spec.ts`:

```ts
import { ANAMNESE_RULES_V1 as RULES } from './anamnese-rules';
import { classifyBloodPressure } from './blood-pressure';

const adult = (systolic: number | null, diastolic: number | null) => classifyBloodPressure(RULES, { systolic, diastolic, ageYears: 40 });

describe('pressão arterial (AN-05, DIV-07, H-22)', () => {
  it.each([
    [150, 80, 'HA Sistólica Isolada – Estágio 1'],
    [190, 105, 'HA Estágio 3'],
    [170, 115, 'HA Estágio 3'],
    [85, 70, 'Pressão Baixa / Hipotensão'],
  ])('H-22: %i/%i → %s', (s, d, label) => {
    expect(adult(s, d)).toEqual({ status: 'CLASSIFICADA', classification: label });
  });

  it.each([
    [119, 79, 'PA normal'],
    [90, 60, 'PA normal'],
    [120, 79, 'Pré-hipertensão'],
    [119, 80, 'Pré-hipertensão'],
    [139, 89, 'Pré-hipertensão'],
  ])('normal e pré-hipertensão: %i/%i → %s', (s, d, label) => {
    expect(adult(s, d).classification).toBe(label);
  });

  it.each([
    [89, 70],
    [100, 59],
    [135, 55],
    [125, 55],
    [139, 59],
  ])('pressão baixa antes de normal e pré-hipertensão: %i/%i', (s, d) => {
    expect(adult(s, d).classification).toBe('Pressão Baixa / Hipotensão');
  });

  it.each([
    [140, 89, 'HA Sistólica Isolada – Estágio 1'],
    [159, 89, 'HA Sistólica Isolada – Estágio 1'],
    [160, 89, 'HA Sistólica Isolada – Estágio 2'],
    [179, 60, 'HA Sistólica Isolada – Estágio 2'],
    [180, 89, 'HA Sistólica Isolada – Estágio 3'],
    [150, 55, 'HA Sistólica Isolada – Estágio 1'],
  ])('HA sistólica isolada, estágio pela PAS: %i/%i → %s', (s, d, label) => {
    expect(adult(s, d).classification).toBe(label);
  });

  it.each([
    [139, 90, 'HA Diastólica Isolada – Estágio 1'],
    [119, 99, 'HA Diastólica Isolada – Estágio 1'],
    [139, 100, 'HA Diastólica Isolada – Estágio 2'],
    [110, 109, 'HA Diastólica Isolada – Estágio 2'],
    [100, 110, 'HA Diastólica Isolada – Estágio 3'],
    [85, 95, 'HA Diastólica Isolada – Estágio 1'],
  ])('HA diastólica isolada, estágio pela PAD: %i/%i → %s', (s, d, label) => {
    expect(adult(s, d).classification).toBe(label);
  });

  it.each([
    [140, 90, 'HA Estágio 1'],
    [159, 99, 'HA Estágio 1'],
    [160, 90, 'HA Estágio 2'],
    [140, 100, 'HA Estágio 2'],
    [179, 109, 'HA Estágio 2'],
    [180, 90, 'HA Estágio 3'],
    [140, 110, 'HA Estágio 3'],
  ])('PAS e PAD alteradas, vale a categoria mais alta: %i/%i → %s', (s, d, label) => {
    expect(adult(s, d).classification).toBe(label);
  });

  it.each([
    [120, null],
    [null, 80],
    [null, null],
  ])('sem PAS ou sem PAD: dados incompletos (%p/%p)', (s, d) => {
    expect(adult(s, d)).toEqual({ status: 'DADOS_INCOMPLETOS', classification: null });
  });

  it('menor de 18 anos não é classificado, mesmo sem PAS e PAD; aos 18 é', () => {
    expect(classifyBloodPressure(RULES, { systolic: 150, diastolic: 80, ageYears: 17 })).toEqual({ status: 'FORA_DA_FAIXA_ETARIA', classification: null });
    expect(classifyBloodPressure(RULES, { systolic: null, diastolic: null, ageYears: 17 })).toEqual({ status: 'FORA_DA_FAIXA_ETARIA', classification: null });
    expect(classifyBloodPressure(RULES, { systolic: 150, diastolic: 80, ageYears: 18 })).toEqual({
      status: 'CLASSIFICADA',
      classification: 'HA Sistólica Isolada – Estágio 1',
    });
  });

  it('todo par de 1 a 300 mmHg recebe um dos 12 nomes provisórios, e nenhum nome do legado', () => {
    const allowed = new Set([
      'PA normal', 'Pré-hipertensão', 'Pressão Baixa / Hipotensão',
      ...[1, 2, 3].flatMap((n) => [`HA Estágio ${n}`, `HA Sistólica Isolada – Estágio ${n}`, `HA Diastólica Isolada – Estágio ${n}`]),
    ]);
    const seen = new Set<string>();
    for (let s = 1; s <= 300; s++) {
      for (let d = 1; d <= 300; d++) {
        const result = adult(s, d);
        expect(result.status).toBe('CLASSIFICADA');
        seen.add(result.classification as string);
      }
    }
    expect([...seen].sort()).toEqual([...allowed].sort());
  });
});
```

`apps/api/src/clinical-rules/anamnese-calculation.spec.ts`:

```ts
import { calculateAnamnesis } from './anamnese-calculation';
import { ANAMNESE_RULES_V1 as RULES } from './anamnese-rules';

const empty = {
  restingHeartRate: null, heartRateMethod: null, systolicPressure: null, diastolicPressure: null,
  urineScale: null, waterWeightKg: null, waterMlPerKg: null,
};

describe('cálculo da anamnese', () => {
  it('junta as regras e registra a versão usada', () => {
    expect(
      calculateAnamnesis(RULES, {
        sex: 'MASCULINO', ageYears: 45, restingHeartRate: 70, heartRateMethod: 'tanaka-2001',
        systolicPressure: 150, diastolicPressure: 80, urineScale: 4, waterWeightKg: 70, waterMlPerKg: 35,
      }),
    ).toEqual({
      rulesVersion: 'anamnese-v1',
      ageYears: 45,
      maxHeartRate: 177,
      reserveHeartRate: 107,
      bloodPressureStatus: 'CLASSIFICADA',
      bloodPressureClassification: 'HA Sistólica Isolada – Estágio 1',
      urineClassification: 'Levemente Desidratado',
      idealWaterLiters: 2.45,
    });
  });

  it('sem dados: nada calculado e PA com dados incompletos', () => {
    expect(calculateAnamnesis(RULES, { sex: 'FEMININO', ageYears: 30, ...empty })).toEqual({
      rulesVersion: 'anamnese-v1', ageYears: 30, maxHeartRate: null, reserveHeartRate: null,
      bloodPressureStatus: 'DADOS_INCOMPLETOS', bloodPressureClassification: null, urineClassification: null, idealWaterLiters: null,
    });
  });

  it('método de outro sexo não calcula FC máxima', () => {
    const result = calculateAnamnesis(RULES, { sex: 'MASCULINO', ageYears: 40, ...empty, restingHeartRate: 70, heartRateMethod: 'hossack-1981' });
    expect(result).toMatchObject({ maxHeartRate: null, reserveHeartRate: null });
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: FAIL com `Cannot find module './blood-pressure'` e `'./anamnese-calculation'`.

- [ ] **Step 3: Implementar `blood-pressure.ts`**

A ordem das decisões é a regra. Escreva-a como está:

```ts
import type { AnamneseRules, HypertensionStageRule } from './anamnese-rules';

export type BloodPressureStatus = 'CLASSIFICADA' | 'DADOS_INCOMPLETOS' | 'FORA_DA_FAIXA_ETARIA';

export interface BloodPressureResult {
  status: BloodPressureStatus;
  classification: string | null;
}

export interface BloodPressureInput {
  systolic: number | null;
  diastolic: number | null;
  ageYears: number;
}

/**
 * BloodPressureClassifier (AN-05, decisão provisória de 2026-10-04 para a DIV-07). A idade vem antes dos dados
 * incompletos: abaixo da idade mínima nada é classificado, falte ou não uma das pressões.
 */
export function classifyBloodPressure(rules: AnamneseRules, input: BloodPressureInput): BloodPressureResult {
  const bp = rules.bloodPressure;
  if (input.ageYears < bp.minAgeYears) return { status: 'FORA_DA_FAIXA_ETARIA', classification: null };
  const { systolic, diastolic } = input;
  if (systolic === null || diastolic === null) return { status: 'DADOS_INCOMPLETOS', classification: null };

  // TODO(PREMISSA-CLINICA PC-01): validar com o médico responsável a categoria mais alta e a HA isolada (Quadro 3.2: "e", não "e/ou").
  const systolicStage = stageReached(bp.stages, (rule) => systolic >= rule.systolicFrom);
  const diastolicStage = stageReached(bp.stages, (rule) => diastolic >= rule.diastolicFrom);
  if (systolicStage > 0 && diastolicStage === 0) return classified(bp.labels.isolatedSystolic, systolicStage);
  if (diastolicStage > 0 && systolicStage === 0) return classified(bp.labels.isolatedDiastolic, diastolicStage);
  if (systolicStage > 0) return classified(bp.labels.stage, Math.max(systolicStage, diastolicStage));

  // TODO(PREMISSA-CLINICA PC-02): validar com o médico responsável a pressão baixa antes de PA normal e pré-hipertensão.
  if (systolic < bp.hypotension.systolicBelow || diastolic < bp.hypotension.diastolicBelow) return classified(bp.labels.hypotension);
  if (systolic >= bp.preHypertension.systolicFrom || diastolic >= bp.preHypertension.diastolicFrom) {
    return classified(bp.labels.preHypertension);
  }
  return classified(bp.labels.normal);
}

/** Maior estágio atingido (0 = nenhum). */
function stageReached(stages: readonly HypertensionStageRule[], reached: (rule: HypertensionStageRule) => boolean): number {
  let stage = 0;
  for (const rule of stages) if (reached(rule)) stage = rule.stage;
  return stage;
}

function classified(label: string, stage?: number): BloodPressureResult {
  return { status: 'CLASSIFICADA', classification: stage === undefined ? label : label.replace('{n}', String(stage)) };
}
```

- [ ] **Step 4: Implementar `anamnese-calculation.ts`**

`calculateAnamnesis` faz o seguinte:

- procura o método com `heartRateMethodsFor(rules, input.sex)`, para que um método de outro sexo não seja encontrado e a FC máxima fique `null`;
- calcula `maxHeartRate(method, input.ageYears)`;
- calcula `reserveHeartRate(max, input.restingHeartRate)`;
- chama `classifyBloodPressure(rules, { systolic: input.systolicPressure, diastolic: input.diastolicPressure, ageYears: input.ageYears })`;
- calcula `urineClassification(rules, input.urineScale)` e `idealWaterLiters(input.waterWeightKg, input.waterMlPerKg)`;
- devolve o objeto `AnamnesisCalculation`, com `rulesVersion: rules.version`.

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: PASS — `blood-pressure.spec` (38), `anamnese-calculation.spec` (3), além das suítes das Tasks 1 e 2.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat(api): classificação provisória da pressão arterial e cálculo da anamnese"
```

- [ ] **Step 7: PONTO DE CONTROLE DA PESSOA — regras clínicas**

Pare e mostre à pessoa a saída de:

```powershell
Set-Location apps/api
npx jest src/clinical-rules --verbose
Set-Location ../..
```

Cada linha é um caso com o valor esperado no nome. A pessoa confere:

- H-22: 150/80 → HA Sistólica Isolada – Estágio 1; 190/105 → HA Estágio 3; 170/115 → HA Estágio 3; 85/70 → Pressão Baixa / Hipotensão;
- cada limite da tabela de PA e a pressão baixa antes da normal (135/55 → Pressão Baixa);
- os 9 métodos de FC máxima aos 40 anos e o arredondamento para cima;
- água: 70 kg dá 2,45 L e 60 kg dá 2,10 L;
- escala de urina de 1 a 8;
- idade em 29/02;
- o arquivo `docs/premissas-clinicas.md`.

Só siga para a Task 4 com o "ok" da pessoa. Uma correção pedida aqui altera a `anamnese-v1` antes da primeira gravação; ela ainda não foi publicada para nenhum dado.

---

### Task 4: Modelo de dados da anamnese e validação contra o cadastro

**Files:**
- Modify: `apps/api/prisma/schema.prisma`: enums `AnamnesisStatus` e `BloodPressureStatus`, modelo `Anamnesis` e relação em `Patient`.
- Create: `apps/api/src/anamneses/anamnesis-normalize.ts`
- Test: `apps/api/src/anamneses/anamnesis-normalize.spec.ts`

**Interfaces:**
- Consumes: `ageOnDate`, `AnamneseRules`, `ANAMNESE_RULES_V1`, `heartRateMethodsFor`; `parseIsoDate`, `formatIsoDate`, `cleanText` e `cleanMultiline`.
- Produces:
  - `YES_NO_FIELDS` (22, em ordem) e `TEXT_FIELDS` (11), com os tipos `YesNoField` e `TextField`;
  - os tipos `AnamnesisInput`, `PatientFacts { sex: Sex; birthDate: string }`, `AnamnesisValues` e `AnamnesisFieldErrors`;
  - `normalizeAnamnesis(input: AnamnesisInput, patient: PatientFacts, rules: AnamneseRules, today: string): { values: AnamnesisValues; errors: AnamnesisFieldErrors }`;
  - o modelo Prisma `Anamnesis`, tabela `anamneses`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/anamneses/anamnesis-normalize.spec.ts`:

```ts
import { ANAMNESE_RULES_V1 as RULES } from '../clinical-rules/anamnese-rules';
import { normalizeAnamnesis, TEXT_FIELDS, YES_NO_FIELDS } from './anamnesis-normalize';

const man = { sex: 'MASCULINO' as const, birthDate: '1980-05-10' };
const TODAY = '2026-10-04';

describe('normalizeAnamnesis', () => {
  it('só a data: idade na data, 22 perguntas sem resposta, textos e medidas vazios', () => {
    const { values, errors } = normalizeAnamnesis({ anamnesisDate: '2026-05-09' }, man, RULES, TODAY);

    expect(errors).toEqual({});
    expect(values).toMatchObject({
      anamnesisDate: '2026-05-09', ageYears: 45, restingHeartRate: null, heartRateMethod: null, systolicPressure: null,
      diastolicPressure: null, urineScale: null, waterWeightKg: null, waterMlPerKg: null,
    });
    expect(Object.keys(values.answers)).toEqual([...YES_NO_FIELDS]);
    expect(Object.values(values.answers)).toEqual(Array(22).fill(null));
    expect(Object.keys(values.texts)).toEqual([...TEXT_FIELDS]);
    expect(Object.values(values.texts)).toEqual(Array(11).fill(null));
  });

  it('preserva Sim, Não e não respondida; limpa os textos', () => {
    const { values } = normalizeAnamnesis(
      { anamnesisDate: '2026-05-09', smoking: true, alcohol: false, goodSleep: null, sleepDetails: '  Dorme 6 h\r\npor noite ', observations: '   ' },
      man, RULES, TODAY,
    );

    expect(values.answers).toMatchObject({ smoking: true, alcohol: false, goodSleep: null, physicalActivity: null });
    expect(values.texts).toMatchObject({ sleepDetails: 'Dorme 6 h\npor noite', observations: null });
  });

  it('erros por campo em português; o campo inválido fica null', () => {
    const { values, errors } = normalizeAnamnesis(
      {
        anamnesisDate: '2026-05-09', restingHeartRate: 0, heartRateMethod: 'hossack-1981', systolicPressure: 120.5,
        diastolicPressure: 301, urineScale: 9, waterWeightKg: 70.123, waterMlPerKg: 0,
      },
      man, RULES, TODAY,
    );

    expect(errors).toEqual({
      restingHeartRate: 'A FC em repouso deve ser um número inteiro de 1 a 250 bpm.',
      heartRateMethod: 'Este método de FC máxima não se aplica ao sexo do paciente.',
      systolicPressure: 'A pressão sistólica deve ser um número inteiro de 1 a 300 mmHg.',
      diastolicPressure: 'A pressão diastólica deve ser um número inteiro de 1 a 300 mmHg.',
      urineScale: 'A escala de urina deve ser um número inteiro de 1 a 8.',
      waterWeightKg: 'O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
      waterMlPerKg: 'O fator deve ser maior que 0 e no máximo 200 mL/kg, com até 2 casas decimais.',
    });
    expect(values).toMatchObject({
      anamnesisDate: '2026-05-09', restingHeartRate: null, heartRateMethod: null, systolicPressure: null,
      diastolicPressure: null, urineScale: null, waterWeightKg: null, waterMlPerKg: null,
    });
  });

  it('aceita os limites', () => {
    const low = normalizeAnamnesis(
      { anamnesisDate: '2026-05-09', restingHeartRate: 1, systolicPressure: 1, diastolicPressure: 1, urineScale: 1, waterWeightKg: 0.01, waterMlPerKg: 0.01 },
      man, RULES, TODAY,
    );
    const high = normalizeAnamnesis(
      { anamnesisDate: '2026-05-09', restingHeartRate: 250, systolicPressure: 300, diastolicPressure: 300, urineScale: 8, waterWeightKg: 500, waterMlPerKg: 200 },
      man, RULES, TODAY,
    );
    const decimal = normalizeAnamnesis({ anamnesisDate: '2026-05-09', waterWeightKg: 48.9, waterMlPerKg: 33.33 }, man, RULES, TODAY);

    expect(low.errors).toEqual({});
    expect(high.errors).toEqual({});
    expect(decimal.errors).toEqual({});
    expect(decimal.values).toMatchObject({ waterWeightKg: 48.9, waterMlPerKg: 33.33 });
  });

  it('método desconhecido', () => {
    expect(normalizeAnamnesis({ anamnesisDate: '2026-05-09', heartRateMethod: 'astrand' }, man, RULES, TODAY).errors).toEqual({
      heartRateMethod: 'Método de FC máxima desconhecido.',
    });
  });

  it.each([
    [undefined, 'Informe a data da anamnese.'],
    ['', 'Informe a data da anamnese.'],
    ['2026-02-30', 'Data da anamnese inválida.'],
    ['04/10/2026', 'Data da anamnese inválida.'],
    ['2026-10-05', 'A data da anamnese não pode estar no futuro.'],
    ['1899-12-31', 'A data da anamnese deve ser a partir de 1900.'],
    ['1980-05-09', 'A data da anamnese não pode ser anterior ao nascimento do paciente.'],
  ])('data %p → %s', (anamnesisDate, message) => {
    const { values, errors } = normalizeAnamnesis({ anamnesisDate }, man, RULES, TODAY);
    expect(errors).toEqual({ anamnesisDate: message });
    expect(values).toMatchObject({ anamnesisDate: null, ageYears: null });
  });

  it('aceita a data de hoje e a do nascimento (idade 0)', () => {
    expect(normalizeAnamnesis({ anamnesisDate: TODAY }, man, RULES, TODAY).values.ageYears).toBe(46);
    expect(normalizeAnamnesis({ anamnesisDate: '1980-05-10' }, man, RULES, TODAY).values.ageYears).toBe(0);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/anamneses`
Expected: FAIL com `Cannot find module './anamnesis-normalize'`.

- [ ] **Step 3: Implementar `anamnesis-normalize.ts`**

```ts
import type { Sex } from '@prisma/client';
import { ageOnDate } from '../clinical-rules/age';
import type { AnamneseRules } from '../clinical-rules/anamnese-rules';
import { heartRateMethodsFor } from '../clinical-rules/heart-rate';
import { formatIsoDate, parseIsoDate } from '../common/civil-date';
import { cleanMultiline, cleanText } from '../common/text';

/** As 22 perguntas Sim/Não (DIV-13), na ordem do formulário; a coluna da aba Anamnese vai ao lado. */
export const YES_NO_FIELDS = [
  'alcohol', // P
  'smoking', // Q
  'goodSleep', // R
  'physicalActivity', // T
  'adequateWater', // V
  'supplements', // Y
  'anemia', // AB
  'diabetes', // AC
  'cardiacConditions', // AD
  'bloodPressureConditions', // AE
  'circulatoryConditions', // AF
  'allergies', // AG
  'treatment', // AI
  'medications', // AK
  'healthDevice', // AM (no legado, controle 70 = Sim e 69 = Não, invertido: atenção numa futura importação)
  'anxiety', // AO
  'depression', // AP
  'eatingDisorders', // AQ
  'bipolarDisorder', // AR
  'ocd', // AS
  'postTraumaticStress', // AT
  'otherMentalConditions', // AU
] as const;

/** Os 11 textos livres: colunas S, U, X, Z, AA, AH, AJ, AL, AN, AV e O. */
export const TEXT_FIELDS = [
  'sleepDetails', 'activityDetails', 'waterDetails', 'supplementDetails', 'eatingHabits', 'otherConditions',
  'treatmentDetails', 'medicationDetails', 'healthDeviceDetails', 'mentalConditionsDetails', 'observations',
] as const;

export type YesNoField = (typeof YES_NO_FIELDS)[number];
export type TextField = (typeof TEXT_FIELDS)[number];

export type AnamnesisInput = {
  anamnesisDate?: string | null;
  restingHeartRate?: number | null;
  heartRateMethod?: string | null;
  systolicPressure?: number | null;
  diastolicPressure?: number | null;
  urineScale?: number | null;
  waterWeightKg?: number | null;
  waterMlPerKg?: number | null;
} & Partial<Record<YesNoField, boolean | null>> &
  Partial<Record<TextField, string | null>>;

export interface PatientFacts {
  sex: Sex;
  /** AAAA-MM-DD */
  birthDate: string;
}

export interface AnamnesisValues {
  anamnesisDate: string | null;
  ageYears: number | null;
  restingHeartRate: number | null;
  heartRateMethod: string | null;
  systolicPressure: number | null;
  diastolicPressure: number | null;
  urineScale: number | null;
  waterWeightKg: number | null;
  waterMlPerKg: number | null;
  answers: Record<YesNoField, boolean | null>;
  texts: Record<TextField, string | null>;
}

export type AnamnesisFieldErrors = Partial<Record<keyof AnamnesisInput, string>>;

const MIN_DATE = '1900-01-01';
const INVALID = Symbol('inválido');
type Checked = number | null | typeof INVALID;

/**
 * Valida e limpa a anamnese contra o cadastro do paciente. Campo inválido fica null em `values` e ganha mensagem em
 * `errors`: a prévia calcula o que der e a gravação recusa qualquer erro. Sem resposta é null, nunca "Não".
 */
export function normalizeAnamnesis(
  input: AnamnesisInput,
  patient: PatientFacts,
  rules: AnamneseRules,
  today: string,
): { values: AnamnesisValues; errors: AnamnesisFieldErrors } {
  const errors: AnamnesisFieldErrors = {};
  const check = (field: keyof AnamnesisInput, value: Checked, message: string): number | null => {
    if (value !== INVALID) return value;
    errors[field] = message;
    return null;
  };

  const anamnesisDate = validDate(input.anamnesisDate, patient.birthDate, today, errors);
  const values: AnamnesisValues = {
    anamnesisDate,
    ageYears: anamnesisDate === null ? null : ageOnDate(patient.birthDate, anamnesisDate),
    restingHeartRate: check('restingHeartRate', integerIn(input.restingHeartRate, 1, 250), 'A FC em repouso deve ser um número inteiro de 1 a 250 bpm.'),
    heartRateMethod: validMethod(input.heartRateMethod, patient.sex, rules, errors),
    systolicPressure: check('systolicPressure', integerIn(input.systolicPressure, 1, 300), 'A pressão sistólica deve ser um número inteiro de 1 a 300 mmHg.'),
    diastolicPressure: check('diastolicPressure', integerIn(input.diastolicPressure, 1, 300), 'A pressão diastólica deve ser um número inteiro de 1 a 300 mmHg.'),
    urineScale: check('urineScale', integerIn(input.urineScale, 1, 8), 'A escala de urina deve ser um número inteiro de 1 a 8.'),
    waterWeightKg: check('waterWeightKg', centesimalUpTo(input.waterWeightKg, 500), 'O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.'),
    waterMlPerKg: check('waterMlPerKg', centesimalUpTo(input.waterMlPerKg, 200), 'O fator deve ser maior que 0 e no máximo 200 mL/kg, com até 2 casas decimais.'),
    answers: Object.fromEntries(YES_NO_FIELDS.map((field) => [field, input[field] ?? null])) as Record<YesNoField, boolean | null>,
    texts: Object.fromEntries(TEXT_FIELDS.map((field) => [field, cleanMultiline(input[field])])) as Record<TextField, string | null>,
  };
  return { values, errors };
}

function validDate(value: string | null | undefined, birthDate: string, today: string, errors: AnamnesisFieldErrors): string | null {
  if (value === null || value === undefined || value.trim() === '') {
    errors.anamnesisDate = 'Informe a data da anamnese.';
    return null;
  }
  const parsed = parseIsoDate(value);
  if (!parsed) {
    errors.anamnesisDate = 'Data da anamnese inválida.';
    return null;
  }
  const iso = formatIsoDate(parsed);
  if (iso > today) errors.anamnesisDate = 'A data da anamnese não pode estar no futuro.';
  else if (iso < MIN_DATE) errors.anamnesisDate = 'A data da anamnese deve ser a partir de 1900.';
  else if (iso < birthDate) errors.anamnesisDate = 'A data da anamnese não pode ser anterior ao nascimento do paciente.';
  else return iso;
  return null;
}

function validMethod(value: string | null | undefined, sex: Sex, rules: AnamneseRules, errors: AnamnesisFieldErrors): string | null {
  const id = cleanText(value);
  if (id === null) return null;
  if (!rules.heartRate.methods.some((method) => method.id === id)) {
    errors.heartRateMethod = 'Método de FC máxima desconhecido.';
    return null;
  }
  if (!heartRateMethodsFor(rules, sex).some((method) => method.id === id)) {
    errors.heartRateMethod = 'Este método de FC máxima não se aplica ao sexo do paciente.';
    return null;
  }
  return id;
}

function integerIn(value: number | null | undefined, min: number, max: number): Checked {
  if (value === null || value === undefined) return null;
  return Number.isInteger(value) && value >= min && value <= max ? value : INVALID;
}

/** Maior que 0, até `max`, com no máximo 2 casas decimais. */
function centesimalUpTo(value: number | null | undefined, max: number): Checked {
  if (value === null || value === undefined) return null;
  const twoDecimals = Math.abs(value * 100 - Math.round(value * 100)) < 1e-6;
  return Number.isFinite(value) && value > 0 && value <= max && twoDecimals ? value : INVALID;
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/anamneses`
Expected: PASS — `anamnesis-normalize.spec` (13).

- [ ] **Step 5: Acrescentar o modelo ao schema e gerar a migração**

Em `apps/api/prisma/schema.prisma`, acrescente `anamneses Anamnesis[]` ao modelo `Patient` (depois de `updatedById`). No fim do arquivo, acrescente:

```prisma
enum AnamnesisStatus {
  RASCUNHO
  FINALIZADA
}

enum BloodPressureStatus {
  CLASSIFICADA
  DADOS_INCOMPLETOS
  FORA_DA_FAIXA_ETARIA
}

/// Anamnese (aba Anamnese do legado). Valores calculados, idade e sexo são gravados com a versão das regras usada.
model Anamnesis {
  id                          String              @id @default(uuid()) @db.Uuid
  patientId                   String              @map("patient_id") @db.Uuid
  patient                     Patient             @relation(fields: [patientId], references: [id], onDelete: Restrict)
  status                      AnamnesisStatus     @default(RASCUNHO)
  anamnesisDate               DateTime            @map("anamnesis_date") @db.Date
  rulesVersion                String              @map("rules_version")
  sex                         Sex
  ageYears                    Int                 @map("age_years")
  restingHeartRate            Int?                @map("resting_heart_rate")
  heartRateMethod             String?             @map("heart_rate_method")
  maxHeartRate                Int?                @map("max_heart_rate")
  reserveHeartRate            Int?                @map("reserve_heart_rate")
  systolicPressure            Int?                @map("systolic_pressure")
  diastolicPressure           Int?                @map("diastolic_pressure")
  bloodPressureStatus         BloodPressureStatus @map("blood_pressure_status")
  bloodPressureClassification String?             @map("blood_pressure_classification")
  urineScale                  Int?                @map("urine_scale")
  urineClassification         String?             @map("urine_classification")
  waterWeightKg               Decimal?            @map("water_weight_kg") @db.Decimal(5, 2)
  waterMlPerKg                Decimal?            @map("water_ml_per_kg") @db.Decimal(5, 2)
  idealWaterLiters            Decimal?            @map("ideal_water_liters") @db.Decimal(5, 2)
  alcohol                     Boolean?
  smoking                     Boolean?
  goodSleep                   Boolean?            @map("good_sleep")
  physicalActivity            Boolean?            @map("physical_activity")
  adequateWater               Boolean?            @map("adequate_water")
  supplements                 Boolean?
  anemia                      Boolean?
  diabetes                    Boolean?
  cardiacConditions           Boolean?            @map("cardiac_conditions")
  bloodPressureConditions     Boolean?            @map("blood_pressure_conditions")
  circulatoryConditions       Boolean?            @map("circulatory_conditions")
  allergies                   Boolean?
  treatment                   Boolean?
  medications                 Boolean?
  healthDevice                Boolean?            @map("health_device")
  anxiety                     Boolean?
  depression                  Boolean?
  eatingDisorders             Boolean?            @map("eating_disorders")
  bipolarDisorder             Boolean?            @map("bipolar_disorder")
  ocd                         Boolean?
  postTraumaticStress         Boolean?            @map("post_traumatic_stress")
  otherMentalConditions       Boolean?            @map("other_mental_conditions")
  sleepDetails                String?             @map("sleep_details")
  activityDetails             String?             @map("activity_details")
  waterDetails                String?             @map("water_details")
  supplementDetails           String?             @map("supplement_details")
  eatingHabits                String?             @map("eating_habits")
  otherConditions             String?             @map("other_conditions")
  treatmentDetails            String?             @map("treatment_details")
  medicationDetails           String?             @map("medication_details")
  healthDeviceDetails         String?             @map("health_device_details")
  mentalConditionsDetails     String?             @map("mental_conditions_details")
  observations                String?
  version                     Int                 @default(1)
  createdAt                   DateTime            @default(now()) @map("created_at") @db.Timestamptz(3)
  updatedAt                   DateTime            @updatedAt @map("updated_at") @db.Timestamptz(3)
  createdById                 String              @map("created_by_id") @db.Uuid
  updatedById                 String              @map("updated_by_id") @db.Uuid
  finalizedAt                 DateTime?           @map("finalized_at") @db.Timestamptz(3)
  finalizedById               String?             @map("finalized_by_id") @db.Uuid

  @@index([patientId, anamnesisDate])
  @@map("anamneses")
}
```

```powershell
Set-Location apps/api
npx prisma migrate dev --name anamneses
Set-Location ../..
```

Expected: a pasta `apps/api/prisma/migrations/<carimbo>_anamneses/` é criada com `CREATE TABLE "anamneses"` e os dois `CREATE TYPE`, e o Prisma Client é gerado.

- [ ] **Step 6: Conferir que nada quebrou**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS em todas as suítes (19 anteriores e 9 novas); `typecheck` sem erros.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat(api): modelo da anamnese e validação contra o cadastro do paciente"
```

---

### Task 5: API da anamnese — regras, lista, cálculo, criação, leitura e rascunho

**Files:**
- Modify: `apps/api/src/auth/require-permissions.decorator.ts`, `apps/api/src/auth/auth.guard.ts:33-36`, `apps/api/src/app.module.ts`
- Create: `apps/api/src/anamneses/anamnesis.view.ts`, `apps/api/src/anamneses/dto/anamnesis.dto.ts`, `apps/api/src/anamneses/anamneses.service.ts`, `apps/api/src/anamneses/anamneses.controller.ts`, `apps/api/src/anamneses/anamneses.module.ts`
- Test: `apps/api/test/anamneses.e2e-spec.ts`

**Interfaces:**
- Consumes: `normalizeAnamnesis`, `YES_NO_FIELDS`, `TEXT_FIELDS`, `AnamnesisInput`, `AnamnesisFieldErrors` (Task 4); `calculateAnamnesis`, `AnamnesisCalculation` (Task 3); `CURRENT_ANAMNESE_RULES`, `rulesForVersion` (Task 1); `URINE_SCALES` (Task 2); `AuditService.record`, `diffFields`, `todayInTimeZone`, `APP_CONFIG`.
- Produces:
  - `RequireAnyPermission(...permissions: Permission[])`, com a metadata `REQUIRED_ANY_PERMISSIONS`;
  - os tipos `AnamnesisView`, `AnamnesisListItem = { id; anamnesisDate; status; finalizedAt; updatedAt }` e `AnamnesisRulesView = { version; status; heartRateMethods: { id; label; sexes }[]; bloodPressure: { source; minAgeYears }; urineScale: { scale; label }[]; water: { defaultMlPerKg } }`;
  - o serviço `AnamnesesService`, com `rules`, `list`, `calculate`, `get`, `create` e `update`. O método privado `save(id, body, actorId, mode: 'update' | 'finalize')` já fica pronto para a Task 6.
  - Rotas:

  | Rota | Resposta |
  |---|---|
  | `GET /api/anamneses/rules` | `AnamnesisRulesView` |
  | `GET /api/patients/:patientId/anamneses` | `AnamnesisListItem[]` |
  | `GET /api/patients/:patientId/anamneses/calculation?anamnesisDate&restingHeartRate&heartRateMethod&systolicPressure&diastolicPressure&urineScale&waterWeightKg&waterMlPerKg` | `{ calculation: AnamnesisCalculation \| null; errors }` |
  | `POST /api/patients/:patientId/anamneses` | 201, `AnamnesisView` |
  | `GET /api/anamneses/:id` | `AnamnesisView` |
  | `PUT /api/anamneses/:id`, com `version` no corpo | `AnamnesisView` |

  - Erros:

  | Situação | Status | Corpo |
  |---|---|---|
  | dados inválidos | 400 | `{ message: 'Dados inválidos.', errors }` |
  | conflito de versão | 409 | `{ message: 'Esta anamnese foi alterada por outra pessoa. Recarregue para ver a versão atual.', current }` |
  | anamnese finalizada | 409 | `{ message: 'Esta anamnese já foi finalizada e não pode ser alterada.', current }` |
  | anamnese inexistente | 404 | `'Anamnese não encontrada.'` |
  | paciente inexistente | 404 | `'Paciente não encontrado.'` |

- [ ] **Step 1: Escrever os testes**

`apps/api/test/anamneses.e2e-spec.ts`:

```ts
import { randomUUID } from 'node:crypto';
import { INestApplication } from '@nestjs/common';
import { TEXT_FIELDS, YES_NO_FIELDS } from '../src/anamneses/anamnesis-normalize';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';

const homem = { fullName: 'Paciente Sintético Um', sex: 'MASCULINO', birthDate: '1980-05-10' };
const completa = {
  anamnesisDate: '2026-05-09', restingHeartRate: 70, heartRateMethod: 'tanaka-2001', systolicPressure: 150, diastolicPressure: 80,
  urineScale: 4, waterWeightKg: 70, waterMlPerKg: 35, smoking: true, alcohol: false, sleepDetails: '  Dorme 6 h  ',
};
const CONFLICT = 'Esta anamnese foi alterada por outra pessoa. Recarregue para ver a versão atual.';

describe('Anamneses (HTTP)', () => {
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

  async function patientFor(agent: Agent, body: Record<string, unknown> = homem): Promise<string> {
    const res = await agent.post('/api/patients').send(body);
    expect(res.status).toBe(201);
    return res.body.id as string;
  }

  async function draft(agent: Agent, patientId: string, body: Record<string, unknown> = completa) {
    const res = await agent.post(`/api/patients/${patientId}/anamneses`).send(body);
    expect(res.status).toBe(201);
    return res.body as { id: string; version: number };
  }

  async function setAssistant(admin: Agent, changes: { permission: string; granted: boolean }[]) {
    expect((await admin.put('/api/permissions/assistant').send({ changes })).status).toBe(200);
  }

  it('assistente cria rascunho com os cálculos da API, a idade na data da anamnese e auditoria', async () => {
    const { agent, user } = await loginAs(app, 'ASSISTENTE');
    const patientId = await patientFor(agent);

    const res = await agent.post(`/api/patients/${patientId}/anamneses`).send(completa);

    expect(res.status).toBe(201);
    expect(res.body).toMatchObject({
      patientId, status: 'RASCUNHO', version: 1, rulesVersion: 'anamnese-v1', sex: 'MASCULINO', ageYears: 45,
      anamnesisDate: '2026-05-09', restingHeartRate: 70, heartRateMethod: 'tanaka-2001',
      heartRateMethodLabel: 'Tanaka et al., 2001: 208 − 0,7 × idade', maxHeartRate: 177, reserveHeartRate: 107,
      systolicPressure: 150, diastolicPressure: 80, bloodPressureStatus: 'CLASSIFICADA',
      bloodPressureClassification: 'HA Sistólica Isolada – Estágio 1', urineScale: 4, urineClassification: 'Levemente Desidratado',
      waterWeightKg: 70, waterMlPerKg: 35, idealWaterLiters: 2.45, smoking: true, alcohol: false, goodSleep: null,
      sleepDetails: 'Dorme 6 h', finalizedAt: null,
    });
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'anamnesis.create' } });
    expect(event).toMatchObject({ actorId: user.id, entityType: 'anamnesis', entityId: res.body.id, data: { patientId } });
  });

  it('pergunta não marcada é null: nenhuma resposta pré-marcada e nada calculado sem dados', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);

    const res = await agent.post(`/api/patients/${patientId}/anamneses`).send({ anamnesisDate: '2026-05-09' });

    expect(res.status).toBe(201);
    for (const field of [...YES_NO_FIELDS, ...TEXT_FIELDS]) expect(res.body[field]).toBeNull();
    expect(res.body).toMatchObject({
      maxHeartRate: null, reserveHeartRate: null, bloodPressureStatus: 'DADOS_INCOMPLETOS', bloodPressureClassification: null,
      urineScale: null, urineClassification: null, waterWeightKg: null, waterMlPerKg: null, idealWaterLiters: null,
    });
  });

  it('H-12: a escala gravada é a escolhida, inclusive a 8', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);
    const { id } = await draft(agent, patientId, { anamnesisDate: '2026-05-09', urineScale: 8 });

    expect((await agent.get(`/api/anamneses/${id}`)).body).toMatchObject({ urineScale: 8, urineClassification: 'Severamente Desidratado' });
  });

  it('devolve erros por campo em português e não grava nada', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);

    const res = await agent.post(`/api/patients/${patientId}/anamneses`).send({
      anamnesisDate: '1980-05-09', heartRateMethod: 'hossack-1981', urineScale: 9, waterWeightKg: 0, systolicPressure: 120.5,
    });

    expect(res.status).toBe(400);
    expect(res.body).toEqual({
      message: 'Dados inválidos.',
      errors: {
        anamnesisDate: 'A data da anamnese não pode ser anterior ao nascimento do paciente.',
        heartRateMethod: 'Este método de FC máxima não se aplica ao sexo do paciente.',
        systolicPressure: 'A pressão sistólica deve ser um número inteiro de 1 a 300 mmHg.',
        urineScale: 'A escala de urina deve ser um número inteiro de 1 a 8.',
        waterWeightKg: 'O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
      },
    });
    expect(await prisma.anamnesis.count()).toBe(0);
    expect(await prisma.auditEvent.count({ where: { action: 'anamnesis.create' } })).toBe(0);
  });

  it('lista as anamneses do paciente da mais recente para a mais antiga, só com os campos da lista', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);
    const other = await patientFor(agent, { ...homem, fullName: 'Paciente Sintético Dois' });
    for (const anamnesisDate of ['2026-01-10', '2026-03-01', '2026-02-15']) await draft(agent, patientId, { anamnesisDate });
    await draft(agent, other, { anamnesisDate: '2026-04-01' });

    const res = await agent.get(`/api/patients/${patientId}/anamneses`);

    expect(res.status).toBe(200);
    expect(res.body.map((item: { anamnesisDate: string }) => item.anamnesisDate)).toEqual(['2026-03-01', '2026-02-15', '2026-01-10']);
    expect(Object.keys(res.body[0]).sort()).toEqual(['anamnesisDate', 'finalizedAt', 'id', 'status', 'updatedAt']);
    expect(res.body[0]).toMatchObject({ status: 'RASCUNHO', finalizedAt: null });
  });

  it('atualiza o rascunho com a versão atual, audita o que mudou e recusa versão antiga com 409', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);
    const { id } = await draft(agent, patientId);

    const first = await agent.put(`/api/anamneses/${id}`).send({ ...completa, smoking: false, version: 1 });
    const stale = await agent.put(`/api/anamneses/${id}`).send({ ...completa, smoking: null, version: 1 });

    expect(first.status).toBe(200);
    expect(first.body).toMatchObject({ smoking: false, version: 2, status: 'RASCUNHO' });
    expect(stale.status).toBe(409);
    expect(stale.body.message).toBe(CONFLICT);
    expect(stale.body.current).toMatchObject({ smoking: false, version: 2 });
    const update = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'anamnesis.update' } });
    expect(update.data).toEqual({ patientId, changes: { smoking: { from: true, to: false } } });
  });

  it('cálculo sem gravar: devolve os valores possíveis e os erros por campo', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);

    const res = await agent.get(`/api/patients/${patientId}/anamneses/calculation`).query({
      anamnesisDate: '2026-05-09', heartRateMethod: 'hossack-1981', restingHeartRate: 70, systolicPressure: 190,
      diastolicPressure: 105, waterWeightKg: 60, waterMlPerKg: 35,
    });
    const withoutDate = await agent.get(`/api/patients/${patientId}/anamneses/calculation`).query({ systolicPressure: 120 });

    expect(res.status).toBe(200);
    expect(res.body).toEqual({
      calculation: {
        rulesVersion: 'anamnese-v1', ageYears: 45, maxHeartRate: null, reserveHeartRate: null, bloodPressureStatus: 'CLASSIFICADA',
        bloodPressureClassification: 'HA Estágio 3', urineClassification: null, idealWaterLiters: 2.1,
      },
      errors: { heartRateMethod: 'Este método de FC máxima não se aplica ao sexo do paciente.' },
    });
    expect(withoutDate.body).toEqual({ calculation: null, errors: { anamnesisDate: 'Informe a data da anamnese.' } });
    expect(await prisma.anamnesis.count()).toBe(0);
  });

  it('regras correntes para o formulário', async () => {
    const { agent } = await loginAs(app, 'ASSISTENTE');

    const res = await agent.get('/api/anamneses/rules');

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({ version: 'anamnese-v1', status: 'PROVISORIA', water: { defaultMlPerKg: 35 }, bloodPressure: { minAgeYears: 18 } });
    expect(res.body.heartRateMethods).toHaveLength(9);
    expect(res.body.heartRateMethods[0]).toEqual({
      id: 'karvonen-1957', label: 'Karvonen et al., 1957 — clássica: 220 − idade', sexes: ['FEMININO', 'MASCULINO'],
    });
    expect(res.body.urineScale).toHaveLength(8);
    expect(res.body.urineScale[3]).toEqual({ scale: 4, label: 'Levemente Desidratado' });
  });

  it('rascunho exige anamneses.draft ou anamneses.finalize, além de patients.read', async () => {
    const professional = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(professional.agent);
    const { id } = await draft(professional.agent, patientId);
    const assistant = await loginAs(app, 'ASSISTENTE');
    const admin = await loginAs(app, 'ADMIN');
    expect((await assistant.agent.get(`/api/anamneses/${id}`)).status).toBe(200);

    await setAssistant(admin.agent, [{ permission: 'anamneses.draft', granted: false }]);
    const forbidden = [
      () => assistant.agent.get('/api/anamneses/rules'),
      () => assistant.agent.get(`/api/patients/${patientId}/anamneses`),
      () => assistant.agent.get(`/api/patients/${patientId}/anamneses/calculation`).query({ anamnesisDate: '2026-05-09' }),
      () => assistant.agent.post(`/api/patients/${patientId}/anamneses`).send(completa),
      () => assistant.agent.get(`/api/anamneses/${id}`),
      () => assistant.agent.put(`/api/anamneses/${id}`).send({ ...completa, version: 1 }),
    ];
    for (const send of forbidden) expect((await send()).status).toBe(403);

    // Delegação da finalização (desligada por padrão): basta uma das duas para ler e editar o rascunho.
    await setAssistant(admin.agent, [{ permission: 'anamneses.finalize', granted: true }]);
    expect((await assistant.agent.get(`/api/anamneses/${id}`)).status).toBe(200);
  });

  it('sem patients.read nenhuma rota de anamnese responde dados, nem o 409', async () => {
    const professional = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(professional.agent);
    const { id } = await draft(professional.agent, patientId);
    const assistant = await loginAs(app, 'ASSISTENTE');
    const admin = await loginAs(app, 'ADMIN');
    await setAssistant(admin.agent, [{ permission: 'patients.read', granted: false }]);

    const responses = [
      await assistant.agent.get(`/api/patients/${patientId}/anamneses`),
      await assistant.agent.get(`/api/anamneses/${id}`),
      await assistant.agent.put(`/api/anamneses/${id}`).send({ ...completa, version: 99 }),
      await assistant.agent.put(`/api/anamneses/${id}`).send({ ...completa, smoking: false, version: 1 }),
      await assistant.agent.post(`/api/patients/${patientId}/anamneses`).send(completa),
    ];

    for (const res of responses) {
      expect(res.status).toBe(403);
      expect(res.body).not.toHaveProperty('current');
      expect(JSON.stringify(res.body)).not.toContain('Dorme');
    }
    expect(await prisma.anamnesis.findUniqueOrThrow({ where: { id } })).toMatchObject({ smoking: true, version: 1 });
  });

  it('responde 404 para paciente ou anamnese inexistente e 400 para id inválido', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');

    const noPatient = await agent.post(`/api/patients/${randomUUID()}/anamneses`).send(completa);
    const noAnamnesis = await agent.get(`/api/anamneses/${randomUUID()}`);

    expect(noPatient.status).toBe(404);
    expect(noPatient.body.message).toBe('Paciente não encontrado.');
    expect(noAnamnesis.status).toBe(404);
    expect(noAnamnesis.body.message).toBe('Anamnese não encontrada.');
    expect((await agent.get('/api/anamneses/abc')).status).toBe(400);
    expect((await agent.get('/api/patients/abc/anamneses')).status).toBe(400);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- test/anamneses.e2e-spec.ts`
Expected: FAIL. Todas as rotas respondem 404 (`Cannot GET /api/anamneses/rules`, `Cannot POST /api/patients/.../anamneses`).

- [ ] **Step 3: "Basta uma das permissões" na guarda**

Em `apps/api/src/auth/require-permissions.decorator.ts`, acrescente:

```ts
export const REQUIRED_ANY_PERMISSIONS = 'requiredAnyPermissions';
/** Basta uma destas (combina com RequirePermissions, que exige todas as dela). */
export const RequireAnyPermission = (...permissions: Permission[]) => SetMetadata(REQUIRED_ANY_PERMISSIONS, permissions);
```

Em `apps/api/src/auth/auth.guard.ts`, linhas 33–36, troque a checagem por:

```ts
    const required = this.reflector.getAllAndOverride<Permission[] | undefined>(REQUIRED_PERMISSIONS, targets) ?? [];
    const anyOf = this.reflector.getAllAndOverride<Permission[] | undefined>(REQUIRED_ANY_PERMISSIONS, targets) ?? [];
    const allowed =
      required.every((permission) => permissions.includes(permission)) &&
      (anyOf.length === 0 || anyOf.some((permission) => permissions.includes(permission)));
    if (!allowed) {
      throw new ForbiddenException('Você não tem permissão para esta ação.');
```

e importe `REQUIRED_ANY_PERMISSIONS` junto com `REQUIRED_PERMISSIONS`.

- [ ] **Step 4: Visão e DTOs**

`apps/api/src/anamneses/anamnesis.view.ts`:

```ts
import type { Anamnesis, Prisma } from '@prisma/client';
import type { AnamneseRules } from '../clinical-rules/anamnese-rules';
import { rulesForVersion } from '../clinical-rules/anamnese-rules';
import { URINE_SCALES } from '../clinical-rules/urine-scale';
import { formatIsoDate } from '../common/civil-date';
import { TEXT_FIELDS, TextField, YES_NO_FIELDS, YesNoField } from './anamnesis-normalize';

export type AnamnesisView = {
  id: string;
  patientId: string;
  status: Anamnesis['status'];
  anamnesisDate: string;
  rulesVersion: string;
  sex: Anamnesis['sex'];
  ageYears: number;
  restingHeartRate: number | null;
  heartRateMethod: string | null;
  /** Legenda do método na versão de regras da própria anamnese. */
  heartRateMethodLabel: string | null;
  maxHeartRate: number | null;
  reserveHeartRate: number | null;
  systolicPressure: number | null;
  diastolicPressure: number | null;
  bloodPressureStatus: Anamnesis['bloodPressureStatus'];
  bloodPressureClassification: string | null;
  urineScale: number | null;
  urineClassification: string | null;
  waterWeightKg: number | null;
  waterMlPerKg: number | null;
  idealWaterLiters: number | null;
  version: number;
  createdAt: string;
  updatedAt: string;
  finalizedAt: string | null;
} & Record<YesNoField, boolean | null> &
  Record<TextField, string | null>;

export type AnamnesisListItem = Pick<AnamnesisView, 'id' | 'anamnesisDate' | 'status' | 'finalizedAt' | 'updatedAt'>;

export interface AnamnesisRulesView {
  version: string;
  status: AnamneseRules['status'];
  heartRateMethods: { id: string; label: string; sexes: Anamnesis['sex'][] }[];
  bloodPressure: { source: string; minAgeYears: number };
  urineScale: { scale: number; label: string }[];
  water: { defaultMlPerKg: number };
}

function decimal(value: Prisma.Decimal | null): number | null {
  return value === null ? null : value.toNumber();
}

function pick<K extends string, V>(source: Record<K, V>, fields: readonly K[]): Record<K, V> {
  return Object.fromEntries(fields.map((field) => [field, source[field]])) as Record<K, V>;
}

export function toAnamnesisView(a: Anamnesis): AnamnesisView {
  const method = a.heartRateMethod ? rulesForVersion(a.rulesVersion).heartRate.methods.find((m) => m.id === a.heartRateMethod) : undefined;
  return {
    id: a.id,
    patientId: a.patientId,
    status: a.status,
    anamnesisDate: formatIsoDate(a.anamnesisDate),
    rulesVersion: a.rulesVersion,
    sex: a.sex,
    ageYears: a.ageYears,
    restingHeartRate: a.restingHeartRate,
    heartRateMethod: a.heartRateMethod,
    heartRateMethodLabel: method?.label ?? null,
    maxHeartRate: a.maxHeartRate,
    reserveHeartRate: a.reserveHeartRate,
    systolicPressure: a.systolicPressure,
    diastolicPressure: a.diastolicPressure,
    bloodPressureStatus: a.bloodPressureStatus,
    bloodPressureClassification: a.bloodPressureClassification,
    urineScale: a.urineScale,
    urineClassification: a.urineClassification,
    waterWeightKg: decimal(a.waterWeightKg),
    waterMlPerKg: decimal(a.waterMlPerKg),
    idealWaterLiters: decimal(a.idealWaterLiters),
    ...pick(a, YES_NO_FIELDS),
    ...pick(a, TEXT_FIELDS),
    version: a.version,
    createdAt: a.createdAt.toISOString(),
    updatedAt: a.updatedAt.toISOString(),
    finalizedAt: a.finalizedAt?.toISOString() ?? null,
  };
}

export function toAnamnesisListItem(a: Anamnesis): AnamnesisListItem {
  return {
    id: a.id,
    anamnesisDate: formatIsoDate(a.anamnesisDate),
    status: a.status,
    finalizedAt: a.finalizedAt?.toISOString() ?? null,
    updatedAt: a.updatedAt.toISOString(),
  };
}

export function toRulesView(rules: AnamneseRules): AnamnesisRulesView {
  return {
    version: rules.version,
    status: rules.status,
    heartRateMethods: rules.heartRate.methods.map((m) => ({ id: m.id, label: m.label, sexes: [...m.sexes] })),
    bloodPressure: { source: rules.bloodPressure.source, minAgeYears: rules.bloodPressure.minAgeYears },
    urineScale: URINE_SCALES.map((scale) => ({ scale, label: rules.urine.labels[scale] })),
    water: { defaultMlPerKg: rules.water.defaultMlPerKg },
  };
}
```

`apps/api/src/anamneses/dto/anamnesis.dto.ts`. A data é opcional no DTO para que a falta dela receba a mensagem em português de `normalizeAnamnesis`. Os 22 booleanos e os 11 textos são declarados um a um, com os nomes de `YES_NO_FIELDS` e `TEXT_FIELDS` (exemplos abaixo):

```ts
import { Type } from 'class-transformer';
import { IsBoolean, IsInt, IsNumber, IsOptional, IsString, MaxLength, Min } from 'class-validator';

const FINITE = { allowNaN: false, allowInfinity: false };

export class AnamnesisBodyDto {
  @IsOptional() @IsString() @MaxLength(10) anamnesisDate?: string | null;
  @IsOptional() @IsNumber(FINITE) restingHeartRate?: number | null;
  @IsOptional() @IsString() @MaxLength(40) heartRateMethod?: string | null;
  @IsOptional() @IsNumber(FINITE) systolicPressure?: number | null;
  @IsOptional() @IsNumber(FINITE) diastolicPressure?: number | null;
  @IsOptional() @IsNumber(FINITE) urineScale?: number | null;
  @IsOptional() @IsNumber(FINITE) waterWeightKg?: number | null;
  @IsOptional() @IsNumber(FINITE) waterMlPerKg?: number | null;
  @IsOptional() @IsBoolean() alcohol?: boolean | null;
  // … um @IsOptional() @IsBoolean() para cada um dos 22 campos de YES_NO_FIELDS, na mesma ordem …
  @IsOptional() @IsBoolean() otherMentalConditions?: boolean | null;
  @IsOptional() @IsString() @MaxLength(4000) sleepDetails?: string | null;
  // … um @IsOptional() @IsString() @MaxLength(4000) para cada um dos 11 campos de TEXT_FIELDS …
  @IsOptional() @IsString() @MaxLength(4000) observations?: string | null;
}

export class UpdateAnamnesisDto extends AnamnesisBodyDto {
  @IsInt() @Min(1) version!: number;
}

/** Só o que entra nos cálculos; números chegam como texto na query. */
export class AnamnesisCalculationQueryDto {
  @IsOptional() @IsString() @MaxLength(10) anamnesisDate?: string;
  @IsOptional() @Type(() => Number) @IsNumber(FINITE) restingHeartRate?: number;
  @IsOptional() @IsString() @MaxLength(40) heartRateMethod?: string;
  @IsOptional() @Type(() => Number) @IsNumber(FINITE) systolicPressure?: number;
  @IsOptional() @Type(() => Number) @IsNumber(FINITE) diastolicPressure?: number;
  @IsOptional() @Type(() => Number) @IsNumber(FINITE) urineScale?: number;
  @IsOptional() @Type(() => Number) @IsNumber(FINITE) waterWeightKg?: number;
  @IsOptional() @Type(() => Number) @IsNumber(FINITE) waterMlPerKg?: number;
}
```

- [ ] **Step 5: Serviço**

`apps/api/src/anamneses/anamneses.service.ts`:

```ts
import { BadRequestException, ConflictException, Inject, Injectable, NotFoundException } from '@nestjs/common';
import type { Anamnesis, Patient, Prisma, Sex } from '@prisma/client';
import { AuditService } from '../audit/audit.service';
import { diffFields } from '../audit/diff';
import { AnamnesisCalculation, calculateAnamnesis } from '../clinical-rules/anamnese-calculation';
import { CURRENT_ANAMNESE_RULES } from '../clinical-rules/anamnese-rules';
import { formatIsoDate, parseIsoDate, todayInTimeZone } from '../common/civil-date';
import { APP_CONFIG, AppConfig } from '../config';
import { PrismaService } from '../prisma/prisma.service';
import {
  AnamnesisFieldErrors, AnamnesisInput, AnamnesisValues, normalizeAnamnesis, TEXT_FIELDS, YES_NO_FIELDS,
} from './anamnesis-normalize';
import {
  AnamnesisListItem, AnamnesisRulesView, AnamnesisView, toAnamnesisListItem, toAnamnesisView, toRulesView,
} from './anamnesis.view';
import type { UpdateAnamnesisDto } from './dto/anamnesis.dto';

export interface AnamnesisCalculationResult {
  calculation: AnamnesisCalculation | null;
  errors: AnamnesisFieldErrors;
}

const NOT_FOUND = 'Anamnese não encontrada.';
const PATIENT_NOT_FOUND = 'Paciente não encontrado.';
const CONFLICT_MESSAGE = 'Esta anamnese foi alterada por outra pessoa. Recarregue para ver a versão atual.';
const FINALIZED_MESSAGE = 'Esta anamnese já foi finalizada e não pode ser alterada.';

/** Campos comparados na auditoria de rascunho e finalização. */
const AUDIT_FIELDS = [
  'status', 'anamnesisDate', 'rulesVersion', 'sex', 'ageYears', 'restingHeartRate', 'heartRateMethod', 'maxHeartRate',
  'reserveHeartRate', 'systolicPressure', 'diastolicPressure', 'bloodPressureStatus', 'bloodPressureClassification',
  'urineScale', 'urineClassification', 'waterWeightKg', 'waterMlPerKg', 'idealWaterLiters', ...YES_NO_FIELDS, ...TEXT_FIELDS,
] as const satisfies readonly (keyof Anamnesis & string)[];

type AnamnesisData = Omit<Prisma.AnamnesisUncheckedCreateInput, 'patientId' | 'createdById' | 'updatedById'>;

@Injectable()
export class AnamnesesService {
  constructor(
    private readonly prisma: PrismaService,
    private readonly audit: AuditService,
    @Inject(APP_CONFIG) private readonly config: AppConfig,
  ) {}

  rules(): AnamnesisRulesView {
    return toRulesView(CURRENT_ANAMNESE_RULES);
  }

  async list(patientId: string): Promise<AnamnesisListItem[]> {
    await this.patientOrThrow(this.prisma, patientId);
    const rows = await this.prisma.anamnesis.findMany({
      where: { patientId },
      orderBy: [{ anamnesisDate: 'desc' }, { createdAt: 'desc' }, { id: 'asc' }],
    });
    return rows.map(toAnamnesisListItem);
  }

  /** Prévia dos cálculos, sem gravar: calcula o que os campos válidos permitem e devolve os erros dos demais. */
  async calculate(patientId: string, input: AnamnesisInput): Promise<AnamnesisCalculationResult> {
    const patient = await this.patientOrThrow(this.prisma, patientId);
    const { values, errors } = this.normalize(input, patient);
    return { calculation: values.ageYears === null ? null : this.calculation(values, values.ageYears, patient.sex), errors };
  }

  async get(id: string): Promise<AnamnesisView> {
    const anamnesis = await this.prisma.anamnesis.findUnique({ where: { id } });
    if (!anamnesis) throw new NotFoundException(NOT_FOUND);
    return toAnamnesisView(anamnesis);
  }

  async create(patientId: string, body: AnamnesisInput, actorId: string): Promise<AnamnesisView> {
    const created = await this.prisma.$transaction(async (tx) => {
      const patient = await this.patientOrThrow(tx, patientId);
      const anamnesis = await tx.anamnesis.create({
        data: { ...this.dataOrThrow(body, patient), patientId, createdById: actorId, updatedById: actorId },
      });
      await this.audit.record(
        { actorId, action: 'anamnesis.create', entityType: 'anamnesis', entityId: anamnesis.id, data: { patientId } },
        tx,
      );
      return anamnesis;
    });
    return toAnamnesisView(created);
  }

  update(id: string, body: UpdateAnamnesisDto, actorId: string): Promise<AnamnesisView> {
    return this.save(id, body, actorId, 'update');
  }

  /**
   * Grava o rascunho lido na versão `body.version`, ou grava e finaliza. Idade, sexo e cálculos são refeitos com o
   * cadastro atual e a versão corrente das regras. A finalizada nunca muda: `updateMany` só casa com RASCUNHO na versão lida.
   */
  private async save(id: string, body: UpdateAnamnesisDto, actorId: string, mode: 'update' | 'finalize'): Promise<AnamnesisView> {
    return this.prisma.$transaction(async (tx) => {
      const before = await tx.anamnesis.findUnique({ where: { id } });
      if (!before) throw new NotFoundException(NOT_FOUND);
      if (before.status === 'FINALIZADA') throw this.finalized(before);
      if (before.version !== body.version) throw this.conflict(before);
      const patient = await this.patientOrThrow(tx, before.patientId);
      const data = this.dataOrThrow(body, patient);
      const finalizing = mode === 'finalize' ? { status: 'FINALIZADA' as const, finalizedAt: new Date(), finalizedById: actorId } : {};

      const result = await tx.anamnesis.updateMany({
        where: { id, version: body.version, status: 'RASCUNHO' },
        data: { ...data, ...finalizing, updatedById: actorId, version: { increment: 1 } },
      });
      if (result.count === 0) {
        const current = await tx.anamnesis.findUniqueOrThrow({ where: { id } });
        throw current.status === 'FINALIZADA' ? this.finalized(current) : this.conflict(current);
      }
      const after = await tx.anamnesis.findUniqueOrThrow({ where: { id } });
      const changes = diffFields(before, after, AUDIT_FIELDS);
      await this.audit.record(
        { actorId, action: `anamnesis.${mode}`, entityType: 'anamnesis', entityId: id, data: { patientId: before.patientId, changes } },
        tx,
      );
      return toAnamnesisView(after);
    });
  }

  private async patientOrThrow(db: Prisma.TransactionClient, id: string): Promise<Patient> {
    const patient = await db.patient.findUnique({ where: { id } });
    if (!patient) throw new NotFoundException(PATIENT_NOT_FOUND);
    return patient;
  }

  private normalize(input: AnamnesisInput, patient: Patient) {
    return normalizeAnamnesis(
      input,
      { sex: patient.sex, birthDate: formatIsoDate(patient.birthDate) },
      CURRENT_ANAMNESE_RULES,
      todayInTimeZone(this.config.clinicTimeZone),
    );
  }

  private calculation(values: AnamnesisValues, ageYears: number, sex: Sex): AnamnesisCalculation {
    return calculateAnamnesis(CURRENT_ANAMNESE_RULES, {
      sex,
      ageYears,
      restingHeartRate: values.restingHeartRate,
      heartRateMethod: values.heartRateMethod,
      systolicPressure: values.systolicPressure,
      diastolicPressure: values.diastolicPressure,
      urineScale: values.urineScale,
      waterWeightKg: values.waterWeightKg,
      waterMlPerKg: values.waterMlPerKg,
    });
  }

  private dataOrThrow(body: AnamnesisInput, patient: Patient): AnamnesisData {
    const { values, errors } = this.normalize(body, patient);
    if (Object.keys(errors).length > 0 || values.anamnesisDate === null || values.ageYears === null) {
      throw new BadRequestException({ message: 'Dados inválidos.', errors });
    }
    const calc = this.calculation(values, values.ageYears, patient.sex);
    return {
      anamnesisDate: parseIsoDate(values.anamnesisDate) as Date,
      rulesVersion: calc.rulesVersion,
      sex: patient.sex,
      ageYears: calc.ageYears,
      restingHeartRate: values.restingHeartRate,
      heartRateMethod: values.heartRateMethod,
      maxHeartRate: calc.maxHeartRate,
      reserveHeartRate: calc.reserveHeartRate,
      systolicPressure: values.systolicPressure,
      diastolicPressure: values.diastolicPressure,
      bloodPressureStatus: calc.bloodPressureStatus,
      bloodPressureClassification: calc.bloodPressureClassification,
      urineScale: values.urineScale,
      urineClassification: calc.urineClassification,
      waterWeightKg: values.waterWeightKg,
      waterMlPerKg: values.waterMlPerKg,
      idealWaterLiters: calc.idealWaterLiters,
      ...values.answers,
      ...values.texts,
    };
  }

  private conflict(current: Anamnesis): ConflictException {
    return new ConflictException({ message: CONFLICT_MESSAGE, current: toAnamnesisView(current) });
  }

  private finalized(current: Anamnesis): ConflictException {
    return new ConflictException({ message: FINALIZED_MESSAGE, current: toAnamnesisView(current) });
  }
}
```

- [ ] **Step 6: Controller, módulo e registro**

`apps/api/src/anamneses/anamneses.controller.ts`:

```ts
import { Body, Controller, Get, Param, ParseUUIDPipe, Post, Put, Query } from '@nestjs/common';
import type { AuthContext } from '../auth/auth-context';
import { CurrentAuth } from '../auth/current-auth.decorator';
import { RequireAnyPermission, RequirePermissions } from '../auth/require-permissions.decorator';
import type { AnamnesisListItem, AnamnesisRulesView, AnamnesisView } from './anamnesis.view';
import { AnamnesesService, AnamnesisCalculationResult } from './anamneses.service';
import { AnamnesisBodyDto, AnamnesisCalculationQueryDto, UpdateAnamnesisDto } from './dto/anamnesis.dto';

/** Rascunho: basta uma das duas. Toda rota com dados do paciente exige também patients.read (inclusive o 409). */
const DRAFT_OR_FINALIZE = ['anamneses.draft', 'anamneses.finalize'] as const;

@Controller()
export class AnamnesesController {
  constructor(private readonly anamneses: AnamnesesService) {}

  @Get('anamneses/rules')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  rules(): AnamnesisRulesView {
    return this.anamneses.rules();
  }

  @Get('patients/:patientId/anamneses')
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  list(@Param('patientId', new ParseUUIDPipe()) patientId: string): Promise<AnamnesisListItem[]> {
    return this.anamneses.list(patientId);
  }

  @Get('patients/:patientId/anamneses/calculation')
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  calculate(
    @Param('patientId', new ParseUUIDPipe()) patientId: string,
    @Query() query: AnamnesisCalculationQueryDto,
  ): Promise<AnamnesisCalculationResult> {
    return this.anamneses.calculate(patientId, query);
  }

  @Post('patients/:patientId/anamneses')
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  create(
    @Param('patientId', new ParseUUIDPipe()) patientId: string,
    @Body() body: AnamnesisBodyDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<AnamnesisView> {
    return this.anamneses.create(patientId, body, auth.user.id);
  }

  @Get('anamneses/:id')
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  get(@Param('id', new ParseUUIDPipe()) id: string): Promise<AnamnesisView> {
    return this.anamneses.get(id);
  }

  @Put('anamneses/:id')
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  update(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Body() body: UpdateAnamnesisDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<AnamnesisView> {
    return this.anamneses.update(id, body, auth.user.id);
  }
}
```

A rota `anamneses/rules` fica antes de `anamneses/:id`. `apps/api/src/anamneses/anamneses.module.ts` segue o padrão de `PatientsModule`: `@Module({ controllers: [AnamnesesController], providers: [AnamnesesService] })`. Acrescente `AnamnesesModule` ao fim dos imports de `apps/api/src/app.module.ts`.

- [ ] **Step 7: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS. `anamneses.e2e-spec` passa nos 11 testes, e as suítes anteriores, inclusive `permissions.e2e-spec` e `patients.e2e-spec`, seguem verdes depois da mudança na guarda. `typecheck` sai sem erros.

- [ ] **Step 8: Commit**

```powershell
git add -A
git commit -m "feat(api): anamneses com rascunho, lista por paciente, prévia dos cálculos e regras correntes"
```

---

### Task 6: Finalização, congelamento e concorrência

**Files:**
- Modify: `apps/api/src/anamneses/anamneses.service.ts` (método público `finalize`), `apps/api/src/anamneses/anamneses.controller.ts` (rota)
- Create: `apps/api/test/helpers/run-paused.ts`
- Test: `apps/api/test/anamneses-finalize.e2e-spec.ts`

**Interfaces:**
- Consumes: `AnamnesesService.save` (Task 5), `AuditService`, `PrismaService`.
- Produces:
  - `AnamnesesService.finalize(id: string, body: UpdateAnamnesisDto, actorId: string): Promise<AnamnesisView>`;
  - a rota `POST /api/anamneses/:id/finalize`, com o mesmo corpo do PUT (incluindo `version`). Ela responde 200 com `AnamnesisView` e exige `patients.read` e `anamneses.finalize`;
  - o helper de teste `runPaused<T>(app: INestApplication, pauseOn: (input: AuditInput) => boolean, first: () => PromiseLike<T>, second: () => PromiseLike<T>): Promise<[T, T]>`.

- [ ] **Step 1: Extrair o roteiro de concorrência para os testes da anamnese**

`apps/api/test/helpers/run-paused.ts` recebe o `runPaused` de `test/patients.e2e-spec.ts` (linhas 169–232), com três diferenças:

- é exportado;
- recebe `app` como primeiro parâmetro;
- obtém `AuditService` e `PrismaService` por `app.get(...)`.

`within` e `waitForBlockedBy` vão junto, como funções do módulo, e `waitForBlockedBy` recebe o `prisma`. Os testes existentes não são alterados.

- [ ] **Step 2: Escrever os testes**

`apps/api/test/anamneses-finalize.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';
import { runPaused } from './helpers/run-paused';

const homem = { fullName: 'Paciente Sintético Um', sex: 'MASCULINO', birthDate: '1980-05-10' };
const completa = {
  anamnesisDate: '2026-05-09', restingHeartRate: 70, heartRateMethod: 'tanaka-2001', systolicPressure: 150, diastolicPressure: 80,
  urineScale: 4, waterWeightKg: 70, waterMlPerKg: 35, smoking: true, alcohol: false,
};
const FINALIZED = 'Esta anamnese já foi finalizada e não pode ser alterada.';
const CONFLICT = 'Esta anamnese foi alterada por outra pessoa. Recarregue para ver a versão atual.';

describe('Anamneses — finalização (HTTP)', () => {
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

  async function patientWithDraft(agent: Agent, body: Record<string, unknown> = completa) {
    const patient = await agent.post('/api/patients').send(homem);
    expect(patient.status).toBe(201);
    const anamnesis = await agent.post(`/api/patients/${patient.body.id}/anamneses`).send(body);
    expect(anamnesis.status).toBe(201);
    return { patientId: patient.body.id as string, id: anamnesis.body.id as string };
  }

  it('assistente sem anamneses.finalize recebe 403 e nada muda', async () => {
    const assistant = await loginAs(app, 'ASSISTENTE');
    const { id } = await patientWithDraft(assistant.agent);

    const res = await assistant.agent.post(`/api/anamneses/${id}/finalize`).send({ ...completa, version: 1 });

    expect(res.status).toBe(403);
    expect(await prisma.anamnesis.findUniqueOrThrow({ where: { id } })).toMatchObject({ status: 'RASCUNHO', version: 1, finalizedAt: null });
  });

  it('profissional finaliza: grava o que enviou, congela e audita', async () => {
    const { agent, user } = await loginAs(app, 'PROFISSIONAL');
    const { patientId, id } = await patientWithDraft(agent);

    const res = await agent.post(`/api/anamneses/${id}/finalize`).send({ ...completa, smoking: false, version: 1 });

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({ status: 'FINALIZADA', version: 2, smoking: false, maxHeartRate: 177, rulesVersion: 'anamnese-v1' });
    expect(res.body.finalizedAt).toEqual(expect.any(String));
    expect(await prisma.anamnesis.findUniqueOrThrow({ where: { id } })).toMatchObject({ finalizedById: user.id });
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'anamnesis.finalize' } });
    expect(event).toMatchObject({ actorId: user.id, entityType: 'anamnesis', entityId: id });
    expect(event.data).toEqual({
      patientId,
      changes: { status: { from: 'RASCUNHO', to: 'FINALIZADA' }, smoking: { from: true, to: false } },
    });
  });

  it('finalizada: novo PUT e nova finalização recebem 409 com a anamnese atual e não gravam nada', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { id } = await patientWithDraft(agent);
    expect((await agent.post(`/api/anamneses/${id}/finalize`).send({ ...completa, version: 1 })).status).toBe(200);

    const responses = [
      await agent.put(`/api/anamneses/${id}`).send({ ...completa, smoking: false, version: 2 }),
      await agent.put(`/api/anamneses/${id}`).send({ ...completa, smoking: false, version: 1 }),
      await agent.post(`/api/anamneses/${id}/finalize`).send({ ...completa, version: 2 }),
    ];

    for (const res of responses) {
      expect(res.status).toBe(409);
      expect(res.body.message).toBe(FINALIZED);
      expect(res.body.current).toMatchObject({ status: 'FINALIZADA', version: 2, smoking: true });
    }
    expect(await prisma.auditEvent.count({ where: { action: 'anamnesis.update' } })).toBe(0);
    expect(await prisma.auditEvent.count({ where: { action: 'anamnesis.finalize' } })).toBe(1);
  });

  it('finalizar também valida: com erro por campo responde 400 e continua rascunho', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { id } = await patientWithDraft(agent);

    const res = await agent.post(`/api/anamneses/${id}/finalize`).send({ ...completa, urineScale: 9, version: 1 });

    expect(res.status).toBe(400);
    expect(res.body.errors).toEqual({ urineScale: 'A escala de urina deve ser um número inteiro de 1 a 8.' });
    expect(await prisma.anamnesis.findUniqueOrThrow({ where: { id } })).toMatchObject({ status: 'RASCUNHO', version: 1 });
  });

  it('idade e sexo são recalculados a cada rascunho e congelados ao finalizar', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { patientId, id } = await patientWithDraft(agent, { anamnesisDate: '2026-05-09' });
    expect((await agent.put(`/api/patients/${patientId}`).send({ ...homem, birthDate: '1990-05-10', version: 1 })).status).toBe(200);

    const redraft = await agent.put(`/api/anamneses/${id}`).send({ anamnesisDate: '2026-05-09', version: 1 });
    const done = await agent.post(`/api/anamneses/${id}/finalize`).send({ anamnesisDate: '2026-05-09', version: 2 });
    expect(
      (await agent.put(`/api/patients/${patientId}`).send({ ...homem, sex: 'FEMININO', birthDate: '2000-01-01', version: 2 })).status,
    ).toBe(200);

    expect(redraft.body).toMatchObject({ ageYears: 35, version: 2 });
    expect(done.status).toBe(200);
    expect((await agent.get(`/api/anamneses/${id}`)).body).toMatchObject({ ageYears: 35, sex: 'MASCULINO', status: 'FINALIZADA', version: 3 });
  });

  it('delegação: assistente com anamneses.finalize concedida finaliza', async () => {
    const assistant = await loginAs(app, 'ASSISTENTE');
    const { id } = await patientWithDraft(assistant.agent);
    const admin = await loginAs(app, 'ADMIN');
    await admin.agent.put('/api/permissions/assistant').send({ changes: [{ permission: 'anamneses.finalize', granted: true }] });

    const res = await assistant.agent.post(`/api/anamneses/${id}/finalize`).send({ ...completa, version: 1 });

    expect(res.status).toBe(200);
    expect(res.body.status).toBe('FINALIZADA');
  });

  it('dois rascunhos da versão 1 ao mesmo tempo: um grava e o outro recebe 409 com a versão atual', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const { id } = await patientWithDraft(agent);

    const [first, second] = await runPaused(
      app,
      (input) => input.action === 'anamnesis.update',
      () => agent.put(`/api/anamneses/${id}`).send({ ...completa, smoking: false, version: 1 }),
      () => agent.put(`/api/anamneses/${id}`).send({ ...completa, alcohol: true, version: 1 }),
    );

    expect([first.status, second.status]).toEqual([200, 409]);
    expect(second.body.message).toBe(CONFLICT);
    expect(second.body.current).toMatchObject({ smoking: false, alcohol: false, version: 2 });
    expect(await prisma.anamnesis.findUniqueOrThrow({ where: { id } })).toMatchObject({ smoking: false, alcohol: false, version: 2 });
  });

  it('rascunho da versão 1 salvo enquanto outra pessoa finaliza: 409 de finalizada, sem gravar', async () => {
    const professional = await loginAs(app, 'PROFISSIONAL');
    const { id } = await patientWithDraft(professional.agent);
    const assistant = await loginAs(app, 'ASSISTENTE');

    const [finalized, late] = await runPaused(
      app,
      (input) => input.action === 'anamnesis.finalize',
      () => professional.agent.post(`/api/anamneses/${id}/finalize`).send({ ...completa, version: 1 }),
      () => assistant.agent.put(`/api/anamneses/${id}`).send({ ...completa, smoking: false, version: 1 }),
    );

    // O PUT tardio leu a versão 1 em RASCUNHO, passou pelas checagens e ficou preso no UPDATE até a finalização gravar.
    expect([finalized.status, late.status]).toEqual([200, 409]);
    expect(late.body.message).toBe(FINALIZED);
    expect(late.body.current).toMatchObject({ status: 'FINALIZADA', version: 2, smoking: true });
    expect(await prisma.auditEvent.count({ where: { action: 'anamnesis.update' } })).toBe(0);
  });
});
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `npm test -w @psm/api -- test/anamneses-finalize.e2e-spec.ts`
Expected: FAIL. `POST /api/anamneses/:id/finalize` responde 404 (`Cannot POST`).

- [ ] **Step 4: Implementar a finalização**

Em `anamneses.service.ts`, acrescente ao lado de `update`:

```ts
  finalize(id: string, body: UpdateAnamnesisDto, actorId: string): Promise<AnamnesisView> {
    return this.save(id, body, actorId, 'finalize');
  }
```

Em `anamneses.controller.ts`, acrescente a rota abaixo e importe `HttpCode`:

```ts
  @Post('anamneses/:id/finalize')
  @HttpCode(200)
  @RequirePermissions('patients.read', 'anamneses.finalize')
  finalize(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Body() body: UpdateAnamnesisDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<AnamnesisView> {
    return this.anamneses.finalize(id, body, auth.user.id);
  }
```

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS. A API fica com 30 suítes: as 19 anteriores e 11 novas. `anamneses-finalize.e2e-spec` passa nos 8 testes, e `typecheck` sai sem erros.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat(api): finalização da anamnese, congelamento e conflitos com rascunho tardio"
```

- [ ] **Step 7: PONTO DE CONTROLE DA PESSOA — API da anamnese**

Pare e apresente à pessoa:

1. A saída de `npm test -w @psm/api` (30 suítes verdes).
2. Os nomes dos testes de `anamneses.e2e-spec.ts` e `anamneses-finalize.e2e-spec.ts` (`npx jest test/anamneses --verbose` em `apps/api`). Eles descrevem:
   - quem cria, edita, lê e finaliza;
   - o 403 sem dados;
   - o 409 de rascunho tardio;
   - a idade congelada.
3. A tabela de rotas e permissões da Task 5 ("Produces") e da Task 6.

Só siga para a Task 7 com o "ok" da pessoa.

---

### Task 7: Web — tipos, anamneses na página do paciente e 403 em cascata

**Files:**
- Create: `apps/web/src/anamneses/types.ts`, `apps/web/src/anamneses/PatientAnamnesesSection.tsx`, `apps/web/src/test/anamnesis-fixtures.ts`
- Modify: `apps/web/src/lib/query-client.ts:122-137`, `apps/web/src/patients/PatientFormPage.tsx:5,278`
- Test: `apps/web/src/anamneses/anamneses-section.test.tsx`

**Interfaces:**
- Consumes: as rotas da API das Tasks 5 e 6; `api`, `errorMessage`, `formatDateBr`, `useCurrentUser`, `canAny`, `mockApi` e `renderApp`.
- Produces:
  - em `types.ts`: `ANAMNESIS_PERMISSIONS`; `YES_NO_FIELDS` e `TEXT_FIELDS` (mesma ordem da API), com os tipos `YesNoField` e `TextField`; os tipos `AnamnesisStatus`, `BloodPressureStatus`, `AnamnesisResults`, `Anamnesis`, `AnamnesisListItem`, `AnamnesisCalculation`, `AnamnesisCalculationResult` e `AnamnesisRules`; `STATUS_LABELS`;
  - `PatientAnamnesesSection({ patientId }: { patientId: string })`;
  - nas fixtures: `patientMan`, `rulesV1`, `calcEmpty`, `calcFull`, `draftAnamnesis`, `finalizedAnamnesis`, `anamnesisList` e `mockAnamnesisApi(routes?)`;
  - as chaves de consulta `['anamneses', patientId]`, `['anamnesis', id]`, `['anamnesis-calculation', patientId, params]` e `['anamnesis-rules']`.

- [ ] **Step 1: Tipos e dados sintéticos**

`apps/web/src/anamneses/types.ts`:

```ts
import type { Sex } from '../patients/types';

/** Basta uma delas para ver, criar e editar rascunhos (a API também exige patients.read). */
export const ANAMNESIS_PERMISSIONS = ['anamneses.draft', 'anamneses.finalize'] as const;

/** Mesma ordem de YES_NO_FIELDS da API (apps/api/src/anamneses/anamnesis-normalize.ts). */
export const YES_NO_FIELDS = [
  'alcohol', 'smoking', 'goodSleep', 'physicalActivity', 'adequateWater', 'supplements', 'anemia', 'diabetes',
  'cardiacConditions', 'bloodPressureConditions', 'circulatoryConditions', 'allergies', 'treatment', 'medications',
  'healthDevice', 'anxiety', 'depression', 'eatingDisorders', 'bipolarDisorder', 'ocd', 'postTraumaticStress',
  'otherMentalConditions',
] as const;

export const TEXT_FIELDS = [
  'sleepDetails', 'activityDetails', 'waterDetails', 'supplementDetails', 'eatingHabits', 'otherConditions',
  'treatmentDetails', 'medicationDetails', 'healthDeviceDetails', 'mentalConditionsDetails', 'observations',
] as const;

export type YesNoField = (typeof YES_NO_FIELDS)[number];
export type TextField = (typeof TEXT_FIELDS)[number];
export type AnamnesisStatus = 'RASCUNHO' | 'FINALIZADA';
export type BloodPressureStatus = 'CLASSIFICADA' | 'DADOS_INCOMPLETOS' | 'FORA_DA_FAIXA_ETARIA';

/** Valores calculados pela API (na prévia ou gravados na anamnese). */
export interface AnamnesisResults {
  ageYears: number;
  maxHeartRate: number | null;
  reserveHeartRate: number | null;
  bloodPressureStatus: BloodPressureStatus;
  bloodPressureClassification: string | null;
  urineClassification: string | null;
  idealWaterLiters: number | null;
}

export type Anamnesis = AnamnesisResults &
  Record<YesNoField, boolean | null> &
  Record<TextField, string | null> & {
    id: string;
    patientId: string;
    status: AnamnesisStatus;
    anamnesisDate: string;
    rulesVersion: string;
    sex: Sex;
    restingHeartRate: number | null;
    heartRateMethod: string | null;
    heartRateMethodLabel: string | null;
    systolicPressure: number | null;
    diastolicPressure: number | null;
    urineScale: number | null;
    waterWeightKg: number | null;
    waterMlPerKg: number | null;
    version: number;
    createdAt: string;
    updatedAt: string;
    finalizedAt: string | null;
  };

export type AnamnesisListItem = Pick<Anamnesis, 'id' | 'anamnesisDate' | 'status' | 'finalizedAt' | 'updatedAt'>;

export interface AnamnesisCalculation extends AnamnesisResults {
  rulesVersion: string;
}

export interface AnamnesisCalculationResult {
  calculation: AnamnesisCalculation | null;
  errors: Record<string, string>;
}

export interface AnamnesisRules {
  version: string;
  status: 'PROVISORIA' | 'VALIDADA';
  heartRateMethods: { id: string; label: string; sexes: Sex[] }[];
  bloodPressure: { source: string; minAgeYears: number };
  urineScale: { scale: number; label: string }[];
  water: { defaultMlPerKg: number };
}

export const STATUS_LABELS: Record<AnamnesisStatus, string> = { RASCUNHO: 'Rascunho', FINALIZADA: 'Finalizada' };
```

`apps/web/src/test/anamnesis-fixtures.ts`: dados sintéticos e a API mínima do formulário.

```ts
import { TEXT_FIELDS, YES_NO_FIELDS } from '../anamneses/types';
import type { Anamnesis, AnamnesisCalculation, AnamnesisListItem, AnamnesisRules, TextField, YesNoField } from '../anamneses/types';
import type { Patient } from '../patients/types';
import { professionalMe } from './fixtures';
import { mockApi } from './mock-api';

export const patientMan: Patient = {
  id: 'p1', legacyId: null, fullName: 'Paciente Sintético Um', sex: 'MASCULINO', birthDate: '1980-05-10',
  profession: null, phone: null, mobile: null, email: null, notes: null, postalCode: null, street: null,
  streetNumber: null, addressComplement: null, district: null, city: null, state: null, rg: null,
  cpf: null, otherDocument: null, version: 1,
  createdAt: '2026-05-01T12:00:00.000Z', updatedAt: '2026-05-01T12:00:00.000Z',
};

const BOTH: AnamnesisRules['heartRateMethods'][number]['sexes'] = ['FEMININO', 'MASCULINO'];

/** Espelha GET /api/anamneses/rules da anamnese-v1. */
export const rulesV1: AnamnesisRules = {
  version: 'anamnese-v1',
  status: 'PROVISORIA',
  heartRateMethods: [
    { id: 'karvonen-1957', label: 'Karvonen et al., 1957 — clássica: 220 − idade', sexes: BOTH },
    { id: 'jones-1975', label: 'Jones et al., 1975: 220 − 0,65 × idade', sexes: BOTH },
    { id: 'tanaka-2001', label: 'Tanaka et al., 2001: 208 − 0,7 × idade', sexes: BOTH },
    { id: 'calvert-1977-homens', label: 'Calvert et al., 1977: 201 − 0,6 × idade', sexes: ['MASCULINO'] },
    { id: 'sheffield-1965-sedentarios', label: 'Sheffield et al., 1965 (sedentários): 205 − 0,41 × idade', sexes: ['MASCULINO'] },
    { id: 'sheffield-1965-ativos', label: 'Sheffield et al., 1965 (ativos): 198 − 0,41 × idade', sexes: ['MASCULINO'] },
    { id: 'branco-2004', label: 'Branco et al., 2004 (corredores de fundo): 205 − 0,7 × idade', sexes: ['MASCULINO'] },
    { id: 'calvert-1977-mulheres', label: 'Calvert et al., 1977: 192 − 0,7 × idade', sexes: ['FEMININO'] },
    { id: 'hossack-1981', label: 'Hossack et al., 1981: 206 − 0,597 × idade', sexes: ['FEMININO'] },
  ],
  bloodPressure: {
    source:
      'Diretriz Brasileira de Hipertensão Arterial – 2025 (SBC, SBH, SBN). Arq Bras Cardiol. 2025;122(9):e20250624. Quadro 3.2, p. 36 (consultório, a partir de 18 anos).',
    minAgeYears: 18,
  },
  urineScale: [
    { scale: 1, label: 'Bem Hidratado' },
    { scale: 2, label: 'Bem Hidratado' },
    { scale: 3, label: 'Levemente Desidratado' },
    { scale: 4, label: 'Levemente Desidratado' },
    { scale: 5, label: 'Moderadamente Desidratado' },
    { scale: 6, label: 'Moderadamente Desidratado' },
    { scale: 7, label: 'Severamente Desidratado' },
    { scale: 8, label: 'Severamente Desidratado' },
  ],
  water: { defaultMlPerKg: 35 },
};

export const calcEmpty: AnamnesisCalculation = {
  rulesVersion: 'anamnese-v1', ageYears: 46, maxHeartRate: null, reserveHeartRate: null, bloodPressureStatus: 'DADOS_INCOMPLETOS',
  bloodPressureClassification: null, urineClassification: null, idealWaterLiters: null,
};

export const calcFull: AnamnesisCalculation = {
  rulesVersion: 'anamnese-v1', ageYears: 45, maxHeartRate: 177, reserveHeartRate: 107, bloodPressureStatus: 'CLASSIFICADA',
  bloodPressureClassification: 'HA Sistólica Isolada – Estágio 1', urineClassification: null, idealWaterLiters: 2.45,
};

const noAnswers = Object.fromEntries(YES_NO_FIELDS.map((field) => [field, null])) as Record<YesNoField, boolean | null>;
const noTexts = Object.fromEntries(TEXT_FIELDS.map((field) => [field, null])) as Record<TextField, string | null>;

export const draftAnamnesis: Anamnesis = {
  ...noAnswers,
  ...noTexts,
  id: 'a1', patientId: 'p1', status: 'RASCUNHO', anamnesisDate: '2026-05-09', rulesVersion: 'anamnese-v1', sex: 'MASCULINO',
  ageYears: 45, restingHeartRate: 70, heartRateMethod: 'tanaka-2001', heartRateMethodLabel: 'Tanaka et al., 2001: 208 − 0,7 × idade',
  maxHeartRate: 177, reserveHeartRate: 107, systolicPressure: 150, diastolicPressure: 80, bloodPressureStatus: 'CLASSIFICADA',
  bloodPressureClassification: 'HA Sistólica Isolada – Estágio 1', urineScale: 4, urineClassification: 'Levemente Desidratado',
  waterWeightKg: 70, waterMlPerKg: 35, idealWaterLiters: 2.45, smoking: true, sleepDetails: 'Dorme 6 h',
  version: 1, createdAt: '2026-05-09T13:00:00.000Z', updatedAt: '2026-05-09T13:00:00.000Z', finalizedAt: null,
};

export const finalizedAnamnesis: Anamnesis = {
  ...draftAnamnesis, status: 'FINALIZADA', version: 2, updatedAt: '2026-05-09T15:30:00.000Z', finalizedAt: '2026-05-09T15:30:00.000Z',
};

export const anamnesisList: AnamnesisListItem[] = [
  { id: 'a2', anamnesisDate: '2026-03-01', status: 'FINALIZADA', finalizedAt: '2026-03-01T15:00:00.000Z', updatedAt: '2026-03-01T15:00:00.000Z' },
  { id: 'a1', anamnesisDate: '2026-02-15', status: 'RASCUNHO', finalizedAt: null, updatedAt: '2026-02-15T13:00:00.000Z' },
];

/** API mínima do formulário (profissional, paciente p1, regras, prévia vazia); `routes` sobrepõe. */
export function mockAnamnesisApi(routes: Parameters<typeof mockApi>[0] = {}) {
  return mockApi({
    'GET /auth/me': { body: professionalMe },
    'GET /patients/p1': { body: patientMan },
    'GET /anamneses/rules': { body: rulesV1 },
    'GET /patients/p1/anamneses': { body: [] },
    'GET /patients/p1/anamneses/calculation': { body: { calculation: calcEmpty, errors: {} } },
    ...routes,
  });
}
```

- [ ] **Step 2: Escrever os testes**

`apps/web/src/anamneses/anamneses-section.test.tsx`:

```tsx
import { act, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { Me } from '../auth/me';
import { anamnesisList, patientMan } from '../test/anamnesis-fixtures';
import { assistantMe, professionalMe } from '../test/fixtures';
import { mockApi, type MockResponse } from '../test/mock-api';
import { renderApp } from '../test/render';

const FORBIDDEN: MockResponse = { status: 403, body: { statusCode: 403, message: 'Você não tem permissão para esta ação.' } };
const draftAssistantMe: Me = { ...assistantMe, permissions: ['patients.read', 'patients.write', 'anamneses.draft'] };

describe('Anamneses na página do paciente', () => {
  it('lista as anamneses na ordem da API, com situação, link e Nova anamnese', async () => {
    mockApi({ 'GET /auth/me': { body: professionalMe }, 'GET /patients/p1': { body: patientMan }, 'GET /patients/p1/anamneses': { body: anamnesisList } });
    renderApp('/pacientes/p1');

    const section = await screen.findByRole('region', { name: 'Anamneses' });
    expect(await within(section).findByRole('link', { name: '01/03/2026' })).toHaveAttribute('href', '/pacientes/p1/anamneses/a2');
    expect(within(section).getAllByRole('row').slice(1).map((row) => row.textContent)).toEqual(['01/03/2026Finalizada', '15/02/2026Rascunho']);
    expect(within(section).getByRole('link', { name: 'Nova anamnese' })).toHaveAttribute('href', '/pacientes/p1/anamneses/nova');
  });

  it('assistente com anamneses.draft vê a seção', async () => {
    mockApi({ 'GET /auth/me': { body: draftAssistantMe }, 'GET /patients/p1': { body: patientMan }, 'GET /patients/p1/anamneses': { body: [] } });
    renderApp('/pacientes/p1');

    const section = await screen.findByRole('region', { name: 'Anamneses' });
    expect(await within(section).findByText('Nenhuma anamnese registrada.')).toBeInTheDocument();
  });

  it('sem anamneses.draft nem anamneses.finalize, a seção não aparece e nada é buscado', async () => {
    const { calls } = mockApi({ 'GET /auth/me': { body: assistantMe }, 'GET /patients/p1': { body: patientMan } });
    renderApp('/pacientes/p1');

    expect(await screen.findByDisplayValue('Paciente Sintético Um')).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Anamneses' })).not.toBeInTheDocument();
    expect(calls.filter((c) => c.path.includes('/anamneses'))).toEqual([]);
  });

  it('403 nas anamneses tira as anamneses do cache e mantém o cadastro do paciente', async () => {
    let forbidden = false;
    mockApi({
      'GET /auth/me': { body: professionalMe },
      'GET /patients/p1': { body: patientMan },
      'GET /patients/p1/anamneses': () => (forbidden ? FORBIDDEN : { body: anamnesisList }),
    });
    const { queryClient } = renderApp('/pacientes/p1');
    expect(await screen.findByRole('link', { name: '01/03/2026' })).toBeInTheDocument();

    forbidden = true;
    await act(() => queryClient.invalidateQueries({ queryKey: ['anamneses'] }));

    expect(await screen.findByText('Você não tem permissão para esta ação.')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: '01/03/2026' })).not.toBeInTheDocument();
    expect(screen.getByDisplayValue('Paciente Sintético Um')).toBeInTheDocument();
    expect(queryClient.getQueryData(['patient', 'p1'])).toEqual(patientMan);
    expect(queryClient.getQueryData(['anamneses', 'p1'])).toBeUndefined();
  });

  it('403 no cadastro do paciente tira do cache também as anamneses', async () => {
    let forbidden = false;
    mockApi({
      'GET /auth/me': { body: professionalMe },
      'GET /patients/p1': () => (forbidden ? FORBIDDEN : { body: patientMan }),
      'GET /patients/p1/anamneses': { body: anamnesisList },
    });
    const { queryClient } = renderApp('/pacientes/p1');
    expect(await screen.findByRole('link', { name: '01/03/2026' })).toBeInTheDocument();

    forbidden = true;
    await act(() => queryClient.invalidateQueries({ queryKey: ['patient'] }));

    expect(await screen.findByText('Você não tem permissão para esta ação.')).toBeInTheDocument();
    expect(queryClient.getQueryData(['anamneses', 'p1'])).toBeUndefined();
    expect(screen.queryByRole('link', { name: '01/03/2026' })).not.toBeInTheDocument();
  });
});
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `npm test -w @psm/web -- src/anamneses`
Expected: FAIL. A região "Anamneses" não existe, e o 403 nas anamneses não limpa o cache.

- [ ] **Step 4: 403 em cascata no query-client**

Em `apps/web/src/lib/query-client.ts`, troque `FORBIDDEN_GROUPS` (linhas 122–123) por:

```ts
/** Raízes das consultas de anamnese. */
const ANAMNESIS_ROOTS = ['anamneses', 'anamnesis', 'anamnesis-calculation'] as const;

/**
 * O que um 403 numa consulta derruba, pela raiz dela. Lista e detalhes de pacientes caem juntos e levam as anamneses (a
 * API só as mostra com patients.read). Um 403 nas anamneses (ex.: anamneses.draft retirada) não derruba o cadastro do
 * paciente, que continua legível. Fora daqui, cada raiz derruba só a si mesma. As regras ('anamnesis-rules') não têm
 * dado de paciente.
 */
const FORBIDDEN_CASCADE: Readonly<Record<string, readonly string[]>> = {
  patients: ['patients', 'patient', ...ANAMNESIS_ROOTS],
  patient: ['patients', 'patient', ...ANAMNESIS_ROOTS],
  anamneses: ANAMNESIS_ROOTS,
  anamnesis: ANAMNESIS_ROOTS,
  'anamnesis-calculation': ANAMNESIS_ROOTS,
};
```

Na linha 137, troque a busca do grupo por `const group = FORBIDDEN_CASCADE[root] ?? [root];`. Ajuste o JSDoc de `handleForbiddenQuery` ("o grupo inteiro dela" passa a ser "o que FORBIDDEN_CASCADE manda derrubar").

- [ ] **Step 5: Seção de anamneses**

`apps/web/src/anamneses/PatientAnamnesesSection.tsx`:

```tsx
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { Alert } from '../components/ui/Alert';
import { buttonClass } from '../components/ui/Button';
import { api } from '../lib/api';
import { errorMessage } from '../lib/error-message';
import { formatDateBr } from '../lib/format';
import { STATUS_LABELS, type AnamnesisListItem } from './types';

/** Anamneses do paciente, da mais recente para a mais antiga (ordem da API). */
export function PatientAnamnesesSection({ patientId }: { patientId: string }) {
  const anamneses = useQuery({
    queryKey: ['anamneses', patientId],
    queryFn: () => api<AnamnesisListItem[]>(`/patients/${patientId}/anamneses`),
  });
  // Com erro (ex.: 403 depois de perder a permissão) os dados anteriores não aparecem.
  const items = anamneses.error ? undefined : anamneses.data;

  return (
    <section aria-labelledby="anamneses-titulo" className="space-y-3 rounded-lg border border-areia bg-white p-6">
      <div className="flex items-center justify-between">
        <h2 id="anamneses-titulo" className="font-display text-xl text-terracota">
          Anamneses
        </h2>
        <Link to={`/pacientes/${patientId}/anamneses/nova`} className={buttonClass('primary')}>
          Nova anamnese
        </Link>
      </div>
      {anamneses.error ? <Alert>{errorMessage(anamneses.error)}</Alert> : null}
      {anamneses.isPending ? <p className="text-sm">Carregando…</p> : null}
      {items?.length === 0 ? <p className="text-sm">Nenhuma anamnese registrada.</p> : null}
      {items && items.length > 0 ? (
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left">
              <th className="py-2 font-semibold">Data</th>
              <th className="py-2 font-semibold">Situação</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.id} className="border-t border-areia">
                <td className="py-2">
                  <Link to={`/pacientes/${patientId}/anamneses/${item.id}`} className="text-terracota underline">
                    {formatDateBr(item.anamnesisDate)}
                  </Link>
                </td>
                <td className="py-2">{STATUS_LABELS[item.status]}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </section>
  );
}
```

Em `apps/web/src/patients/PatientFormPage.tsx`:

- na linha 5, importe também `canAny`: `import { can, canAny } from '../auth/me';`;
- importe `PatientAnamnesesSection` e `ANAMNESIS_PERMISSIONS`;
- logo depois de `</form>` (linha 278), acrescente:

```tsx
      {!isNew && id && canAny(me, ANAMNESIS_PERMISSIONS) ? <PatientAnamnesesSection patientId={id} /> : null}
```

- [ ] **Step 6: Rodar e ver passar**

Run: `npm test -w @psm/web; npm run typecheck -w @psm/web`
Expected: PASS. A web fica com 17 arquivos: os 16 anteriores e `anamneses-section.test.tsx`, com 5 testes. Os testes de 403 de `patients.test.tsx` continuam verdes, e `typecheck` sai sem erros.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat(web): anamneses na página do paciente e 403 de pacientes derrubando as anamneses"
```

---

### Task 8: Web — formulário da anamnese, prévia dos cálculos e rascunho

**Files:**
- Create: `apps/web/src/anamneses/questions.ts`, `apps/web/src/anamneses/YesNoQuestion.tsx`, `apps/web/src/anamneses/use-debounced-value.ts`, `apps/web/src/anamneses/AnamnesisFormPage.tsx`
- Modify: `apps/web/src/lib/format.ts`, `apps/web/src/router.tsx`
- Test: `apps/web/src/anamneses/anamnesis-form.test.tsx`, `apps/web/src/lib/format.test.ts`

**Interfaces:**
- Consumes: Task 7 (tipos, fixtures, chaves de consulta); `endSession` (`lib/query-client.ts`); `Field`, `Input`, `Select`, `Textarea`, `Button`, `Alert`, `PageTitle`; `fieldErrors`, `ApiError`, `errorMessage`.
- Produces:
  - `todayInClinic(now?: Date): string`, no formato AAAA-MM-DD;
  - `formatDecimalBr(value: number, digits: number): string`;
  - o tipo `QuestionItem` e as constantes `QUESTION_GROUPS` e `OBSERVATIONS_LABEL`;
  - `YesNoQuestion({ name, label, value, onChange, clearable })`;
  - `useDebouncedValue<T>(value: T, delayMs: number): T`;
  - `AnamnesisFormRoute`, nas rotas `/pacientes/:patientId/anamneses/nova` e `/pacientes/:patientId/anamneses/:anamnesisId`.

- [ ] **Step 1: Escrever os testes**

Em `apps/web/src/lib/format.test.ts`, acrescente ao `describe` existente (e importe `todayInClinic` e `formatDecimalBr`):

```ts
  it('hoje no fuso da clínica', () => {
    expect(todayInClinic(new Date('2026-10-01T02:30:00Z'))).toBe('2026-09-30');
    expect(todayInClinic(new Date('2026-10-01T03:30:00Z'))).toBe('2026-10-01');
  });

  it('decimal com casas fixas em pt-BR', () => {
    expect(formatDecimalBr(2.45, 2)).toBe('2,45');
    expect(formatDecimalBr(2.1, 2)).toBe('2,10');
    expect(formatDecimalBr(100, 2)).toBe('100,00');
  });
```

`apps/web/src/anamneses/anamnesis-form.test.tsx`:

```tsx
import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { todayInClinic } from '../lib/format';
import { endSession } from '../lib/query-client';
import { calcEmpty, calcFull, draftAnamnesis, mockAnamnesisApi, patientMan } from '../test/anamnesis-fixtures';
import type { MockResponse } from '../test/mock-api';
import { renderApp } from '../test/render';
import { QUESTION_GROUPS } from './questions';
import { TEXT_FIELDS, YES_NO_FIELDS } from './types';

const settle = () => act(() => new Promise((resolve) => setTimeout(resolve, 200)));
const created: MockResponse = { status: 201, body: draftAnamnesis };
const FULL_CALCULATION =
  '/patients/p1/anamneses/calculation?anamnesisDate=2026-05-09&restingHeartRate=70&systolicPressure=150&diastolicPressure=80&waterWeightKg=70&waterMlPerKg=35&heartRateMethod=tanaka-2001';

describe('Formulário da anamnese', () => {
  it('tem as 22 perguntas Sim/Não e os 11 textos, cada campo uma vez, nos 4 grupos do legado', () => {
    const items = QUESTION_GROUPS.flatMap((group) => group.items);
    expect(items.filter((item) => item.kind === 'yesNo').map((item) => item.field).sort()).toEqual([...YES_NO_FIELDS].sort());
    expect([...items.filter((item) => item.kind === 'text').map((item) => item.field), 'observations'].sort()).toEqual([...TEXT_FIELDS].sort());
    expect(QUESTION_GROUPS.map((group) => group.title)).toEqual([
      'Hábitos e Histórico Social',
      'Alimentação e Hidratação',
      'Doenças, Tratamentos e Medicamentos, Dispositivos de Saúde',
      'Distúrbios psíquicos',
    ]);
  });

  it('nova anamnese: nenhuma resposta marcada, data de hoje, 35 mL/kg; o que não foi respondido vai como null', async () => {
    const { calls } = mockAnamnesisApi({ 'POST /patients/p1/anamneses': created });
    const user = userEvent.setup();
    const { router } = renderApp('/pacientes/p1/anamneses/nova');

    expect(await screen.findByRole('heading', { name: 'Nova anamnese' })).toBeInTheDocument();
    const questions = QUESTION_GROUPS.flatMap((group) => group.items).filter((item) => item.kind === 'yesNo');
    for (const question of questions) {
      const radios = within(screen.getByRole('group', { name: question.label })).getAllByRole('radio');
      expect(radios.filter((radio) => (radio as HTMLInputElement).checked)).toEqual([]);
    }
    expect(screen.getByLabelText('Data da anamnese *')).toHaveValue(todayInClinic());
    expect(screen.getByLabelText('Fator (mL/kg)')).toHaveValue(35);

    await user.click(screen.getByRole('button', { name: 'Salvar rascunho' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/pacientes/p1/anamneses/a1'));
    expect(calls.find((c) => c.method === 'POST')?.body).toEqual({
      anamnesisDate: todayInClinic(), heartRateMethod: null, urineScale: null, restingHeartRate: null, systolicPressure: null,
      diastolicPressure: null, waterWeightKg: null, waterMlPerKg: 35,
      ...Object.fromEntries(YES_NO_FIELDS.map((field) => [field, null])),
      ...Object.fromEntries(TEXT_FIELDS.map((field) => [field, null])),
    });
    expect(await screen.findByText('Rascunho salvo.')).toBeInTheDocument();
  });

  it('Sim, Não e Limpar resposta: a pergunta limpa volta a não respondida', async () => {
    const { calls } = mockAnamnesisApi({ 'POST /patients/p1/anamneses': created });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/anamneses/nova');

    await user.click(within(await screen.findByRole('group', { name: 'Fumante?' })).getByRole('radio', { name: 'Sim' }));
    await user.click(within(screen.getByRole('group', { name: 'Ingere Bebida Alcoólica?' })).getByRole('radio', { name: 'Não' }));
    const sleep = screen.getByRole('group', { name: 'Dorme Bem?' });
    await user.click(within(sleep).getByRole('radio', { name: 'Sim' }));
    await user.click(within(sleep).getByRole('button', { name: 'Limpar resposta' }));
    expect(within(sleep).queryByRole('button', { name: 'Limpar resposta' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Salvar rascunho' }));

    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toMatchObject({ smoking: true, alcohol: false, goodSleep: null }));
  });

  it('H-12: a opção 4 envia a escala 4; reabrir com a escala 8 marca a opção 8', async () => {
    const { calls } = mockAnamnesisApi({
      'POST /patients/p1/anamneses': created,
      'GET /anamneses/a8': { body: { ...draftAnamnesis, id: 'a8', urineScale: 8, urineClassification: 'Severamente Desidratado' } },
    });
    const user = userEvent.setup();
    const { router } = renderApp('/pacientes/p1/anamneses/nova');

    await user.click(await screen.findByRole('radio', { name: '4 — Levemente Desidratado' }));
    await user.click(screen.getByRole('button', { name: 'Salvar rascunho' }));
    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toMatchObject({ urineScale: 4 }));

    await act(() => router.navigate('/pacientes/p1/anamneses/a8'));
    expect(await screen.findByRole('radio', { name: '8 — Severamente Desidratado' })).toBeChecked();
    expect(screen.getByRole('radio', { name: '7 — Severamente Desidratado' })).not.toBeChecked();
  });

  it('métodos de FC máxima conforme o sexo do paciente', async () => {
    mockAnamnesisApi({ 'GET /patients/p1': { body: { ...patientMan, sex: 'FEMININO' } } });
    renderApp('/pacientes/p1/anamneses/nova');

    const select = await screen.findByLabelText('Método de FC máxima');
    expect(within(select).getAllByRole('option').map((option) => option.textContent)).toEqual([
      'Selecione',
      'Karvonen et al., 1957 — clássica: 220 − idade',
      'Jones et al., 1975: 220 − 0,65 × idade',
      'Tanaka et al., 2001: 208 − 0,7 × idade',
      'Calvert et al., 1977: 192 − 0,7 × idade',
      'Hossack et al., 1981: 206 − 0,597 × idade',
    ]);
  });

  it('prévia: idade, FC máxima e de reserva, PA e água vêm do cálculo da API', async () => {
    mockAnamnesisApi({
      'GET /patients/p1/anamneses/calculation': (request) => ({
        body: { calculation: request.path === FULL_CALCULATION ? calcFull : calcEmpty, errors: {} },
      }),
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/anamneses/nova');
    await screen.findByRole('heading', { name: 'Nova anamnese' });
    await waitFor(() => expect(screen.getByLabelText('Classificação da PA')).toHaveTextContent('Informe PAS e PAD para classificar.'));

    fireEvent.change(screen.getByLabelText('Data da anamnese *'), { target: { value: '2026-05-09' } });
    await user.type(screen.getByLabelText('FC em repouso (bpm)'), '70');
    await user.selectOptions(screen.getByLabelText('Método de FC máxima'), 'tanaka-2001');
    await user.type(screen.getByLabelText('PAS (mmHg)'), '150');
    await user.type(screen.getByLabelText('PAD (mmHg)'), '80');
    await user.type(screen.getByLabelText('Peso (kg)'), '70');

    await waitFor(() => expect(screen.getByLabelText('Classificação da PA')).toHaveTextContent('HA Sistólica Isolada – Estágio 1'));
    expect(screen.getByLabelText('Idade na data')).toHaveTextContent('45 anos');
    expect(screen.getByLabelText('FC máxima')).toHaveTextContent('177 bpm');
    expect(screen.getByLabelText('FC de reserva')).toHaveTextContent('107 bpm');
    expect(screen.getByLabelText('Água ideal')).toHaveTextContent('2,45 L');
  });

  it('erros por campo da API aparecem junto aos campos', async () => {
    mockAnamnesisApi({
      'POST /patients/p1/anamneses': {
        status: 400,
        body: {
          message: 'Dados inválidos.',
          errors: {
            anamnesisDate: 'A data da anamnese não pode ser anterior ao nascimento do paciente.',
            waterWeightKg: 'O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
          },
        },
      },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/anamneses/nova');

    await user.click(await screen.findByRole('button', { name: 'Salvar rascunho' }));

    expect(await screen.findByText('A data da anamnese não pode ser anterior ao nascimento do paciente.')).toBeInTheDocument();
    expect(screen.getByText('O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.')).toBeInTheDocument();
    expect(screen.getByLabelText('Peso (kg)')).toHaveAttribute('aria-invalid', 'true');
  });

  it('409 ao salvar: Carregar versão atual traz a versão do servidor', async () => {
    const current = { ...draftAnamnesis, version: 2, smoking: false };
    const { calls } = mockAnamnesisApi({
      'GET /anamneses/a1': { body: draftAnamnesis },
      'PUT /anamneses/a1': { status: 409, body: { message: 'Esta anamnese foi alterada por outra pessoa. Recarregue para ver a versão atual.', current } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/anamneses/a1');

    expect(within(await screen.findByRole('group', { name: 'Fumante?' })).getByRole('radio', { name: 'Sim' })).toBeChecked();
    await user.click(screen.getByRole('button', { name: 'Salvar rascunho' }));

    expect(await screen.findByText('Esta anamnese foi alterada por outra pessoa. Recarregue para ver a versão atual.')).toBeInTheDocument();
    expect(calls.find((c) => c.method === 'PUT')?.body).toMatchObject({ version: 1, smoking: true });
    await user.click(screen.getByRole('button', { name: 'Carregar versão atual' }));
    expect(within(screen.getByRole('group', { name: 'Fumante?' })).getByRole('radio', { name: 'Não' })).toBeChecked();
  });

  it('resposta do salvamento que chega depois da troca de sessão não navega nem grava no cache', async () => {
    let respond: ((response: MockResponse) => void) | undefined;
    mockAnamnesisApi({ 'POST /patients/p1/anamneses': () => new Promise<MockResponse>((resolve) => (respond = resolve)) });
    const user = userEvent.setup();
    const { router, queryClient } = renderApp('/pacientes/p1/anamneses/nova');

    await user.click(await screen.findByRole('button', { name: 'Salvar rascunho' }));
    await waitFor(() => expect(respond).toBeDefined());
    act(() => endSession(queryClient));
    await act(async () => respond?.(created));
    await settle();

    expect(router.state.location.pathname).not.toBe('/pacientes/p1/anamneses/a1');
    expect(queryClient.getQueryData(['anamnesis', 'a1'])).toBeUndefined();
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/web -- src/anamneses src/lib/format.test.ts`
Expected: FAIL. Os imports `./questions`, `todayInClinic` e `formatDecimalBr` não existem, e a rota `/pacientes/p1/anamneses/nova` cai no `*`.

- [ ] **Step 3: Formatação**

Em `apps/web/src/lib/format.ts`, acrescente:

```ts
/** Hoje (AAAA-MM-DD) no fuso da clínica. */
export function todayInClinic(now: Date = new Date()): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: CLINIC_TIME_ZONE, year: 'numeric', month: '2-digit', day: '2-digit' }).format(now);
}

/** Número com casas fixas em pt-BR (2,45). */
export function formatDecimalBr(value: number, digits: number): string {
  return new Intl.NumberFormat('pt-BR', { minimumFractionDigits: digits, maximumFractionDigits: digits }).format(value);
}
```

- [ ] **Step 4: Perguntas, pergunta Sim/Não e atraso da prévia**

`apps/web/src/anamneses/questions.ts`. As legendas são as do Excel, conferidas pela pessoa (DIV-13). Copie-as exatamente:

```ts
import type { TextField, YesNoField } from './types';

export type QuestionItem = { kind: 'yesNo'; field: YesNoField; label: string } | { kind: 'text'; field: TextField; label: string };

export interface QuestionGroup {
  title: string;
  items: QuestionItem[];
}

const yesNo = (field: YesNoField, label: string): QuestionItem => ({ kind: 'yesNo', field, label });
const text = (field: TextField, label: string): QuestionItem => ({ kind: 'text', field, label });

/** Grupos e legendas do formulário do legado (DIV-13, conferidos no Excel em 2026-10-04). Coluna da aba Anamnese ao lado. */
export const QUESTION_GROUPS: QuestionGroup[] = [
  {
    title: 'Hábitos e Histórico Social',
    items: [
      yesNo('alcohol', 'Ingere Bebida Alcoólica?'), // P
      yesNo('smoking', 'Fumante?'), // Q
      yesNo('goodSleep', 'Dorme Bem?'), // R
      text('sleepDetails', 'Descrição do sono'), // S
      yesNo('physicalActivity', 'Pratica Atividade Física?'), // T
      text('activityDetails', 'Descrição das atividades físicas'), // U
    ],
  },
  {
    title: 'Alimentação e Hidratação',
    items: [
      yesNo('adequateWater', 'Hidratação. Ingere bastante Água?'), // V
      text('waterDetails', 'Descrição da ingestão de água'), // X
      yesNo('supplements', 'Ingere Suplementos Alimentares?'), // Y
      text('supplementDetails', 'Descrição dos suplementos'), // Z
      text('eatingHabits', 'Descreva sobre Hábitos Alimentares'), // AA
    ],
  },
  {
    title: 'Doenças, Tratamentos e Medicamentos, Dispositivos de Saúde',
    items: [
      yesNo('anemia', 'Possui Anemia?'), // AB
      yesNo('diabetes', 'Possui Diabetes?'), // AC
      yesNo('cardiacConditions', 'Problemas Cardíacos?'), // AD
      yesNo('bloodPressureConditions', 'Hipo/Hipertensão Arterial?'), // AE
      yesNo('circulatoryConditions', 'Problemas Circulatórios?'), // AF
      yesNo('allergies', 'Possui Alergia?'), // AG
      text('otherConditions', 'Alguma outra Doença não mencionada?'), // AH
      yesNo('treatment', 'Faz algum Tratamento Médico?'), // AI
      text('treatmentDetails', 'Descrição dos tratamentos'), // AJ
      yesNo('medications', 'Faz uso de algum Medicamento?'), // AK
      text('medicationDetails', 'Descrição dos medicamentos'), // AL
      yesNo('healthDevice', 'Utiliza Algum dispositivo de saúde? Ex.Marcapasso?'), // AM
      text('healthDeviceDetails', 'Descrição do dispositivo de saúde'), // AN
    ],
  },
  {
    title: 'Distúrbios psíquicos',
    items: [
      yesNo('anxiety', 'Predisposição a Ansiedade?'), // AO
      yesNo('depression', 'Predisposição a Depressão?'), // AP
      yesNo('eatingDisorders', 'Transtornos Alimentares?'), // AQ
      yesNo('bipolarDisorder', 'Predisposição a Transtorno Bipolar?'), // AR
      yesNo('ocd', 'Predisposição a Transtorno obsessivo-compulsivo?'), // AS
      yesNo('postTraumaticStress', 'Estresse Pós-Traumático?'), // AT
      yesNo('otherMentalConditions', 'Outros transtornos não mencionados?'), // AU
      text('mentalConditionsDetails', 'Descrição dos outros transtornos'), // AV
    ],
  },
];

/** Coluna O, numa seção própria no fim do formulário. */
export const OBSERVATIONS_LABEL = 'Observações Complementares';
```

`apps/web/src/anamneses/YesNoQuestion.tsx`:

```tsx
interface YesNoQuestionProps {
  name: string;
  label: string;
  value: boolean | null;
  onChange: (value: boolean | null) => void;
  /** Mostra "Limpar resposta" quando há resposta (falso na anamnese finalizada). */
  clearable: boolean;
}

/** Pergunta Sim/Não que começa sem resposta (null); "Limpar resposta" volta para não respondida. */
export function YesNoQuestion({ name, label, value, onChange, clearable }: YesNoQuestionProps) {
  return (
    <fieldset className="space-y-1">
      <legend className="text-sm font-semibold">{label}</legend>
      <div className="flex flex-wrap items-center gap-4">
        <label className="flex items-center gap-1 text-sm">
          <input type="radio" name={name} checked={value === true} onChange={() => onChange(true)} />
          Sim
        </label>
        <label className="flex items-center gap-1 text-sm">
          <input type="radio" name={name} checked={value === false} onChange={() => onChange(false)} />
          Não
        </label>
        {clearable && value !== null ? (
          <button type="button" className="text-sm text-terracota underline" onClick={() => onChange(null)}>
            Limpar resposta
          </button>
        ) : null}
      </div>
    </fieldset>
  );
}
```

`apps/web/src/anamneses/use-debounced-value.ts`: `useDebouncedValue(value, delayMs)` devolve `value` só depois de `delayMs` sem mudanças. Ela usa `useState(value)` e um `useEffect([value, delayMs])` com `setTimeout`/`clearTimeout`. O valor inicial é devolvido de imediato.

- [ ] **Step 5: Página do formulário**

`apps/web/src/anamneses/AnamnesisFormPage.tsx`. As regras de efeitos da Global Constraints valem aqui:

- `save.mutate()` é chamado sem callbacks;
- cache, navegação e estado só mudam em `onSuccess` e `onError` das opções.

```tsx
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useState, type ReactNode } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router';
import { useCurrentUser } from '../auth/current-user';
import { can, canAny } from '../auth/me';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Field } from '../components/ui/Field';
import { Input } from '../components/ui/Input';
import { PageTitle } from '../components/ui/PageTitle';
import { Select } from '../components/ui/Select';
import { Textarea } from '../components/ui/Textarea';
import { api, ApiError, fieldErrors } from '../lib/api';
import { errorMessage } from '../lib/error-message';
import { formatDateBr, formatDecimalBr, todayInClinic } from '../lib/format';
import type { Patient } from '../patients/types';
import { OBSERVATIONS_LABEL, QUESTION_GROUPS } from './questions';
import {
  ANAMNESIS_PERMISSIONS, TEXT_FIELDS, YES_NO_FIELDS, type Anamnesis, type AnamnesisCalculationResult, type AnamnesisResults,
  type AnamnesisRules, type TextField, type YesNoField,
} from './types';
import { useDebouncedValue } from './use-debounced-value';
import { YesNoQuestion } from './YesNoQuestion';

/** MaxLength dos textos no AnamnesisBodyDto. */
const TEXT_MAX_LENGTH = 4000;
const PREVIEW_DELAY_MS = 300;
const NUMBER_FIELDS = ['restingHeartRate', 'systolicPressure', 'diastolicPressure', 'waterWeightKg', 'waterMlPerKg'] as const;
type NumberField = (typeof NUMBER_FIELDS)[number];

interface FormState {
  anamnesisDate: string;
  heartRateMethod: string;
  urineScale: number | null;
  numbers: Record<NumberField, string>;
  answers: Record<YesNoField, boolean | null>;
  texts: Record<TextField, string>;
}

function emptyForm(rules: AnamnesisRules): FormState {
  return {
    anamnesisDate: todayInClinic(),
    heartRateMethod: '',
    urineScale: null,
    numbers: { restingHeartRate: '', systolicPressure: '', diastolicPressure: '', waterWeightKg: '', waterMlPerKg: String(rules.water.defaultMlPerKg) },
    answers: Object.fromEntries(YES_NO_FIELDS.map((field) => [field, null])) as Record<YesNoField, boolean | null>,
    texts: Object.fromEntries(TEXT_FIELDS.map((field) => [field, ''])) as Record<TextField, string>,
  };
}

function toForm(a: Anamnesis): FormState {
  const asText = (value: number | null) => (value === null ? '' : String(value));
  return {
    anamnesisDate: a.anamnesisDate,
    heartRateMethod: a.heartRateMethod ?? '',
    urineScale: a.urineScale,
    numbers: {
      restingHeartRate: asText(a.restingHeartRate),
      systolicPressure: asText(a.systolicPressure),
      diastolicPressure: asText(a.diastolicPressure),
      waterWeightKg: asText(a.waterWeightKg),
      waterMlPerKg: asText(a.waterMlPerKg),
    },
    answers: Object.fromEntries(YES_NO_FIELDS.map((field) => [field, a[field]])) as Record<YesNoField, boolean | null>,
    texts: Object.fromEntries(TEXT_FIELDS.map((field) => [field, a[field] ?? ''])) as Record<TextField, string>,
  };
}

/** Corpo do POST/PUT: vazio vai como null; os campos numéricos são type="number" e vão como número. */
function toPayload(form: FormState) {
  return {
    anamnesisDate: form.anamnesisDate,
    heartRateMethod: form.heartRateMethod === '' ? null : form.heartRateMethod,
    urineScale: form.urineScale,
    ...Object.fromEntries(NUMBER_FIELDS.map((field) => [field, form.numbers[field].trim() === '' ? null : Number(form.numbers[field])])),
    ...form.answers,
    ...Object.fromEntries(TEXT_FIELDS.map((field) => [field, form.texts[field].trim() === '' ? null : form.texts[field].trim()])),
  };
}

/** Query string da prévia: só o que entra nos cálculos, nesta ordem; sem data não há prévia. */
function calculationQuery(form: FormState): string | null {
  if (form.anamnesisDate === '') return null;
  const params = new URLSearchParams({ anamnesisDate: form.anamnesisDate });
  for (const field of NUMBER_FIELDS) if (form.numbers[field].trim() !== '') params.set(field, form.numbers[field].trim());
  if (form.heartRateMethod !== '') params.set('heartRateMethod', form.heartRateMethod);
  if (form.urineScale !== null) params.set('urineScale', String(form.urineScale));
  return params.toString();
}

const bpm = (value: number | null | undefined) => (value === null || value === undefined ? '—' : `${value} bpm`);
const liters = (value: number | null | undefined) => (value === null || value === undefined ? '—' : `${formatDecimalBr(value, 2)} L`);

function pressureText(results: AnamnesisResults | null, rules: AnamnesisRules): string {
  if (!results) return '—';
  if (results.bloodPressureStatus === 'DADOS_INCOMPLETOS') return 'Informe PAS e PAD para classificar.';
  if (results.bloodPressureStatus === 'FORA_DA_FAIXA_ETARIA') return `Não se aplica a menores de ${rules.bloodPressure.minAgeYears} anos.`;
  return results.bloodPressureClassification ?? '—';
}

function FormSection({ title, children }: { title: string; children: ReactNode }) {
  const id = useId();
  return (
    <section aria-labelledby={id} className="space-y-4">
      <h2 id={id} className="font-display text-lg text-terracota">
        {title}
      </h2>
      <div className="grid gap-4 sm:grid-cols-2">{children}</div>
    </section>
  );
}

/** Valor calculado (somente leitura); o nome acessível é o rótulo, para leitores de tela e testes. */
function Result({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm font-semibold">{label}</span>
      <output aria-label={label} className="text-sm">
        {value}
      </output>
    </div>
  );
}

/** Remonta o formulário quando muda a anamnese (ou de "nova" para a salva). */
export function AnamnesisFormRoute() {
  const { patientId, anamnesisId } = useParams();
  const me = useCurrentUser();
  if (!patientId || !can(me, 'patients.read') || !canAny(me, ANAMNESIS_PERMISSIONS)) {
    return <Alert>Você não tem permissão para acessar esta página.</Alert>;
  }
  return <AnamnesisPage key={`${patientId}/${anamnesisId ?? 'nova'}`} patientId={patientId} anamnesisId={anamnesisId} />;
}

function AnamnesisPage({ patientId, anamnesisId }: { patientId: string; anamnesisId?: string }) {
  const patient = useQuery({
    queryKey: ['patient', patientId],
    queryFn: () => api<Patient>(`/patients/${patientId}`),
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });
  const rules = useQuery({ queryKey: ['anamnesis-rules'], queryFn: () => api<AnamnesisRules>('/anamneses/rules'), staleTime: Infinity });
  const anamnesis = useQuery({
    queryKey: ['anamnesis', anamnesisId],
    queryFn: () => api<Anamnesis>(`/anamneses/${anamnesisId}`),
    enabled: anamnesisId !== undefined,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  const error = patient.error ?? rules.error ?? anamnesis.error;
  if (error) return <Alert>{errorMessage(error)}</Alert>;
  if (!patient.data || !rules.data || (anamnesisId !== undefined && !anamnesis.data)) return <p className="text-sm">Carregando…</p>;
  if (anamnesis.data && anamnesis.data.patientId !== patientId) return <Alert>Anamnese não encontrada.</Alert>;
  return <AnamnesisForm patient={patient.data} rules={rules.data} anamnesis={anamnesis.data ?? null} />;
}

function AnamnesisForm({ patient, rules, anamnesis }: { patient: Patient; rules: AnamnesisRules; anamnesis: Anamnesis | null }) {
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FormState>(() => (anamnesis ? toForm(anamnesis) : emptyForm(rules)));
  const [version, setVersion] = useState<number | null>(anamnesis?.version ?? null);
  const [conflict, setConflict] = useState<Anamnesis | null>(null);
  const [notice, setNotice] = useState<string | null>((location.state as { notice?: string } | null)?.notice ?? null);
  const isNew = anamnesis === null;
  const editable = anamnesis?.status !== 'FINALIZADA';

  useEffect(() => {
    if (anamnesis) {
      setForm(toForm(anamnesis));
      setVersion(anamnesis.version);
    }
  }, [anamnesis]);

  const previewParams = useDebouncedValue(editable ? calculationQuery(form) : null, PREVIEW_DELAY_MS);
  const preview = useQuery({
    queryKey: ['anamnesis-calculation', patient.id, previewParams],
    queryFn: () => api<AnamnesisCalculationResult>(`/patients/${patient.id}/anamneses/calculation?${previewParams}`),
    enabled: previewParams !== null,
    placeholderData: keepPreviousData,
  });
  // Rascunho: o que a API calculou para o formulário atual. Finalizada: o que foi gravado.
  const results: AnamnesisResults | null = editable ? (preview.data?.calculation ?? null) : anamnesis;

  function keepConflict(error: unknown) {
    if (error instanceof ApiError && error.status === 409 && error.body.current) setConflict(error.body.current as Anamnesis);
  }

  const save = useMutation({
    mutationFn: () =>
      anamnesis === null
        ? api<Anamnesis>(`/patients/${patient.id}/anamneses`, { method: 'POST', json: toPayload(form) })
        : api<Anamnesis>(`/anamneses/${anamnesis.id}`, { method: 'PUT', json: { ...toPayload(form), version } }),
    onSuccess: (result) => {
      setConflict(null);
      void queryClient.invalidateQueries({ queryKey: ['anamneses', patient.id] });
      queryClient.setQueryData(['anamnesis', result.id], result);
      if (isNew) navigate(`/pacientes/${patient.id}/anamneses/${result.id}`, { replace: true, state: { notice: 'Rascunho salvo.' } });
      else setNotice('Rascunho salvo.');
    },
    onError: (error) => {
      setNotice(null);
      keepConflict(error);
    },
  });

  function update(change: (current: FormState) => FormState) {
    setForm(change);
    setNotice(null);
  }

  const setNumber = (field: NumberField, value: string) => update((current) => ({ ...current, numbers: { ...current.numbers, [field]: value } }));

  function loadCurrentVersion() {
    if (!conflict) return;
    queryClient.setQueryData(['anamnesis', conflict.id], conflict);
    setConflict(null);
    save.reset();
  }

  const lastError = save.error;
  // Erros da prévia aparecem enquanto se digita; os do salvamento têm prioridade.
  const errors: Record<string, string | undefined> = { ...(editable ? preview.data?.errors : {}), ...fieldErrors(lastError) };
  const generalError = lastError && !conflict ? errorMessage(lastError) : null;
  const busy = save.isPending;
  const methods = rules.heartRateMethods.filter((method) => method.sexes.includes(patient.sex));
  const age = results?.ageYears;

  return (
    <section className="space-y-4">
      <PageTitle>{isNew ? 'Nova anamnese' : `Anamnese de ${formatDateBr(anamnesis.anamnesisDate)}`}</PageTitle>
      <p className="text-sm">
        {'Paciente: '}
        <Link to={`/pacientes/${patient.id}`} className="text-terracota underline">
          {patient.fullName}
        </Link>
      </p>
      <p className="text-sm">
        {editable
          ? `Regras ${rules.version}${rules.status === 'PROVISORIA' ? ' (provisórias, a validar com o médico responsável)' : ''}.`
          : `Regras ${anamnesis?.rulesVersion}.`}
      </p>
      {notice ? <Alert tone="success">{notice}</Alert> : null}
      {conflict ? (
        <Alert>
          <p>{errorMessage(lastError)}</p>
          <Button variant="secondary" className="mt-2" onClick={loadCurrentVersion}>
            Carregar versão atual
          </Button>
        </Alert>
      ) : null}
      {generalError ? <Alert>{generalError}</Alert> : null}

      <form
        className="space-y-6 rounded-lg border border-areia bg-white p-6"
        onSubmit={(event) => {
          event.preventDefault();
          save.mutate();
        }}
      >
        <fieldset disabled={!editable || busy} className="space-y-8">
          <FormSection title="Atendimento">
            <Field label="Data da anamnese" error={errors.anamnesisDate} required>
              <Input
                type="date"
                name="anamnesisDate"
                value={form.anamnesisDate}
                onChange={(e) => update((current) => ({ ...current, anamnesisDate: e.target.value }))}
                required
              />
            </Field>
            <Result label="Idade na data" value={age === undefined ? '—' : `${age} anos`} />
          </FormSection>

          <FormSection title="Frequência cardíaca">
            <Field label="FC em repouso (bpm)" error={errors.restingHeartRate}>
              <Input type="number" name="restingHeartRate" min={1} max={250} step={1} value={form.numbers.restingHeartRate} onChange={(e) => setNumber('restingHeartRate', e.target.value)} />
            </Field>
            {editable ? (
              <Field label="Método de FC máxima" error={errors.heartRateMethod}>
                <Select name="heartRateMethod" value={form.heartRateMethod} onChange={(e) => update((current) => ({ ...current, heartRateMethod: e.target.value }))}>
                  <option value="">Selecione</option>
                  {methods.map((method) => (
                    <option key={method.id} value={method.id}>
                      {method.label}
                    </option>
                  ))}
                </Select>
              </Field>
            ) : (
              <Result label="Método de FC máxima" value={anamnesis?.heartRateMethodLabel ?? '—'} />
            )}
            <Result label="FC máxima" value={bpm(results?.maxHeartRate)} />
            <Result label="FC de reserva" value={bpm(results?.reserveHeartRate)} />
          </FormSection>

          <FormSection title="Pressão arterial">
            <Field label="PAS (mmHg)" error={errors.systolicPressure}>
              <Input type="number" name="systolicPressure" min={1} max={300} step={1} value={form.numbers.systolicPressure} onChange={(e) => setNumber('systolicPressure', e.target.value)} />
            </Field>
            <Field label="PAD (mmHg)" error={errors.diastolicPressure}>
              <Input type="number" name="diastolicPressure" min={1} max={300} step={1} value={form.numbers.diastolicPressure} onChange={(e) => setNumber('diastolicPressure', e.target.value)} />
            </Field>
            <Result label="Classificação da PA" value={pressureText(results, rules)} />
            <p className="text-sm sm:col-span-2">{`Referência: ${rules.bloodPressure.source}`}</p>
          </FormSection>

          <FormSection title="Hidratação">
            <fieldset className="sm:col-span-2">
              <legend className="text-sm font-semibold">Escala de urina</legend>
              <div className="mt-2 grid gap-2 sm:grid-cols-2">
                {rules.urineScale.map(({ scale, label }) => (
                  <label key={scale} className="flex items-center gap-2 text-sm">
                    <input
                      type="radio"
                      name="urineScale"
                      checked={form.urineScale === scale}
                      onChange={() => update((current) => ({ ...current, urineScale: scale }))}
                    />
                    {`${scale} — ${label}`}
                  </label>
                ))}
              </div>
              {errors.urineScale ? <p className="text-sm text-terracota">{errors.urineScale}</p> : null}
              {editable && form.urineScale !== null ? (
                <Button variant="secondary" className="mt-2" onClick={() => update((current) => ({ ...current, urineScale: null }))}>
                  Limpar escala
                </Button>
              ) : null}
            </fieldset>
            <Result label="Classificação da hidratação" value={results?.urineClassification ?? '—'} />
            <Field label="Peso (kg)" error={errors.waterWeightKg}>
              <Input type="number" name="waterWeightKg" min={0.01} max={500} step={0.01} value={form.numbers.waterWeightKg} onChange={(e) => setNumber('waterWeightKg', e.target.value)} />
            </Field>
            <Field label="Fator (mL/kg)" error={errors.waterMlPerKg}>
              <Input type="number" name="waterMlPerKg" min={0.01} max={200} step={0.01} value={form.numbers.waterMlPerKg} onChange={(e) => setNumber('waterMlPerKg', e.target.value)} />
            </Field>
            <Result label="Água ideal" value={liters(results?.idealWaterLiters)} />
          </FormSection>

          {QUESTION_GROUPS.map((group) => (
            <FormSection key={group.title} title={group.title}>
              {group.items.map((item) =>
                item.kind === 'yesNo' ? (
                  <YesNoQuestion
                    key={item.field}
                    name={item.field}
                    label={item.label}
                    value={form.answers[item.field]}
                    onChange={(value) => update((current) => ({ ...current, answers: { ...current.answers, [item.field]: value } }))}
                    clearable={editable}
                  />
                ) : (
                  <div key={item.field} className="sm:col-span-2">
                    <Field label={item.label} error={errors[item.field]}>
                      <Textarea
                        name={item.field}
                        rows={2}
                        maxLength={TEXT_MAX_LENGTH}
                        value={form.texts[item.field]}
                        onChange={(e) => update((current) => ({ ...current, texts: { ...current.texts, [item.field]: e.target.value } }))}
                      />
                    </Field>
                  </div>
                ),
              )}
            </FormSection>
          ))}

          <FormSection title="Observações">
            <div className="sm:col-span-2">
              <Field label={OBSERVATIONS_LABEL} error={errors.observations}>
                <Textarea
                  name="observations"
                  rows={4}
                  maxLength={TEXT_MAX_LENGTH}
                  value={form.texts.observations}
                  onChange={(e) => update((current) => ({ ...current, texts: { ...current.texts, observations: e.target.value } }))}
                />
              </Field>
            </div>
          </FormSection>
        </fieldset>

        {editable ? (
          <div className="flex flex-wrap gap-3">
            <Button type="submit" disabled={busy}>
              {save.isPending ? 'Salvando…' : 'Salvar rascunho'}
            </Button>
          </div>
        ) : null}
      </form>
    </section>
  );
}
```

- [ ] **Step 6: Rotas**

Em `apps/web/src/router.tsx`, importe `AnamnesisFormRoute` e acrescente, depois de `pacientes/:id`:

```tsx
      // A página confere patients.read e anamneses.draft ou anamneses.finalize.
      { path: 'pacientes/:patientId/anamneses/nova', element: <AnamnesisFormRoute /> },
      { path: 'pacientes/:patientId/anamneses/:anamnesisId', element: <AnamnesisFormRoute /> },
```

- [ ] **Step 7: Rodar e ver passar**

Run: `npm test -w @psm/web; npm run typecheck -w @psm/web`
Expected: PASS. A web fica com 18 arquivos: `anamnesis-form.test.tsx` passa nos 9 testes e `format.test.ts` ganha 2. `typecheck` sai sem erros.

- [ ] **Step 8: Commit**

```powershell
git add -A
git commit -m "feat(web): formulário da anamnese com prévia dos cálculos da API e rascunho"
```

---

### Task 9: Web — finalização e anamnese somente leitura

**Files:**
- Modify: `apps/web/src/anamneses/AnamnesisFormPage.tsx`
- Test: `apps/web/src/anamneses/anamnesis-finalize.test.tsx`

**Interfaces:**
- Consumes: `POST /api/anamneses/:id/finalize` (Task 6); `AnamnesisForm` (Task 8); `formatDateTimeBr`; `draftAnamnesis`, `finalizedAnamnesis` e `mockAnamnesisApi` (Task 7).
- Produces:
  - o botão "Finalizar", com confirmação "Confirmar finalização", visível só com `anamneses.finalize`, num rascunho já salvo;
  - o aviso "Anamnese finalizada em dd/mm/aaaa hh:mm. Não pode ser alterada.".

- [ ] **Step 1: Escrever os testes**

`apps/web/src/anamneses/anamnesis-finalize.test.tsx`:

```tsx
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import type { Me } from '../auth/me';
import { draftAnamnesis, finalizedAnamnesis, mockAnamnesisApi } from '../test/anamnesis-fixtures';
import { assistantMe } from '../test/fixtures';
import { renderApp } from '../test/render';

const draftAssistantMe: Me = { ...assistantMe, permissions: ['patients.read', 'patients.write', 'anamneses.draft'] };
const FINALIZED_TEXT = /Anamnese finalizada em 09\/05\/2026.*12:30\. Não pode ser alterada\./;

describe('Finalização da anamnese', () => {
  it('profissional finaliza: confirma, envia o formulário com a versão e a anamnese fica somente leitura', async () => {
    const { calls } = mockAnamnesisApi({ 'GET /anamneses/a1': { body: draftAnamnesis }, 'POST /anamneses/a1/finalize': { body: finalizedAnamnesis } });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/anamneses/a1');

    await user.click(await screen.findByRole('button', { name: 'Finalizar' }));
    expect(screen.getByText('Depois de finalizada, a anamnese não pode ser alterada.')).toBeInTheDocument();
    expect(calls.some((c) => c.path.endsWith('/finalize'))).toBe(false);
    await user.click(screen.getByRole('button', { name: 'Confirmar finalização' }));

    expect(await screen.findByText('Anamnese finalizada.')).toBeInTheDocument();
    expect(calls.find((c) => c.path === '/anamneses/a1/finalize')?.body).toMatchObject({
      version: 1, anamnesisDate: '2026-05-09', smoking: true, urineScale: 4, heartRateMethod: 'tanaka-2001',
    });
    expect(screen.getByText(FINALIZED_TEXT)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Salvar rascunho' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Finalizar' })).not.toBeInTheDocument();
    expect(screen.getByLabelText('FC em repouso (bpm)')).toBeDisabled();
  });

  it('cancelar a confirmação não finaliza', async () => {
    const { calls } = mockAnamnesisApi({ 'GET /anamneses/a1': { body: draftAnamnesis } });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/anamneses/a1');

    await user.click(await screen.findByRole('button', { name: 'Finalizar' }));
    await user.click(screen.getByRole('button', { name: 'Cancelar' }));

    expect(screen.queryByText('Depois de finalizada, a anamnese não pode ser alterada.')).not.toBeInTheDocument();
    expect(calls.some((c) => c.path.endsWith('/finalize'))).toBe(false);
  });

  it('assistente sem anamneses.finalize salva rascunho, mas não vê Finalizar', async () => {
    mockAnamnesisApi({ 'GET /auth/me': { body: draftAssistantMe }, 'GET /anamneses/a1': { body: draftAnamnesis } });
    renderApp('/pacientes/p1/anamneses/a1');

    expect(await screen.findByRole('button', { name: 'Salvar rascunho' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Finalizar' })).not.toBeInTheDocument();
  });

  it('nova anamnese não oferece Finalizar antes do primeiro salvamento', async () => {
    mockAnamnesisApi();
    renderApp('/pacientes/p1/anamneses/nova');

    expect(await screen.findByRole('button', { name: 'Salvar rascunho' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Finalizar' })).not.toBeInTheDocument();
  });

  it('finalizada abre somente leitura, com os valores gravados e sem prévia', async () => {
    const { calls } = mockAnamnesisApi({ 'GET /anamneses/a1': { body: finalizedAnamnesis } });
    renderApp('/pacientes/p1/anamneses/a1');

    expect(await screen.findByText(FINALIZED_TEXT)).toBeInTheDocument();
    expect(screen.getByLabelText('FC máxima')).toHaveTextContent('177 bpm');
    expect(screen.getByLabelText('Classificação da PA')).toHaveTextContent('HA Sistólica Isolada – Estágio 1');
    expect(screen.getByLabelText('Método de FC máxima')).toHaveTextContent('Tanaka et al., 2001: 208 − 0,7 × idade');
    expect(screen.getByLabelText('Água ideal')).toHaveTextContent('2,45 L');
    expect(screen.getByText('Regras anamnese-v1.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Limpar resposta' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Salvar rascunho' })).not.toBeInTheDocument();
    expect(calls.filter((c) => c.path.includes('/calculation'))).toEqual([]);
  });

  it('salvar uma anamnese que outra pessoa finalizou: 409 e Carregar versão atual mostra a finalizada', async () => {
    mockAnamnesisApi({
      'GET /anamneses/a1': { body: draftAnamnesis },
      'PUT /anamneses/a1': { status: 409, body: { message: 'Esta anamnese já foi finalizada e não pode ser alterada.', current: finalizedAnamnesis } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1/anamneses/a1');

    await user.click(await screen.findByRole('button', { name: 'Salvar rascunho' }));
    expect(await screen.findByText('Esta anamnese já foi finalizada e não pode ser alterada.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Carregar versão atual' }));

    await waitFor(() => expect(screen.getByText(FINALIZED_TEXT)).toBeInTheDocument());
    expect(screen.queryByRole('button', { name: 'Salvar rascunho' })).not.toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/web -- src/anamneses/anamnesis-finalize.test.tsx`
Expected: FAIL. O botão "Finalizar" e o aviso "Anamnese finalizada em …" não existem.

- [ ] **Step 3: Implementar a finalização em `AnamnesisFormPage.tsx`**

Mudanças no `AnamnesisForm`:

1. Imports: inclua `formatDateTimeBr` no import de `../lib/format` e use `useCurrentUser` dentro de `AnamnesisForm`.
2. Estado e mutação, depois de `save`:

```tsx
  const me = useCurrentUser();
  const [confirming, setConfirming] = useState(false);
  const canFinalize = !isNew && editable && can(me, 'anamneses.finalize');

  const finalize = useMutation({
    mutationFn: () =>
      api<Anamnesis>(`/anamneses/${anamnesis?.id}/finalize`, { method: 'POST', json: { ...toPayload(form), version } }),
    onSuccess: (result) => {
      setConflict(null);
      setConfirming(false);
      void queryClient.invalidateQueries({ queryKey: ['anamneses', patient.id] });
      queryClient.setQueryData(['anamnesis', result.id], result);
      setNotice('Anamnese finalizada.');
    },
    onError: (error) => {
      setNotice(null);
      setConfirming(false);
      keepConflict(error);
    },
  });
```

3. "Feedback só da última ação" (mesmo padrão dos pacientes):
   - no `onSubmit`, chame `finalize.reset()` antes de `save.mutate()`;
   - `loadCurrentVersion` passa a chamar também `finalize.reset()`;
   - `lastError = save.error ?? finalize.error`;
   - `busy = save.isPending || finalize.isPending`.
4. Logo depois do `<PageTitle>`:

```tsx
      {anamnesis?.status === 'FINALIZADA' && anamnesis.finalizedAt ? (
        <Alert tone="info">{`Anamnese finalizada em ${formatDateTimeBr(anamnesis.finalizedAt)}. Não pode ser alterada.`}</Alert>
      ) : null}
```

5. No `div` dos botões, depois de "Salvar rascunho":

```tsx
            {canFinalize && !confirming ? (
              <Button variant="secondary" onClick={() => setConfirming(true)} disabled={busy}>
                Finalizar
              </Button>
            ) : null}
```

e logo abaixo do `div`, ainda dentro do `{editable ? … : null}`:

```tsx
        {confirming ? (
          <Alert tone="info">
            <p>Depois de finalizada, a anamnese não pode ser alterada.</p>
            <div className="mt-2 flex gap-3">
              <Button
                onClick={() => {
                  save.reset();
                  finalize.mutate();
                }}
                disabled={finalize.isPending}
              >
                {finalize.isPending ? 'Finalizando…' : 'Confirmar finalização'}
              </Button>
              <Button variant="secondary" onClick={() => setConfirming(false)}>
                Cancelar
              </Button>
            </div>
          </Alert>
        ) : null}
```

Envolva o `div` e a confirmação num fragmento.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/web; npm run typecheck -w @psm/web; npm run build -w @psm/web`
Expected: PASS. A web fica com 19 arquivos e `anamnesis-finalize.test.tsx` passa nos 6 testes. `typecheck` sai sem erros e o `build` gera `apps/web/dist/`.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(web): finalização da anamnese com confirmação e leitura da finalizada"
```

---

### Task 10: Verificação ponta a ponta e README

**Files:**
- Modify: `README.md`

**Interfaces:**
- Consumes: tudo o que as Tasks 1–9 produziram.
- Produces: o `README.md` com a entrega e o registro da verificação manual do Plano 2.

- [ ] **Step 1: Rodar toda a verificação automática**

Run: `npm test; npm run typecheck; npm run build`
Expected:

- API com 30 suítes verdes.
- Web com 19 arquivos de teste verdes.
- `typecheck` sem erros.
- `apps/api/dist/` e `apps/web/dist/` gerados.
- `git grep -n "TODO(PREMISSA-CLINICA"` lista só PC-01 a PC-04, nos arquivos indicados em `docs/premissas-clinicas.md`.

- [ ] **Step 2: Subir o ambiente**

```powershell
npm run db:up
Set-Location apps/api; npx prisma migrate deploy; npx prisma generate; Set-Location ../..
```

Em dois terminais, rode `npm run dev -w @psm/api` e `npm run dev -w @psm/web`, e abra `http://localhost:5173`. Os usuários `admin`, `prof` e `assist` do roteiro do Plano 1 continuam valendo. Se não existirem mais, crie-os em Usuários.

- [ ] **Step 3: PONTO DE CONTROLE DA PESSOA — roteiro manual**

A pessoa executa o roteiro abaixo e anota OK ou a divergência em cada item. Use só dados fictícios.

1. Como `admin`, cadastre os pacientes "Paciente Teste Anamnese" (Masculino, 10/05/1980) e "Paciente Teste Jovem" (Feminino, 15/03/2008).
2. Como `assist`, abra "Paciente Teste Anamnese".
   - Esperado: aparece a seção "Anamneses", com "Nenhuma anamnese registrada.".
   - Clique em "Nova anamnese". Esperado: nenhuma das 22 perguntas está marcada, a data é a de hoje e o fator é 35.
3. Mude a data para 09/05/2026, preencha FC em repouso 70 e escolha "Tanaka et al., 2001".
   - Esperado: idade 45 anos, FC máxima 177 bpm e FC de reserva 107 bpm.
   - Esperado também: a lista de métodos tem 7 itens, sem Hossack.
4. Teste a pressão arterial:

   | PAS/PAD | Classificação esperada |
   |---|---|
   | 150/80 | HA Sistólica Isolada – Estágio 1 |
   | 190/105 | HA Estágio 3 |
   | 170/115 | HA Estágio 3 |
   | 85/70 | Pressão Baixa / Hipotensão |
   | 135/55 | Pressão Baixa / Hipotensão |

   Depois apague a PAD. Esperado: "Informe PAS e PAD para classificar.".
5. Preencha peso 70. Esperado: água ideal 2,45 L. Mude o peso para 60. Esperado: 2,10 L.
6. Marque "Fumante?" Sim e clique em "Limpar resposta". Esperado: a pergunta volta a ficar sem resposta.
7. Escolha a opção de urina "4 — Levemente Desidratado" e clique em "Salvar rascunho".
   - Esperado: "Rascunho salvo." e nenhum botão "Finalizar" para o assistente.
   - Recarregue a página (F5). Esperado: a opção 4 continua marcada.
   - Marque a opção 8, salve e recarregue. Esperado: a 8 continua marcada.
8. Preencha peso 0 e salve. Esperado: o campo mostra "O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.". Volte o peso para 70.
9. Em outra janela anônima, entre como `prof` e abra a mesma anamnese. Clique em "Finalizar" e depois em "Confirmar finalização".
   - Esperado: "Anamnese finalizada." e o formulário somente leitura, com "Anamnese finalizada em …".
10. Volte à janela do `assist` (sem recarregar), mude algo e salve.
    - Esperado: "Esta anamnese já foi finalizada e não pode ser alterada.".
    - Clique em "Carregar versão atual". Esperado: a anamnese aparece somente leitura.
11. Como `prof`, abra "Paciente Teste Jovem" e crie uma anamnese com data 14/03/2026 e PA 150/80.
    - Esperado: "Não se aplica a menores de 18 anos.", e a lista de métodos tem 5 itens, sem Branco.
    - Mude a data para 15/03/2026. Esperado: HA Sistólica Isolada – Estágio 1.
12. Como `admin`, em Permissões, desmarque "Preencher rascunhos de anamnese" e salve. Como `assist`, abra "Paciente Teste Anamnese".
    - Esperado: a seção "Anamneses" não aparece. Se ela estava aberta, mostra "Você não tem permissão para esta ação." na próxima consulta.
    - Ao final do roteiro, marque a permissão de novo.
13. Como `admin`, abra Auditoria. Esperado:
    - `anamnesis.create`, `anamnesis.update` e `anamnesis.finalize` aparecem com autor e horário de Brasília;
    - a finalização mostra a mudança de status de RASCUNHO para FINALIZADA.

Só siga para o Step 4 com os 13 itens OK ou com as divergências aceitas pela pessoa.

- [ ] **Step 4: Atualizar o README**

Em `README.md`:

- na seção "O que o Plano 1 entrega", troque a linha `Fora deste plano: anamnese, avaliações e regras clínicas, …` por `Fora deste plano: avaliações e regras clínicas das avaliações, documentos em PDF, OCR, exclusão de pacientes (política de retenção indefinida) e hospedagem.`;
- acrescente ao fim:

```markdown
## O que o Plano 2 entrega

Anamnese por paciente, com estes itens:

- rascunho (`anamneses.draft` ou `anamneses.finalize`) e finalização (`anamneses.finalize`); a anamnese finalizada é somente leitura;
- 22 perguntas Sim/Não sem resposta pré-marcada e 11 textos;
- FC máxima (9 métodos, por sexo) e de reserva, classificação da pressão arterial, escala de urina e água ideal.

Os cálculos são feitos na API, com regras versionadas (`apps/api/src/clinical-rules/anamnese-rules.ts`); cada anamnese grava a versão usada (`anamnese-v1`). As premissas clínicas provisórias, a validar com o médico responsável, estão em [docs/premissas-clinicas.md](docs/premissas-clinicas.md).

Fora deste plano: reabertura, revisão e exclusão de anamnese, documento PDF da anamnese (Plano 5) e importação de anamneses antigas.

## Verificação do Plano 2

Data: <AAAA-MM-DD>. Roteiro manual da Task 10, itens 1–13: <OK ou divergências aceitas>.
```

Preencha a data e o resultado com o que a pessoa anotou no Step 3.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "docs: entrega e verificação do Plano 2 (anamnese)"
```

---

## Fora do escopo deste plano

- Reabertura, edição ou revisão de anamnese finalizada. A correção depois da finalização entra nas revisões com histórico, num plano posterior; a pessoa decide antes.
- Exclusão de anamnese, inclusive de rascunho, enquanto a política de retenção não estiver definida.
- `history.read`: a consulta de histórico entra com as avaliações (Plano 4).
- Documento PDF da anamnese (Plano 5).
- Importação de anamneses antigas. Se um dia entrar, atenção ao dispositivo de saúde: no legado, o controle 70 é Sim e o 69 é Não, ao contrário das outras perguntas.
- Tabela de faixas editável no banco: as regras mudam só por nova versão no código.
- Validação clínica das premissas PC-01 a PC-04. Ela é feita pelo médico responsável, fora do código; uma correção vira `anamnese-v2`.
