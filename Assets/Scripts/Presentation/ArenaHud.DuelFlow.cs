using Reborn.Core;

namespace Reborn.Presentation
{
    /// <summary>
    /// Player UX flow only. All legal actions still go through DuelMatch.
    /// Placing a card enters battle automatically. Passing is explicit when
    /// a usable monster remains; an empty battle can end automatically.
    /// </summary>
    public sealed partial class ArenaHud
    {
        public bool PlaceFaceDown { get; private set; }
        public int SelectedFieldMonster { get; set; } = -1;
        public int SelectedSupportSlot { get; set; } = -1;

        public void ConfigurePlacement(CardDefinition card)
        {
            if (card == null) return;
            PlaceFaceDown = card.Kind != CardKind.Monster;
            UseDefense = false;
            UseSecondaryStar = false;
        }

        public void AfterHandPlacement()
        {
            if (match == null || !DuelModeActive || match.CurrentSide != DuelSide.Player ||
                match.Phase != MatchPhase.Main) return;
            if (!match.TryBeginBattle(DuelSide.Player, out var reason))
            {
                Announce(reason);
                return;
            }
            AttackMode = true;
            SelectedAttacker = -1;
            SelectedFieldMonster = -1;
            // Unlike the older prototype, never pass automatically.
            // SPACE is the explicit START equivalent, even in empty battle.
            Announce("Carta baixada. Q/E alterna ATK/DEF; Espaco passa o turno.");
        }

        public void AfterPlayerAttack()
        {
            if (match == null || InputLocked ||
                match.CurrentSide != DuelSide.Player ||
                match.Phase != MatchPhase.Battle) return;
            Announce("Ataque concluido. Escolha outra acao ou pressione Espaco.");
        }

        public void TryPassPlayerTurn()
        {
            if (match == null || !DuelModeActive || InputLocked ||
                match.CurrentSide != DuelSide.Player ||
                match.Phase == MatchPhase.Finished) return;
            if (!match.TryEndTurn(DuelSide.Player, out var error))
            {
                Announce(error);
                return;
            }
            AttackMode = false;
            SelectedAttacker = -1;
            SelectedFieldMonster = -1;
            SelectedSupportSlot = -1;
            PreviewTargetSlot = -1;
            if (match.Phase == MatchPhase.Finished) Announce("Duelo encerrado.");
            else
            {
                OpponentBusy = true;
                Announce("Turno adversario...");
                OpponentTurnRequested?.Invoke();
            }
        }

        public void TryChangeSelectedMonster()
        {
            if (match == null || SelectedFieldMonster < 0 || InputLocked ||
                match.CurrentSide != DuelSide.Player) return;
            int slot = SelectedFieldMonster;
            var monster = match.Player.Monster(slot);
            if (monster == null) return;
            var next = monster.Position == BattlePosition.Attack
                ? BattlePosition.Defense : BattlePosition.Attack;
            if (match.TryChangePosition(DuelSide.Player, slot, next, out var reason))
            {
                // Match.Changed clears transient selections; keep the same card
                // focused so consecutive Q/E presses work without re-clicking it.
                SelectedFieldMonster = slot;
                SelectedAttacker = -1;
                Announce(next == BattlePosition.Attack ?
                    "Monstro em ATK. Escolha um alvo ou use Q/E para DEF." :
                    "Monstro em DEF. Use Q/E para ATK ou Espaco para encerrar.");
            }
            else Announce(reason);
        }

        public void TryActivateSelectedSupport()
        {
            if (match == null || SelectedSupportSlot < 0 || InputLocked ||
                match.CurrentSide != DuelSide.Player) return;
            int selected = SelectedSupportSlot;
            var card = match.Player.Support(selected);
            if (card == null) return;
            if (!match.TryActivateSupport(DuelSide.Player, selected, out var reason))
            {
                Announce(reason);
                return;
            }
            SelectedSupportSlot = -1;
            Announce("Magia ativada: " + card.Name + ".");
        }
    }
}
