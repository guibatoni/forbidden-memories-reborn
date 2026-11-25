# Prototype 0.22 — Armadilhas reativas e suporte da IA

Continuidade da 0.21 (Terrenos + seis Traps de ataque).

## Duas armadilhas do PS1 com efeito verificado

| ID | Card | Disparo automático |
| --- | --- | --- |
| 687 | Goblin Fan | Quando o adversário tenta causar dano LP por magia direta: redireciona todo o dano da magia ao lançador |
| 688 | Bad Reaction to Simochi | Quando o adversário tenta recuperar LP por magia: a recuperação é convertida em dano ao próprio lançador |

Exemplos: se o jogador usar **Sparks (#343, 50 de dano)** e o adversário
tiver Goblin Fan setado, o jogador perde 50 LP e o oponente não perde LP.
Se o jogador usar **Red Medicine (#339, recupera 500 LP)** contra uma
Simochi setada, o jogador perde 500 LP em vez de recuperar.

Essas armadilhas disparam automaticamente apenas se estiverem
viradas para baixo, setadas em um turno anterior e no lado que seria
afetado pela magia. A carta ativada vai ao cemitério e o evento de
Trap é público **só no momento do disparo**. Sem trap, a magia continua
comportando-se exatamente como na 0.19.

## IA usa seus suportes já preparados
No começo de seus turnos, a IA:
1. Procura magias que já estavam setadas em turnos anteriores e têm
   rotina verificada, mostrando mensagem e intervalo visual antes da
   ativação.
2. Procura Equip setado com monstro próprio revelado e compatível,
   aplicando o equipamento ao alvo após confirmação interna.
3. Continua a lógica de selecionar e baixar sua carta obrigatória e
   executar o combate. Uma magia que termina o duelo interrompe
   a sequência e libera a UI.

Nada disso dá à IA visão de cartas ocultas do jogador. Também não
ativa arbitrariamente traps, outros supports nem efeitos não validados.

## Segurança e limitações
- Reverse Trap (#689) e Fake Trap (#690) **não têm** efeito presumido.
  Suas condições exatas ainda requerem validação in-game, especialmente
  Fake Trap, descrito de maneiras inconsistentes em fontes externas.
- A reação é somente à operação de spell cura/dano verificada, não
  qualquer evento de LP (por exemplo dano de combate).
- O modelo é estritamente ativo em perfis Original do PS1; outros mods
  requerem catálogos específicos por SHA e auditoria.
- Importar `effectText` ou descrições não autoriza execução de scripts.

## Testes
- Dotnet Core Smoke: ativação automática, timing de turnos, carta
  enterrada uma vez, proteção contra efeitos não relacionados.
- NUnit de editor: Goblin Fan e Simochi em turnos diferentes, armadilhas
  ocultas não ativadas por magia de efeito diferente.
- Testes estáticos de CI: eventos e limites de dados, IA preparada,
  nenhuma simulação de efeitos desconhecidos.

## Verificação visual no Unity
No Unity 6000.3.28f1, usar um conjunto local de teste com carta #687 e
#688 setadas: ativar Sparks (#343) e Red Medicine (#339), observar
-50/-500 LP no **lançador**, partículas na zona do Trap, cemitério e
mensagem do HUD. Testar também a IA com uma magia Face Down após
passar o turno. Testes de Core não substituem execução em Game View.

Fontes de referência:
- https://ftp.yugipedia.com/wiki/Goblin_Fan_%28FMR%29
- https://ftp.yugipedia.com/wiki/Bad_Reaction_to_Simochi_%28FMR%29
- https://www.neoseeker.com/yugioh-forbidden/faqs/59151-yu-gi-oh-fm-b.html
