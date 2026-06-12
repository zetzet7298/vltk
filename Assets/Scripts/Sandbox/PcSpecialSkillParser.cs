// -----------------------------------------------------------------------------
// VLTK Mobile — PC skills1.txt special-skill script catalog parser.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/settings/skills1.txt
// The committed specialskills.txt preserves the PC skills1.txt header plus rows where
// LvlSetScript starts with "\\script\\skill\\special". PC source proves 576 rows,
// not an old standalone 58-row specialskills.txt table.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSpecialSkillParser
    {
        public const int SkillNameCol = 0;
        public const int PropertyCol = 1;
        public const int SkillIdCol = 2;
        public const int SkillStyleCol = 4;
        public const int SkillIconCol = 5;
        public const int AttackRadiusCol = 14;
        public const int MissilesFormCol = 19;
        public const int ChildSkillIdCol = 20;
        public const int ChildSkillLevelCol = 21;
        public const int ChildSkillNumCol = 22;
        public const int ReqLevelCol = 53;
        public const int MaxLevelCol = 54;
        public const int CostValueCol = 31;
        public const int TimePerCastCol = 32;
        public const int SeriesCol = 69;
        public const int LvlSetScriptCol = 71;
        public const int LvlSetting1Col = 72;
        public const int LvlData1Col = 73;
        public const int PcSkills1ColumnCount = 115;
        public const string SpecialScriptPrefix = "\\script\\skill\\special";

        public static List<PcSpecialSkillEntry> ParseFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new List<PcSpecialSkillEntry>();
            return ParseLines(PcText.ReadLinesTcvn3(path));
        }

        public static List<PcSpecialSkillEntry> ParseLines(IReadOnlyList<string> lines)
        {
            var rows = new List<PcSpecialSkillEntry>();
            if (lines == null || lines.Count == 0) return rows;

            bool headerSkipped = false;
            int sourceRowNumber = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                sourceRowNumber++;
                var entry = ParseRow(line.Split('\t'), sourceRowNumber);
                if (entry != null) rows.Add(entry);
            }
            return rows;
        }

        public static PcSpecialSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcSpecialSkillRegistry();
            if (string.IsNullOrEmpty(dir)) return reg;
            string main = Directory.Exists(dir) ? Path.Combine(dir, "specialskills.txt") : dir;
            if (!File.Exists(main)) return reg;
            foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }

        private static PcSpecialSkillEntry ParseRow(string[] cols, int sourceRowNumber)
        {
            if (cols == null || cols.Length <= LvlSetScriptCol) return null;
            int id = PcItemCommon.Int(cols, SkillIdCol);
            if (id <= 0) return null;

            string script = PcItemCommon.Str(cols, LvlSetScriptCol);
            if (!script.StartsWith(SpecialScriptPrefix, StringComparison.OrdinalIgnoreCase)) return null;

            int style = PcItemCommon.Int(cols, SkillStyleCol);
            int series = PcItemCommon.Int(cols, SeriesCol, -1);
            return new PcSpecialSkillEntry
            {
                skillId = id,
                nameRaw = PcItemCommon.Str(cols, SkillNameCol),
                propertyRaw = PcItemCommon.Str(cols, PropertyCol),
                skillStyle = style,
                skillIcon = PcItemCommon.Str(cols, SkillIconCol),
                attackRadius = PcItemCommon.Int(cols, AttackRadiusCol),
                missilesForm = PcItemCommon.Int(cols, MissilesFormCol),
                childSkillId = PcItemCommon.Int(cols, ChildSkillIdCol),
                childSkillLevel = PcItemCommon.Int(cols, ChildSkillLevelCol),
                childSkillNum = PcItemCommon.Int(cols, ChildSkillNumCol),
                reqLevel = PcItemCommon.Int(cols, ReqLevelCol),
                maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                costValue = PcItemCommon.Int(cols, CostValueCol),
                timePerCast = PcItemCommon.Int(cols, TimePerCastCol),
                series = series,
                levelSetScript = script,
                levelSetting1 = PcItemCommon.Str(cols, LvlSetting1Col),
                levelData1 = PcItemCommon.Str(cols, LvlData1Col),
                sourceColumnCount = cols.Length,
                sourceRowNumber = sourceRowNumber,
                isSpecialSkillScript = true,
                factionId = series,
                skillType = style,
                manaCost = PcItemCommon.Int(cols, CostValueCol),
                coolDownMs = PcItemCommon.Int(cols, TimePerCastCol),
                icon = PcItemCommon.Str(cols, SkillIconCol),
                scriptFile = script,
            };
        }
    }

    [System.Serializable]
    public class PcSpecialSkillEntry
    {
        public int skillId;
        public string nameRaw;
        public string propertyRaw;
        public int skillStyle;
        public string skillIcon;
        public int attackRadius;
        public int missilesForm;
        public int childSkillId;
        public int childSkillLevel;
        public int childSkillNum;
        public int reqLevel;
        public int maxLevel;
        public int costValue;
        public int timePerCast;
        public int series;
        public string levelSetScript;
        public string levelSetting1;
        public string levelData1;
        public int sourceColumnCount;
        public int sourceRowNumber;
        public bool isSpecialSkillScript;

        // Backward-compatible aliases for the earlier provisional parser surface.
        public int factionId;
        public int skillType;
        public int manaCost;
        public int coolDownMs;
        public string icon;
        public string scriptFile;
    }

    public sealed class PcSpecialSkillRegistry
    {
        private readonly List<PcSpecialSkillEntry> _all = new List<PcSpecialSkillEntry>();
        private readonly Dictionary<int, PcSpecialSkillEntry> _byId = new Dictionary<int, PcSpecialSkillEntry>();
        private readonly Dictionary<string, List<PcSpecialSkillEntry>> _byScript =
            new Dictionary<string, List<PcSpecialSkillEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, List<PcSpecialSkillEntry>> _byFaction = new Dictionary<int, List<PcSpecialSkillEntry>>();

        public int Count => _all.Count;
        public int UniqueSkillIdCount => _byId.Count;
        public int UniqueScriptCount => _byScript.Count;
        public IReadOnlyList<PcSpecialSkillEntry> All => _all;

        public void Register(PcSpecialSkillEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            _all.Add(e);
            _byId[e.skillId] = e;

            string script = e.levelSetScript ?? string.Empty;
            if (!_byScript.TryGetValue(script, out var scriptList))
            {
                scriptList = new List<PcSpecialSkillEntry>();
                _byScript[script] = scriptList;
            }
            scriptList.Add(e);

            if (!_byFaction.TryGetValue(e.factionId, out var factionList))
            {
                factionList = new List<PcSpecialSkillEntry>();
                _byFaction[e.factionId] = factionList;
            }
            factionList.Add(e);
        }

        public PcSpecialSkillEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcSpecialSkillEntry> GetByScript(string script)
            => _byScript.TryGetValue(script ?? string.Empty, out var v)
                ? (IReadOnlyList<PcSpecialSkillEntry>)v
                : (IReadOnlyList<PcSpecialSkillEntry>)Array.Empty<PcSpecialSkillEntry>();

        public IReadOnlyList<PcSpecialSkillEntry> GetByFaction(int factionId)
            => _byFaction.TryGetValue(factionId, out var v)
                ? (IReadOnlyList<PcSpecialSkillEntry>)v
                : (IReadOnlyList<PcSpecialSkillEntry>)Array.Empty<PcSpecialSkillEntry>();
    }
}
