using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class MountedCombatActorParityTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CreateCombatActor_UsesLiveMountState_ForMaleAndFemaleVisuals(bool female)
        {
            typeof(SandboxManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?
                .GetSetMethod(true)?.Invoke(null, new object[] { null });

            var controllerGo = new GameObject("MountedCombatActorController");
            var playerGo = new GameObject("MountedCombatActorPlayer");
            playerGo.SetActive(false);
            var player = playerGo.AddComponent<SandboxPlayerController>();
            player.isFemale = female;
            player.startMounted = false;

            try
            {
                var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
                var footOnly = catalog.Resolve(47);
                var mountedAllowed = catalog.Resolve(45);
                Assert.AreEqual(1, footOnly.horseLimit, "PC Tang Men 47 is foot-only");
                Assert.AreEqual(0, mountedAllowed.horseLimit, "PC Tang Men 45 permits riding");

                var progression = new PlayerProgressionState { faction = CombatFaction.TangMen, level = 60 };
                progression.knownSkills.Add(45);
                progression.knownSkills.Add(47);
                progression.skillLevels[45] = 1;
                progression.skillLevels[47] = 1;

                var controller = controllerGo.AddComponent<CombatSkillSlotController>();
                typeof(CombatSkillSlotController).GetField("_progression", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(controller, progression);
                typeof(CombatSkillSlotController).GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(controller, catalog);
                var createActor = typeof(CombatSkillSlotController).GetMethod(
                    "CreateCombatActor", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(createActor);

                  playerGo.SetActive(true);
                  // Explicitly finish serialized gender setup for inactive test objects;
                  // production scene objects invoke Awake with the serialized value.
                  player.SetGender(female);
                  Assert.IsInstanceOf(female ? typeof(FemalePlayerVisual) : typeof(MalePlayerVisual), player.visual);

                var footActor = Create(createActor, controller, player, footOnly);
                Assert.IsFalse(footActor.rideHorse);
                Assert.IsTrue(new CombatRuntimeService(catalog).Cast(
                    footActor, Enemy(), footOnly.skillId, Vector2.zero, CombatRelation.Enemy).success);

                player.Mount.Mount(player.defaultHorseId);
                player.Mount.Tick(player.Mount.MountTransitionTime);
                Assert.IsTrue(player.Mount.IsMounted);

                var mountedActor = Create(createActor, controller, player, footOnly);
                Assert.IsTrue(mountedActor.rideHorse);
                Assert.AreEqual(CombatCastRejectReason.HorseRestricted, new CombatRuntimeService(catalog).Cast(
                    mountedActor, Enemy(), footOnly.skillId, Vector2.zero, CombatRelation.Enemy).reason);
                Assert.IsTrue(new CombatRuntimeService(catalog).Cast(
                    mountedActor, Enemy(), mountedAllowed.skillId, Vector2.zero, CombatRelation.Enemy).success);
            }
            finally
            {
                Object.DestroyImmediate(controllerGo);
                Object.DestroyImmediate(playerGo);
            }
        }

        private static CombatActorState Create(MethodInfo method, CombatSkillSlotController controller,
            SandboxPlayerController player, SkillDefinition skill)
        {
            return method.Invoke(controller, new object[] { player, skill }) as CombatActorState;
        }

        private static CombatActorState Enemy() => new CombatActorState
        {
            actorId = 99,
            faction = CombatFaction.None,
            currentLife = 1000,
            maxLife = 1000,
            position = Vector2.zero,
        };
    }
}
