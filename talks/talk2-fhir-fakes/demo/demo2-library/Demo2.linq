<Query Kind="Program">
  <NuGetReference Version="0.5.6">Ignixa.FhirFakes</NuGetReference>
  <NuGetReference Version="0.5.6">Ignixa.Specification</NuGetReference>
  <Namespace>Ignixa.Abstractions</Namespace>
  <Namespace>Ignixa.FhirFakes</Namespace>
  <Namespace>Ignixa.FhirFakes.Builders</Namespace>
  <Namespace>Ignixa.FhirFakes.EdgeCases</Namespace>
  <Namespace>Ignixa.FhirFakes.Lifecycle</Namespace>
  <Namespace>Ignixa.FhirFakes.Population</Namespace>
  <Namespace>Ignixa.FhirFakes.Scenarios</Namespace>
  <Namespace>Ignixa.Specification.Extensions</Namespace>
</Query>

// LINQPad version of the Talk 2 library demo (mirror of Program.cs).
// Requires Ignixa 0.5.6+ (the edge-case / seeding API).
// Press F4 to confirm the NuGet refs, then hit ▶. Each layer .Dump()s its result.

void Main()
{
    // Shared schema provider
    var schemaProvider = FhirVersion.R4.GetSchemaProvider();

    // ── Layer 1 — Seeded Determinism ──────────────────────────────────────────
    // First-class seeding (PR #283): seed goes straight to the faker — no global
    // Randomizer.Seed static. Same seed => byte-identical JSON (excl. meta.lastUpdated).
    var json1a = GeneratePatientJson(schemaProvider, 42);
    var json1b = GeneratePatientJson(schemaProvider, 42);
    new
    {
        Run1_chars = json1a.Length,
        Run2_chars = json1b.Length,
        ByteIdentical_exclMeta = json1a == json1b
    }.Dump("Layer 1 — Seeded Determinism");

    // ── Layer 2 — ScenarioBuilder (patient journey composition) ───────────────
    var scenarioContext = new ScenarioBuilder(schemaProvider)
        .WithPatient(p => p.WithAge(45).WithGender(g => g.Male))
        .AddEncounter(reason: "Annual wellness visit")
        .AddSubScenario(CommonScenarios.RecordVitalSigns(), "Record Vitals")
        .Build();
    new
    {
        Encounters = scenarioContext.Encounters.Count,
        Observations = scenarioContext.Observations.Count,
        TotalResources = scenarioContext.AllResources.Count
    }.Dump("Layer 2 — ScenarioBuilder");

    // ── Layer 3 — Patient Lifecycle (metabolic syndrome example) ──────────────
    var lifecycleContext = LifecycleExampleScenarios.GetMetabolicSyndromeLifecycle(schemaProvider);
    new
    {
        Patient = lifecycleContext.Patient?.Id ?? "(none)",
        Encounters = lifecycleContext.Encounters.Count,
        Conditions = lifecycleContext.Conditions.Count,
        Medications = lifecycleContext.Medications.Count,
        Immunizations = lifecycleContext.Immunizations.Count,
        TotalResources = lifecycleContext.AllResources.Count
    }.Dump("Layer 3 — Patient Lifecycle");

    // The real LINQPad payoff: explore a generated FHIR resource as a live object graph.
    lifecycleContext.Patient.Dump("Layer 3 — generated Patient resource (expand me)");

    // ── Layer 4 — Population Generator ────────────────────────────────────────
    var generator = new PopulationGenerator(schemaProvider);
    var population = generator.Generate("Massachusetts", 5).ToList();
    var first = population.FirstOrDefault();
    new
    {
        Generated = population.Count,
        FirstPatientId = first?.Patient?.Id ?? "(none)",
        FirstPatientResources = first?.AllResources.Count ?? 0
    }.Dump("Layer 4 — Population Generator");

    // ── Layer 5 — Extensibility: compose your OWN scenario ────────────────────
    // The built-in CommonScenarios.* helpers are just Func<ScenarioBuilder, ScenarioBuilder> —
    // so anything you write is a first-class scenario too. Plug it in with AddSubScenario.
    var customContext = new ScenarioBuilder(schemaProvider)
        .WithPatient(p => p.WithAge(58).WithGender(g => g.Female))
        .AddSubScenario(AnnualDiabeticReview(), "My custom scenario")
        .Build();
    new
    {
        Scenario = "AnnualDiabeticReview (user-defined)",
        Encounters = customContext.Encounters.Count,
        Observations = customContext.Observations.Count,
        TotalResources = customContext.AllResources.Count
    }.Dump("Layer 5 — Extensibility (bring your own scenario)");

    // ── Layer 6 — Adversarial / edge-case data (valid-but-hostile) ────────────
    // Same realistic generators, then a seeded decorator perturbs free-text and dates:
    // unicode/RTL names, leap-year & boundary dates, max-length & injection-like strings.
    // Validity-preserving by default; every mutation is recorded for replay. The CLI's
    // --include-invalid emits intentionally-invalid values — how we found a real gap in
    // our own validator (empty-string primitives).
    var edgeBuilder = PatientBuilderFactory.Create(schemaProvider, seed: 42)
        .WithAge(45)
        .WithEdgeCases(); // edge-case seed derives from the base seed -> reproducible
    var hostilePatient = edgeBuilder.Build();
    edgeBuilder.LastEdgeCaseManifest?.Mutations
        .Select(m => new { m.Category, m.Path, m.Before, m.After })
        .Dump("Layer 6 — Adversarial / edge-case mutations (replayable manifest)");

    "Talk 2 demo complete.".Dump();
}

// Generate a Patient with a seeded faker and return its content JSON (meta stripped),
// so two same-seed runs can be compared byte-for-byte.
static string GeneratePatientJson(IFhirSchemaProvider schema, int seed)
{
    var faker = new SchemaBasedFhirResourceFaker(schema, seed);
    var patient = faker.Generate("Patient");
    patient.MutableNode.Remove("meta");
    return patient.MutableNode.ToJsonString();
}

// A reusable, user-defined scenario — exactly the shape of the built-in CommonScenarios.* helpers.
static Func<ScenarioBuilder, ScenarioBuilder> AnnualDiabeticReview() => sb => sb
    .AddEncounter(reason: "Annual diabetic review")
    .AddSubScenario(CommonScenarios.RecordVitalSigns(), "Vitals");
