# Reborn Prototype 0.23 — Equips extraídos de verdade e tipos de monstro

## Por que essa etapa foi necessária

A extração anterior já gravava os IDs e a lista de alvos de cada
Equip no arquivo local `PrivateExtracts/reborn-disc/disc_tables.json`.
O Unity porém ainda não tinha o `equip_rules.json` correspondente,
e por isso aplicava apenas Megamorph (#657) e Bright Castle (#668).

Além disso, o importador da ISO original não salvava o `typeLabel` das
cartas no JSON. Sem este campo, os seis terrenos da 0.21 não sabiam quais
monstros eram Dragon, Spellcaster, Zombie, Machine, etc., apesar de esses
tipos já estarem corretamente presentes no binário.

**Agora os dois campos são conectados automaticamente.**

## Usando os dados que você já extraiu

Se você já usou `IMPORTAR_ORIGINAL.ps1` e possui:
- `PrivateExtracts/reborn-disc/disc_tables.json`
- `Assets/StreamingAssets/RebornContent/original_export.json`

Execute na pasta principal do Reborn:

    powershell -ExecutionPolicy Bypass -File .\tools\EXPORTAR_EQUIPS.ps1

O comando lê apenas arquivos locais, verifica que os SHA-256 são
idênticos e grava:

    Assets/StreamingAssets/RebornContent/equip_rules.json
    PrivateExtracts/reborn-disc/equip-import-report.json

O processo **não usa rede, não faz push, não relê o disco e não altera
dados do DUELTRACE**. Nenhum desses arquivos privados é versionado.

Para reaproveitar os bônus dos terrenos com `typeLabel` original,
é necessário executar novamente `IMPORTAR_ORIGINAL.ps1`, porque
a extração 0.17 não persistiu o subtipo de monstro nos arquivos
existentes. A partir desta 0.23, a importação do disco já gera
automaticamente tanto o `typeLabel` como o `equip_rules.json`.

## Validação de integridade

O conversor aplica todas estas restrições:
1. Os dois arquivos têm esquema 1, perfil `original-ps1*` e o
   mesmo SHA-256 completo da ISO/BIN.
2. Existem as 722 cartas únicas no catálogo, com `kind` verificado.
3. Cada Equip encontrado no arquivo de extração é uma carta do tipo
   `Equip` no catálogo do mesmo disco.
4. Cada alvo é único e é um monstro real com ID 001..722.
5. `parserNotes` não contém divergências das tabelas Equip entre os
   sete bancos de terreno.
6. #657 e #668 não são registrados duas vezes (as duas regras
   universais já fazem parte do Core).
7. Se houver falha no importador da ISO, ele remove qualquer manifesto
   antigo em vez de reutilizar regras de outro mod por acidente.

O arquivo traz os equipamentos com IDs e listas de alvos encontrados,
além dos bônus originais de +500 ATK/DEF nos Equips específicos.
Megamorph mantém +1.000, e Bright Castle +500 para qualquer monstro.

**Limite importante:** esse é um *resultado estrutural da ISO*, não uma
afirmação de que cada Equip foi testado individualmente no jogo.
O relatório informa quantos Equips e quantos alvos foram carregados e
quais ficaram sem regra. Um mod que altera o funcionamento do binário
pode exigir análise específica adicional.

## Testes

CI roda testes com catálogo sintético de 722 cartas e falsas tabelas
de Equip, verificando:
- atualização do schema local sem publicar o binário;
- SHA diferente rejeitado;
- divergência das cópias por terreno rejeitada;
- alvo duplicado, inexistente ou não-monstro rejeitado;
- IDs universais preservados;
- classificação de monstros para terrenos da 0.21.

No Unity 6000.3.28f1: importar sua ISO original, abrir Play,
equipar monstros compatíveis com Dragon Treasure ou outros Equips
listados no relatório e confirmar que incompatíveis são rejeitados
sem gastar cartas. Teste Mountain com Dragon e Yami com Spellcaster;
o valor deve mudar com o campo.

Nenhum arquivo da ISO deve ser publicado no GitHub. Os assets podem
ser mantidos apenas localmente e sua distribuição depende dos direitos
dos titulares.

Referência técnica para o layout das tabelas originais:
https://github.com/Unchiga/Yu-Gi-Oh-Forbidden-Memories-Recompiled/blob/master/tools/pc/fm_editor/gamedata.py
