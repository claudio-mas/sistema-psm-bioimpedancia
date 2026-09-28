# Plano 1 — Fundação (psm-web) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar a base da aplicação web: login com sessão no servidor, três perfis com permissões verificadas na API, auditoria, cadastro de pacientes com detecção de edição concorrente e importação dos cadastros da aba Clientes do `Bioimpedancia.xlsm`.

**Architecture:** Repositório novo `C:\Sistema\PSM\psm-web` com npm workspaces: `apps/api` (NestJS 11 + Prisma 6 + PostgreSQL 16) e `apps/web` (React 19 + Vite 6 + React Router 7 + TanStack Query 5 + Tailwind 4). A API é a única fonte de regras e permissões; o navegador só exibe. O Postgres de desenvolvimento e de teste roda em Docker.

**Tech Stack:** Node 24, npm ≥ 10, NestJS 11, Prisma 6, PostgreSQL 16 (Docker), Jest 29 + Supertest (API), React 19, Vite 6, Vitest 3 + Testing Library (web), Tailwind CSS 4, jszip + fast-xml-parser (leitura da planilha).

**Spec:** [consolidação aprovada](../specs/2026-09-27-migracao-web-brainstorming.md) e [portões por plano](../../migracao-web/divergencias-e-homologacao.md#5-portões-por-plano-de-implementação). Dicionário de campos: [dicionario-dados.md](../../migracao-web/dicionario-dados.md) (Pacientes — Clientes, 25 colunas).

Todos os caminhos de código abaixo são relativos a `C:\Sistema\PSM\psm-web`. Comandos em PowerShell, executados nessa pasta, salvo indicação.

## Global Constraints

- Operação: "Uma clínica, hospedada na nuvem"; "Até cinco usuários" simultâneos.
- Stack aprovada: "SPA em React + TypeScript", "NestJS", "PostgreSQL".
- Migração: "Somente cadastros de pacientes; novas anamneses e avaliações no sistema web". Fotos e anexos cadastrais ficam fora da carga inicial; o anexo de anamnese (coluna Y) não entra.
- "Preservar o identificador legado como referência": `legacyId` = Seq da aba Clientes. IDs internos são UUID, independentes do legado.
- Perfis: `ADMIN`, `PROFISSIONAL`, `ASSISTENTE`. "Finalização e emissão clínica ficam reservadas ao profissional/administrador por padrão"; a delegação ao assistente é configurável e começa desligada.
- Padrão do assistente ("preenchimento supervisionado"): concedidas `patients.read`, `patients.write`, `anamneses.draft`, `assessments.draft`, `ocr.submit`, `history.read`; negadas `documents.read`, `anamneses.finalize`, `assessments.finalize`, `documents.issue`.
- "Todas as permissões devem ser verificadas na API, inclusive acesso a anexos."
- "Mudanças de permissão e revisões clínicas devem registrar autor e data." Toda escrita de usuário, permissão, paciente e importação grava `audit_events`.
- Concorrência: "verificar a versão do registro ao salvar". Paciente tem `version`; salvar com versão antiga responde 409 com a versão atual.
- Ausente é `null`, nunca `0` nem `''`. CPF, RG, CEP e telefones são texto; zeros à esquerda preservados.
- CPF: 11 dígitos, sem validação de dígito verificador (igual ao legado). CEP: 8 dígitos. UF: uma das 27 siglas.
- Sessão: cookie `psm_sid` httpOnly, SameSite=Lax, `Secure` em produção; ociosidade 120 min; limite absoluto 12 h. Bloqueio de 15 min após 5 senhas erradas. Senha com no mínimo 10 caracteres, hash scrypt (N = 131072 em produção).
- Duplicidade na importação: pelo identificador legado; alerta quando CPF ou nome + nascimento coincidirem. Possível duplicado só é importado com confirmação explícita.
- Datas civis (`birth_date`) são `date`; instantes (auditoria, sessão) são `timestamptz`. Fuso da clínica: `America/Sao_Paulo`.
- Nenhuma regra clínica neste plano: idade, IMC e classificações ficam para o Plano 3 (ver DIV-15). A idade não é armazenada no paciente.
- Identidade visual: alabastro #FDFBF7, grafite #1F1A17, terracota #7A2B22, ouro #C5A059, oliva #8A9A70, areia #E2DCD3. "Playfair Display em títulos; Inter em formulários, tabelas e textos." Texto só em grafite ou terracota; ouro, oliva e areia são decorativos (contraste insuficiente para texto).
- Interface em português do Brasil. Dados de teste sempre sintéticos; nunca usar registros reais de pacientes em testes, logs ou commits.

## Review Focus

1. **Planilha fora do layout** (coluna movida ou renomeada, aba ausente, arquivo que não é planilha): a importação inteira é recusada com a lista de problemas; nada é gravado. Testes: Task 7 (`clientes-mapper.spec.ts`, `xlsx-reader.spec.ts`) e Task 8 (`import.e2e-spec.ts`).
2. **Documentos gravados como número pelo VBA** (`CDbl` removeu zeros à esquerda de CPF/CEP) e CPF com máscara: restaurar zeros com aviso; nunca gravar CPF/CEP com tamanho errado. Testes: Task 7.
3. **Nascimento como serial do Excel, texto dd/mm/aaaa, vazio, futuro ou inexistente (31/02)**: aceitar serial e texto; rejeitar os demais com motivo no relatório. Testes: Task 7.
4. **Sessão aberta de usuário desativado ou com senha redefinida**: a próxima requisição responde 401. Testes: Task 5 (`admin.e2e-spec.ts`).
5. **Busca sem acento, com maiúsculas, espaços extras ou palavras fora de ordem** ("joao  SILVA" encontra "João da Silva"; cada palavra digitada precisa aparecer no nome). Testes: Task 6 (`patients.e2e-spec.ts`).

---

## Mapa de arquivos

```
psm-web/
  package.json                      workspaces e scripts da raiz
  docker-compose.yml                Postgres 16 (porta 5433) com bancos psm e psm_test
  docker/initdb/01-test-db.sql      cria o banco psm_test
  README.md                         como rodar, primeiro administrador, verificação
  apps/api/
    prisma/schema.prisma            modelos: AuditEvent, User, Session, AssistantPermission, Patient, ImportBatch
    src/main.ts                     bootstrap HTTP
    src/app.module.ts               composição dos módulos
    src/app.setup.ts                prefixo /api, cookies, validação, verificação de origem
    src/config.ts, config.module.ts variáveis de ambiente tipadas
    src/prisma/                     PrismaService
    src/audit/                      AuditService, diff de campos, GET /api/audit
    src/users/                      PasswordService (scrypt), UsersService, /api/users
    src/permissions/                catálogo, PermissionsService, /api/permissions/assistant
    src/auth/                       sessões, login/logout/me, AuthGuard global, decorators
    src/security/                   middleware de origem
    src/common/                     texto, datas civis, erros do Prisma
    src/patients/                   normalização, PatientsService, /api/patients
    src/import/                     leitor xlsx, mapeamento Clientes, classificação, /api/imports
    src/scripts/create-admin.ts     cria o primeiro administrador
    test/                           e2e com Postgres real + helpers
  apps/web/
    src/lib/                        cliente da API, QueryClient, formatação
    src/auth/                       sessão atual, RequireAuth, RequirePermission
    src/layout/                     layout e navegação por permissão
    src/components/ui/              Button, Input, Select, Textarea, Field, Alert, PageTitle
    src/pages/                      Login, Início
    src/patients/                   lista e formulário
    src/admin/                      usuários, permissões, auditoria, importação
    src/test/                       mock de fetch, render, fixtures
```

---

### Task 1: Repositório, Postgres em Docker e esqueleto da API com auditoria

**Files:**
- Create: `package.json`, `.gitignore`, `.nvmrc`, `.editorconfig`, `docker-compose.yml`, `docker/initdb/01-test-db.sql`, `README.md`
- Create: `apps/api/package.json`, `apps/api/tsconfig.json`, `apps/api/tsconfig.build.json`, `apps/api/nest-cli.json`, `apps/api/jest.config.js`, `apps/api/.env.example`, `apps/api/.env.test`, `apps/api/prisma/schema.prisma`
- Create: `apps/api/src/main.ts`, `src/app.module.ts`, `src/app.setup.ts`, `src/config.ts`, `src/config.module.ts`, `src/prisma/prisma.service.ts`, `src/prisma/prisma.module.ts`, `src/auth/public.decorator.ts`, `src/health/health.controller.ts`, `src/audit/audit.service.ts`, `src/audit/audit.module.ts`, `src/audit/diff.ts`
- Test: `apps/api/test/setup-env.ts`, `test/global-setup.ts`, `test/helpers/app.ts`, `test/helpers/db.ts`, `test/health.e2e-spec.ts`, `test/audit.e2e-spec.ts`, `src/audit/diff.spec.ts`

**Interfaces:**
- Produces: `configureApp(app: INestApplication): void`; `APP_CONFIG` + `AppConfig { appOrigin; cookieSecure; clinicTimeZone; scryptN; sessionIdleMinutes; sessionAbsoluteHours }`; `PrismaService` (global); `AuditService.record(input: AuditInput, db?: Prisma.TransactionClient): Promise<void>`; `AuditService.list(q: AuditQuery): Promise<AuditEvent[]>`; `diffFields<T extends object>(before: T, after: T, fields: readonly (keyof T & string)[]): FieldChanges`; `Public()` decorator e `IS_PUBLIC`; helpers de teste `createTestApp(): Promise<INestApplication>` e `resetDb(prisma: PrismaClient): Promise<void>`.

- [ ] **Step 1: Conferir ferramentas**

Run: `npm --version`
Expected: `10.x` ou superior. Se aparecer `8.x` (há um npm global antigo em `%APPDATA%\npm` nesta máquina), rode `npm install -g npm@11` e confira de novo.

Run: `docker compose version`
Expected: `Docker Compose version v2...`. Se falhar, abra o Docker Desktop e aguarde "Engine running".

- [ ] **Step 2: Criar o repositório e os arquivos da raiz**

```powershell
New-Item -ItemType Directory -Force C:\Sistema\PSM\psm-web | Out-Null
Set-Location C:\Sistema\PSM\psm-web
git init
```

`package.json`:

```json
{
  "name": "psm-web",
  "private": true,
  "workspaces": ["apps/*"],
  "engines": { "node": ">=22", "npm": ">=10" },
  "scripts": {
    "db:up": "docker compose up -d postgres",
    "db:down": "docker compose down",
    "test": "npm run test --workspaces --if-present",
    "typecheck": "npm run typecheck --workspaces --if-present",
    "build": "npm run build --workspaces --if-present"
  }
}
```

`.gitignore`:

```
node_modules/
dist/
coverage/
.env
*.log
```

`.nvmrc`:

```
24
```

`.editorconfig`:

```
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
indent_size = 2
insert_final_newline = true
trim_trailing_whitespace = true
```

`docker-compose.yml`:

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_USER: psm
      POSTGRES_PASSWORD: psm
      POSTGRES_DB: psm
    ports:
      - "5433:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./docker/initdb:/docker-entrypoint-initdb.d:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U psm -d psm"]
      interval: 5s
      timeout: 3s
      retries: 20
volumes:
  pgdata:
```

`docker/initdb/01-test-db.sql`:

```sql
CREATE DATABASE psm_test OWNER psm;
```

`README.md`:

````markdown
# psm-web

Aplicação web do Sistema PSM (migração do Bioimpedancia.xlsm). Especificação no repositório do suplemento, em `docs/migracao-web/` e `docs/superpowers/`.

## Desenvolvimento

```powershell
npm install
npm run db:up
Copy-Item apps/api/.env.example apps/api/.env
npm test
```
````

- [ ] **Step 3: Subir o Postgres e conferir os dois bancos**

Run: `npm run db:up`
Then: `docker compose exec postgres psql -U psm -d psm -c "SELECT datname FROM pg_database ORDER BY 1;"`
Expected: a lista contém `psm` e `psm_test`.

- [ ] **Step 4: Criar a configuração do pacote da API**

`apps/api/package.json`:

```json
{
  "name": "@psm/api",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "dev": "nest start --watch",
    "build": "tsc -p tsconfig.build.json",
    "start": "node dist/main.js",
    "test": "jest --runInBand",
    "typecheck": "tsc --noEmit"
  },
  "dependencies": {
    "@nestjs/common": "^11.0.0",
    "@nestjs/core": "^11.0.0",
    "@nestjs/platform-express": "^11.0.0",
    "@prisma/client": "^6.0.0",
    "class-transformer": "^0.5.1",
    "class-validator": "^0.14.1",
    "cookie-parser": "^1.4.7",
    "dotenv": "^16.4.5",
    "reflect-metadata": "^0.2.2",
    "rxjs": "^7.8.1"
  },
  "devDependencies": {
    "@nestjs/cli": "^11.0.0",
    "@nestjs/testing": "^11.0.0",
    "@types/cookie-parser": "^1.4.7",
    "@types/express": "^5.0.0",
    "@types/jest": "^29.5.12",
    "@types/node": "^24.0.0",
    "@types/supertest": "^6.0.2",
    "jest": "^29.7.0",
    "prisma": "^6.0.0",
    "supertest": "^7.0.0",
    "ts-jest": "^29.2.5",
    "typescript": "^5.7.0"
  }
}
```

`apps/api/tsconfig.json`:

```json
{
  "compilerOptions": {
    "module": "commonjs",
    "target": "ES2022",
    "lib": ["ES2022"],
    "strict": true,
    "strictPropertyInitialization": false,
    "emitDecoratorMetadata": true,
    "experimentalDecorators": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "sourceMap": true,
    "outDir": "dist",
    "rootDir": ".",
    "types": ["node", "jest"]
  },
  "include": ["src", "test"]
}
```

`apps/api/tsconfig.build.json`:

```json
{
  "extends": "./tsconfig.json",
  "compilerOptions": { "rootDir": "src", "outDir": "dist", "types": ["node"] },
  "include": ["src"],
  "exclude": ["src/**/*.spec.ts"]
}
```

`apps/api/nest-cli.json`:

```json
{
  "$schema": "https://json.schemastore.org/nest-cli",
  "sourceRoot": "src",
  "compilerOptions": { "tsConfigPath": "tsconfig.build.json", "deleteOutDir": true }
}
```

`apps/api/jest.config.js`:

```js
/** @type {import('jest').Config} */
module.exports = {
  rootDir: '.',
  testEnvironment: 'node',
  moduleFileExtensions: ['ts', 'js', 'json'],
  transform: { '^.+\\.ts$': ['ts-jest', { tsconfig: 'tsconfig.json' }] },
  testRegex: '(src|test)/.*\\.(spec|e2e-spec)\\.ts$',
  setupFiles: ['<rootDir>/test/setup-env.ts'],
  globalSetup: '<rootDir>/test/global-setup.ts',
};
```

`apps/api/.env.example`:

```
DATABASE_URL=postgresql://psm:psm@localhost:5433/psm?schema=public
APP_ORIGIN=http://localhost:5173
COOKIE_SECURE=false
PORT=3000
CLINIC_TIME_ZONE=America/Sao_Paulo
# Produção: omitir (padrão 131072). Desenvolvimento: 16384 deixa o login rápido.
SCRYPT_N=16384
SESSION_IDLE_MINUTES=120
SESSION_ABSOLUTE_HOURS=12
```

`apps/api/.env.test`:

```
DATABASE_URL=postgresql://psm:psm@localhost:5433/psm_test?schema=public
APP_ORIGIN=http://localhost:5173
COOKIE_SECURE=false
CLINIC_TIME_ZONE=America/Sao_Paulo
SCRYPT_N=16384
SESSION_IDLE_MINUTES=120
SESSION_ABSOLUTE_HOURS=12
```

Run:

```powershell
Copy-Item apps/api/.env.example apps/api/.env
npm install
```

Expected: instalação sem erros (avisos de `deprecated` são aceitáveis).

- [ ] **Step 5: Escrever os testes (health, auditoria, diff) e os helpers**

`apps/api/test/setup-env.ts`:

```ts
import { join } from 'node:path';
import { config } from 'dotenv';

config({ path: join(__dirname, '..', '.env.test'), override: true });
```

`apps/api/test/global-setup.ts`:

```ts
import { execSync } from 'node:child_process';
import { join } from 'node:path';
import { config } from 'dotenv';

export default function globalSetup(): void {
  const parsed = config({ path: join(__dirname, '..', '.env.test'), override: true }).parsed ?? {};
  execSync('npx prisma migrate reset --force --skip-seed --skip-generate', {
    cwd: join(__dirname, '..'),
    env: { ...process.env, ...parsed },
    stdio: 'inherit',
  });
}
```

`apps/api/test/helpers/app.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import { AppModule } from '../../src/app.module';
import { configureApp } from '../../src/app.setup';

export async function createTestApp(): Promise<INestApplication> {
  const moduleRef = await Test.createTestingModule({ imports: [AppModule] }).compile();
  const app = moduleRef.createNestApplication();
  configureApp(app);
  await app.init();
  return app;
}
```

`apps/api/test/helpers/db.ts`:

```ts
import { PrismaClient } from '@prisma/client';

export async function resetDb(prisma: PrismaClient): Promise<void> {
  const tables = await prisma.$queryRaw<{ tablename: string }[]>`
    SELECT tablename FROM pg_tables
    WHERE schemaname = 'public' AND tablename <> '_prisma_migrations'`;
  if (tables.length === 0) return;
  const list = tables.map((t) => `"public"."${t.tablename}"`).join(', ');
  await prisma.$executeRawUnsafe(`TRUNCATE TABLE ${list} RESTART IDENTITY CASCADE`);
}
```

`apps/api/test/health.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import request from 'supertest';
import { createTestApp } from './helpers/app';

describe('GET /api/health', () => {
  let app: INestApplication;

  beforeAll(async () => {
    app = await createTestApp();
  });

  afterAll(async () => {
    await app.close();
  });

  it('responde ok quando o banco está acessível', async () => {
    const res = await request(app.getHttpServer()).get('/api/health');
    expect(res.status).toBe(200);
    expect(res.body).toEqual({ status: 'ok' });
  });
});
```

`apps/api/test/audit.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import { AuditService } from '../src/audit/audit.service';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { resetDb } from './helpers/db';

describe('AuditService', () => {
  let app: INestApplication;
  let audit: AuditService;
  let prisma: PrismaService;

  beforeAll(async () => {
    app = await createTestApp();
    audit = app.get(AuditService);
    prisma = app.get(PrismaService);
  });

  beforeEach(() => resetDb(prisma));

  afterAll(async () => {
    await app.close();
  });

  it('grava e lista eventos do mais recente para o mais antigo', async () => {
    await audit.record({ actorId: null, action: 'teste.primeiro', entityType: 'x', entityId: '1' });
    await audit.record({ actorId: null, action: 'teste.segundo', entityType: 'x', entityId: '1', data: { campo: 'valor' } });
    await audit.record({ actorId: null, action: 'teste.outro', entityType: 'y', entityId: '2' });

    const events = await audit.list({ entityType: 'x', entityId: '1' });

    expect(events.map((e) => e.action)).toEqual(['teste.segundo', 'teste.primeiro']);
    expect(events[0].data).toEqual({ campo: 'valor' });
    expect(events[1].data).toBeNull();
  });

  it('grava dentro de uma transação e desfaz junto com ela', async () => {
    await expect(
      prisma.$transaction(async (tx) => {
        await audit.record({ actorId: null, action: 'teste.desfeito' }, tx);
        throw new Error('falha proposital');
      }),
    ).rejects.toThrow('falha proposital');

    expect(await prisma.auditEvent.count()).toBe(0);
  });
});
```

`apps/api/src/audit/diff.spec.ts`:

```ts
import { diffFields } from './diff';

describe('diffFields', () => {
  it('retorna só os campos alterados, com datas em ISO e ausente como null', () => {
    const before = { a: 'x', b: new Date('2000-01-02T00:00:00Z'), c: undefined as string | undefined, d: 1 };
    const after = { a: 'y', b: new Date('2000-01-02T00:00:00Z'), c: 'novo' as string | undefined, d: 1 };

    expect(diffFields(before, after, ['a', 'b', 'c', 'd'])).toEqual({
      a: { from: 'x', to: 'y' },
      c: { from: null, to: 'novo' },
    });
  });

  it('retorna objeto vazio quando nada mudou', () => {
    expect(diffFields({ a: null }, { a: null }, ['a'])).toEqual({});
  });
});
```

- [ ] **Step 6: Rodar os testes e ver falhar**

Run: `npm test -w @psm/api`
Expected: FAIL — `prisma migrate reset` avisa que não há schema/migrations, ou os testes falham com `Cannot find module '../src/app.module'` / `'./diff'`.

- [ ] **Step 7: Implementar schema, configuração, Prisma, auditoria e health**

`apps/api/prisma/schema.prisma`:

```prisma
generator client {
  provider = "prisma-client-js"
}

datasource db {
  provider = "postgresql"
  url      = env("DATABASE_URL")
}

model AuditEvent {
  id         Int      @id @default(autoincrement())
  occurredAt DateTime @default(now()) @map("occurred_at") @db.Timestamptz(3)
  actorId    String?  @map("actor_id") @db.Uuid
  action     String
  entityType String?  @map("entity_type")
  entityId   String?  @map("entity_id")
  data       Json?
  ip         String?

  @@index([entityType, entityId])
  @@map("audit_events")
}
```

`apps/api/src/config.ts`:

```ts
export const APP_CONFIG = Symbol('APP_CONFIG');

export interface AppConfig {
  appOrigin: string;
  cookieSecure: boolean;
  clinicTimeZone: string;
  scryptN: number;
  sessionIdleMinutes: number;
  sessionAbsoluteHours: number;
}

function required(name: string): string {
  const value = process.env[name];
  if (!value) throw new Error(`Variável de ambiente ausente: ${name}`);
  return value;
}

function positiveInt(name: string, fallback: number): number {
  const raw = process.env[name];
  if (raw === undefined || raw === '') return fallback;
  const value = Number(raw);
  if (!Number.isInteger(value) || value <= 0) throw new Error(`Variável de ambiente inválida: ${name}=${raw}`);
  return value;
}

export function loadConfig(): AppConfig {
  return {
    appOrigin: required('APP_ORIGIN'),
    cookieSecure: process.env.COOKIE_SECURE !== 'false',
    clinicTimeZone: process.env.CLINIC_TIME_ZONE || 'America/Sao_Paulo',
    scryptN: positiveInt('SCRYPT_N', 131072),
    sessionIdleMinutes: positiveInt('SESSION_IDLE_MINUTES', 120),
    sessionAbsoluteHours: positiveInt('SESSION_ABSOLUTE_HOURS', 12),
  };
}
```

`apps/api/src/config.module.ts`:

```ts
import { Global, Module } from '@nestjs/common';
import { APP_CONFIG, loadConfig } from './config';

@Global()
@Module({
  providers: [{ provide: APP_CONFIG, useFactory: loadConfig }],
  exports: [APP_CONFIG],
})
export class ConfigModule {}
```

`apps/api/src/prisma/prisma.service.ts`:

```ts
import { Injectable, OnModuleDestroy, OnModuleInit } from '@nestjs/common';
import { PrismaClient } from '@prisma/client';

@Injectable()
export class PrismaService extends PrismaClient implements OnModuleInit, OnModuleDestroy {
  async onModuleInit(): Promise<void> {
    await this.$connect();
  }

  async onModuleDestroy(): Promise<void> {
    await this.$disconnect();
  }
}
```

`apps/api/src/prisma/prisma.module.ts`:

```ts
import { Global, Module } from '@nestjs/common';
import { PrismaService } from './prisma.service';

@Global()
@Module({ providers: [PrismaService], exports: [PrismaService] })
export class PrismaModule {}
```

`apps/api/src/audit/diff.ts`:

```ts
export type JsonScalar = string | number | boolean | null;
export type FieldChanges = Record<string, { from: JsonScalar; to: JsonScalar }>;

function normalize(value: unknown): JsonScalar {
  if (value === null || value === undefined) return null;
  if (value instanceof Date) return value.toISOString();
  if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') return value;
  return String(value);
}

export function diffFields<T extends object>(before: T, after: T, fields: readonly (keyof T & string)[]): FieldChanges {
  const changes: FieldChanges = {};
  for (const field of fields) {
    const from = normalize((before as Record<string, unknown>)[field]);
    const to = normalize((after as Record<string, unknown>)[field]);
    if (from !== to) changes[field] = { from, to };
  }
  return changes;
}
```

`apps/api/src/audit/audit.service.ts`:

```ts
import { Injectable } from '@nestjs/common';
import { AuditEvent, Prisma } from '@prisma/client';
import { PrismaService } from '../prisma/prisma.service';

export interface AuditInput {
  actorId: string | null;
  action: string;
  entityType?: string;
  entityId?: string;
  data?: Prisma.InputJsonValue;
  ip?: string | null;
}

export interface AuditQuery {
  entityType?: string;
  entityId?: string;
  limit?: number;
}

@Injectable()
export class AuditService {
  constructor(private readonly prisma: PrismaService) {}

  async record(input: AuditInput, db: Prisma.TransactionClient = this.prisma): Promise<void> {
    await db.auditEvent.create({
      data: {
        actorId: input.actorId,
        action: input.action,
        entityType: input.entityType,
        entityId: input.entityId,
        data: input.data,
        ip: input.ip ?? undefined,
      },
    });
  }

  list(query: AuditQuery): Promise<AuditEvent[]> {
    return this.prisma.auditEvent.findMany({
      where: { entityType: query.entityType, entityId: query.entityId },
      orderBy: { id: 'desc' },
      take: Math.min(query.limit ?? 100, 500),
    });
  }
}
```

`apps/api/src/audit/audit.module.ts`:

```ts
import { Global, Module } from '@nestjs/common';
import { AuditService } from './audit.service';

@Global()
@Module({ providers: [AuditService], exports: [AuditService] })
export class AuditModule {}
```

`apps/api/src/auth/public.decorator.ts`:

```ts
import { SetMetadata } from '@nestjs/common';

export const IS_PUBLIC = 'isPublic';
export const Public = () => SetMetadata(IS_PUBLIC, true);
```

`apps/api/src/health/health.controller.ts`:

```ts
import { Controller, Get } from '@nestjs/common';
import { Public } from '../auth/public.decorator';
import { PrismaService } from '../prisma/prisma.service';

@Controller('health')
export class HealthController {
  constructor(private readonly prisma: PrismaService) {}

  @Public()
  @Get()
  async check(): Promise<{ status: 'ok' }> {
    await this.prisma.$queryRaw`SELECT 1`;
    return { status: 'ok' };
  }
}
```

`apps/api/src/app.module.ts`:

```ts
import { Module } from '@nestjs/common';
import { AuditModule } from './audit/audit.module';
import { ConfigModule } from './config.module';
import { HealthController } from './health/health.controller';
import { PrismaModule } from './prisma/prisma.module';

@Module({
  imports: [ConfigModule, PrismaModule, AuditModule],
  controllers: [HealthController],
})
export class AppModule {}
```

`apps/api/src/app.setup.ts`:

```ts
import { INestApplication, ValidationPipe } from '@nestjs/common';
import cookieParser from 'cookie-parser';

export function configureApp(app: INestApplication): void {
  app.setGlobalPrefix('api');
  app.use(cookieParser());
  app.useGlobalPipes(new ValidationPipe({ whitelist: true, forbidNonWhitelisted: true, transform: true }));
}
```

`apps/api/src/main.ts`:

```ts
import 'reflect-metadata';
import 'dotenv/config';
import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { configureApp } from './app.setup';

async function bootstrap(): Promise<void> {
  const app = await NestFactory.create(AppModule);
  configureApp(app);
  await app.listen(Number(process.env.PORT ?? 3000));
}

void bootstrap();
```

Criar a migration no banco de desenvolvimento:

```powershell
Set-Location apps/api
npx prisma migrate dev --name init
Set-Location ../..
```

Expected: `Your database is now in sync with your schema.` e a pasta `apps/api/prisma/migrations/<data>_init/` criada.

- [ ] **Step 8: Rodar os testes e ver passar**

Run: `npm test -w @psm/api`
Expected: PASS — 3 suítes (`health`, `audit`, `diff`), 5 testes.

Run: `npm run typecheck -w @psm/api`
Expected: sem erros.

- [ ] **Step 9: Commit**

```powershell
git add -A
git commit -m "feat(api): esqueleto NestJS com Postgres, health e auditoria"
```

---

### Task 2: Usuários e senhas

**Files:**
- Modify: `apps/api/prisma/schema.prisma` (enum `Role`, modelo `User`)
- Create: `apps/api/src/common/prisma-errors.ts`, `src/users/password.service.ts`, `src/users/users.types.ts`, `src/users/users.service.ts`, `src/users/users.module.ts`, `src/scripts/create-admin.ts`
- Modify: `apps/api/src/app.module.ts`, `apps/api/package.json` (script `create-admin`, dependência `ts-node`)
- Test: `apps/api/src/users/password.service.spec.ts`, `apps/api/test/users.e2e-spec.ts`

**Interfaces:**
- Consumes: `AuditService.record`, `PrismaService`, `APP_CONFIG`.
- Produces: `PasswordService.hash(password: string): Promise<string>`; `PasswordService.verify(password: string, stored: string): Promise<boolean>`; `UsersService.create(input: CreateUserInput, actorId: string | null): Promise<PublicUser>`; `UsersService.list(): Promise<PublicUser[]>`; `UsersService.normalizeUsername(raw: string): string` (estático); `PublicUser { id; username; displayName; role: Role; active; createdAt: string }`; `toPublicUser(user: User): PublicUser`; `assertPasswordPolicy(password: string): void`; `MIN_PASSWORD_LENGTH = 10`; `isUniqueViolation(error: unknown): boolean`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/users/password.service.spec.ts`:

```ts
import type { AppConfig } from '../config';
import { PasswordService } from './password.service';

function service(scryptN: number): PasswordService {
  return new PasswordService({ scryptN } as AppConfig);
}

describe('PasswordService', () => {
  it('gera hash scrypt com parâmetros e valida a senha correta', async () => {
    const passwords = service(16384);
    const stored = await passwords.hash('senha-segura-123');
    expect(stored).toMatch(/^scrypt\$16384\$8\$1\$[A-Za-z0-9+/=]+\$[A-Za-z0-9+/=]+$/);
    await expect(passwords.verify('senha-segura-123', stored)).resolves.toBe(true);
  });

  it('recusa senha errada e hash malformado', async () => {
    const passwords = service(16384);
    const stored = await passwords.hash('senha-segura-123');
    await expect(passwords.verify('senha-errada-123', stored)).resolves.toBe(false);
    await expect(passwords.verify('senha-segura-123', 'texto-qualquer')).resolves.toBe(false);
  });

  it('usa sal aleatório', async () => {
    const passwords = service(16384);
    expect(await passwords.hash('mesma-senha-123')).not.toBe(await passwords.hash('mesma-senha-123'));
  });

  it('valida hash antigo mesmo depois de mudar o custo configurado', async () => {
    const stored = await service(16384).hash('senha-segura-123');
    await expect(service(32768).verify('senha-segura-123', stored)).resolves.toBe(true);
  });
});
```

`apps/api/test/users.e2e-spec.ts`:

