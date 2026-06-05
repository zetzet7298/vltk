// -----------------------------------------------------------------------------
// VLTK Mobile — ST-3 Skill Mastery Service
// Source: PC settings/skillmastery.txt. Tinh thông kỹ năng theo môn phái + thể loại.
// Vietnamese: "Tinh Thông", "Kiếm", "Đao", "Côn", "Cung", "Trảo", "Quyền",
//             "Song", "Ẩn", "Độc", "Đặc Biệt", "Điểm Thưởng", "Tối Đa".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SkillMasteryService
    {
        public const string LogTag = "SkillMastery";
        public const string DefaultStreamingDir = "Reference/PcSkill";

        private PcSkillMasteryRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public SkillMasteryService() { }
        public SkillMasteryService(PcSkillMasteryRegistry reg) { AttachRegistry(reg); }

        public void AttachRegistry(PcSkillMasteryRegistry reg)
        {
            _registry = reg ?? new PcSkillMasteryRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} quy tắc tinh thông");
        }

        public PcSkillMasteryEntry GetMastery(int masteryId)
            => _registry != null ? _registry.Get(masteryId) : null;

        public IReadOnlyList<PcSkillMasteryEntry> GetByClass(int cls)
            => _registry != null ? _registry.GetByClass(cls) : System.Array.Empty<PcSkillMasteryEntry>();

        public IReadOnlyList<PcSkillMasteryEntry> GetByGenre(int genre)
            => _registry != null ? _registry.GetByGenre(genre) : System.Array.Empty<PcSkillMasteryEntry>();

        public int ComputeBonus(int masteryId, int points)
        {
            var e = GetMastery(masteryId);
            if (e == null) return 0;
            if (points <= 0) return 0;
            if (e.maxPoints > 0 && points > e.maxPoints) points = e.maxPoints;
            return e.bonusValue * points;
        }

        public int GetMaxPoints(int masteryId)
        {
            var e = GetMastery(masteryId);
            return e != null ? e.maxPoints : 0;
        }

        public static string GetGenreName(int genre)
        {
            switch (genre)
            {
                case 0: return "Kiếm";
                case 1: return "Đao";
                case 2: return "Côn";
                case 3: return "Cung";
                case 4: return "Trảo";
                case 5: return "Quyền";
                case 6: return "Song";
                case 7: return "Ẩn";
                case 8: return "Độc";
                case 9: return "Đặc biệt";
                default: return "Không rõ";
            }
        }

        public static SkillMasteryService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new SkillMasteryService();
            if (Directory.Exists(dir))
            {
                var reg = PcSkillMasteryParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại: {dir}");
            }
            return svc;
        }
    }
}
