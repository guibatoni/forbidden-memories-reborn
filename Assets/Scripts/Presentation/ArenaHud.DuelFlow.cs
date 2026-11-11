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

        // Kept for the mouse button: clicking toggles the current stance.
        public void TryChangeSelectedMonster()
        {
            if (match == null || SelectedFieldMonster < 0 ||
                SelectedFieldMonster >= DuelMatch.ZoneCount) return;
            var monster = match.Player.Monster(SelectedFieldMonster);
            if (monster == null) return;
            TrySetSelectedMonsterPosition(monster.Position == BattlePosition.Attack
                ? BattlePosition.Defense : BattlePosition.Attack);
        }

        // Keyboard Q = ATK and E = DEF, regardless of the current stance.
        // Repeated presses of the same key are intentional no-ops.
        public void TrySetSelectedMonsterPosition(BattlePosition position)
        {
            if (match == null || SelectedFieldMonster < 0 ||
                SelectedFieldMonster >= DuelMatch.ZoneCount || InputLocked ||
                match.CurrentSide != DuelSide.Player) return;
            int slot = SelectedFieldMonster;
            var monster = match.Player.Monster(slot);
            if (monster == null) return;
            if (monster.Position == position) return;
            if (match.TryChangePosition(DuelSide.Player, slot, position, out var reason))
            {
                // The Match.Changed observer clears the transient selection.
                // Restore the current slot for another Q/E without reselecting.
                SelectedFieldMonster = slot;
                SelectedAttacker = -1;
                Announce(position == BattlePosition.Attack ?
                    "Monstro em ATK. Q escolhe ATK; E escolhe DEF." :
                    "Monstro em DEF. Q escolhe ATK; E escolhe DEF.");
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
