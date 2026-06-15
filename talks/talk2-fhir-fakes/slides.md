# Fluent, Fast, and Fake
## .NET Synthetic FHIR Data for Developer Happiness

> Talk: DevDays 2026 | Community Talk | 20–25 min
> Source: https://github.com/brendankowitz/ignixa-fhir
> Audience: FHIR developers familiar with Synthea; building in .NET

---

## Slide 1 — Title

**Headline:** Fluent, Fast, and Fake

**Subhead:** .NET Synthetic FHIR Data for Developer Happiness

**Body:**
- Brendan Kowitz
- Engineering Manager, Microsoft — Azure FHIR Server
- Ignixa: open-source FHIR ecosystem for .NET
- DevDays 2026

**Speaker notes:**
Every FHIR developer I've talked to has hit the same wall. You're writing a test. You need realistic data. And every option in front of you is unsatisfying. This session is about fixing that for .NET developers.

---

## Slide 2 — Every FHIR dev hits this wall

**Headline:** Every FHIR dev hits this wall.

**Body:**
You're writing a test. You need a Patient with type-2 diabetes, 3 conditions, 2 medications, realistic vitals over 5 years.

Your options:
- **Hand-craft JSON** — hours of work, breaks when your model changes, zero clinical realism
- **Copy from production** — compliance nightmare, can't run in CI
- **Fire up Synthea** — JVM + minutes to generate + non-deterministic per test run

**Speaker notes:**
I want you to feel this. Not abstractly — concretely. You open your test file. You need a diabetic patient. You start writing the Patient resource JSON. You add the name array, the identifier array, the address. Then you add the Condition. Then you realise you need an Encounter for context. Then you need Observations for the labs. Two hours later you have 400 lines of JSON that breaks every time you update your model. You've been there. I've been there. Let's talk about a better way.

---

## Slide 3 — Synthea is great — for some things

**Headline:** Synthea is great — for some things.

**Body:**
Give credit where it's due:
- Gold standard for population simulation and epidemiological realism
- Powers research datasets worldwide
- Mature, battle-tested, community-supported

Where .NET developers hit friction:
- JVM dependency in a .NET project
- Minutes to generate (not milliseconds)
- Not deterministic per test run — hard to use in unit tests
- No NuGet package, no `dotnet add package`
- No fluent C# API — can't express clinical intent inline

**Speaker notes:**
Synthea is not the competition. It's the inspiration. We solve a different problem. Synthea is what you reach for when you're doing population health research or building a reference dataset. FhirFakes is what you reach for when you're writing a unit test at 11pm and you need a diabetic patient in the next 10 seconds.

---

## Slide 4 — What we actually need in a test suite

**Headline:** What developers actually need.

**Body:**

| Requirement | Hand-crafted | Synthea | FhirFakes |
|---|---|---|---|
| Fast (milliseconds) | ✓ | ✗ | ✓ |
| Deterministic / seedable | ✓ (static) | ✗ | ✓ |
| Native .NET / NuGet | ✓ | ✗ | ✓ |
| Clinically realistic | ✗ | ✓ | ✓ |
| Fluent C# API | ✗ | ✗ | ✓ |
| FHIR-valid output | maybe | ✓ | ✓ |
| Population-scale | ✗ | ✓ | ✓ |

**Speaker notes:**
This is the requirements matrix nobody wrote down but everyone intuitively knows. Hand-crafted JSON wins on speed and determinism but loses on realism and maintainability. Synthea wins on realism and scale but loses on everything .NET developers need for test suites. FhirFakes is designed to fill the entire matrix.

---

## Slide 5 — Introducing Ignixa.FhirFakes

**Headline:** Ignixa.FhirFakes

**Body:**
- Native .NET 9 — no JVM, generates in milliseconds
- `dotnet add package Ignixa.FhirFakes`
- `dotnet tool install -g Ignixa.FhirFakes.Cli`
- FHIR-valid output — passes schema + binding validation
- Multi-version: STU3, R4, R4B, R5, R6
- MIT license. Open source. Inspired by Synthea.

**Speaker notes:**
Two minutes to integrate. The NuGet package gives you the full generator API. The CLI gives you a tool for pipeline testing and load testing. Everything generates valid FHIR — correct coded values from the right value sets, valid cross-references, proper cardinality.

---

## Slide 6 — Four layers. Pick your level.

