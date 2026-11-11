# Reborn Prototype 0.24.2 — Estabilização e diagnóstico de fusões

Esta etapa é propositalmente pequena. Nenhuma animação, regra de magia,
ISO, deck, batalha ou recurso protegido foi alterado.

## 1. ATK/DEF pelo teclado agora é determinístico

Na 0.24.1, Q e E chamavam o **mesmo método de alternância**. Isso
significava que pressionar E em um monstro já em DEF podia colocá-lo em
ATK. A partir da 0.24.2:

- **Q sempre seleciona ATK.**
- **E sempre seleciona DEF.**
- Apertar a tecla da posição já ativa não faz nada; não cria um evento
  nem modifica a carta.
- Uma carta setada Face Down mantém o verso oculto durante a mudança
  de posição.
- O botão de mouse que alternava ATK/DEF continua disponível e chama
  uma função separada, preservando a funcionalidade anterior.
- As regras do Core continuam impedindo mudanças ilegais após atacar.

Implementação: `ArenaHud.TrySetSelectedMonsterPosition()` + roteamento
por `ArenaInteraction.Keyboard`. O teste EditMode é
`Assets/Tests/Editor/KeyboardPositionTests.cs`.

## 2. Diagnóstico dos dados reais de fusão

Se o Reborn possui nomes de 722 cartas, mas não consegue realizar
fusões, precisamos distinguir **problema de dados** de **problema
de interface**. Não vamos inventar receitas e nem tentar interpretar
descrições de cartas como regras.

Na pasta principal do projeto Unity, execute em PowerShell:

    powershell -ExecutionPolicy Bypass -File .\tools\DIAGNOSTICAR_FUSOES.ps1

Para consultar especificamente receitas envolvendo a carta #016:

    powershell -ExecutionPolicy Bypass -File .\tools\DIAGNOSTICAR_FUSOES.ps1 -Card 16

O diagnóstico lê somente:

    Assets/StreamingAssets/RebornContent/original_export.json

Ele confirma o formato, o perfil, a presença das 722 cartas, o SHA da
imagem e os pares únicos `materialA/materialB/result` com
`verified=true`, além de exigir resultados de tipo Monster.

**ZERO RECEITAS** é um erro detectável mesmo quando as 722 cartas foram
importadas corretamente. Em caso de falha, o comando devolve um código
não zero e explica como reimportar sua própria ISO original ou
um lote revisado do DUELTRACE. Nenhum arquivo é enviado nem alterado.

A diferença entre os caminhos existentes é importante:

- `IMPORTAR_ORIGINAL.ps1` tenta importar a tabela de fusões do disco.
- `IMPORTAR_DO_DUELTRACE.ps1` utiliza receitas manualmente verificadas
  e pode incluir um lote revisado quando explicitamente fornecido.
- O fallback automático `DueltraceLocalProfileLoader` aceita apenas
  registros `observada_manualmente_no_jogo`, o que pode representar
  **apenas uma pequena parte** das fusões do PS1.
- Se `original_export.json` existe mas está inválido, não devemos
  ocultar essa falha carregando um perfil diferente sem explicação.

## 3. Nossa nova ordem de trabalho

| Etapa | Critério de conclusão |
| --- | --- |
| A. Inicialização | Entrar em Play sem UnityException e sem NullReferenceException |
| B. Teclado e mouse | Q=ATK, E=DEF, Z/X e dois cliques funcionam sem gastar cartas indevidamente |
| C. Fusões | Receitas carregadas >0, ID/resultado validado e fusão em partida real |
| D. Posicionamento | Próximo slot livre, carta Face Down permanece oculta e suporte não sobrepõe outro |
| E. Interações | Magia, Trap, Equip e fusão de campo mantêm seus efeitos e estados |
| F. Animações | Sem erro no Console, sem vazamento de GameObjects nem travamento nas transições |
| G. Empacotamento | CI aprovada + teste EditMode/PlayMode no Unity + backup isolado |

A CI do GitHub executa Core .NET, testes sintéticos sem ROM e contratos
estáticos. **Ela não executa a janela Game View.**
Os testes `KeyboardInitializationTests` e `KeyboardPositionTests` são
destinados ao Test Runner do Unity. Portanto, um check verde na CI
NÃO significa "funcionou visualmente no seu computador".

## Próximas ações que ainda exigem evidência local

1. Abrir a 0.24.2 no Unity 6000.3.28f1 e executar EditMode tests.
2. Usar F1 para confirmar remapeamento e reiniciar o Play.
3. Executar `DIAGNOSTICAR_FUSOES.ps1` e conferir as contagens
   sem enviar dados privados publicamente.
4. No Play, selecionar duas cartas com receita efetiva e confirmar
   o resultado e consumo corretos; repetir com monstro já na mesa.
5. Gravar qualquer erro persistente com a stack trace e a etapa em
   que ocorre. Só então retomar desenvolvimento de novas mecânicas.

Não inclui novos arquivos de ROM, imagens comerciais nem perfis
privados no GitHub. A `main` permanece separada.
