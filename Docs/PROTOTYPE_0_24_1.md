# Reborn Prototype 0.24.1 — Correção de inicialização do teclado

## Sintoma

Ao abrir Play no Unity 6000.3.28f1 após atualizar para 0.24,
o Console podia apresentar:

```text
UnityException: GetString is not allowed to be called from a
MonoBehaviour constructor (or instance field initializer).
Reborn.Presentation.RebornKeyBindings..ctor()
Reborn.Presentation.ArenaHud..ctor()
```

Em seguida, o `ArenaHud.Initialize` podia gerar
`NullReferenceException`, porque o componente fora criado de
forma incompleta após a exceção durante a inicialização dos campos.

## Correção

- O construtor de `RebornKeyBindings` agora apenas aplica as teclas
  padrão em memória; **não acessa PlayerPrefs**.
- `ArenaHud` deixa de criar e carregar as teclas em um inicializador
  de campo.
- O `Awake()` do `ArenaHud` cria as teclas e chama
  `RebornKeyBindings.LoadSaved()`, que lê as preferências salvas
  **somente depois** de o Unity inicializar o componente.
- Teclas salvas inválidas/duplicadas são ignoradas de forma individual
  sem impedir a disponibilidade dos comandos padrão.
- As preferências ainda são alteradas e persistidas no menu F1.
  Nenhum arquivo de cartas, ROM, fusões, imagens, partidas ou decks foi alterado.

## Regressão e validação

O fluxo do GitHub Actions verifica estaticamente que a leitura de
`PlayerPrefs` não retorna ao construtor, além de rodar os testes
anteriores de Core, fusion, magia, Equip e IA.

Um novo teste em `Assets/Tests/Editor/KeyboardInitializationTests.cs`
instancia `GameObject.AddComponent<ArenaHud>()` dentro do Unity,
verifica que `Awake` criou as teclas e rejeita teclas duplicadas.
Este teste **precisa ser executado no Unity Test Runner** (a CI
atual não executa o Unity Editor).

Checklist no Unity:

1. Abra o projeto 0.24.1 usando Unity 6000.3.28f1.
2. Abra `Window > General > Test Runner`, selecione **EditMode** e execute
   `KeyboardInitializationTests`.
3. Entre em Play: não deve aparecer `UnityException` no `GetString`
   nem `NullReferenceException` no `ArenaHud.Initialize`.
4. Abra F1, remapeie uma tecla não utilizada e reinicie o Play.
   Confirme a persistência da escolha.
5. Teste Z, X, Q/E, espaço, seleção de fusão e duplo clique.

A 0.24.1 é um hotfix exclusivamente de inicialização:
o resto da versão 0.24 permanece inalterado.
