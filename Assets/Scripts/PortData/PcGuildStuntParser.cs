// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/tong/tongstunt_setting.txt Bang (GUILD) stunt parser
// Source: server settings/tong/tongstunt_setting.txt (GB2312, tab-separated).
//   StuntID  StuntName  MaxMemberCnt  MaxStuntCntPer  Cycle  Consume  RightLimit  SkillID  Describe
// PC quirk: this file is 1-based for SkillID, with the first row being the
// Stunt header (column 0 = "StuntID"). We keep the parser tolerant of extra
// columns and only rely on the first 5 numeric columns for runtime queries.
// Vietnamese: "Kỹ Năng Bang", "Phượng Hoàng Ấn", "Bảo Trì Hàng Tuần".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGuildStuntParser
    {
        public const int StuntIdCol = 0;
        public const int StuntNameCol = 1;
        public const int MaxMemberCntCol = 2;
        public const int MaxStuntCntPerCol = 3;
        public const int CycleCol = 4;
        public const int ConsumeCol = 5;
        public const int RightLimitCol = 6;
        public const int SkillIdCol = 7;

        public static List<PcGuildStuntEntry> ParseFile(string path)
        {
            var rows = new List<PcGuildStuntEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcText.ReadLinesTcvn3(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, StuntIdCol);
                if (id <= 0) continue;
                rows.Add(new PcGuildStuntEntry
                {
                    stuntId = id,
                    stuntName = PcItemCommon.Str(cols, StuntNameCol),
                    maxMemberCount = cols.Length > MaxMemberCntCol ? PcItemCommon.Int(cols, MaxMemberCntCol) : 0,
                    maxStuntPerMember = cols.Length > MaxStuntCntPerCol ? PcItemCommon.Int(cols, MaxStuntCntPerCol) : 0,
                    cycleWeeks = cols.Length > CycleCol ? PcItemCommon.Int(cols, CycleCol) : 0,
                    weeklyBudget = cols.Length > ConsumeCol ? PcItemCommon.Int(cols, ConsumeCol) : 0,
                    rightLimit = cols.Length > RightLimitCol ? PcItemCommon.Int(cols, RightLimitCol) : 0,
                    skillId = cols.Length > SkillIdCol ? PcItemCommon.Int(cols, SkillIdCol) : 0,
                });
            }
            return rows;
        }

        public static PcGuildStuntRegistry BuildRegistry(string dir)
        {
            var reg = new PcGuildStuntRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "tongstunt_setting.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcGuildStuntEntry
    {
        public int stuntId;
        public string stuntName;
        public int maxMemberCount;       // Số người nhận tối đa
        public int maxStuntPerMember;    // Số lượng người nhận tối đa
        public int cycleWeeks;           // Bảo trì hàng tuần
        public int weeklyBudget;         // Ngân sách cần để bảo trì
        public int rightLimit;           // 1 = chỉ cấp lãnh đạo nhận
        public int skillId;              // Mã skill đặc biệt cắm biếu tượng
    }

    public sealed class PcGuildStuntRegistry
    {
        private readonly Dictionary<int, PcGuildStuntEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcGuildStuntEntry e)
        {
            if (e == null || e.stuntId <= 0) return;
            _byId[e.stuntId] = e;
        }
        public PcGuildStuntEntry Get(int stuntId)
            => _byId.TryGetValue(stuntId, out var v) ? v : null;

        /// <summary>
        /// Lấy danh sách stunt khả dụng theo cấp bang (cycleWeeks là số tuần giữa các lần).
        /// Runtime heuristic: cycleWeeks <= guildLevel → coi như đã mở khóa.
        /// </summary>
        public IReadOnlyList<PcGuildStuntEntry> GetForLevel(int guildLevel)
        {
            var result = new List<PcGuildStuntEntry>();
            foreach (var e in _byId.Values)
            {
                if (e == null) continue;
                if (e.cycleWeeks <= guildLevel) result.Add(e);
            }
            return result;
        }

        public IEnumerable<PcGuildStuntEntry> All => _byId.Values;
    }
}
