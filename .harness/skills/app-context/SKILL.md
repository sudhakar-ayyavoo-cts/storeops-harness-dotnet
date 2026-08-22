# Skill: app-context

**Purpose.** Give every agent the same shared mental model of StoreOps before it acts —
what the application is, what its five modules own, and what "correct" looks like at the
data-model level. Read by all four agents.

> **Source note:** the module list, entities, and enum values below are taken directly from
> the capstone brief (Section 3.3). The endpoint list is this project's own baseline,
> derived from the brief's "9 REST endpoints" requirement and the verification call in
> Section 2.3, Step 4 (`GET /api/tasks`) — the brief is being treated as the complete and
> final specification for this build, per programme guidance. If your own reading of the
> brief suggests a different endpoint split, change the table below; it is the working
> contract the Generator and Evaluator will be held to either way.

## What StoreOps is

A REST API for retail store operations management: creating operational programmes,
assigning and tracking activities across departments, coordinating staff, delivering
alerts, and reporting performance by store and region. In-memory storage, no database,
stub business logic — this is a governed-development reference app, not a production
system.

## The five modules

| Module | Owns | Key types |
|---|---|---|
| `activities` | Operational activities: restocking runs, planogram resets, compliance checks, general tasks | `Task`, `TaskStatus` (`Todo, InProgress, Done, Blocked`), `TaskPriority` (`Low, Medium, High, Critical`), `TaskCategory` (`Restocking, Planogram, Audit, Compliance, General`) |
| `programmes` | Store programmes and staff membership: seasonal rollouts, compliance drives, store refits | `Project`, `ProjectMember`, `ProjectRole` (`StoreManager, DepartmentLead, Associate`) |
| `staff` | Staff registration, authentication, profile | `User`, `UserProfile`, `StaffRole` (`RegionalManager, StoreManager, DepartmentLead, Associate`), `AuthToken` |
| `alerts` | In-app alerts from operational events: inventory flags, SLA breaches, shift handover prompts | `Notification`, `NotificationChannel` (`InApp, Email`), `NotificationStatus`, `AlertType` (`Inventory, SlaBreach, ShiftHandover, Escalation`) |
| `reports` | Store/regional performance summaries — task completion, overdue counts, rollups. **Read-only**: aggregates `activities`, `programmes`, `staff`; never writes to them. | `Report`, `ReportType` (`StoreSummary, RegionalRollup, DepartmentPerformance`), `ReportStatus` (`Pending, Ready, Failed`) |

## Baseline endpoints

| Module | Endpoint | Notes |
|---|---|---|
| activities | `GET /api/tasks` | list, filterable by status/store — this is the endpoint used to verify the running app in Section 2.3, Step 4 |
| activities | `POST /api/tasks` | create |
| programmes | `GET /api/programmes` | list |
| programmes | `POST /api/programmes` | create |
| staff | `GET /api/staff` | list (regional/store manager only, in a full build) |
| staff | `POST /api/staff/login` | issues an `AuthToken` |
| alerts | `GET /api/alerts` | list for the current user/store |
| alerts | `PATCH /api/alerts/{id}/status` | mark read/acknowledged |
| reports | `GET /api/reports/store/{id}` | store summary report |

## .NET 8 solution shape

```
src/
  StoreOps.Domain/            entities, enums, event contracts — no project references out
  StoreOps.Application/       service interfaces + implementations, IEventBus, AppError hierarchy
  StoreOps.Infrastructure/    in-memory repositories, InMemoryEventBus — implements Application interfaces
  StoreOps.Api/                Controllers (Routes layer), Program.cs composition root, DI wiring
tests/
  StoreOps.Application.Tests/  service-layer unit tests, per module
  StoreOps.Api.Tests/          WebApplicationFactory integration tests, per module
```

Each module (`activities`, `programmes`, `staff`, `alerts`, `reports`) is a folder inside
`Domain`, `Application`, `Infrastructure`, and `Api` — not a separate .csproj. This keeps
the module boundary a *convention enforced by the harness*, reviewable in every sprint,
rather than something only a full assembly-splitting refactor could fix. (Architecture-
principles/SKILL.md explains how the boundary is nonetheless made hard to violate by
accident using `internal` visibility.)
