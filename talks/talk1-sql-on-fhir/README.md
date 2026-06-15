# Flattening the Curve
### Implementing the SQL on FHIR ViewDefinition in .NET

> **DevDays 2026** · Community Talk · 20–25 min
> Built on [Ignixa v0.5.0](https://github.com/brendankowitz/ignixa-fhir)

SQL on FHIR is solved — in JavaScript, Python, and Java. This session is the story of
building it natively in **.NET**: a from-scratch parser, evaluator, and CLI that passes
**100%** of the official SQL on FHIR v2 conformance suite, and turns `$export` NDJSON into
Parquet, CSV, or NDJSON with no JVM, no Python, and no sidecar.

## 📂 In this folder

| File | What it is |
|------|------------|
| [abstract.md](abstract.md) | Session abstract + speaker bio |
| [slides.md](slides.md) | Slides in Markdown |
| [DD26_CT_260616_Brendan-Kowitz_SqlOnFhir.pptx](DD26_CT_260616_Brendan-Kowitz_SqlOnFhir.pptx) | Slides in PowerPoint |
| [speaker-notes.md](speaker-notes.md) | Rehearsal / speaker notes |
| [demo/](demo/) | Both demos |

## 🎬 Demos

> First time here? Install the CLI tool and clone the repo — see the
> [root quick-start](../../README.md#-run-the-demos-yourself).

### Demo 1 — CLI (`ignixa-sqlonfhir`)

Run a ViewDefinition over sample FHIR data, straight from the command line:

```bash
cd talks/talk1-sql-on-fhir/demo/data

# Preview the output schema — no data needed
ignixa-sqlonfhir r4 preview --views patient-view.json

# Run one ViewDefinition over NDJSON → Parquet
ignixa-sqlonfhir r4 run --views patient-view.json --input patients.ndjson --out patients.parquet

# Batch mode: every ViewDefinition × every resource type, in one command
ignixa-sqlonfhir r4 run --views views/ --input fhir-ndjson/ --out output/ --format parquet
```

📜 Full walkthrough (preview → validate → run → batch): [demo/demo1-cli-script.md](demo/demo1-cli-script.md)
🟢 Run it live in VS Code ([runme.dev](https://runme.dev/) one-click cells): [demo/demo1-runme.md](demo/demo1-runme.md)

### Demo 2 — Library (C#)

The same evaluation from code — the entire API surface in one small program:

```bash
cd talks/talk1-sql-on-fhir/demo/demo2-library
dotnet run
```

Loads a ViewDefinition, evaluates it over 20 patients, and prints the flattened rows.
Source: [demo2-library/Program.cs](demo/demo2-library/Program.cs)

### The payoff — a real clinical query

Flattening is the means; the question is the end. Two closing demos join the flattened
Parquet back together with plain SQL (DuckDB) to answer a clinical question:

- **Diabetic registry** — Patient ⋈ Condition ⋈ HbA1c Observations → who's above the 7%
  target and trending worse. See Beat 6 in [demo/demo1-cli-script.md](demo/demo1-cli-script.md).
- **Growth chart** — height-for-age across a pediatric cohort → a population growth curve.
  See [demo/demo-growth-chart.md](demo/demo-growth-chart.md).

Both cohorts are generated with `ignixa-fakes` and committed under `demo/data/`. The demo
scripts are [runme.dev](https://runme.dev/)-friendly — each command is a one-click cell in VS Code.

## 🔗 Learn more

- **Ignixa.SqlOnFhir** on NuGet: [nuget.org/packages/Ignixa.SqlOnFhir](https://www.nuget.org/packages/Ignixa.SqlOnFhir)
- **Source & docs:** [github.com/brendankowitz/ignixa-fhir](https://github.com/brendankowitz/ignixa-fhir) · [docs](https://brendankowitz.github.io/ignixa-fhir/)
- **SQL on FHIR spec:** [sql-on-fhir.org](https://sql-on-fhir.org/)

---

<p align="center"><a href="../../README.md">← Back to all talks</a></p>
