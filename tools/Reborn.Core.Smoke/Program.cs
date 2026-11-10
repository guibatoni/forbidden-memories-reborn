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
        // Tests that jump to a later turn still have to supply the required
        // placement. Put a harmless card in a free rear slot for fixture setup.
        if (!m.PlayedThisTurn)
        {
            bool placed = false;
            var hand = m.Side(side).Hand;
            for (int h = 0; h < hand.Count && !placed; h++)
            {
                var card = hand[h];
                var lane = card.Kind == CardKind.Monster ?
                    FieldLane.Monster : FieldLane.Support;
                for (int slot = 4; slot >= 0; slot--)
                {
                    if (!m.CanPlay(side,new[]{h},lane,slot,card.StarA,out _)) continue;
                    if (!m.TryPlay(side,new[]{h},lane,slot,
                        BattlePosition.Defense,card.StarA,out var placementReason))
                        throw new Exception(placementReason);
                    placed = true;
                    break;
                }
            }
            if (!placed) throw new Exception("Smoke fixture has no valid mandatory play");
        }
        if (!m.TryBeginBattle(side,out var reason)) throw new Exception(reason);
    }
    private static void End(DuelMatch m, DuelSide side)
    {
        if (!m.TryEndTurn(side,out var reason)) throw new Exception(reason);
    }
    private static void Main()
    {
        var m = Match(M("mine",2000,1500),M("theirs",1200,800));
        Check(!m.TryEndTurn(DuelSide.Player,out _),
            "START cannot pass before one card has been played");
        Check(!m.TryBeginBattle(DuelSide.Player,out _),
            "battle cannot begin before mandatory placement");
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
        var aiMatch = Match(S("opening"),M("opponent",1800,1000));
        Check(aiMatch.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,true,out _),
            "player may spend the mandatory play on support");
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

        // Any supported card kind counts as the one compulsory placement:
        // the opponent must set a support when the hand has no monsters.
        var supportOnlyAi = new DuelMatch(
            Deck(S("myOpening")),
            Deck(S("spellA"),S("trapB"),S("equipC"),S("ritualD"),S("spellE")));
        Check(supportOnlyAi.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,true,out _),
            "opening support counts as one player turn action");
        End(supportOnlyAi,DuelSide.Player);
        Check(supportOnlyAi.TryChooseSupportSet(DuelSide.Opponent,
            out int fallbackHand, out int fallbackSlot) &&
            fallbackHand >= 0 && fallbackSlot >= 0,
            "opponent can find a legal support-only mandatory play");
        SimpleOpponentAi.TakeTurn(supportOnlyAi);
        Check(supportOnlyAi.CurrentSide == DuelSide.Player &&
            supportOnlyAi.Opponent.Support(fallbackSlot) != null &&
            supportOnlyAi.Opponent.SupportIsFaceDown(fallbackSlot),
            "support-only opponent sets a card instead of skipping turn");

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
        Play(adaptiveMatch, DuelSide.Player);
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

        var targetSafety = Match(S("setup"),M("o",1400,900));
        Check(targetSafety.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,true,out _),
            "set support counts as mandatory play without blocking direct attacks");
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
            Deck(M("wall", 200, 1800), S("passSetup")),
            Deck(M("small", 2100, 900), M("strong", 3200, 1500)));
        Check(sequenceMatch.TryPlay(DuelSide.Player, new[] {0},
            FieldLane.Monster, 0, BattlePosition.Defense,
            GuardianStar.None, out _), "sequence wall is summoned in defense");
        End(sequenceMatch, DuelSide.Player);
        Check(sequenceMatch.TryPlay(DuelSide.Opponent, new[] {0},
            FieldLane.Monster, 0, BattlePosition.Attack,
            GuardianStar.None, out _), "first AI attacker is summoned");
        End(sequenceMatch, DuelSide.Opponent);
        Check(sequenceMatch.TryPlay(DuelSide.Player, new[]{0},
            FieldLane.Support, 0, BattlePosition.Attack, GuardianStar.None,
            true, out _), "player sets support instead of skipping turn");
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

        var positional = new DuelMatch(
            Deck(M("player-threat", 2100, 1100)),
            Deck(M("winning-attacker", 2200, 600), M("passive-wall", 1900, 2900)));
        Play(positional, DuelSide.Player);
        End(positional, DuelSide.Player);
        var freshLearning = new AiLearningState("learning2:smoke");
        freshLearning.tacticValues = new float[] { 2, -2, -2, -2, -2 };
        freshLearning.tacticGames = null;
        freshLearning.tacticWins = null;
        var improvedBrain = new AdaptiveOpponentBrain(new Random(17),
            AiBrainVersion.Learning2);
        improvedBrain.StartDuel(freshLearning);
        int beforePositionalEvents = positional.Events.Count;
        StrategicPlacement winningPlacement;
        bool plannedWinning = improvedBrain.TryChoosePlacement(positional, out winningPlacement);
        Check(improvedBrain.Tactic == AiTactic.Balanced &&
            plannedWinning && winningPlacement != null &&
            winningPlacement.Result.Id == "winning-attacker" &&
            winningPlacement.Position == BattlePosition.Attack &&
            positional.CanPlay(DuelSide.Opponent, winningPlacement.HandIndices,
                FieldLane.Monster, winningPlacement.Slot, winningPlacement.Star, out _),
            "Learning 2.0 chooses winning attacker instead of a passive wall");
        Check(positional.Events.Count == beforePositionalEvents &&
            !positional.PlayedThisTurn &&
            positional.Opponent.Monster(winningPlacement.Slot) == null,
            "Learning 2.0 placement evaluation never mutates the match");
        Check(freshLearning.tacticGames.Length == 5 &&
            freshLearning.tacticWins.Length == 5,
            "Learning 2.0 safely migrates old saved profiles without tactic counters");
        var legacyBrain = new AdaptiveOpponentBrain(new Random(17),
            AiBrainVersion.Legacy);
        legacyBrain.StartDuel(new AiLearningState("learning1:smoke"));
        Check(legacyBrain.Version == AiBrainVersion.Legacy &&
            improvedBrain.Version == AiBrainVersion.Learning2 &&
            legacyBrain.TryChoosePlacement(positional, out var oldPlacement) &&
            positional.CanPlay(DuelSide.Opponent, oldPlacement.HandIndices,
                FieldLane.Monster, oldPlacement.Slot, oldPlacement.Star, out _),
            "legacy and Learning 2.0 policies both remain available for A/B evaluation");

        var learningDuel = Match(M("huge", 9500, 1000), M("weak", 800, 700));
        var counting = new AiLearningState("learning2:counters");
        var updatedBrain = new AdaptiveOpponentBrain(new Random(24),
            AiBrainVersion.Learning2);
        updatedBrain.StartDuel(counting);
        Play(learningDuel, DuelSide.Player);
        Battle(learningDuel, DuelSide.Player);
        Check(learningDuel.TryAttack(DuelSide.Player, 0, -1, out _) &&
            learningDuel.Phase == MatchPhase.Finished &&
            updatedBrain.RecordCompletedDuel(learningDuel),
            "Learning 2.0 accepts only the completed duel outcome");
        int chosenTacticIndex = (int)updatedBrain.Tactic;
        Check(counting.duels == 1 && counting.tacticGames[chosenTacticIndex] == 1 &&
            counting.tacticWins[chosenTacticIndex] == 0 &&
            !updatedBrain.RecordCompletedDuel(learningDuel) &&
            counting.tacticGames.Sum() == 1,
            "Learning 2.0 records one loss, updates only the active tactic and never trains twice");

        // New PS1-inspired duel controls: orientation, one hand play,
        // toggling an old monster, and preserving attack commitment.
        var fluid = new DuelMatch(
            Deck(M("setMonster", 2200, 1800), S("hiddenSpell")),
            Deck(M("enemy", 1600, 1400)));
        Check(fluid.TryPlay(DuelSide.Player, new[] { 0 }, FieldLane.Monster,
            0, BattlePosition.Defense, GuardianStar.None, true, out _) &&
            fluid.Player.Monster(0).FaceDown,
            "one monster can be set face-down in defense");
        Check(fluid.TryChangePosition(DuelSide.Player, 0,
            BattlePosition.Attack, out _) &&
            fluid.TryChangePosition(DuelSide.Player, 0,
            BattlePosition.Defense, out _),
            "new monster can toggle ATK/DEF repeatedly before attacking");
        Check(!fluid.CanPlay(DuelSide.Player, new[] { 0 },
            FieldLane.Support, 0, GuardianStar.None, out _),
            "one hand placement per turn remains enforced");
        Battle(fluid, DuelSide.Player);
        End(fluid, DuelSide.Player);
        Play(fluid, DuelSide.Opponent);
        End(fluid, DuelSide.Opponent);
        Check(fluid.CanChangePosition(DuelSide.Player, 0,
            BattlePosition.Attack, out _) &&
            fluid.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Attack, out _) &&
            fluid.Player.Monster(0).Position == BattlePosition.Attack &&
            !fluid.Player.Monster(0).FaceDown,
            "set monster can be flipped to attack only on its owner's later turn");
        Check(fluid.TryChangePosition(DuelSide.Player, 0,
            BattlePosition.Defense, out _) &&
            fluid.TryChangePosition(DuelSide.Player, 0,
            BattlePosition.Attack, out _),
            "Q/E can alternate positions repeatedly");
        Check(fluid.TryPlay(DuelSide.Player, new[]{0},
            FieldLane.Support, 0, BattlePosition.Attack, GuardianStar.None,
            true, out _), "a second-turn support card satisfies required placement");
        Battle(fluid, DuelSide.Player);
        Check(fluid.TryAttack(DuelSide.Player, 0, 0, out _) &&
            fluid.Player.Monster(0).AttackedThisTurn,
            "flipped monster can attack after legal reposition");
        Check(!fluid.TryChangePosition(DuelSide.Player, 0,
            BattlePosition.Defense, out _),
            "attacker cannot switch back to defense after attacking");

        var supportSet = new DuelMatch(Deck(S("setEquip"), M("backup", 1500, 1500)),
            Deck(M("other", 1000, 1000)));
        Check(supportSet.TryPlay(DuelSide.Player, new[] { 0 }, FieldLane.Support,
            1, BattlePosition.Attack, GuardianStar.None, true, out _) &&
            supportSet.Player.SupportIsFaceDown(1),
            "spell/equip support can be set facedown independently of monsters");
        Battle(supportSet, DuelSide.Player);
        int eventsBeforeUnsupported = supportSet.Events.Count;
        Check(!supportSet.CanActivateSupport(DuelSide.Player, 1, out _) &&
            supportSet.Events.Count == eventsBeforeUnsupported &&
            supportSet.Player.SupportIsFaceDown(1),
            "unverified support effects remain disabled without consuming the set card");

        var hiddenCombat = new DuelMatch(Deck(M("weakAttacker", 1300, 1000)),
            Deck(M("hiddenDefender", 500, 1800)));
        Play(hiddenCombat, DuelSide.Player);
        End(hiddenCombat, DuelSide.Player);
        Check(hiddenCombat.TryPlay(DuelSide.Opponent, new[] { 0 },
            FieldLane.Monster, 0, BattlePosition.Defense, GuardianStar.None,
            true, out _) && hiddenCombat.Opponent.Monster(0).FaceDown,
            "opponent defense can be set face down");
        End(hiddenCombat, DuelSide.Opponent);
        Battle(hiddenCombat, DuelSide.Player);
        int hiddenPreviewEvents = hiddenCombat.Events.Count;
        Check(hiddenCombat.TryPreviewAttack(DuelSide.Player, 0, 0,
            out var hiddenForecast, out _) &&
            hiddenForecast.DamageToAttacker == 500 &&
            hiddenCombat.Opponent.Monster(0).FaceDown &&
            hiddenCombat.Events.Count == hiddenPreviewEvents,
            "forecast is read-only and does not flip a face-down defender");
        Check(hiddenCombat.TryAttack(DuelSide.Player, 0, 0, out _) &&
            !hiddenCombat.Opponent.Monster(0).FaceDown &&
            hiddenCombat.Player.Life == 7500,
            "face-down defender is revealed on actual combat, without false destruction");

        var concealedAttacker = new DuelMatch(
            Deck(M("hiddenAttack", 2100, 1200)),
            Deck(M("opponentCard", 300, 800)));
        Check(concealedAttacker.TryPlay(DuelSide.Player, new[] { 0 },
            FieldLane.Monster, 0, BattlePosition.Attack, GuardianStar.None,
            true, out _) && concealedAttacker.Player.Monster(0).FaceDown,
            "attacker may be set face down in attack position");
        Battle(concealedAttacker, DuelSide.Player);
        Check(concealedAttacker.TryAttack(DuelSide.Player, 0, -1, out _) &&
            !concealedAttacker.Player.Monster(0).FaceDown,
            "face-down attacker is revealed on successful attack");
        Check(supportSet.Player.SupportSetTurn(1) == 1,
            "support card records the original turn it was set");

        // Existing enemy defense may switch into attack on a later turn.
        var opponentFlip = new DuelMatch(
            Deck(M("playerWall", 400, 1400), S("turn2Support")),
            Deck(M("aiSet", 2500, 1700)));
        Check(opponentFlip.TryPlay(DuelSide.Player, new[] {0},FieldLane.Monster,
            0,BattlePosition.Defense,GuardianStar.None,true,out _),
            "player hides a defensive monster");
        End(opponentFlip,DuelSide.Player);
        Check(opponentFlip.TryPlay(DuelSide.Opponent,new[] {0},FieldLane.Monster,
            0,BattlePosition.Defense,GuardianStar.None,true,out _),
            "AI can set a monster in face-down defense");
        Check(opponentFlip.CanChangePosition(DuelSide.Opponent,0,
            BattlePosition.Attack,out _),
            "the same unrestricted pre-attack toggle applies to a freshly set monster");
        End(opponentFlip,DuelSide.Opponent);
        Check(opponentFlip.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,true,out _),
            "player sets support before ending the following turn");
        End(opponentFlip,DuelSide.Player);
        var tacticalFlip = new AdaptiveOpponentBrain(new Random(203));
        tacticalFlip.StartDuel(new AiLearningState("tactical:flip"));
        Check(tacticalFlip.TryChoosePositionChange(opponentFlip,
            out int switchSlot, out var nextMode) &&
            switchSlot == 0 && nextMode == BattlePosition.Attack &&
            opponentFlip.TryChangePosition(DuelSide.Opponent,switchSlot,nextMode,out _) &&
            !opponentFlip.Opponent.Monster(0).FaceDown,
            "AI switches previously set defense into attack without reading a concealed target");
        Check(opponentFlip.TryChangePosition(DuelSide.Opponent,0,
            BattlePosition.Defense,out _),
            "AI can return to defense before attacking");

        // Prototype 0.14.1 inverted and responsive 3D hand fan geometry.
        // These checks are pure C# and can run in CI without a Unity Editor.
        var cLeft = HandFanGeometry.At(0, 5, false);
        var cMiddle = HandFanGeometry.At(2, 5, false);
        var cRight = HandFanGeometry.At(4, 5, false);
        var eLeft = HandFanGeometry.At(0, 5, true);
        var eMiddle = HandFanGeometry.At(2, 5, true);
        var eRight = HandFanGeometry.At(4, 5, true);
        Check(cLeft.X < cMiddle.X && cMiddle.X < cRight.X &&
            eLeft.X < eMiddle.X && eMiddle.X < eRight.X,
            "responsive fan preserves left-to-right original card order");
        Check(cLeft.Yaw < 0f && cRight.Yaw > 0f &&
            eLeft.Yaw < 0f && eRight.Yaw > 0f &&
            cMiddle.Yaw == 0f && eMiddle.Yaw == 0f,
            "fan tilts reversed compared with former negative yaw");
        Check(cLeft.Z < cMiddle.Z && cRight.Z < cMiddle.Z &&
            eLeft.Z < eMiddle.Z && eRight.Z < eMiddle.Z,
            "outer hand cards curve toward player rather than the battlefield");
        Check(Math.Abs(HandFanGeometry.ExpandedSpacing - 1.05f - 0.90f) < 0.0001f &&
            Math.Abs(eRight.X - eMiddle.X -
                2f * HandFanGeometry.ExpandedSpacing) < 0.0001f,
            "expanded fan adds exactly 0.90 table units to old 1.05 spacing");
        Check(Math.Abs(cRight.X - cMiddle.X -
                2f * HandFanGeometry.CompactSpacing) < 0.0001f &&
            eRight.X - eLeft.X > cRight.X - cLeft.X,
            "compact fan gathers cards, expanded fan spreads them wider");
        Check(eMiddle.Y > cMiddle.Y && eMiddle.Z > cMiddle.Z,
            "arena-focus hand sits lower and further toward player");
        Check(Math.Abs(eMiddle.X) < 0.0001f &&
            Math.Abs(cMiddle.X) < 0.0001f,
            "five-card fan remains centered without camera movement");
        bool invalidHandRejected = false;
        try { HandFanGeometry.At(5, 5, true); }
        catch (ArgumentOutOfRangeException) { invalidHandRejected = true; }
        Check(invalidHandRejected,
            "fan refuses nonexistent hand card indices");

        // 0.14.2 - make the hovered hand card readable without changing
        // its index, blocking a neighbor or corrupting the fusion order.
        for (int handCount = 1; handCount <= 5; handCount++)
        {
            for (int focus = 0; focus < handCount; focus++)
            {
                var focused = HandFanGeometry.WithHoveredCard(
                    focus, handCount, true, focus);
                var baseline = HandFanGeometry.At(focus, handCount, true);
                Check(Math.Abs(focused.X - baseline.X) < 0.0001f &&
                    Math.Abs(focused.Y - baseline.Y) < 0.0001f &&
                    Math.Abs(focused.Z - baseline.Z) < 0.0001f,
                    "hovered hand card stays at the same target location");
                for (int neighbor = 0; neighbor < handCount; neighbor++)
                {
                    var adjusted = HandFanGeometry.WithHoveredCard(
                        neighbor, handCount, true, focus);
                    var original = HandFanGeometry.At(neighbor, handCount, true);
                    if (neighbor < focus)
                        Check(adjusted.X < original.X,
                            "left hand neighbor shifts away from hovered card");
                    else if (neighbor > focus)
                        Check(adjusted.X > original.X,
                            "right hand neighbor shifts away from hovered card");
                    else
                        Check(Math.Abs(adjusted.X - original.X) < 0.0001f,
                            "focused card does not jump sideways on hover");
                    Check(adjusted.Yaw == original.Yaw &&
                        adjusted.Y == original.Y && adjusted.Z == original.Z,
                        "hover gaps never rotate or reorder cards");
                    var compact = HandFanGeometry.WithHoveredCard(
                        neighbor, handCount, false, focus);
                    var compactOriginal = HandFanGeometry.At(
                        neighbor, handCount, false);
                    Check(Math.Abs(compact.X - compactOriginal.X) < 0.0001f,
                        "arena focus never opens neighbor spacing");
                }
            }
        }
        bool invalidFocusedCard = false;
        try { HandFanGeometry.WithHoveredCard(0, 5, true, 5); }
        catch (ArgumentOutOfRangeException) { invalidFocusedCard = true; }
        Check(invalidFocusedCard, "invalid focused card index cannot corrupt hand layout");
        Check(HandFanGeometry.WithHoveredCard(1, 5, true, -1).X ==
            HandFanGeometry.At(1, 5, true).X,
            "no hover selection restores original fanned layout");
        Console.WriteLine("PASS: " + passed + " deterministic smoke assertions");
    }
}
