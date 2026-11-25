# Prototype 0.24 — Fusões, teclado/mouse e colocação clássica

## Sequência de invocação

1. Escolha a carta com clique ou com as setas e **Z**.
2. Para fusão, selecione os materiais em ordem:
   - **Mouse:** Shift + clique acrescenta o material; cartões recebem
     números 1, 2, 3, 4, 5 no leque.
   - **Teclado:** esquerda/direita navegam na mão, **seta para cima**
     acrescenta o material atual à sequência; **seta para baixo**
     desmarca o material atual sem alterar a ordem dos outros.
   - O Reborn mostra a quantidade de **receitas válidas realmente
     executadas**, não apenas a carta que sobra após fusão fracassada.
3. **Duplo clique** no cartão selecionado escolhe a primeira zona livre
   e abre a janela de confirmação. Pelo teclado, **Z** seleciona a carta,
   **Z novamente** prepara a colocação.
4. Após selecionar o alvo, escolha **Guardian Star** quando houver duas;
   em seguida, **Face Up** ou **Face Down**.
5. Para monstros, aparece uma **carta visual** que pode ser girada
   ANTES da jogada. **Q = vertical (ATK)**, **E = horizontal (DEF)**.
   É possível clicar na carta para alternar. **Z** confirma; **X**
   cancela sem gastar a carta. Nessa etapa não há mais botões textuais
   separados “ATAQUE” e “DEFESA”.
6. Somente após confirmar é executada **uma** jogada no motor
   e a carta voa da mão à zona pela curva animada. Depois disso,
   Q/E continuam podendo alternar posição legalmente até atacar,
   sem revelar a carta Face Down.

## Regra automática das zonas

O Reborn procura exclusivamente o primeiro slot vazio da fileira
correspondente ao resultado final da seleção:

    [livre, livre, livre, livre, livre] -> slot 1
    [ocupado, livre, livre, livre, livre] -> slot 2
    [ocupado, ocupado, livre, livre, livre] -> slot 3

O resultado final **Monster** usa a fileira de monstros; magia,
ritual, Trap e Equip preparados usam a fileira de suportes. A seleção
automática nunca substitui um monstro nem uma Trap existente.
Com todos os slots ocupados, selecione explicitamente uma carta
existente para tentar uma fusão sobre ela, um Equip ou uma substituição
permitida pelo motor.

Um **duplo clique em uma magia** abre Face Up ou Face Down.
Face Up só aparece habilitado para um efeito validado. Trap e Equip
também abrem confirmação: quando não há efeito Face Up verificado,
podem ser setados Face Down, sem execução fictícia de efeito.

## Fusões de mão e de campo

O motor continua usando apenas `FusionCatalog`, extraído do jogo;
a seleção do jogador agora persiste corretamente enquanto navega
com o teclado. As prévias de fusão são puras, e a ação final manda
os índices na ordem escolhida para o mesmo `DuelMatch.TryPlay()`
usado pela entrada por mouse.

Para fundir com uma carta que já está na mesa, selecione os materiais
e depois a zona ocupada por seu monstro. Este monstro é o material #1,
como no original; o motor usa `TryPlayOnField()`.
Fusão bem-sucedida dispara uma cerimônia e **rastros luminosos
independentes, em sequência**, desde cada material até o resultado.
Esses VFX não manipulam cartas, LP nem o resultado da fusão.

O HUD mostra **Fusões: N**. Se N for zero, o programa avisa
que o perfil original carregado não trouxe receitas verificadas.
Nesse caso, reimporte o disco com `tools/IMPORTAR_ORIGINAL.ps1`
e confirme no Unity que `original_export.json` tem o array
`fusions`. Não geramos receitas a partir de nome, tipo ou ataque.

## Equip diretamente sobre o monstro

Com **Equip selecionado na mão**, clique em um monstro próprio
revelado ou navegue até ele pelo teclado:
- **Equipar agora:** o Equip compatível concede o bônus e vai
  ao cemitério imediatamente.
