# Spec — SLA Breach Alerting

## Feature summary

When an operational `StoreTask` carrying `Priority = High` or `Priority = Critical` passes
its `DueDate` without reaching `Status = Done`, StoreOps must automatically raise an
`SLA_BREACH` notification to the task's Department Lead. If the breach is still unresolved
(task still not `Done`) after a configurable grace period, StoreOps escalates by raising a
further notification to the Store Manager. This touches the `activities` module (breach
detection, since it owns `StoreTask`) and the `alerts` module (notification delivery and
escalation, since it owns `Notification`), connected only via `IEventBus` per Rule 2 —
`activities` never calls `alerts` directly, and `alerts` never writes to `activities`'
repository.

## Affected modules

- `activities` — **primary** for Sprint 1: owns the SLA-breach detection sweep and the
  `SlaBreachEvent` publish. Read-only cross-module call into `staff`
  (`IStaffService.ListAsync`) to resolve the task's Department Lead.
- `alerts` — **primary** for Sprint 2: owns the `SlaBreachEvent` subscriber that creates the
  Department Lead notification, and the escalation sweep that creates the Store Manager
  notification after the grace period. Read-only cross-module calls into `activities`
  (`IActivitiesService.GetByIdAsync`, to confirm the task is still unresolved before
  escalating) and `staff` (`IStaffService.ListAsync`, to resolve the Store Manager).
- `staff` — read-only lookup target only; no changes.
- `programmes`, `reports` — untouched.

## Cross-module interaction

`activities` publishes via `IEventBus.Publish<SlaBreachEvent>(...)`. The event contract
already exists in the baseline (`StoreOps.Domain.Events.SlaBreachEvent`) and is reused
as-is:

```csharp
public sealed record SlaBreachEvent(
    Guid TaskId,
    Guid AssignedToUserId,
    Guid DepartmentLeadId,
    DateTimeOffset BreachedAt) : IDomainEvent;
```

`alerts` subscribes to `SlaBreachEvent` via `IEventBus.Subscribe<SlaBreachEvent>(...)` and,
on receipt, creates a `Notification` (`AlertType = SlaBreach`, `UserId = DepartmentLeadId`).
No new event type is needed for escalation — the escalation path is driven entirely inside
`alerts` by its own sweep over its own `Notification` store, reading `activities` and
`staff` only through their public Service interfaces (both read-only calls, permitted under
Rule 2).

## Sprint list

1. **Sprint 1 — SLA-breach detection (`activities`, primary)**: a sweep that finds tasks
   with `Priority ∈ {High, Critical}`, `Status != Done`, `DueDate` in the past, and
   `SlaBreachedAt == null`; resolves the task's Department Lead via `IStaffService`; publishes
   `SlaBreachEvent`; stamps `StoreTask.SlaBreachedAt` for idempotency. Includes a trigger
   endpoint (`POST /api/tasks/sla-sweep`) so the sweep can be run and demonstrated manually,
   since this baseline has no background scheduler.

2. **Sprint 2 — Notification + escalation (`alerts`, primary)**: a subscriber that turns each
   `SlaBreachEvent` into a `Notification` (`AlertType = SlaBreach`) for the Department Lead,
   plus a second sweep that finds `SlaBreach` notifications older than the configurable grace
   period whose task is still unresolved (checked via `IActivitiesService.GetByIdAsync`) and
   have not already been escalated, resolves the Store Manager via `IStaffService`, and
   creates a further `Notification` (`AlertType = Escalation`). Includes a trigger endpoint
   (`POST /api/alerts/sla-escalation-sweep`).

## Open questions

- **Department Lead / Store Manager resolution.** The data model has no formal link between
  a `StoreTask` and a specific Department Lead (`TaskCategory` and `UserProfile.Department`
  are both free-form and not joined). Resolved as: the first `User` with
  `Role = DepartmentLead` (respectively `Role = StoreManager`) returned by
  `IStaffService.ListAsync(task.StoreId, ct)`, ordered by `CreatedAt`. If a store has no user
  in that role, the sweep skips the task for that recipient (no exception) — logged as an
  unresolved case rather than failing the whole sweep. This is a stub-appropriate default
  consistent with StoreOps' "reference app, stub business logic" scope (app-context/SKILL.md);
  a real build would need an explicit department-to-lead mapping.
- **Grace period default.** Not specified in the prompt. Defaults to **4 hours**, configurable
  via `appsettings.json` under `Alerts:SlaEscalationGraceHours` (read through
  `IConfiguration`, no new DI infrastructure required).
- **Sweep trigger mechanism.** No background scheduler exists in this baseline (in-memory,
  no hosted services yet). Both sweeps are exposed as directly-invokable service methods
  (`ISlaSweepService.RunSweepAsync`, `ISlaEscalationSweepService.RunSweepAsync`) reachable
  either from a test or from the new trigger endpoints listed above. Wiring a real
  `BackgroundService`/timer is explicitly out of scope for both sprints.
- **New field: `StoreTask.SlaBreachedAt`.** Added (nullable `DateTimeOffset`, set once, in
  `activities` only) purely for Sprint 1's own idempotency check — a second sweep before the
  task resolves must not publish a second event for the same task. Escalation idempotency
  (Sprint 2) is tracked independently, inside `alerts`' own `Notification` store (an existing
  `Notification` with `AlertType = Escalation` and matching `RelatedEntityId` means "already
  escalated") — no cross-module field sharing needed, keeping Rule 1 intact.

STATUS: AWAITING APPROVAL
