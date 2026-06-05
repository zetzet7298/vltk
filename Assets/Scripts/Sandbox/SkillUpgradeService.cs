// -----------------------------------------------------------------------------
// VLTK Mobile — ST-3 Skill Upgrade Service
// Source: PC settings/skillupgrade.txt. Quản lý nâng cấp kỹ năng (tăng cường,
// tiến hóa, siêu việt) với các tiền điều kiện: cấp, điểm kỹ năng, danh vọng.
// Vietnamese: "Nâng Cấp", "Tăng Cường", "Tiến Hóa", "Siêu Việt",
//             "Yêu Cầu", "Cấp", "Điểm Kỹ Năng", "Danh Vọng".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SkillUpgradeService
    {
        public const string LogTag = "SkillUpgrade";
        public const string DefaultStreamingDir = "Reference/PcSkill";

        private PcSkillUpgradeRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public SkillUpgradeService() { }
        public SkillUpgradeService(PcSkillUpgradeRegistry reg) { AttachRegistry(reg); }

        public void AttachRegistry(PcSkillUpgradeRegistry reg)
        {
            _registry = reg ?? new PcSkillUpgradeRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} quy tắc nâng cấp kỹ năng");
        }

        public PcSkillUpgradeEntry GetUpgrade(int skillId)
            => _registry != null ? _registry.Get(skillId) : null;

        public IReadOnlyList<PcSkillUpgradeEntry> GetByRequiredSkill(int skillId)
            => _registry != null ? _registry.GetByRequiredSkill(skillId) : System.Array.Empty<PcSkillUpgradeEntry>();

        public bool CanUpgrade(int skillId, int playerLevel, int skillPoints, int reputation, HashSet<int> learnedSkills)
        {
            var e = GetUpgrade(skillId);
            if (e == null) return false;
            if (playerLevel < e.requiredPlayerLevel) return false;
            if (skillPoints < e.requiredSkillPoints) return false;
            if (reputation < e.requiredReputation) return false;
            if (e.requiredPrevSkill > 0 && learnedSkills != null && !learnedSkills.Contains(e.requiredPrevSkill))
                return false;
            return true;
        }

        /// <summary>
        /// Thử nâng cấp kỹ năng. Trả về skillId kết quả, hoặc 0 nếu thất bại.
        /// Tiêu hao điểm kỹ năng qua tham chiếu ref.
        /// </summary>
        public int TryUpgrade(int skillId, int playerLevel, ref int skillPoints, int reputation, HashSet<int> learnedSkills)
        {
            if (!CanUpgrade(skillId, playerLevel, skillPoints, reputation, learnedSkills))
                return 0;
            var e = GetUpgrade(skillId);
            skillPoints -= e.requiredSkillPoints;
            if (learnedSkills != null && e.resultSkillId > 0)
                learnedSkills.Add(e.resultSkillId);
            return e.resultSkillId;
        }

        public int GetNextSkillInChain(int skillId)
        {
            var e = GetUpgrade(skillId);
            return e != null ? e.resultSkillId : 0;
        }

        public static SkillUpgradeService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new SkillUpgradeService();
            if (Directory.Exists(dir))
            {
                var reg = PcSkillUpgradeParser.BuildRegistry(dir);
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