```ts
import { randomUUID } from 'node:crypto';
import { BadRequestException, ConflictException, INestApplication } from '@nestjs/common';
import { PrismaService } from '../src/prisma/prisma.service';
import { UsersService } from '../src/users/users.service';
import { createTestApp } from './helpers/app';
import { resetDb } from './helpers/db';

describe('UsersService', () => {
  let app: INestApplication;
  let users: UsersService;
  let prisma: PrismaService;

  beforeAll(async () => {
    app = await createTestApp();
    users = app.get(UsersService);
    prisma = app.get(PrismaService);
  });

  beforeEach(() => resetDb(prisma));

  afterAll(async () => {
    await app.close();
  });

  it('cria usuário com nome normalizado, hash da senha e auditoria', async () => {
    const actorId = randomUUID();
    const user = await users.create(
      { username: '  Ana.Silva ', displayName: ' Ana Silva ', password: 'senha-segura-123', role: 'PROFISSIONAL' },
      actorId,
    );

    expect(user).toEqual({
      id: expect.any(String),
      username: 'ana.silva',
      displayName: 'Ana Silva',
      role: 'PROFISSIONAL',
      active: true,
      createdAt: expect.any(String),
    });
    const stored = await prisma.user.findUniqueOrThrow({ where: { id: user.id } });
    expect(stored.passwordHash).toMatch(/^scrypt\$16384\$8\$1\$/);
    const events = await prisma.auditEvent.findMany({ where: { action: 'user.create' } });
    expect(events).toHaveLength(1);
    expect(events[0]).toMatchObject({
      actorId,
      entityType: 'user',
      entityId: user.id,
      data: { username: 'ana.silva', role: 'PROFISSIONAL' },
    });
  });

  it('recusa nome de usuário repetido sem diferenciar maiúsculas', async () => {
    await users.create({ username: 'ana', displayName: 'Ana', password: 'senha-segura-123', role: 'ADMIN' }, null);
    await expect(
      users.create({ username: 'ANA', displayName: 'Outra', password: 'senha-segura-123', role: 'ADMIN' }, null),
    ).rejects.toBeInstanceOf(ConflictException);
  });

  it('recusa senha curta, usuário inválido e nome vazio sem gravar nada', async () => {
    await expect(
      users.create({ username: 'bia', displayName: 'Bia', password: 'curta', role: 'ASSISTENTE' }, null),
    ).rejects.toBeInstanceOf(BadRequestException);
    await expect(
      users.create({ username: 'b@', displayName: 'Bia', password: 'senha-segura-123', role: 'ASSISTENTE' }, null),
    ).rejects.toBeInstanceOf(BadRequestException);
    await expect(
      users.create({ username: 'bia', displayName: '   ', password: 'senha-segura-123', role: 'ASSISTENTE' }, null),
    ).rejects.toBeInstanceOf(BadRequestException);
    expect(await prisma.user.count()).toBe(0);
  });

  it('lista em ordem alfabética sem expor o hash', async () => {
    await users.create({ username: 'zeca', displayName: 'Zeca', password: 'senha-segura-123', role: 'ASSISTENTE' }, null);
    await users.create({ username: 'ana', displayName: 'Ana', password: 'senha-segura-123', role: 'ADMIN' }, null);

    const list = await users.list();

    expect(list.map((u) => u.username)).toEqual(['ana', 'zeca']);
    expect(list[0]).not.toHaveProperty('passwordHash');
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- users password`
Expected: FAIL — `Cannot find module '../src/users/users.service'` e `'./password.service'`.

- [ ] **Step 3: Implementar**

Acrescentar ao `apps/api/prisma/schema.prisma`:

```prisma
enum Role {
  ADMIN
  PROFISSIONAL
  ASSISTENTE
}

model User {
  id               String    @id @default(uuid()) @db.Uuid
  username         String    @unique
  displayName      String    @map("display_name")
  passwordHash     String    @map("password_hash")
  role             Role
  active           Boolean   @default(true)
  failedLoginCount Int       @default(0) @map("failed_login_count")
  lockedUntil      DateTime? @map("locked_until") @db.Timestamptz(3)
  createdAt        DateTime  @default(now()) @map("created_at") @db.Timestamptz(3)
  updatedAt        DateTime  @updatedAt @map("updated_at") @db.Timestamptz(3)

  @@map("users")
}
```

`apps/api/src/common/prisma-errors.ts`:

```ts
import { Prisma } from '@prisma/client';

export function isUniqueViolation(error: unknown): boolean {
  return error instanceof Prisma.PrismaClientKnownRequestError && error.code === 'P2002';
}
```

`apps/api/src/users/password.service.ts`:

```ts
import { Inject, Injectable } from '@nestjs/common';
import { randomBytes, scrypt as scryptCallback, timingSafeEqual } from 'node:crypto';
import { APP_CONFIG, AppConfig } from '../config';

const KEY_LENGTH = 32;
const BLOCK_SIZE = 8;
const PARALLELISM = 1;

function scrypt(password: string, salt: Buffer, keyLength: number, n: number, r: number, p: number): Promise<Buffer> {
  return new Promise((resolve, reject) => {
    scryptCallback(password, salt, keyLength, { N: n, r, p, maxmem: 256 * n * r }, (error, key) => {
      if (error) reject(error);
      else resolve(key);
    });
  });
}

@Injectable()
export class PasswordService {
  constructor(@Inject(APP_CONFIG) private readonly config: AppConfig) {}

  async hash(password: string): Promise<string> {
    const salt = randomBytes(16);
    const n = this.config.scryptN;
    const key = await scrypt(password, salt, KEY_LENGTH, n, BLOCK_SIZE, PARALLELISM);
    return ['scrypt', n, BLOCK_SIZE, PARALLELISM, salt.toString('base64'), key.toString('base64')].join('$');
  }

  async verify(password: string, stored: string): Promise<boolean> {
    const parts = stored.split('$');
    if (parts.length !== 6 || parts[0] !== 'scrypt') return false;
    const [n, r, p] = [Number(parts[1]), Number(parts[2]), Number(parts[3])];
    if (![n, r, p].every((v) => Number.isInteger(v) && v > 0)) return false;
    const expected = Buffer.from(parts[5], 'base64');
    const key = await scrypt(password, Buffer.from(parts[4], 'base64'), expected.length, n, r, p);
    return key.length === expected.length && timingSafeEqual(key, expected);
  }
}
```

`apps/api/src/users/users.types.ts`:

```ts
import { BadRequestException } from '@nestjs/common';
import type { Role, User } from '@prisma/client';

export const MIN_PASSWORD_LENGTH = 10;

export interface PublicUser {
  id: string;
  username: string;
  displayName: string;
  role: Role;
  active: boolean;
  createdAt: string;
}

export interface CreateUserInput {
  username: string;
  displayName: string;
  password: string;
  role: Role;
}

export function toPublicUser(user: User): PublicUser {
  return {
    id: user.id,
    username: user.username,
    displayName: user.displayName,
    role: user.role,
    active: user.active,
    createdAt: user.createdAt.toISOString(),
  };
}

export function assertPasswordPolicy(password: string): void {
  if (password.length < MIN_PASSWORD_LENGTH) {
    throw new BadRequestException(`A senha deve ter pelo menos ${MIN_PASSWORD_LENGTH} caracteres.`);
  }
}
```

`apps/api/src/users/users.service.ts`:

```ts
import { BadRequestException, ConflictException, Injectable } from '@nestjs/common';
import type { User } from '@prisma/client';
import { AuditService } from '../audit/audit.service';
import { isUniqueViolation } from '../common/prisma-errors';
import { PrismaService } from '../prisma/prisma.service';
import { PasswordService } from './password.service';
import { assertPasswordPolicy, CreateUserInput, PublicUser, toPublicUser } from './users.types';

const USERNAME_PATTERN = /^[a-z0-9._-]{3,40}$/;

@Injectable()
export class UsersService {
  constructor(
    private readonly prisma: PrismaService,
    private readonly passwords: PasswordService,
    private readonly audit: AuditService,
  ) {}

  static normalizeUsername(raw: string): string {
    return raw.trim().toLowerCase();
  }

  async create(input: CreateUserInput, actorId: string | null): Promise<PublicUser> {
    const username = UsersService.normalizeUsername(input.username);
    const displayName = input.displayName.trim();
    if (!USERNAME_PATTERN.test(username)) {
      throw new BadRequestException('O usuário deve ter de 3 a 40 caracteres: letras, números, ponto, hífen ou sublinhado.');
    }
    if (displayName === '') throw new BadRequestException('Informe o nome de exibição.');
    assertPasswordPolicy(input.password);

    const passwordHash = await this.passwords.hash(input.password);
    let user: User;
    try {
      user = await this.prisma.$transaction(async (tx) => {
        const created = await tx.user.create({ data: { username, displayName, passwordHash, role: input.role } });
        await this.audit.record(
          { actorId, action: 'user.create', entityType: 'user', entityId: created.id, data: { username, role: input.role } },
          tx,
        );
        return created;
      });
    } catch (error) {
      if (isUniqueViolation(error)) throw new ConflictException('Nome de usuário já existe.');
      throw error;
    }
    return toPublicUser(user);
  }

  async list(): Promise<PublicUser[]> {
    const users = await this.prisma.user.findMany({ orderBy: { username: 'asc' } });
    return users.map(toPublicUser);
  }
}
```

`apps/api/src/users/users.module.ts`:

```ts
import { Module } from '@nestjs/common';
import { PasswordService } from './password.service';
import { UsersService } from './users.service';

@Module({
  providers: [PasswordService, UsersService],
  exports: [PasswordService, UsersService],
})
export class UsersModule {}
```

`apps/api/src/app.module.ts` — adicionar `UsersModule` aos imports:

```ts
import { Module } from '@nestjs/common';
import { AuditModule } from './audit/audit.module';
import { ConfigModule } from './config.module';
import { HealthController } from './health/health.controller';
import { PrismaModule } from './prisma/prisma.module';
import { UsersModule } from './users/users.module';

@Module({
  imports: [ConfigModule, PrismaModule, AuditModule, UsersModule],
  controllers: [HealthController],
})
export class AppModule {}
```

`apps/api/src/scripts/create-admin.ts`:

```ts
import 'reflect-metadata';
import 'dotenv/config';
import { NestFactory } from '@nestjs/core';
import { AppModule } from '../app.module';
import { UsersService } from '../users/users.service';

async function main(): Promise<void> {
  const [username, displayName] = process.argv.slice(2);
  const password = process.env.ADMIN_PASSWORD;
  if (!username || !displayName || !password) {
    console.error('Uso: $env:ADMIN_PASSWORD="..."; npm run create-admin -w @psm/api -- <usuario> "<Nome>"');
    process.exit(1);
  }
  const app = await NestFactory.createApplicationContext(AppModule, { logger: ['error'] });
  try {
    const user = await app.get(UsersService).create({ username, displayName, password, role: 'ADMIN' }, null);
    console.log(`Administrador criado: ${user.username}`);
  } finally {
    await app.close();
  }
}

void main();
```

Em `apps/api/package.json`, acrescentar ao bloco `scripts`:

```json
"create-admin": "ts-node src/scripts/create-admin.ts"
```

Instalar `ts-node` e migrar:

```powershell
npm install -D -w @psm/api ts-node@^10.9.2
Set-Location apps/api
npx prisma migrate dev --name users
Set-Location ../..
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api`
Expected: PASS — todas as suítes, incluindo `password.service.spec` (4) e `users.e2e-spec` (4).

Conferir o script no banco de desenvolvimento:

```powershell
$env:ADMIN_PASSWORD = "troque-esta-senha-1"
npm run create-admin -w @psm/api -- admin "Administrador"
Remove-Item Env:ADMIN_PASSWORD
```

Expected: `Administrador criado: admin`.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(api): usuários com senha scrypt e script do primeiro administrador"
```

---

### Task 3: Catálogo de permissões e configuração do assistente

**Files:**
- Modify: `apps/api/prisma/schema.prisma` (modelo `AssistantPermission`)
- Create: `apps/api/src/permissions/permissions.ts`, `src/permissions/permissions.service.ts`, `src/permissions/permissions.module.ts`
- Modify: `apps/api/src/app.module.ts`
- Test: `apps/api/src/permissions/permissions.spec.ts`, `apps/api/test/permissions.e2e-spec.ts`

**Interfaces:**
- Consumes: `AuditService.record`, `PrismaService`.
- Produces: `PERMISSIONS` (tupla de 14 strings); `type Permission`; `PERMISSION_LABELS`; `RESERVED_BY_DEFAULT`; `ASSISTANT_CONFIGURABLE`; `ASSISTANT_DEFAULT_GRANTED`; `isPermission(value: string): value is Permission`; `fixedPermissions(role: Role): Permission[] | null`; `PermissionsService.forRole(role: Role): Promise<Permission[]>`; `PermissionsService.assistantConfig(): Promise<AssistantPermissionView[]>`; `PermissionsService.updateAssistantConfig(changes: { permission: string; granted: boolean }[], actorId: string): Promise<AssistantPermissionView[]>`; `AssistantPermissionView { permission; label; reservedByDefault; granted }`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/permissions/permissions.spec.ts`:

```ts
import {
  ASSISTANT_CONFIGURABLE,
  ASSISTANT_DEFAULT_GRANTED,
  fixedPermissions,
  PERMISSION_LABELS,
  PERMISSIONS,
  RESERVED_BY_DEFAULT,
} from './permissions';

describe('catálogo de permissões', () => {
  it('administrador tem todas as 14 permissões', () => {
    expect(PERMISSIONS).toHaveLength(14);
    expect(fixedPermissions('ADMIN')).toEqual([...PERMISSIONS]);
  });

  it('profissional tem as clínicas, mas não as administrativas', () => {
    const professional = fixedPermissions('PROFISSIONAL');
    expect(professional).toEqual(expect.arrayContaining(['documents.issue', 'assessments.finalize', 'patients.write']));
    expect(professional).not.toEqual(expect.arrayContaining(['users.manage']));
    expect(professional).not.toContain('permissions.manage');
    expect(professional).not.toContain('audit.read');
    expect(professional).not.toContain('import.run');
  });

  it('assistente não tem conjunto fixo', () => {
    expect(fixedPermissions('ASSISTENTE')).toBeNull();
  });

  it('padrão do assistente não inclui ações reservadas nem acesso a documentos', () => {
    for (const permission of RESERVED_BY_DEFAULT) expect(ASSISTANT_DEFAULT_GRANTED).not.toContain(permission);
    expect(ASSISTANT_DEFAULT_GRANTED).not.toContain('documents.read');
    for (const permission of ASSISTANT_DEFAULT_GRANTED) expect(ASSISTANT_CONFIGURABLE).toContain(permission);
  });

  it('toda permissão tem rótulo em português', () => {
    for (const permission of PERMISSIONS) expect(PERMISSION_LABELS[permission]).toMatch(/\S/);
  });
});
```

`apps/api/test/permissions.e2e-spec.ts`:

```ts
import { randomUUID } from 'node:crypto';
import { BadRequestException, INestApplication } from '@nestjs/common';
import { ASSISTANT_DEFAULT_GRANTED } from '../src/permissions/permissions';
import { PermissionsService } from '../src/permissions/permissions.service';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { resetDb } from './helpers/db';

describe('PermissionsService', () => {
  let app: INestApplication;
  let permissions: PermissionsService;
  let prisma: PrismaService;

  beforeAll(async () => {
    app = await createTestApp();
    permissions = app.get(PermissionsService);
    prisma = app.get(PrismaService);
  });

  beforeEach(() => resetDb(prisma));

  afterAll(async () => {
    await app.close();
  });

  it('usa o padrão supervisionado quando nada foi configurado', async () => {
    expect(await permissions.forRole('ASSISTENTE')).toEqual([...ASSISTANT_DEFAULT_GRANTED]);
    const config = await permissions.assistantConfig();
    expect(config.find((c) => c.permission === 'documents.issue')).toEqual({
      permission: 'documents.issue',
      label: 'Emitir documentos clínicos',
      reservedByDefault: true,
      granted: false,
    });
  });

  it('grava mudanças, audita só o que mudou e reflete no perfil', async () => {
    const actorId = randomUUID();
    await permissions.updateAssistantConfig(
      [
        { permission: 'documents.issue', granted: true },
        { permission: 'patients.write', granted: false },
        { permission: 'patients.read', granted: true },
      ],
      actorId,
    );

    const granted = await permissions.forRole('ASSISTENTE');
    expect(granted).toContain('documents.issue');
    expect(granted).not.toContain('patients.write');
    const events = await prisma.auditEvent.findMany({ orderBy: { id: 'asc' } });
    expect(events.map((e) => [e.action, e.entityId, e.actorId])).toEqual([
      ['permission.grant', 'documents.issue', actorId],
      ['permission.revoke', 'patients.write', actorId],
    ]);
  });

  it('recusa permissão não configurável sem gravar nada', async () => {
    await expect(
      permissions.updateAssistantConfig(
        [
          { permission: 'documents.read', granted: true },
          { permission: 'users.manage', granted: true },
        ],
        randomUUID(),
      ),
    ).rejects.toBeInstanceOf(BadRequestException);
    expect(await prisma.assistantPermission.count()).toBe(0);
    expect(await prisma.auditEvent.count()).toBe(0);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- permissions`
Expected: FAIL — `Cannot find module './permissions'` e `'../src/permissions/permissions.service'`.

- [ ] **Step 3: Implementar**

Acrescentar ao `schema.prisma`:

```prisma
model AssistantPermission {
  permission  String   @id
  granted     Boolean
  updatedAt   DateTime @updatedAt @map("updated_at") @db.Timestamptz(3)
  updatedById String?  @map("updated_by_id") @db.Uuid

  @@map("assistant_permissions")
}
```

`apps/api/src/permissions/permissions.ts`:

```ts
import type { Role } from '@prisma/client';

export const PERMISSIONS = [
  'patients.read',
  'patients.write',
  'anamneses.draft',
  'anamneses.finalize',
  'assessments.draft',
  'assessments.finalize',
  'ocr.submit',
  'history.read',
  'documents.read',
  'documents.issue',
  'users.manage',
  'permissions.manage',
  'audit.read',
  'import.run',
] as const;

export type Permission = (typeof PERMISSIONS)[number];

export const PERMISSION_LABELS: Record<Permission, string> = {
  'patients.read': 'Consultar cadastros de pacientes',
  'patients.write': 'Cadastrar e editar pacientes',
  'anamneses.draft': 'Preencher rascunhos de anamnese',
  'anamneses.finalize': 'Finalizar anamneses',
  'assessments.draft': 'Preencher rascunhos de avaliação',
  'assessments.finalize': 'Finalizar avaliações',
  'ocr.submit': 'Enviar e revisar OCR',
  'history.read': 'Consultar histórico',
  'documents.read': 'Acessar documentos emitidos',
  'documents.issue': 'Emitir documentos clínicos',
  'users.manage': 'Gerenciar usuários',
  'permissions.manage': 'Gerenciar permissões',
  'audit.read': 'Consultar auditoria',
  'import.run': 'Importar cadastros da planilha',
};

const CLINICAL: readonly Permission[] = [
  'patients.read',
  'patients.write',
  'anamneses.draft',
  'anamneses.finalize',
  'assessments.draft',
  'assessments.finalize',
  'ocr.submit',
  'history.read',
  'documents.read',
  'documents.issue',
];

/** Finalização e emissão: reservadas ao profissional/administrador; delegáveis ao assistente, desligadas por padrão. */
export const RESERVED_BY_DEFAULT: readonly Permission[] = ['anamneses.finalize', 'assessments.finalize', 'documents.issue'];

export const ASSISTANT_CONFIGURABLE: readonly Permission[] = CLINICAL;

export const ASSISTANT_DEFAULT_GRANTED: readonly Permission[] = [
  'patients.read',
  'patients.write',
  'anamneses.draft',
  'assessments.draft',
  'ocr.submit',
  'history.read',
];

export function isPermission(value: string): value is Permission {
  return (PERMISSIONS as readonly string[]).includes(value);
}

export function fixedPermissions(role: Role): Permission[] | null {
  if (role === 'ADMIN') return [...PERMISSIONS];
  if (role === 'PROFISSIONAL') return [...CLINICAL];
  return null;
}
```

`apps/api/src/permissions/permissions.service.ts`:

```ts
import { BadRequestException, Injectable } from '@nestjs/common';
import type { Role } from '@prisma/client';
import { AuditService } from '../audit/audit.service';
import { PrismaService } from '../prisma/prisma.service';
import {
  ASSISTANT_CONFIGURABLE,
  ASSISTANT_DEFAULT_GRANTED,
  fixedPermissions,
  isPermission,
  Permission,
  PERMISSION_LABELS,
  RESERVED_BY_DEFAULT,
} from './permissions';

export interface AssistantPermissionView {
  permission: Permission;
  label: string;
  reservedByDefault: boolean;
  granted: boolean;
}

@Injectable()
export class PermissionsService {
  constructor(
    private readonly prisma: PrismaService,
    private readonly audit: AuditService,
  ) {}

  async forRole(role: Role): Promise<Permission[]> {
    const fixed = fixedPermissions(role);
    if (fixed) return fixed;
    const config = await this.assistantConfig();
    return config.filter((c) => c.granted).map((c) => c.permission);
  }

  async assistantConfig(): Promise<AssistantPermissionView[]> {
    const rows = await this.prisma.assistantPermission.findMany();
    const stored = new Map(rows.map((r) => [r.permission, r.granted]));
    return ASSISTANT_CONFIGURABLE.map((permission) => ({
      permission,
      label: PERMISSION_LABELS[permission],
      reservedByDefault: RESERVED_BY_DEFAULT.includes(permission),
      granted: stored.get(permission) ?? ASSISTANT_DEFAULT_GRANTED.includes(permission),
    }));
  }

  async updateAssistantConfig(
    changes: { permission: string; granted: boolean }[],
    actorId: string,
  ): Promise<AssistantPermissionView[]> {
    const invalid = changes.filter((c) => !isPermission(c.permission) || !ASSISTANT_CONFIGURABLE.includes(c.permission));
    if (invalid.length > 0) {
      throw new BadRequestException(`Permissão não configurável para o assistente: ${invalid.map((c) => c.permission).join(', ')}.`);
    }
    const before = new Map((await this.assistantConfig()).map((c) => [c.permission as string, c.granted]));
    await this.prisma.$transaction(async (tx) => {
      for (const change of changes) {
        await tx.assistantPermission.upsert({
          where: { permission: change.permission },
          create: { permission: change.permission, granted: change.granted, updatedById: actorId },
          update: { granted: change.granted, updatedById: actorId },
        });
        if (before.get(change.permission) !== change.granted) {
          await this.audit.record(
            {
              actorId,
              action: change.granted ? 'permission.grant' : 'permission.revoke',
              entityType: 'assistant_permission',
              entityId: change.permission,
            },
            tx,
          );
        }
      }
    });
    return this.assistantConfig();
  }
}
```

`apps/api/src/permissions/permissions.module.ts`:

```ts
import { Module } from '@nestjs/common';
import { PermissionsService } from './permissions.service';

@Module({ providers: [PermissionsService], exports: [PermissionsService] })
export class PermissionsModule {}
```

`apps/api/src/app.module.ts` — imports passam a ser `[ConfigModule, PrismaModule, AuditModule, UsersModule, PermissionsModule]`, com `import { PermissionsModule } from './permissions/permissions.module';`.

```powershell
Set-Location apps/api
npx prisma migrate dev --name assistant_permissions
Set-Location ../..
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api`
Expected: PASS — incluindo `permissions.spec` (5) e `permissions.e2e-spec` (3).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(api): catálogo de permissões e configuração do assistente"
```

---

### Task 4: Sessões, login e guarda de acesso global

**Files:**
- Modify: `apps/api/prisma/schema.prisma` (modelo `Session` e relação `User.sessions`)
- Create: `apps/api/src/auth/auth.constants.ts`, `src/auth/auth-context.ts`, `src/auth/require-permissions.decorator.ts`, `src/auth/current-auth.decorator.ts`, `src/auth/sessions.service.ts`, `src/auth/auth.service.ts`, `src/auth/auth.guard.ts`, `src/auth/dto/login.dto.ts`, `src/auth/auth.controller.ts`, `src/auth/auth.module.ts`, `src/security/origin-check.middleware.ts`
- Modify: `apps/api/src/app.setup.ts`, `apps/api/src/app.module.ts`
- Test: `apps/api/test/helpers/auth.ts`, `apps/api/test/sessions.e2e-spec.ts`, `apps/api/test/auth.e2e-spec.ts`

**Interfaces:**
- Consumes: `UsersService.normalizeUsername`, `PasswordService.verify/hash`, `PermissionsService.forRole`, `AuditService.record`, `Public()`.
- Produces: `SESSION_COOKIE = 'psm_sid'`; `AuthContext { sessionId: string; user: User; permissions: Permission[] }` (em `req.auth`); `MeView { id; username; displayName; role; permissions }`; `toMeView(user, permissions): MeView`; `RequirePermissions(...permissions: Permission[])`; `CurrentAuth()` (param decorator → `AuthContext`); `SessionsService.create(userId, now?) → { token; expiresAt }`, `.resolve(token, now?) → ResolvedSession | null`, `.revoke(sessionId)`; rotas `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`; helpers de teste `TEST_PASSWORD`, `createUser(app, role, username?)`, `loginAs(app, role, username?) → { agent; user }`.

- [ ] **Step 1: Escrever os testes**

`apps/api/test/helpers/auth.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import type { Role } from '@prisma/client';
import request from 'supertest';
import { UsersService } from '../../src/users/users.service';
import type { PublicUser } from '../../src/users/users.types';

export const TEST_PASSWORD = 'senha-de-teste-123';
export type Agent = ReturnType<typeof request.agent>;

export function createUser(app: INestApplication, role: Role, username: string = role.toLowerCase()): Promise<PublicUser> {
  return app.get(UsersService).create({ username, displayName: `Pessoa ${username}`, password: TEST_PASSWORD, role }, null);
}

export async function loginAs(
  app: INestApplication,
  role: Role,
  username: string = role.toLowerCase(),
): Promise<{ agent: Agent; user: PublicUser }> {
  const user = await createUser(app, role, username);
  const agent = request.agent(app.getHttpServer());
  const res = await agent.post('/api/auth/login').send({ username, password: TEST_PASSWORD });
  if (res.status !== 200) throw new Error(`Login de teste falhou: ${res.status} ${JSON.stringify(res.body)}`);
  return { agent, user };
}
```

`apps/api/test/sessions.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import { SessionsService } from '../src/auth/sessions.service';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { createUser } from './helpers/auth';
import { resetDb } from './helpers/db';

const t0 = new Date('2026-10-01T12:00:00Z');
const at = (minutes: number) => new Date(t0.getTime() + minutes * 60_000);

describe('SessionsService', () => {
  let app: INestApplication;
  let sessions: SessionsService;
  let prisma: PrismaService;

  beforeAll(async () => {
    app = await createTestApp();
    sessions = app.get(SessionsService);
    prisma = app.get(PrismaService);
  });

  beforeEach(() => resetDb(prisma));

  afterAll(async () => {
    await app.close();
  });

  it('guarda só o hash do token e resolve a sessão', async () => {
    const user = await createUser(app, 'ASSISTENTE');
    const { token, expiresAt } = await sessions.create(user.id, t0);

    const stored = await prisma.session.findFirstOrThrow();
    expect(stored.tokenHash).toHaveLength(64);
    expect(stored.tokenHash).not.toBe(token);
    expect(expiresAt).toEqual(at(12 * 60));
    expect((await sessions.resolve(token, at(1)))?.user.id).toBe(user.id);
    expect(await sessions.resolve('token-inexistente', at(1))).toBeNull();
  });

  it('expira após 120 minutos sem uso e apaga a sessão', async () => {
    const user = await createUser(app, 'ASSISTENTE');
    const { token } = await sessions.create(user.id, t0);

    expect(await sessions.resolve(token, at(121))).toBeNull();
    expect(await prisma.session.count()).toBe(0);
  });

  it('renova a ociosidade com o uso, mas respeita o limite de 12 horas', async () => {
    const user = await createUser(app, 'ASSISTENTE');
    const { token } = await sessions.create(user.id, t0);

    for (let minute = 100; minute <= 700; minute += 100) {
      expect(await sessions.resolve(token, at(minute))).not.toBeNull();
    }
    expect(await sessions.resolve(token, at(721))).toBeNull();
  });

  it('recusa sessão de usuário desativado', async () => {
    const user = await createUser(app, 'ASSISTENTE');
    const { token } = await sessions.create(user.id, t0);
    await prisma.user.update({ where: { id: user.id }, data: { active: false } });

    expect(await sessions.resolve(token, at(1))).toBeNull();
  });
});
```

`apps/api/test/auth.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import request from 'supertest';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { createUser, loginAs, TEST_PASSWORD } from './helpers/auth';
import { resetDb } from './helpers/db';

