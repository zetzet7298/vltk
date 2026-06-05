// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/shitu.txt Sư Đồ (SHITU/APPRENTICE) parser
// Source: server settings/shitu.txt (6 entries, GB2312, tab-separated).
//   ShituId  MasterLevel  ApprenticeLevel  RequiredMasterFame  RewardItemId  RewardCount
// Vietnamese: "Sư Phụ", "Đồ Đệ", "Danh Vọng Sư Phụ", "Phần Thưởng Sư Đồ".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcShituParser
    {
        public const int ShituIdCol = 0;
        public const int MasterLevelCol = 1;
        public const int ApprenticeLevelCol = 2;
        public const int RequiredMasterFameCol = 3;
        public const int RewardItemIdCol = 4;
        public const int RewardCountCol = 5;

        public static List<PcShituEntry> ParseFile(string path)
        {
            var rows = new List<PcShituEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int id = PcItemCommon.Int(cols, ShituIdCol);
                if (id <= 0) continue;
                rows.Add(new PcShituEntry
                {
                    shituId = id,
                    masterLevel = PcItemCommon.Int(cols, MasterLevelCol),
                    apprenticeLevel = PcItemCommon.Int(cols, ApprenticeLevelCol),
                    requiredMasterFame = PcItemCommon.Int(cols, RequiredMasterFameCol),
                    rewardItemId = cols.Length > RewardItemIdCol ? PcItemCommon.Int(cols, RewardItemIdCol) : 0,
                    rewardCount = cols.Length > RewardCountCol ? PcItemCommon.Int(cols, RewardCountCol) : 0,
                });
            }
            return rows;
        }

        public static PcShituRegistry BuildRegistry(string dir)
        {
            var reg = new PcShituRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "shitu.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcShituEntry
    {
        public int shituId;
        public int masterLevel;          // Cấp tối thiểu của Sư Phụ
        public int apprenticeLevel;      // Cấp tối đa của Đồ Đệ
        public int requiredMasterFame;   // Danh vọng sư phụ yêu cầu
        public int rewardItemId;
        public int rewardCount;
    }

    public sealed class PcShituRegistry
    {
        private readonly Dictionary<int, PcShituEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcShituEntry e)
        {
            if (e == null || e.shituId <= 0) return;
            _byId[e.shituId] = e;
        }
        public PcShituEntry Get(int shituId)
            => _byId.TryGetValue(shituId, out var v) ? v : null;
        public IEnumerable<PcShituEntry> All => _byId.Values;
    }
}