- **Setar Face Down:** a carta fica no slot de suporte
  correspondente à coluna do monstro. Visualmente, seu verso
  fica parcialmente sobreposto acima da carta do monstro.
  A carta NÃO aplica bônus até ser ativada legalmente em turno
  posterior. Se a zona de suporte correspondente estiver ocupada,
  essa opção não é oferecida.
- É possível clicar no Equip sobreposto ou navegar até o slot
  Support para selecioná-lo. O motor mantém o cartão na fileira
  de suporte para que nenhuma regra seja duplicada.
- Ao ativar Equip já setado, o jogador pode fazê-lo mesmo depois
  de baixar sua carta obrigatória, desde que as regras permitam;
  isso não consome nova jogada de colocação.

**Não se pode** equipar um monstro oculto, atribuir um bônus
não validado ou obrigar o Equip a substituir um slot ocupado.

## Controles padrão

| Tecla | Ação |
|-------|------|
| Espaço | Passa o turno (após baixar ao menos uma carta) |
| Q / E | ATK / DEF na prévia; altera posição do monstro após jogar |
| Z | Seleciona, confirma, escolhe slot ou executa ataque |
| X | Cancela materiais/menus sem consumir carta |
| S | Abre informações da carta em foco |
| W | Vai para o campo adversário; novamente volta à mão |
| ← / → | Navega entre cartas na mão ou slots da mesa |
| ↑ com foco na mão | Marca cartão numerado para fusão |
| ↓ com foco na mão | Desmarca o cartão atual |
| ↑ / ↓ com foco no campo | Alterna linha de suporte/monstros e lados |
| F1 | Abre/fecha as configurações de teclado |

No menu **CONFIGURAR TECLADO** no canto inferior direito, clique
em qualquer tecla para substituí-la por outra. O sistema detecta
conflitos entre comandos e salva as escolhas no `PlayerPrefs` do
Unity neste computador. O botão “Restaurar padrão” apaga os mapeamentos
customizados. F1 fica reservado como tecla de acesso de emergência
aos controles. É possível jogar inteiramente pelo teclado ou mouse.

## Testes automatizados

O Core Smoke testa fusão de mão (ordem, preview, carta resultante,
consumo dos dois materiais), fusão usando material em campo,
slot 1 ocupado -> slot 2 livre e Equip setado na coluna do monstro
sem bônus prematuro. Os scripts de CI também cobrem cancelamento,
configuração de teclas, menus pré-jogada e fonte de dados validada.

**A CI não compila o Game View do Unity.** Ainda precisamos verificar
no Unity 6000.3.28f1:
1. Selecionar materiais 1+2 com ↑ e observar o resultado real;
   testar também Shift+clique e a seleção em outra ordem.
2. Duplo clique em carta de monstro; slot 1 ocupado deve
   selecionar slot 2 e abrir a confirmação.
3. Q / E devem girar a carta antes de colocá-la; cancelar com X
   não pode usar carta nem avançar a fase.
4. Confirmar Face Down + ATK ou DEF deve manter o verso oculto.
5. Duplo clique em Magia, Trap e Equip; efeitos só quando verificados.
6. Arrastar Equip para cima de monstro, escolher “Setar Face Down”,
   avançar um turno, ativar. Confirmar visual e logicamente os buffs.
7. Abrir F1, mudar Z para outra tecla, confirmar operação e
   reiniciar Play para conferir persistência. Testar restauração.
8. Jogar com mouse desconectado, teclado apenas, e vice-versa.
9. Resoluções 16:9 e 9:16; quadro visual e escala do modal.
10. Confirmar ausencia de erros no Console e que o leque da mão
    aprovado não foi alterado.

Fonte de dados e fusões: `OriginalContentLoader`, `FusionCatalog`,
`DuelMatch.FieldFusion`, `tools/import_fm_disc.py`. Arte, ROM e perfis
privados permanecem fora do repositório público.
