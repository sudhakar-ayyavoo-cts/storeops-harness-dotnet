# Sprint 1 Contract — SLA-Breach Detection (`activities`)

## Module(s) touched

- **`StoreOps.Domain.Activities`** — `StoreTask` gains a new property, `SlaBreachedAt`
  (`DateTimeOffset?`, settable), used only for this sprint's idempotency check.
- **`StoreOps.Application.Common`** — new `IClock` abstraction (`DateTimeOffset UtcNow { get; }`),
  so the sweep's notion of "now" is deterministic and mockable in tests. Cross-cutting, not
  activities-specific, but introduced in this sprint since it's the first consumer.
- **`StoreOps.Application.Activities`** (primary — Service layer):
  - `ITaskRepository` gains `Task<StoreTask> UpdateAsync(StoreTask task, CancellationToken ct)`,
    mirroring `INotificationRepository.UpdateAsync` — the existing convention in this codebase
    is an explicit persistence call after mutating a fetched entity, not reliance on
    reference-sharing through the in-memory store.
  - New `ISlaSweepService` / `SlaSweepService`, constructor-injecting `ITaskRepository`,
    `IStaffService` (read-only lookup only), `IEventBus`, `IClock`. One public method:
    `Task<int> SweepAsync(CancellationToken ct)`, returning the count of breaches published
    in that run.
  - Registered in `StoreOps.Application.Activities.ServiceCollectionExtensions.AddActivitiesModule`.
- **`StoreOps.Infrastructure`**:
  - `InMemoryTaskRepository.UpdateAsync` — same pattern as `InMemoryNotificationRepository.UpdateAsync`.
  - New `SystemClock : IClock` (returns `DateTimeOffset.UtcNow`), registered as a singleton
    in `ServiceCollectionExtensions.AddInfrastructure`.
- **`StoreOps.Api`** (Routes layer):
  - `ActivitiesController` gains `POST /api/tasks/sla-sweep` → calls
    `ISlaSweepService.SweepAsync(ct)` only, returns `200 OK` with a new
    `StoreOps.Api.Contracts.Activities.SlaSweepResultDto { int BreachesDetected }`.

Read-only cross-module call: `SlaSweepService` calls `IStaffService.ListAsync(task.StoreId, ct)`
to resolve the Department Lead — a query-shaped call into `staff`'s public Service interface,
permitted under architecture-principles Rule 2. No write, no repository reference into `staff`.

## Acceptance criteria

AC-1:
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

AC-2:
GIVEN a `StoreTask` identical to AC-1 except `Priority = TaskPriority.High`
WHEN the sweep runs
THEN the same publish behaviour as AC-1 occurs — `High` breaches exactly like `Critical`

AC-3 (idempotency — run twice):
GIVEN the `StoreTask` from AC-1 has already been swept once in a prior call (its
`SlaBreachedAt` is non-null) and its `Status` is still not `Done`
WHEN `SweepAsync` is invoked a second time
THEN `IEventBus.Publish` is **not** called again for that task's `Id` (`Times.Never` for
an `SlaBreachEvent` matching that `TaskId` on the second call)

AC-4 (not yet due):
GIVEN a `StoreTask` with `Priority = TaskPriority.Critical`, `Status = TaskStatus.Todo`,
`DueDate = clock.UtcNow.AddDays(1)`
WHEN the sweep runs
THEN `IEventBus.Publish` is not called for that task and its `SlaBreachedAt` remains `null`

AC-5 (already resolved):
GIVEN a `StoreTask` with `Priority = TaskPriority.Critical`, `Status = TaskStatus.Done`,
`DueDate = clock.UtcNow.AddDays(-1)`
WHEN the sweep runs
THEN `IEventBus.Publish` is not called for that task

AC-6 (no Department Lead in store — skip, don't fail the run):
GIVEN a `StoreTask` with `Priority = TaskPriority.Critical`, `Status = TaskStatus.InProgress`,
`DueDate = clock.UtcNow.AddHours(-1)`, `StoreId` equal to a store with **no** `User` of
`Role = StaffRole.DepartmentLead`, and a second, independent `StoreTask` in a **different**
store that does have a Department Lead and otherwise matches AC-1
WHEN the sweep runs once over both tasks
THEN `IEventBus.Publish` is not called for the first task (no exception propagates), and
**is** called once for the second task — one unresolvable recipient does not abort the sweep

AC-7 (unassigned task still breaches):
GIVEN a `StoreTask` with `Priority = TaskPriority.Critical`, `Status = TaskStatus.InProgress`,
`DueDate = clock.UtcNow.AddHours(-1)`, `AssignedToUserId = null`, `StoreId` equal to a store
with a Department Lead
WHEN the sweep runs
THEN `IEventBus.Publish` is called once with an `SlaBreachEvent` whose `AssignedToUserId`
equals `Guid.Empty` and `DepartmentLeadId` equals that store's Department Lead's `Id`

AC-8 (endpoint, integration):
GIVEN a `StoreTask` persisted through the running API that matches AC-1's breach conditions
WHEN a client sends `POST /api/tasks/sla-sweep`
THEN the response is `200 OK` with body `{ "breachesDetected": <n> }` where `n >= 1`, and a
subsequent `GET /api/tasks?storeId=<that store>` shows the task's `SlaBreachedAt` populated
(exposing it on `TaskDto` for this check is fine but not required beyond what the test needs
to observe the persisted state — verifying via the repository directly is equally acceptable)

## Out of scope (do not implement in this sprint)

- Creating any `Notification` record, or any call into the `alerts` module — that is Sprint 2.
- The escalation-after-grace-period logic and its configuration key — Sprint 2.
- Wiring a real background scheduler/timer (`IHostedService` or similar) to call
  `SweepAsync` automatically — the sweep stays directly/manually invokable
  (test, or `POST /api/tasks/sla-sweep`) for this build.
- Any formal `TaskCategory` → `UserProfile.Department` mapping — Department Lead resolution
  stays "first `DepartmentLead` in the task's store by `CreatedAt`," per `spec.md`'s Open
  Questions.
- Exposing `SlaBreachedAt` on `GET /api/tasks` response filtering/sorting — only needs to be
  readable for AC-8's verification.

## Handoff

This contract is read next by the Generator (`generator.agent.md`), which treats every AC
above as a hard requirement to self-check against before writing `generator-summary.md`.
