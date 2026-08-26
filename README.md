# StoreOps

StoreOps is a REST API for retail store operations management. It coordinates
operational programmes, tracks activities/tasks across departments, manages staff and
roles, delivers in-app alerts, and reports on store/regional performance.

This repository doubles as a **governed AI-development harness**: a Planner →
Generator → Evaluator → Monitor loop that turns a one-line feature prompt into
reviewed, tested code, with every decision recorded to a permanent audit trail.

## Application

### Modules

StoreOps is organized into five modules, each a folder inside every layer of the
solution (not a separate project):

| Module | Owns | Key types |
|---|---|---|
| `activities` | Restocking runs, planogram resets, compliance checks, general tasks | `Task`, `TaskStatus`, `TaskPriority`, `TaskCategory` |
| `programmes` | Store programmes and staff membership (seasonal rollouts, compliance drives, refits) | `Project`, `ProjectMember`, `ProjectRole` |
| `staff` | Staff registration, authentication, profile | `User`, `UserProfile`, `StaffRole`, `AuthToken` |
| `alerts` | In-app alerts raised by operational events (inventory flags, SLA breaches, shift handovers) | `Notification`, `NotificationChannel`, `NotificationStatus`, `AlertType` |
| `reports` | Store/regional performance summaries. Read-only — aggregates the other four modules, never writes to them | `Report`, `ReportType`, `ReportStatus` |

### Baseline endpoints

| Module | Endpoint | Notes |
|---|---|---|
| activities | `GET /api/tasks` | list, filterable by status/store |
| activities | `POST /api/tasks` | create |
| programmes | `GET /api/programmes` | list |
| programmes | `POST /api/programmes` | create |
| staff | `GET /api/staff` | list (manager-only, in a full build) |
| staff | `POST /api/staff/login` | issues an `AuthToken` |
| alerts | `GET /api/alerts` | list for the current user/store |
| alerts | `PATCH /api/alerts/{id}/status` | mark read/acknowledged |
| reports | `GET /api/reports/store/{id}` | store summary report |

Storage is in-memory — there is no database, and every restart starts clean. This is a
governed-development reference app, not a production system.

## Technical stack

- **.NET 8 / C# 12** — ASP.NET Core Web API
- **In-memory repositories** — no external database; state resets on restart
- **xUnit** — unit tests (`StoreOps.Application.Tests`) and integration tests via
  `WebApplicationFactory` (`StoreOps.Api.Tests`)
- **Docker** — multi-stage build (`mcr.microsoft.com/dotnet/sdk:8.0` →
  `mcr.microsoft.com/dotnet/aspnet:8.0`), orchestrated with `docker-compose.yml`

### Solution layout

```
src/
  StoreOps.Domain/            entities, enums, event contracts — no outgoing project references
  StoreOps.Application/       service interfaces + implementations, IEventBus, AppError hierarchy
  StoreOps.Infrastructure/    in-memory repositories, InMemoryEventBus, demo seeder
  StoreOps.Api/               Controllers, Program.cs composition root, DI wiring
tests/
  StoreOps.Application.Tests/ service-layer unit tests, per module
  StoreOps.Api.Tests/         WebApplicationFactory integration tests, per module
```

Each module is a folder inside `Domain`, `Application`, `Infrastructure`, and `Api` —
the module boundary is a convention enforced by the harness (see below), not a separate
assembly.

### Architecture rules

Five rules are enforced on every change, automatically where possible:

1. **Module boundary** — a module's repositories are `internal`; other modules may only
   depend on its public Service interface. Violating this is a compile error.
2. **Event bus only** — a side effect that crosses a module boundary goes through
   `IEventBus.Publish(...)`, never a direct write call into another module's service or
   repository. Read-only lookups across modules are fine.
3. **Error contract** — no raw `throw new Exception(...)` in Service or Routes code; all
   business errors are `AppError` subtypes carrying `Code`, `Message`, `StatusCode`,
   mapped to HTTP responses by a global exception-handling middleware.
4. **Layer separation** — Controllers → Service → Repository, no skipping; controllers
   hold HTTP concerns only, repositories hold data access only.
5. **Read-only reports** — the `reports` module may only call query-shaped methods
   (`Get*`/`List*`) on other modules' services, never a write.

### Running locally

```
dotnet build
dotnet test
dotnet run --project src/StoreOps.Api
```

