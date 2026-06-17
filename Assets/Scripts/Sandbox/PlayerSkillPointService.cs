// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.2 Player Skill Point Service
// Manages skill level allocation, validation, and resets using PlayerLevelService.
// Source: PC JX skill allocations, level limits based on player level.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý phân bổ điểm kỹ năng môn phái.
    /// Đảm bảo logic nâng kỹ năng tuân thủ giới hạn của PC JX1.
    /// </summary>
    public class PlayerSkillPointService
    {
        private readonly PlayerProgressionState _progression;
        private readonly PlayerLevelService _levelService;
        private readonly SkillCatalog _catalog;

        public PlayerSkillPointService(PlayerProgressionState progression, PlayerLevelService levelService, SkillCatalog catalog)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>
        /// Nâng cấp kỹ năng thêm 1 cấp.
        /// Tiêu hao 1 điểm kỹ năng từ PlayerLevelService.
        /// </summary>
        public bool UpgradeSkill(int skillId)
        {
            var skill = _catalog.Resolve(skillId);
            if (skill == null) return false;

            var ruleCatalog = SkillLevelUpScriptCatalog.CreateDefault();
            var rule = ruleCatalog.Resolve(skillId) ?? ruleCatalog.ResolveScript(skill.levelUpScript);
            bool usesTranslifePool = rule != null && rule.usesTranslife4PointPool;

            // Kiểm tra xem nhân vật có điểm kỹ năng không
            if (!usesTranslifePool && _levelService.SkillPoints < 1)
            {
                SubsystemLog.Warn("SkillPoint", "Not enough skill points to upgrade.");
                return false;
            }

            // Đồng bộ cấp độ nhân vật và điểm kỹ năng vào progression state để tính level cap chính xác
            _progression.level = _levelService.Level;
            if (!usesTranslifePool)
                _progression.fightSkillPoints = _levelService.SkillPoints;

            // Kiểm tra điều kiện nâng cấp trong progression state
            if (!_progression.CanUpgradeSkill(skill, ruleCatalog, 1))
            {
                SubsystemLog.Warn("SkillPoint", $"Cannot upgrade skill {skill.DisplayName} (id={skillId})");
                return false;
            }

            // Thực hiện nâng cấp và trừ điểm
            if (_progression.TryUpgradeSkill(skill, ruleCatalog, 1))
            {
                if (!usesTranslifePool)
                {
                    _levelService.SpendSkillPoints(1);
                    _progression.fightSkillPoints = _levelService.SkillPoints;
                }
                SubsystemLog.Info("SkillPoint", $"Upgraded skill {skill.DisplayName} to level {_progression.GetSkillLevel(skillId)}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset toàn bộ điểm kỹ năng đã nâng và hoàn trả lại cho PlayerLevelService.
        /// </summary>
        public void ResetSkills()
        {
            int totalRefunded = 0;

            // Thu thập toàn bộ kỹ năng đã nâng
            var keys = new List<int>(_progression.skillLevels.Keys);
            foreach (var key in keys)
            {
                int currentLevel = _progression.skillLevels[key];
                if (currentLevel > 0)
                {
                    totalRefunded += currentLevel;
                    _progression.skillLevels[key] = 0;
                }
            }

            _levelService.RefundSkillPoints(totalRefunded);
            _progression.fightSkillPoints = _levelService.SkillPoints;
            SubsystemLog.Info("SkillPoint", $"Reset all skills. Refunded {totalRefunded} skill points.");
        }

        /// <summary>
        /// Đồng bộ kỹ năng môn phái khi gia nhập môn phái mới.
        /// </summary>
        public void JoinFaction(int factionId)
        {
            _progression.level = _levelService.Level;
            _progression.fightSkillPoints = _levelService.SkillPoints;
            _progression.faction = (CombatFaction)factionId;
            _progression.knownSkills.Clear();

            foreach (var skill in _catalog.All)
            {
                // Lọc skill theo faction ngoại trừ boss skill
                if ((int)skill.faction == factionId && !PlayerProgressionState.IsNpcVariant(skill.skillId))
                {
                    _progression.knownSkills.Add(skill.skillId);
                    if (!_progression.skillLevels.ContainsKey(skill.skillId))
                        _progression.skillLevels[skill.skillId] = 0;
                }
            }
            SubsystemLog.Info("SkillPoint", $"Joined faction {factionId}, registered {_progression.knownSkills.Count} skills.");
        }
    }
}
