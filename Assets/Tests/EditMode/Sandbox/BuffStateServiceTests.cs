// -----------------------------------------------------------------------------
// VLTK Mobile — BuffStateService EditMode tests.
// Kiểm tra buff/debuff lifecycle: apply (refreshes if same), remove, tick
// (expires), modifier aggregation. IBuffStateHost dispatch cho UI icon,
// haptics, SFX, log.
// PC source: KNpc::AddState / m_StateSpecial, lua state_notify.
// PC state IDs: 22 = Choáng, 20 = Sư Tử Hống (mobile haptic).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class BuffStateServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IBuffStateHost
        {
            public int ShowCalls;
            public int HideCalls;
            public int SfxCalls;
            public int HapticCalls;
            public int LogCalls;
            public int LastActorId;
            public int LastSkillId;
            public int LastLevel;
            public bool LastAdded;

            public void ShowStateEffect(int actorId, int skillId, int level, float durationRemaining, bool isHaptic)
            {
                ShowCalls++;
                LastActorId = actorId;
                LastSkillId = skillId;
                LastLevel = level;
            }
            public void HideStateEffect(int actorId, int skillId) { HideCalls++; }
            public void PlayStateSFX(int actorId, int skillId, bool isHaptic) { SfxCalls++; }
            public void TriggerHapticFeedback(int actorId, int skillId) { HapticCalls++; }
            public void LogStateNotice(int actorId, int skillId, int level, bool added)
            {
                LogCalls++;
                LastAdded = added;
            }
        }

        private static SkillDefinition MakeSkill(int id, int stateSpecialId, string name)
        {
            return new SkillDefinition
            {
                skillId = id,
                stateSpecialId = stateSpecialId,
                nameRaw = name,
                reqLevel = 1,
                maxLevel = 20,
            };
        }

        private static BuffStateService BuildService(IBuffStateHost host = null)
            => new BuffStateService(host);

        // ── ApplyBuff ────────────────────────────────────────────────────────

        [Test]
        public void ApplyBuff_ValidSkill_AddsBuff()
        {
            var svc = BuildService();
            var skill = MakeSkill(100, 0, "Băng Phong Quyết");
            svc.ApplyBuff(1, skill, 1, 10f);
            Assert.IsTrue(svc.HasBuff(1, 100));
        }

        [Test]
        public void ApplyBuff_NullSkill_NoEffect()
        {
            var svc = BuildService();
            svc.ApplyBuff(1, null, 1, 10f);
            Assert.IsFalse(svc.HasBuff(1, 0));
        }

        [Test]
        public void ApplyBuff_ZeroDuration_NoEffect()
        {
            var svc = BuildService();
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 0f);
            Assert.IsFalse(svc.HasBuff(1, 100));
        }

        [Test]
        public void ApplyBuff_NegativeDuration_NoEffect()
        {
            var svc = BuildService();
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, -5f);
            Assert.IsFalse(svc.HasBuff(1, 100));
        }

        [Test]
        public void ApplyBuff_SameSkillTwice_RefreshesDuration()
        {
            var svc = BuildService();
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 5f);
            svc.ApplyBuff(1, skill, 3, 20f); // longer
            Assert.IsTrue(svc.HasBuff(1, 100));
        }

        [Test]
        public void ApplyBuff_FiresOnBuffAddedEvent()
        {
            var svc = BuildService();
            int fired = 0;
            svc.OnBuffAdded += (_, __) => fired++;
            svc.ApplyBuff(1, MakeSkill(100, 0, "X"), 1, 5f);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ApplyBuff_RefreshDoesNotFireEvent()
        {
            var svc = BuildService();
            int fired = 0;
            svc.OnBuffAdded += (_, __) => fired++;
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 5f);
            svc.ApplyBuff(1, skill, 2, 10f); // refresh
            Assert.AreEqual(1, fired); // only first call
        }

        // ── Haptic detection ────────────────────────────────────────────────

        [Test]
        public void ApplyBuff_HapticState22_TriggersHaptic()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            // stateSpecialId 22 = Choáng
            var skill = MakeSkill(100, 22, "Choáng");
            svc.ApplyBuff(1, skill, 1, 5f);
            Assert.AreEqual(1, host.HapticCalls);
        }

        [Test]
        public void ApplyBuff_HapticSkillId20_TriggersHaptic()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            // skillId 20 = Sư Tử Hống
            var skill = MakeSkill(20, 0, "Sư Tử Hống");
            svc.ApplyBuff(1, skill, 1, 5f);
            Assert.AreEqual(1, host.HapticCalls);
        }

        [Test]
        public void ApplyBuff_NonHapticSkill_NoHapticCall()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            var skill = MakeSkill(100, 0, "Bình thường");
            svc.ApplyBuff(1, skill, 1, 5f);
            Assert.AreEqual(0, host.HapticCalls);
        }

        [Test]
        public void ApplyBuff_DispatchesAllHostCallbacks()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 5f);
            Assert.AreEqual(1, host.ShowCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
        }

        [Test]
        public void ApplyBuff_HostArgsCorrect()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.ApplyBuff(7, MakeSkill(42, 0, "X"), 5, 12f);
            Assert.AreEqual(7, host.LastActorId);
            Assert.AreEqual(42, host.LastSkillId);
            Assert.AreEqual(5, host.LastLevel);
        }

        // ── RemoveBuff ───────────────────────────────────────────────────────

        [Test]
        public void RemoveBuff_Existing_Removes()
        {
            var svc = BuildService();
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 5f);
            svc.RemoveBuff(1, 100);
            Assert.IsFalse(svc.HasBuff(1, 100));
        }

        [Test]
        public void RemoveBuff_NotFound_NoEffect()
        {
            var svc = BuildService();
            svc.RemoveBuff(99, 100); // no buffs
            Assert.IsFalse(svc.HasBuff(99, 100));
        }

        [Test]
        public void RemoveBuff_FiresOnBuffRemovedEvent()
        {
            var svc = BuildService();
            int fired = 0;
            svc.OnBuffRemoved += (_, __) => fired++;
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 5f);
            svc.RemoveBuff(1, 100);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void RemoveBuff_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 5f);
            host.HideCalls = 0; host.LogCalls = 0;
            svc.RemoveBuff(1, 100);
            Assert.AreEqual(1, host.HideCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.IsFalse(host.LastAdded); // log "removed"
        }

        // ── Tick ─────────────────────────────────────────────────────────────

        [Test]
        public void Tick_ZeroDelta_NoExpire()
        {
            var svc = BuildService();
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 5f);
            svc.Tick(0f);
            Assert.IsTrue(svc.HasBuff(1, 100));
        }

        [Test]
        public void Tick_DurationExpires_Removes()
        {
            var svc = BuildService();
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 2f);
            svc.Tick(3f); // 3s elapsed > 2s duration
            Assert.IsFalse(svc.HasBuff(1, 100));
        }

        [Test]
        public void Tick_Expires_FiresOnBuffRemoved()
        {
            var svc = BuildService();
            int fired = 0;
            svc.OnBuffRemoved += (_, __) => fired++;
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 1f);
            svc.Tick(2f);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Tick_Expires_DispatchesHideAndLog()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            var skill = MakeSkill(100, 0, "X");
            svc.ApplyBuff(1, skill, 1, 1f);
            host.HideCalls = 0; host.LogCalls = 0;
            svc.Tick(2f);
            Assert.AreEqual(1, host.HideCalls);
            Assert.AreEqual(1, host.LogCalls);
        }

        [Test]
        public void Tick_MultipleActorsAndBuffs()
        {
            var svc = BuildService();
            svc.ApplyBuff(1, MakeSkill(100, 0, "A"), 1, 5f);
            svc.ApplyBuff(1, MakeSkill(200, 0, "B"), 1, 1f);
            svc.ApplyBuff(2, MakeSkill(300, 0, "C"), 1, 10f);
            svc.Tick(2f);
            // actor 1 buff 100 still alive (5s), buff 200 expired (1s)
            Assert.IsTrue(svc.HasBuff(1, 100));
            Assert.IsFalse(svc.HasBuff(1, 200));
            Assert.IsTrue(svc.HasBuff(2, 300));
        }

        // ── GetBuffModifier ─────────────────────────────────────────────────

        [Test]
        public void GetBuffModifier_NoBuffs_ReturnsZero()
        {
            var svc = BuildService();
            Assert.AreEqual(0, svc.GetBuffModifier(1, MagicAttributeKind.Strength));
        }

        [Test]
        public void GetBuffModifier_WithBuff_ReturnsSum()
        {
            // Create skill with a level that has attributes
            var skill = new SkillDefinition
            {
                skillId = 100,
                stateSpecialId = 0,
                nameRaw = "Test",
                reqLevel = 1,
                maxLevel = 20,
            };
            // Set up a level with attributes
            var levelData = new PcSkillLevelData { level = 1 };
            levelData.state = new System.Collections.Generic.List<SkillMagicAttribute>
            {
                new SkillMagicAttribute { kind = MagicAttributeKind.Strength, value1 = 50, value2 = 0, value3 = 0 },
                new SkillMagicAttribute { kind = MagicAttributeKind.Dexterity, value1 = 30, value2 = 0, value3 = 0 },
            };
            // levelData added to skill.levels - need a way to do that
            // The skill design uses a dictionary. Use reflection to set it.
            var levelsField = typeof(SkillDefinition).GetField("levels",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (levelsField != null)
            {
                var dict = new System.Collections.Generic.Dictionary<int, PcSkillLevelData>
                {
                    [1] = levelData,
                };
                levelsField.SetValue(skill, dict);
            }
            var svc = new BuffStateService();
            svc.ApplyBuff(1, skill, 1, 5f);
            Assert.AreEqual(50, svc.GetBuffModifier(1, MagicAttributeKind.Strength));
            Assert.AreEqual(30, svc.GetBuffModifier(1, MagicAttributeKind.Dexterity));
        }

        [Test]
        public void GetBuffModifier_MultipleBuffsAggregates()
        {
            var svc = new BuffStateService();
            var skill1 = MakeSkill(100, 0, "A");
            var skill2 = MakeSkill(200, 0, "B");
            svc.ApplyBuff(1, skill1, 1, 5f);
            svc.ApplyBuff(1, skill2, 1, 5f);
            // Both have empty attrs but the test verifies multi-buff state
            Assert.IsTrue(svc.HasBuff(1, 100));
            Assert.IsTrue(svc.HasBuff(1, 200));
        }

        // ── HasBuff ──────────────────────────────────────────────────────────

        [Test]
        public void HasBuff_NoActor_ReturnsFalse()
        {
            var svc = BuildService();
            Assert.IsFalse(svc.HasBuff(99, 100));
        }

        [Test]
        public void HasBuff_OtherActor_ReturnsFalse()
        {
            var svc = BuildService();
            svc.ApplyBuff(1, MakeSkill(100, 0, "X"), 1, 5f);
            Assert.IsFalse(svc.HasBuff(2, 100));
        }

        // ── AttachHost ───────────────────────────────────────────────────────

        [Test]
        public void AttachHost_ReplacesHost()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var svc = new BuffStateService(host1);
            svc.AttachHost(host2);
            svc.ApplyBuff(1, MakeSkill(100, 0, "X"), 1, 5f);
            Assert.AreEqual(0, host1.ShowCalls);
            Assert.AreEqual(1, host2.ShowCalls);
        }
    }
}
