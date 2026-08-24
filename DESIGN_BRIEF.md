# Harness Design Brief

## Section A — Intent Decomposition

- Feature was splitted into two sprints. Because the changes involves event creation and 
  publishing the notification. This helps the evaluator to grade the sprints seperately.
- AC-1:
  GIVEN a `StoreTask` with `Priority = TaskPriority.Critical`, `Status = TaskStatus.InProgress`,
  `DueDate = clock.UtcNow.AddHours(-2)`, `AssignedToUserId` set to an existing `User.Id`,
  `SlaBreachedAt = null`, and `StoreId` equal to a store that has one `User` with
  `Role = StaffRole.DepartmentLead`
  WHEN `ISlaSweepService.SweepAsync` is invoked
  THEN `IEventBus.Publish` is called exactly once with an `SlaBreachEvent` whose `TaskId`
  equals the task's `Id`, `AssignedToUserId` equals the task's `AssignedToUserId`,
  `DepartmentLeadId` equals that store's Department Lead's `Id`, and `BreachedAt` equals
  `clock.UtcNow`; and re-reading the task via `ITaskRepository.GetByIdAsync` shows
  `SlaBreachedAt` is no longer `null`

## Section B — Governance Framework

### Skill File Strategy 
| Skill file | Governs | Read by | What it encodes that's StoreOps-specific |
|---|---|---|---|
| `app-context/SKILL.md` | The domain model itself | All 4 agents (shared) | The 5 modules, real entity/enum names (`TaskStatus`, `AlertType`, etc.), the baseline endpoint table, the 4-project .NET solution shape with `internal`-visibility repositories |
| `architecture-principles/SKILL.md` | The 5 non-negotiable rules (module boundary, event-bus-only, error contract, layer separation, read-only reports) | All 4 agents (shared) | Each rule mapped to one of the 4 client failure modes, plus its .NET enforcement mechanism — compiler error via `internal`, Roslyn/text-scan, or LLM-assessed diff review |
| `sprint-decomposition/SKILL.md` | Sprint sizing and GIVEN/WHEN/THEN testability | Planner only | The worked SLA-sweep AC example, including the idempotency clause — the sizing heuristic tied to this feature's actual 2–3 sprint split |
| `coding-conventions/SKILL.md` | Generator's implementation patterns | Generator only | The `AppError`/`IEventBus` code contracts, naming and DI registration pattern, what "stub implementation" means for this bootstrap |
| `how-to-test/SKILL.md` | What/how the Generator must test | Generator only | The "no status-code-only tests" rule targeting Failure Mode 3 directly, event-bus assertion pattern, 80% coverage bar |
| `how-to-review/SKILL.md` | The Evaluator's review procedure | Evaluator only | The 9-step order (build → test → coverage → module-boundary scan → ...), tuned so most boundary violations are already caught by step 1 |
| `evaluation-criteria/SKILL.md` | The graded rubric itself | Evaluator only | 3 weighted dimensions, the hard-gate table, the deterministic verdict formula |

### What is captured

| File |	Contents |
|---|---|
| sprint-N-run-log.md	| Sprint ID + feature name, final verdict, iterations used (out of 3), escalation flag, estimated token cost, which hard gates fired this sprint, and a free-text "quality trend note" comparing this sprint to prior ones in the same run |
|sprint-N-evaluator-feedback.md	| Copied verbatim from .harness/output/ — the full verdict, per-dimension scores, and file+line-level findings |
|sprint-N-generator-summary.md	| Copied verbatim — the Generator's own AC self-check table and known-gaps list |


## Section C — Non-Determinism Strategy

| Hard gate	| Dimension	| Automated / LLM	| Failure mode it prevents	| Why it can't be a soft check |
|---|---|---|---|---|
| dotnet build succeeds, 0 warnings	| Architecture Compliance	| Automated	| Compile-time module-boundary violations, nullability holes	| A build that doesn't compile isn't a matter of degree — there's nothing to average it against |
| dotnet test — all pass	| Correctness vs. AC	| Automated	| Regressions, broken sprint output	| Same reasoning — a failing test suite isn't a quality signal, it's a broken state |
| Zero cross-module Infrastructure repository references	| Architecture Compliance	| Automated (build) + LLM confirmation	| Failure Mode 1 — direct imports bypassing the service boundary	| This is the client's literal, named dealbreaker. One instance reaching main is the failure, not a degree of it |
| Zero raw non-AppError throw new in Service/Controller	| Architecture Compliance	| Automated (text-scan) + LLM confirmation	| Failure Mode 2 — raw Error throws bypassing the typed hierarchy	| Same reasoning — a single raw throw defeats the whole point of having a typed error contract |
| Every cross-module side effect uses IEventBus.Publish	| Architecture Compliance	| LLM-assessed	| Failure Mode 4 — missing event bus integration / direct sibling writes	| A single direct write reintroduces the exact bug the prior AI-assisted experiment produced — no amount of good scoring elsewhere offsets that |
| Coverage ≥ 80% on changed Application files	| Code Quality & Tests	| Automated (Coverlet/cobertura)	| Business logic shipping with no verifiable test evidence	| This gate protects the other dimensions' scoring — below the threshold, the Generator's "MET" self-check claims can't actually be verified, so nothing above it can be trusted either |

