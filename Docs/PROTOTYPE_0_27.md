# Forbidden Memories Reborn — Prototype 0.27

## IA Learning 2.0: cadeias de fusão reais e apresentação em etapas

A 0.27 continua a 0.26, mantendo a main, a câmera fixa, o leque e os dados locais intactos.

### Evoluções

- A IA Learning 2.0 passa a avaliar fusões verificadas de **3 a 5 cartas da própria mão**, além das opções já existentes de 1 ou 2 cartas.
- Cada caminho vem da mesma fonte oficial em tempo de execução que abastece o assistente F: o FusionCatalog importado. Não são inventadas receitas.
- Uma penalidade proporcional ao número de cartas extras evita gastar a mão inteira por ganhos pequenos. A tática FusionFocused aceita uma penalidade menor.
- O modo Legacy mantém a decisão antiga de 1 ou 2 cartas, preservando a base de comparação de comportamento dos testes de IA.
- O diretor do oponente continua exibindo cada ação em sequência. Após colocar cartas, agora dispara a animação de fusão **somente** quando o motor registra eventos reais Fusion.
- Quando a criatura é setada virada para baixo, o texto não revela a identidade do monstro.

### Validação automatizada

- Teste .NET determinístico: versão Legacy não escolhe cadeia longa; Learning 2.0 escolhe cadeia de três materiais quando há vantagem clara.
- O mesmo teste comprova que consultar o plano não muda eventos ou cartas e que o commit segue DuelMatch.TryPlay.
- Verificador de código executado na CI e em tools/VALIDAR_REBORN.ps1.

Na raiz do projeto (PowerShell):

    powershell -ExecutionPolicy Bypass -File .\tools\VALIDAR_REBORN.ps1

### Validação necessária no Unity 6000.3.28f1

1. Importar localmente um perfil com receitas reais e conferir a contagem pelo assistente F.
2. Duelo contra a IA com uma fusão de três materiais realmente disponível: observar seleção, invocação e efeito.
3. Verificar que a IA não mostra conteúdo de sua mão escondida e não anuncia identidade de monstros Face Down.
4. Avaliar visualmente ritmo de invocações e ataques com animações ligadas e desligadas.
5. Executar os testes EditMode e conferir o Console completo.

**Limites:** a CI compila o núcleo .NET, mas não a Game View nem os scripts de apresentação do Unity. O resultado visual e as receitas da ISO dependem de validação local.
