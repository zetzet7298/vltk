// -----------------------------------------------------------------------------
// VLTK Mobile — New Player Guide (Tân Thủ) parser
// Source: settings/tagnewplayer/newplayer.txt (17 entries, GB2312, tab columns).
//   GuideId  Step  RequiredLevel  MapId  NpcId  ScriptId  RewardExp  RewardItem
// Each row = 1 bước hướng dẫn tân thủ (mở bản đồ, gặp NPC, nhận thưởng).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcNewPlayerGuideParser
    {
        public const int GuideIdCol = 0;
        public const int StepCol = 1;
        public const int RequiredLevelCol = 2;
        public const int MapIdCol = 3;
        public const int NpcIdCol = 4;
        public const int ScriptIdCol = 5;
        public const int RewardExpCol = 6;
        public const int RewardItemCol = 7;

        public static List<PcNewPlayerGuideEntry> ParseFile(string path)
        {
            var rows = new List<PcNewPlayerGuideEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcNewPlayerGuideEntry
                {
                    guideId = PcItemCommon.Int(cols, GuideIdCol),
                    step = PcItemCommon.Int(cols, StepCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    mapId = cols.Length > MapIdCol ? PcItemCommon.Int(cols, MapIdCol) : 0,
                    npcId = cols.Length > NpcIdCol ? PcItemCommon.Int(cols, NpcIdCol) : 0,
                    scriptId = cols.Length > ScriptIdCol ? PcItemCommon.Int(cols, ScriptIdCol) : 0,
                    rewardExp = cols.Length > RewardExpCol ? PcItemCommon.Int(cols, RewardExpCol) : 0,
                    rewardItem = cols.Length > RewardItemCol ? PcItemCommon.Int(cols, RewardItemCol) : 0,
                });
            }
            return rows;
        }

        public static PcNewPlayerGuideRegistry BuildRegistry(string dir)
        {
            var reg = new PcNewPlayerGuideRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcNewPlayerGuideEntry
    {
        public int guideId;
        public int step;
        public int requiredLevel;
        public int mapId;
        public int npcId;
        public int scriptId;
        public int rewardExp;
        public int rewardItem;
    }

    public sealed class PcNewPlayerGuideRegistry
    {
        private readonly Dictionary<int, PcNewPlayerGuideEntry> _byId = new();
        public int Count => _byId.Count;
        public IEnumerable<PcNewPlayerGuideEntry> All => _byId.Values;
        public void Register(PcNewPlayerGuideEntry e)
        {
            if (e == null || e.guideId <= 0) return;
            _byId[e.guideId] = e;
        }
        public PcNewPlayerGuideEntry Get(int guideId)
            => _byId.TryGetValue(guideId, out var v) ? v : null;
        public IEnumerable<PcNewPlayerGuideEntry> GetForLevel(int level)
        {
            foreach (var e in _byId.Values)
                if (e != null && e.requiredLevel <= level) yield return e;
        }
    }
}
