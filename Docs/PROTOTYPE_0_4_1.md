# Reborn Prototype 0.4.1 — correcoes visuais de fim de duelo

Incremento de estabilizacao do Prototype 0.4, a partir da captura de 07/11/2026.

## Mudancas

- Resultado de partida exibido em painel separado, sem sobrepor botoes.
- Comandos de combate ocultados quando a partida termina; somente nova partida, sandbox e animacoes continuam disponiveis.
- Modo de duelo bloqueia selecao/arraste de cartas ao finalizar.
- Rotulos das cartas exibem nomes abreviados para evitar texto sobreposto; nomes completos, IDs e descricoes continuam no painel de detalhes.
- Ajuste das intensidades das luzes que estavam superexpondo colunas, sem modificar a posicao, o campo de visao ou o controlador da camera.
- Estado de fase exibido em portugues.

## Limitacoes e validacao

A revisao e estatica: compilacao da camada Unity e comportamento visual devem ser conferidos no Editor `6000.3.28f1`.
O motor experimental segue sem as regras oficiais completas da ISO e sem suporte para efeitos de cartas especiais.
O GitHub Actions cobre o nucleo C# independente da Unity, nao as interfaces IMGUI.