**Headline:** Four layers. Pick your level.

**Body:**
```
Layer 4: Population Generation    "ignixa-fakes r4 population --from Seattle --count 1000"
Layer 3: Patient Lifecycles       "50-year-old with metabolic syndrome, full history"
Layer 2: Clinical Scenarios       "Wellness visit, 15% chance of hyperlipidemia"
Layer 1: Random Valid Resources   "A valid Patient resource"
```
Use as much or as little as you need. Each layer builds on the one below.

**Speaker notes:**
This is the core design. You don't have to commit to the full stack. If you just need a valid Patient resource with correct structure, take Layer 1 — three lines. If you need a clinically coherent patient history for an integration test, go to Layer 3. If you're doing load testing on a pipeline, go to Layer 4.

---

## Slide 7 — And a CLI for pipeline testing

**Headline:** And a CLI for pipeline testing.

**Body:**
```bash
ignixa-fakes r4 population --from Seattle --count 1000 --ndjson --out ./output
```

- One command: 1,000 patients from Seattle
- Census-accurate demographics, culturally appropriate names
- Full medical history per patient
- NDJSON output ready for `$import` or SQL on FHIR pipeline

**Speaker notes:**
If you watched the previous session, you saw these patients already — they were the demo data for the SQL on FHIR talk. One command. Realistic cohort. NDJSON ready to pipe into your server or your analytics pipeline.

---

## Slide 8 — Layer 1: Random but valid resources

**Headline:** Layer 1: Random but valid.

**Body:**
- `SchemaBasedFhirResourceFaker` — generates any FHIR resource type
- Respects FHIR profiles and terminology bindings (`BindingAwareGenerator`)
- Valid coded values from correct value sets — not random strings
- Valid cross-references and complex types
- All FHIR versions: STU3, R4, R4B, R5, R6

**Speaker notes:**
The foundation of everything. Layer 1 knows the FHIR schema. It doesn't just fill fields with random strings — it uses the correct coded values from the right value sets. A generated Patient has a valid gender code. A generated Observation has a valid LOINC code. It's not just structurally valid — it's semantically valid.

---

## Slide 9 — Layer 2: Clinical scenarios

**Headline:** Layer 2: Express clinical intent in C#.

**Body:**
`ScenarioBuilder` — fluent composition API:
- `WithPatient(p => p.WithAge(55).WithGender(g => g.Male))`
- `AddEncounter("Annual wellness visit")`
- `AddSubScenario(CommonScenarios.RecordVitalSigns())`
- `AddProbabilisticBranch(0.15, trueState, falseState)` — evidence-based branching

Key components:
- `ProbabilisticBranchState` — 15% chance of hyperlipidemia matches real-world prevalence
- `VitalSignCorrelationEngine` — BMI correlates with conditions, labs are physiologically plausible
- `CommonScenarios` — reusable clinical fragments (`RecordVitalSigns`, `LipidPanel`, `BloodGlucose`)

**Speaker notes:**
This is where the library earns its name. The fluent API reads like a clinical protocol. You're not constructing JSON — you're describing what happened to a patient. The 15% branch means 15% of your test population will have hyperlipidemia and be on a statin — matching real-world prevalence from epidemiological data.

---

## Slide 10 — Layer 3: Patient lifecycles

**Headline:** Layer 3: A full life, simulated.

**Body:**
- `PatientLifecycleGenerator` — age-based event scheduling from birth to today
- `DiseaseRiskCalculator` — evidence-based, age-adjusted onset probabilities
- `AdultWellnessSchedule` / `PediatricWellnessSchedule` — realistic visit frequency by age

Pre-built archetypes in `LifecycleExampleScenarios`:
- Healthy child (0–18 years)
- Typical adult (0–45 years)
- **Metabolic syndrome** (0–50 years): 40–45 encounters, 3–6 medications, ~75 immunizations
- Pediatric asthma (0–10 years)
- Elderly multi-morbidity (0–80 years)

**Speaker notes:**
Layer 3 simulates an entire patient life. The metabolic syndrome archetype generates a 50-year-old with the full clinical trajectory — childhood wellness visits, the onset of obesity in their 30s, diabetes diagnosis in their 40s, medication escalation, regular monitoring. 43 encounters, 75 immunizations spanning a full lifetime, 3 medications. This is the kind of data you need to test a chronic disease management feature end-to-end.

