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
            bool actionable = false;
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
            {
                var monster = match.Player.Monster(i);
                if (monster == null) continue;
                if (monster.Position == BattlePosition.Attack && !monster.AttackedThisTurn)
                    actionable = true;
                var opposite = monster.Position == BattlePosition.Attack
                    ? BattlePosition.Defense : BattlePosition.Attack;
                if (match.CanChangePosition(DuelSide.Player, i, opposite, out _))
                    actionable = true;
            }
            if (!actionable)
            {
                Announce("Sem ataque disponivel. Passando turno automaticamente.");
                TryPassPlayerTurn();
            }
            else Announce("Batalha liberada: clique no seu monstro e depois no alvo.");
        }

        public void AfterPlayerAttack()
        {
            if (match == null || InputLocked ||
                match.CurrentSide != DuelSide.Player ||
                match.Phase != MatchPhase.Battle) return;
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
            {
                var monster = match.Player.Monster(i);
                if (monster == null) continue;
                if (monster.Position == BattlePosition.Attack && !monster.AttackedThisTurn)
                    return;
                var next = monster.Position == BattlePosition.Attack
                    ? BattlePosition.Defense : BattlePosition.Attack;
                if (match.CanChangePosition(DuelSide.Player, i, next, out _))
                    return;
            }
            Announce("Todos os ataques utilizados. Passando turno.");
            TryPassPlayerTurn();
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
            var monster = match.Player.Monster(SelectedFieldMonster);
            if (monster == null) return;
            var next = monster.Position == BattlePosition.Attack
                ? BattlePosition.Defense : BattlePosition.Attack;
            if (match.TryChangePosition(DuelSide.Player, SelectedFieldMonster,
                next, out var reason))
                Announce("Monstro alterado para " + next + ".");
            else Announce(reason);
        }

        public void TryActivateSelectedSupport()
        {
            if (match == null || SelectedSupportSlot < 0) return;
            if (!match.CanActivateSupport(DuelSide.Player, SelectedSupportSlot, out var reason))
                Announce(reason);
        }
    }
}
