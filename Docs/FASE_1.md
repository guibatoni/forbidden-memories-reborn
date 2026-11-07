# Fase 1: fundação e arena (prototype 0.1)

## Metas

1. Projeto Unity 6.3 LTS com estrutura independente.
2. Protótipo de arena 3D procedimental com duas fileiras por jogador.
3. Mão com cinco cartas fictícias e interação por mouse.
4. Separação entre estado e apresentação.
5. Testes de validação mínima e caminho para build Windows.

## Decisões técnicas

- `Reborn.Core`: C# puro sem dependência de Unity.
- `Reborn.Presentation`: objetos, física (raycast), câmera, cenas e HUD via IMGUI.
- `RebornEditorSetup`: cria a cena e configura as cenas de build no editor.
- Dados demonstrativos são programáticos. Em futura fase, JSON versionado e validação de proveniência.
- O projeto declara URP 17.3.0, mas a inicialização do asset URP precisa ser configurada no Editor: nesta fase os materiais têm fallback para Standard; use o assistente URP do editor ao ajustar o visual.

## Critérios de aceite no seu Windows

- [ ] Pacotes resolvidos e scripts compilados sem erros no Console.
- [ ] Reborn > 01 cria e salva `Assets/Scenes/Prototype.unity`.
- [ ] Play exibe arena 3D, 20 zonas, cinco cartas na mão e HUD.
- [ ] Câmera gira com botão direito e faz zoom com a roda.
- [ ] Carta de monstro vai para uma zona de monstro; suporte não aceita monstro.
- [ ] Reposição da mão funciona sem exceder cinco cartas.
- [ ] EditMode Test Runner executa os seis testes do Core.
- [ ] Windows Build Support gera EXE quando acionado o menu 02.

## Próxima fase

Motor de regras determinístico com testes de regressão: mão, fusões em cadeia, Guardian Stars, terrenos, estados de batalha, pontos de vida e condições de vitória. Somente depois acoplar efeitos 3D e IA de oponente.
