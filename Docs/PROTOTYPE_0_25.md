# Forbidden Memories Reborn — Prototype 0.25
## Fusões verificadas, controles estáveis e testes locais

Esta atualização continua a 0.24.3, **sem alterar a `main`**.
Nenhuma ROM, BIN/ISO, arte comercial ou perfil privado é enviada ao GitHub.

### 1. Assistente de fusões de verdade — tecla F

Durante sua fase principal, pressione **F** ou clique no botão
**FUSÕES DISPONÍVEIS [F]**, no canto inferior esquerdo.

O assistente mostra:
- **Total de receitas importadas** no perfil do duelo.
- **Pares jogáveis agora** envolvendo duas cartas da sua própria mão.
- Combinações do tipo **monstro próprio no campo + carta da mão**.
- Nome do resultado, cartas envolvidas e números dos slots.

A lista vem de `FusionCatalog`, por meio de
`DuelMatch.FindAvailableFusions(DuelSide.Player)`.
A busca respeita fase, turno, estado de colocação e zonas disponíveis;
não consulta cartas escondidas do oponente e não interpreta texto
de cartas como regras.

**Clicar numa opção (ou ↑/↓ e Z)** seleciona a receita e abre
o fluxo já existente de Guardian Star → Face Up/Down → ATK/DEF.
A carta só é consumida **depois** de confirmar a colocação final.
X/F fecha a lista sem alterar cartas, LP, eventos ou turnos.

Se não houver receitas, a janela explica a diferença entre
**722 cartas importadas** e **fusão de fato importada**.
Use os comandos de diagnóstico no final deste documento.

### 2. Corrigida a seleção por setas, Z e duplo clique

Na versão anterior, ao navegar para outro material e pressionar Z,
a seleção podia voltar a uma única carta, descartando o restante
da sequência. A 0.25 preserva a lista ordenada marcada por
**↑ (marcar)** e **↓ (desmarcar)** durante a movimentação com ←/→.
**Z confirma a seleção atual**, mesmo após mover o cursor.

Pelo mouse, clicar novamente sobre uma carta já marcada
preserva os outros materiais. O duplo clique numa carta marcada
usa a sequência completa. Um clique normal em carta não marcada
inicia outra seleção; Shift+clique acrescenta à seleção corrente.

Quando houver **duas ou mais cartas sem qualquer receita válida**
na ordem atual, Z/duplo clique automático não vai descartá-las
acidentalmente; o programa explica que não há fusão verificada
e permite refazer a sequência. A combinação proposital com falha
segue disponível por colocação manual, se o usuário realmente
pretender isso (mecânica original).

### 3. Troca sem fusão não é anunciada como fusão

Ao posicionar uma carta da mão sobre um monstro já no campo,
o motor pode efetuar uma substituição sem existir receita.
O Reborn agora conta os passos efetivamente fusionados com
`CountFieldFusionSteps()` e exibe **TROCA SEM FUSÃO**
na janela de confirmação quando a sequência não tem receita.
Ninguém perde um monstro inadvertidamente só por confiar
no texto da prévia.

A substituição legal permanece possível após a confirmação,
preservando as regras do motor já existente.

### 4. Face Down preservado após falha numa sequência de campo

Corrigido um erro na resolução de múltiplos materiais:
se uma fusão intermediária funcionava e a carta seguinte
falhava, o motor guardava o indicador de que *alguma*
fusão ocorreu e revelava o último sobrevivente ao baixar.
Agora a visibilidade depende de a **última etapa** realmente
resultar em fusão. Se a última carta for original, ela
pode continuar **Face Down** até combate.

Esse cenário é coberto por regressão determinística com
três materiais e eventos Fusion/FusionFailed.

### 5. Teclas atualizadas

| Tecla | Ação |
|-------|------|
| Espaço | Passar turno |
| Q / E | ATK / DEF sem alternância involuntária |
| Z / X | Confirmar / cancelar |
| S | Inspecionar carta |
| W | Mesa adversária |
| ← / → | Mover seletor |
| ↑ / ↓ na mão | Marcar / desmarcar material |
| **F** | **Assistente de fusões disponíveis** |
| F1 | Remapear teclas |

