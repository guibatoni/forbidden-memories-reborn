# Prototype 0.17.2 — corrigindo cartas demonstrativas

A carta **Fera do Nilo** com ID **p04** e descricao **Criatura de rio**
pertence ao `DemoDeck`; ela nao pertence ao catalogo PS1 Original.

## O que muda
- Ao iniciar, a compilacao tenta abrir o export completo do Reborn:
  `Assets/StreamingAssets/RebornContent/original_export.json`.
- Se o export estiver **ausente**, ela tenta ler automaticamente o **perfil
  Original unico e validado** no DUELTRACE instalado no mesmo Windows.
- Se houver mais de um perfil Original, o jogo nao escolhe um ao acaso.
  Execute o importador com um `profile.json` especifico.
- Se o arquivo exportado existir mas estiver malformado/incompleto, nao se
  faz fallback silencioso para outra edicao.
- Se nenhum catalogo original puder ser carregado, um aviso ocupa a tela,
  bloqueando o duelo com cartas ficticias. Para testar conscientemente as
  regras com p01-p08, escolha o botao **Continuar apenas no modo DEMONSTRACAO**.
- A colecao completa de 722 cartas e nomes `001`–`722` e obrigatoria.
  Um pacote com um ou dois registros nao autoriza o modo Original.

## Importar dados no Windows
1. Pare o Play no Unity.
2. Extraia e abra este novo projeto no Unity 6000.3.28f1.
3. Use o PowerShell na raiz do projeto:

       powershell -ExecutionPolicy Bypass -File .\tools\IMPORTAR_DO_DUELTRACE.ps1 -ArtesLocais

4. Confirme o relatorio exibindo **722 cartas** e o caminho do export.
5. Reinicie o Play no Unity.

O `-ArtesLocais` copia os PNGs existentes no DUELTRACE para a pasta local
`Assets/Resources/RebornCardArt/`, sem sobrescrever artes personalizadas.
Se as PNGs nao foram extraidas no DUELTRACE, o Reborn usa um placeholder
ate que a arte seja importada; nao substitui os **nomes** corretos.

## Detalhes de dados
O carregamento direto usa `%LOCALAPPDATA%\ForbiddenMemoriesMultiMod\registro_mods.json`
e o perfil identificado como Original; exige SHA coincidente, 722 cartas
validadas e tipos suportados. Esse atalho e **somente leitura**. Ele traz
nomes, IDs, tipos, ATK/DEF e fusoes observadas manualmente.
Apenas o export completo pode incluir todas as receitas revisadas em lote
(com fingerprint), descricoes, Guardian Stars e rituais verificados.

Se o importador nao encontrar um perfil unico, escolha explicitamente o
arquivo do Original:

       powershell -ExecutionPolicy Bypass -File .\tools\IMPORTAR_DO_DUELTRACE.ps1 -Perfil "C:\...\profile.json" -ArtesLocais

Nao renomeie ou publique as artes originais sem autorizacao do titular.
O uso dos dados no projeto privado nao autoriza distribuicao comercial
dos nomes, artes ou descricoes protegidos.

## Validacao
- CI executa verificacoes de carregamento e de nao-retorno silencioso ao Demo.
- A existencia do registro local DUELTRACE, a leitura dos 722 IDs reais,
  o pacote grafico e o jogo no Unity precisam ser conferidos na maquina do usuario.
- As cartas do DUELTRACE sem descricoes/efeitos validados continuam identificadas
  como incompletas; nenhum efeito magico e executado por deducao.
