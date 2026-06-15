# FHIR DevDays 2026 — Presentation Design

**Date:** 2026-05-19  
**Format:** Community talk, 20–25 minutes each, live-recorded  
**Approach:** Story-first (pain → solution → architecture → examples → live demos → retrospective)  
**Audience:** FHIR developers comfortable with the spec; familiar with SQL on FHIR and Synthea; lacking a native .NET solution  
**Source repo:** `C:\Src\fhir-server-contrib` (ignixa-fhir, main branch)  
**Docs:** https://brendankowitz.github.io/ignixa-fhir/  
**GitHub:** https://github.com/brendankowitz/ignixa-fhir

---

## Talk 1 — "Flattening the Curve: Implementing the SQL on FHIR ViewDefinition in .NET"

### Abstract (from reference/talk-abstracts.md)
> The emerging SQL on FHIR specification takes great strides to solve the "nested data" problem for analytics, but we need a variety of robust tooling for all ecosystems. To bridge this gap for the .NET ecosystem, this session looks at building an open-source project featuring a native parser, evaluator, and CLI that passes the official SQL on FHIR conformance test suite.
>
> This session is a technical retrospective on implementing a complex FHIR standard from scratch. Beyond just spec compliance, we will explore how a high-performance .NET native engine enables modern data pipelines to export into a Data Lake in Parquet format, simplifying and accelerating downstream data processing.

---

### Slide Structure (20 slides, ~22 minutes)

#### Section 1 — The Problem (~3 min)

**Slide 1 — Title**
- "Flattening the Curve: Implementing the SQL on FHIR ViewDefinition in .NET"
- Speaker name, role (Engineering Manager, Microsoft / Azure FHIR Server), Ignixa project
- DevDays 2026

**Slide 2 — You know SQL on FHIR. Here's the .NET problem.**
- Quick acknowledgment: spec exists (HL7 standard), other implementations exist (reference JS impl, Python libraries)
- The real problem: "If you're building in .NET, you're either calling a sidecar JVM process, wrapping a Python subprocess, or writing it yourself."
- *Speaker note:* Don't re-explain what SQL on FHIR is — this audience knows. Jump straight to the ecosystem gap.

**Slide 3 — What .NET devs reach for today**
- Option A: Firely SDK + FHIRPath LINQ — powerful but has no native ViewDefinition support; you still have to write extraction logic
- Option B: Wrap an external runtime (Java/Python) — adds operational overhead, breaks native deployment, sidecar process management in production
- Option C: Hand-roll FHIRPath extraction — brittle, no conformance guarantee, reimplemented per project
- *Speaker note:* Brief code snippet showing each option's friction. The goal is to make the audience nod — they've been here.

**Slide 4 — The gap: no native, conformant .NET implementation**
- Single statement slide: "Until now."
- *Speaker note:* Pause here. Let it land.

---

#### Section 2 — How This Solves It (~3 min)

**Slide 5 — Introducing Ignixa.SqlOnFhir**
- Native .NET 9, no JVM dependency, no sidecar
- Two distribution paths: NuGet package (`Ignixa.SqlOnFhir`) and CLI (`Ignixa.SqlOnFhir.Cli`)
- Install commands:
  ```bash
  dotnet add package Ignixa.SqlOnFhir
  dotnet tool install -g Ignixa.SqlOnFhir.Cli
  ```
- Open source, MIT license

**Slide 6 — It passes the official conformance suite**
- The SQL on FHIR v2 test suite: dynamically loaded JSON test files covering the full spec
- `OfficialSqlOnFhirTestRunner` — theory-based xUnit runner that loads every spec test case
- Passes all tests with two documented edge-case skips:
  1. `fn_boundary` decimal tests — precision preservation issue with JSON parsing (known JSON decimal limitation)
  2. `row_index` unionAll ordering — per-resource evaluation ordering vs SQL UNION ALL ordering; `%rowIndex` semantics are correct
- *Speaker note:* Being honest about the skips is more credible than claiming 100%. The spec has ambiguities — finding them is part of implementing it.

**Slide 7 — And it's fast — because it has to be**
- SQL on FHIR at scale means evaluating ViewDefinitions across millions of resources in a `$export` pipeline — FHIRPath performance is load-bearing
- BenchmarkDotNet results (Intel Core i7-14700K, .NET 9.0):

