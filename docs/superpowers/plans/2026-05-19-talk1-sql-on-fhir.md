# Talk 1: SQL on FHIR — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build out all content for "Flattening the Curve: Implementing the SQL on FHIR ViewDefinition in .NET" — 20 slides with speaker notes, demo data files, CLI demo script, and a working C# library demo project.

**Architecture:** Presentation content lives under `talks/talk1-sql-on-fhir/`. Slides and speaker notes are Markdown. Demo data is real valid FHIR R4 JSON/NDJSON. The library demo is a runnable .NET 9 console app that references `Ignixa.SqlOnFhir` from NuGet.

**Tech Stack:** Markdown (slides/notes), FHIR R4 JSON, .NET 9 / C#, `Ignixa.SqlOnFhir` NuGet package, `Ignixa.SqlOnFhir.Cli` dotnet global tool

**Design doc:** `docs/superpowers/specs/2026-05-19-devdays-presentations-design.md`

---

## File Map

| File | Purpose |
|---|---|
| `talks/talk1-sql-on-fhir/slides.md` | Full slide deck — all 20 slides, body text, speaker notes |
| `talks/talk1-sql-on-fhir/speaker-notes.md` | Condensed rehearsal notes (one paragraph per slide) |
| `talks/talk1-sql-on-fhir/demo/data/patient-view.json` | Patient ViewDefinition (single-file demos) |
| `talks/talk1-sql-on-fhir/demo/data/patients.ndjson` | 10 Patient resources (mix of active/inactive) |
| `talks/talk1-sql-on-fhir/demo/data/views/patient-view.json` | Patient ViewDefinition (batch demo) |
| `talks/talk1-sql-on-fhir/demo/data/views/observation-view.json` | Observation ViewDefinition (batch demo) |
| `talks/talk1-sql-on-fhir/demo/data/views/condition-view.json` | Condition ViewDefinition (batch demo) |
| `talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Patient.ndjson` | Patient NDJSON (batch demo) |
| `talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Observation.ndjson` | Observation NDJSON (batch demo) |
| `talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Condition.ndjson` | Condition NDJSON (batch demo) |
| `talks/talk1-sql-on-fhir/demo/demo1-cli-script.md` | CLI demo — exact commands, expected output, fallback plan |
| `talks/talk1-sql-on-fhir/demo/demo2-library/Demo2.csproj` | .NET 9 console app project |
| `talks/talk1-sql-on-fhir/demo/demo2-library/Program.cs` | Complete working library demo |

---

## Task 1: Slides — Section 1 (The Problem) and Section 2 (How This Solves It)

**Files:**
- Create: `talks/talk1-sql-on-fhir/slides.md` (slides 1–7)

- [ ] **Write slides 1–7 into slides.md**

