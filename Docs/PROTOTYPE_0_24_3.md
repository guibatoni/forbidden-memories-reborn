# Prototype 0.24.3 — Unity Editor Test Assembly Hotfix

## Erros recebidos

```text
Assets/Tests/Editor/KeyboardInitializationTests.cs(4,14):
CS0234: 'Presentation' does not exist in namespace 'Reborn'

Assets/Tests/Editor/KeyboardInitializationTests.cs(6,19):
CS0234: 'InputSystem' does not exist in namespace 'UnityEngine'

Assets/Tests/Editor/KeyboardPositionTests.cs(4,14):
CS0234: 'Presentation' does not exist in namespace 'Reborn'
```

Esses erros são de **visibilidade entre assemblies** no Unity. Não
representam por si só uma falha no código de fusões ou nas regras do
jogo. Os dois testes EditMode foram adicionados em versões recentes,
mas o `Assets/Tests/Editor/Reborn.Core.Tests.asmdef` ainda referenciava
apenas `Reborn.Core`.

## Correção aplicada

O assembly `Reborn.Core.Tests` continua restrito ao Editor e marcado
como `TestAssemblies` para o Test Runner. Sua lista de referências
agora inclui, além do Core:

- `Reborn.Presentation` (onde `ArenaHud` e os atalhos vivem);
- `Unity.InputSystem` (onde está o enum `Key` usado nos testes).

O arquivo `Packages/manifest.json` já declarava
`com.unity.inputsystem` e `com.unity.test-framework`, portanto
**não foi preciso introduzir outro pacote ou trocar a versão do Unity**.

Também criamos `tools/check_unity_editor_asmdef.py`: uma verificação
estática de CI que confere a presença dessas referências, nomes dos
assemblies, pacotes e metadados dos testes.

## Como testar em seu computador

1. Faça backup dos seus arquivos privados extraídos da ISO em
   `Assets/StreamingAssets/RebornContent` e `PrivateExtracts`,
   antes de trocar qualquer projeto.
2. Abra o projeto 0.24.3 no **Unity 6000.3.28f1**.
3. Aguarde o Package Manager atualizar os assemblies e as mensagens
   de compilação desaparecerem.
4. Entre em `Window > General > Test Runner` e selecione **EditMode**.
5. Execute os testes `KeyboardInitializationTests` e
   `KeyboardPositionTests`, além dos demais testes do projeto.
6. Entre em Play e teste: abertura do HUD sem
   `UnityException`/`NullReferenceException`; remapeamento F1;
   seleção, Q/E, posição Face Down, fusões e turno por Espaço.

Se `UnityEngine.InputSystem` continuar ausente, abra o Package Manager
e verifique que o pacote **Input System** foi realmente resolvido pelo
Unity; estar declarado no `manifest.json` não basta se a resolução
local falhar.

## Validação e limites

A CI do GitHub testa o motor C# independente do Unity, scripts Python e
agora a estrutura dos `asmdef`. Ela não executa o compilador
do Unity nem o EditMode Test Runner em uma instância Unity. Assim,
nenhum teste local foi declarado aprovado sem ter sido executado.
Esta entrega é um hotfix de referências: nenhuma regra de jogabilidade
ou arquivo de cartas/ROM foi alterado.

Referência oficial de assembly de testes:
https://docs.unity.com/en-us/engine/6000.3/manual/scripting/test-framework-introduction/getting-started/workflow-create-test-assembly
