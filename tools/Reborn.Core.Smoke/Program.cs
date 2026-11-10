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

        var weights = new int[722];
        for (int i = 0; i < 20; i++) weights[i] = i + 1;
        var pool = new WeightedOpponentPool(7, "TEST_OPPONENT", "pool-7", weights);
        var available = new List<CardDefinition>();
        for (int i = 1; i <= 20; i++)
            available.Add(M(i.ToString("000"), 1000 + i, 900));
        var dealt1 = pool.Deal(available, new Random(42));
        var dealt2 = pool.Deal(available, new Random(42));
        var dealt3 = pool.Deal(available, new Random(79));
        Check(dealt1.Count == 40 && dealt1.GroupBy(x => x.Id).All(x => x.Count() <= 3),
            "opponent weighted deck has 40 cards, max three copies");
        Check(dealt1.Select(x => x.Id).SequenceEqual(dealt2.Select(x => x.Id)),
            "weighted opponent pool is reproducible with a diagnostic seed");
        Check(!dealt1.Select(x => x.Id).SequenceEqual(dealt3.Select(x => x.Id)),
            "different seeds generate different weighted decks");
        Check(weights[0] == 1 && pool.WeightForCardId(1) == 1 &&
            pool.WeightForCardId(722) == 0, "card ID to weight mapping is one-based");
        Check(!dealt1.Any(x => x == null) && available.Count == 20,
            "dealing does not change the source catalog");
        var invalidWeights = new int[722];
        invalidWeights[0] = 2048;
        bool rejected = false;
        try { new WeightedOpponentPool(0,"INVALID","invalid", invalidWeights); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "incomplete weighted pool is rejected");

        var brainProfile = new AiLearningState("demo:opponent");
        var brain = new AdaptiveOpponentBrain(new Random(2459));
        brain.StartDuel(brainProfile);
        var adaptiveMatch = Match(M("player",1500,1300),M("opponent",2600,1900));
        Check(!brain.TryChoosePlacement(adaptiveMatch, out _),
            "adaptive brain refuses placement outside opponent turn");
        End(adaptiveMatch, DuelSide.Player);
        int adaptiveEvents = adaptiveMatch.Events.Count;
        Check(brain.TryChoosePlacement(adaptiveMatch, out var adaptivePlan) &&
            adaptivePlan != null && adaptivePlan.Slot >= 0 && adaptivePlan.Slot < 5,
            "adaptive brain finds legal placement");
        Check(adaptiveMatch.Events.Count == adaptiveEvents &&
            adaptiveMatch.Opponent.Monster(adaptivePlan.Slot) == null,
            "adaptive planning does not mutate the duel");
        Check(adaptiveMatch.CanPlay(DuelSide.Opponent, adaptivePlan.HandIndices,
            FieldLane.Monster,adaptivePlan.Slot,adaptivePlan.Star,out _),
            "adaptive placement is checked by the rules engine");
        Check(adaptiveMatch.TryPlay(DuelSide.Opponent,adaptivePlan.HandIndices,
            FieldLane.Monster,adaptivePlan.Slot,adaptivePlan.Position,
            adaptivePlan.Star,out _),
            "adaptive placement executes through normal duel actions");
        Check(!brain.TryChooseAttack(adaptiveMatch,out _,out _),
            "adaptive brain refuses combat outside battle phase");
        Battle(adaptiveMatch,DuelSide.Opponent);
        Check(brain.TryChooseAttack(adaptiveMatch,out var adaptiveFrom,out var adaptiveTo) &&
            adaptiveMatch.CanAttack(DuelSide.Opponent,adaptiveFrom,adaptiveTo,out _),
            "adaptive attack is legal and only reads game state");

        var profileBefore = new AiLearningState("demo:untouched");
        brain.StartDuel(profileBefore);
        Check(!brain.RecordCompletedDuel(adaptiveMatch) &&
            profileBefore.duels == 0, "unfinished matches do not train the profile");
        var finishedForLearning = Match(M("winning-player",9000,8000),
            M("losing-opponent",600,500));
        Play(finishedForLearning,DuelSide.Player);
        Battle(finishedForLearning,DuelSide.Player);
        finishedForLearning.TryAttack(DuelSide.Player,0,-1,out _);
        Check(finishedForLearning.Phase == MatchPhase.Finished &&
            brain.RecordCompletedDuel(finishedForLearning) &&
            profileBefore.duels == 1 && profileBefore.playerAttacks == 1,
            "a completed duel updates the player's observed attack count");
        Check(!brain.RecordCompletedDuel(finishedForLearning) &&
            profileBefore.duels == 1, "the same duel never trains twice");
        Check(profileBefore.wins == 0 && profileBefore.previousTactic >= 0 &&
            profileBefore.tacticValues[profileBefore.previousTactic] < 0,
            "a loss changes strategy preference without changing another profile");
        var seenTactics = new HashSet<AiTactic>();
        for (int i = 0; i < 50; i++)
        {
            brain.StartDuel(profileBefore);
            seenTactics.Add(brain.Tactic);
        }
        Check(seenTactics.Count >= 3,
            "adaptive opponent explores multiple strategies on repeated duels");
        var malformed = new AiLearningState("demo:corrupted");
        malformed.tacticValues = new[] { float.NaN };
        malformed.duels = -20;
        malformed.wins = 20;
        malformed.Sanitize();
        Check(malformed.tacticValues.Length == 5 && malformed.duels == 0 &&
            malformed.wins == 0, "corrupted saved learning data is safely sanitized");
        Check(brainProfile.duels == 0,
            "learning profiles remain isolated per opponent");

        var defenseNormal = Match(M("atk2100",2100,1200),M("def1800",500,1800));
        Play(defenseNormal,DuelSide.Player);
        End(defenseNormal,DuelSide.Player);
        Check(defenseNormal.TryPlay(DuelSide.Opponent,new[] {0},FieldLane.Monster,0,
            BattlePosition.Defense,GuardianStar.None,out _),
            "defender can enter defense mode before 2100 vs 1800 combat");
        End(defenseNormal,DuelSide.Opponent);
        Battle(defenseNormal,DuelSide.Player);
        long normalEvents = defenseNormal.Events.Count;
        Check(defenseNormal.TryPreviewAttack(DuelSide.Player,0,0,out var normalForecast,out _) &&
            normalForecast.EffectiveAttack == 2100 &&
            normalForecast.EffectiveDefense == 1800 &&
            normalForecast.DefenderDestroyed && normalForecast.DamageToDefender == 0,
            "2100 ATK destroys 1800 DEF without Guardian advantage");
        Check(defenseNormal.Events.Count == normalEvents &&
            defenseNormal.Opponent.Monster(0) != null &&
            !defenseNormal.Player.Monster(0).AttackedThisTurn,
            "combat preview is strictly read-only");
        Check(defenseNormal.TryAttack(DuelSide.Player,0,0,out _) &&
            defenseNormal.Opponent.Monster(0) == null &&
            defenseNormal.Opponent.Graveyard.Count == 1 &&
            defenseNormal.Opponent.Life == 8000,
            "defense monster destruction creates graveyard event without LP damage");

        var defenseGuardian = Match(M("atkSun",2100,1200,GuardianStar.Sun),
            M("defMercury",500,1800,GuardianStar.Mercury));
        Play(defenseGuardian,DuelSide.Player,GuardianStar.Sun);
        End(defenseGuardian,DuelSide.Player);
        Check(defenseGuardian.TryPlay(DuelSide.Opponent,new[] {0},FieldLane.Monster,0,
            BattlePosition.Defense,GuardianStar.Mercury,out _),
            "Guardian defender can enter defense mode");
        End(defenseGuardian,DuelSide.Opponent);
        Battle(defenseGuardian,DuelSide.Player);
        Check(defenseGuardian.TryPreviewAttack(DuelSide.Player,0,0,
            out var boostedDefense,out _) &&
            boostedDefense.DefenderBase == 1800 &&
            boostedDefense.DefenderGuardianBonus == 500 &&
            boostedDefense.EffectiveDefense == 2300 &&
            !boostedDefense.DefenderDestroyed &&
            boostedDefense.DamageToAttacker == 200,
            "defender Guardian Star adds 500 to defense against 2100 ATK");
        Check(defenseGuardian.TryAttack(DuelSide.Player,0,0,out _) &&
            defenseGuardian.Opponent.Monster(0) != null &&
            defenseGuardian.Player.Monster(0) != null &&
            defenseGuardian.Player.Life == 7800,
            "2100 attack versus 2300 effective DEF does not destroy defender");

        var equalDef = Match(M("equalAtk",2100,1000),M("equalDef",300,2100));
        Play(equalDef,DuelSide.Player);
        End(equalDef,DuelSide.Player);
        equalDef.TryPlay(DuelSide.Opponent,new[] {0},FieldLane.Monster,0,
            BattlePosition.Defense,GuardianStar.None,out _);
        End(equalDef,DuelSide.Opponent);
        Battle(equalDef,DuelSide.Player);
        Check(equalDef.TryPreviewAttack(DuelSide.Player,0,0,
            out var equalForecast,out _) &&
            !equalForecast.DefenderDestroyed &&
            equalForecast.DamageToAttacker == 0,
            "equal ATK versus DEF does not destroy or cause LP damage");
        var guardedDefender = Match(
            M("mercuryDef",600,1800,GuardianStar.Mercury),
            M("sunAttack",2100,900,GuardianStar.Sun));
        Check(guardedDefender.TryPlay(DuelSide.Player,new[]{0},
            FieldLane.Monster,0,BattlePosition.Defense,
            GuardianStar.Mercury,out _),
            "Guardian defender can be placed in defense");
        End(guardedDefender,DuelSide.Player);
        Check(guardedDefender.TryPlay(DuelSide.Opponent,new[]{0},
            FieldLane.Monster,0,BattlePosition.Attack,
            GuardianStar.Sun,out _),
            "adaptive attacker can be summoned");
        Battle(guardedDefender,DuelSide.Opponent);
        var tactical = new AdaptiveOpponentBrain(new Random(400));
        tactical.StartDuel(new AiLearningState("tactical-guardian"));
        long guardEvents = guardedDefender.Events.Count;
        Check(guardedDefender.TryPreviewAttack(DuelSide.Opponent,0,0,
            out var badCombat,out _) &&
            badCombat.DefenderGuardianBonus == 500 &&
            badCombat.DamageToAttacker == 200,
            "Guardian 500 makes 2100 ATK lose LP against 1800 DEF");
        Check(!tactical.TryChooseAttack(guardedDefender,out _,out _) &&
            guardedDefender.Events.Count == guardEvents &&
            !guardedDefender.Opponent.Monster(0).AttackedThisTurn,
            "adaptive AI refuses losing defense, without mutating state");

        var favorableDefender = Match(
            M("moonDef",600,1800,GuardianStar.Moon),
            M("sunAttack",2100,900,GuardianStar.Sun));
        favorableDefender.TryPlay(DuelSide.Player,new[]{0},
            FieldLane.Monster,0,BattlePosition.Defense,GuardianStar.Moon,out _);
        End(favorableDefender,DuelSide.Player);
        favorableDefender.TryPlay(DuelSide.Opponent,new[]{0},
            FieldLane.Monster,0,BattlePosition.Attack,GuardianStar.Sun,out _);
        Battle(favorableDefender,DuelSide.Opponent);
        Check(favorableDefender.TryPreviewAttack(DuelSide.Opponent,0,0,
            out var goodCombat,out _) &&
            goodCombat.EffectiveAttack == 2600 &&
            goodCombat.DefenderDestroyed,
            "Guardian 500 advantage raises 2100 ATK to 2600");
        Check(tactical.TryChooseAttack(favorableDefender,out int tacticalAttacker,
            out int tacticalTarget) && tacticalAttacker == 0 && tacticalTarget == 0,
            "adaptive AI chooses a legal winning attack against defense");
        Check(favorableDefender.TryAttack(DuelSide.Opponent,tacticalAttacker,
            tacticalTarget,out _) &&
            favorableDefender.Player.Monster(0) == null,
            "shared forecast agrees with actual combat destruction");

        var targetSafety = Match(M("p",900,900),M("o",1400,900));
        End(targetSafety,DuelSide.Player);
        targetSafety.TryPlay(DuelSide.Opponent,new[]{0},FieldLane.Monster,0,
            BattlePosition.Attack,GuardianStar.None,out _);
        Battle(targetSafety,DuelSide.Opponent);
        Check(!targetSafety.CanAttack(DuelSide.Opponent,0,3,out _) &&
            !targetSafety.TryPreviewAttack(DuelSide.Opponent,0,3,out _,out _),
            "clicking an empty zone cannot turn into a direct attack");
        Check(targetSafety.TryPreviewAttack(DuelSide.Opponent,0,-1,
            out var allowedDirect,out _) && allowedDirect.IsDirect,
            "explicit direct attack is legal only on an empty opposing board");


        // The low attacker must destroy the DEF wall first so the stronger
        // attacker can go direct. Greedy attack scoring could waste the
        // stronger attacker on a defender without inflicting LP damage.
        var sequenceMatch = new DuelMatch(
            Deck(M("wall", 200, 1800)),
            Deck(M("small", 2100, 900), M("strong", 3200, 1500)));
        Check(sequenceMatch.TryPlay(DuelSide.Player, new[] {0},
            FieldLane.Monster, 0, BattlePosition.Defense,
            GuardianStar.None, out _), "sequence wall is summoned in defense");
        End(sequenceMatch, DuelSide.Player);
        Check(sequenceMatch.TryPlay(DuelSide.Opponent, new[] {0},
            FieldLane.Monster, 0, BattlePosition.Attack,
            GuardianStar.None, out _), "first AI attacker is summoned");
        End(sequenceMatch, DuelSide.Opponent);
        End(sequenceMatch, DuelSide.Player);
        Check(sequenceMatch.TryPlay(DuelSide.Opponent, new[] {0},
            FieldLane.Monster, 1, BattlePosition.Attack,
            GuardianStar.None, out _), "second AI attacker is summoned");
        Check(!MultiAttackPlanner.TryPlan(sequenceMatch,
            AiTactic.Balanced, new Random(90), out _),
            "multistep planner refuses to attack outside Battle");
        Battle(sequenceMatch, DuelSide.Opponent);
        int beforePlanEvents = sequenceMatch.Events.Count;
        int beforePlanLife = sequenceMatch.Player.Life;
        Check(MultiAttackPlanner.TryPlan(sequenceMatch, AiTactic.Balanced,
            new Random(90), out var sequence) &&
            sequence.Steps.Count == 2 && sequence.Steps[0].AttackerSlot == 0 &&
            sequence.Steps[0].TargetSlot == 0 &&
            sequence.Steps[1].AttackerSlot == 1 &&
            sequence.Steps[1].TargetSlot == -1 &&
            sequence.ExpectedDamage == 3200 &&
            sequence.ExpectedDestroyedDefenders == 1,
            "planner clears 1800 DEF with 2100 ATK, preserves 3200 direct attack");
        Check(sequenceMatch.Events.Count == beforePlanEvents &&
            sequenceMatch.Player.Life == beforePlanLife &&
            !sequenceMatch.Opponent.Monster(0).AttackedThisTurn &&
            !sequenceMatch.Opponent.Monster(1).AttackedThisTurn,
            "planning multiple attacks never changes duel state");
        var sequenceBrain = new AdaptiveOpponentBrain(new Random(92));
        sequenceBrain.StartDuel(new AiLearningState("multi-sequence"));
        Check(sequenceBrain.TryChooseAttack(sequenceMatch, out var firstSlot,
            out var firstTarget) && firstSlot == 0 && firstTarget == 0 &&
            sequenceBrain.LastAttackPlan.Steps.Count == 2,
            "adaptive AI applies the multistep plan rather than single greedy attack");
        Check(sequenceMatch.TryAttack(DuelSide.Opponent,firstSlot,firstTarget,out _) &&
            sequenceMatch.Player.Monster(0) == null &&
            sequenceMatch.Player.Life == 8000,
            "first planned strike removes defense without LP damage");
        Check(sequenceBrain.TryChooseAttack(sequenceMatch, out var secondSlot,
            out var secondTarget) && secondSlot == 1 && secondTarget == -1,
            "AI replans direct attack after defender was actually destroyed");
        Check(sequenceMatch.TryAttack(DuelSide.Opponent,secondSlot,secondTarget,out _) &&
            sequenceMatch.Player.Life == 4800,
            "replanned direct attack inflicts the expected 3200 LP");
        Check(!sequenceBrain.TryChooseAttack(sequenceMatch,out _,out _) &&
            sequenceBrain.LastAttackPlan == null,
            "used attackers cannot be planned a second time this turn");
        Console.WriteLine("PASS: " + passed + " deterministic smoke assertions");
    }
}