```markdown
# Flattening the Curve
## Implementing the SQL on FHIR ViewDefinition in .NET

> Talk: DevDays 2026 | Community Talk | 20–25 min
> Source: https://github.com/brendankowitz/ignixa-fhir
> Audience: FHIR developers who know SQL on FHIR; lacking a native .NET solution

---

## Slide 1 — Title

**Headline:** Flattening the Curve

**Subhead:** Implementing the SQL on FHIR ViewDefinition in .NET

**Body:**
- Brendan Kowitz
- Engineering Manager, Microsoft — Azure FHIR Server
- Ignixa: open-source FHIR ecosystem for .NET
- DevDays 2026

**Speaker notes:**
Welcome everyone. This is a technical retrospective — I built something real, from scratch, to spec, and I want to walk you through what that actually looked like. Not the happy path. The whole path.

---

## Slide 2 — SQL on FHIR is solved. Just not in .NET.

**Headline:** SQL on FHIR is solved. Just not in .NET.

**Body:**
- HL7 reference implementation → JavaScript ✓
- Python libraries → exist ✓
- Java → yes ✓
- .NET → …

**Speaker notes:**
I'm not going to explain what SQL on FHIR is — you're here, you know the spec. What I want to talk about is where .NET sits in this ecosystem. If you search for SQL on FHIR + .NET today, you find a lot of workarounds and a lot of people asking the same question. That's the gap this session is about.

---

## Slide 3 — Three options. None of them great.

**Headline:** Three options. None of them great.

**Body:**

**Option A — Call a sidecar**
Run the HL7 JS impl or a Python library as a subprocess. Works, but now you have a JVM or Python runtime in your .NET deployment, cross-process overhead, and a sidecar to maintain.

**Option B — Write it yourself**
Hand-roll FHIRPath extraction per project. Fast to start, painful to maintain, zero conformance guarantee.

**Option C — Skip SQL on FHIR entirely**
Fall back to proprietary query patterns or ORM-based extraction. Leaves the ecosystem interoperability gains on the table.

**Speaker notes:**
Quick show of hands — how many of you have done Option B? Yeah. You write a little FHIRPath, it works, then six months later someone adds a new resource type and the extraction breaks. These options all have real costs.

---

## Slide 4 — The gap

**Headline:** There's no native, conformant .NET implementation.

**Subhead:** Until now.

**Speaker notes:**
[Pause. Let it land. Then move forward.]

---

## Slide 5 — Introducing Ignixa.SqlOnFhir

**Headline:** Ignixa.SqlOnFhir

**Body:**
- Native .NET 9 — no JVM, no Python, no sidecar
- `dotnet add package Ignixa.SqlOnFhir`
- `dotnet tool install -g Ignixa.SqlOnFhir.Cli`
- MIT license. Open source.
- Multi-version: STU3, R4, R4B, R5, R6

**Speaker notes:**
Two minutes to integrate. The NuGet package gives you the evaluator as a library. The CLI gives you a tool you can run in pipelines, CI, and on the command line right now. Let me tell you why you should trust it.

---

## Slide 6 — It passes the official conformance suite

**Headline:** Passes the official SQL on FHIR v2 conformance suite.

**Body:**
- The SQL on FHIR test suite ships as official JSON test files from the spec repo
- xUnit theory runner loads every test case dynamically — no cherry-picking
- Passes all tests with two documented edge-case skips:
  1. **fn_boundary decimals** — JSON number precision vs FHIR's high-precision decimal requirement; a known JSON parsing limitation, not a spec disagreement
  2. **row_index / unionAll ordering** — per-resource evaluation vs SQL UNION ALL ordering; `%rowIndex` semantics are correct, row order differs from a batch evaluator

**Speaker notes:**
I'm being deliberate about the two skips. I could have fudged them or not mentioned them. But implementing a spec at this level of detail means you will find its edges. These two test cases revealed genuine ambiguity in the spec — in both cases we filed feedback. Being honest about this is more credible than claiming 100%.

---

## Slide 7 — Fast enough for $export pipelines

**Headline:** Fast enough for $export pipelines.

**Body:**
- SQL on FHIR at scale = evaluating ViewDefinitions across millions of resources
- FHIRPath performance is load-bearing for batch workloads
- Compiled + cached expressions — parse once, evaluate per resource

| Operation | Ignixa |
|---|---|
| `Patient.name.family` | 345 ns / 1.1 KB |
| `Patient.name[0].given` | 484 ns / 1.5 KB |
| `where + first` | 675 ns / 2.3 KB |
| Search param extraction | 1,508 ns / 4.6 KB |

*(BenchmarkDotNet, .NET 9, i7-14700K)*

**Speaker notes:**
Sub-microsecond FHIRPath evaluation. The reason this matters: if you're running a nightly $export of 10 million resources and evaluating 5 ViewDefinitions per resource, you're making 50 million FHIRPath evaluations. At 500ns each, that's 25 seconds. At 300 microseconds each — which is what you'd pay without a compiled engine — that's over 2 hours. The compiled+cached approach is what makes this usable at pipeline scale.
```

- [ ] **Verify slides 1–7 against the design doc** — check every bullet point is present, speaker notes match the intent in `docs/superpowers/specs/2026-05-19-devdays-presentations-design.md` Sections 1–2

---

## Task 2: Slides — Section 3 (Architecture) and Section 4 (Examples)

**Files:**
- Modify: `talks/talk1-sql-on-fhir/slides.md` (append slides 8–14)

- [ ] **Append slides 8–14 to slides.md**

```markdown
---

## Slide 8 — Package anatomy

**Headline:** Modular. Pick what you need.

**Body (dependency stack, bottom to top):**
```
Ignixa.Abstractions          IElement, ISourceNavigator — no dependencies
       ↓
Ignixa.Serialization         JSON → ISourceNavigator (JsonSourceNodeFactory)
Ignixa.Specification         Version-aware schema: R4, R4B, R5, R6, STU3
Ignixa.FhirPath              Compiled FHIRPath expression engine
       ↓
Ignixa.SqlOnFhir             ViewDefinition parser + row evaluator
Ignixa.SqlOnFhir.Writers     Parquet / CSV / NDJSON output
       ↓
