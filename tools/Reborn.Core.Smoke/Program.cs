using System;
using System.Collections.Generic;
using System.Linq;
using Reborn.Core;

internal static class Program
{
    private static int passed;
    private static CardDefinition M(string id, int atk, int def, GuardianStar star = GuardianStar.None)
        => new CardDefinition(id, id, CardKind.Monster, atk, def, "smoke", star, star);
    private static CardDefinition S(string id)
        => new CardDefinition(id, id, CardKind.Spell, 0, 0, "smoke");
    private static IReadOnlyList<CardDefinition> Deck(params CardDefinition[] cards)
    {
        var result = new List<CardDefinition>(cards);
        while (result.Count < 40) result.Add(M("filler" + result.Count, 1000, 1000));
        return result;
    }
    private static DuelMatch Match(CardDefinition a, CardDefinition b)
        => new DuelMatch(Deck(a), Deck(b));
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL " + message);
        passed++;
        Console.WriteLine("OK " + message);
    }
    private static void Play(DuelMatch m, DuelSide side, GuardianStar star = GuardianStar.None)
    {
        if (!m.TryPlay(side, new [] { 0 }, FieldLane.Monster, 0,
            BattlePosition.Attack, star, out var reason)) throw new Exception(reason);
    }
    private static void Battle(DuelMatch m, DuelSide side)
    {
        if (!m.TryBeginBattle(side,out var reason)) throw new Exception(reason);
    }
    private static void End(DuelMatch m, DuelSide side)
    {
        if (!m.TryEndTurn(side,out var reason)) throw new Exception(reason);
    }
    private static void Main()
    {
        var m = Match(M("mine",2000,1500),M("theirs",1200,800));
        Check(m.Player.Hand.Count==5 && m.Opponent.Hand.Count==5 && m.Player.DeckCount==35,
            "two hands and deck counts");
        Check(!m.TryPlay(DuelSide.Player, new [] {0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,out _),"wrong zone rejection");
        Play(m,DuelSide.Player);
        Check(m.Player.Monster(0)?.Card.Id=="mine","summon and field state");
        Check(!m.TryPlay(DuelSide.Player,new [] {0},FieldLane.Monster,1,
            BattlePosition.Attack,GuardianStar.None,out _),"one play per turn");
        Battle(m,DuelSide.Player);
        long previewEvents = m.Events.Count;
        Check(m.CanAttack(DuelSide.Player,0,-1,out _) && m.Opponent.Life==8000,
            "attack preview is legal without resolving damage");
        Check(!m.Player.Monster(0).AttackedThisTurn && m.Events.Count==previewEvents,
            "attack preview cannot mark a monster or emit events");
        Check(m.TryAttack(DuelSide.Player,0,-1,out _) && m.Opponent.Life==6000,
            "direct attack and damage");
        Check(!m.TryAttack(DuelSide.Player,0,-1,out _),"one attack per monster per turn");
        Check(!m.CanAttack(DuelSide.Player,0,-1,out _),
            "attack preview rejects monsters already used this turn");
        End(m,DuelSide.Player);
        Check(m.CurrentSide==DuelSide.Opponent,"opponent turn");
        Check(SimpleOpponentAi.TryChoosePlay(m,out int chosenHand,out int chosenSlot) &&
            chosenHand >= 0 && chosenSlot >= 0,
            "AI exposes a legal summon choice before executing it");
        Check(!SimpleOpponentAi.TryChooseAttack(m,out _,out _),
            "AI refuses attacks until the battle phase");
        SimpleOpponentAi.TakeTurn(m);
        Check(m.CurrentSide==DuelSide.Player,"AI returns turn");
        Check(GuardianStarRules.Bonus(GuardianStar.Mercury,GuardianStar.Sun)==500 &&
            GuardianStarRules.Bonus(GuardianStar.Mercury,GuardianStar.Moon)==0,
            "guardian advantage");
        var fusion=new FusionCatalog();
        fusion.Add("a","b",M("result",2700,2100));
        var m2=new DuelMatch(Deck(M("a",1000,500),M("b",1200,500)),Deck(S("spell")),fusion);
        Check(m2.TryPlay(DuelSide.Player,new [] {0,1},FieldLane.Monster,0,
            BattlePosition.Attack,GuardianStar.None,out _) &&
            m2.Player.Monster(0)?.Card.Id=="result" && m2.Player.Graveyard.Count==2,
            "fusion by explicit data");
        Check(m2.Events.Select(e=>e.Sequence).SequenceEqual(m2.Events.Select(e=>e.Sequence).OrderBy(v=>v)),
            "ordered engine events");
        var m3=Match(M("big",8500,500),M("small",500,500));
        Play(m3,DuelSide.Player);Battle(m3,DuelSide.Player);
        m3.TryAttack(DuelSide.Player,0,-1,out _);
        Check(m3.Phase==MatchPhase.Finished && m3.Winner==DuelSide.Player,
            "win by zero life");
        var aiMatch = Match(M("player",1200,1000),M("opponent",1800,1000));
        End(aiMatch,DuelSide.Player);
        Check(SimpleOpponentAi.TryChoosePlay(aiMatch,out int aiHand,out int aiZone),
            "AI can expose one playable monster without moving it");
        Check(aiMatch.Opponent.Monster(aiZone) == null &&
            aiMatch.TryPlay(DuelSide.Opponent,new [] {aiHand},FieldLane.Monster,aiZone,
                BattlePosition.Attack,GuardianStar.None,out _),
            "AI selection and actual placement are separate steps");
        Battle(aiMatch,DuelSide.Opponent);
        Check(SimpleOpponentAi.TryChooseAttack(aiMatch,out int fromSlot,out int toSlot) &&
            toSlot == -1 && fromSlot == aiZone,
            "AI can preview its direct attack before resolving it");
        Check(aiMatch.Player.Life == 8000 &&
            aiMatch.TryAttack(DuelSide.Opponent,fromSlot,toSlot,out _) &&
            aiMatch.Player.Life == 6200,
            "AI attack applies damage only when the engine is invoked");

        var standardDeck = DemoDeck.Create();
        var shuffledA = DeckRandomizer.Shuffle(standardDeck, new Random(154));
        var shuffledB = DeckRandomizer.Shuffle(standardDeck, new Random(154));
        var shuffledC = DeckRandomizer.Shuffle(standardDeck, new Random(8472));
        Check(shuffledA.Count == 40 && shuffledA.Select(x => x.Id).OrderBy(x => x)
            .SequenceEqual(standardDeck.Select(x => x.Id).OrderBy(x => x)),
            "shuffle preserves all 40 physical cards");
        Check(shuffledA.Select(x => x.Id).SequenceEqual(shuffledB.Select(x => x.Id)),
            "seeded shuffle is reproducible for diagnostics");
        Check(!shuffledA.Select(x => x.Id).SequenceEqual(shuffledC.Select(x => x.Id)),
            "different seeds yield different hands");
        var cautious = Match(M("shield",2000,1400),M("weak",800,800));
        Play(cautious,DuelSide.Player);
        End(cautious,DuelSide.Player);
        Check(StrategicOpponentAi.TryChoosePlacement(cautious, out var decision) &&
            decision.HandIndices.Count >= 1 &&
            cautious.Opponent.Monster(decision.Slot) == null,
            "AI evaluates a legal placement without mutating the board");
        Check(cautious.Opponent.Hand.Count == 5 &&
            !StrategicOpponentAi.TryChooseAttack(cautious,out _,out _),
            "AI does not attack in main phase or consume its hand during planning");
        var wary = Match(M("strong",3500,3200),M("weak",500,500));
        Play(wary,DuelSide.Player);
        End(wary,DuelSide.Player);
        Check(wary.TryPlay(DuelSide.Opponent, new[] {0}, FieldLane.Monster, 0,
              BattlePosition.Attack, GuardianStar.None, out _) &&
              wary.TryBeginBattle(DuelSide.Opponent,out _) &&
              !StrategicOpponentAi.TryChooseAttack(wary,out _,out _),
              "AI avoids suicidal trades against stronger defenders");
        Console.WriteLine("PASS: " + passed + " deterministic smoke assertions");
    }
}
