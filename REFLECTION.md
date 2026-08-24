# Reflection

## What the harness did well

On the SLA-breach-alerting sprint, a DI-wiring change
(`StoreOps.Application.Activities.ServiceCollectionExtensions.cs`, registering
`ISlaSweepService`) showed 0% coverage in the Coverlet report, while the actual business
logic it wired up, `SlaSweepService.cs`, was 100% covered. Rather than either failing the
sprint on a one-line file with nothing to meaningfully test, or silently waving it through,
the Evaluator reasoned by analogy to the existing Controller/Repository coverage exemption,
applied it, and explicitly wrote down that it had made that call and why — naming the exact
file and line. That's the non-determinism handling working as intended: an edge case the
skill files didn't explicitly cover got a reasoned, auditable answer instead of an
unexplained pass or a false-alarm failure. 

## One concrete improvement

`how-to-test/SKILL.md`'s coverage exemption named only Controllers and Repositories as
"thin by convention." The Evaluator had to independently re-derive that
`ServiceCollectionExtensions.cs` files belong in the same category during the SLA-breach
sprint review. Fix (already applied): name DI-wiring files in the exemption list
explicitly, so a future Evaluator run doesn't have to re-derive the same judgment call from
first principles.
