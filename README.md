# Northwind Support Desk

Agents raise tickets, filter them, escalate them, and sign in. **.NET 10 Web API**, **React 19 / TypeScript**, **SQL Server**, one monorepo.

## What was implemented

The rules below are the ones the code follows.

**Defect.** The ticket list refetched only when the search text, the page, or the page size changed. A status change updated the controls and waited for a later search.

**Filtering.** `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId` are exact matches. `unassignedOnly=true` keeps tickets with no agent. `search` matches title, reference, or customer name, after trim; a blank search is ignored. `slaStatus` uses the same windows as the on-screen status. Every supplied filter is AND. SQL applies them before the count and the page.

**Triage.** Priority is the requested value, or Medium, unless the category's `ForcesCriticalPriority` flag forces Critical. The due date is the moment of creation or escalation plus the configured window. Premium customers get the multiplier, and the result is never shorter than the floor. The owner is the active eligible agent with the fewest open tickets. A tie uses the lowest agent id. The current agent can keep an escalated ticket even when that ticket fills their limit. Anyone else must be strictly below their limit. If nobody qualifies, the ticket is created or left unassigned and the response says why. That is a 201, not an error. Escalation raises priority one step, requires a reason of 5–500 characters, and writes a history row. Critical, Resolved, and Closed tickets cannot be escalated. SLA status is derived. Unresolved tickets with 25% or less of the window left are At risk.

**Authentication.** Agents sign in with the email on `Agent` and a `PasswordHash` on that row. Every seeded agent, including the inactive one, uses `Local_Dev_AgentPassword1`. An unknown email, a wrong password, and an inactive agent all get 401 with "The email or password is incorrect." Every endpoint except login, Swagger, and `GET /health` requires a token. The name on an escalation comes from the token.

**Docker.** `docker compose up --build` builds the API from `apps/api/Dockerfile`, waits until SQL Server is healthy, then migrates and seeds an empty volume. The API listens on HTTP 8080 inside the container and on localhost:5080 on the host.

## Design decisions

Triage lives in `TicketTriage`, and the rules live in `TicketPriorityRules`, `SlaWindow`, and `AgentAssigner`. Create and escalate both call that path. Copying the rules into each handler would let them drift, and switching on category names would break the moment a category was renamed. SLA status is calculated when it is read. Storing it would go stale as the clock moves.

List filtering is a query (`TicketQueries`), separate from the repositories that load a ticket to change it. The list shape, the count, and the page are one SQL query. Doing that work in memory would make `totalCount` wrong and would not survive a larger table.

Passwords sit on `Agent`. A users table would copy the person the rest of the model already calls an agent. Hashing uses ASP.NET Core Identity's `PasswordHasher` (PBKDF2 and a random salt). A column of plaintext, or a hash written by hand, was rejected.

The API uses one fallback authorization policy. Putting `[Authorize]` on each controller would leave the next controller open by accident. Login carries `[AllowAnonymous]`. `GET /health` is mapped once in `Program.cs` with `AllowAnonymous`, beside that policy, so a probe does not need a token and the exception is not scattered across controllers.

The browser keeps the JWT in `localStorage` under `support-desk.session` and sends `Authorization: Bearer`. That survives a refresh and is shared by tabs on this origin. Any script that can run in the page can read it. An httpOnly cookie would not, and it would need the API and the browser to share a site. This client is a separate app that attaches a bearer token. Tokens last 8 hours (`Jwt:LifetimeMinutes` = 480). There is no refresh token.

The container is a multi-stage build: the SDK image restores and publishes, the ASP.NET runtime image runs. Shipping the SDK image would have put the compiler and the source in production. `appsettings.Development.json` holds the local connection string and the JWT fixture, so publish sets `CopyToPublishDirectory` to Never and `.dockerignore` leaves the file out. The image does keep `appsettings.json`, which has the SLA defaults, logging, and `AllowedHosts`. Those are application defaults, not a connection string or a key. The container's connection string uses the compose service name `sqlserver`. The `localhost` value in `.env` is for `npm run api` on the host. Using that value inside the container would point the API at itself.

Migrations run on startup only when `ASPNETCORE_ENVIRONMENT` is Development. Compose sets that for the local stack, which is how an empty volume ends up migrated and seeded. Running the same step in every environment would let several instances race, and it would give the running app permission to change the schema.

## The defect

The list hook treated the whole filter object as something to debounce, then listed search, page, and page size as the values that start a fetch. Changing status replaced the filter object and produced no request. The next search sent the status that was already on screen. Clearing the search sent a request with no status, so the list went stale again.

Search stays debounced by 300 ms. Any other change to the filter object fetches immediately. `useTicketList.test.ts` covers a status change that sends at once, the same status still being sent after the search is cleared, and an `slaStatus` change that needs no extra wiring in the hook.

## Filtering

`TicketQueries.ApplyFilters` adds one `Where` for each supplied value. The database applies them before `CountAsync` and before `Skip`/`Take`. A further filter is another condition in that method, then a field on `TicketQuery` and a control that writes it into the same filter object the hook already watches. The query tests run against SQLite and assert the filtered count. The logged SQL for the priority filter contains `WHERE`.