describe('Autenticação (HTTP)', () => {
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

  const login = (username: string, password: string) =>
    request(app.getHttpServer()).post('/api/auth/login').send({ username, password });

  it('login define cookie httpOnly e devolve o usuário com permissões', async () => {
    await createUser(app, 'ADMIN', 'admin');

    const res = await login(' ADMIN ', TEST_PASSWORD);

    expect(res.status).toBe(200);
    const cookie = String(res.headers['set-cookie']);
    expect(cookie).toContain('psm_sid=');
    expect(cookie).toContain('HttpOnly');
    expect(cookie).toContain('SameSite=Lax');
    expect(cookie).toContain('Path=/');
    expect(res.body).toMatchObject({ username: 'admin', role: 'ADMIN' });
    expect(res.body.permissions).toContain('users.manage');
    expect(res.body).not.toHaveProperty('passwordHash');
    expect(await prisma.auditEvent.count({ where: { action: 'auth.login' } })).toBe(1);
  });

  it('GET /auth/me exige sessão', async () => {
    const res = await request(app.getHttpServer()).get('/api/auth/me');
    expect(res.status).toBe(401);
    expect(res.body.message).toBe('Sessão ausente ou expirada.');
  });

  it('GET /auth/me devolve o usuário da sessão com as permissões do perfil', async () => {
    const { agent, user } = await loginAs(app, 'PROFISSIONAL');

    const res = await agent.get('/api/auth/me');

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({ id: user.id, role: 'PROFISSIONAL' });
    expect(res.body.permissions).toContain('documents.issue');
    expect(res.body.permissions).not.toContain('users.manage');
  });

  it('senha errada e usuário inexistente recebem a mesma resposta e ficam auditados', async () => {
    await createUser(app, 'ADMIN', 'admin');

    const wrong = await login('admin', 'errada-errada');
    const unknown = await login('ninguem', 'errada-errada');

    expect(wrong.status).toBe(401);
    expect(unknown.status).toBe(401);
    expect(wrong.body.message).toBe('Usuário ou senha inválidos.');
    expect(unknown.body.message).toBe(wrong.body.message);
    expect(await prisma.auditEvent.count({ where: { action: 'auth.login_failed' } })).toBe(2);
  });

  it('bloqueia a conta por 15 minutos após 5 senhas erradas', async () => {
    await createUser(app, 'ADMIN', 'admin');
    for (let i = 0; i < 5; i++) expect((await login('admin', 'errada-errada')).status).toBe(401);

    const res = await login('admin', TEST_PASSWORD);

    expect(res.status).toBe(429);
    const user = await prisma.user.findUniqueOrThrow({ where: { username: 'admin' } });
    expect(user.lockedUntil!.getTime()).toBeGreaterThan(Date.now() + 14 * 60_000);
  });

  it('logout encerra a sessão e limpa o cookie', async () => {
    const { agent } = await loginAs(app, 'ASSISTENTE');

    const out = await agent.post('/api/auth/logout');

    expect(out.status).toBe(204);
    expect(String(out.headers['set-cookie'])).toContain('psm_sid=;');
    expect(await prisma.session.count()).toBe(0);
    expect((await agent.get('/api/auth/me')).status).toBe(401);
  });

  it('recusa requisição que muda dados vinda de outra origem', async () => {
    await createUser(app, 'ADMIN', 'admin');

    const evil = await login('admin', TEST_PASSWORD).set('Origin', 'https://evil.example');
    const ok = await login('admin', TEST_PASSWORD).set('Origin', 'http://localhost:5173');

    expect(evil.status).toBe(403);
    expect(evil.body.message).toBe('Origem não permitida.');
    expect(ok.status).toBe(200);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- sessions auth`
Expected: FAIL — `Cannot find module '../src/auth/sessions.service'`; em `auth.e2e-spec`, 404 nas rotas `/api/auth/*`.

- [ ] **Step 3: Implementar**

No `schema.prisma`, acrescentar ao modelo `User` a linha `sessions Session[]` (antes de `@@map`) e o modelo:

```prisma
model Session {
  id                String   @id @default(uuid()) @db.Uuid
  tokenHash         String   @unique @map("token_hash")
  userId            String   @map("user_id") @db.Uuid
  user              User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  createdAt         DateTime @default(now()) @map("created_at") @db.Timestamptz(3)
  lastSeenAt        DateTime @default(now()) @map("last_seen_at") @db.Timestamptz(3)
  idleExpiresAt     DateTime @map("idle_expires_at") @db.Timestamptz(3)
  absoluteExpiresAt DateTime @map("absolute_expires_at") @db.Timestamptz(3)

  @@index([userId])
  @@map("sessions")
}
```

`apps/api/src/auth/auth.constants.ts`:

```ts
export const SESSION_COOKIE = 'psm_sid';
```

`apps/api/src/auth/auth-context.ts`:

```ts
import type { User } from '@prisma/client';
import type { Permission } from '../permissions/permissions';

export interface AuthContext {
  sessionId: string;
  user: User;
  permissions: Permission[];
}

export interface MeView {
  id: string;
  username: string;
  displayName: string;
  role: User['role'];
  permissions: Permission[];
}

export function toMeView(user: User, permissions: Permission[]): MeView {
  return { id: user.id, username: user.username, displayName: user.displayName, role: user.role, permissions };
}

declare global {
  // eslint-disable-next-line @typescript-eslint/no-namespace
  namespace Express {
    interface Request {
      auth?: AuthContext;
    }
  }
}
```

`apps/api/src/auth/require-permissions.decorator.ts`:

```ts
import { SetMetadata } from '@nestjs/common';
import type { Permission } from '../permissions/permissions';

export const REQUIRED_PERMISSIONS = 'requiredPermissions';
export const RequirePermissions = (...permissions: Permission[]) => SetMetadata(REQUIRED_PERMISSIONS, permissions);
```

`apps/api/src/auth/current-auth.decorator.ts`:

```ts
import { createParamDecorator, ExecutionContext, UnauthorizedException } from '@nestjs/common';
import type { Request } from 'express';
import type { AuthContext } from './auth-context';

export const CurrentAuth = createParamDecorator((_data: unknown, ctx: ExecutionContext): AuthContext => {
  const req = ctx.switchToHttp().getRequest<Request>();
  if (!req.auth) throw new UnauthorizedException('Sessão ausente ou expirada.');
  return req.auth;
});
```

`apps/api/src/auth/sessions.service.ts`:

```ts
import { createHash, randomBytes } from 'node:crypto';
import { Inject, Injectable } from '@nestjs/common';
import type { User } from '@prisma/client';
import { APP_CONFIG, AppConfig } from '../config';
import { PrismaService } from '../prisma/prisma.service';

const SLIDE_AFTER_MS = 60_000;

export interface ResolvedSession {
  sessionId: string;
  user: User;
}

@Injectable()
export class SessionsService {
  constructor(
    private readonly prisma: PrismaService,
    @Inject(APP_CONFIG) private readonly config: AppConfig,
  ) {}

  static hashToken(token: string): string {
    return createHash('sha256').update(token).digest('hex');
  }

  async create(userId: string, now: Date = new Date()): Promise<{ token: string; expiresAt: Date }> {
    const token = randomBytes(32).toString('base64url');
    const absoluteExpiresAt = new Date(now.getTime() + this.config.sessionAbsoluteHours * 3_600_000);
    await this.prisma.session.create({
      data: {
        tokenHash: SessionsService.hashToken(token),
        userId,
        createdAt: now,
        lastSeenAt: now,
        idleExpiresAt: this.idleFrom(now, absoluteExpiresAt),
        absoluteExpiresAt,
      },
    });
    return { token, expiresAt: absoluteExpiresAt };
  }

  async resolve(token: string, now: Date = new Date()): Promise<ResolvedSession | null> {
    const session = await this.prisma.session.findUnique({
      where: { tokenHash: SessionsService.hashToken(token) },
      include: { user: true },
    });
    if (!session) return null;
    if (session.idleExpiresAt <= now || session.absoluteExpiresAt <= now || !session.user.active) {
      await this.prisma.session.deleteMany({ where: { id: session.id } });
      return null;
    }
    if (now.getTime() - session.lastSeenAt.getTime() >= SLIDE_AFTER_MS) {
      await this.prisma.session.updateMany({
        where: { id: session.id },
        data: { lastSeenAt: now, idleExpiresAt: this.idleFrom(now, session.absoluteExpiresAt) },
      });
    }
    return { sessionId: session.id, user: session.user };
  }

  async revoke(sessionId: string): Promise<void> {
    await this.prisma.session.deleteMany({ where: { id: sessionId } });
  }

  private idleFrom(now: Date, absolute: Date): Date {
    return new Date(Math.min(now.getTime() + this.config.sessionIdleMinutes * 60_000, absolute.getTime()));
  }
}
```

`apps/api/src/auth/auth.service.ts`:

```ts
import { HttpException, HttpStatus, Injectable, UnauthorizedException } from '@nestjs/common';
import { AuditService } from '../audit/audit.service';
import { PermissionsService } from '../permissions/permissions.service';
import { PrismaService } from '../prisma/prisma.service';
import { PasswordService } from '../users/password.service';
import { UsersService } from '../users/users.service';
import { MeView, toMeView } from './auth-context';
import { SessionsService } from './sessions.service';

const MAX_FAILED_ATTEMPTS = 5;
const LOCK_MINUTES = 15;
const INVALID_CREDENTIALS = 'Usuário ou senha inválidos.';

export interface LoginResult {
  token: string;
  expiresAt: Date;
  me: MeView;
}

@Injectable()
export class AuthService {
  private dummyHash: Promise<string> | null = null;

  constructor(
    private readonly prisma: PrismaService,
    private readonly passwords: PasswordService,
    private readonly sessions: SessionsService,
    private readonly permissions: PermissionsService,
    private readonly audit: AuditService,
  ) {}

  async login(rawUsername: string, password: string, ip: string | null, now: Date = new Date()): Promise<LoginResult> {
    const username = UsersService.normalizeUsername(rawUsername);
    const user = await this.prisma.user.findUnique({ where: { username } });

    if (!user || !user.active) {
      await this.passwords.verify(password, await this.getDummyHash());
      await this.audit.record({
        actorId: null,
        action: 'auth.login_failed',
        data: { username, reason: user ? 'inativo' : 'inexistente' },
        ip,
      });
      throw new UnauthorizedException(INVALID_CREDENTIALS);
    }

    if (user.lockedUntil && user.lockedUntil > now) {
      await this.audit.record({ actorId: user.id, action: 'auth.login_blocked', entityType: 'user', entityId: user.id, ip });
      throw new HttpException(
        { statusCode: 429, message: 'Conta bloqueada por tentativas inválidas. Tente de novo em 15 minutos.' },
        HttpStatus.TOO_MANY_REQUESTS,
      );
    }

    if (!(await this.passwords.verify(password, user.passwordHash))) {
      const failed = user.failedLoginCount + 1;
      const lock = failed >= MAX_FAILED_ATTEMPTS;
      await this.prisma.user.update({
        where: { id: user.id },
        data: {
          failedLoginCount: lock ? 0 : failed,
          lockedUntil: lock ? new Date(now.getTime() + LOCK_MINUTES * 60_000) : null,
        },
      });
      await this.audit.record({
        actorId: user.id,
        action: 'auth.login_failed',
        entityType: 'user',
        entityId: user.id,
        data: { reason: 'senha', locked: lock },
        ip,
      });
      throw new UnauthorizedException(INVALID_CREDENTIALS);
    }

    await this.prisma.user.update({ where: { id: user.id }, data: { failedLoginCount: 0, lockedUntil: null } });
    const { token, expiresAt } = await this.sessions.create(user.id, now);
    await this.audit.record({ actorId: user.id, action: 'auth.login', entityType: 'user', entityId: user.id, ip });
    return { token, expiresAt, me: toMeView(user, await this.permissions.forRole(user.role)) };
  }

  private getDummyHash(): Promise<string> {
    this.dummyHash ??= this.passwords.hash('senha-inexistente-para-tempo-constante');
    return this.dummyHash;
  }
}
```

`apps/api/src/auth/auth.guard.ts`:

```ts
import { CanActivate, ExecutionContext, ForbiddenException, Injectable, UnauthorizedException } from '@nestjs/common';
import { Reflector } from '@nestjs/core';
import type { Request } from 'express';
import type { Permission } from '../permissions/permissions';
import { PermissionsService } from '../permissions/permissions.service';
import { SESSION_COOKIE } from './auth.constants';
import { IS_PUBLIC } from './public.decorator';
import { REQUIRED_PERMISSIONS } from './require-permissions.decorator';
import { SessionsService } from './sessions.service';

@Injectable()
export class AuthGuard implements CanActivate {
  constructor(
    private readonly reflector: Reflector,
    private readonly sessions: SessionsService,
    private readonly permissions: PermissionsService,
  ) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const targets = [context.getHandler(), context.getClass()];
    if (this.reflector.getAllAndOverride<boolean>(IS_PUBLIC, targets)) return true;

    const req = context.switchToHttp().getRequest<Request>();
    const token: unknown = req.cookies?.[SESSION_COOKIE];
    if (typeof token !== 'string' || token === '') throw new UnauthorizedException('Sessão ausente ou expirada.');

    const resolved = await this.sessions.resolve(token);
    if (!resolved) throw new UnauthorizedException('Sessão ausente ou expirada.');

    const permissions = await this.permissions.forRole(resolved.user.role);
    req.auth = { sessionId: resolved.sessionId, user: resolved.user, permissions };

    const required = this.reflector.getAllAndOverride<Permission[] | undefined>(REQUIRED_PERMISSIONS, targets) ?? [];
    if (!required.every((permission) => permissions.includes(permission))) {
      throw new ForbiddenException('Você não tem permissão para esta ação.');
    }
    return true;
  }
}
```

`apps/api/src/auth/dto/login.dto.ts`:

```ts
import { IsString, MaxLength } from 'class-validator';

export class LoginDto {
  @IsString()
  @MaxLength(100)
  username!: string;

  @IsString()
  @MaxLength(200)
  password!: string;
}
```

`apps/api/src/auth/auth.controller.ts`:

```ts
import { Body, Controller, Get, HttpCode, Inject, Post, Req, Res } from '@nestjs/common';
import type { Request, Response } from 'express';
import { AuditService } from '../audit/audit.service';
import { APP_CONFIG, AppConfig } from '../config';
import { AuthContext, MeView, toMeView } from './auth-context';
import { SESSION_COOKIE } from './auth.constants';
import { AuthService } from './auth.service';
import { CurrentAuth } from './current-auth.decorator';
import { LoginDto } from './dto/login.dto';
import { Public } from './public.decorator';
import { SessionsService } from './sessions.service';

@Controller('auth')
export class AuthController {
  constructor(
    private readonly auth: AuthService,
    private readonly sessions: SessionsService,
    private readonly audit: AuditService,
    @Inject(APP_CONFIG) private readonly config: AppConfig,
  ) {}

  @Public()
  @Post('login')
  @HttpCode(200)
  async login(@Body() body: LoginDto, @Req() req: Request, @Res({ passthrough: true }) res: Response): Promise<MeView> {
    const result = await this.auth.login(body.username, body.password, req.ip ?? null);
    res.cookie(SESSION_COOKIE, result.token, {
      httpOnly: true,
      sameSite: 'lax',
      secure: this.config.cookieSecure,
      path: '/',
      expires: result.expiresAt,
    });
    return result.me;
  }

  @Post('logout')
  @HttpCode(204)
  async logout(
    @CurrentAuth() auth: AuthContext,
    @Req() req: Request,
    @Res({ passthrough: true }) res: Response,
  ): Promise<void> {
    await this.sessions.revoke(auth.sessionId);
    await this.audit.record({ actorId: auth.user.id, action: 'auth.logout', entityType: 'user', entityId: auth.user.id, ip: req.ip ?? null });
    res.clearCookie(SESSION_COOKIE, { path: '/', httpOnly: true, sameSite: 'lax', secure: this.config.cookieSecure });
  }

  @Get('me')
  me(@CurrentAuth() auth: AuthContext): MeView {
    return toMeView(auth.user, auth.permissions);
  }
}
```

`apps/api/src/auth/auth.module.ts`:

```ts
import { Module } from '@nestjs/common';
import { APP_GUARD } from '@nestjs/core';
import { PermissionsModule } from '../permissions/permissions.module';
import { UsersModule } from '../users/users.module';
import { AuthController } from './auth.controller';
import { AuthGuard } from './auth.guard';
import { AuthService } from './auth.service';
import { SessionsService } from './sessions.service';

@Module({
  imports: [UsersModule, PermissionsModule],
  controllers: [AuthController],
  providers: [SessionsService, AuthService, { provide: APP_GUARD, useClass: AuthGuard }],
  exports: [SessionsService],
})
export class AuthModule {}
```

`apps/api/src/security/origin-check.middleware.ts`:

```ts
import type { NextFunction, Request, Response } from 'express';

const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS']);

/** Complementa o SameSite=Lax: requisições que mudam dados só são aceitas da origem da SPA. */
export function originCheck(allowedOrigin: string) {
  return (req: Request, res: Response, next: NextFunction): void => {
    const origin = req.headers.origin;
    if (!SAFE_METHODS.has(req.method) && origin !== undefined && origin !== allowedOrigin) {
      res.status(403).json({ statusCode: 403, message: 'Origem não permitida.' });
      return;
    }
    next();
  };
}
```

`apps/api/src/app.setup.ts` (substituir o conteúdo):

```ts
import { INestApplication, ValidationPipe } from '@nestjs/common';
import cookieParser from 'cookie-parser';
import { APP_CONFIG, AppConfig } from './config';
import { originCheck } from './security/origin-check.middleware';

export function configureApp(app: INestApplication): void {
  const config = app.get<AppConfig>(APP_CONFIG);
  app.setGlobalPrefix('api');
  app.use(cookieParser());
  app.use(originCheck(config.appOrigin));
  app.useGlobalPipes(new ValidationPipe({ whitelist: true, forbidNonWhitelisted: true, transform: true }));
}
```

`apps/api/src/app.module.ts` — imports passam a ser `[ConfigModule, PrismaModule, AuditModule, UsersModule, PermissionsModule, AuthModule]`, com `import { AuthModule } from './auth/auth.module';`.

```powershell
Set-Location apps/api
npx prisma migrate dev --name sessions
Set-Location ../..
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api`
Expected: PASS — incluindo `sessions.e2e-spec` (4) e `auth.e2e-spec` (7). O `health` continua 200 por ser `@Public()`.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(api): sessão no servidor, login com bloqueio e guarda global de permissões"
```

---

### Task 5: Endpoints de administração — usuários, permissões do assistente e auditoria

**Files:**
- Modify: `apps/api/src/users/users.types.ts` (`UpdateUserInput`), `src/users/users.service.ts` (`update`, `resetPassword`), `src/users/users.module.ts` (controller)
- Create: `apps/api/src/users/dto/user.dto.ts`, `src/users/users.controller.ts`
- Create: `apps/api/src/permissions/dto/update-assistant-permissions.dto.ts`, `src/permissions/permissions.controller.ts`
- Modify: `apps/api/src/permissions/permissions.module.ts` (controller)
- Create: `apps/api/src/audit/dto/audit-query.dto.ts`, `src/audit/audit.controller.ts`
- Modify: `apps/api/src/audit/audit.module.ts` (controller)
- Test: `apps/api/test/admin.e2e-spec.ts`

**Interfaces:**
- Consumes: `RequirePermissions`, `CurrentAuth`, `AuthContext`, `diffFields`, `PermissionsService.assistantConfig/updateAssistantConfig`, `AuditService.list`.
- Produces: `UsersService.update(id, input: UpdateUserInput, actorId): Promise<PublicUser>`; `UsersService.resetPassword(id, password, actorId): Promise<void>`; rotas `GET/POST /api/users`, `PATCH /api/users/:id`, `POST /api/users/:id/password` (204), `GET/PUT /api/permissions/assistant`, `GET /api/audit?entityType&entityId&limit` → `AuditEventView[] { id; occurredAt; actorId; actorName; action; entityType; entityId; data }`.

- [ ] **Step 1: Escrever os testes**

`apps/api/test/admin.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import request from 'supertest';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';

describe('Administração (HTTP)', () => {
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

  it('só quem gerencia usuários consegue listá-los', async () => {
    const admin = await loginAs(app, 'ADMIN');
    const professional = await loginAs(app, 'PROFISSIONAL');
    const assistant = await loginAs(app, 'ASSISTENTE');

    expect((await assistant.agent.get('/api/users')).status).toBe(403);
    expect((await professional.agent.get('/api/users')).status).toBe(403);
    const res = await admin.agent.get('/api/users');
    expect(res.status).toBe(200);
    expect(res.body.map((u: { username: string }) => u.username)).toEqual(['admin', 'assistente', 'profissional']);
  });

  it('administrador cria usuário e a criação fica auditada com o autor', async () => {
    const { agent, user: admin } = await loginAs(app, 'ADMIN');

    const res = await agent
      .post('/api/users')
      .send({ username: 'nova', displayName: 'Nova Pessoa', password: 'senha-segura-123', role: 'ASSISTENTE' });

    expect(res.status).toBe(201);
    expect(res.body).toMatchObject({ username: 'nova', role: 'ASSISTENTE', active: true });
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'user.create', entityId: res.body.id } });
    expect(event.actorId).toBe(admin.id);
  });

  it('valida o corpo da criação de usuário', async () => {
    const { agent } = await loginAs(app, 'ADMIN');
    const res = await agent.post('/api/users').send({ username: 'xy', displayName: 'X', password: 'curta', role: 'CHEFE' });
    expect(res.status).toBe(400);
  });

  it('não permite desativar nem rebaixar o último administrador', async () => {
    const { agent, user } = await loginAs(app, 'ADMIN');
    expect((await agent.patch(`/api/users/${user.id}`).send({ active: false })).status).toBe(409);
    expect((await agent.patch(`/api/users/${user.id}`).send({ role: 'PROFISSIONAL' })).status).toBe(409);
  });

  it('desativar usuário derruba a sessão aberta dele e audita a mudança', async () => {
    const admin = await loginAs(app, 'ADMIN');
    const assistant = await loginAs(app, 'ASSISTENTE');
    expect((await assistant.agent.get('/api/auth/me')).status).toBe(200);

    const res = await admin.agent.patch(`/api/users/${assistant.user.id}`).send({ active: false });

    expect(res.status).toBe(200);
    expect(res.body.active).toBe(false);
    expect((await assistant.agent.get('/api/auth/me')).status).toBe(401);
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'user.update', entityId: assistant.user.id } });
    expect(event.data).toEqual({ changes: { active: { from: true, to: false } } });
  });

  it('redefinir senha derruba as sessões e a nova senha passa a valer', async () => {
    const admin = await loginAs(app, 'ADMIN');
    const professional = await loginAs(app, 'PROFISSIONAL');

    const reset = await admin.agent.post(`/api/users/${professional.user.id}/password`).send({ password: 'nova-senha-segura-1' });

    expect(reset.status).toBe(204);
    expect((await professional.agent.get('/api/auth/me')).status).toBe(401);
    const login = await request(app.getHttpServer())
      .post('/api/auth/login')
      .send({ username: 'profissional', password: 'nova-senha-segura-1' });
    expect(login.status).toBe(200);
  });

  it('administrador ajusta as permissões do assistente com efeito imediato', async () => {
    const admin = await loginAs(app, 'ADMIN');
    const assistant = await loginAs(app, 'ASSISTENTE');
    expect((await assistant.agent.get('/api/auth/me')).body.permissions).not.toContain('documents.issue');

    const put = await admin.agent
      .put('/api/permissions/assistant')
      .send({ changes: [{ permission: 'documents.issue', granted: true }] });

    expect(put.status).toBe(200);
    expect(put.body.find((p: { permission: string }) => p.permission === 'documents.issue').granted).toBe(true);
    expect((await assistant.agent.get('/api/auth/me')).body.permissions).toContain('documents.issue');
  });

  it('profissional não mexe em permissões; permissão administrativa é recusada', async () => {
    const professional = await loginAs(app, 'PROFISSIONAL');
    expect((await professional.agent.get('/api/permissions/assistant')).status).toBe(403);

    const admin = await loginAs(app, 'ADMIN');
    const res = await admin.agent.put('/api/permissions/assistant').send({ changes: [{ permission: 'users.manage', granted: true }] });
    expect(res.status).toBe(400);
  });

  it('auditoria lista eventos com o nome do autor e exige permissão', async () => {
    const admin = await loginAs(app, 'ADMIN');
    await admin.agent.put('/api/permissions/assistant').send({ changes: [{ permission: 'documents.read', granted: true }] });

    const res = await admin.agent.get('/api/audit?entityType=assistant_permission');

    expect(res.status).toBe(200);
    expect(res.body[0]).toMatchObject({ action: 'permission.grant', entityId: 'documents.read', actorName: 'Pessoa admin' });
    const assistant = await loginAs(app, 'ASSISTENTE');
    expect((await assistant.agent.get('/api/audit')).status).toBe(403);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- admin`
Expected: FAIL — 404 em `/api/users`, `/api/permissions/assistant` e `/api/audit`.

- [ ] **Step 3: Implementar**

Acrescentar a `apps/api/src/users/users.types.ts`:

```ts
export interface UpdateUserInput {
  displayName?: string;
  role?: Role;
  active?: boolean;
}
```

Acrescentar à classe `UsersService` (e os imports `NotFoundException` de `@nestjs/common`, `diffFields` de `'../audit/diff'` e `UpdateUserInput` de `'./users.types'`):

```ts
  async update(id: string, input: UpdateUserInput, actorId: string): Promise<PublicUser> {
    const before = await this.prisma.user.findUnique({ where: { id } });
    if (!before) throw new NotFoundException('Usuário não encontrado.');
    const displayName = input.displayName === undefined ? before.displayName : input.displayName.trim();
    if (displayName === '') throw new BadRequestException('Informe o nome de exibição.');
    const next = { displayName, role: input.role ?? before.role, active: input.active ?? before.active };

    const losesAdmin = before.role === 'ADMIN' && before.active && (next.role !== 'ADMIN' || !next.active);
    if (losesAdmin && (await this.prisma.user.count({ where: { role: 'ADMIN', active: true } })) <= 1) {
      throw new ConflictException('Deve existir ao menos um administrador ativo.');
    }

    const changes = diffFields(before, { ...before, ...next }, ['displayName', 'role', 'active']);
    const updated = await this.prisma.$transaction(async (tx) => {
      const user = await tx.user.update({ where: { id }, data: next });
      if (!next.active) await tx.session.deleteMany({ where: { userId: id } });
      if (Object.keys(changes).length > 0) {
        await this.audit.record({ actorId, action: 'user.update', entityType: 'user', entityId: id, data: { changes } }, tx);
      }
      return user;
    });
    return toPublicUser(updated);
  }

  async resetPassword(id: string, password: string, actorId: string): Promise<void> {
    assertPasswordPolicy(password);
    const user = await this.prisma.user.findUnique({ where: { id } });
    if (!user) throw new NotFoundException('Usuário não encontrado.');
    const passwordHash = await this.passwords.hash(password);
    await this.prisma.$transaction(async (tx) => {
      await tx.user.update({ where: { id }, data: { passwordHash, failedLoginCount: 0, lockedUntil: null } });
      await tx.session.deleteMany({ where: { userId: id } });
      await this.audit.record({ actorId, action: 'user.password_reset', entityType: 'user', entityId: id }, tx);
    });
  }
```

`apps/api/src/users/dto/user.dto.ts`:

```ts
import { IsBoolean, IsIn, IsOptional, IsString, MaxLength } from 'class-validator';

const ROLES = ['ADMIN', 'PROFISSIONAL', 'ASSISTENTE'] as const;
type RoleValue = (typeof ROLES)[number];

export class CreateUserDto {
  @IsString()
  @MaxLength(40)
  username!: string;

  @IsString()
  @MaxLength(120)
  displayName!: string;

  @IsString()
  @MaxLength(200)
  password!: string;

  @IsIn(ROLES)
  role!: RoleValue;
}

export class UpdateUserDto {
  @IsOptional()
  @IsString()
  @MaxLength(120)
  displayName?: string;

  @IsOptional()
  @IsIn(ROLES)
  role?: RoleValue;

  @IsOptional()
  @IsBoolean()
  active?: boolean;
}

export class ResetPasswordDto {
  @IsString()
  @MaxLength(200)
  password!: string;
}
```

`apps/api/src/users/users.controller.ts`:

```ts
import { Body, Controller, Get, HttpCode, Param, ParseUUIDPipe, Patch, Post } from '@nestjs/common';
import type { AuthContext } from '../auth/auth-context';
import { CurrentAuth } from '../auth/current-auth.decorator';
import { RequirePermissions } from '../auth/require-permissions.decorator';
import { CreateUserDto, ResetPasswordDto, UpdateUserDto } from './dto/user.dto';
import { UsersService } from './users.service';
import type { PublicUser } from './users.types';

@Controller('users')
@RequirePermissions('users.manage')
export class UsersController {
  constructor(private readonly users: UsersService) {}

  @Get()
  list(): Promise<PublicUser[]> {
    return this.users.list();
  }

  @Post()
  create(@Body() body: CreateUserDto, @CurrentAuth() auth: AuthContext): Promise<PublicUser> {
    return this.users.create(body, auth.user.id);
  }

  @Patch(':id')
  update(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Body() body: UpdateUserDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<PublicUser> {
    return this.users.update(id, body, auth.user.id);
  }

  @Post(':id/password')
  @HttpCode(204)
  async resetPassword(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Body() body: ResetPasswordDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<void> {
    await this.users.resetPassword(id, body.password, auth.user.id);
  }
}
```

`apps/api/src/users/users.module.ts`:

```ts
import { Module } from '@nestjs/common';
import { PasswordService } from './password.service';
import { UsersController } from './users.controller';
import { UsersService } from './users.service';

@Module({
  controllers: [UsersController],
  providers: [PasswordService, UsersService],
  exports: [PasswordService, UsersService],
})
export class UsersModule {}
```

`apps/api/src/permissions/dto/update-assistant-permissions.dto.ts`:

```ts
import { Type } from 'class-transformer';
import { ArrayMaxSize, IsArray, IsBoolean, IsString, ValidateNested } from 'class-validator';

export class PermissionChangeDto {
  @IsString()
  permission!: string;

  @IsBoolean()
  granted!: boolean;
}

export class UpdateAssistantPermissionsDto {
  @IsArray()
  @ArrayMaxSize(50)
  @ValidateNested({ each: true })
  @Type(() => PermissionChangeDto)
  changes!: PermissionChangeDto[];
}
```

`apps/api/src/permissions/permissions.controller.ts`:

```ts
import { Body, Controller, Get, Put } from '@nestjs/common';
import type { AuthContext } from '../auth/auth-context';
import { CurrentAuth } from '../auth/current-auth.decorator';
import { RequirePermissions } from '../auth/require-permissions.decorator';
import { UpdateAssistantPermissionsDto } from './dto/update-assistant-permissions.dto';
import { AssistantPermissionView, PermissionsService } from './permissions.service';

@Controller('permissions')
@RequirePermissions('permissions.manage')
export class PermissionsController {
  constructor(private readonly permissions: PermissionsService) {}

  @Get('assistant')
  get(): Promise<AssistantPermissionView[]> {
    return this.permissions.assistantConfig();
  }

  @Put('assistant')
  update(@Body() body: UpdateAssistantPermissionsDto, @CurrentAuth() auth: AuthContext): Promise<AssistantPermissionView[]> {
    return this.permissions.updateAssistantConfig(body.changes, auth.user.id);
  }
}
```

`apps/api/src/permissions/permissions.module.ts`:

```ts
import { Module } from '@nestjs/common';
import { PermissionsController } from './permissions.controller';
import { PermissionsService } from './permissions.service';

@Module({ controllers: [PermissionsController], providers: [PermissionsService], exports: [PermissionsService] })
export class PermissionsModule {}
```

`apps/api/src/audit/dto/audit-query.dto.ts`:

```ts
import { Type } from 'class-transformer';
import { IsInt, IsOptional, IsString, Max, MaxLength, Min } from 'class-validator';

export class AuditQueryDto {
  @IsOptional()
  @IsString()
  @MaxLength(60)
  entityType?: string;

  @IsOptional()
  @IsString()
  @MaxLength(100)
  entityId?: string;

  @IsOptional()
  @Type(() => Number)
  @IsInt()
  @Min(1)
  @Max(500)
  limit?: number;
}
```

`apps/api/src/audit/audit.controller.ts`:

```ts
import { Controller, Get, Query } from '@nestjs/common';
import type { Prisma } from '@prisma/client';
import { RequirePermissions } from '../auth/require-permissions.decorator';
import { PrismaService } from '../prisma/prisma.service';
import { AuditService } from './audit.service';
import { AuditQueryDto } from './dto/audit-query.dto';

export interface AuditEventView {
  id: number;
  occurredAt: string;
  actorId: string | null;
  actorName: string | null;
  action: string;
  entityType: string | null;
  entityId: string | null;
  data: Prisma.JsonValue | null;
}

@Controller('audit')
@RequirePermissions('audit.read')
export class AuditController {
  constructor(
    private readonly audit: AuditService,
    private readonly prisma: PrismaService,
  ) {}

  @Get()
  async list(@Query() query: AuditQueryDto): Promise<AuditEventView[]> {
    const events = await this.audit.list(query);
    const actorIds = [...new Set(events.map((e) => e.actorId).filter((id): id is string => id !== null))];
    const actors = await this.prisma.user.findMany({ where: { id: { in: actorIds } }, select: { id: true, displayName: true } });
    const names = new Map(actors.map((a) => [a.id, a.displayName]));
    return events.map((e) => ({
      id: e.id,
      occurredAt: e.occurredAt.toISOString(),
      actorId: e.actorId,
      actorName: e.actorId ? (names.get(e.actorId) ?? null) : null,
      action: e.action,
      entityType: e.entityType,
      entityId: e.entityId,
      data: e.data,
    }));
  }
}
```

`apps/api/src/audit/audit.module.ts`:

```ts
import { Global, Module } from '@nestjs/common';
import { AuditController } from './audit.controller';
import { AuditService } from './audit.service';

@Global()
@Module({ controllers: [AuditController], providers: [AuditService], exports: [AuditService] })
export class AuditModule {}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api`
Expected: PASS — incluindo `admin.e2e-spec` (9).

Run: `npm run typecheck -w @psm/api`
Expected: sem erros.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(api): administração de usuários, permissões do assistente e consulta de auditoria"
```

---

### Task 6: Pacientes — normalização, API e detecção de edição concorrente

**Files:**
- Modify: `apps/api/prisma/schema.prisma` (enum `Sex`, modelo `Patient`)
- Create: `apps/api/src/common/text.ts`, `src/common/civil-date.ts`, `src/patients/patient-normalize.ts`, `src/patients/patient.view.ts`, `src/patients/dto/patient.dto.ts`, `src/patients/patients.service.ts`, `src/patients/patients.controller.ts`, `src/patients/patients.module.ts`
- Modify: `apps/api/src/app.module.ts`
- Test: `apps/api/src/common/text.spec.ts`, `src/common/civil-date.spec.ts`, `src/patients/patient-normalize.spec.ts`, `apps/api/test/patients.e2e-spec.ts`

**Interfaces:**
- Consumes: `RequirePermissions`, `CurrentAuth`, `AuditService.record`, `diffFields`, `APP_CONFIG.clinicTimeZone`.
- Produces: `cleanText(v: unknown): string | null`; `cleanMultiline(v: unknown): string | null`; `digitsOnly(v: string | null): string | null`; `normalizeForSearch(v: string): string`; `UFS: readonly string[]`; `parseIsoDate(s: string): Date | null`; `formatIsoDate(d: Date): string`; `todayInTimeZone(tz: string, now?: Date): string`; `normalizePatient(input: PatientInput, today: string): { data: PatientData | null; errors: FieldErrors }`; `PatientView`, `PatientListItem`, `toPatientView`, `toPatientListItem`; rotas `GET /api/patients?q&page&pageSize` → `{ items; total; page; pageSize }`, `GET /api/patients/:id`, `POST /api/patients`, `PUT /api/patients/:id` (com `version`; 409 → `{ message; current: PatientView }`; 400 → `{ message; errors: Record<campo, mensagem> }`).

- [ ] **Step 1: Escrever os testes unitários**

`apps/api/src/common/text.spec.ts`:

```ts
import { cleanMultiline, cleanText, digitsOnly, normalizeForSearch, UFS } from './text';

describe('texto', () => {
  it('cleanText colapsa espaços e transforma vazio em null', () => {
    expect(cleanText('  João   da  Silva ')).toBe('João da Silva');
    expect(cleanText('   ')).toBeNull();
    expect(cleanText(undefined)).toBeNull();
    expect(cleanText(123)).toBe('123');
  });

  it('cleanMultiline preserva quebras de linha', () => {
    expect(cleanMultiline(' linha 1\r\nlinha 2 ')).toBe('linha 1\nlinha 2');
    expect(cleanMultiline('\n \n')).toBeNull();
  });

  it('digitsOnly remove máscara', () => {
    expect(digitsOnly('123.456.789-01')).toBe('12345678901');
    expect(digitsOnly('abc')).toBeNull();
    expect(digitsOnly(null)).toBeNull();
  });

  it('normalizeForSearch ignora acento, caixa e espaços extras', () => {
    expect(normalizeForSearch('  JOÃO   da Silva ')).toBe('joao da silva');
    expect(normalizeForSearch('Célia Conceição')).toBe('celia conceicao');
  });

  it('lista as 27 UFs', () => {
    expect(UFS).toHaveLength(27);
    expect(UFS).toContain('RR');
    expect(UFS).toContain('DF');
  });
});
```

`apps/api/src/common/civil-date.spec.ts`:

```ts
import { formatIsoDate, parseIsoDate, todayInTimeZone } from './civil-date';

describe('datas civis', () => {
  it('aceita só datas ISO existentes', () => {
    expect(formatIsoDate(parseIsoDate('2024-02-29')!)).toBe('2024-02-29');
    expect(parseIsoDate('2023-02-29')).toBeNull();
    expect(parseIsoDate('10/05/1980')).toBeNull();
    expect(parseIsoDate('')).toBeNull();
  });

  it('calcula "hoje" no fuso da clínica', () => {
    expect(todayInTimeZone('America/Sao_Paulo', new Date('2026-10-01T02:30:00Z'))).toBe('2026-09-30');
    expect(todayInTimeZone('America/Sao_Paulo', new Date('2026-10-01T03:30:00Z'))).toBe('2026-10-01');
  });
});
```

`apps/api/src/patients/patient-normalize.spec.ts`:

```ts
import { normalizePatient } from './patient-normalize';

const TODAY = '2026-10-01';
const valid = { fullName: '  João   da Silva ', sex: 'MASCULINO', birthDate: '1980-05-10' };

describe('normalizePatient', () => {
  it('normaliza texto, documentos, UF e nome de busca', () => {
    const { data, errors } = normalizePatient(
      { ...valid, cpf: '123.456.789-01', postalCode: '01310-100', state: 'sp', email: ' ana@exemplo.com ', notes: ' a\nb ', phone: '' },
      TODAY,
    );

    expect(errors).toEqual({});
    expect(data).toMatchObject({
      fullName: 'João da Silva',
      searchName: 'joao da silva',
      sex: 'MASCULINO',
      cpf: '12345678901',
      postalCode: '01310100',
      state: 'SP',
      email: 'ana@exemplo.com',
      notes: 'a\nb',
      phone: null,
      rg: null,
    });
    expect(data!.birthDate.toISOString()).toBe('1980-05-10T00:00:00.000Z');
  });

  it('aponta erro por campo', () => {
    const { data, errors } = normalizePatient(
      { fullName: ' ', sex: 'X', birthDate: '2030-01-01', cpf: '123', postalCode: '123', state: 'XX', email: 'sem-arroba' },
      TODAY,
    );

    expect(data).toBeNull();
    expect(errors).toEqual({
      fullName: 'Informe o nome.',
      sex: 'Selecione o sexo.',
      birthDate: 'A data de nascimento não pode estar no futuro.',
      cpf: 'O CPF deve ter 11 dígitos.',
      postalCode: 'O CEP deve ter 8 dígitos.',
      state: 'UF inválida.',
      email: 'E-mail inválido.',
    });
  });

  it('recusa data inexistente e anterior a 1900', () => {
    expect(normalizePatient({ ...valid, birthDate: '1980-02-31' }, TODAY).errors.birthDate).toBe('Data de nascimento inválida.');
    expect(normalizePatient({ ...valid, birthDate: '1899-12-31' }, TODAY).errors.birthDate).toBe(
      'A data de nascimento deve ser a partir de 1900.',
    );
  });
});
```

- [ ] **Step 2: Escrever o teste de API**

`apps/api/test/patients.e2e-spec.ts`:

```ts
import { randomUUID } from 'node:crypto';
import { INestApplication } from '@nestjs/common';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';

const joao = { fullName: 'João  da Silva', sex: 'MASCULINO', birthDate: '1980-05-10', cpf: '123.456.789-01' };

describe('Pacientes (HTTP)', () => {
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

  async function create(agent: Agent, body: Record<string, unknown> = joao) {
    const res = await agent.post('/api/patients').send(body);
    expect(res.status).toBe(201);
    return res.body as { id: string; version: number; fullName: string };
  }

  it('profissional cadastra paciente com dados normalizados e auditoria', async () => {
    const { agent, user } = await loginAs(app, 'PROFISSIONAL');

    const patient = await create(agent);

    expect(patient).toMatchObject({ fullName: 'João da Silva', cpf: '12345678901', birthDate: '1980-05-10', version: 1, legacyId: null });
    expect(patient).not.toHaveProperty('searchName');
    const event = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'patient.create' } });
    expect(event).toMatchObject({ actorId: user.id, entityId: patient.id, data: { source: 'manual' } });
  });

  it('devolve erros por campo em português', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');

    const res = await agent.post('/api/patients').send({ ...joao, cpf: '123' });

    expect(res.status).toBe(400);
    expect(res.body).toEqual({ message: 'Dados inválidos.', errors: { cpf: 'O CPF deve ter 11 dígitos.' } });
  });

  it('busca sem acento, sem caixa e com espaços extras, por CPF e por código da planilha', async () => {
    const { agent, user } = await loginAs(app, 'PROFISSIONAL');
    await create(agent);
    await create(agent, { fullName: 'Maria Souza', sex: 'FEMININO', birthDate: '1990-01-01' });
    await prisma.patient.create({
      data: {
        legacyId: 42,
        fullName: 'Célia Legado',
        searchName: 'celia legado',
        sex: 'FEMININO',
        birthDate: new Date('1970-01-01T00:00:00Z'),
        createdById: user.id,
        updatedById: user.id,
      },
    });

    const byName = await agent.get('/api/patients').query({ q: 'joao  SILVA' });
    const byCpf = await agent.get('/api/patients').query({ q: '456.789' });
    const byLegacy = await agent.get('/api/patients').query({ q: '42' });
    const all = await agent.get('/api/patients').query({ pageSize: 2, page: 2 });

    expect(byName.body.items.map((p: { fullName: string }) => p.fullName)).toEqual(['João da Silva']);
    expect(byCpf.body.items.map((p: { fullName: string }) => p.fullName)).toEqual(['João da Silva']);
    expect(byLegacy.body.items.map((p: { fullName: string }) => p.fullName)).toEqual(['Célia Legado']);
    expect(all.body).toMatchObject({ total: 3, page: 2, pageSize: 2 });
    expect(all.body.items.map((p: { fullName: string }) => p.fullName)).toEqual(['Maria Souza']);
  });

  it('atualiza com a versão atual, audita o que mudou e recusa versão antiga com 409', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const patient = await create(agent);

    const first = await agent.put(`/api/patients/${patient.id}`).send({ ...joao, fullName: 'João Silva', version: 1 });
    const stale = await agent.put(`/api/patients/${patient.id}`).send({ ...joao, fullName: 'Outro Nome', version: 1 });

    expect(first.status).toBe(200);
    expect(first.body).toMatchObject({ fullName: 'João Silva', version: 2 });
    expect(stale.status).toBe(409);
    expect(stale.body.message).toBe('Este cadastro foi alterado por outra pessoa. Recarregue para ver a versão atual.');
    expect(stale.body.current).toMatchObject({ fullName: 'João Silva', version: 2 });
    const update = await prisma.auditEvent.findFirstOrThrow({ where: { action: 'patient.update' } });
    expect(update.data).toEqual({ changes: { fullName: { from: 'João da Silva', to: 'João Silva' } } });
  });

  it('responde 404 para paciente inexistente e 400 para id inválido', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    expect((await agent.get(`/api/patients/${randomUUID()}`)).status).toBe(404);
    expect((await agent.get('/api/patients/abc')).status).toBe(400);
  });

  it('respeita as permissões do assistente configuradas pelo administrador', async () => {
    const assistant = await loginAs(app, 'ASSISTENTE');
    await create(assistant.agent);
    const admin = await loginAs(app, 'ADMIN');
    await admin.agent.put('/api/permissions/assistant').send({ changes: [{ permission: 'patients.write', granted: false }] });

    expect((await assistant.agent.post('/api/patients').send(joao)).status).toBe(403);
    expect((await assistant.agent.get('/api/patients')).status).toBe(200);
  });
});
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `npm test -w @psm/api -- text civil-date patient`
Expected: FAIL — `Cannot find module './text'`, `'./civil-date'`, `'./patient-normalize'`; `patients.e2e-spec` com 404.

