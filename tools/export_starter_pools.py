#!/usr/bin/env python3
"""Convert already extracted private PS1 tables into local Unity starter JSON.

No ROM reread, no network, no fake weights. Source SHA must match cards.
"""
from __future__ import annotations
import argparse
import json
import re
import sys
from pathlib import Path
from import_fm_disc import normalize_starter_weights

DRAWS = [16, 16, 4, 1, 1, 1, 1]


def build(tables, catalog):
    source = tables.get("source") or {}
    original = catalog.get("source") or {}
    sha = str(source.get("discSha256") or "").lower()
    catalog_sha = str(original.get("discSha256") or "").lower()
    if not re.fullmatch(r"[0-9a-f]{64}", sha) or sha != catalog_sha:
        raise ValueError("Tabelas e catalogo pertencem a discos diferentes")
    rows = tables.get("starterPools")
    if not isinstance(rows, list) or len(rows) != 7:
        raise ValueError("Nao existem sete pools de starter no extrato privado")
    normalized = [None] * 7
    cards = {int(c["id"]): c for c in catalog.get("cards", [])}
    if len(cards) != 722 or set(cards) != set(range(1, 723)):
        raise ValueError("Catalogo Original de 722 cartas incompleto")
    for item in rows:
        if not isinstance(item, dict) or type(item.get("group")) is not int:
            raise ValueError("Grupo de starter invalido")
        idx = item["group"]
        if idx < 0 or idx >= 7 or normalized[idx] is not None or                 item.get("draws") != DRAWS[idx]:
            raise ValueError("Pool duplicado ou com quantidade de draws invalida")
        weights = normalize_starter_weights(item.get("weights"))
        for cid, weight in enumerate(weights, 1):
            if weight == 0:
                continue
            card = cards[cid]
            kind = card["kind"]
            if idx < 4:
                strength = int(card["attack"]) + int(card["defense"])
                if kind != "Monster" or not (
                    (idx == 0 and strength < 1100) or
                    (idx == 1 and 1100 <= strength < 1600) or
                    (idx == 2 and 1600 <= strength < 2100) or
                    (idx == 3 and strength >= 2100)):
                    raise ValueError("Monstro fora de seu pool de starter: #" + str(cid))
            elif idx in (4, 5) and kind != "Spell":
                raise ValueError("Pool de magia contem nao-magia")
            elif idx == 6 and kind != "Equip":
                raise ValueError("Pool de equip contem nao-equip")
        normalized[idx] = {"group": idx, "draws": DRAWS[idx], "weights": weights}
    return {"schema": 1, "source": source, "starterPools": normalized}


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[1])
    args = ap.parse_args()
    project = args.project.resolve()
    prior = project / "PrivateExtracts" / "reborn-disc" / "disc_tables.json"
    catalog = project / "Assets" / "StreamingAssets" / "RebornContent" / "original_export.json"
    target = catalog.with_name("starter_pools.json")
    try:
        tables = json.loads(prior.read_text(encoding="utf-8"))
        cards = json.loads(catalog.read_text(encoding="utf-8"))
        result = build(tables, cards)
        target.parent.mkdir(parents=True, exist_ok=True)
        tmp = target.with_suffix(".json.tmp")
        tmp.write_text(json.dumps(result, ensure_ascii=False, indent=2)+"\n",
                       encoding="utf-8")
        tmp.replace(target)
        print("PASS: 7 pools iniciais, 40 cartas por sorteio, mesmo SHA-256")
        print("Dados locais: " + str(target))
        return 0
    except (OSError, ValueError, TypeError, KeyError) as exc:
        print("IMPORTACAO DE DECKS: " + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
