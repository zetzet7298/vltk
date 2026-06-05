// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/magicscript.txt (Magic Script) parser
// Source: magicscript.txt (5,142 entries, GB2312, 27 tab columns).
//   Cols 0:  Name
//   Cols 1..3: ItemGenre, DetailType, ParticularType
//   Col  4:  SpritePath
//   Cols 5..6: Quality, Series
//   Col  7:  Description
//   Col  8:  Genre
//   Col  9:  Cost
//   Col  10: ReqLevel
//   Col  12: ScriptId
//   Col  13: MagicAttribId
//   Col  14: ParamCount
//   Col  25: TriggerOn (0=hit, 1=kill, 2=equip)
// Mobile keeps script id + magic attrib id + trigger for runtime proc effects.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcMagicScriptEntry
    {
        public int itemGenre;
        public int detailType;
        public int particularType;
        public string name;
        public int scriptId;       // Mã script phép
        public int magicAttribId;  // Mã thuộc tính phép
        public int paramCount;     // Số tham số
        public int triggerOn;      // 0=đánh trúng, 1=giết, 2=trang bị
        public int requiredLevel;
    }

    public sealed class PcMagicScriptRegistry
    {
        private readonly Dictionary<int, PcMagicScriptEntry> _byId = new();
        private readonly Dictionary<int, List<PcMagicScriptEntry>> _byAttrib = new();
        private readonly Dictionary<int, List<PcMagicScriptEntry>> _byTrigger = new();
        public int Count => _byId.Count;

        public void Register(PcMagicScriptEntry e)
        {
            if (e == null) return;
            if (e.scriptId <= 0) return; // Skip placeholder rows to avoid zero-ID collision.
            _byId[e.scriptId] = e;
            if (!_byAttrib.TryGetValue(e.magicAttribId, out var al)) { al = new(); _byAttrib[e.magicAttribId] = al; }
            al.Add(e);
            if (!_byTrigger.TryGetValue(e.triggerOn, out var tl)) { tl = new(); _byTrigger[e.triggerOn] = tl; }
            tl.Add(e);
        }

        public PcMagicScriptEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public List<PcMagicScriptEntry> GetByAttrib(int attribId)
            => _byAttrib.TryGetValue(attribId, out var v) ? v : new List<PcMagicScriptEntry>();

        public List<PcMagicScriptEntry> GetByTrigger(int trigger)
            => _byTrigger.TryGetValue(trigger, out var v) ? v : new List<PcMagicScriptEntry>();

        public IEnumerable<PcMagicScriptEntry> All => _byId.Values;
    }

    public static class PcMagicScriptParser
    {
        public const int NameCol = 0;
        public const int ScriptIdCol = 12;
        public const int MagicAttribIdCol = 13;
        public const int ParamCountCol = 14;
        public const int TriggerOnCol = 25;
        public const int ReqLevelCol = 10;

        public static List<PcMagicScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcMagicScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 26) continue;
                int scriptId = PcItemCommon.Int(cols, ScriptIdCol);
                if (scriptId <= 0) continue; // Drop placeholder rows early to keep registry clean.
                rows.Add(new PcMagicScriptEntry
                {
                    itemGenre = PcItemCommon.Int(cols, 1),
                    detailType = PcItemCommon.Int(cols, 2),
                    particularType = PcItemCommon.Int(cols, 3),
                    name = PcItemCommon.Str(cols, NameCol),
                    scriptId = scriptId,
                    magicAttribId = PcItemCommon.Int(cols, MagicAttribIdCol),
                    paramCount = PcItemCommon.Int(cols, ParamCountCol),
                    triggerOn = PcItemCommon.Int(cols, TriggerOnCol),
                    requiredLevel = PcItemCommon.Int(cols, ReqLevelCol),
                });
            }
            return rows;
        }

        public static PcMagicScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcMagicScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "magicscript*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
