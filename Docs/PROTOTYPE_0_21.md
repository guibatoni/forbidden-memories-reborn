# Prototype 0.21 — Terrenos globais e armadilhas automáticas

## Terrenos do Forbidden Memories (PS1)

Seis cartas originais, todas do grupo Spell, ativam um terreno
compartilhado pelos dois jogadores. Um novo terreno substitui o anterior.
O cartão vai ao cemitério após a ativação, e seu efeito permanece.

| ID | Terreno | +500 ATK e DEF | -500 ATK e DEF |
|----|---------|----------------|----------------|
| 330 | Forest | Beast, Beast-Warrior, Insect, Plant | — |
| 331 | Wasteland | Zombie, Dinosaur, Rock | — |
| 332 | Mountain | Dragon, Winged Beast, Thunder | — |
| 333 | Sogen | Warrior, Beast-Warrior | — |
| 334 | Umi | Aqua, Fish, Sea Serpent, Thunder | Machine, Pyro |
| 335 | Yami | Fiend, Spellcaster | Fairy |

Os valores são calculados pelo Core apenas para tipos **conhecidos**
da tabela importada no `TypeLabel`. Tipo desconhecido não recebe bônus
inventado. Equip acumulado e Guardian Star continuam sendo calculados
separadamente. Terreno afeta ATK, DEF e ataques diretos.

Para usar: jogue a carta de campo Face Up em uma das zonas de Spell; ela
ativa e é enterrada, persistindo como `CurrentTerrainCardId`. Se ela
estiver setada Face Down, ative no turno posterior pelo botão de magia
setada. A tabela só é habilitada no perfil `original-ps1*`.

## Armadilhas de reação a ataque

| ID | Trap | Ativa se ATK atacante for... |
|----|------|----------------------------|
| 681 | House of Adhesive Tape | até 500 |
| 682 | Eatgaboon | até 1.000 |
| 683 | Bear Trap | até 1.500 |
| 684 | Invisible Wire | até 2.000 |
| 685 | Acid Trap Hole | até 3.000 |
| 686 | Widespread Ruin | até 25.500 |

A armadilha deve estar setada no campo adversário, Face Down, em
turno anterior. A ativação é **automática**, durante um ataque real.
O ataque não causa dano; o atacante é destruído, a armadilha vai ao
cemitério e o monstro defensor **não é revelado** caso o combate seja
interceptado.

Com várias armadilhas possíveis, ganha o menor limite que abrange
o ATK efetivo do atacante. A prioridade replicada é a da rotina
do executável do PS1 (offset dos seis bytes originais).
A prévia de combate NÃO consulta armadilhas ocultas nem expõe nomes.

Ao tentar atacar com ATK maior que o limite da armadilha, ela continua
setada e o combate procede normalmente.

## Apresentação
- Visor de terreno ativo (Normal/Forest/Wasteland/Mountain/Sogen/Umi/Yami).
- Mudança de terreno com varredura procedural de partículas, sem trocar
  imagens ou a posição fixa da câmera.
- Disparo de Trap com partículas vermelhas sobre sua zona de suporte.
- O texto do resultado de ataque mostra **interceptação** em vez da
  previsão normal de dano se a Trap ativar. Jogador e IA recebem
  mensagens simétricas.
- A mão em leque, equipes cumulativas, Face Down 0.18.1, fusões,
  rituais e magias 0.19 continuam presentes.

## Integridade e limitações
São regras para IDs originais. Mods que alterem as regras da ROM não
devem reutilizar automaticamente esses efeitos. Equip e magias textuais
de origem desconhecida continuam não executáveis.

As outras quatro Traps do PS1 ainda não estão ativas nesta etapa:
Goblin Fan (#687), Bad Reaction to Simochi (#688), Reverse Trap (#689),
Fake Trap (#690). Elas são reações diferentes e precisam ser
implementadas independentemente.

## Testes e verificação
CI C# simula terrenos de ambos os jogadores, penalidades negativas,
ataques diretos, limiares de armadilhas, menor limiar elegível e
interceptação com zero dano. NUnit cobre os mesmos mecanismos.

Teste visual no Unity 6000.3.28f1:
1. Jogue Mountain e confirme Dragon +500; troque para Yami e veja
   bônus migrar para Spellcaster, nos dois lados.
2. Set Bear Trap e Widespread Ruin em turnos diferentes. Ataque com
   monstro de 1.400 ATK. Bear Trap deve disparar e Widespread ficar.
3. Configure uma Trap de limite menor que o ATK do atacante: ela deve
   permanecer oculta, o ataque resolve normalmente.
4. Confirme que a prévia não revela Trap e que as animações de
   ataques interceptados nunca anunciam dano fictício.
5. Inspecione Console no Unity: não devem existir exceções e o
   projeto precisa compilar com Particle System habilitado.

Fontes usadas para a validação inicial:
- https://yamiyugi.io/datos/wiki/pt/reglas/
- https://github.com/Unchiga/Yu-Gi-Oh-Forbidden-Memories-Recompiled/blob/master/notes/modding-tutorial-evidence.md
- https://yugiohforbiddenmemories.com/cartas/acid-trap-hole/
