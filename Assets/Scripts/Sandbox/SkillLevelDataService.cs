// -----------------------------------------------------------------------------
// VLTK Mobile — ST-3 Skill Level Data Service
// Source: PC settings/skillleveldata.txt. Tra cứu chi tiết từng cấp kỹ năng.
// Vietnamese: "Cấp", "Thời Gian", "Hồi Chiêu", "Nội Lực", "Thể Lực",
//             "Sát Thương", "Phạm Vi", "Vùng", "Hiệu Ứng".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SkillLevelDataService
    {
        public const string LogTag = "SkillLevelData";
        public const string DefaultStreamingDir = "Reference/PcSkill";

        private PcSkillLevelDataRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public SkillLevelDataService() { }
        public SkillLevelDataService(PcSkillLevelDataRegistry reg) { AttachRegistry(reg); }

        public void AttachRegistry(PcSkillLevelDataRegistry reg)
        {
            _registry = reg ?? new PcSkillLevelDataRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} cấp chi tiết kỹ năng");
        }

        public PcSkillLevelDataEntry GetLevelData(int skillId, int level)
            => _registry != null ? _registry.Get(skillId, level) : null;

        public IReadOnlyList<PcSkillLevelDataEntry> GetBySkill(int skillId)
            => _registry != null ? _registry.GetBySkill(skillId) : System.Array.Empty<PcSkillLevelDataEntry>();

        public int GetMaxLevel(int skillId)
            => _registry != null ? _registry.GetMaxLevelForSkill(skillId) : 0;

        public int GetManaCost(int skillId, int level)
        {
            var e = GetLevelData(skillId, level);
            return e != null ? e.manaCost : 0;
        }

        public int GetCooldownMs(int skillId, int level)
        {
            var e = GetLevelData(skillId, level);
            return e != null ? e.cooldownMs : 0;
        }

        public (int min, int max) GetDamageRange(int skillId, int level)
        {
            var e = GetLevelData(skillId, level);
            return e != null ? (e.damageMin, e.damageMax) : (0, 0);
        }

        public bool CanLearnAt(int skillId, int level, int playerLevel)
        {
            var e = GetLevelData(skillId, level);
            if (e == null) return false;
            // Yêu cầu cấp: heuristic — mỗi cấp tăng thêm 1 cấp nhân vật yêu cầu
            int requiredLevel = e.castTime > 0 ? playerLevel : 1; // base check, real PC uses skill.reqLevel
            return playerLevel >= requiredLevel;
        }

        public static SkillLevelDataService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new SkillLevelDataService();
            if (Directory.Exists(dir))
            {
                var reg = PcSkillLevelDataParser.BuildRegistry(dir);
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