---

## Slide 11 — Layer 4: Population generation

**Headline:** Layer 4: Realistic populations at scale.

**Body:**
- `PopulationGenerator` — large-scale cohort creation
- `KnownCities` — 14 cities across 11 states (US + international), real Census demographics:
  - Age distribution sampled from city profile
  - Race distribution (Boston: 53% White, 25% Black, 19% Hispanic, 9% Asian)
  - Real zip codes and area codes
- `LocalBasedNameGenerator` — culturally appropriate names via Bogus locales
- Full medical history generated per patient from birth to current age

**Speaker notes:**
Layer 4 is for when you need a realistic cohort, not just a realistic patient. The demographics come from real US Census data. A Seattle cohort looks different from a Boston cohort — different age distribution, different race distribution, different disease prevalence because the risk calculator is demographically aware. This is what makes pipeline stress-testing meaningful.

---

## Slide 12 — Layer 1 in a unit test

**Headline:** Layer 1 in three lines.

**Body:**
```csharp
var schemaProvider = FhirVersion.R4.GetSchemaProvider();
var faker = new SchemaBasedFhirResourceFaker(schemaProvider);

var patient = faker.Generate("Patient");
// Valid R4 Patient — correct structure, valid coded values, realistic demographics
```

Compare: the old way was a 200-line JSON fixture that broke on every model change.

**Speaker notes:**
Three lines. No file. No subprocess. No JVM. And it passes FHIR validation — correct coded values, valid cardinality, realistic demographics. You can drop this into any existing xUnit or NUnit test suite in two minutes.

---

## Slide 13 — Layer 2: expressing clinical intent

**Headline:** Layer 2: This reads like a clinical protocol.

**Body:**
```csharp
var context = new ScenarioBuilder(schemaProvider)
    .WithPatient(p => p.WithAge(55).WithGender(g => g.Male))
    .AddEncounter("Annual wellness visit")
    .AddSubScenario(CommonScenarios.RecordVitalSigns())
    .AddSubScenario(CommonScenarios.LipidPanel())
    .AddProbabilisticBranch(
        probability: 0.15,
        trueState: new ConditionOnsetState { Code = FhirCode.Conditions.Hyperlipidemia },
        falseState: new DelayState { Exact = TimeSpan.Zero }
    )
    .Build();
```

**Speaker notes:**
Read this out loud and it sounds like a clinical scenario description. Fifty-five year old male. Annual wellness visit. Vitals recorded. Lipid panel. Fifteen percent chance of hyperlipidemia — we prescribe a statin, delay 90 days, schedule a follow-up. The VitalSignCorrelationEngine ensures the BMI and blood pressure values are physiologically consistent with the condition outcomes.

---

## Slide 14 — Layer 3 + 4: lifecycle and population

**Headline:** Layer 3 + 4: From one patient to a cohort.

**Body:**
```csharp
// Layer 3: full lifecycle archetype
var context = LifecycleExampleScenarios.GetMetabolicSyndromeLifecycle(schemaProvider);
Console.WriteLine($"Encounters:    {context.Encounters.Count}");    // 40–45
Console.WriteLine($"Conditions:    {context.Conditions.Count}");    // 2–4
Console.WriteLine($"Medications:   {context.Medications.Count}");   // 3–6
Console.WriteLine($"Immunizations: {context.Immunizations.Count}"); // ~75
Console.WriteLine($"Total:         {context.AllResources.Count}");  // 120+

// Layer 4: population cohort
var generator = new PopulationGenerator(schemaProvider);
var patients = generator.Generate("Massachusetts", 100);
// 100 patients: Boston census demographics, culturally appropriate names,
// real zip codes, full medical history per patient
```

**Speaker notes:**
The metabolic syndrome lifecycle generates a 50-year-old with the full clinical trajectory — 43 encounters, 75 immunizations across a lifetime, 3 active medications. Then Layer 4 scales that to a population. One line gives you 100 patients with demographically accurate distributions for Massachusetts. Name, age, race, zip code, medical history — all correlated.

---

## Slide 15 — Demo 1: CLI

**Headline:** Let's generate some patients.

**Subhead:** resource → scenario → population

**Body:**
- demo script: `talks/talk2-fhir-fakes/demo/demo1-cli-script.md`

