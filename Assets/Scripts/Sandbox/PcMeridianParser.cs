// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/meridian/* Kinh mạch parser
// Source: settings/meridian/meridian_level.txt — 128 acupoints (穴位) =
//   8 meridians (所属经脉ID 1-8) × 16 levels each (穴位ID「同时也是等级」 1-16).
//   Cols: 穴位名称 所属经脉ID 穴位ID(=等级) 失败回退到几级 成功概率 说明  + (5 more)
//
// IMPORTANT: 穴位ID (col2) is NOT globally unique — it is the per-meridian tier
// (1-16) and is reused across all 8 meridians. The registry therefore keys on the
// COMPOSITE (meridianId, level) so all 128 acupoints are preserved. Keying on the
// level alone collapses the table to 16 rows (last-writer-wins). PC breakthrough
// script (tbEhanceRateWay) likewise addresses acupoints by (nMeridianIndex,
// nNewLevel) as two separate dimensions.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMeridianParser
    {
        public const int NameCol = 0;
        public const int MeridianIdCol = 1;
        public const int AcupointIdCol = 2; // 穴位ID（同时也是等级）= per-meridian tier 1-16
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
        /// <summary>穴位ID — per-meridian tier/level (1-16). Also the player level required to cultivate it.</summary>
        public int acupointId;
        /// <summary>所属经脉ID — meridian this acupoint belongs to (1-8).</summary>
        public int meridianId;
        public string nameRaw;
        public int fallbackLevel;
        public int successRate;
        public string description;
    }

    public sealed class PcMeridianRegistry
    {
        // Composite key (meridianId, level) — the per-meridian tier is reused across
        // all 8 meridians, so a single-int key would collapse 128 rows to 16.
        private readonly Dictionary<(int meridianId, int level), PcMeridianEntry> _byKey = new();
        private readonly Dictionary<int, List<PcMeridianEntry>> _byMeridian = new();
        private readonly List<int> _meridianOrder = new(); // insertion order, de-duped

        public int Count => _byKey.Count;

        public void Register(PcMeridianEntry e)
        {
            if (e == null || e.meridianId <= 0 || e.acupointId <= 0) return;
            _byKey[(e.meridianId, e.acupointId)] = e;
            if (!_byMeridian.TryGetValue(e.meridianId, out var list))
            {
                list = new List<PcMeridianEntry>();
                _byMeridian[e.meridianId] = list;
                _meridianOrder.Add(e.meridianId);
            }
            list.Add(e);
        }

        /// <summary>Look up an acupoint by its composite (meridian, level) identity.</summary>
        public PcMeridianEntry GetAcupoint(int meridianId, int level)
            => _byKey.TryGetValue((meridianId, level), out var v) ? v : null;

        public bool Contains(int meridianId, int level)
            => _byKey.ContainsKey((meridianId, level));

        public IReadOnlyList<PcMeridianEntry> GetMeridianPoints(int meridianId)
            => _byMeridian.TryGetValue(meridianId, out var v) ? v : (IReadOnlyList<PcMeridianEntry>)System.Array.Empty<PcMeridianEntry>();

        /// <summary>Distinct meridian IDs in first-seen order (1-8 for the shipped file).</summary>
        public IReadOnlyList<int> MeridianIds => _meridianOrder;

        /// <summary>Highest tier/level present for a given meridian (0 if the meridian is unknown).</summary>
        public int MaxLevelFor(int meridianId)
        {
            int m = 0;
            if (_byMeridian.TryGetValue(meridianId, out var list))
                foreach (var e in list) if (e.acupointId > m) m = e.acupointId;
            return m;
        }

        public IEnumerable<PcMeridianEntry> AllAcupoints() => _byKey.Values;
    }
}
