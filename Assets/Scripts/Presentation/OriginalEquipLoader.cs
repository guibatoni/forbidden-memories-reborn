using System;
using System.Collections.Generic;
using System.IO;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Optional same-disc user-provided compatibility manifest. Allows adding
    /// individually verified PS1 equip targets without bundling proprietary
    /// disc data or guessing from monster category names.
    /// </summary>
    public static class OriginalEquipLoader
    {
        [Serializable] private sealed class RulesFile
        {
            public int schema;
            public SourceData source;
            public EquipData[] equips;
        }
        [Serializable] private sealed class SourceData
        {
            public string discSha256;
            public string edition;
        }
        [Serializable] private sealed class EquipData
        {
            public int cardId;
            public bool verified;
            public int attackBonus;
            public int defenseBonus;
            public bool universal;
            public int[] targets;
        }

        public static string ExpectedPath => Path.Combine(
            Application.streamingAssetsPath, "RebornContent", "equip_rules.json");

        public static VerifiedEquipCatalog Load(GameContentBundle content,
            out string status)
        {
            if (content == null || !content.Edition.StartsWith("original-ps1",
                StringComparison.OrdinalIgnoreCase))
            {
                status = "Equips originais inativos em sandbox/mod nao confirmado.";
                return new VerifiedEquipCatalog();
            }

            // Exactly two broadly compatible original equips are known in
            // the baseline. A local manifest can extend the rest by card ID.
            var baseline = VerifiedEquipCatalog.OriginalPs1Foundation();
            if (!File.Exists(ExpectedPath))
            {
                status = "Equips PS1: Megamorph (#657) e Bright Castle (#668). " +
                    "Os demais aguardam tabela de compatibilidade validada.";
                return baseline;
            }
            try
            {
                var parsed = JsonUtility.FromJson<RulesFile>(
                    File.ReadAllText(ExpectedPath));
                if (parsed == null || parsed.schema != 1 ||
                    parsed.source == null || parsed.equips == null ||
                    string.IsNullOrEmpty(content.SourceSha256) ||
                    !string.Equals(parsed.source.discSha256,
                        content.SourceSha256, StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(parsed.source.edition) ||
                    !parsed.source.edition.StartsWith("original-ps1",
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        "Equip rules sem origem Original PS1 ou SHA-256 correspondente");

                var byId = new Dictionary<int, CardDefinition>();
                foreach (var card in content.Cards)
                {
                    if (int.TryParse(card.Id, out int cardId))
                        byId[cardId] = card;
                }
                var loaded = new VerifiedEquipCatalog();
                loaded.AddUniversal("657", 1000, 1000);
                loaded.AddUniversal("668", 500, 500);
                var seen = new HashSet<int>();
                foreach (var equip in parsed.equips)
                {
                    if (equip == null || !equip.verified ||
                        equip.cardId < 1 || equip.cardId > 722 ||
                        !seen.Add(equip.cardId) ||
                        equip.cardId == 657 || equip.cardId == 668 ||
                        !byId.TryGetValue(equip.cardId, out var equipCard) ||
                        equipCard.Kind != CardKind.Equip)
                        throw new InvalidDataException(
                            "Equip ID invalido, duplicado ou sem verificacao");
                    var number = equip.cardId.ToString("000");
                    if (equip.universal)
                    {
                        if (equip.targets != null && equip.targets.Length != 0)
                            throw new InvalidDataException(
                                "Equip universal com alvos explicitos");
                        loaded.AddUniversal(number, equip.attackBonus,
                            equip.defenseBonus);
                        continue;
                    }
                    if (equip.targets == null || equip.targets.Length == 0)
                        throw new InvalidDataException("Equip sem alvos verificados");
                    var targets = new List<string>();
                    var seenTargets = new HashSet<int>();
                    foreach (int monsterId in equip.targets)
                    {
                        if (monsterId < 1 || monsterId > 722 ||
                            !seenTargets.Add(monsterId) ||
                            !byId.TryGetValue(monsterId, out var monster) ||
                            monster.Kind != CardKind.Monster)
                            throw new InvalidDataException(
                                "Equip com alvo inexistente ou nao-monstro");
                        targets.Add(monsterId.ToString("000"));
                    }
                    loaded.AddTargets(number, equip.attackBonus,
                        equip.defenseBonus, targets);
                }
                status = "Compatibilidades Equip estruturais importadas: " +
                    loaded.Count + " cartas (mesma ISO/SHA; testes individuais pendentes).";
                return loaded;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("REBORN: equip manifest recusado. " + ex.Message);
                status = "Equip manifest invalido: " + ex.Message +
                    ". Apenas Megamorph/Bright Castle permanecem ativos.";
                return baseline;
            }
        }
    }
}
