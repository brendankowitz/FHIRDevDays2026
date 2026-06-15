# Demo 1 — ignixa-fakes CLI (Runme)

> Presentation version for [runme.dev](https://runme.dev/) — each cell below is a one-click run on stage.
> For the full narrative, expected outputs, and beat timing see [`demo1-cli-script.md`](demo1-cli-script.md).
> **Prerequisite:** `ignixa-fakes` installed and on PATH.

---

## Beat 1 — Discover what's available

**Say:** "Let's see what the CLI can generate — scenarios and cities."

```sh {"name":"beat1-discover","cwd":"."}
ignixa-fakes help scenarios
ignixa-fakes help cities
```

---

## Beat 2 — Single patient resource

**Say:** "Start simple — one Patient resource, seeded from Seattle."

```sh {"name":"beat2-resource","cwd":"."}
ignixa-fakes r4 resource Patient --out ./output --from Seattle
```

*Look for `r4-patient-{id}.json` in `./output/`.*

---

## Beat 3 — Clinical scenario

**Say:** "Now a full clinical scenario — diabetic patient with complete context and resolved references."

```sh {"name":"beat3-scenario","cwd":"."}
ignixa-fakes r4 scenario DiabeticPatient --out ./output --resolved-references
```

*Look for `r4-bundle-DiabeticPatient-{id}.json` — Patient, Conditions, Medications, Encounters.*

---

## Beat 4 — Population as NDJSON

**Say:** "Scale it up — fifty patients as NDJSON, ready for `$import` or a SQL on FHIR pipeline."

```sh {"name":"beat4-population","cwd":"."}
ignixa-fakes r4 population --out ./output --from Seattle --count 50 --ndjson
```

*One NDJSON file per resource type in `./output/` — same data that drove the SQL on FHIR session.*
