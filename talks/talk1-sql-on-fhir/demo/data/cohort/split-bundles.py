"""Split FhirFakes scenario bundles into per-resource-type NDJSON.

Usage:
    python split-bundles.py <input-folder-of-bundles> <output-folder>

Reads every *.json FHIR Bundle in the input folder and writes one NDJSON file per
resource type (Patient.ndjson, Condition.ndjson, Observation.ndjson, ...) to the output
folder. Used to turn `ignixa-fakes scenario` bundle output into the batch input that the
SQL on FHIR CLI expects. See regenerate.md.
"""
import json
import sys
import glob
import os
from collections import defaultdict

RESOURCE_TYPES = ["Patient", "Condition", "Observation", "Encounter", "MedicationRequest"]


def main(in_dir: str, out_dir: str) -> None:
    os.makedirs(out_dir, exist_ok=True)
    buckets = defaultdict(list)
    for path in sorted(glob.glob(os.path.join(in_dir, "*.json"))):
        bundle = json.load(open(path, encoding="utf-8"))
        for entry in bundle.get("entry", []):
            resource = entry["resource"]
            buckets[resource["resourceType"]].append(resource)

    for rt in RESOURCE_TYPES:
        resources = buckets.get(rt, [])
        out_path = os.path.join(out_dir, f"{rt}.ndjson")
        with open(out_path, "w", encoding="utf-8", newline="\n") as fh:
            for r in resources:
                fh.write(json.dumps(r, ensure_ascii=False, separators=(",", ":")) + "\n")
        print(f"{rt}.ndjson: {len(resources)} resources")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)
    main(sys.argv[1], sys.argv[2])
