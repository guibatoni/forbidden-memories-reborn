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
