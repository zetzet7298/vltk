// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/meridian/* Kinh mạch parser
// Source: meridian_level.txt — 128 acupoints (穴位) across 12+ meridians.
//   Cols: 穴位名称 所属经脉ID 穴位ID 失败回退到几级 成功概率 说明  + (5 more)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMeridianParser
    {
        public const int NameCol = 0;
        public const int MeridianIdCol = 1;
        public const int AcupointIdCol = 2;
        public const int FailFallbackCol = 3;
        public const int SuccessRateCol = 4;
        public const int DescriptionCol = 5;

        public static List<PcMeridianEntry> ParseFile(string path)
        {
            var rows = new List<PcMeridianEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcMeridianEntry
                {
                    acupointId = PcItemCommon.Int(cols, AcupointIdCol),
                    meridianId = PcItemCommon.Int(cols, MeridianIdCol),
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    fallbackLevel = PcItemCommon.Int(cols, FailFallbackCol),
                    successRate = PcItemCommon.Int(cols, SuccessRateCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcMeridianRegistry BuildRegistry(string dir)
        {
            var reg = new PcMeridianRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "meridian_level.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcMeridianEntry
    {
        public int acupointId;
        public int meridianId;
        public string nameRaw;
        public int fallbackLevel;
        public int successRate;
        public string description;
    }

    public sealed class PcMeridianRegistry
    {
        private readonly Dictionary<int, PcMeridianEntry> _byAcupoint = new();
        private readonly Dictionary<int, List<PcMeridianEntry>> _byMeridian = new();
        public int Count => _byAcupoint.Count;

        public void Register(PcMeridianEntry e)
        {
            if (e == null || e.acupointId <= 0) return;
            _byAcupoint[e.acupointId] = e;
            if (!_byMeridian.TryGetValue(e.meridianId, out var list))
            {
                list = new List<PcMeridianEntry>();
                _byMeridian[e.meridianId] = list;
            }
            list.Add(e);
        }

        public PcMeridianEntry GetAcupoint(int id) => _byAcupoint.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcMeridianEntry> GetMeridianPoints(int meridianId)
            => _byMeridian.TryGetValue(meridianId, out var v) ? v : (IReadOnlyList<PcMeridianEntry>)System.Array.Empty<PcMeridianEntry>();
        public int MaxAcupointId => _byAcupoint.Count == 0 ? 0 : MaxKey();
        private int MaxKey() { int m = 0; foreach (var k in _byAcupoint.Keys) if (k > m) m = k; return m; }
    }
}
