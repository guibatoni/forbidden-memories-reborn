using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reborn.Core;

namespace Reborn.Tests
{
    public sealed class DuelMatchTests
    {
        private static CardDefinition Monster(string id, int atk, int def,
            GuardianStar first = GuardianStar.None, GuardianStar second = GuardianStar.None)
        {
            return new CardDefinition(id, "Test " + id, CardKind.Monster, atk, def,
                "Prototype test card", first, second);
        }

        private static CardDefinition Support(string id, CardKind kind = CardKind.Spell)
        {
            return new CardDefinition(id, "Test " + id, kind, 0, 0, "Placeholder");
        }

        private static IReadOnlyList<CardDefinition> Deck(params CardDefinition[] first)
        {
            var cards = new List<CardDefinition>(first);
            while (cards.Count < DuelMatch.DeckSize)
                cards.Add(Monster("extra-" + cards.Count, 1000, 1000));
            return cards;
        }

        private static DuelMatch Match(CardDefinition my, CardDefinition enemy)
        {
            return new DuelMatch(Deck(my), Deck(enemy));
        }

        private static void Play(DuelMatch match, DuelSide side, int slot = 0,
            BattlePosition position = BattlePosition.Attack, GuardianStar star = GuardianStar.None)
        {
            Assert.IsTrue(match.TryPlay(side, new [] { 0 }, FieldLane.Monster,
                slot, position, star, out var reason), reason);
        }

        private static void Begin(DuelMatch match, DuelSide side)
        {
            // Scenario setup may advance to later turns; satisfy the new
            // mandatory placement with a harmless free board slot first.
            if (!match.PlayedThisTurn)
            {
                var owner = match.Side(side);
                bool placed = false;
                for (int i = 0; i < owner.Hand.Count && !placed; i++)
                {
                    var card = owner.Hand[i];
                    var lane = card.Kind == CardKind.Monster ?
                        FieldLane.Monster : FieldLane.Support;
                    for (int slot = 4; slot >= 0; slot--)
                    {
                        if (!match.CanPlay(side, new[] {i}, lane, slot,
                            card.StarA, out _)) continue;
                        Assert.IsTrue(match.TryPlay(side, new[] {i}, lane, slot,
                            BattlePosition.Defense, card.StarA, out _));
                        placed = true;
                        break;
                    }
                }
                Assert.IsTrue(placed, "Test setup needs at least one legal hand play");
            }
            Assert.IsTrue(match.TryBeginBattle(side, out var error), error);
        }

        private static void End(DuelMatch match, DuelSide side)
        {
            Assert.IsTrue(match.TryEndTurn(side, out var error), error);
        }

        private static DuelMatch WithOpponentOnField(
            CardDefinition my, CardDefinition enemy,
            BattlePosition enemyPosition = BattlePosition.Attack,
            GuardianStar myStar = GuardianStar.None,
            GuardianStar opponentStar = GuardianStar.None)
        {
            var match = Match(my, enemy);
            Play(match, DuelSide.Player, 0, BattlePosition.Attack, myStar);
            End(match, DuelSide.Player);
            Play(match, DuelSide.Opponent, 0, enemyPosition, opponentStar);
            End(match, DuelSide.Opponent);
            return match;
        }


        private static CardDefinition TypedMonster(
            string id, int attack, int defense, string type)
        {
            return new CardDefinition(id, "Test " + id,
                CardKind.Monster, attack, defense, "",
                GuardianStar.None, GuardianStar.None, "", "", "", type);
        }

        [Test]
        public void AllSixFieldTypesIncludeCorrectBoostsAndPenalties()
        {
            Assert.AreEqual(500,TerrainRules.Bonus("330",
                TypedMonster("beast",900,900,"Beast-Warrior")));
            Assert.AreEqual(500,TerrainRules.Bonus("331",
                TypedMonster("dinosaur",900,900,"Dinosaur")));
            Assert.AreEqual(500,TerrainRules.Bonus("332",
                TypedMonster("dragon",900,900,"Dragon")));
            Assert.AreEqual(500,TerrainRules.Bonus("333",
                TypedMonster("warrior",900,900,"Warrior")));
            Assert.AreEqual(500,TerrainRules.Bonus("334",
                TypedMonster("sea",900,900,"Sea Serpent")));
            Assert.AreEqual(-500,TerrainRules.Bonus("334",
                TypedMonster("machine",900,900,"Machine")));
            Assert.AreEqual(-500,TerrainRules.Bonus("335",
                TypedMonster("fairy",900,900,"Fairy")));
            Assert.AreEqual(0,TerrainRules.Bonus("335",
                TypedMonster("unknown",900,900,"")));
        }

        [Test]
        public void TerrainSpellsReplaceGlobalFieldAndChangeBothCombatSides()
        {
            var game = new DuelMatch(
                Deck(TypedMonster("dragon",1600,1000,"Dragon"),Support("332")),
                Deck(TypedMonster("wizard",1300,900,"Spellcaster"),Support("335")),
                null,null,VerifiedSpellCatalog.OriginalPs1Core());
            Play(game,DuelSide.Player);
            End(game,DuelSide.Player);
            Play(game,DuelSide.Opponent);
            End(game,DuelSide.Opponent);
            Assert.IsTrue(game.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Support,0,BattlePosition.Attack,GuardianStar.None,
                false,out _));
            Assert.AreEqual("Mountain",game.CurrentTerrainName);
            Begin(game,DuelSide.Player);
            Assert.IsTrue(game.TryPreviewAttack(DuelSide.Player,0,0,
                out var forecast,out _));
            Assert.AreEqual(2100,forecast.EffectiveAttack);
            End(game,DuelSide.Player);
            Assert.IsTrue(game.TryPlay(DuelSide.Opponent,new[]{0},
                FieldLane.Support,0,BattlePosition.Attack,GuardianStar.None,
                false,out _));
            Assert.AreEqual("Yami",game.CurrentTerrainName);
            Begin(game,DuelSide.Opponent);
            Assert.IsTrue(game.TryPreviewAttack(DuelSide.Opponent,0,0,
                out var newForecast,out _));
            Assert.AreEqual(500,newForecast.AttackerTerrainBonus);
            Assert.AreEqual(0,newForecast.DefenderTerrainBonus);
        }

