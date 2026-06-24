# TAREFA: Elaborar o SRD (Software Requirements Document) do micro-SaaS de Bioimpedância

## CONTEXTO
Já existe um PRD completo gerado por engenharia reversa em `C:\Sistema\PSM\Projeto 14\zanoni\PRD.md` (incluindo a seção "Follow-up" com fórmulas/tabelas transcritas do VBA). LEIA o PRD.md INTEGRALMENTE primeiro — ele é a fonte de verdade das regras de negócio. As evidências de origem estão em `_prd_evidence/`.

## OBJETIVO
Produzir um SRD detalhado e implementável que sirva de base direta para o desenvolvimento, derivado do PRD, com rastreabilidade SRD -> PRD.

## DECISÕES TÉCNICAS FIXADAS (pelo usuário)
- Backend: **Node.js + NestJS**
- Frontend: **React + Vite**
- Arquitetura: **Multitenant** (defina e justifique a estratégia: schema/row-level/DB por tenant — recomende a mais adequada e marque alternativas)
- Escopo desta fase: **NÚCLEO / MVP** = fluxo `cadastro de cliente -> avaliação (R/A/C) -> motor de cálculos -> emissão de laudo PDF`. Integrações avançadas, comparações/evolução, administração de SaaS e anamnese completa podem ser citadas como **Fase 2 (fora do escopo do MVP)**.

## ENTREGÁVEL
Gere `C:\Sistema\PSM\Projeto 14\zanoni\SRD.md` contendo:

1. **Introdução**: objetivo, escopo do MVP, definições/acrônimos, referências (PRD).
2. **Visão geral do sistema**: contexto, diagrama de alto nível (componentes: frontend, API, DB, gerador de PDF), premissas e restrições.
3. **Arquitetura**:
   - Estratégia multitenant escolhida + justificativa
   - Estrutura de módulos NestJS (ex.: Auth/Tenant, Clients, Assessments, Calculation engine, Reports/PDF)
   - Estrutura do frontend React/Vite (rotas, telas do núcleo, gerência de estado)
   - Autenticação/autorização (RBAC: profissional, assistente, admin do tenant)
4. **Requisitos funcionais (RF)**: numerados (RF-001...), cada um com descrição, atores, pré-condições, fluxo principal, fluxos alternativos/exceções e critérios de aceite. Cobrir todo o núcleo.
5. **Motor de cálculo (seção crítica)**: especifique cada cálculo como função pura testável, com assinatura (entradas/tipos/unidades), fórmula EXATA conforme PRD/VBA, arredondamentos, e referência à origem. Inclua IMC, %gordura/peso gordo, MLG, massa muscular, RCQ + classificações (TabelaIMC, TabelaGordura, TabelaRCQ, intervalos), gordura alvo, peso alvo, TMB (Harris-Benedict, OMS), DRI adolescentes/adultos, GET (fator × TMB). NÃO altere fórmulas — preserve inclusive as inconsistências do legado e SINALIZE-as como "decisão pendente de validação".
6. **Modelo de dados técnico**: entidades, atributos com tipos, chaves, relacionamentos, isolamento por tenant; esboço de schema (tabelas/colunas) compatível com Postgres (assuma Postgres salvo justificativa).
7. **Especificação de API REST**: endpoints do núcleo (método, path, request/response schema resumido, códigos de status, autorização). Agrupar por módulo.
8. **Regras de validação**: máscaras/validações de cadastro (CPF, telefone, CEP, datas), validações das avaliações (campos obrigatórios, RCQ exige cintura/quadril != 0, etc.).
9. **Geração de laudo PDF**: requisitos de layout (seções do laudo conforme PRD), variantes M/H, identidade visual configurável por tenant, abordagem técnica sugerida.
10. **Requisitos não-funcionais (RNF)**: numerados e MENSURÁVEIS — desempenho, segurança, LGPD (criptografia, controle de acesso, auditoria, retenção), disponibilidade, observabilidade/logs, backup, i18n/pt-BR, acessibilidade básica.
11. **Estratégia de testes**: unitários (motor de cálculo com casos derivados das tabelas), integração, e2e do fluxo núcleo.
12. **Matriz de rastreabilidade**: RF/RNF -> seção do PRD de origem.
13. **Itens em aberto / A VALIDAR**: decisões que dependem do especialista de domínio (ex.: inconsistências de tabela do legado, IMC sem conversão de altura, fator de atividade default).

## REGRAS
- NÃO invente regras de negócio: use o PRD como fonte; se algo não estiver no PRD, marque como "A VALIDAR".
- Preserve fielmente fórmulas e faixas; o SRD deve ser implementável sem precisar reabrir a planilha.
- Use numeração consistente (RF-XXX, RNF-XXX) para permitir rastreabilidade.

## RELATÓRIO FINAL
Reporte: caminho absoluto do SRD.md, contagem de RF/RNF, principais decisões de arquitetura, e a lista de itens A VALIDAR.
