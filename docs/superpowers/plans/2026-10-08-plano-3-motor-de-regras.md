# Plano 3 — Motor de regras das avaliações A, B/R e C (psm-web) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar o motor de regras das avaliações A, B/R e C. Ele cobre:

- faixa etária;
- IMC e classificação;
- gordura (kg, massa livre e classificação);
- composição da A (residual, óssea e muscular) e percentuais da C;
- músculo esquelético e intervalos de referência da B;
- RCQ;
- sugestões e peso-alvo;
- TMB (Harris-Benedict, OMS, DRI e bioimpedância) e GET.

Tudo é calculado na API com a configuração versionada `avaliacao-v1` e exposto por um endpoint de prévia que não grava nada.

**Architecture:** As regras ficam em módulos puros em `apps/api/src/clinical-rules/`, sem Prisma Client, sem Nest e sem I/O. Faixas, coeficientes, textos e limites técnicos vêm de `avaliacao-rules.ts`, que segue o padrão de `anamnese-rules.ts`. Um orquestrador (`calculateAssessment`) monta o resultado de cada protocolo. O novo módulo `assessments` da API valida a entrada contra o cadastro do paciente (idade na data) e responde `POST /api/patients/:patientId/assessments/calculation` com `{ calculation, errors }`, sem gravar nada. As tabelas, a gravação com a versão das regras e as telas ficam para o Plano 4.

**Tech Stack:** a mesma dos Planos 1 e 2, sem dependência nova: Node 24, NestJS 11, Prisma 6, PostgreSQL 16 (Docker, porta 5433, banco `psm_test` nos testes), Jest 29 + Supertest.

**Spec:**
- Divergências e homologação: [divergencias-e-homologacao.md](../../migracao-web/divergencias-e-homologacao.md). DIV-01, 02, 03, 10, 14, 15 e 16; casos H-01 a H-11 e H-23; §5.
- Regras: [catalogo-regras.md](../../migracao-web/catalogo-regras.md): CAD-03, AV-01 a AV-14, CL-01 a CL-08 e EN-01 a EN-04.
- Código legado: [anexo-fontes-regras.md](../../migracao-web/anexo-fontes-regras.md), com as transcrições de Energia.bas, IMC.bas, GORDURA.bas, MUSCULOS.bas, Rcq.bas, Equações_Avaliações.bas, FrmCalcPeso, FrmBioimpC, FrmAvaliacaoA/B e FrmNecessidadeEnergetica.
- Campos: [dicionario-dados.md](../../migracao-web/dicionario-dados.md), "Avaliações — 108 posições A:DD".
- Decisões da pessoa de 2026-10-08: seção "Decisões incorporadas", abaixo.

Os caminhos de código abaixo são relativos a `C:\Sistema\PSM\psm-web`. Os comandos são em PowerShell e rodam nessa pasta, salvo indicação. Este plano foi escrito sobre o código real da branch `master`, no commit `23d7f36` (Planos 1 e 2 concluídos). Todos os valores esperados foram conferidos com um protótipo em Node antes de entrar no plano.

## Decisões incorporadas (2026-10-08)

Todas as decisões abaixo são da pessoa. As clínicas são **provisórias** e ficam a validar com o médico responsável, pelo método das premissas (PC-05 a PC-13). O executor não as reabre. Se algo contradisser uma delas, registre em "Decisões e bloqueios" e pare.

1. **Escopo (fronteira com o Plano 4).** Este plano entrega as regras puras de A, B/R e C, a configuração versionada `avaliacao-v1` e dois endpoints:
   - `GET /api/assessments/rules`;
   - `POST /api/patients/:patientId/assessments/calculation`, que é uma prévia sem gravação.

   Ficam para o Plano 4: tabela de avaliações, gravação com a versão das regras, telas, rascunho, finalização, revisões, classificações manuais da B e da C (CL-08, DIV-05) e o limite de pontuação no formulário.
2. **Arredondamento.** É meio para cima (afastando do zero) em todas as regras, sobre o valor decimal com 15 algarismos significativos, como o `Format` do VBA. Isso vale também onde o legado usava `Round()`, que arredonda o empate para o par. Uma diferença de 0,01, ou de 1 kcal num empate exato, é aceita. Nenhum caso H muda.
3. **Classificação pelo valor arredondado.** Como no legado, o IMC e o RCQ são arredondados a 2 casas antes de classificar. No bloco L, Feminino 25 anos, quadril 100: cintura 82,4 dá 0,82, "Risco Alto"; cintura 82,5 dá 0,83, "Muito Alto".
4. **DIV-01, energia DRI (EN-03).**
   - O IMC interno das equações DRI passa a ser em kg/m², e a comparação com o limiar é exata, por conta inteira.
   - Os limiares do legado continuam: adolescentes, sobrepeso acima de 21,8; adultos, acima de 24,99.
   - O coeficiente da idade no sobrepeso masculino adolescente passa a 33,5 (o legado usa 35,5). A fonte é secundária: a revisão sistemática PMC8685418, Tabela 3, que transcreve a equação do IOM. Isso é premissa (PC-06), e a conferência fica pendente no relatório original.
   - H-23 (masculino, 40 anos, 90 kg, 173 cm, adultos): o legado dá 1875; a web dá 1841.
5. **DIV-02, peso-alvo (AV-12, AV-13).** Só os dados da avaliação corrente entram, nos três protocolos.
   - Pelo método da gordura, exige só peso, gordura atual e gordura-alvo; altura e IMC não são exigidos.
   - Pelo método do IMC, exige IMC-alvo e altura; o peso só entra no controle.
   - H-08: IMC-alvo 22 e 170 cm dão 63,58 kg.
6. **DIV-03.** De 5 a 19 anos, a classificação do IMC é "Não classificado (5 a 19 anos)". A tabela `Imc_CA_CAM` não entra.
7. **DIV-10.** Na C, o kg informado de cada componente é a entrada, e o percentual é derivado: kg ÷ peso × 100, com 2 casas, meio para cima.
   - H-09: água 41,3 kg com 70 kg dá 59,00%; proteína 11,7 kg dá 16,71%.
8. **DIV-14.** A pontuação não tem teto clínico. O limite técnico é um inteiro de 0 a 999, até a definição com amostras do aparelho (PC-13). Este plano só guarda o limite na configuração; o formulário que usa a pontuação é do Plano 4.
9. **DIV-15.** A idade é calculada na data da avaliação, a partir do nascimento, com `ageOnDate` (29/02 faz aniversário em 01/03 nos anos não bissextos).
10. **DIV-16.**
    - Os textos e limites do legado sem referência ficam como premissas, inclusive o IMC idoso > 27 = "Obesidade" e a grafia "ABaixo da Média".
    - O IMC-alvo 22,5 e a gordura-alvo (`TabelaGorduraAlvo`) são sugestões. Quando a entrada traz um alvo, ele prevalece.
11. **Perguntas dos testes no Excel.**
    - **a) RCQ:** sem quadril ou sem cintura, RCQ e classificação ficam vazios.
    - **b) Peso total:** é o peso da avaliação e 100,00% nos três protocolos.
    - **c) Peso-alvo pela gordura:** ver o item 5.
    - **d) Recálculo:** a prévia recalcula tudo a cada mudança, sem botão.
    - **e) IMC na B e na C:** é o informado, vindo do aparelho. A prévia também mostra o calculado (peso/altura²) quando há altura, sem alerta. A classificação continua manual (Plano 4).
    - **f) Peso:** a validação recusa peso ≤ 0; sem peso, o que depende dele fica vazio.
12. **Zeros do legado viram vazio (null), mantendo o texto "N/A" onde o legado escreve:**
    - IMC abaixo de 5 anos: vazio, com "N/A";
    - residual, óssea e muscular fora de 20–59 anos ou sem os dois diâmetros: vazios;
    - gordura-alvo sem faixa na tabela: sem sugestão.
13. **Limites técnicos (não são regra clínica).** Todos aceitam até 2 casas:
    - peso: maior que 0 e até 500 kg;
    - altura: de 30 a 250 cm;
    - circunferências: maiores que 0 e até 300 cm;
    - diâmetros: maiores que 0 e até 200 mm;
    - percentuais: maiores que 0 e menores que 100;
    - kg de componente: maior que 0 e até 500;
    - IMC informado ou alvo: maior que 0 e até 100;
    - fator de atividade: maior que 0 e até 10;
    - TMB informada: maior que 0 e até 10000;
    - pontuação: inteiro de 0 a 999.

    O nível de gordura visceral, que não estava na lista, só exige ser maior que 0, com até 2 casas, sem máximo.
14. **Campos por protocolo (DIV-11).** Um campo que não pertence ao protocolo é recusado com erro no campo e nunca é herdado em silêncio:
    - **A:** gordura % e diâmetros do punho e do fêmur;
    - **B:** gordura %, IMC informado, músculo esquelético % e nível de gordura visceral;
    - **C:** IMC informado e os kg de gordura, massa óssea, proteína, água, massa muscular e músculo esquelético.

## Global Constraints

- Continuam valendo as restrições dos Planos 1 e 2:
  - dados de teste sempre sintéticos;
  - campo ausente é `null`, nunca `0` nem `''`;
  - interface e mensagens em português do Brasil;
  - fuso `America/Sao_Paulo`;
  - a pasta `zanoni` é só leitura durante a execução;
  - o `Bioimpedancia.xlsm` não é alterado.
- Regras clínicas ficam só em `apps/api/src/clinical-rules/`: funções puras que recebem a configuração por parâmetro e nunca importam a versão corrente por dentro. A importação de **tipos** de `@prisma/client` é permitida.
- Faixas, coeficientes, textos, sugestões e limites técnicos ficam só em `apps/api/src/clinical-rules/avaliacao-rules.ts`.
- A versão publicada é imutável: mudar uma regra significa criar `avaliacao-v2`. O teste de instantâneo (snapshot) da `avaliacao-v1` nunca é atualizado com `-u`.
- Todo resultado traz `rulesVersion` (o Plano 4 grava esse valor).
- Toda premissa provisória tem a marcação `// TODO(PREMISSA-CLINICA PC-xx): validar com o médico responsável ...` e uma linha em `docs/premissas-clinicas.md`. A numeração continua de PC-05 a PC-13, e `premissas.spec.ts` cruza as duas listas.
- Todo arredondamento passa por `roundHalfUp` (Task 1). Ficam proibidos `toFixed`, `Math.round(x * 100) / 100` e qualquer outro arredondamento feito à mão nas regras.
- Comparações de limiar sobre um valor calculado e não arredondado, como o IMC interno do DRI, são feitas por conta inteira, com `bmiAbove` (Task 3).
- Testes à prova de mutação:
  - cada limite de tabela é testado no limite e logo abaixo ou acima;
  - cada coeficiente é pinado por um caso cujo resultado muda se o coeficiente mudar;
  - cada regra de arredondamento tem pelo menos um caso que falha com o arredondamento ingênuo.
- Commits sempre com a lista explícita de arquivos. **Nunca use `git add -A`, `git add .` nem `git commit -a`**, porque `AGENTS.md` e `CLAUDE.md` não são versionados.
- Permissões:
  - regras: `assessments.draft` ou `assessments.finalize`;
  - prévia: `patients.read` e mais `assessments.draft` ou `assessments.finalize`.

  As permissões de anamnese não valem para avaliações. Cada combinação é testada de modo a isolar uma permissão por vez.
- A prévia sempre responde 200 com `{ calculation, errors }`, e não há mensagem 400 nova para a web. O 400 genérico do `ValidationPipe` (tipo errado, campo desconhecido) cai no texto genérico de `errorMessage`.

## Review Focus

Estes pontos não aparecem explicitamente na especificação, mas são onde a pessoa mais provavelmente seria prejudicada. Cada um tem teste na tarefa indicada.

1. **Valor decimal exato no empate, que o ponto flutuante põe do lado errado.** Os casos abaixo dariam errado com `Math.round(x * 100) / 100` ou `toFixed`:

   | Entrada | Resultado esperado |
   |---|---|
   | RCQ 62,8/80 | 0,79, "Risco Alto" aos 35 anos, Feminino (e não 0,78, "Risco Moderado") |
   | IMC 40 kg / 160 cm | 15,63 |
   | Gordura 5% de 80,3 kg | 4,02 kg |
   | % da C, 6,72 kg de 51,2 kg | 13,13 |
   | HB Feminino, 58 kg, 151 cm, 29 anos | 1354 |
   | OMS Feminino, 30,95 kg, 25 anos | 950,97 |
   | Residual Feminino, 45 kg | 9,41 |
   | Residual Masculino, 135 kg | 32,54 |
   | Controle de peso | −4,71 (e não −4,709999…) |

   Testes: Tasks 1, 3, 4, 5 e 6.
2. **Valor exatamente no limite de uma tabela, e lacunas do legado.**
   - Classificar depois de arredondar: IMC 73,98 kg / 200 cm dá 18,50, "Peso Normal"; 99,98 / 200 dá 25,00, "Sobrepeso".
   - RCQ Masculino 50–59 anos com `< 0,90`.
   - Gordura-alvo: num valor de lacuna, ela "cai" na faixa etária seguinte, como no legado (Masculino 25 anos com 25% dá 19). Sem faixa nenhuma, fica sem sugestão (Masculino 55 anos com 31%).
   - DRI no limiar exato: 24,99 com 99,96 kg / 200 cm continua "normal"; 21,8 com 49,05 kg / 150 cm também.

   Testes: Tasks 2, 3, 5 e 6.
3. **Idade na fronteira de faixa.**

   | Regra | Idades testadas |
   |---|---|
   | IMC | 4/5, 19/20, 59/60 |
   | Gordura | 17/18, 25/26, 35/36, 45/46, 55/56, 65/66 |
   | RCQ | 19/20, 29/30 … 69/70 |
   | OMS | 2/3, 9/10, 17/18, 29/30, 59/60 |
   | Composição da A | 19/20, 59/60 |
   | Intervalos da B | gordura 19/20, 39/40, 59/60, 79/80; músculo 17/18, 80/81 |
   | Faixa etária | 10/11, 19/20, 59/60 |

   A idade da prévia é calculada na data da avaliação, e não a do cadastro.

   Testes: Tasks 2, 3, 4, 6 e 10.
4. **Dado ausente ou de outro protocolo.**
   - Sem peso, nada que dependa dele é calculado.
   - Sem quadril, o RCQ fica vazio.
   - Um campo da A enviado numa B é recusado no campo.
   - Peso 0 é recusado.
   - Sem método de TMB, não há TMB nem GET.

   Testes: Tasks 7–10.
5. **Permissão.**
   - A prévia exige `patients.read` e mais `assessments.draft` ou `assessments.finalize`.
   - Ter só as permissões de anamnese não basta.
   - As regras não exigem `patients.read`.
   - Sem leitura, a resposta 403 não traz dado do paciente.

   Teste: Task 11.

---

## Mapa de arquivos

```
psm-web/
  docs/premissas-clinicas.md                     + PC-05 a PC-13
  README.md                                      entrega e verificação do Plano 3
  apps/api/src/clinical-rules/
    rounding.ts                                  roundHalfUp e percentOf (meio para cima, 15 algarismos)
    banded-table.ts                              tabelas por idade e por valor (blocos e cortes do legado)
    avaliacao-rules.ts                           configuração versionada avaliacao-v1
    __snapshots__/avaliacao-rules.spec.ts.snap   instantâneo da versão publicada (gerado pelo Jest)
    age-band.ts                                  faixa etária (CAD-03)
    bmi.ts                                       IMC, classificação e comparação exata de limiar
    body-fat.ts                                  classificação e intervalo de gordura
    waist-hip.ts                                 RCQ, classificação e intervalo
    composition.ts                               gordura kg, massa livre, músculo esquelético, composição A
    targets.ts                                   sugestões e peso-alvo
    energy.ts                                    TMB (HB, OMS, DRI, bioimpedância) e GET
    assessment-calculation.ts                    resultado por protocolo (A, B, C)
    premissas.spec.ts                            lista esperada passa a PC-01…PC-13
  apps/api/src/assessments/
    assessment-normalize.ts                      validação, limites e campos por protocolo
    assessment-rules.view.ts                     visão das regras para o Plano 4
    dto/assessment-calculation.dto.ts            corpo da prévia
    assessments.service.ts                       regras e prévia
    assessments.controller.ts                    rotas
    assessments.module.ts
  apps/api/src/app.module.ts                     + AssessmentsModule
  apps/api/test/assessments-calculation.e2e-spec.ts   rotas, erros e permissões
  apps/api/test/assessments-homologacao.e2e-spec.ts   casos H pela API
```

---

## Antes da Task 1

- [ ] **Conferir a base**

```powershell
Set-Location C:\Sistema\PSM\psm-web
git status --short
git switch -c plano-3-motor-de-regras
npm run db:up
npm test -w @psm/api
```

Expected:
- só `AGENTS.md` e `CLAUDE.md` aparecem como não rastreados;
- a branch é criada a partir de `master` em `23d7f36`;
- a API passa com 30 suítes.

- [ ] **Apontar as instruções dos agentes para este plano**

Em `CLAUDE.md` e `AGENTS.md`, que não são rastreados e continuam assim, troque a linha `Plano:` por:

```
Plano: C:/Sistema/PSM/Projeto 14/zanoni/docs/superpowers/plans/2026-10-08-plano-3-motor-de-regras.md
```

---

### Task 1: Arredondamento meio para cima e tabelas por faixas

**Files:**
- Create: `apps/api/src/clinical-rules/rounding.ts`, `apps/api/src/clinical-rules/banded-table.ts`
- Test: `apps/api/src/clinical-rules/rounding.spec.ts`, `apps/api/src/clinical-rules/banded-table.spec.ts`

**Interfaces:**
- Produces:
  - em `rounding.ts`: `roundHalfUp(value: number, decimals: number): number` e `percentOf(part: number, whole: number): number`;
  - em `banded-table.ts`: os tipos `Comparison`, `Cut<T>`, `AgeBlock<T>` e `AgeText`, e as funções `cut`, `compare`, `firstCut`, `evaluateAgeBlocks` e `textForAge`, com estas assinaturas:

```ts
type Comparison = '<' | '<=' | '>' | '>=';
interface Cut<T> { op: Comparison; limit: number; value: T }
interface AgeBlock<T> { ageOp: Comparison; ageLimit: number; cuts: readonly Cut<T>[] }
interface AgeText { ageOp: Comparison; ageLimit: number; text: string }
function cut<T>(op: Comparison, limit: number, value: T): Cut<T>
function compare(x: number, op: Comparison, limit: number): boolean
function firstCut<T>(cuts: readonly Cut<T>[], value: number): T | null
function evaluateAgeBlocks<T>(blocks: readonly AgeBlock<T>[], ageYears: number, value: number): T | null
function textForAge(rules: readonly AgeText[], ageYears: number): string | null
```

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/rounding.spec.ts`:

```ts
import { percentOf, roundHalfUp } from './rounding';

describe('roundHalfUp (meio para cima, como o Format do VBA)', () => {
  it.each([
    // [descrição, valor calculado em ponto flutuante, casas, esperado]
    ['RCQ 82,5/100 (bloco L)', 82.5 / 100, 2, 0.83],
    ['RCQ 62,8/80 = 0,785', 62.8 / 80, 2, 0.79],
    ['IMC 40/1,6² = 15,625', 40 / 1.6 ** 2, 2, 15.63],
    ['gordura 5% de 80,3 = 4,015', (5 / 100) * 80.3, 2, 4.02],
    ['17,4 de 32 = 54,375%', (17.4 / 32) * 100, 2, 54.38],
    ['HB 1353,5 a inteiro', 655.1 + 9.563 * 58 + 1.85 * 151 - 4.676 * 29, 0, 1354],
    ['OMS 950,965', 14.7 * 30.95 + 496, 2, 950.97],
    ['residual 45 × 0,209 = 9,405', 45 * 0.209, 2, 9.41],
  ])('%s', (_label, value, decimals, expected) => {
    expect(roundHalfUp(value, decimals)).toBe(expected);
  });

  it('o arredondamento ingênuo erraria esses mesmos casos (o teste acima é o que protege)', () => {
    expect(Math.round((62.8 / 80) * 100) / 100).toBe(0.78);
    expect((82.5 / 100).toFixed(2)).toBe('0.82');
  });

  it('negativo afasta do zero; sem empate, arredonda normalmente', () => {
    expect(roundHalfUp(75.29 - 80, 2)).toBe(-4.71);
    expect(roundHalfUp(-0.825, 2)).toBe(-0.83);
    expect(roundHalfUp(16.714285714285715, 2)).toBe(16.71);
    expect(roundHalfUp(1341.6, 0)).toBe(1342);
    expect(roundHalfUp(2.5, 0)).toBe(3);
    expect(roundHalfUp(0, 2)).toBe(0);
    expect(Object.is(roundHalfUp(-0.001, 2), -0)).toBe(false);
  });

  it('recusa valor não finito', () => {
    expect(() => roundHalfUp(Number.NaN, 2)).toThrow('Valor não finito.');
    expect(() => roundHalfUp(Infinity, 2)).toThrow('Valor não finito.');
  });
});

describe('percentOf', () => {
  it('parte ÷ todo × 100, 2 casas, meio para cima', () => {
    expect(percentOf(41.3, 70)).toBe(59);
    expect(percentOf(11.7, 70)).toBe(16.71);
    expect(percentOf(6.72, 51.2)).toBe(13.13);
    expect(percentOf(16, 80)).toBe(20);
  });
});
```

`apps/api/src/clinical-rules/banded-table.spec.ts`:

```ts
import { compare, cut, evaluateAgeBlocks, firstCut, textForAge, type AgeBlock } from './banded-table';

