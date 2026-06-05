// -----------------------------------------------------------------------------
// VLTK Mobile — ST-6.2 Mission Scripts metadata parser
// Source: missionscripts.txt (Reference/PcMission or root). 985 scripts.
// Cols: ScriptId  MissionId  ScriptName  TriggerOn  Type  Target  Count  NextScriptId  Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMissionScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int MissionIdCol = 1;
        public const int ScriptNameCol = 2;
        public const int TriggerOnCol = 3;
        public const int TypeCol = 4;
        public const int TargetCol = 5;
        public const int CountCol = 6;
        public const int NextScriptIdCol = 7;
        public const int DescriptionCol = 8;

        public static List<PcMissionScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcMissionScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, ScriptIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMissionScriptEntry
                {
                    scriptId = id,
                    missionId = PcItemCommon.Int(cols, MissionIdCol),
                    scriptName = PcItemCommon.Str(cols, ScriptNameCol),
                    triggerOn = PcItemCommon.Int(cols, TriggerOnCol),
                    type = PcItemCommon.Int(cols, TypeCol),
                    target = PcItemCommon.Int(cols, TargetCol),
                    count = PcItemCommon.Int(cols, CountCol),
                    nextScriptId = PcItemCommon.Int(cols, NextScriptIdCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcMissionScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcMissionScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcMissionScriptEntry
    {
        public int scriptId;
        public int missionId;
        public string scriptName;
        public int triggerOn;     // 0=accept, 1=progress, 2=complete, 3=fail
        public int type;          // 0=npc_kill, 2=item_collect, 3=map_reach, 4=npc_talk, 5=time_elapsed
        public int target;
        public int count;
        public int nextScriptId;
        public string description;
    }

    public sealed class PcMissionScriptRegistry
    {
        private readonly Dictionary<int, PcMissionScriptEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcMissionScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
        }

        public PcMissionScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcMissionScriptEntry> GetByMission(int missionId)
        {
            var list = new List<PcMissionScriptEntry>();
            foreach (var e in _byId.Values) if (e.missionId == missionId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcMissionScriptEntry> GetByType(int type)
        {
            var list = new List<PcMissionScriptEntry>();
            foreach (var e in _byId.Values) if (e.type == type) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcMissionScriptEntry> GetByTrigger(int triggerOn)
        {
            var list = new List<PcMissionScriptEntry>();
            foreach (var e in _byId.Values) if (e.triggerOn == triggerOn) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcMissionScriptEntry> All => new List<PcMissionScriptEntry>(_byId.Values);
    }
}