Ignixa.SqlOnFhir.Cli         dotnet global tool (ignixa-sqlonfhir)
```

**Speaker notes:**
Each layer is a standalone NuGet package. If you just need the FHIRPath engine, take `Ignixa.FhirPath`. If you want ViewDefinition evaluation in your app, take `Ignixa.SqlOnFhir`. The CLI is the full stack packaged as a tool. This modularity also means you can unit-test each layer independently — which is how we built the conformance suite.

---

## Slide 9 — Navigate JSON. Don't copy it.

**Headline:** Navigate JSON. Don't copy it.

**Body:**
- `JsonSourceNodeFactory` parses JSON into lightweight `ISourceNavigator` — no POCO allocation
- `IElement` (typed) only when the spec requires it — not by default
- Expressions compiled once at parse time, cached for reuse across resources
- Large Bundle: **70 µs parse, 47 KB allocated**

**Speaker notes:**
This is the key design decision behind the performance numbers. Most FHIRPath libraries deserialize FHIR JSON into a rich C# object model and then navigate that. We skip the object model entirely. We parse the JSON once into a lightweight tree of navigable nodes, and we walk that tree directly. The compiled FHIRPath expressions are closures over that tree — they run fast because there's no intermediate representation to construct.

---

## Slide 10 — Parse → Evaluate → Write

**Headline:** Three stages. Clean separation.

**Body:**

**1. Parse**
`ViewDefinitionExpressionParser.Parse()` reads ViewDefinition JSON and compiles all FHIRPath expressions upfront. Fail fast — invalid FHIRPath is caught here, not at evaluation time.

**2. Evaluate**
`SqlOnFhirEvaluator.Evaluate(viewDef, resource)` per resource:
- WHERE clauses filter resources
- SELECT columns extract values via compiled FHIRPath
- `forEach` groups unnest arrays into multiple rows
- `%resource` variable gives access to the root from inside a forEach

**3. Write**
`ParquetFileWriter` / `CsvFileWriter` / `NdjsonFileWriter` consume the row stream. Pluggable — implement `IRowWriter` to add your own sink.

**Speaker notes:**
The separation matters for two reasons. First, you can validate ViewDefinitions without running them — the `ignixa-sqlonfhir validate` command uses the Parse stage only. Second, the Evaluate stage is pure and stateless — you can parallelize across resources trivially. The Writers are sinks, not part of the evaluation logic.

---

## Slide 11 — One codebase. Five FHIR versions.

**Headline:** One codebase. Five FHIR versions.

**Body:**
- STU3, R4, R4B, R5, R6
- Version is the first CLI argument: `ignixa-sqlonfhir r4 run ...`
- In code: `FhirVersion.R4.GetSchemaProvider()`
- Same ViewDefinition JSON runs against any version
- Version-specific schema resolves element types, bindings, cardinality

**Speaker notes:**
You pick the FHIR version once — at the CLI level or at DI registration time. Everything below that is version-agnostic. The same patient-view.json you write for R4 will run against R5 resources without changes, as long as the paths are valid in both versions. This is important for organisations that are mid-migration between FHIR versions.

---

## Slide 12 — A ViewDefinition you'd actually write

**Headline:** A ViewDefinition you'd actually write.

**Body:**
```json
{
  "resourceType": "ViewDefinition",
  "resource": "Patient",
  "where": [
    { "path": "active = true" }
  ],
  "select": [
    {
      "column": [
        { "name": "id",          "path": "id",                                           "type": "string" },
        { "name": "family_name", "path": "name.where(use='official').first().family",    "type": "string" },
        { "name": "given_name",  "path": "name.where(use='official').first().given.first()", "type": "string" },
        { "name": "birth_date",  "path": "birthDate",                                   "type": "date"   },
        { "name": "gender",      "path": "gender",                                       "type": "string" }
      ]
    }
  ]
}
```

**Speaker notes:**
Nothing exotic. WHERE filters to active patients only. SELECT pulls scalar columns using standard FHIRPath. If you've used the HL7 JS reference implementation, this is the same syntax — that's deliberate. Portability of ViewDefinitions across implementations is a spec goal, and this shows it working.

---

## Slide 13 — One resource. Many rows.

**Headline:** One resource. Many rows.

**Body:**
```json
{
  "resourceType": "ViewDefinition",
  "resource": "Patient",
  "select": [
    {
      "column": [
        { "name": "id", "path": "id", "type": "string" }
      ]
    },
    {
      "forEach": "name",
      "column": [
        { "name": "name_use",    "path": "use",    "type": "string" },
        { "name": "family_name", "path": "family", "type": "string" },
        { "name": "given_name",  "path": "given.first()", "type": "string" }
      ]
    }
  ]
}
```

**Result:**
| id | name_use | family_name | given_name |
|---|---|---|---|
| pt-001 | official | Smith | Alice |
| pt-001 | maiden | Jones | Alice |

**Speaker notes:**
This is the operation that makes FHIR analytics actually work. A Patient has an array of names. `forEach` gives you one row per name — standard SQL unnesting, expressed as a ViewDefinition. The first select group produces the `id` column once. The `forEach` group repeats for each element in the `name` array. Use `%resource` inside a forEach to reference back to the root resource.

---

## Slide 14 — No subprocess. No sidecar. 10 lines.

**Headline:** No subprocess. No sidecar. 10 lines.

**Body:**
```csharp
var schemaProvider = FhirVersion.R4.GetSchemaProvider();

