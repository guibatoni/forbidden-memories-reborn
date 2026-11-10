#!/usr/bin/env python3
"""Import a LOCAL DUELTRACE profile into Reborn without copying a ROM or art.

DUELTRACE provides 722 names/types/stats and optionally effect text + reviewed
fusion entries. Missing descriptions/Guardian Stars/rituals are retained from
a prior SAME-SHA disc export, never hallucinated from a card's name.
"""
from __future__ import annotations

import argparse
import copy
import json
import os
import re
import shutil
import sys
from pathlib import Path

CARD_COUNT = 722
SHA_RE = re.compile(r"^[0-9a-fA-F]{64}$")
TYPE_TO_KIND = {**{n: "Monster" for n in range(20)},
                20: "Spell", 21: "Trap", 22: "Ritual", 23: "Equip"}
VALIDATION_MANUAL = "observada_manualmente_no_jogo"
VALIDATION_REVIEWED = "revisada_usuario_em_lote"


def read_json(path: Path) -> dict:
    result = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(result, dict):
        raise ValueError("JSON precisa conter um objeto")
    return result


def full_sha(value: object) -> str:
    if not isinstance(value, str) or not SHA_RE.fullmatch(value):
        raise ValueError("SHA-256 do perfil DUELTRACE ausente ou invalido")
    return value.lower()


def card_id(value: object) -> int:
    if type(value) is int:
        number = value
    elif isinstance(value, str) and value.isdecimal():
        number = int(value)
    else:
        raise ValueError("ID de carta invalido: " + repr(value))
    if not (1 <= number <= CARD_COUNT):
        raise ValueError("ID fora do catalogo 1..722: " + str(number))
    return number


def card_int(value: object, field: str) -> int:
    if isinstance(value, bool):
        raise ValueError("Atributo booleano invalido: " + field)
    if type(value) is int or isinstance(value, str) and value.isdecimal():
        number = int(value)
        if 0 <= number <= 99999:
            return number
    raise ValueError("Atributo invalido: " + field)


def choose_profile(explicit: Path | None, home: Path) -> Path:
    if explicit is not None:
        result = explicit.expanduser().resolve()
        if not result.is_file():
            raise FileNotFoundError("Perfil nao encontrado: " + str(result))
        return result
    registry = home / "registro_mods.json"
    if not registry.is_file():
        raise FileNotFoundError("DUELTRACE nao encontrado. Use --profile com seu profile.json.")
    rows = read_json(registry).get("entries", [])
    matches = [Path(row.get("profile_path", "")) for row in rows
               if isinstance(row, dict) and
               "original" in (str(row.get("file_name") or "") + " " +
                              str(row.get("name") or "")).lower() and
               row.get("profile_path") and Path(row["profile_path"]).is_file()]
    if len(matches) != 1:
        raise ValueError(
            "Selecione explicitamente o perfil do mod Original com --profile "
            f"(encontrados {len(matches)} perfis originais possiveis)")
    return matches[0].resolve()


