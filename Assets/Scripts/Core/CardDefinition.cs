using System;

namespace Reborn.Core
{
    public enum CardKind { Monster, Spell, Trap, Equip, Ritual }
    public enum FieldLane { Monster, Support }

    // The classic alignment chart contains two independent cycles. None means
    // the imported source has not supplied validated Guardian Star data yet.
    public enum GuardianStar
    {
        None, Mercury, Sun, Moon, Venus, Mars, Jupiter, Saturn, Uranus, Pluto, Neptune
    }
    public enum BattlePosition { Attack, Defense }
    public enum DuelSide { Player, Opponent }
    public enum MatchPhase { Main, Battle, Finished }

    /// <summary>Dados imutáveis. Nada aqui depende de Unity ou de arquivos ISO.</summary>
    public sealed class CardDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public CardKind Kind { get; }
        public int Attack { get; }
        public int Defense { get; }
        public string Description { get; }
        public string EffectText { get; }
        public string EffectSource { get; }
        public string EffectStatus { get; }
        public bool HasEffectText => !string.IsNullOrWhiteSpace(EffectText);
        public GuardianStar StarA { get; }
        public GuardianStar StarB { get; }

        public CardDefinition(string id, string name, CardKind kind,
            int attack, int defense, string description,
            GuardianStar starA = GuardianStar.None, GuardianStar starB = GuardianStar.None,
            string effectText = "", string effectSource = "", string effectStatus = "")
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID obrigatório", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome obrigatório", nameof(name));
            if (attack < 0 || defense < 0) throw new ArgumentOutOfRangeException(nameof(attack));
            Id = id; Name = name; Kind = kind;
            Attack = attack; Defense = defense; Description = description ?? "";
            StarA = starA; StarB = starB;
            // DUELTRACE effect text is display-only metadata, NOT an executed rule.
            EffectText = effectText ?? "";
            EffectSource = effectSource ?? "";
            EffectStatus = effectStatus ?? "";
        }
    }
}
