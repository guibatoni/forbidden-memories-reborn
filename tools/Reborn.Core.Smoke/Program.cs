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
    private static void CheckOriginalStarterChoices()
    {
        var catalog = new List<CardDefinition>();
        for (int id = 1; id <= 722; id++)
        {
            CardKind kind = id <= 690 ? CardKind.Monster :
                id <= 719 ? CardKind.Spell : CardKind.Equip;
            int atk = id <= 200 ? 300 : id <= 400 ? 600 :
                id <= 600 ? 900 : 1600;
            int def = id <= 200 ? 350 : id <= 400 ? 650 :
                id <= 600 ? 900 : 1100;
            catalog.Add(new CardDefinition(id.ToString("000"),
                "Synthetic #" + id, kind, kind == CardKind.Monster ? atk : 0,
                kind == CardKind.Monster ? def : 0, ""));
        }
        var weights = new List<int[]>();
        for (int group=0;group<7;group++)
        {
            var row = new int[722];
            int start = group == 0 ? 1 : group == 1 ? 201 :
                group == 2 ? 401 : group == 3 ? 601 :
                group == 4 ? 691 : group == 5 ? 705 : 720;
            int count = group < 2 ? 8 : group == 6 ? 3 : 4;
            int baseWeight = 2048/count;
            for (int i=0;i<count;i++) row[start-1+i]=baseWeight;
            row[start-1]+=2048-baseWeight*count;
            weights.Add(row);
        }
        var source = new StarterDeckCatalog(catalog,weights);
        var options = source.FourChoices(new Random(9917));
        Check(options.Count == 4 && options.All(d => d.Count == 40),
            "four independent original-pool starter proposals, 40 cards each");
        var signatures = new HashSet<string>();
        foreach(var deck in options)
        {
            Check(deck.GroupBy(c=>c.Id).All(g=>g.Count()<=3),
                "starter caps duplicated physical cards at three copies");
            Check(deck.Count(c=>c.Kind==CardKind.Monster &&
                c.Attack+c.Defense<1100)==16 &&
                deck.Count(c=>c.Kind==CardKind.Monster &&
                c.Attack+c.Defense>=1100 && c.Attack+c.Defense<1600)==16 &&
                deck.Count(c=>c.Kind==CardKind.Monster &&
                c.Attack+c.Defense>=1600 && c.Attack+c.Defense<2100)==4 &&
                deck.Count(c=>c.Kind==CardKind.Monster &&
                c.Attack+c.Defense>=2100)==1 &&
                deck.Count(c=>c.Kind==CardKind.Spell)==2 &&
                deck.Count(c=>c.Kind==CardKind.Equip)==1,
                "starter distribution matches authentic 16+16+4+1+1+1+1");
            signatures.Add(string.Join(",", deck.Select(c=>c.Id)
                .OrderBy(x=>x,StringComparer.Ordinal)));
        }
        Check(signatures.Count==4,
            "four starter choices have distinct physical card sets");
        var corrupt = weights.Select(w=>(int[])w.Clone()).ToList();
        corrupt[0][0]=0;
        try
        {
            new StarterDeckCatalog(catalog,corrupt);
            throw new Exception("FAIL corrupt starter weights were accepted");
        }
        catch(ArgumentException)
        {
            Check(true,"corrupt original starter rows rejected");
        }
    }

    private static void CheckVerifiedMagicEngine()
    {
        var spells = VerifiedSpellCatalog.OriginalPs1Core();
        Check(spells.Count == 18 &&
            spells.TryGet(S("338"), out var recovery) &&
            recovery.Amount == 200 &&
            recovery.Operation == VerifiedSpellOperation.RestoreLife &&
            !spells.TryGet(S("701"), out _),
            "0.21 PS1 rule catalog retains exact verified magic and terrain IDs");

        var healing = new DuelMatch(Deck(S("338")), Deck(M("enemy",1000,900)),
            null, null, spells);
        Check(healing.TryPlay(DuelSide.Player, new[]{0}, FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,false,out var why) &&
            healing.Player.Life == 8200 &&
            healing.Player.Support(0) == null &&
            healing.Player.Graveyard.Count == 1 &&
            healing.Player.Graveyard[0].Id == "338" &&
            healing.Events.Any(e=>e.Kind==MatchEventKind.SupportActivated &&
                e.CardId=="338") &&
            healing.Events.Any(e=>e.Kind==MatchEventKind.LifeChanged &&
                e.Value==200), "0.19 face-up Mooyan Curry heals 200 and goes to graveyard");
        Check(!healing.TryPlay(DuelSide.Player,new[]{0},FieldLane.Monster,1,
            BattlePosition.Attack,GuardianStar.None,out _),
            "0.19 instant spell still consumes exactly one mandatory play");

        var fakeEffect = new CardDefinition("701","Synthetic unverified",
            CardKind.Spell,0,0,"",GuardianStar.None,GuardianStar.None,
            "Destroy all cards and win","untrusted-text","not-a-verified-rule");
        var unknown = new DuelMatch(Deck(fakeEffect), Deck(M("enemy",1500,1200)),
            null,null,spells);
        int beforeEvents=unknown.Events.Count;
        int beforeHand=unknown.Player.Hand.Count;
        Check(!unknown.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,false,out _) &&
            unknown.Events.Count==beforeEvents &&
            unknown.Player.Hand.Count==beforeHand &&
            !unknown.PlayedThisTurn &&
            unknown.Player.Support(0)==null,
            "0.19 unknown face-up spell is rejected atomically; text cannot execute");
        Check(unknown.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,true,out _) &&
            unknown.Player.Support(0)?.Id=="701" &&
            !unknown.CanActivateSupport(DuelSide.Player,0,out _),
            "0.19 unknown spell can be prepared facedown without fabricating an effect");

        var prepared = new DuelMatch(Deck(S("343")),
            Deck(M("rival",1000,1000)),null,null,spells);
        Check(prepared.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,true,out _) &&
            !prepared.CanActivateSupport(DuelSide.Player,0,out _),
            "0.19 facedown Sparks remains hidden and cannot activate in placement turn");
        End(prepared,DuelSide.Player);
        Play(prepared,DuelSide.Opponent);
        End(prepared,DuelSide.Opponent);
        int supportEvents = prepared.Events.Count;
        Check(prepared.CanActivateSupport(DuelSide.Player,0,out _) &&
            prepared.Events.Count==supportEvents &&
            prepared.TryActivateSupport(DuelSide.Player,0,out _) &&
            prepared.Opponent.Life==7950 &&
            prepared.Player.Support(0)==null &&
            prepared.Player.Graveyard.Count==1 &&
            !prepared.PlayedThisTurn &&
            prepared.Events.Any(e=>e.Kind==MatchEventKind.SupportActivated &&
                e.CardId=="343"),
            "0.19 previously set Sparks activates later without spending a second placement");
        Check(!prepared.TryActivateSupport(DuelSide.Player,0,out _),
            "0.19 spent spell cannot be activated twice");

        var lightning = new DuelMatch(
            Deck(M("mine",1900,1600),S("337")),
            Deck(M("enemy",2000,1400)),null,null,spells);
        Play(lightning,DuelSide.Player);
        End(lightning,DuelSide.Player);
        Play(lightning,DuelSide.Opponent);
        End(lightning,DuelSide.Opponent);
        Check(lightning.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,false,out _) &&
            lightning.Player.Monster(0)!=null &&
            lightning.Opponent.Monster(0)==null &&
            lightning.Opponent.Graveyard.Any(c=>c.Id=="enemy") &&
            lightning.Events.Any(e=>e.Kind==MatchEventKind.Destroy &&
                e.Side==DuelSide.Opponent && e.Lane==FieldLane.Monster),
            "0.19 Raigeki destroys opponent monsters but not allied monsters");

        var dark = new DuelMatch(
            Deck(M("own",900,1200),S("336")),
            Deck(M("foe",1000,1000)),null,null,spells);
        Play(dark,DuelSide.Player);
        End(dark,DuelSide.Player);
        Play(dark,DuelSide.Opponent);
        End(dark,DuelSide.Opponent);
        Check(dark.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,false,out _) &&
            dark.Player.MonsterCount==0 &&
            dark.Opponent.MonsterCount==0 &&
            dark.Player.Graveyard.Any(c=>c.Id=="own") &&
            dark.Opponent.Graveyard.Any(c=>c.Id=="foe"),
            "0.19 Dark Hole wipes both monster rows and buries physical cards");

        var lethal = new DuelMatch(Deck(S("343")),Deck(M("other",800,800)),
            null,null,spells);
        lethal.Opponent.Life=40;
        Check(lethal.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,false,out _) &&
            lethal.Opponent.Life==0 &&
            lethal.Phase==MatchPhase.Finished &&
            lethal.Winner==DuelSide.Player,
            "0.19 magical damage can finish a duel without an attack");

        var noRules = new DuelMatch(Deck(S("343")),Deck(M("other",800,800)));
        Check(noRules.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,false,out _) &&
            noRules.Opponent.Life==8000 &&
            noRules.Player.Support(0)!=null,
            "0.19 synthetic training mode never executes Original PS1 spells");
    }

    private static void CheckVerifiedEquipEngine()
    {
        var rules = VerifiedEquipCatalog.OriginalPs1Foundation();
        var megamorph = new CardDefinition("657","Megamorph",CardKind.Equip,0,0,"");
        var castle = new CardDefinition("668","Bright Castle",CardKind.Equip,0,0,"");
        Check(rules.Count == 2 &&
            rules.CanApply(megamorph,M("001",1200,900),out var megaRule) &&
            megaRule.AttackGain == 1000 && megaRule.DefenseGain == 1000 &&
            rules.CanApply(castle,M("050",500,500),out var castleRule) &&
            castleRule.AttackGain == 500 && castleRule.DefenseGain == 500,
            "0.20 PS1 universal Megamorph and Bright Castle are explicitly verified");

        var specific = new VerifiedEquipCatalog();
        specific.AddTargets("315",500,500,new[]{"001","002"});
        var treasure = new CardDefinition("315","Dragon Treasure",CardKind.Equip,0,0,"");
        Check(specific.CanApply(treasure,M("001",1000,1000),out _) &&
            !specific.CanApply(treasure,M("003",1000,1000),out _) &&
            !specific.CanApply(megamorph,M("001",1000,1000),out _),
            "0.20 monster-ID equip allowlist never guesses compatibility by name or type");

        var match = new DuelMatch(
            Deck(M("own",1700,900),megamorph,castle),
            Deck(M("enemy",900,900)),null,null,null,rules);
        Play(match,DuelSide.Player);
        End(match,DuelSide.Player);
        Play(match,DuelSide.Opponent);
        End(match,DuelSide.Opponent);
        int handBefore = match.Player.Hand.Count;
        int graveBefore = match.Player.Graveyard.Count;
        Check(match.CanEquipFromHand(DuelSide.Player,0,0,out _,out _) &&
            match.Player.Monster(0).EffectiveAttack == 1700 &&
            match.Events.Count > 0,
            "0.20 read-only equip preview leaves monster unmodified");
        Check(match.TryEquipFromHand(DuelSide.Player,0,0,out _) &&
            match.Player.Monster(0).EffectiveAttack == 2700 &&
            match.Player.Monster(0).EffectiveDefense == 1900 &&
            match.Player.Monster(0).EquipCount == 1 &&
            match.Player.Hand.Count == handBefore-1 &&
            match.Player.Graveyard.Count == graveBefore+1 &&
            match.Player.Graveyard.Last().Id == "657" &&
            match.Player.Support(0) == null &&
            match.Events.Any(e=>e.Kind==MatchEventKind.Equipped &&
                e.CardId=="657" && e.Value==1000),
            "0.20 Megamorph adds +1000 ATK/DEF and vanishes, consuming exactly one hand play");
        Check(!match.TryEquipFromHand(DuelSide.Player,0,0,out _) &&
            !match.CanEquipFromHand(DuelSide.Player,0,0,out _,out _),
            "0.20 cannot consume a second hand play in the same turn");
        Battle(match,DuelSide.Player);
        Check(match.TryPreviewAttack(DuelSide.Player,0,0,out var equippedForecast,out _) &&
            equippedForecast.AttackerBase == 2700 &&
            equippedForecast.AttackerEquipBonus == 1000 &&
            equippedForecast.DamageToDefender == 1800,
            "0.20 combat prediction includes equipped ATK without changing LP/events");
        Check(match.TryAttack(DuelSide.Player,0,0,out _) &&
            match.Opponent.Life == 6200,
            "0.20 resolved combat uses the same equipped attack power");
        End(match,DuelSide.Player);
        // Opponent has lost its first monster: place the next in a new zone.
        Check(match.TryPlay(DuelSide.Opponent,new[]{0},
            FieldLane.Monster,0,BattlePosition.Defense,
            GuardianStar.None,out _),"0.20 fixture opponent placement after combat");
        End(match,DuelSide.Opponent);
        Check(match.TryEquipFromHand(DuelSide.Player,0,0,out _) &&
            match.Player.Monster(0).EffectiveAttack == 3200 &&
            match.Player.Monster(0).EffectiveDefense == 2400 &&
            match.Player.Monster(0).EquipCount == 2 &&
            match.Player.Graveyard.Any(c=>c.Id=="668"),
            "0.20 stacking two retail equips preserves both bonuses");

        var concealed = new DuelMatch(
            Deck(M("hidden",1500,1500),megamorph),
            Deck(M("foe",950,950)),null,null,null,rules);
        Check(concealed.TryPlay(DuelSide.Player,new[]{0},FieldLane.Monster,0,
            BattlePosition.Defense,GuardianStar.None,true,out _),
            "0.20 facedown monster set");
        End(concealed,DuelSide.Player);
        Play(concealed,DuelSide.Opponent);
        End(concealed,DuelSide.Opponent);
        int concealedEvents=concealed.Events.Count;
        int concealedHand=concealed.Player.Hand.Count;
        Check(!concealed.TryEquipFromHand(DuelSide.Player,0,0,out _) &&
            concealed.Player.Monster(0).FaceDown &&
            concealed.Player.Monster(0).EquipCount==0 &&
            concealed.Events.Count==concealedEvents &&
            concealed.Player.Hand.Count==concealedHand &&
            !concealed.PlayedThisTurn,
            "0.20 hidden monster cannot be boosted or revealed by rejected equip");

        var saved = new DuelMatch(
            Deck(M("own",1600,1300),megamorph),
            Deck(M("enemy",600,600)),null,null,null,rules);
        Play(saved,DuelSide.Player);
        End(saved,DuelSide.Player);
        Play(saved,DuelSide.Opponent);
        End(saved,DuelSide.Opponent);
        Check(saved.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,1,
            BattlePosition.Attack,GuardianStar.None,true,out _) &&
            saved.Player.SupportIsFaceDown(1) &&
            !saved.CanEquipFromSupport(DuelSide.Player,1,0,out _,out _),
            "0.20 set equip is not activatable in same turn");
        End(saved,DuelSide.Player);
        Check(saved.TryPlay(DuelSide.Opponent,new[]{0},
            FieldLane.Monster,1,BattlePosition.Attack,
            GuardianStar.None,out _),"0.20 fixture set equip next turn");
        End(saved,DuelSide.Opponent);
        Check(saved.CanEquipFromSupport(DuelSide.Player,1,0,out _,out _) &&
            saved.TryEquipFromSupport(DuelSide.Player,1,0,out _) &&
            saved.Player.Support(1)==null &&
            saved.Player.Monster(0).EffectiveAttack==2600 &&
            !saved.PlayedThisTurn &&
            !saved.TryEquipFromSupport(DuelSide.Player,1,0,out _),
            "0.20 next-turn set equip activates exactly once, without consuming placement");

        var unknown = new VerifiedEquipCatalog();
        var failed = new DuelMatch(
            Deck(M("mine",1100,1100),megamorph),
            Deck(M("rival",800,800)),null,null,null,unknown);
        Play(failed,DuelSide.Player);
        End(failed,DuelSide.Player);
        Play(failed,DuelSide.Opponent);
        End(failed,DuelSide.Opponent);
        int eventsBefore=failed.Events.Count;
        Check(!failed.TryEquipFromHand(DuelSide.Player,0,0,out _) &&
            failed.Events.Count==eventsBefore &&
            !failed.PlayedThisTurn &&
            failed.Player.Monster(0).EquipCount==0 &&
            failed.Player.Hand.Any(c=>c.Id=="657"),
            "0.20 missing verified compatibility never consumes physical cards");
    }

    private static CardDefinition Typed(string id, int atk, int def, string type)
        => new CardDefinition(id, id, CardKind.Monster, atk, def,
            "synthetic", GuardianStar.None, GuardianStar.None, "", "", "", type);

    private static CardDefinition Trap(string id)
        => new CardDefinition(id, id, CardKind.Trap, 0, 0, "synthetic");

    private static void CheckTerrainAndTraps()
    {
        Check(TerrainRules.Bonus("332", Typed("dragon",1000,1000,"Dragon")) == 500 &&
            TerrainRules.Bonus("332", Typed("warrior",1000,1000,"Warrior")) == 0 &&
            TerrainRules.Bonus("335", Typed("fairy",1000,1000,"Fairy")) == -500 &&
            TerrainRules.Bonus("334", Typed("machine",1000,1000,"Machine")) == -500 &&
            TerrainRules.Bonus("334", Typed("thunder",1000,1000,"Thunder")) == 500 &&
            TerrainRules.Bonus("330", Typed("beast",1000,1000,"Beast-Warrior")) == 500 &&
            TerrainRules.Bonus("335", Typed("missing",1000,1000,"")) == 0,
            "0.21 six fields include positive and negative type bonuses; unknown types not guessed");

        var terrainDuel = new DuelMatch(
            Deck(Typed("dragon",1800,1500,"Dragon"),S("332"),S("330")),
            Deck(Typed("wizard",1500,1400,"Spellcaster"),S("335")),
            null,null,VerifiedSpellCatalog.OriginalPs1Core());
        Play(terrainDuel,DuelSide.Player);
        End(terrainDuel,DuelSide.Player);
        Play(terrainDuel,DuelSide.Opponent);
        End(terrainDuel,DuelSide.Opponent);
        Check(terrainDuel.TryPlay(DuelSide.Player,new[]{0},
            FieldLane.Support,0,BattlePosition.Attack,GuardianStar.None,
            false,out _) && terrainDuel.CurrentTerrainCardId == "332" &&
            terrainDuel.Player.Support(0) == null &&
            terrainDuel.Player.Graveyard.Any(c=>c.Id=="332") &&
            terrainDuel.Events.Any(e=>e.Kind==MatchEventKind.TerrainChanged &&
                e.CardId=="332"),
            "0.21 Mountain activates immediately, becomes one global terrain and goes to graveyard");
        Battle(terrainDuel,DuelSide.Player);
        Check(terrainDuel.TryPreviewAttack(DuelSide.Player,0,0,
            out var mountainForecast,out _) &&
            mountainForecast.AttackerBase == 2300 &&
            mountainForecast.AttackerTerrainBonus == 500 &&
            mountainForecast.DefenderTerrainBonus == 0,
            "0.21 global terrain affects combat forecast before attack");
        End(terrainDuel,DuelSide.Player);
        Check(terrainDuel.TryPlay(DuelSide.Opponent,new[]{0},
            FieldLane.Support,0,BattlePosition.Attack,GuardianStar.None,
            false,out _) && terrainDuel.CurrentTerrainCardId=="335",
            "0.21 opponent Yami replaces Mountain across both players");
        Battle(terrainDuel,DuelSide.Opponent);
        Check(terrainDuel.TryPreviewAttack(DuelSide.Opponent,0,0,
            out var yamiForecast,out _) &&
            yamiForecast.AttackerTerrainBonus==500 &&
            yamiForecast.DefenderTerrainBonus==0 &&
            yamiForecast.AttackerBase==2000,
            "0.21 opponent field bonus is applied to both sides rather than only caster");

        var allTraps=AttackTrapCatalog.OriginalPs1();
        Check(allTraps.Count==6 &&
            allTraps.TryMatch(Trap("681"),500,out int limit) && limit==500 &&
            !allTraps.TryMatch(Trap("681"),501,out _) &&
            allTraps.TryMatch(Trap("685"),3000,out _) &&
            !allTraps.TryMatch(Trap("685"),3001,out _) &&
            allTraps.TryMatch(Trap("686"),25000,out _),
            "0.21 traps #681-686 use inclusive retail attack thresholds");

        var protect = new DuelMatch(
            Deck(M("attacker",1450,1000)),
            Deck(Trap("686"),Trap("683"),M("foe",800,850)),
            null,null,null,null,allTraps);
        Play(protect,DuelSide.Player);
        End(protect,DuelSide.Player);
        Check(protect.TryPlay(DuelSide.Opponent,new[]{0},FieldLane.Support,1,
            BattlePosition.Defense,GuardianStar.None,true,out _),
            "0.21 opponent sets Widespread Ruin without revealing its content");
        End(protect,DuelSide.Opponent);
        Battle(protect,DuelSide.Player); End(protect,DuelSide.Player);
        Check(protect.TryPlay(DuelSide.Opponent,new[]{0},FieldLane.Support,2,
            BattlePosition.Defense,GuardianStar.None,true,out _),
            "0.21 opponent sets Bear Trap on another turn");
        End(protect,DuelSide.Opponent);
        Battle(protect,DuelSide.Player);
        int eventsBefore=protect.Events.Count;
        int hpBefore=protect.Opponent.Life;
        Check(protect.TryPreviewAttack(DuelSide.Player,0,-1,out var cleanPreview,out _) &&
            protect.Events.Count==eventsBefore &&
            protect.Opponent.SupportIsFaceDown(1) &&
            protect.Opponent.SupportIsFaceDown(2) &&
            cleanPreview.DamageToDefender==1450,
            "0.21 preview does not reveal either hidden trap");
        Check(protect.TryAttack(DuelSide.Player,0,-1,out _) &&
            protect.Player.Monster(0)==null &&
            protect.Opponent.Life==hpBefore &&
            protect.Opponent.Support(2)==null &&
            protect.Opponent.Support(1)?.Id=="686" &&
            protect.Opponent.Graveyard.Any(c=>c.Id=="683") &&
            protect.Events.Any(e=>e.Kind==MatchEventKind.TrapTriggered &&
                e.CardId=="683"),
            "0.21 lowest eligible trap interrupts direct attack, kills attacker and leaves broader trap set");
        Check(!protect.TryAttack(DuelSide.Player,0,-1,out _),
            "0.21 destroyed attacker cannot declare a second attack");

        var tooWeak = new DuelMatch(Deck(M("strong",1050,800)),
            Deck(Trap("681"),M("small",800,900)),
            null,null,null,null,allTraps);
        Play(tooWeak,DuelSide.Player); End(tooWeak,DuelSide.Player);
        Check(tooWeak.TryPlay(DuelSide.Opponent,new[]{0},FieldLane.Support,0,
            BattlePosition.Defense,GuardianStar.None,true,out _),
            "0.21 one narrow trap placed");
        End(tooWeak,DuelSide.Opponent);
        Battle(tooWeak,DuelSide.Player);
        Check(tooWeak.TryAttack(DuelSide.Player,0,-1,out _) &&
            tooWeak.Opponent.Life==6950 &&
            tooWeak.Opponent.Support(0)?.Id=="681" &&
            !tooWeak.Events.Any(e=>e.Kind==MatchEventKind.TrapTriggered),
            "0.21 a trap below attack power stays hidden and ordinary battle resolves");
    }

    private static void CheckReactiveTraps()
    {
        var catalog=ReactiveTrapCatalog.OriginalPs1();
        Check(catalog.Count==2 &&
            catalog.Matches(Trap("687"),
                ReactiveTrapOperation.ReflectDirectSpellDamage) &&
            catalog.Matches(Trap("688"),
                ReactiveTrapOperation.ReverseSpellHealing) &&
            !catalog.Matches(Trap("689"),
                ReactiveTrapOperation.ReflectDirectSpellDamage),
            "0.22 reactive traps are exactly Goblin Fan and Simochi");

        var game=new DuelMatch(
            Deck(M("mine",1000,800), S("343"), S("339")),
            Deck(Trap("687"),Trap("688"),M("foe",900,900)),
            null,null,VerifiedSpellCatalog.OriginalPs1Core(),
            null,AttackTrapCatalog.OriginalPs1(),catalog);
        Play(game,DuelSide.Player); End(game,DuelSide.Player);
        Check(game.TryPlay(DuelSide.Opponent,new[]{0},FieldLane.Support,1,
            BattlePosition.Defense,GuardianStar.None,true,out _),
            "0.22 opponent prepares Goblin Fan in prior turn");
        End(game,DuelSide.Opponent);
        int events=game.Events.Count;
        int grave=game.Opponent.Graveyard.Count;
        Check(game.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,false,out _) &&
            game.Player.Life==7950 && game.Opponent.Life==8000 &&
            game.Opponent.Support(1)==null &&
            game.Opponent.Graveyard.Count==grave+1 &&
            game.Events.Any(e=>e.Kind==MatchEventKind.TrapTriggered &&
                e.CardId=="687") &&
            game.Events.Count>events,
            "0.22 Goblin Fan redirects Sparks burn to original caster and consumes trap once");
        End(game,DuelSide.Player);
        Check(game.TryPlay(DuelSide.Opponent,new[]{0},FieldLane.Support,2,
            BattlePosition.Defense,GuardianStar.None,true,out _),
            "0.22 opponent sets Simochi independently");
        End(game,DuelSide.Opponent);
        Check(game.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,false,out _) &&
            game.Player.Life==7450 && game.Opponent.Life==8000 &&
            game.Opponent.Support(2)==null &&
            game.Opponent.Graveyard.Any(c=>c.Id=="688") &&
            game.Events.Any(e=>e.Kind==MatchEventKind.TrapTriggered &&
                e.CardId=="688"),
            "0.22 Simochi converts Red Medicine +500 into -500 LP to healing caster");

        var unrelated=new DuelMatch(
            Deck(M("mine",800,600),S("343")),
            Deck(Trap("688"),M("foe",600,600)),
            null,null,VerifiedSpellCatalog.OriginalPs1Core(),
            null,null,catalog);
        Play(unrelated,DuelSide.Player);End(unrelated,DuelSide.Player);
        Check(unrelated.TryPlay(DuelSide.Opponent,new[]{0},FieldLane.Support,0,
            BattlePosition.Defense,GuardianStar.None,true,out _),
            "0.22 unrelated Simochi is prepared");
        End(unrelated,DuelSide.Opponent);
        Check(unrelated.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,1,
            BattlePosition.Attack,GuardianStar.None,false,out _) &&
            unrelated.Opponent.Life==7950 &&
            unrelated.Opponent.Support(0)?.Id=="688",
            "0.22 Simochi must not trigger on a burn spell");
        var fake=new DuelMatch(Deck(S("343")),
            Deck(Trap("689"),Trap("690")),
            null,null,VerifiedSpellCatalog.OriginalPs1Core(),
            null,null,catalog);
        // Synthetic placeholders #689/#690 cannot execute through 0.22
        Check(!catalog.Matches(Trap("690"),
                ReactiveTrapOperation.ReverseSpellHealing) &&
            fake.Opponent.Support(0)==null,
            "0.22 Reverse Trap/Fake Trap are never assigned invented effects");

        var safe=new DuelMatch(Deck(S("343")),
            Deck(Trap("687")),null,null,
            VerifiedSpellCatalog.OriginalPs1Core(),null,null,catalog);
        Check(safe.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
            BattlePosition.Attack,GuardianStar.None,false,out _) &&
            safe.Opponent.Life==7950 &&
            safe.Player.Life==8000,
            "0.22 no set opponent trap means ordinary burn behavior unchanged");
    }

    private static void CheckNewFusionAndAutoSlotRules()
    {
        // Explicit recipes only: this is synthetic CI data, not a ROM import.
        var alpha = M("fusion-alpha",850,650);
        var beta = M("fusion-beta",1000,600);
        var combined = M("fusion-result",2200,1300);
        var recipes = new FusionCatalog();
        recipes.Add(alpha.Id,beta.Id,combined);
        var duel = new DuelMatch(Deck(alpha,beta,M("other",900,900)),
            Deck(M("rival",900,800)),recipes);
        int before = duel.Events.Count;
        Check(duel.FusionRecipeCount == 1 &&
            duel.CountHandFusionSteps(DuelSide.Player,new[]{0,1})==1 &&
            duel.CountHandFusionSteps(DuelSide.Player,new[]{1,0})==1 &&
            duel.CountHandFusionSteps(DuelSide.Player,new[]{0,2})==0 &&
            duel.CountHandFusionSteps(DuelSide.Player,new[]{0,0})==0 &&
            duel.PreviewResult(DuelSide.Player,new[]{0,1}).Id==combined.Id &&
            duel.Events.Count==before && !duel.PlayedThisTurn,
            "0.24 ordered hand fusion preview uses actual recipe without spending cards");
        Check(duel.TryPlay(DuelSide.Player,new[]{0,1},FieldLane.Monster,0,
            BattlePosition.Defense,GuardianStar.None,true,out _) &&
            duel.Player.Monster(0).Card.Id==combined.Id &&
            duel.Player.Monster(0).Position==BattlePosition.Defense &&
            duel.Player.Monster(0).FaceDown &&
            duel.Player.Hand.Count==3 &&
            duel.Player.Monster(1)==null &&
            duel.Events.Any(e=>e.Kind==MatchEventKind.Fusion),
            "0.24 two materials fuse in a single confirmed turn and respect preselected DEF/FaceDown");

        var slots = new DuelMatch(Deck(alpha,beta,M("next",1000,1000)),
            Deck(M("rival",800,800)),recipes);
        Check(slots.TryPlay(DuelSide.Player,new[]{0},FieldLane.Monster,0,
            BattlePosition.Attack,GuardianStar.None,out _),
            "0.24 occupied slot fixture");
        End(slots,DuelSide.Player);
        Play(slots,DuelSide.Opponent);
        End(slots,DuelSide.Opponent);
        Check(!slots.CanPlay(DuelSide.Player,new[]{0},
                FieldLane.Monster,0,GuardianStar.None,out _) &&
            slots.CanPlay(DuelSide.Player,new[]{0},
                FieldLane.Monster,1,GuardianStar.None,out _) &&
            slots.TryPlay(DuelSide.Player,new[]{0},FieldLane.Monster,1,
                BattlePosition.Attack,GuardianStar.None,out _) &&
            slots.Player.Monster(0).Card.Id == alpha.Id &&
            slots.Player.Monster(1).Card.Id == beta.Id,
            "0.24 first free slot is 2 when slot 1 occupied, without overwriting first monster");

        var field = new DuelMatch(Deck(alpha,beta),
            Deck(M("enemy",700,700)),recipes);
        Play(field,DuelSide.Player);End(field,DuelSide.Player);
        Play(field,DuelSide.Opponent);End(field,DuelSide.Opponent);
        Check(field.PreviewFieldResult(DuelSide.Player,new[]{0},0)?.Id ==
            combined.Id &&
            field.TryPlayOnField(DuelSide.Player,new[]{0},0,
                BattlePosition.Attack,GuardianStar.None,false,
                out var result,out var fused,out _) &&
            fused && result.Id==combined.Id &&
            field.Player.Monster(0).Card.Id==combined.Id,
            "0.24 on-field fusion accepts actual own monster as material #1");

        var equip=new CardDefinition("657","Megamorph",CardKind.Equip,0,0,"");
        var equipMatch = new DuelMatch(
            Deck(M("owned",1400,1200),equip),
            Deck(M("opponent",900,900)),null,null,null,
            VerifiedEquipCatalog.OriginalPs1Foundation());
        Play(equipMatch,DuelSide.Player);End(equipMatch,DuelSide.Player);
        Play(equipMatch,DuelSide.Opponent);End(equipMatch,DuelSide.Opponent);
        Check(equipMatch.CanPlay(DuelSide.Player,new[]{0},
                FieldLane.Support,0,GuardianStar.None,out _) &&
            equipMatch.TryPlay(DuelSide.Player,new[]{0},FieldLane.Support,0,
                BattlePosition.Defense,GuardianStar.None,true,out _) &&
            equipMatch.Player.Monster(0).EffectiveAttack==1400 &&
            equipMatch.Player.Support(0)?.Id=="657" &&
            equipMatch.Player.SupportIsFaceDown(0),
            "0.24 setting Equip over occupied monster does not apply bonus prematurely");
        End(equipMatch,DuelSide.Player);
        Check(equipMatch.TryPlay(DuelSide.Opponent,new[]{0},
            FieldLane.Monster,1,BattlePosition.Attack,
            GuardianStar.None,out _),"0.24 equip next turn fixture");
        End(equipMatch,DuelSide.Opponent);
        Check(equipMatch.TryEquipFromSupport(DuelSide.Player,0,0,out _) &&
            equipMatch.Player.Monster(0).EffectiveAttack==2400 &&
            equipMatch.Player.Support(0)==null &&
            !equipMatch.PlayedThisTurn,
            "0.24 set-over-monster Equip activates on next turn with +1000 and no extra placement");
    }

    private static void CheckAvailableFusionAssistant()
    {
        // Synthetic IDs: exercising the rules engine, not pretending these
        // pairings belong to the user's retail ISO.
        var first = M("hero", 1000, 700);
        var second = M("supporter", 800, 600);
        var result = M("fusion-win", 2300, 1800);
        var rules = new FusionCatalog();
        rules.Add(first.Id, second.Id, result);

        var handMatch = new DuelMatch(Deck(first, second, M("other", 900, 500)),
            Deck(M("enemy",1000,800)), rules);
        int eventCount = handMatch.Events.Count;
        int originalHand = handMatch.Player.Hand.Count;
        var choices = handMatch.FindAvailableFusions(DuelSide.Player);
        Check(choices.Count == 1 && !choices[0].UsesField &&
            choices[0].FirstHandIndex == 0 &&
            choices[0].SecondHandIndex == 1 &&
            choices[0].FieldSlot == -1 &&
            choices[0].Result.Id == result.Id &&
            handMatch.Events.Count == eventCount &&
            handMatch.Player.Hand.Count == originalHand &&
            !handMatch.PlayedThisTurn,
            "0.25 fusion assistant lists only validated, playable hand pairs without any mutation");
        Check(handMatch.FindAvailableFusions(DuelSide.Opponent).Count == 0,
            "0.25 fusion assistant cannot inspect or suggest opponent private hand during player turn");
        Check(handMatch.TryPlay(DuelSide.Player,
            new[] {choices[0].FirstHandIndex,choices[0].SecondHandIndex},
            FieldLane.Monster,0,BattlePosition.Defense,GuardianStar.None,true,
            out var reason) && handMatch.Player.Monster(0).Card.Id == result.Id &&
            handMatch.Player.Monster(0).FaceDown &&
            handMatch.Player.Monster(0).Position == BattlePosition.Defense &&
            handMatch.FindAvailableFusions(DuelSide.Player).Count == 0,
            "0.25 selecting a suggested hand fusion performs exactly one real placement");

        var fieldMatch = new DuelMatch(Deck(first,second),
            Deck(M("enemy",1000,800)),rules);
        Play(fieldMatch,DuelSide.Player);
        End(fieldMatch,DuelSide.Player);
        Play(fieldMatch,DuelSide.Opponent);
        End(fieldMatch,DuelSide.Opponent);
        int previousEvents = fieldMatch.Events.Count;
        int previousHand = fieldMatch.Player.Hand.Count;
        var fields = fieldMatch.FindAvailableFusions(DuelSide.Player);
        Check(fields.Count == 1 && fields[0].UsesField &&
            fields[0].FieldSlot == 0 &&
            fields[0].FirstHandIndex == 0 &&
            fields[0].SecondHandIndex == -1 &&
            fields[0].Result.Id == result.Id &&
            fieldMatch.Events.Count == previousEvents &&
            fieldMatch.Player.Hand.Count == previousHand,
            "0.25 field assistant considers own field monster first and preserves read-only state");
        Check(fieldMatch.TryPlayOnField(DuelSide.Player,
            new[] {fields[0].FirstHandIndex}, fields[0].FieldSlot,
            BattlePosition.Attack,GuardianStar.None,false,
            out var fieldResult,out var fused,out _) &&
            fused && fieldResult.Id == result.Id,
            "0.25 field suggestion commits a verified on-table fusion");

        var none = new DuelMatch(Deck(first,second),Deck(M("enemy",1000,800)));
        Check(none.FusionRecipeCount == 0 &&
            none.FindAvailableFusions(DuelSide.Player).Count == 0,
            "0.25 missing imported recipes never generate invented fusion advice");
    }

    private static void Main()
    {
        CheckOriginalStarterChoices();
        CheckAvailableFusionAssistant();
        CheckNewFusionAndAutoSlotRules();
        CheckTerrainAndTraps();
        CheckReactiveTraps();
        CheckVerifiedEquipEngine();
        CheckVerifiedMagicEngine();
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
        // Prototype 0.16 — field card FIRST, followed by up to five hand
        // materials. Intermediate results are virtual (never buried).
        var fieldRecipes = new FusionCatalog();
        fieldRecipes.Add("base", "a", M("f1", 1400, 1200));
        fieldRecipes.Add("f1", "b", M("f2", 1700, 1450));
        fieldRecipes.Add("f2", "c", M("f3", 2200, 1550));
        fieldRecipes.Add("f3", "d", M("f4", 2500, 1600));
        fieldRecipes.Add("f4", "e", M("f5", 3000, 2300, GuardianStar.Mercury));
        var fieldGame = new DuelMatch(
            Deck(M("base", 900, 850), M("a", 400, 400),
                M("b", 500, 500), M("c", 600, 600),
                M("d", 700, 700), M("e", 800, 800)),
            Deck(M("enemy", 900, 850)), fieldRecipes);
        Check(fieldGame.TryPlay(DuelSide.Player, new[]{0},
            FieldLane.Monster, 0, BattlePosition.Defense,
            GuardianStar.None, true, out _), "0.16 setup: field material set");
        End(fieldGame, DuelSide.Player);
        Play(fieldGame, DuelSide.Opponent);
        End(fieldGame, DuelSide.Opponent);
        var fiveMaterials = new[]{0,1,2,3,4};
        int previewEventCount = fieldGame.Events.Count;
        int previewGraveCount = fieldGame.Player.Graveyard.Count;
        var fieldPreview = fieldGame.PreviewFieldResult(DuelSide.Player,
            fiveMaterials, 0);
        Check(fieldPreview?.Id == "f5" &&
            fieldGame.CanPlayOnField(DuelSide.Player, fiveMaterials, 0,
                GuardianStar.Mercury, out var legalResult, out _) &&
            legalResult.Id == "f5",
            "field first plus five hand cards produces ordered sixth-card result");
        Check(fieldGame.Events.Count == previewEventCount &&
            fieldGame.Player.Graveyard.Count == previewGraveCount &&
            fieldGame.Player.Hand.Count == 5,
            "field fusion preview is read-only");
        Check(!fieldGame.CanPlayOnField(DuelSide.Player,
            new[]{0,0},0,GuardianStar.None,out _,out _) &&
            !fieldGame.CanPlayOnField(DuelSide.Player,
                new[]{5},0,GuardianStar.None,out _,out _) &&
            !fieldGame.CanPlayOnField(DuelSide.Player,
                new[]{0},-1,GuardianStar.None,out _,out _),
            "duplicate, invalid and out-of-range field materials are rejected");
        Check(fieldGame.TryPlayOnField(DuelSide.Player,
            fiveMaterials, 0, BattlePosition.Attack, GuardianStar.Mercury,
            true, out var fieldOutcome, out bool combined, out _) &&
            combined && fieldOutcome.Id == "f5" &&
            fieldGame.Player.Monster(0).Card.Id == "f5" &&
            !fieldGame.Player.Monster(0).FaceDown &&
            fieldGame.Player.Monster(0).Star == GuardianStar.Mercury,
            "validated six-material field fusion sets correct face-up result and star");
        Check(fieldGame.Player.Hand.Count == 0 &&
            fieldGame.Player.Graveyard.Count == previewGraveCount + 6 &&
            fieldGame.Events.Count(e => e.Kind == MatchEventKind.Fusion &&
                e.Side == DuelSide.Player) == 5,
            "only six physical materials buried; no virtual intermediate results");
        Check(!fieldGame.CanPlayOnField(DuelSide.Player,
            new[]{0},0,GuardianStar.None,out _,out _) &&
            !fieldGame.TryPlayOnField(DuelSide.Player,
                new[]{0},0,BattlePosition.Attack,GuardianStar.None,false,
                out _,out _,out _),
            "field fusion consumes the same one mandatory placement action");

        // A failed recipe replaces the older field monster with the last
        // selected hand monster; unsupported Equip effects never run.
        var failedField = new DuelMatch(
            Deck(M("old", 2400, 1800), S("unverifiedEquip"),
                M("new", 1300, 900)), Deck(M("op", 1000, 1000)));
        Play(failedField,DuelSide.Player);
        End(failedField,DuelSide.Player);
        Play(failedField,DuelSide.Opponent);
        End(failedField,DuelSide.Opponent);
        int failedBefore = failedField.Events.Count;
        Check(!failedField.CanPlayOnField(DuelSide.Player,new[]{0},
            0,GuardianStar.None,out _,out _) &&
            !failedField.TryPlayOnField(DuelSide.Player,new[]{0},
                0,BattlePosition.Attack,GuardianStar.None,false,
                out _,out _,out _),
            "unverified equip on occupied monster cannot fabricate an effect");
        Check(failedField.Events.Count == failedBefore &&
            failedField.Player.Monster(0).Card.Id == "old" &&
            failedField.Player.Graveyard.Count == 0,
            "invalid support combination preserves full duel state");
        Check(failedField.TryPlayOnField(DuelSide.Player,new[]{1},
            0,BattlePosition.Defense,GuardianStar.None,true,
            out var failResult,out bool wasFusion,out _) &&
            !wasFusion && failResult.Id == "new" &&
            failedField.Player.Monster(0).FaceDown &&
            failedField.Player.Graveyard.Count == 1,
            "failed monster fusion replaces old field material without inventing a result");
        Check(failedField.Player.Hand.Count == 4 &&
            failedField.Player.Graveyard[0].Id == "old",
            "failed field combination preserves surviving hand card as on-field material");

        // A failed first field pair must continue with the next hand card,
        // allowing a later successful recipe (rather than pre-fusing hand).
        var restarted = new FusionCatalog();
        restarted.Add("handA","handB",M("afterFailure",2100,1800));
        var restartMatch = new DuelMatch(
            Deck(M("fieldOld",1600,1200),M("handA",600,500),M("handB",700,600)),
            Deck(M("op",900,800)), restarted);
        Play(restartMatch,DuelSide.Player);
        End(restartMatch,DuelSide.Player);
        Play(restartMatch,DuelSide.Opponent);
        End(restartMatch,DuelSide.Opponent);
        Check(restartMatch.PreviewFieldResult(DuelSide.Player,
            new[]{0,1},0)?.Id=="afterFailure",
            "failed field-first pair can later fuse with the next ordered material");
        Check(restartMatch.TryPlayOnField(DuelSide.Player,new[]{0,1},0,
            BattlePosition.Attack,GuardianStar.None,false,out var restartedResult,
            out bool restartedFusion,out _) && restartedFusion &&
            restartedResult.Id=="afterFailure" &&
            restartMatch.Player.Graveyard.Count==3,
            "field-first failure then success keeps all physical originals accounted for");

        // Five occupied monster slots must NOT block the required hand play:
        // replacement is a legal alternative to a sixth zone.
        var fullBoard = Match(M("own",1400,1300),M("enemy",1100,950));
        for (int slot=0;slot<DuelMatch.ZoneCount;slot++)
        {
            Check(fullBoard.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Monster,slot,BattlePosition.Defense,GuardianStar.None,out _),
                "player fills one legal monster slot");
            End(fullBoard,DuelSide.Player);
            Check(fullBoard.TryPlay(DuelSide.Opponent,new[]{0},
                FieldLane.Monster,slot,BattlePosition.Defense,GuardianStar.None,out _),
                "opponent fills one legal monster slot");
            End(fullBoard,DuelSide.Opponent);
        }
        Check(fullBoard.Player.MonsterCount==5 &&
            !fullBoard.CanPlay(DuelSide.Player,new[]{0},
                FieldLane.Monster,0,GuardianStar.None,out _) &&
            !fullBoard.TryEndTurn(DuelSide.Player,out _),
            "full board still requires exactly one legal hand placement");
        Check(fullBoard.TryChooseFieldCombination(DuelSide.Player,false,
            out int fullHand,out int fullSlot,out var fullResult) &&
            fullBoard.TryPlayOnField(DuelSide.Player,new[]{fullHand},fullSlot,
                BattlePosition.Attack,fullResult.StarA,false,
                out _,out _,out _) &&
            fullBoard.Player.MonsterCount==5,
            "full field can replace an own monster without adding a sixth slot");
        End(fullBoard,DuelSide.Player);
        Check(fullBoard.CurrentSide==DuelSide.Opponent,
            "full-board replacement satisfies mandatory turn action");

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

        // PS1-specific ritual: three exact, face-up on-field monsters;
        // the ritual result does not have to be in the 40-card deck or hand.
        var ritualBook = new RitualCatalog();
        var summonedByRitual = M("ritual-result", 3200, 2500,
            GuardianStar.Mercury);
        ritualBook.Add("ritual-spell",
            new[] {"alpha", "beta", "gamma"}, summonedByRitual);
        var ritualGame = new DuelMatch(
            Deck(M("alpha", 700, 600), M("beta", 800, 700),
                M("gamma", 900, 800),
                new CardDefinition("ritual-spell", "Ritual", CardKind.Ritual,
                    0, 0, "Original 3 material invocation")),
            Deck(M("rival", 1000, 1000)), new FusionCatalog(), ritualBook);
        Check(!ritualGame.TryEndTurn(DuelSide.Player,out _),
            "PS1 ritual also respects mandatory hand play");
        for (int i = 0; i < 3; i++)
        {
            Check(ritualGame.TryPlay(DuelSide.Player,new[] {0},
                FieldLane.Monster,i,BattlePosition.Attack,
                GuardianStar.None,out _),
                "place ritual requirement in its own field slot");
            End(ritualGame,DuelSide.Player);
            Check(ritualGame.TryPlay(DuelSide.Opponent,new[] {0},
                FieldLane.Monster,i,BattlePosition.Defense,
                GuardianStar.None,out _),
                "opponent advances without skipping its one compulsory play");
            End(ritualGame,DuelSide.Opponent);
        }
        Check(ritualGame.Player.Hand[0].Id=="ritual-spell" &&
            ritualGame.PreviewRitual(DuelSide.Player,0)==summonedByRitual,
            "PS1 ritual preview requires three named face-up field monsters");
        int eventsBeforeRitual = ritualGame.Events.Count;
        Check(!ritualGame.CanSummonRitual(DuelSide.Player,0,4,
            GuardianStar.Mercury,out _,out _) &&
            !ritualGame.CanSummonRitual(DuelSide.Player,0,0,
                GuardianStar.None,out _,out _),
            "ritual rejects a non-tribute zone and invalid guardian star");
        Check(ritualGame.CanSummonRitual(DuelSide.Player,0,0,
            GuardianStar.Mercury,out var ritualPreview,out _) &&
            ritualPreview.Id=="ritual-result" &&
            ritualGame.Events.Count==eventsBeforeRitual,
            "ritual preview is fully read-only");
        Check(ritualGame.TrySummonRitual(DuelSide.Player,0,0,
            BattlePosition.Attack,GuardianStar.Mercury,
            out var ritualOutcome,out _) &&
            ritualOutcome.Id=="ritual-result" &&
            ritualGame.Player.Monster(0).Card.Id=="ritual-result" &&
            ritualGame.Player.Monster(1)==null &&
            ritualGame.Player.Monster(2)==null &&
            ritualGame.Player.Graveyard.Count==4 &&
            ritualGame.PlayedThisTurn,
            "ritual consumes exactly 3 field tributes plus ritual card and places result");
        Check(!ritualGame.TrySummonRitual(DuelSide.Player,0,0,
            BattlePosition.Attack,GuardianStar.Mercury,out _,out _),
            "ritual respects single hand placement limit per turn");

        // Retail PS1 permits triple-copy rituals: three equal card IDs must
        // still be three DIFFERENT physical monsters on the duel field.
        var triples = new RitualCatalog();
        triples.Add("triple-ritual",new[]{"blue","blue","blue"},
            M("ultimate",4500,3800));
        var tripleDuel = new DuelMatch(
            Deck(M("blue",3000,2500),M("blue",3000,2500),
                M("blue",3000,2500),
                new CardDefinition("triple-ritual","Triple",
                    CardKind.Ritual,0,0,"")),
            Deck(M("enemy",500,500)),new FusionCatalog(),triples);
        for (int tributeSlot=0;tributeSlot<3;tributeSlot++)
        {
            Check(tripleDuel.TryPlay(DuelSide.Player,new[]{0},
                FieldLane.Monster,tributeSlot,BattlePosition.Attack,
                GuardianStar.None,out _),
                "triple-copy ritual places one separate physical monster");
            End(tripleDuel,DuelSide.Player);
            Check(tripleDuel.TryPlay(DuelSide.Opponent,new[]{0},
                FieldLane.Monster,tributeSlot,BattlePosition.Defense,
                GuardianStar.None,out _),
                "triple-copy ritual setup preserves opponent compulsory play");
            End(tripleDuel,DuelSide.Opponent);
        }
        Check(tripleDuel.TrySummonRitual(DuelSide.Player,0,2,
                BattlePosition.Attack,GuardianStar.None,
                out var tripleResult,out _) &&
              tripleResult.Id=="ultimate" &&
              tripleDuel.Player.Monster(0)==null &&
              tripleDuel.Player.Monster(1)==null &&
              tripleDuel.Player.Monster(2).Card.Id=="ultimate" &&
              tripleDuel.Player.Graveyard.Count==4,
              "triple-copy PS1 ritual accepts duplicate IDs in separate field slots");

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
            fluid.Player.Monster(0).FaceDown &&
            !fluid.TryChangePosition(DuelSide.Player, 0,
                BattlePosition.Attack, out _) &&
            fluid.TryChangePosition(DuelSide.Player, 0,
            BattlePosition.Defense, out _) &&
            fluid.Player.Monster(0).FaceDown,
            "new facedown monster can toggle ATK/DEF repeatedly without revealing");
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
            fluid.Player.Monster(0).FaceDown,
            "set monster remains hidden when switched to ATK on a later turn");
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
            fluid.Player.Monster(0).AttackedThisTurn &&
            !fluid.Player.Monster(0).FaceDown,
            "attack itself reveals the previously hidden attacker");
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
            opponentFlip.Opponent.Monster(0).FaceDown,
            "AI switches previously set DEF into ATK while remaining hidden");
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
