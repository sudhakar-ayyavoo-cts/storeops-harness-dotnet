# Generator Summary — Sprint 1

## AC self-check

| AC | Status | Where it's satisfied |
|----|--------|----------------------|
| AC-1 | MET | src/StoreOps.Application/Activities/SlaSweepService.cs:39-63; test: SlaSweepServiceTests.cs:75 (`SweepAsync_CriticalOverdueTaskWithDepartmentLead_PublishesSlaBreachEvent`) |
| AC-2 | MET | same source; test: SlaSweepServiceTests.cs:99 (`SweepAsync_HighPriorityOverdueTask_PublishesSlaBreachEvent`) |
| AC-3 | MET | `SlaBreachedAt is null` filter, SlaSweepService.cs:41-46; test: SlaSweepServiceTests.cs:112 (`SweepAsync_RunTwice_DoesNotPublishSecondEventForAlreadyBreachedTask`) |
| AC-4 | MET | `DueDate.Value < now` filter, SlaSweepService.cs:44; test: SlaSweepServiceTests.cs:135 (`SweepAsync_NotYetDue_DoesNotPublish`) |
| AC-5 | MET | `Status != Done` filter, SlaSweepService.cs:43; test: SlaSweepServiceTests.cs:149 (`SweepAsync_TaskAlreadyDone_DoesNotPublish`) |
| AC-6 | MET | `departmentLead is null → continue`, SlaSweepService.cs:54-57; test: SlaSweepServiceTests.cs:162 (`SweepAsync_NoDepartmentLeadInStore_SkipsTaskButProcessesOthers`) — asserts both the skip and that the sweep still processes the second, resolvable task in the same run |
| AC-7 | MET | `task.AssignedToUserId ?? Guid.Empty`, SlaSweepService.cs:59; test: SlaSweepServiceTests.cs:187 (`SweepAsync_UnassignedTask_PublishesEventWithEmptyAssignee`) |
| AC-8 | MET | POST /api/tasks/sla-sweep, ActivitiesController.cs (SlaSweep action); test: SlaSweepEndpointTests.cs (`Post_SlaSweep_DetectsOverdueCriticalTaskAndPersistsBreach`) — asserts 200 + body shape + persisted `SlaBreachedAt` via repository read-back |

## Files changed

- src/StoreOps.Domain/Activities/StoreTask.cs (modified — added `SlaBreachedAt`)
- src/StoreOps.Application/Common/IClock.cs (new)
- src/StoreOps.Infrastructure/Common/SystemClock.cs (new)
- src/StoreOps.Application/Activities/ITaskRepository.cs (modified — added `UpdateAsync`)
- src/StoreOps.Infrastructure/Activities/InMemoryTaskRepository.cs (modified — implemented `UpdateAsync`)
- src/StoreOps.Application/Activities/ISlaSweepService.cs (new)
- src/StoreOps.Application/Activities/SlaSweepService.cs (new)
- src/StoreOps.Application/Activities/ServiceCollectionExtensions.cs (modified — registered `ISlaSweepService`)
- src/StoreOps.Infrastructure/ServiceCollectionExtensions.cs (modified — registered `IClock`)
- src/StoreOps.Api/Contracts/Activities/SlaSweepResultDto.cs (new)
- src/StoreOps.Api/Controllers/ActivitiesController.cs (modified — added `POST /api/tasks/sla-sweep`)
- tests/StoreOps.Application.Tests/Activities/SlaSweepServiceTests.cs (new, 7 tests)
- tests/StoreOps.Api.Tests/Activities/SlaSweepEndpointTests.cs (new, 1 test)

## Layer map (for Evaluator layer-separation check)

- StoreTask.cs → Domain layer, activities module (data only, no logic)
- IClock.cs → Application/Common (cross-cutting abstraction, no module-specific logic)
- SystemClock.cs → Infrastructure/Common (Infrastructure implementation of an Application abstraction)
- ITaskRepository.cs → Application layer, activities module (interface — Repository contract)
- InMemoryTaskRepository.cs → Infrastructure layer, activities module (Repository — data access only, no IEventBus/HttpClient/Service calls)
- ISlaSweepService.cs / SlaSweepService.cs → Application layer, activities module (Service — business logic; the only place `IStaffService` is called and the only place `IEventBus.Publish` is called)
- ServiceCollectionExtensions.cs (both) → DI composition, no business logic
- SlaSweepResultDto.cs → Api/Contracts, activities module (DTO only)
- ActivitiesController.cs (SlaSweep action) → Routes layer — model binding + one Service call + status code only, no business rules in the action body

## Known gaps

- None for this sprint's scope. `SlaBreachedAt` is not exposed on `TaskDto`/`GET /api/tasks` —
  AC-8 was verified via direct repository read-back instead, which the contract's "Out of
  scope" section explicitly allows.
- Per `sprint-1-contract.md`'s Out of scope: no `Notification` is created and no `alerts`
  module code changed — the `SlaBreachEvent` published by this sprint currently has no
  subscriber, so it is published and silently dropped (`InMemoryEventBus.Publish` no-ops when
  there are no registered handlers for the event type). This is expected and by design —
  Sprint 2 adds the subscriber.

## Automated check results (self-run before handoff)

- dotnet build: PASS (0 warnings, 0 errors)
- dotnet test: PASS (27/27 — 17 in StoreOps.Application.Tests, 10 in StoreOps.Api.Tests;
  8 new tests added this sprint: 7 unit + 1 integration)

## Architecture rule self-check

- Rule 1 (module boundary): no new cross-module `Infrastructure` reference. `SlaSweepService`
  depends on `IStaffService` (a public Application-layer interface), never
  `InMemoryUserRepository` or any `StoreOps.Infrastructure.Staff` type.
- Rule 2 (event bus only): the only cross-module side effect is `IEventBus.Publish(new
  SlaBreachEvent(...))`. The one cross-module call that isn't via the event bus —
  `IStaffService.ListAsync(task.StoreId, ct)` — is a `List*` (query-shaped) read, which Rule 2
  explicitly permits.
- Rule 3 (error contract): no new `throw new Exception(...)`/built-in exception added; the
  "no Department Lead found" case is handled as a skip (`continue`), not an error, per AC-6.
- Rule 4 (layer separation): see Layer map above — Controller action has no business-rule
  branching; Repository has no `IEventBus`/Service reference.
- Rule 5 (read-only reports): not applicable — `reports` module untouched this sprint.
