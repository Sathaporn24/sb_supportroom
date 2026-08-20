# SupportRoom — Local Docker Environment

## Environments

| Environment | Runtime | Database | URLs | Status |
|---|---|---|---|---|
| Local Docker | Docker Compose | PostgreSQL 16 in named volume `postgres-data` | This workstation: Frontend `http://localhost:3001` · API `http://localhost:5138` · health `http://localhost:5138/api/health` | Prepared for local verification; not a deployment |
| Staging / Production | Not selected | Not selected | — | Out of scope until QA FULL, Security and proxy/TLS/logging gates pass |

Local Docker reads provider and bootstrap credentials from the gitignored
`backend/src/SupportRoom.Api/.env`. Compose overrides only the database connection, browser origin
and local storage path. No credential is copied into an image layer.

## Runbook

### First start and normal start

1. Copy `backend/src/SupportRoom.Api/.env.example` to
   `backend/src/SupportRoom.Api/.env` if the real local file does not exist.
2. Optionally copy the repository-root `.env.example` to `.env` to override host ports. This
   workstation uses `LOCAL_FRONTEND_PORT=3001` because port `3000` is already occupied.
3. Set all five provider switches, a `JWT_SECRET` of at least 32 characters, and
   `FIRST_OWNER_EMAIL` / `FIRST_OWNER_PASSWORD`. External provider credentials may remain empty
   only when the corresponding Google Slides, AI or RAG flow will not be exercised.
4. Run `docker compose up --build --detach` from the repository root.
5. Wait for `postgres`, `api` and `frontend` to become healthy with `docker compose ps`.
6. Open `http://localhost:3001/admin/login` on this workstation (or the configured frontend port).
   On a fresh database, the first-owner values create
   one owner account; the first login requires changing that password.
7. As owner, create/select a Company before creating lessons and training links.

The one-shot `migrate` service waits for PostgreSQL, applies every pending EF Core migration, then
must exit with code 0 before the API starts. Re-running Compose is idempotent: EF reports an
up-to-date database and the first-owner seeder does nothing once an account exists.

### Useful checks

```sh
docker compose ps
curl --fail http://localhost:5138/api/health
docker compose logs --tail=100 migrate api frontend
```

Use `docker compose stop` to pause the stack while preserving the PostgreSQL and document-storage
volumes. Use `docker compose down` to remove containers and the network while preserving those
volumes. Do not add `--volumes` unless intentionally discarding all local demo data.

Native frontend checks require Node.js 20 or newer. The host's Node 18 can run lint/typecheck but
cannot start Vitest 4/Rolldown because it lacks `node:util.styleText`; Docker uses Node 22 and the
verified local test run used bundled Node 24.

### Local rollback

Application rollback is rebuild/restart with the previous source revision while keeping a database
backup made before a migration. `SplitLinkAndAddAuth.Down()` is not a data-safe rollback because it
drops `SessionSummary` data and collapses multiple learning rounds into the legacy link shape.
Never use `docker compose down --volumes` as a migration rollback.

## Required Environment Variables

| Group | Keys | Notes |
|---|---|---|
| Provider selection | `SLIDES_PROVIDER`, `TTS_PROVIDER`, `VOICE_QUESTION_PROVIDER`, `KNOWLEDGE_PROVIDER`, `DOCUMENT_STORAGE_PROVIDER` | All five are required for API startup; no mock provider exists |
| App authentication | `JWT_SECRET`; optional `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRY_MINUTES` | `JWT_SECRET` must be at least 32 characters |
| First local owner | `FIRST_OWNER_EMAIL`, `FIRST_OWNER_PASSWORD`; optional `FIRST_OWNER_NAME` | Used only while `AdminUser` is empty |
| Google / AI / RAG | `GOOGLE_SERVICE_ACCOUNT_*`, `GEMINI_*`, `PINECONE_*`; `OPENAI_*` when selected | Required by the provider flows that use them; never put these in frontend variables |
| Local Compose overrides | Optional `LOCAL_POSTGRES_DB`, `LOCAL_POSTGRES_USER`, `LOCAL_POSTGRES_PASSWORD`, `LOCAL_POSTGRES_PORT`, `LOCAL_BACKEND_PORT`, `LOCAL_FRONTEND_PORT` | Defaults are local-only: ports `55432`, `5138`, `3000` |
| Backend local runtime | `POSTGRES_CONNECTION_STRING`, `ALLOWED_ORIGINS`, `LOCAL_STORAGE_PATH` | Compose supplies these; native runs use `.env` |
| Frontend build | `NEXT_PUBLIC_API_BASE_URL` | Compose bakes the browser-reachable local API URL into the standalone build; it is public, never a secret |

## Verification Evidence

- `docker compose config --quiet` passed.
- Backend migration/runtime and frontend standalone images built successfully on Linux ARM64.
- PostgreSQL applied all nine migrations through `20260818155126_AddTotalSlideCount`; a second
  `migrate` run reported that the database was already up to date.
- `postgres`, `api` and `frontend` are healthy. API health and frontend login return HTTP 200;
  CORS returns `Access-Control-Allow-Origin: http://localhost:3001`.
- Fresh-database bootstrap created exactly one owner. Login returned HTTP 200 and
  `mustChangePassword=true`; no credential or access token was printed.
- Browser smoke test rendered the login form without console errors and redirected anonymous
  `/admin` access to `/admin/login`.
- Frontend lint/typecheck passed, Vitest passed 36/36 on Node 24, and the Docker production build
  passed. The runtime image excludes build toolchains and the traced TypeScript package.
- Backend non-integration tests passed 140/140. Container Release publish succeeded with eight
  warnings in existing document-parser/PDF-renderer source; the local API is healthy.

## Local Readiness Gaps

- Manual LS-QA-05 (realtime isolation, resume confirmation, direct-room redirect, React Strict Mode,
  6-case LR-3) is now fully closed as of `review.md` MANUAL-5 (2026-08-19) — see that file's Change
  Log for method and evidence.
- Security audit LS-QA-08 and reverse-proxy/TLS/logging evidence LS-QA-10 remain mandatory before
  any shared or production deployment. This local setup does not claim to clear them.
- UI visual polish and final UX/UI implementation remain with the user's UX/UI team. Preserve this
  reminder when the technical local environment is reported ready.
- Empty external-provider credentials allow the shell, database and non-provider screens to run,
  but Google Slides, transcription, RAG and answer-generation flows will fail until valid local
  credentials are supplied.
- `npm ci` reported four high-severity dependency advisories. They were not auto-fixed because a
  forced upgrade may change application behavior; route them through the explicit Security review.

## Deploy History

- 2026-08-19 — Prepared and rehearsed the project-wide Local Docker environment only; no staging,
  production, shared database or external deployment was changed.
