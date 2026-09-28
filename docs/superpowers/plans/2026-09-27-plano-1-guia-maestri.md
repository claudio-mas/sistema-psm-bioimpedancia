# Guia Maestri — executar o Plano 1 (Fundação)

**Comece aqui:** faça o [Passo 0](#passo-0--teste-de-5-minutos-no-windows-10) (5 minutos). Ele diz se dá para seguir nesta máquina.

Este guia monta no Maestri um time de 3 agentes para executar o [Plano 1](2026-09-27-plano-1-fundacao.md), uma tarefa por vez, com revisão após cada uma: dois Claude Code (coordenador e implementador) e um Codex (revisor). Você só atua nos pontos de controle.

O revisor usa um modelo diferente de propósito: o Codex revisa o código que o Claude escreveu, o que tende a pegar erros que o mesmo modelo deixaria passar.

| Item | Valor |
|---|---|
| Tempo seu para montar tudo | ~45 min, uma vez |
| Tempo de execução dos agentes | 4 a 6 horas no total (13 tarefas) |
| Sua atenção durante a execução | ~10 min em cada um dos 5 pontos de controle, mais aprovações de comandos |
| Custo de uso | ~2× uma sessão comum de Claude Code, mais o uso do Codex na sua conta ChatGPT |

---

## Como o time funciona

```
            ┌──────────────────────────┐
            │ Regente (Maestro)        │  coordena, não escreve código
            │ lê o plano, delega,      │
            │ marca o progresso        │
            └──────┬──────────┬────────┘
       delega      │          │     pede revisão
                   ▼          ▼
        ┌──────────────┐  ┌──────────────┐
        │ Forja        │  │ Lupa         │
        │ Claude Code  │  │ Codex        │
        │ implementa   │  │ revisa, não  │
        │ 1 tarefa     │  │ edita nada   │
        └──────────────┘  └──────────────┘
   Notas ligadas aos três: "Plano 1 — Progresso" e "Decisões e bloqueios"
```

Ciclo de cada tarefa:
1. O Regente pede ao Forja: "execute a Task N".
2. O Forja segue o plano (teste primeiro), faz commit e avisa o Regente.
3. O Regente pede ao Lupa a revisão do commit.
4. Se houver achados críticos ou importantes, o Forja corrige e o Lupa revisa de novo.
5. O Regente marca a tarefa na nota de progresso e passa para a próxima.

Os agentes conversam pelo comando `maestri ask`, que só funciona entre terminais **conectados** por uma corda no canvas.

---

## Passo 0 — Teste de 5 minutos no Windows 10

O Maestri para Windows pede oficialmente **Windows 11 x64**. Esta máquina é Windows 10 Pro, mas o Maestri 0.18.1 já está instalado em `C:\Users\claud\AppData\Local\Programs\Maestri`. Confira se ele funciona:

1. Abra o Maestri pelo menu Iniciar.
2. Na barra superior, escolha a ferramenta **Terminal** e arraste um retângulo no canvas. No modal, escolha **Shell**.
3. No terminal, rode:
   ```powershell
   maestri debug
   ```
4. Resultado:
   - **Sem erros:** siga para o Passo 1.
   - **Erro de "pipe", "elevation" ou o comando não existe:** feche e reabra o Maestri **sem** "Executar como administrador" e rode de novo.
   - **Continua falhando:** a causa provável é o Windows 10. As saídas são atualizar para Windows 11 ou rodar o time num Mac (o formato do workspace é o mesmo). Pare aqui e anote o erro exato.

Confira também, no mesmo terminal:

```powershell
docker info --format "{{.ServerVersion}}"
npm --version
claude --version
```

- `docker info` deve mostrar uma versão. Se não mostrar, abra o Docker Desktop e espere "Engine running".
- `npm --version` deve ser 10 ou mais. Se aparecer `8.x`, rode `npm install -g npm@11`.
- `claude --version` deve mostrar `2.x (Claude Code)`.
- `codex --version` hoje dá "não reconhecido": o Codex Desktop não coloca o `codex` no PATH. O Passo 1b resolve.

---

## Passo 1 — Preparar a pasta do código (5 min)

O workspace do Maestri precisa de uma pasta que já exista. Num terminal PowerShell:

```powershell
New-Item -ItemType Directory -Force C:\Sistema\PSM\psm-web | Out-Null
Set-Location C:\Sistema\PSM\psm-web
git init
New-Item -ItemType Directory -Force .claude | Out-Null
```

Crie o arquivo `C:\Sistema\PSM\psm-web\.claude\settings.local.json` com:

```json
{
  "permissions": {
    "additionalDirectories": ["C:/Sistema/PSM/Projeto 14/zanoni"],
    "defaultMode": "acceptEdits"
  }
}
```

O que isso faz:
- **`additionalDirectories`:** os agentes leem o plano, que fica na pasta `zanoni`, sem pedir permissão a cada vez.
- **`acceptEdits`:** edições de arquivo são aceitas automaticamente. **Comandos** (npm, git, docker) continuam pedindo sua aprovação.

---

## Passo 1b — Instalar o Codex CLI (5 min)

O Maestri roda agentes de **terminal**. O Codex Desktop (app da Microsoft Store) não serve: o `codex.exe` que vem dentro dele é bloqueado pelo Windows ("Acesso negado"). O Codex CLI é o mesmo Codex em linha de comando e usa a mesma pasta `~/.codex`, portanto a mesma conta e as mesmas configurações do app.

1. Num PowerShell comum:
   ```powershell
   npm install -g npm@11
   npm install -g @openai/codex
   codex --version
   ```
   `codex --version` deve mostrar uma versão.
2. Rode `codex` uma vez. Se ele pedir login, entre com a mesma conta ChatGPT do Codex Desktop. Depois saia com **Ctrl+C**.
3. **Feche e reabra o Maestri**, para ele enxergar o `codex` recém-instalado.
4. Num terminal **Shell** do Maestri, rode `maestri preset list` e confirme que existe um preset do Codex. Anote o nome exato (por exemplo, `Codex`).

Se o Codex CLI não funcionar bem no Windows, pule este passo: o Lupa roda no Claude Code (veja a observação do Passo 6).

---

## Passo 2 — Criar o workspace (3 min)

1. Na barra lateral, clique em **+**.
2. **Diretório de trabalho:** `C:\Sistema\PSM\psm-web`.
3. **Nome:** `PSM Web`. Escolha qualquer ícone.

Use um workspace só para o código novo. Se você já tem um workspace apontando para a pasta `zanoni`, mantenha-o separado: ele serve para o suplemento VSTO e para a especificação.

**Opcional, com a licença:** crie um segundo workspace `PSM Especificação` com diretório `C:\Sistema\PSM\Projeto 14\zanoni`. Ele é útil depois, para discutir divergências e preparar os próximos planos sem misturar com a execução. Agentes de workspaces diferentes podem conversar: o agente do outro workspace aparece como `Nome @ Workspace` no `maestri list`. Para o Plano 1 não é necessário.

---

## Passo 3 — Instruções do workspace (3 min)

Clique com o botão direito no workspace **PSM Web** → **Editar** → campo de instruções dos agentes. Cole o bloco [Instruções do workspace](#instruções-do-workspace). Elas valem para todos os agentes deste workspace.

---

## Passo 4 — Criar o papel do coordenador (3 min)

1. Abra **Configurações → Agentes**.
2. Crie uma responsabilidade chamada `Coordenador do Plano 1`.
3. Cole o texto [Papel: Coordenador do Plano 1](#papel-coordenador-do-plano-1).

Os papéis do Forja e do Lupa não precisam ser criados à mão: o Regente cria os dois no Passo 6.

---

## Passo 5 — Criar o Regente, o terminal Maestro (3 min)

1. Na barra superior, ferramenta **Terminal** → arraste um retângulo grande no canvas.
2. No modal:
   - **Agente:** Claude Code.
   - **Nome:** `Regente`.
   - **Responsabilidade:** `Coordenador do Plano 1`.
   - Aba **Detalhes:** marque **Maestro**.
3. Salve e espere o Claude Code abrir no terminal.

Sem a caixa **Maestro** marcada, o Regente não consegue recrutar outros agentes.

---

## Passo 6 — Montar o time (5 min)

Cole no terminal do Regente:

```text
Leia o guia C:/Sistema/PSM/Projeto 14/zanoni/docs/superpowers/plans/2026-09-27-plano-1-guia-maestri.md, seção "Textos para copiar".
1. Crie as notas com `maestri note create --name "Plano 1 — Progresso"` e `maestri note create --name "Decisões e bloqueios"`, com os conteúdos do guia.
2. Crie os papéis "Implementador PSM" e "Revisor PSM" com `maestri role create`, usando exatamente os textos do guia (escopo do workspace atual).
3. Rode `maestri preset list`. Recrute "Forja" com o papel Implementador PSM e o preset Claude Code. Recrute "Lupa" com o papel Revisor PSM e o preset do Codex (use o nome exato da lista).
4. Conecte as duas notas ao Forja e ao Lupa com `maestri connect`.
5. Mostre o resultado de `maestri list` e pare. Não comece a Task 1.
```

**Sem Codex** (se você pulou o Passo 1b): troque no item 3 "o preset do Codex" por "o preset Claude Code".

**Confira:** o canvas mostra 3 terminais (Regente, Forja, Lupa) e 2 notas, todos ligados por cordas ao Regente. Se faltar alguma corda, selecione um terminal, pressione **Ctrl+L** e arraste até o outro.

---

## Passo 7 — Executar (horas; você só nos pontos de controle)

Cole no Regente:

```text
Comece pela Task 1 do plano e siga o ciclo do seu papel até o próximo ponto de controle.
```

Enquanto os agentes trabalham:
- **Aprovar comandos:** quando um terminal pedir permissão, aprove. Para comandos que se repetem (`npm test`, `git commit`, `docker compose`), escolha a opção de **não perguntar de novo**.
- **Lupa (Codex):** ele roda comandos num ambiente isolado. Para executar os testes, que precisam do Postgres no Docker, ele pedirá para rodar fora desse ambiente. Aprove só comandos de leitura e teste (`git show`, `git diff`, `npm test`). Se pedir para editar arquivos, recuse: o papel dele é só revisar.
- **Não selecionar terminais:** deixe o terminal de quem está respondendo sem seleção (sem borda tracejada). Com a borda tracejada, o Maestri não acompanha a resposta.
- **Ir a um terminal:** segure **Ctrl** para ver os números e aperte o número do terminal.
- **Pausar tudo:** clique no terminal do Regente e aperte **Esc**.

### Pontos de controle

O Regente para e manda uma notificação em cada um deles.

| Após | O que você confere (~10 min) | Para seguir |
|---|---|---|
| Task 1 | `docker compose ps` mostra o Postgres; `npm test -w @psm/api` passa | "Pode seguir para a Task 2." |
| Task 5 | Login, bloqueio após 5 erros e permissões: leia o resumo do Lupa | "Pode seguir para a Task 6." |
| Task 8 | API completa: `npm test -w @psm/api` passa (18 suítes) | "Pode seguir para a Task 9." |
| Task 12 | Abra `http://localhost:5173` com API e web rodando; entre como admin | "Pode seguir para a Task 13." |
| Task 13, Step 3 | **Você** prepara a planilha sintética e roda o roteiro manual (11 itens) | Diga ao Regente o resultado de cada item |

Se algo estiver errado num ponto de controle, descreva o problema ao Regente em uma frase. Ele manda o Forja corrigir.

---

## Quando algo dá errado

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| Comando `maestri` falha | Pipe ou elevação | No terminal afetado: `maestri debug`. Reabra o Maestri sem modo administrador |
| Terminal parado há muito tempo | Esperando sua aprovação | Olhe o terminal e aprove ou recuse o comando |
| Regente diz que o tempo do `ask` acabou | Tarefa longa | Normal. Ele deve rodar `maestri check "Forja"` e esperar, **sem reenviar** |
| Mesma falha 3 vezes | Plano errado ou ambiente | O Regente para e registra em "Decisões e bloqueios". Leia a nota e decida |
| Canvas não responde a teclas | Foco perdido | Menu **Exibir → Redefinir foco** (Reset Focus) |
| Máquina lenta | Memória dos agentes | **Configurações → Terminal** → limite de memória por terminal |
| Um agente trabalha mal | Contexto poluído | Peça ao Regente: `maestri role assign "Forja" "Implementador PSM"`, que reinicia o agente mantendo as conexões. **Não** use `dismiss` |
| Docker parado no meio | Docker Desktop fechado | Abra o Docker Desktop e diga ao Regente para repetir o último passo |
| Preset do Codex não aparece | Maestri aberto antes da instalação do CLI | Feche e reabra o Maestri; confira `codex --version` num terminal Shell do Maestri |
| Lupa (Codex) falha ou não responde ao Regente | Codex CLI no Windows ou login expirado | Rode `codex` num terminal Shell para refazer o login. Se continuar, peça ao Regente: `maestri recruit "Lupa" --preset "Claude Code" --replace "Lupa"`, que troca o agente e mantém as conexões |

---

## Por que não usar andares (floors) agora

Andares criam uma cópia isolada do repositório numa branch própria. No Windows, cada andar fica numa branch própria, sem a cópia instantânea do macOS. As 13 tarefas do Plano 1 dependem umas das outras, então trabalho paralelo não ajuda. Andares valem a pena nos próximos planos (por exemplo, Documentos e OCR em paralelo), com **Aterrissar** para juntar o trabalho.

---

## Cola rápida

| Ação | Como |
|---|---|
| Ver time, notas e conexões | `maestri list` |
| Ver a tela de um agente sem interromper | `maestri check "Forja"` |
| Pedir algo a um agente | `maestri ask "Lupa" "..."` |
| Conectar dois terminais ou nota | Selecionar → **Ctrl+L** → arrastar |
| Ir a um terminal | Segurar **Ctrl** → número |
| Trocar de workspace | **Ctrl+↑/↓** |
| Diagnóstico | `maestri debug` |

---

## Textos para copiar

### Instruções do workspace

```text
Projeto: psm-web, aplicação web que substitui a planilha Bioimpedancia.xlsm (Sistema PSM).
Código: C:\Sistema\PSM\psm-web (este workspace).
Plano: C:/Sistema/PSM/Projeto 14/zanoni/docs/superpowers/plans/2026-09-27-plano-1-fundacao.md
Especificação: C:/Sistema/PSM/Projeto 14/zanoni/docs/migracao-web/ e C:/Sistema/PSM/Projeto 14/zanoni/docs/superpowers/specs/

Regras para todos os agentes:
- Siga o plano ao pé da letra. Não acrescente escopo. Se o plano estiver errado ou ambíguo, registre na nota "Decisões e bloqueios" e pare.
- A pasta zanoni é só leitura: nunca edite, crie ou apague arquivos nela.
- Use só dados sintéticos. Nunca use, copie ou exiba dados reais de pacientes.
- Ambiente: Windows, PowerShell, Docker Desktop, Postgres na porta 5433, npm 10 ou mais.
- Na Task 1, acrescente a linha `.claude/settings.local.json` ao .gitignore (ajuste combinado, fora do plano).
- Antes de falar com outro agente, rode `maestri list` para ver os nomes exatos.
- Escreva em português.
```

### Papel: Coordenador do Plano 1

```text
Você coordena a execução do Plano 1 (13 tarefas) e NÃO escreve código nem edita arquivos do projeto.
Fontes: o plano (caminho nas instruções do workspace) e as notas "Plano 1 — Progresso" e "Decisões e bloqueios".

Ciclo de cada tarefa, uma por vez, na ordem:
1. Leia a tarefa no plano e rode `maestri list`.
2. Delegue ao Forja: `maestri ask "Forja" "Execute a Task N do plano. Siga os passos na ordem. Ao terminar, responda com maestri ask \"Regente\" \"<resumo>\""`.
3. Quando o Forja responder, peça revisão ao Lupa: `maestri ask "Lupa" "Revise o commit <hash> da Task N. Responda com maestri ask \"Regente\" \"<achados>\""`.
4. Achado Crítico ou Importante: devolva ao Forja para corrigir e peça nova revisão ao Lupa. Achados Menores: registre em "Decisões e bloqueios" e siga.
5. Marque a tarefa como concluída em "Plano 1 — Progresso" com `maestri note edit`, anotando o hash do commit.

Se o tempo de um `maestri ask` acabar, NÃO reenvie o pedido: rode `maestri check "<nome>"` e espere de novo.

Pare, rode `maestri notify "<motivo>"` e espere a pessoa quando:
- terminar as Tasks 1, 5, 8 ou 12 (pontos de controle);
- chegar ao Step 3 da Task 13 (etapas manuais da pessoa);
- uma tarefa falhar 3 vezes seguidas;
- o plano estiver errado ou ambíguo, ou o Lupa apontar algo que exija decisão de produto.

Nunca pule a revisão. Nunca rode duas tarefas ao mesmo tempo: elas dependem umas das outras.
```

### Papel: Implementador PSM

```text
Você implementa UMA tarefa por vez do Plano 1 no repositório C:\Sistema\PSM\psm-web.
- Leia a tarefa pedida no plano e as seções "Global Constraints" e "Review Focus".
- Siga os passos na ordem: escreva o teste, veja falhar, implemente, veja passar, faça o commit indicado.
- Use o código do plano. Se algo do plano não compilar ou um teste falhar por erro do próprio plano, faça a menor correção possível e explique no relatório.
- Não comece outra tarefa e não mexa em arquivos fora da tarefa sem explicar por quê.
- Se o mesmo erro aparecer 3 vezes, pare e reporte o erro exato.
- Ao terminar, rode `maestri list` e responda ao Regente com `maestri ask "Regente" "..."`, informando: arquivos criados ou alterados, resultado dos testes (quantos passaram), hash do commit e desvios do plano.
- Leia as notas "Plano 1 — Progresso" e "Decisões e bloqueios" antes de começar cada tarefa.
```

### Papel: Revisor PSM

```text
Você revisa e NUNCA edita arquivos. Para cada pedido do Regente:
- Em C:\Sistema\PSM\psm-web, rode `git show <hash>` e compare com a tarefa correspondente do plano e com "Global Constraints".
- Rode os testes da tarefa para confirmar que passam.
- Prioridades: segurança de sessão e permissões (toda rota protegida na API), exposição de dados pessoais, regra do plano não cumprida, teste que não testa o que diz, erro de tipo ou de lógica.
- Classifique cada achado como Crítico, Importante ou Menor, com arquivo:linha e a correção sugerida.
- Responda ao Regente com `maestri ask "Regente" "..."`. Se não houver achados, responda "Aprovado".
- Antes de responder, rode `maestri list` para confirmar o nome do Regente.
```

### Nota: Plano 1 — Progresso

```markdown
# Plano 1 — Progresso

- [ ] Task 1 — Repositório, Postgres em Docker e esqueleto da API com auditoria  ⟵ ponto de controle
- [ ] Task 2 — Usuários e senhas
- [ ] Task 3 — Catálogo de permissões e configuração do assistente
- [ ] Task 4 — Sessões, login e guarda de acesso global
- [ ] Task 5 — Endpoints de administração  ⟵ ponto de controle
- [ ] Task 6 — Pacientes: normalização, API e edição concorrente
- [ ] Task 7 — Leitura da planilha e mapeamento da aba Clientes
- [ ] Task 8 — Importação de cadastros  ⟵ ponto de controle
- [ ] Task 9 — SPA: esqueleto, identidade visual, login
- [ ] Task 10 — SPA: pacientes
- [ ] Task 11 — SPA: administração
- [ ] Task 12 — SPA: importação  ⟵ ponto de controle
- [ ] Task 13 — Verificação ponta a ponta e README  ⟵ etapas manuais da pessoa
```

### Nota: Decisões e bloqueios

```markdown
# Decisões e bloqueios

Registre aqui: desvios do plano, achados Menores da revisão e perguntas para a pessoa.
Formato: data · Task N · quem · o que aconteceu · decisão ou pendência.
```
