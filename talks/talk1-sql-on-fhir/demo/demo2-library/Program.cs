using Ignixa.Serialization;
using Ignixa.Specification.Extensions;
using Ignixa.Abstractions;
using Ignixa.SqlOnFhir.Evaluation;

// ── Setup ──────────────────────────────────────────────────────────────────
var schema = FhirVersion.R4.GetSchemaProvider();
var baseDir = AppContext.BaseDirectory;

// ── Load ViewDefinition ────────────────────────────────────────────────────
var viewDefNode = JsonSourceNodeFactory
    .Parse(File.ReadAllText(Path.Combine(baseDir, "patient-view.json")))
    .ToSourceNavigator();

// ── Evaluate ───────────────────────────────────────────────────────────────
var evaluator = new SqlOnFhirEvaluator();

Console.WriteLine($"{"ID",-38} {"Family",-14} {"Given",-14} {"Born",-12} {"Gender"}");
Console.WriteLine(new string('─', 90));

var rowCount = 0;
foreach (var line in File.ReadLines(Path.Combine(baseDir, "patients.ndjson")))
{
    if (string.IsNullOrWhiteSpace(line)) continue;

    var resource = JsonSourceNodeFactory
        .Parse(line)
        .ToElement(schema);

    foreach (var row in evaluator.Evaluate(viewDefNode, resource))
    {
        rowCount++;
        Console.WriteLine(
            $"{row["id"],-38} {row["family_name"],-14} {row["given_name"],-14} " +
            $"{row["birth_date"],-12} {row["gender"]}");
    }
}

Console.WriteLine(new string('─', 90));
Console.WriteLine($"{rowCount} patients evaluated");
