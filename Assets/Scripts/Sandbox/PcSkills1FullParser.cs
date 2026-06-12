// -----------------------------------------------------------------------------
// VLTK Mobile — full PC Server settings/skills1.txt source-audit parser.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/settings/skills1.txt
// This is a catalog/audit surface only; it does not claim runtime skill behavior.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkills1FullParser
    {
        public const string FileName = "skills1_full.txt";
        public const int SkillNameCol = 0;
        public const int SkillIdCol = 2;
        public const int SkillIconCol = 5;
        public const int MaxLevelCol = 54;
        public const int LvlSetScriptCol = 71;
        public const int ExpectedColumnCount = 115;
        public const int ModSkillsExpansionMinSkillId = 1216;
        public const string SpecialScriptPrefix = "\\script\\skill\\special";
        public const string NpcScriptPrefix = "\\script\\skill\\npc";

        public static PcSkills1FullCatalog ParseFile(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return PcSkills1FullCatalog.Empty;
            return ParseLines(PcText.ReadLinesTcvn3(absolutePath));
        }

        public static PcSkills1FullCatalog ParseLines(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0) return PcSkills1FullCatalog.Empty;

            string[] header = null;
            var rows = new List<PcSkills1FullRow>();
            int nonEmpty = 0;
            int sourceRow = 0;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                nonEmpty++;
                if (header == null)
                {
                    header = raw.Split('\t');
                    continue;
                }

                sourceRow++;
                var parsed = ParseRow(raw.Split('\t'), sourceRow);
                if (parsed != null) rows.Add(parsed);
            }

            return new PcSkills1FullCatalog(header ?? Array.Empty<string>(), rows, lines.Count, nonEmpty);
        }

        public static PcSkills1FullRow ParseRow(string[] cols, int sourceRowNumber)
        {
            if (cols == null || cols.Length == 0) return null;
            int id = PcItemCommon.Int(cols, SkillIdCol);
            string name = PcItemCommon.Str(cols, SkillNameCol);
            if (id <= 0 && string.IsNullOrEmpty(name)) return null;

            string script = PcItemCommon.Str(cols, LvlSetScriptCol);
            bool isSpecial = script.StartsWith(SpecialScriptPrefix, StringComparison.OrdinalIgnoreCase);
            bool isNpc = script.StartsWith(NpcScriptPrefix, StringComparison.OrdinalIgnoreCase);
            bool isBoss = name.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0;

            return new PcSkills1FullRow
            {
                sourceRowNumber = sourceRowNumber,
                skillId = id,
                skillName = name,
                skillIcon = PcItemCommon.Str(cols, SkillIconCol),
                maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                levelSetScript = script,
                sourceColumnCount = cols.Length,
                columns = Array.AsReadOnly(cols),
                isSpecialSkillScript = isSpecial,
                isNpcSkillScript = isNpc,
                isBossName = isBoss,
                isNpcSubsetRow = isNpc || isBoss,
                isSkillIdAtLeast1216 = id >= ModSkillsExpansionMinSkillId,
            };
        }
    }

    [Serializable]
    public sealed class PcSkills1FullRow
    {
        public int sourceRowNumber;
        public int skillId;
        public string skillName;
        public string skillIcon;
        public int maxLevel;
        public string levelSetScript;
        public int sourceColumnCount;
        public IReadOnlyList<string> columns;
        public bool isSpecialSkillScript;
        public bool isNpcSkillScript;
        public bool isBossName;
        public bool isNpcSubsetRow;
        public bool isSkillIdAtLeast1216;
    }

    public sealed class PcSkills1FullCatalog
    {
        public static readonly PcSkills1FullCatalog Empty =
            new PcSkills1FullCatalog(Array.Empty<string>(), new List<PcSkills1FullRow>(), 0, 0);

        public readonly IReadOnlyList<string> header;
        public readonly IReadOnlyList<PcSkills1FullRow> rows;
        public readonly int sourceLineCount;
        public readonly int nonEmptyLineCount;

        public PcSkills1FullCatalog(IReadOnlyList<string> header, IReadOnlyList<PcSkills1FullRow> rows, int sourceLineCount, int nonEmptyLineCount)
        {
            this.header = header ?? Array.Empty<string>();
            this.rows = rows ?? Array.Empty<PcSkills1FullRow>();
            this.sourceLineCount = sourceLineCount;
            this.nonEmptyLineCount = nonEmptyLineCount;
        }

        public int HeaderColumnCount => header.Count;
        public int DataRowCount => rows.Count;
        public int SpecialSkillScriptRowCount => Count(r => r.isSpecialSkillScript);
        public int NpcSkillScriptRowCount => Count(r => r.isNpcSkillScript);
        public int BossNameRowCount => Count(r => r.isBossName);
        public int NpcSubsetUnionRowCount => Count(r => r.isNpcSubsetRow);
        public int SkillIdAtLeast1216RowCount => Count(r => r.isSkillIdAtLeast1216);

        private int Count(Predicate<PcSkills1FullRow> predicate)
        {
            int count = 0;
            foreach (var row in rows) if (row != null && predicate(row)) count++;
            return count;
        }
    }
}
