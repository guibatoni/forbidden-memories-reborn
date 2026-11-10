# Reborn Prototype 0.19 — Primeiros efeitos reais do PS1

## Estado do projeto
Este marco deriva do backup 0.18.1 e **nao modifica main**.
O objetivo e executar os primeiros efeitos comprovados no duelo sem
confundir o texto do DUELTRACE com um interpretador de instrucoes.

## Efeitos adicionados (IDs da edicao Original PS1)
- #336 Dark Hole — remove todas as cartas dos dois campos.
- #337 Raigeki — destrói os monstros do oponente.
- #338 Mooyan Curry — recupera 200 LP.
- #339 Red Medicine — recupera 500 LP.
- #340 Goblin's Secret Remedy — recupera 1.000 LP.
- #341 Soul of the Pure — recupera 2.000 LP.
- #342 Dian Keto the Cure Master — recupera 5.000 LP.
- #343 Sparks — causa 50 LP de dano direto.
- #344 Hinotama — causa 100 LP de dano direto.
- #345 Final Flame — causa 200 LP de dano direto.
- #346 Ookazi — causa 500 LP de dano direto.
- #347 Tremendous Fire — causa 1.000 LP de dano direto.

Referencias das regras originais:
- https://gamefaqs.gamespot.com/ps/561010-yu-gi-oh-forbidden-memories/faqs/21391
- https://ftp.yugipedia.com/wiki/Face_up
- https://ftp.yugipedia.com/wiki/Face-down

As 12 rotinas ficam em `VerifiedSpellCatalog.OriginalPs1Core()`, com
um ID e uma operacao estruturada. Os textos `effectText` e `description`
do DUELTRACE **nunca sao interpretados nem executados**.
A lista so e ativada quando o catalogo carregado informa
`edition=original-ps1*`. Alteracoes de efeitos nos mods exigem
validacao propria antes de adicionar regras especiais.

## Fluxo do jogador
1. Escolha a carta de magia na mao e a zona de suporte disponivel.
2. Escolha Face Up: se o ID for reconhecido, o efeito e aplicado no mesmo
   ato da colocacao; a magia vai para o cemiterio e nao permanece setada.
   Se nao houver regra verificada, a acao e recusada **sem perder a carta**.
3. Escolha Face Down: a carta permanece oculta no suporte.
4. No turno seguinte do dono, selecione o suporte e pressione
   "Ativar magia setada" no painel. O efeito verificado e aplicado.
   Nao e possivel ativa-lo duas vezes nem ativar no turno em que foi setado.
5. A jogada de baixar uma carta continua obrigatoria e limitada a uma
   por turno; ativar uma magia anteriormente setada nao consome uma
   segunda acao de baixar carta.

## Apresentacao
- VFX original procedural de magia ativada sobre a zona de suporte.
- Eventos `LifeChanged` positivos geram texto verde `+N LP`;
  negativos geram `-N LP`.
- Destruicoes de cartas de suporte agora apontam para a propria
  zona (em vez de tentar animar um monstro inexistente).
- O leque, a selecao Guardian/Face/ATK-DEF, as fusoes 0.16–0.18,
  os rituais e a Face Down 0.18.1 permanecem inalterados.

## Proximos efeitos ainda pendentes
- Efeitos de equipamentos por compatibilidade de alvo, incluindo
  seus valores de ATK/DEF extraidos e comprovados da ISO original.
- Traps que disparam automaticamente durante o ataque ou outros eventos.
- Magias de terreno (Forest, Mountain, etc.), estado global do campo,
  alteracao de atributos e duracao de efeitos.
- Magias com outras rotinas: Stop Defense, Swords of Revealing Light,
  Spellbinding Circle, Dark-Piercing Light e efeitos especificos.
- Integrar ao motor de combate buffs de campo/equips e o calculo de rank.
  Esses sistemas nao devem usar a descricao da carta como codigo.

## Validacao
CI compila o Core no .NET 8, roda regressao deterministica e valida
as fronteiras UI; NUnit Editor adicional cobre cura, dano, cemiterio,
efeitos desconhecidos, suporte preparado e destruicoes.

Ainda precisa ser testado no Unity 6000.3.28f1: selecionar e ativar
uma magia face-up; setar, passar a vez e ativar no turno seguinte;
observar particulas e floating LP; verificar que nao ha erro de compilacao
ou objeto grafico orfao. O GitHub Actions nao executa Game View do Unity.

Nao incluir ROM, efeitos nao verificados, imagens comerciais ou
perfis privados de mods no repositorio publico.