- [ ] **Step 4: Implementar**

Acrescentar ao `schema.prisma`:

```prisma
enum Sex {
  FEMININO
  MASCULINO
}

model Patient {
  id                String   @id @default(uuid()) @db.Uuid
  legacyId          Int?     @unique @map("legacy_id")
  fullName          String   @map("full_name")
  searchName        String   @map("search_name")
  sex               Sex
  birthDate         DateTime @map("birth_date") @db.Date
  profession        String?
  phone             String?
  mobile            String?
  email             String?
  notes             String?
  postalCode        String?  @map("postal_code")
  street            String?
  streetNumber      String?  @map("street_number")
  addressComplement String?  @map("address_complement")
  district          String?
  city              String?
  state             String?
  rg                String?
  cpf               String?
  otherDocument     String?  @map("other_document")
  version           Int      @default(1)
  createdAt         DateTime @default(now()) @map("created_at") @db.Timestamptz(3)
  updatedAt         DateTime @updatedAt @map("updated_at") @db.Timestamptz(3)
  createdById       String?  @map("created_by_id") @db.Uuid
  updatedById       String?  @map("updated_by_id") @db.Uuid

  @@index([searchName])
  @@index([cpf])
  @@map("patients")
}
```

`apps/api/src/common/text.ts`:

```ts
export function cleanText(value: unknown): string | null {
  if (value === null || value === undefined) return null;
  const text = String(value).replace(/\s+/g, ' ').trim();
  return text === '' ? null : text;
}

export function cleanMultiline(value: unknown): string | null {
  if (value === null || value === undefined) return null;
  const text = String(value).replace(/\r\n?/g, '\n').trim();
  return text === '' ? null : text;
}

export function digitsOnly(value: string | null): string | null {
  if (value === null) return null;
  const digits = value.replace(/\D/g, '');
  return digits === '' ? null : digits;
}

export function normalizeForSearch(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/\s+/g, ' ')
    .trim();
}

export const UFS: readonly string[] = [
  'AC', 'AL', 'AP', 'AM', 'BA', 'CE', 'DF', 'ES', 'GO', 'MA', 'MT', 'MS', 'MG', 'PA',
  'PB', 'PR', 'PE', 'PI', 'RJ', 'RN', 'RS', 'RO', 'RR', 'SC', 'SP', 'SE', 'TO',
];
```

`apps/api/src/common/civil-date.ts`:

```ts
export function parseIsoDate(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return null;
  const [year, month, day] = [Number(match[1]), Number(match[2]), Number(match[3])];
  const date = new Date(Date.UTC(year, month - 1, day));
  const exists = date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day;
  return exists ? date : null;
}

export function formatIsoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function todayInTimeZone(timeZone: string, now: Date = new Date()): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone, year: 'numeric', month: '2-digit', day: '2-digit' }).format(now);
}
```

`apps/api/src/patients/patient-normalize.ts`:

```ts
import type { Sex } from '@prisma/client';
import { formatIsoDate, parseIsoDate } from '../common/civil-date';
import { cleanMultiline, cleanText, digitsOnly, normalizeForSearch, UFS } from '../common/text';

export interface PatientInput {
  fullName?: string | null;
  sex?: string | null;
  birthDate?: string | null;
  profession?: string | null;
  phone?: string | null;
  mobile?: string | null;
  email?: string | null;
  notes?: string | null;
  postalCode?: string | null;
  street?: string | null;
  streetNumber?: string | null;
  addressComplement?: string | null;
  district?: string | null;
  city?: string | null;
  state?: string | null;
  rg?: string | null;
  cpf?: string | null;
  otherDocument?: string | null;
}

export interface PatientData {
  fullName: string;
  searchName: string;
  sex: Sex;
  birthDate: Date;
  profession: string | null;
  phone: string | null;
  mobile: string | null;
  email: string | null;
  notes: string | null;
  postalCode: string | null;
  street: string | null;
  streetNumber: string | null;
  addressComplement: string | null;
  district: string | null;
  city: string | null;
  state: string | null;
  rg: string | null;
  cpf: string | null;
  otherDocument: string | null;
}

export type FieldErrors = Partial<Record<keyof PatientInput, string>>;

export const PATIENT_AUDIT_FIELDS = [
  'fullName', 'sex', 'birthDate', 'profession', 'phone', 'mobile', 'email', 'notes', 'postalCode', 'street',
  'streetNumber', 'addressComplement', 'district', 'city', 'state', 'rg', 'cpf', 'otherDocument',
] as const;

const MIN_BIRTH_DATE = '1900-01-01';
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function normalizePatient(input: PatientInput, today: string): { data: PatientData | null; errors: FieldErrors } {
  const errors: FieldErrors = {};

  const fullName = cleanText(input.fullName);
  if (!fullName) errors.fullName = 'Informe o nome.';
  else if (fullName.length > 200) errors.fullName = 'O nome deve ter no máximo 200 caracteres.';

  const sex: Sex | null = input.sex === 'FEMININO' || input.sex === 'MASCULINO' ? input.sex : null;
  if (!sex) errors.sex = 'Selecione o sexo.';

  const birthDate = parseIsoDate(input.birthDate ?? '');
  if (!birthDate) errors.birthDate = 'Data de nascimento inválida.';
  else if (formatIsoDate(birthDate) > today) errors.birthDate = 'A data de nascimento não pode estar no futuro.';
  else if (formatIsoDate(birthDate) < MIN_BIRTH_DATE) errors.birthDate = 'A data de nascimento deve ser a partir de 1900.';

  const cpf = digitsOnly(cleanText(input.cpf));
  if (cpf !== null && cpf.length !== 11) errors.cpf = 'O CPF deve ter 11 dígitos.';

  const postalCode = digitsOnly(cleanText(input.postalCode));
  if (postalCode !== null && postalCode.length !== 8) errors.postalCode = 'O CEP deve ter 8 dígitos.';

  const state = cleanText(input.state)?.toUpperCase() ?? null;
  if (state !== null && !UFS.includes(state)) errors.state = 'UF inválida.';

  const email = cleanText(input.email);
  if (email !== null && !EMAIL_PATTERN.test(email)) errors.email = 'E-mail inválido.';

  if (Object.keys(errors).length > 0 || !fullName || !sex || !birthDate) return { data: null, errors };

  return {
    errors,
    data: {
      fullName,
      searchName: normalizeForSearch(fullName),
      sex,
      birthDate,
      profession: cleanText(input.profession),
      phone: cleanText(input.phone),
      mobile: cleanText(input.mobile),
      email,
      notes: cleanMultiline(input.notes),
      postalCode,
      street: cleanText(input.street),
      streetNumber: cleanText(input.streetNumber),
      addressComplement: cleanText(input.addressComplement),
      district: cleanText(input.district),
      city: cleanText(input.city),
      state,
      rg: cleanText(input.rg),
      cpf,
      otherDocument: cleanText(input.otherDocument),
    },
  };
}
```

`apps/api/src/patients/patient.view.ts`:

```ts
import type { Patient } from '@prisma/client';
import { formatIsoDate } from '../common/civil-date';

export type PatientView = Omit<Patient, 'birthDate' | 'createdAt' | 'updatedAt' | 'searchName' | 'createdById' | 'updatedById'> & {
  birthDate: string;
  createdAt: string;
  updatedAt: string;
};

export type PatientListItem = Pick<PatientView, 'id' | 'legacyId' | 'fullName' | 'sex' | 'birthDate' | 'cpf'>;

export function toPatientView(p: Patient): PatientView {
  return {
    id: p.id,
    legacyId: p.legacyId,
    fullName: p.fullName,
    sex: p.sex,
    birthDate: formatIsoDate(p.birthDate),
    profession: p.profession,
    phone: p.phone,
    mobile: p.mobile,
    email: p.email,
    notes: p.notes,
    postalCode: p.postalCode,
    street: p.street,
    streetNumber: p.streetNumber,
    addressComplement: p.addressComplement,
    district: p.district,
    city: p.city,
    state: p.state,
    rg: p.rg,
    cpf: p.cpf,
    otherDocument: p.otherDocument,
    version: p.version,
    createdAt: p.createdAt.toISOString(),
    updatedAt: p.updatedAt.toISOString(),
  };
}

export function toPatientListItem(p: Patient): PatientListItem {
  return { id: p.id, legacyId: p.legacyId, fullName: p.fullName, sex: p.sex, birthDate: formatIsoDate(p.birthDate), cpf: p.cpf };
}
```

`apps/api/src/patients/dto/patient.dto.ts`:

```ts
import { Type } from 'class-transformer';
import { IsInt, IsOptional, IsString, Max, MaxLength, Min } from 'class-validator';

export class PatientBodyDto {
  @IsString() @MaxLength(250) fullName!: string;
  @IsString() @MaxLength(20) sex!: string;
  @IsString() @MaxLength(10) birthDate!: string;
  @IsOptional() @IsString() @MaxLength(120) profession?: string | null;
  @IsOptional() @IsString() @MaxLength(40) phone?: string | null;
  @IsOptional() @IsString() @MaxLength(40) mobile?: string | null;
  @IsOptional() @IsString() @MaxLength(200) email?: string | null;
  @IsOptional() @IsString() @MaxLength(4000) notes?: string | null;
  @IsOptional() @IsString() @MaxLength(20) postalCode?: string | null;
  @IsOptional() @IsString() @MaxLength(200) street?: string | null;
  @IsOptional() @IsString() @MaxLength(20) streetNumber?: string | null;
  @IsOptional() @IsString() @MaxLength(120) addressComplement?: string | null;
  @IsOptional() @IsString() @MaxLength(120) district?: string | null;
  @IsOptional() @IsString() @MaxLength(120) city?: string | null;
  @IsOptional() @IsString() @MaxLength(10) state?: string | null;
  @IsOptional() @IsString() @MaxLength(30) rg?: string | null;
  @IsOptional() @IsString() @MaxLength(20) cpf?: string | null;
  @IsOptional() @IsString() @MaxLength(60) otherDocument?: string | null;
}

export class UpdatePatientDto extends PatientBodyDto {
  @IsInt() @Min(1) version!: number;
}

export class ListPatientsQueryDto {
  @IsOptional() @IsString() @MaxLength(100) q?: string;
  @IsOptional() @Type(() => Number) @IsInt() @Min(1) page?: number;
  @IsOptional() @Type(() => Number) @IsInt() @Min(1) @Max(100) pageSize?: number;
}
```

`apps/api/src/patients/patients.service.ts`:

```ts
import { BadRequestException, ConflictException, Inject, Injectable, NotFoundException } from '@nestjs/common';
import type { Patient, Prisma } from '@prisma/client';
import { AuditService } from '../audit/audit.service';
import { diffFields } from '../audit/diff';
import { todayInTimeZone } from '../common/civil-date';
import { cleanText, normalizeForSearch } from '../common/text';
import { APP_CONFIG, AppConfig } from '../config';
import { PrismaService } from '../prisma/prisma.service';
import { ListPatientsQueryDto, PatientBodyDto, UpdatePatientDto } from './dto/patient.dto';
import { normalizePatient, PATIENT_AUDIT_FIELDS, PatientData } from './patient-normalize';
import { PatientListItem, PatientView, toPatientListItem, toPatientView } from './patient.view';

export interface PatientPage {
  items: PatientListItem[];
  total: number;
  page: number;
  pageSize: number;
}

const CONFLICT_MESSAGE = 'Este cadastro foi alterado por outra pessoa. Recarregue para ver a versão atual.';

@Injectable()
export class PatientsService {
  constructor(
    private readonly prisma: PrismaService,
    private readonly audit: AuditService,
    @Inject(APP_CONFIG) private readonly config: AppConfig,
  ) {}

  async list(query: ListPatientsQueryDto): Promise<PatientPage> {
    const page = query.page ?? 1;
    const pageSize = query.pageSize ?? 20;
    const q = cleanText(query.q);
    let where: Prisma.PatientWhereInput = {};
    if (q) {
      const words = normalizeForSearch(q).split(' ');
      const or: Prisma.PatientWhereInput[] = [{ AND: words.map((word) => ({ searchName: { contains: word } })) }];
      const digits = q.replace(/\D/g, '');
      if (digits.length >= 3) or.push({ cpf: { contains: digits } });
      if (/^\d{1,9}$/.test(q)) or.push({ legacyId: Number(q) });
      where = { OR: or };
    }
    const [items, total] = await this.prisma.$transaction([
      this.prisma.patient.findMany({
        where,
        orderBy: [{ searchName: 'asc' }, { id: 'asc' }],
        skip: (page - 1) * pageSize,
        take: pageSize,
      }),
      this.prisma.patient.count({ where }),
    ]);
    return { items: items.map(toPatientListItem), total, page, pageSize };
  }

  async get(id: string): Promise<PatientView> {
    const patient = await this.prisma.patient.findUnique({ where: { id } });
    if (!patient) throw new NotFoundException('Paciente não encontrado.');
    return toPatientView(patient);
  }

  async create(body: PatientBodyDto, actorId: string): Promise<PatientView> {
    const data = this.validOrThrow(body);
    const created = await this.prisma.$transaction(async (tx) => {
      const patient = await tx.patient.create({ data: { ...data, createdById: actorId, updatedById: actorId } });
      await this.audit.record(
        { actorId, action: 'patient.create', entityType: 'patient', entityId: patient.id, data: { source: 'manual' } },
        tx,
      );
      return patient;
    });
    return toPatientView(created);
  }

  async update(id: string, body: UpdatePatientDto, actorId: string): Promise<PatientView> {
    const data = this.validOrThrow(body);
    return this.prisma.$transaction(async (tx) => {
      const before = await tx.patient.findUnique({ where: { id } });
      if (!before) throw new NotFoundException('Paciente não encontrado.');
      if (before.version !== body.version) throw this.conflict(before);
      const result = await tx.patient.updateMany({
        where: { id, version: body.version },
        data: { ...data, updatedById: actorId, version: { increment: 1 } },
      });
      if (result.count === 0) throw this.conflict(await tx.patient.findUniqueOrThrow({ where: { id } }));
      const after = await tx.patient.findUniqueOrThrow({ where: { id } });
      const changes = diffFields(before, after, PATIENT_AUDIT_FIELDS);
      await this.audit.record({ actorId, action: 'patient.update', entityType: 'patient', entityId: id, data: { changes } }, tx);
      return toPatientView(after);
    });
  }

  private validOrThrow(body: PatientBodyDto): PatientData {
    const { data, errors } = normalizePatient(body, todayInTimeZone(this.config.clinicTimeZone));
    if (!data) throw new BadRequestException({ message: 'Dados inválidos.', errors });
    return data;
  }

  private conflict(current: Patient): ConflictException {
    return new ConflictException({ message: CONFLICT_MESSAGE, current: toPatientView(current) });
  }
}
```

`apps/api/src/patients/patients.controller.ts`:

```ts
import { Body, Controller, Get, Param, ParseUUIDPipe, Post, Put, Query } from '@nestjs/common';
import type { AuthContext } from '../auth/auth-context';
import { CurrentAuth } from '../auth/current-auth.decorator';
import { RequirePermissions } from '../auth/require-permissions.decorator';
import { ListPatientsQueryDto, PatientBodyDto, UpdatePatientDto } from './dto/patient.dto';
import type { PatientView } from './patient.view';
import { PatientPage, PatientsService } from './patients.service';

@Controller('patients')
export class PatientsController {
  constructor(private readonly patients: PatientsService) {}

  @Get()
  @RequirePermissions('patients.read')
  list(@Query() query: ListPatientsQueryDto): Promise<PatientPage> {
    return this.patients.list(query);
  }

  @Get(':id')
  @RequirePermissions('patients.read')
  get(@Param('id', new ParseUUIDPipe()) id: string): Promise<PatientView> {
    return this.patients.get(id);
  }

  @Post()
  @RequirePermissions('patients.write')
  create(@Body() body: PatientBodyDto, @CurrentAuth() auth: AuthContext): Promise<PatientView> {
    return this.patients.create(body, auth.user.id);
  }

  @Put(':id')
  @RequirePermissions('patients.write')
  update(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Body() body: UpdatePatientDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<PatientView> {
    return this.patients.update(id, body, auth.user.id);
  }
}
```

`apps/api/src/patients/patients.module.ts`:

```ts
import { Module } from '@nestjs/common';
import { PatientsController } from './patients.controller';
import { PatientsService } from './patients.service';

@Module({ controllers: [PatientsController], providers: [PatientsService], exports: [PatientsService] })
export class PatientsModule {}
```

`apps/api/src/app.module.ts` — acrescentar `PatientsModule` aos imports (`import { PatientsModule } from './patients/patients.module';`).

```powershell
Set-Location apps/api
npx prisma migrate dev --name patients
Set-Location ../..
```

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/api`
Expected: PASS — incluindo `text.spec` (5), `civil-date.spec` (2), `patient-normalize.spec` (3) e `patients.e2e-spec` (6).

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat(api): cadastro de pacientes com busca normalizada e controle de versão"
```

---

### Task 7: Leitura da planilha e mapeamento da aba Clientes

**Files:**
- Modify: `apps/api/package.json` (dependências `jszip`, `fast-xml-parser`)
- Create: `apps/api/src/import/import.types.ts`, `src/import/xlsx-reader.ts`, `src/import/clientes-mapper.ts`
- Create: `apps/api/test/helpers/xlsx-fixture.ts`
- Test: `apps/api/src/import/xlsx-reader.spec.ts`, `src/import/clientes-mapper.spec.ts`

**Interfaces:**
- Consumes: `cleanText`, `cleanMultiline`, `digitsOnly`, `normalizeForSearch`, `UFS`, `parseIsoDate`, `formatIsoDate`.
- Produces: `readSheet(buffer: Buffer, sheetName: string): Promise<SheetRow[]>`; `SheetRow { rowNumber: number; cells: Record<string, CellValue> }`; `CellValue = string | number | boolean | null`; `XlsxReadError`; `CLIENTES_SHEET = 'Clientes'`; `mapClientes(rows: SheetRow[], today: string): { headerErrors: string[]; candidates: CandidateRow[] }`; `parseBirthDate(value): string | null`; tipos `PatientImportData`, `CandidateRow`, `RowStatus`, `ImportMatch`, `ClassifiedRow`; helpers de teste `buildXlsx(sheets: FixtureSheet[]): Promise<Buffer>`, `CLIENTES_HEADER`, `clientesRow(values)`, `FixtureCell`.

- [ ] **Step 1: Instalar dependências**

```powershell
npm install -w @psm/api jszip@^3.10.1 fast-xml-parser@^4.5.0
```

- [ ] **Step 2: Criar o gerador de planilhas de teste**

`apps/api/test/helpers/xlsx-fixture.ts`:

```ts
import JSZip from 'jszip';

export type FixtureCell = string | number | null;

export interface FixtureSheet {
  name: string;
  rows: FixtureCell[][];
  inlineStrings?: boolean;
}

function columnLetter(index: number): string {
  let n = index + 1;
  let letters = '';
  while (n > 0) {
    const remainder = (n - 1) % 26;
    letters = String.fromCharCode(65 + remainder) + letters;
    n = Math.floor((n - 1) / 26);
  }
  return letters;
}

function escapeXml(value: string): string {
  return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

const XML_HEADER = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>';
const MAIN_NS = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main';

/** Gera um .xlsx mínimo com dados sintéticos: strings compartilhadas (ou inline), números e células vazias. */
export async function buildXlsx(sheets: FixtureSheet[]): Promise<Buffer> {
  const zip = new JSZip();
  const shared: string[] = [];
  const sharedIndex = new Map<string, number>();

  sheets.forEach((sheet, sheetIndex) => {
    const rowsXml = sheet.rows
      .map((row, rowIndex) => {
        const cells = row
          .map((value, columnIndex) => {
            const ref = `${columnLetter(columnIndex)}${rowIndex + 1}`;
            if (value === null) return '';
            if (typeof value === 'number') return `<c r="${ref}"><v>${value}</v></c>`;
            if (sheet.inlineStrings) {
              return `<c r="${ref}" t="inlineStr"><is><t xml:space="preserve">${escapeXml(value)}</t></is></c>`;
            }
            let index = sharedIndex.get(value);
            if (index === undefined) {
              index = shared.length;
              shared.push(value);
              sharedIndex.set(value, index);
            }
            return `<c r="${ref}" t="s"><v>${index}</v></c>`;
          })
          .join('');
        return `<row r="${rowIndex + 1}">${cells}</row>`;
      })
      .join('');
    zip.file(
      `xl/worksheets/sheet${sheetIndex + 1}.xml`,
      `${XML_HEADER}<worksheet xmlns="${MAIN_NS}"><sheetData>${rowsXml}</sheetData></worksheet>`,
    );
  });

  const sheetsXml = sheets
    .map((sheet, i) => `<sheet name="${escapeXml(sheet.name)}" sheetId="${i + 1}" r:id="rId${i + 1}"/>`)
    .join('');
  zip.file(
    'xl/workbook.xml',
    `${XML_HEADER}<workbook xmlns="${MAIN_NS}" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>${sheetsXml}</sheets></workbook>`,
  );
  const relsXml = sheets
    .map(
      (_sheet, i) =>
        `<Relationship Id="rId${i + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet${i + 1}.xml"/>`,
    )
    .join('');
  zip.file(
    'xl/_rels/workbook.xml.rels',
    `${XML_HEADER}<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">${relsXml}</Relationships>`,
  );
  const sharedXml = shared.map((s) => `<si><t xml:space="preserve">${escapeXml(s)}</t></si>`).join('');
  zip.file(
    'xl/sharedStrings.xml',
    `${XML_HEADER}<sst xmlns="${MAIN_NS}" count="${shared.length}" uniqueCount="${shared.length}">${sharedXml}</sst>`,
  );
  return zip.generateAsync({ type: 'nodebuffer' });
}

/** Cabeçalho real da aba Clientes (com os espaços finais do arquivo atual). */
export const CLIENTES_HEADER: FixtureCell[] = [
  'Seq.', 'Nome ', 'Sexo ', 'Nascimento', 'Idade ', 'Profissao', 'Foto Anexo', 'Telefone', 'Celular', 'Email',
  'Observações Gerais', 'Cep', 'Logradouro', 'Numero', 'Complemento', 'Bairro', 'Município', 'UF', 'Rg', 'Rg Anexo',
  'Cpf', 'Cpf Anexo', 'Outro', 'Outro Anexo', 'Anamnese Anexo',
];

const CLIENTES_KEYS = [
  'seq', 'nome', 'sexo', 'nascimento', 'idade', 'profissao', 'foto', 'telefone', 'celular', 'email',
  'obs', 'cep', 'logradouro', 'numero', 'complemento', 'bairro', 'municipio', 'uf', 'rg', 'rgAnexo',
  'cpf', 'cpfAnexo', 'outro', 'outroAnexo', 'anamneseAnexo',
] as const;

/** Linha da aba Clientes na ordem das colunas A–Y; campos omitidos ficam vazios. */
export function clientesRow(values: Partial<Record<(typeof CLIENTES_KEYS)[number], FixtureCell>>): FixtureCell[] {
  return CLIENTES_KEYS.map((key) => values[key] ?? null);
}
```

- [ ] **Step 3: Escrever os testes**

`apps/api/src/import/xlsx-reader.spec.ts`:

```ts
import JSZip from 'jszip';
import { buildXlsx } from '../../test/helpers/xlsx-fixture';
import { readSheet, XlsxReadError } from './xlsx-reader';

describe('readSheet', () => {
  it('lê strings compartilhadas, números e células vazias da aba pedida', async () => {
    const buffer = await buildXlsx([
      { name: 'Início', rows: [['ignorar']] },
      { name: 'Clientes', rows: [['Seq.', 'Nome '], [7, 'Ana'], [null, 'Sem seq']] },
    ]);

    expect(await readSheet(buffer, 'Clientes')).toEqual([
      { rowNumber: 1, cells: { A: 'Seq.', B: 'Nome ' } },
      { rowNumber: 2, cells: { A: 7, B: 'Ana' } },
      { rowNumber: 3, cells: { B: 'Sem seq' } },
    ]);
  });

  it('lê strings inline preservando espaços e zeros à esquerda', async () => {
    const buffer = await buildXlsx([{ name: 'Clientes', inlineStrings: true, rows: [[' com espaço ', '00123']] }]);
    expect(await readSheet(buffer, 'Clientes')).toEqual([{ rowNumber: 1, cells: { A: ' com espaço ', B: '00123' } }]);
  });

  it('junta os trechos de texto formatado', async () => {
    const zip = await JSZip.loadAsync(await buildXlsx([{ name: 'Clientes', rows: [['trocar']] }]));
    zip.file(
      'xl/sharedStrings.xml',
      '<?xml version="1.0" encoding="UTF-8"?><sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><si><r><t>Jo</t></r><r><rPr><b/></rPr><t>ão</t></r></si></sst>',
    );

    const rows = await readSheet(await zip.generateAsync({ type: 'nodebuffer' }), 'Clientes');

    expect(rows[0].cells.A).toBe('João');
  });

  it('avisa quando a aba não existe', async () => {
    const buffer = await buildXlsx([{ name: 'Outra', rows: [['x']] }]);
    await expect(readSheet(buffer, 'Clientes')).rejects.toThrow(new XlsxReadError('Aba "Clientes" não encontrada na planilha.'));
  });

  it('avisa quando o arquivo não é planilha', async () => {
    await expect(readSheet(Buffer.from('não sou um zip'), 'Clientes')).rejects.toThrow(
      new XlsxReadError('O arquivo não é uma planilha .xlsx ou .xlsm válida.'),
    );
  });
});
```

`apps/api/src/import/clientes-mapper.spec.ts`:

```ts
import { CLIENTES_HEADER, clientesRow, FixtureCell } from '../../test/helpers/xlsx-fixture';
import { mapClientes, parseBirthDate } from './clientes-mapper';
import type { SheetRow } from './xlsx-reader';

