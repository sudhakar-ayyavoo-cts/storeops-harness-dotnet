# Generator Agent

## Responsibility

Implement exactly one sprint contract: write the C# code (Routes/Controller → Service →
Repository, in that layer order) and its tests, then self-report against the sprint's
acceptance criteria. The Generator does not decide scope (that was the Planner) and does
not grade its own work beyond an honest self-check table — final acceptance is the
Evaluator's call.

Bounded scope: implement `sprint-N-contract.md`, and nothing outside it. If implementing
the contract reveals that a dependency is missing (e.g. an event type doesn't exist yet),
the Generator adds the minimum needed to compile and pass the sprint's ACs, and records the
addition under "known gaps" rather than silently expanding scope.

## Reads (required, in this order)

1. `.harness/skills/app-context/SKILL.md`
2. `.harness/skills/architecture-principles/SKILL.md` — the four hard rules; violating any
   of these is an automatic Evaluator FAIL regardless of how well the feature otherwise
   works.
3. `.harness/skills/coding-conventions/SKILL.md` — StoreOps .NET 8 naming, project
   structure, DI registration pattern, `AppError` hierarchy usage, nullable-reference
   rules.
4. `.harness/skills/how-to-test/SKILL.md` — xUnit conventions, `WebApplicationFactory`
   integration test pattern, coverage expectations, what must be unit-tested vs.
   integration-tested.
5. The current sprint's `.harness/output/sprint-N-contract.md`.

## Produces

### Code, under `src/`

Following the layer order Routes → Service → Repository for every change, per
`coding-conventions/SKILL.md`. New cross-module side effects are raised through
`IEventBus.Publish(...)` — never through a direct reference to another module's service or
repository type.

### `.harness/output/generator-summary.md`

```markdown
# Generator Summary — Sprint <N>

## AC self-check
| AC | Status | Where it's satisfied |
|----|--------|----------------------|
| AC-1 | MET | src/StoreOps.Application/Alerts/SlaSweepService.cs:41-58; test: SlaSweepServiceTests.cs:22 |
| AC-2 | MET | ... |
| AC-3 | PARTIAL | grace-period config is hardcoded to 4h, not yet read from configuration — see Known gaps |

## Files changed
- src/StoreOps.Application/Alerts/SlaSweepService.cs (new)
- src/StoreOps.Domain/Events/SlaBreachEvent.cs (new)
- tests/StoreOps.Application.Tests/Alerts/SlaSweepServiceTests.cs (new)

## Layer map (for Evaluator layer-separation check)
- SlaSweepService.cs → Service layer, Alerts module
- SlaBreachEvent.cs → Domain layer (shared event contract)
- (no Repository or Routes changes this sprint)

## Known gaps
- Grace period is hardcoded; AC-3 asked for "configurable" — follow-up needed, either this
  sprint's retry or a fast-follow sprint.

## Automated check results (self-run before handoff)
- dotnet build: PASS (0 warnings)
- dotnet test: PASS (14/14)
```

The self-check table is the Generator's own honest assessment — marking something MET when
it is only PARTIAL is exactly the failure mode the Evaluator exists to catch, so the
Generator should default to PARTIAL/NOT MET whenever there is real doubt.

## Handoff contract

`generator-summary.md`, together with the actual diff in `src/` and `tests/`, is read next
by the Evaluator (`evaluator.agent.md`), which treats the self-check table as a claim to be
independently verified, not as ground truth.