## Authentication

The hash is written when development data is seeded, and again for any agent row that still has an empty hash after the column was added. Login compares with the same hasher. The token is an HMAC JWT. The signing key is `Jwt:SigningKey` (`Jwt__SigningKey` in the environment), at least 32 characters, listed in `.env.example`. The development value is a fixture. A 401 from any call except login clears the session and returns the agent to `/login`, which then sends them back to the page they asked for.

Deactivating an agent does not cancel a token that was already issued. It stops being accepted when it expires. A production deployment would use its own signing key, a shorter lifetime, and an httpOnly cookie if the site is shared. It would also need a way to revoke a token when an agent is deactivated. None of that is in this app.

### Development login credentials

Password for every seeded agent: `Local_Dev_AgentPassword1`

| Agent | Email | Can sign in |
| --- | --- | --- |
| Alex Turner | alex.turner@northwind-support.example | Yes |
| Ben Osei | ben.osei@northwind-support.example | Yes |
| Priya Nair | priya.nair@northwind-support.example | Yes |
| Sara Lindqvist | sara.lindqvist@northwind-support.example | Yes |
| Marcus Doyle | marcus.doyle@northwind-support.example | Yes |
| Yuki Tanaka | yuki.tanaka@northwind-support.example | No, inactive |

## Docker

```bash
docker compose down -v && docker compose up --build
```

The API is then at http://localhost:5080. SQL Server is on localhost:1433. In PowerShell, point the web app at the container with:

```powershell
$env:VITE_API_PROXY_TARGET = "http://localhost:5080"
npm run web
```

The bash form is `VITE_API_PROXY_TARGET=http://localhost:5080 npm run web`.

`apps/api/Dockerfile` is built with the repository as the context. Restore copies `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and the four project files, then runs `dotnet restore`, then copies the rest of `apps/api/src`. The runtime stage is `mcr.microsoft.com/dotnet/aspnet:10.0`. It listens on HTTP port 8080, runs as the image's `app` user, and has no development certificate. Compose publishes `5080:8080` and starts the API only after the SQL Server health check passes.

Connection string, JWT settings, and `ASPNETCORE_ENVIRONMENT` are compose environment variables, with the same defaults as `.env.example`. The image does not contain them. `docker compose exec api cat /app/appsettings.json` shows the SLA defaults only. `appsettings.Development.json` is not in `/app`.

On an empty volume, Development startup applies migrations and seeds 5 categories, 6 customers, 6 agents, and 40 tickets. In a real deployment that step would be a release job: `dotnet ef database update`, or a SQL script generated from the migrations, run before the new instances start, with a credential that can change schema. The process that serves traffic would use a login that can read and write rows and cannot migrate. Demo seed would not run. Startup migration is limited to Development so a production environment name does not do it. Several replicas racing `MigrateAsync` can block on the EF migration lock, and a failed migration would then crash every instance.

`docker images` on this machine, after the build:

| Image | Size |
| --- | --- |
| `mcr.microsoft.com/dotnet/sdk:10.0` | 1.3 GB |
| `mcr.microsoft.com/dotnet/aspnet:10.0` | 340 MB |
| `supportdesk-api` | 455 MB |

The published app adds about 115 MB to the runtime base. The SDK image is the build stage and is not what runs.

Checked here:

- `docker compose down -v && docker compose up --build` logged `Seeded 5 categories, 6 customers, 6 agents and 40 tickets` and `Now listening on: http://[::]:8080`. `GET /health` returned 200 with `database` Healthy and "Database is reachable." `GET /api/tickets` without a token returned 401. After login as Alex Turner it returned `totalCount` 40.
- Editing `DatabaseHealthCheck.cs` and running `docker compose build api` printed `CACHED` for `RUN dotnet restore`. The source copy and publish ran again.
- `docker compose exec api whoami` printed `app`.
- With `VITE_API_PROXY_TARGET=http://localhost:5080`, the Vite server forwarded `/api/auth/login` and `/api/tickets`. Login as Alex Turner returned 40 tickets.

Before production the image would be pinned by digest, run with an environment name other than Development (so Swagger and startup migrate stay off), and use a database login that is not `sa`.

## Database

`TicketEscalations` stores the before and after priority, agent, and due date, plus reason, who, and when. Deleting a ticket deletes its history. Agent foreign keys restrict deletes so history is not dropped with an agent. `Agents.PasswordHash` is `nvarchar(500)`, nullable, so the migration can run on rows that already exist. The hash is filled at seed time.

`IX_TicketEscalations_TicketId_EscalatedAtUtc` exists because the history endpoint always reads one ticket, newest first. `IX_TicketEscalations_FromAgentId` and `IX_TicketEscalations_ToAgentId` exist so a change to an agent does not scan the whole history table to enforce those foreign keys. `IX_Tickets_Status_Priority` includes `DueAtUtc` and `AssignedAgentId` because status and priority are the list's common filter pair, and those two columns are then available without a lookup back to the row. `IX_Tickets_AssignedAgentId` is filtered to non-null agents so unassigned tickets do not fill it. Login looks up agents by the unique email index that was already there.