## Section D — Architectural Decisions

**Decision 1 — Enforce the module boundary via C# `internal` visibility, not the Evaluator alone**

*Decision:* Repository types are marked `internal` to `StoreOps.Infrastructure` and namespaced per module, so a cross-module repository reference fails at compile time — the Evaluator's review is a second, confirmatory layer, not the primary enforcement mechanism.

*Alternatives considered:* Relying entirely on the Evaluator's LLM-assessed review plus a text-scan of the diff (the only mechanism available to it before this decision); a separate dependency-analyser step run as its own tool, matching the `depcruiser`-based approach the brief describes for the Node.js stack.

*Rationale:* A compile error is the earliest possible feedback — the Generator hits it before the Evaluator ever runs, with zero LLM judgment involved and zero risk of the Evaluator missing an instance on a bad day. It also means this particular hard gate degrades gracefully: even if the Evaluator's LLM-assessed pass is ever skipped or weakened, the violation still can't ship, because it can't compile. The Evaluator's review stays in place as defense-in-depth — `how-to-review/SKILL.md` still flags anything that compiled anyway (e.g. reaching the internal type via reflection) as independently suspicious.

*Assumption:* This depends on the whole solution staying a single compiled assembly graph. `internal` visibility only has teeth within one assembly boundary — if StoreOps ever split into independently-deployed per-module services, this enforcement mechanism would stop applying, and the Evaluator's LLM/dependency-analysis check would become the *primary* safeguard again, not a backup.

**Decision 2 — Split `src/` into four projects rather than folders in one project**

*Decision:* `StoreOps.Domain` / `StoreOps.Application` / `StoreOps.Infrastructure` / `StoreOps.Api` as four separate .csproj files, with modules as folders inside each, rather than one project with module folders across all layers.

*Alternatives considered:* A single project with `Modules/<name>/{Controller,Service,Repository}` folders (lower ceremony, closer to the stub-implementation spirit of the bootstrap prompt); a fully modular-monolith split with one project per module per layer (20 projects for 5 modules × 4 layers) — rejected as disproportionate ceremony for a capstone scoped to stub logic.

*Rationale:* This decision is a direct dependency of Decision 1 — `internal` only means something at an assembly boundary, so repository types have to live in their own assembly (`StoreOps.Infrastructure`) for the visibility rule to be enforceable at all. A single-project, folder-only structure can't compile-time-gate the module boundary no matter how the folders are named; the four-project split is the minimum structure that makes Decision 1 real rather than aspirational.

*Assumption:* Assumes the team accepts the added project-reference management overhead (four `.csproj` files, explicit `<ProjectReference>` wiring) in exchange for compile-time enforcement. For a much shorter-lived prototype where that overhead isn't worth it, the folders-in-one-project alternative would be defensible — it just gives up Decision 1's guarantee.

**Decision 3 — Seed demo data at startup, rather than starting fully empty**

*Decision:* The application seeds a small, fixed dataset into the in-memory store at startup (one overdue `Critical` task, its assigned `DepartmentLead`, etc.) when running in a demo/development configuration, rather than starting with empty in-memory collections.

*Alternatives considered:* Start fully empty and require the demoer to `POST` the needed fixture data by hand via curl/Postman before triggering the SLA sweep, closest to the bootstrap prompt's literal "in-memory storage, no database" instruction with zero extra code; seed data unconditionally in every environment, including any future non-demo configuration.

*Rationale:* Use case require a screenshot or URL showing the new endpoint actually responding with the feature working — an empty store means several manual setup calls have to happen live (or be scripted separately) before the SLA breach can even be observed, which adds friction and risk to both the deployment evidence and the panel walkthrough. A small seed dataset means `docker compose up` plus one call to trigger the sweep is enough to demonstrate the feature end to end. Worth noting for credibility in the brief: the seed logic itself has to respect the module boundary rule — it inserts data via each module's own repository/service, not by writing directly into another module's in-memory store, so the demo convenience doesn't quietly violate Rule 1.

*Assumption:* Assumes seeding is explicitly gated to a demo/development environment (e.g. behind `ASPNETCORE_ENVIRONMENT=Development` or a dedicated config flag) and never runs unconditionally — worth stating outright since it's easy to forget a guard like that once it's convenient. Also assumes seeding is treated as baseline/bootstrap infrastructure (Day 1 setup), not something a harness sprint builds — if it showed up as a Planner sprint mid-run, it would need its own AC and Evaluator review like anything else, which is unnecessary ceremony for what's really demo tooling.

---