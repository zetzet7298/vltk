// -----------------------------------------------------------------------------
// VLTK Mobile — PC Tong Battle Script parser
// Source: tongbattlescript.txt — script tấn công, phòng thủ, cổng, hồi sinh, điểm trong công thành chiến.
// Cols: TongBattleScriptId, Name, ScriptType, MapId, FunctionName, Description.
// ScriptType: 0=attack, 1=defend, 2=gate, 3=respawn, 4=score.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTongBattleScriptParser
    {
        public const int TongBattleScriptIdCol = 0;
        public const int NameCol = 1;
        public const int ScriptTypeCol = 2;
        public const int MapIdCol = 3;
        public const int FunctionNameCol = 4;
        public const int DescriptionCol = 5;

        public static List<PcTongBattleScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcTongBattleScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, TongBattleScriptIdCol);
                if (id <= 0) continue;
                rows.Add(new PcTongBattleScriptEntry
                {
                    tongBattleScriptId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    scriptType = PcItemCommon.Int(cols, ScriptTypeCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    functionName = PcItemCommon.Str(cols, FunctionNameCol),
                    descriptionRaw = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcTongBattleScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcTongBattleScriptRegistry();
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
    public class PcTongBattleScriptEntry
    {
        public int tongBattleScriptId;
        public string nameRaw;
        public int scriptType;
        public int mapId;
        public string functionName;
        public string descriptionRaw;
    }

    public sealed class PcTongBattleScriptRegistry
    {
        private readonly Dictionary<int, PcTongBattleScriptEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcTongBattleScriptEntry e) { if (e == null || e.tongBattleScriptId <= 0) return; _byId[e.tongBattleScriptId] = e; }
        public PcTongBattleScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcTongBattleScriptEntry> All => new List<PcTongBattleScriptEntry>(_byId.Values);

        public IReadOnlyList<PcTongBattleScriptEntry> GetByType(int scriptType)
        {
            var list = new List<PcTongBattleScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.scriptType == scriptType) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcTongBattleScriptEntry> GetByMap(int mapId)
        {
            var list = new List<PcTongBattleScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }
    }
}
