// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/skillleveldata.txt Skill Level Data parser
// Source: skillleveldata.txt (Reference/PcSkill, tab-separated).
//   SkillId  Level  CastTime  CooldownMs  ManaCost  StaminaCost
//   DamageMin  DamageMax  RangeRadius  EffectArea  BuffId  Description
// Cấu hình chi tiết từng cấp cho từng kỹ năng: cast time, cooldown, mana,
// sát thương tối thiểu/tối đa, phạm vi, vùng hiệu ứng, buff kèm theo.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkillLevelDataParser
    {
        public const int SkillIdCol = 0;
        public const int LevelCol = 1;
        public const int CastTimeCol = 2;
        public const int CooldownMsCol = 3;
        public const int ManaCostCol = 4;
        public const int StaminaCostCol = 5;
        public const int DamageMinCol = 6;
        public const int DamageMaxCol = 7;
        public const int RangeRadiusCol = 8;
        public const int EffectAreaCol = 9;
        public const int BuffIdCol = 10;
        public const int DescriptionCol = 11;

        public static List<PcSkillLevelDataEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillLevelDataEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, SkillIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSkillLevelDataEntry
                {
                    skillId = id,
                    level = PcItemCommon.Int(cols, LevelCol),
                    castTime = cols.Length > CastTimeCol ? PcItemCommon.Int(cols, CastTimeCol) : 0,
                    cooldownMs = cols.Length > CooldownMsCol ? PcItemCommon.Int(cols, CooldownMsCol) : 0,
                    manaCost = cols.Length > ManaCostCol ? PcItemCommon.Int(cols, ManaCostCol) : 0,
                    staminaCost = cols.Length > StaminaCostCol ? PcItemCommon.Int(cols, StaminaCostCol) : 0,
                    damageMin = cols.Length > DamageMinCol ? PcItemCommon.Int(cols, DamageMinCol) : 0,
                    damageMax = cols.Length > DamageMaxCol ? PcItemCommon.Int(cols, DamageMaxCol) : 0,
                    rangeRadius = cols.Length > RangeRadiusCol ? PcItemCommon.Int(cols, RangeRadiusCol) : 0,
                    effectArea = cols.Length > EffectAreaCol ? PcItemCommon.Int(cols, EffectAreaCol) : 0,
                    buffId = cols.Length > BuffIdCol ? PcItemCommon.Int(cols, BuffIdCol) : 0,
                    description = cols.Length > DescriptionCol ? PcItemCommon.Str(cols, DescriptionCol) : string.Empty,
                });
            }
            return rows;
        }

        public static PcSkillLevelDataRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillLevelDataRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Use the explicit file-name family to avoid sweeping unrelated
            // .txt/.ini files in the directory tree.
            foreach (var f in Directory.GetFiles(dir, "skillleveldata*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcSkillLevelDataEntry
    {
        public int skillId;
        public int level;
        public int castTime;
        public int cooldownMs;
        public int manaCost;
        public int staminaCost;
        public int damageMin;
        public int damageMax;
        public int rangeRadius;
        public int effectArea;
        public int buffId;
        public string description;
    }

    public sealed class PcSkillLevelDataRegistry
    {
        private readonly Dictionary<long, PcSkillLevelDataEntry> _byKey = new();
        // Secondary index keyed by skillId to make per-skill lookups O(1)
        // instead of scanning every entry in _byKey.
        private readonly Dictionary<int, List<PcSkillLevelDataEntry>> _bySkill = new();
        private readonly Dictionary<int, int> _maxLevelBySkill = new();
        public int Count => _byKey.Count;

        public void Register(PcSkillLevelDataEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            _byKey[((long)e.skillId << 32) | (uint)e.level] = e;
            if (!_bySkill.TryGetValue(e.skillId, out var list))
            {
                list = new List<PcSkillLevelDataEntry>();
                _bySkill[e.skillId] = list;
            }
            list.Add(e);
            if (!_maxLevelBySkill.TryGetValue(e.skillId, out int max) || e.level > max)
                _maxLevelBySkill[e.skillId] = e.level;
        }

        public PcSkillLevelDataEntry Get(int skillId, int level)
        {
            _byKey.TryGetValue(((long)skillId << 32) | (uint)level, out var v);
            return v;
        }

        public IReadOnlyList<PcSkillLevelDataEntry> GetBySkill(int skillId)
        {
            return _bySkill.TryGetValue(skillId, out var list)
                ? (IReadOnlyList<PcSkillLevelDataEntry>)list
                : System.Array.Empty<PcSkillLevelDataEntry>();
        }

        public int GetMaxLevelForSkill(int skillId)
        {
            return _maxLevelBySkill.TryGetValue(skillId, out int max) ? max : 0;
        }

        public IReadOnlyList<PcSkillLevelDataEntry> All
        {
            get { return new List<PcSkillLevelDataEntry>(_byKey.Values); }
        }
    }
}
