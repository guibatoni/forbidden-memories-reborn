using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>Dados externos: codigo do jogo nao incorpora arte, dialogos ou bytes de ISO.</summary>
    public sealed class GameContentBundle
    {
        public string Edition { get; }
        public string Language { get; }
        public string SourceSha256 { get; }
        public IReadOnlyList<CardDefinition> Cards { get; }
        public IReadOnlyList<OpponentDefinition> Opponents { get; }
        public CampaignScript Campaign { get; }
        public FusionCatalog Fusions { get; }
        public int MissingDescriptions { get; }

        public GameContentBundle(string edition, string language, string sourceSha256,
            IReadOnlyList<CardDefinition> cards, IReadOnlyList<OpponentDefinition> opponents,
            CampaignScript campaign, FusionCatalog fusions = null)
        {
            if (cards == null || cards.Count == 0) throw new ArgumentException("Catalogo de cartas vazio");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var missing = 0;
            foreach (var card in cards)
            {
                if (card == null || !ids.Add(card.Id)) throw new ArgumentException("Carta ausente ou ID duplicado");
                if (string.IsNullOrWhiteSpace(card.Description)) missing++;
            }
            var opponentsIds = new HashSet<int>();
            if (opponents != null) foreach (var opponent in opponents)
                if (opponent == null || !opponentsIds.Add(opponent.Id))
                    throw new ArgumentException("Oponente ausente ou ID duplicado");
            Edition = edition ?? ""; Language = language ?? ""; SourceSha256 = sourceSha256 ?? "";
            Cards = cards; Opponents = opponents ?? new OpponentDefinition[0];
            Campaign = campaign; Fusions = fusions ?? new FusionCatalog();
            MissingDescriptions = missing;
        }
    }
}
