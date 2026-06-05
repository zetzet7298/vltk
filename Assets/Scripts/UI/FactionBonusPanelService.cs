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
            var snapshot = new FactionBonusPanelSnapshot
            {
                playerId = playerId,
                factionId = factionId,
                factionName = FactionVietnameseCatalog.GetVietnameseName(factionId) ?? $"Môn phái #{factionId}",
                playerLevel = 1,
                totalBonus = 0,
                rows = Array.Empty<FactionBonusPanelRow>()
            };
            if (service == null) return snapshot;
            int level = service.GetPlayerLevel(playerId);
            snapshot.playerLevel = level;
            var all = service.GetByFaction(factionId);
            var rows = new List<FactionBonusPanelRow>();
            int total = 0;
            foreach (var entry in all)
            {
                if (entry == null) continue;
                int hp, mp, atk, def, speed;
                service.GetBonusAtLevel(factionId, entry.level, out hp, out mp, out atk, out def, out speed);
                bool isCurrent = entry.level == level;
                rows.Add(new FactionBonusPanelRow(entry.level, hp, mp, atk, def, speed, isCurrent));
                if (isCurrent) total = hp + mp + atk + def + speed;
            }
            snapshot.totalBonus = total;
            snapshot.rows = rows;
            return snapshot;
        }

        public static IReadOnlyList<FactionBonusPanelRow> GetByFaction(FactionBonusService service, int factionId)
        {
            if (service == null) return Array.Empty<FactionBonusPanelRow>();
            var rows = new List<FactionBonusPanelRow>();
            foreach (var entry in service.GetByFaction(factionId))
            {
                if (entry == null) continue;
                int hp, mp, atk, def, speed;
                service.GetBonusAtLevel(factionId, entry.level, out hp, out mp, out atk, out def, out speed);
                rows.Add(new FactionBonusPanelRow(entry.level, hp, mp, atk, def, speed, false));
            }
            return rows;
        }

        public static (int hp, int mp, int atk, int def, int speed) GetBonusAtLevel(FactionBonusService service, int factionId, int level)
        {
            int hp = 0, mp = 0, atk = 0, def = 0, speed = 0;
            if (service == null || factionId <= 0 || level <= 0) return (hp, mp, atk, def, speed);
            service.GetBonusAtLevel(factionId, level, out hp, out mp, out atk, out def, out speed);
            return (hp, mp, atk, def, speed);
        }
    }
}
