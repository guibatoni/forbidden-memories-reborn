# Forbidden Memories Reborn — Prototype 0.26

## Fusão encadeada fiel a receitas verificadas

Esta etapa é construída sobre a 0.25.1, sem alterar a branch main, o projeto DUELTRACE ou os dados privados do disco.

### Funcionalidades

- Pressione **F** durante a fase principal para consultar caminhos de fusão reais.
- O assistente lista **2 a 5 cartas da mão** ou **um monstro próprio no campo seguido de 1 a 5 cartas da mão**. Todas as etapas precisam resultar em uma fusão existente no catálogo importado.
- Sequências que fracassam em qualquer etapa não são anunciadas como fusão. A substituição manual intencional continua possível pela mecânica já existente.
- As opções preservam os índices e a ordem física completa dos materiais; o mesmo comando de colocação existente executa a jogada após Guardian Star, Face Up/Down e ATK/DEF.
- A lista prioriza sequências curtas e, em caso de empate, resultados de maior ATK; a seleção permanece visível ao navegar pelas setas.
- A tecla F1 não abre o menu de teclado por trás do assistente. Feche o assistente com F/X antes de remapear as teclas.

### Comportamento seguro

- Consultar, rolar, escolher ou cancelar uma opção não consome cartas, LP, eventos, turno ou qualquer material.
- As recomendações passam pela validação de fase, lado atual, zonas disponíveis e Guardian Star do motor de partida.
- O UI revalida a sequência **completa** antes de iniciar o wizard, para evitar uma escolha desatualizada.
- Somente o catálogo já carregado pode gerar resultados. Sem receitas importadas, a lista permanece vazia.
- A busca tem limite natural de cinco cartas na mão; não existe consulta a cartas ocultas do rival nem geração de receitas a partir de nome/tipo/ATK.

### Testes

1. Core Smoke .NET: cadeias de 2, 3 e 4 materiais, chain de campo com três fusões sucessivas, previsões sem efeitos colaterais e colocação efetiva.
2. NUnit EditMode no Unity: escolha por modal de cadeia com três cartas, ordem preservada, abertura e fechamento sem gastar mão/eventos.
3. Contratos estáticos v0.25 e v0.26 executados na CI e no validador Windows.

Na raiz do projeto da branch 0.26, execute no PowerShell:

    powershell -ExecutionPolicy Bypass -File .\tools\VALIDAR_REBORN.ps1

Depois, abra **Unity 6000.3.28f1**, execute Window > General > Test Runner > EditMode e rode FusionAssistantTests, KeyboardInitializationTests e KeyboardPositionTests. No Game View teste F, ↑/↓, Z, cancelamento X, três ou mais materiais e a mesma receita escolhida manualmente.

### Limitações honestas

- A CI não abre nem compila a Game View/Unity Editor: a compilação do HUD, a rolagem, a escala 9:16 e as animações precisam ser verificadas localmente.
- A ISO Original e qualquer arte/dados licenciados continuam fora do repositório público.
- O assistente depende de receitas importadas e verificadas; 722 cartas no catálogo não significam que as fusões foram extraídas.
- Não houve alteração na câmera fixa, arena, leque de cartas ou lógica de aprendizado da IA nesta entrega.
