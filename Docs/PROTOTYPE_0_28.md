# Forbidden Memories Reborn — Prototype 0.28

## Efeitos de fusão adversários e ritmo visual consistente

Continuação da 0.27. Não altera câmera, cartas, IA, fases, LP ou regras do duelo.

### Novidades

- Rastros de fusão procedurais agora localizam também as cinco zonas do oponente.
- A fusão da IA com cartas da mão gera luzes de várias origens do lado adversário, sem exibir nomes ou artes das cartas ocultas.
- Fusões com um monstro já em campo animam dois materiais até a zona de destino.
- A cerimônia começa somente quando os eventos do motor confirmam que houve fusão real.
- Quando animações estão ligadas, o diretor do oponente aguarda ao menos 1,12 segundos antes da ação seguinte para evitar que os rastros/cerimônia sejam interrompidos pelo próximo movimento.
- Com animações desligadas, a pausa permanece reduzida e respeita a velocidade configurada.

### Testes e limites

- A CI usa verificador estático, sem licença Unity, para proteger lado dos rastros, eventos reais e duração mínima.
- Execute tools/VALIDAR_REBORN.ps1 e depois abra a Game View no Unity 6000.3.28f1.
- Teste dois e três materiais na mão do oponente, fusão com carta em campo, suportes setados, ataques consecutivos e velocidade adversária acelerada.
- Teste também com animações desligadas e resoluções 16:9 e 9:16.
- Não é possível atestar a experiência visual ou a compilação dos scripts MonoBehaviour apenas com os testes do GitHub Actions atuais.

**Importante:** imagens, binários e tabela de fusões da ISO original permanecem privados e não são publicados.
