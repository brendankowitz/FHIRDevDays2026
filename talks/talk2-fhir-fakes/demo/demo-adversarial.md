# Layer 6 — Adversarial / Edge-Case Data

> **Shipped** in [PR #283](https://github.com/brendankowitz/ignixa-fhir/pull/283). This is the
> speaker's deep-dive for the Layer 6 beat. The live run is **Beat 5** in
> [`demo1-runme.md`](demo1-runme.md) (one-click) and [`demo1-cli-script.md`](demo1-cli-script.md)
> (narrated). The C# version is **Layer 6** in [`demo2-library/`](demo2-library/).

## The pitch (one line)

*"Every other layer makes data that behaves. This one makes data that **misbehaves** — on purpose — so you can find out what your pipeline does before your users do."*

## Why it lands — three real bugs

Building these very talks surfaced three bugs that an adversarial generator catches automatically:

- **Unicode names** rendered as `?` / threw `URI malformed` — the OEM-codepage console bug ([#280](https://github.com/brendankowitz/ignixa-fhir/pull/280)).
- **Every patient born on Jan 1** — the fixed-birthday bug ([#281](https://github.com/brendankowitz/ignixa-fhir/issues/281)).
- **Validator accepted impossible data** — empty-but-present strings and calendar dates like `2000-02-31`. **Found by this mode** (`--include-invalid`) and fixed in the same PR.

The third one is the payoff: the adversarial generator found a bug in *our own* validator.

## How it works

A seeded **decorator pipeline** runs *after* the realistic generators. It walks the typed
element tree (`resource.ToElement(schema)`), so targeting is schema-driven, not string-guessing:

- **Unicode/string** strategies hit free-text elements (`string`/`markdown`) — names, address lines, city.
- **Temporal** strategies hit `date`/`dateTime` elements — `birthDate`, etc.
- **Codes, URIs, and bound values are never touched** — `gender` stays a valid code.

Every perturbation is written to a `*.manifest.json` sidecar (category, path, before, after,
description) and is deterministic per `--seed`, so any hostile resource is fully replayable.

**Validity is intentional, and measured.** Validity-preserving strategies run by default;
`--include-invalid` opts into the `MayViolate` / `AlwaysInvalid` families. Pipe either into
`--validate` to see exactly what survives.

## Verified commands

Valid-but-hostile (default) — 12 mutations, still passes:

```bash
ignixa-fakes r4 resource Patient --out ./output --from Seattle --edge-cases --seed 7 --validate
```
```
  Edge cases: seed=7, mutations=12
    temporal.year-boundary: 1
    unicode.multi-script-long: 2
    unicode.cjk: 2
    unicode.rtl: 3
    string.injection-like: 1
    string.max-length: 1
    ...
✓ Validation passed
```

Intentionally invalid — the validator now rejects it (the bug we fixed):

```bash
ignixa-fakes r4 resource Patient --out ./output --edge-cases --include-invalid --seed 99 --validate
```
```
  Status: ✗ INVALID
❌ ERROR  @ Patient.name[0].given[0]
   type-1: value must not be empty for FHIR type 'string'
```

Target a single family, and look at the replay manifest:

```bash
ignixa-fakes r4 resource Patient --out ./output --edge-cases temporal --seed 3
cat ./output/*.manifest.json
```

## Strategy families

| Family | Examples | Default validity |
|---|---|---|
| `unicode` | cjk, rtl, combining, emoji, zero-width, multi-script-long | preserves |
| `temporal` | leap-year, year-boundary, far-past, far-future, partial-precision | preserves |
| `string` | max-length, injection-like | preserves |
| `string` | empty-present, whitespace-only, control-chars | `--include-invalid` only |

Selectors are families or categories: `--edge-cases unicode.rtl,temporal`.

## Companion axis — generation density

Separate from per-leaf mutation, `--density maximal` populates *every* optional element (the
"every-optional-present" cardinality edge case) for any resource type. `minimal` (default)
preserves today's required-only output.

```bash
ignixa-fakes r4 resource Patient --out ./output --density maximal --validate
```

## From C# (library)

```csharp
var builder = PatientBuilderFactory.Create(schemaProvider, seed: 42)
    .WithAge(45)
    .WithEdgeCases();              // seed derives from the base seed -> reproducible
var patient = builder.Build();
foreach (var m in builder.LastEdgeCaseManifest!.Mutations)
    Console.WriteLine($"{m.Category} @ {m.Path}: {m.Before} -> {m.After}");
```

## The narrative hook

It closes the loop with the **Conformance** layer (Layer 5, free via `--validate`):
Layer 5 proves your *good* data is correct; Layer 6 proves your *pipeline* survives *bad* data —
and the first pipeline it broke was ours.
