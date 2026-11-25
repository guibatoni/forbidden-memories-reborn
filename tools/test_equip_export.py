#!/usr/bin/env python3
"""Synthetic 722-card Equip import regression, no ROM/ISO/assets."""
from copy import deepcopy
from export_equip_rules import build

SHA="a"*64
cards=[]
for cid in range(1,723):
    kind = "Equip" if cid in (315,324,657,668) else "Monster"
    cards.append({"id":cid,"kind":kind,"name":f"synthetic #{cid}"})
catalog={"schema":1,
         "source":{"edition":"original-ps1-drop-x15","discSha256":SHA},
         "cards":cards}
tables={"schema":1,
        "source":{"edition":"original-ps1-drop-x15","discSha256":SHA},
        "equips":{"315":[1,2],"324":[3],"657":[1,2],"668":[1,2]},
        "parserNotes":[]}
output,report=build(tables,catalog)
assert len(output["equips"])==2
assert [r["cardId"] for r in output["equips"]]==[315,324]
assert output["equips"][0]["targets"]==[1,2]
assert all(r["verified"] and r["attackBonus"]==500 and
           r["defenseBonus"]==500 for r in output["equips"])
assert output["source"]["discSha256"]==SHA
assert report["specificEquipsExported"]==2 and report["totalExplicitTargets"]==3
assert report["equipsNotCovered"]==[]
print("PASS: existing private disc tables export all specific 500-point equip pairs")
bad=deepcopy(tables);bad["source"]["discSha256"]="b"*64
try: build(bad,catalog)
except ValueError as e: assert "SHA" in str(e)
else: raise AssertionError("Different ISO pairing tables were accepted")
bad=deepcopy(tables);bad["parserNotes"]=[
    "terrain copy 2's equip table differs from copy 0; copy 0 is the one shown"]
try: build(bad,catalog)
except ValueError as e: assert "divergentes" in str(e)
else: raise AssertionError("Divergent terrain banks accepted")
bad=deepcopy(tables);bad["equips"]["315"]=[1,1]
try: build(bad,catalog)
except ValueError: pass
else: raise AssertionError("Duplicated target accepted")
bad=deepcopy(tables);bad["equips"]["315"]=[324]
try: build(bad,catalog)
except ValueError: pass
else: raise AssertionError("Equip used as a monster target")
bad=deepcopy(tables);bad["equips"]["324"]=[0]
try: build(bad,catalog)
except ValueError: pass
else: raise AssertionError("Out-of-range target accepted")
print("PASS: rejects wrong SHA, divergent banks, duplicates, non-monsters and invalid IDs")
