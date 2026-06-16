# Demo 1 — SQL on FHIR CLI (runme.dev)

Presentation version — one-click cells for live stage execution.
**Prerequisites:** `ignixa-sqlonfhir` and `duckdb` installed and on PATH.
Run these cells from the `demo/` folder (runme's default — it's where this file lives). Paths are relative to that folder; `"interactive":false` renders output inline.

---

## Beat 1 — Preview schema

*Preview the output schema the ViewDefinition will produce — no input data required.*

```sh {"name":"beat1-preview","interactive":false}
ignixa-sqlonfhir r4 preview --views ./data/patient-view.json
```

---

## Beat 2 — Preview with sample rows

*Add `--input` to pull sample rows through the evaluator.*

```sh {"name":"beat2-preview-rows","interactive":false}
ignixa-sqlonfhir r4 preview --views ./data/patient-view.json --input ./data/patients.ndjson --rows 5
```

*5 rows from the synthetic Seattle patient cohort.*

---

## Beat 3 — Validate

*Validate catches FHIRPath errors before a pipeline run — no data needed.*

```sh {"name":"beat3-validate","interactive":false}
ignixa-sqlonfhir r4 validate --views ./data/patient-view.json
```

---

## Beat 4 — Run to Parquet

*Output format is inferred from the file extension — `.parquet` selects Parquet automatically.*

```sh {"name":"beat4-run-parquet","interactive":false}
ignixa-sqlonfhir r4 run \
  --views ./data/patient-view.json \
  --input ./data/patients.ndjson \
  --out ./data/patients.parquet
```

```sh {"name":"beat4-duckdb","interactive":false}
duckdb -c "SELECT * FROM './data/patients.parquet' LIMIT 5;"
```

---

## Beat 5 — Batch mode

*Run multiple ViewDefinitions across multiple resource types in a single command.*

```sh {"name":"beat5-batch","interactive":false}
ignixa-sqlonfhir r4 run \
  --views ./data/views/ \
  --input ./data/fhir-ndjson/ \
  --out ./data/output/ \
  --format parquet \
  --stats-out ./data/output/stats.json
```

*3 ViewDefinitions → 3 Parquet files; 376 encounters flattened.*

---

## Beat 6 — The payoff: diabetic cohort registry

*ViewDefinitions flatten the FHIR data; the SQL query identifies diabetics above 7% HbA1c and trending the wrong way.*

Same convention as the other beats — run from `demo/`, `./data/…` paths, no `cd`. The flatten writes Parquet to `output/` and the shared `diabetic-cohort.sql` reads from `output/`, so both land in `demo/output/`.

```sh {"name":"beat6-flatten","interactive":false}
ignixa-sqlonfhir r4 run --views ./data/cohort/views/ --input ./data/cohort/fhir-ndjson/ --out output/ --format parquet
```

```sh {"name":"beat6-query","interactive":false}
duckdb -c ".read ./data/cohort/diabetic-cohort.sql"
```

*12 diabetics above target, trend column shows who needs outreach.*

---

## Beat 7 — One view, any lab: runtime variables

*The cohort Observation view filters on a `%hba1c_code` constant (default `4548-4`). Pass `--var` to repoint the **same** ViewDefinition at a different lab — no file edit.*

```sh {"name":"beat7-default","interactive":false}
ignixa-sqlonfhir r4 preview --views ./data/cohort/views/observation-view.json --input ./data/cohort/fhir-ndjson/Observation.ndjson --rows 3
```

*Default constant → HbA1c (LOINC `4548-4`), values in `%`.*

```sh {"name":"beat7-override","interactive":false}
ignixa-sqlonfhir r4 preview --views ./data/cohort/views/observation-view.json --input ./data/cohort/fhir-ndjson/Observation.ndjson --rows 3 --var hba1c_code=2339-0
```

*Same view, `--var hba1c_code=2339-0` → glucose (LOINC `2339-0`), values in `mg/dL`. One ViewDefinition, any lab, decided at runtime.*

---

**Alternative payoff:** see [demo-growth-chart.md](demo-growth-chart.md) for the population growth curve demo (same pipeline, different clinical question).
