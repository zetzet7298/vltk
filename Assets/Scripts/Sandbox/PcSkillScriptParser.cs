// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.6 Skill Scripts metadata parser
// Source: skillscripts.txt (Reference/PcSkill or root). 2,486 scripts.
// Cols: ScriptId  SkillId  Version  FileName  FunctionName  ParamsCount  Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkillScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int SkillIdCol = 1;
        public const int VersionCol = 2;
        public const int FileNameCol = 3;
        public const int FunctionNameCol = 4;
        public const int ParamsCountCol = 5;
        public const int DescriptionCol = 6;

        public static List<PcSkillScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, ScriptIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSkillScriptEntry
                {
                    scriptId = id,
                    skillId = PcItemCommon.Int(cols, SkillIdCol),
                    version = PcItemCommon.Int(cols, VersionCol),
                    fileName = PcItemCommon.Str(cols, FileNameCol),
                    functionName = PcItemCommon.Str(cols, FunctionNameCol),
                    paramsCount = PcItemCommon.Int(cols, ParamsCountCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcSkillScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Use the explicit file-name family to avoid sweeping unrelated
            // .txt/.ini files in the directory tree.
            foreach (var f in Directory.GetFiles(dir, "skillscripts*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcSkillScriptEntry
    {
        public int scriptId;
        public int skillId;
        public int version;
        public string fileName;
        public string functionName;
        public int paramsCount;
        public string description;
    }

    public sealed class PcSkillScriptRegistry
    {
        private readonly Dictionary<int, PcSkillScriptEntry> _byId = new();
        // Secondary index keyed by skillId to make per-skill lookups O(1)
        // instead of scanning every entry in _byId.
        private readonly Dictionary<int, List<PcSkillScriptEntry>> _bySkill = new();
        private readonly Dictionary<int, List<PcSkillScriptEntry>> _byVersion = new();
        public int Count => _byId.Count;

        public void Register(PcSkillScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
            if (!_bySkill.TryGetValue(e.skillId, out var sl))
            {
                sl = new List<PcSkillScriptEntry>();
                _bySkill[e.skillId] = sl;
            }
            sl.Add(e);
            if (!_byVersion.TryGetValue(e.version, out var vl))
            {
                vl = new List<PcSkillScriptEntry>();
                _byVersion[e.version] = vl;
            }
            vl.Add(e);
        }

        public PcSkillScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcSkillScriptEntry> GetBySkill(int skillId)
        {
            return _bySkill.TryGetValue(skillId, out var list)
                ? (IReadOnlyList<PcSkillScriptEntry>)list
                : System.Array.Empty<PcSkillScriptEntry>();
        }

        public IReadOnlyList<PcSkillScriptEntry> GetByVersion(int version)
        {
            return _byVersion.TryGetValue(version, out var list)
                ? (IReadOnlyList<PcSkillScriptEntry>)list
                : System.Array.Empty<PcSkillScriptEntry>();
        }

        public IReadOnlyList<PcSkillScriptEntry> All => new List<PcSkillScriptEntry>(_byId.Values);
    }
}
