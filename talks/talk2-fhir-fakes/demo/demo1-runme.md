# Demo 1 — ignixa-fakes CLI (Runme)

> Presentation version for [runme.dev](https://runme.dev/) — each cell below is a one-click run on stage.
> For the full narrative, expected outputs, and beat timing see [`demo1-cli-script.md`](demo1-cli-script.md).
> **Prerequisite:** `ignixa-fakes` installed and on PATH.
> Run these cells from the `demo/` folder (runme's default — it's where this file lives); `--out ./output` is relative to it, and `"interactive":false` renders output inline.

---

## Beat 1 — Discover what's available

*Discover available scenarios and cities the CLI can generate.*

```sh {"name":"beat1-discover","interactive":false}
ignixa-fakes help scenarios
ignixa-fakes help cities
```

---

## Beat 2 — Single patient resource

*Generate a single Patient resource seeded from Seattle.*

```sh {"name":"beat2-resource","interactive":false}
ignixa-fakes r4 resource Patient --out ./output --from Seattle
```

*Look for `r4-patient-{id}.json` in `./output/`.*

---

## Beat 3 — Clinical scenario

*Generate a full DiabeticPatient scenario bundle with resolved references.*

```sh {"name":"beat3-scenario","interactive":false}
ignixa-fakes r4 scenario DiabeticPatient --out ./output --resolved-references
```

*Look for `r4-bundle-DiabeticPatient-{id}.json` — Patient, Conditions, Medications, Encounters, Observations.*

---

## Beat 4 — Population as NDJSON

*Scale up to fifty patients as NDJSON, ready for `$import` or a SQL on FHIR pipeline.*

```sh {"name":"beat4-population","interactive":false}
ignixa-fakes r4 population --out ./output --from Seattle --count 50 --ndjson
```

*One NDJSON file per resource type in `./output/` — same data that drove the SQL on FHIR session.*
