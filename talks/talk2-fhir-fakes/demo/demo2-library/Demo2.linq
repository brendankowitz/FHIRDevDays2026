<Query Kind="Program">
  <NuGetReference Version="35.6.5">Bogus</NuGetReference>
  <NuGetReference Version="0.5.0">Ignixa.FhirFakes</NuGetReference>
  <NuGetReference Version="0.5.0">Ignixa.Specification</NuGetReference>
  <Namespace>Bogus</Namespace>
  <Namespace>Ignixa.Abstractions</Namespace>
  <Namespace>Ignixa.FhirFakes</Namespace>
  <Namespace>Ignixa.FhirFakes.Lifecycle</Namespace>
  <Namespace>Ignixa.FhirFakes.Population</Namespace>
  <Namespace>Ignixa.FhirFakes.Scenarios</Namespace>
  <Namespace>Ignixa.Specification.Extensions</Namespace>
</Query>

// LINQPad version of the Talk 2 library demo (mirror of Program.cs).
// Press F4 to confirm the NuGet refs, then hit ▶. Each layer .Dump()s its result.

void Main()
{
    // Shared schema provider
    var schemaProvider = FhirVersion.R4.GetSchemaProvider();

    // ── Layer 1 — Seeded Determinism ──────────────────────────────────────────
    var id1a = GeneratePatientWithSeed(schemaProvider, 42);
    var id1b = GeneratePatientWithSeed(schemaProvider, 42);
    new
    {
        Run1_seed42 = id1a,
        Run2_seed42 = id1b,
        Deterministic = id1a == id1b
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

    "Talk 2 demo complete.".Dump();
}

// Generate a Patient with a fixed Bogus seed so the id is reproducible across runs.
static string GeneratePatientWithSeed(IFhirSchemaProvider schema, int seed)
{
    Randomizer.Seed = new Random(seed);
    var faker = new SchemaBasedFhirResourceFaker(schema);
    var patient = faker.Generate("Patient");
    return patient.Id;
}

// A reusable, user-defined scenario — exactly the shape of the built-in CommonScenarios.* helpers.
static Func<ScenarioBuilder, ScenarioBuilder> AnnualDiabeticReview() => sb => sb
    .AddEncounter(reason: "Annual diabetic review")
    .AddSubScenario(CommonScenarios.RecordVitalSigns(), "Vitals");
