# Historico de backups do Forbidden Memories Reborn

Pasta persistente na Biblioteca do ChatGPT: **/Forbidden Memories Reborn/Backups**.

- `2026-11-07_reborn-v0.3_base.zip` — snapshot completo publicado antes das alteracoes 0.4.
- `2026-11-07_reborn-v0.4_prototype.zip` — snapshot completo do codigo 0.4.
- `2026-11-07_reborn-v0.4_patch.zip` — somente arquivos novos ou alterados em relacao ao pacote 0.3.
- `2026-11-07_reborn-v0.4_backup-manifest.json` — SHA-256 e inventario dos pacotes.

**Fluxo para as proximas etapas:** preservar a ultima versao funcional > executar alteracoes > testes possiveis > publicar no GitHub > gerar pacote completo e patch > gerar manifest com hashes > arquivar na Biblioteca. Nao sobrescrever backups anteriores.

- `Prototype 0.5/` — novo pacote completo, patch incremental, manifesto com hashes de integridade.
  - `2026-11-07_reborn-v0.5_prototype.zip`: projeto completo Unity.
  - `2026-11-07_reborn-v0.5_patch.zip`: somente arquivos atualizados da versao 0.4.2.1.
  - `2026-11-07_reborn-v0.5_backup-manifest.json`: hashes SHA-256 dos pacotes.

A interface Unity so e considerada validada depois dos testes locais do editor.

## Prototype 0.6 — 7/nov/2026
- Cartas 2.5D reutilizáveis; preview de dados visíveis e arte local opcional.
- Backup completo e atualização incremental em Biblioteca: `/Forbidden Memories Reborn/Backups/Prototype 0.6`.
- A câmera e o motor de duelo permanecem como na versão 0.5.



## Prototype 0.8 (GitHub backup)
- Permanent backup branch: backup/prototype-0.8 (if created successfully).
- CI clean-source artifact: Reborn-0.8-Clean-Source (90-day retention).
- Contains source only. Private disc exports/artwork/ROMs are excluded.


## Prototype 0.9
- GitHub recovery branch: backup/prototype-0.9.
- GitHub Actions clean-source artifact: Reborn-0.9-Clean-Source (90-day retention).
- ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.9.
- AI learning file stays ONLY on the player's machine: Application.persistentDataPath/Reborn/adaptive-ai-v1.json.


## Prototype 0.9.3
- GitHub recovery branch: backup/prototype-0.9.3
- Clean-source CI artifact: Reborn-0.9.3-Clean-Source
- ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.9.3
- Excludes user ISOs, card art and local adaptive-AI learning profiles.


## Prototype 0.10
- GitHub recovery branch: backup/prototype-0.10
- GitHub Actions clean-source artifact: Reborn-0.10-Clean-Source
- ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.10
- Includes tests and tactical combat forecast UI; no local ISO or AI save data.


## Prototype 0.11
- Recovery branch: backup/prototype-0.11
- GitHub Actions source artifact: Reborn-0.11-Clean-Source
- ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.11
- Offline multistep attack planner, preserving camera and duel rules.
- No ROM, copyrighted art, extracted private content or learning saves.


## Prototype 0.12
- GitHub recovery branch: backup/prototype-0.12
- Source CI artifact: Reborn-0.12-Clean-Source
- Benchmark report CI artifact: Reborn-0.12-AiLab-Report
- ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.12
- No personal AI save, ISO, protected images or disc-derived data in the archives.


## Prototype 0.12.1
- Recovery branch: backup/prototype-0.12.1
- CI source artifact: Reborn-0.12.1-Clean-Source
- CI benchmark artifact: Reborn-0.12.1-AiLab-MultiSeed
- ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.12.1
- Reports contain only synthetic performance data (no local ISO, user profiles or artworks).


## Prototype 0.13 — AI Learning 2.0
- Recovery branch: backup/prototype-0.13
- Clean source CI artifact: Reborn-0.13-Clean-Source
- A/B benchmark CI artifact: Reborn-0.13-AiLab-Learning2
- ChatGPT Library: Forbidden Memories Reborn / Backups / Prototype 0.13
- Preserves previous local adaptive profiles; no user save data in public artifacts.


## Prototype 0.13.1 — Unity compilation hotfix
- GitHub recovery branch: backup/prototype-0.13.1
- GitHub Actions clean-source artifact: Reborn-0.13.1-Clean-Source
- ChatGPT Library folder: Forbidden Memories Reborn/Backups/Prototype 0.13.1
- Fixes public Core to Presentation MonsterCount access; no changes to AI Learning 2.0 behavior.


## Prototype 0.14 — Fluent Duel
- GitHub recovery branch: backup/prototype-0.14
- CI source artifact: Reborn-0.14-Clean-Source
- ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.14
- MTGA-like fan hand, numbered Shift fusions, face choice, automatic battle,
  direct attacker-to-target clicks and legal monster position changes.
- Unverified spell, equip, ritual and trap effects remain disabled.


## Prototype 0.14.1 — Responsive Card Fan
- Recovery branch: backup/prototype-0.14.1
- GitHub Actions source artifact: Reborn-0.14.1-Clean-Source
- ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.14.1
- Inverted fan, +0.90 open spacing, compact/lowered arena focus,
  smooth animations and preserved Shift-fusion ordering.
