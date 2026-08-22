# Skill: evaluation-criteria

**Purpose.** The full graded evaluation framework: dimensions, weights, hard gates, and
explicit pass/fail criteria per checklist item. Read by the Evaluator alongside
`how-to-review/SKILL.md` (procedure) — this file is the rubric itself.

## Dimensions (sum to 100%)

| Dimension | Weight | Why this weight, for StoreOps |
|---|---|---|
| Architecture Compliance | 40% | The client's stated reason for requiring a harness at all was four specific architecture violations (Section 2). This dimension is weighted highest because a feature that is functionally correct but violates the module boundary or event-bus rule is *worse* than one that fails an AC — it reintroduces exactly the risk the harness exists to remove. |
| Correctness vs. Acceptance Criteria | 35% | The feature still has to do what was asked. Weighted just under Architecture Compliance, not above it, because a correctness gap is usually a fast, local fix; an architecture violation that ships is a standing risk until someone notices. |
| Code Quality & Tests | 25% | Coverage, test quality (see how-to-test/SKILL.md's "asserts more than a status code" rule), and naming/DI conventions. Lower weight because these are largely mechanical to fix in a retry and rarely represent irrecoverable risk the way an architecture violation does. |

## Hard gates (deterministic — any one violated forces `VERDICT: FAIL`, regardless of dimension scores)

Hard gates exist because these five failure classes are not matters of degree — a single
instance is a defect, not a quality signal to average into a score. Each is deterministic:
given the same check result (build output, test result, or a located violating line), the
verdict is always the same, independent of who or what is doing the review.

| Hard gate | Dimension | Automated / LLM | Failure it prevents |
|---|---|---|---|
| `dotnet build` succeeds, 0 warnings (nullable + `warnaserror`) | Architecture Compliance | Automated | Compile-time module-boundary violations (internal repository referenced across modules), nullability holes |
| `dotnet test` — all tests pass | Correctness vs. AC | Automated | Regressions, broken sprint output |
| Zero cross-module `Infrastructure` repository references outside their own module | Architecture Compliance | Automated (build) + LLM confirmation | Failure Mode 1 — direct imports bypassing service boundary |
| Zero raw non-`AppError` `throw new` in Service/Controller code | Architecture Compliance | Automated (text-scan) + LLM confirmation | Failure Mode 2 — raw Error throws bypassing typed hierarchy |
| Every cross-module side effect uses `IEventBus.Publish`, never a direct write-shaped call into another module's Service | Architecture Compliance | LLM-assessed | Failure Mode 4 — missing event bus integration / direct sibling writes |
| Coverage ≥ 80% on `StoreOps.Application` files changed this sprint | Code Quality & Tests | Automated (Coverlet/cobertura report) | Generator quietly shipping business logic with no test evidence at all — the minimum bar below which "Correctness vs. AC" can't be trusted regardless of what the self-check table claims |

Every dimension has at least one hard gate, and at least one of those gates is a plain
automated tool check (not an LLM judgement call) — this is deliberate: automated checks are
deterministic by construction, so anchoring each dimension to one keeps the Evaluator's
overall leniency bounded even where the remaining checks in that dimension require LLM
judgement.

A sixth candidate hard gate — "every test asserts more than an HTTP status code" — is
intentionally scored under Code Quality & Tests rather than gated, because a single
under-asserting test in an otherwise-correct sprint is a quality gap to flag and fix on
retry, not a reason to fail outright the way shipping with near-zero coverage is. (Contrast
with the hard gates above, which each represent a defect that must never reach `main` even
once.)

## Correctness vs. Acceptance Criteria — checklist

| Check | Pass criteria |
|---|---|
| Every AC has a located test | Generator's self-check table entry references a real test file+line; Evaluator confirms the test exists |
| Every "MET" self-check is independently verified | Evaluator reads the referenced test and confirms it asserts what the AC requires, not merely that it exists and passes |
| No AC silently narrowed | The implementation covers the AC's GIVEN state as specified (e.g. does not only handle `Critical` priority when the AC said `High or Critical`) |
| Idempotency/edge cases from the AC are covered | Sweep/bulk/retry ACs have a "run twice" or partial-failure test, per how-to-test/SKILL.md |

Score band: 4/4 met → 35/35. Each unmet item → −8 to −10 points, at Evaluator judgement
based on severity, with the specific item named in the feedback.

## Code Quality & Tests — checklist

| Check | Pass criteria |
|---|---|
| Coverage ≥ 80% on changed `StoreOps.Application` files | Hard gate — see table above; a FAIL here fails the sprint outright rather than only deducting points |
| No status-code-only tests | Every test added this sprint asserts at least one business-rule field, persisted state, or event-bus call |
| Naming/DI conventions followed | Matches coding-conventions/SKILL.md (file names, namespace, `ServiceCollectionExtensions` pattern) |
| `CancellationToken` threaded through async calls | No new async method drops the token |

The coverage row is listed here too because it still contributes to this dimension's
displayed score when it passes (it isn't only a gate — meeting it cleanly is part of what
"25/25" means); a FAIL on it, however, is decided by the hard-gate rule above; it is not
a partial deduction on this row like the other three.

Score band on the remaining 3 scored checklist items: 3/3 met → 25/25 (coverage gate
passing is a precondition, not part of this sub-score); each unmet item among the 3 →
−7 to −9 points.

## Verdict rules (deterministic mapping from checks to VERDICT)

```
IF any hard gate check result = FAIL:
    VERDICT = FAIL
ELSE IF total weighted score >= 90 AND no checklist item scored below 75% of its band:
    VERDICT = PASS
ELSE IF total weighted score >= 75:
    VERDICT = CONDITIONAL PASS   (minor findings recorded, sprint advances, debt logged)
ELSE:
    VERDICT = FAIL               (no hard gate violated, but correctness/quality too low
                                   to advance without a retry)
```

This mapping is applied mechanically once the check results and dimension scores are
filled in — the same inputs always produce the same VERDICT line, which is what makes the
Evaluator's output usable as a routing signal for `CLAUDE.md`'s orchestration logic rather
than something a human has to re-interpret each time.

### Worked example: variable Generator output → deterministic verdict

Generator claims AC-1, AC-2, AC-3 all MET. Evaluator's independent check (how-to-review,
step 9) finds AC-3's test only asserts a 200 status code and doesn't check the persisted
`SlaBreachedAt` timestamp the AC required. Build and tests pass; no hard gate is violated.