def make_cards(profile: dict, existing: dict | None) -> tuple[list[dict], dict]:
    if profile.get("verified", {}).get("cartas") != "verificado":
        raise ValueError("O DUELTRACE ainda nao validou os 722 registros de cartas")
    input_cards = profile.get("cards", [])
    if not isinstance(input_cards, list) or len(input_cards) != CARD_COUNT:
        raise ValueError("Perfil precisa conter exatamente 722 cartas")
    old = {card_id(c["id"]): c for c in (existing or {}).get("cards", [])}
    cards = []
    seen = set()
    count_effects = 0
    count_descriptions = 0
    for row in input_cards:
        cid = card_id(row.get("id"))
        if cid in seen:
            raise ValueError("Carta duplicada: " + str(cid))
        seen.add(cid)
        if type(row.get("type_id")) is not int or row["type_id"] not in TYPE_TO_KIND:
            raise ValueError("Tipo nao identificado para carta #" + str(cid))
        kind = TYPE_TO_KIND[row["type_id"]]
        previous = old.get(cid, {})
        if previous and previous.get("kind") != kind:
            raise ValueError("Tipo divergente entre DUELTRACE e export do disco: #" + str(cid))
        name = str(row.get("name") or "").strip()
        if not name:
            raise ValueError("Nome ausente no DUELTRACE: #" + str(cid))
        attack = card_int(row.get("atk"), "atk #" + str(cid))
        defense = card_int(row.get("def"), "def #" + str(cid))
        if previous and (previous.get("attack") != attack or previous.get("defense") != defense):
            raise ValueError("ATK/DEF divergentes no mesmo SHA: #" + str(cid))
        # DUELTRACE's current card catalog has no built-in PS1 descriptions.
        desc = str(row.get("description") or previous.get("description") or "").strip()
        effect = str(row.get("effect_text") or previous.get("effectText") or "").strip()
        if len(desc) > 4096 or len(effect) > 1800:
            raise ValueError("Texto excessivo para #" + str(cid))
        if "\x00" in desc or "\x00" in effect:
            raise ValueError("Texto binario nao permitido para #" + str(cid))
        source = str(row.get("effect_source") or previous.get("effectSource") or "") if effect else ""
        status = str(row.get("effect_status") or previous.get("effectStatus") or "") if effect else ""
        if effect and (not source or status in ("", "nao_identificado")):
            raise ValueError("Texto de efeito sem procedencia: #" + str(cid))
        cards.append({
            "id": cid, "name": name, "kind": kind,
            "attack": attack, "defense": defense,
            "description": desc,
            "typeLabel": str(row.get("type_label") or previous.get("typeLabel") or "").strip(),
            "guardianStarA": previous.get("guardianStarA") or "",
            "guardianStarB": previous.get("guardianStarB") or "",
            "effectText": effect, "effectSource": source,
            "effectStatus": status,
        })
        count_descriptions += bool(desc)
        count_effects += bool(effect)
    if seen != set(range(1, CARD_COUNT + 1)):
        raise ValueError("IDs nao abrangem todas as 722 cartas")
    cards.sort(key=lambda c: c["id"])
    return cards, {"cards": len(cards), "descriptions": count_descriptions,
                   "effectTexts": count_effects,
                   "missingDescriptions": CARD_COUNT - count_descriptions,
                   "starsFromPriorDiscExport": sum(bool(c["guardianStarA"]) for c in cards)}


def mirror_report_matches(profile: dict, report: dict) -> bool:
    sha = profile["rom_sha256"].lower()
    review = profile.get("fusion_review") or {}
    fp = review.get("candidate_fingerprint")
    if not (isinstance(fp, str) and fp and
            review.get("status") == "revisado_usuario_nao_testado_individualmente" and
            review.get("rom_sha256", "").lower() == sha and
            report.get("rom_sha256", "").lower() == sha and
            report.get("candidate_fingerprint") == fp and
            report.get("valid_banks") == 7 and report.get("matching_banks") == 7 and
            report.get("divergent_banks") is False and
            report.get("bank_conflicts") == 0):
        return False
    banks = report.get("banks", [])
    return (len(banks) == 7 and
            all(b.get("status") == "decodificado" and
                b.get("recipelist_sha256") == fp and
                b.get("conflict_count") == 0 for b in banks))


