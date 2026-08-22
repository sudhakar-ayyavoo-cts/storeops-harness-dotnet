# Skill: how-to-test

**Purpose.** Generator-specific rules for what must be tested, at which level, and with
what xUnit/`WebApplicationFactory` patterns — so tests actually verify business rules
(the third failure mode from Section 2 of the brief: "tests that asserted HTTP status
codes but did not verify business rule compliance"), not just that an endpoint returns 200.

## The rule this file exists to enforce

**A test that only asserts an HTTP status code is not a passing test for this project.**
Every test must additionally assert on at least one of: the response body's business-rule
fields, a persisted state change (read back via the repository), or an `IEventBus.Publish`
call with its payload. This directly targets Failure Mode 3 from the client context.

Bad (rejected by the Evaluator even if it's green):
```csharp
[Fact]
public async Task Post_ReturnsCreated()
{
    var response = await _client.PostAsJsonAsync("/api/tasks", request);
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

Good:
```csharp
[Fact]
public async Task Post_WithValidRequest_CreatesTaskAndReturnsLocation()
{
    var response = await _client.PostAsJsonAsync("/api/tasks", request);

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var body = await response.Content.ReadFromJsonAsync<TaskDto>();
    body!.Status.Should().Be(TaskStatus.Todo);          // business-rule field
    body.Priority.Should().Be(request.Priority);

    var stored = await _taskRepository.GetByIdAsync(body.Id);
    stored.Should().NotBeNull();                         // persisted state, not just the response
}
```

## Test levels

| Level | Framework | What it covers | Where |
|---|---|---|---|
| Unit | xUnit + `Moq`/`NSubstitute` (mock repository & event bus) | Service-layer business rules, `AppError` cases, event-bus calls | `tests/StoreOps.Application.Tests/<Module>/` |
| Integration | xUnit + `WebApplicationFactory<Program>` | Full HTTP round-trip through Controller → Service → in-memory Repository; middleware error mapping | `tests/StoreOps.Api.Tests/<Module>/` |

Every sprint contract AC that describes an HTTP-observable outcome gets an integration
test; every AC that describes an internal business rule or an event-bus call gets (at
minimum) a unit test — some ACs warrant both.

## Event-bus assertions

Because Rule 2 (event bus only) is a hard gate, any AC involving a cross-module side effect
must have a test that asserts on the *event*, not on a side effect of the event having been
handled elsewhere:

```csharp
var eventBus = new Mock<IEventBus>();
var sut = new SlaSweepService(taskRepository, eventBus.Object, clock);

await sut.SweepAsync(CancellationToken.None);

eventBus.Verify(b => b.Publish(It.Is<SlaBreachEvent>(
    e => e.TaskId == overdueTask.Id && e.AssignedToUserId == overdueTask.AssignedTo)),
    Times.Once);
```

## Coverage expectation

Minimum 80% line coverage on `StoreOps.Application` (Service layer, where the business
rules live) for files touched by a sprint, measured via `dotnet test
/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura` (or `dotnet-coverage`/Coverlet
equivalent). Controller and Repository coverage is not separately gated — they are thin by
convention (per architecture-principles, Rule 4) and are exercised indirectly by the
integration tests.

## Idempotency and edge cases

Any sprint whose AC involves a scheduled sweep, retry, or bulk operation must include a
test for the "run twice" / "partial failure" case (see the idempotency note in
sprint-decomposition/SKILL.md's worked example) — not just the happy path.
