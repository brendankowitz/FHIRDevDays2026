# Regenerating the growth cohort

The data in `fhir-ndjson/` is **committed**, so the demo runs out of the box. This documents
how it was produced.

> **Requires `ignixa-fakes` 0.5.1+.** In 0.5.0, `population` generation emitted no Observations
> (see [ignixa-fhir#279](https://github.com/brendankowitz/ignixa-fhir/pull/279)). 0.5.1 adds
> age-appropriate vital signs, so population patients get realistic height/weight across their
> well-child and adult wellness visits — which is what makes a growth chart possible.
>
> The SQL on FHIR side (`ignixa-sqlonfhir`) flattens the committed NDJSON fine on 0.5.0+.

## 1. Generate a population

```powershell
ignixa-fakes r4 population --out $env:TEMP\gdemo --from Seattle --count 60 --ndjson
```

Each patient is simulated from birth, with height/weight recorded at every wellness visit
(pediatric: ages 1,2,4,6,8,10,12,14,16,18; adult: annually from 18).

## 2. Keep the growth-relevant resources

The committed dataset is the Patient file plus the **body-height (8302-2) and body-weight
(29463-7)** observations — the vitals the growth chart uses — to keep the repo lean:

```powershell
python keep-vitals.py $env:TEMP\gdemo ./fhir-ndjson
```

`keep-vitals.py` copies Patient.ndjson and writes an Observation.ndjson containing only the
height and weight observations.

## What's here

| Path | Contents |
|------|----------|
| `fhir-ndjson/` | 60 patients + their height/weight observations (committed) |
| `views/` | `patient-view`, `observation-view` (height/weight, with a `where` LOINC filter) |
| `growth-chart.sql` | DuckDB query: median height-for-age across the cohort |
| `output/` | Parquet produced by the SQL on FHIR CLI — gitignored |