| Operation | Ignixa |
|---|---|
| Simple FHIRPath (`Patient.name.family`) | 345 ns / 1.1 KB |
| Array indexing (`Patient.name[0].given`) | 484 ns / 1.5 KB |
| Complex navigation (`where + first`) | 675 ns / 2.3 KB |
| Search parameter extraction | 1,508 ns / 4.6 KB |

- *Speaker note:* "We needed a FHIRPath engine that could keep up with batch workloads. Sub-microsecond evaluation per expression means processing millions of resources doesn't become a bottleneck. The compiled + cached approach is what makes this possible."

---

#### Section 3 — Architecture (~4 min)

**Slide 8 — Package anatomy**
- Dependency stack (bottom to top):
  ```
  Ignixa.Abstractions          (IElement, ISourceNavigator — no deps)
       ↓
  Ignixa.Serialization         (JSON parsing, JsonSourceNodeFactory)
  Ignixa.Specification         (version-aware schema providers: R4, R4B, R5, R6, STU3)
  Ignixa.FhirPath              (compiled FHIRPath expression engine)
       ↓
  Ignixa.SqlOnFhir             (ViewDefinition parser + evaluator)
  Ignixa.SqlOnFhir.Writers     (Parquet, CSV, NDJSON output)
       ↓
  Ignixa.SqlOnFhir.Cli         (dotnet global tool)
  ```
- "Pick what you need. Each layer is a standalone NuGet package."

**Slide 9 — The FHIRPath engine underneath**
- Key design: compiled + cached expressions — parse once, evaluate many times
- Zero-copy approach: `JsonSourceNodeFactory` parses JSON into lightweight `ISourceNavigator` without full type conversion to a POCO tree
- `IElement` (typed) vs `ISourceNavigator` (lightweight) — only convert when the spec requires typing
- Serialization benchmark: large Bundle parsed in 70 µs, 47 KB allocated — important when processing `$export` output at scale
- *Speaker note:* "We don't deserialize to a C# object model — we navigate the JSON directly. Parse once, evaluate many ViewDefinition expressions against the same in-memory structure. This is what keeps the memory footprint flat even on large datasets."

**Slide 10 — Parser → Evaluator → Writers**
- Three-stage pipeline:
  1. **Parsing** — `ViewDefinitionExpressionParser.Parse()` reads ViewDefinition JSON, compiles all FHIRPath expressions upfront
  2. **Evaluation** — `SqlOnFhirEvaluator.Evaluate()` processes each FHIR resource: WHERE filtering, SELECT column extraction, forEach unnesting into row groups
  3. **Writing** — `ParquetFileWriter`, `CsvFileWriter`, `NdjsonFileWriter` consume the row stream
- forEach unnesting: a single resource can produce N rows (one per array element), with `%resource` reference back to the root
- Variables: caller-supplied `--var name=value` override ViewDefinition constants at runtime

**Slide 11 — Multi-version: one codebase**
- FHIR version is the first CLI argument: `stu3`, `r4`, `r4b`, `r5`, `r6`
- Resolved to a version-specific `ISchema` via `FhirSpecificationExtensions.FromVersionString()`
- Same ViewDefinition JSON works across versions (paths are version-appropriate)
- *Speaker note:* "You pick the version once per command or once per DI registration. The rest of your code is version-agnostic."

---

#### Section 4 — Examples of Use (~3 min)

**Slide 12 — A ViewDefinition in practice**
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
        { "name": "id",          "path": "id",           "type": "string" },
        { "name": "family_name", "path": "name.where(use='official').first().family", "type": "string" },
        { "name": "given_name",  "path": "name.where(use='official').first().given.first()", "type": "string" },
        { "name": "birth_date",  "path": "birthDate",    "type": "date" }
      ]
    }
  ]
}
```
- "If you've used the HL7 reference implementation, this is identical syntax — that's the point."

**Slide 13 — The forEach pattern**
- Show unnesting names array:
  ```json
  {
    "select": [{
      "column": [{ "name": "id", "path": "%resource.id", "type": "string" }]
    }, {
      "forEach": "name",
      "column": [
        { "name": "use",    "path": "use",    "type": "string" },
        { "name": "family", "path": "family", "type": "string" }
      ]
    }]
  }
  ```
- Result table: one row per name, id repeated
- *Speaker note:* "This is the fundamental operation that makes FHIR analytics work — turning nested arrays into flat rows that SQL engines can consume."

**Slide 14 — Library usage in C#**
```csharp
var schemaProvider = FhirVersion.R4.GetSchemaProvider();

