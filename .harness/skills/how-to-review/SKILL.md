# Skill: how-to-review

**Purpose.** The Evaluator's step-by-step review procedure — what to run, in what order,
and how a tool's raw output maps onto a specific check in `evaluation-criteria/SKILL.md`.
Read by the Evaluator, together with `evaluation-criteria/SKILL.md` (which holds the
weights and pass/fail bar; this file holds the *procedure*).

## Review order

Run cheapest/most-deterministic checks first — this fails fast and keeps LLM-assessed
review time focused only on what automated tooling could not already decide.

1. **Build.** `dotnet build --configuration Release -warnaserror`
   - Any error or warning (nullable, unused `using`, accidental `public` on a
     module-internal type) → hard gate FAIL immediately. Do not proceed to run tests if the
     build fails — record the build output verbatim in evaluator-feedback.md and stop.

2. **Test.** `dotnet test --no-build --configuration Release`
   - Any failing test → hard gate FAIL. Record which test(s), and whether the failure is in
     a test this sprint added (Generator's own work is wrong) or a pre-existing test
     (Generator's change broke something outside its stated scope — call this out
     specifically, it is a scope-discipline problem worth flagging even if not this sprint's
     direct fault).

3. **Coverage.** Parse the Coverlet/cobertura output for files changed this sprint.
   - Below 80% on `StoreOps.Application` files touched → hard gate FAIL (Code Quality &
     Tests dimension, per evaluation-criteria/SKILL.md). This is the one Code Quality gate
     that is a hard gate rather than a scored checklist item — the reasoning is that a
     sprint with too little test evidence can't be trusted on the *other* dimensions
     either, since "MET" self-check claims are only as verifiable as the tests behind them.

4. **Module boundary scan.** Search the diff for any `using StoreOps.Infrastructure.<Module>`
   where `<Module>` differs from the file's own module folder. Cross-reference against the
   build result — since repositories are `internal`, most violations are already build
   failures; this step catches anything that compiled anyway (e.g. reflection-based access,
   which itself is worth flagging as suspicious).

5. **Error contract scan.** Search Service/Controller diff lines for `throw new` where the
   type is not an `AppError` subtype (`throw new ArgumentException`, `throw new
   InvalidOperationException`, bare `throw new Exception`, etc.).

6. **Event-bus-only review (LLM-assessed).** Read every Service method touched this sprint
   that has a cross-module effect. For each, confirm it calls `IEventBus.Publish` rather
   than another module's Service write method. This requires understanding intent, not just
   pattern-matching — a call like `_activitiesService.GetById(...)` from within `reports` is
   fine (read), but `_activitiesService.UpdateStatus(...)` from `reports` is not, and no
   simple text search reliably tells those apart, hence LLM assessment.

7. **Layer separation review (LLM-assessed).** Compare `generator-summary.md`'s "Layer map"
   claim for each changed file against its actual content. A Controller with an `if` branch
   implementing a business rule (not just request validation) fails this check even if the
   Generator labeled it correctly.

8. **Read-only reports review (LLM-assessed, only if `reports` module touched).** Confirm
   every call from `reports` into another module's Service is query-shaped.

9. **AC verification.** For each AC in the sprint contract, locate the test(s) that cover it
   (per `generator-summary.md`'s self-check table) and independently confirm the test
   actually asserts what the AC requires — not just that a test with a plausible name
   exists and passes. This is the step most likely to catch a Generator that quietly
   narrowed scope to make a test pass.

## Turning findings into per-check detail

Every finding in `evaluator-feedback.md`'s "Per-check detail" and "Required fixes" sections
names: the file, the line (or line range), the rule violated (by name, referencing
architecture-principles/SKILL.md or the specific AC), and — for a fix — what change would
resolve it. "Improve error handling" is not acceptable feedback; "line 64: replace
`_notificationService.Send(...)` with `_eventBus.Publish(new SlaBreachEvent(...))` — Rule 2"
is.

## When to stop and mark ambiguous rather than guess

If, after completing steps 1–9, a check's answer genuinely depends on information not
available in the diff or skill files (e.g. an AC's intent is underspecified in a way the
Planner should have caught), do not silently pick an interpretation. Use the Fallback note
in `evaluator-feedback.md` (see `evaluator.agent.md`), default to the stricter verdict for
that check, and flag it — this is exactly the kind of gap the Monitor's quality-trend notes
should surface for skill-file tuning.
