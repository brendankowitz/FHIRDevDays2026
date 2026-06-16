# Demo (STUB) — Layer 6: Adversarial / Edge-Case Data

> 🚧 **Stub for a future demo beat.** The library capability is in design — see the Ignixa
> investigation: `docs/features/fhir-faker/investigations/adversarial-data-generation.md`
> (branch `investigation/fhirfakes-adversarial-data`). This file holds the demo idea so the
> beat is ready to flesh out once the feature lands.

## The pitch (one line)

*"Every other layer makes data that behaves. This one makes data that **misbehaves** — on purpose — so you can find out what your pipeline does before your users do."*

## Why it lands

The two bugs we hit building these very talks are the proof:

- **Unicode names** rendered as `?` / threw `URI malformed` (the OEM-codepage bug, ignixa #280).
- **Every patient born on the same day** (the fixed-birthday bug, ignixa #281).

An adversarial generator would have surfaced both automatically.

## Planned beat (once built)

```sh
# Generate valid-but-hostile resources and pipe straight into the validator.
ignixa-fakes r4 population --from Seattle --count 50 --edge-cases --out ./output
ignixa-validator r4 --input ./output/...  --console      # watch what survives
```

Edge-case families to show: unicode/RTL names, leap-year & extreme dates, max-length strings,
all-optional-omitted vs extension-heavy resources.

## Narrative hook

It closes the loop with the **Conformance** layer (Layer 5, already free via `--validate`):
Layer 5 proves your *good* data is correct; Layer 6 proves your *pipeline* survives *bad* data.

## TODO before this becomes a real beat

- [ ] Land the adversarial generation mode (`--edge-cases`) in Ignixa.FhirFakes
- [ ] Decide the valid-but-hostile vs intentionally-invalid boundary (see investigation)
- [ ] Replace the commands above with verified output
- [ ] Add slide + speaker notes
