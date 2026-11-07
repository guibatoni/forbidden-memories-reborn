# Forbidden Memories Reborn — Prototype 0.4 (motor de duelo experimental)

> **Status:** entregue em codigo-fonte, ainda nao compilado/testado no Unity local do usuario.
> O motor NAO implementa integralmente as regras do Forbidden Memories PS1.

## Principios inalteraveis

- A camera 0.3 e o script `OrbitCamera.cs` nao foram modificados.
- Cada jogador possui 5 zonas de monstros + 5 zonas de suporte, sem slots extras.
- Arquitetura C# independente da Unity em `Reborn.Core`.
- Nenhum script insere cartas, arte, historia ou oponentes oficiais no repositorio.
- O modo Sandbox 0.3 continua disponivel para comparacao/regressao.
- A sequencia de eventos e dados reais da campanha depende de exportacao validada da ISO do proprio usuario.

## Modos

1. Clique Play na cena como nas versoes anteriores.
2. No HUD Sandbox, clique **Duelo experimental 0.4**.
3. Selecione uma carta da mao e clique em zona correspondente (ou arraste).
4. Para tentar uma cadeia de fusao, selecione a primeira e use **Shift+clique** para adicionar as proximas na ordem.
5. **Posicao: ATK/DEF** escolhe a postura do monstro ao entrar. **Guardian: primeira/segunda** escolhe um dos astros presentes na definicao da carta.
6. Clique **Iniciar combate**. Clique **Selecionar ataque**, depois selecione seu monstro e o alvo adversario. Se o adversario nao tiver monstros, use **Atacar diretamente**.
7. Clique **Encerrar turno** para executar a IA demonstrativa. Ela joga uma carta disponivel, tenta atacar e devolve o turno.
8. **Novo duelo** reinicia o modo; **Voltar ao Sandbox** restaura as interacoes anteriores.

## Implementado

- Estado deterministico de duelo para dois participantes (40 cartas cada, mao ate cinco).
- LP 8000, estado de mao, deck, monstros, suportes e cemiterio, com dados C# independentes da engine.
- Turnos alternados, uma jogada de mao por turno no modelo simplificado; fase de preparacao e fase de batalha.
- Ataque contra ATK e DEF, empate, dano de vida, ataque direto, bloqueio de acoes ilegais e vitoria por LP zerados.
- 10 Guardian Stars em dois ciclos, com vantagem temporaria de 500 pontos. Sem star importada = sem bonus.
- Cadeias ordenadas de ate cinco cartas na mao; fusoes reconhecidas apenas quando o catalogo contem a receita explicita.
- Receitas ficticias separadas no `DemoDeck.CreateSampleFusions()`, destinadas exclusivamente ao teste. Importacoes reais nao as utilizam.
- Eventos imutaveis ordenados, para ligar efeitos visuais sem alterar as regras; VFX de destruicao e animacao de entrada.
- IA deterministica de demonstração, NAO corresponde a AI/comportamento dos personagens do jogo oficial.
- Testes NUnit do motor de duelo, em `Assets/Tests/Editor/DuelMatchTests.cs`.
- Teste de compilacao/exercicio da camada Core por .NET 8 (GitHub Actions) sem licenca Unity.

## FIDELIDADE PENDENTE (nao implementado)

- Cartas/receitas oficiais extraidas da ISO, metadados reais de Guardian Star, cartas especiais com efeitos, equips, armadilhas, rituais, terreno, rank, spoils, deck-out integral, campanha completa, oponentes e baralhos oficiais.
- O codigo de suporte apenas **coloca** a carta no espaco. Ele nao dispara efeitos nao verificados.
- O ciclo de turnos e o comportamento de falha na fusao sao regras de demonstracao, a confirmar contra o jogo original antes de declarar fidelidade binaria.
- Sem compilacao/teste Unity neste ambiente. Verificar o Console, o Test Runner e o build Windows no seu computador.

## Dados opcionais do export `schema=1`

Arquivo esperado no projeto do usuario: `Assets/StreamingAssets/RebornContent/original_export.json`.
O formato anterior continua compativel. Novos campos opcionais:

```json
{
  "cards": [
    { "id": 1, "name": "EXEMPLO FICTICIO", "kind": "Monster", "attack": 1000, "defense": 900,
      "description": "Apenas para validar o schema.", "guardianStarA": "Mercury", "guardianStarB": "Mars" }
  ],
  "fusions": [
    { "materialA": 1, "materialB": 2, "result": 3, "verified": true }
  ]
}
```

Apenas demonstracao de campos; o JSON acima nao e um export completo nem uma receita real. Uma receita so e aceita se todos os IDs existirem, o resultado for monstro, nao houver duplicatas e `verified=true`.
O marcador de validacao deve vir de um processo de extracao/revisao confiavel. O loader **nao** comprova por si so que os dados equivalem aos bytes da ISO.

Validacao local antes do import:

```bash
python tools/validate_import.py caminho/para/original_export.json
```

## Testes

- Unity Editor: `Window > General > Test Runner`, testes em Edit Mode, e depois Play.
- CI: `.github/workflows/core-smoke.yml` compila o nucleo sem Unity e executa 12 verificacoes de comportamento.
- Sem o Unity Editor neste ambiente, o resultado visual e os testes do Editor estao **pendentes de validacao**.

## Backup

Guardar o backup completo da etapa em `ChatGPT > Biblioteca > Forbidden Memories Reborn > Backups` e manter artefatos de cada etapa sem sobrescrever. O backup 0.4 deve vir acompanhado de hash SHA-256 em `BACKUP_MANIFEST.json`.
