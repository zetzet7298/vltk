// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/honor.txt Vinh Danh (HONOR) parser
// Source: server settings/honor.txt (6 entries, GB2312, tab-separated).
//   HonorId  HonorName  RequiredPoints  TitleReward  AuraSkillId
// Vietnamese: "Vinh Danh", "Danh Hiệu", "Quang Huy", "Hào Quang".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcHonorParser
    {
        public const int HonorIdCol = 0;
        public const int HonorNameCol = 1;
        public const int RequiredPointsCol = 2;
        public const int TitleRewardCol = 3;
        public const int AuraSkillIdCol = 4;

        public static List<PcHonorEntry> ParseFile(string path)
        {
            var rows = new List<PcHonorEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, HonorIdCol);
                if (id <= 0) continue;
                rows.Add(new PcHonorEntry
                {
                    honorId = id,
                    honorName = PcItemCommon.Str(cols, HonorNameCol),
                    requiredPoints = PcItemCommon.Int(cols, RequiredPointsCol),
                    titleReward = cols.Length > TitleRewardCol ? PcItemCommon.Int(cols, TitleRewardCol) : 0,
                    auraSkillId = cols.Length > AuraSkillIdCol ? PcItemCommon.Int(cols, AuraSkillIdCol) : 0,
                });
            }
            return rows;
        }

        public static PcHonorRegistry BuildRegistry(string dir)
        {
            var reg = new PcHonorRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                // Fallback: PC honor.txt có thể nằm ở root hoặc PcAttrib. Thử cả hai.
                return reg;
            }
            string main = Path.Combine(dir, "honor.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }

        public static PcHonorRegistry BuildRegistryFromRoot()
        {
            // Tìm honor.txt ở các vị trí quen thuộc.
            string[] candidates =
            {
                "Reference/PcAttrib/honor.txt",
                "Reference/honor.txt",
            };
            var reg = new PcHonorRegistry();
            string root = UnityEngine.Application.streamingAssetsPath;
            foreach (var rel in candidates)
            {
                string full = Path.Combine(root, rel);
                if (File.Exists(full))
                    foreach (var s in ParseFile(full)) reg.Register(s);
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcHonorEntry
    {
        public int honorId;
        public string honorName;
        public int requiredPoints;
        public int titleReward;       // Mã danh hiệu nhận được
        public int auraSkillId;       // Mã hào quang kích hoạt
    }

    public sealed class PcHonorRegistry
    {
        private readonly Dictionary<int, PcHonorEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcHonorEntry e)
        {
            if (e == null || e.honorId <= 0) return;
            _byId[e.honorId] = e;
        }
        public PcHonorEntry Get(int honorId)
            => _byId.TryGetValue(honorId, out var v) ? v : null;

        /// <summary>Lấy vinh danh cao nhất mà người chơi đủ điểm.</summary>
        public PcHonorEntry GetByPoints(int points)
        {
            PcHonorEntry best = null;
            foreach (var e in _byId.Values)
            {
                if (e == null) continue;
                if (points >= e.requiredPoints)
                {
                    if (best == null || e.requiredPoints > best.requiredPoints) best = e;
                }
            }
            return best;
        }

        public IEnumerable<PcHonorEntry> All => _byId.Values;
    }
}
