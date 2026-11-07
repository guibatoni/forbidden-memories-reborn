using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>Deterministic baseline AI. Does not imitate official opponent logic.</summary>
    public static class SimpleOpponentAi
    {
        public static void TakeTurn(DuelMatch match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (match.Phase == MatchPhase.Finished || match.CurrentSide != DuelSide.Opponent) return;
            var rival = match.Opponent;
            int bestIndex = -1;
            for (int i = 0; i < rival.Hand.Count; i++)
                if (rival.Hand[i].Kind == CardKind.Monster &&
                    (bestIndex < 0 || rival.Hand[i].Attack > rival.Hand[bestIndex].Attack))
                    bestIndex = i;
            if (bestIndex >= 0)
                for (int zone = 0; zone < DuelMatch.ZoneCount; zone++)
                    if (rival.Monster(zone) == null)
                    {
                        var card = rival.Hand[bestIndex];
                        match.TryPlay(DuelSide.Opponent, new [] { bestIndex }, FieldLane.Monster,
                            zone, BattlePosition.Attack, card.StarA, out _);
                        break;
                    }
            match.TryBeginBattle(DuelSide.Opponent, out _);
            for (int zone = 0; zone < DuelMatch.ZoneCount; zone++)
            {
                if (match.Phase == MatchPhase.Finished) break;
                var monster = rival.Monster(zone);
                if (monster == null || monster.Position == BattlePosition.Defense || monster.AttackedThisTurn)
                    continue;
                int target = -1;
                int weakest = int.MaxValue;
                for (int i = 0; i < DuelMatch.ZoneCount; i++)
                {
                    var foe = match.Player.Monster(i);
                    if (foe != null)
                    {
                        int power = foe.Position == BattlePosition.Attack ? foe.Card.Attack : foe.Card.Defense;
                        if (power < weakest) { target = i; weakest = power; }
                    }
                }
                match.TryAttack(DuelSide.Opponent, zone, target, out _);
            }
            if (match.Phase != MatchPhase.Finished)
                match.TryEndTurn(DuelSide.Opponent, out _);
        }
    }
}
