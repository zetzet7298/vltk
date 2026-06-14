// -----------------------------------------------------------------------------
// VLTK Mobile — MissileSpawner EditMode tests.
// Kiểm tra spawn đạn: Single/Fan/Surround/Chain/None forms, hit detection,
// host dispatch chain.
// PC source: PcMissles.txt Speed, LifeTime + KMissle::Activate.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class MissileSpawnerTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IMissileSpawnerHost
        {
            public int SpawnStartCalls;
            public int SpawnCompleteCalls;
            public int MissileHitCalls;
            public int BatchSpawnedCalls;
            public int ShowCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public int LastSkillId;
            public int LastChildCount;
            public SkillMissileForm LastForm;
            public int LastSpawnedCount;
            public float LastSpeed;
            public float LastDuration;
            public int LastMissileId;
            public int LastTargetActorId;
            public int LastDamage;

            public void OnSpawnStart(int skillId, int childCount, SkillMissileForm form)
            {
                SpawnStartCalls++;
                LastSkillId = skillId;
                LastChildCount = childCount;
                LastForm = form;
            }
            public void OnSpawnComplete(int skillId, int spawnedCount, float speed, float duration)
            {
                SpawnCompleteCalls++;
                LastSpawnedCount = spawnedCount;
                LastSpeed = speed;
                LastDuration = duration;
            }
            public void OnMissileHit(int missileId, int targetActorId, int damage)
            {
                MissileHitCalls++;
                LastMissileId = missileId;
                LastTargetActorId = targetActorId;
                LastDamage = damage;
            }
            public void OnMissileBatchSpawned(int skillId, int missileCount, SkillMissileForm form)
            {
                BatchSpawnedCalls++;
            }
            public void ShowSkillEffect(int skillId, SkillMissileForm form) { ShowCalls++; }
            public void LogMissileEvent(int skillId, int missileCount, SkillMissileForm form) { LogCalls++; }
            public void PlayMissileSFX(int skillId, SkillMissileForm form) { SfxCalls++; }
            public void SaveMissileLog(int skillId, int missileCount, SkillMissileForm form) { SaveCalls++; }
        }

        private static SkillDefinition MakeSkill(int skillId, SkillMissileForm form, int childSkillId = 0, int attackRadius = 200)
        {
            return new SkillDefinition
            {
                skillId = skillId,
                nameRaw = $"Skill{skillId}",
                missileForm = form,
                childSkillId = childSkillId,
                attackRadius = attackRadius,
            };
        }

        private static CombatActorState MakeTarget(int id, Vector2 pos, int life = 1000)
        {
            return new CombatActorState
            {
                actorId = id,
                position = pos,
                currentLife = life,
                maxLife = life,
            };
        }

        // ── Ctor / Count ────────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new MissileSpawner();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Constructor_WithProjectileService()
        {
            var proj = new ProjectileService();
            var svc = new MissileSpawner(proj);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var proj = new ProjectileService();
            var svc = new MissileSpawner(proj, host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Constructor_NullProjectileService_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new MissileSpawner(null));
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new MissileSpawner();
            svc.AttachHost(host);
            svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Single), Vector2.zero, Vector2.right);
            Assert.AreEqual(1, host.SpawnStartCalls);
        }

        // ── SpawnMissiles ────────────────────────────────────────────────────

        [Test]
        public void SpawnMissiles_NullSkill_ReturnsEmpty()
        {
            var svc = new MissileSpawner();
            var spawned = svc.SpawnMissiles(null, Vector2.zero, Vector2.right);
            Assert.AreEqual(0, spawned.Count);
        }

        [Test]
        public void SpawnMissiles_Single_OneMissile()
        {
            var svc = new MissileSpawner();
            var spawned = svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Single), Vector2.zero, new Vector2(100, 0));
            Assert.AreEqual(1, spawned.Count);
        }

        [Test]
        public void SpawnMissiles_Fan_DefaultCount3()
        {
            var svc = new MissileSpawner();
            var spawned = svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Fan), Vector2.zero, new Vector2(100, 0));
            Assert.AreEqual(3, spawned.Count);
        }

        [Test]
        public void SpawnMissiles_Fan_CustomCount()
        {
            var svc = new MissileSpawner();
            var spawned = svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Fan), Vector2.zero, new Vector2(100, 0), childCount: 5);
            Assert.AreEqual(5, spawned.Count);
        }

        [Test]
        public void SpawnMissiles_Surround_DefaultCount8()
        {
            var svc = new MissileSpawner();
            var spawned = svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Surround), Vector2.zero, new Vector2(100, 0));
            Assert.AreEqual(8, spawned.Count);
        }

        [Test]
        public void SpawnMissiles_Surround_CustomCount()
        {
            var svc = new MissileSpawner();
            var spawned = svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Surround), Vector2.zero, new Vector2(100, 0), childCount: 4);
            Assert.AreEqual(4, spawned.Count);
        }

        [Test]
        public void SpawnMissiles_Chain_OneMissile()
        {
            var svc = new MissileSpawner();
            var spawned = svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Chain), Vector2.zero, new Vector2(100, 0));
            Assert.AreEqual(1, spawned.Count);
        }

        [Test]
        public void SpawnMissiles_None_NoMissile()
        {
            var svc = new MissileSpawner();
            var spawned = svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.None), Vector2.zero, new Vector2(100, 0));
            Assert.AreEqual(0, spawned.Count);
        }

        [Test]
        public void SpawnMissiles_SpeedOverride_Applied()
        {
            var host = new FakeHost();
            var svc = new MissileSpawner(new ProjectileService(), host);
            svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Single), Vector2.zero, new Vector2(100, 0), speedOverride: 500f);
            Assert.AreEqual(500f, host.LastSpeed);
        }

        [Test]
        public void SpawnMissiles_UniqueIds()
        {
            var svc = new MissileSpawner();
            var spawned = svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Fan), Vector2.zero, new Vector2(100, 0));
            var ids = new System.Collections.Generic.HashSet<int>();
            foreach (var p in spawned) ids.Add(p.instanceId);
            Assert.AreEqual(spawned.Count, ids.Count);
        }

        // ── Host dispatch ───────────────────────────────────────────────────

        [Test]
        public void SpawnMissiles_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new MissileSpawner(new ProjectileService(), host);
            svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Single), Vector2.zero, new Vector2(100, 0));
            Assert.AreEqual(1, host.SpawnStartCalls);
            Assert.AreEqual(1, host.SpawnCompleteCalls);
            Assert.AreEqual(1, host.BatchSpawnedCalls);
            Assert.AreEqual(1, host.ShowCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void SpawnMissiles_NoneForm_NoCompleteDispatch()
        {
            var host = new FakeHost();
            var svc = new MissileSpawner(new ProjectileService(), host);
            svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.None), Vector2.zero, new Vector2(100, 0));
            Assert.AreEqual(0, host.SpawnCompleteCalls);
            Assert.AreEqual(0, host.BatchSpawnedCalls);
        }

        [Test]
        public void SpawnMissiles_FanForm_DispatchesSpawnedCount()
        {
            var host = new FakeHost();
            var svc = new MissileSpawner(new ProjectileService(), host);
            svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Fan), Vector2.zero, new Vector2(100, 0), childCount: 5);
            Assert.AreEqual(5, host.LastSpawnedCount);
        }

        [Test]
        public void SpawnMissiles_DispatchesForm()
        {
            var host = new FakeHost();
            var svc = new MissileSpawner(new ProjectileService(), host);
            svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Chain), Vector2.zero, new Vector2(100, 0));
            Assert.AreEqual(SkillMissileForm.Chain, host.LastForm);
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void MissileSpawner_WithoutHost_DoesNotThrow()
        {
            var svc = new MissileSpawner();
            Assert.DoesNotThrow(() => svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Single), Vector2.zero, new Vector2(100, 0)));
            Assert.DoesNotThrow(() => svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Fan), Vector2.zero, new Vector2(100, 0)));
            Assert.DoesNotThrow(() => svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.None), Vector2.zero, new Vector2(100, 0)));
        }

        // ── UpdateMissiles / OnMissileHit ───────────────────────────────────

        [Test]
        public void UpdateMissiles_HitDetection_DispatchesHost()
        {
            var host = new FakeHost();
            var proj = new ProjectileService();
            var svc = new MissileSpawner(proj, host);
            var spawned = svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Single), Vector2.zero, new Vector2(10, 0));
            // Place target very close to the projectile origin
            var target = MakeTarget(2, new Vector2(5, 0));
            svc.UpdateMissiles(0.016f, new[] { target });
            Assert.AreEqual(1, host.MissileHitCalls);
        }

        [Test]
        public void UpdateMissiles_NoHit_NoDispatch()
        {
            var host = new FakeHost();
            var proj = new ProjectileService();
            var svc = new MissileSpawner(proj, host);
            svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Single), Vector2.zero, new Vector2(10, 0));
            // Place target very far away
            var target = MakeTarget(2, new Vector2(10000, 10000));
            svc.UpdateMissiles(0.016f, new[] { target });
            Assert.AreEqual(0, host.MissileHitCalls);
        }

        [Test]
        public void UpdateMissiles_FiresOnMissileHitEvent()
        {
            var proj = new ProjectileService();
            var svc = new MissileSpawner(proj);
            svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Single), Vector2.zero, new Vector2(10, 0));
            int fired = 0;
            svc.OnMissileHit += (p, t) => fired++;
            var target = MakeTarget(2, new Vector2(5, 0));
            svc.UpdateMissiles(0.016f, new[] { target });
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void UpdateMissiles_SkipsDeadTarget()
        {
            var host = new FakeHost();
            var proj = new ProjectileService();
            var svc = new MissileSpawner(proj, host);
            svc.SpawnMissiles(MakeSkill(1, SkillMissileForm.Single), Vector2.zero, new Vector2(10, 0));
            var target = MakeTarget(2, new Vector2(5, 0), life: 0);
            svc.UpdateMissiles(0.016f, new[] { target });
            Assert.AreEqual(0, host.MissileHitCalls);
        }
    }
}
