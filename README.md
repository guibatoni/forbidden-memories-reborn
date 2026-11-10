# Prototype 0.19 — Efeitos de magias do PS1

O Reborn agora consegue executar 12 efeitos previamente documentados
de magias da edicao Original: cura de LP, dano direto, Raigeki e Dark Hole.
Magias Face Up ativam imediatamente; Face Down podem ser ativadas
posteriormente. Efeitos sem validacao nao executam dados textuais
arbitrarios extraidos do DUELTRACE.

[Controles, lista de IDs e roteiro de testes da 0.19](Docs/PROTOTYPE_0_19.md).

---

# Prototype 0.18.1 — Face Down preservada em ATK/DEF

Corrigido: um monstro colocado virado para baixo permanece oculto
quando é alterado de DEF para ATK (ou vice-versa), inclusive via Q/E.
Ele é revelado apenas ao atacar ou ser atacado.

[Regras e checklist do Prototype 0.18.1](Docs/PROTOTYPE_0_18_1.md)

---

# Prototype 0.18 — Escolha das cartas e 4 decks iniciais

Sequencia visual: selecione as cartas (Shift para fusao), escolha uma zona,
Guardian Star (quando disponivel), face-up/face-down, carta baixada e
ATK/DEF em tela. Nenhuma carta e consumida antes de confirmar.

Quatro decks iniciais alternativos sao sorteados usando os **sete pools
ponderados originais** (16+16+4+1+1+1+1), desde que tenham sido extraidos
localmente da mesma ISO. O PS1 original oferece um sorteio, nao quatro.

[Regras, controles e testes do Prototype 0.18](Docs/PROTOTYPE_0_18.md)

---

# Prototype 0.17.2 — Corrigido o carregamento do catalogo Original

A carta ficticia **Fera do Nilo (p04)** nao deve aparecer como se fosse
um card do jogo PS1. O Reborn agora tenta ler diretamente o perfil Original
do DUELTRACE quando nao existe um export Reborn, e exibe aviso bloqueante
caso nenhum catalogo real seja encontrado.

[Como carregar as cartas do PS1 e as 722 artes locais](Docs/PROTOTYPE_0_17_2_AUTOLOAD.md).

---

# Prototype 0.17.1 — Reaproveitamento do banco DUELTRACE (rascunho)

A ponte local de dados importa nomes, IDs, subtipos, ATK/DEF, receitas revisadas e
textos informativos de efeitos do perfil Original do DUELTRACE. O Reborn usa
molduras e animacoes proprias; o motor nao executa efeitos nao verificados.

Consulte [o guia de importacao DUELTRACE](Docs/PROTOTYPE_0_17_1_DUELTRACE.md).
Para importar no Windows, execute tools/IMPORTAR_DO_DUELTRACE.ps1.

---

# Forbidden Memories Reborn — Prototype 0.17 (em validação)

Esta versão prepara a importação **local e privada** das 722 cartas, descrições
disponíveis, receitas de fusão do disco PS1 (inclusive peculiaridades do
leitor) e rituais de três tributos. O jogo inclui a primeira execução da
mecânica de ritual do PS1 e cerimônias visuais de fusão/ritual inéditas.

- Guia completo: [Prototype 0.17](Docs/PROTOTYPE_0_17.md)
- No Windows, execute: `tools/IMPORTAR_ORIGINAL.ps1 -Disc "C:\\Jogos\\Yu-Gi-Oh! FM Original (Drop x15).bin"`
- O arquivo do disco, textos originais e artes **não** são incluídos no GitHub.
- As descrições exportadas são os textos disponíveis na imagem do jogo; textos
  não significam efeitos já implementados.
- O motor aceita três tributos específicos, incluindo três cópias do mesmo ID.
- Não considerar este projeto compilado/validado no Unity até testes manuais.

---

# Prototype 0.16 — In-field fusion (draft)

Place selected hand card(s) onto an occupied **player monster** to resolve
field-first fusion, using up to five ordered hand materials. Valid recipes
yield the verified monster; failed recipes may replace the field monster.
The resulting card/Guardian Stars are previewed before a legal play. AI can
also perform safe field combinations when its zones are full.

See [0.16 rules, limits and Unity checklist](Docs/PROTOTYPE_0_16.md).

---

# Forbidden Memories Reborn — Prototype 0.15 (draft)

Classic turn controls: mandatory placement before ending turn, Q/E to toggle
ATK/DEF repeatedly until attacking, SPACE to pass explicitly, and enlarged
right-click card inspection. New cards glide into the unchanged inverted fan.

