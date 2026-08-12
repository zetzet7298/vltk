// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/skillstate.txt Skill State parser
// Source: skillstate.txt (Reference/PcSkill, tab-separated).
//   StateId  Name  Type  DurationMs  TickIntervalMs  EffectValue  StackMax
// Type: 0=buff, 1=debuff, 2=choáng, 3=làm chậm, 4=chảy máu, 5=cháy,
//       6=đóng băng, 7=độc.
// Trạng thái kỹ năng (DOT/HOT/CC/stacks) áp dụng lên nhân vật/npc.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkillStateParser
    {
        public const int StateIdCol = 0;
        public const int NameCol = 1;
        public const int TypeCol = 2;
        public const int DurationMsCol = 3;
        public const int TickIntervalMsCol = 4;
        public const int EffectValueCol = 5;
        public const int StackMaxCol = 6;

        public static List<PcSkillStateEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillStateEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, StateIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSkillStateEntry
                {
                    stateId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    type = PcItemCommon.Int(cols, TypeCol),
                    durationMs = cols.Length > DurationMsCol ? PcItemCommon.Int(cols, DurationMsCol) : 0,
                    tickIntervalMs = cols.Length > TickIntervalMsCol ? PcItemCommon.Int(cols, TickIntervalMsCol) : 0,
                    effectValue = cols.Length > EffectValueCol ? PcItemCommon.Int(cols, EffectValueCol) : 0,
                    stackMax = cols.Length > StackMaxCol ? PcItemCommon.Int(cols, StackMaxCol) : 1,
                });
            }
            return rows;
        }

        public static PcSkillStateRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillStateRegistry();
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
    public class PcSkillStateEntry
    {
        public int stateId;
        public string nameRaw;
        public int type;
        public int durationMs;
        public int tickIntervalMs;
        public int effectValue;
        public int stackMax;
    }

    public sealed class PcSkillStateRegistry
    {
        private readonly Dictionary<int, PcSkillStateEntry> _byId = new();
        private readonly Dictionary<int, List<PcSkillStateEntry>> _byType = new();
        public int Count => _byId.Count;

        public void Register(PcSkillStateEntry e)
        {
            if (e == null || e.stateId <= 0) return;
            _byId[e.stateId] = e;
            if (!_byType.TryGetValue(e.type, out var list))
            {
                list = new List<PcSkillStateEntry>();
                _byType[e.type] = list;
            }
            list.Add(e);
        }

        public PcSkillStateEntry Get(int stateId)
            => _byId.TryGetValue(stateId, out var v) ? v : null;

        public IReadOnlyList<PcSkillStateEntry> GetByType(int type)
            => _byType.TryGetValue(type, out var v) ? v : (IReadOnlyList<PcSkillStateEntry>)System.Array.Empty<PcSkillStateEntry>();

        public IReadOnlyList<PcSkillStateEntry> All
            => new List<PcSkillStateEntry>(_byId.Values);
    }
}
