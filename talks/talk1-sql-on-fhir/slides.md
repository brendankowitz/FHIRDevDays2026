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

## Slide 6 — Passes the official conformance suite

**Headline:** Passes the official SQL on FHIR v2 conformance suite.

**Body:**
- The SQL on FHIR test suite ships as official JSON test files from the spec repo
- xUnit theory runner loads every test case dynamically — no cherry-picking
- Passes **100%** of official tests — no skips, no asterisks

**Speaker notes:**
100% is a number worth pausing on. The two cases that were previously skipped — JSON decimal precision and row-index ordering under UNION ALL — revealed genuine spec ambiguity. We filed feedback. The spec was clarified. v0.5.0 now passes all tests. That's what good spec implementation looks like: you find the edges, you contribute back, and eventually the suite is clean.

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
Sub-microsecond FHIRPath evaluation. The reason this matters: if you're running a nightly $export of 10 million resources and evaluating 5 ViewDefinitions per resource, you're making 50 million FHIRPath evaluations. At 500ns each, that's 25 seconds. At 300 microseconds each — which is what you'd pay without a compiled engine — that's over 4 hours. The compiled+cached approach is what makes this usable at pipeline scale.

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

---

## Slide 15 — Demo 1: CLI

**Headline:** Let's run it.

**Subhead:** preview → validate → run

**Body:**
- demo script: `talks/talk1-sql-on-fhir/demo/demo1-cli-script.md`

**Speaker notes:**
[Switch to terminal. Data files are pre-staged in `demo/data/`.] I've got a patient ViewDefinition and some NDJSON patients ready — generated by Ignixa.FhirFakes from Seattle demographics, which I'll show you how to do in the next session. Let's start with preview so you can see the schema before touching any data.

---

## Slide 16 — CLI reference

**Headline:** CLI quick reference.

**Body:**

| Command | What it does |
|---|---|
| `ignixa-sqlonfhir r4 preview --views <file>` | Show schema without data |
| `ignixa-sqlonfhir r4 preview --views <file> --input <file> --rows 10` | Show sample rows |
| `ignixa-sqlonfhir r4 validate --views <dir>` | Validate all ViewDefinitions |
| `ignixa-sqlonfhir r4 run --views <file> --input <file> --out file.parquet` | Single Parquet output |
| `ignixa-sqlonfhir r4 run --views <dir> --input <dir> --out dir/ --format csv` | Batch CSV output |
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
Two cases that revealed genuine spec ambiguity — and are now resolved:

**fn_boundary decimals**
JSON number parsing loses precision that FHIR's decimal type requires. The spec doesn't prescribe how to handle this at the JSON layer. We filed feedback → spec clarified → test now passes.

**row_index / unionAll**
Per-resource evaluation produces `%rowIndex = 0` per resource (correct). SQL UNION ALL semantics would number rows across all resources. We filed feedback → spec clarified → test now passes.

→ v0.5.0: 100% conformance suite pass.

**Speaker notes:**
The best outcome of implementing a spec rigorously: you find its edges, you contribute back, and the spec gets better. Both ambiguities are now resolved — the spec was clarified, v0.5.0 passes everything. The story here isn't "we had two skips." It's "we found the edges, we engaged with the spec body, and we helped close them." That's what this kind of open-source work enables.

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
