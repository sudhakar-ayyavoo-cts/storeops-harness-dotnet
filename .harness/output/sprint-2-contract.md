# Sprint 2 Contract — Notification + Escalation (`alerts`)

## Module(s) touched

- **`StoreOps.Application.Alerts`** (primary — Service layer):
  - New `AlertsOptions` (`SlaEscalationGraceHours` int, default `4`) — the "configurable
    grace period" from `spec.md`'s Open Questions, bound from configuration via the
    `IOptions<AlertsOptions>` pattern (not read from raw `IConfiguration` inline in the
    service — keeps it mockable in unit tests).
  - New `SlaBreachEventSubscriber : IHostedService`. Constructor takes `IEventBus` and
    `IServiceScopeFactory` — **not** `IAlertsService` directly. `IAlertsService` is
    `Scoped`; this subscriber is registered as a singleton-lifetime hosted service (so its
    `Subscribe` call survives the app's lifetime), which means its event handler must open a
    new DI scope per event via `IServiceScopeFactory.CreateScope()` and resolve
    `IAlertsService` from that scope — resolving a `Scoped` service straight from a singleton
    is the captive-dependency bug to avoid here. `StartAsync` calls
    `eventBus.Subscribe<SlaBreachEvent>(HandleAsync)`; `StopAsync` is a no-op
    (`Task.CompletedTask`); `HandleAsync` builds a `CreateAlertRequest` (`UserId =
    e.DepartmentLeadId`, `AlertType = AlertType.SlaBreach`, `Channel =
    NotificationChannel.InApp`, `RelatedEntityId = e.TaskId`, a `Message` that includes the
    `TaskId`) and calls the scoped `IAlertsService.CreateAsync`.
  - New `IAlertsEscalationSweepService` / `AlertsEscalationSweepService`, constructor-injecting
    `INotificationRepository`, `IActivitiesService` (read-only), `IStaffService` (read-only),
    `IClock`, `IOptions<AlertsOptions>`. One public method: `Task<int> SweepAsync(CancellationToken ct)`,
    returning the count of escalations created in that run.
  - Registered in `StoreOps.Application.Alerts.ServiceCollectionExtensions`. Its signature
    changes to `AddAlertsModule(this IServiceCollection services, IConfiguration configuration)`
    so it can call `services.Configure<AlertsOptions>(configuration.GetSection("Alerts"))`;
    also registers `services.AddHostedService<SlaBreachEventSubscriber>()` and
    `services.AddScoped<IAlertsEscalationSweepService, AlertsEscalationSweepService>()`.
    `Program.cs`'s call site updates to `builder.Services.AddAlertsModule(builder.Configuration)`.
- **`StoreOps.Api`** (Routes layer):
  - `AlertsController` gains `POST /api/alerts/sla-escalation-sweep` → calls
    `IAlertsEscalationSweepService.SweepAsync(ct)` only, returns `200 OK` with a new
    `StoreOps.Api.Contracts.Alerts.SlaEscalationSweepResultDto { int EscalationsCreated }`.
- **`appsettings.json`**: add
  ```json
  "Alerts": { "SlaEscalationGraceHours": 4 }
  ```

Read-only cross-module calls: `AlertsEscalationSweepService` calls
`IActivitiesService.GetByIdAsync(taskId, ct)` (to check the task is still unresolved — catch
`StoreOps.Application.Activities.Errors.TaskNotFoundError` and treat it as "skip, nothing to
escalate," not a sweep-ending exception) and `IStaffService.ListAsync(task.StoreId, ct)` (to
resolve the Store Manager). Both are query-shaped reads into another module's public Service
interface, permitted under architecture-principles Rule 2. No repository reference into
`activities` or `staff`, and no write back into either module.

## Resolved assumption carried from `spec.md`

"Unresolved after the grace period" is treated as true only while **both** of the following
hold: the underlying `StoreTask.Status != Done` (checked via `IActivitiesService`) **and**
the originating `SlaBreach` `Notification.Status != NotificationStatus.Acknowledged`. Either
one flipping is enough to mean "handled" and skip the escalation — a Department Lead
acknowledging the alert counts as much as the task itself reaching `Done`.

## Acceptance criteria

AC-1 (subscriber creates the Department Lead notification):
GIVEN the `activities` module publishes `IEventBus.Publish(new SlaBreachEvent(taskId,
assignedToUserId, departmentLeadId, breachedAt))`
WHEN the event reaches `SlaBreachEventSubscriber`
THEN a new `Notification` is persisted (verified via `INotificationRepository` or
`IAlertsService.ListAsync`) with `UserId = departmentLeadId`, `AlertType =
AlertType.SlaBreach`, `Status = NotificationStatus.Unread`, `Channel =
NotificationChannel.InApp`, and `RelatedEntityId = taskId`

AC-2 (escalation happy path):
GIVEN a `Notification` with `AlertType = AlertType.SlaBreach`, `Status =
NotificationStatus.Unread`, `RelatedEntityId = taskId`, `CreatedAt = clock.UtcNow.AddHours(-(graceHours + 1))`;
`IActivitiesService.GetByIdAsync(taskId, ct)` returns a task with `Status !=
TaskStatus.Done`; and that task's `StoreId` has one `User` with `Role =
StaffRole.StoreManager`
WHEN `IAlertsEscalationSweepService.SweepAsync` is invoked
THEN a new `Notification` is persisted with `UserId` equal to that Store Manager's `Id`,
`AlertType = AlertType.Escalation`, `RelatedEntityId = taskId`

AC-3 (grace period not yet elapsed):
GIVEN the same `SlaBreach` `Notification` but `CreatedAt = clock.UtcNow.AddHours(-(graceHours - 1))`
WHEN the sweep runs
THEN no `Escalation` `Notification` is created for that task

AC-4 (task already resolved):
GIVEN the `SlaBreach` `Notification` is older than the grace period, but
`IActivitiesService.GetByIdAsync(taskId, ct)` returns a task with `Status =
TaskStatus.Done`
WHEN the sweep runs
THEN no `Escalation` `Notification` is created for that task

AC-5 (breach already acknowledged):
GIVEN the `SlaBreach` `Notification` is older than the grace period and its own `Status =
NotificationStatus.Acknowledged`, even though the underlying task is still not `Done`
WHEN the sweep runs
THEN no `Escalation` `Notification` is created for that task

AC-6 (idempotency — already escalated):
GIVEN an `Escalation` `Notification` already exists with `RelatedEntityId = taskId`, and the
originating `SlaBreach` `Notification` for that same task is still eligible (older than
grace period, `Unread`, task not `Done`)
WHEN the sweep runs again
THEN no second `Escalation` `Notification` is created for that `taskId` (`AddAsync` for an
`Escalation`-typed `Notification` with that `RelatedEntityId` is not called again)

AC-7 (no Store Manager in store — skip, don't fail the run):
GIVEN an eligible `SlaBreach` `Notification` whose task's `StoreId` has **no** `User` of
`Role = StaffRole.StoreManager`, and a second, independent eligible `SlaBreach`
`Notification` for a task in a **different** store that does have a Store Manager
WHEN the sweep runs once over both
THEN no `Escalation` `Notification` is created for the first (no exception propagates), and
one **is** created for the second

AC-8 (grace period is configurable):
GIVEN `AlertsOptions.SlaEscalationGraceHours` is set to `1` (via `IOptions<AlertsOptions>`,
not the default `4`) and an eligible `SlaBreach` `Notification` with `CreatedAt =
clock.UtcNow.AddMinutes(-90)`
WHEN the sweep runs
THEN an `Escalation` `Notification` **is** created (90 minutes exceeds the configured 1-hour
grace period, proving the value is read from configuration rather than hardcoded)

AC-9 (endpoint, integration):
GIVEN an eligible `SlaBreach` `Notification` persisted through the running API (seeded
directly via `INotificationRepository`/`IUserRepository` where no HTTP endpoint exists to
create the precondition — e.g. no staff-creation endpoint) whose grace period has elapsed
WHEN a client sends `POST /api/alerts/sla-escalation-sweep`
THEN the response is `200 OK` with body `{ "escalationsCreated": <n> }` where `n >= 1`, and a
subsequent `GET /api/alerts?userId=<storeManagerId>` includes a `Notification` with
`AlertType = Escalation` and the expected `RelatedEntityId`

## Out of scope (do not implement in this sprint)

- Deduplicating repeated `SlaBreachEvent` publishes for the same `TaskId` — Sprint 1's
  `StoreTask.SlaBreachedAt` guard already prevents the event from being published more than
  once per task, so the subscriber does not need its own dedup logic.
- Wiring a real background scheduler/timer to call `AlertsEscalationSweepService.SweepAsync`
  automatically — stays manually/test-invokable via `POST /api/alerts/sla-escalation-sweep`,
  same as Sprint 1's sweep endpoint.
- Email/external delivery for either notification type — `Channel = NotificationChannel.InApp`
  only, consistent with the rest of the baseline's stub notification delivery.
- A further escalation tier beyond Store Manager (the prompt specifies exactly one escalation
  step).
- Changing `GET /api/alerts` or `PATCH /api/alerts/{id}/status` beyond what already exists —
  both notification types created by this sprint are ordinary `Notification` rows, already
  fully readable/acknowledgeable through the existing baseline endpoints.

## Handoff

This contract is read next by the Generator (`generator.agent.md`), which treats every AC
above as a hard requirement to self-check against before writing `generator-summary.md`.
