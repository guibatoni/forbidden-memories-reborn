# Prototype 0.3 — Card Interaction & Event Foundation

## Contrato de comportamento

- **Camera**: `OrbitCamera.cs` NAO foi alterado em relacao ao 0.2/0.1; angulo e zoom permanecem os aprovados.
- **Zonas**: 5 `Monster` e 5 `Support` por jogador, 20 posicoes visuais no total. `Equip`, `Ritual`, `Spell`, `Trap` vao a `Support` nesta demo.
- **Regras**: `DuelActionRules` e o ponto unico de verificacao de colocacao; as zonas realcadas usam a mesma regra que `TryPlace`.
- **Eventos**: `DuelSession` registra `Reset`, `CardDrawn`, `CardPlaced`, `CardRemoved` (apenas teste) e `SandboxTurnAdvanced` com sequencia.
- **Visual**: carta da mao tem hover e arraste; card de campo entra com curva de movimento sem deslocar camera. Pode desligar a animacao no HUD.
- **Destruicao demonstrativa**: botao `Remover teste: ON` permite remover uma carta do jogador com particulas. NAO e resultado de combate, NAO altera pontos de vida, nem adiciona itens ao cemiterio.
- **Conteudo**: cartas ficticias para testes. Importador JSON da 0.2 preservado; nao inclui dados do disco original.

## Verificacao manual no Editor

1. Backup do projeto antes de instalar; editor fechado ao copiar arquivos.
2. Abra a cena ja existente. **Nao e preciso gerar outra cena**; a base e procedural quando Play comeca.
3. Confirme ausencia de erros no Console. Se houver, copie a primeira mensagem e stack trace.
4. Com o modo Play ativo, passe o mouse sobre as cartas da mao: deve haver elevação suave.
5. Clique em um monstro: SOMENTE as zonas de monstro livres do seu lado devem acender.
6. Clique na primeira zona iluminada: o monstro deve se deslocar da mao ate a zona e permanecer la.
7. Clique em uma magia: SOMENTE as zonas de suporte livres devem acender.
8. Arraste uma carta de suporte ate uma zona valida e solte. Soltar fora deve devolver a carta a mao.
9. Desligue `Animacoes: ON` e teste outra carta. O posicionamento deve continuar funcionando, sem transicao.
10. Ative `Remover teste: ON`, clique na zona ocupada do jogador: a carta deve desaparecer e surgir um efeito breve. Desative o modo antes de continuar.
11. Confirme que oponente nao pode receber carta do jogador e que as cinco zonas de cada fileira sao respeitadas.
12. Em `Window > General > Test Runner`, aba Edit Mode, execute os testes de `Reborn.Core.Tests`.

## Arquitetura

```text
Clique/arraste -> ArenaInteraction
    -> DuelSession.CanPlace / DuelActionRules
    -> DuelSession.TryPlace (modifica os dados)
    -> DuelEvent (historico em memoria)
    -> Changed -> ArenaFactory.Refresh
    -> CardMotion / CardView / ZoneView
```

O desenho separa transicoes da simulacao: futuramente `DuelSession` deve ser evoluido para comandos tipados, duracao de turnos correta, fusoes, efeitos, alvos, batalha e IA. **Nao chamar o sandbox de motor final do Forbidden Memories.**

## Validacao e limitacoes

- Codigo fonte gerado e revisto estaticamente fora do Windows; compilacao Unity e testes NUnit **nao executados aqui** por falta do Unity Editor.
- Nao ha dados proprietarios embutidos, ISO/BIN, cartas originais, artes ou story script licenciado.
- Nenhuma integracao real com Unity CLI/MCP foi executada; a instalacao local e um trabalho independente.
- O repository GitHub do usuario foi consultado e estava **publico** em 7/nov/2026. Nao publicar o codigo ate mudar a visibilidade, se o objetivo for protege-lo.
