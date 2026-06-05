// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/battlescripts.txt battle script (Kịch Bản Chiến Đấu) parser
// Source: settings/battlescripts.txt (183 entries, GB2312, tab-separated).
//   Cols: ScriptId  ScriptName  TriggerType  MapId  NpcId  RewardId  RewardCount  ScoreReward
//   TriggerType: 0 = start, 1 = end, 2 = kill_boss, 3 = death
// Kịch bản chiến đấu cho Tống Kim, Công Thành Chiến, Võ Lâm Liên Đấu, ...
// Tolerant of missing file (trả về registry rỗng).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcBattleScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int ScriptNameCol = 1;
        public const int TriggerTypeCol = 2;
        public const int MapIdCol = 3;
        public const int NpcIdCol = 4;
        public const int RewardIdCol = 5;
        public const int RewardCountCol = 6;
        public const int ScoreRewardCol = 7;

        public const int TriggerStart = 0;
        public const int TriggerEnd = 1;
        public const int TriggerKillBoss = 2;
        public const int TriggerDeath = 3;

        public static List<PcBattleScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcBattleScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            string[] lines;
            try { lines = PcItemCommon.ReadServerLines(path); }
            catch { lines = File.ReadAllLines(path); }
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                rows.Add(new PcBattleScriptEntry
                {
                    scriptId = PcItemCommon.Int(cols, ScriptIdCol),
                    scriptName = PcItemCommon.Str(cols, ScriptNameCol),
                    triggerType = PcItemCommon.Int(cols, TriggerTypeCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    npcId = cols.Length > NpcIdCol ? PcItemCommon.Int(cols, NpcIdCol) : 0,
                    rewardId = cols.Length > RewardIdCol ? PcItemCommon.Int(cols, RewardIdCol) : 0,
                    rewardCount = cols.Length > RewardCountCol ? PcItemCommon.Int(cols, RewardCountCol) : 0,
                    scoreReward = cols.Length > ScoreRewardCol ? PcItemCommon.Int(cols, ScoreRewardCol) : 0,
                });
            }
            return rows;
        }

        public static PcBattleScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcBattleScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string[] candidates = { "battlescripts.txt", "battle_script.txt", "battle.txt", "battlescript.txt" };
            foreach (var fn in candidates)
            {
                string main = Path.Combine(dir, fn);
                if (File.Exists(main))
                {
                    foreach (var s in ParseFile(main)) reg.Register(s);
                    return reg;
                }
            }
            // Fallback: quét tất cả *.txt
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
            {
                foreach (var s in ParseFile(f)) reg.Register(s);
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcBattleScriptEntry
    {
        public int scriptId;
        public string scriptName;
        public int triggerType;     // 0=start, 1=end, 2=kill_boss, 3=death
        public int mapId;
        public int npcId;
        public int rewardId;
        public int rewardCount;
        public int scoreReward;
    }

    public sealed class PcBattleScriptRegistry
    {
        private readonly Dictionary<int, PcBattleScriptEntry> _byId = new();
        private readonly Dictionary<int, List<PcBattleScriptEntry>> _byMap = new();
        private readonly Dictionary<int, List<PcBattleScriptEntry>> _byTrigger = new();
        public int Count => _byId.Count;
        public void Register(PcBattleScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
            if (!_byMap.TryGetValue(e.mapId, out var mList))
            {
                mList = new List<PcBattleScriptEntry>();
                _byMap[e.mapId] = mList;
            }
            mList.Add(e);
            if (!_byTrigger.TryGetValue(e.triggerType, out var tList))
            {
                tList = new List<PcBattleScriptEntry>();
                _byTrigger[e.triggerType] = tList;
            }
            tList.Add(e);
        }
        public PcBattleScriptEntry Get(int scriptId)
            => _byId.TryGetValue(scriptId, out var v) ? v : null;

        public IEnumerable<PcBattleScriptEntry> GetByMap(int mapId)
            => _byMap.TryGetValue(mapId, out var v)
                ? (IEnumerable<PcBattleScriptEntry>)v
                : System.Array.Empty<PcBattleScriptEntry>();

        public IEnumerable<PcBattleScriptEntry> GetByTriggerType(int triggerType)
            => _byTrigger.TryGetValue(triggerType, out var v)
                ? (IEnumerable<PcBattleScriptEntry>)v
                : System.Array.Empty<PcBattleScriptEntry>();

        public IEnumerable<PcBattleScriptEntry> All => _byId.Values;
    }
}