var viewDef = JsonSourceNodeFactory
    .Parse(File.ReadAllText("patient-view.json"))
    .ToSourceNavigator();

var patient = JsonSourceNodeFactory
    .Parse(File.ReadAllText("patient.json"))
    .ToElement(schemaProvider);

var evaluator = new SqlOnFhirEvaluator();
foreach (var row in evaluator.Evaluate(viewDef, patient))
    Console.WriteLine($"{row["id"]} — {row["family_name"]}, {row["birth_date"]}");
```

**Speaker notes:**
This is the entire API surface you need to evaluate a ViewDefinition from C#. `GetSchemaProvider()` is the version-aware schema — the only place FHIR version enters the picture. `ToSourceNavigator()` gives you the lightweight navigation structure. `ToElement()` converts it to a typed structure using the schema. Then evaluate. The result is an `IEnumerable<Dictionary<string, object?>>` — flat rows you can write anywhere.
```

- [ ] **Verify slides 8–14** — confirm code snippets match the actual API (`SqlOnFhirEvaluator`, `JsonSourceNodeFactory`, `FhirVersion.R4.GetSchemaProvider()`) by checking `C:\Src\fhir-server-contrib\src\Core\Ignixa.SqlOnFhir\` and `C:\Src\fhir-server-contrib\src\Core\Ignixa.Serialization\`

---

## Task 3: Slides — Section 5 (Demos) and Section 6 (What We Learned)

**Files:**
- Modify: `talks/talk1-sql-on-fhir/slides.md` (append slides 15–20)
- Create: `talks/talk1-sql-on-fhir/speaker-notes.md`

- [ ] **Append slides 15–20 to slides.md**

```markdown
---

## Slide 15 — Demo 1: CLI

**Headline:** Let's run it.

**Subhead:** preview → validate → run

**Body:**
- demo script: `talks/talk1-sql-on-fhir/demo/demo1-cli-script.md`

**Speaker notes:**
[Switch to terminal. Data files are pre-staged in `demo/data/`.] I've got a patient ViewDefinition and some NDJSON patients ready. Let's start with preview so you can see the schema before touching any data.

---

## Slide 16 — CLI reference

**Headline:** CLI quick reference.

**Body:**

| Command | What it does |
|---|---|
| `ignixa-sqlonfhir r4 preview --views <file>` | Show schema without data |
| `ignixa-sqlonfhir r4 preview --views <file> --input <file> --rows 10` | Show sample rows |
| `ignixa-sqlonfhir r4 validate --views <dir>` | Validate all ViewDefinitions |
| `ignixa-sqlonfhir r4 run --out file.parquet` | Single Parquet output |
| `ignixa-sqlonfhir r4 run --out dir/ --format csv` | Batch CSV output |
| `--var name=value` | Override ViewDefinition constants at runtime |
| `--stats-out stats.json` | Write batch statistics |

All commands: `<version> <command> [options]`
Versions: `stu3` `r4` `r4b` `r5` `r6`

**Speaker notes:**
[Leave this up during/after Demo 1 — audience can read while you transition.] The version is always first. Then the command. The rest is flags. Preview is schema-only without `--input`, adds sample rows with it. Validate catches errors before you run. Run produces the output.

---

## Slide 17 — Demo 2: Library

**Headline:** Now from C#.

**Subhead:** load → evaluate → print

**Body:**
- demo project: `talks/talk1-sql-on-fhir/demo/demo2-library/`

**Speaker notes:**
[Switch to IDE — open `demo2-library/Program.cs`.] Same data, different entry point. This is what you'd write inside a data pipeline, a test, or a background job. Watch how thin the API surface is.

---

## Slide 18 — Implementing a spec is a conversation with the spec

**Headline:** Implementing a spec is a conversation with the spec.

**Body:**
Two skipped tests — both revealed genuine spec ambiguity:

**fn_boundary decimals**
JSON number parsing loses precision that FHIR's decimal type requires. The spec doesn't prescribe how to handle this at the JSON layer. Known tension between JSON and FHIR semantics.

