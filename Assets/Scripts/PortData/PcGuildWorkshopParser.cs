// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/tong/tong_workshop.txt guild workshop (công trình) parser
// Source: tong_workshop.txt (GB2312, tab-separated).
//   Level \t WorkshopType \t UpgradeCost \t MaintenanceCost \t MaxMembers \t EffectId
//   WorkshopType: 0=kho (store), 1=đại sảnh (hall), 2=luyện đồ (forge),
//                 3=phòng chữa (clinic), 4=chuồng ngựa (stable).
// Mobile runtime exposes level/type index for GuildWorkshopService lookups.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGuildWorkshopParser
    {
        public const string MainFile = "tong_workshop.txt";

        public static List<PcGuildWorkshopEntry> ParseFile(string path)
        {
            var rows = new List<PcGuildWorkshopEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcGuildWorkshopEntry
                {
                    level = PcItemCommon.Int(cols, 0),
                    workshopType = PcItemCommon.Int(cols, 1),
                    upgradeCost = cols.Length > 2 ? PcItemCommon.Int(cols, 2) : 0,
                    maintenanceCost = cols.Length > 3 ? PcItemCommon.Int(cols, 3) : 0,
                    maxMembers = cols.Length > 4 ? PcItemCommon.Int(cols, 4) : 0,
                    effectId = cols.Length > 5 ? PcItemCommon.Int(cols, 5) : 0,
                });
            }
            return rows;
        }

        public static PcGuildWorkshopRegistry BuildRegistry(string dir)
        {
            var reg = new PcGuildWorkshopRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, MainFile);
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcGuildWorkshopEntry
    {
        public int level;
        public int workshopType;        // 0..4
        public int upgradeCost;         // Bạc cần để nâng cấp
        public int maintenanceCost;     // Phí duy trì hàng tuần
        public int maxMembers;          // Số thành viên tối đa
        public int effectId;            // ID hiệu ứng khi kích hoạt
    }

    public sealed class PcGuildWorkshopRegistry
    {
        // Keyed by (level, type). PC: same level can have multiple workshop types.
        private readonly Dictionary<(int level, int type), PcGuildWorkshopEntry> _byKey = new();
        private readonly List<PcGuildWorkshopEntry> _all = new();
        public int Count => _all.Count;
        public IEnumerable<PcGuildWorkshopEntry> All => _all;

        public void Register(PcGuildWorkshopEntry e)
        {
            if (e == null || e.level <= 0) return;
            _all.Add(e);
            _byKey[(e.level, e.workshopType)] = e;
        }

        public PcGuildWorkshopEntry Get(int level)
        {
            // PC: level 1 may map to multiple types. Return first match.
            foreach (var e in _all)
                if (e.level == level) return e;
            return null;
        }

        public PcGuildWorkshopEntry Get(int level, int type)
            => _byKey.TryGetValue((level, type), out var v) ? v : null;

        public IReadOnlyList<PcGuildWorkshopEntry> GetByType(int type)
        {
            var list = new List<PcGuildWorkshopEntry>();
            foreach (var e in _all)
                if (e.workshopType == type) list.Add(e);
            return list;
        }
    }
}
