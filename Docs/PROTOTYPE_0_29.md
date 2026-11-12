# Forbidden Memories Reborn — Prototype 0.29

## Testes EditMode reais no Unity do Windows

Esta etapa oferece um gate que invoca o **Unity Editor real**, diferente dos smoke tests do motor .NET e das verificações estáticas da CI.

### Preparação

- Baixe a branch feature/prototype-0.29-local-unity-gate pelo GitHub e abra a pasta com o Unity Hub.
- Instale exatamente o Unity indicado em ProjectSettings/ProjectVersion.txt (atualmente 6000.3.28f1).
- Feche o Unity Editor **desse projeto** antes de chamar o processo automatizado: o Editor não deve manter a mesma pasta aberta simultaneamente.
- Os dados privados importados, ISOs, imagens e perfis locais não são enviados ao GitHub por este teste.

### Validação rápida do núcleo e contratos

Na raiz do projeto, execute no PowerShell:

    powershell -ExecutionPolicy Bypass -File .\tools\VALIDAR_REBORN.ps1

Executa contratos Python e testes .NET quando instalado; **não** inicia o Unity automaticamente.

### Validação de compilação Unity e EditMode

    powershell -ExecutionPolicy Bypass -File .\tools\TESTAR_UNITY_REBORN.ps1

O script lê a versão Unity do projeto, encontra o editor instalado pelo Unity Hub e inicia o Test Runner com -batchmode -runTests -testPlatform EditMode -assemblyNames Reborn.Core.Tests. A execução produz relatório NUnit XML e log, sem -quit. O comando **falha** se o Editor retornar erro, se o XML não existir, se nenhum teste tiver executado, ou se houver qualquer teste com falha ou erro.

Se o Unity estiver em um caminho personalizado:

    powershell -ExecutionPolicy Bypass -File .\tools\TESTAR_UNITY_REBORN.ps1 -UnityExe 'D:\Unity\Editor\Unity.exe'

### Onde ficam os relatórios

- Library/RebornValidation/Reborn-EditMode-results.xml
- Library/RebornValidation/Reborn-Unity-Editor.log

O arquivo de resultados anterior é removido antes da execução, para que uma falha atual jamais reutilize um resultado antigo de sucesso. Quando a compilação falha sem gerar XML, o script exibe trechos de erro CS do log.

### Portões de qualidade

1. CI atual verifica Core .NET, regras e contratos estáticos, incluindo que o novo script possui flags e análise de resultados corretas.
2. O novo TESTAR_UNITY_REBORN.ps1 precisa ser executado no computador com a licença/instalação Unity correspondente: isso valida compilação e testes EditMode de verdade.
3. Ainda é obrigatório testar visualmente a Game View em 16:9/9:16, efeitos de fusão da IA, controles por teclado e mouse, performance e conteúdo original importado localmente.

**Limitação:** nenhum processo Unity foi executado na CI remota nem neste ambiente. Testes .NET/estáticos aprovados não garantem que MonoBehaviours e UI do Unity compilam. A versão final do cliente continua condicionada à verificação local.
