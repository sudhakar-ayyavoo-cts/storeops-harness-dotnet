# StoreOps Development Harness — Orchestrator

This file is read automatically by Claude Code when it starts in this repository. It is the
root of the harness: it defines how a feature request becomes governed, production-ready
code through a Planner → Generator → Evaluator loop, with a Monitor recording every
outcome to `.harness/reviews/` as a permanent audit trail.

Stack: **.NET 8 / C# 12 / ASP.NET Core Web API**. Reference application: **StoreOps**
(retail store operations management REST API — see `.harness/skills/app-context/SKILL.md`).

> Everything below is orchestration logic for *this* harness. It is not generic — the
> iteration cap, the approval marker, and the escalation format are the actual contract
> Claude Code follows when it drives the loop. Do not treat this as a template; if you
> change a rule here, update the corresponding skill/agent file so they stay consistent.

## 1. Entry prompt format

A developer starts a harness run with a single prompt addressed to the Planner:

```
@planner <feature description in one or two sentences>
```

Example (the demonstration run for this repository):

```
@planner Add SLA breach alerting: when a HIGH or CRITICAL task passes its due date without
reaching DONE, automatically fire a SLA_BREACH notification to the assigned Department Lead,
and escalate to STORE_MANAGER if unresolved after a configurable grace period.
```

On seeing a `@planner` prompt, Claude Code:

1. Loads `.harness/agents/planner.agent.md` and the skill files it lists as required reading.
2. Produces `.harness/output/spec.md`, ending with a `STATUS: AWAITING APPROVAL` marker.
3. Stops and waits. **No further action is taken until the developer types `APPROVED`.**

## 2. Agent files and how each is invoked

