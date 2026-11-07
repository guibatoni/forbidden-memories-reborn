# Forbidden Memories Reborn — Prototype 0.5 (combat feedback)

## Release scope

- `DuelMatch.CanAttack` is read-only: legal attacks can be previewed before VFX begins.
- Player attack director: selected attacker -> highlighted target -> projectile -> engine resolution. Input locked until completed.
- Opponent director now validates each attack before animating it.
- `CombatFeedbackOverlay`: floating LP damage and destruction text, plus animated LP gauges and Guardian Star +500 feedback.
- `ImpactRing`: short procedural shockwave layered on the particle impact, without requiring imported art or an additional field slot.
- The engine still owns LP, graveyard, combat state and victory. VFX never writes into `DuelMatch`.
- Camera, `OrbitCamera.cs`, five monster and five support zones per side are unchanged.

## How to validate in Unity 6000.3.28f1

1. Back up your 0.4.2.1 project, stop Play and close the Unity Editor.
2. Merge the patch ZIP into the folder containing `Assets/`, `Packages/`, `ProjectSettings/`.
3. Open project; confirm zero red Console errors, and press Play.
4. In experimental duel choose a creature, start combat, select attacker and target, watch projectile land before LP changes. Repeat with a direct attack.
5. Observe the animated LP meters and `-XXX LP` popup. Check destruction and Guardian Star advantage for cards that have that relationship.
6. End turn, check that the opponent attacks in steps. Start a new duel and confirm old VFX does not update it.
7. Disable Animations and confirm the engine still resolves damage correctly.
8. Run Edit Mode NUnit tests using Window > General > Test Runner (or Window > Test Runner).

## Known gaps

- Unity Editor play-mode VFX and GUI have not been automatically compiled/tested outside the user's Unity installation.
- Animation timing currently uses a fixed projectile duration; future work will use explicit event completion/async sequencing.
- Import of real ISO-derived cards, original opponents and campaign content is pending; demo data is not official.
- The full ruleset for spells, traps, equips, rituals, terrain and ranks remains incomplete.
- Audio and final 2.5D card presentation are deferred.