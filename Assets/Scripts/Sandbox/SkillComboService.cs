// -----------------------------------------------------------------------------
// VLTK Mobile — ST-3 Skill Combo Service
// Source: PC settings/skillcombo.txt. Quản lý chuỗi kỹ năng (combo).
// Vietnamese: "Chuỗi Kỹ Năng", "Combo", "Trình Tự", "Yêu Cầu",
//             "Môn Phái", "Cấp", "Hiệu Ứng Thưởng".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SkillComboService
    {
        public const string LogTag = "SkillCombo";
        public const string DefaultStreamingDir = "Reference/PcSkill";

        private PcSkillComboRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public SkillComboService() { }
        public SkillComboService(PcSkillComboRegistry reg) { AttachRegistry(reg); }

        public void AttachRegistry(PcSkillComboRegistry reg)
        {
            _registry = reg ?? new PcSkillComboRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} chuỗi kỹ năng");
        }

        public PcSkillComboEntry GetCombo(int comboId)
            => _registry != null ? _registry.Get(comboId) : null;

        public IReadOnlyList<PcSkillComboEntry> GetByClass(int cls)
            => _registry != null ? _registry.GetByClass(cls) : System.Array.Empty<PcSkillComboEntry>();

        public IReadOnlyList<PcSkillComboEntry> GetByLevel(int level)
            => _registry != null ? _registry.GetByLevel(level) : System.Array.Empty<PcSkillComboEntry>();

        /// <summary>
        /// Kiểm tra xem combo có thể kích hoạt với trình tự kỹ năng gần đây.
        /// recentlyUsedSkills là danh sách skillId vừa dùng (thứ tự thời gian).
        /// </summary>
        public bool CanExecuteCombo(int comboId, int playerClass, int playerLevel, List<int> recentlyUsedSkills)
        {
            var e = GetCombo(comboId);
            if (e == null) return false;
            if (playerLevel < e.requiredPlayerLevel) return false;
            if (e.requiredClass > 0 && e.requiredClass != playerClass) return false;
            var seq = e.GetSkillSequence();
            if (seq.Length == 0) return false;
            if (recentlyUsedSkills == null || recentlyUsedSkills.Count < seq.Length) return false;
            // Check last N skills match sequence in order
            int startIdx = recentlyUsedSkills.Count - seq.Length;
            for (int i = 0; i < seq.Length; i++)
            {
                if (recentlyUsedSkills[startIdx + i] != seq[i]) return false;
            }
            return true;
        }

        public int GetBonusEffect(int comboId)
        {
            var e = GetCombo(comboId);
            return e != null ? e.bonusEffect : 0;
        }

        public int GetNextSkillInCombo(int comboId)
        {
            var e = GetCombo(comboId);
            if (e == null) return 0;
            var seq = e.GetSkillSequence();
            return seq.Length > 0 ? seq[0] : 0;
        }

        public static SkillComboService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new SkillComboService();
            if (Directory.Exists(dir))
            {
                var reg = PcSkillComboParser.BuildRegistry(dir);
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