- Coverage gate: 84% on changed Application files → hard gate PASSES (this is what makes
  the rest of the scoring relevant at all — see the gate table above).
- Correctness vs. AC: 3/4 checklist items met ("every MET independently verified" fails for
  AC-3) → 35 × (3/4, adjusted) ≈ 26/35.
- Code Quality & Tests: 2/3 remaining scored items met — "no status-code-only tests" fails
  for AC-3's test — → 25 × (2/3, adjusted) ≈ 17/25.
- Architecture Compliance: no violations → 40/40.
- Total: 83/100. No hard gate FAIL, score ≥ 75 but < 90 → **VERDICT: CONDITIONAL PASS.**

Feedback names the exact test file/line and the exact assertion missing, so a retry (if the
developer chooses to force one) or the next sprint's Generator has an unambiguous fix
target — and the sprint still advances, since nothing here represents the kind of standing
architectural risk a hard gate protects against.

## Escalation path

Handled by the orchestrator (`CLAUDE.md`, Section 4), not by this file directly: three
consecutive `FAIL` verdicts on the same sprint (no hard-gate-free path to at least
CONDITIONAL PASS found in 3 Generator attempts) triggers `escalation.md` and hands control
to the developer. The Evaluator's role in that path is only to keep producing honest,
specific `evaluator-feedback.md` on each attempt — the escalation decision itself is the
orchestrator's, based on the iteration count.