describe('compare', () => {
  it.each([
    [1, '<', 2, true], [2, '<', 2, false],
    [2, '<=', 2, true], [3, '<=', 2, false],
    [3, '>', 2, true], [2, '>', 2, false],
    [2, '>=', 2, true], [1, '>=', 2, false],
  ] as const)('%p %s %p → %p', (x, op, limit, expected) => {
    expect(compare(x, op, limit)).toBe(expected);
  });
});

describe('firstCut', () => {
  const cuts = [cut('<', 10, 'a'), cut('<=', 20, 'b'), cut('>', 20, 'c')];
  it('o primeiro corte que casa vence', () => {
    expect([9.99, 10, 20, 20.01].map((v) => firstCut(cuts, v))).toEqual(['a', 'b', 'b', 'c']);
  });
  it('sem corte que case, null', () => {
    expect(firstCut([cut('<', 10, 'a')], 10)).toBeNull();
  });
});

describe('evaluateAgeBlocks (blocos "If idade ... Then" em sequência, como no legado)', () => {
  const blocks: AgeBlock<string>[] = [
    { ageOp: '<=', ageLimit: 25, cuts: [cut('<', 10, 'jovem baixo'), cut('>', 10, 'jovem alto')] },
    { ageOp: '<=', ageLimit: 35, cuts: [cut('<', 15, 'adulto baixo'), cut('>=', 15, 'adulto alto')] },
  ];

  it('usa o primeiro bloco cuja idade casa', () => {
    expect(evaluateAgeBlocks(blocks, 20, 5)).toBe('jovem baixo');
    expect(evaluateAgeBlocks(blocks, 30, 5)).toBe('adulto baixo');
  });

  it('valor sem corte no bloco segue para o próximo bloco cuja idade também casa (lacuna do legado)', () => {
    expect(evaluateAgeBlocks(blocks, 20, 10)).toBe('adulto baixo');
  });

  it('sem bloco nem corte que case, null', () => {
    expect(evaluateAgeBlocks(blocks, 36, 5)).toBeNull();
  });
});

