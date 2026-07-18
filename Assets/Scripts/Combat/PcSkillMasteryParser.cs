// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/skillmastery.txt Skill Mastery parser
// Source: skillmastery.txt (Reference/PcSkill, tab-separated).
//   MasteryId  Name  Class  SkillGenre  BonusType  BonusValue  MaxPoints
// SkillGenre: 0=kiếm, 1=đao, 2=côn, 3=cung, 4=trảo, 5=quyền, 6=song,
//             7=ẩn, 8=độc, 9=đặc biệt.
// Tinh thông kỹ năng — phân theo môn phái + thể loại vũ khí/đòn đánh,
// mỗi điểm đầu tư cộng thêm BonusValue vào chỉ số (BonusType).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkillMasteryParser
    {
        public const int MasteryIdCol = 0;
        public const int NameCol = 1;
        public const int ClassCol = 2;
        public const int SkillGenreCol = 3;
        public const int BonusTypeCol = 4;
        public const int BonusValueCol = 5;
        public const int MaxPointsCol = 6;

        public static List<PcSkillMasteryEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillMasteryEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int id = PcItemCommon.Int(cols, MasteryIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSkillMasteryEntry
                {
                    masteryId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    classId = PcItemCommon.Int(cols, ClassCol),
                    skillGenre = PcItemCommon.Int(cols, SkillGenreCol),
                    bonusType = cols.Length > BonusTypeCol ? PcItemCommon.Int(cols, BonusTypeCol) : 0,
                    bonusValue = cols.Length > BonusValueCol ? PcItemCommon.Int(cols, BonusValueCol) : 0,
                    maxPoints = cols.Length > MaxPointsCol ? PcItemCommon.Int(cols, MaxPointsCol) : 0,
                });
            }
            return rows;
        }

        public static PcSkillMasteryRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillMasteryRegistry();
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
    public class PcSkillMasteryEntry
    {
        public int masteryId;
        public string nameRaw;
        public int classId;
        public int skillGenre;
        public int bonusType;
        public int bonusValue;
        public int maxPoints;
    }

    public sealed class PcSkillMasteryRegistry
    {
        private readonly Dictionary<int, PcSkillMasteryEntry> _byId = new();
        private readonly Dictionary<int, List<PcSkillMasteryEntry>> _byClass = new();
        private readonly Dictionary<int, List<PcSkillMasteryEntry>> _byGenre = new();
        public int Count => _byId.Count;

        public void Register(PcSkillMasteryEntry e)
        {
            if (e == null || e.masteryId <= 0) return;
            _byId[e.masteryId] = e;
            if (e.classId > 0)
            {
                if (!_byClass.TryGetValue(e.classId, out var byClass))
                {
                    byClass = new List<PcSkillMasteryEntry>();
                    _byClass[e.classId] = byClass;
                }
                byClass.Add(e);
            }
            if (e.skillGenre >= 0)
            {
                if (!_byGenre.TryGetValue(e.skillGenre, out var byGenre))
                {
                    byGenre = new List<PcSkillMasteryEntry>();
                    _byGenre[e.skillGenre] = byGenre;
                }
                byGenre.Add(e);
            }
        }

        public PcSkillMasteryEntry Get(int masteryId)
            => _byId.TryGetValue(masteryId, out var v) ? v : null;

        public IReadOnlyList<PcSkillMasteryEntry> GetByClass(int cls)
            => _byClass.TryGetValue(cls, out var v) ? v : (IReadOnlyList<PcSkillMasteryEntry>)System.Array.Empty<PcSkillMasteryEntry>();

        public IReadOnlyList<PcSkillMasteryEntry> GetByGenre(int genre)
            => _byGenre.TryGetValue(genre, out var v) ? v : (IReadOnlyList<PcSkillMasteryEntry>)System.Array.Empty<PcSkillMasteryEntry>();

        public IReadOnlyList<PcSkillMasteryEntry> All
            => new List<PcSkillMasteryEntry>(_byId.Values);
    }
}
