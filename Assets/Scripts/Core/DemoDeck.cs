using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>Conteúdo fictício e provisório, sem importar assets de Yu-Gi-Oh!</summary>
    public static class DemoDeck
    {
        /// <summary>Usa IDs importados em um deck de sandbox; NAO e o deck inicial oficial.</summary>
        public static IReadOnlyList<CardDefinition> FromCatalog(IReadOnlyList<CardDefinition> cards)
        {
            if (cards == null || cards.Count < 5) return Create();
            var deck = new List<CardDefinition>(40);
            var range = System.Math.Min(40, cards.Count);
            for (int i = 0; i < 40; i++) deck.Add(cards[i % range]);
            return deck;
        }

        public static IReadOnlyList<CardDefinition> Create()
        {
            var samples = new[]
            {
                new CardDefinition("p01", "Dragao Celeste", CardKind.Monster, 3000, 2500, "Invocacao de demonstracao."),
                new CardDefinition("p02", "Guardiao das Dunas", CardKind.Monster, 1800, 1600, "Guerreiro de pedra."),
                new CardDefinition("p03", "Cavaleiro Solar", CardKind.Monster, 1600, 1200, "Protege o templo."),
                new CardDefinition("p04", "Fera do Nilo", CardKind.Monster, 1400, 1800, "Criatura de rio."),
                new CardDefinition("p05", "Feitico de Luz", CardKind.Spell, 0, 0, "Carta de apoio de teste."),
                new CardDefinition("p06", "Escudo de Areia", CardKind.Trap, 0, 0, "Armadilha de teste."),
                new CardDefinition("p07", "Mago das Estrelas", CardKind.Monster, 2100, 1800, "Guardiao astral."),
                new CardDefinition("p08", "Lanca de Bronze", CardKind.Equip, 0, 0, "Equipamento ficticio de demonstracao.")
            };
            var deck = new List<CardDefinition>(40);
            for (var i = 0; i < 40; i++) deck.Add(samples[i % samples.Length]);
            return deck;
        }
    }
}