def make_fusions(profile: dict, existing: dict | None, *, reviewed: bool,
                 report: dict | None) -> tuple[list[dict], dict]:
    fusion_map = {}
    for row in (existing or {}).get("fusions", []):
        a, b, result = (card_id(row[key]) for key in
                        ("materialA", "materialB", "result"))
        if row.get("verified") is not True:
            raise ValueError("Export anterior contem fusao nao validada")
        fusion_map[(min(a,b), max(a,b))] = {
            "materialA": min(a,b), "materialB": max(a,b),
            "result": result, "verified": True,
            "origin": str(row.get("origin") or "prior-disc-export")}
    stats = {"baselineFusions": len(fusion_map), "dueltraceManual": 0,
             "dueltraceReviewed": 0, "ignoredUnreviewed": 0}
    can_review = reviewed and report is not None and mirror_report_matches(profile, report)
    if reviewed and not can_review:
        raise ValueError("Lote revisado exige relatorio DUELTRACE do mesmo SHA, "
                         "sete bancos iguais e fingerprint revisado")
    batch = profile.get("fusion_recipes") or []
    if not isinstance(batch, list):
        raise ValueError("fusion_recipes precisa ser lista")
    for row in batch:
        if not isinstance(row, dict):
            raise ValueError("Receita DUELTRACE incorreta")
        validation = row.get("validation")
        if validation == VALIDATION_MANUAL:
            stats["dueltraceManual"] += 1
        elif validation == VALIDATION_REVIEWED and can_review and \
                row.get("fusion_candidate_fingerprint") == \
                (profile.get("fusion_review") or {}).get("candidate_fingerprint"):
            stats["dueltraceReviewed"] += 1
        else:
            stats["ignoredUnreviewed"] += 1
            continue
        a, b, result = (card_id(row[key]) for key in ("a", "b", "result"))
        key = (min(a,b), max(a,b))
        if key in fusion_map:
            if fusion_map[key]["result"] != result:
                raise ValueError(f"Fusao divergente para materiais {a}/{b}")
            continue
        fusion_map[key] = {
            "materialA": key[0], "materialB": key[1],
            "result": result, "verified": True,
            "origin": ("dueltrace-manual" if validation == VALIDATION_MANUAL
                       else "dueltrace-reviewed-seven-banks"),
        }
    return [fusion_map[k] for k in sorted(fusion_map)], stats


def build_export(profile: dict, prior: dict | None = None, *,
                 reviewed: bool = False, fusion_report: dict | None = None) -> tuple[dict, dict]:
    sha = full_sha(profile.get("rom_sha256"))
    if prior is not None:
        original_sha = (prior.get("source") or {}).get("discSha256")
        if not original_sha or full_sha(original_sha) != sha:
            raise ValueError("Perfil DUELTRACE e export do disco nao sao da mesma ISO/BIN (SHA diferente)")
    cards, coverage = make_cards(profile, prior)
    fusions, fusion_stats = make_fusions(profile, prior, reviewed=reviewed,
                                        report=fusion_report)
    lookup = {c["id"]: c for c in cards}
    for row in fusions:
        if lookup[row["result"]]["kind"] != "Monster":
            raise ValueError("Fusao com resultado nao-monstro: " + str(row))
    # DUELTRACE currently does not export PS1 ritual tables: use SAME-SHA
    # previously imported disc export if one exists, otherwise report missing.
    result = {
        "schema": 1,
        "source": {"edition": "original-ps1-dueltrace", "language": "und",
                   "discSha256": sha},
        "cards": cards,
        "fusions": fusions,
        "rituals": copy.deepcopy((prior or {}).get("rituals") or []),
        "opponents": copy.deepcopy((prior or {}).get("opponents") or
                                   [{"id": int(o["id"]), "name": str(o["name"]),
                                     "deckKey": "dueltrace-opponent-"+str(o["id"])}
                                    for o in profile.get("opponents", [])
                                    if o.get("id") is not None and o.get("name")]),
        "story": copy.deepcopy((prior or {}).get("story")),
    }
    try:
        from validate_import import validate
    except ImportError:
        from tools.validate_import import validate
    errors, _ = validate(result)
    if errors:
        raise ValueError("Export recusado: " + "; ".join(errors[:10]))
    return result, {**coverage, **fusion_stats, "fusions": len(fusions),
                    "rituals": len(result["rituals"]), "sourceSha256": sha,
                    "effectsExecutable": False,
                    "notes": [
                        "Descricoes ausentes nao sao inventadas.",
                        "O texto de efeito e somente informativo; a execucao nao esta validada.",
                        "Fusoes revisadas em lote exigem permissao explicita e sete bancos iguais.",
                        "Os rituais requerem export do disco original com SHA identico.",
                        "O rotulo Original (Drop x15) nao comprova identidade com o retail."
                    ]}