const TODAY = '2026-10-01';

function toSheet(rows: FixtureCell[][]): SheetRow[] {
  return rows.map((row, index) => ({
    rowNumber: index + 1,
    cells: Object.fromEntries(
      row.flatMap((value, column) => (value === null ? [] : [[String.fromCharCode(65 + column), value]])),
    ),
  }));
}

describe('mapClientes', () => {
  it('aceita o cabeçalho atual e normaliza a linha, ignorando a idade gravada', () => {
    const { headerErrors, candidates } = mapClientes(
      toSheet([
        CLIENTES_HEADER,
        clientesRow({
          seq: 7, nome: '  Ana   Souza ', sexo: 'Feminino ', nascimento: 29351, idade: 99, telefone: 1133334444,
          email: 'ana@exemplo.com', obs: 'linha 1\nlinha 2', cep: '01310-100', uf: 'sp', rg: 'MG-12.345',
          cpf: '123.456.789-01', numero: 150,
        }),
      ]),
      TODAY,
    );

    expect(headerErrors).toEqual([]);
    expect(candidates).toEqual([
      {
        rowNumber: 2,
        legacyId: 7,
        errors: [],
        warnings: [],
        data: {
          fullName: 'Ana Souza', searchName: 'ana souza', sex: 'FEMININO', birthDate: '1980-05-10',
          profession: null, phone: '1133334444', mobile: null, email: 'ana@exemplo.com', notes: 'linha 1\nlinha 2',
          postalCode: '01310100', street: null, streetNumber: '150', addressComplement: null, district: null,
          city: null, state: 'SP', rg: 'MG-12.345', cpf: '12345678901', otherDocument: null,
        },
      },
    ]);
  });

  it('recusa a aba inteira quando colunas estão fora do lugar', () => {
    const header = [...CLIENTES_HEADER];
    [header[18], header[20]] = [header[20], header[18]];

    const { headerErrors, candidates } = mapClientes(
      toSheet([header, clientesRow({ seq: 1, nome: 'Ana', sexo: 'Feminino', nascimento: 29351 })]),
      TODAY,
    );

    expect(headerErrors).toEqual([
      'Coluna S: esperado "rg", encontrado "cpf".',
      'Coluna U: esperado "cpf", encontrado "rg".',
    ]);
    expect(candidates).toEqual([]);
  });

  it('recusa aba sem cabeçalho', () => {
    expect(mapClientes([], TODAY).headerErrors).toEqual(['Cabeçalho não encontrado na linha 1 da aba Clientes.']);
  });

  it('restaura zeros de CPF e CEP gravados como número e descarta tamanhos errados com aviso', () => {
    const { candidates } = mapClientes(
      toSheet([
        CLIENTES_HEADER,
        clientesRow({ seq: 1, nome: 'A', sexo: 'Feminino', nascimento: 29351, cpf: 1234567890, cep: 1310100, rg: 1234567 }),
        clientesRow({ seq: 2, nome: 'B', sexo: 'Feminino', nascimento: 29351, cpf: '123', cep: '1234', uf: 'XX' }),
      ]),
      TODAY,
    );

    expect(candidates[0].data).toMatchObject({ cpf: '01234567890', postalCode: '01310100', rg: '1234567' });
    expect(candidates[0].warnings).toEqual([
      'CPF gravado como número: zeros à esquerda restaurados.',
      'CEP gravado como número: zeros à esquerda restaurados.',
      'RG gravado como número na planilha: confira zeros à esquerda.',
    ]);
    expect(candidates[1].data).toMatchObject({ cpf: null, postalCode: null, state: null });
    expect(candidates[1].warnings).toEqual([
      'CPF inválido "123"; importado sem CPF.',
      'CEP inválido "1234"; importado sem CEP.',
      'UF inválida "XX"; importada sem UF.',
    ]);
  });

  it('aceita nascimento em serial ou dd/mm/aaaa e rejeita vazio, futuro e data inexistente', () => {
    const { candidates } = mapClientes(
      toSheet([
        CLIENTES_HEADER,
        clientesRow({ seq: 1, nome: 'A', sexo: 'Feminino', nascimento: '2/3/1975' }),
        clientesRow({ seq: 2, nome: 'B', sexo: 'Feminino' }),
        clientesRow({ seq: 3, nome: 'C', sexo: 'Feminino', nascimento: '01/01/2030' }),
        clientesRow({ seq: 4, nome: 'D', sexo: 'Feminino', nascimento: '31/02/1980' }),
      ]),
      TODAY,
    );

    expect(candidates.map((c) => c.data?.birthDate ?? c.errors)).toEqual([
      '1975-03-02',
      ['Nascimento ausente ou inválido.'],
      ['Nascimento no futuro.'],
      ['Nascimento ausente ou inválido.'],
    ]);
  });

  it('converte serial do Excel em data civil', () => {
    expect(parseBirthDate(29351)).toBe('1980-05-10');
    expect(parseBirthDate(32874.75)).toBe('1990-01-01');
    expect(parseBirthDate(60)).toBeNull();
  });

  it('exige Seq inteiro, nome e sexo válidos', () => {
    const { candidates } = mapClientes(
      toSheet([CLIENTES_HEADER, clientesRow({ seq: 3.5, nome: '  ', sexo: 'F', nascimento: 29351 })]),
      TODAY,
    );

    expect(candidates[0]).toMatchObject({
      legacyId: null,
      data: null,
      errors: ['Seq ausente ou inválido.', 'Nome ausente.', 'Sexo inválido: "F".'],
    });
  });

  it('ignora linhas vazias e avisa que foto e anexos não são importados', () => {
    const { candidates } = mapClientes(
      toSheet([
        CLIENTES_HEADER,
        clientesRow({}),
        clientesRow({ seq: 5, nome: 'E', sexo: 'Masculino', nascimento: 29351, foto: 'C:\\CLIENTES\\5 E\\foto.jpg' }),
      ]),
      TODAY,
    );

    expect(candidates).toHaveLength(1);
    expect(candidates[0].rowNumber).toBe(3);
    expect(candidates[0].warnings).toEqual(['Foto e anexos não fazem parte da carga inicial e não foram importados.']);
  });
});
```

- [ ] **Step 4: Rodar e ver falhar**

Run: `npm test -w @psm/api -- xlsx-reader clientes-mapper`
Expected: FAIL — `Cannot find module './xlsx-reader'` e `'./clientes-mapper'`.

- [ ] **Step 5: Implementar**

`apps/api/src/import/import.types.ts`:

```ts
export type Sex = 'FEMININO' | 'MASCULINO';

export interface PatientImportData {
  fullName: string;
  searchName: string;
  sex: Sex;
  birthDate: string;
  profession: string | null;
  phone: string | null;
  mobile: string | null;
  email: string | null;
  notes: string | null;
  postalCode: string | null;
  street: string | null;
  streetNumber: string | null;
  addressComplement: string | null;
  district: string | null;
  city: string | null;
  state: string | null;
  rg: string | null;
  cpf: string | null;
  otherDocument: string | null;
}

export interface CandidateRow {
  rowNumber: number;
  legacyId: number | null;
  data: PatientImportData | null;
  errors: string[];
  warnings: string[];
}

export type RowStatus = 'novo' | 'ja_importado' | 'possivel_duplicado' | 'rejeitado';

export interface ImportMatch {
  reason: 'CPF' | 'nome e nascimento';
  patientId?: string;
  rowNumber?: number;
}

export interface ClassifiedRow extends CandidateRow {
  status: RowStatus;
  matches: ImportMatch[];
  /** Mensagens da classificação (ex.: Seq repetido), separadas dos erros do mapeamento. */
  notes: string[];
}
```

`apps/api/src/import/xlsx-reader.ts`:

```ts
import { XMLParser } from 'fast-xml-parser';
import JSZip from 'jszip';

export type CellValue = string | number | boolean | null;

export interface SheetRow {
  rowNumber: number;
  cells: Record<string, CellValue>;
}

export class XlsxReadError extends Error {}

type XmlNode = Record<string, unknown>;

const ARRAY_TAGS = new Set(['sheet', 'Relationship', 'si', 'r', 'row', 'c']);

const parser = new XMLParser({
  ignoreAttributes: false,
  attributeNamePrefix: '@_',
  parseTagValue: false,
  trimValues: false,
  isArray: (name, _jpath, _isLeafNode, isAttribute) => !isAttribute && ARRAY_TAGS.has(name),
});

export async function readSheet(buffer: Buffer, sheetName: string): Promise<SheetRow[]> {
  let zip: JSZip;
  try {
    zip = await JSZip.loadAsync(buffer);
  } catch {
    throw new XlsxReadError('O arquivo não é uma planilha .xlsx ou .xlsm válida.');
  }

  const workbook = await readXml(zip, 'xl/workbook.xml');
  const sheet = list(child(child(workbook, 'workbook'), 'sheets'), 'sheet').find((s) => s['@_name'] === sheetName);
  if (!sheet) throw new XlsxReadError(`Aba "${sheetName}" não encontrada na planilha.`);

  const rels = await readXml(zip, 'xl/_rels/workbook.xml.rels');
  const rel = list(child(rels, 'Relationships'), 'Relationship').find((r) => r['@_Id'] === sheet['@_r:id']);
  if (!rel) throw new XlsxReadError(`Aba "${sheetName}" sem arquivo associado.`);
  const target = String(rel['@_Target']);
  const sheetPath = target.startsWith('/') ? target.slice(1) : `xl/${target}`;

  const sharedStrings = zip.file('xl/sharedStrings.xml')
    ? list(child(await readXml(zip, 'xl/sharedStrings.xml'), 'sst'), 'si').map(richText)
    : [];

  const worksheet = await readXml(zip, sheetPath);
  return list(child(child(worksheet, 'worksheet'), 'sheetData'), 'row').map((row) => {
    const cells: Record<string, CellValue> = {};
    for (const cell of list(row, 'c')) {
      const ref = /^([A-Z]+)\d+$/.exec(String(cell['@_r'] ?? ''));
      if (ref) cells[ref[1]] = cellValue(cell, sharedStrings);
    }
    return { rowNumber: Number(row['@_r']), cells };
  });
}

async function readXml(zip: JSZip, path: string): Promise<XmlNode> {
  const file = zip.file(path);
  if (!file) throw new XlsxReadError(`Estrutura de planilha inválida: ${path} ausente.`);
  return parser.parse(await file.async('string')) as XmlNode;
}

function child(node: unknown, key: string): XmlNode {
  if (typeof node !== 'object' || node === null) return {};
  const value = (node as XmlNode)[key];
  return typeof value === 'object' && value !== null ? (value as XmlNode) : {};
}

function list(node: XmlNode, key: string): XmlNode[] {
  const value = node[key];
  return Array.isArray(value) ? value.filter((v): v is XmlNode => typeof v === 'object' && v !== null) : [];
}

function text(node: unknown): string {
  if (node === undefined || node === null) return '';
  if (typeof node === 'string' || typeof node === 'number') return String(node);
  if (typeof node === 'object' && '#text' in node) return String((node as XmlNode)['#text']);
  return '';
}

function richText(node: XmlNode): string {
  if ('t' in node) return text(node.t);
  return list(node, 'r')
    .map((run) => text(run.t))
    .join('');
}

function cellValue(cell: XmlNode, sharedStrings: string[]): CellValue {
  const type = String(cell['@_t'] ?? 'n');
  const raw = cell.v === undefined ? undefined : text(cell.v);
  switch (type) {
    case 's':
      return raw === undefined ? null : (sharedStrings[Number(raw)] ?? null);
    case 'inlineStr':
      return richText(child(cell, 'is'));
    case 'str':
      return raw ?? null;
    case 'b':
      return raw === '1';
    case 'e':
      return null;
    default: {
      if (raw === undefined || raw.trim() === '') return null;
      const value = Number(raw);
      return Number.isFinite(value) ? value : null;
    }
  }
}
```

`apps/api/src/import/clientes-mapper.ts`:

```ts
import { formatIsoDate, parseIsoDate } from '../common/civil-date';
import { cleanMultiline, cleanText, digitsOnly, normalizeForSearch, UFS } from '../common/text';
import type { CandidateRow, PatientImportData, Sex } from './import.types';
import type { CellValue, SheetRow } from './xlsx-reader';

export const CLIENTES_SHEET = 'Clientes';

/** Cabeçalhos da aba Clientes (dicionário de dados, 25 colunas), comparados sem acento, caixa ou espaços extras. */
const EXPECTED_HEADERS: Readonly<Record<string, string>> = {
  A: 'seq.', B: 'nome', C: 'sexo', D: 'nascimento', E: 'idade', F: 'profissao', G: 'foto anexo', H: 'telefone',
  I: 'celular', J: 'email', K: 'observacoes gerais', L: 'cep', M: 'logradouro', N: 'numero', O: 'complemento',
  P: 'bairro', Q: 'municipio', R: 'uf', S: 'rg', T: 'rg anexo', U: 'cpf', V: 'cpf anexo', W: 'outro',
  X: 'outro anexo', Y: 'anamnese anexo',
};

const ATTACHMENT_COLUMNS = ['G', 'T', 'V', 'X', 'Y'];
/** Serial de 1970-01-01 no sistema de datas 1900 do Excel. */
const EXCEL_UNIX_EPOCH = 25569;
/** Seriais menores que 61 (antes de 01/03/1900) sofrem o bug do 29/02/1900 do Excel. */
const FIRST_RELIABLE_SERIAL = 61;
const MIN_BIRTH_DATE = '1900-01-01';

export interface ClientesMapping {
  headerErrors: string[];
  candidates: CandidateRow[];
}

export function mapClientes(rows: SheetRow[], today: string): ClientesMapping {
  const header = rows.find((r) => r.rowNumber === 1);
  if (!header) return { headerErrors: ['Cabeçalho não encontrado na linha 1 da aba Clientes.'], candidates: [] };

  const headerErrors = Object.entries(EXPECTED_HEADERS).flatMap(([column, expected]) => {
    const found = normalizeForSearch(String(header.cells[column] ?? ''));
    return found === expected ? [] : [`Coluna ${column}: esperado "${expected}", encontrado "${found || '(vazio)'}".`];
  });
  if (headerErrors.length > 0) return { headerErrors, candidates: [] };

  const candidates = rows
    .filter((r) => r.rowNumber > 1 && Object.values(r.cells).some((v) => cleanText(v) !== null))
    .map((r) => mapRow(r, today));
  return { headerErrors: [], candidates };
}

function mapRow(row: SheetRow, today: string): CandidateRow {
  const c = row.cells;
  const errors: string[] = [];
  const warnings: string[] = [];

  const seq = c.A;
  const legacyId = typeof seq === 'number' && Number.isInteger(seq) && seq > 0 ? seq : null;
  if (legacyId === null) errors.push('Seq ausente ou inválido.');

  const fullName = cleanText(c.B);
  if (!fullName) errors.push('Nome ausente.');

  const sex = parseSex(c.C);
  if (!sex) errors.push(`Sexo inválido: "${cleanText(c.C) ?? ''}".`);

  const parsedBirth = parseBirthDate(c.D);
  let birthDate: string | null = null;
  if (!parsedBirth) errors.push('Nascimento ausente ou inválido.');
  else if (parsedBirth > today) errors.push('Nascimento no futuro.');
  else if (parsedBirth < MIN_BIRTH_DATE) errors.push('Nascimento anterior a 1900.');
  else birthDate = parsedBirth;

  const cpf = documentDigits(c.U, 11, 'CPF', warnings);
  const postalCode = documentDigits(c.L, 8, 'CEP', warnings);

  const stateText = cleanText(c.R)?.toUpperCase() ?? null;
  let state: string | null = null;
  if (stateText !== null) {
    if (UFS.includes(stateText)) state = stateText;
    else warnings.push(`UF inválida "${stateText}"; importada sem UF.`);
  }

  if (typeof c.S === 'number') warnings.push('RG gravado como número na planilha: confira zeros à esquerda.');
  if (ATTACHMENT_COLUMNS.some((column) => cleanText(c[column]) !== null)) {
    warnings.push('Foto e anexos não fazem parte da carga inicial e não foram importados.');
  }

  if (errors.length > 0 || !fullName || !sex || !birthDate) {
    return { rowNumber: row.rowNumber, legacyId, data: null, errors, warnings };
  }

  const data: PatientImportData = {
    fullName,
    searchName: normalizeForSearch(fullName),
    sex,
    birthDate,
    profession: cleanText(c.F),
    phone: asText(c.H),
    mobile: asText(c.I),
    email: cleanText(c.J),
    notes: cleanMultiline(c.K),
    postalCode,
    street: cleanText(c.M),
    streetNumber: asText(c.N),
    addressComplement: asText(c.O),
    district: cleanText(c.P),
    city: cleanText(c.Q),
    state,
    rg: asText(c.S),
    cpf,
    otherDocument: asText(c.W),
  };
  return { rowNumber: row.rowNumber, legacyId, data, errors, warnings };
}

export function parseBirthDate(value: CellValue | undefined): string | null {
  if (typeof value === 'number') {
    if (!Number.isFinite(value) || value < FIRST_RELIABLE_SERIAL) return null;
    return new Date((Math.floor(value) - EXCEL_UNIX_EPOCH) * 86_400_000).toISOString().slice(0, 10);
  }
  const text = cleanText(value);
  if (!text) return null;
  const br = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec(text);
  const iso = br ? `${br[3]}-${br[2].padStart(2, '0')}-${br[1].padStart(2, '0')}` : text;
  const parsed = parseIsoDate(iso);
  return parsed ? formatIsoDate(parsed) : null;
}

function parseSex(value: CellValue | undefined): Sex | null {
  const normalized = normalizeForSearch(String(value ?? ''));
  if (normalized === 'feminino') return 'FEMININO';
  if (normalized === 'masculino') return 'MASCULINO';
  return null;
}

function numberToText(value: number): string {
  return Number.isInteger(value) ? value.toFixed(0) : String(value);
}

function asText(value: CellValue | undefined): string | null {
  return typeof value === 'number' ? numberToText(value) : cleanText(value);
}

/** CPF/CEP: só dígitos, no tamanho exato. Números perderam zeros à esquerda no VBA (CDbl): restaura até 3 zeros. */
function documentDigits(value: CellValue | undefined, length: number, label: string, warnings: string[]): string | null {
  if (typeof value === 'number') {
    const digits = numberToText(value);
    if (/^\d+$/.test(digits) && digits.length === length) return digits;
    if (/^\d+$/.test(digits) && digits.length < length && digits.length >= length - 3) {
      warnings.push(`${label} gravado como número: zeros à esquerda restaurados.`);
      return digits.padStart(length, '0');
    }
    warnings.push(`${label} inválido "${digits}"; importado sem ${label}.`);
    return null;
  }
  const original = cleanText(value);
  const digits = digitsOnly(original);
  if (digits === null) return null;
  if (digits.length === length) return digits;
  warnings.push(`${label} inválido "${original}"; importado sem ${label}.`);
  return null;
}
```

- [ ] **Step 6: Rodar e ver passar**

Run: `npm test -w @psm/api -- xlsx-reader clientes-mapper`
Expected: PASS — `xlsx-reader.spec` (5) e `clientes-mapper.spec` (8).

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat(api): leitura de .xlsx/.xlsm e mapeamento da aba Clientes"
```

---

### Task 8: Importação de cadastros — prévia, classificação e confirmação

**Files:**
- Modify: `apps/api/prisma/schema.prisma` (enum `ImportStatus`, modelo `ImportBatch`)
- Create: `apps/api/src/import/classify.ts`, `src/import/import.service.ts`, `src/import/dto/commit-import.dto.ts`, `src/import/import.controller.ts`, `src/import/import.module.ts`
- Modify: `apps/api/src/app.module.ts`
- Test: `apps/api/src/import/classify.spec.ts`, `apps/api/test/import.e2e-spec.ts`

**Interfaces:**
- Consumes: `readSheet`, `XlsxReadError`, `mapClientes`, `CLIENTES_SHEET`, tipos de `import.types.ts`, `AuditService.record`, `isUniqueViolation`, `parseIsoDate`, `formatIsoDate`, `todayInTimeZone`, `RequirePermissions`, `CurrentAuth`.
- Produces: `classifyCandidates(candidates: CandidateRow[], existing: ExistingPatientKey[]): ClassifiedRow[]`; `ImportService.preview(file: UploadedSpreadsheet, actorId): Promise<ImportBatchView>`, `.get(id)`, `.commit(id, forceLegacyIds: number[], actorId, now?)`; `ImportBatchView { id; fileName; status: 'PREVIEW' | 'COMMITTED'; createdAt; committedAt; counts: Record<RowStatus, number>; rows: ClassifiedRow[]; report: ImportReport | null }`; `ImportReport { imported; alreadyImported; duplicatesSkipped; rejected; outcomes }`; rotas `POST /api/imports` (multipart, campo `file`, 201), `GET /api/imports/:id`, `POST /api/imports/:id/commit` (`{ forceLegacyIds?: number[] }`, 200). Erros 400 no formato `{ message; errors: string[] }`.

- [ ] **Step 1: Escrever os testes**

`apps/api/src/import/classify.spec.ts`:

```ts
import { classifyCandidates, ExistingPatientKey } from './classify';
import type { CandidateRow, PatientImportData } from './import.types';

function candidate(rowNumber: number, legacyId: number | null, data: Partial<PatientImportData> | null): CandidateRow {
  return {
    rowNumber,
    legacyId,
    errors: data === null ? ['erro do mapeamento'] : [],
    warnings: [],
    data:
      data === null
        ? null
        : {
            fullName: 'X', searchName: 'x', sex: 'FEMININO', birthDate: '1980-05-10', profession: null, phone: null,
            mobile: null, email: null, notes: null, postalCode: null, street: null, streetNumber: null,
            addressComplement: null, district: null, city: null, state: null, rg: null, cpf: null, otherDocument: null,
            ...data,
          },
  };
}

const existing: ExistingPatientKey[] = [
  { id: 'p-legado', legacyId: 10, cpf: '11111111111', searchName: 'maria', birthDate: '1970-01-01' },
];

describe('classifyCandidates', () => {
  it('marca já importado pelo Seq, mesmo com dados diferentes', () => {
    const [row] = classifyCandidates([candidate(2, 10, { searchName: 'outra pessoa' })], existing);
    expect(row).toMatchObject({ status: 'ja_importado', matches: [], notes: [] });
  });

  it('aponta possível duplicado por CPF e por nome + nascimento de cadastros existentes', () => {
    const rows = classifyCandidates(
      [candidate(2, 20, { cpf: '11111111111', searchName: 'm silva' }), candidate(3, 21, { searchName: 'maria', birthDate: '1970-01-01' })],
      existing,
    );
    expect(rows.map((r) => [r.status, r.matches])).toEqual([
      ['possivel_duplicado', [{ reason: 'CPF', patientId: 'p-legado' }]],
      ['possivel_duplicado', [{ reason: 'nome e nascimento', patientId: 'p-legado' }]],
    ]);
  });

  it('não repete o mesmo cadastro quando CPF e nome coincidem', () => {
    const [row] = classifyCandidates(
      [candidate(2, 20, { cpf: '11111111111', searchName: 'maria', birthDate: '1970-01-01' })],
      existing,
    );
    expect(row.matches).toEqual([{ reason: 'CPF', patientId: 'p-legado' }]);
  });

  it('aponta repetições dentro da planilha e rejeita Seq repetido', () => {
    const rows = classifyCandidates(
      [
        candidate(2, 30, { cpf: '22222222222', searchName: 'joao' }),
        candidate(3, 31, { cpf: '22222222222', searchName: 'jose' }),
        candidate(4, 30, { searchName: 'pedro' }),
      ],
      [],
    );
    expect(rows.map((r) => r.status)).toEqual(['novo', 'possivel_duplicado', 'rejeitado']);
    expect(rows[1].matches).toEqual([{ reason: 'CPF', rowNumber: 2 }]);
    expect(rows[2].notes).toEqual(['Seq 30 repetido na planilha (linha 2).']);
  });

  it('linha rejeitada no mapeamento não serve de referência para outras', () => {
    const rows = classifyCandidates([candidate(2, null, null), candidate(3, 40, { searchName: 'x' })], []);
    expect(rows.map((r) => r.status)).toEqual(['rejeitado', 'novo']);
  });
});
```

`apps/api/test/import.e2e-spec.ts`:

```ts
import { INestApplication } from '@nestjs/common';
import { PrismaService } from '../src/prisma/prisma.service';
import { createTestApp } from './helpers/app';
import { Agent, loginAs } from './helpers/auth';
import { resetDb } from './helpers/db';
import { buildXlsx, CLIENTES_HEADER, clientesRow, FixtureCell } from './helpers/xlsx-fixture';

describe('Importação de cadastros (HTTP)', () => {
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

  async function upload(agent: Agent, rows: FixtureCell[][], header: FixtureCell[] = CLIENTES_HEADER) {
    const buffer = await buildXlsx([
      { name: 'Início', rows: [['capa']] },
      { name: 'Clientes', rows: [header, ...rows] },
    ]);
    return agent.post('/api/imports').attach('file', buffer, 'Bioimpedancia.xlsm');
  }

  it('gera prévia classificando novos, rejeitados, Seq repetido e possíveis duplicados, sem gravar pacientes', async () => {
    const { agent, user } = await loginAs(app, 'ADMIN');
    await prisma.patient.create({
      data: {
        fullName: 'Carla Existente', searchName: 'carla existente', sex: 'FEMININO',
        birthDate: new Date('1990-01-01T00:00:00Z'), cpf: '98765432100', createdById: user.id, updatedById: user.id,
      },
    });

    const res = await upload(agent, [
      clientesRow({ seq: 1, nome: 'Ana Nova', sexo: 'Feminino', nascimento: 29351, cpf: '11122233344' }),
      clientesRow({ seq: 2, nome: 'Sem Nascimento', sexo: 'Masculino' }),
      clientesRow({ seq: 1, nome: 'Ana Repetida', sexo: 'Feminino', nascimento: 29351 }),
      clientesRow({ seq: 4, nome: 'Carla Outra Grafia', sexo: 'Feminino', nascimento: 32874, cpf: '987.654.321-00' }),
      clientesRow({ seq: 5, nome: 'ANA  NOVA', sexo: 'Feminino', nascimento: 29351 }),
    ]);

    expect(res.status).toBe(201);
    expect(res.body.status).toBe('PREVIEW');
    expect(res.body.counts).toEqual({ novo: 1, ja_importado: 0, possivel_duplicado: 2, rejeitado: 2 });
    const byRow = Object.fromEntries(res.body.rows.map((r: { rowNumber: number }) => [r.rowNumber, r]));
    expect(byRow[2].status).toBe('novo');
    expect(byRow[3]).toMatchObject({ status: 'rejeitado', errors: ['Nascimento ausente ou inválido.'] });
    expect(byRow[4]).toMatchObject({ status: 'rejeitado', notes: ['Seq 1 repetido na planilha (linha 2).'] });
    expect(byRow[5].matches).toEqual([{ reason: 'CPF', patientId: expect.any(String) }]);
    expect(byRow[6].matches).toEqual([{ reason: 'nome e nascimento', rowNumber: 2 }]);
    expect(await prisma.patient.count()).toBe(1);
    expect(await prisma.auditEvent.count({ where: { action: 'import.preview' } })).toBe(1);
  });

  it('confirma novos e duplicados forçados; reenviar a mesma planilha não duplica', async () => {
    const { agent } = await loginAs(app, 'ADMIN');
    const rows = [
      clientesRow({ seq: 10, nome: 'Bruno Lima', sexo: 'Masculino', nascimento: 29351 }),
      clientesRow({ seq: 11, nome: 'Bruno  Lima', sexo: 'Masculino', nascimento: 29351 }),
      clientesRow({ seq: 12, nome: 'Célia Souza', sexo: 'Feminino', nascimento: '02/03/1975', cep: 1310100 }),
    ];
    const preview = await upload(agent, rows);

    const commit = await agent.post(`/api/imports/${preview.body.id}/commit`).send({ forceLegacyIds: [11] });

    expect(commit.status).toBe(200);
    expect(commit.body.status).toBe('COMMITTED');
    expect(commit.body.report).toMatchObject({ imported: 3, alreadyImported: 0, duplicatesSkipped: 0, rejected: 0 });
    const celia = await prisma.patient.findUniqueOrThrow({ where: { legacyId: 12 } });
    expect(celia).toMatchObject({ fullName: 'Célia Souza', searchName: 'celia souza', postalCode: '01310100', version: 1 });
    expect(celia.birthDate.toISOString().slice(0, 10)).toBe('1975-03-02');
    expect(await prisma.auditEvent.count({ where: { action: 'patient.create' } })).toBe(3);

    const again = await upload(agent, rows);
    expect(again.body.counts).toEqual({ novo: 0, ja_importado: 3, possivel_duplicado: 0, rejeitado: 0 });
  });

  it('não importa possível duplicado sem confirmação explícita', async () => {
    const { agent } = await loginAs(app, 'ADMIN');
    const preview = await upload(agent, [
      clientesRow({ seq: 20, nome: 'Dora Reis', sexo: 'Feminino', nascimento: 29351 }),
      clientesRow({ seq: 21, nome: 'Dora Reis', sexo: 'Feminino', nascimento: 29351 }),
    ]);

    const commit = await agent.post(`/api/imports/${preview.body.id}/commit`).send({});

    expect(commit.body.report).toMatchObject({ imported: 1, duplicatesSkipped: 1 });
    expect(await prisma.patient.count()).toBe(1);
  });

  it('não permite confirmar a mesma prévia duas vezes', async () => {
    const { agent } = await loginAs(app, 'ADMIN');
    const preview = await upload(agent, [clientesRow({ seq: 30, nome: 'Eva', sexo: 'Feminino', nascimento: 29351 })]);

    await agent.post(`/api/imports/${preview.body.id}/commit`).send({}).expect(200);
    const second = await agent.post(`/api/imports/${preview.body.id}/commit`).send({});

    expect(second.status).toBe(409);
    expect(second.body.message).toBe('Esta importação já foi confirmada.');
  });

  it('recusa confirmar prévia com mais de 24 horas', async () => {
    const { agent } = await loginAs(app, 'ADMIN');
    const preview = await upload(agent, [clientesRow({ seq: 40, nome: 'Fabi', sexo: 'Feminino', nascimento: 29351 })]);
    await prisma.importBatch.update({ where: { id: preview.body.id }, data: { createdAt: new Date(Date.now() - 25 * 3_600_000) } });

    const commit = await agent.post(`/api/imports/${preview.body.id}/commit`).send({});

    expect(commit.status).toBe(409);
    expect(commit.body.message).toBe('A prévia expirou. Envie a planilha novamente.');
    expect(await prisma.patient.count()).toBe(0);
  });

  it('recusa planilha fora do layout sem gravar nada', async () => {
    const { agent } = await loginAs(app, 'ADMIN');
    const header = [...CLIENTES_HEADER];
    [header[18], header[20]] = [header[20], header[18]];

    const res = await upload(agent, [clientesRow({ seq: 1, nome: 'Ana', sexo: 'Feminino', nascimento: 29351 })], header);

    expect(res.status).toBe(400);
    expect(res.body.message).toBe('A aba Clientes não está no layout esperado. Nada foi importado.');
    expect(res.body.errors).toContain('Coluna S: esperado "rg", encontrado "cpf".');
    expect(await prisma.importBatch.count()).toBe(0);
  });

  it('recusa arquivo que não é planilha e extensão não suportada', async () => {
    const { agent } = await loginAs(app, 'ADMIN');

    const notZip = await agent.post('/api/imports').attach('file', Buffer.from('texto'), 'dados.xlsx');
    const wrongExt = await agent.post('/api/imports').attach('file', Buffer.from('texto'), 'dados.csv');

    expect(notZip.status).toBe(400);
    expect(notZip.body.message).toBe('O arquivo não é uma planilha .xlsx ou .xlsm válida.');
    expect(wrongExt.status).toBe(400);
    expect(wrongExt.body.message).toBe('Envie um arquivo .xlsx ou .xlsm.');
  });

  it('exige a permissão de importação', async () => {
    const { agent } = await loginAs(app, 'PROFISSIONAL');
    const res = await upload(agent, [clientesRow({ seq: 1, nome: 'Ana', sexo: 'Feminino', nascimento: 29351 })]);
    expect(res.status).toBe(403);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/api -- classify import`
Expected: FAIL — `Cannot find module './classify'`; `import.e2e-spec` com 404 em `/api/imports`.

- [ ] **Step 3: Implementar**

Acrescentar ao `schema.prisma`:

```prisma
enum ImportStatus {
  PREVIEW
  COMMITTED
}

model ImportBatch {
  id          String       @id @default(uuid()) @db.Uuid
  fileName    String       @map("file_name")
  fileSha256  String       @map("file_sha256")
  status      ImportStatus @default(PREVIEW)
  rows        Json
  report      Json?
  createdAt   DateTime     @default(now()) @map("created_at") @db.Timestamptz(3)
  createdById String       @map("created_by_id") @db.Uuid
  committedAt DateTime?    @map("committed_at") @db.Timestamptz(3)

  @@map("import_batches")
}
```

`apps/api/src/import/classify.ts`:

```ts
import type { CandidateRow, ClassifiedRow, ImportMatch } from './import.types';

export interface ExistingPatientKey {
  id: string;
  legacyId: number | null;
  cpf: string | null;
  searchName: string;
  birthDate: string;
}

/** Duplicidade: pelo Seq (já importado); alerta por CPF ou nome + nascimento, no banco e dentro da própria planilha. */
export function classifyCandidates(candidates: CandidateRow[], existing: ExistingPatientKey[]): ClassifiedRow[] {
  const existingLegacyIds = new Set(existing.flatMap((p) => (p.legacyId === null ? [] : [p.legacyId])));
  const existingByCpf = new Map(existing.flatMap((p) => (p.cpf ? [[p.cpf, p] as const] : [])));
  const existingByNameBirth = new Map(existing.map((p) => [`${p.searchName}|${p.birthDate}`, p] as const));
  const firstRowBySeq = new Map<number, number>();
  const fileRowByCpf = new Map<string, number>();
  const fileRowByNameBirth = new Map<string, number>();

  return candidates.map((candidate) => {
    const row: ClassifiedRow = { ...candidate, status: 'novo', matches: [], notes: [] };

    if (candidate.legacyId !== null) {
      const first = firstRowBySeq.get(candidate.legacyId);
      if (first === undefined) firstRowBySeq.set(candidate.legacyId, candidate.rowNumber);
      else row.notes.push(`Seq ${candidate.legacyId} repetido na planilha (linha ${first}).`);
    }

    const data = candidate.data;
    if (data === null || candidate.legacyId === null || row.notes.length > 0) {
      row.status = 'rejeitado';
      return row;
    }
    if (existingLegacyIds.has(candidate.legacyId)) {
      row.status = 'ja_importado';
      return row;
    }

    const nameBirth = `${data.searchName}|${data.birthDate}`;
    const dbByCpf = data.cpf ? existingByCpf.get(data.cpf) : undefined;
    const dbByName = existingByNameBirth.get(nameBirth);
    const fileByCpf = data.cpf ? fileRowByCpf.get(data.cpf) : undefined;
    const fileByName = fileRowByNameBirth.get(nameBirth);

    const matches: ImportMatch[] = [];
    if (dbByCpf) matches.push({ reason: 'CPF', patientId: dbByCpf.id });
    if (dbByName && dbByName !== dbByCpf) matches.push({ reason: 'nome e nascimento', patientId: dbByName.id });
    if (fileByCpf !== undefined) matches.push({ reason: 'CPF', rowNumber: fileByCpf });
    if (fileByName !== undefined && fileByName !== fileByCpf) matches.push({ reason: 'nome e nascimento', rowNumber: fileByName });

    if (data.cpf && fileByCpf === undefined) fileRowByCpf.set(data.cpf, candidate.rowNumber);
    if (fileByName === undefined) fileRowByNameBirth.set(nameBirth, candidate.rowNumber);

    row.matches = matches;
    row.status = matches.length > 0 ? 'possivel_duplicado' : 'novo';
    return row;
  });
}
```

`apps/api/src/import/import.service.ts`:

```ts
import { createHash } from 'node:crypto';
import { BadRequestException, ConflictException, Inject, Injectable, NotFoundException } from '@nestjs/common';
import type { ImportBatch, Prisma } from '@prisma/client';
import { AuditService } from '../audit/audit.service';
import { formatIsoDate, parseIsoDate, todayInTimeZone } from '../common/civil-date';
import { isUniqueViolation } from '../common/prisma-errors';
import { APP_CONFIG, AppConfig } from '../config';
import { PrismaService } from '../prisma/prisma.service';
import { classifyCandidates, ExistingPatientKey } from './classify';
import { CLIENTES_SHEET, mapClientes } from './clientes-mapper';
import type { CandidateRow, ClassifiedRow, RowStatus } from './import.types';
import { readSheet, SheetRow, XlsxReadError } from './xlsx-reader';

const PREVIEW_TTL_MS = 24 * 60 * 60 * 1000;

export interface UploadedSpreadsheet {
  originalname: string;
  buffer: Buffer;
}

export type ImportCounts = Record<RowStatus, number>;

export interface ImportOutcome {
  rowNumber: number;
  legacyId: number | null;
  outcome: 'importado' | 'ja_importado' | 'duplicado_nao_importado' | 'rejeitado';
  patientId?: string;
}

export interface ImportReport {
  imported: number;
  alreadyImported: number;
  duplicatesSkipped: number;
  rejected: number;
  outcomes: ImportOutcome[];
}

export interface ImportBatchView {
  id: string;
  fileName: string;
  status: 'PREVIEW' | 'COMMITTED';
  createdAt: string;
  committedAt: string | null;
  counts: ImportCounts;
  rows: ClassifiedRow[];
  report: ImportReport | null;
}

const SKIPPED_OUTCOME: Record<Exclude<RowStatus, 'novo'>, ImportOutcome['outcome']> = {
  ja_importado: 'ja_importado',
  possivel_duplicado: 'duplicado_nao_importado',
  rejeitado: 'rejeitado',
};

@Injectable()
export class ImportService {
  constructor(
    private readonly prisma: PrismaService,
    private readonly audit: AuditService,
    @Inject(APP_CONFIG) private readonly config: AppConfig,
  ) {}

  async preview(file: UploadedSpreadsheet, actorId: string): Promise<ImportBatchView> {
    let sheet: SheetRow[];
    try {
      sheet = await readSheet(file.buffer, CLIENTES_SHEET);
    } catch (error) {
      if (error instanceof XlsxReadError) throw new BadRequestException({ message: error.message, errors: [error.message] });
      throw error;
    }
    const mapping = mapClientes(sheet, todayInTimeZone(this.config.clinicTimeZone));
    if (mapping.headerErrors.length > 0) {
      throw new BadRequestException({
        message: 'A aba Clientes não está no layout esperado. Nada foi importado.',
        errors: mapping.headerErrors,
      });
    }
    const rows = classifyCandidates(mapping.candidates, await this.existingKeys(this.prisma));
    const sha256 = createHash('sha256').update(file.buffer).digest('hex');
    const batch = await this.prisma.importBatch.create({
      data: { fileName: file.originalname, fileSha256: sha256, rows: toJson(rows), createdById: actorId },
    });
    await this.audit.record({
      actorId,
      action: 'import.preview',
      entityType: 'import_batch',
      entityId: batch.id,
      data: { fileName: file.originalname, sha256, counts: countStatuses(rows) },
    });
    return toBatchView(batch);
  }

  async get(id: string): Promise<ImportBatchView> {
    const batch = await this.prisma.importBatch.findUnique({ where: { id } });
    if (!batch) throw new NotFoundException('Importação não encontrada.');
    return toBatchView(batch);
  }

  async commit(id: string, forceLegacyIds: number[], actorId: string, now: Date = new Date()): Promise<ImportBatchView> {
    const batch = await this.prisma.importBatch.findUnique({ where: { id } });
    if (!batch) throw new NotFoundException('Importação não encontrada.');
    if (batch.status === 'COMMITTED') throw new ConflictException('Esta importação já foi confirmada.');
    if (now.getTime() - batch.createdAt.getTime() > PREVIEW_TTL_MS) {
      throw new ConflictException('A prévia expirou. Envie a planilha novamente.');
    }
    const force = new Set(forceLegacyIds);

    try {
      const saved = await this.prisma.$transaction(
        async (tx) => {
          const claimed = await tx.importBatch.updateMany({
            where: { id, status: 'PREVIEW' },
            data: { status: 'COMMITTED', committedAt: now },
          });
          if (claimed.count === 0) throw new ConflictException('Esta importação já foi confirmada.');

          const candidates = fromJson<ClassifiedRow[]>(batch.rows).map(toCandidate);
          const rows = classifyCandidates(candidates, await this.existingKeys(tx));
          const outcomes: ImportOutcome[] = [];
          for (const row of rows) {
            const forced = row.status === 'possivel_duplicado' && row.legacyId !== null && force.has(row.legacyId);
            if (row.data && row.legacyId !== null && (row.status === 'novo' || forced)) {
              const patient = await tx.patient.create({
                data: {
                  ...row.data,
                  birthDate: parseIsoDate(row.data.birthDate) as Date,
                  legacyId: row.legacyId,
                  createdById: actorId,
                  updatedById: actorId,
                },
              });
              await this.audit.record(
                {
                  actorId,
                  action: 'patient.create',
                  entityType: 'patient',
                  entityId: patient.id,
                  data: { source: 'import', batchId: id, rowNumber: row.rowNumber, legacyId: row.legacyId },
                },
                tx,
              );
              outcomes.push({ rowNumber: row.rowNumber, legacyId: row.legacyId, outcome: 'importado', patientId: patient.id });
            } else if (row.status !== 'novo') {
              outcomes.push({ rowNumber: row.rowNumber, legacyId: row.legacyId, outcome: SKIPPED_OUTCOME[row.status] });
            }
          }

          const report = buildReport(outcomes);
          const updated = await tx.importBatch.update({ where: { id }, data: { rows: toJson(rows), report: toJson(report) } });
          await this.audit.record(
            {
              actorId,
              action: 'import.commit',
              entityType: 'import_batch',
              entityId: id,
              data: {
                imported: report.imported,
                alreadyImported: report.alreadyImported,
                duplicatesSkipped: report.duplicatesSkipped,
                rejected: report.rejected,
              },
            },
            tx,
          );
          return updated;
        },
        { timeout: 120_000 },
      );
      return toBatchView(saved);
    } catch (error) {
      if (isUniqueViolation(error)) {
        throw new ConflictException('Outra importação gravou parte destes cadastros. Gere uma nova prévia.');
      }
      throw error;
    }
  }

  private async existingKeys(db: Prisma.TransactionClient): Promise<ExistingPatientKey[]> {
    const patients = await db.patient.findMany({
      select: { id: true, legacyId: true, cpf: true, searchName: true, birthDate: true },
    });
    return patients.map((p) => ({ ...p, birthDate: formatIsoDate(p.birthDate) }));
  }
}

function toJson(value: unknown): Prisma.InputJsonValue {
  return JSON.parse(JSON.stringify(value)) as Prisma.InputJsonValue;
}

function fromJson<T>(value: Prisma.JsonValue | null): T {
  return value as unknown as T;
}

function toCandidate(row: ClassifiedRow): CandidateRow {
  return { rowNumber: row.rowNumber, legacyId: row.legacyId, data: row.data, errors: row.errors, warnings: row.warnings };
}

function countStatuses(rows: ClassifiedRow[]): ImportCounts {
  const counts: ImportCounts = { novo: 0, ja_importado: 0, possivel_duplicado: 0, rejeitado: 0 };
  for (const row of rows) counts[row.status] += 1;
  return counts;
}

function buildReport(outcomes: ImportOutcome[]): ImportReport {
  const count = (outcome: ImportOutcome['outcome']) => outcomes.filter((o) => o.outcome === outcome).length;
  return {
    imported: count('importado'),
    alreadyImported: count('ja_importado'),
    duplicatesSkipped: count('duplicado_nao_importado'),
    rejected: count('rejeitado'),
    outcomes,
  };
}

function toBatchView(batch: ImportBatch): ImportBatchView {
  const rows = fromJson<ClassifiedRow[]>(batch.rows);
  return {
    id: batch.id,
    fileName: batch.fileName,
    status: batch.status,
    createdAt: batch.createdAt.toISOString(),
    committedAt: batch.committedAt?.toISOString() ?? null,
    counts: countStatuses(rows),
    rows,
    report: fromJson<ImportReport | null>(batch.report),
  };
}
```

`apps/api/src/import/dto/commit-import.dto.ts`:

```ts
import { ArrayMaxSize, IsArray, IsInt, IsOptional } from 'class-validator';

export class CommitImportDto {
  @IsOptional()
  @IsArray()
  @ArrayMaxSize(10000)
  @IsInt({ each: true })
  forceLegacyIds?: number[];
}
```

`apps/api/src/import/import.controller.ts`:

```ts
import {
  BadRequestException,
  Body,
  Controller,
  Get,
  HttpCode,
  Param,
  ParseUUIDPipe,
  Post,
  UploadedFile,
  UseInterceptors,
} from '@nestjs/common';
import { FileInterceptor } from '@nestjs/platform-express';
import type { AuthContext } from '../auth/auth-context';
import { CurrentAuth } from '../auth/current-auth.decorator';
import { RequirePermissions } from '../auth/require-permissions.decorator';
import { CommitImportDto } from './dto/commit-import.dto';
import { ImportBatchView, ImportService, UploadedSpreadsheet } from './import.service';

const MAX_UPLOAD_BYTES = 25 * 1024 * 1024;

@Controller('imports')
@RequirePermissions('import.run')
export class ImportController {
  constructor(private readonly imports: ImportService) {}

  @Post()
  @UseInterceptors(FileInterceptor('file', { limits: { fileSize: MAX_UPLOAD_BYTES } }))
  preview(@UploadedFile() file: UploadedSpreadsheet | undefined, @CurrentAuth() auth: AuthContext): Promise<ImportBatchView> {
    if (!file) throw new BadRequestException({ message: 'Envie a planilha no campo "file".', errors: ['Arquivo ausente.'] });
    if (!/\.(xlsx|xlsm)$/i.test(file.originalname)) {
      throw new BadRequestException({ message: 'Envie um arquivo .xlsx ou .xlsm.', errors: ['Extensão não suportada.'] });
    }
    return this.imports.preview(file, auth.user.id);
  }

  @Get(':id')
  get(@Param('id', new ParseUUIDPipe()) id: string): Promise<ImportBatchView> {
    return this.imports.get(id);
  }

  @Post(':id/commit')
  @HttpCode(200)
  commit(
    @Param('id', new ParseUUIDPipe()) id: string,
    @Body() body: CommitImportDto,
    @CurrentAuth() auth: AuthContext,
  ): Promise<ImportBatchView> {
    return this.imports.commit(id, body.forceLegacyIds ?? [], auth.user.id);
  }
}
```

`apps/api/src/import/import.module.ts`:

```ts
import { Module } from '@nestjs/common';
import { ImportController } from './import.controller';
import { ImportService } from './import.service';

@Module({ controllers: [ImportController], providers: [ImportService] })
export class ImportModule {}
```

`apps/api/src/app.module.ts` — acrescentar `ImportModule` aos imports (`import { ImportModule } from './import/import.module';`).

```powershell
Set-Location apps/api
npx prisma migrate dev --name import_batches
Set-Location ../..
```

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/api`
Expected: PASS — incluindo `classify.spec` (5) e `import.e2e-spec` (8).

Run: `npm run typecheck -w @psm/api; npm run build -w @psm/api`
Expected: sem erros; `apps/api/dist/main.js` gerado.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(api): importação de cadastros com prévia, duplicidade e confirmação idempotente"
```

---

### Task 9: SPA — esqueleto, identidade visual, login e navegação por permissão

**Files:**
- Create: `apps/web/package.json`, `apps/web/tsconfig.json`, `apps/web/vite.config.ts`, `apps/web/index.html`
- Create: `apps/web/src/main.tsx`, `src/index.css`, `src/router.tsx`
- Create: `apps/web/src/lib/api.ts`, `src/lib/query-client.ts`
- Create: `apps/web/src/auth/me.ts`, `src/auth/current-user.tsx`, `src/auth/RequireAuth.tsx`, `src/auth/RequirePermission.tsx`
- Create: `apps/web/src/layout/nav.ts`, `src/layout/AppLayout.tsx`
- Create: `apps/web/src/components/ui/Button.tsx`, `Input.tsx`, `Select.tsx`, `Textarea.tsx`, `Field.tsx`, `Alert.tsx`, `PageTitle.tsx`
- Create: `apps/web/src/pages/LoginPage.tsx`, `src/pages/HomePage.tsx`
- Create: `apps/web/src/test/setup.ts`, `src/test/mock-api.ts`, `src/test/render.tsx`, `src/test/fixtures.ts`
- Test: `apps/web/src/lib/api.test.ts`, `src/pages/login.test.tsx`, `src/layout/layout.test.tsx`

**Interfaces:**
- Consumes: `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me` (Task 4).
- Produces: `api<T>(path, { method?, json?, formData? }): Promise<T>`; `ApiError { status; body: { message; errors?; current? } }`; `fieldErrors(error): Record<string, string>`; `createQueryClient(): QueryClient` (401 em qualquer consulta zera a sessão); `Me`, `Role`, `ME_KEY`, `useMe()`, `can(me, permission)`, `ROLE_LABELS`; `useCurrentUser(): Me`; `RequireAuth`; `RequirePermission({ permission, children })`; `NAV_ITEMS`; componentes `Button` (+ `buttonClass(variant)`), `Input`, `Select`, `Textarea`, `Field({ label, error?, required?, children })`, `Alert({ tone? })`, `PageTitle`; `routes: RouteObject[]`; helpers de teste `mockApi(routes) → { calls }`, `renderApp(path)`, `adminMe`, `professionalMe`, `assistantMe`.

- [ ] **Step 1: Criar o pacote da web e instalar**

`apps/web/package.json`:

```json
{
  "name": "@psm/web",
  "version": "0.1.0",
  "private": true,
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc --noEmit && vite build",
    "test": "vitest run",
    "typecheck": "tsc --noEmit"
  },
  "dependencies": {
    "@fontsource/inter": "^5.1.0",
    "@fontsource/playfair-display": "^5.1.0",
    "@tanstack/react-query": "^5.62.0",
    "react": "^19.0.0",
    "react-dom": "^19.0.0",
    "react-router": "^7.1.0"
  },
  "devDependencies": {
    "@tailwindcss/vite": "^4.0.0",
    "@testing-library/dom": "^10.4.0",
    "@testing-library/jest-dom": "^6.6.3",
    "@testing-library/react": "^16.1.0",
    "@testing-library/user-event": "^14.5.2",
    "@types/node": "^24.0.0",
    "@types/react": "^19.0.0",
    "@types/react-dom": "^19.0.0",
    "@vitejs/plugin-react": "^4.3.4",
    "jsdom": "^25.0.1",
    "tailwindcss": "^4.0.0",
    "typescript": "^5.7.0",
    "vite": "^6.0.0",
    "vitest": "^3.0.0"
  }
}
```

`apps/web/tsconfig.json`:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "lib": ["ES2022", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "moduleResolution": "bundler",
    "jsx": "react-jsx",
    "strict": true,
    "noEmit": true,
    "isolatedModules": true,
    "skipLibCheck": true,
    "types": ["vite/client", "node"]
  },
  "include": ["src", "vite.config.ts"]
}
```

`apps/web/vite.config.ts`:

```ts
/// <reference types="vitest/config" />
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react(), process.env.VITEST ? null : tailwindcss()],
  server: {
    port: 5173,
    proxy: { '/api': 'http://localhost:3000' },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
});
```

`apps/web/index.html`:

```html
<!doctype html>
<html lang="pt-BR">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Sistema PSM</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
```

Run: `npm install`
Expected: instalação sem erros.

- [ ] **Step 2: Criar os utilitários de teste e escrever os testes**

`apps/web/src/test/setup.ts`:

```ts
import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, vi } from 'vitest';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});
```

`apps/web/src/test/mock-api.ts`:

```ts
import { vi } from 'vitest';

export interface MockRequest {
  method: string;
  path: string;
  body: unknown;
}

export interface MockResponse {
  status?: number;
  body?: unknown;
}

type Route = MockResponse | ((request: MockRequest) => MockResponse);

function respond(status: number, body: unknown): Response {
  if (status === 204) return new Response(null, { status });
  return new Response(JSON.stringify(body ?? {}), { status, headers: { 'Content-Type': 'application/json' } });
}

/** Substitui fetch: rotas "MÉTODO /caminho" (com ou sem query string). Sem rota → 418, para não disparar novas tentativas. */
export function mockApi(routes: Record<string, Route>) {
  const calls: MockRequest[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
    const path = url.replace(/^\/api/, '');
    const method = init?.method ?? 'GET';
    const body = typeof init?.body === 'string' ? JSON.parse(init.body) : (init?.body ?? null);
    const request = { method, path, body };
    calls.push(request);
    const route = routes[`${method} ${path}`] ?? routes[`${method} ${path.split('?')[0]}`];
    if (!route) return respond(418, { message: `Sem mock para ${method} ${path}` });
    const result = typeof route === 'function' ? route(request) : route;
    return respond(result.status ?? 200, result.body);
  });
  vi.stubGlobal('fetch', fetchMock);
  return { calls, fetchMock };
}
```

`apps/web/src/test/render.tsx`:

```tsx
import { QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { createQueryClient } from '../lib/query-client';
import { routes } from '../router';

export function renderApp(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  const queryClient = createQueryClient();
  const utils = render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
  return { ...utils, router, queryClient };
}
```

`apps/web/src/test/fixtures.ts`:

```ts
import type { Me } from '../auth/me';

const ALL = [
  'patients.read', 'patients.write', 'anamneses.draft', 'anamneses.finalize', 'assessments.draft',
  'assessments.finalize', 'ocr.submit', 'history.read', 'documents.read', 'documents.issue',
  'users.manage', 'permissions.manage', 'audit.read', 'import.run',
];

export const adminMe: Me = { id: 'u-admin', username: 'admin', displayName: 'Ana Admin', role: 'ADMIN', permissions: ALL };

export const professionalMe: Me = {
  id: 'u-prof',
  username: 'prof',
  displayName: 'Paula Prof',
  role: 'PROFISSIONAL',
  permissions: ALL.slice(0, 10),
};

export const assistantMe: Me = {
  id: 'u-assist',
  username: 'assist',
  displayName: 'Artur Assistente',
  role: 'ASSISTENTE',
  permissions: ['patients.read', 'history.read'],
};
```

`apps/web/src/lib/api.test.ts`:

```ts
import { describe, expect, it } from 'vitest';
import { mockApi } from '../test/mock-api';
import { api, ApiError, fieldErrors } from './api';

describe('api', () => {
  it('envia JSON para /api e devolve o corpo', async () => {
    const { calls } = mockApi({ 'POST /coisa': { status: 201, body: { ok: true } } });
    await expect(api('/coisa', { method: 'POST', json: { a: 1 } })).resolves.toEqual({ ok: true });
    expect(calls[0]).toEqual({ method: 'POST', path: '/coisa', body: { a: 1 } });
  });

  it('devolve undefined em 204', async () => {
    mockApi({ 'POST /sair': { status: 204 } });
    await expect(api('/sair', { method: 'POST' })).resolves.toBeUndefined();
  });

  it('transforma erro em ApiError com erros por campo', async () => {
    mockApi({ 'POST /x': { status: 400, body: { message: 'Dados inválidos.', errors: { cpf: 'O CPF deve ter 11 dígitos.' } } } });

    const error = await api('/x', { method: 'POST', json: {} }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(400);
    expect((error as ApiError).message).toBe('Dados inválidos.');
    expect(fieldErrors(error)).toEqual({ cpf: 'O CPF deve ter 11 dígitos.' });
  });

  it('junta mensagens de validação que chegam em lista', async () => {
    mockApi({ 'POST /y': { status: 400, body: { message: ['a deve ser texto', 'b é obrigatório'] } } });
    await expect(api('/y', { method: 'POST', json: {} })).rejects.toThrow('a deve ser texto b é obrigatório');
  });
});
```

`apps/web/src/pages/login.test.tsx`:

```tsx
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { adminMe } from '../test/fixtures';
import { mockApi } from '../test/mock-api';
import { renderApp } from '../test/render';

describe('Login', () => {
  it('leva ao login quando não há sessão', async () => {
    mockApi({ 'GET /auth/me': { status: 401, body: { message: 'Sessão ausente ou expirada.' } } });
    renderApp('/');
    expect(await screen.findByRole('button', { name: 'Entrar' })).toBeInTheDocument();
  });

  it('entra e mostra o início com as seções permitidas', async () => {
    let loggedIn = false;
    const { calls } = mockApi({
      'GET /auth/me': () => (loggedIn ? { body: adminMe } : { status: 401, body: { message: 'Sessão ausente ou expirada.' } }),
      'POST /auth/login': () => {
        loggedIn = true;
        return { body: adminMe };
      },
    });
    const user = userEvent.setup();
    renderApp('/');

    await user.type(await screen.findByLabelText('Usuário'), 'admin');
    await user.type(screen.getByLabelText('Senha'), 'senha-segura-123');
    await user.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(await screen.findByRole('heading', { name: 'Olá, Ana Admin' })).toBeInTheDocument();
    expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ username: 'admin', password: 'senha-segura-123' });
  });

  it('mostra a mensagem da API quando a senha está errada', async () => {
    mockApi({
      'GET /auth/me': { status: 401, body: { message: 'Sessão ausente ou expirada.' } },
      'POST /auth/login': { status: 401, body: { statusCode: 401, message: 'Usuário ou senha inválidos.' } },
    });
    const user = userEvent.setup();
    renderApp('/login');

    await user.type(await screen.findByLabelText('Usuário'), 'admin');
    await user.type(screen.getByLabelText('Senha'), 'errada-errada');
    await user.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Usuário ou senha inválidos.');
  });
});
```

`apps/web/src/layout/layout.test.tsx`:

```tsx
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { adminMe, assistantMe } from '../test/fixtures';
import { mockApi } from '../test/mock-api';
import { renderApp } from '../test/render';

describe('Layout', () => {
  it('mostra só as seções permitidas ao perfil', async () => {
    mockApi({ 'GET /auth/me': { body: assistantMe } });
    renderApp('/');

    expect(await screen.findByRole('heading', { name: 'Olá, Artur Assistente' })).toBeInTheDocument();
    const nav = screen.getByRole('navigation', { name: 'Principal' });
    expect(within(nav).getByRole('link', { name: 'Pacientes' })).toBeInTheDocument();
    expect(within(nav).queryByRole('link', { name: 'Usuários' })).not.toBeInTheDocument();
    expect(screen.getByText('Artur Assistente · Assistente')).toBeInTheDocument();
  });

  it('sai do sistema e volta ao login', async () => {
    const { calls } = mockApi({ 'GET /auth/me': { body: adminMe }, 'POST /auth/logout': { status: 204 } });
    const user = userEvent.setup();
    renderApp('/');

    await user.click(await screen.findByRole('button', { name: 'Sair' }));

    expect(await screen.findByRole('button', { name: 'Entrar' })).toBeInTheDocument();
    expect(calls.some((c) => c.method === 'POST' && c.path === '/auth/logout')).toBe(true);
  });
});
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `npm test -w @psm/web`
Expected: FAIL — `Failed to resolve import "../lib/query-client"`, `"./api"` e `"../router"`.

- [ ] **Step 4: Implementar**

`apps/web/src/index.css`:

```css
@import "tailwindcss";

@theme {
  --color-alabastro: #fdfbf7;
  --color-grafite: #1f1a17;
  --color-terracota: #7a2b22;
  --color-ouro: #c5a059;
  --color-oliva: #8a9a70;
  --color-areia: #e2dcd3;
  --font-sans: "Inter", ui-sans-serif, system-ui, sans-serif;
  --font-display: "Playfair Display", ui-serif, Georgia, serif;
}

@layer base {
  body {
    @apply bg-alabastro font-sans text-grafite antialiased;
  }
}
```

`apps/web/src/lib/api.ts`:

```ts
export interface ApiErrorBody {
  message: string;
  errors?: Record<string, string> | string[];
  current?: unknown;
}

export class ApiError extends Error {
  readonly status: number;
  readonly body: ApiErrorBody;

  constructor(status: number, body: ApiErrorBody) {
    super(body.message);
    this.name = 'ApiError';
    this.status = status;
    this.body = body;
  }
}

interface RequestOptions {
  method?: string;
  json?: unknown;
  formData?: FormData;
}

export async function api<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  let body: BodyInit | undefined;
  if (options.json !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(options.json);
  } else if (options.formData) {
    body = options.formData;
  }
  const response = await fetch(`/api${path}`, { method: options.method ?? 'GET', headers, body, credentials: 'same-origin' });
  if (response.status === 204) return undefined as T;
  const data = parseJson(await response.text());
  if (!response.ok) throw new ApiError(response.status, toErrorBody(response.status, data));
  return data as T;
}

export function fieldErrors(error: unknown): Record<string, string> {
  if (error instanceof ApiError && error.body.errors && !Array.isArray(error.body.errors)) return error.body.errors;
  return {};
}

function parseJson(text: string): unknown {
  if (!text) return undefined;
  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

function toErrorBody(status: number, data: unknown): ApiErrorBody {
  const raw = (typeof data === 'object' && data !== null ? data : {}) as { message?: unknown; errors?: unknown; current?: unknown };
  const message = Array.isArray(raw.message)
    ? raw.message.join(' ')
    : typeof raw.message === 'string'
      ? raw.message
      : `Erro ${status}.`;
  return { message, errors: raw.errors as ApiErrorBody['errors'], current: raw.current };
}
```

`apps/web/src/auth/me.ts`:

```ts
import { useQuery } from '@tanstack/react-query';
import { api, ApiError } from '../lib/api';

export type Role = 'ADMIN' | 'PROFISSIONAL' | 'ASSISTENTE';

export interface Me {
  id: string;
  username: string;
  displayName: string;
  role: Role;
  permissions: string[];
}

export const ME_KEY = ['me'] as const;

export const ROLE_LABELS: Record<Role, string> = {
  ADMIN: 'Administrador',
  PROFISSIONAL: 'Profissional de saúde',
  ASSISTENTE: 'Assistente',
};

async function fetchMe(): Promise<Me | null> {
  try {
    return await api<Me>('/auth/me');
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) return null;
    throw error;
  }
}

export function useMe() {
  return useQuery({ queryKey: ME_KEY, queryFn: fetchMe, staleTime: 60_000 });
}

export function can(me: Me | null | undefined, permission: string): boolean {
  return me?.permissions.includes(permission) ?? false;
}
```

`apps/web/src/lib/query-client.ts`:

```ts
import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';
import { ME_KEY } from '../auth/me';
import { ApiError } from './api';

/** 401 em qualquer consulta ou mutação encerra a sessão local; o RequireAuth leva ao login. */
export function createQueryClient(): QueryClient {
  const client: QueryClient = new QueryClient({
    queryCache: new QueryCache({ onError: (error) => handleUnauthorized(client, error) }),
    mutationCache: new MutationCache({ onError: (error) => handleUnauthorized(client, error) }),
    defaultOptions: {
      queries: {
        retry: (failureCount, error) => !(error instanceof ApiError && error.status < 500) && failureCount < 2,
      },
    },
  });
  return client;
}

function handleUnauthorized(client: QueryClient, error: unknown): void {
  if (error instanceof ApiError && error.status === 401) client.setQueryData(ME_KEY, null);
}
```

`apps/web/src/auth/current-user.tsx`:

```tsx
import { createContext, useContext, type ReactNode } from 'react';
import type { Me } from './me';

const CurrentUserContext = createContext<Me | null>(null);

export function CurrentUserProvider({ value, children }: { value: Me; children: ReactNode }) {
  return <CurrentUserContext.Provider value={value}>{children}</CurrentUserContext.Provider>;
}

export function useCurrentUser(): Me {
  const me = useContext(CurrentUserContext);
  if (!me) throw new Error('useCurrentUser usado fora de RequireAuth.');
  return me;
}
```

`apps/web/src/components/ui/Button.tsx`:

```tsx
import type { ButtonHTMLAttributes } from 'react';

type Variant = 'primary' | 'secondary' | 'danger';

const VARIANTS: Record<Variant, string> = {
  primary: 'bg-terracota text-alabastro hover:bg-terracota/90',
  secondary: 'border border-areia bg-white text-grafite hover:border-ouro',
  danger: 'border border-terracota bg-white text-terracota hover:bg-terracota/5',
};

export function buttonClass(variant: Variant = 'primary'): string {
  return `inline-flex items-center justify-center rounded-md px-4 py-2 text-sm font-semibold focus:outline-none focus-visible:ring-2 focus-visible:ring-ouro disabled:cursor-not-allowed disabled:opacity-60 ${VARIANTS[variant]}`;
}

export function Button({
  variant = 'primary',
  className = '',
  type = 'button',
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant }) {
  return <button type={type} className={`${buttonClass(variant)} ${className}`} {...props} />;
}
```

`apps/web/src/components/ui/Input.tsx`:

