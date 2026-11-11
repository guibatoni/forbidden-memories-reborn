#!/usr/bin/env python3
"""Synthetic local fusion catalog diagnostics, with no ROM or original game data."""
from copy import deepcopy
from diagnosticar_fusoes import inspect, report

def original():
    cards = [
        {"id": i, "name": f"synthetic {i}",
         "kind": "Monster" if i <= 600 else "Spell"}
        for i in range(1,723)
    ]
    return {
        "schema":1,
        "source":{"edition":"original-ps1-drop-x15","discSha256":"a"*64},
        "cards": cards,
        "fusions": [
            {"materialA":1,"materialB":2,"result":3,"verified":True},
            {"materialA":2,"materialB":4,"result":5,"verified":True}
        ],
    }
data = original()
errors, summary = inspect(data,2)
assert not errors, errors
assert summary["cards"] == 722
assert summary["fusionRecipesValid"] == 2
assert len(summary["queriedMatches"]) == 2
assert "Estrutura OK" in report(errors,summary)
print("PASS: verified synthetic fusion pairs and per-card lookup")

data = original();data["fusions"]=[]
errors, summary = inspect(data)
assert any("ZERO RECEITAS" in item for item in errors)
assert summary["fusionRecipesValid"] == 0
print("PASS: 722 cards alone cannot pass as a complete fusion import")

data=original();data["fusions"][0]["verified"]=False
errors,_=inspect(data)
assert any("verified=true" in item for item in errors)
data=original();data["fusions"][1]["materialA"]=2;data["fusions"][1]["materialB"]=1
errors,_=inspect(data)
assert any("duplicada" in item for item in errors)
data=original();data["fusions"][0]["result"]=700
errors,_=inspect(data)
assert any("nao e monstro" in item for item in errors)
data=original();data["source"]["discSha256"]="invalid"
errors,_=inspect(data)
assert any("SHA-256" in item for item in errors)
data=original();data["cards"]=data["cards"][:721]
errors,_=inspect(data)
assert any("incompleto" in item for item in errors)
data=original();data["fusions"]=[{"materialA":1,"materialB":2,"result":900,"verified":True}]
errors,_=inspect(data)
assert any("invalidos" in item for item in errors)
print("PASS: reject missing, invalid, duplicated, unverified and cross-type recipes")
