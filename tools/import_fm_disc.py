#!/usr/bin/env python3
"""Private, local disc data import for the Reborn Unity prototype.

Uses the independently developed open-source FM Editor parser from the
Yu-Gi-Oh-Forbidden-Memories-Recompiled project; its code is not vendored here.
The user must provide their own disc and upstream checkout. No disc bytes, names,
artwork, or derived data are sent to the GitHub repository or to Vercel.
"""
import argparse
import hashlib
import importlib.util
import json
import shutil
import struct
import sys
from pathlib import Path


def find_upstream(path: Path) -> Path:
    path = path.expanduser().resolve()
    options = [path / "tools" / "pc", path]
    for pc in options:
        if (pc / "fm_editor" / "gamedata.py").is_file() and (pc / "extract_images.py").is_file():
            return pc
    raise ValueError("A pasta --upstream precisa conter tools/pc/fm_editor e tools/pc/extract_images.py")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(4 * 1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def card_kind(card, gamedata) -> str:
    return {
        gamedata.TYPE_MAGIC: "Spell",
        gamedata.TYPE_TRAP: "Trap",
        gamedata.TYPE_EQUIP: "Equip",
        gamedata.TYPE_RITUAL: "Ritual",
    }.get(card.type, "Monster")




def normalize_deck_weights(source):
    """Map disc pool weights to exactly 722 card IDs (index 0 is card #1)."""
    if isinstance(source, dict):
        values = [0] * 722
        for key, value in source.items():
            try:
                card_id = int(key)
            except (TypeError, ValueError) as exc:
                raise ValueError("Invalid deck pool card ID: " + str(key)) from exc
            if card_id < 1 or card_id > 722:
                raise ValueError("Deck pool card ID outside 1..722: " + str(key))
            if type(value) is not int or value < 0:
                raise ValueError("Invalid pool weight at card ID " + str(key))
            values[card_id - 1] = value
    elif isinstance(source, (list, tuple)):
        if len(source) == 722:
            values = list(source)
        elif len(source) == 723 and source[0] == 0:
            values = list(source[1:])
        else:
            raise ValueError("Deck pool array must have 722 weights")
        if any(type(x) is not int or x < 0 for x in values):
            raise ValueError("Invalid deck pool weight value")
    else:
        raise ValueError("Unknown deck pool format")
    if sum(x > 0 for x in values) < 14:
        raise ValueError("At least 14 weighted cards required for 40 draws capped at three")
    return values

def normalize_starter_weights(source):
    """Seven starter rows can have only 2-6 cards, unlike opponent decks."""
    if not isinstance(source, dict):
        raise ValueError("Starter weights must be a 1-based ID dictionary")
    values = [0] * 722
    for key, value in source.items():
        number = int(key)
        if number < 1 or number > 722 or type(value) is not int or value < 0 or value > 2048:
            raise ValueError("Invalid starter pool ID or weight")
        values[number - 1] = value
    if sum(values) != 2048:
        raise ValueError("Starter pool row does not total 2048")
    return values


def load_game(disc_path: Path, pc: Path):
    sys.path.insert(0, str(pc))
    from fm_editor import disc, gamedata
    return disc.load(disc_path), gamedata


def export(disc_path: Path, project: Path, pc: Path, edition: str,
           upscale: int, extract_art: bool) -> dict:
    files, gamedata = load_game(disc_path, pc)
    data = gamedata.read_game(files.slus, files.wa)
    if len(data.cards) != 722:
        raise ValueError("Catalogo inconsistente: esperado 722 cartas")
    source = {"edition": edition, "language": "en", "discSha256": sha256(disc_path)}
    cards = []
    partial_stars = 0
    for cid in range(1, 723):
        card = data.cards[cid]
        if not card.name.strip():
            raise ValueError("Nome ausente na fonte binaria: carta #" + str(cid))
        first, second = int(card.star1), int(card.star2)
        if first not in range(len(gamedata.STAR_NAMES)) or second not in range(len(gamedata.STAR_NAMES)):
            raise ValueError("Guardian Star fora do catalogo para #" + str(cid))
        if bool(first) != bool(second):
            partial_stars += 1
            first = second = 0  # schema 1 requires a validated pair
        cards.append({
            "id": cid, "name": card.name, "kind": card_kind(card, gamedata),
            # Needed by the 0.21 field rules; without it Mountain, Umi,
            # Yami etc. could never classify imported PS1 monsters.
            "typeLabel": (
                gamedata.TYPE_NAMES[int(card.type)]
                if 0 <= int(card.type) < len(gamedata.TYPE_NAMES) else ""
            ),
            "attack": card.attack, "defense": card.defense,
            "description": card.description,
            "guardianStarA": gamedata.STAR_NAMES[first],
            "guardianStarB": gamedata.STAR_NAMES[second],
        })

    # Preserve *all* combinations performed by the PS1 game, including the
    # odd-record over-read combinations that the executable actually allows.
    # They are tagged to distinguish disc table entries from reader quirks.
    fusions = []
    glitch_count = 0
    for (left, right), result in sorted(data.fusions.items()):
        if not all(1 <= v <= 722 for v in (left, right, result)):
            raise ValueError("Fusion table contains an out-of-range original ID")
        is_glitch = (left, right) in data.glitch_fusions
        glitch_count += int(is_glitch)
        fusions.append({
            "materialA": left, "materialB": right, "result": result,
            "verified": True,
            "origin": "ps1-reader-quirk" if is_glitch else "disc-fusion-table",
        })

    # The ORIGINAL title has three specific, face-up tributes in the field.
    # The ritual result is not a hand card and has no TCG level-sum check.
    rituals = []
    for ritual_id, sequence in sorted(data.rituals.items()):
        if len(sequence) != 4:
            raise ValueError("Original ritual does not have three tributes: " + str(ritual_id))
        t1, t2, t3, monster_result = map(int, sequence)
        involved = [int(ritual_id), t1, t2, t3, monster_result]
        if not all(1 <= v <= 722 for v in involved):
            raise ValueError("Ritual references a card beyond 1..722: " + str(ritual_id))
        if cards[int(ritual_id) - 1]["kind"] != "Ritual" or \
                cards[monster_result - 1]["kind"] != "Monster" or \
                any(cards[t - 1]["kind"] != "Monster" for t in (t1, t2, t3)):
            raise ValueError("Ritual references unexpected card types: " + str(ritual_id))
        rituals.append({"ritualCard": int(ritual_id), "tributes": [t1, t2, t3],
                        "result": monster_result, "verified": True})
    opponents = []
    pools = []
    for index, weights in enumerate(data.pools):
        name = gamedata.DUELIST_NAMES[index]
        opponents.append({"id": index, "name": name, "deckKey": f"disc-duelist-{index}"})
        pools.append({"id": index, "name": name, "deckKey": f"disc-duelist-{index}",
                      "deckWeights": weights["deck"], "dropPow": weights["pow"],
                      "dropBcd": weights["bcd"], "dropTec": weights["tec"],
                      "aiParameters": list(gamedata.AI_ROWS[index])})
    # Retail has seven weighted starter pools, each with its own draw count.
    starter_pools = []
    try:
        offset = gamedata.STARTER_BASE
        for group in range(7):
            draws = struct.unpack_from("<H", files.wa, offset)[0]
            weights = struct.unpack_from("<722H", files.wa, offset + 2)
            starter_pools.append({"group": group, "draws": draws,
                                  "weights": {str(i): weight for i, weight in enumerate(weights, 1) if weight}})
            offset += 2 + 722 * 2
    except (IndexError, struct.error):
        starter_pools = []

    # Serializable Unity companion file. No disc-derived content is committed to Git.
    deck_pools = []
    for pool in pools:
        deck_pools.append({
            "id": pool["id"], "name": pool["name"], "deckKey": pool["deckKey"],
            "weights": normalize_deck_weights(pool["deckWeights"]),
        })
    deck_file = {"schema": 1, "source": source, "deckPools": deck_pools}
    export_file = {"schema": 1, "source": source,
                   "cards": cards, "opponents": opponents, "story": None,
                   "fusions": fusions, "rituals": rituals}
    tables_file = {"schema": 1, "source": source,
                   "deckMode": "WEIGHTED_POOLS_NOT_FIXED_DECKS",
                   "duelists": pools, "starterPools": starter_pools,
                   "equips": {str(k): v for k, v in data.equips.items()},
                   "rituals": {str(k): list(v) for k, v in data.rituals.items()},
                   "guardianStars": {"names": gamedata.STAR_NAMES,
                                     "advantage": "two original cycles, +500"},
                   "parserNotes": data.notes,
                   "ps1ReaderQuirkFusionsIncluded": glitch_count,
                   "cardsWithPartialStarPairs": partial_stars}

    # Originals and derivative assets must stay local, out of public Git history.
    private = project / "PrivateExtracts" / "reborn-disc"
    private.mkdir(parents=True, exist_ok=True)
    target = project / "Assets" / "StreamingAssets" / "RebornContent"
    target.mkdir(parents=True, exist_ok=True)
    (target / "original_export.json").write_text(
        json.dumps(export_file, indent=2, ensure_ascii=False), encoding="utf-8")
    (target / "opponent_decks.json").write_text(
        json.dumps(deck_file, indent=2, ensure_ascii=False), encoding="utf-8")
    # Streamable seven-row starter deck table. JsonUtility cannot directly
    # consume private disc_tables.json's sparse string-key dictionaries.
    # Never substitute fabricated probabilities for a missing disc table.
    starter_path = target / "starter_pools.json"
    if len(starter_pools) == 7:
        starter_file = {
            "schema": 1, "source": source,
            "starterPools": [
                {"group": row["group"], "draws": row["draws"],
                 "weights": normalize_starter_weights(row["weights"])}
                for row in starter_pools
            ]
        }
        starter_path.write_text(json.dumps(
            starter_file, indent=2, ensure_ascii=False),
            encoding="utf-8")
    else:
        starter_path.unlink(missing_ok=True)
    (private / "disc_tables.json").write_text(
        json.dumps(tables_file, indent=2, ensure_ascii=False), encoding="utf-8")

    # Reuse the already extracted local Equip table. The seven terrain
    # copies must agree, and the card catalog must be from this exact SHA.
    equip_report = None
    try:
        from export_equip_rules import build as build_equip_rules
        equip_export, equip_report = build_equip_rules(tables_file, export_file)
        (target / "equip_rules.json").write_text(
            json.dumps(equip_export, indent=2, ensure_ascii=False),
            encoding="utf-8")
    except ValueError as exc:
        # Never leave a stale other-disc equip_rules.json active.
        (target / "equip_rules.json").unlink(missing_ok=True)
        equip_report = {"error": str(exc), "status": "not imported"}
    (private / "equip-import-report.json").write_text(
        json.dumps(equip_report, indent=2, ensure_ascii=False),
        encoding="utf-8")

    images = 0
    if extract_art:
        path = pc / "extract_images.py"
        spec = importlib.util.spec_from_file_location("reborn_external_extract_images", path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        art_dir = private / "art"
        art_dir.mkdir(exist_ok=True)
        extractor = module.Extractor(str(private), str(art_dir))
        extractor.archives["WA_MRG.MRG"] = files.wa
        extractor.cards({})
        extractor.portraits()
        extractor.save_manifest()
        unity_art = project / "Assets" / "Resources" / "RebornCardArt"
        unity_art.mkdir(parents=True, exist_ok=True)
        try:
            from PIL import Image, ImageFilter
            pillow_available = True
        except ImportError:
            pillow_available = False
        for cid in range(1, 723):
            original = art_dir / "cards" / f"{cid:04d}.png"
            if not original.exists():
                raise ValueError("Arte da carta faltando #" + str(cid))
            output = unity_art / f"{cid:03d}.png"
            if pillow_available and upscale > 1:
                with Image.open(original) as img:
                    img = img.convert("RGBA").resize(
                        (img.width * upscale, img.height * upscale),
                        Image.Resampling.LANCZOS)
                    # Sharpen the existing pixels only, never hallucinate new art.
                    img.filter(ImageFilter.UnsharpMask(radius=1.1, percent=110,
                                                       threshold=3)).save(output)
            else:
                shutil.copyfile(original, output)
            images += 1

    categories = {}
    for card in cards:
        kind = card["kind"]
        row = categories.setdefault(kind, {"total": 0, "withDescription": 0,
                                            "missingDescription": 0})
        row["total"] += 1
        if isinstance(card.get("description"), str) and card["description"].strip():
            row["withDescription"] += 1
        else:
            row["missingDescription"] += 1
    report = {
        "source": source, "cards": len(cards), "fusions": len(fusions),
        "ps1ReaderQuirkFusionsIncluded": glitch_count,
        "rituals": len(rituals), "descriptionCoverageByKind": categories,
        "descriptionCount": sum(x["withDescription"] for x in categories.values()),
        "missingDescriptionCount": sum(x["missingDescription"] for x in categories.values()),
        "duelists": len(opponents),
        "starterPools": len(starter_pools), "opponentDeckPools": len(deck_pools), "artsImported": images,
        "equips": equip_report,
        "artSource": "disc-derived PNG; visual quality limited by PS1 originals",
        "tables": str(private / "disc_tables.json"),
        "unityJson": str(target / "original_export.json"),
        "unityOpponentDecks": str(target / "opponent_decks.json"),
        "warnings": ["Story branches are not extracted by this tool",
                     "Opponent decks are weight pools, not fixed deck sequences",
                     "Effects and text need gameplay validation",
                     "This image may be a mod (Drop x15) and must not be labeled retail without checksum verification",
                     "Descriptions are original disc text, not confirmed gameplay effects",
                     "Retail reader quirks are tagged, not silently discarded"],
    }
    (private / "import-report.json").write_text(
        json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--disc", type=Path, required=True,
                        help="Your .bin or .iso (never add this to public Git)")
    parser.add_argument("--upstream", type=Path, required=True,
                        help="Local checkout of Unchiga/Yu-Gi-Oh-Forbidden-Memories-Recompiled")
    parser.add_argument("--project", type=Path, default=Path.cwd(),
                        help="Unity project root containing Assets/")
    parser.add_argument("--edition", default="original-ps1-variant",
                        help="Use original-ps1-drop-x15 for the supplied modified disc")
    parser.add_argument("--art", action="store_true",
                        help="Also extract card art and opponent portraits locally")
    parser.add_argument("--upscale", type=int, choices=(1, 2, 4), default=4,
                        help="Art size multiplier; requires Pillow for resampling")
    args = parser.parse_args()
    try:
        pc = find_upstream(args.upstream)
        if not (args.project / "Assets").is_dir():
            raise ValueError("--project must point to your Unity project root")
        if not args.disc.is_file():
            raise ValueError("--disc not found: " + str(args.disc))
        report = export(args.disc, args.project.resolve(), pc,
                        args.edition, args.upscale, args.art)
        print(json.dumps(report, ensure_ascii=False, indent=2))
        return 0
    except Exception as ex:
        print(f"REBORN import error: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
