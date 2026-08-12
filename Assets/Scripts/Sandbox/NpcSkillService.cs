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
        private INpcSkillServiceHost _host;

        public int Count => _registry != null ? _registry.Count : 0;

        public NpcSkillService() : this(null) { }

        public NpcSkillService(PcNpcSkillRegistry registry)
        {
            RegisterRegistry(registry);
        }

        public void AttachHost(INpcSkillServiceHost host) { _host = host; }

        public void RegisterRegistry(PcNpcSkillRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Kỹ Năng Quái loaded: {Count} skill quái");
            if (_host != null)
            {
                _host.OnNpcSkillRegistryAttached(Count);
                _host.LogNpcSkillEvent("load", 0, $"Loaded {Count} NPC skills");
                _host.PlayNpcSkillSFX("load", 0);
                _host.SaveNpcSkillState(0, 0, 0);
            }
        }

        public PcNpcSkillEntry GetNpcSkill(int id)
        {
            var s = _registry != null ? _registry.Get(id) : null;
            if (_host != null)
            {
                if (s != null)
                    _host.OnNpcSkillResolved(s.skillId, s.nameRaw, s.skillStyle, s.attackRadius);
                else
                    _host.LogNpcSkillEvent("query_missing", id, "NPC skill not found in registry");
            }
            return s;
        }

        public NpcBossSkillCastPlan BuildCastPlan(int skillId)
        {
            var entry = GetNpcSkill(skillId);
            if (entry == null)
            {
                if (_host != null)
                {
                    _host.OnCastPlanMissingSkill(skillId, "missing npcskills.txt row");
                    _host.LogNpcSkillEvent("cast_plan_missing", skillId, "No registry entry");
                }
                return new NpcBossSkillCastPlan { skillId = skillId, guardReason = "missing npcskills.txt row" };
            }
            bool missingScript = !string.IsNullOrEmpty(entry.levelSetScript)
                && !NpcSkillScriptCatalogService.PcScriptFileExists(NpcSkillScriptCatalogService.PcServerScriptRoot, entry.levelSetScript);
            var plan = new NpcBossSkillCastPlan
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
            if (_host != null)
            {
                _host.OnCastPlanBuilt(plan.skillId, plan.canCast, plan.missingScriptGuard, plan.guardReason);
                _host.LogNpcSkillEvent("cast_plan_built", plan.skillId, plan.canCast ? "ok" : "blocked");
                _host.PlayNpcSkillSFX(plan.canCast ? "ready" : "guard", plan.skillId);
            }
            return plan;
        }

        public IReadOnlyList<PcNpcSkillEntry> GetByNpcTemplate(int templateId)
        {
            var list = _registry != null
                ? _registry.GetByNpcTemplate(templateId)
                : (IReadOnlyList<PcNpcSkillEntry>)Array.Empty<PcNpcSkillEntry>();
            if (_host != null)
                _host.OnNpcTemplateSkillsQueried(templateId, list.Count);
            return list;
        }

        // ── AI dispatch (called by NPC AI code) ─────────────
        public void CastSkill(int skillId, int casterTemplateId, int targetTemplateId)
        {
            var s = GetNpcSkill(skillId);
            if (s == null) return;
            if (_host != null)
            {
                _host.OnNpcCastSkill(s.skillId, casterTemplateId, targetTemplateId);
                _host.ShowNpcSkillUI(s.skillId, s.nameRaw, s.skillStyle);
                _host.LogNpcSkillEvent("cast", s.skillId, $"NPC {casterTemplateId} cast on {targetTemplateId}");
                _host.PlayNpcSkillSFX("cast", s.skillId);
                _host.SaveNpcSkillState(s.skillId, casterTemplateId, s.timePerCast);
            }
        }

        public void CompleteCast(int skillId, int casterTemplateId, bool success)
        {
            var s = GetNpcSkill(skillId);
            if (s == null) return;
            if (_host != null)
            {
                _host.OnNpcCastCompleted(s.skillId, casterTemplateId, success);
                _host.LogNpcSkillEvent(success ? "complete" : "interrupt", s.skillId, $"NPC {casterTemplateId} {(success ? "thành công" : "gián đoạn")}");
                _host.PlayNpcSkillSFX(success ? "complete" : "interrupt", s.skillId);
                _host.SaveNpcSkillState(s.skillId, casterTemplateId, 0);
            }
        }

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
