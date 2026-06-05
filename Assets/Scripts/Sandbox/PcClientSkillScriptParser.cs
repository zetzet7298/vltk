// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/clientskillscripts.txt Client Skill Script parser
// Source: clientskillscripts.txt (722 entries) — kịch bản client-side kỹ năng.
//   ScriptId  SkillId  ClientEvent  ScriptFile  IconOverride
// ClientEvent: 0=pre_cast, 1=on_hit, 2=on_crit, 3=on_kill
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcClientSkillScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int SkillIdCol = 1;
        public const int ClientEventCol = 2;
        public const int ScriptFileCol = 3;
        public const int IconOverrideCol = 4;

        public static List<PcClientSkillScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcClientSkillScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcClientSkillScriptEntry
                {
                    scriptId = PcItemCommon.Int(cols, ScriptIdCol),
                    skillId = PcItemCommon.Int(cols, SkillIdCol),
                    clientEvent = PcItemCommon.Int(cols, ClientEventCol),
                    scriptFile = PcItemCommon.Str(cols, ScriptFileCol),
                    iconOverride = PcItemCommon.Str(cols, IconOverrideCol),
                });
            }
            return rows;
        }

        public static PcClientSkillScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcClientSkillScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "clientskillscripts.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcClientSkillScriptEntry
    {
        public int scriptId;
        public int skillId;
        public int clientEvent;     // 0=pre_cast, 1=on_hit, 2=on_crit, 3=on_kill
        public string scriptFile;
        public string iconOverride; // empty = giữ icon mặc định
    }

    public sealed class PcClientSkillScriptRegistry
    {
        private readonly Dictionary<int, PcClientSkillScriptEntry> _byId = new();
        private readonly Dictionary<int, List<PcClientSkillScriptEntry>> _bySkill = new();
        private readonly Dictionary<int, List<PcClientSkillScriptEntry>> _byEvent = new();
        public int Count => _byId.Count;
        public IEnumerable<PcClientSkillScriptEntry> All => _byId.Values;
        public void Register(PcClientSkillScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
            if (!_bySkill.TryGetValue(e.skillId, out var slist))
            {
                slist = new List<PcClientSkillScriptEntry>();
                _bySkill[e.skillId] = slist;
            }
            slist.Add(e);
            if (!_byEvent.TryGetValue(e.clientEvent, out var elist))
            {
                elist = new List<PcClientSkillScriptEntry>();
                _byEvent[e.clientEvent] = elist;
            }
            elist.Add(e);
        }
        public PcClientSkillScriptEntry Get(int scriptId)
            => _byId.TryGetValue(scriptId, out var v) ? v : null;
        public IReadOnlyList<PcClientSkillScriptEntry> GetBySkill(int skillId)
            => _bySkill.TryGetValue(skillId, out var v)
                ? (IReadOnlyList<PcClientSkillScriptEntry>)v
                : (IReadOnlyList<PcClientSkillScriptEntry>)System.Array.Empty<PcClientSkillScriptEntry>();
        public IReadOnlyList<PcClientSkillScriptEntry> GetByEvent(int evt)
            => _byEvent.TryGetValue(evt, out var v)
                ? (IReadOnlyList<PcClientSkillScriptEntry>)v
                : (IReadOnlyList<PcClientSkillScriptEntry>)System.Array.Empty<PcClientSkillScriptEntry>();
    }
}
