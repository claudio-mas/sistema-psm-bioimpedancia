# Sistema PSM — Suplemento VSTO de Bioimpedância

Suplemento do Microsoft Excel (VSTO / C#, .NET Framework 4.7.2) que adiciona um
**painel lateral** (`CustomTaskPane`) à planilha `Bioimpedancia.xlsm` para gestão de
clientes, anamneses, avaliações e **emissão de laudos em PDF** (composição corporal).

A interface do painel é em C#, mas a **lógica de negócio permanece no VBA** da planilha:
o painel apenas chama os wrappers do módulo `MenuPainel` (`UI_Inicio`, `UI_MeuCadastro`,
… `UI_Backup`) via `Application.Run`. Os laudos são gerados convertendo HTML/CSS em PDF
com o `wkhtmltopdf.exe` (processo externo). A distribuição é feita por **ClickOnce**.

---

## Pré-requisitos

| Item | Versão / Observação |
|------|---------------------|
| Windows | 10/11 |
| Microsoft Excel | Desktop (Office 15.0+ / Office 365) |
| Visual Studio 2022 **Professional** | Com a carga *Office/SharePoint development* (VSTO) |
| .NET Framework | 4.7.2 (Developer Pack) |
| `wkhtmltopdf.exe` | **Não versionado** — ver [Dependências externas](#dependências-externas-não-versionadas) |
| Certificado de assinatura (`.pfx`) | **Não versionado** — ver [Assinatura ClickOnce](#assinatura-clickonce) |

> O `.csproj` foi montado manualmente para build por linha de comando. O carregamento
> do projeto pelo IDE do Visual Studio pode falhar (flavor VSTO); **prefira o build via
> MSBuild** descrito abaixo.

---

## Estrutura do repositório

```
.
├── Bioimpedancia.xlsm           # Planilha-base (UI VBA + dados de modelo)
├── SistemaPSM.AddIn/            # Projeto do suplemento VSTO (C#)
│   ├── ThisAddIn.cs             # Ponto de entrada; registra o CustomTaskPane
│   ├── PainelSistema.cs         # UI do painel lateral (tema visual)
│   ├── RibbonSistema.cs/.xml    # Aba "Sistema PSM" com toggle do painel
│   ├── Laudo/                   # Geração de laudos (HTML builders + wkhtmltopdf)
│   ├── Icons/  Fonts/           # Recursos embutidos no assembly
│   ├── SistemaPSM.AddIn.csproj  # Build (referências VSTO por HintPath)
│   └── Publicar-NovaVersao.cmd  # Recompila (Release) e publica via ClickOnce
├── modelos/                     # Modelos de laudo (HTML/imagens)
└── logo*.png/jpg                # Identidade visual
```

### Dependências externas **não versionadas**

Por privacidade (LGPD), segurança e tamanho, os itens abaixo ficam **fora do Git**
(ver `.gitignore`) e precisam ser providenciados localmente:

| Item | Onde colocar / obter |
|------|----------------------|
| **`wkhtmltopdf.exe`** | Colocar em `SistemaPSM.AddIn/wkhtmltopdf/wkhtmltopdf.exe`. Baixar de <https://wkhtmltopdf.org/downloads.html> (build com Qt *patched*). O `.csproj` o copia como *Content* para a pasta da aplicação no ClickOnce. |
| **`SistemaPSM.AddIn_Key.pfx`** | Chave de assinatura ClickOnce — manter backup seguro **fora do repositório**. |
| **`CLIENTES/`** | Dados pessoais de saúde de clientes reais — **nunca** versionar. |

---

## Build (linha de comando)

Feche o Excel antes de compilar/publicar. Use o MSBuild do Visual Studio 2022:

```bat
"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" ^
  "SistemaPSM.AddIn\SistemaPSM.AddIn.csproj" ^
  /t:Build /p:Configuration=Release /p:Platform=AnyCPU /p:VisualStudioVersion=17.0
```

Notas (ver detalhes nos comentários do `.csproj`):

- As referências VSTO `Microsoft.Office.Tools.*.v4.0.Utilities` vêm por `HintPath` e ficam
  **CopyLocal=True** (senão a task `FindRibbons` falha no publish).
- O `.csproj` força `VSToolsPath` absoluto para não cair no fallback errado.
- As PIAs do Office (Office15) e `stdole` são embutidas (`EmbedInteropTypes`), dispensando
  PIAs nas máquinas-alvo.

---

## Publicação (ClickOnce)

Para gerar uma nova versão e publicar no Google Drive, use o script
[`Publicar-NovaVersao.cmd`](SistemaPSM.AddIn/Publicar-NovaVersao.cmd):

```bat
cd SistemaPSM.AddIn
Publicar-NovaVersao.cmd
```

O script:

1. Limpa `_Publish/` e recompila em **Release**.
2. Calcula uma versão **crescente** `1.0.<(AA*1000)+dia-do-ano>.<HHMM>` via PowerShell.
3. Publica em `G:\Meu Drive\Sistema PSM\Suplemento\` com `UpdateEnabled=false` e
   `BootstrapperEnabled=false`.
4. Copia os artefatos para a pasta do Drive.

> **Quirk de versionamento:** `AutoIncrementApplicationRevision` **não** funciona em build
> por linha de comando (sai sempre `1.0.0.0`); por isso o script calcula `ApplicationVersion`
> manualmente. Após publicar, **reinstale o `.vsto`** em cada PC (duplo clique em
> `SistemaPSM.AddIn.vsto`) para que a nova versão seja aplicada — o auto-update por URL
> está desligado.

---

## Assinatura ClickOnce

- Certificado **autoassinado** em `Cert:\CurrentUser\My`.
- Thumbprint configurado no `.csproj` (`ManifestCertificateThumbprint`):
  `835244F2D90E6607A537D05A438167C5C8F9288D`.
- Chave privada: `SistemaPSM.AddIn_Key.pfx` (**não versionada**; senha guardada à parte
  em local seguro).
- Certificado **público**: [`SistemaPSM_PublicCert.cer`](SistemaPSM.AddIn/SistemaPSM_PublicCert.cer)
  — pode ser distribuído para confiar no publicador nas máquinas-alvo.

Sem o `.pfx`, é possível compilar (Build), mas **não** assinar os manifestos para publicar.

---

## Itens fora do controle de versão

O `.gitignore` exclui: chaves (`*.pfx`), dados de clientes (`CLIENTES/`, `APR_*.pdf`),
artefatos de build (`bin/`, `obj/`), saída do ClickOnce (`_Publish/`), backups
(`BACKUP/`, `_backup/`), `wkhtmltopdf.exe`, previews/testes gerados e temporários do Office.
