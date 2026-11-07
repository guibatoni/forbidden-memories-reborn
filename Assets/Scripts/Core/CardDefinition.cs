using System;

namespace Reborn.Core
{
    public enum CardKind { Monster, Spell, Trap, Equip, Ritual }
    public enum FieldLane { Monster, Support }

    /// <summary>Dados imutáveis. Nada aqui depende de Unity ou de arquivos ISO.</summary>
    public sealed class CardDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public CardKind Kind { get; }
        public int Attack { get; }
        public int Defense { get; }
        public string Description { get; }

        public CardDefinition(string id, string name, CardKind kind,
            int attack, int defense, string description)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID obrigatório", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome obrigatório", nameof(name));
            if (attack < 0 || defense < 0) throw new ArgumentOutOfRangeException(nameof(attack));
            Id = id; Name = name; Kind = kind;
            Attack = attack; Defense = defense; Description = description ?? "";
        }
    }
}
