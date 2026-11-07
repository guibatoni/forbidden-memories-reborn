# Fase 1.1 — Fidelidade ao Forbidden Memories original

## Decisoes confirmadas pelo usuario

1. **Camera:** manter os valores e controles da Prototype 0.1. NAO aproximar nem reenquadrar sem nova aprovacao.
2. **Cartas:** importar nomes, IDs, descricoes e atributos do disco original da edicao informada. NAO utilizar as cartas ficticias como dados finais.
3. **Tipos de cartas:** Monster ocupa a zona superior; Spell, Trap, Equip e Ritual ocupam as cinco zonas de suporte, conforme validado no prototipo.
4. **Oponentes:** IDs e identidade relacionados a campanha original. Nao trocar adversarios nem usar a ordem da lista de Free Duel como ordem de eventos.
5. **Historia:** os textos, as escolhas e as respostas devem ser extraidos de arquivos locais fornecidos pelo usuario. O motor suporta caminhos condicionais, duelos e resultados diferentes. Nao supor que o roteiro e linear.
6. **Animacoes cinematograficas:** opcionais, sem alteracao nas regras nem nos resultados do duelo.

## Situacao atual — limites da entrega

- A **ISO original nao foi fornecida nesta etapa**: nao ha export validado de historia, oponentes e textos para incluir.
- O novo projeto aceita um `original_export.json` externo quando houver uma extracao confiavel. **Nao le ISO/BIN diretamente nesta versao.**
- Os metadados discSha256 nao sao verificados contra o binario pelo Unity; um arquivo JSON carregado e rotulado como *importado, nao validado contra ISO*.
- O catalogo de cartas importado alimenta somente um *deck de sandbox*, nao o deck original do jogador.
- O modo Historia consegue mostrar textos e escolhas de um export importado, mas para ao chegar em um duelo: **ainda nao ha motor de batalha para processar o resultado real**.
- Nenhum texto, arte, musica ou ROM proprietarios esta incluido no pacote. O uso e a distribuicao de dados originais exigem autorizacao conforme o caso.

## Layout de conteudo

Criar localmente: `Assets/StreamingAssets/RebornContent/original_export.json`

Um modelo para formatacao (dados inventados, **NAO** sao dialogos ou cartas de Forbidden Memories) encontra-se em `Docs/exemplo-esquema-importacao.json`.

Campos esperados:

| Grupo | Campos importantes |
|---|---|
| `source` | `edition`: `original-ps1`, `language`, `discSha256` (opcional, 64 hex) |
| `cards[]` | `id` inteiro de 1 a 722, `name`, `kind`, `attack`, `defense`, `description` |
| `opponents[]` | `id`, `name`, `deckKey` (identificacao interna), sem presumir sequencia de Free Duel |
| `story.startNodeId` | primeiro evento da campanha da edicao importada |
| `story.nodes[]` | `id`, `location`, `speaker`, `text`, `choices[]` ou `opponentId` com `winNodeId` e `loseNodeId` |
| `choices[]` | `text`, `nextNodeId`, `requiresFlag`, `forbidsFlag`, `setsFlag` (aceitam listas separadas por virgula) |

`opponentId = -1` significa dialogo; IDs de oponentes podem comecar em zero. Um evento de duelo usa ID de oponente >= 0 e pode registrar flags com `winSetsFlag`/`loseSetsFlag`.

O loader exige IDs unicos, destinos de escolhas existentes, e oponentes reconhecidos nos nos de duelo. Cartas com descricao ausente sao contadas como lacunas; o importador nao inventa a descricao.

## Pipeline de integracao previsto

```text
ISO/BIN original do usuario
    -> extracao binaria local controlada (DUELTRACE/ISO Forensic Lab)
    -> manifestos de cartas, oponentes e script de eventos
    -> validacao de integridade, edicao, IDs, transicoes e textos
    -> JSON externo ao repositorio
    -> Unity: OriginalContentLoader
    -> CardDefinition, CampaignScript, CampaignRuntime
    -> sistema de duelos real (a implementar)
```

**Passo seguinte para fidelidade real:** fornecer e identificar a ISO/BIN original especifica (regiao, idioma e hash) e confirmar de quais tabelas sao obtidos os textos e os fluxos. O DUELTRACE tem material de extracao de cartas e alguns adversarios, mas nao devemos presumir que todos os dialogos e escolhas ja estejam corretamente extraidos.

## Testes

- Os testes `CampaignRuntimeTests.cs` verificam escolhas, flags, caminhos de vitoria e derrota, bloqueio de avancar dialogo durante duelo e rejeicao de referencias ausentes.
- `DuelSessionTests.cs` continua garantindo a interacao basica da arena.
- Testar no Unity Test Runner, EditMode. Nenhum teste garante fidelidade de dados originais sem comparar com uma ISO fornecida.