**row_index / unionAll**
Per-resource evaluation produces `%rowIndex = 0` per resource (correct). SQL UNION ALL semantics would number rows across all resources. The spec doesn't prescribe which is correct for a per-resource evaluator.

→ Both cases: we filed feedback.

**Speaker notes:**
This is the most honest slide in the deck. Implementing a spec at this level of detail is one of the most effective ways to find its edges — and to contribute back. We didn't sweep these under the rug. We documented them, we filed feedback, and we were transparent about them here. That's what good spec implementation looks like.

---

## Slide 19 — What this unlocks for .NET

**Headline:** $export → ViewDefinitions → Parquet → Data Lake.

**Body:**
- **BI dashboards** — Power BI / Tableau consuming Parquet directly, no ETL middleware
- **Research cohorts** — reproducible ViewDefinitions as the extraction contract
- **Data lake pipelines** — `$export` NDJSON → ViewDefinitions → Parquet in one CLI command
- **CI validation** — validate ViewDefinitions against schema before deployment
- **Multi-version migration** — same ViewDefinition, different FHIR version flag

**Speaker notes:**
The pipeline this enables: your FHIR server's $export operation gives you NDJSON. Your ViewDefinitions give you a defined, version-controlled schema. The CLI turns that NDJSON into Parquet with one command. Every analytics tool in your organisation can read Parquet. You've gone from FHIR server to BI-ready data without leaving .NET and without writing a custom ETL.

---

## Slide 20 — Try it.

**Headline:** Try it.

**Body:**
```bash
# Library
dotnet add package Ignixa.SqlOnFhir

# CLI
dotnet tool install -g Ignixa.SqlOnFhir.Cli

# Run your first ViewDefinition
ignixa-sqlonfhir r4 preview --views patient-view.json
```

- GitHub: https://github.com/brendankowitz/ignixa-fhir
- Docs: https://brendankowitz.github.io/ignixa-fhir/
- MIT license — contributions welcome

**Speaker notes:**
That's it. Three commands to get started. The GitHub repo has the ViewDefinition samples, the docs walk through the CLI options, and the NuGet package is ready to drop into any .NET 9 project. Thank you.
```

- [ ] **Create `talks/talk1-sql-on-fhir/speaker-notes.md`** with condensed one-paragraph-per-slide rehearsal notes:

```markdown
# Talk 1 — Speaker Notes (Rehearsal)

**Target time:** 22 minutes | **Demo time budget:** 6 minutes

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
Lead with the signal, then be explicit about the two skips. "Being honest about this is more credible than claiming 100%." Name what they are briefly.

**S7 — Performance (1m)**
Frame it as a pipeline requirement first. Then the table. One sentence on the implication: "at pipeline scale, this is the difference between 25 seconds and 2 hours."

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
Switch to terminal. Preview → validate → run → open Parquet. Batch mode if time allows.

**S16 — CLI reference (bridge, 30s)**
Leave up while transitioning. Point out key flags briefly.

**S17 — Demo 2 (3m)**
Switch to IDE. Open Program.cs. Evaluate → print rows. Runtime variable override.

**S18 — What we learned (1m)**
"The two skips revealed spec ambiguity." Name them. "We filed feedback. This is what good spec implementation looks like."

**S19 — What this unlocks (1m)**
"$export → ViewDefinitions → Parquet → Data Lake." Five bullets, one sentence each. End with: "without leaving .NET."

**S20 — Try it (30s)**
Three commands. GitHub. Docs. Thank you.
```

---

## Task 4: Generate Demo Data with Ignixa.FhirFakes CLI

The demo data for this talk is generated using `ignixa-fakes` — the same tool featured in Talk 2. This is intentional: it lets you say during the demo *"these patients were generated with Ignixa.FhirFakes — I'll show you how in the next session"*, connecting the two talks naturally.

**Files:**
- Create: `talks/talk1-sql-on-fhir/demo/data/patient-view.json`
- Create: `talks/talk1-sql-on-fhir/demo/data/views/patient-view.json`
- Create: `talks/talk1-sql-on-fhir/demo/data/views/observation-view.json`
- Create: `talks/talk1-sql-on-fhir/demo/data/views/condition-view.json`
- Generated by CLI: `talks/talk1-sql-on-fhir/demo/data/patients.ndjson`
- Generated by CLI: `talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Patient.ndjson`
- Generated by CLI: `talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Observation.ndjson`
- Generated by CLI: `talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Condition.ndjson`

- [ ] **Install `ignixa-fakes` CLI** (if not already installed):

```bash
dotnet tool install -g Ignixa.FhirFakes.Cli
ignixa-fakes help cities   # confirm Seattle is available
```

- [ ] **Generate the population NDJSON into a staging directory, then extract per-resource files**:

```bash
# Generate 20 patients from Seattle as NDJSON (more than we need — gives realistic variety)
ignixa-fakes r4 population --out "talks/talk1-sql-on-fhir/demo/data/generated" --from Seattle --count 20 --ndjson
```

The CLI will produce files named `r4-population-seattle-{ResourceType}-20-{guid}.ndjson`.

- [ ] **Move the generated NDJSON files to the expected locations**:

```bash
# Patient NDJSON — used by both single-file and batch demos
# Grab the Patient NDJSON file (there will be one, named with a GUID)
$patientFile = Get-ChildItem "talks/talk1-sql-on-fhir/demo/data/generated" -Filter "*Patient*.ndjson" | Select-Object -First 1
Copy-Item $patientFile.FullName "talks/talk1-sql-on-fhir/demo/data/patients.ndjson"
Copy-Item $patientFile.FullName "talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Patient.ndjson"

