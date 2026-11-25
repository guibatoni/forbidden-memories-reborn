# Prototype 0.18.1 — manter Face Down ao escolher ATK ou DEF

## Erro corrigido
Em `MonsterOnField.SetPosition()`, selecionar ATK ou DEF modificava
`Position` **e** forçava `FaceDown = false`. O Unity apenas refletia essa
alteração do estado: a carta virava para cima antes do combate.

Agora a orientação e a visibilidade são independentes.

1. Escolher **Face Down** no diálogo antes de colocar o monstro
   grava `FaceDown = true`.
2. Escolher ATK ou DEF no diálogo posterior modifica somente `Position`.
   A carta continua com o verso visível na mesa.
3. Q/E alternam ATK e DEF quantas vezes permitido pelas regras,
   sem revelar monstros ocultos. Repetir a mesma posição não provoca
   nenhuma alteração.
4. O botão de iniciar batalha ou o comando Espaço também não revelam.
5. Quando o monstro realmente **ataca**, `TryAttack()` revela o atacante.
   Se um monstro oculto é **atacado**, `TryAttack()` revela o defensor
   antes de resolver o combate.
6. Prévia/realce de alvo não expõe a identidade de monstros adversários
   ocultos. Após atacar, a posição do atacante continua travada
   até o próximo turno, conforme o Prototype 0.15.

## Verificação
- NUnit: monstro recém-setado e monstro de turno posterior; mudança
  de orientação, tentativa redundante e revelação no ataque.
- NUnit: oponente em DEF oculto muda para ATK oculto, é atacado
  e torna-se visível ao sobreviver.
- Smoke C# na CI: jogador e IA mantém ocultação ao trocar ATK/DEF.
- `tools/check_hidden_stance.py`: valida que a única rotina de revelação
  está associada ao combate, não a `SetPosition()`.
- Unity 6000.3.28f1: posicionar carta Face Down, escolher ATK, observar
  o verso; trocar Q/E, observar verso; atacar e observar a face. Depois
  repetir como defensor e com o adversário.

**Limite**: o GitHub testa o Core e contratos de código, mas não
executa a câmera, o Game View ou a animação real do Unity. Esta etapa
permanece em PR de rascunho até validação visual.

Não altera o leque, as animações de compra, o seletor dos quatro
decks nem o catálogo de cartas. A versão 0.18 anterior é preservada.