```tsx
import type { InputHTMLAttributes } from 'react';

export const controlClass =
  'w-full rounded-md border border-areia bg-white px-3 py-2 text-sm text-grafite focus:border-ouro focus:outline-none focus:ring-2 focus:ring-ouro/40 aria-[invalid=true]:border-terracota';

export function Input({ className = '', ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={`${controlClass} ${className}`} {...props} />;
}
```

`apps/web/src/components/ui/Select.tsx`:

```tsx
import type { SelectHTMLAttributes } from 'react';
import { controlClass } from './Input';

export function Select({ className = '', ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={`${controlClass} ${className}`} {...props} />;
}
```

`apps/web/src/components/ui/Textarea.tsx`:

```tsx
import type { TextareaHTMLAttributes } from 'react';
import { controlClass } from './Input';

export function Textarea({ className = '', ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={`${controlClass} ${className}`} {...props} />;
}
```

`apps/web/src/components/ui/Field.tsx`:

```tsx
import { cloneElement, useId, type ReactElement } from 'react';

interface FieldProps {
  label: string;
  error?: string;
  required?: boolean;
  children: ReactElement<{ id?: string; 'aria-invalid'?: boolean; 'aria-describedby'?: string }>;
}

export function Field({ label, error, required, children }: FieldProps) {
  const id = useId();
  const errorId = `${id}-erro`;
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="text-sm font-semibold">
        {required ? `${label} *` : label}
      </label>
      {cloneElement(children, {
        id,
        'aria-invalid': error ? true : undefined,
        'aria-describedby': error ? errorId : undefined,
      })}
      {error ? (
        <p id={errorId} className="text-sm text-terracota">
          {error}
        </p>
      ) : null}
    </div>
  );
}
```

`apps/web/src/components/ui/Alert.tsx`:

```tsx
import type { ReactNode } from 'react';

type Tone = 'error' | 'info' | 'success';

const TONES: Record<Tone, string> = {
  error: 'border-terracota bg-terracota/5 text-terracota',
  info: 'border-areia bg-white text-grafite',
  success: 'border-oliva bg-oliva/10 text-grafite',
};

export function Alert({ children, tone = 'error' }: { children: ReactNode; tone?: Tone }) {
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={`rounded-md border px-4 py-3 text-sm ${TONES[tone]}`}>
      {children}
    </div>
  );
}
```

`apps/web/src/components/ui/PageTitle.tsx`:

```tsx
import type { ReactNode } from 'react';

export function PageTitle({ children }: { children: ReactNode }) {
  return <h1 className="font-display text-2xl text-terracota">{children}</h1>;
}
```

`apps/web/src/layout/nav.ts`:

```ts
export interface NavItem {
  to: string;
  label: string;
  description: string;
  permission: string;
}

export const NAV_ITEMS: NavItem[] = [
  { to: '/pacientes', label: 'Pacientes', description: 'Buscar, cadastrar e editar pacientes.', permission: 'patients.read' },
  { to: '/admin/importacao', label: 'Importação', description: 'Trazer os cadastros da planilha antiga.', permission: 'import.run' },
  { to: '/admin/usuarios', label: 'Usuários', description: 'Criar contas, desativar e redefinir senhas.', permission: 'users.manage' },
  { to: '/admin/permissoes', label: 'Permissões', description: 'Definir o que o assistente pode fazer.', permission: 'permissions.manage' },
  { to: '/admin/auditoria', label: 'Auditoria', description: 'Ver quem fez o quê e quando.', permission: 'audit.read' },
];
```

`apps/web/src/layout/AppLayout.tsx`:

```tsx
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { Link, NavLink, useNavigate } from 'react-router';
import { useCurrentUser } from '../auth/current-user';
import { can, ME_KEY, ROLE_LABELS } from '../auth/me';
import { Button } from '../components/ui/Button';
import { api } from '../lib/api';
import { NAV_ITEMS } from './nav';

export function AppLayout({ children }: { children: ReactNode }) {
  const me = useCurrentUser();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const logout = useMutation({
    mutationFn: () => api<void>('/auth/logout', { method: 'POST' }),
    onSettled: () => {
      queryClient.removeQueries({ predicate: (query) => query.queryKey[0] !== ME_KEY[0] });
      queryClient.setQueryData(ME_KEY, null);
      navigate('/login', { replace: true });
    },
  });

  return (
    <div className="min-h-screen">
      <header className="border-b border-areia bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-6 px-4 py-3">
          <Link to="/" className="font-display text-xl text-terracota">
            Sistema PSM
          </Link>
          <nav aria-label="Principal" className="flex flex-1 flex-wrap gap-4 text-sm">
            {NAV_ITEMS.filter((item) => can(me, item.permission)).map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) => (isActive ? 'font-semibold text-terracota' : 'hover:text-terracota')}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
          <span className="text-sm">{`${me.displayName} · ${ROLE_LABELS[me.role]}`}</span>
          <Button variant="secondary" onClick={() => logout.mutate()} disabled={logout.isPending}>
            Sair
          </Button>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">{children}</main>
    </div>
  );
}
```

`apps/web/src/auth/RequireAuth.tsx`:

```tsx
import { Navigate, Outlet, useLocation } from 'react-router';
import { Alert } from '../components/ui/Alert';
import { AppLayout } from '../layout/AppLayout';
import { CurrentUserProvider } from './current-user';
import { useMe } from './me';

export function RequireAuth() {
  const { data: me, isPending, isError } = useMe();
  const location = useLocation();

  if (isPending) return <p className="p-8 text-sm">Carregando…</p>;
  if (isError) {
    return (
      <div className="p-8">
        <Alert>Não foi possível verificar a sessão. Tente recarregar a página.</Alert>
      </div>
    );
  }
  if (!me) return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;

  return (
    <CurrentUserProvider value={me}>
      <AppLayout>
        <Outlet />
      </AppLayout>
    </CurrentUserProvider>
  );
}
```

`apps/web/src/auth/RequirePermission.tsx`:

```tsx
import type { ReactNode } from 'react';
import { Alert } from '../components/ui/Alert';
import { useCurrentUser } from './current-user';
import { can } from './me';

export function RequirePermission({ permission, children }: { permission: string; children: ReactNode }) {
  const me = useCurrentUser();
  if (!can(me, permission)) return <Alert>Você não tem permissão para acessar esta página.</Alert>;
  return <>{children}</>;
}
```

`apps/web/src/pages/LoginPage.tsx`:

```tsx
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router';
import { ME_KEY, type Me } from '../auth/me';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Field } from '../components/ui/Field';
import { Input } from '../components/ui/Input';
import { api, ApiError } from '../lib/api';

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');

  const login = useMutation({
    mutationFn: () => api<Me>('/auth/login', { method: 'POST', json: { username, password } }),
    onSuccess: (me) => {
      queryClient.setQueryData(ME_KEY, me);
      const from = (location.state as { from?: string } | null)?.from;
      navigate(from && from !== '/login' ? from : '/', { replace: true });
    },
  });

  return (
    <main className="flex min-h-screen items-center justify-center px-4">
      <form
        className="w-full max-w-sm space-y-4 rounded-lg border border-areia bg-white p-8"
        onSubmit={(event) => {
          event.preventDefault();
          login.mutate();
        }}
      >
        <h1 className="font-display text-3xl text-terracota">Sistema PSM</h1>
        <Field label="Usuário">
          <Input autoComplete="username" value={username} onChange={(e) => setUsername(e.target.value)} required />
        </Field>
        <Field label="Senha">
          <Input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </Field>
        {login.error ? (
          <Alert>{login.error instanceof ApiError ? login.error.message : 'Não foi possível entrar. Verifique a conexão.'}</Alert>
        ) : null}
        <Button type="submit" className="w-full" disabled={login.isPending}>
          {login.isPending ? 'Entrando…' : 'Entrar'}
        </Button>
      </form>
    </main>
  );
}
```

`apps/web/src/pages/HomePage.tsx`:

```tsx
import { Link } from 'react-router';
import { useCurrentUser } from '../auth/current-user';
import { can } from '../auth/me';
import { PageTitle } from '../components/ui/PageTitle';
import { NAV_ITEMS } from '../layout/nav';

export function HomePage() {
  const me = useCurrentUser();
  return (
    <section>
      <PageTitle>{`Olá, ${me.displayName}`}</PageTitle>
      <ul className="mt-6 grid gap-4 sm:grid-cols-2">
        {NAV_ITEMS.filter((item) => can(me, item.permission)).map((item) => (
          <li key={item.to}>
            <Link to={item.to} className="block rounded-lg border border-areia bg-white p-4 hover:border-ouro">
              <span className="font-display text-lg text-terracota">{item.label}</span>
              <p className="mt-1 text-sm">{item.description}</p>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
```

`apps/web/src/router.tsx`:

```tsx
import { Navigate, type RouteObject } from 'react-router';
import { RequireAuth } from './auth/RequireAuth';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';

export const routes: RouteObject[] = [
  { path: '/login', element: <LoginPage /> },
  {
    path: '/',
    element: <RequireAuth />,
    children: [{ index: true, element: <HomePage /> }],
  },
  { path: '*', element: <Navigate to="/" replace /> },
];
```

`apps/web/src/main.tsx`:

```tsx
import '@fontsource/inter/400.css';
import '@fontsource/inter/600.css';
import '@fontsource/playfair-display/600.css';
import './index.css';
import { QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { createQueryClient } from './lib/query-client';
import { routes } from './router';

const router = createBrowserRouter(routes);
const queryClient = createQueryClient();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
);
```

- [ ] **Step 5: Rodar e ver passar**

Run: `npm test -w @psm/web`
Expected: PASS — `api.test` (4), `login.test` (3), `layout.test` (2).

Run: `npm run build -w @psm/web`
Expected: `tsc` sem erros e `apps/web/dist/` gerado.

Conferência visual rápida (com a API da Task 8 rodando: `npm run dev -w @psm/api` em outro terminal):

Run: `npm run dev -w @psm/web` e abrir `http://localhost:5173`
Expected: tela de login em alabastro, título em Playfair terracota; entrar com o administrador criado na Task 2 mostra "Olá, Administrador" e os cinco cartões.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat(web): SPA com login, sessão e navegação por permissão na identidade do laudo"
```

---

### Task 10: SPA — pacientes (lista, busca, cadastro, edição e conflito)

**Files:**
- Create: `apps/web/src/patients/types.ts`, `src/lib/format.ts`, `src/patients/PatientsListPage.tsx`, `src/patients/PatientFormPage.tsx`
- Modify: `apps/web/src/router.tsx`
- Test: `apps/web/src/lib/format.test.ts`, `apps/web/src/patients/patients.test.tsx`

**Interfaces:**
- Consumes: `GET/POST /api/patients`, `GET/PUT /api/patients/:id` (Task 6); `api`, `ApiError`, `fieldErrors`, `useCurrentUser`, `can`, componentes de UI.
- Produces: `Patient`, `PatientListItem`, `PatientPage`, `Sex`, `SEX_LABELS`, `UFS`; `formatDateBr(iso)`, `formatCpf(cpf)`, `formatDateTimeBr(iso)`; `PatientsListPage`; `PatientFormRoute` (remonta o formulário por id); rotas `/pacientes`, `/pacientes/novo`, `/pacientes/:id`.

- [ ] **Step 1: Escrever os testes**

`apps/web/src/lib/format.test.ts`:

```ts
import { describe, expect, it } from 'vitest';
import { formatCpf, formatDateBr, formatDateTimeBr } from './format';