| Agent | File | Invoked | Skill files it must read first |
|---|---|---|---|
| Planner | `.harness/agents/planner.agent.md` | On a `@planner` prompt | app-context, architecture-principles, sprint-decomposition |
| Generator | `.harness/agents/generator.agent.md` | Automatically, once per sprint, after `APPROVED` or after a `CONDITIONAL PASS` / `FAIL` retry | app-context, architecture-principles, coding-conventions, how-to-test |
| Evaluator | `.harness/agents/evaluator.agent.md` | Automatically, immediately after every Generator run | architecture-principles, how-to-review, evaluation-criteria |
| Monitor | `.harness/agents/monitor.agent.md` | Automatically, immediately after every Evaluator verdict | app-context (reads the sprint's `evaluator-feedback.md` + `generator-summary.md`) |

Claude Code drives this sequence itself once the loop starts — the developer does not
manually re-invoke the Generator or Evaluator between iterations.

## 3. The run sequence

```
developer: "@planner <feature>"
   → Planner reads spec-relevant skills, decomposes the feature into sprint contracts
   → writes .harness/output/spec.md  (ends with STATUS: AWAITING APPROVAL)
   → Claude Code stops and displays spec.md

developer: "APPROVED"
   → Claude Code writes .harness/output/sprint-1-contract.md from spec.md
   → LOOP START (per sprint, sprint = 1, 2, 3, ...):

        Generator runs on sprint-N-contract.md
           → writes code under src/, plus .harness/output/generator-summary.md

        Evaluator runs on the Generator's diff + generator-summary.md
           → writes .harness/output/evaluator-feedback.md with a verdict:
             PASS | CONDITIONAL PASS | FAIL

        Orchestrator (this file, via Claude Code) reads the verdict:
           - PASS              → Monitor archives the sprint, advance to sprint N+1
                                  (or finish, if this was the last sprint)
           - CONDITIONAL PASS  → Monitor archives the sprint, advance to sprint N+1,
                                  but the sprint's minor findings are appended to that
                                  sprint's run-log.md as "carried debt" for visibility
           - FAIL              → increment iteration count for sprint N;
                                  if iteration count <= 3: feed evaluator-feedback.md
                                  back to the Generator as additional context and re-run
                                  the Generator for sprint N (same contract, no re-plan)
                                  if iteration count > 3: ESCALATE

        Monitor runs after every verdict (PASS, CONDITIONAL PASS, FAIL, or escalation)
           → writes .harness/reviews/sprint-N-run-log.md
   → LOOP ENDS when the last sprint reaches PASS/CONDITIONAL PASS, or an escalation fires
```

Machine-readable markers Claude Code looks for when routing:

- `STATUS: AWAITING APPROVAL` — end of `spec.md`, halts the run until `APPROVED`.
- `VERDICT: PASS` / `VERDICT: CONDITIONAL PASS` / `VERDICT: FAIL` — first line of
  `evaluator-feedback.md`, drives the routing table above.
- `ESCALATION: TRUE` — written by the orchestrator into `escalation.md` when a sprint
  exceeds the iteration cap.

## 4. Iteration cap and escalation

**Maximum 3 Generator iterations per sprint.** A sprint that has not reached PASS or
CONDITIONAL PASS after 3 Evaluator FAIL verdicts is escalated — the harness does not
attempt a 4th iteration on its own judgement, because a 4th attempt without new human
input is unlikely to fix a problem the Evaluator has already flagged three times, and
burning further tokens on it fails the cost-awareness goal of the harness.

On escalation, Claude Code writes `.harness/output/escalation.md`:

```markdown
# Escalation — Sprint <N>

- Sprint: <N> — <sprint contract title>
- Iterations used: 3 / 3
- Last verdict: FAIL
- Blocking issue: <the single highest-severity unresolved finding from
  evaluator-feedback.md, with file + line reference>
- Evaluator feedback history: linked to .harness/reviews/sprint-<N>-evaluator-feedback.md
  for each of the 3 iterations
- Recommended next step: <what the Evaluator's own feedback suggests trying — e.g.
  "the Generator repeatedly imported ActivitiesRepository directly from the alerts
  module; consider re-scoping this sprint contract to require the event bus explicitly
  in the Given/When/Then wording">
```

Claude Code then stops the loop and hands control back to the developer. The developer
is the only party who can close an escalation — by fixing the code by hand, rewriting the
sprint contract, or explicitly instructing the harness to retry.

## 5. Context scoping strategy

Each agent invocation is given only the skill files listed for it in Section 2, plus the
specific handoff file(s) it needs for that step (e.g. the Generator sees
`sprint-N-contract.md`, not `spec.md` in full, once a sprint is underway). This keeps each
agent's context window scoped to what its role actually needs and keeps token cost
predictable across a long run.

Context is **reset between sprints**, not carried forward as conversation history: sprint
N+1's Generator run starts from `sprint-(N+1)-contract.md` plus the shared skill files, not
from the accumulated back-and-forth of sprint N's iterations. This prevents context window
degradation (drift, stale assumptions, quote of an earlier failed attempt bleeding into a
later one) across multi-sprint runs. What *does* carry forward across sprints is the
committed code in `src/` itself — the Generator always works against the current state of
the repository, so sprint N+1 sees the real, evaluated output of sprint N, not a summary of
it.

Within a single sprint's retry loop (FAIL → re-run Generator), the Evaluator's feedback for
the failed iteration *is* explicitly carried forward — that is the one place accumulated
context is intentional, because the Generator needs to know what it got wrong last time
inside the same sprint boundary.

## 6. Relationship to CI/CD

`.harness/` is deliberately separate from `.github/` (or any CI/CD configuration
directory). The Evaluator's automated checks (`dotnet build && dotnet test`, plus the
architecture rule checks — see `.harness/skills/evaluation-criteria/SKILL.md`) **precede**
CI: they run locally, in Claude Code, before a commit is proposed. They are not a
replacement for CI/CD — the same `dotnet build && dotnet test` command (and, once
configured, the dependency-boundary check) should also be wired into the project's actual
CI pipeline in `.github/workflows/`, so that code reaching `main` by any path (not just
through the harness) is still checked. The harness's job is to catch the four known failure
modes *before* a PR is opened; CI's job is to guarantee they were caught, regardless of how
the code was written.

## 7. Repository structure this orchestrator assumes

```
CLAUDE.md                      this file
PROMPT.md                      the feature prompt used for the demonstration run
.harness/agents/                planner.agent.md, generator.agent.md, evaluator.agent.md, monitor.agent.md
.harness/skills/                one subdirectory per skill, each containing SKILL.md
.harness/output/                spec.md, sprint-N-contract.md, generator-summary.md,
                                 evaluator-feedback.md, escalation.md — working files,
                                 gitignored during an active run
.harness/reviews/               sprint-N-run-log.md, sprint-N-evaluator-feedback.md,
                                 sprint-N-generator-summary.md — committed permanently
src/                            StoreOps.Api, StoreOps.Application, StoreOps.Domain, StoreOps.Infrastructure
tests/                          StoreOps.Api.Tests (mirrors src/ module structure)
```
