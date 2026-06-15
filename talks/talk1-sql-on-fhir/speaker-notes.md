# Talk 1 — Speaker Notes (Rehearsal)

**Target time:** ~22–23 minutes (within 20–25 min slot) | **Demo time budget:** 6 minutes

---

**S1 — Title (30s)**
Introduce yourself briefly. "Technical retrospective — I built something real, from scratch, to spec, and I want to walk you through what that actually looked like."

**S2 — The .NET gap (1m)**
No preamble on what SQL on FHIR is. Go straight to the ecosystem map. JS ✓, Python ✓, Java ✓, .NET — dead air. "That's the gap."

**S3 — Three bad options (1.5m)**
One sentence per option. Go fast. The audience recognises these — let them nod. Don't linger.

**S4 — The gap (15s)**
Say the words. Pause 3 seconds. Move on.

**S5 — Introducing the package (45s)**
Two install commands on screen. Emphasise: "no JVM, no sidecar." Then: "let me tell you why you should trust it."

**S6 — Conformance (1.5m)**
Lead with the number: 100%. Then tell the story — two cases revealed genuine spec ambiguity, we filed feedback, the spec was clarified, v0.5.0 closes them clean. "This is what good spec implementation looks like."

**S7 — Performance (1m)**
Frame it as a pipeline requirement first. Then the table. One sentence on the implication: "at pipeline scale, this is the difference between 25 seconds and over 4 hours."

**S8 — Package anatomy (1m)**
Walk the stack bottom to top. "Each layer is a standalone NuGet. Take what you need."

**S9 — FHIRPath engine (1m)**
Key phrase: "we navigate JSON, we don't copy it." No POCO allocation. Parse once, evaluate many.

**S10 — Three stages (1.5m)**
Left to right: Parse (fail fast), Evaluate (pure, parallelisable), Write (pluggable sinks). Emphasise the separation.

**S11 — Multi-version (45s)**
"Pick version once, everything else is version-agnostic." Show the one-liner.

**S12 — ViewDefinition example (1m)**
"Nothing exotic. Same syntax as the HL7 JS impl — portability is a spec goal."

**S13 — forEach (1m)**
"This is the operation that makes FHIR analytics work." Show the result table. Mention `%resource`.

**S14 — 10 lines of C# (45s)**
"This is the whole API." Read the code. "Flat rows. Write anywhere."

**S15 — Demo 1 (3m)**
Switch to terminal. Preview → validate → run → open Parquet. Batch mode if time allows. Bridge line: "These patients were generated with Ignixa.FhirFakes — stay tuned for Talk 2."

**S16 — CLI reference (bridge, 30s)**
Leave up while transitioning. Point out key flags briefly.

**S17 — Demo 2 (3m)**
Switch to IDE. Open Program.cs. Evaluate → print rows. 20 patients, flat output.

**S18 — What we learned (1m)**
"Two cases revealed spec ambiguity. We filed feedback. The spec was clarified. v0.5.0 passes 100%." Beat: "That's what this kind of work enables."

**S19 — What this unlocks (1m)**
"$export → ViewDefinitions → Parquet → Data Lake." Five bullets, one sentence each. End with: "without leaving .NET."

**S20 — Try it (30s)**
Three commands. GitHub. Docs. Thank you.
