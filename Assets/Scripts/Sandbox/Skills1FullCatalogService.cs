// -----------------------------------------------------------------------------
// VLTK Mobile — read-only service for full PC skills1 catalog audits.
// Catalog only: no runtime combat formula/script evaluation is implied here.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public sealed class Skills1FullCatalogService
    {
        private readonly PcSkills1FullCatalog _catalog;
        private readonly Dictionary<int, PcSkills1FullRow> _bySkillId = new Dictionary<int, PcSkills1FullRow>();

        public Skills1FullCatalogService(PcSkills1FullCatalog catalog)
        {
            _catalog = catalog ?? PcSkills1FullCatalog.Empty;
            foreach (var row in _catalog.rows)
            {
                if (row == null || row.skillId <= 0) continue;
                if (!_bySkillId.ContainsKey(row.skillId)) _bySkillId.Add(row.skillId, row);
            }
        }

        public PcSkills1FullCatalog Catalog => _catalog;
        public Skills1FullCatalogStats Stats => Skills1FullCatalogStats.FromCatalog(_catalog);

        public static Skills1FullCatalogService LoadFromDirectory(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return new Skills1FullCatalogService(PcSkills1FullCatalog.Empty);
            string path = Directory.Exists(dir) ? Path.Combine(dir, PcSkills1FullParser.FileName) : dir;
            return new Skills1FullCatalogService(PcSkills1FullParser.ParseFile(path));
        }

        public PcSkills1FullRow Resolve(int skillId)
            => _bySkillId.TryGetValue(skillId, out var row) ? row : null;

        public List<PcSkills1FullRow> SpecialSkillScriptRows() => Filter(r => r.isSpecialSkillScript);
        public List<PcSkills1FullRow> NpcSubsetRows() => Filter(r => r.isNpcSubsetRow);
        public List<PcSkills1FullRow> SkillIdAtLeast1216Rows() => Filter(r => r.isSkillIdAtLeast1216);

        private List<PcSkills1FullRow> Filter(Predicate<PcSkills1FullRow> predicate)
        {
            var rows = new List<PcSkills1FullRow>();
            foreach (var row in _catalog.rows)
                if (row != null && predicate(row)) rows.Add(row);
            return rows;
        }
    }

    public sealed class Skills1FullCatalogStats
    {
        public int sourceLineCount;
        public int nonEmptyLineCount;
        public int headerColumnCount;
        public int dataRowCount;
        public int rowsWithExpectedColumnCount;
        public int uniqueSkillIdCount;
        public int duplicateSkillIdCount;
        public int specialSkillScriptRows;
        public int npcSkillScriptRows;
        public int bossNameRows;
        public int npcSubsetUnionRows;
        public int skillIdAtLeast1216Rows;

        public static Skills1FullCatalogStats FromCatalog(PcSkills1FullCatalog catalog)
        {
            catalog = catalog ?? PcSkills1FullCatalog.Empty;
            var ids = new HashSet<int>();
            int expectedCols = 0;
            foreach (var row in catalog.rows)
            {
                if (row == null) continue;
                if (row.sourceColumnCount == PcSkills1FullParser.ExpectedColumnCount) expectedCols++;
                if (row.skillId > 0) ids.Add(row.skillId);
            }

            return new Skills1FullCatalogStats
            {
                sourceLineCount = catalog.sourceLineCount,
                nonEmptyLineCount = catalog.nonEmptyLineCount,
                headerColumnCount = catalog.HeaderColumnCount,
                dataRowCount = catalog.DataRowCount,
                rowsWithExpectedColumnCount = expectedCols,
                uniqueSkillIdCount = ids.Count,
                duplicateSkillIdCount = catalog.DataRowCount - ids.Count,
                specialSkillScriptRows = catalog.SpecialSkillScriptRowCount,
                npcSkillScriptRows = catalog.NpcSkillScriptRowCount,
                bossNameRows = catalog.BossNameRowCount,
                npcSubsetUnionRows = catalog.NpcSubsetUnionRowCount,
                skillIdAtLeast1216Rows = catalog.SkillIdAtLeast1216RowCount,
            };
        }
    }
}
