// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX NPC Skill Service (Kỹ Năng Quái runtime)
// Wraps PcNpcSkillRegistry. PC source: settings/npcskills.txt (43).
// Tra cứu skill theo id / template NPC.
// Vietnamese: "Kỹ Năng Quái", "Boss Skill", "AI Dùng Skill".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý kỹ năng quái / boss.
    /// PC source: settings/npcskills.txt.
    /// </summary>
    public class NpcSkillService
    {
        public const string LogTag = "NpcSkill";

        private PcNpcSkillRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public NpcSkillService() : this(null) { }

        public NpcSkillService(PcNpcSkillRegistry registry)
        {
            RegisterRegistry(registry);
        }

        public void RegisterRegistry(PcNpcSkillRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Kỹ Năng Quái loaded: {Count} skill quái");
        }

        public PcNpcSkillEntry GetNpcSkill(int id)
            => _registry != null ? _registry.Get(id) : null;

        public NpcBossSkillCastPlan BuildCastPlan(int skillId)
        {
            var entry = GetNpcSkill(skillId);
            if (entry == null) return new NpcBossSkillCastPlan { skillId = skillId, guardReason = "missing npcskills.txt row" };
            bool missingScript = !string.IsNullOrEmpty(entry.levelSetScript)
                && !NpcSkillScriptCatalogService.PcScriptFileExists(NpcSkillScriptCatalogService.PcServerScriptRoot, entry.levelSetScript);
            return new NpcBossSkillCastPlan
            {
                canCast = true,
                skillId = entry.skillId,
                skillNameRaw = entry.nameRaw,
                skillStyle = entry.skillStyle,
                attackRadius = entry.attackRadius,
                childSkillId = entry.childSkillId,
                childSkillLevel = entry.childSkillLevel,
                childSkillNum = entry.childSkillNum,
                cooldownTicks = entry.timePerCast,
                skillCostType = entry.skillCostType,
                costValue = entry.costValue,
                isPhysical = entry.isPhysical,
                isMelee = entry.isMelee,
                targetOnly = entry.targetOnly,
                targetEnemy = entry.targetEnemy,
                targetAlly = entry.targetAlly,
                targetSelf = entry.targetSelf,
                targetOther = entry.targetOther,
                targetObj = entry.targetObj,
                targetNoNpc = entry.targetNoNpc,
                horseLimit = entry.horseLimit,
                doHurt = entry.doHurt,
                weaponSkill = entry.weaponSkill,
                maxLevel = entry.maxLevel,
                levelSetScript = entry.levelSetScript,
                missingScriptGuard = missingScript,
                guardReason = missingScript ? $"Referenced PC Lua missing under scoped source: {entry.levelSetScript}" : null,
            };
        }

        public IReadOnlyList<PcNpcSkillEntry> GetByNpcTemplate(int templateId)
            => _registry != null
                ? _registry.GetByNpcTemplate(templateId)
                : (IReadOnlyList<PcNpcSkillEntry>)Array.Empty<PcNpcSkillEntry>();

        public static NpcSkillService LoadFromStreamingAssets(string subdir = "Reference/PcSkill")
        {
            var svc = new NpcSkillService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                var reg = PcNpcSkillParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"NpcSkillService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
