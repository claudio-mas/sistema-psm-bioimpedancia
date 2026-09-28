# Cerebrum

> OpenWolf's learning memory. Updated automatically as the AI learns from interactions.
> Do not edit manually unless correcting an error.
> Last updated: 2026-06-24

## User Preferences

<!-- How the user likes things done. Code style, tools, patterns, communication. -->

## Key Learnings

- **Project:** zanoni
- **Description:** Suplemento do Microsoft Excel (VSTO / C#, .NET Framework 4.7.2) que adiciona um
- **Coluna "Avaliação" do laudo = transcrição verbatim.** O quadro *Análise da composição
  corporal* lê 7 células de texto da aba "Avaliações" e imprime sem calcular. `LaudoAvaliacaoHtmlBuilder.Cls()`
  só converte `N/A` / `Não Calculado` / vazio em `-`; qualquer outro texto passa intacto.
  Não há whitelist nem normalização de rótulos.
- **Mapeamento dos 7 componentes (offset A=0):** Peso→`PesoTotalClassif`/BT(71),
  Gordura→`GorduraClassif`/AH(33), Sal inorgânico→`OsseaClassif`/BB(53),
  Proteína→`ProteinaClassif`/AY(50), Água→`AguaClassif`/AW(48),
  Músculo→`PesoMuscularClassif`/AL(37), Músculo esquelético→`MusculoClassif`/AQ(42).
  Definidos em `LaudoRepositorio.cs` (OFF_*) e usados em `Composicao()`.
- **Fluxo C (2026-06-24): rótulos padronizados** para Baixo · Saudável · Alto · Excelente nos
  7 grupos do `FrmBioimpC`. **Mudança é 100% VBA/formulário — o C# não muda nem recompila**,
  porque grava nas mesmas colunas e o C# transcreve verbatim.
- **Gotcha VBA:** mudar a `Caption` do radio no form só basta se o `SalvaAvaliacaoC` gravar
  `optSelecionado.Caption`. Se gravar literais fixos (`If optBaixo Then ... = "Baixo"`), os
  literais também precisam ser atualizados.
- **Acento importa:** "Saudável" deve ser gravado com acento (planilha e laudo são Unicode/UTF-8).
- **BIA.pdf (laudo Relaxmedic) é imagem pura** — 1 página, 1 JPEG, zero texto (`pdftotext`/`pypdf`→0). Extrair dados exige OCR, não parse.
- **OCR sem dependência nova:** `Windows.Media.Ocr` (nativo Win10/11, offline, pt-BR) via `powershell.exe -EncodedCommand` (script embutido em C#, env vars `OCR_IN`/`OCR_OUT`, saída TSV). Espelha o padrão wkhtmltopdf (processo externo). Evita referenciar WinRT dentro do .NET Fw 4.7.2.
- **OCR de página inteira embaralha a ordem de leitura** (números de gráficos entram no meio). Solução: **OCR zonal** — cada palavra tem `BoundingRect`; casa-se por retângulo normalizado (per-mil). Ver `RelatorioRelaxmedicLayout.cs` (zonas calibradas contra BIA.pdf, 39 campos, 100% de acerto no teste).
- **Extrair o JPEG do PDF sem lib:** varredura de bytes pelo maior bloco `FFD8..FFD9` (laudo tem 1 JPEG DCTDecode). Sem PdfPig/NuGet — o `.csproj` é clássico sem restore.
- **VBA não é editável por arquivo** (fica em `vbaProject.bin` binário). C# gera-se por completo; VBA entrega-se pronto p/ colar (ver `Importacao/FrmBioimpC-ImportarPdf.vba.txt`).
- **Testar a DLL VSTO fora do Excel:** `powershell.exe` + `[Reflection.Assembly]::Load(bytes)` + invoke reflexivo do método estático. Provou o pipeline OCR ponta a ponta sem abrir o Excel.
- **Gotcha MSForms + Date (classe geral, 2+ ocorrências confirmadas):** qualquer atribuição direta de célula-Date crua pra controle MSForms (`ListBox.List` ou `TextBox.Value`) sem `Format` explícito faz o controle renderizar usando o locale regional do Windows (mês/dia/ano), **ignorando** o número-formato `dd/mm/aaaa` da célula/planilha. Fix sempre: `Format(valor, "dd/mm/yyyy")` explícito antes de popular o controle.
  - Ocorrência 1: `FiltrosLTB.bas` / `Sub FiltroB` (form `FrmRelatorioB`, ListBox, coluna "Data Aval." = `Arr(i,2)`, mesmo offset de `OFF_DATA=1` em `LaudoRepositorio.cs`).
  - Ocorrência 2: `AvaliacaoC.bas` / `Sub EditaAvaliacaoC` (form `FrmAvaliacaoC`, TextBox `TxtData`, linha `TxtData = ActiveCell.offset(0,1).Value` sem Format — só esse campo, os demais da mesma Sub já formatam).
  - Ocorrência 3 e 4 (confirmadas por leitura, mesma linha copiada nos 3 protocolos): `Sub EditaAvaliacaoB` linha 6593 e `Sub EditaAvaliacaoA` linha 20550 — idêntico ao `EditaAvaliacaoC`, `Frm...TxtData = ActiveCell.offset(0,1).Value` sem Format. Fix igual (IsDate + Format "dd/mm/yyyy") entregue mas não confirmado colado pelo usuário ainda.
  - **Suspeita não verificada:** `FiltroAN`/`FiltroClientes` se tiverem coluna de data.
- **Unidade das colunas de valor (fluxo C) NÃO é uniforme:** Água (offset 47) e Proteína (offset 49)
  guardam **percentual**; Óssea tem par separado (col51=% , col52=kg=`PesoOsseo`); Gordura idem
  (col31=%, col32=kg). Prova: o relatório VBA interno (`Planilha11/12/14/17/18`) lê 31/47/49/51 com
  **`/100`** e 32/52 direto. `LaudoRepositorio.cs` (OFF_AGUA=47/OFF_PROT=49) tratava 47/49 como kg → kg
  errado no laudo; corrigido derivando kg=%/100×peso (bug-008). **Não** trocar 47/49 p/ kg: quebra o relatório VBA.
- **Cadeia de dados do fluxo C (bioimpedância):** `FrmBioimpC` (digitação/import PDF) ⇄ `FrmAvaliacaoC`
  via `CommandButton179_Click` (aplica, olevba~15738) e `UserForm_Initialize` (recarrega, ~16362) →
  `SalvaAvaliacaoC()` (olevba~17642) grava a linha na aba "Avaliações" por `ActiveCell.offset(0,N)` →
  `LaudoRepositorio` lê os offsets → builder monta o laudo. Os offsets do C# espelham exatamente essa Sub.
- **`FrmAvaliacaoC` auto-calcula `TxtPesoGordo`** (gordura kg) = `TxtPGorduraAtual`/100×`TxtPesoKg`
  (olevba~16955) — col32 sempre populada sem precisar de transfer do FrmBioimpC.
- **Rótulos do laudo (`Composicao`) casam com os captions do FrmBioimpC:** "Sal inorgânico", "Músculo",
  "Músculo esquelético", "Proteína", "Água corporal", "Gordura corporal", "Peso". Renomear captions do
  form é cosmético (nenhuma rotina VBA lê caption de Frame; controles novos são ignorados por nome).
- **Coluna "%" do laudo é calculada** (`Prop`=kg/peso) p/ Gordura/Sal inorgânico/Proteína/Água — ignora
  o %-medido (col31/col51). Só Músculo usa col34 (TaxaMuscular) e Músculo esq. usa col40 quando >0.

## Do-Not-Repeat

<!-- Mistakes made and corrected. Each entry prevents the same mistake recurring. -->
<!-- Format: [YYYY-MM-DD] Description of what went wrong and what to do instead. -->

## Decision Log

<!-- Significant technical decisions with rationale. Why X was chosen over Y. -->
- **2026-08-03 — Produto da clínica é projeto SEPARADO, on-premise.** Novo PRD em
  `C:\Projetos\sistema-fz\docs\PRD.md` (repo git próprio, docs-only): app **web local
  single-tenant** (Node/TS + React) num servidor da clínica, acessada por navegador (resolve o Mac da
  rede), 2 perfis (profissional/assistente), escopo v1 = núcleo clínico + anamnese + config/backup,
  **sem migração de dados**, laudos idênticos aos atuais. **O `PRD.md` deste repo (micro-SaaS
  multi-inquilino) continua válido e não foi alterado** — são dois produtos distintos. Import OCR do
  aparelho fica p/ Fase 2.
- **2026-08-03 — Onde ficam os pedidos da Dra. que não estão em doc nenhum:** transcrições de sessão
  em `~/.claude/projects/c--Sistema-PSM-Projeto-14-zanoni/*.jsonl` (extrair mensagens `type=user`),
  `.wolf/buglog.json` e `~/.claude/plans/*.md`. Foi assim que as 13 mudanças pedidas entre 24/06 e
  03/08 foram recuperadas para o PRD novo.
- **2026-07-16 — Composição corporal: digitar só kg, planilha calcula % (uniforme nos 7 itens).** O
  laudo Relaxmedic dá kg **e** %, e o % = kg/peso×100 exato (peso=100%). Escolhido: kg = input, % =
  calculado (`RecalcComposicao` no FrmBioimpC). **Mantidas as colunas com o significado atual** (kg onde
  é kg, % onde é %) em vez de trocar p/ kg — assim o relatório VBA interno (que faz `/100` em 31/47/49/51)
  e o laudo C# seguem válidos; muda só qual campo o operador digita. Água/Proteína (só têm coluna de %,
  47/49): grava o % calculado, laudo C# deriva o kg (bug-008). Fiação nova fica no `CommandButton179`
  (óssea %→col51 via TextBox50; músculo %→col34 via TxtTaxaMuscular); `SalvaAvaliacaoC` **inalterado**.
- **2026-07-16 — Import OCR passa a ler só a coluna kg** (`RelatorioRelaxmedicLayout.cs`), pois os campos
  de % viram calculados. Retarget: TxtProteina→TxtProteinaKg, TxtAguaCorporal→TxtAguaCorporalKg,
  TxtMassaEsqueletica→TxtPesoMuscularEsq, e gordura movida da coluna % p/ a coluna kg (TxtGorduraAtualKg).
  Ver [[bug-009]].

- **2026-06-24 — Rótulos de avaliação como texto simples (não cor por nível).** Escolhido para
  manter a mudança em zero alteração de C# (só VBA/form). Cor por nível exigiria
  `LaudoAvaliacaoHtmlBuilder` + CSS + recompilar/republicar — descartado.

## Contextualização verificada em 2026-09-27

- XML do Bioimpedancia.xlsm atual: 25 abas; Avaliações com dimensão A1:DD3 e sem fórmulas de célula; Analise C com 21 fórmulas. A extração _prd_evidence/olevba.txt (23/06/2026) é anterior ao XLSM (14/08/2026); não presumir que reproduz o VBA atual.
- Publicar-NovaVersao.cmd aponta para G:\Meu Drive\Sistema FZ\Suplemento\; README ainda cita Sistema PSM. Nesta máquina, o executável MSBuild no caminho Professional 2022 configurado não foi encontrado; wkhtmltopdf local existe. Nenhum build foi executado nesta análise.

## Decisões confirmadas — migração web, 2026-09-27

- Usuário definiu uma clínica na nuvem, até cinco usuários simultâneos, React + TypeScript, NestJS e PostgreSQL. Importar somente cadastros; histórico anterior consultado na planilha. Todos os protocolos A/B/R/C (três fluxos: A, B/R e C), OCR apenas do modelo atual Relaxmedic e os quatro documentos (avaliação, evolução, anamnese e ficha cadastral) entram na v1.
- Perfis: administrador, profissional e assistente; permissões do assistente configuráveis pelo administrador, padrão supervisionado. Revisões com histórico. Layout pode ser reorganizado preservando conteúdo e identidade, com arquitetura preparada para template fiel ao original no futuro. A fase continua sendo brainstorming, sem implementação. Ver documento de consolidação em docs/superpowers/specs/2026-09-27-migracao-web-brainstorming.md.

## Aprendizados da especifica??o ? 2026-09-27

- A consolida??o foi aprovada; o usu?rio autorizou continuar a especifica??o, mantendo a restri??o de n?o implementar. Entrega em docs/migracao-web/README.md.
- Extra??o atual: 89 m?dulos, 651 procedimentos, 25 abas. Mapas: Clientes 25, Avalia??es 108, Anamnese 48, Meu Cadastro 43; OCR 44 zonas (20 sinalizadas estaticamente). Avalia??es!DD ? hora apesar de cabe?alho ausente.
- Analise C!G7 ? f?rmula compartilhada resolvida =1-F7. N?o repetir o erro da extra??o XML antiga que documentou somente =.
- N?o confiar nas captions extra?das por oletools nesta amostra: fragmentos e caracteres de controle; verificar no Excel. Procedimentos declarados n?o comprovam uso; Imc_CA_CAM e equa??es antropom?tricas exigem confirma??o de chamadas.
- Diverg?ncias est?ticas de DRI, altura da calculadora, escala de urina, eventos e apresenta??o est?o catalogadas, sem corre??o nem aprova??o cl?nica. Comparar em c?pia controlada antes de portar.
- Ferramentas: oletools ausente no Python inicial; instala??o tempor?ria por PyPI expl?cito funcionou. Leitura dos arquivos tempor?rios instalados exigiu execu??o autorizada fora do sandbox. N?o alterar depend?ncias do produto para essa an?lise.
- Verifica??o documental: Meu Cadastro ? invent?rio de cabe?alhos sem offsets; contar suas 43 linhas por coluna, separadamente das tabelas com offsets. Asser??o inicial corrigida; verifica??o final passou 16/16 checagens.

## Revisão das divergências — 2026-09-27 (tarde)

- DIV-01 é determinística: o IMC interno das DRI (peso/cm²) fica em torno de 0,003, então o legado usa sempre a equação de peso normal. Não precisa ser medido no Excel.
- DIV-02: FrmCalcPeso está amarrada a FrmAvaliacaoB (cliques de opção :134–166 e altura :68). Em A/C a calculadora devolve zeros; não há legado a preservar ali.
- DIV-03: ImcDesc nunca é atribuída (Imc_CA_CAM não tem chamador). IMC de 5–19 anos no fluxo A = classificação sempre vazia.
- DIV-05: grupos do FrmBioimpC mapeados pelo stream de frame no manifest.json (i4625 músc. esquelético, i4704 água, i4712 massa muscular, i4720 proteína, i4724 óssea). Opt4/5/6/7 sem ramo. Use o stream do manifesto para descobrir o frame de um controle.
- DIV-15: a idade das avaliações e anamneses vem de Clientes!E (valor gravado no último salvamento do cadastro), não é recalculada. Age() só roda no cadastro.
- Só a aba 25 (Analise C) tem fórmulas; nenhuma chama função VBA. Verificado lendo xl/worksheets/*.xml com zipfile.
- Implementação web: 7 planos por subsistema com portões (divergencias-e-homologacao.md §5). Só o Plano 1 (Fundação) está liberado.

## Do-Not-Repeat (migração)

- [2026-09-27] `cd` dentro do Bash muda o diretório de trabalho da sessão inteira. Usar caminhos absolutos.
- [2026-09-27] Não tratar Clientes!E (Idade) como idade atual do paciente: é um valor congelado do último salvamento do cadastro.

## Decision Log — Plano 1 da migração web (2026-09-27)

- Código da aplicação web em repositório NOVO e separado: `C:\Sistema\PSM\psm-web` (npm workspaces `apps/api` + `apps/web`). Motivo: o deploy na nuvem não pode carregar o Bioimpedancia.xlsm versionado (dados de saúde), CLIENTES/ nem a chave .pfx. Planos e specs continuam neste repo.
- Aceitos os 4 padrões do Plano 1: sessão no servidor com cookie httpOnly (`psm_sid`); duplicidade na importação pelo Seq, com alerta por CPF ou nome + nascimento; fotos e anexos fora da carga inicial; finalização/emissão reservadas ao profissional/administrador (delegação ao assistente configurável, desligada).
- Plano 1 escrito com writing-plans em docs/superpowers/plans/2026-09-27-plano-1-fundacao.md (13 tarefas, TDD). Stack fixada: NestJS 11, Prisma 6, Postgres 16 em Docker (porta 5433, bancos psm/psm_test), Jest+Supertest; React 19, Vite 6, React Router 7, TanStack Query 5, Tailwind 4, Vitest.
- Ambiente desta máquina: Node 24; o `npm` no PATH é 8.12 (global antigo em %APPDATA%\npm; o do Node é 11.19); PostgreSQL não instalado (só pasta data vazia); Docker Desktop instalado.
- Aba Clientes do XLSM: cabeçalhos com espaço final ('Nome ', 'Sexo ', 'Idade '), nascimento como serial do Excel (sistema 1900; offset 25569), não é 1904. A cópia versionada tem só 2 linhas de dados.

## Maestri (orquestrador de agentes) — verificado em 2026-09-27

- Maestri 0.18.1 instalado em C:\Users\claud\AppData\Local\Programs\Maestri (18/09/2026); CLI em resources\cli\maestri.exe, disponível só DENTRO dos terminais do Maestri (fora dele, MAESTRI_CLI vazio). Skills maestri* em ~/.claude/skills.
- Requisito oficial do Maestri para Windows: Windows 11 x64. Esta máquina é Windows 10 Pro 19045 — validar com `maestri debug` antes de depender dele.
- Agentes: Claude Code 2.1.283 (CLI). Codex só como **Codex Desktop** da Microsoft Store (OpenAI.Codex 26.924, MSIX): o `codex.exe` embutido em WindowsApps dá "Acesso negado", então o Maestri não o usa — é preciso `npm install -g @openai/codex` (CLI compartilha ~/.codex, com login já salvo pelo Desktop). OpenCode/Gemini ausentes.
- Usuário tem **licença paga do Maestri** (workspaces ilimitados) — corrigido pelo usuário em 2026-09-27; não assumir plano gratuito. Andares no Windows = branch própria, sem copy-on-write (APFS é só macOS). Maestro Mode: caixa "Maestro" na aba Detalhes do terminal. Papéis: Configurações → Agentes. Conectar: Ctrl+L.
- Guia de execução do Plano 1 com Maestri: docs/superpowers/plans/2026-09-27-plano-1-guia-maestri.md (time Regente/Forja/Lupa, pontos de controle após Tasks 1, 5, 8, 12 e na Task 13).
- [2026-09-27] Do-Not-Repeat: ao checar agentes instalados, não olhar só o PATH — procurar também apps de desktop/MSIX (`Get-AppxPackage`). E não presumir plano gratuito de ferramentas pagas; perguntar ou verificar.
- [2026-09-27] Maestri deu erro no `maestri debug` nesta máquina (Windows 10, abaixo do requisito Windows 11). Projeto será retomado numa máquina nova com Windows 11, Codex CLI e ferramentas já configuradas. Instruções de retomada: docs/superpowers/plans/2026-09-27-retomada-nova-maquina.md (branch `migracao-web` no GitHub privado claudio-mas/sistema-psm-bioimpedancia; `_Teste/` e histórico do Claude vão por pendrive com BitLocker).