[0.15 controls, limitations and Unity validation](Docs/PROTOTYPE_0_15.md).

---

# Forbidden Memories Reborn — Prototype 0.14.4 (proposed)

Legal battle target aiming: a lightweight gold arc connects the selected
attacker to a valid hovered enemy target, including direct attacks when legal.
Visual feedback only; existing rules, camera and hand interaction unchanged.

[Prototype 0.14.4 verification](Docs/PROTOTYPE_0_14_4.md).

---

# Forbidden Memories Reborn — Prototype 0.14.3

More readable field play: legal placement zones in cyan, a bright hover
outline for valid targets, a gold selected attacker, and a smoother,
arcing card summon with final landing ease. Existing core rules,
adaptive AI and fixed camera remain unchanged.

[0.14.3 field interaction notes](Docs/PROTOTYPE_0_14_3.md).

---

# Forbidden Memories Reborn — Prototype 0.14.2

Improved MTGA-inspired hand focus: the hovered card rises more clearly,
neighboring cards separate smoothly, the arena-focus hand remains compact,
and Shift-fusion selection plus drag targeting stay stable.

[0.14.2 hand hover notes](Docs/PROTOTYPE_0_14_2.md).

---

# Forbidden Memories Reborn — Prototype 0.14.1

**Responsive inverted card fan:** corrected tilt/curvature, more spacing
when hovering the hand (+0.90 tabletop units), compact/lowered layout
when the pointer returns to the arena, and smooth transitions that
preserve dragging and Shift-fusion selections.

[Review the responsive hand changes](Docs/PROTOTYPE_0_14_1.md).

---

# Forbidden Memories Reborn — Prototype 0.14

**Fluent PS1-inspired duel controls:** larger overlapping MTGA-style
fanned hand, numbered Shift-fusion selections, chosen face up/down before
Guardian Star, battle entered automatically on placement, attacker-to-target
clicks, direct attacks to the opposing side and legal ATK/DEF switching.

[Read gameplay and limitations](Docs/PROTOTYPE_0_14.md).

---

# Prototype 0.13.1 — Unity cross-assembly hotfix

Fixes the `MatchSide.MonsterCount` CS1061 error in `ArenaHud.cs`.
Core now exposes the read-only count to the separate presentation assembly.
A new independent C# assembly smoke test guards against regressions.

[Hotfix notes](Docs/HOTFIX_0_13_1.md).

---

# Forbidden Memories Reborn — Prototype 0.13 AI Learning 2.0

Improved risk-aware attack/defense placement, migration-safe persistent
per-strategy outcomes, and controlled same-seed **legacy vs Learning 2.0**
benchmarks. The Unity arena and camera stay unchanged.

[Read the AI Learning 2.0 guide](Docs/PROTOTYPE_0_13.md).

---

# Forbidden Memories Reborn — Prototype 0.12.1 AI Lab MultiSeed

Expanded offline AI evaluation over **10 seeds and 3 synthetic opponent scenarios**
with paired before/after results, per-seed CSV and seed-cluster bootstrap
confidence intervals. Runs alongside existing 0.12 and changes neither
Unity gameplay nor the learning files.

[Read the 0.12.1 lab guide](Docs/PROTOTYPE_0_12_1.md).

---

# Forbidden Memories Reborn — Prototype 0.12 AI Lab

Offline benchmark of the adaptive opponent AI with **seeded synthetic duels**,
before/after training comparison, win/loss/unresolved accounting and an
exported JSON report. This measures adaptation; it does not guarantee better
play. See [Prototype 0.12](Docs/PROTOTYPE_0_12.md).

---

# Forbidden Memories Reborn — Prototype 0.11

**Opponent AI plans multiple attacks:** evaluates visible targets and Guardian
Star outcomes, clears defenses strategically and preserves strong attackers
for potential direct damage. Actual attacks stay sequential and governed
by DuelMatch. See [Prototype 0.11 notes](Docs/PROTOTYPE_0_11.md).

---

# Prototype 0.10 — Tactical Combat Preview

A shared forecast now powers the player's hover preview and the adaptive
opponent's target selection. Read the [Prototype 0.10 notes](Docs/PROTOTYPE_0_10.md)
for battle testing, Guardian Star checks and known limitations.

---

# Prototype 0.9.3 — Battle results explained

The combat motor now uses one non-mutating forecast to calculate and display
effective ATK/DEF, Guardian Star +500, destruction and LP damage.
See [Docs/PROTOTYPE_0_9_3.md](Docs/PROTOTYPE_0_9_3.md).

