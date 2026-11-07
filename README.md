# Forbidden Memories Reborn — Prototype 0.4.1

Correcao de interface da versao 0.4: fim de duelo sem botoes sobrepostos, nomes compactos nas cartas, comandos bloqueados apos a partida e iluminacao mais equilibrada. **A camera aprovada permanece inalterada.**

Veja [Docs/PROTOTYPE_0_4_1.md](Docs/PROTOTYPE_0_4_1.md). A revisao precisa ser validada no Unity Editor; os testes do GitHub Actions cobrem somente o motor C# independente do Unity.

---

# Forbidden Memories Reborn — Prototype 0.4

**Unity recomendado:** 6.3 LTS `6000.3.28f1` • **Plataforma:** Windows x64 • **Status:** motor experimental, nao jogo finalizado.

O Prototype 0.4 adiciona um modo de duelo alternativo, com dois lados, turnos, fusoes por receitas explicitas, Guardian Stars, ataques e efeitos de entrada/destruicao. **O sandbox 0.3 continua acessivel**.

Leia [Docs/PROTOTYPE_0_4.md](Docs/PROTOTYPE_0_4.md) antes de testar. Os dados oficiais, efeitos, campanha completa, IA original e receitas extraidas da ISO ainda nao estao incluidos. Nao contem ROM/ISO nem assets oficiais.

# Forbidden Memories Reborn — Prototype 0.3 (interacao e eventos de cartas)

Protótipo **não oficial** de um jogo de cartas 3D inspirado na experiência de duelos clássicos e na apresentação de arenas digitais modernas. Projeto novo, independente do DUELTRACE, sem assets e código extraídos dos jogos originais.

> Estado: o usuário confirmou que a versão 0.1 abriu no Unity e permitiu colocar monstros e suportes nas fileiras corretas. **Os novos scripts da versão 0.3 ainda não foram compilados/testados dentro do Unity nesta entrega.** Continuam faltando os dados extraídos da ISO original, o motor de duelo e o build Windows.

## Requisitos

- Unity Hub e **Unity 6.3 LTS (6000.3)** com módulo Windows Build Support, caso queira exportar um .exe.
- Windows 10/11, teclado e mouse.
- Acesso à rede na primeira abertura para baixar pacotes de `Packages/manifest.json`.
- Projeto pensado para 1920×1080, com adaptações simples em outras resoluções.

## Abrir pela primeira vez

1. Extraia o ZIP para uma pasta comum (ex.: `C:\Projetos\forbidden-memories-reborn`).
2. Abra **Unity Hub > Add > Add project from disk** e selecione a pasta que contém `Assets`, `Packages` e `ProjectSettings`.
3. Abra com o editor **Unity 6.3 LTS** e aguarde a importação e compilação de scripts.
4. Quando a Unity solicitar, habilite **Input System Package (New)** ou **Both** em **Edit > Project Settings > Player > Active Input Handling** e reinicie o editor.
5. No menu superior do Unity, clique **Reborn > 01 - Gerar cena do prototipo**.
6. Entre no modo **Play**. A arena será gerada por código.
7. Para build Windows: **Reborn > 02 - Build Windows x64**. A pasta `Builds/Windows/` não deve ser incluída no Git.

> A cena é criada pelo editor porque a arena é procedural, sem texturas ou modelos externos. Na fase 1 isso simplifica versionamento e testes. Se você estiver numa cena vazia e apertar Play, o bootstrap também tenta montar a demo automaticamente.

## Atualizar o protótipo que já funciona

**Faça uma cópia de segurança do projeto antes de atualizar.** Feche o Unity Editor e extraia o ZIP de **atualização incremental** sobre a pasta atual que contém `Assets`, `Packages` e `ProjectSettings`. Escolha substituir os arquivos quando perguntado. A atualizacao nao modifica `OrbitCamera.cs` nem o enquadramento da camera. Leia `Docs/PROTOTYPE_0_3.md` para testar animacoes, alvos e movimento. Nao exclua `Assets/Scenes` nem `Library` do projeto existente.

Se preferir um projeto separado, abra o ZIP completo em outra pasta no Unity Hub.

## Dados do jogo original (ainda nao fornecidos)

Os dados originais **nao acompanham este pacote**. No futuro, apos extracao local verificada de uma ISO/BIN que voce possui, coloque um manifesto no caminho:

`Assets/StreamingAssets/RebornContent/original_export.json`

Veja `Docs/FIDELIDADE_AO_ORIGINAL.md` e `Docs/exemplo-esquema-importacao.json`. O formato registra nome, ID, descricao, tipo da carta, oponentes por ID e um grafo de dialogos com alternativas/flags. A importacao do JSON **nao equivale a importacao direta da ISO nem prova exatidao dos dados**. O programa indica a quantidade de cartas importadas e as descricoes vazias no Console.

