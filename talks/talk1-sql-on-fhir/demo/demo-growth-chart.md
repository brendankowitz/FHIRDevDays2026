# Demo (alternative payoff) — Growth chart from FHIR

> **Alternative to Beat 6's diabetic registry.** Same idea — flatten FHIR with SQL on FHIR,
> then ask a real clinical question — but the answer is a **population growth curve**.
>
> Runnable with the [runme.dev](https://runme.dev/) VS Code extension: each `sh` cell below is
> a one-click run. `cwd` is set per cell to the data folder.
>
> **Data requirement:** the committed cohort was generated with `ignixa-fakes` **0.5.1+**
> (0.5.0 population produced no observations). The flatten step works on `ignixa-sqlonfhir` 0.5.0+.

---

## Beat A: Flatten patients + height/weight observations (~30s)

*Flatten 60 synthetic patients and their height/weight observations into Parquet.*

```sh {"name":"growth-flatten","cwd":"data/growth"}
ignixa-sqlonfhir r4 run --views views/ --input fhir-ndjson/ --out output/ --format parquet
```

**Expected output:**
```
✓ Found 2 ViewDefinition(s)  [R4]

  [1/2] observation-view                             2,888 rows  observation-view.parquet (0.1 MB)
  [2/2] patient-view                                    60 rows  patient-view.parquet (0.0 MB)

✓ Done: 2 completed, 0 skipped, 0 failed
```

*The observation view's WHERE clause pre-filtered to body height and weight only.*

---

## Beat B: The growth curve (~45s)

*Join height to patients, compute age at each measurement, and derive the median growth curve.*

```sh {"name":"growth-query","cwd":"data/growth"}
duckdb -c ".read growth-chart.sql"
```

**Expected output:**
```
┌───────────┬──────────────┬──────────────────┬─────────────┬────────────┐
│ age_years │ measurements │ median_height_cm │ shortest_cm │ tallest_cm │
├───────────┼──────────────┼──────────────────┼─────────────┼────────────┤
│         1 │            6 │             75.2 │        72.4 │       77.2 │
│         2 │            5 │             86.6 │        86.0 │       89.9 │
│         4 │            4 │            101.7 │       100.6 │      102.9 │
│         6 │            4 │            113.9 │       113.3 │      118.0 │
│         8 │            3 │            129.8 │       124.8 │      129.9 │
│        10 │            3 │            138.3 │       135.8 │      138.9 │
│        12 │            3 │            150.8 │       147.5 │      152.7 │
│        14 │            1 │            162.6 │       162.6 │      162.6 │
│        18 │           54 │            171.9 │       158.7 │      180.4 │
└───────────┴──────────────┴──────────────────┴─────────────┴────────────┘
```

*Median height climbs from 75cm at age one to adult height — a textbook growth curve from FHIR, one ViewDefinition, one SQL query.*

---

## Pre-demo checklist

- [ ] `duckdb` on PATH
- [ ] `data/growth/fhir-ndjson/` has Patient.ndjson + Observation.ndjson
- [ ] `data/growth/output/` is empty (cleared before the talk)
- [ ] Terminal font ≥ 24pt
