# Forbidden Memories Reborn — Hotfix 0.13.1

## Cause of Unity Safe Mode
The Unity editor reported:

```
Assets/Scripts/Presentation/ArenaHud.cs(302,57):
error CS1061: 'MatchSide' does not contain a definition for 'MonsterCount'
```

`ArenaHud.cs` belongs to the `Reborn.Presentation` Unity assembly,
while `DuelMatch.cs` belongs to `Reborn.Core`. The `MonsterCount`
property on `MatchSide` was declared `internal`, which made it
inaccessible to the independent presentation assembly.

## Fix
- Changed only `MatchSide.MonsterCount` to `public`, preserving a getter
  without a setter.
- Retained the existing monster-count computation and all duel rules.
- Added a separate .NET 8 Core library project and a cross-assembly
  console smoke test, verifying `MonsterCount` is accessible and read-only
  outside the Core assembly.
- Added that cross-assembly compile to GitHub Actions. Earlier Core-only
  tests compiled all Core scripts into one assembly, so they could not catch
  this particular visibility regression.
- AI Learning 2.0, camera, animation timing and local player save files
  are unchanged.

## Local Unity verification
1. Close Unity or enter Safe Mode if prompted.
2. Replace the contents of `Assets/Scripts/Core/DuelMatch.cs`
   with the updated file from `main` (or change `internal int MonsterCount`
   to `public int MonsterCount` in the original).
3. Save and allow Unity 6000.3.28f1 to recompile.
4. Check the Console for any other red compilation errors.
5. Exit Safe Mode and start the duel only after all red errors are gone.

GitHub Actions is NOT a complete Unity Editor compilation. This hotfix
addresses the reported CS1061; follow-up Unity validation remains necessary.

Recovery branch: `backup/prototype-0.13.1`
Library folder: `Forbidden Memories Reborn/Backups/Prototype 0.13.1`.
