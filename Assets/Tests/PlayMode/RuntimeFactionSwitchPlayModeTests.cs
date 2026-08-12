using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.PlayMode
{
    [Category("RuntimeFactionSwitch")]
    public sealed class RuntimeFactionSwitchPlayModeTests
    {
        private GameObject _sandboxGo;
        private GameObject _hotbarGo;

        [SetUp]
        public void SetUp()
        {
            if (SandboxManager.Instance != null)
                Object.DestroyImmediate(SandboxManager.Instance.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hotbarGo != null) Object.DestroyImmediate(_hotbarGo);
            if (_sandboxGo != null) Object.DestroyImmediate(_sandboxGo);
            if (SandboxManager.Instance != null)
                Object.DestroyImmediate(SandboxManager.Instance.gameObject);
            _hotbarGo = null;
            _sandboxGo = null;
        }

        [UnityTest]
        public IEnumerator ManagerSwitch_UpdatesProgressionActorAndEmitsOneEvent()
        {
            _sandboxGo = new GameObject("SandboxManager_RuntimeFactionSwitch");
            _sandboxGo.AddComponent<SandboxManager>();
            yield return null;

            var manager = SandboxManager.Instance;
            Assert.IsNotNull(manager);
            Assert.IsTrue(manager.IsInitialized);
            int eventCount = 0;
            CombatFaction observed = CombatFaction.None;
            manager.RuntimeFactionSwitched += _ => throw new System.InvalidOperationException("listener-probe");
            manager.RuntimeFactionSwitched += faction =>
            {
                eventCount++;
                observed = faction;
            };
            manager.SkillEffectVisual.PlayHitFlash(Vector2.zero, Color.white);

            Assert.IsTrue(
                manager.TrySwitchRuntimeFaction(CombatFaction.WuDang, out string detail),
                detail);

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(CombatFaction.WuDang, observed);
            StringAssert.Contains("notificationFailures=1", detail);
            Assert.AreEqual(CombatFaction.WuDang, manager.PlayerProgression.faction);
            Assert.AreEqual(CombatFaction.WuDang, manager.GameplayLoop.Player.combat.faction);
            Assert.AreSame(manager.PlayerProgression.knownSkills, manager.GameplayLoop.Player.combat.knownSkills);
            Assert.AreSame(manager.PlayerProgression.skillLevels, manager.GameplayLoop.Player.combat.skillLevels);
            Assert.AreEqual(0, manager.SkillEffectVisual.ActiveEffectCount);
            Assert.IsTrue(manager.GameplayLoop.Player.combat.currentLife > 0);
            Assert.IsTrue(manager.PlayerProgression.knownSkills.Count > 1);
            foreach (int skillId in manager.PlayerProgression.knownSkills)
            {
                var skill = manager.CombatSkillCatalog.Resolve(skillId);
                Assert.IsNotNull(skill, $"Missing learned skill {skillId}");
                Assert.IsTrue(
                    skill.faction == CombatFaction.WuDang ||
                    skillId == PcCombatCatalogFactory.UniversalLightnessSkill ||
                    skill.isLeapSkill,
                    $"Cross-faction skill leaked: {skillId}/{skill.faction}");
                Assert.Greater(manager.PlayerProgression.GetSkillLevel(skillId), 0);
            }
        }


        [UnityTest]
        public IEnumerator ManagerSwitch_EventResetsHotbarWithoutProductionVisualTree()
        {
            _sandboxGo = new GameObject("SandboxManager_RuntimeFactionHotbarSwitch");
            _sandboxGo.AddComponent<SandboxManager>();
            yield return null;

            var manager = SandboxManager.Instance;
            Assert.IsNotNull(manager);
            _hotbarGo = new GameObject("RuntimeFactionSwitchHotbar_EventProbe");
            _hotbarGo.AddComponent<UIDocument>();
            var hotbar = _hotbarGo.AddComponent<CombatSkillSlotController>();
            hotbar.Initialize(manager.CombatSkillCatalog, manager.PlayerProgression);
            hotbar.AssignPrimarySkill(999001);
            hotbar.ToggleDeck();
            hotbar.AssignPrimarySkill(999002);
            hotbar.AssignSkill(0, 999003);
            hotbar.ToggleDeck();

            Assert.IsTrue(manager.TrySwitchRuntimeFaction(CombatFaction.WuDang, out string detail), detail);
            yield return null;

            Assert.AreEqual(0, hotbar.ActiveDeckIndex);
            Assert.AreEqual(0, hotbar.GetAssignedPrimarySkill(1), "deck B primary clears from manager event");
            int primarySkillId = hotbar.GetAssignedPrimarySkill(0);
            Assert.Greater(primarySkillId, 0);
            Assert.AreEqual(CombatFaction.WuDang, manager.CombatSkillCatalog.Resolve(primarySkillId).faction);
            for (int slot = 0; slot < CombatSkillSlotController.MobileSkillSlotCount; slot++)
            {
                int skillId = hotbar.GetAssignedSkill(slot, 0);
                Assert.Greater(skillId, 0, $"slot {slot} should be filled when catalog has enough skills");
                Assert.AreEqual(CombatFaction.WuDang, manager.CombatSkillCatalog.Resolve(skillId).faction);
                Assert.AreEqual(0, hotbar.GetAssignedSkill(slot, 1));
            }
        }

        [UnityTest]
        public IEnumerator ManagerBoot_DefaultsProgressionAndCombatActorToCaiBang()
        {
            _sandboxGo = new GameObject("SandboxManager_DefaultCaiBang");
            _sandboxGo.AddComponent<SandboxManager>();
            yield return null;

            var manager = SandboxManager.Instance;
            Assert.IsNotNull(manager);
            Assert.IsTrue(manager.IsInitialized);
            Assert.AreEqual(CombatFaction.CaiBang, GameplayLoopService.DefaultPlayerFaction);
            Assert.AreEqual(CombatFaction.CaiBang, manager.PlayerProgression.faction);
            Assert.AreEqual(CombatFaction.CaiBang, manager.GameplayLoop.Player.combat.faction);
            StringAssert.Contains("Cái Bang", manager.GameplayLoop.Player.nameVi);
        }

        [UnityTest]
        public IEnumerator RestrictedGenderSwitch_RebuildsLiveVisualAndPreservesRuntimePose()
        {
            _sandboxGo = new GameObject("SandboxManager_RuntimeFactionGenderSwitch");
            _sandboxGo.AddComponent<SandboxManager>();
            yield return null;

            var manager = SandboxManager.Instance;
            var controller = manager.PlayerController;
            for (int frame = 0; frame < 60 && manager.InventoryService == null; frame++)
                yield return null;
            Assert.IsNotNull(manager.InventoryService, "Deferred equipment wiring must settle before preservation assertions");
            yield return null;
            Vector3 position = controller.transform.position;
            PcWeaponType weapon = controller.EquippedWeapon;
            bool mounted = controller.Mount.IsMounted;

            Assert.IsTrue(manager.TrySwitchRuntimeFaction(CombatFaction.EMei, out string emeiDetail), emeiDetail);
            yield return null;
            Assert.IsTrue(controller.isFemale);
            Assert.IsInstanceOf<FemalePlayerVisual>(controller.visual);
            Assert.IsTrue((controller.visual as MonoBehaviour).gameObject.activeInHierarchy);

            Assert.IsTrue(manager.TrySwitchRuntimeFaction(CombatFaction.Shaolin, out string shaolinDetail), shaolinDetail);
            yield return null;
            Assert.IsFalse(controller.isFemale);
            Assert.IsInstanceOf<MalePlayerVisual>(controller.visual);
            Assert.IsTrue((controller.visual as MonoBehaviour).gameObject.activeInHierarchy);
            Assert.AreEqual(position, controller.transform.position);
            Assert.AreEqual(weapon, controller.EquippedWeapon);
            Assert.AreEqual(mounted, controller.Mount.IsMounted);
        }
    }
}