var viewDef = JsonSourceNodeFactory.Parse(File.ReadAllText("patient-view.json"))
                                   .ToSourceNavigator();
var patient = JsonSourceNodeFactory.Parse(File.ReadAllText("patient.json"))
                                   .ToElement(schemaProvider);

var evaluator = new SqlOnFhirEvaluator();
foreach (var row in evaluator.Evaluate(viewDef, patient))
{
    Console.WriteLine($"{row["id"]} — {row["family_name"]}, {row["birth_date"]}");
}
```
- "No subprocess. No sidecar. No JVM. Ten lines."

---

#### Section 5 — Demos (~6 min)

**Slide 15 — Demo 1: CLI**
- *Live demo — pre-stage: have a `patient-view.json` and `patients.ndjson` ready*
- Sequence:
  ```bash
  # Preview schema without data
  ignixa-sqlonfhir r4 preview --views patient-view.json

  # Validate ViewDefinition
  ignixa-sqlonfhir r4 validate --views patient-view.json

  # Run — single file to Parquet
  ignixa-sqlonfhir r4 run --views patient-view.json --input patients.ndjson --out patients.parquet

  # Batch mode — directory of views to a directory of Parquet files
  ignixa-sqlonfhir r4 run --views ./views --input ./fhir-ndjson --out ./output --format parquet

  # Runtime variables
  ignixa-sqlonfhir r4 run --views patient-view.json --input patients.ndjson --out out.csv --var cohortId=research-2026
  ```
- Show the Parquet output (open in DuckDB or a Parquet viewer)

**Slide 16 — Bridge: CLI reference**
| Command | Purpose |
|---|---|
| `preview --views <file>` | Show schema without running |
| `preview --views <file> --input <file> --rows 10` | Show sample data |
| `validate --views <dir>` | Validate all ViewDefinitions |
| `run --out file.parquet` | Single-file Parquet output |
| `run --out dir --format csv` | Batch CSV output |
| `--var name=value` | Override ViewDefinition constants |
| `--stats-out stats.json` | Batch run statistics |

**Slide 17 — Demo 2: Library**
- *Live demo — pre-stage: small .NET console app or xUnit test project*
- Show:
  1. `dotnet add package Ignixa.SqlOnFhir`
  2. Load NDJSON from disk, evaluate ViewDefinition, print rows
  3. Show how it slots into a pipeline: iterate NDJSON, evaluate per resource, write to output
  4. Brief: runtime variable override via `Dictionary<string, string>`

---

#### Section 6 — What We Learned (~2 min)

**Slide 18 — Implementing a spec is a conversation with the spec**
- The two skipped test cases revealed genuine ambiguities in the SQL on FHIR v2 specification
- `row_index` / unionAll: per-resource evaluation vs batch evaluation produce different row orderings — the spec doesn't prescribe which is correct
- `fn_boundary` decimals: JSON number precision vs FHIR decimal semantics — a known tension between JSON parsing and FHIR's high-precision decimal requirement
- *Speaker note:* "We filed feedback. Implementing a spec at this level of detail is one of the most effective ways to find its edges."

**Slide 19 — What this unlocks for .NET**
- `$export` → ViewDefinitions → Parquet → Data Lake: end-to-end FHIR analytics pipeline without leaving .NET
- BI dashboards (Power BI, Tableau) directly consuming Parquet
- Research cohort extraction with reproducible ViewDefinition definitions
- CI pipelines that validate ViewDefinitions against schema before deployment
- *Speaker note:* "The $export operation gives you NDJSON. ViewDefinitions give you schema. Parquet gives you a format every analytics tool understands. This is the pipeline."

**Slide 20 — Get started**
- Package: `dotnet add package Ignixa.SqlOnFhir`
- CLI: `dotnet tool install -g Ignixa.SqlOnFhir.Cli`
- GitHub: https://github.com/brendankowitz/ignixa-fhir
- Docs: https://brendankowitz.github.io/ignixa-fhir/
- MIT license — contributions welcome

---

### Key Technical Facts (reference)

**Conformance:** Passes SQL on FHIR v2 official test suite. Two documented skips with clear rationale.

**Performance (BenchmarkDotNet, .NET 9, i7-14700K):**
- Simple FHIRPath: 345 ns / 1.11 KB
- Array indexing: 484 ns / 1.54 KB
- Large Bundle parse: 70 µs / 47 KB
- Large Bundle serialize: 90 µs / 79 KB
- *Note: raw benchmark data retains Firely comparison — do not show in slides. Frame performance as a batch pipeline requirement, not a comparison.*

**FHIR versions:** STU3, R4, R4B, R5, R6

**Output formats:** Parquet, CSV, NDJSON

**Key packages:**
- `Ignixa.Abstractions` — core interfaces, no deps
- `Ignixa.Serialization` — JSON parsing, `JsonSourceNodeFactory`
- `Ignixa.FhirPath` — compiled FHIRPath engine
- `Ignixa.SqlOnFhir` — ViewDefinition parser + evaluator
- `Ignixa.SqlOnFhir.Writers` — Parquet/CSV/NDJSON writers
- `Ignixa.SqlOnFhir.Cli` — dotnet global tool (`ignixa-sqlonfhir`)

---

---

## Talk 2 — "Fluent, Fast, and Fake: .NET Synthetic FHIR Data for Developer Happiness"

### Abstract (from reference/talk-abstracts.md)
> Every FHIR developer faces the same hurdle: getting good test data. We often rely on the robust, industry-standard Synthea for population simulations, or we fall back to hand-crafting or example JSON files for testing. But for .NET developers building high-velocity applications, neither option is ideal. We need data that is lightweight, deterministic, and tightly integrated into our test suites.
>
> We introduce a native .NET generator prioritizing developer experience. Explore a 4-layer architecture, from random schema-based generation to fluent builders, expressing clinical intent in fluent C#. Learn to create deterministic test data or use the CLI to spawn localized cohorts (e.g. "1,000 patients from Seattle") for pipeline stress-testing.

---

### Slide Structure (20 slides, ~22 minutes)

#### Section 1 — The Problem (~3 min)

**Slide 1 — Title**
- "Fluent, Fast, and Fake: .NET Synthetic FHIR Data for Developer Happiness"
- Speaker name, role, Ignixa project
- DevDays 2026

**Slide 2 — Every FHIR dev hits this wall**
- Story: you're writing a test. You need a Patient with type-2 diabetes, 3 conditions, 2 medications, realistic vitals over 5 years. Your options:
  - Hand-craft JSON: hours of work, breaks when you change your model, no clinical realism
  - Copy from production: compliance/privacy nightmare, can't do in CI
  - Fire up Synthea: JVM + minutes to generate + non-deterministic per test run
- *Speaker note:* Make this vivid. The hand-crafted JSON with 200 lines of boilerplate is universal FHIR dev pain.

**Slide 3 — Synthea is great — for some things**
- Give genuine credit: Synthea is the gold standard for population simulation and epidemiological realism
- Where it's the right tool: research datasets, population health studies, interoperability testing at scale
- Where .NET devs hit friction:
  - JVM dependency in a .NET project
  - Minutes to generate (not milliseconds)
  - Not deterministic per test (hard to use in unit tests)
  - No NuGet package — no `dotnet add package`
  - No fluent API — can't express clinical intent in C#
- *Speaker note:* "Synthea is not the competition. It's the inspiration. We solve a different problem."

**Slide 4 — What we actually need in a test suite**
- The requirements matrix:
  | Requirement | Hand-crafted JSON | Synthea | FhirFakes |
  |---|---|---|---|
  | Fast (milliseconds) | ✓ | ✗ | ✓ |
  | Deterministic | ✓ (static) | ✗ | ✓ (seeded) |
  | Native .NET / NuGet | ✓ | ✗ | ✓ |
  | Clinically realistic | ✗ | ✓ | ✓ |
  | Fluent C# API | ✗ | ✗ | ✓ |
  | FHIR-valid | maybe | ✓ | ✓ |
  | Population-scale | ✗ | ✓ | ✓ |

---

#### Section 2 — How This Solves It (~3 min)

**Slide 5 — Introducing Ignixa.FhirFakes**
- Native .NET 9, no JVM, generates in milliseconds
- Two paths: NuGet package and CLI
  ```bash
  dotnet add package Ignixa.FhirFakes
  dotnet tool install -g Ignixa.FhirFakes.Cli
  ```
- Generates FHIR-valid resources (passes schema + binding validation)
- MIT license, open source

**Slide 6 — Four layers, pick your level**
- Visual stack:
  ```
  Layer 4: Population Generation    → "1,000 patients from Seattle"
  Layer 3: Patient Lifecycles       → "50-year-old with metabolic syndrome"
  Layer 2: Clinical Scenarios       → "Wellness visit with 15% chance of hyperlipidemia"
  Layer 1: Random Valid Resources   → "A valid Patient resource"
  ```
- "Use as much or as little as you need. Each layer builds on the one below."

**Slide 7 — And a CLI for pipeline testing**
- Quick demo preview:
  ```bash
  ignixa-fakes r4 population --from Seattle --count 1000 --ndjson
  ```
- Output ready for `$import`, SQL on FHIR pipeline, load testing
- *Speaker note:* "One command gives you a realistic cohort — names, ages, race distribution matching Seattle census data — as NDJSON ready to pipe into your server."

---

#### Section 3 — Architecture (~4 min)

**Slide 8 — Layer 1: Random but valid resources**
- `SchemaBasedFhirResourceFaker` — generates any FHIR resource type
- Respects FHIR profiles and terminology bindings (`BindingAwareGenerator`)
- Generates valid cross-references and complex types
- Supports all FHIR versions: STU3, R4, R4B, R5, R6
- *Speaker note:* "This is the foundation. It knows the schema. It doesn't just fill fields with random strings — it uses valid coded values from the right value sets."

**Slide 9 — Layer 2: Clinical scenarios**
- `ScenarioBuilder` — fluent composition API
- Key components:
  - `ProbabilisticBranchState` — evidence-based disease onset (e.g., 15% chance of hyperlipidemia)
  - `VitalSignCorrelationEngine` — BMI correlates with conditions, lab values are physiologically plausible
  - `CommonScenarios` — reusable clinical fragments (`RecordVitalSigns()`, `LipidPanel()`, `BloodGlucose`)
  - State machine: `ConditionOnsetState`, `MedicationOrderState`, `DelayState`, `EncounterState`

**Slide 10 — Layer 3: Patient lifecycles**
- `PatientLifecycleGenerator` — age-based event scheduling from birth to present
- `DiseaseRiskCalculator` — evidence-based risk modeling, age-adjusted onset probabilities
- `AdultWellnessSchedule`, `PediatricWellnessSchedule` — realistic visit frequency by age
- Pre-built archetypes in `LifecycleExampleScenarios`:
  - Healthy child (0–18 years)
  - Typical adult (0–45 years)
  - Metabolic syndrome (0–50 years, generates 40–45 encounters, 150+ vitals/labs)
  - Pediatric asthma (0–10 years)
  - Elderly multi-morbidity (0–80 years)

**Slide 11 — Layer 4: Population generation**
- `PopulationGenerator` — large-scale cohort creation
- `KnownCities` — 11 major US cities with real US Census demographics:
  - Age distribution sampled from city profile
  - Race distribution (e.g., Boston: 53% White, 25% Black, 19% Hispanic, 9% Asian)
  - Zip codes and area codes matching city
- `EthnicNameGenerator` — culturally appropriate names via Bogus locales
- Full medical history generated per patient from birth to current age

---

#### Section 4 — Examples of Use (~3 min)

**Slide 12 — Layer 1 in a unit test**
```csharp
var schemaProvider = new R4CoreSchemaProvider();
var faker = new SchemaBasedFhirResourceFaker(schemaProvider);