describe('formatação', () => {
  it('data civil em dd/mm/aaaa sem mudar o dia por fuso', () => {
    expect(formatDateBr('1980-05-10')).toBe('10/05/1980');
    expect(formatDateBr(null)).toBe('—');
  });

  it('CPF com máscara só quando tem 11 dígitos', () => {
    expect(formatCpf('01234567890')).toBe('012.345.678-90');
    expect(formatCpf(null)).toBe('—');
  });

  it('instante em horário da clínica', () => {
    expect(formatDateTimeBr('2026-10-01T15:30:00.000Z')).toMatch(/01\/10\/2026.*12:30/);
  });
});
```

`apps/web/src/patients/patients.test.tsx`:

```tsx
import { fireEvent, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { assistantMe, professionalMe } from '../test/fixtures';
import { mockApi } from '../test/mock-api';
import { renderApp } from '../test/render';
import type { Patient } from './types';

const patient: Patient = {
  id: 'p1', legacyId: 12, fullName: 'João da Silva', sex: 'MASCULINO', birthDate: '1980-05-10',
  profession: null, phone: null, mobile: null, email: null, notes: null, postalCode: null, street: null,
  streetNumber: null, addressComplement: null, district: null, city: null, state: null, rg: null,
  cpf: '12345678901', otherDocument: null, version: 1,
  createdAt: '2026-10-01T12:00:00.000Z', updatedAt: '2026-10-01T12:00:00.000Z',
};

const page = {
  items: [{ id: 'p1', legacyId: 12, fullName: 'João da Silva', sex: 'MASCULINO', birthDate: '1980-05-10', cpf: '12345678901' }],
  total: 1,
  page: 1,
  pageSize: 20,
};

async function fillRequired(user: ReturnType<typeof userEvent.setup>) {
  await user.type(await screen.findByLabelText('Nome *'), 'João da Silva');
  await user.selectOptions(screen.getByLabelText('Sexo *'), 'MASCULINO');
  fireEvent.change(screen.getByLabelText('Nascimento *'), { target: { value: '1980-05-10' } });
}

describe('Pacientes', () => {
  it('lista pacientes e busca pelo termo digitado', async () => {
    const { calls } = mockApi({ 'GET /auth/me': { body: professionalMe }, 'GET /patients': { body: page } });
    const user = userEvent.setup();
    renderApp('/pacientes');

    expect(await screen.findByRole('link', { name: 'João da Silva' })).toHaveAttribute('href', '/pacientes/p1');
    expect(screen.getByText('10/05/1980')).toBeInTheDocument();
    expect(screen.getByText('123.456.789-01')).toBeInTheDocument();

    await user.type(screen.getByLabelText('Buscar por nome, CPF ou código'), 'joão');
    await user.click(screen.getByRole('button', { name: 'Buscar' }));

    await waitFor(() => expect(calls.some((c) => c.path.includes('q=jo%C3%A3o'))).toBe(true));
  });

  it('não oferece cadastro a quem só consulta', async () => {
    mockApi({ 'GET /auth/me': { body: assistantMe }, 'GET /patients': { body: { items: [], total: 0, page: 1, pageSize: 20 } } });
    renderApp('/pacientes');

    expect(await screen.findByText('Nenhum paciente encontrado.')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Novo paciente' })).not.toBeInTheDocument();
  });

  it('cadastra paciente enviando campos vazios como nulos', async () => {
    const { calls } = mockApi({ 'GET /auth/me': { body: professionalMe }, 'POST /patients': { status: 201, body: patient } });
    const user = userEvent.setup();
    renderApp('/pacientes/novo');

    await fillRequired(user);
    await user.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByText('Cadastro salvo.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'João da Silva' })).toBeInTheDocument();
    expect(calls.find((c) => c.method === 'POST')?.body).toMatchObject({
      fullName: 'João da Silva',
      sex: 'MASCULINO',
      birthDate: '1980-05-10',
      cpf: null,
      email: null,
    });
  });

  it('mostra no campo o erro devolvido pela API', async () => {
    mockApi({
      'GET /auth/me': { body: professionalMe },
      'POST /patients': { status: 400, body: { message: 'Dados inválidos.', errors: { cpf: 'O CPF deve ter 11 dígitos.' } } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/novo');

    await fillRequired(user);
    await user.type(screen.getByLabelText('CPF'), '123');
    await user.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByText('O CPF deve ter 11 dígitos.')).toBeInTheDocument();
    expect(screen.getByLabelText('CPF')).toHaveAttribute('aria-invalid', 'true');
  });

  it('avisa quando outra pessoa alterou o cadastro e carrega a versão atual', async () => {
    const current = { ...patient, fullName: 'João Silva Atualizado', version: 2 };
    const { calls } = mockApi({
      'GET /auth/me': { body: professionalMe },
      'GET /patients/p1': { body: patient },
      'PUT /patients/p1': ({ body }) =>
        (body as { version: number }).version === 1
          ? { status: 409, body: { message: 'Este cadastro foi alterado por outra pessoa. Recarregue para ver a versão atual.', current } }
          : { body: { ...current, version: 3 } },
    });
    const user = userEvent.setup();
    renderApp('/pacientes/p1');

    const name = await screen.findByLabelText('Nome *');
    await user.clear(name);
    await user.type(name, 'Meu Nome');
    await user.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(await screen.findByText(/alterado por outra pessoa/)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Carregar versão atual' }));
    expect(screen.getByLabelText('Nome *')).toHaveValue('João Silva Atualizado');

    await user.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(await screen.findByText('Cadastro salvo.')).toBeInTheDocument();
    expect(calls.filter((c) => c.method === 'PUT').map((c) => (c.body as { version: number }).version)).toEqual([1, 2]);
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/web -- format patients`
Expected: FAIL — `Failed to resolve import "./format"` e `"./types"`.

- [ ] **Step 3: Implementar**

`apps/web/src/patients/types.ts`:

```ts
export type Sex = 'FEMININO' | 'MASCULINO';

export interface Patient {
  id: string;
  legacyId: number | null;
  fullName: string;
  sex: Sex;
  birthDate: string;
  profession: string | null;
  phone: string | null;
  mobile: string | null;
  email: string | null;
  notes: string | null;
  postalCode: string | null;
  street: string | null;
  streetNumber: string | null;
  addressComplement: string | null;
  district: string | null;
  city: string | null;
  state: string | null;
  rg: string | null;
  cpf: string | null;
  otherDocument: string | null;
  version: number;
  createdAt: string;
  updatedAt: string;
}

export type PatientListItem = Pick<Patient, 'id' | 'legacyId' | 'fullName' | 'sex' | 'birthDate' | 'cpf'>;

export interface PatientPage {
  items: PatientListItem[];
  total: number;
  page: number;
  pageSize: number;
}

export const SEX_LABELS: Record<Sex, string> = { FEMININO: 'Feminino', MASCULINO: 'Masculino' };

export const UFS = [
  'AC', 'AL', 'AP', 'AM', 'BA', 'CE', 'DF', 'ES', 'GO', 'MA', 'MT', 'MS', 'MG', 'PA',
  'PB', 'PR', 'PE', 'PI', 'RJ', 'RN', 'RS', 'RO', 'RR', 'SC', 'SP', 'SE', 'TO',
] as const;
```

`apps/web/src/lib/format.ts`:

```ts
const CLINIC_TIME_ZONE = 'America/Sao_Paulo';

/** Data civil (AAAA-MM-DD) em dd/mm/aaaa, sem passar por Date para não deslocar o dia. */
export function formatDateBr(iso: string | null): string {
  if (!iso) return '—';
  const [year, month, day] = iso.slice(0, 10).split('-');
  return `${day}/${month}/${year}`;
}

export function formatCpf(cpf: string | null): string {
  if (!cpf) return '—';
  if (cpf.length !== 11) return cpf;
  return `${cpf.slice(0, 3)}.${cpf.slice(3, 6)}.${cpf.slice(6, 9)}-${cpf.slice(9)}`;
}

export function formatDateTimeBr(iso: string): string {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short', timeZone: CLINIC_TIME_ZONE }).format(new Date(iso));
}
```

`apps/web/src/patients/PatientsListPage.tsx`:

```tsx
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Link } from 'react-router';
import { useCurrentUser } from '../auth/current-user';
import { can } from '../auth/me';
import { Alert } from '../components/ui/Alert';
import { Button, buttonClass } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { PageTitle } from '../components/ui/PageTitle';
import { api } from '../lib/api';
import { formatCpf, formatDateBr } from '../lib/format';
import { SEX_LABELS, type PatientPage } from './types';

const PAGE_SIZE = 20;

export function PatientsListPage() {
  const me = useCurrentUser();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [page, setPage] = useState(1);

  const patients = useQuery({
    queryKey: ['patients', query, page],
    queryFn: () => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) });
      if (query) params.set('q', query);
      return api<PatientPage>(`/patients?${params.toString()}`);
    },
  });
  const lastPage = patients.data ? Math.max(1, Math.ceil(patients.data.total / patients.data.pageSize)) : 1;

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <PageTitle>Pacientes</PageTitle>
        {can(me, 'patients.write') ? (
          <Link to="/pacientes/novo" className={buttonClass('primary')}>
            Novo paciente
          </Link>
        ) : null}
      </div>

      <form
        role="search"
        className="flex gap-2"
        onSubmit={(event) => {
          event.preventDefault();
          setPage(1);
          setQuery(search.trim());
        }}
      >
        <Input
          aria-label="Buscar por nome, CPF ou código"
          placeholder="Nome, CPF ou código da planilha"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Button type="submit" variant="secondary">
          Buscar
        </Button>
      </form>

      {patients.isPending ? <p className="text-sm">Carregando…</p> : null}
      {patients.error ? <Alert>{patients.error.message}</Alert> : null}
      {patients.data && patients.data.items.length === 0 ? <Alert tone="info">Nenhum paciente encontrado.</Alert> : null}

      {patients.data && patients.data.items.length > 0 ? (
        <table className="w-full border-collapse bg-white text-sm">
          <thead>
            <tr className="border-b border-areia text-left">
              <th className="p-2">Nome</th>
              <th className="p-2">Nascimento</th>
              <th className="p-2">Sexo</th>
              <th className="p-2">CPF</th>
              <th className="p-2">Código da planilha</th>
            </tr>
          </thead>
          <tbody>
            {patients.data.items.map((p) => (
              <tr key={p.id} className="border-b border-areia/60">
                <td className="p-2">
                  <Link className="text-terracota hover:underline" to={`/pacientes/${p.id}`}>
                    {p.fullName}
                  </Link>
                </td>
                <td className="p-2">{formatDateBr(p.birthDate)}</td>
                <td className="p-2">{SEX_LABELS[p.sex]}</td>
                <td className="p-2">{formatCpf(p.cpf)}</td>
                <td className="p-2">{p.legacyId ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}

      {lastPage > 1 ? (
        <nav aria-label="Paginação" className="flex items-center gap-2 text-sm">
          <Button variant="secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>
            Anterior
          </Button>
          <span>{`Página ${page} de ${lastPage}`}</span>
          <Button variant="secondary" disabled={page >= lastPage} onClick={() => setPage(page + 1)}>
            Próxima
          </Button>
        </nav>
      ) : null}
    </section>
  );
}
```

`apps/web/src/patients/PatientFormPage.tsx`:

```tsx
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState, type InputHTMLAttributes } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router';
import { useCurrentUser } from '../auth/current-user';
import { can } from '../auth/me';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Field } from '../components/ui/Field';
import { Input } from '../components/ui/Input';
import { PageTitle } from '../components/ui/PageTitle';
import { Select } from '../components/ui/Select';
import { Textarea } from '../components/ui/Textarea';
import { api, ApiError, fieldErrors } from '../lib/api';
import { SEX_LABELS, UFS, type Patient } from './types';

const FIELD_NAMES = [
  'fullName', 'sex', 'birthDate', 'cpf', 'rg', 'otherDocument', 'profession', 'phone', 'mobile', 'email',
  'postalCode', 'street', 'streetNumber', 'addressComplement', 'district', 'city', 'state', 'notes',
] as const;
type FieldName = (typeof FIELD_NAMES)[number];
type FormValues = Record<FieldName, string>;

const REQUIRED: readonly FieldName[] = ['fullName', 'sex', 'birthDate'];
const EMPTY = Object.fromEntries(FIELD_NAMES.map((name) => [name, ''])) as FormValues;

function toForm(patient: Patient): FormValues {
  return Object.fromEntries(FIELD_NAMES.map((name) => [name, patient[name] ?? ''])) as FormValues;
}

function toPayload(values: FormValues): Record<FieldName, string | null> {
  return Object.fromEntries(
    FIELD_NAMES.map((name) => {
      const value = values[name].trim();
      return [name, value === '' && !REQUIRED.includes(name) ? null : value];
    }),
  ) as Record<FieldName, string | null>;
}

/** Remonta o formulário quando muda o paciente (ou de "novo" para o cadastro salvo). */
export function PatientFormRoute() {
  const { id } = useParams();
  return <PatientFormPage key={id ?? 'novo'} />;
}

function PatientFormPage() {
  const { id } = useParams();
  const isNew = id === undefined;
  const me = useCurrentUser();
  const canWrite = can(me, 'patients.write');
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const [values, setValues] = useState<FormValues>(EMPTY);
  const [version, setVersion] = useState<number | null>(null);
  const [conflict, setConflict] = useState<Patient | null>(null);
  const [saved, setSaved] = useState(Boolean((location.state as { saved?: boolean } | null)?.saved));

  const patient = useQuery({
    queryKey: ['patient', id],
    queryFn: () => api<Patient>(`/patients/${id}`),
    enabled: !isNew,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  useEffect(() => {
    if (patient.data) {
      setValues(toForm(patient.data));
      setVersion(patient.data.version);
    }
  }, [patient.data]);

  const save = useMutation({
    mutationFn: () => {
      const payload = toPayload(values);
      return isNew
        ? api<Patient>('/patients', { method: 'POST', json: payload })
        : api<Patient>(`/patients/${id}`, { method: 'PUT', json: { ...payload, version } });
    },
    onSuccess: (result) => {
      setConflict(null);
      queryClient.setQueryData(['patient', result.id], result);
      void queryClient.invalidateQueries({ queryKey: ['patients'] });
      if (isNew) navigate(`/pacientes/${result.id}`, { replace: true, state: { saved: true } });
      else setSaved(true);
    },
    onError: (error) => {
      setSaved(false);
      if (error instanceof ApiError && error.status === 409 && error.body.current) setConflict(error.body.current as Patient);
    },
  });

  function set(name: FieldName, value: string) {
    setValues((current) => ({ ...current, [name]: value }));
    setSaved(false);
  }

  function loadCurrentVersion() {
    if (!conflict) return;
    queryClient.setQueryData(['patient', conflict.id], conflict);
    setConflict(null);
    save.reset();
  }

  if (!isNew && patient.isPending) return <p className="text-sm">Carregando…</p>;
  if (!isNew && patient.error) return <Alert>{patient.error.message}</Alert>;

  const errors = fieldErrors(save.error);
  const generalError = save.error && !conflict ? save.error.message : null;

  const textField = (name: FieldName, label: string, props: InputHTMLAttributes<HTMLInputElement> = {}) => (
    <Field label={label} error={errors[name]} required={REQUIRED.includes(name)}>
      <Input name={name} value={values[name]} onChange={(e) => set(name, e.target.value)} required={REQUIRED.includes(name)} {...props} />
    </Field>
  );

  return (
    <section className="space-y-4">
      <PageTitle>{isNew ? 'Novo paciente' : (patient.data?.fullName ?? 'Paciente')}</PageTitle>
      {patient.data?.legacyId ? <p className="text-sm">{`Código na planilha: ${patient.data.legacyId}`}</p> : null}
      {saved ? <Alert tone="success">Cadastro salvo.</Alert> : null}
      {conflict ? (
        <Alert>
          <p>{save.error?.message}</p>
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
        <fieldset disabled={!canWrite || save.isPending} className="grid gap-4 sm:grid-cols-2">
          {textField('fullName', 'Nome', { maxLength: 200 })}
          <Field label="Sexo" error={errors.sex} required>
            <Select name="sex" value={values.sex} onChange={(e) => set('sex', e.target.value)} required>
              <option value="">Selecione</option>
              <option value="FEMININO">{SEX_LABELS.FEMININO}</option>
              <option value="MASCULINO">{SEX_LABELS.MASCULINO}</option>
            </Select>
          </Field>
          {textField('birthDate', 'Nascimento', { type: 'date' })}
          {textField('cpf', 'CPF', { inputMode: 'numeric' })}
          {textField('rg', 'RG')}
          {textField('otherDocument', 'Outro documento')}
          {textField('profession', 'Profissão')}
          {textField('phone', 'Telefone', { type: 'tel' })}
          {textField('mobile', 'Celular', { type: 'tel' })}
          {textField('email', 'E-mail', { type: 'email' })}
          {textField('postalCode', 'CEP', { inputMode: 'numeric' })}
          {textField('street', 'Logradouro')}
          {textField('streetNumber', 'Número')}
          {textField('addressComplement', 'Complemento')}
          {textField('district', 'Bairro')}
          {textField('city', 'Município')}
          <Field label="UF" error={errors.state}>
            <Select name="state" value={values.state} onChange={(e) => set('state', e.target.value)}>
              <option value="">—</option>
              {UFS.map((uf) => (
                <option key={uf} value={uf}>
                  {uf}
                </option>
              ))}
            </Select>
          </Field>
          <div className="sm:col-span-2">
            <Field label="Observações" error={errors.notes}>
              <Textarea name="notes" rows={4} value={values.notes} onChange={(e) => set('notes', e.target.value)} />
            </Field>
          </div>
        </fieldset>
        {canWrite ? (
          <Button type="submit" disabled={save.isPending}>
            {save.isPending ? 'Salvando…' : 'Salvar'}
          </Button>
        ) : (
          <Alert tone="info">Você pode consultar este cadastro, mas não editá-lo.</Alert>
        )}
      </form>
    </section>
  );
}
```

`apps/web/src/router.tsx` — os filhos da rota `/` passam a ser:

```tsx
    children: [
      { index: true, element: <HomePage /> },
      { path: 'pacientes', element: <PatientsListPage /> },
      { path: 'pacientes/novo', element: <PatientFormRoute /> },
      { path: 'pacientes/:id', element: <PatientFormRoute /> },
    ],
```

com `import { PatientFormRoute } from './patients/PatientFormPage';` e `import { PatientsListPage } from './patients/PatientsListPage';`.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/web`
Expected: PASS — incluindo `format.test` (3) e `patients.test` (5).

Run: `npm run typecheck -w @psm/web`
Expected: sem erros.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(web): lista, busca e formulário de pacientes com aviso de edição concorrente"
```

---

### Task 11: SPA — administração (usuários, permissões do assistente, auditoria)

**Files:**
- Create: `apps/web/src/admin/UsersPage.tsx`, `src/admin/AssistantPermissionsPage.tsx`, `src/admin/AuditPage.tsx`
- Modify: `apps/web/src/router.tsx`
- Test: `apps/web/src/admin/admin.test.tsx`

**Interfaces:**
- Consumes: `GET/POST /api/users`, `PATCH /api/users/:id`, `POST /api/users/:id/password`, `GET/PUT /api/permissions/assistant`, `GET /api/audit` (Task 5); `RequirePermission`, `ROLE_LABELS`, `formatDateTimeBr`, componentes de UI.
- Produces: `UsersPage`, `AssistantPermissionsPage`, `AuditPage`; rotas `/admin/usuarios`, `/admin/permissoes`, `/admin/auditoria`.

- [ ] **Step 1: Escrever os testes**

`apps/web/src/admin/admin.test.tsx`:

```tsx
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { adminMe, professionalMe } from '../test/fixtures';
import { mockApi } from '../test/mock-api';
import { renderApp } from '../test/render';

describe('Usuários', () => {
  it('cria usuário e desativa outro', async () => {
    const paula = { id: 'u1', username: 'paula', displayName: 'Paula Prof', role: 'PROFISSIONAL', active: true, createdAt: '2026-10-01T12:00:00.000Z' };
    const { calls } = mockApi({
      'GET /auth/me': { body: adminMe },
      'GET /users': { body: [paula] },
      'POST /users': ({ body }) => ({ status: 201, body: { id: 'u2', ...(body as object), active: true, createdAt: paula.createdAt } }),
      'PATCH /users/u1': { body: { ...paula, active: false } },
    });
    const user = userEvent.setup();
    renderApp('/admin/usuarios');

    await user.type(await screen.findByLabelText('Usuário *'), 'artur');
    await user.type(screen.getByLabelText('Nome de exibição *'), 'Artur');
    await user.selectOptions(screen.getByLabelText('Perfil *'), 'ASSISTENTE');
    await user.type(screen.getByLabelText('Senha inicial *'), 'senha-segura-123');
    await user.click(screen.getByRole('button', { name: 'Criar usuário' }));

    expect(await screen.findByText('Usuário artur criado.')).toBeInTheDocument();
    expect(calls.find((c) => c.method === 'POST')?.body).toEqual({
      username: 'artur',
      displayName: 'Artur',
      role: 'ASSISTENTE',
      password: 'senha-segura-123',
    });

    await user.click(await screen.findByRole('button', { name: 'Desativar paula' }));
    await waitFor(() =>
      expect(calls.some((c) => c.method === 'PATCH' && (c.body as { active?: boolean }).active === false)).toBe(true),
    );
  });
});

describe('Permissões do assistente', () => {
  const rows = [
    { permission: 'patients.read', label: 'Consultar cadastros de pacientes', reservedByDefault: false, granted: true },
    { permission: 'documents.issue', label: 'Emitir documentos clínicos', reservedByDefault: true, granted: false },
  ];

  it('concede uma permissão reservada e salva todas', async () => {
    const { calls } = mockApi({
      'GET /auth/me': { body: adminMe },
      'GET /permissions/assistant': { body: rows },
      'PUT /permissions/assistant': ({ body }) => {
        const changes = (body as { changes: { permission: string; granted: boolean }[] }).changes;
        return { body: rows.map((r) => ({ ...r, granted: changes.find((c) => c.permission === r.permission)?.granted ?? r.granted })) };
      },
    });
    const user = userEvent.setup();
    renderApp('/admin/permissoes');

    const box = await screen.findByRole('checkbox', { name: /Emitir documentos clínicos/ });
    expect(box).not.toBeChecked();
    expect(screen.getByText('Reservado ao profissional por padrão')).toBeInTheDocument();
    await user.click(box);
    await user.click(screen.getByRole('button', { name: 'Salvar permissões' }));

    expect(await screen.findByText('Permissões salvas.')).toBeInTheDocument();
    expect(calls.find((c) => c.method === 'PUT')?.body).toEqual({
      changes: [
        { permission: 'patients.read', granted: true },
        { permission: 'documents.issue', granted: true },
      ],
    });
  });

  it('bloqueia a página para quem não gerencia permissões', async () => {
    mockApi({ 'GET /auth/me': { body: professionalMe } });
    renderApp('/admin/permissoes');
    expect(await screen.findByText('Você não tem permissão para acessar esta página.')).toBeInTheDocument();
  });
});

describe('Auditoria', () => {
  it('lista eventos com autor, ação legível e horário da clínica', async () => {
    mockApi({
      'GET /auth/me': { body: adminMe },
      'GET /audit': {
        body: [
          {
            id: 7,
            occurredAt: '2026-10-01T15:30:00.000Z',
            actorId: 'u-admin',
            actorName: 'Ana Admin',
            action: 'permission.grant',
            entityType: 'assistant_permission',
            entityId: 'documents.issue',
            data: null,
          },
        ],
      },
    });
    renderApp('/admin/auditoria');

    expect(await screen.findByText('Permissão concedida ao assistente')).toBeInTheDocument();
    expect(screen.getByText('Ana Admin')).toBeInTheDocument();
    expect(screen.getByText(/01\/10\/2026.*12:30/)).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/web -- admin`
Expected: FAIL — as rotas `/admin/*` caem no redirecionamento para o início; os campos não são encontrados.

- [ ] **Step 3: Implementar**

`apps/web/src/admin/UsersPage.tsx`:

```tsx
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { ROLE_LABELS, type Role } from '../auth/me';
import { RequirePermission } from '../auth/RequirePermission';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Field } from '../components/ui/Field';
import { Input } from '../components/ui/Input';
import { PageTitle } from '../components/ui/PageTitle';
import { Select } from '../components/ui/Select';
import { api } from '../lib/api';

interface UserRow {
  id: string;
  username: string;
  displayName: string;
  role: Role;
  active: boolean;
  createdAt: string;
}

const ROLES: Role[] = ['ADMIN', 'PROFISSIONAL', 'ASSISTENTE'];
const EMPTY_FORM: { username: string; displayName: string; role: Role; password: string } = {
  username: '',
  displayName: '',
  role: 'ASSISTENTE',
  password: '',
};

export function UsersPage() {
  return (
    <RequirePermission permission="users.manage">
      <UsersContent />
    </RequirePermission>
  );
}

function UsersContent() {
  const queryClient = useQueryClient();
  const users = useQuery({ queryKey: ['users'], queryFn: () => api<UserRow[]>('/users') });
  const [form, setForm] = useState(EMPTY_FORM);
  const [message, setMessage] = useState<string | null>(null);
  const [resetFor, setResetFor] = useState<UserRow | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const refresh = () => queryClient.invalidateQueries({ queryKey: ['users'] });

  const create = useMutation({
    mutationFn: () => api<UserRow>('/users', { method: 'POST', json: form }),
    onSuccess: (user) => {
      setMessage(`Usuário ${user.username} criado.`);
      setForm(EMPTY_FORM);
      void refresh();
    },
  });
  const update = useMutation({
    mutationFn: ({ id, changes }: { id: string; changes: Partial<Pick<UserRow, 'role' | 'active'>> }) =>
      api<UserRow>(`/users/${id}`, { method: 'PATCH', json: changes }),
    onSuccess: (user) => {
      setMessage(`Usuário ${user.username} atualizado.`);
      void refresh();
    },
  });
  const reset = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) =>
      api<void>(`/users/${id}/password`, { method: 'POST', json: { password } }),
    onSuccess: () => {
      setMessage('Senha redefinida. As sessões abertas desse usuário foram encerradas.');
      setResetFor(null);
      setNewPassword('');
    },
  });
  const error = create.error ?? update.error ?? reset.error;

  return (
    <section className="space-y-6">
      <PageTitle>Usuários</PageTitle>
      {message ? <Alert tone="success">{message}</Alert> : null}
      {error ? <Alert>{error.message}</Alert> : null}

      <form
        className="grid gap-4 rounded-lg border border-areia bg-white p-6 sm:grid-cols-2"
        onSubmit={(event) => {
          event.preventDefault();
          setMessage(null);
          create.mutate();
        }}
      >
        <h2 className="font-display text-lg text-terracota sm:col-span-2">Novo usuário</h2>
        <Field label="Usuário" required>
          <Input value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} required autoComplete="off" />
        </Field>
        <Field label="Nome de exibição" required>
          <Input value={form.displayName} onChange={(e) => setForm({ ...form, displayName: e.target.value })} required />
        </Field>
        <Field label="Perfil" required>
          <Select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as Role })} required>
            {ROLES.map((role) => (
              <option key={role} value={role}>
                {ROLE_LABELS[role]}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Senha inicial" required>
          <Input
            type="password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            required
            minLength={10}
            autoComplete="new-password"
          />
        </Field>
        <div className="sm:col-span-2">
          <Button type="submit" disabled={create.isPending}>
            Criar usuário
          </Button>
        </div>
      </form>

      {users.isPending ? <p className="text-sm">Carregando…</p> : null}
      {users.data ? (
        <table className="w-full border-collapse bg-white text-sm">
          <thead>
            <tr className="border-b border-areia text-left">
              <th className="p-2">Usuário</th>
              <th className="p-2">Nome</th>
              <th className="p-2">Perfil</th>
              <th className="p-2">Situação</th>
              <th className="p-2">Ações</th>
            </tr>
          </thead>
          <tbody>
            {users.data.map((u) => (
              <tr key={u.id} className="border-b border-areia/60">
                <td className="p-2">{u.username}</td>
                <td className="p-2">{u.displayName}</td>
                <td className="p-2">
                  <Select
                    aria-label={`Perfil de ${u.username}`}
                    value={u.role}
                    onChange={(e) => update.mutate({ id: u.id, changes: { role: e.target.value as Role } })}
                  >
                    {ROLES.map((role) => (
                      <option key={role} value={role}>
                        {ROLE_LABELS[role]}
                      </option>
                    ))}
                  </Select>
                </td>
                <td className="p-2">{u.active ? 'Ativo' : 'Inativo'}</td>
                <td className="flex flex-wrap gap-2 p-2">
                  <Button
                    variant={u.active ? 'danger' : 'secondary'}
                    aria-label={`${u.active ? 'Desativar' : 'Reativar'} ${u.username}`}
                    onClick={() => update.mutate({ id: u.id, changes: { active: !u.active } })}
                  >
                    {u.active ? 'Desativar' : 'Reativar'}
                  </Button>
                  <Button variant="secondary" aria-label={`Redefinir senha de ${u.username}`} onClick={() => setResetFor(u)}>
                    Redefinir senha
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}

      {resetFor ? (
        <form
          className="flex flex-wrap items-end gap-4 rounded-lg border border-areia bg-white p-6"
          onSubmit={(event) => {
            event.preventDefault();
            reset.mutate({ id: resetFor.id, password: newPassword });
          }}
        >
          <div className="min-w-64 flex-1">
            <Field label={`Nova senha para ${resetFor.username}`} required>
              <Input
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                required
                minLength={10}
                autoComplete="new-password"
              />
            </Field>
          </div>
          <Button type="submit" disabled={reset.isPending}>
            Confirmar nova senha
          </Button>
          <Button variant="secondary" onClick={() => setResetFor(null)}>
            Cancelar
          </Button>
        </form>
      ) : null}
    </section>
  );
}
```

`apps/web/src/admin/AssistantPermissionsPage.tsx`:

```tsx
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { RequirePermission } from '../auth/RequirePermission';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { PageTitle } from '../components/ui/PageTitle';
import { api } from '../lib/api';

interface PermissionRow {
  permission: string;
  label: string;
  reservedByDefault: boolean;
  granted: boolean;
}

const KEY = ['assistant-permissions'];

export function AssistantPermissionsPage() {
  return (
    <RequirePermission permission="permissions.manage">
      <PermissionsContent />
    </RequirePermission>
  );
}

function PermissionsContent() {
  const queryClient = useQueryClient();
  const config = useQuery({ queryKey: KEY, queryFn: () => api<PermissionRow[]>('/permissions/assistant') });
  const [draft, setDraft] = useState<Record<string, boolean>>({});
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (config.data) setDraft(Object.fromEntries(config.data.map((row) => [row.permission, row.granted])));
  }, [config.data]);

  const save = useMutation({
    mutationFn: () =>
      api<PermissionRow[]>('/permissions/assistant', {
        method: 'PUT',
        json: {
          changes: (config.data ?? []).map((row) => ({ permission: row.permission, granted: draft[row.permission] ?? row.granted })),
        },
      }),
    onSuccess: (rows) => {
      queryClient.setQueryData(KEY, rows);
      setSaved(true);
    },
  });

  return (
    <section className="space-y-4">
      <PageTitle>Permissões do assistente</PageTitle>
      <p className="text-sm">
        Administrador e profissional de saúde têm permissões fixas. Aqui você define o que o assistente pode fazer. A mudança vale na
        próxima ação do assistente.
      </p>
      {config.isPending ? <p className="text-sm">Carregando…</p> : null}
      {config.error ? <Alert>{config.error.message}</Alert> : null}
      {config.data ? (
        <ul className="space-y-3 rounded-lg border border-areia bg-white p-6">
          {config.data.map((row) => (
            <li key={row.permission}>
              <label className="flex items-start gap-3 text-sm">
                <input
                  type="checkbox"
                  className="mt-1 accent-terracota"
                  checked={draft[row.permission] ?? row.granted}
                  onChange={(e) => {
                    setSaved(false);
                    setDraft({ ...draft, [row.permission]: e.target.checked });
                  }}
                />
                <span>
                  {row.label}
                  {row.reservedByDefault ? <small className="block text-xs">Reservado ao profissional por padrão</small> : null}
                </span>
              </label>
            </li>
          ))}
        </ul>
      ) : null}
      {save.error ? <Alert>{save.error.message}</Alert> : null}
      {saved ? <Alert tone="success">Permissões salvas.</Alert> : null}
      <Button onClick={() => save.mutate()} disabled={!config.data || save.isPending}>
        Salvar permissões
      </Button>
    </section>
  );
}
```

`apps/web/src/admin/AuditPage.tsx`:

```tsx
import { useQuery } from '@tanstack/react-query';
import { RequirePermission } from '../auth/RequirePermission';
import { Alert } from '../components/ui/Alert';
import { PageTitle } from '../components/ui/PageTitle';
import { api } from '../lib/api';
import { formatDateTimeBr } from '../lib/format';

interface AuditRow {
  id: number;
  occurredAt: string;
  actorId: string | null;
  actorName: string | null;
  action: string;
  entityType: string | null;
  entityId: string | null;
  data: unknown;
}

const ACTION_LABELS: Record<string, string> = {
  'auth.login': 'Entrada no sistema',
  'auth.login_failed': 'Falha de login',
  'auth.login_blocked': 'Login bloqueado',
  'auth.logout': 'Saída do sistema',
  'user.create': 'Usuário criado',
  'user.update': 'Usuário alterado',
  'user.password_reset': 'Senha redefinida',
  'permission.grant': 'Permissão concedida ao assistente',
  'permission.revoke': 'Permissão retirada do assistente',
  'patient.create': 'Paciente cadastrado',
  'patient.update': 'Paciente alterado',
  'import.preview': 'Prévia de importação',
  'import.commit': 'Importação confirmada',
};

export function AuditPage() {
  return (
    <RequirePermission permission="audit.read">
      <AuditContent />
    </RequirePermission>
  );
}

function AuditContent() {
  const events = useQuery({ queryKey: ['audit'], queryFn: () => api<AuditRow[]>('/audit?limit=100') });

  return (
    <section className="space-y-4">
      <PageTitle>Auditoria</PageTitle>
      <p className="text-sm">Últimos 100 eventos, do mais recente para o mais antigo.</p>
      {events.isPending ? <p className="text-sm">Carregando…</p> : null}
      {events.error ? <Alert>{events.error.message}</Alert> : null}
      {events.data ? (
        <table className="w-full border-collapse bg-white text-sm">
          <thead>
            <tr className="border-b border-areia text-left">
              <th className="p-2">Data e hora</th>
              <th className="p-2">Autor</th>
              <th className="p-2">Ação</th>
              <th className="p-2">Registro</th>
              <th className="p-2">Detalhes</th>
            </tr>
          </thead>
          <tbody>
            {events.data.map((event) => (
              <tr key={event.id} className="border-b border-areia/60 align-top">
                <td className="p-2">{formatDateTimeBr(event.occurredAt)}</td>
                <td className="p-2">{event.actorName ?? 'Sistema'}</td>
                <td className="p-2">{ACTION_LABELS[event.action] ?? event.action}</td>
                <td className="p-2">{[event.entityType, event.entityId].filter(Boolean).join(' · ') || '—'}</td>
                <td className="p-2">
                  {event.data ? (
                    <details>
                      <summary className="cursor-pointer">Ver</summary>
                      <pre className="mt-1 whitespace-pre-wrap text-xs">{JSON.stringify(event.data, null, 2)}</pre>
                    </details>
                  ) : (
                    '—'
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </section>
  );
}
```

`apps/web/src/router.tsx` — acrescentar aos filhos de `/`:

```tsx
      { path: 'admin/usuarios', element: <UsersPage /> },
      { path: 'admin/permissoes', element: <AssistantPermissionsPage /> },
      { path: 'admin/auditoria', element: <AuditPage /> },
```

com `import { AssistantPermissionsPage } from './admin/AssistantPermissionsPage';`, `import { AuditPage } from './admin/AuditPage';` e `import { UsersPage } from './admin/UsersPage';`.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/web`
Expected: PASS — incluindo `admin.test` (4).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(web): telas de usuários, permissões do assistente e auditoria"
```

---

### Task 12: SPA — importação de cadastros

**Files:**
- Create: `apps/web/src/admin/import-types.ts`, `src/admin/ImportPage.tsx`
- Modify: `apps/web/src/router.tsx`
- Test: `apps/web/src/admin/import.test.tsx`

**Interfaces:**
- Consumes: `POST /api/imports` (multipart), `POST /api/imports/:id/commit` (Task 8); `api` com `formData`, `ApiError.body.errors` (lista).
- Produces: `ImportPage`; rota `/admin/importacao`.

- [ ] **Step 1: Escrever os testes**

`apps/web/src/admin/import.test.tsx`:

```tsx
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { adminMe } from '../test/fixtures';
import { mockApi } from '../test/mock-api';
import { renderApp } from '../test/render';

const previewBody = {
  id: 'b1',
  fileName: 'Bioimpedancia.xlsm',
  status: 'PREVIEW',
  createdAt: '2026-10-01T12:00:00.000Z',
  committedAt: null,
  counts: { novo: 1, ja_importado: 0, possivel_duplicado: 1, rejeitado: 1 },
  rows: [
    { rowNumber: 2, legacyId: 1, data: { fullName: 'Ana Nova', birthDate: '1980-05-10' }, errors: [], warnings: [], notes: [], status: 'novo', matches: [] },
    { rowNumber: 3, legacyId: 2, data: null, errors: ['Nascimento ausente ou inválido.'], warnings: [], notes: [], status: 'rejeitado', matches: [] },
    {
      rowNumber: 4,
      legacyId: 4,
      data: { fullName: 'Carla', birthDate: '1990-01-01' },
      errors: [],
      warnings: ['CPF gravado como número: zeros à esquerda restaurados.'],
      notes: [],
      status: 'possivel_duplicado',
      matches: [{ reason: 'CPF', patientId: 'p9' }],
    },
  ],
  report: null,
};

async function uploadAndPreview(user: ReturnType<typeof userEvent.setup>) {
  await user.upload(await screen.findByLabelText('Planilha (.xlsx ou .xlsm) *'), new File(['conteúdo'], 'Bioimpedancia.xlsm'));
  await user.click(screen.getByRole('button', { name: 'Gerar prévia' }));
}

describe('Importação', () => {
  it('gera prévia, força um possível duplicado e confirma', async () => {
    const { calls } = mockApi({
      'GET /auth/me': { body: adminMe },
      'POST /imports': { status: 201, body: previewBody },
      'POST /imports/b1/commit': {
        body: {
          ...previewBody,
          status: 'COMMITTED',
          report: { imported: 2, alreadyImported: 0, duplicatesSkipped: 0, rejected: 1, outcomes: [] },
        },
      },
    });
    const user = userEvent.setup();
    renderApp('/admin/importacao');

    await uploadAndPreview(user);

    expect(await screen.findByText('Nascimento ausente ou inválido.')).toBeInTheDocument();
    expect(screen.getByText('Possível duplicado')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'cadastro existente' })).toHaveAttribute('href', '/pacientes/p9');
    await user.click(screen.getByRole('checkbox', { name: 'Importar linha 4 mesmo assim' }));
    await user.click(screen.getByRole('button', { name: 'Confirmar importação' }));

    expect(await screen.findByText(/2 cadastros importados/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Confirmar importação' })).not.toBeInTheDocument();
    expect(calls.find((c) => c.path === '/imports/b1/commit')?.body).toEqual({ forceLegacyIds: [4] });
    expect(calls.find((c) => c.path === '/imports')?.body).toBeInstanceOf(FormData);
  });

  it('lista os problemas de layout da planilha', async () => {
    mockApi({
      'GET /auth/me': { body: adminMe },
      'POST /imports': {
        status: 400,
        body: {
          message: 'A aba Clientes não está no layout esperado. Nada foi importado.',
          errors: ['Coluna S: esperado "rg", encontrado "cpf".'],
        },
      },
    });
    const user = userEvent.setup();
    renderApp('/admin/importacao');

    await uploadAndPreview(user);

    expect(await screen.findByText('A aba Clientes não está no layout esperado. Nada foi importado.')).toBeInTheDocument();
    expect(screen.getByText('Coluna S: esperado "rg", encontrado "cpf".')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `npm test -w @psm/web -- import`
Expected: FAIL — o campo "Planilha (.xlsx ou .xlsm) *" não existe (rota ainda redireciona ao início).

- [ ] **Step 3: Implementar**

`apps/web/src/admin/import-types.ts`:

```ts
export type RowStatus = 'novo' | 'ja_importado' | 'possivel_duplicado' | 'rejeitado';

export interface ImportMatch {
  reason: 'CPF' | 'nome e nascimento';
  patientId?: string;
  rowNumber?: number;
}

export interface ImportRow {
  rowNumber: number;
  legacyId: number | null;
  data: { fullName: string; birthDate: string } | null;
  errors: string[];
  warnings: string[];
  notes: string[];
  status: RowStatus;
  matches: ImportMatch[];
}

export interface ImportReport {
  imported: number;
  alreadyImported: number;
  duplicatesSkipped: number;
  rejected: number;
}

export interface ImportBatch {
  id: string;
  fileName: string;
  status: 'PREVIEW' | 'COMMITTED';
  createdAt: string;
  committedAt: string | null;
  counts: Record<RowStatus, number>;
  rows: ImportRow[];
  report: ImportReport | null;
}

export const STATUS_LABELS: Record<RowStatus, string> = {
  novo: 'Novo',
  ja_importado: 'Já importado',
  possivel_duplicado: 'Possível duplicado',
  rejeitado: 'Rejeitado',
};
```

`apps/web/src/admin/ImportPage.tsx`:

```tsx
import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { Link } from 'react-router';
import { RequirePermission } from '../auth/RequirePermission';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Field } from '../components/ui/Field';
import { Input } from '../components/ui/Input';
import { PageTitle } from '../components/ui/PageTitle';
import { api, ApiError } from '../lib/api';
import { formatDateBr } from '../lib/format';
import { STATUS_LABELS, type ImportBatch, type ImportMatch, type ImportRow } from './import-types';

export function ImportPage() {
  return (
    <RequirePermission permission="import.run">
      <ImportContent />
    </RequirePermission>
  );
}

function MatchDescription({ match }: { match: ImportMatch }) {
  if (match.patientId) {
    return (
      <>
        Possível duplicado de <Link to={`/pacientes/${match.patientId}`} className="text-terracota underline">cadastro existente</Link>
        {` (${match.reason})`}
      </>
    );
  }
  return <>{`Possível duplicado da linha ${match.rowNumber} (${match.reason})`}</>;
}

function ImportContent() {
  const [file, setFile] = useState<File | null>(null);
  const [force, setForce] = useState<number[]>([]);

  const preview = useMutation({
    mutationFn: (selected: File) => {
      const form = new FormData();
      form.append('file', selected);
      return api<ImportBatch>('/imports', { method: 'POST', formData: form });
    },
    onSuccess: () => setForce([]),
  });
  const commit = useMutation({
    mutationFn: (batchId: string) => api<ImportBatch>(`/imports/${batchId}/commit`, { method: 'POST', json: { forceLegacyIds: force } }),
  });

  const batch = commit.data ?? preview.data;
  const committed = batch?.status === 'COMMITTED';
  const layoutErrors = preview.error instanceof ApiError && Array.isArray(preview.error.body.errors) ? preview.error.body.errors : [];

  function toggle(row: ImportRow, checked: boolean) {
    if (row.legacyId === null) return;
    const legacyId = row.legacyId;
    setForce((current) => (checked ? [...current, legacyId] : current.filter((id) => id !== legacyId)));
  }

  return (
    <section className="space-y-6">
      <PageTitle>Importação de cadastros</PageTitle>
      <p className="text-sm">
        Envie o Bioimpedancia.xlsm (ou uma cópia .xlsx). Só a aba Clientes é lida; fotos, anexos, anamneses e avaliações não entram. Nada é
        gravado antes da confirmação, e reenviar a mesma planilha não duplica cadastros.
      </p>

      <form
        className="flex flex-wrap items-end gap-4 rounded-lg border border-areia bg-white p-6"
        onSubmit={(event) => {
          event.preventDefault();
          if (!file) return;
          commit.reset();
          preview.mutate(file);
        }}
      >
        <div className="min-w-72 flex-1">
          <Field label="Planilha (.xlsx ou .xlsm)" required>
            <Input type="file" accept=".xlsx,.xlsm" required onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
          </Field>
        </div>
        <Button type="submit" disabled={!file || preview.isPending}>
          {preview.isPending ? 'Lendo planilha…' : 'Gerar prévia'}
        </Button>
      </form>

      {preview.error ? (
        <Alert>
          <p>{preview.error.message}</p>
          {layoutErrors.length > 0 ? (
            <ul className="mt-2 list-disc pl-5">
              {layoutErrors.map((message) => (
                <li key={message}>{message}</li>
              ))}
            </ul>
          ) : null}
        </Alert>
      ) : null}
      {commit.error ? <Alert>{commit.error.message}</Alert> : null}
      {batch?.report ? (
        <Alert tone="success">
          {`${batch.report.imported} cadastros importados · ${batch.report.alreadyImported} já importados · ${batch.report.duplicatesSkipped} possíveis duplicados não importados · ${batch.report.rejected} rejeitados.`}
        </Alert>
      ) : null}

      {batch ? (
        <div className="space-y-4">
          <p className="text-sm">
            {`${batch.fileName}: ${batch.counts.novo} novos · ${batch.counts.possivel_duplicado} possíveis duplicados · ${batch.counts.rejeitado} rejeitados · ${batch.counts.ja_importado} já importados`}
          </p>
          <table className="w-full border-collapse bg-white text-sm">
            <thead>
              <tr className="border-b border-areia text-left">
                <th className="p-2">Linha</th>
                <th className="p-2">Seq</th>
                <th className="p-2">Nome</th>
                <th className="p-2">Nascimento</th>
                <th className="p-2">Situação</th>
                <th className="p-2">Observações</th>
                <th className="p-2">Importar mesmo assim</th>
              </tr>
            </thead>
            <tbody>
              {batch.rows.map((row) => (
                <tr key={row.rowNumber} className="border-b border-areia/60 align-top">
                  <td className="p-2">{row.rowNumber}</td>
                  <td className="p-2">{row.legacyId ?? '—'}</td>
                  <td className="p-2">{row.data?.fullName ?? '—'}</td>
                  <td className="p-2">{formatDateBr(row.data?.birthDate ?? null)}</td>
                  <td className="p-2">{STATUS_LABELS[row.status]}</td>
                  <td className="p-2">
                    <ul className="space-y-1">
                      {[...row.errors, ...row.notes].map((message) => (
                        <li key={message} className="text-terracota">
                          {message}
                        </li>
                      ))}
                      {row.matches.map((match, index) => (
                        <li key={`match-${index}`}>
                          <MatchDescription match={match} />
                        </li>
                      ))}
                      {row.warnings.map((message) => (
                        <li key={message}>{message}</li>
                      ))}
                    </ul>
                  </td>
                  <td className="p-2">
                    {row.status === 'possivel_duplicado' && row.legacyId !== null && !committed ? (
                      <input
                        type="checkbox"
                        className="accent-terracota"
                        aria-label={`Importar linha ${row.rowNumber} mesmo assim`}
                        checked={force.includes(row.legacyId)}
                        onChange={(e) => toggle(row, e.target.checked)}
                      />
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!committed ? (
            <Button onClick={() => commit.mutate(batch.id)} disabled={commit.isPending}>
              {commit.isPending ? 'Importando…' : 'Confirmar importação'}
            </Button>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}
```

`apps/web/src/router.tsx` — acrescentar aos filhos de `/`:

```tsx
      { path: 'admin/importacao', element: <ImportPage /> },
```

com `import { ImportPage } from './admin/ImportPage';`.

- [ ] **Step 4: Rodar e ver passar**

Run: `npm test -w @psm/web`
Expected: PASS — incluindo `import.test` (2).

Run: `npm run build -w @psm/web`
Expected: sem erros.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(web): importação de cadastros com prévia, duplicados e confirmação"
```

---

### Task 13: Verificação ponta a ponta e README

**Files:**
- Modify: `README.md`

**Interfaces:**
- Consumes: tudo o que as Tasks 1–12 produziram.
- Produces: `README.md` com instruções de execução e o registro da verificação manual do Plano 1.

- [ ] **Step 1: Rodar toda a verificação automática**

Run: `npm test; npm run typecheck; npm run build`
Expected: API e web com todos os testes passando (API: 18 suítes; web: 7 arquivos), `typecheck` sem erros, `apps/api/dist/` e `apps/web/dist/` gerados.

- [ ] **Step 2: Subir o ambiente de desenvolvimento**

```powershell
npm run db:up
Set-Location apps/api
npx prisma migrate deploy
Set-Location ../..
```

Se o administrador da Task 2 não existir mais no banco de desenvolvimento:

```powershell
$env:ADMIN_PASSWORD = "troque-esta-senha-1"
npm run create-admin -w @psm/api -- admin "Administrador"
Remove-Item Env:ADMIN_PASSWORD
```

Em dois terminais: `npm run dev -w @psm/api` e `npm run dev -w @psm/web`. Abrir `http://localhost:5173`.

- [ ] **Step 3: Preparar uma planilha sintética no layout real**

Copiar `C:\Sistema\PSM\Projeto 14\zanoni\Bioimpedancia.xlsm` para uma pasta temporária, abrir a cópia no Excel **com macros desabilitadas**, apagar as linhas de dados da aba Clientes (manter a linha 1) e digitar três pacientes fictícios:

| Seq. | Nome | Sexo | Nascimento | Cpf |
|---|---|---|---|---|
| 1 | Teste Um | Feminino | 10/05/1980 | 012.345.678-90 |
| 2 | Teste Dois | Masculino | (vazio) | |
| 3 | TESTE  UM | Feminino | 10/05/1980 | |

Salvar a cópia. Ela reproduz o layout real sem dados de pacientes.

- [ ] **Step 4: Executar o roteiro manual**

Cada item tem o resultado esperado; anotar OK ou a divergência.

1. Entrar como `admin` → o início mostra 5 cartões (Pacientes, Importação, Usuários, Permissões, Auditoria).
2. Usuários → criar `prof` (Profissional de saúde) e `assist` (Assistente) com senhas de 10+ caracteres → os dois aparecem na lista.
3. Permissões → desmarcar "Cadastrar e editar pacientes" e salvar → "Permissões salvas.".
4. Em janela anônima, entrar como `assist` → Pacientes sem o botão "Novo paciente"; abrir um paciente mostra "Você pode consultar este cadastro, mas não editá-lo.".
5. Como `prof`, cadastrar "Maria Teste" com CPF `123.456.789-01` → "Cadastro salvo."; a lista mostra `123.456.789-01`; a busca `maria  TESTE` encontra.
6. Abrir "Maria Teste" em duas abas como `prof`; salvar uma mudança na primeira e depois outra na segunda → a segunda mostra "Este cadastro foi alterado por outra pessoa…"; "Carregar versão atual" traz o valor da primeira aba.
7. Como `admin`, Importação → enviar a planilha sintética → prévia: linha 2 "Novo", linha 3 "Rejeitado" (nascimento), linha 4 "Possível duplicado" da linha 2. Confirmar sem marcar a linha 4 → "1 cadastros importados · 0 já importados · 1 possíveis duplicados não importados · 1 rejeitados.".
8. Reenviar a mesma planilha → linha 2 "Já importado"; nenhum cadastro novo na lista de pacientes.
9. Pacientes → "Teste Um" com código da planilha 1 e CPF `012.345.678-90`.
10. Sair e errar a senha de `prof` 5 vezes → a 6ª tentativa mostra "Conta bloqueada por tentativas inválidas…".
11. Auditoria → aparecem entrada, falhas de login, bloqueio, criação de usuários, permissão retirada, paciente cadastrado/alterado, prévia e confirmação de importação, com autor e horário de Brasília.

- [ ] **Step 5: Atualizar o README**

Substituir `README.md` por:

````markdown
# psm-web

Aplicação web do Sistema PSM, substituta do `Bioimpedancia.xlsm`. Especificação, divergências e planos ficam no repositório do suplemento: `docs/migracao-web/` e `docs/superpowers/`.

## Requisitos

- Node 24 e npm 10 ou superior
- Docker Desktop

## Primeira execução

```powershell
npm install
npm run db:up
Copy-Item apps/api/.env.example apps/api/.env
Set-Location apps/api; npx prisma migrate deploy; Set-Location ../..
$env:ADMIN_PASSWORD = "<senha com 10 ou mais caracteres>"
npm run create-admin -w @psm/api -- admin "Administrador"
Remove-Item Env:ADMIN_PASSWORD
```

## Rodar

- API: `npm run dev -w @psm/api` — saúde em http://localhost:3000/api/health
- Web: `npm run dev -w @psm/web` — http://localhost:5173 (encaminha `/api` para a API)

## Testes

`npm test` roda a API (Jest, com Postgres de teste em Docker, banco `psm_test`) e a web (Vitest). Os testes usam só dados sintéticos.

## O que o Plano 1 entrega

Login com sessão no servidor, perfis Administrador/Profissional/Assistente com permissões verificadas na API, permissões do assistente configuráveis, auditoria, cadastro de pacientes com detecção de edição concorrente e importação dos cadastros da aba Clientes (prévia, duplicidade e confirmação).

Fora deste plano: anamnese, avaliações e regras clínicas, documentos em PDF, OCR, exclusão de pacientes (política de retenção indefinida) e hospedagem.

## Verificação do Plano 1

Data: <preencher>. Roteiro manual da Task 13, itens 1–11: <OK ou divergências encontradas>.
````

Preencher a data e o resultado do roteiro no último parágrafo.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "docs: instruções de execução e verificação do Plano 1"
```

---

## Fora do escopo deste plano

- Regras clínicas, anamnese, avaliações, documentos e OCR: Planos 2 a 6, liberados pelos portões em `docs/migracao-web/divergencias-e-homologacao.md` §5.
- Exclusão e retenção de cadastros: política ainda não definida; nenhuma rota de exclusão existe.
- Hospedagem, HTTPS, `trust proxy`, backup e importação definitiva com a planilha de produção: Plano 7.
