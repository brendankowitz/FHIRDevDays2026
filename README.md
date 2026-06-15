<div align="center">
  <h1>FHIR DevDays 2026 — Brendan Kowitz</h1>
  <p>
    <b>Slides &amp; demo code from my DevDays 2026 community talks</b>
  </p>

[![DevDays](https://img.shields.io/badge/DevDays-2026-7B2D8E)](https://www.devdays.com/)
[![FHIR](https://img.shields.io/badge/FHIR-R4%20%7C%20R4B%20%7C%20R5%20%7C%20R6%20%7C%20STU3-orange)](https://hl7.org/fhir/)
[![dotnet](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Ignixa](https://img.shields.io/badge/Built_on-Ignixa_v0.5.0-004880?logo=nuget)](https://github.com/brendankowitz/ignixa-fhir)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

</div>

---

> 👋 **Thanks for coming!** Everything from both sessions lives here — slides, speaker
> notes, and the demo code, ready for you to run yourself. Didn't make it to the room?
> You're just as welcome — the demos are self-contained and the README in each talk
> folder walks you through every command.

## 📑 The Talks

| # | Talk | What it's about | Resources |
|---|------|-----------------|-----------|
| 1 | **Flattening the Curve** | A native, conformant **SQL on FHIR** ViewDefinition engine for .NET — turn FHIR into Parquet / CSV / NDJSON with no JVM, no sidecar. | [→ talk1-sql-on-fhir](talks/talk1-sql-on-fhir/) |
| 2 | **Fluent, Fast, and Fake** | **Synthetic FHIR data** for .NET developers — a fluent, deterministic, millisecond-fast generator from a single resource to a whole population. | [→ talk2-fhir-fakes](talks/talk2-fhir-fakes/) |

Both talks are a technical retrospective on building real, open-source FHIR tooling on
**.NET**, powered by [**Ignixa**](https://github.com/brendankowitz/ignixa-fhir).

## 🚀 Run the Demos Yourself

### Prerequisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** — runs the CLI tools
  and builds/runs the C# library demos. (Ignixa v0.5.0 dual-targets `net9.0`/`net10.0`, so
  a .NET 10 SDK covers everything here.)
- *(optional)* **[DuckDB](https://duckdb.org/)** — to peek at the Parquet output in Talk 1.

### Install the CLI tools

```bash
dotnet tool install -g Ignixa.SqlOnFhir.Cli   # → ignixa-sqlonfhir
dotnet tool install -g Ignixa.FhirFakes.Cli   # → ignixa-fakes
```

### Clone this repo

```bash
git clone https://github.com/brendankowitz/FHIRDevDays2026.git
cd FHIRDevDays2026
```

➡️ Then jump into each talk's README for the step-by-step demo walkthroughs:
[**Talk 1**](talks/talk1-sql-on-fhir/README.md) · [**Talk 2**](talks/talk2-fhir-fakes/README.md)

## 🗂️ Repository Layout

```
talks/
├── talk1-sql-on-fhir/
│   ├── README.md            ← start here
│   ├── abstract.md          ← session abstract + bio
│   ├── slides.md            ← slides (Markdown)
│   ├── speaker-notes.md     ← rehearsal notes
│   ├── *.pptx               ← slides (PowerPoint)
│   └── demo/
│       ├── demo1-cli-script.md   ← CLI demo walkthrough
│       ├── demo2-library/        ← C# library demo (dotnet run)
│       └── data/                 ← ViewDefinitions + sample NDJSON
└── talk2-fhir-fakes/
    ├── README.md            ← start here
    ├── abstract.md
    ├── slides.md
    ├── speaker-notes.md
    ├── *.pptx
    └── demo/
        ├── demo1-cli-script.md
        └── demo2-library/
```

## 🔗 Built on Ignixa

The demos use packages from [**Ignixa**](https://github.com/brendankowitz/ignixa-fhir), an
open-source, high-performance FHIR ecosystem for .NET. Everything you see here is available
on NuGet:

| Package | Used for |
|---------|----------|
| [Ignixa.SqlOnFhir](https://www.nuget.org/packages/Ignixa.SqlOnFhir) | SQL on FHIR ViewDefinition evaluator (Talk 1 library demo) |
| [Ignixa.SqlOnFhir.Cli](https://www.nuget.org/packages/Ignixa.SqlOnFhir.Cli) | `ignixa-sqlonfhir` global tool (Talk 1 CLI demo) |
| [Ignixa.FhirFakes](https://www.nuget.org/packages/Ignixa.FhirFakes) | Synthetic FHIR data generator (Talk 2 library demo) |
| [Ignixa.FhirFakes.Cli](https://www.nuget.org/packages/Ignixa.FhirFakes.Cli) | `ignixa-fakes` global tool (Talk 2 CLI demo) |
| [Ignixa.Specification](https://www.nuget.org/packages/Ignixa.Specification) | Version-aware FHIR schema providers (both demos) |

📦 **Source:** [github.com/brendankowitz/ignixa-fhir](https://github.com/brendankowitz/ignixa-fhir)  ·  📚 **Docs:** [brendankowitz.github.io/ignixa-fhir](https://brendankowitz.github.io/ignixa-fhir/)

## 📄 License

Demo code in this repository is licensed under the [MIT License](LICENSE). Slide content and
speaker notes are © Brendan Kowitz — shared for attendees to learn from and reference.

---

<p align="center">
  Built with ❤️ on .NET · <b>FHIR®</b> is a registered trademark of <a href="https://www.hl7.org/">HL7</a>.
</p>
