# Talk 2 — Speaker Notes (Rehearsal)

**Target time:** 23–24 minutes (within 20–25 min slot) | **Demo time budget:** 6 minutes

---

**S1 — Title (30s)**
"Every FHIR developer I've talked to has hit the same wall." Set the stage — this is about developer pain, not spec compliance.

**S2 — The wall (2m)**
Make this vivid. Walk through the mental process of hand-crafting a diabetic patient test fixture. Let the audience feel the pain before you offer the solution.

**S3 — Synthea credit (1.5m)**
Genuine credit first — Synthea is great, it's the inspiration. Then the .NET-specific friction list. One sentence each. End with: "Synthea is not the competition. It's the inspiration."

**S4 — Requirements matrix (1m)**
"This is the requirements matrix nobody wrote down but everyone knows." Walk the columns, not the rows. Hand-crafted wins speed but loses realism. Synthea wins realism but loses .NET. FhirFakes fills the matrix.

**S5 — Introducing FhirFakes (45s)**
Install commands on screen. "No JVM. Milliseconds. FHIR-valid." Then: "let me show you how it's structured."

**S6 — Four layers (1m)**
Walk the stack bottom to top. "Use as much or as little as you need." Leave the stack diagram up — you'll refer back to it.

**S7 — CLI preview (30s)**
Single command on screen. "You saw these patients in the last session — this is how they were generated."

**S8 — Layer 1 (1m)**
"The foundation knows the schema." Emphasise: not random strings — valid coded values from correct value sets.

**S9 — Layer 2 (1.5m)**
Read the ScenarioBuilder code out loud. "This reads like a clinical protocol." Mention VitalSignCorrelationEngine — physiological coherence.

**S10 — Layer 3 (1m)**
Walk the lifecycle archetypes. "43 encounters, 75 immunizations across a lifetime, 3 medications. This is what end-to-end testing of a chronic disease feature needs."

**S11 — Layer 4 (1m)**
"Seattle looks different from Boston." Demographically-aware risk calculator. One call, realistic cohort.

**S12 — Layer 1 code (45s)**
"Three lines." Show the before (200-line JSON fixture) vs after. "Drops into any test suite."

**S13 — Layer 2 code (1.5m)**
Read the builder chain aloud. Pause on `AddProbabilisticBranch(0.15, ...)`. "15% — that's real-world hyperlipidemia prevalence."

**S14 — Layer 3 + 4 code (1m)**
"43 encounters, 75 immunizations, 120+ total resources from one call." Then the population: "100 patients, demographically accurate, full medical history, one line."

**S15 — Demo 1 (3m)**
Switch to terminal. resource → scenario → population → ndjson. Open a generated file. Show `help scenarios`.

**S16 — CLI reference (bridge, 30s)**
Leave up during transition. Point at `help scenarios` and `help cities`.

**S17 — Demo 2 (3m)**
Switch to IDE. Layer 1: three lines. Layer 2: ScenarioBuilder. Layer 4: population. Show seed for determinism.

**S18 — Clinical realism (1m)**
"Random ≠ realistic." VitalSignCorrelationEngine. "The FHIR spec tells you the schema. Clinical medicine tells you the constraints."

**S19 — Determinism vs realism (1m)**
"`Randomizer.Seed` for unit tests. No seed for load tests. Make the choice explicit — don't let it happen accidentally."

**S20 — Try it (30s)**
One command. GitHub. Docs. "Inspired by Synthea — complements it, doesn't replace it." Thank you.
