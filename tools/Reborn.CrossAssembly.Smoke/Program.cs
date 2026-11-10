using System;
using System.Collections.Generic;
using System.Reflection;
using Reborn.Core;

internal static class Program
{
    private static int Main()
    {
        var deck = new List<CardDefinition>();
        for (int i = 0; i < DuelMatch.DeckSize; i++)
            deck.Add(new CardDefinition("test-" + i, "Test " + i,
                CardKind.Monster, 1800, 1200, "",
                GuardianStar.Sun, GuardianStar.Moon));

        // This project is a DIFFERENT assembly from Reborn.Core.PublicApi.
        // A nonpublic (internal) Core member cannot compile at this callsite.
        var duel = new DuelMatch(deck, deck);
        if (duel.Player.MonsterCount != 0 || duel.Opponent.MonsterCount != 0)
            throw new Exception("New duel must start with no monsters on the field");
        if (!duel.TryPlay(DuelSide.Player, new[] { 0 }, FieldLane.Monster, 0,
            BattlePosition.Attack, GuardianStar.Sun, out var reason))
            throw new Exception("Failed to summon: " + reason);
        if (duel.Player.MonsterCount != 1 || duel.Player.Monster(0) == null)
            throw new Exception("Public MonsterCount did not reflect a monster summon");

        var property = typeof(MatchSide).GetProperty("MonsterCount",
            BindingFlags.Public | BindingFlags.Instance);
        if (property == null || !property.CanRead || property.SetMethod != null)
            throw new Exception("MonsterCount must be public and read-only");
        if (!duel.TryBeginBattle(DuelSide.Player, out reason) ||
            !duel.TryPreviewAttack(DuelSide.Player, 0, -1, out var preview, out reason) ||
            !preview.IsDirect || preview.DamageToDefender != 1800)
            throw new Exception("Cross-assembly combat preview failed: " + reason);

        Console.WriteLine("PASS: separately compiled Core public API, read-only MonsterCount, legal summon and combat forecast");
        return 0;
    }
}