# Observation NDJSON — batch demo
$obsFile = Get-ChildItem "talks/talk1-sql-on-fhir/demo/data/generated" -Filter "*Observation*.ndjson" | Select-Object -First 1
Copy-Item $obsFile.FullName "talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Observation.ndjson"

# Condition NDJSON — batch demo
$condFile = Get-ChildItem "talks/talk1-sql-on-fhir/demo/data/generated" -Filter "*Condition*.ndjson" | Select-Object -First 1
Copy-Item $condFile.FullName "talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Condition.ndjson"
```

- [ ] **Verify the generated data looks reasonable**:

```bash
# Count lines in each file
(Get-Content "talks/talk1-sql-on-fhir/demo/data/patients.ndjson").Count   # expect ~20
(Get-Content "talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Observation.ndjson").Count  # expect many (vitals per patient)
(Get-Content "talks/talk1-sql-on-fhir/demo/data/fhir-ndjson/Condition.ndjson").Count    # expect some
```

- [ ] **Create `demo/data/patient-view.json`** (single-file demo ViewDefinition — used in Demo 1 preview/validate/run and Demo 2). Note: no `active` filter since FhirFakes generates all patients as active — drop the WHERE clause:

```json
{
  "resourceType": "ViewDefinition",
  "resource": "Patient",
  "select": [
    {
      "column": [
        { "name": "id",          "path": "id",                                                "type": "string" },
        { "name": "family_name", "path": "name.where(use='official').first().family",         "type": "string" },
        { "name": "given_name",  "path": "name.where(use='official').first().given.first()",  "type": "string" },
        { "name": "birth_date",  "path": "birthDate",                                         "type": "date"   },
        { "name": "gender",      "path": "gender",                                            "type": "string" }
      ]
    }
  ]
}
```

- [ ] **Copy `patient-view.json` to `demo/data/views/patient-view.json`** (batch demo reads from `views/` directory)

- [ ] **Create `demo/data/views/observation-view.json`**:

```json
{
  "resourceType": "ViewDefinition",
  "resource": "Observation",
  "select": [
    {
      "column": [
        { "name": "id",             "path": "id",                              "type": "string"  },
        { "name": "patient_id",     "path": "subject.reference",               "type": "string"  },
        { "name": "code",           "path": "code.coding.first().code",        "type": "string"  },
        { "name": "display",        "path": "code.coding.first().display",     "type": "string"  },
        { "name": "value",          "path": "valueQuantity.value",             "type": "decimal" },
        { "name": "unit",           "path": "valueQuantity.unit",              "type": "string"  },
        { "name": "effective_date", "path": "effectiveDateTime",               "type": "date"    }
      ]
    }
  ]
}
```

- [ ] **Create `demo/data/views/condition-view.json`**:

```json
{
  "resourceType": "ViewDefinition",
  "resource": "Condition",
  "select": [
    {
      "column": [
        { "name": "id",              "path": "id",                                     "type": "string" },
        { "name": "patient_id",      "path": "subject.reference",                      "type": "string" },
        { "name": "code",            "path": "code.coding.first().code",               "type": "string" },
        { "name": "display",         "path": "code.coding.first().display",            "type": "string" },
        { "name": "onset_date",      "path": "onsetDateTime",                          "type": "date"   },
        { "name": "clinical_status", "path": "clinicalStatus.coding.first().code",     "type": "string" }
      ]
    }
  ]
}
```

- [ ] **Add the bridge moment to slide 15 speaker notes** — update `slides.md` slide 15 speaker notes to include:

> "By the way — these patients weren't hand-crafted. They were generated in one command using Ignixa.FhirFakes, the tool I'll cover in the next session: `ignixa-fakes r4 population --from Seattle --count 20 --ndjson`. Realistic demographics, clinically plausible data, valid FHIR — in milliseconds. Stay tuned."

---

## Task 5: Demo 1 CLI Script

**Files:**
- Create: `talks/talk1-sql-on-fhir/demo/demo1-cli-script.md`

- [ ] **Create `demo1-cli-script.md`**:

```markdown
# Demo 1 — CLI Script

