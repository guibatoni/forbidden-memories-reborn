# Prototype 0.4.2 — Visualização passo a passo de ataques e turnos da IA

## Escopo implementado

- `SimpleOpponentAi` fornece decisões puras de jogar carta e atacar, sem aplicar o resultado até o momento escolhido pelo diretor.
- `OpponentTurnDirector` usa coroutine para apresentar o turno inimigo em etapas, com mensagens de atividade entre elas.
- `AttackProjectile` exibe projétil esférico, trilha e partículas ao atingir a zona alvo.
- `ArenaFactory` permite animar a entrada da carta do oponente e realçar o atacante, mantendo as zonas fixas.
- `ArenaHud` exibe o estado "Turno da IA", o **registro das últimas cinco ações** e o botão **Ritmo IA** (Detalhado, Normal, Rápido).
- `ArenaInteraction` ignora cliques enquanto o adversário está jogando.
- `RebornGame` apresenta os ataques do jogador e atrasa apenas o efeito visual de destruição até o impacto.

## Roteiro de validação no Unity 6.3 LTS

1. Abrir o projeto e confirmar Console sem erros vermelhos.
2. Clicar em Play, entrar no duelo experimental e colocar um monstro.
3. Encerrar turno; observar espera, escolha de carta, entrada no campo, seleção de atacante, efeito de ataque, perda de LP/destruição e passagem de turno.
4. Trocar ritmo durante a vez do adversário; conferir que as etapas não são puladas no modo detalhado.
5. No turno do jogador, iniciar combate, selecionar atacante e alvo; conferir o projétil e depois o efeito de destruição.
6. Desligar animações; confirmar que o motor mantém resultados e a IA ainda executa em sequência.
7. Reiniciar o duelo e repetir para detectar referências antigas ou VFX pendentes.
8. Confirmar que há cinco zonas de monstros e cinco de suporte por jogador e que a câmera não mudou.

## Limites e próximos passos

- Não há modelos 3D de monstros, efeitos oficiais de cartas ou dados importados da ISO original.
- Os testes .NET do GitHub verificam o motor puro de regras e o planejador de ações; o comportamento visual deve ser validado no Unity Editor.
- Evoluir para combate animado com efeitos específicos por tipo de ataque, som, popups de LP, preview de cartas, eventos de fusão, Guardian Stars, e posteriormente campanha e oponentes oficiais mediante dados autorizados e verificados.