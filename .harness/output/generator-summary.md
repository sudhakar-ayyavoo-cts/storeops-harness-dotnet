# Generator Summary — Sprint 2

## AC self-check

| AC | Status | Where it's satisfied |
|----|--------|----------------------|
| AC-1 | MET | src/StoreOps.Application/Alerts/SlaBreachEventSubscriber.cs:26-41; test: SlaBreachEventSubscriberTests.cs (`HandleAsync_OnSlaBreachEvent_CreatesSlaBreachNotificationForDepartmentLead`) |
| AC-2 | MET | src/StoreOps.Application/Alerts/AlertsEscalationSweepService.cs:37-83; test: AlertsEscalationSweepServiceTests.cs (`SweepAsync_UnresolvedBreachPastGracePeriod_CreatesEscalationForStoreManager`) |
| AC-3 | MET | `n.CreatedAt <= graceCutoff` filter, AlertsEscalationSweepService.cs:44-49; test: `SweepAsync_GracePeriodNotYetElapsed_DoesNotEscalate` |
| AC-4 | MET | `task.Status == Done → continue`, AlertsEscalationSweepService.cs:65-68; test: `SweepAsync_TaskAlreadyDone_DoesNotEscalate` |
| AC-5 | MET | `n.Status != Acknowledged` filter, AlertsEscalationSweepService.cs:46; test: `SweepAsync_BreachAlreadyAcknowledged_DoesNotEscalate` (also asserts `IActivitiesService.GetByIdAsync` is never called for an already-acknowledged breach — the filter short-circuits before the cross-module read) |
| AC-6 | MET | `alreadyEscalatedTaskIds` set + `!Contains(...)` filter, AlertsEscalationSweepService.cs:40-49; test: `SweepAsync_AlreadyEscalated_DoesNotCreateDuplicateEscalation` |
| AC-7 | MET | `storeManager is null → continue`, AlertsEscalationSweepService.cs:75-78; test: `SweepAsync_NoStoreManagerInStore_SkipsButProcessesOtherCandidates` |
| AC-8 | MET | `AlertsOptions.SlaEscalationGraceHours` read via `IOptions<AlertsOptions>`, not hardcoded; test: `SweepAsync_GracePeriodIsConfigurable_ShorterGraceEscalatesSooner` constructs the SUT with `graceHours: 1` and proves a 90-minute-old breach escalates under that config but would not under the 4-hour default |
| AC-9 | MET | `POST /api/alerts/sla-escalation-sweep`, AlertsController.cs (SlaEscalationSweep action); test: SlaEscalationSweepEndpointTests.cs (`Post_SlaEscalationSweep_EscalatesUnresolvedBreachToStoreManager`) — asserts 200 + body shape + a follow-up `GET /api/alerts?userId=...` shows the resulting `Escalation` notification |

Also manually verified live (not part of the automated suite): started the API in `Development`,
ran `POST /api/tasks/sla-sweep` against the demo-seeded overdue CRITICAL task, confirmed via
`GET /api/alerts?userId=<Aisha's id>` that `SlaBreachEventSubscriber` created a new
`SlaBreach` notification for her, and confirmed `POST /api/alerts/sla-escalation-sweep`
correctly returned `0` since the grace period had not yet elapsed.

## Files changed

- src/StoreOps.Application/StoreOps.Application.csproj (modified — added `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Options.ConfigurationExtensions`)
- src/StoreOps.Application/Alerts/AlertsOptions.cs (new)
- src/StoreOps.Application/Alerts/SlaBreachEventSubscriber.cs (new)
- src/StoreOps.Application/Alerts/IAlertsEscalationSweepService.cs (new)
- src/StoreOps.Application/Alerts/AlertsEscalationSweepService.cs (new)
- src/StoreOps.Application/Alerts/ServiceCollectionExtensions.cs (modified — `AddAlertsModule` now takes `IConfiguration`; registers `AlertsOptions` binding, `IAlertsEscalationSweepService`, and the `SlaBreachEventSubscriber` hosted service)
- src/StoreOps.Api/Program.cs (modified — `AddAlertsModule(builder.Configuration)`)
- src/StoreOps.Api/appsettings.json (modified — added `Alerts:SlaEscalationGraceHours = 4`)
- src/StoreOps.Api/Contracts/Alerts/SlaEscalationSweepResultDto.cs (new)
- src/StoreOps.Api/Controllers/AlertsController.cs (modified — added `POST /api/alerts/sla-escalation-sweep`)
- tests/StoreOps.Application.Tests/Alerts/SlaBreachEventSubscriberTests.cs (new, 1 test)
- tests/StoreOps.Application.Tests/Alerts/AlertsEscalationSweepServiceTests.cs (new, 7 tests)
- tests/StoreOps.Api.Tests/Alerts/SlaEscalationSweepEndpointTests.cs (new, 1 test)

