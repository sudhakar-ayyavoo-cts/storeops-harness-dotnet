# Skill: coding-conventions

**Purpose.** The Generator-specific rules for writing idiomatic, consistent StoreOps C#
code — naming, project structure, DI, nullability, and the exact shape of the `AppError`
and `IEventBus` contracts. Read by the Generator.

## Nullable reference types

`<Nullable>enable</Nullable>` in every `.csproj`. A method that can legitimately not find
something returns `Task<Entity?>` and the caller handles `null` explicitly (usually by
throwing the relevant `NotFoundError : AppError`) — never a silent default or an
unannotated nullable-forgiving `!`.

## Project & namespace conventions

- Namespace mirrors folder: `StoreOps.Application.Activities`, `StoreOps.Domain.Alerts`, etc.
- One file per public type. Controllers named `<Module>Controller.cs`
  (`ActivitiesController.cs`). Services named `<Module>Service.cs` implementing
  `I<Module>Service`. Repositories named `InMemory<Entity>Repository.cs` implementing
  `I<Entity>Repository`.
- DI registrations for a module live in that module's own
  `ServiceCollectionExtensions.cs` (e.g. `AddActivitiesModule(this IServiceCollection)`),
  called from `Program.cs`. Do not hand-register module services directly in `Program.cs` —
  this keeps `Program.cs` a plain composition root and each module's DI footprint reviewable
  in one file.

## AppError hierarchy (see architecture-principles/SKILL.md, Rule 3, for the base class)

Every module defines its own error subtypes in `StoreOps.Application.<Module>.Errors`:

```csharp
namespace StoreOps.Application.Activities.Errors;

public sealed class TaskNotFoundError : AppError
{
    public override string Code => "TASK_NOT_FOUND";
    public override int StatusCode => 404;
    public TaskNotFoundError(Guid taskId) : base($"Task {taskId} was not found.") { }
}
```

Controllers never construct an HTTP status code by hand for a business-rule failure — they
let an `AppError` propagate to `ExceptionHandlingMiddleware`. A Controller action's only
direct `StatusCode`/`Ok`/`Created` calls are for the *success* path.

## IEventBus contract

```csharp
namespace StoreOps.Application.Common;

public interface IEventBus
{
    void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent;
}
```

`StoreOps.Infrastructure.EventBus.InMemoryEventBus` is the only implementation for this
capstone (no external broker required — in-memory storage only, per the bootstrap prompt).
Event contracts (`SlaBreachEvent`, etc.) live in `StoreOps.Domain.Events` as `record`
types implementing `IDomainEvent`, so they are usable from any module without creating a
dependency between two Application-layer modules.

```csharp
namespace StoreOps.Domain.Events;

public sealed record SlaBreachEvent(
    Guid TaskId,
    Guid AssignedToUserId,
    Guid DepartmentLeadId,
    DateTimeOffset BreachedAt) : IDomainEvent;
```

## Controller conventions

```csharp
[ApiController]
[Route("api/tasks")]
public sealed class ActivitiesController : ControllerBase
{
    private readonly IActivitiesService _service;
    public ActivitiesController(IActivitiesService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> List(
        [FromQuery] TaskStatus? status, CancellationToken ct)
        => Ok(await _service.ListAsync(status, ct));
}
```

- Route attribute uses the plural REST resource path exactly as specified in
  `app-context/SKILL.md`'s endpoint table.
- DTOs (`TaskDto`, `CreateTaskRequest`, etc.) live beside the Controller in
  `StoreOps.Api.Contracts.<Module>` — the Application layer's domain types
  (`StoreOps.Domain.Activities.Task`) are never returned directly from a Controller.
- Every action takes a `CancellationToken` parameter and threads it through.

## Async & CancellationToken

All Service and Repository methods are `async Task<T>` (or `async Task`), and accept a
`CancellationToken` as the last parameter, threaded through to any awaited call — even
against the in-memory store — so the pattern is already correct once a real persistence
layer replaces it later.

## What "stub implementation" means for this bootstrap

Per the bootstrap prompt: generate enough structure to compile, run tests,
and pass lint — not full business logic. Concretely: Repository methods may be a real,
working in-memory `ConcurrentDictionary`-backed implementation (this is cheap to make fully
correct and gives the Evaluator something real to test against); Service methods should
implement real validation and the real `AppError` cases, but may leave a deliberately
narrow TODO for complex business rules not yet covered by an AC — recorded as a "known gap"
in `generator-summary.md`, never silently.
