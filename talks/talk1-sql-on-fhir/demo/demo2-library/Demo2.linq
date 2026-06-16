<Query Kind="Statements">
  <NuGetReference Version="0.5.0">Ignixa.SqlOnFhir</NuGetReference>
  <NuGetReference Version="0.5.0">Ignixa.Specification</NuGetReference>
  <Namespace>Ignixa.Abstractions</Namespace>
  <Namespace>Ignixa.Serialization</Namespace>
  <Namespace>Ignixa.SqlOnFhir.Evaluation</Namespace>
  <Namespace>Ignixa.Specification.Extensions</Namespace>
</Query>

// SQL on FHIR library demo — LINQPad version of demo2-library/Program.cs.
// Load a ViewDefinition + NDJSON patients, evaluate the view, .Dump() the flat rows.

var schema = FhirVersion.R4.GetSchemaProvider();

// Locate the demo data relative to this .linq file (no copy-to-output needed).
var dataDir = Path.GetFullPath(
    Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, "..", "data"));

// ── Load ViewDefinition ────────────────────────────────────────────────────
var viewDefNode = JsonSourceNodeFactory
    .Parse(File.ReadAllText(Path.Combine(dataDir, "patient-view.json")))
    .ToSourceNavigator();

// ── Evaluate every patient in the NDJSON against the view ──────────────────
var evaluator = new SqlOnFhirEvaluator();

var rows =
    (from line in File.ReadLines(Path.Combine(dataDir, "patients.ndjson"))
     where !string.IsNullOrWhiteSpace(line)
     let resource = JsonSourceNodeFactory.Parse(line).ToElement(schema)
     from row in evaluator.Evaluate(viewDefNode, resource)
     select new
     {
         Id = row["id"],
         Family = row["family_name"],
         Given = row["given_name"],
         Born = row["birth_date"],
         Gender = row["gender"],
     })
    .ToList();

// In the LINQPad GUI this renders as an interactive grid; under LPRun it prints as text.
rows.Dump($"patient-view.json → {rows.Count} patients evaluated");