def copy_local_art(profile: dict, project: Path) -> dict:
    """Optionally reuse locally extracted PS1 PNGs without overwriting custom Reborn art."""
    root_name = profile.get("art_root")
    if not profile.get("supported_features", {}).get("art") or not root_name:
        raise ValueError("Este perfil DUELTRACE nao possui 722 artes extraidas")
    source = Path(root_name).expanduser().resolve()
    if not source.is_dir():
        raise FileNotFoundError("Pasta local de artes DUELTRACE indisponivel: " + str(source))
    # Validate all source files before writing a single image to the project.
    for cid in range(1, CARD_COUNT + 1):
        path = source / f"{cid:03d}.png"
        if not path.is_file() or path.stat().st_size > 12_000_000:
            raise ValueError("Arte DUELTRACE ausente ou muito grande: #" + str(cid))
        with path.open("rb") as stream:
            if stream.read(8) != b"\x89PNG\r\n\x1a\n":
                raise ValueError("Arquivo de arte nao e PNG: #" + str(cid))
    dest = project / "Assets" / "Resources" / "RebornCardArt"
    dest.mkdir(parents=True, exist_ok=True)
    copied = preserved = 0
    for cid in range(1, CARD_COUNT + 1):
        target = dest / f"{cid:03d}.png"
        if target.exists():
            preserved += 1
        else:
            shutil.copy2(source / f"{cid:03d}.png", target)
            copied += 1
    return {"copied": copied, "customArtPreserved": preserved,
            "artFolder": "Assets/Resources/RebornCardArt (local ignored)"}


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--profile", type=Path,
                    help="DUELTRACE profiles/<sha-prefix>/profile.json; se omitido, detecta 1 unico Original")
    ap.add_argument("--home", type=Path,
                    default=Path(os.environ.get("LOCALAPPDATA", str(Path.home()))) /
                    "ForbiddenMemoriesMultiMod")
    ap.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[1])
    ap.add_argument("--fusion-report", type=Path,
                    help="investigacoes/<sha>.fusoes.json; exigido para lote revisado")
    ap.add_argument("--include-reviewed-batch", action="store_true",
                    help="Permite usar fusoes revisadas em lote (nao verificadas 1 a 1)")
    ap.add_argument("--copy-local-art", action="store_true",
                    help="Copia PNGs locais do DUELTRACE sem sobrescrever artes personalizadas")
    args = ap.parse_args()
    project = args.project.resolve()
    target = project / "Assets" / "StreamingAssets" / "RebornContent" / "original_export.json"
    if not (project / "Assets").is_dir():
        ap.error("--project precisa apontar para a pasta do projeto Unity")
    try:
        profile_path = choose_profile(args.profile, args.home)
        profile = read_json(profile_path)
        prior = read_json(target) if target.is_file() else None
        fusion_report = read_json(args.fusion_report) if args.fusion_report else None
        if args.include_reviewed_batch and fusion_report is None:
            raise ValueError("--include-reviewed-batch requer --fusion-report")
        export, summary = build_export(profile, prior,
                                       reviewed=args.include_reviewed_batch,
                                       fusion_report=fusion_report)
        if args.copy_local_art:
            summary["art"] = copy_local_art(profile, project)
        else:
            summary["art"] = "not copied (use --copy-local-art to reuse local PNGs)"
        target.parent.mkdir(parents=True, exist_ok=True)
        private = project / "PrivateExtracts" / "reborn-disc"
        private.mkdir(parents=True, exist_ok=True)
        if target.exists():
            # Create a private backup before ever overwriting an earlier import.
            shutil.copy2(target, private / "original_export.before_dueltrace.json")
        temporary = target.with_suffix(".json.tmp")
        temporary.write_text(json.dumps(export, ensure_ascii=False, indent=2) + "\n",
                             encoding="utf-8")
        temporary.replace(target)
        report_path = private / "dueltrace-bridge-report.json"
        report_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n",
                               encoding="utf-8")
        print(json.dumps(summary, ensure_ascii=False, indent=2))
        print("Export local: ", target)
        print("Relatorio privado: ", report_path)
        return 0
    except (ValueError, FileNotFoundError, KeyError, TypeError, OSError) as exc:
        print("DUELTRACE -> REBORN: " + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
