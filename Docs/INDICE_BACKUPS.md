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
