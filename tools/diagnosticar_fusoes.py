#!/usr/bin/env python3
"""Reborn fusion health-check using a LOCAL same-disc export (no ROM or network).

Usage:
    python tools/diagnosticar_fusoes.py
    python tools/diagnosticar_fusoes.py --card 16
    python tools/diagnosticar_fusoes.py --project C:/Reborn --card 16

A JSON file with 722 cards but 0 fusion recipes is NOT a complete game
import. Returns nonzero for missing/invalid/empty fusion catalogs, and
never writes back to the original data.
"""
from __future__ import annotations
import argparse
import json
import re
import sys
from pathlib import Path

VALID_SHA = re.compile(r"^[0-9a-fA-F]{64}$")


def inspect(data: dict, card_id: int | None = None) -> tuple[list[str], dict]:
    errors: list[str] = []
    if not isinstance(data, dict) or data.get("schema") != 1:
        return ["Schema incorreto: esperado schema=1."], {}
    source = data.get("source") or {}
    if not isinstance(source, dict):
        source = {}
    edition = source.get("edition") or ""
    sha = source.get("discSha256") or ""
    if not isinstance(edition, str) or not edition.startswith("original-ps1"):
        errors.append("Perfil nao identificado como Original PS1.")
    if not isinstance(sha, str) or not VALID_SHA.fullmatch(sha):
        errors.append("SHA-256 da ISO ausente/invalido.")
    cards = data.get("cards")
    if not isinstance(cards, list) or len(cards) != 722:
        errors.append("Catalogo incompleto: esperado exatamente 722 cartas.")
        cards = cards if isinstance(cards, list) else []
    catalog: dict[int, dict] = {}
    for c in cards:
        if not isinstance(c, dict) or type(c.get("id")) is not int:
            errors.append("Registro de carta sem ID numerico.")
            continue
        cid = c["id"]
        if cid in catalog or not 1 <= cid <= 722:
            errors.append(f"Carta duplicada ou fora de 1..722: #{cid}")
        else:
            catalog[cid] = c
    if len(catalog) != 722:
        errors.append("IDs 1..722 ausentes ou duplicados.")
    raw = data.get("fusions")
    if raw is None:
        raw = []
    if not isinstance(raw, list):
        errors.append("O campo 'fusions' deve ser um array.")
        raw = []
    if not raw:
        errors.append("ZERO RECEITAS DE FUSAO: importacao incompleta.")
    unique = set()
    supported = []
    for i, f in enumerate(raw):
        if not isinstance(f, dict):
            errors.append(f"fusions[{i}] nao e objeto.")
            continue
        a, b, result = (f.get(k) for k in ("materialA", "materialB", "result"))
        if any(type(x) is not int or not 1 <= x <= 722 for x in (a, b, result)):
            errors.append(f"fusions[{i}]: IDs de materiais/resultado invalidos.")
            continue
        if a not in catalog or b not in catalog or result not in catalog:
            errors.append(f"fusions[{i}]: ID ausente do mesmo catalogo.")
            continue
        if f.get("verified") is not True:
            errors.append(f"fusions[{i}]: verified=true nao confirmado.")
            continue
        if str(catalog[result].get("kind") or "").lower() != "monster":
            errors.append(f"fusions[{i}]: resultado nao e monstro.")
            continue
        key = tuple(sorted((a, b)))
        if key in unique:
            errors.append(f"fusions[{i}]: receita duplicada {key}.")
            continue
        unique.add(key)
        supported.append((a, b, result))
    selected = []
    if card_id is not None:
        if card_id not in catalog:
            errors.append(f"Carta consultada #{card_id} nao existe.")
        else:
            selected = [(a, b, r) for a,b,r in supported
                        if a == card_id or b == card_id]
    summary = {
        "edition": edition,
        "sha256Prefix": sha[:12] if isinstance(sha, str) else "",
        "cards": len(catalog),
        "fusionRecipesDeclared": len(raw),
        "fusionRecipesValid": len(supported),
        "queriedCard": card_id,
        "queriedMatches": selected,
        "exampleRecipes": supported[:5],
    }
    return errors, summary


def report(errors: list[str], summary: dict) -> str:
    lines = [
        "=== REBORN: DIAGNOSTICO LOCAL DE FUSOES ===",
        f"Edicao: {summary.get('edition','desconhecida')}",
        f"SHA da ISO (prefixo): {summary.get('sha256Prefix','')}",
        f"Cartas reconhecidas: {summary.get('cards',0)}/722",
        f"Receitas declaradas: {summary.get('fusionRecipesDeclared',0)}",
        f"Receitas validas: {summary.get('fusionRecipesValid',0)}",
    ]
    if summary.get("queriedCard") is not None:
        lines.append("Consulta da carta #" + str(summary["queriedCard"]) +
                     ": " + str(len(summary.get("queriedMatches",[]))) +
                     " receitas envolvendo-a")
        for a,b,r in summary.get("queriedMatches",[])[:25]:
            lines.append(f"  #{a:03d} + #{b:03d} -> #{r:03d}")
    if errors:
        lines.extend(["ERRO: " + e for e in errors])
        lines.append("ACAO: execute tools/IMPORTAR_ORIGINAL.ps1 com sua propria ISO,")
        lines.append("      ou IMPORTAR_DO_DUELTRACE.ps1 com lote revisado e SHA correspondente.")
        lines.append("      Se ja importou, veja se 'fusions' contem receitas verificadas.")
    else:
        lines.append("Estrutura OK: estas receitas estao disponiveis no export local.")
        lines.append("Ainda e necessario validar o resultado dentro do Unity.")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path,
                        default=Path(__file__).resolve().parents[1])
    parser.add_argument("--card", type=int)
    args = parser.parse_args()
    if args.card is not None and not 1 <= args.card <= 722:
        parser.error("--card deve estar entre 1 e 722")
    path = args.project / "Assets" / "StreamingAssets" / "RebornContent" / "original_export.json"
    if not path.is_file():
        print("ERRO: original_export.json nao encontrado: " + str(path),
              file=sys.stderr)
        print("ACAO: importe sua ISO Original (IMPORTAR_ORIGINAL.ps1).",
              file=sys.stderr)
        return 2
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        errors, summary = inspect(data, args.card)
    except (OSError, ValueError, TypeError, json.JSONDecodeError) as exc:
        print("ERRO: JSON de fusoes ilegivel: " + str(exc), file=sys.stderr)
        return 2
    print(report(errors, summary))
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
