// -----------------------------------------------------------------------------
// VLTK Mobile — PC Chuyển Sinh skill source parser.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/settings/skills.txt
// No standalone PC translifeskill.txt exists; committed data preserves the PC
// skills.txt header plus the 9 rows whose LvlSetScript is special/translife4th.lua.
// This is deliberately separate from PcTask/translife.txt (level bonus table).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTranslifeSkillSourceParser
    {
        public const string SourceFileName = "translifeskill.txt";
        public const string PcSourceRelativePath = "Server 6.0/server/home_jxser/server1/settings/skills.txt";
        public const string TranslifeLevelSetScript = "\\script\\skill\\special\\translife4th.lua";
        public const int PcSkillsColumnCount = 113;

        public const int SkillNameCol = 0;
        public const int PropertyCol = 1;
        public const int SkillIdCol = 2;
        public const int AttribCol = 3;
        public const int SkillStyleCol = 4;
        public const int SkillIconCol = 5;
        public const int MaxLevelCol = 53;
        public const int IsExpSkillCol = 67;
        public const int LvlSetScriptCol = 70;
        public const int LvlSetting1Col = 71;
        public const int LvlData1Col = 72;
        public const int LevelSettingPairCount = 20;

        public static List<PcTranslifeSkillSourceEntry> ParseFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new List<PcTranslifeSkillSourceEntry>();
            return ParseLines(PcItemCommon.ReadServerLines(path));
        }

        public static List<PcTranslifeSkillSourceEntry> ParseLines(IReadOnlyList<string> lines)
        {
            var rows = new List<PcTranslifeSkillSourceEntry>();
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
                var cols = line.Split('\t');
                var entry = ParseRow(cols, sourceRowNumber);
                if (entry != null) rows.Add(entry);
            }
            return rows;
        }

        public static PcTranslifeSkillSourceRegistry BuildRegistry(string dir)
        {
            var reg = new PcTranslifeSkillSourceRegistry();
            if (string.IsNullOrEmpty(dir)) return reg;
            string path = Directory.Exists(dir) ? Path.Combine(dir, SourceFileName) : dir;
            if (!File.Exists(path)) return reg;
            foreach (var row in ParseFile(path)) reg.Register(row);
            return reg;
        }

        private static PcTranslifeSkillSourceEntry ParseRow(string[] cols, int sourceRowNumber)
        {
            if (cols == null || cols.Length <= LvlSetScriptCol) return null;
            int skillId = PcItemCommon.Int(cols, SkillIdCol);
            if (skillId <= 0) return null;
            string script = PcItemCommon.Str(cols, LvlSetScriptCol);
            if (!string.Equals(script, TranslifeLevelSetScript, StringComparison.OrdinalIgnoreCase)) return null;

            return new PcTranslifeSkillSourceEntry
            {
                skillId = skillId,
                nameRaw = PcItemCommon.Str(cols, SkillNameCol),
                propertyRaw = PcItemCommon.Str(cols, PropertyCol),
                attrib = PcItemCommon.Int(cols, AttribCol),
                skillStyle = PcItemCommon.Int(cols, SkillStyleCol),
                skillIcon = PcItemCommon.Str(cols, SkillIconCol),
                maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                isExpSkill = PcItemCommon.Int(cols, IsExpSkillCol) != 0,
                levelSetScript = script,
                sourceColumnCount = cols.Length,
                sourceRowNumber = sourceRowNumber,
                levelSettings = ParseLevelSettings(cols),
            };
        }

        private static PcTranslifeSkillLevelSetting[] ParseLevelSettings(string[] cols)
        {
            var list = new List<PcTranslifeSkillLevelSetting>();
            for (int i = 0; i < LevelSettingPairCount; i++)
            {
                int settingCol = LvlSetting1Col + i * 2;
                int dataCol = LvlData1Col + i * 2;
                string setting = PcItemCommon.Str(cols, settingCol);
                string data = PcItemCommon.Str(cols, dataCol);
                if (string.IsNullOrEmpty(setting) && string.IsNullOrEmpty(data)) continue;
                list.Add(new PcTranslifeSkillLevelSetting
                {
                    settingName = setting,
                    dataKey = data,
                });
            }
            return list.ToArray();
        }
    }

    [Serializable]
    public sealed class PcTranslifeSkillSourceEntry
    {
        public int skillId;
        public string nameRaw;
        public string propertyRaw;
        public int attrib;
        public int skillStyle;
        public string skillIcon;
        public int maxLevel;
        public bool isExpSkill;
        public string levelSetScript;
        public int sourceColumnCount;
        public int sourceRowNumber;
        public PcTranslifeSkillLevelSetting[] levelSettings = Array.Empty<PcTranslifeSkillLevelSetting>();
    }

    [Serializable]
    public struct PcTranslifeSkillLevelSetting
    {
        public string settingName;
        public string dataKey;
    }

    public sealed class PcTranslifeSkillSourceRegistry
    {
        private readonly List<PcTranslifeSkillSourceEntry> _all = new List<PcTranslifeSkillSourceEntry>();
        private readonly Dictionary<int, PcTranslifeSkillSourceEntry> _byId = new Dictionary<int, PcTranslifeSkillSourceEntry>();

        public int Count => _all.Count;
        public IReadOnlyList<PcTranslifeSkillSourceEntry> All => _all;

        public void Register(PcTranslifeSkillSourceEntry entry)
        {
            if (entry == null || entry.skillId <= 0) return;
            if (_byId.ContainsKey(entry.skillId)) return;
            _all.Add(entry);
            _byId[entry.skillId] = entry;
        }

        public PcTranslifeSkillSourceEntry Get(int skillId)
            => _byId.TryGetValue(skillId, out var entry) ? entry : null;
    }
}
