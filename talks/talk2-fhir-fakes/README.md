# Fluent, Fast, and Fake
### .NET Synthetic FHIR Data for Developer Happiness

> **DevDays 2026** · Community Talk · 20–25 min
> Built on [Ignixa v0.5.0](https://github.com/brendankowitz/ignixa-fhir)

Every FHIR developer hits the same wall: you need realistic test data, and every option is
unsatisfying — hand-crafted JSON breaks, production data is a compliance nightmare, and
Synthea means a JVM and minutes per run. This session introduces a native **.NET** generator
that produces FHIR-valid synthetic data in **milliseconds**, with a fluent C# API and a
four-layer architecture — from a single random resource to a census-accurate population.

## 📂 In this folder

| File | What it is |
|------|------------|
| [abstract.md](abstract.md) | Session abstract + speaker bio |
| [slides.md](slides.md) | Slides in Markdown |
| [DD26_CT_260617_Brendan-Kowitz_FhirFakes-dotNET.pptx](DD26_CT_260617_Brendan-Kowitz_FhirFakes-dotNET.pptx) | Slides in PowerPoint |
| [speaker-notes.md](speaker-notes.md) | Rehearsal / speaker notes |
| [demo/](demo/) | Both demos |

## 🎬 Demos

> First time here? Install the CLI tool and clone the repo — see the
> [root quick-start](../../README.md#-run-the-demos-yourself).

### Demo 1 — CLI (`ignixa-fakes`)

Generate everything from a single patient to a full population:

```bash
# See what's available
ignixa-fakes help cities

# One valid R4 Patient from Seattle (census-accurate demographics)
ignixa-fakes r4 resource Patient --out ./output --from Seattle

# A complete clinical scenario as a batch bundle
ignixa-fakes r4 scenario DiabeticPatient --out ./output --resolved-references

# A 50-patient population as NDJSON — ready for $import or a SQL on FHIR pipeline
ignixa-fakes r4 population --out ./output --from Seattle --count 50 --ndjson
```

📜 Full walkthrough (resource → scenario → population): [demo/demo1-cli-script.md](demo/demo1-cli-script.md)

### Demo 2 — Library (C#)

All four layers from code — determinism, fluent scenarios, lifecycles, and populations:

```bash
cd talks/talk2-fhir-fakes/demo/demo2-library
dotnet run
```

Shows seeded determinism (same seed → same patient), the fluent `ScenarioBuilder`, a full
metabolic-syndrome lifecycle, and a population cohort. Source:
[demo2-library/Program.cs](demo/demo2-library/Program.cs)

## 🔗 Learn more

- **Ignixa.FhirFakes** on NuGet: [nuget.org/packages/Ignixa.FhirFakes](https://www.nuget.org/packages/Ignixa.FhirFakes)
- **Source & docs:** [github.com/brendankowitz/ignixa-fhir](https://github.com/brendankowitz/ignixa-fhir) · [docs](https://brendankowitz.github.io/ignixa-fhir/)
- **Synthea** (the inspiration): [synthetichealth.github.io/synthea](https://synthetichealth.github.io/synthea/)

---

<p align="center"><a href="../../README.md">← Back to all talks</a></p>