**Time budget:** 3 minutes  
**Data location:** `talks/talk1-sql-on-fhir/demo/data/`  
**Pre-demo:** `cd` to `demo/data/`. Confirm `ignixa-sqlonfhir` is on PATH (`ignixa-sqlonfhir --version`).  
**Terminal font:** 24pt minimum. Light theme or high-contrast dark.

---

## Beat 1: Preview schema (no data yet) (~30s)

**Say:** "Let's start with `preview`. This shows the schema the ViewDefinition will produce — without touching any data. Great for CI validation."

```bash
ignixa-sqlonfhir r4 preview --views patient-view.json
```

**Expected output:**
```
Schema for ViewDefinition: Patient
──────────────────────────────────────
  id            string
  family_name   string
  given_name    string
  birth_date    date
  gender        string

WHERE filter: active = true
```

---

## Beat 2: Preview with sample rows (~30s)

**Say:** "Add `--input` and `--rows` to see actual data coming through."

```bash
ignixa-sqlonfhir r4 preview --views patient-view.json --input patients.ndjson --rows 5
```

**Expected output:**
```
Schema for ViewDefinition: Patient
──────────────────────────────────────
  id            string
  family_name   string
  given_name    string
  birth_date    date
  gender        string

Sample rows (5 of 7 matching, 3 filtered by WHERE):
  pt-001 | Nakamura | Aiko      | 1984-03-15 | female
  pt-002 | Okafor   | Emeka     | 1971-11-28 | male
  pt-003 | Rodriguez| Maria     | 1990-07-04 | female
  pt-005 | Johnson  | Marcus    | 2001-09-11 | male
  pt-006 | Patel    | Priya     | 1978-05-30 | female
```

**Say while it runs:** "Twenty patients, generated with Ignixa.FhirFakes from Seattle demographics. Real names, real zip codes, clinically plausible data — I'll show you how in the next session."

---

## Beat 3: Validate (~20s)

**Say:** "Before running in production, validate. This catches FHIRPath errors without touching data."

```bash
ignixa-sqlonfhir r4 validate --views patient-view.json
```

**Expected output:**
```
✓ patient-view.json — valid
```

---

## Beat 4: Run — single file to Parquet (~30s)

**Say:** "Now run it. Output format is inferred from the extension."

```bash
ignixa-sqlonfhir r4 run \
  --views patient-view.json \
  --input patients.ndjson \
  --out patients.parquet
```

**Expected output:**
```
Processing Patient resources...
  ✓ 10 resources read, 7 matched WHERE filter
  ✓ Written: patients.parquet (7 rows)
```

**Say:** "Open it." [Open in DuckDB or Parquet viewer]

```bash
# If using DuckDB:
duckdb -c "SELECT * FROM 'patients.parquet';"
```

---

## Beat 5: Batch mode (~45s)

**Say:** "In a real pipeline you have multiple ViewDefinitions and multiple resource types. Batch mode handles that."

```bash
ignixa-sqlonfhir r4 run \
  --views views/ \
  --input fhir-ndjson/ \
  --out output/ \
  --format parquet \
  --stats-out output/stats.json
```

**Expected output:**
```
Batch mode: 3 ViewDefinitions × 3 resource files
  ✓ Patient  → output/patient-view.parquet   (7 rows)
  ✓ Observation → output/observation-view.parquet (8 rows)
  ✓ Condition   → output/condition-view.parquet   (6 rows)
  ✓ Stats written: output/stats.json
```

**Say:** "Three commands. Three Parquet files. Ready for any analytics tool."

---

## Fallback plan

If the CLI is not installed or produces unexpected output:
1. Show the preview output as a static screenshot (pre-prepared in `demo/data/screenshots/`)
2. Explain the commands verbally while pointing at slide 16 (CLI reference)
3. Proceed directly to Demo 2 — the library demo is independent

---

## Pre-demo checklist

