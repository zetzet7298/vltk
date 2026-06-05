// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel: Faction Bonus (Bonus Môn Phái)
// Bảng UI hiển thị bonus theo cấp cho từng môn phái (tăng máu, MP, công, thủ, tốc).
// Vietnamese: "Bonus Môn Phái", "Tăng Máu", "Tăng Nội Lực", "Tăng Công", "Tăng Thủ".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct FactionBonusPanelRow
    {
        public readonly int level;
        public readonly int hpBonus;
        public readonly int mpBonus;
        public readonly int atkBonus;
        public readonly int defBonus;
        public readonly int speedBonus;
        public readonly bool isCurrent;

        public FactionBonusPanelRow(int level, int hpBonus, int mpBonus, int atkBonus, int defBonus, int speedBonus, bool isCurrent)
        {
            this.level = level;
            this.hpBonus = hpBonus;
            this.mpBonus = mpBonus;
            this.atkBonus = atkBonus;
            this.defBonus = defBonus;
            this.speedBonus = speedBonus;
            this.isCurrent = isCurrent;
        }
    }

    public sealed class FactionBonusPanelSnapshot
    {
        public int playerId;
        public int factionId;
        public string factionName;
        public int playerLevel;
        public int totalBonus;
        public IReadOnlyList<FactionBonusPanelRow> rows;
    }

    public static class FactionBonusPanelService
    {
        public const string LabelFactionBonus = "Bonus Môn Phái";
        public const string LabelHp = "Tăng Máu";
        public const string LabelMp = "Tăng Nội Lực";
        public const string LabelAtk = "Tăng Công";
        public const string LabelDef = "Tăng Thủ";

        public static FactionBonusPanelSnapshot BuildSnapshot(FactionBonusService service, int playerId, int factionId)
        {
            return new FactionBonusPanelSnapshot { rows = System.Array.Empty<FactionBonusPanelRow>() };
        }

        public static IReadOnlyList<FactionBonusPanelRow> GetByFaction(FactionBonusService service, int factionId)
        {
            return System.Array.Empty<FactionBonusPanelRow>();
        }

        public static (int hp, int mp, int atk, int def, int speed) GetBonusAtLevel(FactionBonusService service, int factionId, int level)
        {
            return default;
        }

    }
}
