// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/skillbook.txt Skill Book parser
// Source: skillbook.txt (Reference/PcSkill or PcItemFull, tab-separated).
//   BookId  Name  TeachesSkillId  BookType  RequiredLevel  ItemId
// BookType: 0=sơ cấp, 1=cao cấp, 2=đại sư, 3=thiên cấp.
// Sách kỹ năng — dùng để học kỹ năng mới (skillId mới được add vào known skills).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkillBookParser
    {
        public const int BookIdCol = 0;
        public const int NameCol = 1;
        public const int TeachesSkillIdCol = 2;
        public const int BookTypeCol = 3;
        public const int RequiredLevelCol = 4;
        public const int ItemIdCol = 5;

        public static List<PcSkillBookEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillBookEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, BookIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSkillBookEntry
                {
                    bookId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    teachesSkillId = cols.Length > TeachesSkillIdCol ? PcItemCommon.Int(cols, TeachesSkillIdCol) : 0,
                    bookType = cols.Length > BookTypeCol ? PcItemCommon.Int(cols, BookTypeCol) : 0,
                    requiredLevel = cols.Length > RequiredLevelCol ? PcItemCommon.Int(cols, RequiredLevelCol) : 0,
                    itemId = cols.Length > ItemIdCol ? PcItemCommon.Int(cols, ItemIdCol) : 0,
                });
            }
            return rows;
        }

        public static PcSkillBookRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillBookRegistry();
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
    public class PcSkillBookEntry
    {
        public int bookId;
        public string nameRaw;
        public int teachesSkillId;
        public int bookType;
        public int requiredLevel;
        public int itemId;
    }

    public sealed class PcSkillBookRegistry
    {
        private readonly Dictionary<int, PcSkillBookEntry> _byId = new();
        private readonly Dictionary<int, List<PcSkillBookEntry>> _bySkill = new();
        private readonly Dictionary<int, List<PcSkillBookEntry>> _byType = new();
        public int Count => _byId.Count;

        public void Register(PcSkillBookEntry e)
        {
            if (e == null || e.bookId <= 0) return;
            _byId[e.bookId] = e;
            if (e.teachesSkillId > 0)
            {
                if (!_bySkill.TryGetValue(e.teachesSkillId, out var bySkill))
                {
                    bySkill = new List<PcSkillBookEntry>();
                    _bySkill[e.teachesSkillId] = bySkill;
                }
                bySkill.Add(e);
            }
            if (!_byType.TryGetValue(e.bookType, out var byType))
            {
                byType = new List<PcSkillBookEntry>();
                _byType[e.bookType] = byType;
            }
            byType.Add(e);
        }

        public PcSkillBookEntry Get(int bookId)
            => _byId.TryGetValue(bookId, out var v) ? v : null;

        public IReadOnlyList<PcSkillBookEntry> GetBySkill(int skillId)
            => _bySkill.TryGetValue(skillId, out var v) ? v : (IReadOnlyList<PcSkillBookEntry>)System.Array.Empty<PcSkillBookEntry>();

        public IReadOnlyList<PcSkillBookEntry> GetByType(int type)
            => _byType.TryGetValue(type, out var v) ? v : (IReadOnlyList<PcSkillBookEntry>)System.Array.Empty<PcSkillBookEntry>();

        public IReadOnlyList<PcSkillBookEntry> All
            => new List<PcSkillBookEntry>(_byId.Values);
    }
}
