# StoreOps Harness Starter Kit (.NET 8)

This folder is a **starting scaffold**, not a finished submission. It contains a fully
drafted orchestrator, agent files, and skill files consistent with the AI-Native Tech
Architect capstone brief (Build Track), customized for:

- Stack: .NET 8 / C# 12 / ASP.NET Core Web API
- Demonstration feature: SLA breach alerting
- Deployment: local Docker

## What's here

```
CLAUDE.md                  root orchestrator
BOOTSTRAP_PROMPT.md         the Section 2.3 bootstrap prompt, filled in for .NET 8 — run
                             this FIRST, before anything else, to generate the StoreOps
                             codebase itself
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
```

## What you still need to do

Per programme guidance, the capstone brief is the complete specification for this build —
there is no separate scaffold document to reconcile against. What's left is yours to do:

1. Create your own empty git repository (`storeops-harness-dotnet` is the suggested name)
   and copy this folder's contents into it.
2. Open it in Claude Code and run `BOOTSTRAP_PROMPT.md`'s prompt to generate the actual
   StoreOps source code — this starter kit only has the harness *around* the app; the app
   itself does not exist yet.
3. Verify, correct, and commit the baseline (Section 2.3, Step 4 of the brief).
4. Read every file in `.harness/`, adjust anything that doesn't match decisions you'd
   actually defend in the panel walkthrough — these are drafted to be StoreOps-specific and
   usable as-is, but the brief is explicit that the architectural judgment has to be yours,
   not an AI tool's (Section 8, "Note on AI tool use"). The four-project solution shape in
   `coding-conventions/SKILL.md` in particular is this kit's own proposal, not something
   dictated by the brief — keep it deliberately or change it.
5. Run the harness: `@planner` + the prompt in `PROMPT.md`, review `spec.md`, type
   `APPROVED`, let the Generator/Evaluator loop run, capture the artefacts.
6. Fill in `DESIGN_BRIEF.md`, `REFLECTION.md`, `DEPLOYMENT.md` from what actually happened.

See the accompanying development guide for the full phase-by-phase plan and a breakdown of
what's manual vs. AI-assistable at each step.
