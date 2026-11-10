#!/usr/bin/env python3
"""Audit local DUELTRACE fusion candidates against a PS1 disc export.

No data is uploaded, and no candidate is automatically promoted to a rule.
The optional --out JSON contains aggregate counts and mismatched IDs only.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def recipes_of(obj: dict, *, dueltrace: bool):
    if dueltrace:
        rows = obj.get("candidates", obj.get("fusion_recipes",
                obj.get("recipes", obj.get("fusions", []))))
    else:
        rows = obj.get("fusions", [])
    if not isinstance(rows, list):
        raise ValueError("Expected a recipe list")
    parsed = {}
    for entry in rows:
        if not isinstance(entry, dict):
            raise ValueError("Malformed fusion recipe")
        a = entry.get("a", entry.get("materialA", entry.get("material_a")))
        b = entry.get("b", entry.get("materialB", entry.get("material_b")))
        result = entry.get("result", entry.get("result_id"))
        if any(type(item) is not int or item < 1 or item > 722
               for item in (a, b, result)):
            raise ValueError("Invalid fusion card IDs")
        key = tuple(sorted((a,b)))
        previous = parsed.get(key)
        if previous is not None and previous != result:
            raise ValueError(f"Conflicting results for {key}")
        parsed[key] = result
    return parsed


def compare(disc_export: dict, dueltrace_report: dict) -> dict:
    if disc_export.get("schema") != 1:
        raise ValueError("Unknown Reborn export schema")
    disc = recipes_of(disc_export, dueltrace=False)
    candidates = recipes_of(dueltrace_report, dueltrace=True)
    source_sha = str((disc_export.get("source") or {}).get("discSha256") or "")
    report_sha = str(dueltrace_report.get("rom_sha256") or
                     dueltrace_report.get("discSha256") or "")
    matching_sha = bool(source_sha and report_sha and
                        source_sha.lower() == report_sha.lower())
    both = set(disc) & set(candidates)
    mismatch = [
        {"a": a, "b": b, "discResult": disc[a,b],
         "dueltraceResult": candidates[a,b]}
        for a,b in sorted(both) if disc[a,b] != candidates[a,b]
    ]
    return {
        "schema": 1,
        "discRecipeCount": len(disc),
        "dueltraceCandidateCount": len(candidates),
        "commonPairs": len(both),
        "matchingPairs": len(both) - len(mismatch),
        "conflictingPairs": len(mismatch),
        "discOnlyPairs": len(set(disc)-set(candidates)),
        "dueltraceOnlyPairs": len(set(candidates)-set(disc)),
        "romSha256Matches": matching_sha,
        "sameDiscNotProven": not matching_sha,
        "conflicts": mismatch,
        "warning": ("No exact retail claim: the source may be Drop x15, "
                    "and DUELTRACE candidates may not be gameplay validated.")
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--export", required=True, type=Path)
    parser.add_argument("--dueltrace", required=True, type=Path)
    parser.add_argument("--out", type=Path)
    args = parser.parse_args()
    audit = compare(read_json(args.export), read_json(args.dueltrace))
    report = json.dumps(audit, ensure_ascii=False, indent=2)
    print(report)
    if args.out is not None:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(report + "\n", encoding="utf-8")
    return 0 if audit["conflictingPairs"] == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
