"""Keep only Patient + height/weight Observations from a FhirFakes population run.

Usage:
    python keep-vitals.py <population-ndjson-folder> <output-folder>

Copies Patient.ndjson and writes an Observation.ndjson containing only body-height (LOINC
8302-2) and body-weight (29463-7) observations — the growth-chart inputs — to keep the
committed demo dataset small. See regenerate.md.
"""
import json
import sys
import glob
import os

HEIGHT_WEIGHT = {"8302-2", "29463-7"}


def first(pattern, folder):
    matches = glob.glob(os.path.join(folder, pattern))
    if not matches:
        raise SystemExit(f"No file matching {pattern} in {folder}")
    return matches[0]


def main(in_dir: str, out_dir: str) -> None:
    os.makedirs(out_dir, exist_ok=True)

    patients = [json.loads(line) for line in open(first("*Patient*.ndjson", in_dir), encoding="utf-8")]
    with open(os.path.join(out_dir, "Patient.ndjson"), "w", encoding="utf-8", newline="\n") as fh:
        for p in patients:
            fh.write(json.dumps(p, ensure_ascii=False, separators=(",", ":")) + "\n")

    kept = 0
    obs_path = first("*Observation*.ndjson", in_dir)
    with open(os.path.join(out_dir, "Observation.ndjson"), "w", encoding="utf-8", newline="\n") as fh:
        for line in open(obs_path, encoding="utf-8"):
            o = json.loads(line)
            code = o.get("code", {}).get("coding", [{}])[0].get("code")
            if code in HEIGHT_WEIGHT:
                fh.write(json.dumps(o, ensure_ascii=False, separators=(",", ":")) + "\n")
                kept += 1

    print(f"Patients: {len(patients)}, height/weight observations: {kept}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)
    main(sys.argv[1], sys.argv[2])
