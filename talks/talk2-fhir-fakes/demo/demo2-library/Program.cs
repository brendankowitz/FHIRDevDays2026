using Bogus;
using Ignixa.FhirFakes;
using Ignixa.FhirFakes.Lifecycle;
using Ignixa.FhirFakes.Population;
using Ignixa.FhirFakes.Scenarios;
using Ignixa.Abstractions;
using Ignixa.Specification.Extensions;

// ── Shared schema provider ─────────────────────────────────────────────────
var schemaProvider = FhirVersion.R4.GetSchemaProvider();

// ─────────────────────────────────────────────────────────────────────────────
// Layer 1 — Seeded Determinism
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("=== Layer 1: Seeded Determinism ===");

static string GeneratePatientWithSeed(IFhirSchemaProvider schema, int seed)
{
    Randomizer.Seed = new Random(seed);
    var faker = new SchemaBasedFhirResourceFaker(schema);
    var patient = faker.Generate("Patient");
    return patient.Id;
}

var id1a = GeneratePatientWithSeed(schemaProvider, 42);
Console.WriteLine($"Run 1 (seed=42) Patient.id = {id1a}");

var id1b = GeneratePatientWithSeed(schemaProvider, 42);
Console.WriteLine($"Run 2 (seed=42) Patient.id = {id1b}");

Console.WriteLine($"Deterministic: {id1a == id1b}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// Layer 2 — ScenarioBuilder (patient journey composition)
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("=== Layer 2: ScenarioBuilder ===");

var scenarioContext = new ScenarioBuilder(schemaProvider)
    .WithPatient(p => p.WithAge(45).WithGender(g => g.Male))
    .AddEncounter(reason: "Annual wellness visit")
    .AddSubScenario(CommonScenarios.RecordVitalSigns(), "Record Vitals")
    .Build();

Console.WriteLine($"Encounters  : {scenarioContext.Encounters.Count}");
Console.WriteLine($"Observations: {scenarioContext.Observations.Count}");
Console.WriteLine($"Total resources: {scenarioContext.AllResources.Count}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// Layer 3 — Patient Lifecycle (metabolic syndrome example)
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("=== Layer 3: Patient Lifecycle ===");

var lifecycleContext = LifecycleExampleScenarios.GetMetabolicSyndromeLifecycle(schemaProvider);

Console.WriteLine($"Patient     : {lifecycleContext.Patient?.Id ?? "(none)"}");
Console.WriteLine($"Encounters  : {lifecycleContext.Encounters.Count}");
Console.WriteLine($"Conditions  : {lifecycleContext.Conditions.Count}");
Console.WriteLine($"Medications : {lifecycleContext.Medications.Count}");
Console.WriteLine($"Immunizations: {lifecycleContext.Immunizations.Count}");
Console.WriteLine($"Total resources: {lifecycleContext.AllResources.Count}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// Layer 4 — Population Generator
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("=== Layer 4: Population Generator ===");

var generator = new PopulationGenerator(schemaProvider);
var population = generator.Generate("Massachusetts", 5).ToList();

Console.WriteLine($"Generated   : {population.Count} patients");
if (population.Count > 0)
{
    var first = population[0];
    Console.WriteLine($"First patient id: {first.Patient?.Id ?? "(none)"}");
    Console.WriteLine($"First patient resources: {first.AllResources.Count}");
}
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("Talk 2 demo complete.");
