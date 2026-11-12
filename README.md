# Forbidden Memories Reborn — Prototype 0.27 (development)

Learning 2.0 opponent AI now evaluates fully verified 3–5-card fusion chains,
accounts for additional material cost and announces only engine-confirmed
fusion events with a presentation effect. Legacy benchmark policy is retained.

[0.27 gameplay, tests and local Unity validation](Docs/PROTOTYPE_0_27.md).

---

# Forbidden Memories Reborn — Prototype 0.26 (development)

Multi-step verified fusion recommendations (2–5 hand materials or own field
monster + up to 5 hand materials), full ordered selection, keyboard-scrolling
modal and deterministic Core/Unity EditMode regression coverage. Built on 0.25.1.

[0.26 feature notes and validation](Docs/PROTOTYPE_0_26.md).

---

# Prototype 0.25 — Assistente de fusões reais e estabilidade

O Reborn agora inclui um **assistente de fusões verificadas [F]**.
Ele lista receitas da mão e combinações com monstros próprios no
campo, sem inventar cartas/efeitos, e abre a seleção Guardian/Face/ATK-DEF
sem consumir materiais até a confirmação. As setas preservam a ordem
marcada por ↑, e Z/duplo clique não descartam automaticamente
múltiplas cartas sem nenhuma fusão validada.

Também corrigimos o Face Down da carta final numa sequência de
campo com fusão intermediária seguida de falha, distinguimos
**TROCA SEM FUSÃO** no modal e reduzimos execuções GitHub Actions
duplicadas.

Para validação local, sem depender do Actions:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\VALIDAR_REBORN.ps1
```

[Guia detalhado, comandos e testes da versão 0.25](Docs/PROTOTYPE_0_25.md)

---

# Prototype 0.24.3 — Correção de referências nos testes Unity

Corrigidos os três erros de compilação `CS0234` reportados
em `KeyboardInitializationTests.cs` e `KeyboardPositionTests.cs`.
O assembly `Reborn.Core.Tests` agora referencia
`Reborn.Presentation` e `Unity.InputSystem`, além de `Reborn.Core`.
O projeto mantém os recursos da 0.24.2; nenhuma regra de jogo foi alterada.

[Instruções para testar no Unity 6000.3.28f1](Docs/PROTOTYPE_0_24_3.md)

---

# Prototype 0.24.2 — Estabilidade, Q/E exatos e diagnóstico de fusões

Esta entrega corrige a diferença entre Q e E depois de posicionar o
monstro (**Q = ATK**, **E = DEF**, sem alternância involuntária) e
introduz um diagnóstico **offline** para saber se a ISO/DUELTRACE
realmente forneceu receitas de fusão ao Reborn.

No Windows, na raiz do projeto:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\DIAGNOSTICAR_FUSOES.ps1
```

[Guia da versão 0.24.2](Docs/PROTOTYPE_0_24_2.md) ·
[Protocolo de qualidade das próximas entregas](Docs/WORKFLOW_RELEASE_GATES.md)

---

# Prototype 0.24.1 — Correção de inicialização no Unity

Corrigido o erro `UnityException: GetString is not allowed to be called
from a MonoBehaviour constructor`, que provocava um
`NullReferenceException` no `ArenaHud.Initialize`.
Preferências do teclado passam a ser carregadas apenas em
`ArenaHud.Awake()`. As funções da versão 0.24 foram preservadas.

[Diagnóstico e teste no Unity](Docs/PROTOTYPE_0_24_1.md)

---

# Prototype 0.24 — Teclado completo, fusoes e invocacao classica

O Reborn pode ser jogado por mouse ou teclado remapeavel. O jogador
marca fusoes em ordem com CIMA, desmarca com BAIXO e pode fazer
duplo clique para enviar uma carta ao primeiro slot realmente livre.
Guardian Star, Face Up/Down e escolha visual de ATK/DEF acontecem
ANTES de baixar a carta; Q/E gira a carta e Z confirma. X cancela
sem consumir materiais.

Equip pode ser aplicado diretamente sobre monstro compativel ou
setado Face Down acima de seu monstro na mesma coluna (zona de
suporte correspondente). A entrada em campo ganhou uma curva suave,
e fusoes confirmadas geram rastros luminosos individuais de
material. Nenhuma receita sem origem validada e inventada.

[Guia do Prototype 0.24, tabela de teclas e verificacoes](Docs/PROTOTYPE_0_24.md).

---

# Prototype 0.23 — Equips da ISO e subtipos reais

A tabela privada original do PS1 já incluía os alvos de Equip, mas
faltava convertê-los para o Unity. O Reborn agora gera o
`equip_rules.json` automaticamente e permite reexportar as tabelas
existentes sem nova leitura do disco, preservando a conferência SHA-256.
Corrigi também a extração de `typeLabel` para que os bônus de terreno
funcionem em todos os monstros importados da ISO.

**Com dados antigos:** execute `tools/EXPORTAR_EQUIPS.ps1`.
Para obter `typeLabel`, reimporte a imagem pelo importador atualizado.

[Guia e limites do Prototype 0.23](Docs/PROTOTYPE_0_23.md).

---

# Prototype 0.22 — Goblin Fan, Simochi e IA de suportes

Goblin Fan (#687) reflete dano de magia direta, e Bad Reaction to Simochi
(#688) transforma recuperação de LP em dano ao lançador. Ambas são
armadilhas automáticas quando setadas em turno anterior. A IA também
começa a ativar magias e equipamentos próprios já preparados.

[Regras, limites e testes do Prototype 0.22](Docs/PROTOTYPE_0_22.md)

---

# Prototype 0.21 — Terrenos e armadilhas originais

**Terrenos:** Forest, Wasteland, Mountain, Sogen, Umi e Yami alteram o campo
global e modificam ATK/DEF conforme os tipos e as penalidades documentadas.
**Traps:** House of Adhesive Tape, Eatgaboon, Bear Trap, Invisible Wire,
Acid Trap Hole e Widespread Ruin disparam automaticamente quando o ATK do
atacante está dentro do limite. O atacante é destruído antes do combate;
a armadilha elegível de menor limiar tem prioridade.

[Guia e validação do Prototype 0.21](Docs/PROTOTYPE_0_21.md)

---

# Prototype 0.20 — Equips do Forbidden Memories (PS1)

Equipamentos agora aumentam ATK/DEF de monstros compativeis, somam bonus,
afetam combate e desaparecem do campo apos a ativacao, como no PS1.
Megamorph (#657) e Bright Castle (#668) funcionam com compatibilidade
universal documentada. Os demais podem ser acrescentados via tabela
local verificada com SHA da ISO; efeitos sem comprovacao permanecem
desabilitados. Equip pode ser aplicado da mao ou preparado Face Down
para ativacao posterior, mediante confirmacao visual.

[Guia, limites, IDs suportados e testes da 0.20](Docs/PROTOTYPE_0_20.md).

---

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
