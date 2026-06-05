// -----------------------------------------------------------------------------
// VLTK Mobile — ST-3 Skill Book Service
// Source: PC settings/skillbook.txt. Quản lý sách kỹ năng (học kỹ năng mới qua sách).
// Vietnamese: "Sách Kỹ Năng", "Sơ Cấp", "Cao Cấp", "Đại Sư", "Thiên Cấp",
//             "Học", "Sử Dụng", "Yêu Cầu".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SkillBookService
    {
        public const string LogTag = "SkillBook";
        public const string DefaultStreamingDir = "Reference/PcSkill";

        private PcSkillBookRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public SkillBookService() { }
        public SkillBookService(PcSkillBookRegistry reg) { AttachRegistry(reg); }

        public void AttachRegistry(PcSkillBookRegistry reg)
        {
            _registry = reg ?? new PcSkillBookRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} sách kỹ năng");
        }

        public PcSkillBookEntry GetBook(int bookId)
            => _registry != null ? _registry.Get(bookId) : null;

        public IReadOnlyList<PcSkillBookEntry> GetBySkill(int skillId)
            => _registry != null ? _registry.GetBySkill(skillId) : System.Array.Empty<PcSkillBookEntry>();

        public IReadOnlyList<PcSkillBookEntry> GetBooksForType(int type)
            => _registry != null ? _registry.GetByType(type) : System.Array.Empty<PcSkillBookEntry>();

        public bool CanUseBook(int bookId, int playerLevel)
        {
            var e = GetBook(bookId);
            return e != null && playerLevel >= e.requiredLevel;
        }

        /// <summary>
        /// Thử dùng sách. Trả về skillId mới học được, hoặc 0 nếu thất bại.
        /// Không thêm vào knownSkills nếu đã biết.
        /// </summary>
        public int TryUseBook(int bookId, int playerLevel, HashSet<int> knownSkills)
        {
            var e = GetBook(bookId);
            if (e == null) return 0;
            if (playerLevel < e.requiredLevel) return 0;
            if (e.teachesSkillId <= 0) return 0;
            if (knownSkills != null && knownSkills.Contains(e.teachesSkillId)) return 0;
            if (knownSkills != null) knownSkills.Add(e.teachesSkillId);
            return e.teachesSkillId;
        }

        public static string GetBookTypeName(int type)
        {
            switch (type)
            {
                case 0: return "Sơ cấp";
                case 1: return "Cao cấp";
                case 2: return "Đại sư";
                case 3: return "Thiên cấp";
                default: return "Không rõ";
            }
        }

        public static SkillBookService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new SkillBookService();
            if (Directory.Exists(dir))
            {
                var reg = PcSkillBookParser.BuildRegistry(dir);
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
