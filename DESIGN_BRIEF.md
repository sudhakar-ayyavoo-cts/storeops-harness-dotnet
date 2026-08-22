# Harness Design Brief — StoreOps (.NET 8)

> Template only. Every bracketed prompt below must be replaced with your own reasoning
> from your actual build and demonstration run — reviewers are explicitly checking for
> decisions specific to what you built, not restated brief content (Section 10: "Quality
> Markers"). Delete this notice line before submitting. Target 3–5 pages including
> diagrams.

## Section A — Intent Decomposition (0.5–1 page)

- How did you break the SLA breach alerting feature into sprint contracts? Why did you
  draw the sprint boundary where you did — what would have gone wrong (for the Evaluator,
  or for reviewability) if you'd made it one sprint, or five?
  [Your answer — reference your actual `spec.md` sprint list.]
- How did you structure acceptance criteria using GIVEN/WHEN/THEN, and what specifically
  made a criterion testable rather than subjective? Give a concrete example of an AC you
  rejected or rewrote during planning, and why the rewrite was better.
  [Your answer.]
- Reproduce one full sprint contract entry (AC in full) from your actual run.
  [Paste from your real `sprint-1-contract.md`.]

## Section B — Governance Framework (1 page)

- Your skill file strategy: which files govern which aspect of the codebase, which are
  shared across agents and why, and — for each — what it encodes that is *specific to
  StoreOps* rather than generic. (If you used this starter kit's skill files as a base,
  say what you changed and why, based on what you actually observed during your run.)
  [Your answer.]
- How does `.harness/reviews/` function as a governance audit trail in your build — what
  exactly is captured, who would look at it, and walk through how it would surface a
  recurring quality issue if the same architecture violation appeared in two different
  sprints.
  [Your answer — if you have a real run-log.md showing a repeated pattern, use it.]
- Pick one skill file rule that encodes a specific StoreOps decision (e.g. the
  `internal`-visibility enforcement of the module boundary rule, or the event-bus-only
  rule). State the rule and explain concretely what would break in your codebase without
  it — ideally with a real example from a Generator attempt that violated it.
  [Your answer.]

## Section C — Non-Determinism Strategy (1 page)

- Your evaluation dimensions and weights (Architecture Compliance 40% / Correctness 35% /
  Code Quality & Tests 25%, if using this starter kit's framework) — why these weights fit
  the StoreOps context specifically, not just "in general."
  [Your answer — you may keep this starter kit's rationale if it matches your reasoning, or
  argue for different weights based on what you observed.]
- Your hard gates — for each, name the specific failure mode it prevents (tie back to
  Section 2 of the brief) and explain why it cannot be a soft/scored check instead.
  [Your answer.]
- Walk through one real example from your demonstration run: variable Generator output →
  the specific checks that ran → the deterministic verdict produced. Use an actual
  `evaluator-feedback.md` from your `.harness/reviews/`, not a hypothetical.
  [Your answer.]
- Your escalation path: what triggers it (iteration count, per `CLAUDE.md`), what the
  escalation output contains, and who acts on it. Did your run actually hit escalation? If
  so, describe it; if not, describe what would have happened at iteration 4.
  [Your answer.]

## Section D — Architectural Decisions (0.5–1 page)

Document 2–3 real decisions from your build. For each: the decision, the alternatives you
considered, your rationale, and an assumption it depends on. Candidates from this starter
kit you may have kept, changed, or rejected:

1. Enforcing the module boundary via C# `internal` visibility + compile errors, rather than
   a runtime dependency-analyser tool (the approach the Node.js reference harness uses).
   [Your decision, alternatives, rationale, assumption.]
2. Splitting `src/` into four projects (Domain/Application/Infrastructure/Api) rather than
   folders within a single project.
   [Your decision, alternatives, rationale, assumption.]
3. [A third decision specific to your actual build — e.g. how you chose the 3-iteration
   cap, how you scoped context between sprints, or a deployment decision.]

---

*Reminder: reviewers will probe these sections directly in the panel walkthrough (Section
6.4 of the brief) — write what you actually decided and observed, not what a generic good
harness would say.*