var patient = faker.Generate<Patient>();
// Valid R4 Patient — correct structure, valid coded values
Assert.NotNull(patient.Id);
```
- Show old alternative: 50-line JSON fixture file
- *Speaker note:* "Three lines. No file. No subprocess. And it passes FHIR validation."

**Slide 13 — Layer 2: expressing clinical intent**
```csharp
var context = new ScenarioBuilder(schemaProvider)
    .WithPatient(p => p.WithAge(55).WithGender(g => g.Male))
    .AddEncounter("Annual wellness visit")
    .AddSubScenario(CommonScenarios.RecordVitalSigns(), "Vitals")
    .AddSubScenario(CommonScenarios.LipidPanel(), "Cholesterol Screening")
    .AddProbabilisticBranch(
        probability: 0.15,
        truePath: new ConditionOnsetState { Code = FhirCode.Conditions.Hyperlipidemia }
                      .ThenAddMedicationOrder(MedicationOrderState.Atorvastatin20mg())
                      .ThenDelay(TimeSpan.FromDays(90))
                      .ThenAddEncounter("Lipid panel follow-up"),
        falsePath: new DelayState { Exact = TimeSpan.Zero }
    )
    .Build();
```
- *Speaker note:* "This reads like a clinical protocol. The 15% branch means 15% of your test population will have hyperlipidemia and be on a statin — matching real-world prevalence."

**Slide 14 — Layer 3 + 4: lifecycle and population**
```csharp
// Layer 3: full lifecycle archetype
var context = LifecycleExampleScenarios.GetMetabolicSyndromeLifecycle(schemaProvider);
// Patient: 50 years old
// Conditions: 2–4 chronic conditions
// Medications: 3–6 medications
// Encounters: 40–45 visits over 50 years
// Observations: 150+ vitals and labs

