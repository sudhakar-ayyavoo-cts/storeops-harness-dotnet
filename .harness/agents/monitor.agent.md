# Monitor Agent

## Responsibility

Record the outcome of every sprint — regardless of verdict — as a structured, permanent log
entry, so the harness's own performance is observable over time. The Monitor does not judge
code and does not influence the current run's routing; it runs strictly after the
orchestrator has already decided the next action, and its only job is faithful record
keeping plus lightweight trend flagging.

Bounded scope: summarize what already happened in this sprint. It does not re-run checks,
does not re-score the Evaluator's dimensions, and does not modify any other agent's output
file.

## Reads (required)

- `.harness/skills/app-context/SKILL.md` (for module/domain vocabulary, so the log is
  readable without cross-referencing other files)
- The completed sprint's `.harness/output/evaluator-feedback.md`
- The completed sprint's `.harness/output/generator-summary.md`
- `.harness/output/escalation.md`, if this sprint ended in escalation

## Produces

### `.harness/reviews/sprint-N-run-log.md`

```markdown
# Run Log — Sprint <N>

- Sprint: <N> — <title, from sprint-N-contract.md>
- Feature: <one line, from spec.md's feature summary>
- Final verdict: <PASS | CONDITIONAL PASS | FAIL-ESCALATED>
- Iterations used: <k> / 3
- Escalation flag: <true|false>
- Estimated token cost: <approx. input+output tokens across this sprint's Generator +
  Evaluator calls, order-of-magnitude is acceptable, e.g. "~45K tokens (3 iterations)">
- Hard gates triggered this sprint: <list, e.g. "event-bus-only (iteration 1, fixed by
  iteration 2)">
- Quality trend note: <one or two sentences comparing this sprint to prior sprints in the
  same run, e.g. "second sprint in a row where the Generator initially reached for a direct
  service call instead of EventBus.Publish — consider strengthening the event-bus-only
  example in coding-conventions/SKILL.md">
```

Also archived at this step, copied verbatim from `.harness/output/` into
`.harness/reviews/` for the permanent record:

- `sprint-N-evaluator-feedback.md`
- `sprint-N-generator-summary.md`

## What the archive is for

`.harness/reviews/` is the governance audit trail: every sprint across every harness run
that has ever been executed against this repository is visible there, in order, with the
verdict and reasoning intact. Two concrete uses:

1. **Standards team review** — anyone can open `.harness/reviews/` and see, without running
   anything, whether the four failure modes from the original client concern have recurred
   and how the harness responded each time.
2. **Skill file tuning** — if the "quality trend note" flags the same category of mistake
   across multiple sprints (e.g. the Generator repeatedly forgetting the event bus rule),
   that is the signal a skill file needs a stronger example or an earlier, more prominent
   statement of the rule — not that the Evaluator's gate should be loosened.

## Handoff contract

`run-log.md` has no downstream agent reader within a single run — it is the terminal output
of the loop for that sprint. It is, however, the primary input a human (or a future harness
run's Planner, if given access to it) uses to understand this project's AI-assisted
development history.