        [Test]
        public void HiddenTrapsUseLowestEligibleThresholdWithoutRevealingOthers()
        {
            var game = new DuelMatch(
                Deck(Monster("attacker",1400,900)),
                Deck(Support("686",CardKind.Trap),Support("683",CardKind.Trap)),
                null,null,null,null,AttackTrapCatalog.OriginalPs1());
            Play(game,DuelSide.Player); End(game,DuelSide.Player);
            Assert.IsTrue(game.TryPlay(DuelSide.Opponent,new[]{0},
                FieldLane.Support,0,BattlePosition.Defense,GuardianStar.None,
                true,out _));
            End(game,DuelSide.Opponent);
            Begin(game,DuelSide.Player); End(game,DuelSide.Player);
            Assert.IsTrue(game.TryPlay(DuelSide.Opponent,new[]{0},
                FieldLane.Support,1,BattlePosition.Defense,GuardianStar.None,
                true,out _));
            End(game,DuelSide.Opponent);
            Begin(game,DuelSide.Player);
            var count = game.Events.Count;
            Assert.IsTrue(game.TryPreviewAttack(DuelSide.Player,0,-1,
                out var publicForecast,out _));
            Assert.AreEqual(1400,publicForecast.DamageToDefender);
            Assert.AreEqual(count,game.Events.Count);
            Assert.IsTrue(game.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.IsNull(game.Player.Monster(0));
            Assert.AreEqual(8000,game.Opponent.Life);
            Assert.IsNotNull(game.Opponent.Support(0));
            Assert.IsNull(game.Opponent.Support(1));
            Assert.IsTrue(game.Events.Any(e=>e.Kind==MatchEventKind.TrapTriggered &&
                e.CardId=="683"));
        }

        [Test]
        public void VerifiedOriginalMagicHealsAndAutomaticallyLeavesSupportZone()
        {
            var duel = new DuelMatch(
                Deck(Support("339")), Deck(Monster("enemy", 1000, 1000)),
                null, null, VerifiedSpellCatalog.OriginalPs1Core());
            Assert.IsTrue(duel.TryPlay(DuelSide.Player, new[]{0},
                FieldLane.Support, 1, BattlePosition.Attack,
                GuardianStar.None, false, out var reason), reason);
            Assert.AreEqual(8500, duel.Player.Life);
            Assert.IsNull(duel.Player.Support(1));
            Assert.AreEqual("339", duel.Player.Graveyard.Last().Id);
            Assert.IsTrue(duel.Events.Any(e => e.Kind ==
                MatchEventKind.LifeChanged && e.Value == 500));
            Assert.IsTrue(duel.PlayedThisTurn);
        }

        [Test]
        public void UnverifiedEffectTextCannotActivateOrConsumeCards()
        {
            var raw = new CardDefinition("700","Untrusted",CardKind.Spell,
                0,0,"",GuardianStar.None,GuardianStar.None,
                "Wipe every card", "external-note", "unverified");
            var duel = new DuelMatch(Deck(raw),Deck(Monster("enemy",1000,1000)),
                null,null,VerifiedSpellCatalog.OriginalPs1Core());
            int before = duel.Events.Count;
            Assert.IsFalse(duel.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Support,0,BattlePosition.Attack,
                GuardianStar.None,false,out _));
            Assert.AreEqual(before,duel.Events.Count);
            Assert.AreEqual(5,duel.Player.Hand.Count);
            Assert.IsFalse(duel.PlayedThisTurn);
            Assert.IsTrue(duel.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Support,0,BattlePosition.Attack,
                GuardianStar.None,true,out _));
            Assert.IsFalse(duel.CanActivateSupport(DuelSide.Player,0,out _));
        }

        [Test]
        public void PreviouslySetMagicActivatesOnlyAfterItsPlacementTurn()
        {
            var duel = new DuelMatch(
                Deck(Support("343")), Deck(Monster("enemy",1000,1000)),
                null,null,VerifiedSpellCatalog.OriginalPs1Core());
            Assert.IsTrue(duel.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Support,2,BattlePosition.Defense,
                GuardianStar.None,true,out _));
            Assert.IsFalse(duel.CanActivateSupport(DuelSide.Player,2,out _));
            End(duel,DuelSide.Player);
            Play(duel,DuelSide.Opponent);
            End(duel,DuelSide.Opponent);
            int events = duel.Events.Count;
            Assert.IsTrue(duel.CanActivateSupport(DuelSide.Player,2,out _));
            Assert.AreEqual(events,duel.Events.Count);
            Assert.IsTrue(duel.TryActivateSupport(DuelSide.Player,2,out _));
            Assert.AreEqual(7950,duel.Opponent.Life);
            Assert.IsNull(duel.Player.Support(2));
            Assert.IsFalse(duel.PlayedThisTurn);
            Assert.IsFalse(duel.TryActivateSupport(DuelSide.Player,2,out _));
        }

        [Test]
        public void RaigekiAndDarkHoleOnlyDestroyTheExpectedFields()
        {
            var catalogue = VerifiedSpellCatalog.OriginalPs1Core();
            var lightning = new DuelMatch(
                Deck(Monster("own",1800,1800),Support("337")),
                Deck(Monster("foe",1800,1600)),null,null,catalogue);
            Play(lightning,DuelSide.Player);
            End(lightning,DuelSide.Player);
            Play(lightning,DuelSide.Opponent);
            End(lightning,DuelSide.Opponent);
            Assert.IsTrue(lightning.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Support,0,BattlePosition.Attack,
                GuardianStar.None,false,out _));
            Assert.IsNotNull(lightning.Player.Monster(0));
            Assert.IsNull(lightning.Opponent.Monster(0));
            var dark = new DuelMatch(
                Deck(Monster("my",700,700),Support("336")),
                Deck(Monster("foe",800,800)),null,null,catalogue);
            Play(dark,DuelSide.Player);
            End(dark,DuelSide.Player);
            Play(dark,DuelSide.Opponent);
            End(dark,DuelSide.Opponent);
            Assert.IsTrue(dark.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Support,1,BattlePosition.Attack,
                GuardianStar.None,false,out _));
            Assert.AreEqual(0,dark.Player.MonsterCount);
            Assert.AreEqual(0,dark.Opponent.MonsterCount);
            Assert.IsTrue(dark.Player.Graveyard.Any(c=>c.Id=="my"));
            Assert.IsTrue(dark.Opponent.Graveyard.Any(c=>c.Id=="foe"));
        }

        [Test]
        public void VerifiedSpellCannotTurnInertTrapOrEquipFaceUp()
        {
            var catalogue = VerifiedSpellCatalog.OriginalPs1Core();
            foreach(var kind in new[]{CardKind.Trap,CardKind.Equip,
                CardKind.Ritual})
            {
                var duel = new DuelMatch(
                    Deck(Support("special",kind)),
                    Deck(Monster("enemy",1000,1000)),null,null,catalogue);
                Assert.IsFalse(duel.TryPlay(DuelSide.Player,new[]{0},
                    FieldLane.Support,0,BattlePosition.Attack,
                    GuardianStar.None,false,out _));
                Assert.IsFalse(duel.PlayedThisTurn);
                Assert.IsTrue(duel.TryPlay(DuelSide.Player,new[]{0},
                    FieldLane.Support,0,BattlePosition.Attack,
                    GuardianStar.None,true,out _));
            }
        }

        [Test]
        public void EquipApplicationStacksBonusesAndCombatUsesModifiedAttack()
        {
            var equip = new CardDefinition("657","Megamorph",CardKind.Equip,0,0,"");
            var second = new CardDefinition("668","Bright Castle",CardKind.Equip,0,0,"");
            var duel = new DuelMatch(
                Deck(Monster("own",1700,1200),equip,second),
                Deck(Monster("enemy",900,900)),
                null,null,null,VerifiedEquipCatalog.OriginalPs1Foundation());
            Play(duel,DuelSide.Player);
            End(duel,DuelSide.Player);
            Play(duel,DuelSide.Opponent);
            End(duel,DuelSide.Opponent);
            Assert.IsTrue(duel.TryEquipFromHand(DuelSide.Player,0,0,out var why),why);
            var monster=duel.Player.Monster(0);
            Assert.AreEqual(2700,monster.EffectiveAttack);
            Assert.AreEqual(2200,monster.EffectiveDefense);
            Assert.AreEqual(1,monster.EquipCount);
            Assert.IsTrue(duel.TryPreviewAttack(DuelSide.Player,0,0,
                out _, out _) == false, "Battle must be entered explicitly");
            Begin(duel,DuelSide.Player);
            Assert.IsTrue(duel.TryPreviewAttack(DuelSide.Player,0,0,
                out var forecast,out why),why);
            Assert.AreEqual(2700,forecast.EffectiveAttack);
            Assert.AreEqual(1000,forecast.AttackerEquipBonus);
            Assert.AreEqual(1800,forecast.DamageToDefender);
            End(duel,DuelSide.Player);
            Assert.IsTrue(duel.TryPlay(DuelSide.Opponent,new[]{0},
                FieldLane.Monster,1,BattlePosition.Attack,GuardianStar.None,out _));
            End(duel,DuelSide.Opponent);
            Assert.IsTrue(duel.TryEquipFromHand(DuelSide.Player,0,0,out why),why);
            Assert.AreEqual(3200,duel.Player.Monster(0).EffectiveAttack);
            Assert.AreEqual(2700,duel.Player.Monster(0).EffectiveDefense);
            Assert.AreEqual(2,duel.Player.Monster(0).EquipCount);
            Assert.IsNull(duel.Player.Support(0));
            Assert.AreEqual(2,duel.Player.Graveyard.Count);
        }

        [Test]
        public void IncompatibleAndHiddenEquipTargetsAreRejectedWithoutMutation()
        {
            var rules = new VerifiedEquipCatalog();
            rules.AddTargets("315",500,500,new[]{"001"});
            var equip = new CardDefinition("315","Dragon Treasure",CardKind.Equip,0,0,"");
            var duel = new DuelMatch(
                Deck(Monster("hidden",900,800),equip),
                Deck(Monster("enemy",900,800)),null,null,null,rules);
            Assert.IsTrue(duel.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Monster,0,BattlePosition.Defense,
                GuardianStar.None,true,out _));
            End(duel,DuelSide.Player);
            Play(duel,DuelSide.Opponent);
            End(duel,DuelSide.Opponent);
            var events=duel.Events.Count;
            Assert.IsFalse(duel.TryEquipFromHand(DuelSide.Player,0,0,out _));
            Assert.AreEqual(events,duel.Events.Count);
            Assert.IsTrue(duel.Player.Monster(0).FaceDown);
            Assert.AreEqual(0,duel.Player.Monster(0).EquipCount);
            Assert.IsFalse(duel.PlayedThisTurn);
        }

        [Test]
        public void PreparedEquipCanTargetFaceupMonsterOnLaterTurn()
        {
            var magic = new CardDefinition("657","Megamorph",CardKind.Equip,0,0,"");
            var duel = new DuelMatch(
                Deck(Monster("own",1600,1400),magic),
                Deck(Monster("enemy",1000,1000)),
                null,null,null,VerifiedEquipCatalog.OriginalPs1Foundation());
            Play(duel,DuelSide.Player);
            End(duel,DuelSide.Player);
            Play(duel,DuelSide.Opponent);
            End(duel,DuelSide.Opponent);
            Assert.IsTrue(duel.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Support,0,BattlePosition.Attack,
                GuardianStar.None,true,out _));
            Assert.IsFalse(duel.CanEquipFromSupport(DuelSide.Player,0,0,out _,out _));
            End(duel,DuelSide.Player);
            Assert.IsTrue(duel.TryPlay(DuelSide.Opponent,new[]{0},
                FieldLane.Monster,1,BattlePosition.Attack,GuardianStar.None,out _));
            End(duel,DuelSide.Opponent);
            Assert.IsTrue(duel.TryEquipFromSupport(DuelSide.Player,0,0,out _));
            Assert.AreEqual(2600,duel.Player.Monster(0).EffectiveAttack);
            Assert.IsNull(duel.Player.Support(0));
            Assert.IsFalse(duel.PlayedThisTurn);
            Assert.IsFalse(duel.TryEquipFromSupport(DuelSide.Player,0,0,out _));
        }

        [Test]
        public void FieldFusionStartsWithTableCardThenConsumesHandInSelectionOrder()
        {
            var recipes = new FusionCatalog();
            var first = Monster("firstResult", 1900, 1700);
            var final = Monster("finalResult", 3000, 2800, GuardianStar.Mercury);
            recipes.Add("base", "a", first);
            recipes.Add("firstResult", "b", final);
            var match = new DuelMatch(
                Deck(Monster("base", 800, 900), Monster("a", 500, 600),
                    Monster("b", 700, 800)),
                Deck(Monster("enemy", 1000, 1000)), recipes);
            Play(match, DuelSide.Player, 0);
            End(match, DuelSide.Player);
            Play(match, DuelSide.Opponent, 0);
            End(match, DuelSide.Opponent);
            var cards = new[] { 0, 1 };
            int count = match.Events.Count;
            Assert.AreEqual(final, match.PreviewFieldResult(
                DuelSide.Player, cards, 0));
            Assert.AreEqual(count, match.Events.Count);
            Assert.IsTrue(match.CanPlayOnField(DuelSide.Player, cards, 0,
                GuardianStar.Mercury, out var preview, out var reason), reason);
            Assert.AreEqual(final, preview);
            Assert.IsTrue(match.TryPlayOnField(DuelSide.Player, cards, 0,
                BattlePosition.Attack, GuardianStar.Mercury, true,
                out var result, out bool fused, out reason), reason);
            Assert.IsTrue(fused);
            Assert.AreEqual(final, result);
            Assert.AreEqual(final, match.Player.Monster(0).Card);
            Assert.IsFalse(match.Player.Monster(0).FaceDown);
            Assert.AreEqual(3, match.Player.Graveyard.Count);
            Assert.IsFalse(match.TryPlayOnField(DuelSide.Player, new[] { 0 }, 0,
                BattlePosition.Attack, GuardianStar.None, false,
                out _, out _, out _));
        }

        [Test]
        public void UnsupportedFieldEquipIsRejectedButMonsterReplacementIsLegal()
        {
            var match = new DuelMatch(
                Deck(Monster("old", 2600, 2100),
                    Support("equip", CardKind.Equip), Monster("replacement", 1200, 900)),
                Deck(Monster("enemy", 1000, 1000)));
            Play(match, DuelSide.Player);
            End(match, DuelSide.Player);
            Play(match, DuelSide.Opponent);
            End(match, DuelSide.Opponent);
            long events = match.Events.Count;
            Assert.IsFalse(match.CanPlayOnField(DuelSide.Player, new[] { 0 },
                0, GuardianStar.None, out _, out _));
            Assert.IsFalse(match.TryPlayOnField(DuelSide.Player, new[] { 0 },
                0, BattlePosition.Attack, GuardianStar.None, false,
                out _, out _, out _));
            Assert.AreEqual(events, match.Events.Count);
            Assert.AreEqual("old", match.Player.Monster(0).Card.Id);
            Assert.IsTrue(match.TryPlayOnField(DuelSide.Player, new[] { 1 },
                0, BattlePosition.Defense, GuardianStar.None, true,
                out var result, out bool fused, out _));
            Assert.IsFalse(fused);
            Assert.AreEqual("replacement", result.Id);
            Assert.IsTrue(match.Player.Monster(0).FaceDown);
            Assert.AreEqual("old", match.Player.Graveyard[0].Id);
        }

        [Test]
        public void FullMonsterFieldCanStillUseMandatoryHandPlayByReplacingOne()
        {
            var match = Match(Monster("player", 1500, 1000),
                Monster("opponent", 1200, 1300));
            for (int slot = 0; slot < DuelMatch.ZoneCount; slot++)
            {
                Play(match, DuelSide.Player, slot);
                End(match, DuelSide.Player);
                Play(match, DuelSide.Opponent, slot);
                End(match, DuelSide.Opponent);
            }
            Assert.AreEqual(5, match.Player.MonsterCount);
            Assert.IsFalse(match.CanPlay(DuelSide.Player, new[] { 0 },
                FieldLane.Monster, 0, GuardianStar.None, out _));
            Assert.IsFalse(match.TryEndTurn(DuelSide.Player, out _));
            Assert.IsTrue(match.TryChooseFieldCombination(DuelSide.Player, false,
                out int hand, out int slotChosen, out var result));
            Assert.IsTrue(match.CanPlayOnField(DuelSide.Player, new[] { hand },
                slotChosen, result.StarA, out _, out _));
            Assert.IsTrue(match.TryPlayOnField(DuelSide.Player, new[] { hand },
                slotChosen, BattlePosition.Attack, result.StarA, false,
                out _, out _, out _));
            Assert.AreEqual(5, match.Player.MonsterCount);
            End(match, DuelSide.Player);
        }

        [Test]
        public void MandatoryPlayAndRepeatablePositionChangesUntilAttack()
        {
            var match = Match(Monster("mine", 2300, 1500), Monster("enemy", 500, 900));
            Assert.IsFalse(match.TryEndTurn(DuelSide.Player, out var rejected));
            StringAssert.Contains("Baixe", rejected);
            Assert.IsFalse(match.TryBeginBattle(DuelSide.Player, out _));
            Play(match, DuelSide.Player);
            Assert.IsTrue(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Defense, out _));
            Assert.IsTrue(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Attack, out _));
            Assert.IsTrue(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Defense, out _));
            Assert.IsTrue(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Attack, out _));
            Begin(match, DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player, 0, -1, out _));
            Assert.IsFalse(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Defense, out _));
            End(match, DuelSide.Player);
            Assert.AreEqual(DuelSide.Opponent, match.CurrentSide);
        }

        [Test]
        public void SetMonsterRemainsHiddenOnPositionChangesUntilItAttacks()
        {
            var match = new DuelMatch(
                Deck(Monster("mine", 2100, 1400), Support("turn2Spell")),
                Deck(Monster("enemy", 1200, 900)));
            Assert.IsTrue(match.TryPlay(DuelSide.Player, new[] { 0 },
                FieldLane.Monster, 0, BattlePosition.Defense,
                GuardianStar.None, true, out _));
            Assert.IsTrue(match.Player.Monster(0).FaceDown);
            Assert.IsTrue(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Attack, out _));
            Assert.IsTrue(match.Player.Monster(0).FaceDown,
                "ATK stance must not expose a facedown monster");
            Assert.IsFalse(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Attack, out _),
                "Choosing an identical stance is not a reveal action");
            Assert.IsTrue(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Defense, out _));
            Assert.IsTrue(match.Player.Monster(0).FaceDown);
            End(match, DuelSide.Player);
            Play(match, DuelSide.Opponent);
            End(match, DuelSide.Opponent);
            Assert.IsTrue(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Attack, out _));
            Assert.IsTrue(match.Player.Monster(0).FaceDown,
                "FaceDown persists across turns and DEF -> ATK");
            Assert.IsTrue(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Defense, out _));
            Assert.IsTrue(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Attack, out _));
            Begin(match, DuelSide.Player);
            Assert.IsTrue(match.Player.Monster(0).FaceDown,
                "Beginning Battle is not a reveal event");
            Assert.IsTrue(match.TryAttack(DuelSide.Player, 0, 0, out _));
            Assert.IsFalse(match.Player.Monster(0).FaceDown,
                "A facedown attacker is revealed by a resolved attack");
            Assert.IsTrue(match.Player.Monster(0).AttackedThisTurn);
            Assert.IsFalse(match.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Defense, out _));
        }

        [Test]
        public void FaceDownDefenderStaysHiddenDuringPreviewAndRevealsOnAttack()
        {
            var match = Match(Monster("mine", 1300, 900), Monster("wall", 500, 1800));
            Play(match, DuelSide.Player);
            End(match, DuelSide.Player);
            Assert.IsTrue(match.TryPlay(DuelSide.Opponent, new[] { 0 },
                FieldLane.Monster, 0, BattlePosition.Defense,
                GuardianStar.None, true, out _));
            End(match, DuelSide.Opponent);
            Begin(match, DuelSide.Player);
            int before = match.Events.Count;
            Assert.IsTrue(match.TryPreviewAttack(DuelSide.Player, 0, 0, out _, out _));
            Assert.AreEqual(before, match.Events.Count);
            Assert.IsTrue(match.Opponent.Monster(0).FaceDown);
            Assert.IsTrue(match.TryAttack(DuelSide.Player, 0, 0, out _));
            Assert.IsFalse(match.Opponent.Monster(0).FaceDown);
            Assert.AreEqual(7500, match.Player.Life);
        }

        [Test]
        public void FacedownTargetCanSwitchToAttackWithoutRevealingUntilHit()
        {
            var match = Match(Monster("attacker", 1700, 800),
                Monster("concealed", 2300, 1700));
            Play(match, DuelSide.Player);
            End(match, DuelSide.Player);
            Assert.IsTrue(match.TryPlay(DuelSide.Opponent, new[] { 0 },
                FieldLane.Monster, 0, BattlePosition.Defense,
                GuardianStar.None, true, out _));
            Assert.IsTrue(match.TryChangePosition(DuelSide.Opponent, 0,
                BattlePosition.Attack, out _));
            Assert.IsTrue(match.Opponent.Monster(0).FaceDown);
            Assert.IsFalse(match.CanChangePosition(DuelSide.Opponent, 0,
                BattlePosition.Attack, out _),
                "Selecting an already active stance must be a no-op");
            End(match, DuelSide.Opponent);
            Begin(match, DuelSide.Player);
            int before = match.Events.Count;
            Assert.IsTrue(match.TryPreviewAttack(DuelSide.Player, 0, 0,
                out _, out _));
            Assert.AreEqual(before, match.Events.Count);
            Assert.IsTrue(match.Opponent.Monster(0).FaceDown,
                "Attack prediction must not reveal the target");
            Assert.IsTrue(match.TryAttack(DuelSide.Player, 0, 0, out _));
            Assert.IsTrue(match.Events.Count > before);
            Assert.IsNotNull(match.Opponent.Monster(0),
                "Strong target should survive to verify its revealed state");
            Assert.IsFalse(match.Opponent.Monster(0).FaceDown,
                "A facedown target is revealed when attacked");
        }

        [Test]
        public void SupportsRequirePriorFacedownSetAndVerifiedEffect()
        {
            var match = new DuelMatch(Deck(Support("spell")), Deck(Monster("enemy", 1000, 1000)));
            Assert.IsTrue(match.TryPlay(DuelSide.Player, new[] { 0 },
                FieldLane.Support, 2, BattlePosition.Attack,
                GuardianStar.None, true, out _));
            Assert.IsTrue(match.Player.SupportIsFaceDown(2));
            Assert.AreEqual(1, match.Player.SupportSetTurn(2));
            Assert.IsFalse(match.CanActivateSupport(DuelSide.Player, 2, out _));
            End(match, DuelSide.Player);
            Play(match, DuelSide.Opponent);
            End(match, DuelSide.Opponent);
            Assert.IsFalse(match.CanActivateSupport(DuelSide.Player, 2, out var reason));
            StringAssert.Contains("nao validado", reason);
            Assert.IsTrue(match.Player.SupportIsFaceDown(2));
        }

        [Test]
        public void CanAttackPreviewDoesNotChangeStateOrEmitEvents()
        {
            var match = Match(Monster("p",1700,1100),Monster("o",1500,1000));
            Play(match,DuelSide.Player);
            Begin(match,DuelSide.Player);
            long eventsBefore=match.Events.Count;
            Assert.IsTrue(match.CanAttack(DuelSide.Player,0,-1,out var reason),reason);
            Assert.AreEqual(8000,match.Opponent.Life);
            Assert.IsFalse(match.Player.Monster(0).AttackedThisTurn);
            Assert.AreEqual(eventsBefore,match.Events.Count);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,-1,out reason),reason);
            Assert.AreEqual(6300,match.Opponent.Life);
            Assert.IsFalse(match.CanAttack(DuelSide.Player,0,-1,out _));
        }

        [Test]
        public void PreviewRejectsBlockedDirectAttackAndWrongPhaseWithoutMutation()
        {
            var match = WithOpponentOnField(Monster("p",1600,1300),Monster("o",1000,700));
            int eventsBefore=match.Events.Count;
            Assert.IsFalse(match.CanAttack(DuelSide.Player,0,-1,out _));
            Assert.IsFalse(match.CanAttack(DuelSide.Player,0,0,out _));
            Assert.AreEqual(eventsBefore,match.Events.Count);
            Begin(match,DuelSide.Player);
            eventsBefore=match.Events.Count;
            Assert.IsFalse(match.CanAttack(DuelSide.Player,0,-1,out _));
            Assert.IsFalse(match.CanAttack(DuelSide.Player,-1,0,out _));
            Assert.IsTrue(match.CanAttack(DuelSide.Player,0,0,out _));
            Assert.AreEqual(eventsBefore,match.Events.Count);
        }

        [Test]
        public void AiPlanningDoesNotMutateStateAndRequiresOpponentTurn()
        {
            var match = new DuelMatch(Deck(Monster("mine",1200,900)),
                Deck(Monster("weak",1300,900),Monster("strong",2600,2000)));
            long count = match.Events.Count;
            Assert.IsFalse(SimpleOpponentAi.TryChoosePlay(match,out _,out _));
            Assert.AreEqual(count,match.Events.Count);
            Play(match,DuelSide.Player);
            End(match,DuelSide.Player);
            count = match.Events.Count;
            Assert.IsTrue(SimpleOpponentAi.TryChoosePlay(match,out int hand,out int slot));
            Assert.AreEqual("strong",match.Opponent.Hand[hand].Id);
            Assert.AreEqual(0,slot);
            Assert.AreEqual(count,match.Events.Count);
            Assert.IsNull(match.Opponent.Monster(0));
        }

        [Test]
        public void OpponentSupportFallbackSatisfiesMandatoryPlayWithoutEffects()
        {
            var match = new DuelMatch(Deck(Support("opening")),
                Deck(Support("spell"), Support("trap", CardKind.Trap),
                    Support("equip", CardKind.Equip), Support("ritual", CardKind.Ritual),
                    Support("spell2")));
            Assert.IsTrue(match.TryPlay(DuelSide.Player, new[] {0}, FieldLane.Support,
                0, BattlePosition.Attack, GuardianStar.None, true, out _));
            End(match, DuelSide.Player);
            Assert.IsTrue(match.TryChooseSupportSet(DuelSide.Opponent,
                out int hand, out int zone));
            Assert.IsFalse(match.TryEndTurn(DuelSide.Opponent, out _));
            Assert.IsTrue(match.TryPlay(DuelSide.Opponent, new[] {hand},
                FieldLane.Support, zone, BattlePosition.Defense,
                GuardianStar.None, true, out _));
            Assert.IsTrue(match.Opponent.SupportIsFaceDown(zone));
            Assert.IsFalse(match.CanActivateSupport(DuelSide.Opponent,zone,out _));
            End(match, DuelSide.Opponent);
        }

        [Test]
        public void AiHasNoSummonWhenOpponentHandHasNoMonster()
        {
            var match = new DuelMatch(Deck(Monster("p",1000,900)),
                Deck(Support("s1"),Support("s2"),Support("s3"),
                    Support("s4"),Support("s5")));
            Play(match,DuelSide.Player);
            End(match,DuelSide.Player);
            Assert.IsFalse(SimpleOpponentAi.TryChoosePlay(match,out _,out _));
            Assert.IsFalse(match.PlayedThisTurn);
        }

        [Test]
        public void AiChoosesAttackerAndWeakestValidTargetWithoutMutation()
        {
            var match = Match(Monster("defender",1100,1000), Monster("attacker",1800,1200));
            Play(match,DuelSide.Player,2);
            End(match,DuelSide.Player);
            Play(match,DuelSide.Opponent,4);
            Begin(match,DuelSide.Opponent);
            int oldLife = match.Player.Life;
            Assert.IsTrue(SimpleOpponentAi.TryChooseAttack(match,out int attacker,out int target));
            Assert.AreEqual(4,attacker);
            Assert.AreEqual(2,target);
            Assert.AreEqual(oldLife,match.Player.Life);
            Assert.IsFalse(match.Opponent.Monster(4).AttackedThisTurn);
            Assert.IsTrue(match.TryAttack(DuelSide.Opponent,attacker,target,out _));
            Assert.IsFalse(SimpleOpponentAi.TryChooseAttack(match,out _,out _));
        }

        [Test]
        public void AiSelectsDirectAttackOnlyWhenEnemyHasNoMonster()
        {
            var match = new DuelMatch(
                Deck(Support("opening")), Deck(Monster("o",1800,1000)));
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
                BattlePosition.Attack,GuardianStar.None,true,out _));
            End(match,DuelSide.Player);
            Play(match,DuelSide.Opponent,0);
            Begin(match,DuelSide.Opponent);
            Assert.IsTrue(SimpleOpponentAi.TryChooseAttack(match,out int from,out int to));
            Assert.AreEqual(0,from);
            Assert.AreEqual(-1,to);
        }

        [Test]
        public void StartsWithTwoDistinctHandsAndFixedField()
        {
            var match = Match(Monster("p",1500,1000), Monster("o",1300,900));
            Assert.AreEqual(DuelSide.Player, match.CurrentSide);
            Assert.AreEqual(5,match.Player.Hand.Count);
            Assert.AreEqual(5,match.Opponent.Hand.Count);
            Assert.AreEqual(35,match.Player.DeckCount);
            Assert.AreEqual(35,match.Opponent.DeckCount);
            Assert.AreEqual(8000,match.Player.Life);
            Assert.AreEqual(8000,match.Opponent.Life);
            Assert.IsNull(match.Player.Monster(4));
            Assert.IsNull(match.Opponent.Support(4));
        }

        [Test]
        public void RejectsDeckOtherThan40Cards()
        {
            Assert.Throws<ArgumentException>(() =>
                new DuelMatch(new [] { Monster("p",1000,1000) }, Deck(Monster("o",1000,1000))));
        }

        [Test]
        public void RejectsNullCardsInDeck()
        {
            var deck = Deck(Monster("p",1000,1000)).ToArray();
            deck[12] = null;
            Assert.Throws<ArgumentException>(() => new DuelMatch(deck,Deck(Monster("o",1000,1000))));
        }

        [Test]
        public void RejectsOpponentPlayingOnPlayersTurnWithoutMutation()
        {
            var match = Match(Monster("p",1000,1000),Monster("o",1000,1000));
            Assert.IsFalse(match.TryPlay(DuelSide.Opponent,new [] { 0 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(5,match.Opponent.Hand.Count);
            Assert.IsNull(match.Opponent.Monster(0));
        }

        [Test]
        public void OnePlayPerTurnIsEnforced()
        {
            var match = Match(Monster("p",1500,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player);
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Monster,1,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(4,match.Player.Hand.Count);
        }

        [Test]
        public void RejectsOutOfRangeSlotsWithoutConsumingHand()
        {
            var match = Match(Monster("p",1500,1000),Monster("o",1000,1000));
            foreach (var i in new [] { -1, 5, 99 })
                Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Monster,i,
                    BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(5,match.Player.Hand.Count);
        }

        [Test]
        public void RejectsCardInWrongLane()
        {
            var match = Match(Monster("p",1500,1000),Monster("o",1000,1000));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Support,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.IsNull(match.Player.Support(0));
        }

        [Test]
        public void SupportCardsUseSupportLaneWithoutFakeEffect()
        {
            var match = Match(Support("magic"),Monster("o",1000,1000));
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Support,2,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual("magic",match.Player.Support(2).Id);
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void InvalidIndicesAndDuplicatesAreAtomic()
        {
            var match = Match(Monster("p",1800,1200),Monster("o",1000,1000));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0,0 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0,5 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new int[0],FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(5,match.Player.Hand.Count);
            Assert.IsNull(match.Player.Monster(0));
        }

        [Test]
        public void GuardianStarHasTwoIndependentCycles()
        {
            var ring = new [] { GuardianStar.Mercury,GuardianStar.Sun,GuardianStar.Moon,
                GuardianStar.Venus };
            for (int i=0;i<ring.Length;i++)
                Assert.IsTrue(GuardianStarRules.HasAdvantage(ring[i],ring[(i+1)%ring.Length]));
            var other = new [] { GuardianStar.Mars,GuardianStar.Jupiter,GuardianStar.Saturn,
                GuardianStar.Uranus,GuardianStar.Pluto,GuardianStar.Neptune };
            for (int i=0;i<other.Length;i++)
                Assert.IsTrue(GuardianStarRules.HasAdvantage(other[i],other[(i+1)%other.Length]));
            Assert.IsFalse(GuardianStarRules.HasAdvantage(GuardianStar.Mars,GuardianStar.Sun));
            Assert.IsFalse(GuardianStarRules.HasAdvantage(GuardianStar.None,GuardianStar.Sun));
            Assert.AreEqual(500,GuardianStarRules.Bonus(GuardianStar.Sun,GuardianStar.Moon));
        }

        [Test]
        public void RefusesGuardianStarOutsideTheCardsTwoChoices()
        {
            var match = Match(Monster("p",1800,1300,GuardianStar.Mercury,GuardianStar.Mars),
                Monster("o",1000,1000));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.Moon,out _));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(5,match.Player.Hand.Count);
        }

        [Test]
        public void TacticalAiSkipsLosingGuardianDefenseAndChoosesWinningBattle()
        {
            var losing = Match(
                Monster("p",600,1800,GuardianStar.Mercury),
                Monster("o",2100,900,GuardianStar.Sun));
            Play(losing,DuelSide.Player,0,BattlePosition.Defense,GuardianStar.Mercury);
            End(losing,DuelSide.Player);
            Play(losing,DuelSide.Opponent,0,BattlePosition.Attack,GuardianStar.Sun);
            Begin(losing,DuelSide.Opponent);
            var brain = new AdaptiveOpponentBrain(new Random(8));
            brain.StartDuel(new AiLearningState("learn:merc-def"));
            var previousEvents = losing.Events.Count;
            Assert.IsFalse(brain.TryChooseAttack(losing,out _,out _));
            Assert.AreEqual(previousEvents,losing.Events.Count);
            Assert.IsFalse(losing.Opponent.Monster(0).AttackedThisTurn);

            var winning = Match(
                Monster("p",600,1800,GuardianStar.Moon),
                Monster("o",2100,900,GuardianStar.Sun));
            Play(winning,DuelSide.Player,0,BattlePosition.Defense,GuardianStar.Moon);
            End(winning,DuelSide.Player);
            Play(winning,DuelSide.Opponent,0,BattlePosition.Attack,GuardianStar.Sun);
            Begin(winning,DuelSide.Opponent);
            Assert.IsTrue(brain.TryChooseAttack(winning,out var source,out var target));
            Assert.AreEqual(0,source);
            Assert.AreEqual(0,target);
            Assert.IsTrue(winning.TryAttack(DuelSide.Opponent,source,target,out _));
            Assert.IsNull(winning.Player.Monster(0));
        }

        [Test]
        public void EmptyTargetCannotBypassExplicitDirectAttack()
        {
            var duel = Match(Monster("p",700,700),Monster("o",1900,900));
            Play(duel,DuelSide.Player);
            Begin(duel,DuelSide.Player);
            Assert.IsFalse(duel.CanAttack(DuelSide.Player,0,4,out _));
            Assert.IsFalse(duel.TryPreviewAttack(DuelSide.Player,0,4,out _,out _));
            Assert.IsTrue(duel.TryPreviewAttack(DuelSide.Player,0,-1,
                out var preview,out _));
            Assert.IsTrue(preview.IsDirect);
            Assert.AreEqual(700, preview.DamageToDefender);
        }

        [Test]
        public void UnknownGuardianStarsGiveNoUnverifiedBonus()
        {
            Assert.AreEqual(0,GuardianStarRules.Bonus(GuardianStar.None,GuardianStar.Moon));
            Assert.IsTrue(GuardianStarRules.IsAvailable(Monster("x",1000,1000),GuardianStar.None));
        }

        [Test]
        public void DirectAttackDeductsLifeWhenEnemyHasNoMonsters()
        {
            var match = Match(Monster("p",1700,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(6300,match.Opponent.Life);
            Assert.IsNotNull(match.Player.Monster(0));
        }

        [Test]
        public void DirectAttackIsBlockedByEnemyMonster()
        {
            var match = WithOpponentOnField(Monster("p",1800,1000),Monster("o",1100,1000));
            Begin(match,DuelSide.Player);
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,4,out _));
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void GuardianBonusDeterminesAttackVsAttackOutcome()
        {
            var match = WithOpponentOnField(
                Monster("p",2000,1000,GuardianStar.Mercury),
                Monster("o",2200,1000,GuardianStar.Sun),
                BattlePosition.Attack,GuardianStar.Mercury,GuardianStar.Sun);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.AreEqual(7700,match.Opponent.Life);
            Assert.IsNull(match.Opponent.Monster(0));
            Assert.AreEqual("o",match.Opponent.Graveyard.Last().Id);
            Assert.AreEqual(2000,match.Player.Monster(0).Card.Attack); // Non-permanent bonus
        }

        [Test]
        public void GuardianDisadvantageCanDestroyAttacker()
        {
            var match = WithOpponentOnField(
                Monster("p",2000,1000,GuardianStar.Sun),
                Monster("o",1800,1000,GuardianStar.Mercury),
                BattlePosition.Attack,GuardianStar.Sun,GuardianStar.Mercury);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNull(match.Player.Monster(0));
            Assert.AreEqual(7700,match.Player.Life);
        }

        [Test]
        public void AttackBeatsDefenseWithoutLifeLoss()
        {
            var match = WithOpponentOnField(Monster("p",2300,1000),
                Monster("o",1200,1700),BattlePosition.Defense);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNull(match.Opponent.Monster(0));
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void Attack2100DestroysDefense1800WithoutGuardianBonus()
        {
            var match = WithOpponentOnField(
                Monster("p",2100,1200), Monster("o",500,1800),
                BattlePosition.Defense);
            Begin(match,DuelSide.Player);
            var eventsBefore = match.Events.Count;
            Assert.IsTrue(match.TryPreviewAttack(DuelSide.Player,0,0,
                out var forecast,out var reason),reason);
            Assert.AreEqual(2100,forecast.EffectiveAttack);
            Assert.AreEqual(1800,forecast.EffectiveDefense);
            Assert.IsTrue(forecast.DefenderDestroyed);
            Assert.AreEqual(0,forecast.DamageToDefender);
            Assert.AreEqual(eventsBefore,match.Events.Count);
            Assert.IsNotNull(match.Opponent.Monster(0));
            Assert.IsFalse(match.Player.Monster(0).AttackedThisTurn);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out reason),reason);
            Assert.IsNull(match.Opponent.Monster(0));
            Assert.AreEqual(8000,match.Opponent.Life);
            Assert.AreEqual(1,match.Opponent.Graveyard.Count);
        }

        [Test]
        public void GuardianBonusCanMake1800DefenseResist2100Attack()
        {
            var match = WithOpponentOnField(
                Monster("p",2100,1200,GuardianStar.Sun),
                Monster("o",500,1800,GuardianStar.Mercury),
                BattlePosition.Defense, GuardianStar.Sun, GuardianStar.Mercury);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryPreviewAttack(DuelSide.Player,0,0,
                out var preview,out var reason),reason);
            Assert.AreEqual(2100,preview.AttackerBase);
            Assert.AreEqual(1800,preview.DefenderBase);
            Assert.AreEqual(0,preview.AttackerGuardianBonus);
            Assert.AreEqual(500,preview.DefenderGuardianBonus);
            Assert.AreEqual(2300,preview.EffectiveDefense);
            Assert.IsFalse(preview.DefenderDestroyed);
            Assert.AreEqual(200,preview.DamageToAttacker);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out reason),reason);
            Assert.IsNotNull(match.Opponent.Monster(0));
            Assert.IsNotNull(match.Player.Monster(0));
            Assert.AreEqual(7800,match.Player.Life);
        }

        [Test]
        public void PreviewRejectsIllegalAttacksWithoutMutatingState()
        {
            var match = Match(Monster("p",2100,1200),Monster("o",500,1800));
            Assert.IsFalse(match.TryPreviewAttack(DuelSide.Player,0,-1,
                out var preview,out _));
            Assert.IsNull(preview);
            Play(match,DuelSide.Player);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryPreviewAttack(DuelSide.Player,0,-1,
                out preview,out _));
            Assert.IsTrue(preview.IsDirect);
            Assert.AreEqual(2100,preview.DamageToDefender);
            Assert.AreEqual(8000,match.Opponent.Life);
            Assert.IsFalse(match.Player.Monster(0).AttackedThisTurn);
        }

        [Test]
        public void DefenseWithHigherPointsDamagesAttackerButDoesNotDestroyIt()
        {
            var match = WithOpponentOnField(Monster("p",1500,1000),
                Monster("o",500,2000),BattlePosition.Defense);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNotNull(match.Player.Monster(0));
            Assert.AreEqual(7500,match.Player.Life);
        }

        [Test]
        public void EqualDefenseDoesNotDestroyAnyMonster()
        {
            var match = WithOpponentOnField(Monster("p",2000,1000),
                Monster("o",1200,2000),BattlePosition.Defense);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNotNull(match.Player.Monster(0));
            Assert.IsNotNull(match.Opponent.Monster(0));
            Assert.AreEqual(8000,match.Player.Life);
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void EqualAttackDestroysBothMonsters()
        {
            var match = WithOpponentOnField(Monster("p",1800,1000),Monster("o",1800,1000));
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNull(match.Player.Monster(0));
            Assert.IsNull(match.Opponent.Monster(0));
            Assert.AreEqual(8000,match.Player.Life);
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void CannotAttackBeforeBattlePhase()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player);
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void DefensePositionCannotAttack()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player,0,BattlePosition.Defense);
            Begin(match,DuelSide.Player);
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void SameMonsterCannotAttackTwice()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player); Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(6200,match.Opponent.Life);
        }

        [Test]
        public void TurnChangesSideAndRefillsOnlyWhenPlayerStartsTheirTurn()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player);
            Assert.AreEqual(4,match.Player.Hand.Count);
            End(match,DuelSide.Player);
            Assert.AreEqual(DuelSide.Opponent,match.CurrentSide);
            Assert.AreEqual(4,match.Player.Hand.Count);
            Play(match,DuelSide.Opponent);
            End(match,DuelSide.Opponent);
            Assert.AreEqual(DuelSide.Player,match.CurrentSide);
            Assert.AreEqual(5,match.Player.Hand.Count);
            Assert.AreEqual(34,match.Player.DeckCount);
            Assert.AreEqual(3,match.Turn);
        }

        [Test]
        public void CannotPlayAfterBeginningBattle()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Assert.IsFalse(match.TryBeginBattle(DuelSide.Player,out _));
            Play(match,DuelSide.Player);
            Begin(match,DuelSide.Player);
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] {0},FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
        }

        [Test]
        public void OnlyImportedFusionRecipesAreExecuted()
        {
            var a=Monster("A",1000,1000);var b=Monster("B",1200,1200);
            var fusion=Monster("F",3000,2000,GuardianStar.Sun);
            var catalog=new FusionCatalog(); catalog.Add("A","B",fusion);
            var match=new DuelMatch(Deck(a,b),Deck(Monster("o",1000,1000)),catalog);
            Assert.AreEqual("F",match.PreviewResult(DuelSide.Player,new [] {0,1}).Id);
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new [] {0,1},FieldLane.Monster,1,
                BattlePosition.Attack,GuardianStar.Sun,out _));
            Assert.AreEqual("F",match.Player.Monster(1).Card.Id);
            Assert.AreEqual(3,match.Player.Hand.Count);
            Assert.AreEqual(2,match.Player.Graveyard.Count);
            Assert.AreEqual(1,match.Events.Count(e=>e.Kind==MatchEventKind.Fusion));
        }

        [Test]
        public void UnknownFusionRetainsLastCardAndBuriesPreviousPhysicalMaterial()
        {
            var a=Monster("A",1000,1000);var b=Monster("B",1200,1200);
            var match=new DuelMatch(Deck(a,b),Deck(Monster("o",1000,1000)));
            Assert.AreEqual("B",match.PreviewResult(DuelSide.Player,new [] {0,1}).Id);
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new [] {0,1},FieldLane.Monster,1,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual("B",match.Player.Monster(1).Card.Id);
            Assert.AreEqual(1,match.Player.Graveyard.Count);
            Assert.AreEqual("A",match.Player.Graveyard[0].Id);
            Assert.AreEqual(1,match.Events.Count(e=>e.Kind==MatchEventKind.FusionFailed));
        }

        [Test]
        public void ThreeCardChainDoesNotBuryVirtualFusionCard()
        {
            var a=Monster("A",1000,1000);var b=Monster("B",1100,1000);var c=Monster("C",500,1000);
            var f=Monster("F",2200,2000);
            var catalog=new FusionCatalog();catalog.Add("A","B",f);
            var match=new DuelMatch(Deck(a,b,c),Deck(Monster("o",1000,1000)),catalog);
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new [] {0,1,2},FieldLane.Monster,1,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual("C",match.Player.Monster(1).Card.Id);
            Assert.AreEqual(2,match.Player.Graveyard.Count);
            Assert.IsFalse(match.Player.Graveyard.Any(x=>x.Id=="F"));
        }

        [Test]
        public void DuplicateFusionRecipeIsRejected()
        {
            var catalog=new FusionCatalog();var c=Monster("c",1000,1000);
            catalog.Add("a","b",c);
            Assert.Throws<ArgumentException>(()=>catalog.Add("b","a",c));
        }

        [Test]
        public void SimpleOpponentAiCompletesAnOpponentTurn()
        {
            var match=new DuelMatch(Deck(Support("opening")),Deck(Monster("o",1900,900)));
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
                BattlePosition.Attack,GuardianStar.None,true,out _));
            End(match,DuelSide.Player);
            SimpleOpponentAi.TakeTurn(match);
            Assert.AreEqual(DuelSide.Player,match.CurrentSide);
            Assert.AreEqual(6100,match.Player.Life);
            Assert.AreEqual(3,match.Turn);
            Assert.IsNotNull(match.Opponent.Monster(0));
        }

        [Test]
        public void SimpleOpponentAiDoesNotActOnWrongTurn()
        {
            var match=Match(Monster("p",500,500),Monster("o",1900,900));
            SimpleOpponentAi.TakeTurn(match);
            Assert.AreEqual(DuelSide.Player,match.CurrentSide);
            Assert.IsNull(match.Opponent.Monster(0));
        }

        [Test]
        public void DirectDamageCanFinishMatchAndBlockFurtherChanges()
        {
            var match=Match(Monster("p",8500,500),Monster("o",500,500));
            Play(match,DuelSide.Player);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(MatchPhase.Finished,match.Phase);
            Assert.AreEqual(DuelSide.Player,match.Winner);
            Assert.AreEqual(0,match.Opponent.Life);
            Assert.IsFalse(match.TryEndTurn(DuelSide.Player,out _));
        }

        [Test]
        public void MatchEventsHaveStrictlyIncreasingSequenceNumbers()
        {
            var match=Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player); Begin(match,DuelSide.Player);
            match.TryAttack(DuelSide.Player,0,-1,out _);
            var sequences=match.Events.Select(e=>e.Sequence).ToArray();
            Assert.AreEqual(sequences.Distinct().Count(),sequences.Length);
            Assert.IsTrue(sequences.SequenceEqual(sequences.OrderBy(x=>x)));
        }

        [Test]
        public void MatchStateDoesNotDependOnRendering()
        {
            var match=Match(Monster("p",1800,1000),Monster("o",1000,1000));
            int changes=0;
            match.Changed += ()=>changes++;
            Play(match,DuelSide.Player); Begin(match,DuelSide.Player);
            match.TryAttack(DuelSide.Player,0,-1,out _);
            Assert.GreaterOrEqual(changes,3);
            Assert.AreEqual(6200,match.Opponent.Life);
        }
    }
}
