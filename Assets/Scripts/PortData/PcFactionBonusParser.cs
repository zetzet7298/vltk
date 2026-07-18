// -----------------------------------------------------------------------------
// VLTK Mobile — PC faction bonus parser (Faction Bonus - theo cấp)
// Source: faction_bonus.txt (Reference/PcFaction).
// Columns: FactionId  Level  HpBonus  MpBonus  AtkBonus  DefBonus  SpeedBonus
// Vietnamese: "Môn Phái", "Cấp", "Máu", "Nội Lực", "Công Kích", "Phòng Thủ".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFactionBonusParser
    {
        public const int FactionIdCol = 0;
        public const int LevelCol = 1;
        public const int HpBonusCol = 2;
        public const int MpBonusCol = 3;
        public const int AtkBonusCol = 4;
        public const int DefBonusCol = 5;
        public const int SpeedBonusCol = 6;

        public static List<PcFactionBonusEntry> ParseFile(string path)
        {
            var rows = new List<PcFactionBonusEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int factionId = PcItemCommon.Int(cols, FactionIdCol);
                int level = PcItemCommon.Int(cols, LevelCol);
                if (factionId < 0 || level <= 0) continue;
                rows.Add(new PcFactionBonusEntry
                {
                    factionId = factionId,
                    level = level,
                    hpBonus = PcItemCommon.Int(cols, HpBonusCol),
                    mpBonus = PcItemCommon.Int(cols, MpBonusCol),
                    atkBonus = PcItemCommon.Int(cols, AtkBonusCol),
                    defBonus = PcItemCommon.Int(cols, DefBonusCol),
                    speedBonus = PcItemCommon.Int(cols, SpeedBonusCol),
                });
            }
            return rows;
        }

        public static PcFactionBonusRegistry BuildRegistry(string dir)
        {
            var reg = new PcFactionBonusRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcFactionBonusEntry
    {
        public int factionId;
        public int level;
        public int hpBonus;
        public int mpBonus;
        public int atkBonus;
        public int defBonus;
        public int speedBonus;
    }

    public sealed class PcFactionBonusRegistry
    {
        private readonly Dictionary<long, PcFactionBonusEntry> _byKey = new();
        public int Count => _byKey.Count;

        private static long Key(int factionId, int level)
            => ((long)factionId << 32) | (uint)level;

        public void Register(PcFactionBonusEntry e)
        {
            if (e == null || e.level <= 0) return;
            _byKey[Key(e.factionId, e.level)] = e;
        }

        public PcFactionBonusEntry Get(int factionId, int level)
            => _byKey.TryGetValue(Key(factionId, level), out var v) ? v : null;

        public IReadOnlyList<PcFactionBonusEntry> GetByFaction(int factionId)
        {
            var list = new List<PcFactionBonusEntry>();
            foreach (var e in _byKey.Values)
                if (e.factionId == factionId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcFactionBonusEntry> All => new List<PcFactionBonusEntry>(_byKey.Values);
    }
}