## Layer map (for Evaluator layer-separation check)

- AlertsOptions.cs → Application/Alerts (plain options POCO, no logic)
- SlaBreachEventSubscriber.cs → Application layer, alerts module. Not a Controller and not a
  Repository — it's the module's event-driven entry point (the `alerts` module's analogue of
  a Controller action, except triggered by `IEventBus` instead of HTTP). `StartAsync` only
  subscribes; `HandleAsync` only maps the event into a `CreateAlertRequest` and delegates to
  `IAlertsService.CreateAsync` — no business rules beyond that mapping live here.
- IAlertsEscalationSweepService.cs / AlertsEscalationSweepService.cs → Application layer,
  alerts module (Service — business logic; the only place `IActivitiesService`,
  `IStaffService`, and `INotificationRepository` are all used together)
- ServiceCollectionExtensions.cs → DI composition, no business logic
- Program.cs → composition root call-site update only
- appsettings.json → configuration data only
- SlaEscalationSweepResultDto.cs → Api/Contracts, alerts module (DTO only)
- AlertsController.cs (SlaEscalationSweep action) → Routes layer — model binding + one
  Service call + status code only, no business rules in the action body

## Known gaps

- None for this sprint's scope. Per `sprint-2-contract.md`'s Out of scope: no real background
  scheduler wired for either sweep; both stay manually/test-invokable via their `POST`
  endpoints, matching Sprint 1's pattern.
- `SlaBreachEventSubscriber.HandleAsync` is `private`, tested indirectly by capturing the
  delegate passed to `IEventBus.Subscribe` in the unit test (a standard pattern for testing
  event registration) rather than calling a public method directly — noting this since it's a
  slightly less direct test shape than the rest of the suite, though it does independently
  verify the actual behavior end-to-end (subscribe → handle → `IAlertsService.CreateAsync`
  with the right request), and the live manual run further confirms it against the real
  `InMemoryEventBus`/DI graph, not just mocks.

## Automated check results (self-run before handoff)

- dotnet build: PASS (0 warnings, 0 errors)
- dotnet test: PASS (36/36 — 25 in StoreOps.Application.Tests, 11 in StoreOps.Api.Tests;
  9 new tests added this sprint: 8 unit + 1 integration)
- Manual live run: PASS (see AC-1/AC-9 notes above)

## Architecture rule self-check

- Rule 1 (module boundary): no new cross-module `Infrastructure` reference. `AlertsEscalationSweepService`
  depends on `IActivitiesService` and `IStaffService` (public Application-layer interfaces),
  never `InMemoryTaskRepository`/`InMemoryUserRepository` or any other module's
  `Infrastructure` type.
- Rule 2 (event bus only): `alerts`' consumption of `SlaBreachEvent` is the event-bus side of
  this rule (subscribing, not writing). The two cross-module calls this sprint adds —
  `IActivitiesService.GetByIdAsync` and `IStaffService.ListAsync` — are both reads (`Get*`/
  `List*`-shaped), which Rule 2 explicitly permits. No direct write-shaped call into another
  module's Service was added.
- Rule 3 (error contract): no new `throw new Exception(...)`/built-in exception added.
  `AlertsEscalationSweepService` catches `TaskNotFoundError` (a pre-existing `AppError`
  subtype from `activities`, part of that module's public contract) and treats it as a skip,
  not a new raw throw.
- Rule 4 (layer separation): see Layer map above — `AlertsController`'s new action has no
  business-rule branching; no Repository file in this diff references `IEventBus` or another
  module's Service.
- Rule 5 (read-only reports): not applicable — `reports` module untouched this sprint.
