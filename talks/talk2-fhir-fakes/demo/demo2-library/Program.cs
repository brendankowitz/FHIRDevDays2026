using Ignixa.FhirFakes;
using Ignixa.FhirFakes.Builders;
using Ignixa.FhirFakes.EdgeCases;
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

// First-class seeding (PR #283): pass the seed straight to the faker — no global
// Randomizer.Seed static. Same seed => byte-identical JSON, excluding only the
// server-managed meta.lastUpdated wall-clock value.
static string GeneratePatientJson(IFhirSchemaProvider schema, int seed)
{
    var faker = new SchemaBasedFhirResourceFaker(schema, seed);
    var patient = faker.Generate("Patient");
    patient.MutableNode.Remove("meta"); // drop lastUpdated so the comparison is content-only
    return patient.MutableNode.ToJsonString();
}

var json1a = GeneratePatientJson(schemaProvider, 42);
var json1b = GeneratePatientJson(schemaProvider, 42);

Console.WriteLine($"Run 1 (seed=42): {json1a.Length} chars");
Console.WriteLine($"Run 2 (seed=42): {json1b.Length} chars");
Console.WriteLine($"Byte-identical (excl. meta.lastUpdated): {json1a == json1b}");
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
// Layer 5 — Extensibility: compose your OWN scenario
// ─────────────────────────────────────────────────────────────────────────────
// The built-in CommonScenarios.* helpers are just Func<ScenarioBuilder, ScenarioBuilder> —
// so anything you write is a first-class scenario too. Plug it in with AddSubScenario.
Console.WriteLine("=== Layer 5: Extensibility (bring your own scenario) ===");

var customContext = new ScenarioBuilder(schemaProvider)
    .WithPatient(p => p.WithAge(58).WithGender(g => g.Female))
    .AddSubScenario(AnnualDiabeticReview(), "My custom scenario")
    .Build();

Console.WriteLine($"Scenario    : AnnualDiabeticReview (user-defined)");
Console.WriteLine($"Encounters  : {customContext.Encounters.Count}");
Console.WriteLine($"Observations: {customContext.Observations.Count}");
Console.WriteLine($"Total resources: {customContext.AllResources.Count}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// Layer 6 — Adversarial / edge-case data (valid-but-hostile)
// ─────────────────────────────────────────────────────────────────────────────
// Same realistic generators, then a seeded decorator perturbs free-text and dates:
// unicode/RTL names, leap-year & boundary dates, max-length & injection-like strings.
// Validity-preserving by default; every mutation is recorded for replay. The CLI's
// --include-invalid goes one step further and emits intentionally-invalid values —
// that mode is how we found a real gap in our own validator (empty-string primitives).
Console.WriteLine("=== Layer 6: Adversarial / edge-case data ===");

var edgeBuilder = PatientBuilderFactory.Create(schemaProvider, seed: 42)
    .WithAge(45)
    .WithEdgeCases(); // edge-case seed derives from the base seed -> end-to-end reproducible
var hostilePatient = edgeBuilder.Build();
var manifest = edgeBuilder.LastEdgeCaseManifest;

Console.WriteLine($"Patient id  : {hostilePatient.Id}");
Console.WriteLine($"Mutations   : {manifest?.Mutations.Count ?? 0}");
foreach (var m in manifest?.Mutations ?? Enumerable.Empty<MutationRecord>())
{
    Console.WriteLine($"  {m.Category,-26} @ {m.Path}");
    Console.WriteLine($"      {m.Before}  ->  {m.After}");
}
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("Talk 2 demo complete.");

// A reusable, user-defined scenario — exactly the shape of the built-in CommonScenarios.* helpers.
static Func<ScenarioBuilder, ScenarioBuilder> AnnualDiabeticReview() => sb => sb
    .AddEncounter(reason: "Annual diabetic review")
    .AddSubScenario(CommonScenarios.RecordVitalSigns(), "Vitals");
