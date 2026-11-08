using System;
using System.Collections.Generic;
using System.IO;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>Reads private, same-disc opponent deck pools for experimental duels.</summary>
    public static class OpponentDeckLoader
    {
        public static string ExpectedPath =>
            Path.Combine(Application.streamingAssetsPath, "RebornContent", "opponent_decks.json");

        [Serializable] private sealed class ExportFile
        {
            public int schema;
            public SourceData source;
            public PoolData[] deckPools;
        }
        [Serializable] private sealed class SourceData
        {
            public string edition;
            public string discSha256;
        }
        [Serializable] private sealed class PoolData
        {
            public int id;
            public string name;
            public string deckKey;
            public int[] weights;
        }

        public static bool TryLoad(GameContentBundle cards,
            out OpponentDeckCatalog catalog, out string status)
        {
            catalog = null;
            if (cards == null || !File.Exists(ExpectedPath))
            {
                status = "Pools privados ausentes; adversario demonstrativo.";
                return false;
            }
            try
            {
                var file = JsonUtility.FromJson<ExportFile>(File.ReadAllText(ExpectedPath));
                if (file == null || file.schema != 1 || file.source == null ||
                    file.deckPools == null || file.deckPools.Length == 0)
                    throw new InvalidDataException("Schema de pools ausente ou invalido");
                if (string.IsNullOrWhiteSpace(cards.SourceSha256) ||
                    !string.Equals(file.source.discSha256, cards.SourceSha256,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(file.source.edition, cards.Edition, StringComparison.Ordinal))
                    throw new InvalidDataException("Os pools nao pertencem ao mesmo disco das cartas");
                var byId = new Dictionary<int, OpponentDefinition>();
                foreach (var opponent in cards.Opponents) byId.Add(opponent.Id, opponent);
                var pools = new List<WeightedOpponentPool>();
                foreach (var entry in file.deckPools)
                {
                    if (!byId.TryGetValue(entry.id, out var opponent) ||
                        !string.Equals(entry.deckKey, opponent.DeckKey, StringComparison.Ordinal))
                        throw new InvalidDataException("Pool divergente do oponente ID " + entry.id);
                    var pool = new WeightedOpponentPool(entry.id, entry.name, entry.deckKey, entry.weights);
                    pool.Deal(cards.Cards, new Random(1)); // fail early on missing card IDs
                    pools.Add(pool);
                }
                catalog = new OpponentDeckCatalog(cards.SourceSha256, pools);
                status = pools.Count + " pools importados (distribuicao experimental).";
                return true;
            }
            catch (Exception ex)
            {
                status = "Pools rejeitados: " + ex.Message;
                Debug.LogWarning("REBORN: " + status);
                return false;
            }
        }
    }
}
