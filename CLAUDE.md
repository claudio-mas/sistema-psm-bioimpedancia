# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## O que é

Suplemento VSTO do Microsoft Excel (C#, .NET Framework 4.7.2) para a planilha
`Bioimpedancia.xlsm` (gestão de clientes/anamneses/avaliações de bioimpedância e
emissão de laudos em PDF). Distribuído por **ClickOnce**.

**Princípio central:** a lógica de negócio vive no **VBA** da planilha, não no C#.
O suplemento é uma camada de UI + geração de PDF. Antes de implementar cálculo ou
regra de negócio em C#, verifique se ela já existe (ou deveria existir) no VBA — o
C# normalmente apenas **lê** valores já calculados das colunas da aba "Avaliações".

## Build, publicação, testes

O `.csproj` foi montado manualmente para build por **linha de comando** (MSBuild).
O carregamento pelo IDE do Visual Studio pode falhar (flavor VSTO). **Feche o Excel
antes de compilar/publicar.**

```bat
REM Build (Release)
"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" ^
  "SistemaPSM.AddIn\SistemaPSM.AddIn.csproj" ^
  /t:Build /p:Configuration=Release /p:Platform=AnyCPU /p:VisualStudioVersion=17.0

REM Publicar nova versão ClickOnce no Google Drive
cd SistemaPSM.AddIn && Publicar-NovaVersao.cmd
```

- **Não há suíte de testes** — nenhum framework de teste no projeto. Validação é
  manual (compilar, abrir o Excel, exercitar o painel/laudo).
- `Publicar-NovaVersao.cmd` calcula `ApplicationVersion` manualmente porque
  `AutoIncrementApplicationRevision` não funciona em build CLI (sai sempre `1.0.0.0`).
- Após publicar, **reinstale o `.vsto`** em cada PC (auto-update por URL desligado).
- Dependências **não versionadas** necessárias localmente (ver README e `.gitignore`):
  `SistemaPSM.AddIn/wkhtmltopdf/wkhtmltopdf.exe`, a chave `SistemaPSM.AddIn_Key.pfx`
  (assinatura ClickOnce) e a pasta `CLIENTES/` (dados de saúde — LGPD).

## Arquitetura

### Ciclo de vida e ponte VBA↔C# ([ThisAddIn.cs](SistemaPSM.AddIn/ThisAddIn.cs))

- Mantém **um `CustomTaskPane` por janela** (Excel 2013+ é SDI), indexado pelo `Hwnd`.
  `EhSistemaPSM()` reconhece a pasta certa por `CustomDocumentProperty "SistemaPSM"`
  (robusto a renomeação) com fallback no nome `Bioimpedancia*`.
- `RequestComAddInAutomationService()` expõe `LaudoAutomation` ao VBA. A inicialização
  é deliberadamente **tolerante a falhas** (`try/catch` silenciosos) para não quebrar
  a abertura do Excel.

Há **dois sentidos** de comunicação entre o C# e o VBA:

1. **C# → VBA** ([PainelSistema.cs](SistemaPSM.AddIn/PainelSistema.cs)): cada botão do
   painel chama uma macro VBA via `Application.Run` (`UI_Inicio`, `UI_MeuCadastro`,
   `UI_Clientes`, `UI_Anamneses`, `UI_Avaliacoes`, `UI_Logo`, `UI_Backup`,
   `UI_Informacoes`, `UI_Tutoriais`, `UI_Suporte`). O painel é só visual; o trabalho
   acontece no VBA.
2. **VBA → C#** ([LaudoAutomation.cs](SistemaPSM.AddIn/Laudo/LaudoAutomation.cs)): o
   formulário VBA `FrmRelatorioB` chama
   `Application.COMAddIns("SistemaPSM.AddIn").Object.EmitirLaudoPorSeq Seq` (e
   `EmitirLaudoAvaliacaoPorSeq`) para disparar a geração do laudo. O assembly é
   `ComVisible(false)`; a visibilidade COM é declarada explicitamente nessa classe.

### Geração de laudo (pasta [Laudo/](SistemaPSM.AddIn/Laudo/))

Fluxo: **lê dados → monta HTML → converte em PDF → salva e abre.**

- [LaudoService.cs](SistemaPSM.AddIn/Laudo/LaudoService.cs) — orquestra. Salva o PDF em
  `<pasta do .xlsm>\CLIENTES\<Id Nome>\<prefixo> <data>.pdf`. A logo do laudo é um
  arquivo (`logo.png`/`.jpeg`/`.jpg`) na pasta da planilha, lido como data URI — **não**
  fica embutido no `.xlsm` (para não atrasar a abertura).
- [LaudoRepositorio.cs](SistemaPSM.AddIn/Laudo/LaudoRepositorio.cs) — lê a aba
  "Avaliações" via **offsets de coluna codificados como constantes** (`OFF_*`, 0-based
  a partir de A), espelhando `SalvaAvaliacaoB`/`RelatorioBH_Preenche` do VBA. **Atenção:**
  se o layout de colunas da planilha mudar, esses offsets quebram silenciosamente — são o
  ponto de acoplamento mais frágil entre C# e a planilha. Lê o bloco inteiro numa única
  chamada COM (`Value2`).
- **Dois tipos de laudo:** *Evolução* (`LaudoHtmlBuilder`, série de até 6 avaliações do
  mesmo `Id`) e *Avaliação única* detalhada estilo Relaxmedic (`LaudoAvaliacaoHtmlBuilder`,
  lê todas as colunas A–DC). HTML/CSS embutido como template (`Template.html`,
  `TemplateAvaliacao.html`).
- [RecursosLaudo.cs](SistemaPSM.AddIn/Laudo/RecursosLaudo.cs) — fontes (`.ttf`) embutidas
  na DLL, extraídas para `%LOCALAPPDATA%\SistemaPSM\Laudo\fonts` (o wkhtmltopdf precisa de
  caminhos `file:///` estáveis). O `wkhtmltopdf.exe` é Content do ClickOnce, ao lado do
  assembly; `DirsCandidatos()` sonda `CodeBase` **e** `Location` porque o VSTO carrega a
  DLL de um shadow cache separado dos arquivos de Content.
- [WkHtmlToPdf.cs](SistemaPSM.AddIn/Laudo/WkHtmlToPdf.cs) — invoca o `wkhtmltopdf.exe`
  como processo externo.

### Classificações "Avaliação" do laudo

A coluna "Avaliação" do quadro de composição corporal é **texto lido da planilha** — o C#
não calcula nada (só converte `N/A`/`Não Calculado` em `-`). A classificação depende do
**fluxo de cadastro** (A/B/C) e a maioria é seleção manual no VBA. Detalhes em
[ColunaAvaliacao-Composicao.md](SistemaPSM.AddIn/Laudo/ColunaAvaliacao-Composicao.md).
