# StoreOps Bootstrap Prompt — .NET 8

This is the bootstrap prompt, filled in for the .NET 8 stack. Paste this into
Claude Code **in your own empty git repository**, as the very first thing you do — before
any harness files exist. This generates the baseline StoreOps codebase the harness will
later govern.

```
Generate the StoreOps REST API project using ASP.NET Core Web API on .NET 8 (C# 12).

Requirements:
- Domain: retail store operations management
- 5 modules: activities (operational activities), programmes (store programmes),
  staff (store staff), alerts (ops alerts), reports (store/region metrics)
- Each module: 3 layers (Controllers/Routes → Service → Repository)
- Solution shape: StoreOps.Domain (entities, enums, event contracts), StoreOps.Application
  (service interfaces + implementations, IEventBus, AppError hierarchy), StoreOps.Infrastructure
  (in-memory repositories, InMemoryEventBus), StoreOps.Api (Controllers, Program.cs).
  Repository types are internal to StoreOps.Infrastructure and namespaced per module; only
  each module's service interface is public.
- Module boundary rules: no direct cross-module repository references; notifications and
  other cross-module side effects via an IEventBus only; staff module is read-only for
  other modules (read via IStaffService only)
- Typed error hierarchy: abstract AppError : Exception base class with Code, Message,
  StatusCode — no raw throw new Exception()/ArgumentException()/etc. in Services or
  Controllers; a global ExceptionHandlingMiddleware maps AppError to the HTTP response
- 9 REST endpoints (see .harness/skills/app-context/SKILL.md for the list)
- Test setup: xUnit + WebApplicationFactory for integration tests, xUnit + Moq for
  Application-layer unit tests
- Nullable reference types enabled solution-wide; StyleCop Analyzers configured
- Linting/type checking configured and passing

Generate stub implementations (not full business logic) — enough structure to compile,
run tests, and pass lint. Repository methods may be fully working in-memory
implementations (ConcurrentDictionary-backed); Service methods should implement real
validation and AppError cases but may leave narrow TODOs for complex business rules.

Use in-memory storage (no database required).
```

## After Claude Code generates the code

1. Review the output against `.harness/skills/app-context/SKILL.md` (module/entity model)
   and `.harness/skills/architecture-principles/SKILL.md` (the five enforcement rules) —
   both already in this starter kit. Correct anything that doesn't match: a repository
   accidentally left `public`, a raw exception type, a controller with business logic in it.
2. Run the verification commands:
   ```
   dotnet build && dotnet test
   ```
   Expected: build succeeded, 0 warnings (nullable enabled), all tests pass.
3. Start the app and verify one endpoint:
   ```
   dotnet run --project src/StoreOps.Api
   curl http://localhost:5000/api/tasks   # expect 200 OK
   ```
4. Commit: `git add -A && git commit -m "baseline: generated StoreOps scaffold"`.
   This is the baseline every later Generator diff is measured against.

Keep a short note of every correction you make in this step — these are exactly the
concrete examples that make `coding-conventions/SKILL.md` and `architecture-principles/
SKILL.md` credible rather than generic. If Claude Code got something StoreOps-specific
wrong here, that is worth strengthening in the skill file before you start the harness run
proper.