O botao `Historia` no HUD abre os dialogos importados quando existirem. Ao chegar num evento de duelo, a historia para corretamente — nao ha falsificacao de vitoria, porque o motor original ainda nao foi implementado. Ate a importacao os dados continuam ficticios, como na versao anterior.

Para validar um export no computador (Python 3):

```bash
python tools/validate_import.py Assets/StreamingAssets/RebornContent/original_export.json
```

## Controles

- Passe o mouse sobre uma carta da mão para destacar; clique para selecionar.
- Clique em uma zona livre do seu lado, ou pressione e arraste a carta ate a zona valida, para posicionar.
- Zonas permitidas pelo motor de colocacao acendem; zonas ocupadas ou incompatíveis nao sao habilitadas.
- `Animacoes: ON/OFF`: muda somente a apresentacao; nao modifica regras nem camera.
- `Remover teste: ON/OFF`: ferramenta de particulas/destruicao ficticia, sem combate nem cemiterio real.
- Monstros só entram nas 5 zonas de monstro. Magias/armadilhas só entram nas 5 zonas de suporte.
- Botão direito + mover mouse: girar câmera; roda: zoom; tecla **R**: redefinir câmera.
- **ESC**: cancelar seleção.
- HUD: repor mão, avançar turno simulado, reiniciar arena e câmera.

## O que é funcional neste protótipo

- Arena egípcia em 3D formada por primitivas (pedra, ouro, colunas, pirâmide, luzes).
- Duas áreas de campo, cada uma com 5 zonas de monstro e 5 de suporte.
- 5 cartas de teste visíveis na mão; seleção, destaque visual e posicionamento.
- Câmera manipulável, HUD com pontos de vida demonstrativos e deck.
- Estado de jogo, validacao de zonas e eventos tipados isolados da Unity.
- Testes básicos e de eventos no Unity Test Runner (Edit Mode), ainda pendentes de execucao no Editor.
- Hover, arraste, destaque de zonas permitidas, entrada suave e particulas de remocao demonstrativa.

## O que AINDA NÃO está implementado

- Motor completo das regras originais, fusões, Guardian Stars, terrenos, magias/rituais/armadilhas reais.
- IA adversária funcional, turnos reais, cálculo de dano, cartas oponentes reais.
- Invocacoes 3D especiais, efeitos originais de combate, fusoes cinematograficas, modelos definitivos e UI de producao.
- Leitura direta da ISO ou integração completa com DUELTRACE. **Apenas o carregamento de um JSON externo, preparado para um futuro export verificado, foi implementado.**
- Multiplayer, salvamento de campanha e distribuição comercial.

## Referência visual

`Docs/conceito-visual-arena.png` é a imagem conceitual aprovada na conversa. **Não é uma captura da Unity** nem arte final do jogo.

## Arquitetura

```
Assets/
  Scripts/
    Core/           # C# puro, sem Unity: cartas, adversarios, grafo de historia, sessao
    Presentation/   # Unity 3D, interacao, camera intacta, HUD, loader externo
  Editor/           # Menu de geração da cena e build de Windows
  Tests/Editor/     # Testes de sandbox e roteiro ramificado
  Scenes/           # Protótipo gerado pelo comando no Editor
Docs/               # Escopo, modelo JSON, roteiro e orientacoes de engenharia
tools/              # Validador offline do export
```

## Limites jurídicos

Este repositório **não contém** arte oficial, música, modelos, marcas ou ROMs de Konami/Yu-Gi-Oh!. O material temporário tem nomes fictícios e primitivas originais. Não distribuir como remake oficial nem comercializar conteúdo protegido sem licença. Nome de projeto provisório.

## Repositorio GitHub separado

O repositorio `guibatoni/forbidden-memories-reborn` existe, mas foi identificado como **publico** em 7/nov/2026. Caso deseje manter a criacao fechada, acesse GitHub `Settings > General > Danger Zone > Change repository visibility` e altere para **Private antes de publicar o projeto**.

Com a visibilidade ajustada, voce pode clonar o repositorio para uma pasta vazia e copiar este projeto para dentro da pasta clonada (nao copiar `Library`, `Temp`, `.git` antigo nem arquivos ISO):

```bash
git clone https://github.com/guibatoni/forbidden-memories-reborn.git
cd forbidden-memories-reborn
git add .
git commit -m "feat: prototype 0.3 card interactions and duel events"
git push origin main
```

O ZIP desta entrega foi preparado localmente; **nada foi publicado automaticamente no repositório público**. Nao compartilhe tokens, secrets ou arquivos originais licenciados.