## Testing

`npm test` runs `dotnet test`, then the web tests (Vitest). The API filter tests use SQLite in memory. Everything else uses fakes and a fixed clock. No database server is required.

Covered: SLA windows per priority and tier, including the floor; forced-Critical categories; agent selection (fewest open, specialists, limits, inactive, ties, nobody eligible); escalation (one step, due date from now, assignee kept or changed, history row, rejections); SLA status at the boundaries; password hashing and a wrong password; the list hook's filter requests; a short escalation reason that does not call the API; a 409 shown on the ticket; every SLA indicator state.

Deliberately not covered: a full HTTP pipeline against SQL Server, load, token revocation, and an automated test of `GET /health`. The health check, the seed, and the container user were checked with the Docker commands above.

## AI usage

The Docker work was done in Cursor. The useful loop was to read `Program.cs`, the seeder, and the existing compose file, then change those seams, then run the container and call the endpoints.

Accepted as-is: the runtime image's `app` user and HTTP port 8080. `whoami` printed `app`, and the log showed a single HTTP listener.

Changed: a connection string taken from `.env` uses `localhost`, which inside the container is the API. Compose builds `Server=sqlserver` from `MSSQL_SA_PASSWORD` instead. `appsettings.Development.json` is excluded from publish so the JWT fixture is not in the image. A separate health-check NuGet package and a `curl` binary in the image were skipped. The framework health check and a request from the host are enough.

The image build timed out once downloading `System.IdentityModel.Tokens.Jwt`. Restore retried and finished. A local `dotnet build` could not copy its output DLLs because a `SupportDesk.Presentation` process already had them open. The container build uses its own output directory and was not affected. Generated code was checked by the container log, `GET /health`, a login, `GET /api/tickets`, `whoami`, and the cached restore line on the second build.

## Known limitations

There is no registration, password reset, refresh token, MFA, lockout, or roles. A token issued to an agent who is then deactivated stays valid until it expires. The signing key in `.env.example` is a local fixture. Startup migration and the demo seed run only in Development. The container is a single instance, tagged `10.0` rather than a digest, and the database login is `sa`. The optional SLA summary report is not implemented.

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`; 20.19+ works), Docker.

One command for the backend:

```bash
cp .env.example .env          # optional, every value has a working default
docker compose up --build     # SQL Server, then a migrated and seeded API on :5080
```

Host API and web app, if you would rather not containerise the API:

```bash
npm install
npm run db:up                 # SQL Server only, localhost:1433
npm run api                   # migrates, seeds, then serves the API
npm run web                   # React dev server, proxying to https://localhost:7043
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API in Docker | http://localhost:5080 |
| API from `npm run api` | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger , or http://localhost:5080/swagger when the container is Development |
| Health | http://localhost:5080/health |

**Visual Studio 2026:** run `npm run db:up`, open `SupportDesk.sln`, set **SupportDesk.Presentation** as the startup project, and press F5 on the **https** profile.

## Commands

| Command | What it does |
| --- | --- |
| `docker compose up --build` | SQL Server and the API |
| `npm run db:up` / `db:down` / `db:reset` | Start / stop / wipe SQL Server |
| `npm run api` / `npm run web` | Run the API / the React app on the host |
| `npm run build` | Build both |
| `npm run lint` / `typecheck` / `format` | Web lint and types / `dotnet format` |
| `npm run ef -- migrations add <Name>` | Add an EF Core migration |
| `npm test` | API tests, then web tests |

## API at a glance

| Endpoint | Description |
| --- | --- |
| `GET /health` | Database reachability. No token. |
| `POST /api/auth/login` | JWT, expiry, and the agent's id, name, and email. |
| `GET /api/auth/me` | The agent named by the token. |
| `GET /api/tickets` | Paged list. Filters are applied in the database before the count and the page. |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket, with triage |
| `POST /api/tickets/{id}/escalate` | Escalate, with history |
| `GET /api/tickets/{id}/escalations` | History, newest first |
| `PATCH /api/tickets/{id}/status` · `/assignment` | Change status / assignee |
| `GET /api/customers` · `/api/agents` · `/api/categories` | Reference data |

Errors are `ProblemDetails`: `400` validation, `401` missing or rejected credentials, `404` not found, `409` conflict or business rule.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API cannot reach the database | `docker compose up --build`, or `npm run db:up`, then wait until `docker compose ps` shows SQL Server healthy |
| Port 1433 or 5080 already in use | Stop the other process, or change the host port in `docker-compose.yml` |
| Browser warns about the certificate | The container is HTTP. For `npm run api`, run `dotnet dev-certs https --trust`, or use the web app, which proxies |
| Web app shows "Unable to load tickets" | The API is not running, or `VITE_API_PROXY_TARGET` points at the wrong port. The container is `http://localhost:5080` |
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |
