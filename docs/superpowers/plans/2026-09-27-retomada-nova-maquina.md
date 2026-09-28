# Retomar o projeto em outra máquina

**Comece aqui:** faça a Parte A **nesta** máquina antes de desligá-la (~15 min). A Parte B é na máquina nova (~30 min).

## Onde paramos (27/09/2026)

| Frente | Situação |
|---|---|
| Especificação da migração web | Pronta: `docs/migracao-web/` (16 divergências, 23 casos, portões por plano) |
| Plano 1 — Fundação | Escrito, **não iniciado**. O repositório `C:\Sistema\PSM\psm-web` ainda não existe |
| Guia Maestri | Pronto. O Passo 0 falhou na máquina antiga (Windows 10); recomeçar pelo Passo 0 na nova |
| Suplemento VSTO | 3 arquivos `.cs` e o `Bioimpedancia.xlsm` com alterações pendentes desde **antes** desta etapa |

---

## Parte A — Nesta máquina, antes de desligar

### A1. Enviar o trabalho para o GitHub (5 min)

O repositório `claudio-mas/sistema-psm-bioimpedancia` é **privado**, o que importa porque o `.xlsm` pode conter dados de pacientes.

```powershell
Set-Location "C:\Sistema\PSM\Projeto 14\zanoni"
git switch -c migracao-web
git add docs .wolf .claude/settings.json .codegraph/.gitignore
git commit -m "docs: especificação da migração web, Plano 1, guia Maestri e retomada"
```

As alterações do suplemento VSTO já estavam pendentes antes desta etapa. Confira se podem ir como estão e, se sim:

```powershell
git add SistemaPSM.AddIn Bioimpedancia.xlsm
git commit -m "wip: alterações pendentes do suplemento VSTO"
```

Depois envie:

```powershell
git push -u origin migracao-web
```

O arquivo `.claude/skills/lead-scraping.md` não faz parte deste projeto e fica fora do commit. Se precisar dele, copie à parte.

### A2. Copiar o que o git não leva (10 min)

Use um pendrive com **BitLocker To Go** (botão direito no pendrive → Ativar o BitLocker). Não use nuvem pública para `CLIENTES\` nem para o `.pfx`.

| O quê | Tamanho | Precisa levar? |
|---|---|---|
| `C:\Sistema\PSM\Projeto 14\zanoni\_Teste\` | 23,5 MB | **Sim.** Tem a extração do VBA que embasa as divergências |
| `%USERPROFILE%\.claude\projects\c--Sistema-PSM-Projeto-14-zanoni\` | 3,7 MB | **Recomendado.** Histórico das sessões do Claude neste projeto |
| `SistemaPSM.AddIn\wkhtmltopdf\wkhtmltopdf.exe` | — | Só se for mexer no suplemento VSTO |
| `SistemaPSM.AddIn\SistemaPSM.AddIn_Key.pfx` | — | Só para publicar o suplemento. É uma chave de assinatura: trate como senha |
| `CLIENTES\` | 2,3 MB | Só se for rodar o suplemento com dados reais. **Dados de saúde (LGPD):** se não precisar, não leve |

### A3. Licença do Maestri

A licença vale para 2 máquinas. Se esta máquina já ocupa uma vaga e a nova será a segunda, não é preciso fazer nada. Se precisar liberar a vaga, desative a licença aqui antes.

---

## Parte B — Na máquina nova

### B1. Usar os mesmos caminhos

Mantenha `C:\Sistema\PSM\Projeto 14\zanoni` e `C:\Sistema\PSM\psm-web`. O plano, o guia Maestri e a configuração dos agentes usam esses caminhos absolutos.

Se precisar de outros caminhos, troque-os em:
- `docs/superpowers/plans/2026-09-27-plano-1-fundacao.md`: `C:\Sistema\PSM\psm-web`.
- `docs/superpowers/plans/2026-09-27-plano-1-guia-maestri.md`: Passos 1 e 6, e a seção "Textos para copiar".

### B2. Clonar (3 min)

```powershell
New-Item -ItemType Directory -Force "C:\Sistema\PSM\Projeto 14" | Out-Null
git clone https://github.com/claudio-mas/sistema-psm-bioimpedancia.git "C:\Sistema\PSM\Projeto 14\zanoni"
Set-Location "C:\Sistema\PSM\Projeto 14\zanoni"
git switch migracao-web
```

### B3. Restaurar do pendrive (5 min)

1. `_Teste\` → `C:\Sistema\PSM\Projeto 14\zanoni\_Teste\`.
2. Pasta `c--Sistema-PSM-Projeto-14-zanoni` → `%USERPROFILE%\.claude\projects\`, com o mesmo nome. O nome da pasta vem do caminho do projeto; por isso o B1 pede o mesmo caminho.
3. Itens do suplemento VSTO: só se for usar, nos mesmos lugares relativos.

### B4. Reindexar o CodeGraph (2 min)

A pasta `.codegraph/` chega vazia, porque o índice não vai para o git:

```powershell
Set-Location "C:\Sistema\PSM\Projeto 14\zanoni"
codegraph index
```

### B5. Conferir as ferramentas (5 min)

```powershell
node --version                                # v22 ou mais
npm --version                                 # 10 ou mais
git --version
docker info --format "{{.ServerVersion}}"     # Docker Desktop precisa estar aberto
claude --version
codex --version
```

Depois:
1. Rode `codex` uma vez para confirmar o login e saia com **Ctrl+C**.
2. Abra o Claude Code na pasta `zanoni`. Não devem aparecer erros de hooks: os hooks do OpenWolf usam Node e ficam em `.wolf/hooks/`.
3. No Maestri: ative a licença, abra um terminal **Shell** e rode `maestri debug` e `maestri preset list`. A lista precisa mostrar Claude Code e Codex.

Só para o suplemento VSTO: Visual Studio 2022 com MSBuild, Excel e os itens do A2.

### B6. Retomar

1. Abra `docs/superpowers/plans/2026-09-27-plano-1-guia-maestri.md` e siga a partir do **Passo 0**.
2. O **Passo 1b** (instalar o Codex CLI) pode ser pulado: basta confirmar o preset do Codex no Maestri.
3. Primeira mensagem para o Claude na máquina nova, para ele recuperar o contexto:

```text
Estamos retomando a migração web do Sistema PSM numa máquina nova. Leia .wolf/cerebrum.md, docs/migracao-web/README.md e docs/superpowers/plans/2026-09-27-plano-1-guia-maestri.md. O Plano 1 ainda não começou; vou seguir o guia a partir do Passo 0.
```

---

## Pendências que continuam abertas

1. **Decisões clínicas** DIV-01, 02, 03, 15 e 16: liberam o Plano 3 (motor de regras). Ver `docs/migracao-web/divergencias-e-homologacao.md` §3.
2. **Os 13 pedidos da Dra.** do PRD on-premise anterior: confirmar se entram na v1.
3. **Defeitos ativos na planilha atual** (DIV-05, DIV-15, DIV-02): corrigir no VBA é uma decisão separada da migração.
4. **Alterações pendentes do suplemento VSTO:** revisar e fechar.
