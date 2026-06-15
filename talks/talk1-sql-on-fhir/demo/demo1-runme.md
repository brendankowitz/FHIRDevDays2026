# Demo 1 — SQL on FHIR CLI (runme.dev)

Presentation version — one-click cells for live stage execution.
Full narrative, beat-by-beat cues, and expected outputs live in [demo1-cli-script.md](demo1-cli-script.md).
**Prerequisites:** `ignixa-sqlonfhir` and `duckdb` installed and on PATH.

---

## Beat 1 — Preview schema

*Preview the output schema the ViewDefinition will produce — no input data required.*

```sh {"name":"beat1-preview","cwd":"data"}
ignixa-sqlonfhir r4 preview --views patient-view.json
```

---

## Beat 2 — Preview with sample rows

*Add `--input` to pull sample rows through the evaluator.*

```sh {"name":"beat2-preview-rows","cwd":"data"}
ignixa-sqlonfhir r4 preview --views patient-view.json --input patients.ndjson --rows 5
```

*5 rows from the synthetic Seattle patient cohort.*

---

## Beat 3 — Validate

*Validate catches FHIRPath errors before a pipeline run — no data needed.*

```sh {"name":"beat3-validate","cwd":"data"}
ignixa-sqlonfhir r4 validate --views patient-view.json
```

---

## Beat 4 — Run to Parquet

*Output format is inferred from the file extension — `.parquet` selects Parquet automatically.*

```sh {"name":"beat4-run-parquet","cwd":"data"}
ignixa-sqlonfhir r4 run \
  --views patient-view.json \
  --input patients.ndjson \
  --out patients.parquet
```

```sh {"name":"beat4-duckdb","cwd":"data"}
duckdb -c "SELECT * FROM 'patients.parquet' LIMIT 5;"
```

---

## Beat 5 — Batch mode

*Run multiple ViewDefinitions across multiple resource types in a single command.*

```sh {"name":"beat5-batch","cwd":"data"}
ignixa-sqlonfhir r4 run \
  --views views/ \
  --input fhir-ndjson/ \
  --out output/ \
  --format parquet \
  --stats-out output/stats.json
```

*3 ViewDefinitions → 3 Parquet files; 376 encounters flattened.*

---

## Beat 6 — The payoff: diabetic cohort registry

*ViewDefinitions flatten the FHIR data; the SQL query identifies diabetics above 7% HbA1c and trending the wrong way.*

```sh {"name":"beat6-flatten","cwd":"data/cohort"}
ignixa-sqlonfhir r4 run --views views/ --input fhir-ndjson/ --out output/ --format parquet
```

```sh {"name":"beat6-query","cwd":"data/cohort"}
duckdb -c ".read diabetic-cohort.sql"
```

*12 diabetics above target, trend column shows who needs outreach.*

---

**Alternative payoff:** see [demo-growth-chart.md](demo-growth-chart.md) for the population growth curve demo (same pipeline, different clinical question).
