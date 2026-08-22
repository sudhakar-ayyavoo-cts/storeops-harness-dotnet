# Demonstration Run — Feature Prompt

This is the exact prompt used to invoke the harness for the demonstration run required by
Section 5.5 / 6.3 of the capstone brief.

```
@planner Add SLA breach alerting: when a HIGH or CRITICAL task passes its due date without
reaching DONE, automatically fire a SLA_BREACH notification to the assigned Department
Lead, and escalate to STORE_MANAGER if unresolved after a configurable grace period.
```

Feature source: capstone brief, Section 3.4, second bullet ("Add SLA breach alerting").

Chosen because it is the strongest single-feature showcase of the **event-bus-only** rule
(Section 3.5) — it requires a cross-module trigger (`activities` → `alerts`) with no
natural reason to reach for a direct service call instead, so a Generator that gets this
wrong makes the mistake visibly, and the Evaluator's Rule 2 hard gate has a real,
non-contrived violation to catch if it occurs.