describe('textForAge', () => {
  const rules = [
    { ageOp: '<', ageLimit: 20, text: 'N/A' },
    { ageOp: '<=', ageLimit: 39, text: '8 - 19,9' },
    { ageOp: '>', ageLimit: 39, text: 'outro' },
  ] as const;
  it('primeira regra que casa', () => {
    expect([19, 20, 39, 40].map((age) => textForAge(rules, age))).toEqual(['N/A', '8 - 19,9', '8 - 19,9', 'outro']);
  });
  it('nenhuma, null', () => {
    expect(textForAge([{ ageOp: '<', ageLimit: 20, text: 'N/A' }], 20)).toBeNull();
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules/rounding src/clinical-rules/banded-table`
Expected: FAIL com `Cannot find module './rounding'` e `'./banded-table'`.

- [ ] **Step 3: Implementar `rounding.ts`**

```ts
/**
 * Arredonda a `decimals` casas, meio para cima (afastando do zero), sobre o valor decimal com 15 algarismos
 * significativos — o mesmo que o Format "#,##0.00" do VBA faz. Assim, 82,5/100 (0,82499999… em ponto flutuante) vira
 * 0,83, como no legado (bloco L). Decisão de 2026-10-08: vale também onde o legado usava Round(), que levava o empate
 * para o par.
 */
export function roundHalfUp(value: number, decimals: number): number {
  if (!Number.isFinite(value)) throw new Error('Valor não finito.');
  if (Math.abs(value) >= 1e15) throw new Error('Valor fora da faixa de arredondamento.');
  if (Math.abs(value) < 1e-6) return 0;
  const sign = value < 0 ? -1 : 1;
  const digits = Math.abs(value).toPrecision(15);
  const shifted = Math.round(Number(`${digits}e${decimals}`));
  const result = sign * Number(`${shifted}e-${decimals}`);
  return result === 0 ? 0 : result; // nunca -0
}

/** Percentual de `part` em `whole` (parte ÷ todo × 100), 2 casas, meio para cima. */
export function percentOf(part: number, whole: number): number {
  return roundHalfUp((part / whole) * 100, 2);
}
```

- [ ] **Step 4: Implementar `banded-table.ts`**

Exporte os tipos e as funções da interface.

- `cut` só monta o objeto.
- `compare` aplica o operador.
- `firstCut` devolve o `value` do primeiro corte que casa, ou `null`.
- `textForAge` devolve o `text` da primeira regra cuja idade casa, ou `null`.
- `evaluateAgeBlocks` precisa deste comentário, porque o algoritmo não é óbvio:

```ts
/**
 * Avalia blocos de idade como o legado: blocos "If idade ... Then" em sequência, sem ElseIf. Dentro de um bloco cuja
 * idade casa, o primeiro corte que casa encerra. Se nenhum corte casa, o valor segue para o próximo bloco cuja idade
 * também case — é assim que a gordura-alvo de 25% aos 25 anos (masculino) vira 19 no legado. Sem nada, null.
 */
export function evaluateAgeBlocks<T>(blocks: readonly AgeBlock<T>[], ageYears: number, value: number): T | null {
  for (const block of blocks) {
    if (!compare(ageYears, block.ageOp, block.ageLimit)) continue;
    const found = firstCut(block.cuts, value);
    if (found !== null) return found;
  }
  return null;
}
```

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules/rounding src/clinical-rules/banded-table`
Expected: PASS — `rounding.spec` (12), `banded-table.spec` (15).

- [ ] **Step 6: Commit**

```powershell
git add apps/api/src/clinical-rules/rounding.ts apps/api/src/clinical-rules/rounding.spec.ts apps/api/src/clinical-rules/banded-table.ts apps/api/src/clinical-rules/banded-table.spec.ts
git commit -m "feat(api): arredondamento meio para cima e tabelas por faixas do legado"
```

---

### Task 2: Configuração versionada `avaliacao-v1`, faixa etária e premissas PC-05 a PC-13

**Files:**
- Create: `apps/api/src/clinical-rules/avaliacao-rules.ts`, `apps/api/src/clinical-rules/age-band.ts`
- Modify: `docs/premissas-clinicas.md`, `apps/api/src/clinical-rules/premissas.spec.ts:25`
- Test: `apps/api/src/clinical-rules/avaliacao-rules.spec.ts` (gera `__snapshots__/avaliacao-rules.spec.ts.snap`), `apps/api/src/clinical-rules/age-band.spec.ts`

**Interfaces:**
- Consumes: `cut`, `AgeBlock`, `AgeText` e `Comparison` (Task 1).
- Produces:
  - os tipos `AssessmentProtocol = 'A' | 'B' | 'C'`, `EnergyMethod = 'HARRIS_BENEDICT' | 'OMS' | 'DRI_ADOLESCENTES' | 'DRI_ADULTOS' | 'BIOIMPEDANCIA'`, `HarrisBenedictEquation`, `DriEquation`, `OmsBand`, `Range`, `LimitKey` e `AvaliacaoRules`;
  - as constantes `AVALIACAO_RULES_V1`, `AVALIACAO_RULES_BY_VERSION` e `CURRENT_AVALIACAO_RULES`;
  - `avaliacaoRulesForVersion(version: string): AvaliacaoRules`;
  - `ageBand(rules: AvaliacaoRules, ageYears: number): string`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/avaliacao-rules.spec.ts`:

```ts
import {
  AVALIACAO_RULES_BY_VERSION, AVALIACAO_RULES_V1, avaliacaoRulesForVersion, CURRENT_AVALIACAO_RULES,
} from './avaliacao-rules';

describe('regras das avaliações versionadas', () => {
  it('a versão corrente é a avaliacao-v1, provisória', () => {
    expect(CURRENT_AVALIACAO_RULES).toBe(AVALIACAO_RULES_V1);
    expect(avaliacaoRulesForVersion('avaliacao-v1')).toBe(AVALIACAO_RULES_V1);
    expect(Object.keys(AVALIACAO_RULES_BY_VERSION)).toEqual(['avaliacao-v1']);
    expect(AVALIACAO_RULES_V1).toMatchObject({ version: 'avaliacao-v1', effectiveFrom: '2026-10-08', status: 'PROVISORIA' });
  });

  it('versão desconhecida é erro', () => {
    expect(() => avaliacaoRulesForVersion('avaliacao-v0')).toThrow('Versão de regras de avaliação desconhecida: avaliacao-v0');
  });

  it('a versão publicada não muda em execução', () => {
    expect(Object.isFrozen(AVALIACAO_RULES_V1)).toBe(true);
    expect(Object.isFrozen(AVALIACAO_RULES_V1.fat.classification.FEMININO[3].cuts[0])).toBe(true);
    expect(Object.isFrozen(AVALIACAO_RULES_V1.energy.oms.MASCULINO)).toBe(true);
    expect(Object.isFrozen(AVALIACAO_RULES_V1.limits.bodyScore)).toBe(true);
  });

  // Instantâneo da versão publicada. Se falhar, NÃO rode jest -u: crie a avaliacao-v2.
  it('avaliacao-v1: conteúdo publicado', () => {
    expect(JSON.stringify(AVALIACAO_RULES_V1)).toMatchSnapshot();
  });

  it('valores que o plano fixa', () => {
    const r = AVALIACAO_RULES_V1;
    expect(r.targets.suggestedBmi).toBe(22.5);
    expect(r.energy.activityFactors).toEqual([1, 1.2, 1.3, 1.4, 1.5, 1.55, 1.56, 1.6, 1.64, 1.78, 1.8, 1.82, 1.9, 2.1, 2.2, 2.5, 3, 3.5, 4, 5, 6]);
    expect(r.energy.defaultActivityFactor).toBe(1);
    expect(r.energy.driAdolescents.overweight.MASCULINO).toEqual({ constant: 419.9, perYear: -33.5, perMeter: 418.9, perKg: 16.7 });
    expect(r.energy.driAdolescents.overweightAboveBmi).toBe(21.8);
    expect(r.energy.driAdults.overweightAboveBmi).toBe(24.99);
    expect(r.bmi.childLabel).toBe('Não classificado (5 a 19 anos)');
    expect(r.limits.bodyScore).toEqual({ min: 0, minInclusive: true, max: 999, maxInclusive: true, integer: true });
    expect(r.limits.visceralFatLevel).toEqual({ min: 0, minInclusive: false, max: null, maxInclusive: false, integer: false });
  });
});
```

`apps/api/src/clinical-rules/age-band.spec.ts`:

```ts
import { ageBand } from './age-band';
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';

describe('faixa etária (CAD-03)', () => {
  it.each([
    [0, 'Crianças'], [10, 'Crianças'], [11, 'Adolescentes'], [19, 'Adolescentes'],
    [20, 'Adultos'], [59, 'Adultos'], [60, 'Idosos'], [100, 'Idosos'],
  ])('%i anos → %s', (age, band) => {
    expect(ageBand(RULES, age)).toBe(band);
  });
});
```

Em `apps/api/src/clinical-rules/premissas.spec.ts`, linha 25, troque a lista esperada por:

```ts
    expect(documented).toEqual([
      'PC-01', 'PC-02', 'PC-03', 'PC-04', 'PC-05', 'PC-06', 'PC-07', 'PC-08', 'PC-09', 'PC-10', 'PC-11', 'PC-12', 'PC-13',
    ]);
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: FAIL. Os imports `./avaliacao-rules` e `./age-band` não existem, e `premissas.spec` recebe só PC-01 a PC-04.

- [ ] **Step 3: Implementar `avaliacao-rules.ts`**

Escreva o arquivo como está. Os valores vêm do anexo de fontes, com as correções decididas.

```ts
import type { Sex } from '@prisma/client';
import { cut, type AgeBlock, type AgeText, type Comparison, type Cut } from './banded-table';

export type AssessmentProtocol = 'A' | 'B' | 'C';
export type EnergyMethod = 'HARRIS_BENEDICT' | 'OMS' | 'DRI_ADOLESCENTES' | 'DRI_ADULTOS' | 'BIOIMPEDANCIA';

/** constant + perKg × peso + perCm × altura (cm) + perYear × idade; os sinais estão nos coeficientes. */
export interface HarrisBenedictEquation { constant: number; perKg: number; perCm: number; perYear: number }
/** constant + perYear × idade + perMeter × altura (m) + perKg × peso. */
export interface DriEquation { constant: number; perYear: number; perMeter: number; perKg: number }
/** perKg × peso + constant, para idade abaixo de `belowAge` (null = sem limite). */
export interface OmsBand { belowAge: number | null; perKg: number; constant: number }
/** Limite técnico de digitação (não é regra clínica). `max` null = sem máximo. */
export interface Range { min: number; minInclusive: boolean; max: number | null; maxInclusive: boolean; integer: boolean }

export type LimitKey =
  | 'weightKg' | 'heightCm' | 'circumferenceCm' | 'diameterMm' | 'percent' | 'componentKg' | 'bmi'
  | 'visceralFatLevel' | 'bmrKcal' | 'activityFactor' | 'bodyScore';

type BySex<T> = Readonly<Record<Sex, T>>;

export interface AvaliacaoRules {
  version: string;
  effectiveFrom: string;
  status: 'PROVISORIA' | 'VALIDADA';
  /** Primeira faixa com idade abaixo de `belowAge` (null = demais). */
  ageBands: readonly { belowAge: number | null; label: string }[];
  bmi: {
    notApplicableBelowAge: number;
    notApplicableLabel: string;
    adultFromAge: number;
    childLabel: string;
    elderlyFromAge: number;
    adult: readonly Cut<string>[];
    elderly: readonly Cut<string>[];
    /** Intervalo exibido na B quando há IMC informado. */
    reference: string;
  };
  fat: { classification: BySex<readonly AgeBlock<string>[]>; reference: BySex<readonly AgeText[]> };
  skeletalMuscle: { reference: BySex<readonly AgeText[]> };
  visceralFat: { reference: string };
  waistHip: { classification: BySex<readonly AgeBlock<string>[]>; reference: BySex<readonly AgeText[]> };
  compositionA: {
    fromAge: number;
    toAge: number;
    residualFactor: BySex<number>;
    bone: { coefficient: number; scale: number; exponent: number };
  };
  targets: { suggestedBmi: number; suggestedFat: BySex<readonly AgeBlock<number>[]> };
  energy: {
    methods: readonly { id: EnergyMethod; label: string }[];
    harrisBenedict: BySex<HarrisBenedictEquation>;
    oms: BySex<readonly OmsBand[]>;
    driAdolescents: { overweightAboveBmi: number; normal: BySex<DriEquation>; overweight: BySex<DriEquation> };
    driAdults: { overweightAboveBmi: number; normal: BySex<DriEquation>; overweight: BySex<DriEquation> };
    activityFactors: readonly number[];
    defaultActivityFactor: number;
  };
  limits: Readonly<Record<LimitKey, Range>>;
}

function deepFreeze<T>(value: T): T {
  if (typeof value === 'object' && value !== null && !Object.isFrozen(value)) {
    Object.freeze(value);
    for (const child of Object.values(value)) deepFreeze(child);
  }
  return value;
}

const FAT_ADULT = ['Excelente', 'Bom', 'Acima da Média', 'Média', 'ABaixo da Média', 'Ruim', 'Muito Ruim'] as const;
const FAT_YOUNG = ['Muito Baixo', 'Baixo', 'Ótimo', 'Moderadamente Alto', 'Alto', 'Muito Alto'] as const;

/** Bloco adulto de TabelaGordura: seis cortes "<" e, no último limite, ">=". */
function fatAdultBlock(maxAge: number, limits: readonly number[]): AgeBlock<string> {
  return {
    ageOp: '<=',
    ageLimit: maxAge,
    cuts: [...limits.map((limit, i) => cut('<', limit, FAT_ADULT[i])), cut('>=', limits[5], FAT_ADULT[6])],
  };
}

/** Bloco "< 18 anos" de TabelaGordura: "<" no primeiro limite, "<=" nos seguintes e ">" no último. */
function fatYoungBlock(limits: readonly number[]): AgeBlock<string> {
  return {
    ageOp: '<',
    ageLimit: 18,
    cuts: [cut('<', limits[0], FAT_YOUNG[0]), ...limits.slice(1).map((limit, i) => cut('<=', limit, FAT_YOUNG[i + 1])), cut('>', limits[4], FAT_YOUNG[5])],
  };
}

const NOT_APPLICABLE = (ageOp: Comparison, ageLimit: number): AgeBlock<string> => ({ ageOp, ageLimit, cuts: [cut('>=', 0, 'N/A')] });

/** Bloco de TabelaRCQ: baixo, moderado e alto com "<=" (o masculino de 50–59 usa "<" no baixo) e "Muito Alto" acima. */
function waistHipBlock(maxAge: number, low: number, moderate: number, high: number, lowOp: Comparison = '<='): AgeBlock<string> {
  return {
    ageOp: '<=',
    ageLimit: maxAge,
    cuts: [cut(lowOp, low, 'Risco Baixo'), cut('<=', moderate, 'Risco Moderado'), cut('<=', high, 'Risco Alto'), cut('>', high, 'Muito Alto')],
  };
}

const ageText = (ageOp: Comparison, ageLimit: number, text: string): AgeText => ({ ageOp, ageLimit, text });

/** Bloco de TabelaGorduraAlvo; os cortes vêm na ordem do legado, inclusive os inalcançáveis. */
const targetBlock = (maxAge: number, cuts: readonly Cut<number>[]): AgeBlock<number> => ({ ageOp: '<=', ageLimit: maxAge, cuts });

const range = (min: number, minInclusive: boolean, max: number | null, maxInclusive: boolean, integer = false): Range => ({
  min, minInclusive, max, maxInclusive, integer,
});

/**
 * Versão publicada: NUNCA altere valores, textos ou limites desta constante. As avaliações gravadas com 'avaliacao-v1'
 * (Plano 4) precisam continuar reproduzíveis. Para mudar uma regra, crie 'avaliacao-v2', registre-a em
 * AVALIACAO_RULES_BY_VERSION, aponte CURRENT_AVALIACAO_RULES para ela e atualize docs/premissas-clinicas.md.
 * Arredondamento (decisão de 2026-10-08): meio para cima em todas as regras, inclusive onde o legado usava Round().
 */
export const AVALIACAO_RULES_V1: AvaliacaoRules = deepFreeze({
  version: 'avaliacao-v1',
  effectiveFrom: '2026-10-08',
  status: 'PROVISORIA',
  ageBands: [
    { belowAge: 11, label: 'Crianças' },
    { belowAge: 20, label: 'Adolescentes' },
    { belowAge: 60, label: 'Adultos' },
    { belowAge: null, label: 'Idosos' },
  ],
  bmi: {
    notApplicableBelowAge: 5,
    notApplicableLabel: 'N/A',
    adultFromAge: 20,
    // TODO(PREMISSA-CLINICA PC-07): validar com o médico responsável "Não classificado (5 a 19 anos)" no lugar de uma tabela pediátrica (DIV-03).
    childLabel: 'Não classificado (5 a 19 anos)',
    elderlyFromAge: 60,
    // TODO(PREMISSA-CLINICA PC-09): validar com o médico responsável os textos e limites do legado sem referência (TabelaIMC, TabelaGordura, TabelaRCQ e intervalos da B).
    adult: [
      cut('<', 18.5, 'Abaixo do Peso'),
      cut('<', 25, 'Peso Normal'),
      cut('<', 30, 'Sobrepeso'),
      cut('<', 35, 'Obesidade Classe I'),
      cut('<', 40, 'Obesidade Classe II'),
      cut('>=', 40, 'Obesidade Classe III'),
    ],
    // TODO(PREMISSA-CLINICA PC-08): validar com o médico responsável o IMC do idoso, em especial > 27 = "Obesidade" (Lipschitz usa "sobrepeso").
    elderly: [cut('<', 22, 'Desnutrição'), cut('<=', 27, 'Eutrofia - Peso Normal'), cut('>', 27, 'Obesidade')],
    reference: '18,5 - 24,9',
  },
  fat: {
    classification: {
      MASCULINO: [
        fatYoungBlock([5, 10, 20, 25, 31]),
        fatAdultBlock(25, [7, 11, 13.5, 16.5, 20, 25]),
        fatAdultBlock(35, [11.5, 15.5, 18, 21, 24.5, 28]),
        fatAdultBlock(45, [15, 18.5, 21, 23.5, 26, 29.5]),
        fatAdultBlock(55, [17, 20.5, 23.5, 25.5, 27.5, 31]),
        fatAdultBlock(65, [19, 21.5, 23.5, 25.5, 27.5, 31]),
        NOT_APPLICABLE('>', 65),
      ],
      FEMININO: [
        fatYoungBlock([12, 15, 25, 30, 36]),
        fatAdultBlock(25, [16.5, 19.5, 22.5, 25.5, 28.5, 32]),
        fatAdultBlock(35, [17, 20.5, 23.5, 26, 30, 34.5]),
        fatAdultBlock(45, [19.5, 23.5, 26.5, 29.5, 32.5, 37]),
        fatAdultBlock(55, [22, 25.5, 28.5, 31.5, 34.5, 38.5]),
        fatAdultBlock(65, [23, 26.5, 29.5, 32.5, 35.5, 38.5]),
        NOT_APPLICABLE('>', 65),
      ],
    },
    reference: {
      MASCULINO: [ageText('<', 20, 'N/A'), ageText('<=', 39, '8 - 19,9'), ageText('<=', 59, '11 - 21,9'), ageText('<=', 79, '13 - 24,9'), ageText('>', 79, 'N/A')],
      FEMININO: [ageText('<', 20, 'N/A'), ageText('<=', 39, '21 - 32,9'), ageText('<=', 59, '23 - 33,9'), ageText('<=', 79, '24 - 35,9'), ageText('>', 79, 'N/A')],
    },
  },
  skeletalMuscle: {
    reference: {
      MASCULINO: [ageText('<', 18, 'N/A'), ageText('<=', 39, '33,3 - 39,3'), ageText('<=', 59, '33,1 - 39,1'), ageText('<=', 80, '32,9 - 38,9'), ageText('>', 80, 'N/A')],
      FEMININO: [ageText('<', 18, 'N/A'), ageText('<=', 39, '24,3 - 30,3'), ageText('<=', 59, '24,1 - 30,1'), ageText('<=', 80, '23,9 - 29,9'), ageText('>', 80, 'N/A')],
    },
  },
  visceralFat: { reference: '<= 9' },
  waistHip: {
    classification: {
      MASCULINO: [
        NOT_APPLICABLE('<', 20),
        waistHipBlock(29, 0.83, 0.88, 0.94),
        waistHipBlock(39, 0.84, 0.91, 0.96),
        waistHipBlock(49, 0.88, 0.95, 1),
        waistHipBlock(59, 0.9, 0.96, 1.02, '<'),
        waistHipBlock(69, 0.91, 0.98, 1.03),
        NOT_APPLICABLE('>', 69),
      ],
      FEMININO: [
        NOT_APPLICABLE('<', 20),
        waistHipBlock(29, 0.71, 0.77, 0.82),
        waistHipBlock(39, 0.72, 0.78, 0.84),
        waistHipBlock(49, 0.73, 0.79, 0.87),
        waistHipBlock(59, 0.74, 0.81, 0.88),
        waistHipBlock(69, 0.76, 0.83, 0.9),
        NOT_APPLICABLE('>', 69),
      ],
    },
    reference: {
      MASCULINO: [
        ageText('<', 20, 'N/A'), ageText('<=', 29, '0,83 - 0,88'), ageText('<=', 39, '0,84 - 0,91'), ageText('<=', 49, '0,88 - 0,95'),
        ageText('<=', 59, '0,90 - 0,96'), ageText('<=', 69, '0,91 - 0,98'), ageText('>', 69, 'N/A'),
      ],
      FEMININO: [
        ageText('<', 20, 'N/A'), ageText('<=', 29, '0,71 - 0,77'), ageText('<=', 39, '0,72 - 0,78'), ageText('<=', 49, '0,73 - 0,79'),
        ageText('<=', 59, '0,74 - 0,81'), ageText('<=', 69, '0,76 - 0,83'), ageText('>', 69, 'N/A'),
      ],
    },
  },
  compositionA: {
    // TODO(PREMISSA-CLINICA PC-11): validar com o médico responsável residual 24,1%/20,9% do peso, óssea 3,02 × (altura² × punho × fêmur × 400)^0,712 com diâmetros em mm e a faixa de 20 a 59 anos.
    fromAge: 20,
    toAge: 59,
    residualFactor: { MASCULINO: 0.241, FEMININO: 0.209 },
    bone: { coefficient: 3.02, scale: 400, exponent: 0.712 },
  },
  targets: {
    // TODO(PREMISSA-CLINICA PC-10): validar com o médico responsável as sugestões IMC-alvo 22,5 e gordura-alvo (TabelaGorduraAlvo), com as lacunas do legado preservadas.
    suggestedBmi: 22.5,
    suggestedFat: {
      MASCULINO: [
        targetBlock(25, [cut('<', 7, 5), cut('<', 11, 9), cut('<', 14, 12.5), cut('<=', 17, 15), cut('<', 20, 15), cut('<', 25, 15), cut('>', 25, 15)]),
        targetBlock(35, [cut('<', 11.5, 9.5), cut('<', 15.5, 13.5), cut('<', 18, 17), cut('<', 21, 19), cut('<', 24, 19), cut('<', 27.5, 19), cut('>', 27.5, 19)]),
        targetBlock(45, [cut('<', 15, 12), cut('<', 18.5, 17), cut('<', 21, 20), cut('<', 23.5, 22), cut('<', 26, 22), cut('<', 29.5, 22), cut('>', 29.5, 22)]),
        targetBlock(55, [cut('<', 17, 14), cut('<', 20.5, 19), cut('<', 23.5, 22), cut('<', 23.5, 25), cut('<', 25.5, 25), cut('<', 27.5, 25), cut('>', 31, 25)]),
        targetBlock(65, [cut('<', 19, 16), cut('<', 21.5, 20.5), cut('<', 23.5, 22.5), cut('<', 25.5, 24.5), cut('<', 27.5, 24.5), cut('<', 31, 24.5), cut('>', 31, 24.5)]),
      ],
      FEMININO: [
        targetBlock(25, [cut('<', 16.5, 14.5), cut('<', 19.5, 18), cut('<', 22.5, 21), cut('<=', 25.5, 24), cut('<', 28.5, 24), cut('<', 32, 24), cut('>', 32, 24)]),
        targetBlock(35, [cut('<', 17, 15), cut('<', 20.5, 19), cut('<', 23.5, 22), cut('<', 26, 24.5), cut('<', 30, 24.5), cut('<', 34.5, 24.5), cut('>', 34.5, 24.5)]),
        targetBlock(45, [cut('<', 19.5, 17.5), cut('<', 18.5, 21.5), cut('<', 26.5, 25), cut('<', 29.5, 28), cut('<', 32.5, 28), cut('<', 37, 28), cut('>', 37, 28)]),
        targetBlock(55, [cut('<', 22, 19), cut('<', 25.5, 24), cut('<', 28.5, 27), cut('<', 31.5, 30), cut('<', 34.5, 30), cut('<', 38.5, 30), cut('>', 38.5, 30)]),
        targetBlock(65, [cut('<', 23, 20), cut('<', 26.5, 25), cut('<', 29.5, 28), cut('<', 32.5, 31), cut('<', 35.5, 31), cut('<', 38.5, 31), cut('>', 38.5, 31)]),
      ],
    },
  },
  energy: {
    // Legendas a conferir com as do FrmNecessidadeEnergetica no Plano 4.
    methods: [
      { id: 'HARRIS_BENEDICT', label: 'Harris-Benedict' },
      { id: 'OMS', label: 'OMS' },
      { id: 'DRI_ADOLESCENTES', label: 'DRI — crianças e adolescentes' },
      { id: 'DRI_ADULTOS', label: 'DRI — adultos' },
      { id: 'BIOIMPEDANCIA', label: 'Bioimpedância (TMB do aparelho)' },
    ],
    // TODO(PREMISSA-CLINICA PC-12): validar com o médico responsável Harris-Benedict, as faixas e coeficientes da OMS e a lista de fatores de atividade do legado (fontes identificadas: Harris & Benedict, 1919; FAO/WHO/UNU, 1985).
    harrisBenedict: {
      FEMININO: { constant: 655.1, perKg: 9.563, perCm: 1.85, perYear: -4.676 },
      MASCULINO: { constant: 66.5, perKg: 13.75, perCm: 5.003, perYear: -6.755 },
    },
    oms: {
      FEMININO: [
        { belowAge: 3, perKg: 61, constant: -51 },
        { belowAge: 10, perKg: 22.5, constant: 499 },
        { belowAge: 18, perKg: 12.2, constant: 746 },
        { belowAge: 30, perKg: 14.7, constant: 496 },
        { belowAge: 60, perKg: 8.7, constant: 829 },
        { belowAge: null, perKg: 10.5, constant: 596 },
      ],
      MASCULINO: [
        { belowAge: 3, perKg: 60.9, constant: -54 },
        { belowAge: 10, perKg: 22.7, constant: 495 },
        { belowAge: 18, perKg: 17.5, constant: 651 },
        { belowAge: 30, perKg: 15.3, constant: 679 },
        { belowAge: 60, perKg: 11.6, constant: 879 },
        { belowAge: null, perKg: 13.5, constant: 487 },
      ],
    },
    // TODO(PREMISSA-CLINICA PC-05): validar com o médico responsável o IMC interno em kg/m² (DIV-01), os limiares do legado (21,8 sem referência; 24,99) e a escolha da equação pelo operador.
    driAdolescents: {
      overweightAboveBmi: 21.8,
      normal: {
        FEMININO: { constant: 189, perYear: -17.6, perMeter: 625, perKg: 7.9 },
        MASCULINO: { constant: 68, perYear: -43.3, perMeter: 712, perKg: 19.2 },
      },
      overweight: {
        FEMININO: { constant: 515.8, perYear: -26.8, perMeter: 347, perKg: 12.4 },
        // TODO(PREMISSA-CLINICA PC-06): validar com o médico responsável 33,5 × idade (fonte secundária PMC8685418, Tabela 3; o legado usa 35,5) no relatório IOM original.
        MASCULINO: { constant: 419.9, perYear: -33.5, perMeter: 418.9, perKg: 16.7 },
      },
    },
    driAdults: {
      overweightAboveBmi: 24.99,
      normal: {
        FEMININO: { constant: 255, perYear: -2.35, perMeter: 361.6, perKg: 9.39 },
        MASCULINO: { constant: 204, perYear: -4, perMeter: 450.5, perKg: 11.69 },
      },
      overweight: {
        FEMININO: { constant: 247, perYear: -2.67, perMeter: 401.5, perKg: 8.6 },
        MASCULINO: { constant: 293, perYear: -3.8, perMeter: 456.4, perKg: 10.12 },
      },
    },
    activityFactors: [1, 1.2, 1.3, 1.4, 1.5, 1.55, 1.56, 1.6, 1.64, 1.78, 1.8, 1.82, 1.9, 2.1, 2.2, 2.5, 3, 3.5, 4, 5, 6],
    defaultActivityFactor: 1,
  },
  limits: {
    weightKg: range(0, false, 500, true),
    heightCm: range(30, true, 250, true),
    circumferenceCm: range(0, false, 300, true),
    diameterMm: range(0, false, 200, true),
    percent: range(0, false, 100, false),
    componentKg: range(0, false, 500, true),
    bmi: range(0, false, 100, true),
    visceralFatLevel: range(0, false, null, false),
    bmrKcal: range(0, false, 10000, true),
    activityFactor: range(0, false, 10, true),
    // TODO(PREMISSA-CLINICA PC-13): validar com o médico responsável o limite técnico da pontuação (inteiro de 0 a 999), sem teto clínico, com amostras do aparelho (DIV-14).
    bodyScore: range(0, true, 999, true, true),
  },
});

export const AVALIACAO_RULES_BY_VERSION: Readonly<Record<string, AvaliacaoRules>> = Object.freeze({
  [AVALIACAO_RULES_V1.version]: AVALIACAO_RULES_V1,
});

/** Versão usada na prévia e, no Plano 4, em toda gravação. */
export const CURRENT_AVALIACAO_RULES: AvaliacaoRules = AVALIACAO_RULES_V1;

export function avaliacaoRulesForVersion(version: string): AvaliacaoRules {
  const rules = AVALIACAO_RULES_BY_VERSION[version];
  if (!rules) throw new Error(`Versão de regras de avaliação desconhecida: ${version}`);
  return rules;
}
```

- [ ] **Step 4: Implementar `age-band.ts`**

`ageBand(rules, ageYears)` devolve o `label` da primeira entrada de `rules.ageBands` com `belowAge === null` ou `ageYears < belowAge`.

- [ ] **Step 5: Acrescentar PC-05 a PC-13 ao backlog**

Em `docs/premissas-clinicas.md`, no passo 2 de "Como validar", troque "(`anamnese-v2` em `apps/api/src/clinical-rules/anamnese-rules.ts`)" por "(`anamnese-v2` ou `avaliacao-v2`, nos arquivos de regras em `apps/api/src/clinical-rules/`)". Depois acrescente ao fim da tabela:

```markdown
| PC-05 | DRI: IMC interno em kg/m² (DIV-01; o legado usava altura em cm² e ficava sempre na equação de peso normal). Limiares do legado: adolescentes com sobrepeso acima de 21,8 (sem referência) e adultos acima de 24,99. A equação (adolescentes ou adultos) é escolhida pelo operador, não pela idade. | Energia.bas (EN-03); decisão da pessoa (DIV-01) | 2026-10-08 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (energy.dri*); `energy.ts` |
| PC-06 | DRI adolescentes, sobrepeso masculino: 419,9 − 33,5 × idade + 418,9 × altura (m) + 16,7 × peso. O legado usa 35,5. Os demais coeficientes do legado coincidem com a mesma fonte (adolescentes) ou com H-23 (adultos). | Fonte secundária: revisão sistemática PMC8685418, Tabela 3 (equações do IOM). Conferir no relatório IOM (Dietary Reference Intakes for Energy…, 2002/2005) | 2026-10-08 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (driAdolescents.overweight.MASCULINO) |
| PC-07 | IMC de 5 a 19 anos: "Não classificado (5 a 19 anos)". A tabela Imc_CA_CAM só entra numa versão futura, com validação clínica. | Decisão da pessoa (DIV-03) | 2026-10-08 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (bmi.childLabel); `bmi.ts` |
| PC-08 | IMC de 60 anos ou mais: < 22 Desnutrição; ≤ 27 Eutrofia - Peso Normal; > 27 "Obesidade" (a classificação de Lipschitz usa "sobrepeso"). | IMCidoso do legado (CL-02); DIV-16 | 2026-10-08 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (bmi.elderly) |
| PC-09 | Textos e limites do legado sem referência registrada: TabelaIMC (adultos); TabelaGordura (inclusive "ABaixo da Média" e N/A acima de 65 anos); TabelaRCQ (inclusive "< 0,90" no masculino de 50–59 e N/A abaixo de 20 e acima de 69); intervalos da B (gordura e músculo esquelético OMR, RCQ, "18,5 - 24,9" do IMC e "<= 9" da gordura visceral; os dois últimos vêm de uma extração anterior do VBA e precisam ser conferidos no Excel). | IMC.bas, GORDURA.bas, MUSCULOS.bas, Rcq.bas (CL-01, CL-03, CL-05, CL-06); DIV-16 | 2026-10-08 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (bmi.adult, fat, waistHip, skeletalMuscle, visceralFat) |
| PC-10 | Sugestões editáveis: IMC-alvo 22,5 e gordura-alvo da TabelaGorduraAlvo, com as lacunas e os cortes inalcançáveis do legado preservados. Um valor numa lacuna cai na faixa etária seguinte (masculino, 25 anos, 25% → 19); sem faixa, não há sugestão (masculino, 55 anos, 31%). | GORDURA.bas (CL-04, AV-14); DIV-16 | 2026-10-08 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (targets) |
| PC-11 | Composição da avaliação A: residual 24,1% (masculino) ou 20,9% (feminino) do peso; óssea = 3,02 × (altura² × punho × fêmur × 400)^0,712, com diâmetros em mm; muscular = peso − (gordura + óssea + residual). Só de 20 a 59 anos e com os dois diâmetros. | Equações_Avaliações.bas (AV-08 a AV-11) | 2026-10-08 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (compositionA) |
| PC-12 | TMB por Harris-Benedict e pela OMS (faixas < 3, < 10, < 18, < 30, < 60 e ≥ 60 anos) e lista de fatores de atividade do legado. Fontes identificadas pela equipe: Harris & Benedict, 1919; FAO/WHO/UNU, 1985. | Energia.bas e FrmNecessidadeEnergetica (EN-01, EN-02, EN-04) | 2026-10-08 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (energy.harrisBenedict, oms, activityFactors) |
| PC-13 | Pontuação sem teto clínico; limite técnico de inteiro de 0 a 999, até a definição com amostras do aparelho. | Decisão da pessoa (DIV-14) | 2026-10-08 | Médico responsável pela clínica (nome a registrar) | Provisória | `avaliacao-rules.ts` (limits.bodyScore) |
```

- [ ] **Step 6: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: PASS — `avaliacao-rules.spec` (5, com o instantâneo escrito nesta primeira execução: "1 snapshot written"), `age-band.spec` (8), `premissas.spec` (1) e as suítes da Task 1. Confira que `apps/api/src/clinical-rules/__snapshots__/avaliacao-rules.spec.ts.snap` foi criado.

- [ ] **Step 7: Commit**

```powershell
git add apps/api/src/clinical-rules/avaliacao-rules.ts apps/api/src/clinical-rules/avaliacao-rules.spec.ts apps/api/src/clinical-rules/__snapshots__/avaliacao-rules.spec.ts.snap apps/api/src/clinical-rules/age-band.ts apps/api/src/clinical-rules/age-band.spec.ts apps/api/src/clinical-rules/premissas.spec.ts docs/premissas-clinicas.md
git commit -m "feat(api): regras das avaliações versionadas (avaliacao-v1), faixa etária e premissas PC-05 a PC-13"
```

---

### Task 3: IMC, gordura e RCQ — classificações e intervalos

**Files:**
- Create: `apps/api/src/clinical-rules/bmi.ts`, `apps/api/src/clinical-rules/body-fat.ts`, `apps/api/src/clinical-rules/waist-hip.ts`
- Test: `apps/api/src/clinical-rules/bmi.spec.ts`, `apps/api/src/clinical-rules/body-fat.spec.ts`, `apps/api/src/clinical-rules/waist-hip.spec.ts`

**Interfaces:**
- Consumes: `roundHalfUp`, `firstCut`, `evaluateAgeBlocks` e `textForAge` (Task 1); `AvaliacaoRules` e `AVALIACAO_RULES_V1` (Task 2).
- Produces:

```ts
// bmi.ts
bmiFromWeightHeight(weightKg: number, heightCm: number): number
classifyBmi(rules: AvaliacaoRules, ageYears: number, bmi: number | null): string | null
bmiAbove(weightKg: number, heightCm: number, threshold: number): boolean
// body-fat.ts
classifyFat(rules: AvaliacaoRules, sex: Sex, ageYears: number, fatPct: number): string | null
fatReference(rules: AvaliacaoRules, sex: Sex, ageYears: number): string | null
skeletalMuscleReference(rules: AvaliacaoRules, sex: Sex, ageYears: number): string | null
// waist-hip.ts
waistHipRatio(waistCm: number | null, hipCm: number | null): number | null
classifyWaistHip(rules: AvaliacaoRules, sex: Sex, ageYears: number, ratio: number): string | null
waistHipReference(rules: AvaliacaoRules, sex: Sex, ageYears: number): string | null
```

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/bmi.spec.ts`:

```ts
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';
import { bmiAbove, bmiFromWeightHeight, classifyBmi } from './bmi';

describe('IMC (AV-01)', () => {
  it('H-02: 80 kg e 200 cm → 20,00', () => {
    expect(bmiFromWeightHeight(80, 200)).toBe(20);
  });

  it('2 casas, meio para cima, sem erro de ponto flutuante: 40 kg / 160 cm = 15,625 → 15,63', () => {
    expect(bmiFromWeightHeight(40, 160)).toBe(15.63);
    expect(bmiFromWeightHeight(70, 175)).toBe(22.86);
  });

  it('classifica o valor arredondado: 18,495 → 18,50 "Peso Normal"; 24,995 → 25,00 "Sobrepeso"', () => {
    expect(bmiFromWeightHeight(73.98, 200)).toBe(18.5);
    expect(classifyBmi(RULES, 30, bmiFromWeightHeight(73.98, 200))).toBe('Peso Normal');
    expect(bmiFromWeightHeight(99.98, 200)).toBe(25);
    expect(classifyBmi(RULES, 30, bmiFromWeightHeight(99.98, 200))).toBe('Sobrepeso');
  });
});

describe('classificação do IMC por idade (AV-02, H-10/H-11)', () => {
  it('abaixo de 5 anos: "N/A", com ou sem IMC', () => {
    expect(classifyBmi(RULES, 4, 20)).toBe('N/A');
    expect(classifyBmi(RULES, 4, null)).toBe('N/A');
  });

  it('5 a 19 anos: "Não classificado (5 a 19 anos)" (DIV-03)', () => {
    expect(classifyBmi(RULES, 5, 20)).toBe('Não classificado (5 a 19 anos)');
    expect(classifyBmi(RULES, 19, 20)).toBe('Não classificado (5 a 19 anos)');
  });

  it('20 a 59 anos: TabelaIMC em cada limite', () => {
    const at = (bmi: number) => classifyBmi(RULES, 20, bmi);
    expect([18.49, 18.5, 24.99, 25, 29.99, 30, 34.99, 35, 39.99, 40].map(at)).toEqual([
      'Abaixo do Peso', 'Peso Normal', 'Peso Normal', 'Sobrepeso', 'Sobrepeso', 'Obesidade Classe I',
      'Obesidade Classe I', 'Obesidade Classe II', 'Obesidade Classe II', 'Obesidade Classe III',
    ]);
    expect(classifyBmi(RULES, 59, 25)).toBe('Sobrepeso');
  });

  it('60 anos ou mais: IMCidoso em cada limite', () => {
    const at = (bmi: number) => classifyBmi(RULES, 60, bmi);
    expect([21.99, 22, 27, 27.01].map(at)).toEqual(['Desnutrição', 'Eutrofia - Peso Normal', 'Eutrofia - Peso Normal', 'Obesidade']);
  });

  it('sem IMC a partir de 5 anos: sem classificação', () => {
    expect(classifyBmi(RULES, 30, null)).toBeNull();
  });
});

describe('bmiAbove (comparação exata para o limiar do DRI)', () => {
  it('IMC exatamente no limiar não está acima; um centésimo de kg a mais, está', () => {
    expect(bmiAbove(99.96, 200, 24.99)).toBe(false);
    expect(bmiAbove(99.97, 200, 24.99)).toBe(true);
    expect(bmiAbove(49.05, 150, 21.8)).toBe(false);
    expect(bmiAbove(49.06, 150, 21.8)).toBe(true);
  });

  it('em kg/m² (o legado usava cm²): 90 kg e 173 cm está acima de 24,99', () => {
    expect(bmiAbove(90, 173, 24.99)).toBe(true);
  });
});
```

`apps/api/src/clinical-rules/body-fat.spec.ts`:

```ts
import type { Sex } from '@prisma/client';
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';
import { classifyFat, fatReference, skeletalMuscleReference } from './body-fat';

const ADULT = ['Excelente', 'Bom', 'Acima da Média', 'Média', 'ABaixo da Média', 'Ruim', 'Muito Ruim'];

/** Limites do anexo (GORDURA.bas), independentes da configuração: [sexo, idade mínima, idade máxima, limites]. */
const ADULT_BLOCKS: [Sex, number, number, number[]][] = [
  ['MASCULINO', 18, 25, [7, 11, 13.5, 16.5, 20, 25]],
  ['MASCULINO', 26, 35, [11.5, 15.5, 18, 21, 24.5, 28]],
  ['MASCULINO', 36, 45, [15, 18.5, 21, 23.5, 26, 29.5]],
  ['MASCULINO', 46, 55, [17, 20.5, 23.5, 25.5, 27.5, 31]],
  ['MASCULINO', 56, 65, [19, 21.5, 23.5, 25.5, 27.5, 31]],
  ['FEMININO', 18, 25, [16.5, 19.5, 22.5, 25.5, 28.5, 32]],
  ['FEMININO', 26, 35, [17, 20.5, 23.5, 26, 30, 34.5]],
  ['FEMININO', 36, 45, [19.5, 23.5, 26.5, 29.5, 32.5, 37]],
  ['FEMININO', 46, 55, [22, 25.5, 28.5, 31.5, 34.5, 38.5]],
  ['FEMININO', 56, 65, [23, 26.5, 29.5, 32.5, 35.5, 38.5]],
];

describe('classificação da gordura (TabelaGordura, CL-03)', () => {
  it.each(ADULT_BLOCKS)('%s de %i a %i anos: cada limite abre a categoria seguinte', (sex, from, to, limits) => {
    for (const age of [from, to]) {
      limits.forEach((limit, i) => {
        expect(classifyFat(RULES, sex, age, Math.round((limit - 0.01) * 100) / 100)).toBe(ADULT[i]);
        expect(classifyFat(RULES, sex, age, limit)).toBe(ADULT[i + 1]);
      });
    }
  });

  it('H-11: Feminino 40 anos, como observado no Excel', () => {
    const at = (pct: number) => classifyFat(RULES, 'FEMININO', 40, pct);
    expect([19.49, 19.5, 29.5, 32.5, 37].map(at)).toEqual(['Excelente', 'Bom', 'ABaixo da Média', 'Ruim', 'Muito Ruim']);
  });

  it('H-11: Masculino 17 anos, tabela de menores de 18 com "<" no primeiro limite e "<=" nos seguintes', () => {
    const at = (pct: number) => classifyFat(RULES, 'MASCULINO', 17, pct);
    expect([4.99, 5, 10, 10.01, 20, 20.01, 25, 25.01, 31, 31.01].map(at)).toEqual([
      'Muito Baixo', 'Baixo', 'Baixo', 'Ótimo', 'Ótimo', 'Moderadamente Alto', 'Moderadamente Alto', 'Alto', 'Alto', 'Muito Alto',
    ]);
  });

  it('Feminino com menos de 18 anos', () => {
    const at = (pct: number) => classifyFat(RULES, 'FEMININO', 0, pct);
    expect([11.99, 12, 15, 15.01, 25, 25.01, 30, 30.01, 36, 36.01].map(at)).toEqual([
      'Muito Baixo', 'Baixo', 'Baixo', 'Ótimo', 'Ótimo', 'Moderadamente Alto', 'Moderadamente Alto', 'Alto', 'Alto', 'Muito Alto',
    ]);
  });

  it('acima de 65 anos: "N/A"; aos 65 ainda classifica', () => {
    expect(classifyFat(RULES, 'MASCULINO', 66, 20)).toBe('N/A');
    expect(classifyFat(RULES, 'FEMININO', 66, 30)).toBe('N/A');
    expect(classifyFat(RULES, 'FEMININO', 65, 38.5)).toBe('Muito Ruim');
  });
});

describe('intervalos da B (CL-06)', () => {
  it.each([
    ['MASCULINO', 19, 'N/A'], ['MASCULINO', 20, '8 - 19,9'], ['MASCULINO', 39, '8 - 19,9'], ['MASCULINO', 40, '11 - 21,9'],
    ['MASCULINO', 59, '11 - 21,9'], ['MASCULINO', 60, '13 - 24,9'], ['MASCULINO', 79, '13 - 24,9'], ['MASCULINO', 80, 'N/A'],
    ['FEMININO', 19, 'N/A'], ['FEMININO', 20, '21 - 32,9'], ['FEMININO', 40, '23 - 33,9'], ['FEMININO', 79, '24 - 35,9'], ['FEMININO', 80, 'N/A'],
  ] as [Sex, number, string][])('gordura: %s %i anos → %s', (sex, age, text) => {
    expect(fatReference(RULES, sex, age)).toBe(text);
  });

  it.each([
    ['MASCULINO', 17, 'N/A'], ['MASCULINO', 18, '33,3 - 39,3'], ['MASCULINO', 40, '33,1 - 39,1'], ['MASCULINO', 80, '32,9 - 38,9'],
    ['MASCULINO', 81, 'N/A'], ['FEMININO', 17, 'N/A'], ['FEMININO', 39, '24,3 - 30,3'], ['FEMININO', 59, '24,1 - 30,1'],
    ['FEMININO', 80, '23,9 - 29,9'], ['FEMININO', 81, 'N/A'],
  ] as [Sex, number, string][])('músculo esquelético: %s %i anos → %s', (sex, age, text) => {
    expect(skeletalMuscleReference(RULES, sex, age)).toBe(text);
  });
});
```

`apps/api/src/clinical-rules/waist-hip.spec.ts`:

```ts
import type { Sex } from '@prisma/client';
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';
import { classifyWaistHip, waistHipRatio, waistHipReference } from './waist-hip';

/** TabelaRCQ do anexo: [sexo, idade mínima, idade máxima, baixo, moderado, alto]. */
const BLOCKS: [Sex, number, number, number, number, number][] = [
  ['MASCULINO', 20, 29, 0.83, 0.88, 0.94],
  ['MASCULINO', 30, 39, 0.84, 0.91, 0.96],
  ['MASCULINO', 40, 49, 0.88, 0.95, 1],
  ['MASCULINO', 60, 69, 0.91, 0.98, 1.03],
  ['FEMININO', 20, 29, 0.71, 0.77, 0.82],
  ['FEMININO', 30, 39, 0.72, 0.78, 0.84],
  ['FEMININO', 40, 49, 0.73, 0.79, 0.87],
  ['FEMININO', 50, 59, 0.74, 0.81, 0.88],
  ['FEMININO', 60, 69, 0.76, 0.83, 0.9],
];

const up = (value: number) => Math.round((value + 0.01) * 100) / 100;

describe('RCQ (AV-05)', () => {
  it('H-03: cintura 80 e quadril 100 → 0,80, "Risco Alto" (Feminino 40)', () => {
    const ratio = waistHipRatio(80, 100);
    expect(ratio).toBe(0.8);
    expect(classifyWaistHip(RULES, 'FEMININO', 40, ratio as number)).toBe('Risco Alto');
  });

  it('bloco L: arredonda a 2 casas antes de classificar (Feminino 25, quadril 100)', () => {
    expect(waistHipRatio(82.4, 100)).toBe(0.82);
    expect(classifyWaistHip(RULES, 'FEMININO', 25, waistHipRatio(82.4, 100) as number)).toBe('Risco Alto');
    expect(waistHipRatio(82.5, 100)).toBe(0.83);
    expect(classifyWaistHip(RULES, 'FEMININO', 25, waistHipRatio(82.5, 100) as number)).toBe('Muito Alto');
  });

  it('62,8/80 = 0,785 → 0,79 "Risco Alto" aos 35 (o arredondamento ingênuo daria 0,78 "Risco Moderado")', () => {
    expect(waistHipRatio(62.8, 80)).toBe(0.79);
    expect(classifyWaistHip(RULES, 'FEMININO', 35, 0.79)).toBe('Risco Alto');
  });

  it('decisão (a): sem quadril ou sem cintura, não há RCQ', () => {
    expect(waistHipRatio(80, null)).toBeNull();
    expect(waistHipRatio(null, 100)).toBeNull();
  });

  it.each(BLOCKS)('%s de %i a %i anos: limites inclusivos', (sex, from, to, low, moderate, high) => {
    for (const age of [from, to]) {
      expect(classifyWaistHip(RULES, sex, age, low)).toBe('Risco Baixo');
      expect(classifyWaistHip(RULES, sex, age, up(low))).toBe('Risco Moderado');
      expect(classifyWaistHip(RULES, sex, age, moderate)).toBe('Risco Moderado');
      expect(classifyWaistHip(RULES, sex, age, up(moderate))).toBe('Risco Alto');
      expect(classifyWaistHip(RULES, sex, age, high)).toBe('Risco Alto');
      expect(classifyWaistHip(RULES, sex, age, up(high))).toBe('Muito Alto');
    }
  });

  it('Masculino 50–59: "< 0,90" no baixo (0,90 já é moderado)', () => {
    for (const age of [50, 59]) {
      expect(classifyWaistHip(RULES, 'MASCULINO', age, 0.89)).toBe('Risco Baixo');
      expect(classifyWaistHip(RULES, 'MASCULINO', age, 0.9)).toBe('Risco Moderado');
      expect(classifyWaistHip(RULES, 'MASCULINO', age, 0.96)).toBe('Risco Moderado');
      expect(classifyWaistHip(RULES, 'MASCULINO', age, 0.97)).toBe('Risco Alto');
      expect(classifyWaistHip(RULES, 'MASCULINO', age, 1.02)).toBe('Risco Alto');
      expect(classifyWaistHip(RULES, 'MASCULINO', age, 1.03)).toBe('Muito Alto');
    }
  });

  it('"N/A" abaixo de 20 e acima de 69 anos', () => {
    for (const sex of ['MASCULINO', 'FEMININO'] as Sex[]) {
      expect(classifyWaistHip(RULES, sex, 19, 0.8)).toBe('N/A');
      expect(classifyWaistHip(RULES, sex, 70, 0.8)).toBe('N/A');
    }
  });

  it.each([
    ['MASCULINO', 19, 'N/A'], ['MASCULINO', 20, '0,83 - 0,88'], ['MASCULINO', 30, '0,84 - 0,91'], ['MASCULINO', 40, '0,88 - 0,95'],
    ['MASCULINO', 50, '0,90 - 0,96'], ['MASCULINO', 69, '0,91 - 0,98'], ['MASCULINO', 70, 'N/A'],
    ['FEMININO', 29, '0,71 - 0,77'], ['FEMININO', 39, '0,72 - 0,78'], ['FEMININO', 49, '0,73 - 0,79'], ['FEMININO', 59, '0,74 - 0,81'],
    ['FEMININO', 60, '0,76 - 0,83'], ['FEMININO', 70, 'N/A'],
  ] as [Sex, number, string][])('intervalo da B: %s %i anos → %s', (sex, age, text) => {
    expect(waistHipReference(RULES, sex, age)).toBe(text);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules/bmi src/clinical-rules/body-fat src/clinical-rules/waist-hip`
Expected: FAIL com `Cannot find module './bmi'`, `'./body-fat'` e `'./waist-hip'`.

- [ ] **Step 3: Implementar `bmi.ts`**

- `bmiFromWeightHeight` devolve `roundHalfUp(weightKg / (heightCm / 100) ** 2, 2)`.
- `classifyBmi` segue esta ordem:
  - abaixo de `notApplicableBelowAge`: `notApplicableLabel`;
  - IMC `null`: `null`;
  - abaixo de `adultFromAge`: `childLabel`;
  - a partir de `elderlyFromAge`: `firstCut(elderly, bmi)`;
  - nos demais casos: `firstCut(adult, bmi)`.
- `bmiAbove` usa conta inteira:

```ts
/**
 * IMC = peso ÷ altura² (kg/m²) acima de `threshold`, sem ponto flutuante. Com W = peso × 100 e H = altura (cm) × 100,
 * inteiros: IMC = W × 10⁶ ÷ H², e IMC > t ⟺ W × 10⁸ > (t × 100) × H². Peso e altura chegam com até 2 casas.
 */
export function bmiAbove(weightKg: number, heightCm: number, threshold: number): boolean {
  const w = Math.round(weightKg * 100);
  const h = Math.round(heightCm * 100);
  const t = Math.round(threshold * 100);
  return w * 1e8 > t * h * h;
}
```

- [ ] **Step 4: Implementar `body-fat.ts` e `waist-hip.ts`**

- `classifyFat` → `evaluateAgeBlocks(rules.fat.classification[sex], ageYears, fatPct)`.
- `fatReference` → `textForAge(rules.fat.reference[sex], ageYears)`.
- `skeletalMuscleReference` → `textForAge(rules.skeletalMuscle.reference[sex], ageYears)`.
- `waistHipRatio` devolve `null` quando falta cintura ou quadril. Nos demais casos, devolve `roundHalfUp(waistCm / hipCm, 2)`.
- `classifyWaistHip` → `evaluateAgeBlocks(rules.waistHip.classification[sex], ageYears, ratio)`.
- `waistHipReference` → `textForAge(rules.waistHip.reference[sex], ageYears)`.

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: PASS — `bmi.spec` (10), `body-fat.spec` (37), `waist-hip.spec` (28), além das suítes anteriores.

- [ ] **Step 6: Commit**

```powershell
git add apps/api/src/clinical-rules/bmi.ts apps/api/src/clinical-rules/bmi.spec.ts apps/api/src/clinical-rules/body-fat.ts apps/api/src/clinical-rules/body-fat.spec.ts apps/api/src/clinical-rules/waist-hip.ts apps/api/src/clinical-rules/waist-hip.spec.ts
git commit -m "feat(api): IMC, gordura e RCQ com classificação pelo valor arredondado e intervalos da B"
```

---

### Task 4: Composição corporal — gordura e massa livre, músculo esquelético, composição da A e percentuais da C

**Files:**
- Create: `apps/api/src/clinical-rules/composition.ts`
- Test: `apps/api/src/clinical-rules/composition.spec.ts`

**Interfaces:**
- Consumes: `roundHalfUp`, `percentOf` e `AvaliacaoRules`.
- Produces:

```ts
interface FatBreakdown { fatKg: number; fatPctOfWeight: number; fatFreeKg: number; fatFreePct: number }
fatBreakdown(weightKg: number, fatPct: number): FatBreakdown
skeletalMuscleKg(weightKg: number, skeletalMusclePct: number): number
interface CompositionA {
  residualKg: number; residualPct: number;
  boneKg: number | null; bonePct: number | null;
  muscleKg: number | null; musclePct: number | null;
}
interface CompositionAInput {
  sex: Sex; ageYears: number; weightKg: number; heightCm: number | null;
  wristDiameterMm: number | null; femurDiameterMm: number | null; fatKg: number | null;
}
compositionA(rules: AvaliacaoRules, input: CompositionAInput): CompositionA | null
```

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/composition.spec.ts`:

```ts
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';
import { compositionA, fatBreakdown, skeletalMuscleKg } from './composition';
import { percentOf } from './rounding';

describe('gordura e massa livre (AV-03), com a sequência de arredondamentos do legado', () => {
  it('H-01: 80 kg e 20% → 16,00 kg; 20,00%; massa livre 64,00 kg e 80,00%', () => {
    expect(fatBreakdown(80, 20)).toEqual({ fatKg: 16, fatPctOfWeight: 20, fatFreeKg: 64, fatFreePct: 80 });
  });

  it('cada etapa usa o valor já arredondado: 5% de 80,3 = 4,015 → 4,02', () => {
    expect(fatBreakdown(80.3, 5)).toEqual({ fatKg: 4.02, fatPctOfWeight: 5.01, fatFreeKg: 76.28, fatFreePct: 94.99 });
  });

  it('17,3% de 70,5 kg', () => {
    expect(fatBreakdown(70.5, 17.3)).toEqual({ fatKg: 12.2, fatPctOfWeight: 17.3, fatFreeKg: 58.3, fatFreePct: 82.7 });
  });
});

describe('músculo esquelético da B (AV-04)', () => {
  it('percentual × peso ÷ 100, 2 casas', () => {
    expect(skeletalMuscleKg(80, 33.3)).toBe(26.64);
    expect(skeletalMuscleKg(80.3, 5)).toBe(4.02);
  });
});

describe('composição da A (AV-08 a AV-11)', () => {
  const man = { sex: 'MASCULINO' as const, ageYears: 40, weightKg: 80, heightCm: 175, wristDiameterMm: 60, femurDiameterMm: 95, fatKg: 16 };

  it('masculino: residual 24,1% do peso; óssea pela fórmula com diâmetros em mm; muscular = peso − (gordura + óssea + residual)', () => {
    expect(compositionA(RULES, man)).toEqual({
      residualKg: 19.28, residualPct: 24.1, boneKg: 12.05, bonePct: 15.06, muscleKg: 32.67, musclePct: 40.84,
    });
  });

  it('feminino: residual 20,9%', () => {
    expect(
      compositionA(RULES, { sex: 'FEMININO', ageYears: 30, weightKg: 60, heightCm: 165, wristDiameterMm: 50, femurDiameterMm: 90, fatKg: 15.6 }),
    ).toEqual({ residualKg: 12.54, residualPct: 20.9, boneKg: 9.36, bonePct: 15.6, muscleKg: 22.5, musclePct: 37.5 });
  });

  it('residual arredondado meio para cima: Feminino 45 kg = 9,405 → 9,41; Masculino 135 kg = 32,535 → 32,54', () => {
    expect(compositionA(RULES, { ...man, sex: 'FEMININO', weightKg: 45 })?.residualKg).toBe(9.41);
    expect(compositionA(RULES, { ...man, weightKg: 135 })?.residualKg).toBe(32.54);
  });

  it('só de 20 a 59 anos (19 e 60 ficam vazios)', () => {
    expect(compositionA(RULES, { ...man, ageYears: 19 })).toBeNull();
    expect(compositionA(RULES, { ...man, ageYears: 20 })).not.toBeNull();
    expect(compositionA(RULES, { ...man, ageYears: 59 })).not.toBeNull();
    expect(compositionA(RULES, { ...man, ageYears: 60 })).toBeNull();
  });

  it('sem um dos diâmetros, não há composição', () => {
    expect(compositionA(RULES, { ...man, wristDiameterMm: null })).toBeNull();
    expect(compositionA(RULES, { ...man, femurDiameterMm: null })).toBeNull();
  });

  it('sem altura não há óssea nem muscular; sem gordura não há muscular', () => {
    expect(compositionA(RULES, { ...man, heightCm: null })).toEqual({
      residualKg: 19.28, residualPct: 24.1, boneKg: null, bonePct: null, muscleKg: null, musclePct: null,
    });
    expect(compositionA(RULES, { ...man, fatKg: null })).toMatchObject({ boneKg: 12.05, muscleKg: null, musclePct: null });
  });
});

describe('percentuais da C (AV-06, DIV-10): kg informado é a entrada, % derivado', () => {
  it('H-04: 16 kg de 80 kg → 20,00%', () => {
    expect(percentOf(16, 80)).toBe(20);
  });

  it('H-09: água 41,3 kg e proteína 11,7 kg com 70 kg → 59,00% e 16,71%', () => {
    expect(percentOf(41.3, 70)).toBe(59);
    expect(percentOf(11.7, 70)).toBe(16.71);
  });

  it('meio para cima no empate exato: 6,72 de 51,2 = 13,125 → 13,13', () => {
    expect(percentOf(6.72, 51.2)).toBe(13.13);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules/composition`
Expected: FAIL com `Cannot find module './composition'`.

- [ ] **Step 3: Implementar `composition.ts`**

Toda etapa usa o valor da etapa anterior já arredondado, como os eventos do legado (FrmAvaliacaoA:749–763).

- `fatBreakdown`:
  - `fatKg = roundHalfUp(fatPct / 100 * weightKg, 2)`;
  - `fatPctOfWeight = percentOf(fatKg, weightKg)`;
  - `fatFreeKg = roundHalfUp(weightKg − fatKg, 2)`;
  - `fatFreePct = percentOf(fatFreeKg, weightKg)`.
- `skeletalMuscleKg = roundHalfUp(skeletalMusclePct / 100 * weightKg, 2)`.
- `compositionA`:
  - devolve `null` fora de `rules.compositionA.fromAge`…`toAge` ou sem os dois diâmetros;
  - `residualKg = roundHalfUp(weightKg × residualFactor[sex], 2)`;
  - `residualPct = percentOf(residualKg, weightKg)`;
  - óssea, só com altura: `roundHalfUp(coefficient × ((heightCm/100)² × wrist/1000 × femur/1000 × scale) ** exponent, 2)`, e `bonePct = percentOf(boneKg, weightKg)`;
  - muscular, só com óssea e `fatKg`: `roundHalfUp(weightKg − (fatKg + boneKg + residualKg), 2)`, e `musclePct = percentOf(muscleKg, weightKg)`.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules/composition`
Expected: PASS — `composition.spec` (13).

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/clinical-rules/composition.ts apps/api/src/clinical-rules/composition.spec.ts
git commit -m "feat(api): gordura e massa livre, músculo esquelético, composição da A e percentuais da C"
```

---

### Task 5: Metas — sugestões e peso-alvo (DIV-02)

**Files:**
- Create: `apps/api/src/clinical-rules/targets.ts`
- Test: `apps/api/src/clinical-rules/targets.spec.ts`

**Interfaces:**
- Consumes: `roundHalfUp`, `evaluateAgeBlocks` e `AvaliacaoRules`.
- Produces:

```ts
suggestedTargetFatPct(rules: AvaliacaoRules, sex: Sex, ageYears: number, fatPct: number): number | null
interface WeightTarget { targetWeightKg: number; weightControlKg: number | null }
targetWeightByFat(weightKg: number, fatPct: number, targetFatPct: number): WeightTarget
targetWeightByBmi(targetBmi: number, heightCm: number, weightKg: number | null): WeightTarget
```

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/targets.spec.ts`:

```ts
import type { Sex } from '@prisma/client';
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';
import { suggestedTargetFatPct, targetWeightByBmi, targetWeightByFat } from './targets';

/**
 * TabelaGorduraAlvo do legado em cada limite (limite − 0,01, limite, limite + 0,01), para a idade máxima de cada bloco.
 * Os valores esperados vêm do código transcrito, executado à parte: inclui a "queda" para a faixa etária seguinte e o
 * resultado sem faixa (null).
 */
const EXPECTED: [Sex, number, [number, number | null][]][] = [
  ['MASCULINO', 25, [[6.99, 5], [7, 9], [10.99, 9], [11, 12.5], [13.99, 12.5], [14, 15], [17, 15], [17.01, 15], [24.99, 15], [25, 19], [25.01, 15]]],
  ['MASCULINO', 35, [[11.49, 9.5], [11.5, 13.5], [15.49, 13.5], [15.5, 17], [17.99, 17], [18, 19], [27.49, 19], [27.5, 22], [27.51, 19]]],
  ['MASCULINO', 45, [[14.99, 12], [15, 17], [18.49, 17], [18.5, 20], [20.99, 20], [21, 22], [29.49, 22], [29.5, 24.5], [29.51, 22]]],
  ['MASCULINO', 55, [[16.99, 14], [17, 19], [20.49, 19], [20.5, 22], [23.49, 22], [23.5, 25], [27.49, 25], [27.5, 24.5], [30.99, 24.5], [31, null], [31.01, 25]]],
  ['MASCULINO', 65, [[18.99, 16], [19, 20.5], [21.49, 20.5], [21.5, 22.5], [23.49, 22.5], [23.5, 24.5], [30.99, 24.5], [31, null], [31.01, 24.5]]],
  ['FEMININO', 25, [[16.49, 14.5], [16.5, 18], [19.49, 18], [19.5, 21], [22.49, 21], [22.5, 24], [25.5, 24], [31.99, 24], [32, 24.5], [32.01, 24]]],
  ['FEMININO', 35, [[16.99, 15], [17, 19], [20.49, 19], [20.5, 22], [23.49, 22], [23.5, 24.5], [34.49, 24.5], [34.5, 28], [34.51, 24.5]]],
  ['FEMININO', 45, [[18.49, 17.5], [18.5, 17.5], [19.49, 17.5], [19.5, 25], [26.49, 25], [26.5, 28], [36.99, 28], [37, 30], [37.01, 28]]],
  ['FEMININO', 55, [[21.99, 19], [22, 24], [25.49, 24], [25.5, 27], [28.49, 27], [28.5, 30], [38.49, 30], [38.5, null], [38.51, 30]]],
  ['FEMININO', 65, [[22.99, 20], [23, 25], [26.49, 25], [26.5, 28], [29.49, 28], [29.5, 31], [38.49, 31], [38.5, null], [38.51, 31]]],
];

describe('gordura-alvo sugerida (TabelaGorduraAlvo, AV-14, DIV-16)', () => {
  it.each(EXPECTED)('%s %i anos: cada limite, com as lacunas do legado', (sex, age, probes) => {
    for (const [fatPct, expected] of probes) expect([fatPct, suggestedTargetFatPct(RULES, sex, age, fatPct)]).toEqual([fatPct, expected]);
  });

  it('menores de 18 entram no bloco de até 25 anos; acima de 65, sem sugestão', () => {
    expect(suggestedTargetFatPct(RULES, 'MASCULINO', 17, 20)).toBe(15);
    expect(suggestedTargetFatPct(RULES, 'MASCULINO', 66, 20)).toBeNull();
    expect(suggestedTargetFatPct(RULES, 'FEMININO', 66, 30)).toBeNull();
  });

  it('a idade escolhe o bloco: 26 anos já é o bloco de até 35', () => {
    expect(suggestedTargetFatPct(RULES, 'MASCULINO', 25, 10)).toBe(9);
    expect(suggestedTargetFatPct(RULES, 'MASCULINO', 26, 10)).toBe(9.5);
  });
});

describe('peso-alvo pela gordura (AV-12)', () => {
  it('H-07: 80 kg, 20% e alvo 15% → 75,29 kg; controle −4,71 kg (a partir do valor arredondado)', () => {
    expect(targetWeightByFat(80, 20, 15)).toEqual({ targetWeightKg: 75.29, weightControlKg: -4.71 });
  });

  it.each([
    [73.4, 27.3, 19, 65.88, -7.52],
    [91.7, 31.2, 22, 80.88, -10.82],
    [58.3, 33.3, 24.5, 51.5, -6.8],
    [66.6, 25, 15.5, 59.11, -7.49],
  ])('%p kg, %p%%, alvo %p%% → %p kg (controle %p)', (weight, fat, target, expectedWeight, expectedControl) => {
    expect(targetWeightByFat(weight, fat, target)).toEqual({ targetWeightKg: expectedWeight, weightControlKg: expectedControl });
  });
});

describe('peso-alvo pelo IMC (AV-13, DIV-02)', () => {
  it('H-08: IMC-alvo 22 e 170 cm → 63,58 kg (o legado dava 0,00 em A/C sem a B carregada)', () => {
    expect(targetWeightByBmi(22, 170, 80)).toEqual({ targetWeightKg: 63.58, weightControlKg: -16.42 });
  });

  it('sugestão 22,5 com 165 cm e 70 kg', () => {
    expect(targetWeightByBmi(22.5, 165, 70)).toEqual({ targetWeightKg: 61.26, weightControlKg: -8.74 });
  });

  it('sem peso, há peso-alvo, mas não controle', () => {
    expect(targetWeightByBmi(22, 170, null)).toEqual({ targetWeightKg: 63.58, weightControlKg: null });
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules/targets`
Expected: FAIL com `Cannot find module './targets'`.

- [ ] **Step 3: Implementar `targets.ts`**

- `suggestedTargetFatPct` → `evaluateAgeBlocks(rules.targets.suggestedFat[sex], ageYears, fatPct)`.
- `targetWeightByFat`:
  - `targetWeightKg = roundHalfUp(weightKg × (100 − fatPct) / (100 − targetFatPct), 2)`, que é algebricamente igual a massa magra ÷ (1 − alvo/100) do FrmCalcPeso:44;
  - `weightControlKg = roundHalfUp(targetWeightKg − weightKg, 2)`, a partir do peso-alvo já arredondado, como no legado (:46).
- `targetWeightByBmi`:
  - `targetWeightKg = roundHalfUp(targetBmi × (heightCm / 100) ** 2, 2)`;
  - o controle segue a mesma regra, ou fica `null` sem peso.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules/targets`
Expected: PASS — `targets.spec` (20).

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/clinical-rules/targets.ts apps/api/src/clinical-rules/targets.spec.ts
git commit -m "feat(api): sugestões de alvo e peso-alvo pela gordura e pelo IMC com os dados da avaliação corrente"
```

---

### Task 6: Energia — Harris-Benedict, OMS, DRI, bioimpedância e GET

**Files:**
- Create: `apps/api/src/clinical-rules/energy.ts`
- Test: `apps/api/src/clinical-rules/energy.spec.ts`

**Interfaces:**
- Consumes: `roundHalfUp`, `bmiAbove` (Task 3) e `AvaliacaoRules`, `EnergyMethod`.
- Produces:

```ts
harrisBenedict(rules: AvaliacaoRules, sex: Sex, weightKg: number, heightCm: number, ageYears: number): number
omsBmr(rules: AvaliacaoRules, sex: Sex, weightKg: number, ageYears: number): number
driBmr(rules: AvaliacaoRules, group: 'ADOLESCENTES' | 'ADULTOS', sex: Sex, weightKg: number, heightCm: number, ageYears: number): number
interface BmrInput { sex: Sex; ageYears: number; weightKg: number | null; heightCm: number | null; informedBmrKcal: number | null }
bmrFor(rules: AvaliacaoRules, method: EnergyMethod, input: BmrInput): number | null
expenditure(bmrKcal: number | null, activityFactor: number): number | null
```

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/energy.spec.ts`:

```ts
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';
import { bmrFor, driBmr, expenditure, harrisBenedict, omsBmr } from './energy';

describe('Harris-Benedict (EN-01)', () => {
  it('H-05: Feminino, 60 kg, 165 cm, 40 anos → 1347', () => {
    expect(harrisBenedict(RULES, 'FEMININO', 60, 165, 40)).toBe(1347);
  });

  it('Masculino, 80 kg, 180 cm, 30 anos → 1864', () => {
    expect(harrisBenedict(RULES, 'MASCULINO', 80, 180, 30)).toBe(1864);
  });

  it('inteiro, meio para cima: Feminino, 58 kg, 151 cm, 29 anos = 1353,5 → 1354', () => {
    expect(harrisBenedict(RULES, 'FEMININO', 58, 151, 29)).toBe(1354);
  });
});

describe('OMS (EN-02)', () => {
  it('H-06: Feminino, 60 kg, 40 anos → 1351', () => {
    expect(omsBmr(RULES, 'FEMININO', 60, 40)).toBe(1351);
  });

  it('cada faixa de idade e cada coeficiente (Feminino 60 kg; Masculino 70 kg)', () => {
    expect([2, 3, 9, 10, 17, 18, 29, 30, 59, 60].map((age) => omsBmr(RULES, 'FEMININO', 60, age))).toEqual([
      3609, 1849, 1849, 1478, 1478, 1378, 1378, 1351, 1351, 1226,
    ]);
    expect([2, 3, 9, 10, 17, 18, 29, 30, 59, 60].map((age) => omsBmr(RULES, 'MASCULINO', 70, age))).toEqual([
      4209, 2084, 2084, 1876, 1876, 1750, 1750, 1691, 1691, 1432,
    ]);
  });

  it('2 casas, meio para cima: Feminino, 30,95 kg, 25 anos = 950,965 → 950,97', () => {
    expect(omsBmr(RULES, 'FEMININO', 30.95, 25)).toBe(950.97);
  });
});

describe('DRI (EN-03, DIV-01 corrigida)', () => {
  it('H-23: Masculino, 40 anos, 90 kg, 173 cm (IMC 30,07) → 1841 (o legado dava 1875, a equação de peso normal)', () => {
    expect(driBmr(RULES, 'ADULTOS', 'MASCULINO', 90, 173, 40)).toBe(1841);
  });

  it.each([
    ['ADULTOS', 'MASCULINO', 65, 178, 30, 1646],
    ['ADULTOS', 'FEMININO', 55, 165, 40, 1274],
    ['ADULTOS', 'FEMININO', 70, 160, 40, 1385],
    ['ADOLESCENTES', 'MASCULINO', 50, 170, 15, 1589],
    ['ADOLESCENTES', 'MASCULINO', 49.06, 150, 15, 1365],
    ['ADOLESCENTES', 'FEMININO', 50, 165, 16, 1334],
    ['ADOLESCENTES', 'FEMININO', 60, 160, 15, 1413],
  ] as const)('%s %s, %p kg, %p cm, %p anos → %p', (group, sex, weight, height, age, expected) => {
    expect(driBmr(RULES, group, sex, weight, height, age)).toBe(expected);
  });

  it('PC-06: sobrepeso masculino adolescente usa 33,5 × idade (com 35,5 daria 1335)', () => {
    expect(driBmr(RULES, 'ADOLESCENTES', 'MASCULINO', 49.06, 150, 15)).toBe(1365);
  });

  it('limiar exato fica na equação de peso normal', () => {
    expect(driBmr(RULES, 'ADULTOS', 'MASCULINO', 99.96, 200, 40)).toBe(2114);
    expect(driBmr(RULES, 'ADULTOS', 'MASCULINO', 99.97, 200, 40)).toBe(2065);
    expect(driBmr(RULES, 'ADOLESCENTES', 'MASCULINO', 49.05, 150, 15)).toBe(1428);
  });
});

describe('TMB pelo método escolhido', () => {
  const base = { sex: 'FEMININO' as const, ageYears: 40, weightKg: 60, heightCm: 165, informedBmrKcal: 1400.5 };

  it('cada método', () => {
    expect(bmrFor(RULES, 'HARRIS_BENEDICT', base)).toBe(1347);
    expect(bmrFor(RULES, 'OMS', base)).toBe(1351);
    expect(bmrFor(RULES, 'DRI_ADULTOS', base)).toBe(1321);
    expect(bmrFor(RULES, 'BIOIMPEDANCIA', base)).toBe(1400.5);
  });

  it('o que cada método exige: OMS não usa altura; bioimpedância usa só a TMB do aparelho', () => {
    expect(bmrFor(RULES, 'OMS', { ...base, heightCm: null })).toBe(1351);
    expect(bmrFor(RULES, 'HARRIS_BENEDICT', { ...base, heightCm: null })).toBeNull();
    expect(bmrFor(RULES, 'DRI_ADOLESCENTES', { ...base, weightKg: null })).toBeNull();
    expect(bmrFor(RULES, 'BIOIMPEDANCIA', { ...base, informedBmrKcal: null })).toBeNull();
    expect(bmrFor(RULES, 'BIOIMPEDANCIA', { ...base, weightKg: null, heightCm: null })).toBe(1400.5);
  });
});

describe('GET (EN-04)', () => {
  it('H-06: fator 1,55 × TMB 1351 → 2094,05', () => {
    expect(expenditure(1351, 1.55)).toBe(2094.05);
  });

  it('sem TMB, sem GET; fator 1 repete a TMB', () => {
    expect(expenditure(null, 1.55)).toBeNull();
    expect(expenditure(1347, 1)).toBe(1347);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules/energy`
Expected: FAIL com `Cannot find module './energy'`.

- [ ] **Step 3: Implementar `energy.ts`**

- `harrisBenedict`: `roundHalfUp(e.constant + e.perKg × weightKg + e.perCm × heightCm + e.perYear × ageYears, 0)`, com `e = rules.energy.harrisBenedict[sex]`.
- `omsBmr`: usa a primeira faixa com `belowAge === null` ou `ageYears < belowAge` e devolve `roundHalfUp(band.perKg × weightKg + band.constant, 2)`.
- `driBmr` precisa deste comentário:

```ts
/**
 * DRI (EN-03). DIV-01 corrigida: o sobrepeso é decidido pelo IMC em kg/m² (bmiAbove, conta inteira), não pela altura em
 * cm² do legado. A equação é a do grupo escolhido pelo operador, como no legado (a idade não escolhe o grupo).
 */
export function driBmr(rules: AvaliacaoRules, group: 'ADOLESCENTES' | 'ADULTOS', sex: Sex, weightKg: number, heightCm: number, ageYears: number): number {
  const table = group === 'ADOLESCENTES' ? rules.energy.driAdolescents : rules.energy.driAdults;
  // TODO(PREMISSA-CLINICA PC-05): validar com o médico responsável o IMC interno em kg/m² e os limiares do legado.
  const e = bmiAbove(weightKg, heightCm, table.overweightAboveBmi) ? table.overweight[sex] : table.normal[sex];
  return roundHalfUp(e.constant + e.perYear * ageYears + e.perMeter * (heightCm / 100) + e.perKg * weightKg, 0);
}
```

- `bmrFor` devolve `null` quando falta algo de que o método precisa:
  - Harris-Benedict e DRI: peso e altura;
  - OMS: peso;
  - bioimpedância: TMB informada.

  Para bioimpedância, devolve `roundHalfUp(informedBmrKcal, 2)`. Os demais métodos chamam as funções acima (`DRI_ADOLESCENTES` → grupo `'ADOLESCENTES'`; `DRI_ADULTOS` → `'ADULTOS'`).
- `expenditure`: `null` sem TMB; nos demais casos, `roundHalfUp(activityFactor × bmrKcal, 2)`.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules/energy`
Expected: PASS — `energy.spec` (20).

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/clinical-rules/energy.ts apps/api/src/clinical-rules/energy.spec.ts
git commit -m "feat(api): TMB por Harris-Benedict, OMS, DRI com IMC em kg/m² e bioimpedância, e GET"
```

---

### Task 7: Cálculo por protocolo — avaliação A

**Files:**
- Create: `apps/api/src/clinical-rules/assessment-calculation.ts`
- Test: `apps/api/src/clinical-rules/assessment-calculation-a.spec.ts`

**Interfaces:**
- Consumes: tudo das Tasks 1–6.
- Produces:
  - os tipos `AssessmentCalculationInput` e `AssessmentCalculation` (abaixo);
  - `calculateAssessment(rules: AvaliacaoRules, input: AssessmentCalculationInput): AssessmentCalculation`;
  - `EMPTY_PROTOCOL_RESULT`.

  Nesta tarefa, os protocolos B e C ainda lançam `Error('Protocolo ainda não implementado.')`; as Tasks 8 e 9 os implementam.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/assessment-calculation-a.spec.ts`:

```ts
import { calculateAssessment, type AssessmentCalculationInput } from './assessment-calculation';
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';

const NONE = {
  weightKg: null, heightCm: null, waistCm: null, hipCm: null, fatPct: null, wristDiameterMm: null, femurDiameterMm: null,
  informedBmi: null, skeletalMusclePct: null, visceralFatLevel: null, fatKg: null, boneKg: null, proteinKg: null, waterKg: null,
  muscleKg: null, skeletalMuscleKg: null, targetFatPct: null, targetBmi: null, energyMethod: null, informedBmrKcal: null,
  activityFactor: null,
};

const A: AssessmentCalculationInput = {
  ...NONE, protocol: 'A', sex: 'FEMININO', ageYears: 40, weightKg: 80, heightCm: 170, waistCm: 80, hipCm: 100, fatPct: 20,
  wristDiameterMm: 50, femurDiameterMm: 90, energyMethod: 'HARRIS_BENEDICT', activityFactor: 1.55,
};

describe('avaliação A', () => {
  it('resultado completo (Feminino, 40 anos, 80 kg, 170 cm, 20% de gordura)', () => {
    expect(calculateAssessment(RULES, A)).toEqual({
      rulesVersion: 'avaliacao-v1', protocol: 'A', ageYears: 40, ageBand: 'Adultos', totalWeightKg: 80, totalWeightPct: 100,
      bmi: 27.68, bmiCalculated: 27.68, bmiClassification: 'Sobrepeso', bmiReference: null,
      fatPct: 20, fatKg: 16, fatPctOfWeight: 20, fatClassification: 'Bom', fatReference: null, fatFreeKg: 64, fatFreePct: 80,
      skeletalMusclePct: null, skeletalMuscleKg: null, skeletalMuscleReference: null,
      muscleKg: 37.51, musclePct: 46.89, boneKg: 9.77, bonePct: 12.21, residualKg: 16.72, residualPct: 20.9,
      proteinKg: null, proteinPct: null, waterKg: null, waterPct: null, visceralFatReference: null, waistHipReference: null,
      waistHipRatio: 0.8, waistHipClassification: 'Risco Alto',
      suggestedTargetBmi: 22.5, suggestedTargetFatPct: 25, targetFatPct: 25, targetBmi: 22.5,
      targetWeightByFatKg: 85.33, weightControlByFatKg: 5.33, targetWeightByBmiKg: 65.03, weightControlByBmiKg: -14.97,
      energyMethod: 'HARRIS_BENEDICT', bmrKcal: 1548, activityFactor: 1.55, expenditureKcal: 2399.4,
    });
  });

  it('abaixo de 5 anos: IMC vazio e "N/A"; composição vazia; RCQ "N/A"', () => {
    expect(calculateAssessment(RULES, { ...A, ageYears: 4 })).toMatchObject({
      ageBand: 'Crianças', bmi: null, bmiCalculated: null, bmiClassification: 'N/A', residualKg: null, boneKg: null,
      muscleKg: null, waistHipClassification: 'N/A',
    });
  });

  it('19 anos: IMC calculado e "Não classificado (5 a 19 anos)"; sem composição', () => {
    expect(calculateAssessment(RULES, { ...A, ageYears: 19 })).toMatchObject({
      ageBand: 'Adolescentes', bmi: 27.68, bmiClassification: 'Não classificado (5 a 19 anos)', residualKg: null,
    });
  });

  it('60 anos: IMC do idoso; sem composição', () => {
    expect(calculateAssessment(RULES, { ...A, ageYears: 60 })).toMatchObject({
      ageBand: 'Idosos', bmiClassification: 'Obesidade', residualKg: null, waistHipClassification: 'Risco Moderado',
    });
  });

  it('sem peso: nada que dependa do peso; a classificação da gordura usa só o percentual', () => {
    expect(calculateAssessment(RULES, { ...A, weightKg: null })).toMatchObject({
      totalWeightKg: null, totalWeightPct: null, bmi: null, bmiClassification: null, fatKg: null, fatFreeKg: null,
      fatClassification: 'Bom', residualKg: null, targetWeightByFatKg: null, targetWeightByBmiKg: 65.03,
      weightControlByBmiKg: null, bmrKcal: null, expenditureKcal: null,
    });
  });

  it('alvos informados prevalecem sobre as sugestões', () => {
    expect(calculateAssessment(RULES, { ...A, targetFatPct: 18, targetBmi: 21 })).toMatchObject({
      suggestedTargetFatPct: 25, targetFatPct: 18, targetWeightByFatKg: 78.05, weightControlByFatKg: -1.95,
      suggestedTargetBmi: 22.5, targetBmi: 21, targetWeightByBmiKg: 60.69, weightControlByBmiKg: -19.31,
    });
  });

  it('decisão (a): sem quadril, RCQ e classificação vazios', () => {
    expect(calculateAssessment(RULES, { ...A, hipCm: null })).toMatchObject({ waistHipRatio: null, waistHipClassification: null });
  });

  it('sem método de TMB: sem TMB nem GET; fator padrão 1', () => {
    expect(calculateAssessment(RULES, { ...A, energyMethod: null, activityFactor: null })).toMatchObject({
      energyMethod: null, bmrKcal: null, activityFactor: 1, expenditureKcal: null,
    });
  });

  it('usa as regras recebidas, não uma versão fixa (cópia sintética)', () => {
    const synthetic = { ...RULES, version: 'avaliacao-teste-sintetico', targets: { ...RULES.targets, suggestedBmi: 23 } };
    expect(calculateAssessment(synthetic, A)).toMatchObject({
      rulesVersion: 'avaliacao-teste-sintetico', suggestedTargetBmi: 23, targetBmi: 23, targetWeightByBmiKg: 66.47,
    });
    expect(RULES.version).toBe('avaliacao-v1');
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules/assessment-calculation-a`
Expected: FAIL com `Cannot find module './assessment-calculation'`.

- [ ] **Step 3: Implementar `assessment-calculation.ts`**

```ts
import type { Sex } from '@prisma/client';
import { ageBand } from './age-band';
import type { AssessmentProtocol, AvaliacaoRules, EnergyMethod } from './avaliacao-rules';
import { bmiFromWeightHeight, classifyBmi } from './bmi';
import { classifyFat } from './body-fat';
import { compositionA, fatBreakdown } from './composition';
import { bmrFor, expenditure } from './energy';
import { suggestedTargetFatPct, targetWeightByBmi, targetWeightByFat } from './targets';
import { classifyWaistHip, waistHipRatio } from './waist-hip';

/** Entrada já validada (assessment-normalize): o que não se aplica ao protocolo chega null. */
export interface AssessmentCalculationInput {
  protocol: AssessmentProtocol;
  sex: Sex;
  ageYears: number;
  weightKg: number | null;
  heightCm: number | null;
  waistCm: number | null;
  hipCm: number | null;
  /** A e B. */
  fatPct: number | null;
  /** A. */
  wristDiameterMm: number | null;
  femurDiameterMm: number | null;
  /** B e C (do aparelho). */
  informedBmi: number | null;
  /** B. */
  skeletalMusclePct: number | null;
  visceralFatLevel: number | null;
  /** C (kg informados). */
  fatKg: number | null;
  boneKg: number | null;
  proteinKg: number | null;
  waterKg: number | null;
  muscleKg: number | null;
  skeletalMuscleKg: number | null;
  targetFatPct: number | null;
  targetBmi: number | null;
  energyMethod: EnergyMethod | null;
  informedBmrKcal: number | null;
  activityFactor: number | null;
}

export interface AssessmentCalculation {
  rulesVersion: string;
  protocol: AssessmentProtocol;
  ageYears: number;
  ageBand: string;
  /** Decisão (b): o peso da avaliação e 100% nos três protocolos. */
  totalWeightKg: number | null;
  totalWeightPct: number | null;
  /** A: calculado. B/C: o informado pelo aparelho. */
  bmi: number | null;
  /** Peso ÷ altura², ao lado do informado na B/C (decisão e). */
  bmiCalculated: number | null;
  /** Só na A; na B/C a classificação é manual (Plano 4). */
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
  /** Efetivos: o informado, ou a sugestão. */
  targetFatPct: number | null;
  targetBmi: number;
  targetWeightByFatKg: number | null;
  weightControlByFatKg: number | null;
  targetWeightByBmiKg: number | null;
  weightControlByBmiKg: number | null;
  energyMethod: EnergyMethod | null;
  bmrKcal: number | null;
  activityFactor: number;
  expenditureKcal: number | null;
}

type ProtocolResult = Pick<
  AssessmentCalculation,
  | 'bmi' | 'bmiCalculated' | 'bmiClassification' | 'bmiReference' | 'fatPct' | 'fatKg' | 'fatPctOfWeight'
  | 'fatClassification' | 'fatReference' | 'fatFreeKg' | 'fatFreePct' | 'skeletalMusclePct' | 'skeletalMuscleKg'
  | 'skeletalMuscleReference' | 'muscleKg' | 'musclePct' | 'boneKg' | 'bonePct' | 'residualKg' | 'residualPct'
  | 'proteinKg' | 'proteinPct' | 'waterKg' | 'waterPct' | 'visceralFatReference' | 'waistHipReference'
>;

/** O que não se aplica a um protocolo fica null; nunca herda valor de outro (DIV-11). */
export const EMPTY_PROTOCOL_RESULT: Readonly<ProtocolResult> = Object.freeze({
  bmi: null, bmiCalculated: null, bmiClassification: null, bmiReference: null, fatPct: null, fatKg: null,
  fatPctOfWeight: null, fatClassification: null, fatReference: null, fatFreeKg: null, fatFreePct: null,
  skeletalMusclePct: null, skeletalMuscleKg: null, skeletalMuscleReference: null, muscleKg: null, musclePct: null,
  boneKg: null, bonePct: null, residualKg: null, residualPct: null, proteinKg: null, proteinPct: null, waterKg: null,
  waterPct: null, visceralFatReference: null, waistHipReference: null,
});

type ProtocolCalculator = (
  rules: AvaliacaoRules,
  input: AssessmentCalculationInput,
  bmiCalculated: number | null,
  ratio: number | null,
) => Partial<ProtocolResult>;

/** A: IMC calculado e classificado por idade; gordura informada em %; composição (AV-08 a AV-11). */
const protocolA: ProtocolCalculator = (rules, input, bmiCalculated) => {
  const { sex, ageYears, weightKg, fatPct } = input;
  const bmi = ageYears < rules.bmi.notApplicableBelowAge ? null : bmiCalculated;
  const fat = weightKg !== null && fatPct !== null ? fatBreakdown(weightKg, fatPct) : null;
  const composition =
    weightKg === null
      ? null
      : compositionA(rules, {
          sex, ageYears, weightKg, heightCm: input.heightCm, wristDiameterMm: input.wristDiameterMm,
          femurDiameterMm: input.femurDiameterMm, fatKg: fat?.fatKg ?? null,
        });
  return {
    bmi,
    bmiCalculated: bmi,
    bmiClassification: classifyBmi(rules, ageYears, bmi),
    fatPct,
    fatKg: fat?.fatKg ?? null,
    fatPctOfWeight: fat?.fatPctOfWeight ?? null,
    fatClassification: fatPct === null ? null : classifyFat(rules, sex, ageYears, fatPct),
    fatFreeKg: fat?.fatFreeKg ?? null,
    fatFreePct: fat?.fatFreePct ?? null,
    ...(composition ?? {}),
  };
};

const notYet: ProtocolCalculator = () => {
  throw new Error('Protocolo ainda não implementado.');
};

const PROTOCOLS: Readonly<Record<AssessmentProtocol, ProtocolCalculator>> = { A: protocolA, B: notYet, C: notYet };

/** Resultado da avaliação pelo protocolo, com a versão das regras usada. */
export function calculateAssessment(rules: AvaliacaoRules, input: AssessmentCalculationInput): AssessmentCalculation {
  const { sex, ageYears, weightKg, heightCm } = input;
  const ratio = waistHipRatio(input.waistCm, input.hipCm);
  const bmiCalculated = weightKg !== null && heightCm !== null ? bmiFromWeightHeight(weightKg, heightCm) : null;
  const part: ProtocolResult = { ...EMPTY_PROTOCOL_RESULT, ...PROTOCOLS[input.protocol](rules, input, bmiCalculated, ratio) };

  const suggestedFat = part.fatPct === null ? null : suggestedTargetFatPct(rules, sex, ageYears, part.fatPct);
  const targetFatPct = input.targetFatPct ?? suggestedFat;
  const targetBmi = input.targetBmi ?? rules.targets.suggestedBmi;
  const byFat = weightKg !== null && part.fatPct !== null && targetFatPct !== null ? targetWeightByFat(weightKg, part.fatPct, targetFatPct) : null;
  const byBmi = heightCm !== null ? targetWeightByBmi(targetBmi, heightCm, weightKg) : null;
  const activityFactor = input.activityFactor ?? rules.energy.defaultActivityFactor;
  const bmrKcal =
    input.energyMethod === null
      ? null
      : bmrFor(rules, input.energyMethod, { sex, ageYears, weightKg, heightCm, informedBmrKcal: input.informedBmrKcal });

  return {
    rulesVersion: rules.version,
    protocol: input.protocol,
    ageYears,
    ageBand: ageBand(rules, ageYears),
    totalWeightKg: weightKg,
    totalWeightPct: weightKg === null ? null : 100,
    ...part,
    waistHipRatio: ratio,
    waistHipClassification: ratio === null ? null : classifyWaistHip(rules, sex, ageYears, ratio),
    suggestedTargetBmi: rules.targets.suggestedBmi,
    suggestedTargetFatPct: suggestedFat,
    targetFatPct,
    targetBmi,
    targetWeightByFatKg: byFat?.targetWeightKg ?? null,
    weightControlByFatKg: byFat?.weightControlKg ?? null,
    targetWeightByBmiKg: byBmi?.targetWeightKg ?? null,
    weightControlByBmiKg: byBmi?.weightControlKg ?? null,
    energyMethod: input.energyMethod,
    bmrKcal,
    activityFactor,
    expenditureKcal: expenditure(bmrKcal, activityFactor),
  };
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: PASS — `assessment-calculation-a.spec` (9), além das suítes anteriores.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/clinical-rules/assessment-calculation.ts apps/api/src/clinical-rules/assessment-calculation-a.spec.ts
git commit -m "feat(api): cálculo da avaliação A com a versão das regras usada"
```

- [ ] **Step 6: PONTO DE CONTROLE DA PESSOA — regras da avaliação A**

Pare e mostre à pessoa a saída de:

```powershell
Set-Location apps/api
npx jest src/clinical-rules --verbose
Set-Location ../..
```

A pessoa confere, nos nomes dos testes e na nota do Maestri "Plano 3 — roteiro H-10 e H-11":

- H-01, H-02, H-03 e H-05;
- o bloco L;
- as idades nas fronteiras;
- os limites de TabelaIMC, IMCidoso, TabelaGordura e TabelaRCQ;
- a gordura-alvo com as lacunas;
- a composição da A;
- o DRI com H-23 e o coeficiente 33,5;
- as faixas da OMS e o arredondamento meio para cima.

Uma correção pedida aqui altera a `avaliacao-v1` antes de qualquer gravação; ela ainda não foi publicada para nenhum dado. Nesse caso, apague o instantâneo e gere de novo uma única vez. Só siga para a Task 8 com o "ok" da pessoa.

---

### Task 8: Cálculo por protocolo — avaliação B/R

**Files:**
- Modify: `apps/api/src/clinical-rules/assessment-calculation.ts` (protocolo B)
- Test: `apps/api/src/clinical-rules/assessment-calculation-b.spec.ts`

**Interfaces:**
- Consumes: `fatBreakdown`, `skeletalMuscleKg`, `fatReference`, `skeletalMuscleReference`, `waistHipReference` e `AvaliacaoRules.bmi.reference` / `visceralFat.reference`.
- Produces: `calculateAssessment` com `protocol: 'B'`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/assessment-calculation-b.spec.ts`:

```ts
import { calculateAssessment, type AssessmentCalculationInput } from './assessment-calculation';
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';

const NONE = {
  weightKg: null, heightCm: null, waistCm: null, hipCm: null, fatPct: null, wristDiameterMm: null, femurDiameterMm: null,
  informedBmi: null, skeletalMusclePct: null, visceralFatLevel: null, fatKg: null, boneKg: null, proteinKg: null, waterKg: null,
  muscleKg: null, skeletalMuscleKg: null, targetFatPct: null, targetBmi: null, energyMethod: null, informedBmrKcal: null,
  activityFactor: null,
};

const B: AssessmentCalculationInput = {
  ...NONE, protocol: 'B', sex: 'MASCULINO', ageYears: 30, weightKg: 80, heightCm: 180, waistCm: 85, hipCm: 100, fatPct: 18,
  informedBmi: 24.7, skeletalMusclePct: 40, visceralFatLevel: 8, energyMethod: 'BIOIMPEDANCIA', informedBmrKcal: 1780, targetFatPct: 15,
};

describe('avaliação B/R', () => {
  it('resultado completo: IMC informado com o calculado ao lado, intervalos da B e sem classificações automáticas', () => {
    expect(calculateAssessment(RULES, B)).toEqual({
      rulesVersion: 'avaliacao-v1', protocol: 'B', ageYears: 30, ageBand: 'Adultos', totalWeightKg: 80, totalWeightPct: 100,
      bmi: 24.7, bmiCalculated: 24.69, bmiClassification: null, bmiReference: '18,5 - 24,9',
      fatPct: 18, fatKg: 14.4, fatPctOfWeight: 18, fatClassification: null, fatReference: '8 - 19,9', fatFreeKg: 65.6, fatFreePct: 82,
      skeletalMusclePct: 40, skeletalMuscleKg: 32, skeletalMuscleReference: '33,3 - 39,3',
      muscleKg: null, musclePct: null, boneKg: null, bonePct: null, residualKg: null, residualPct: null,
      proteinKg: null, proteinPct: null, waterKg: null, waterPct: null, visceralFatReference: '<= 9', waistHipReference: '0,84 - 0,91',
      waistHipRatio: 0.85, waistHipClassification: 'Risco Moderado',
      suggestedTargetBmi: 22.5, suggestedTargetFatPct: 19, targetFatPct: 15, targetBmi: 22.5,
      targetWeightByFatKg: 77.18, weightControlByFatKg: -2.82, targetWeightByBmiKg: 72.9, weightControlByBmiKg: -7.1,
      energyMethod: 'BIOIMPEDANCIA', bmrKcal: 1780, activityFactor: 1, expenditureKcal: 1780,
    });
  });

  it('H-07: 80 kg, 20% e alvo 15% → peso-alvo 75,29 kg; controle −4,71 kg', () => {
    const result = calculateAssessment(RULES, { ...NONE, protocol: 'B', sex: 'MASCULINO', ageYears: 30, weightKg: 80, fatPct: 20, targetFatPct: 15 });
    expect(result).toMatchObject({ targetWeightByFatKg: 75.29, weightControlByFatKg: -4.71 });
  });

  it('cada intervalo só aparece com o dado que o legado exigia', () => {
    expect(calculateAssessment(RULES, { ...NONE, protocol: 'B', sex: 'MASCULINO', ageYears: 30, weightKg: 80 })).toMatchObject({
      bmi: null, bmiCalculated: null, bmiReference: null, fatReference: null, skeletalMuscleReference: null,
      visceralFatReference: null, waistHipReference: null, waistHipRatio: null,
    });
  });

  it('a B não calcula a composição da A, mesmo com diâmetros na entrada', () => {
    expect(calculateAssessment(RULES, { ...B, wristDiameterMm: 50, femurDiameterMm: 90 })).toMatchObject({ residualKg: null, boneKg: null });
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules/assessment-calculation-b`
Expected: FAIL com `Protocolo ainda não implementado.`.

- [ ] **Step 3: Implementar o protocolo B**

Em `assessment-calculation.ts`:

- importe `skeletalMuscleKg` de `./composition`;
- importe `fatReference` e `skeletalMuscleReference` de `./body-fat`;
- importe `waistHipReference` de `./waist-hip`;
- troque `B: notYet` por `B: protocolB`, com este conteúdo:

```ts
/** B/R: IMC e gordura % informados (do aparelho); intervalos de referência em vez de classificações automáticas. */
const protocolB: ProtocolCalculator = (rules, input, bmiCalculated, ratio) => {
  const { sex, ageYears, weightKg, fatPct, skeletalMusclePct } = input;
  const fat = weightKg !== null && fatPct !== null ? fatBreakdown(weightKg, fatPct) : null;
  return {
    bmi: input.informedBmi,
    bmiCalculated,
    bmiReference: input.informedBmi === null ? null : rules.bmi.reference,
    fatPct,
    fatKg: fat?.fatKg ?? null,
    fatPctOfWeight: fat?.fatPctOfWeight ?? null,
    fatReference: fatPct === null ? null : fatReference(rules, sex, ageYears),
    fatFreeKg: fat?.fatFreeKg ?? null,
    fatFreePct: fat?.fatFreePct ?? null,
    skeletalMusclePct,
    skeletalMuscleKg: weightKg !== null && skeletalMusclePct !== null ? skeletalMuscleKg(weightKg, skeletalMusclePct) : null,
    skeletalMuscleReference: skeletalMusclePct === null ? null : skeletalMuscleReference(rules, sex, ageYears),
    visceralFatReference: input.visceralFatLevel === null ? null : rules.visceralFat.reference,
    waistHipReference: ratio === null ? null : waistHipReference(rules, sex, ageYears),
  };
};
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules`
Expected: PASS — `assessment-calculation-b.spec` (4), além das anteriores.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/clinical-rules/assessment-calculation.ts apps/api/src/clinical-rules/assessment-calculation-b.spec.ts
git commit -m "feat(api): cálculo da avaliação B/R com IMC informado e intervalos de referência"
```

- [ ] **Step 6: PONTO DE CONTROLE DA PESSOA — regras da avaliação B/R**

Mostre à pessoa a saída de `npx jest src/clinical-rules/assessment-calculation-b src/clinical-rules/body-fat src/clinical-rules/waist-hip --verbose` (em `apps/api`). A pessoa confere:

- H-07;
- o IMC informado ao lado do calculado (24,7 e 24,69);
- os intervalos de gordura, músculo esquelético e RCQ por idade;
- os textos "18,5 - 24,9" e "<= 9" (PC-09: conferir no Excel);
- a ausência de classificação automática na B.

Só siga com o "ok" da pessoa.

---

### Task 9: Cálculo por protocolo — avaliação C

**Files:**
- Modify: `apps/api/src/clinical-rules/assessment-calculation.ts` (protocolo C)
- Test: `apps/api/src/clinical-rules/assessment-calculation-c.spec.ts`

**Interfaces:**
- Consumes: `percentOf` (Task 1).
- Produces: `calculateAssessment` com `protocol: 'C'`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/clinical-rules/assessment-calculation-c.spec.ts`:

```ts
import { calculateAssessment, type AssessmentCalculationInput } from './assessment-calculation';
import { AVALIACAO_RULES_V1 as RULES } from './avaliacao-rules';

const NONE = {
  weightKg: null, heightCm: null, waistCm: null, hipCm: null, fatPct: null, wristDiameterMm: null, femurDiameterMm: null,
  informedBmi: null, skeletalMusclePct: null, visceralFatLevel: null, fatKg: null, boneKg: null, proteinKg: null, waterKg: null,
  muscleKg: null, skeletalMuscleKg: null, targetFatPct: null, targetBmi: null, energyMethod: null, informedBmrKcal: null,
  activityFactor: null,
};

const C: AssessmentCalculationInput = {
  ...NONE, protocol: 'C', sex: 'FEMININO', ageYears: 35, weightKg: 70, heightCm: 165, waistCm: 75, hipCm: 100, informedBmi: 25.7,
  fatKg: 21, boneKg: 2.5, proteinKg: 11.7, waterKg: 41.3, muscleKg: 45.2, skeletalMuscleKg: 25.1, energyMethod: 'OMS', activityFactor: 1.2,
};

describe('avaliação C', () => {
  it('resultado completo: kg informados e percentuais derivados (DIV-10), gordura-alvo pelo percentual derivado', () => {
    expect(calculateAssessment(RULES, C)).toEqual({
      rulesVersion: 'avaliacao-v1', protocol: 'C', ageYears: 35, ageBand: 'Adultos', totalWeightKg: 70, totalWeightPct: 100,
      bmi: 25.7, bmiCalculated: 25.71, bmiClassification: null, bmiReference: null,
      fatPct: 30, fatKg: 21, fatPctOfWeight: null, fatClassification: null, fatReference: null, fatFreeKg: null, fatFreePct: null,
      skeletalMusclePct: 35.86, skeletalMuscleKg: 25.1, skeletalMuscleReference: null,
      muscleKg: 45.2, musclePct: 64.57, boneKg: 2.5, bonePct: 3.57, residualKg: null, residualPct: null,
      proteinKg: 11.7, proteinPct: 16.71, waterKg: 41.3, waterPct: 59, visceralFatReference: null, waistHipReference: null,
      waistHipRatio: 0.75, waistHipClassification: 'Risco Moderado',
      suggestedTargetBmi: 22.5, suggestedTargetFatPct: 24.5, targetFatPct: 24.5, targetBmi: 22.5,
      targetWeightByFatKg: 64.9, weightControlByFatKg: -5.1, targetWeightByBmiKg: 61.26, weightControlByBmiKg: -8.74,
      energyMethod: 'OMS', bmrKcal: 1438, activityFactor: 1.2, expenditureKcal: 1725.6,
    });
  });

  it('H-04: 16 kg de gordura com 80 kg → 20,00%; peso total 100,00%', () => {
    expect(calculateAssessment(RULES, { ...NONE, protocol: 'C', sex: 'FEMININO', ageYears: 40, weightKg: 80, fatKg: 16 })).toMatchObject({
      fatPct: 20, totalWeightKg: 80, totalWeightPct: 100,
    });
  });

  it('H-09: água 41,3 kg → 59,00%; proteína 11,7 kg → 16,71% (o kg original fica)', () => {
    expect(calculateAssessment(RULES, C)).toMatchObject({ waterKg: 41.3, waterPct: 59, proteinKg: 11.7, proteinPct: 16.71 });
  });

  it('meio para cima no empate exato: 6,72 kg de 51,2 kg = 13,125% → 13,13%', () => {
    expect(calculateAssessment(RULES, { ...C, weightKg: 51.2, fatKg: 6.72 }).fatPct).toBe(13.13);
  });

  it('sem peso: os kg ficam, os percentuais e o que depende do peso ficam vazios', () => {
    expect(calculateAssessment(RULES, { ...C, weightKg: null })).toMatchObject({
      fatKg: 21, fatPct: null, waterKg: 41.3, waterPct: null, bmi: 25.7, bmiCalculated: null, suggestedTargetFatPct: null,
      targetWeightByFatKg: null, targetWeightByBmiKg: 61.26, weightControlByBmiKg: null, bmrKcal: null,
    });
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/clinical-rules/assessment-calculation-c`
Expected: FAIL com `Protocolo ainda não implementado.`.

- [ ] **Step 3: Implementar o protocolo C**

Importe `percentOf` de `./rounding`, troque `C: notYet` por `C: protocolC` e remova `notYet`, que fica sem uso:

```ts
/** C: kg informados (aparelho ou OCR) e percentuais derivados do peso (AV-06, DIV-10); classificações manuais (Plano 4). */
const protocolC: ProtocolCalculator = (_rules, input, bmiCalculated) => {
  const pct = (kg: number | null) => (kg === null || input.weightKg === null ? null : percentOf(kg, input.weightKg));
  return {
    bmi: input.informedBmi,
    bmiCalculated,
    fatKg: input.fatKg,
    fatPct: pct(input.fatKg),
    boneKg: input.boneKg,
    bonePct: pct(input.boneKg),
    proteinKg: input.proteinKg,
    proteinPct: pct(input.proteinKg),
    waterKg: input.waterKg,
    waterPct: pct(input.waterKg),
    muscleKg: input.muscleKg,
    musclePct: pct(input.muscleKg),
    skeletalMuscleKg: input.skeletalMuscleKg,
    skeletalMusclePct: pct(input.skeletalMuscleKg),
  };
};
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/clinical-rules; npm run typecheck -w @psm/api`
Expected: PASS — `assessment-calculation-c.spec` (5), além das anteriores; `typecheck` sem erros.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/clinical-rules/assessment-calculation.ts apps/api/src/clinical-rules/assessment-calculation-c.spec.ts
git commit -m "feat(api): cálculo da avaliação C com kg informados e percentuais derivados"
```

- [ ] **Step 6: PONTO DE CONTROLE DA PESSOA — regras da avaliação C**

Mostre à pessoa a saída de `npx jest src/clinical-rules/assessment-calculation-c src/clinical-rules/composition --verbose` (em `apps/api`). A pessoa confere:

- H-04 e H-09;
- os percentuais com 2 casas, meio para cima;
- a ausência de classificação automática na C.

Só siga com o "ok" da pessoa.

---

### Task 10: Validação da entrada — limites técnicos, idade na data e campos por protocolo

**Files:**
- Create: `apps/api/src/assessments/assessment-normalize.ts`
- Test: `apps/api/src/assessments/assessment-normalize.spec.ts`

**Interfaces:**
- Consumes: `ageOnDate` (`clinical-rules/age.ts`); `AvaliacaoRules`, `Range`, `LimitKey`, `AssessmentProtocol` e `EnergyMethod` (Task 2); `AssessmentCalculationInput` (Task 7); `parseIsoDate`, `formatIsoDate` e `cleanText`.
- Produces:

```ts
const NUMERIC_FIELDS: readonly NumericField[]
type AssessmentInput = { protocol?: string | null; assessedOn?: string | null; energyMethod?: string | null }
  & Partial<Record<NumericField, number | null>>
interface PatientFacts { sex: Sex; birthDate: string }
type AssessmentFieldErrors = Partial<Record<keyof AssessmentInput, string>>
interface AssessmentValues {
  protocol: AssessmentProtocol | null; assessedOn: string | null; ageYears: number | null;
  energyMethod: EnergyMethod | null; numbers: Record<NumericField, number | null>;
}
normalizeAssessment(input: AssessmentInput, patient: PatientFacts, rules: AvaliacaoRules, today: string)
  : { values: AssessmentValues; errors: AssessmentFieldErrors }
toCalculationInput(values: AssessmentValues, sex: Sex): AssessmentCalculationInput | null
rangeMessage(label: string, range: Range, unit: string): string
```

- [ ] **Step 1: Escrever os testes**

`apps/api/src/assessments/assessment-normalize.spec.ts`:

```ts
import { AVALIACAO_RULES_V1 as RULES } from '../clinical-rules/avaliacao-rules';
import { NUMERIC_FIELDS, normalizeAssessment, rangeMessage, toCalculationInput, type AssessmentInput } from './assessment-normalize';

const woman = { sex: 'FEMININO' as const, birthDate: '1986-05-09' };
const TODAY = '2026-10-08';
const run = (input: AssessmentInput) => normalizeAssessment(input, woman, RULES, TODAY);

describe('normalizeAssessment', () => {
  it('protocolo e data: idade na data da avaliação (DIV-15), números vazios, sem erros', () => {
    const { values, errors } = run({ protocol: 'A', assessedOn: '2026-05-09' });

    expect(errors).toEqual({});
    expect(values).toMatchObject({ protocol: 'A', assessedOn: '2026-05-09', ageYears: 40, energyMethod: null });
    expect(Object.values(values.numbers)).toEqual(Array(NUMERIC_FIELDS.length).fill(null));
    expect(run({ protocol: 'A', assessedOn: '2026-05-08' }).values.ageYears).toBe(39);
  });

  it('mensagens dos limites técnicos', () => {
    const { values, errors } = run({
      protocol: 'A', assessedOn: '2026-05-09', weightKg: 0, heightCm: 29.99, waistCm: 300.01, fatPct: 100,
      wristDiameterMm: 200.5, targetBmi: 100.01, informedBmrKcal: 10000.01, activityFactor: 10.5,
    });

    expect(errors).toEqual({
      weightKg: 'O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
      heightCm: 'A altura deve estar entre 30 e 250 cm, com até 2 casas decimais.',
      waistCm: 'A cintura deve ser maior que 0 e no máximo 300 cm, com até 2 casas decimais.',
      fatPct: 'O percentual de gordura deve ser maior que 0 e menor que 100, com até 2 casas decimais.',
      wristDiameterMm: 'O diâmetro do punho deve ser maior que 0 e no máximo 200 mm, com até 2 casas decimais.',
      targetBmi: 'O IMC-alvo deve ser maior que 0 e no máximo 100, com até 2 casas decimais.',
      informedBmrKcal: 'A TMB informada deve ser maior que 0 e no máximo 10000 kcal, com até 2 casas decimais.',
      activityFactor: 'O fator de atividade deve ser maior que 0 e no máximo 10, com até 2 casas decimais.',
    });
    expect(values.numbers).toMatchObject({ weightKg: null, heightCm: null, fatPct: null, activityFactor: null });
  });

  it('aceita cada limite e 2 casas cujo produto por 100 não é exato em ponto flutuante', () => {
    const low = run({ protocol: 'A', assessedOn: '2026-05-09', weightKg: 0.01, heightCm: 30, fatPct: 0.01, wristDiameterMm: 0.01 });
    const high = run({ protocol: 'A', assessedOn: '2026-05-09', weightKg: 500, heightCm: 250, waistCm: 300, fatPct: 99.99, femurDiameterMm: 200, activityFactor: 10 });
    const tricky = run({ protocol: 'A', assessedOn: '2026-05-09', weightKg: 40.02, activityFactor: 0.07 });

    expect(low.errors).toEqual({});
    expect(high.errors).toEqual({});
    expect(tricky.errors).toEqual({});
    expect(tricky.values.numbers).toMatchObject({ weightKg: 40.02, activityFactor: 0.07 });
  });

  it('mais de 2 casas é recusado', () => {
    expect(run({ protocol: 'A', assessedOn: '2026-05-09', weightKg: 70.123 }).errors).toEqual({
      weightKg: 'O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
    });
  });

  it('nível de gordura visceral: maior que 0, sem máximo', () => {
    expect(run({ protocol: 'B', assessedOn: '2026-05-09', visceralFatLevel: 1000 }).errors).toEqual({});
    expect(run({ protocol: 'B', assessedOn: '2026-05-09', visceralFatLevel: 0 }).errors).toEqual({
      visceralFatLevel: 'O nível de gordura visceral deve ser maior que 0, com até 2 casas decimais.',
    });
  });

  it('DIV-11: campo de outro protocolo é recusado no campo e não chega ao cálculo', () => {
    const b = run({ protocol: 'B', assessedOn: '2026-05-09', wristDiameterMm: 50, fatKg: 10, fatPct: 20 });
    const c = run({ protocol: 'C', assessedOn: '2026-05-09', fatPct: 20, skeletalMusclePct: 30 });
    const a = run({ protocol: 'A', assessedOn: '2026-05-09', informedBmi: 25, waterKg: 30 });

    expect(b.errors).toEqual({ wristDiameterMm: 'Este campo não se aplica ao protocolo B.', fatKg: 'Este campo não se aplica ao protocolo B.' });
    expect(b.values.numbers).toMatchObject({ wristDiameterMm: null, fatKg: null, fatPct: 20 });
    expect(c.errors).toEqual({ fatPct: 'Este campo não se aplica ao protocolo C.', skeletalMusclePct: 'Este campo não se aplica ao protocolo C.' });
    expect(a.errors).toEqual({ informedBmi: 'Este campo não se aplica ao protocolo A.', waterKg: 'Este campo não se aplica ao protocolo A.' });
  });

  it.each([
    [undefined, 'Escolha o protocolo A, B ou C.'],
    ['D', 'Escolha o protocolo A, B ou C.'],
    ['a', 'Escolha o protocolo A, B ou C.'],
  ])('protocolo %p → %s', (protocol, message) => {
    const { values, errors } = run({ protocol, assessedOn: '2026-05-09' });
    expect(errors).toEqual({ protocol: message });
    expect(values.protocol).toBeNull();
  });

  it('método de TMB', () => {
    expect(run({ protocol: 'A', assessedOn: '2026-05-09', energyMethod: 'DRI_ADULTOS' }).values.energyMethod).toBe('DRI_ADULTOS');
    expect(run({ protocol: 'A', assessedOn: '2026-05-09', energyMethod: 'KATCH' }).errors).toEqual({ energyMethod: 'Método de TMB desconhecido.' });
  });

  it.each([
    [undefined, 'Informe a data da avaliação.'],
    ['2026-02-30', 'Data da avaliação inválida.'],
    ['2026-10-09', 'A data da avaliação não pode estar no futuro.'],
    ['1899-12-31', 'A data da avaliação deve ser a partir de 1900.'],
    ['1986-05-08', 'A data da avaliação não pode ser anterior ao nascimento do paciente.'],
  ])('data %p → %s', (assessedOn, message) => {
    const { values, errors } = run({ protocol: 'A', assessedOn });
    expect(errors).toEqual({ assessedOn: message });
    expect(values).toMatchObject({ assessedOn: null, ageYears: null });
  });
});

describe('toCalculationInput', () => {
  it('sem protocolo ou sem idade, não há cálculo', () => {
    expect(toCalculationInput(run({ assessedOn: '2026-05-09' }).values, 'FEMININO')).toBeNull();
    expect(toCalculationInput(run({ protocol: 'A' }).values, 'FEMININO')).toBeNull();
  });

  it('monta a entrada do cálculo com o sexo do cadastro', () => {
    const input = toCalculationInput(run({ protocol: 'A', assessedOn: '2026-05-09', weightKg: 80 }).values, 'FEMININO');
    expect(input).toMatchObject({ protocol: 'A', sex: 'FEMININO', ageYears: 40, weightKg: 80, heightCm: null, energyMethod: null });
  });
});

describe('rangeMessage', () => {
  it('as quatro formas', () => {
    expect(rangeMessage('O peso', RULES.limits.weightKg, ' kg')).toBe('O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.');
    expect(rangeMessage('A altura', RULES.limits.heightCm, ' cm')).toBe('A altura deve estar entre 30 e 250 cm, com até 2 casas decimais.');
    expect(rangeMessage('A gordura-alvo', RULES.limits.percent, '')).toBe('A gordura-alvo deve ser maior que 0 e menor que 100, com até 2 casas decimais.');
    expect(rangeMessage('O nível de gordura visceral', RULES.limits.visceralFatLevel, '')).toBe(
      'O nível de gordura visceral deve ser maior que 0, com até 2 casas decimais.',
    );
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- src/assessments`
Expected: FAIL com `Cannot find module './assessment-normalize'`.

- [ ] **Step 3: Implementar `assessment-normalize.ts`**

A data segue o mesmo modelo de `validDate` da anamnese, com "avaliação" no lugar de "anamnese" (mínimo 1900-01-01, sem futuro, não anterior ao nascimento). A tabela de campos e as regras de aplicabilidade são decisões e vão como estão:

```ts
const ALL: readonly AssessmentProtocol[] = ['A', 'B', 'C'];

/** Campos numéricos, rótulo das mensagens, limite técnico (avaliacao-rules.limits) e protocolos em que se aplicam (DIV-11). */
export const NUMERIC_FIELDS = [
  { field: 'weightKg', label: 'O peso', limit: 'weightKg', unit: ' kg', protocols: ALL },
  { field: 'heightCm', label: 'A altura', limit: 'heightCm', unit: ' cm', protocols: ALL },
  { field: 'waistCm', label: 'A cintura', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'hipCm', label: 'O quadril', limit: 'circumferenceCm', unit: ' cm', protocols: ALL },
  { field: 'fatPct', label: 'O percentual de gordura', limit: 'percent', unit: '', protocols: ['A', 'B'] },
  { field: 'wristDiameterMm', label: 'O diâmetro do punho', limit: 'diameterMm', unit: ' mm', protocols: ['A'] },
  { field: 'femurDiameterMm', label: 'O diâmetro do fêmur', limit: 'diameterMm', unit: ' mm', protocols: ['A'] },
  { field: 'informedBmi', label: 'O IMC informado', limit: 'bmi', unit: '', protocols: ['B', 'C'] },
  { field: 'skeletalMusclePct', label: 'O percentual de músculo esquelético', limit: 'percent', unit: '', protocols: ['B'] },
  { field: 'visceralFatLevel', label: 'O nível de gordura visceral', limit: 'visceralFatLevel', unit: '', protocols: ['B'] },
  { field: 'fatKg', label: 'O peso de gordura', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'boneKg', label: 'O peso da massa óssea', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'proteinKg', label: 'O peso de proteína', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'waterKg', label: 'O peso de água corporal', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'muscleKg', label: 'O peso da massa muscular', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'skeletalMuscleKg', label: 'O peso do músculo esquelético', limit: 'componentKg', unit: ' kg', protocols: ['C'] },
  { field: 'targetFatPct', label: 'A gordura-alvo', limit: 'percent', unit: '', protocols: ALL },
  { field: 'targetBmi', label: 'O IMC-alvo', limit: 'bmi', unit: '', protocols: ALL },
  { field: 'informedBmrKcal', label: 'A TMB informada', limit: 'bmrKcal', unit: ' kcal', protocols: ALL },
  { field: 'activityFactor', label: 'O fator de atividade', limit: 'activityFactor', unit: '', protocols: ALL },
] as const satisfies readonly { field: string; label: string; limit: LimitKey; unit: string; protocols: readonly AssessmentProtocol[] }[];

export type NumericField = (typeof NUMERIC_FIELDS)[number]['field'];
```

Para cada campo numérico com valor (`null` e `undefined` contam como ausência), nesta ordem:

1. Com protocolo válido e o campo fora de `protocols`: erro `Este campo não se aplica ao protocolo ${protocol}.` e valor `null`.
2. Fora do limite técnico, ou com mais de 2 casas: erro `rangeMessage(label, rules.limits[limit], unit)` e valor `null`. Para as 2 casas, use a mesma tolerância de `centesimalUpTo` da anamnese: `Math.abs(v * 100 − Math.round(v * 100)) < 1e-6`.
3. Nos demais casos, o valor passa.

Os outros campos:

- protocolo: só `'A' | 'B' | 'C'`; ausente ou fora disso dá `'Escolha o protocolo A, B ou C.'`;
- `energyMethod`: depois de `cleanText`, precisa ser um id de `rules.energy.methods`; senão, `'Método de TMB desconhecido.'`;
- `ageYears = ageOnDate(patient.birthDate, assessedOn)` quando a data é válida.

`rangeMessage` monta uma das quatro formas do teste. Os números saem com vírgula decimal (`String(n).replace('.', ',')`), e o sufixo é sempre `, com até 2 casas decimais.`:

- sem máximo: "deve ser maior que {min}";
- mínimo e máximo inclusivos: "deve estar entre {min} e {max}{unidade}";
- máximo exclusivo: "deve ser maior que {min} e menor que {max}{unidade}";
- demais casos: "deve ser maior que {min} e no máximo {max}{unidade}".

`toCalculationInput` devolve `null` sem protocolo ou sem idade. Nos demais casos, devolve `{ protocol, sex, ageYears, energyMethod, ...numbers }`.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api -- src/assessments`
Expected: PASS — `assessment-normalize.spec` (18).

- [ ] **Step 5: Commit**

```powershell
git add apps/api/src/assessments/assessment-normalize.ts apps/api/src/assessments/assessment-normalize.spec.ts
git commit -m "feat(api): validação da avaliação com limites técnicos, idade na data e campos por protocolo"
```

---

### Task 11: API — regras e prévia do cálculo, com permissões

**Files:**
- Create: `apps/api/src/assessments/assessment-rules.view.ts`, `apps/api/src/assessments/dto/assessment-calculation.dto.ts`, `apps/api/src/assessments/assessments.service.ts`, `apps/api/src/assessments/assessments.controller.ts`, `apps/api/src/assessments/assessments.module.ts`
- Modify: `apps/api/src/app.module.ts` (+ `AssessmentsModule`)
- Test: `apps/api/test/assessments-calculation.e2e-spec.ts`

**Interfaces:**
- Consumes: `normalizeAssessment`, `toCalculationInput` (Task 10); `calculateAssessment` (Tasks 7–9); `CURRENT_AVALIACAO_RULES`; `RequirePermissions`, `RequireAnyPermission`, `todayInTimeZone`, `formatIsoDate` e `APP_CONFIG`.
- Produces:
  - a rota `GET /api/assessments/rules`, que devolve `AssessmentRulesView = { version; status; protocols: ['A','B','C']; energyMethods: { id; label }[]; activityFactors: number[]; defaultActivityFactor: number; suggestedTargetBmi: number; limits: Record<LimitKey, Range> }`. Exige `assessments.draft` ou `assessments.finalize`.
  - a rota `POST /api/patients/:patientId/assessments/calculation`, que responde 200 com `{ calculation: AssessmentCalculation | null; errors: AssessmentFieldErrors }`, sem gravar nem auditar. Exige `patients.read` e mais `assessments.draft` ou `assessments.finalize`. Paciente inexistente dá 404 `'Paciente não encontrado.'`.

- [ ] **Step 1: Escrever os testes**

`apps/api/test/assessments-calculation.e2e-spec.ts`:

```ts
import { randomUUID } from 'node:crypto';
import { INestApplication } from '@nestjs/common';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';

const mulher = { fullName: 'Paciente Sintética Avaliação', sex: 'FEMININO', birthDate: '1986-05-09' };
const avaliacaoA = {
  protocol: 'A', assessedOn: '2026-05-09', weightKg: 80, heightCm: 170, waistCm: 80, hipCm: 100, fatPct: 20,
  wristDiameterMm: 50, femurDiameterMm: 90, energyMethod: 'HARRIS_BENEDICT', activityFactor: 1.55,
};

describe('Avaliações — regras e prévia do cálculo (HTTP)', () => {
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

  async function patientFor(agent: Agent): Promise<string> {
    const res = await agent.post('/api/patients').send(mulher);
    expect(res.status).toBe(201);
    return res.body.id as string;
  }

  async function setAssistant(admin: Agent, changes: { permission: string; granted: boolean }[]) {
    expect((await admin.put('/api/permissions/assistant').send({ changes })).status).toBe(200);
  }

  const calculate = (agent: Agent, patientId: string, body: Record<string, unknown> = avaliacaoA) =>
    agent.post(`/api/patients/${patientId}/assessments/calculation`).send(body);

  it('prévia da A com a idade na data da avaliação, sem gravar nem auditar', async () => {
    const { agent } = await loginAs(app, 'ASSISTENTE');
    const patientId = await patientFor(agent);
    const before = await prisma.auditEvent.count();

    const res = await calculate(agent, patientId);
    const dayBefore = await calculate(agent, patientId, { ...avaliacaoA, assessedOn: '2026-05-08' });

    expect(res.status).toBe(200);
    expect(res.body.errors).toEqual({});
    expect(res.body.calculation).toMatchObject({
      rulesVersion: 'avaliacao-v1', protocol: 'A', ageYears: 40, bmi: 27.68, bmiClassification: 'Sobrepeso', fatKg: 16,
      fatClassification: 'Bom', residualKg: 16.72, boneKg: 9.77, muscleKg: 37.51, waistHipRatio: 0.8,
      waistHipClassification: 'Risco Alto', suggestedTargetFatPct: 25, bmrKcal: 1548, expenditureKcal: 2399.4,
    });
    expect(dayBefore.body.calculation.ageYears).toBe(39);
    expect(await prisma.auditEvent.count()).toBe(before);
  });

  it('erros por campo e campo de outro protocolo, calculando o que der', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);

    const res = await calculate(agent, patientId, { protocol: 'B', assessedOn: '2026-05-09', weightKg: 0, wristDiameterMm: 50, waistCm: 80, hipCm: 100 });

    expect(res.status).toBe(200);
    expect(res.body.errors).toEqual({
      weightKg: 'O peso deve ser maior que 0 e no máximo 500 kg, com até 2 casas decimais.',
      wristDiameterMm: 'Este campo não se aplica ao protocolo B.',
    });
    expect(res.body.calculation).toMatchObject({ protocol: 'B', totalWeightKg: null, waistHipRatio: 0.8, waistHipReference: '0,73 - 0,79' });
  });

  it('sem protocolo ou sem data, não há cálculo', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);

    const res = await calculate(agent, patientId, { weightKg: 80 });

    expect(res.status).toBe(200);
    expect(res.body).toEqual({
      calculation: null,
      errors: { protocol: 'Escolha o protocolo A, B ou C.', assessedOn: 'Informe a data da avaliação.' },
    });
  });

  it('regras para o formulário do Plano 4', async () => {
    const { agent } = await loginAs(app, 'ASSISTENTE');

    const res = await agent.get('/api/assessments/rules');

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({
      version: 'avaliacao-v1', status: 'PROVISORIA', protocols: ['A', 'B', 'C'], defaultActivityFactor: 1, suggestedTargetBmi: 22.5,
    });
    expect(res.body.energyMethods.map((m: { id: string }) => m.id)).toEqual(['HARRIS_BENEDICT', 'OMS', 'DRI_ADOLESCENTES', 'DRI_ADULTOS', 'BIOIMPEDANCIA']);
    expect(res.body.activityFactors).toHaveLength(21);
    expect(res.body.limits.bodyScore).toEqual({ min: 0, minInclusive: true, max: 999, maxInclusive: true, integer: true });
    expect(res.body.limits.visceralFatLevel.max).toBeNull();
  });

  it('permissões: cada combinação isola uma permissão', async () => {
    const professional = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(professional.agent);
    const assistant = await loginAs(app, 'ASSISTENTE');
    const admin = await loginAs(app, 'ADMIN');
    const statuses = async () => [(await assistant.agent.get('/api/assessments/rules')).status, (await calculate(assistant.agent, patientId)).status];

    // Padrão do assistente: patients.read + assessments.draft.
    expect(await statuses()).toEqual([200, 200]);

    // Sem assessments.draft e sem finalize: as permissões de anamnese (que o assistente mantém) não bastam.
    await setAssistant(admin.agent, [{ permission: 'assessments.draft', granted: false }]);
    expect((await assistant.agent.get('/api/auth/me')).body.permissions).toEqual(expect.arrayContaining(['anamneses.draft', 'patients.read']));
    expect(await statuses()).toEqual([403, 403]);

    // Só assessments.finalize (delegação): basta uma das duas.
    await setAssistant(admin.agent, [{ permission: 'assessments.finalize', granted: true }]);
    expect(await statuses()).toEqual([200, 200]);

    // Sem patients.read: as regras continuam; a prévia (que usa o cadastro) não, e o 403 não traz dado.
    await setAssistant(admin.agent, [
      { permission: 'assessments.finalize', granted: false },
      { permission: 'assessments.draft', granted: true },
      { permission: 'patients.read', granted: false },
    ]);
    const denied = await calculate(assistant.agent, patientId);
    expect(await statuses()).toEqual([200, 403]);
    expect(denied.body).not.toHaveProperty('calculation');
    expect(JSON.stringify(denied.body)).not.toContain('1986');
  });

  it('404 para paciente inexistente; 400 para id inválido e para campo desconhecido', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patientId = await patientFor(agent);

    const missing = await calculate(agent, randomUUID());
    expect(missing.status).toBe(404);
    expect(missing.body.message).toBe('Paciente não encontrado.');
    expect((await calculate(agent, 'abc')).status).toBe(400);
    expect((await calculate(agent, patientId, { ...avaliacaoA, pontuacao: 10 })).status).toBe(400);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- test/assessments-calculation.e2e-spec.ts`
Expected: FAIL. As rotas respondem 404 (`Cannot POST /api/patients/.../assessments/calculation`, `Cannot GET /api/assessments/rules`).

- [ ] **Step 3: DTO, visão e serviço**

`apps/api/src/assessments/dto/assessment-calculation.dto.ts`:

- `protocol`, `assessedOn` e `energyMethod` levam `@IsOptional() @IsString() @MaxLength(…)` (1, 10 e 40), para que a falta deles receba a mensagem em português da validação.
- Cada campo de `NUMERIC_FIELDS` leva `@IsOptional() @IsNumber({ allowNaN: false, allowInfinity: false })` e o tipo `number | null`, na ordem de `NUMERIC_FIELDS`.
- O corpo é JSON, então não use `@Type`.

`apps/api/src/assessments/assessment-rules.view.ts`: `toAssessmentRulesView(rules)` monta a visão da interface:

- `protocols: ['A', 'B', 'C']`;
- `energyMethods` com cópia de `{ id, label }`;
- `activityFactors: [...rules.energy.activityFactors]`;
- `limits`: cópia rasa de cada `Range`.

`apps/api/src/assessments/assessments.service.ts`:

```ts
import { Inject, Injectable, NotFoundException } from '@nestjs/common';
import { calculateAssessment, type AssessmentCalculation } from '../clinical-rules/assessment-calculation';
import { CURRENT_AVALIACAO_RULES } from '../clinical-rules/avaliacao-rules';
import { formatIsoDate, todayInTimeZone } from '../common/civil-date';
import { APP_CONFIG, AppConfig } from '../config';
import { PrismaService } from '../prisma/prisma.service';
import { normalizeAssessment, toCalculationInput, type AssessmentFieldErrors, type AssessmentInput } from './assessment-normalize';
import { toAssessmentRulesView, type AssessmentRulesView } from './assessment-rules.view';

export interface AssessmentCalculationResult {
  calculation: AssessmentCalculation | null;
  errors: AssessmentFieldErrors;
}

@Injectable()
export class AssessmentsService {
  constructor(
    private readonly prisma: PrismaService,
    @Inject(APP_CONFIG) private readonly config: AppConfig,
  ) {}

  rules(): AssessmentRulesView {
    return toAssessmentRulesView(CURRENT_AVALIACAO_RULES);
  }

  /** Prévia sem gravação: valida contra o cadastro (idade na data, DIV-15) e calcula o que os campos válidos permitem. */
  async calculate(patientId: string, input: AssessmentInput): Promise<AssessmentCalculationResult> {
    const patient = await this.prisma.patient.findUnique({ where: { id: patientId } });
    if (!patient) throw new NotFoundException('Paciente não encontrado.');
    const { values, errors } = normalizeAssessment(
      input,
      { sex: patient.sex, birthDate: formatIsoDate(patient.birthDate) },
      CURRENT_AVALIACAO_RULES,
      todayInTimeZone(this.config.clinicTimeZone),
    );
    const calculationInput = toCalculationInput(values, patient.sex);
    return { calculation: calculationInput === null ? null : calculateAssessment(CURRENT_AVALIACAO_RULES, calculationInput), errors };
  }
}
```

- [ ] **Step 4: Controller, módulo e registro**

`apps/api/src/assessments/assessments.controller.ts`:

```ts
import { Body, Controller, Get, HttpCode, Param, ParseUUIDPipe, Post } from '@nestjs/common';
import { RequireAnyPermission, RequirePermissions } from '../auth/require-permissions.decorator';
import type { AssessmentRulesView } from './assessment-rules.view';
import { AssessmentsService, type AssessmentCalculationResult } from './assessments.service';
import { AssessmentCalculationDto } from './dto/assessment-calculation.dto';

/** Basta uma das duas; as permissões de anamnese não valem aqui. A prévia usa o cadastro: exige também patients.read. */
const DRAFT_OR_FINALIZE = ['assessments.draft', 'assessments.finalize'] as const;

@Controller()
export class AssessmentsController {
  constructor(private readonly assessments: AssessmentsService) {}

  @Get('assessments/rules')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  rules(): AssessmentRulesView {
    return this.assessments.rules();
  }

  @Post('patients/:patientId/assessments/calculation')
  @HttpCode(200)
  @RequirePermissions('patients.read')
  @RequireAnyPermission(...DRAFT_OR_FINALIZE)
  calculate(@Param('patientId', new ParseUUIDPipe()) patientId: string, @Body() body: AssessmentCalculationDto): Promise<AssessmentCalculationResult> {
    return this.assessments.calculate(patientId, body);
  }
}
```

`assessments.module.ts` segue o padrão do `AnamnesesModule`: `@Module({ controllers: [AssessmentsController], providers: [AssessmentsService] })`. Acrescente `AssessmentsModule` ao fim dos imports de `apps/api/src/app.module.ts`.

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/api; npm run typecheck -w @psm/api`
Expected: PASS. `assessments-calculation.e2e-spec` passa nos 6 testes, e as suítes anteriores, inclusive as da anamnese, seguem verdes. `typecheck` sai sem erros.

- [ ] **Step 6: Commit**

```powershell
git add apps/api/src/assessments/assessment-rules.view.ts apps/api/src/assessments/dto/assessment-calculation.dto.ts apps/api/src/assessments/assessments.service.ts apps/api/src/assessments/assessments.controller.ts apps/api/src/assessments/assessments.module.ts apps/api/src/app.module.ts apps/api/test/assessments-calculation.e2e-spec.ts
git commit -m "feat(api): regras e prévia do cálculo das avaliações com permissões próprias"
```

---

### Task 12: Homologação pela API, README e verificação final

**Files:**
- Create: `apps/api/test/assessments-homologacao.e2e-spec.ts`
- Modify: `README.md`

**Interfaces:**
- Consumes: as rotas da Task 11.
- Produces: os casos H executados ponta a ponta pela API, e o README com a entrega e a verificação do Plano 3.

- [ ] **Step 1: Escrever os casos H pela API**

`apps/api/test/assessments-homologacao.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';

/** Casos H (divergencias-e-homologacao.md §4) e bloco L, com os valores observados no Excel em 2026-10-08. */
describe('Homologação das avaliações pela API', () => {
  let app: INestApplication;
  let prisma: PrismaService;
  let agent: Agent;
  const ids: Record<string, string> = {};

  beforeAll(async () => {
    app = await createTestApp();
    prisma = app.get(PrismaService);
    await resetDb(prisma);
    agent = (await loginAs(app, 'PROFISSIONAL')).agent;
    const patients = {
      f40: { fullName: 'Sintética Quarenta', sex: 'FEMININO', birthDate: '1986-05-09' },
      m40: { fullName: 'Sintético Quarenta', sex: 'MASCULINO', birthDate: '1986-05-09' },
      f25: { fullName: 'Sintética Vinte e Cinco', sex: 'FEMININO', birthDate: '2001-05-09' },
      bissexto: { fullName: 'Sintética Bissexta', sex: 'FEMININO', birthDate: '2000-02-29' },
    };
    for (const [key, body] of Object.entries(patients)) ids[key] = (await agent.post('/api/patients').send(body)).body.id;
  });

  afterAll(async () => {
    await app.close();
  });

  async function calc(patient: string, body: Record<string, unknown>) {
    const res = await agent.post(`/api/patients/${ids[patient]}/assessments/calculation`).send({ assessedOn: '2026-05-09', ...body });
    expect(res.status).toBe(200);
    expect(res.body.errors).toEqual({});
    return res.body.calculation;
  }

  it('H-01: 80 kg e 20% → gordura 16,00 kg; massa livre 64,00 kg e 80,00%', async () => {
    expect(await calc('f40', { protocol: 'A', weightKg: 80, fatPct: 20 })).toMatchObject({ fatKg: 16, fatFreeKg: 64, fatFreePct: 80 });
  });

  it('H-02: A, 80 kg e 200 cm → IMC 20,00 "Peso Normal"', async () => {
    expect(await calc('f40', { protocol: 'A', weightKg: 80, heightCm: 200 })).toMatchObject({ bmi: 20, bmiClassification: 'Peso Normal' });
  });

  it('H-03: cintura 80 e quadril 100 → 0,80 "Risco Alto" (Feminino 40); sem quadril, RCQ vazio', async () => {
    expect(await calc('f40', { protocol: 'A', waistCm: 80, hipCm: 100 })).toMatchObject({ waistHipRatio: 0.8, waistHipClassification: 'Risco Alto' });
    expect(await calc('f40', { protocol: 'A', waistCm: 80 })).toMatchObject({ waistHipRatio: null, waistHipClassification: null });
  });

  it('H-04: C, 16 kg de gordura com 80 kg → 20,00%; peso total 100,00%', async () => {
    expect(await calc('f40', { protocol: 'C', weightKg: 80, fatKg: 16 })).toMatchObject({ fatPct: 20, totalWeightKg: 80, totalWeightPct: 100 });
  });

  it('H-05: HB, Feminino, 60 kg, 165 cm, 40 anos → TMB 1347', async () => {
    expect(await calc('f40', { protocol: 'A', weightKg: 60, heightCm: 165, energyMethod: 'HARRIS_BENEDICT' })).toMatchObject({ bmrKcal: 1347 });
  });

  it('H-06: OMS, Feminino, 60 kg, 40 anos, fator 1,55 → TMB 1351,00; GET 2094,05', async () => {
    expect(await calc('f40', { protocol: 'A', weightKg: 60, energyMethod: 'OMS', activityFactor: 1.55 })).toMatchObject({
      bmrKcal: 1351, expenditureKcal: 2094.05,
    });
  });

  it('H-07: B, 80 kg, 20%, alvo 15% → peso-alvo 75,29 kg; controle −4,71 kg', async () => {
    expect(await calc('m40', { protocol: 'B', weightKg: 80, fatPct: 20, targetFatPct: 15 })).toMatchObject({
      targetWeightByFatKg: 75.29, weightControlByFatKg: -4.71,
    });
  });

  it('H-08 (DIV-02): A, IMC-alvo 22, 170 cm → 63,58 kg (o legado dava 0,00)', async () => {
    expect(await calc('f40', { protocol: 'A', weightKg: 80, heightCm: 170, targetBmi: 22 })).toMatchObject({
      targetWeightByBmiKg: 63.58, weightControlByBmiKg: -16.42,
    });
  });

  it('H-09 (DIV-10): C, 70 kg; água 41,3 kg → 59,00%; proteína 11,7 kg → 16,71%', async () => {
    expect(await calc('f40', { protocol: 'C', weightKg: 70, waterKg: 41.3, proteinKg: 11.7 })).toMatchObject({
      waterKg: 41.3, waterPct: 59, proteinKg: 11.7, proteinPct: 16.71,
    });
  });

  it('H-10 (DIV-15): nascida em 29/02/2000 → 25 anos em 28/02/2026 e 26 em 01/03/2026', async () => {
    expect((await calc('bissexto', { protocol: 'A', assessedOn: '2026-02-28' })).ageYears).toBe(25);
    expect((await calc('bissexto', { protocol: 'A', assessedOn: '2026-03-01' })).ageYears).toBe(26);
  });

  it('H-23 (DIV-01): DRI adultos, Masculino, 40 anos, 90 kg, 173 cm → 1841 (legado 1875)', async () => {
    expect(await calc('m40', { protocol: 'A', weightKg: 90, heightCm: 173, energyMethod: 'DRI_ADULTOS' })).toMatchObject({ bmrKcal: 1841 });
  });

  it('bloco L: Feminino 25, quadril 100 — cintura 82,4 → 0,82 "Risco Alto"; 82,5 → 0,83 "Muito Alto"', async () => {
    expect(await calc('f25', { protocol: 'A', waistCm: 82.4, hipCm: 100 })).toMatchObject({ waistHipRatio: 0.82, waistHipClassification: 'Risco Alto' });
    expect(await calc('f25', { protocol: 'A', waistCm: 82.5, hipCm: 100 })).toMatchObject({ waistHipRatio: 0.83, waistHipClassification: 'Muito Alto' });
  });
});
```

- [ ] **Step 2: Rodar**

Run: `npm test -w @psm/api -- test/assessments-homologacao.e2e-spec.ts`
Expected: PASS (12). Esta suíte reúne, pela API, regras já cobertas nas Tasks 1–11; ela não deve exigir código novo. Se um caso falhar, a divergência está na integração e tem de ser investigada antes de seguir. Não ajuste o valor esperado.

- [ ] **Step 3: Verificação automática completa**

Run: `npm test; npm run typecheck; npm run build`
Expected:
- a API passa com 46 suítes: as 30 anteriores e 16 novas (`rounding`, `banded-table`, `avaliacao-rules`, `age-band`, `bmi`, `body-fat`, `waist-hip`, `composition`, `targets`, `energy`, `assessment-calculation-a/b/c`, `assessment-normalize`, `assessments-calculation.e2e` e `assessments-homologacao.e2e`);
- a web continua com os arquivos de teste que já tinha, sem mudança;
- `typecheck` sai sem erros e o `build` gera `apps/api/dist/` e `apps/web/dist/`;
- `git grep -n "TODO(PREMISSA-CLINICA PC-"` lista PC-01 a PC-13, nos arquivos indicados em `docs/premissas-clinicas.md`.

- [ ] **Step 4: PONTO DE CONTROLE DA PESSOA — verificação final**

Mostre à pessoa:

1. A saída de `npx jest test/assessments-homologacao --verbose` (em `apps/api`): os 12 casos, com os valores observados no Excel.
2. `docs/premissas-clinicas.md`, com PC-05 a PC-13.
3. A lista de pendências que ficam para o Plano 4. Nenhuma delas bloqueia este plano:
   - conferir no Excel as legendas dos métodos de TMB;
   - conferir a unidade (mm) dos diâmetros de punho e fêmur no formulário;
   - conferir os textos "18,5 - 24,9" e "<= 9" da B;
   - classificações manuais da B e da C (CL-08, DIV-05);
   - limite da pontuação no formulário.

A pessoa anota OK ou a divergência em cada caso. Só siga para o Step 5 com o "ok".

- [ ] **Step 5: Atualizar o README**

Acrescente ao fim de `README.md`:

```markdown
## O que o Plano 3 entrega

Motor de regras das avaliações A, B/R e C, em módulos puros (`apps/api/src/clinical-rules/`) com a configuração versionada `avaliacao-v1`. Ele cobre:

- faixa etária;
- IMC e classificação por idade;
- gordura, massa livre e classificação;
- composição da A (residual, óssea e muscular);
- percentuais da C;
- músculo esquelético e intervalos da B;
- RCQ (arredondado antes de classificar);
- sugestões e peso-alvo;
- TMB (Harris-Benedict, OMS, DRI com IMC em kg/m² e bioimpedância) e GET.

O arredondamento é meio para cima em todas as regras.

A API expõe `GET /api/assessments/rules` e a prévia `POST /api/patients/:patientId/assessments/calculation`, que não grava nada. As premissas clínicas provisórias estão em [docs/premissas-clinicas.md](docs/premissas-clinicas.md) (PC-05 a PC-13).

Fora deste plano: gravação das avaliações, telas, rascunho e finalização, revisões e classificações manuais da B e da C (Plano 4).

## Verificação do Plano 3

Data: <AAAA-MM-DD>. Casos H pela API (`test/assessments-homologacao.e2e-spec.ts`), 12 casos: <OK ou divergências aceitas>.
```

Preencha com o que a pessoa anotou no Step 4.

- [ ] **Step 6: Commit**

```powershell
git add apps/api/test/assessments-homologacao.e2e-spec.ts README.md
git commit -m "test(api): homologação dos casos H pela API e documentação do Plano 3"
```

---

## Fora do escopo deste plano

- Tabela de avaliações, gravação com `rulesVersion`, telas por protocolo, rascunho, finalização, revisões e concorrência: Plano 4.
- Classificações manuais da B e da C (CL-08) e o mapeamento completo das opções do FrmBioimpC (DIV-05): Plano 4.
- Tabela pediátrica `Imc_CA_CAM` (DIV-03): só numa versão futura, com validação clínica.
- Equações de dobras cutâneas (Pollock, Slaughter, Petroski, Guedes etc.): não têm chamador demonstrado no legado.
- Documentos em PDF (Plano 5) e OCR (Plano 6).
- Tabela de faixas editável no banco: as regras só mudam por uma nova versão no código.
- Validação clínica das premissas PC-05 a PC-13: feita pelo médico responsável, fora do código. Uma correção vira `avaliacao-v2`.
