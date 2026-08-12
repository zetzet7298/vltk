// -----------------------------------------------------------------------------
// VLTK Mobile — PC battle honor parser (Battle Honor - điểm vinh danh)
// Source: battlehonor.txt (Reference/PcBattlefield).
// Columns: HonorId  BattleType  Name  RequiredScore  BonusTitle  BonusEffect
// Vietnamese: "Vinh Danh", "Điểm Yêu Cầu", "Danh Hiệu Thưởng", "Hiệu Ứng".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcBattleHonorParser
    {
        public const int HonorIdCol = 0;
        public const int BattleTypeCol = 1;
        public const int NameCol = 2;
        public const int RequiredScoreCol = 3;
        public const int BonusTitleCol = 4;
        public const int BonusEffectCol = 5;

        public static List<PcBattleHonorEntry> ParseFile(string path)
        {
            var rows = new List<PcBattleHonorEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 1) continue;
                int id = PcItemCommon.Int(cols, HonorIdCol);
                if (id <= 0) continue;
                rows.Add(new PcBattleHonorEntry
                {
                    honorId = id,
                    battleType = PcItemCommon.Int(cols, BattleTypeCol),
                    name = PcItemCommon.Str(cols, NameCol),
                    requiredScore = PcItemCommon.Int(cols, RequiredScoreCol),
                    bonusTitle = PcItemCommon.Str(cols, BonusTitleCol),
                    bonusEffect = PcItemCommon.Str(cols, BonusEffectCol),
                });
            }
            return rows;
        }

        public static PcBattleHonorRegistry BuildRegistry(string dir)
        {
            var reg = new PcBattleHonorRegistry();
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
    public class PcBattleHonorEntry
    {
        public int honorId;
        public int battleType;
        public string name;
        public int requiredScore;
        public string bonusTitle;
        public string bonusEffect;
    }

    public sealed class PcBattleHonorRegistry
    {
        private readonly Dictionary<int, PcBattleHonorEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcBattleHonorEntry e)
        {
            if (e == null || e.honorId <= 0) return;
            _byId[e.honorId] = e;
        }

        public PcBattleHonorEntry Get(int honorId)
            => _byId.TryGetValue(honorId, out var v) ? v : null;

        public IReadOnlyList<PcBattleHonorEntry> GetByBattleType(int battleType)
        {
            var list = new List<PcBattleHonorEntry>();
            foreach (var e in _byId.Values)
                if (e.battleType == battleType) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcBattleHonorEntry> All => new List<PcBattleHonorEntry>(_byId.Values);
    }
}
