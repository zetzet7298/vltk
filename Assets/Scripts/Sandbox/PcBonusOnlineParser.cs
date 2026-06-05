// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/bonus_online.txt Bonus Online Time parser
// Thưởng online: cấp phát exp/silver/item sau RequiredMinutes online.
// Source: settings/bonus_onlinetime/bonus_online.txt (GB2312, 6 tab cols).
//   BonusId  RequiredMinutes  RewardType  RewardId  RewardCount  VipRequired
// RewardType: 0=exp, 1=silver, 2=item, 3=bond, 4=coin, 5=knighthood.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcBonusOnlineParser
    {
        public const int BonusIdCol = 0;
        public const int RequiredMinutesCol = 1;
        public const int RewardTypeCol = 2;
        public const int RewardIdCol = 3;
        public const int RewardCountCol = 4;
        public const int VipRequiredCol = 5;

        public static List<PcBonusOnlineEntry> ParseFile(string path)
        {
            var rows = new List<PcBonusOnlineEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]")) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcBonusOnlineEntry
                {
                    bonusId = PcItemCommon.Int(cols, BonusIdCol),
                    requiredMinutes = PcItemCommon.Int(cols, RequiredMinutesCol),
                    rewardType = PcItemCommon.Int(cols, RewardTypeCol),
                    rewardId = cols.Length > RewardIdCol ? PcItemCommon.Int(cols, RewardIdCol) : 0,
                    rewardCount = cols.Length > RewardCountCol ? PcItemCommon.Int(cols, RewardCountCol) : 0,
                    vipRequired = cols.Length > VipRequiredCol ? PcItemCommon.Int(cols, VipRequiredCol) : 0,
                });
            }
            return rows;
        }

        public static PcBonusOnlineRegistry BuildRegistry(string dir)
        {
            var reg = new PcBonusOnlineRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            foreach (var f in Directory.GetFiles(dir, "*.ini", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcBonusOnlineEntry
    {
        public int bonusId;
        public int requiredMinutes;
        public int rewardType;
        public int rewardId;
        public int rewardCount;
        public int vipRequired;
    }

    public sealed class PcBonusOnlineRegistry
    {
        private readonly Dictionary<int, PcBonusOnlineEntry> _byId = new();
        private readonly Dictionary<int, List<PcBonusOnlineEntry>> _byVip = new();
        private readonly List<PcBonusOnlineEntry> _all = new();
        public int Count => _byId.Count;
        public IEnumerable<PcBonusOnlineEntry> All => _all;

        public void Register(PcBonusOnlineEntry e)
        {
            if (e == null || e.bonusId <= 0) return;
            _byId[e.bonusId] = e;
            _all.Add(e);
            if (!_byVip.TryGetValue(e.vipRequired, out var list))
            {
                list = new List<PcBonusOnlineEntry>();
                _byVip[e.vipRequired] = list;
            }
            list.Add(e);
        }

        public PcBonusOnlineEntry Get(int bonusId)
            => _byId.TryGetValue(bonusId, out var v) ? v : null;

        /// <summary>
        /// Lấy danh sách bonus đủ điều kiện khi đã online đủ <paramref name="minutes"/>.
        /// PC: tìm entry có requiredMinutes &lt;= minutes, ưu tiên entry cao nhất.
        /// </summary>
        public IReadOnlyList<PcBonusOnlineEntry> GetForMinutes(int minutes)
        {
            var result = new List<PcBonusOnlineEntry>();
            foreach (var e in _all)
            {
                if (e != null && e.requiredMinutes <= minutes) result.Add(e);
            }
            return result;
        }

        public IReadOnlyList<PcBonusOnlineEntry> GetByVip(int vipLevel)
        {
            var result = new List<PcBonusOnlineEntry>();
            foreach (var e in _all)
            {
                if (e == null) continue;
                if (e.vipRequired > vipLevel) continue;
                result.Add(e);
            }
            return result;
        }
    }
}
