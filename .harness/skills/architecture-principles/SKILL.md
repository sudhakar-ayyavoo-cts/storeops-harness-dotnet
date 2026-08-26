# Skill: architecture-principles

**Purpose.** State the four non-negotiable StoreOps architecture rules, map each to the
client failure mode it prevents, and say exactly how it is enforced in this .NET 8
solution. Read by all four agents — this is the file every hard gate ultimately traces
back to.

Each rule below corresponds directly to one of the four failure modes the standards team
observed in the prior AI-assisted experiment. If you are
tempted to soften one of these into a "best effort" suggestion, don't — that is exactly how
the original failures happened.

## Rule 1 — Module boundary (prevents: direct imports bypassing service boundary)

**No module's Infrastructure repository may be referenced from outside that module.**
Cross-module reads go through the target module's public *Service interface* only —
never its repository, never its Infrastructure types.

*Enforcement:*
- Repository classes and repository interfaces are declared `internal` to
  `StoreOps.Infrastructure` and namespaced per module (e.g.
  `StoreOps.Infrastructure.Activities.InMemoryTaskRepository` is `internal`). A cross-module
  reference to it is a **compile error**, not a lint warning — this is the .NET-specific
  enforcement mechanism, and it is stronger than the dependency-analyser approach used in
  the Node.js reference harness.
- Only each module's `Application`-layer service interface (e.g. `IActivitiesService`) is
  `public`, and is the only thing another module may inject.
- Automated check: `dotnet build` with `TreatWarningsAsErrors=true` and nullable/visibility
  warnings enabled will fail the build on an accidental `public` repository. The Evaluator
  additionally greps the diff for any `using StoreOps.Infrastructure.<OtherModule>` outside
  that module's own folder as a belt-and-braces check.

## Rule 2 — Event bus only (prevents: missing event bus integration / direct writes to sibling repositories)

**Side effects that cross a module boundary must be raised via `IEventBus.Publish(...)` —
never by directly calling another module's service to cause a write, and never by writing
to another module's repository.**

Concretely: when a `CRITICAL` task becomes overdue, the `activities` module publishes an
`SlaBreachEvent`; it does not call `INotificationService.Send(...)` directly, and it
certainly does not construct an `alerts`-module `Notification` and write it via that
module's repository. The `alerts` module subscribes to `SlaBreachEvent` and decides for
itself how to notify.

*Enforcement (LLM-assessed, per evaluation-criteria/SKILL.md):* the Evaluator scans the
diff for any call from one module's Service into another module's Service where the
callee's method has a side effect (a write) rather than a read — e.g. any call resembling
`_notificationService.Send(...)`, `_reportService.Generate(...)` reached from outside their
own module — and fails the check if found. Calling another module's service for a
**read-only lookup** (e.g. `activities` calling `IStaffService.GetById(...)` to validate an
assignee exists) is allowed; that is the one form of direct cross-module service call this
project permits.

## Rule 3 — Error contract (prevents: raw Error throws bypassing AppError)

**No `throw new Exception(...)` (or any built-in exception type) in Service or Routes
(Controller) code.** All domain/business errors must be an `AppError` subtype, carrying
`Code`, `Message`, and `StatusCode`.

```csharp
public abstract class AppError : Exception
{
    public abstract string Code { get; }
    public abstract int StatusCode { get; }
    protected AppError(string message) : base(message) { }
}

public sealed class TaskAlreadyResolvedError : AppError
{
    public override string Code => "TASK_ALREADY_RESOLVED";
    public override int StatusCode => 409;
    public TaskAlreadyResolvedError(Guid taskId)
        : base($"Task {taskId} is already resolved.") { }
}
```

A global `ExceptionHandlingMiddleware` in `StoreOps.Api` catches `AppError` and maps
`Code`/`StatusCode` to the HTTP response; anything that is *not* an `AppError` reaching the
middleware is itself treated as a defect (logged as a harness violation, returns 500).

*Enforcement:* Roslyn analyzer / custom `dotnet build` warning (or, at minimum, an
Evaluator text-scan for `throw new (?!AppError-derived-type)` patterns in
`Application/**/*.cs` and `Api/Controllers/**/*.cs`) — automated + LLM-assessed.

## Rule 4 — Layer separation (prevents: routes/repositories doing another layer's job)

**Routes (Controllers) → Service → Repository, no skipping.** Controllers contain HTTP
concerns only (model binding, status codes, `[Authorize]`) and call exactly one Service
method per action — no business rules in a controller action body. Repositories contain
data access only — no `IEventBus`, no `HttpClient`, no calls to another Service.

*Enforcement (LLM-assessed):* the Evaluator checks `generator-summary.md`'s "Layer map"
against the actual diff — every changed file must be classified as Routes, Service, or
Repository, and its content must match that classification (no `if` business-rule branching
in a Controller; no `IEventBus` reference in an `Infrastructure/**/*Repository.cs` file).

## Rule 5 — Read-only reports (prevents: reports module producing side effects in other modules)

**`reports` aggregates data from `activities`, `programmes`, and `staff` via their public
Service interfaces; it never calls a write method on any of them, and it has no repository
dependency into another module.**

*Enforcement (LLM-assessed):* Evaluator confirms every `reports`-module call into another
module's Service is a `Get*`/`List*`/query-shaped method, never a `Create*`, `Update*`,
`Delete*`, or command-shaped one.

## Summary table (for quick reference during review)

| Rule | Automated check | LLM-assessed check |
|---|---|---|
| Module boundary | `dotnet build` (internal visibility → compile error) | grep for cross-module `Infrastructure` `using` |
| Event bus only | — | diff scan for direct cross-module write-service calls |
| Error contract | Roslyn analyzer / text-scan for raw `throw new Exception` | confirms `AppError` subtype used correctly (right `Code`/`StatusCode`) |
| Layer separation | — | diff vs. generator-summary layer map |
| Read-only reports | — | diff scan: reports → other modules calls are query-shaped only |
