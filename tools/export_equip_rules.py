#!/usr/bin/env python3
"""Convert locally extracted PS1 equip targets into Unity rules. No disc or network.

Source: PrivateExtracts/reborn-disc/disc_tables.json (written by
import_fm_disc.py from the user's own disc with the upstream PS1 parser).
The 7 terrain copies are required to agree when evidence is available.
"""
from __future__ import annotations

import argparse
import json
import re
import shutil
import sys
from pathlib import Path

SHA = re.compile(r"^[0-9a-fA-F]{64}$")


def _sha(source):
    if not isinstance(source, dict):
        raise ValueError("Fonte original ausente")
    value = source.get("discSha256")
    if not isinstance(value, str) or not SHA.fullmatch(value):
        raise ValueError("SHA-256 de disco ausente ou invalido")
    edition = source.get("edition")
    if not isinstance(edition, str) or not (
        edition == "original-ps1" or edition.startswith("original-ps1-")
    ):
        raise ValueError("Perfil nao identificado como original-ps1")
    return value.lower()


def build(tables: dict, catalog: dict) -> tuple[dict, dict]:
    original_sha = _sha(tables.get("source"))
    catalog_sha = _sha(catalog.get("source"))
    if original_sha != catalog_sha:
        raise ValueError("Tabela Equip e catalogo nao compartilham a mesma ISO (SHA)")
    if tables.get("schema") != 1 or catalog.get("schema") != 1:
        raise ValueError("Schema de extracao invalido")

    cards = catalog.get("cards")
    if not isinstance(cards, list) or len(cards) != 722:
        raise ValueError("Necessario catalogo completo de 722 cartas")
    lookup: dict[int, dict] = {}
    for card in cards:
        if not isinstance(card, dict) or type(card.get("id")) is not int:
            raise ValueError("Carta invalida")
        cid = card["id"]
        if cid < 1 or cid > 722 or cid in lookup:
            raise ValueError("Carta duplicada ou fora do catalogo")
        lookup[cid] = card
    if set(lookup) != set(range(1, 723)):
        raise ValueError("Catalogo de cartas incompleto")

    notes = tables.get("parserNotes") or []
    if not isinstance(notes, list):
        raise ValueError("Notas estruturais invalidas")
    if any("terrain copy" in str(n).lower() and
           "equip table differs" in str(n).lower() for n in notes):
        raise ValueError("Tabelas Equip divergentes entre os sete terrenos")
    source = tables.get("equips")
    if not isinstance(source, dict) or not source:
        raise ValueError("Sem tabelas Equip extraidas do WA_MRG")
    rows = []
    seen = set()
    total_targets = 0
    for raw_id, raw_targets in source.items():
        try:
            equip_id = int(raw_id)
        except (ValueError, TypeError) as exc:
            raise ValueError("ID Equip nao numerico") from exc
        if equip_id < 1 or equip_id > 722 or equip_id in seen:
            raise ValueError("Equip com ID invalido ou repetido")
        seen.add(equip_id)
        if str(lookup[equip_id].get("kind")).lower() != "equip":
            raise ValueError(f"Carta #{equip_id} nao e Equip no mesmo disco")
        if not isinstance(raw_targets, list) or not raw_targets:
            raise ValueError("Equip sem compatibilidades verificaveis: #" + str(equip_id))
        if len(raw_targets) > 722:
            raise ValueError("Equip com mais de 722 alvos")
        targets = []
        unique = set()
        for raw_target in raw_targets:
            if type(raw_target) is not int or not 1 <= raw_target <= 722 or                     raw_target in unique or                     str(lookup[raw_target].get("kind")).lower() != "monster":
                raise ValueError("Alvo incompatível, duplicado ou fora do catalogo no Equip #" +
                                 str(equip_id))
            unique.add(raw_target)
            targets.append(raw_target)
        if equip_id in (657, 668):
            # These two universal rules are already included in the engine.
            # Do not try to re-register them in the private manifest.
            continue
        rows.append({
            "cardId": equip_id, "verified": True,
            "attackBonus": 500, "defenseBonus": 500,
            "universal": False, "targets": sorted(targets),
        })
        total_targets += len(targets)
    if not rows:
        raise ValueError("Nenhum Equip especifico valido foi encontrado")

    rows.sort(key=lambda row: row["cardId"])
    expected = sorted(c["id"] for c in cards if
                      str(c.get("kind")).lower() == "equip")
    known = {r["cardId"] for r in rows} | {657, 668}
    uncovered = sorted(cid for cid in expected if cid not in known)
    export = {
        "schema": 1,
        "source": {
            "edition": tables["source"]["edition"],
            "discSha256": original_sha
        },
        "equips": rows,
    }
    report = {
        "sourceSha256": original_sha,
        "retailUniversalRules": [657, 668],
        "specificEquipsExported": len(rows),
        "totalExplicitTargets": total_targets,
        "totalEquipsInCatalog": len(expected),
        "equipsNotCovered": uncovered,
        "structuralValidation": "same-SHA-disc-tables-with-no-seven-bank-conflict",
        "individualDuelTesting": False,
        "warning": (
            "Dados estruturais extraidos da ISO original; pairings nao foram "
            "confirmados individualmente em partidas reais."
        ),
    }
    return export, report


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--project", type=Path,
                    default=Path(__file__).resolve().parents[1])
    args = ap.parse_args()
    project = args.project.expanduser().resolve()
    private = project / "PrivateExtracts" / "reborn-disc"
    tables_path = private / "disc_tables.json"
    catalog_path = project / "Assets" / "StreamingAssets" / "RebornContent" / "original_export.json"
    output = catalog_path.with_name("equip_rules.json")
    try:
        tables = json.loads(tables_path.read_text(encoding="utf-8"))
        catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
        result, report = build(tables, catalog)
        private.mkdir(parents=True, exist_ok=True)
        output.parent.mkdir(parents=True, exist_ok=True)
        if output.is_file():
            shutil.copy2(output, private / "equip_rules.before_import.json")
        tmp = output.with_suffix(".json.tmp")
        tmp.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n",
                       encoding="utf-8")
        tmp.replace(output)
        (private / "equip-import-report.json").write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8")
        print(json.dumps(report, ensure_ascii=False, indent=2))
        print("IMPORTADO: " + str(output))
        return 0
    except (ValueError, KeyError, OSError, TypeError, json.JSONDecodeError) as exc:
        print("EQUIP ORIGINAL: " + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
