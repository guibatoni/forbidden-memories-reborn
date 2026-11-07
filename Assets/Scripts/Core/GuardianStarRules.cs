using System;

namespace Reborn.Core
{
    /// <summary>Official two-cycle comparison, +500 only for the winning star.
    /// A missing/unverified star (None) deliberately grants no advantage.</summary>
    public static class GuardianStarRules
    {
        public const int AdvantagePoints = 500;

        public static bool HasAdvantage(GuardianStar challenger, GuardianStar defender)
        {
            switch (challenger)
            {
                case GuardianStar.Mercury: return defender == GuardianStar.Sun;
                case GuardianStar.Sun: return defender == GuardianStar.Moon;
                case GuardianStar.Moon: return defender == GuardianStar.Venus;
                case GuardianStar.Venus: return defender == GuardianStar.Mercury;
                case GuardianStar.Mars: return defender == GuardianStar.Jupiter;
                case GuardianStar.Jupiter: return defender == GuardianStar.Saturn;
                case GuardianStar.Saturn: return defender == GuardianStar.Uranus;
                case GuardianStar.Uranus: return defender == GuardianStar.Pluto;
                case GuardianStar.Pluto: return defender == GuardianStar.Neptune;
                case GuardianStar.Neptune: return defender == GuardianStar.Mars;
                default: return false;
            }
        }

        public static int Bonus(GuardianStar mine, GuardianStar other)
        {
            return HasAdvantage(mine, other) ? AdvantagePoints : 0;
        }

        public static bool IsAvailable(CardDefinition card, GuardianStar star)
        {
            if (card == null || card.Kind != CardKind.Monster) return false;
            if (star == GuardianStar.None)
                return card.StarA == GuardianStar.None && card.StarB == GuardianStar.None;
            return star == card.StarA || star == card.StarB;
        }
    }
}
