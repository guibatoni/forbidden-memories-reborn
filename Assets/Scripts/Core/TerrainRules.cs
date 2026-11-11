using System;

namespace Reborn.Core
{
    /// <summary>
    /// Single global PS1 field. Only the six retail IDs have effects;
    /// monster typing comes from the validated imported PS1 card catalog.
    /// No name-based or free-text effect parsing.
    /// </summary>
    public static class TerrainRules
    {
        public static string Name(string cardId)
        {
            switch (cardId)
            {
                case "330": return "Forest";
                case "331": return "Wasteland";
                case "332": return "Mountain";
                case "333": return "Sogen";
                case "334": return "Umi";
                case "335": return "Yami";
                default: return "Normal";
            }
        }
        public static bool IsKnown(string cardId) =>
            cardId == "330" || cardId == "331" || cardId == "332" ||
            cardId == "333" || cardId == "334" || cardId == "335";

        private static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            return raw.Trim().ToLowerInvariant()
                .Replace("-", "").Replace(" ", "").Replace("_", "");
        }

        public static int Bonus(string terrainCardId, CardDefinition monster)
        {
            if (monster == null || monster.Kind != CardKind.Monster ||
                !IsKnown(terrainCardId)) return 0;
            string type = Normalize(monster.TypeLabel);
            if (type.Length == 0) return 0;
            switch (terrainCardId)
            {
                case "330": // Forest
                    return type == "beastwarrior" || type == "beast" ||
                           type == "insect" || type == "plant" ? 500 : 0;
                case "331": // Wasteland
                    return type == "zombie" || type == "dinosaur" ||
                           type == "rock" ? 500 : 0;
                case "332": // Mountain
                    return type == "dragon" || type == "wingedbeast" ||
                           type == "thunder" ? 500 : 0;
                case "333": // Sogen
                    return type == "warrior" || type == "beastwarrior" ? 500 : 0;
                case "334": // Umi
                    if (type == "machine" || type == "pyro") return -500;
                    return type == "aqua" || type == "fish" ||
                           type == "seaserpent" || type == "thunder" ? 500 : 0;
                case "335": // Yami
                    if (type == "fairy") return -500;
                    return type == "fiend" || type == "spellcaster" ? 500 : 0;
                default:
                    return 0;
            }
        }
    }
}
