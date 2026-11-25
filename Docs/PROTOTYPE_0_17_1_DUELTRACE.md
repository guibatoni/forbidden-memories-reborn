# Prototype 0.17.1 — DUELTRACE → Reborn (dados, nao copia de app)

## Objetivo
Ler o banco local da edicao **Original** ja analisada no DUELTRACE,
converter os registros ao schema Unity existente e apresentar tudo em
cartas 2.5D originais do Reborn. Nao precisamos reescanear o disco
para nomes, IDs, ATK/DEF, categorias ou fusoes ja registradas.

## Executar no Windows (PowerShell)
Dentro da pasta do Reborn 0.17.1:

    powershell -ExecutionPolicy Bypass -File .\tools\IMPORTAR_DO_DUELTRACE.ps1

A deteccao automatica encontra UM perfil "Original" registrado em:

    %LOCALAPPDATA%\ForbiddenMemoriesMultiMod\registro_mods.json

Se houver varios perfis/versoes com nomes parecidos, especificar:

    powershell -ExecutionPolicy Bypass -File .\tools\IMPORTAR_DO_DUELTRACE.ps1 -Perfil "C:\Users\...\AppData\Local\ForbiddenMemoriesMultiMod\profiles\<sha>\profile.json"

Para reaproveitar tambem as 722 imagens PNG ja extraidas pelo DUELTRACE,
adicione -ArtesLocais ao comando. Isso copia apenas imagens ausentes em
Assets/Resources/RebornCardArt, sem sobrescrever artes personalizadas.
Nao distribua um build que contenha imagens originais do jogo sem a
autorizacao necessaria; o Unity pode embutir esses assets no executavel.

A ponte le o perfil somente localmente e grava:
- Assets/StreamingAssets/RebornContent/original_export.json
- PrivateExtracts/reborn-disc/dueltrace-bridge-report.json
- PrivateExtracts/reborn-disc/original_export.before_dueltrace.json (backup se existia)

Esses arquivos **nao estao no GitHub** (cobertos por .gitignore).

### Fusoes com diferentes graus de confianca
Por padrao importamos:
- fusoes ja existentes no export do mesmo disco;
- receitas registradas pelo DUELTRACE como
  "observada_manualmente_no_jogo" (confirmadas uma por uma).

As receitas "revisada_usuario_em_lote" ficam de fora por padrao.
Para habilitar as receitas revisadas em lote, use a opcao explicita:

    powershell -ExecutionPolicy Bypass -File .\tools\IMPORTAR_DO_DUELTRACE.ps1 -Perfil "C:\...\profile.json" -IncluirLoteRevisado -RelatorioFusoes "C:\...\investigacoes\<sha>.fusoes.json"

O importador verifica SHA-256, fingerprint, sete bancos consistentes e
ausencia de conflitos. Mesmo assim, uma revisao em lote NAO e prova
de funcionamento individual em duelo; a procedencia fica registrada.
Nao sao importadas fusoes desconhecidas ou receitas da ISO errada.

### Descricoes, subtipos, magias, traps, equips e rituais
O DUELTRACE traz atualmente um catalogo com name, id, atk, def,
type_id e type_label (Dragon, Spellcaster, etc., e tipos especiais).
O banco DUELTRACE consultado **nao** extrai automaticamente todas as
descricoes internas de cartas do executavel original. Portanto:
- aproveitamos descricoes ja presentes no perfil, se existirem;
- preservamos descricoes e Guardian Stars de uma importacao anterior
  do mesmo disco, caso ela exista;
- deixamos campos ausentes identificados, sem inventar conteudo.

Se as descricoes do PS1 ou as receitas de ritual faltarem, execute
o importador do disco **uma unica vez** com IMPORTAR_ORIGINAL.ps1
e depois rode IMPORTAR_DO_DUELTRACE.ps1 novamente para fazer a mescla.
Isso usa seu disco privado, nao altera o DUELTRACE.

Algumas edicoes fornecem effect_text, effect_source e effect_status.
Esses dados agora aparecem separadamente no painel de inspecao de
cartas (clique direito), inclusive em Magias, Armadilhas e Equips.

IMPORTANTE: texto de efeito nao executa mecanicas. Apenas a existencia
de um texto nao prova gatilho, alvo, valor ou interacao correta. O motor
de efeitos do Reborn deve ser implementado a partir de comportamentos
verificados, sem rodar ou interpretar codigo/texto arbitrario de mods.

### Apresentacao independente
O Reborn continua usando seus proprios modelos de cartas, molduras,
sombreamento e movimento. O catalogo afeta textos e referencias do
CardDefinition; o CardFaceBuilder e o overlay do painel exibem dados.
Nao importamos CSS, UI, fontes nem recursos protegidos de outros apps.

### Limites importantes
- **Nao** misturar perfis de mods diferentes: SHA diferente = erro.
- Ter 722 IDs completos nao garante que os nomes sejam retail byte a byte;
  "Original Drop x15" pode ter alteracoes.
- O banco recente de outro tipo (SQLite, por exemplo) precisa de um
  adaptador adicional se nao apresentar o perfil JSON DUELTRACE conhecido.
- Sem perfil do usuario no computador de execucao, a CI apenas roda testes
  **sinteticos**. A compilacao e interacao no Unity 6000.3.28f1 ainda
  precisam ser testadas.
- Essa integracao nao faz merge em main ate validacao visual.
