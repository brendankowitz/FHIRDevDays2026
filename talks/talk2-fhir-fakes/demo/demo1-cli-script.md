# Demo 1 — CLI Script

**Time budget:** 3 minutes
**Output directory:** `./output/` (create fresh before talk — must be empty or non-existent)
**Pre-demo:** Confirm `ignixa-fakes --version` prints a version number.
**Terminal font:** 24pt minimum. High-contrast theme.

---

## Beat 1: Discover what's available (~30s)

**Say:** "Let's start by seeing what the CLI can do."

```bash
ignixa-fakes help scenarios
ignixa-fakes help cities
```

**Expected output (scenarios, partial):**
```
Available scenarios:
  DiabeticPatient         Type 2 diabetes with medication escalation
  WellnessVisit           Routine wellness visit with observations
  UrinaryTractInfection   UTI diagnosis and treatment
  AsthmaticChild          Pediatric asthma management
  ...
```

**Expected output (cities, partial):**
```
Available Cities for Population Generation:

Found 14 cities across 11 states:

  Washington:
    - Seattle (population: 737,015)
  Massachusetts:
    - Boston (population: 675,647)
  ...including Sydney, Melbourne, Amsterdam
```

**Say:** "14 cities across 11 states — including some international ones, which is a fun story. Real census demographics. Let's generate some patients."

---

## Beat 2: Single patient resource (~30s)

**Say:** "Start simple. One Patient from Seattle."

```bash
ignixa-fakes r4 resource Patient --out ./output --from Seattle
```

**Expected:** A `r4-patient-{id}.json` file in `./output/`. Open it.

**Say:** "Valid R4 Patient. Seattle zip code, area code, culturally appropriate name — all from census data. Generated in milliseconds."

---

## Beat 3: Clinical scenario (~45s)

**Say:** "Now a full clinical scenario — a diabetic patient with complete context."

```bash
ignixa-fakes r4 scenario DiabeticPatient --out ./output --resolved-references
```

**Expected:** A `r4-bundle-DiabeticPatient-{id}.json` containing Patient + Conditions + Medications + Encounters. Open it and walk through the bundle entries.

**Say:** "One command. A complete clinical picture. Patient, conditions, medications, encounters — all referenced correctly. `--resolved-references` gives you a batch bundle you can POST directly to a FHIR server."

---

## Beat 4: Population as NDJSON (~45s)

**Say:** "Now scale it. A population ready for $import or a SQL on FHIR pipeline."

```bash
ignixa-fakes r4 population --out ./output --from Seattle --count 50 --ndjson
```

**Expected:** Multiple NDJSON files in `./output/`: Patient, Condition, Encounter, Immunization, MedicationRequest — one file per resource type.

**Say:** "Fifty patients. Separate NDJSON files per resource type — exactly the format that $import expects. And if you need more: just change the count. The patients you saw in the SQL on FHIR session? This is how they were generated."

---

## Fallback plan

If the CLI fails or output is unexpected:
1. Open a pre-generated output directory from a rehearsal run
2. Show the files and narrate what the commands would have produced
3. Proceed directly to Demo 2 — the library demo is independent

---

## Pre-demo checklist

- [ ] `ignixa-fakes --version` prints a version
- [ ] `./output/` directory is empty (or will be created fresh)
- [ ] Terminal font is at least 24pt
- [ ] `ignixa-fakes help scenarios` runs without error