// Layer 4: population cohort
var generator = new PopulationGenerator(schemaProvider);
var patients = generator.Generate("Massachusetts", 100);
// 100 patients with Boston census demographics
// Culturally appropriate names, realistic age/race distribution
// Full medical history per patient
```

---

#### Section 5 — Demos (~6 min)

**Slide 15 — Demo 1: CLI**
- *Live demo — pre-stage: CLI installed, output directory ready*
- Sequence:
  ```bash
  # Discover what's available
  ignixa-fakes help scenarios
  ignixa-fakes help cities

  # Single patient resource
  ignixa-fakes r4 resource Patient --out ./output --firstname Alice --surname Johnson --from Seattle

  # Clinical scenario
  ignixa-fakes r4 scenario DiabeticPatient --out ./output --resolved-references

  # Population — bundles
  ignixa-fakes r4 population --out ./output --from Boston --count 50 --resolved-references

  # Population — NDJSON for $import or SQL on FHIR pipeline
  ignixa-fakes r4 population --out ./output --from Seattle --count 100 --ndjson
  ```
- Open a generated bundle, walk through the patient and related resources

**Slide 16 — Bridge: CLI reference**
| Command | Output |
|---|---|
| `resource Patient` | Single Patient JSON |
| `resource Observation BloodGlucose` | Observation with predefined state |
| `scenario DiabeticPatient` | Bundle with full scenario |
| `population --from Seattle --count 100` | 100-patient bundle |
| `population --ndjson` | Per-resource NDJSON files (for `$import`) |
| `help scenarios` | List all available scenarios |
| `help cities` | List all available cities + demographics |

**Slide 17 — Demo 2: Library in a test project**
- *Live demo — pre-stage: xUnit test project with FhirFakes NuGet reference*
- Show:
  1. Layer 1: generate a Patient in a unit test, assert validity
  2. Layer 2: ScenarioBuilder — build diabetic patient, assert condition code present, assert medication count
  3. Determinism: run twice with same seed, show same resource IDs
  4. `FromCity` builder: generate patient with Seattle demographics, show name/zip/phone match

---

#### Section 6 — What We Learned (~2 min)

**Slide 18 — Clinical realism is a design constraint**
- Building `VitalSignCorrelationEngine`: vitals can't be independent random values
  - BMI must correlate with obesity-related conditions
  - Blood glucose must correlate with diabetes diagnosis
  - Lab values must be physiologically plausible (HbA1c > 6.5 if diabetic)
- The FHIR spec tells you the schema. Clinical medicine tells you the constraints.
- *Speaker note:* "Random ≠ realistic. A patient with BMI of 18 and a diabetes diagnosis with normal glucose is technically valid FHIR but meaningless for testing. We had to model the correlations."

**Slide 19 — Determinism vs realism is a deliberate trade-off**
- Seeded generation: FhirFakes uses [Bogus](https://github.com/bchavez/Bogus) under the hood — set `Randomizer.Seed = new Random(42)` before generating and output is deterministic. Same seed, same patient, stable tests.
- Population simulation: realistic variance, not reproducible per-run — right for load testing and pipeline validation
- The 4-layer architecture makes this explicit: Layers 1–2 benefit from seeding, Layer 4 embraces variance deliberately
- *Speaker note:* "The worst outcome is accidentally using a non-deterministic generator in a unit test — your tests flake. We made the determinism story explicit: seed Bogus for unit tests, let it run free for load tests."

**Slide 20 — Get started**
- Package: `dotnet add package Ignixa.FhirFakes`
- CLI: `dotnet tool install -g Ignixa.FhirFakes.Cli`
- GitHub: https://github.com/brendankowitz/ignixa-fhir
- Docs: https://brendankowitz.github.io/ignixa-fhir/
- Inspired by Synthea — complements it, doesn't replace it
- MIT license

---

### Key Technical Facts (reference)

**4-layer architecture:**
1. `SchemaBasedFhirResourceFaker` — schema-valid random resources, respects FHIR bindings
2. `ScenarioBuilder` + `VitalSignCorrelationEngine` + `ProbabilisticBranchState` — clinical scenarios
3. `PatientLifecycleGenerator` + `DiseaseRiskCalculator` + lifecycle archetypes — full patient histories
4. `PopulationGenerator` + `KnownCities` + `EthnicNameGenerator` — realistic population cohorts

**Available lifecycle archetypes:**
- Healthy child (0–18)
- Typical adult (0–45)
- Metabolic syndrome (0–50) — generates 40–45 encounters, 3–6 medications, 150+ observations
- Pediatric asthma (0–10)
- Elderly multi-morbidity (0–80)

**KnownCities:** 11 major US cities with US Census demographics, real zip codes, real area codes

**FHIR versions:** STU3, R4, R4B, R5, R6

**Key packages:**
- `Ignixa.FhirFakes` — core library (depends on `Ignixa.Specification`, `Ignixa.Serialization`, Bogus)
- `Ignixa.FhirFakes.Cli` — dotnet global tool (`ignixa-fakes`)

**CLI scenarios (partial list):** DiabeticPatient, WellnessVisit, UrinaryTractInfection, AsthmaticChild, and more

---

## Demo Pre-Staging Checklist

### Talk 1 (SQL on FHIR) — prepare before talk:
- [ ] `patient-view.json` — simple Patient ViewDefinition (id, family_name, given_name, birth_date, active filter)
- [ ] `patients.ndjson` — 5–10 Patient resources as NDJSON
- [ ] `./views/` directory — 2–3 ViewDefinitions for batch demo
- [ ] `./fhir-ndjson/` directory — matching NDJSON files
- [ ] Parquet viewer ready (DuckDB CLI or similar)
- [ ] Small .NET console app / test project with `Ignixa.SqlOnFhir` referenced
- [ ] `ignixa-sqlonfhir` tool installed and on PATH

### Talk 2 (FhirFakes) — prepare before talk:
- [ ] `./output/` directory (empty, writable)
- [ ] xUnit test project with `Ignixa.FhirFakes` referenced
- [ ] Two pre-written tests: Layer 1 (simple generate) and Layer 2 (ScenarioBuilder)
- [ ] Determinism demo: test that runs twice and asserts same ID
- [ ] `ignixa-fakes` tool installed and on PATH

---

## Shared Narrative Principles

1. **Lead with pain the audience recognizes** — don't explain the problem from first principles; ask them to nod
2. **Benchmark numbers are not bragging** — they're motivation for why a native implementation matters
3. **Honest about edge cases** — the skipped conformance tests, the Synthea comparison — credibility > perfection
4. **Demos are payoff, not proof** — the audience should already believe it works before the demo starts
5. **Leave with one action** — `dotnet add package` or `dotnet tool install` — make it trivially easy to try

---

## Refinement Log

- 2026-05-19: Initial design. Audience assumption set: knows SQL on FHIR, familiar with Synthea, lacks native .NET solution.
- 2026-05-19: Talk 1 trimmed from 21 to 20 slides after removing "what is SQL on FHIR" explainer.
- 2026-05-19: Both talks confirmed story-first (Approach A).