- [ ] `ignixa-sqlonfhir --version` prints a version number
- [ ] `cd talks/talk1-sql-on-fhir/demo/data` works
- [ ] `patient-view.json` exists
- [ ] `patients.ndjson` exists (10 lines)
- [ ] `views/` directory has 3 JSON files
- [ ] `fhir-ndjson/` directory has 3 NDJSON files
- [ ] `output/` directory exists and is empty
- [ ] DuckDB or Parquet viewer installed and working (for Beat 4)
```

---

## Task 6: Demo 2 — Library Project

**Files:**
- Create: `talks/talk1-sql-on-fhir/demo/demo2-library/Demo2.csproj`
- Create: `talks/talk1-sql-on-fhir/demo/demo2-library/Program.cs`

- [ ] **Create `Demo2.csproj`**:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>SqlOnFhirDemo</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Ignixa.SqlOnFhir" Version="*" />
    <PackageReference Include="Ignixa.SqlOnFhir.Writers" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <!-- Copy demo data alongside the binary for easy running -->
    <None Update="..\data\patient-view.json" CopyToOutputDirectory="PreserveNewest" />
    <None Update="..\data\patients.ndjson" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

- [ ] **Create `Program.cs`** — complete, runnable, written to be live-coded in 3 minutes (short enough to type, long enough to be real):

```csharp
using Ignixa.Serialization;
using Ignixa.Specification;
using Ignixa.SqlOnFhir.Evaluation;

// ── Setup ──────────────────────────────────────────────────────────────────
// Version-aware schema provider — the only place FHIR version enters the picture
var schemaProvider = FhirVersion.R4.GetSchemaProvider();

// ── Load ViewDefinition ────────────────────────────────────────────────────
var viewDef = JsonSourceNodeFactory
    .Parse(File.ReadAllText("patient-view.json"))
    .ToSourceNavigator();

// ── Evaluate ───────────────────────────────────────────────────────────────
var evaluator = new SqlOnFhirEvaluator();

Console.WriteLine($"{"ID",-10} {"Family",-12} {"Given",-12} {"Born",-12} {"Gender"}");
Console.WriteLine(new string('─', 60));

// Read patients.ndjson line by line (one resource per line)
var matchCount = 0;
foreach (var line in File.ReadLines("patients.ndjson"))
{
    if (string.IsNullOrWhiteSpace(line)) continue;

    var resource = JsonSourceNodeFactory
        .Parse(line)
        .ToElement(schemaProvider);

    foreach (var row in evaluator.Evaluate(viewDef, resource))
    {
        matchCount++;
        Console.WriteLine(
            $"{row["id"],-10} {row["family_name"],-12} {row["given_name"],-12} " +
            $"{row["birth_date"],-12} {row["gender"]}");
    }
}

Console.WriteLine(new string('─', 60));
Console.WriteLine($"{matchCount} patients matched (WHERE active = true)");

// ── Runtime variable override ──────────────────────────────────────────────
// Show how constants in ViewDefinitions can be overridden at runtime
Console.WriteLine("\n── With runtime variable override ─────────────────────");

// Example: a ViewDefinition that uses a %cohortTag constant
// The caller can override it without modifying the ViewDefinition file
var vars = new Dictionary<string, string>
{
    ["cohortId"] = "devdays-2026-demo"
};

Console.WriteLine($"Runtime vars: cohortId={vars["cohortId"]}");
Console.WriteLine("(In a real pipeline: ignixa-sqlonfhir r4 run --var cohortId=devdays-2026-demo)");
```

- [ ] **Verify the project builds** — run from `demo/demo2-library/`:
  ```bash
  cd "C:/Src/DevDays2026/talks/talk1-sql-on-fhir/demo/demo2-library"
  dotnet run
  ```
  Expected: table of 7 patients printed (the 3 inactive ones filtered by WHERE)

---

## Task 7: Final Review Pass

**Files:** Read-only pass over all created files

- [ ] **Spec coverage check** — verify every slide in the design doc (`docs/superpowers/specs/2026-05-19-devdays-presentations-design.md`) has a corresponding slide in `slides.md`

- [ ] **API name verification** — confirm the following names exist in `C:\Src\fhir-server-contrib\src\`:
  - `SqlOnFhirEvaluator` → `src/Core/Ignixa.SqlOnFhir/Evaluation/`
  - `JsonSourceNodeFactory` → `src/Core/Ignixa.Serialization/`
  - `FhirVersion.R4.GetSchemaProvider()` → `src/Core/Ignixa.Specification/`
  - `ViewDefinitionExpressionParser` → `src/Core/Ignixa.SqlOnFhir/Parsing/`
  - `ISourceNavigator` → `src/Core/Ignixa.Abstractions/`

- [ ] **Demo data sanity check** — confirm patients.ndjson has exactly 10 lines, 7 with `"active":true`, 3 with `"active":false`. The preview demo output in the CLI script must match actual data.

- [ ] **Timing estimate** — read through all slides + demo script at speaking pace. Target: 20–22 minutes including 6 minutes of demo time.
