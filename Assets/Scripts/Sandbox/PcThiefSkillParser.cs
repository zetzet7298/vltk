// -----------------------------------------------------------------------------
// VLTK Mobile — PC thiefskill.txt focused parser/service data.
// Source: /var/www/vltksource_new/vl_update_27/Client 6.0/settings/thiefskill.txt
// Columns: SkillId SkillName ThiefStyle AttackRadius MaxLevel TimePerCast ...
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcThiefSkillParser
    {
        public const int SkillIdCol = 0;
        public const int SkillNameCol = 1;
        public const int ThiefStyleCol = 2;
        public const int AttackRadiusCol = 3;
        public const int MaxLevelCol = 4;
        public const int TimePerCastCol = 5;
        public const int ThiefPercentCol = 6;
        public const int SkillCostTypeCol = 7;
        public const int Param1Col = 8;
        public const int Param2Col = 9;
        public const int MovieCol = 10;
        public const int TargetMovieInfoCol = 11;
        public const int SkillSoundCol = 12;
        public const int TargetMovieCol = 13;
        public const int SkillIconCol = 14;
        public const int CostCol = 15;
        public const int DescCol = 16;

        public static List<PcThiefSkillEntry> ParseFile(string path)
        {
            var rows = new List<PcThiefSkillEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcText.ReadLinesTcvn3(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length <= SkillIdCol) continue;
                int skillId = PcItemCommon.Int(cols, SkillIdCol);
                if (skillId <= 0) continue;
                rows.Add(new PcThiefSkillEntry
                {
                    skillId = skillId,
                    skillName = PcItemCommon.Str(cols, SkillNameCol),
                    thiefStyle = PcItemCommon.Int(cols, ThiefStyleCol),
                    attackRadius = PcItemCommon.Int(cols, AttackRadiusCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    timePerCast = PcItemCommon.Int(cols, TimePerCastCol),
                    thiefPercent = PcItemCommon.Int(cols, ThiefPercentCol),
                    skillCostType = PcItemCommon.Int(cols, SkillCostTypeCol),
                    param1 = PcItemCommon.Int(cols, Param1Col),
                    param2 = PcItemCommon.Int(cols, Param2Col),
                    movie = PcItemCommon.Str(cols, MovieCol),
                    targetMovieInfo = PcItemCommon.Str(cols, TargetMovieInfoCol),
                    skillSound = PcItemCommon.Str(cols, SkillSoundCol),
                    targetMovie = PcItemCommon.Str(cols, TargetMovieCol),
                    skillIcon = PcItemCommon.Str(cols, SkillIconCol),
                    cost = PcItemCommon.Int(cols, CostCol),
                    desc = PcItemCommon.Str(cols, DescCol),
                });
            }
            return rows;
        }

        public static PcThiefSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcThiefSkillRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string path = Path.Combine(dir, "thiefskill.txt");
            foreach (var entry in ParseFile(path)) reg.Register(entry);
            reg.LinkSkillScripts(Path.Combine(dir, "skills.txt"));
            return reg;
        }
    }

    [System.Serializable]
    public class PcThiefSkillEntry
    {
        public int skillId;
        public string skillName;
        public int thiefStyle;
        public int attackRadius;
        public int maxLevel;
        public int timePerCast;
        public int thiefPercent;
        public int skillCostType;
        public int param1;
        public int param2;
        public string movie;
        public string targetMovieInfo;
        public string skillSound;
        public string targetMovie;
        public string skillIcon;
        public int cost;
        public string desc;
        public string lvlSetScript;
    }

    public sealed class PcThiefSkillRegistry
    {
        private readonly Dictionary<int, PcThiefSkillEntry> _bySkillId = new();
        private readonly Dictionary<int, PcThiefSkillEntry> _byThiefStyle = new();
        public int Count => _bySkillId.Count;
        public IReadOnlyList<PcThiefSkillEntry> All => new List<PcThiefSkillEntry>(_bySkillId.Values);
        public void Register(PcThiefSkillEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            _bySkillId[e.skillId] = e;
            _byThiefStyle[e.thiefStyle] = e;
        }
        public PcThiefSkillEntry Get(int skillId) => _bySkillId.TryGetValue(skillId, out var v) ? v : null;
        public PcThiefSkillEntry GetByThiefStyle(int thiefStyle) => _byThiefStyle.TryGetValue(thiefStyle, out var v) ? v : null;
        public void LinkSkillScripts(string skillsTxtPath)
        {
            var scripts = PcSkillSourceLinkParser.ParseSkillScripts(skillsTxtPath);
            foreach (var entry in _bySkillId.Values)
                if (scripts.TryGetValue(entry.skillId, out var script)) entry.lvlSetScript = script;
        }
    }
}
