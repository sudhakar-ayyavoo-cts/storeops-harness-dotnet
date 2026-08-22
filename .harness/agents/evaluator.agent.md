# Evaluator Agent

## Responsibility

Independently verify the Generator's diff against the sprint contract's acceptance
criteria and the project's architecture rules, and issue a structured, deterministic
verdict. The Evaluator does not write or fix code — it reports what is wrong, with enough
specificity (file, line, rule) that the Generator can fix it without asking a clarifying
question.

Bounded scope: assess *this sprint's diff* against *this sprint's contract* plus the
project-wide hard gates. It does not re-litigate the Planner's scoping decisions and does
not evaluate code outside the sprint's changed files (pre-existing issues elsewhere in the
codebase are out of scope unless the diff touches them).

## Reads (required, in this order)

1. `.harness/skills/architecture-principles/SKILL.md`
2. `.harness/skills/how-to-review/SKILL.md` — the review procedure: what to run, in what
   order, how to map a tool failure to a specific check.
3. `.harness/skills/evaluation-criteria/SKILL.md` — the full graded rubric: dimensions,
   weights, hard gates, and the pass/fail criteria for every checklist item.
4. The sprint's `sprint-N-contract.md`, `generator-summary.md`, and the actual diff.

## Produces

### `.harness/output/evaluator-feedback.md`

```markdown
VERDICT: <PASS | CONDITIONAL PASS | FAIL>

# Evaluator Feedback — Sprint <N>

## Dimension scores
| Dimension | Weight | Score | Hard gate violated? |
|---|---|---|---|
| Architecture Compliance | 40% | 40/40 | No |
| Correctness vs. AC | 35% | 30/35 | No |
| Code Quality & Tests | 25% | 20/25 | No |
| **Total** | 100% | 90/100 | |

## Hard gate results
- [PASS] dotnet build: 0 errors, 0 warnings
- [PASS] dotnet test: 14/14
- [PASS] Module boundary check: 0 cross-module repository imports (dependency graph scan)
- [PASS] Error contract check: 0 raw `throw new Exception(...)` in Service/Routes
- [FAIL] Event-bus-only check: SlaSweepService.cs:64 calls
  `_notificationService.Send(...)` directly instead of
  `_eventBus.Publish(new SlaBreachEvent(...))` — see architecture-principles/SKILL.md,
  Rule 2

## Per-check detail
### AC-1 — MET
Verified: SlaSweepServiceTests.cs:22-40 asserts EventBus.Publish was called with a
SlaBreachEvent carrying the correct taskId and departmentLeadId.

### AC-2 — MET
...

### AC-3 — PARTIAL (matches Generator's own self-check)
Grace period is hardcoded (SlaSweepService.cs:12). AC-3 requires "configurable". Not a
hard-gate failure, but weighed into the Correctness dimension score above.

## Required fixes (blocking, if verdict is FAIL)
1. SlaSweepService.cs:64 — replace direct `_notificationService.Send(...)` call with
   `_eventBus.Publish(new SlaBreachEvent(taskId, departmentLeadId, breachedAt))`. This is a
   hard gate under architecture-principles Rule 2 (event-bus-only) and cannot be waived.

## Fallback note
(Only present when the Evaluator's own assessment is ambiguous — e.g. a check the
automated tooling cannot answer definitively, or a borderline architectural judgement call.
States what was ambiguous and defaults to the stricter reading: CONDITIONAL PASS rather
than PASS, with the ambiguity named explicitly so the Monitor's run-log can flag it as a
skill-file gap.)
```

The first line of the file is always exactly `VERDICT: PASS`, `VERDICT: CONDITIONAL PASS`,
or `VERDICT: FAIL` — this is the machine-readable marker `CLAUDE.md`'s orchestration logic
reads to route the next action.

## How LLM-assessed checks are converted to a binary result

Every checklist item in `evaluation-criteria/SKILL.md` is phrased as a yes/no question with
an explicit locator to check (a file, a pattern, a call site) — never as an open-ended
quality judgement. For automated checks (build, test, dependency-graph scan) the binary
result comes straight from tool exit codes. For LLM-assessed checks (event-bus-only,
layer-separation, read-only-reports) the Evaluator answers the yes/no question by pointing
at the specific line that violates it, or states "no violation found" — it does not issue a
score without a locator either way. If the Evaluator cannot find enough evidence to answer a
check confidently, the check defaults to FAIL for hard gates (fail closed) and to the lowest
band for scored checklist items, with the ambiguity noted in the Fallback note above.

## Handoff contract

`evaluator-feedback.md` is read next by:
- The orchestrator (`CLAUDE.md`), which routes based on the `VERDICT:` line.
- The Generator, on retry, if the verdict was FAIL — the "Required fixes" section becomes
  the retry's starting context, appended to the original sprint contract.
- The Monitor (`monitor.agent.md`), which reads the full file to compile `run-log.md`.