A tecla F pode ser alterada pelo menu F1, sem afetar
as teclas já salvas nas versões anteriores.

### 6. Validação sem depender de GitHub Actions

Em PowerShell, na pasta raiz do Unity:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\VALIDAR_REBORN.ps1
```

Executa contratos estáticos de controles/Unity,
verifica o motor no .NET (se estiver instalado) e
audita o catálogo local de fusões (se existir).
Não requer Node, não requer Python 3.12 e não envia arquivos a terceiros.

Para exigir que as receitas estejam carregadas:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\VALIDAR_REBORN.ps1 -ExigirFusoes
```

Ou apenas listar receitas do Original:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\DIAGNOSTICAR_FUSOES.ps1
```

Os scripts respeitam a pasta do usuário e não reescrevem
`original_export.json`, `PrivateExtracts` nem a ISO.

No GitHub, o workflow agora executa no PR, em `main` e manualmente,
mas **não mais a cada push das branches de desenvolvimento**.
O cancelamento de execuções obsoletas e a remoção de um teste
duplicado reduzem fila e gastos sem remover regressões.

**Nota para quem baixou o projeto do GitHub:** o arquivo
`tools/VALIDAR_REBORN.ps1` está na branch de desenvolvimento
`feature/prototype-0.25.1-editor-csharp-hotfix` e ainda não está na
`main` (que mantém a versão anterior). Ao baixar um ZIP, selecione a
branch correta; execute o comando a partir da raiz desse ZIP, não de
uma cópia antiga. A validação local também executa agora a checagem
preventiva do erro de compilação Unity `CS0819`.

### 7. Matriz de testes

**Core .NET**: pares válidos, falta de receitas, permissões de turno,
preview sem mutação, fusão efetiva mão/campo, colocação única, troca
de campo com falha intermediária seguida de sobrevivente Face Down.

**Unity NUnit EditMode**: abertura/cancelamento do assistente,
bloqueio dos comandos por trás do modal, callback após destravar,
e garantia de que consultas não consomem cartas.

**CI estática**: dependências de `Reborn.Core.Tests.asmdef`,
atalho F configurável, manutenção da sequência de ↑/↓, proteção
contra descarte automático sem receita e workflows sem duplicidade.

**Ainda não validado dentro do Game View do Unity**:
compilação visual do novo modal, escala de GUI em 16:9/9:16,
rastro de fusão, clique físico em botão, coexistência dos modos
teclado/mouse e leitura das receitas reais da ISO instalada.

#### Teste recomendado no Unity 6000.3.28f1
1. Abra `Window > General > Test Runner > EditMode` e execute
   `FusionAssistantTests`, `KeyboardInitializationTests` e
   `KeyboardPositionTests`.
2. Entre em Play e pressione F. A lista deve mostrar **N receitas**
   e as combinações possíveis da mão atual, sem mostrar cartas
   escondidas do rival.
3. Escolha uma opção e cancele com X: nenhuma carta deve sair da mão.
4. Escolha outra e confirme: compare o resultado com a receita
   real importada.
5. Marque duas cartas com ↑, navegue com ←/→ e confirme com Z.
   Repita com mouse/Shift+clique e duplo clique.
6. Teste substituição intencional sem receita na mesa:
   o aviso **TROCA SEM FUSÃO** deve aparecer.
7. Teste Face Down com falha após uma fusão intermediária e
   verifique a ocultação até o combate.
8. Inspecione o Console e reporte qualquer nova mensagem
   de compilação/runtime com a primeira linha e stack trace.

**Observação:** se `FusionRecipeCount == 0`,
reimporte sua própria ISO pelo `tools/IMPORTAR_ORIGINAL.ps1`.
O fallback automático DUELTRACE aceita apenas as combinações
observadas manualmente no jogo; **722 cartas não provam
que todas as receitas foram importadas**.
