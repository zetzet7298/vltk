// -----------------------------------------------------------------------------
// VLTK Mobile — PC titleeffect.txt title effect parser
// Source: server settings/titleeffect.txt (Reference/PcTitle).
// Cols: EffectId, TitleId, EffectType, EffectValue, IsPercent, RequiredTitleLevel
// Types: 0=hp, 1=mp, 2=atk, 3=def, 4=exp, 5=gold, 6=reputation, 7=drop
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTitleEffectParser
    {
        public const int EffectIdCol = 0;
        public const int TitleIdCol = 1;
        public const int EffectTypeCol = 2;
        public const int EffectValueCol = 3;
        public const int IsPercentCol = 4;
        public const int RequiredTitleLevelCol = 5;

        public static List<PcTitleEffectEntry> ParseFile(string path)
        {
            var rows = new List<PcTitleEffectEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, EffectIdCol);
                if (id <= 0) continue;
                rows.Add(new PcTitleEffectEntry
                {
                    effectId = id,
                    titleId = PcItemCommon.Int(cols, TitleIdCol),
                    effectType = PcItemCommon.Int(cols, EffectTypeCol),
                    effectValue = PcItemCommon.Int(cols, EffectValueCol),
                    isPercent = PcItemCommon.Int(cols, IsPercentCol) != 0,
                    requiredTitleLevel = PcItemCommon.Int(cols, RequiredTitleLevelCol),
                });
            }
            return rows;
        }

        public static PcTitleEffectRegistry BuildRegistry(string dir)
        {
            var reg = new PcTitleEffectRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }

    [System.Serializable]
    public class PcTitleEffectEntry
    {
        public int effectId;
        public int titleId;
        public int effectType; // 0=hp, 1=mp, 2=atk, 3=def, 4=exp, 5=gold, 6=reputation, 7=drop
        public int effectValue;
        public bool isPercent;
        public int requiredTitleLevel;
    }

    public sealed class PcTitleEffectRegistry
    {
        private readonly Dictionary<int, PcTitleEffectEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcTitleEffectEntry e) { if (e == null || e.effectId <= 0) return; _byId[e.effectId] = e; }
        public PcTitleEffectEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcTitleEffectEntry> GetByTitle(int titleId)
        {
            var list = new List<PcTitleEffectEntry>();
            foreach (var e in _byId.Values)
                if (e.titleId == titleId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcTitleEffectEntry> GetByType(int effectType)
        {
            var list = new List<PcTitleEffectEntry>();
            foreach (var e in _byId.Values)
                if (e.effectType == effectType) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcTitleEffectEntry> All => new List<PcTitleEffectEntry>(_byId.Values);
    }
}