**Speaker notes:**
[Switch to terminal.] Let's start simple — a single patient resource — and build up to a full population cohort. The CLI is great for pipeline testing, load generation, and ad-hoc exploration.

---

## Slide 16 — CLI reference

**Headline:** CLI quick reference.

**Body:**

| Command | Output |
|---|---|
| `ignixa-fakes r4 resource Patient` | Single Patient JSON |
| `ignixa-fakes r4 resource Observation BloodGlucose` | Observation with predefined state |
| `ignixa-fakes r4 scenario DiabeticPatient` | Bundle with full scenario |
| `ignixa-fakes r4 population --from Seattle --count 100` | 100-patient bundle |
| `ignixa-fakes r4 population --ndjson` | Per-resource NDJSON (for `$import`) |
| `ignixa-fakes help scenarios` | List all available scenarios |
| `ignixa-fakes help cities` | List cities + demographics |

All commands: `<version> <command> --out <folder> [options]`

**Speaker notes:**
[Leave up during/after Demo 1.] The version is always first. Then the command. `--out` is required and will be created if it doesn't exist. The `help` subcommands are genuinely useful — `help scenarios` lists everything available, `help cities` shows the demographics for each city.

---

## Slide 17 — Demo 2: Library

**Headline:** Now from C#.

**Subhead:** Layer 1 → Layer 2 → Layer 4

**Body:**
- demo project: `talks/talk2-fhir-fakes/demo/demo2-library/`

**Speaker notes:**
[Switch to IDE — open `demo2-library/Program.cs`.] Same capabilities, different entry point. Layer 1: generate a valid resource in three lines. Layer 2: express clinical intent with the fluent builder. Layer 4: generate a population cohort with one call. And we'll show determinism — same seed, same patient, every time.

---

## Slide 18 — Clinical realism is a design constraint

**Headline:** Clinical realism is a design constraint, not a feature.

**Body:**
Building `VitalSignCorrelationEngine` — vitals can't be independent random values:
- BMI must correlate with obesity-related conditions
- Blood glucose must correlate with diabetes diagnosis
- HbA1c > 6.5 if diabetic; systolic BP elevated with hypertension
- Lab values must be physiologically plausible together

The FHIR spec tells you the **schema**. Clinical medicine tells you the **constraints**.

**Speaker notes:**
This was the hardest part to get right. Random ≠ realistic. A patient with BMI of 18 and a type-2 diabetes diagnosis with normal fasting glucose is technically valid FHIR — but it's meaningless for testing. Any test that relies on clinical correlation would pass for the wrong reason. We had to model the physiological relationships explicitly. The VitalSignCorrelationEngine is the result of that work.

---

## Slide 19 — Determinism vs realism is a deliberate trade-off

**Headline:** Determinism vs realism — pick deliberately.

**Body:**
**Deterministic (unit tests):**
- Set `Randomizer.Seed = new Random(42)` before generating
- Same seed → same patient → stable test assertions
- Use for: unit tests, snapshot tests, debugging specific scenarios

**Realistic variance (load tests):**
- Don't set a seed — let Bogus run free
- Each run produces a different cohort with the same demographic distribution
- Use for: pipeline stress testing, performance benchmarks, population-scale validation

The 4-layer architecture makes this explicit — don't let it happen accidentally.

**Speaker notes:**
The worst outcome is accidentally using a non-deterministic generator in a unit test. Your CI passes, then flakes on the 47th run because a generated value happened to hit an edge case. We made the choice explicit: seed for determinism in unit tests, free variance for load tests. The API doesn't enforce this — but the 4-layer design makes you think about which mode you're in.

---

## Slide 20 — Try it.

**Headline:** Try it.

**Body:**
```bash
# Library
dotnet add package Ignixa.FhirFakes

# CLI
dotnet tool install -g Ignixa.FhirFakes.Cli

# Generate your first patient
ignixa-fakes r4 resource Patient --out ./output --from Seattle
```

- GitHub: https://github.com/brendankowitz/ignixa-fhir
- Docs: https://brendankowitz.github.io/ignixa-fhir/
- Inspired by Synthea — complements it, doesn't replace it
- MIT license — contributions welcome

**Speaker notes:**
That's it. One command gives you a valid FHIR Patient from Seattle. The GitHub repo has pre-built scenario examples, the docs walk through the layer architecture, and the NuGet package is ready to drop into any .NET 9 test project. Thank you.