When run in the `Development` environment (the default for `dotnet run`),
`DemoDataSeeder` (`src/StoreOps.Infrastructure/Seed/DemoDataSeeder.cs`) pre-populates
the in-memory store with a small, cross-referenced dataset — stores, staff across every
role, tasks in a range of statuses, programmes, notifications, and reports — so the API
returns realistic data immediately instead of an empty store. The seeder does not run
outside `Development`, and because storage is in-memory, it reseeds identically on every
restart.

## Harness

The harness (`.harness/`) is a Planner → Generator → Evaluator → Monitor loop,
orchestrated by `CLAUDE.md` at the repo root, that turns a feature prompt into reviewed,
tested code with a permanent record of how it got there.

```
CLAUDE.md                root orchestrator: routing rules, iteration cap, escalation format
.harness/agents/         planner.agent.md, generator.agent.md, evaluator.agent.md, monitor.agent.md
.harness/skills/         shared reference material each agent reads before acting:
                            app-context, architecture-principles, sprint-decomposition,
                            coding-conventions, how-to-test, how-to-review, evaluation-criteria
.harness/output/         working files for the run in progress (gitignored)
.harness/reviews/        the permanent audit trail — one run-log, evaluator-feedback, and
                          generator-summary per sprint, committed to the repo
```

### The loop

1. **Planner** reads `app-context`, `architecture-principles`, and
   `sprint-decomposition`, then decomposes the feature into a spec and an ordered list of
   sprint contracts, each with `GIVEN/WHEN/THEN` acceptance criteria specific enough to
   check mechanically. It writes `.harness/output/spec.md` ending in
   `STATUS: AWAITING APPROVAL` and stops.
2. Once the developer types `APPROVED`, each sprint runs a **Generator → Evaluator**
   cycle:
   - **Generator** implements one sprint contract (Routes → Service → Repository, plus
     tests) and self-reports against its acceptance criteria in
     `generator-summary.md`.
   - **Evaluator** independently verifies the diff against the contract and the five
     architecture rules, and writes `evaluator-feedback.md` starting with
     `VERDICT: PASS | CONDITIONAL PASS | FAIL`.
3. The orchestrator routes on that verdict:
   - `PASS` / `CONDITIONAL PASS` → sprint is archived, move to the next sprint
     (conditional-pass findings are carried forward as recorded debt).
   - `FAIL` → the Evaluator's feedback is fed back to the Generator and the sprint is
     retried, up to **3 iterations**. Exceeding that raises an escalation
     (`.harness/output/escalation.md`) and hands control back to the developer — the
     harness does not keep retrying past the cap on its own judgement.
4. **Monitor** runs after every verdict, archiving the sprint's outcome to
   `.harness/reviews/sprint-N-run-log.md` regardless of PASS/CONDITIONAL PASS/FAIL, so
   the harness's own track record stays observable over time.

Context is scoped per role and reset between sprints: each agent reads only the skill
files relevant to it, and a new sprint starts from its own contract plus the current
state of `src/`, not the accumulated conversation history of prior sprints. Within a
single sprint's retry loop, though, the Evaluator's feedback *is* carried forward
explicitly, so a retry knows what it got wrong.

### Kicking off a run

Start a run with a single prompt addressed to the Planner:

```
@planner <feature description in one or two sentences>
```

Claude Code loads the Planner agent, produces `.harness/output/spec.md`, and stops,
waiting for review. Reply:

```
APPROVED
```

to begin the Generator/Evaluator/Monitor loop. No further manual invocation is needed —
Claude Code drives the sprint-by-sprint loop itself until the feature is complete or an
escalation is raised.

## Docker

### Files

- `Dockerfile` — multi-stage build: `dotnet restore`/`publish` in an SDK image, copied
  into a slim ASP.NET runtime image (`curl` is installed in the runtime stage so the
  compose healthcheck can run inside the container).
- `docker-compose.yml` — builds the image, maps container port `8080` to host port
  `5000`, sets `ASPNETCORE_ENVIRONMENT=Development` (so the demo seeder runs), and
  healthchecks `GET /api/tasks` every 10s.
- `.dockerignore` — excludes `bin/`, `obj/`, `.git/`, and other local/IDE artifacts from
  the build context, so a locally-built `obj/` (which can embed a Windows-only NuGet
  fallback path) never leaks into the Linux build stage.

### Usage

```
docker compose build
docker compose up -d
docker compose ps          # should report the container as healthy
curl -i http://localhost:5000/api/tasks
```

Storage is in-memory, so the demo dataset reseeds identically on every
`docker compose up`. Stop the stack with `docker compose down`.
