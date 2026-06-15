# DevDays 2026 — Pre-Talk Validation TODOs

> Re-validated 2026-06-15: v0.5.0 released. Both CLIs updated, .NET 10.0.9 runtime installed,
> all demo beats run end-to-end, both library demos build and run against NuGet 0.5.0.
> All slides.md / speaker-notes.md / demo scripts updated to match actual output.

---

## ✅ Resolved by v0.5.0 (no action needed)

- **CLI release** — v0.5.0 published. `run`, `preview`, `validate`, `--views`, `--format`, `--stats-out`, `--var` all work. Both tools updated: `dotnet tool update -g Ignixa.SqlOnFhir.Cli` / `Ignixa.FhirFakes.Cli`.
- **ParquetFileWriter bug** (#269/#270) — fixed. Beat 5 produces 3 Parquet files, 376 encounter rows, 0 failures.
- **Conformance suite** (#271) — now 100%. All tests pass. Slides 6 & 18 updated accordingly.
- **Talk 1 demo csproj floating `Version="*"`** — pinned to `0.5.0`.
- **Talk 2 demo local project refs** — switched to NuGet `Ignixa.FhirFakes 0.5.0` / `Ignixa.Specification 0.5.0`.
- **Talk 1 speaker-notes S7** — "2 hours" → "over 4 hours" fixed.
- **Talk 1 speaker-notes S17** — removed "runtime variable override" (demo doesn't show it).
- **Talk 2 slide 11** — "11 major US cities" → "14 cities across 11 states (including international)"; `EthnicNameGenerator` → `LocalBasedNameGenerator`.
- **Talk 2 CLI demo script** — Seattle population corrected to 737,015; "Eleven cities" → "14 cities"; filename patterns fixed to `r4-patient-{id}.json` / `r4-bundle-{scenario}-{id}.json`.
- **Talk 2 slides/speaker-notes 10/14** — metabolic syndrome numbers corrected to match actual output: 43 encounters, ~75 immunizations, 3 medications, 120+ total. Removed false "150+ vitals/labs" claim.
- **.NET 10.0.9 runtime** — installed. Both CLIs (targeting .NET 10) work.
- **Benchmark table** (Talk 1 slide 7) — numbers verified against `bench/results/`. Stored results match the slides exactly: 345/484/675/1508 ns, 70 µs / 47 KB. No change needed.

---

## ⚠️ Still needs manual action before talks

- [ ] **PPTX files not updated** — slides.md and speaker-notes.md are corrected, but the `.pptx` files still have the old text. Apply these changes to the PPTX before presenting:
  - Talk 1 slide 6: Replace "two documented edge-case skips" with "Passes **100%** of official tests"
  - Talk 1 slide 18: Update two-skips story → now resolved, 100% pass in v0.5.0
  - Talk 2 slide 10: "150+ vitals/labs" → "~75 immunizations"
  - Talk 2 slide 11: "11 major US cities" → "14 cities across 11 states (including international)"; `EthnicNameGenerator` → `LocalBasedNameGenerator`
  - Talk 2 slide 14: Comments `// 15–20` → `// ~75` and `// 100+` → `// 120+`
- [ ] **Talk 1 PPTX slide 16 cross-promo**: "Fluent, Fast, and Fake — tomorrow (Jun 17), 9:15 AM · Ski-U-Mah" — confirm room/time against the published agenda.
- [ ] **Install DuckDB** on the presentation machine (Beat 4's "open it" step), or plan to skip per the script's fallback note.
- [ ] **Clear output directories** before each talk: `talks/talk1-sql-on-fhir/demo/data/output/` and `talks/talk2-fhir-fakes/demo/output/`.
- [ ] **Terminal encoding** — Talk 1 Beat 2 preview shows Chinese/Japanese names correctly in the library demo output but may display as `?` in certain Windows terminals. Set `chcp 65001` or use Windows Terminal before the demo.

---

## Validated clean (no action)

- All Talk 1 CLI beats pass end-to-end: Beat 1 ✓ Beat 2 ✓ Beat 3 ✓ Beat 4 ✓ (2,470 bytes Parquet) Beat 5 ✓ (376 rows, 3 files, Parquet writer fixed).
- Talk 1 library demo: 20 patients evaluated, correct column output.
- Talk 2 CLI beats: Beat 1 ✓ Beat 2 ✓ Beat 3 ✓ Beat 4 ✓ (NDJSON, 5 resource types).
- Talk 2 library demo: Layer 1 determinism ✓ (same ID both runs), Layer 2 ScenarioBuilder ✓, Layer 3 lifecycle ✓, Layer 4 population ✓.
- Both library demos build and run against published NuGet 0.5.0 (no local project refs).
- Data files: 20 patients, 3 views, 3 NDJSON files, exactly 376 encounters.
- Boston 53/25/19/9 demographics match source exactly (sums >100% because Hispanic overlaps race — census methodology; be ready for the question).
- All scenario names in the Talk 2 demo script exist in v0.5.0.