---

# Forbidden Memories Reborn — Prototype 0.9

**Offline adaptive rival AI:** five changing tactics, rewarded outcomes and a small persistent
profile per opponent. The rules engine stays authoritative, and the camera is unchanged.

See [Docs/PROTOTYPE_0_9.md](Docs/PROTOTYPE_0_9.md). The next milestone will evaluate
deeper turn planning/self-play without needing a cloud LLM for gameplay.

---

# Forbidden Memories Reborn — Prototype 0.8

Opponent pools from private local disc extraction, randomized 40-card practice decks, duelist selector and same-disc SHA validation. The camera, fixed five monster slots and five support slots are unchanged.

The opponent pool and source art are never part of the public repository. Exact original AI, campaign and starter-pool rules are still in progress.

See Docs/PROTOTYPE_0_8.md for the local import command and test steps.

---

# Forbidden Memories Reborn — Prototype 0.6

Protótipo **não oficial**, desenvolvido no Unity 6.3 LTS para Windows, com jogabilidade de duelo clássico e apresentação de cartas 2.5D inspirada na fluidez de jogos de cartas digitais modernos. Repositório independente do DUELTRACE.

## Como abrir

1. Abra a pasta do projeto no **Unity Hub** usando **Unity 6.3.28f1** (`6000.3.28f1`).
2. Se solicitado, habilite o pacote **Input System** e o módulo integrado **Particle System**.
3. Em uma cena vazia, clique em **Reborn > 01 - Gerar cena do prototipo**; o modo Play também possui bootstrap automático.
4. Entre em **Play**, escolha **Duelo experimental 0.6** no painel do sandbox e teste as cartas, a mão e os combates.

## Prototype 0.6 — cartas 2.5D

- **Novo modelo de carta:** moldura e cores por categoria (Monstro, Magia, Armadilha, Equipamento, Ritual), espaço de arte, nome e atributos.
- **Preview ampliado** ao passar o mouse ou selecionar, usando a região esquerda da interface. Mostra ID, tipo, nome, descrição, ATK/DEF e Guardian Stars disponíveis.
- **Artes locais opcionais:** `Assets/Resources/RebornCardArt/<ID>.png`. Sem PNG, um padrão geométrico original é gerado por código; não há artes oficiais incluídas.
- **Privacidade do adversário:** cartas ocultas não revelam seus dados no preview.
- **Compatibilidade:** mantém a câmera aprovada, a interação do mouse e cinco posições de monstros e cinco de suporte por jogador.

## Prototype 0.5 — sistema experimental de duelo

- Motor de turnos, ataques, Guardian Stars, fusões com receitas explícitas e IA heurística simples.
- Jogada da IA em etapas, efeitos de ataques, destruições, alterações de LP e registros visuais.
- O motor da partida é independente de `UnityEngine`. O renderer somente apresenta eventos, sem decidir resultados.

## Estado dos dados originais

**Não incluímos ISO/ROM, cartas oficiais, baralhos originais, diálogos, efeitos ou assets licenciados de terceiros.** O jogo ainda utiliza cartas e oponentes demonstrativos. O JSON de conteúdo verificável pode ser colocado em `Assets/StreamingAssets/RebornContent/original_export.json`; a estrutura é descrita em `Docs/exemplo-esquema-importacao.json`. Essa importação não equivale à engenharia reversa completa da ISO.

## Controle de versões e testes

- Verificações do núcleo C# via GitHub Actions: `tools/Reborn.Core.Smoke`.
- Contratos estáticos de apresentação: `python tools/check_presentation_contracts.py` e `python tools/check_card_visuals.py`.
- Testes de gameplay e renderização ainda precisam ser executados no Unity Editor; os testes estáticos não comprovam resultado visual.
- Backups completos e incrementais: Biblioteca do ChatGPT > `Forbidden Memories Reborn/Backups`.

Leia `Docs/PROTOTYPE_0_6.md`, `Docs/FIDELIDADE_AO_ORIGINAL.md` e `Docs/INDICE_BACKUPS.md` para detalhes e limitações.

> Projeto de estudo e prototipagem. Distribuição comercial com propriedade intelectual de terceiros exige licença/autorização apropriada.


## Prototype 0.7: strategic AI and private disc import

See [Docs/PROTOTYPE_0_7.md](Docs/PROTOTYPE_0_7.md). Imported disc data and images are **never included in the public repository**. The local importer uses an external open-source parser from the user's checkout.
