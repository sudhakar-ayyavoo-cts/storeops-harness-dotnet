# StoreOps Harness Starter Kit (.NET 8)

This repository contains a working StoreOps baseline (ASP.NET Core Web API) plus the
harness — orchestrator, agent files, and skill files — that drives further feature work
through it, consistent with the AI-Native Tech Architect capstone brief (Build Track).

- Stack: .NET 8 / C# 12 / ASP.NET Core Web API
- Demonstration feature: SLA breach alerting
- Deployment: local Docker

## What's here

```
CLAUDE.md                  root orchestrator
BOOTSTRAP_PROMPT.md         the bootstrap prompt used to generate the baseline
PROMPT.md                   the feature prompt for the demonstration run
DESIGN_BRIEF.md             template with guided prompts — NOT filled in; the reasoning
                             must be yours, from your actual build
REFLECTION.md               template — same as above
DEPLOYMENT.md               template — same as above
Dockerfile, docker-compose.yml
.gitignore
.harness/agents/            planner.agent.md, generator.agent.md, evaluator.agent.md,
                             monitor.agent.md
.harness/skills/            7 skill files (minimum required is 6):
                               app-context, architecture-principles, sprint-decomposition,
                               coding-conventions, how-to-test, how-to-review,
                               evaluation-criteria
.harness/output/            empty — this is where the harness writes working files during
                             a live run (gitignored)
.harness/reviews/            empty — this is where you commit the permanent audit trail
                             after your demonstration run
src/StoreOps.Api             controllers, contracts (DTOs), middleware, Program.cs
src/StoreOps.Application     services, request/response models, repository interfaces
src/StoreOps.Domain          entities, enums, domain events — no framework dependencies
src/StoreOps.Infrastructure  in-memory repository implementations, event bus, demo seeder
tests/StoreOps.Api.Tests           integration tests (WebApplicationFactory)
tests/StoreOps.Application.Tests   unit tests
```

## Running locally

```
dotnet build
dotnet test
dotnet run --project src/StoreOps.Api
```

The API uses in-memory repositories (no database required), so every restart starts from
a clean state. When run in the `Development` environment (the default for `dotnet run`),
`DemoDataSeeder` (`src/StoreOps.Infrastructure/Seed/DemoDataSeeder.cs`) pre-populates it
with a small, cross-referenced demo dataset — two stores, six staff users across every
role, tasks in every status (including one overdue CRITICAL task for exercising SLA-breach
behaviour), two programmes, notifications, and reports. All demo users share the password
`Demo@123` (login is a plaintext comparison for now — see `StaffService`). The seeder does
not run outside `Development`.

## What you still need to do

Per programme guidance, the capstone brief is the complete specification for this build —
there is no separate scaffold document to reconcile against. The baseline codebase has
already been generated and committed (see git history); what's left is yours to do:

1. Read every file in `.harness/`, adjust anything that doesn't match decisions you'd
   actually defend in the panel walkthrough — these are drafted to be StoreOps-specific and
   usable as-is, but the brief is explicit that the architectural judgment has to be yours,
   not an AI tool's (Section 8, "Note on AI tool use"). The four-project solution shape in
   `coding-conventions/SKILL.md` in particular is this kit's own proposal, not something
   dictated by the brief — keep it deliberately or change it.
2. Run the harness: `@planner` + the prompt in `PROMPT.md`, review `spec.md`, type
   `APPROVED`, let the Generator/Evaluator loop run, capture the artefacts.
3. Fill in `DESIGN_BRIEF.md`, `REFLECTION.md`, `DEPLOYMENT.md` from what actually happened.

See the accompanying development guide for the full phase-by-phase plan and a breakdown of
what's manual vs. AI-assistable at each step.
