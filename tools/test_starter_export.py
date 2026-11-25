#!/usr/bin/env python3
"""Synthetic seven-pool exporter check, no ISO required."""
from copy import deepcopy
from export_starter_pools import build

sha="a"*64
cards=[]
for i in range(1,723):
    kind = "Monster" if i<=690 else ("Spell" if i<=719 else "Equip")
    attack = 300 if i<=200 else 600 if i<=400 else 900 if i<=600 else 1600
    defense = 350 if i<=200 else 650 if i<=400 else 900 if i<=600 else 1100
    cards.append({"id":i,"name":f"Synthetic {i}","kind":kind,
                  "attack":attack if kind=="Monster" else 0,
                  "defense":defense if kind=="Monster" else 0})
groups=[]
for group,draws in enumerate((16,16,4,1,1,1,1)):
    first = (1,201,401,601,691,705,720)[group]
    count = (8,8,4,4,4,2,3)[group]
    weights={str(first+n):2048//count for n in range(count)}
    weights[str(first)] += 2048 - (2048//count)*count
    groups.append({"group":group,"draws":draws,"weights":weights})
catalog={"source":{"discSha256":sha,"edition":"original-ps1-test"},"cards":cards}
tables={"source":{"discSha256":sha,"edition":"original-ps1-test"},
        "starterPools":groups}
result=build(tables,catalog)
assert len(result["starterPools"])==7
assert sum(g["draws"] for g in result["starterPools"])==40
assert all(len(g["weights"])==722 and sum(g["weights"])==2048
           for g in result["starterPools"])
assert sum(x>0 for x in result["starterPools"][5]["weights"])==2
print("PASS: 7 authentic starter row shapes, including 2-candidate field spell pool")
bad=deepcopy(catalog)
bad["source"]["discSha256"]="b"*64
try: build(tables,bad)
except ValueError: pass
else: raise AssertionError("Mismatched disc SHA accepted")
bad=deepcopy(tables)
bad["starterPools"][4]["weights"]={"1":2048}
try: build(bad,catalog)
except ValueError: pass
else: raise AssertionError("Monster masquerading as Spell accepted")
print("PASS: source SHA and category integrity checks")
