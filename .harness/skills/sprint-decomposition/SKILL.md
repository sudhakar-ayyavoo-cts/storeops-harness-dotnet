# Skill: sprint-decomposition

**Purpose.** Give the Planner concrete rules for how to slice a feature prompt into sprint
contracts, and what makes a GIVEN/WHEN/THEN acceptance criterion testable rather than
subjective. Read by the Planner only.

## Sizing a sprint

A sprint should be the smallest unit that:

1. Touches **one primary module** (a sprint may *read* from a second module via its Service
   interface, per Rule 2 in architecture-principles, but its writes stay inside one module).
2. Crosses **all three layers it needs**, not a partial slice — a sprint that adds a
   Service method with no Controller route to call it, or a Controller route with no test,
   is not done; it is unevaluable as a coherent unit.
3. Can be verified by `dotnet build && dotnet test` plus the architecture hard gates,
   without needing the *next* sprint to exist first.

As a default: **one sprint per module-crossing side effect, plus its direct dependencies.**
For the SLA breach alerting feature, that yields a natural 2–3 sprint decomposition:

- **Sprint 1 (activities/alerts boundary):** the SLA-breach detection logic in
  `activities` and the `SlaBreachEvent` contract, publishing via `IEventBus` — proves Rule 2
  (event bus only) end to end without yet requiring the `alerts` module's own notification
  delivery to exist.
- **Sprint 2 (alerts):** the `alerts` module's subscriber that turns an `SlaBreachEvent`
  into a `Notification` record and (stub) delivery, plus the escalation-after-grace-period
  timer logic.
- **Sprint 3 (optional, if scope allows):** the read endpoint(s) staff use to see their
  alerts, if not already covered by baseline.

Do not create a sprint smaller than "one full vertical slice through the layers it needs" —
a sprint that is just "add the enum value" or just "add the DTO" produces nothing the
Evaluator can meaningfully grade against an AC.

## Writing a testable GIVEN/WHEN/THEN

A criterion is testable when all three clauses are concrete:

- **GIVEN** names real StoreOps types and real enum values, not placeholders — "a Task"
  is not enough; "a Task with Priority = Critical, Status = InProgress, DueDate 2 hours in
  the past" is.
- **WHEN** names the exact action — an HTTP call with method + route + relevant body
  fields, or a specific method invocation if the trigger isn't HTTP (e.g. a scheduled sweep).
- **THEN** names an outcome that can be mechanically checked: an HTTP status + response
  shape, a specific `IEventBus.Publish` call with its event type and key payload fields, or
  a persisted state change verifiable via the repository's own read method.

**Reject these patterns** when writing (or reviewing) an AC:
- "...handles the error appropriately" → name the `AppError` subtype and status code.
- "...notifies the right people" → name the event and its recipient-selection field.
- "...works as expected" → not an AC at all; restate as an observable outcome.

## Worked example (bad → good)

Bad: `WHEN a task breaches SLA THEN the right people are notified.`

Good:
```
GIVEN a Task with Priority = Critical, Status = InProgress, DueDate 1 hour in the past,
      AssignedTo = a DepartmentLead user
WHEN the SLA sweep evaluates this task
THEN IEventBus.Publish is called exactly once with an SlaBreachEvent whose
     RecipientId equals the task's AssignedTo, and the task's SlaBreachedAt
     timestamp is set (idempotent: a second sweep before resolution does not
     publish a second event for the same task)
```

Note the added idempotency clause — StoreOps sprints should default to specifying the
"run twice" case for anything triggered by a sweep/timer, since that is the class of bug
most likely to slip past a single-pass Generator implementation.
