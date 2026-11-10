# Reborn Prototype 0.20 — Equips originais do PS1

## Gameplay novo
O equip de Forbidden Memories nao e um Equip Spell do TCG moderno.
Ele nao permanece ocupando uma zona de magia apos ser ativado.
O proprio monstro recebe o bonus permanente enquanto estiver em campo;
a carta usada vai ao cemiterio. O combate passa a calcular os valores
efetivos de ATK e DEF com os bonus acumulados e Guardian Stars.

**Primeiros IDs do PS1 implementados e confirmados:**
- #657 **Megamorph**: +1000 ATK / +1000 DEF em qualquer monstro.
- #668 **Bright Castle**: +500 ATK / +500 DEF em qualquer monstro.

Os **outros 32 Equips** (Dragon Treasure, Legendary Sword, Cyber
Shield, etc.) ainda requerem listas de alvos especificos e valores
confirmados. Na ausencia desses dados, NENHUM desses 32 recebe uma
compatibilidade inventada. Importadores e extensoes podem usar o
arquivo local abaixo para adicionar as tabelas verificadas.

Fontes para revisao das regras:
- https://yugioh.fandom.com/wiki/Yu-Gi-Oh!_Forbidden_Memories
- https://github.com/Unchiga/Yu-Gi-Oh-Forbidden-Memories-Recompiled/blob/master/tools/pc/fm_editor/README.md
- https://www.neoseeker.com/yugioh-forbidden/faqs/59151-yu-gi-oh-fm-b.html

## Jogando
1. Selecione **uma carta Equip** na mao e clique/arraste-a sobre seu
   monstro virado para cima. O Reborn compara os IDs reais com o
   catalogo de compatibilidade.
2. Aparece uma janela dedicada com o nome da carta, do monstro, bonus
   exato e dois botoes: **Confirmar Equip** ou **Cancelar**.
3. Cancelar nao gasta nenhuma carta. Confirmar consome a unica jogada
   de baixar carta do turno, transfere o Equip para o cemiterio e aplica
   o bonus ao monstro sem virar/rotacionar o alvo.
4. Equips se acumulam se aplicados em turnos posteriores. O HUD e a
   mesa exibem o ATK/DEF novo; a previa do combate usa os bonus reais.
5. Alternativamente, coloque o Equip Face Down em uma zona de suporte.
   Num turno futuro, clique nesse Equip setado e depois no seu monstro
   face-up para confirmar a ativacao. A ativacao do Equip ja setado nao
   e uma segunda jogada de baixar carta.
6. Nao se pode equipar um monstro virado para baixo. Trocar ATK/DEF
   com Q/E nao revela a carta nem aplica Equip sem validacao.
7. Equipamento descartado, monstro eliminado/substituido ou fusao que
   substitui a carta antiga: o bonus nao e transferido ao monstro novo.

## Tabelas de compatibilidade privadas
A mesma ISO importa as 722 cartas. Em outra etapa, o DUELTRACE/forensic
lab podera gerar os pares de compatibilidade completos e confirmados.
O Reborn aceita agora opcionalmente:

    Assets/StreamingAssets/RebornContent/equip_rules.json

Schema local (exemplo SINTETICO de compatibilidade; nao identifica
uma receita comprovada de jogo real):

    {
      "schema": 1,
      "source": {
        "edition": "original-ps1",
        "discSha256": "COLE_EXATAMENTE_O_SHA256_DA_IMAGEM_ORIGINAL"
      },
      "equips": [
        {
          "cardId": 315,
          "verified": true,
          "attackBonus": 500,
          "defenseBonus": 500,
          "universal": false,
          "targets": [1, 39]
        }
      ]
    }

ATENCAO: os numeros de alvos acima sao uma estrutura de exemplo, nao
um mapa validado. Altere somente apos conferir o jogo e a tabela do
seu disco. Os IDs 657/668 sao adicionados pelo motor e nao devem ser
duplicados no arquivo. O loader exige SHA e edition compativeis,
`verified=true`, tipos de carta validos e IDs de monstro existentes.
Se qualquer campo falhar, ignora TODO o arquivo e mantem apenas
as duas regras conhecidas, sem modificacoes parcialmente aplicadas.

O catalogo nao usa nomes, descricoes nem subtipos do DUELTRACE como
substitutos para listas de compatibilidade. Mods com regras diferentes
precisam de catalogos dedicados e identificacao especifica.

## Alteracoes no codigo
- `VerifiedEquipCatalog`: tabela estrita de IDs compativeis.
- `DuelMatch.Equips`: prevalidacao pura e execucao atomica da jogada,
  e ativacao posterior de Equip setado.
- `MonsterOnField`: bonus cumulativo, contagem e atributos efetivos.
- `CombatForecast`: ataque direto, ATK/DEF e Guardian consideram bonus.
- `ArenaHud.EquipChoice`: confirmacao de alvo com cancelamento seguro.
- `OriginalEquipLoader`: arquivo local de compatibilidade por SHA.
- `ArenaFactory`: efeito 3D procedural original e indicadores visuais.
- `RebornGame`: eventos conectados sem alterar a camera ou o leque.

## Validacao
CI compila o Core e executa testes deterministas: +500/+1000, alvos
permitidos/rejeitados, multiplos Equips, dano de combate, ataque
direto, Face Down, ativacao apos turno, duplicidade de acao e rollback
quando o Equip ou o alvo e invalido.

A Unity 6000.3.28f1 ainda deve ser testada visualmente. Conferir que
o clique/arrasto abre a janela, cancelar nao consome cartas, os efeitos
aparecem no alvo, os valores de ATK/DEF sao legiveis e nao existe
nenhum erro de compilacao ou vazamento de dados Face Down.

## Proxima etapa
1. Exportar a lista completa dos 34 Equip e seus alvos diretamente
   da tabela do PS1 / DUELTRACE do usuario, com SHA e verificacao.
2. Criar funcoes para efeitos especiais e interacao com Trap Reverse Trap.
3. Implementar magias de terreno com bonus ao tipo de criatura e
   persistencia durante o duelo.
