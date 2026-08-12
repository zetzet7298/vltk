// -----------------------------------------------------------------------------
// VLTK Mobile — NpcSkillService host dispatch tests
// PC source: settings/npcskills.txt — Kỹ Năng Quái / Boss Skill.
// Verifies INpcSkillServiceHost receives expected events for load / query /
// cast-plan build / AI cast dispatch.
// -----------------------------------------------------------------------------

using System;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class NpcSkillServiceHostServiceTests
    {
        private sealed class FakeHost : INpcSkillServiceHost
        {
            public int RegistryAttachedCalls;
            public int LastRegistrySkillCount;

            public int ResolvedCalls;
            public int LastResolvedSkillId;
            public string LastResolvedNameRaw;
            public int LastResolvedSkillStyle;
            public int LastResolvedAttackRadius;

            public int TemplateSkillsQueriedCalls;
            public int LastTemplateId;
            public int LastTemplateResultCount;

            public int CastPlanBuiltCalls;
            public int LastCastPlanSkillId;
            public bool LastCastPlanCanCast;
            public bool LastCastPlanMissingScriptGuard;
            public string LastCastPlanGuardReason;

            public int CastPlanMissingCalls;
            public int LastCastPlanMissingSkillId;

            public int CastSkillCalls;
            public int LastCastSkillId;
            public int LastCastCasterTemplateId;
            public int LastCastTargetTemplateId;

            public int CastCompletedCalls;
            public int LastCastCompletedSkillId;
            public int LastCastCompletedCaster;
            public bool LastCastCompletedSuccess;

            public int UIShowCalls;
            public int LastUISkillId;
            public string LastUINameRaw;
            public int LastUISkillStyle;

            public int LogCalls;
            public int LastLogSkillId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXSkillId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveSkillId;
            public int LastSaveCasterTemplateId;
            public int LastSaveCooldownTicks;

            public void OnNpcSkillRegistryAttached(int skillCount)
            {
                RegistryAttachedCalls++;
                LastRegistrySkillCount = skillCount;
            }
            public void OnNpcSkillResolved(int skillId, string nameRaw, int skillStyle, int attackRadius)
            {
                ResolvedCalls++;
                LastResolvedSkillId = skillId;
                LastResolvedNameRaw = nameRaw;
                LastResolvedSkillStyle = skillStyle;
                LastResolvedAttackRadius = attackRadius;
            }
            public void OnNpcTemplateSkillsQueried(int templateId, int resultCount)
            {
                TemplateSkillsQueriedCalls++;
                LastTemplateId = templateId;
                LastTemplateResultCount = resultCount;
            }
            public void OnCastPlanBuilt(int skillId, bool canCast, bool missingScriptGuard, string guardReasonVi)
            {
                CastPlanBuiltCalls++;
                LastCastPlanSkillId = skillId;
                LastCastPlanCanCast = canCast;
                LastCastPlanMissingScriptGuard = missingScriptGuard;
                LastCastPlanGuardReason = guardReasonVi;
            }
            public void OnCastPlanMissingSkill(int skillId, string reasonVi)
            {
                CastPlanMissingCalls++;
                LastCastPlanMissingSkillId = skillId;
            }
            public void OnNpcCastSkill(int skillId, int casterTemplateId, int targetTemplateId)
            {
                CastSkillCalls++;
                LastCastSkillId = skillId;
                LastCastCasterTemplateId = casterTemplateId;
                LastCastTargetTemplateId = targetTemplateId;
            }
            public void OnNpcCastCompleted(int skillId, int casterTemplateId, bool success)
            {
                CastCompletedCalls++;
                LastCastCompletedSkillId = skillId;
                LastCastCompletedCaster = casterTemplateId;
                LastCastCompletedSuccess = success;
            }
            public void ShowNpcSkillUI(int skillId, string nameRaw, int skillStyle)
            {
                UIShowCalls++;
                LastUISkillId = skillId;
                LastUINameRaw = nameRaw;
                LastUISkillStyle = skillStyle;
            }
            public void LogNpcSkillEvent(string eventType, int skillId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogSkillId = skillId;
                LastLogDetail = detailVi;
            }
            public void PlayNpcSkillSFX(string action, int skillId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXSkillId = skillId;
            }
            public void SaveNpcSkillState(int skillId, int casterTemplateId, int cooldownTicks)
            {
                SaveCalls++;
                LastSaveSkillId = skillId;
                LastSaveCasterTemplateId = casterTemplateId;
                LastSaveCooldownTicks = cooldownTicks;
            }
        }

        private static (PcNpcSkillRegistry reg, PcNpcSkillEntry e1, PcNpcSkillEntry e2) MakeRegistry()
        {
            var reg = new PcNpcSkillRegistry();
            var e1 = new PcNpcSkillEntry
            {
                skillId = 1000, nameRaw = "Bach Van Phi Attack",
                skillStyle = 1, attackRadius = 200, timePerCast = 30,
                npcTemplateId = 1001, isPhysical = true, isMelee = true,
                targetEnemy = true, doHurt = true, maxLevel = 10,
            };
            var e2 = new PcNpcSkillEntry
            {
                skillId = 1001, nameRaw = "Xich Diem Fire",
                skillStyle = 2, attackRadius = 400, timePerCast = 60,
                npcTemplateId = 2001, isPhysical = false, isMelee = false,
                targetEnemy = true, doHurt = true, maxLevel = 10,
            };
            reg.Register(e1);
            reg.Register(e2);
            return (reg, e1, e2);
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_Empty()
        {
            var svc = new NpcSkillService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new NpcSkillService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── RegisterRegistry dispatch ──────────────────────────────────────
        [Test]
        public void RegisterRegistry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new NpcSkillService();
            svc.AttachHost(host);
            var (reg, _, _) = MakeRegistry();
            int baseline = host.RegistryAttachedCalls;
            svc.RegisterRegistry(reg);
            Assert.AreEqual(baseline + 1, host.RegistryAttachedCalls);
            Assert.AreEqual(2, host.LastRegistrySkillCount);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual("load", host.LastSFXAction);
        }

        // ── GetNpcSkill dispatch ───────────────────────────────────────────
        [Test]
        public void GetNpcSkill_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new NpcSkillService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            var s = svc.GetNpcSkill(1000);
            Assert.IsNotNull(s);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(1000, host.LastResolvedSkillId);
            Assert.AreEqual("Bach Van Phi Attack", host.LastResolvedNameRaw);
            Assert.AreEqual(1, host.LastResolvedSkillStyle);
            Assert.AreEqual(200, host.LastResolvedAttackRadius);
        }

        [Test]
        public void GetNpcSkill_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new NpcSkillService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var s = svc.GetNpcSkill(9999);
            Assert.IsNull(s);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── BuildCastPlan dispatch ─────────────────────────────────────────
        [Test]
        public void BuildCastPlan_KnownSkill_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new NpcSkillService(reg);
            svc.AttachHost(host);
            int baselineCastPlan = host.CastPlanBuiltCalls;
            int baselineLog = host.LogCalls;
            int baselineSFX = host.SFXCalls;
            var plan = svc.BuildCastPlan(1000);
            Assert.IsNotNull(plan);
            Assert.IsTrue(plan.canCast);
            Assert.AreEqual(baselineCastPlan + 1, host.CastPlanBuiltCalls);
            Assert.AreEqual(1000, host.LastCastPlanSkillId);
            Assert.IsTrue(host.LastCastPlanCanCast);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("cast_plan_built", host.LastLogEventType);
            Assert.AreEqual(baselineSFX + 1, host.SFXCalls);
            Assert.AreEqual("ready", host.LastSFXAction);
        }

        [Test]
        public void BuildCastPlan_MissingSkill_DispatchesMissing()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new NpcSkillService(reg);
            svc.AttachHost(host);
            var plan = svc.BuildCastPlan(9999);
            Assert.IsNotNull(plan);
            Assert.AreEqual(9999, plan.skillId);
            Assert.IsFalse(plan.canCast);
            Assert.AreEqual(1, host.CastPlanMissingCalls);
            Assert.AreEqual(9999, host.LastCastPlanMissingSkillId);
        }

        // ── GetByNpcTemplate dispatch ──────────────────────────────────────
        [Test]
        public void GetByNpcTemplate_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new NpcSkillService(reg);
            svc.AttachHost(host);
            var list = svc.GetByNpcTemplate(1001);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, host.TemplateSkillsQueriedCalls);
            Assert.AreEqual(1001, host.LastTemplateId);
            Assert.AreEqual(1, host.LastTemplateResultCount);
        }

        [Test]
        public void GetByNpcTemplate_NoRegistry_DispatchesZero()
        {
            var host = new FakeHost();
            var svc = new NpcSkillService();
            svc.AttachHost(host);
            var list = svc.GetByNpcTemplate(1001);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(1, host.TemplateSkillsQueriedCalls);
            Assert.AreEqual(0, host.LastTemplateResultCount);
        }

        // ── AI Cast dispatch ───────────────────────────────────────────────
        [Test]
        public void CastSkill_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new NpcSkillService(reg);
            svc.AttachHost(host);
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            svc.CastSkill(1000, 1001, 2001);
            Assert.AreEqual(1, host.CastSkillCalls);
            Assert.AreEqual(1000, host.LastCastSkillId);
            Assert.AreEqual(1001, host.LastCastCasterTemplateId);
            Assert.AreEqual(2001, host.LastCastTargetTemplateId);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("cast", host.LastLogEventType);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("cast", host.LastSFXAction);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual(30, host.LastSaveCooldownTicks);
        }

        [Test]
        public void CastSkill_UnknownSkill_NoDispatch()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new NpcSkillService(reg);
            svc.AttachHost(host);
            int baseline = host.CastSkillCalls;
            svc.CastSkill(9999, 1, 2);
            Assert.AreEqual(baseline, host.CastSkillCalls);
        }

        [Test]
        public void CompleteCast_Success_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new NpcSkillService(reg);
            svc.AttachHost(host);
            svc.CompleteCast(1000, 1001, true);
            Assert.AreEqual(1, host.CastCompletedCalls);
            Assert.AreEqual(1000, host.LastCastCompletedSkillId);
            Assert.AreEqual(1001, host.LastCastCompletedCaster);
            Assert.IsTrue(host.LastCastCompletedSuccess);
            Assert.AreEqual("complete", host.LastLogEventType);
            Assert.AreEqual("complete", host.LastSFXAction);
        }

        [Test]
        public void CompleteCast_Interrupt_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new NpcSkillService(reg);
            svc.AttachHost(host);
            svc.CompleteCast(1000, 1001, false);
            Assert.IsFalse(host.LastCastCompletedSuccess);
            Assert.AreEqual("interrupt", host.LastLogEventType);
            Assert.AreEqual("interrupt", host.LastSFXAction);
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new NpcSkillService();
            Assert.DoesNotThrow(() => svc.RegisterRegistry(null));
            Assert.DoesNotThrow(() => svc.GetNpcSkill(1000));
            Assert.DoesNotThrow(() => svc.BuildCastPlan(1000));
            Assert.DoesNotThrow(() => svc.GetByNpcTemplate(1001));
            Assert.DoesNotThrow(() => svc.CastSkill(1000, 1, 2));
            Assert.DoesNotThrow(() => svc.CompleteCast(1000, 1, true));
        }
    }
}
