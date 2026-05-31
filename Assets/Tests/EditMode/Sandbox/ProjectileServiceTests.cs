using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M4.2 — Missile/Projectile Prototype tests. Spawns a projectile from skill
    /// data (AC#1), plays the decoded effect sprite when available (AC#2), and
    /// rejects out-of-range / blocked casts with a diagnostic reason (AC#3).
    /// </summary>
    public class ProjectileServiceTests
    {
        private SkillDefinition MakeSkill(SkillMissileForm form, int range,
            bool effectResolved = true)
            => new SkillDefinition
            {
                skillId = 100,
                nameNormalized = "Fireball",
                attackRadius = range,
                missileForm = form,
                effectSourceId = effectResolved ? new SourceAssetId { sourcePath = "skill/fx.spr", uid = 5 } : null,
                effectResolved = effectResolved,
            };

        private (ObstacleQueryService svc, ObstacleGrid grid) Obstacles(params Vector2Int[] blocked)
        {
            var grid = new ObstacleGrid { width = 50, height = 50, cellToWorldScale = 1f, cells = new byte[2500] };
            foreach (var b in blocked) grid.cells[b.y * 50 + b.x] = ObstacleGrid.WalkBlocked;
            return (new ObstacleQueryService(1f, 1f, Vector2.zero), grid);
        }

        // --- AC#1: projectile spawns from skill ---

        [Test]
        public void Cast_MissileSkill_SpawnsProjectile()
        {
            var svc = new ProjectileService { RangeWorldPerUnit = 1f };
            var result = svc.Cast(MakeSkill(SkillMissileForm.Single, 100), Vector2.zero, new Vector2(5, 0));
            Assert.IsTrue(result.success);
            Assert.IsNotNull(result.projectile);
            Assert.AreEqual(1, svc.LiveCount);
            Assert.AreEqual(100, result.projectile.skillId);
        }

        [Test]
        public void Cast_InstantSkill_SucceedsNoProjectile()
        {
            var svc = new ProjectileService();
            var result = svc.Cast(MakeSkill(SkillMissileForm.None, 100), Vector2.zero, new Vector2(3, 0));
            Assert.IsTrue(result.success);
            Assert.IsNull(result.projectile);
            Assert.AreEqual(0, svc.LiveCount);
        }

        [Test]
        public void Projectile_StepsTowardTargetAndArrives()
        {
            var svc = new ProjectileService { DefaultMissileSpeed = 5f, RangeWorldPerUnit = 1f };
            var result = svc.Cast(MakeSkill(SkillMissileForm.Single, 100), Vector2.zero, new Vector2(10, 0));
            var proj = result.projectile;

            svc.Step(1f); // moves 5 units
            Assert.AreEqual(5f, proj.position.x, 0.001f);
            Assert.AreEqual(1, svc.LiveCount);

            svc.Step(1f); // reaches target → removed
            Assert.AreEqual(0, svc.LiveCount);
        }

        // --- AC#2: effect sprite resolved ---

        [Test]
        public void Cast_EffectResolved_ProjectileCarriesClip()
        {
            var svc = new ProjectileService();
            var result = svc.Cast(MakeSkill(SkillMissileForm.Single, 100, effectResolved: true), Vector2.zero, new Vector2(2, 0));
            Assert.IsTrue(result.projectile.effectResolved);
            Assert.AreEqual("skill/fx.spr", result.projectile.effectClipRef);
        }

        [Test]
        public void Cast_EffectMissing_ProjectileStillSpawnsWithoutClip()
        {
            var svc = new ProjectileService();
            var result = svc.Cast(MakeSkill(SkillMissileForm.Single, 100, effectResolved: false), Vector2.zero, new Vector2(2, 0));
            Assert.IsTrue(result.success);
            Assert.IsFalse(result.projectile.effectResolved);
            Assert.IsNull(result.projectile.effectClipRef);
        }

        // --- AC#3: out of range / blocked rejection ---

        [Test]
        public void Cast_OutOfRange_Rejected()
        {
            var svc = new ProjectileService { RangeWorldPerUnit = 1f };
            // range 5, target at distance 20 → rejected.
            var result = svc.Cast(MakeSkill(SkillMissileForm.Single, 5), Vector2.zero, new Vector2(20, 0));
            Assert.IsFalse(result.success);
            Assert.AreEqual(CastRejectReason.OutOfRange, result.reason);
            Assert.AreEqual(0, svc.LiveCount);
        }

        [Test]
        public void Cast_BlockedTarget_Rejected()
        {
            var (obs, grid) = Obstacles(new Vector2Int(5, 0));
            var svc = new ProjectileService(obs) { RangeWorldPerUnit = 1f };
            // target world (5.5,0.5) in blocked cell (5,0), within range 100.
            var result = svc.Cast(MakeSkill(SkillMissileForm.Single, 100), Vector2.zero, new Vector2(5.5f, 0.5f), grid);
            Assert.IsFalse(result.success);
            Assert.AreEqual(CastRejectReason.TargetBlocked, result.reason);
        }

        [Test]
        public void Cast_NullSkill_Rejected()
        {
            var svc = new ProjectileService();
            var result = svc.Cast(null, Vector2.zero, Vector2.one);
            Assert.IsFalse(result.success);
            Assert.AreEqual(CastRejectReason.NoSkill, result.reason);
        }

        [Test]
        public void Cast_InRangeUnblocked_Succeeds()
        {
            var (obs, grid) = Obstacles(); // nothing blocked
            var svc = new ProjectileService(obs) { RangeWorldPerUnit = 1f };
            var result = svc.Cast(MakeSkill(SkillMissileForm.Single, 100), Vector2.zero, new Vector2(3.5f, 0.5f), grid);
            Assert.IsTrue(result.success);
            Assert.AreEqual(CastRejectReason.None, result.reason);
        }
    }
}
