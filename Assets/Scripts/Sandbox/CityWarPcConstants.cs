// -----------------------------------------------------------------------------
// VLTK Mobile — PC CityWar constants proven from vl_update_27 Lua sources.
// Sources:
// - server1/script/missions/citywar_global/head.lua
// - server1/script/missions/citywar_global/infocenter.lua
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Sandbox
{
    public enum CityWarCardSide
    {
        None = 0,
        Defender = 1,
        Attacker = 2,
    }

    public readonly struct CityWarItemTuple : IEquatable<CityWarItemTuple>
    {
        public readonly int Genre;
        public readonly int Detail;
        public readonly int Particular;

        public CityWarItemTuple(int genre, int detail, int particular)
        {
            Genre = genre;
            Detail = detail;
            Particular = particular;
        }

        public bool Equals(CityWarItemTuple other)
        {
            return Genre == other.Genre && Detail == other.Detail && Particular == other.Particular;
        }

        public override bool Equals(object obj)
        {
            return obj is CityWarItemTuple other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Genre;
                hash = hash * 397 ^ Detail;
                hash = hash * 397 ^ Particular;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", Genre, Detail, Particular);
        }
    }

    public static class CityWarPcConstants
    {
        // citywar_global/head.lua:3-22
        public const int CardPrice = 200000;
        public const int ReturnCardPrice = 10000;

        // citywar_global/infocenter.lua:21-29
        public const int ChallengeTokenGenre = 6;
        public const int ChallengeTokenDetail = 1;
        public const int ChallengeTokenParticular = 1499;

        // citywar_global/infocenter.lua:277-344
        public const int TiaoZhanLingTaskDate = 1839;
        public const int TiaoZhanLingTaskCount = 1840;
        public const int TiaoZhanLingDailyCap = 300;
        public const int TiaoZhanLingExpReward = 5000;

        // citywar_global/head.lua:24-26
        public const int TiaoZhanLingLeagueType = 538;
        public const string TiaoZhanLingLeagueName = "tiaozhanling";
        public const int TiaoZhanLingLeagueTaskCount = 1;

        // citywar_global/infocenter.lua:21-24,605-627
        public const int CityWarSignLeagueType = 508;
        public const int CityWarFirstLeagueType = 509;
        public const int QingTongDingLeagueTaskCount = 1;
        public const int CityWarSignCountLeagueTask = 2;

        // PC card side mapping: attacker=CardTab[CityID*2-1], defender=CardTab[CityID*2].
        // Lua is 1-based; C# offsets are attacker cityId*2-2, defender cityId*2-1.
        public static readonly int[] CardTab = { 363, 362, 355, 354, 367, 366, 359, 358, 357, 356, 365, 364, 361, 360 };
        public static readonly CityWarItemTuple ChallengeTokenItem = new CityWarItemTuple(ChallengeTokenGenre, ChallengeTokenDetail, ChallengeTokenParticular);

        public static int GetCardItemIdForCity(int cityId, CityWarCardSide side)
        {
            if (cityId < 1 || cityId > 7)
                return 0;

            int offset;
            if (side == CityWarCardSide.Attacker)
                offset = cityId * 2 - 2;
            else if (side == CityWarCardSide.Defender)
                offset = cityId * 2 - 1;
            else
                return 0;

            return CardTab[offset];
        }

        public static CityWarCardSide GetCardSideForCity(int cityId, int cardItemId)
        {
            if (cardItemId == GetCardItemIdForCity(cityId, CityWarCardSide.Attacker))
                return CityWarCardSide.Attacker;
            if (cardItemId == GetCardItemIdForCity(cityId, CityWarCardSide.Defender))
                return CityWarCardSide.Defender;
            return CityWarCardSide.None;
        }
    }
}
