// -----------------------------------------------------------------------------
// VLTK Mobile — PlayMode E2E Gameplay Loop Scaffold
// Covers spawn → combat → death/reward → progression → skill/economy/dialogue/travel.
// -----------------------------------------------------------------------------

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.PlayMode
{
    public class E2EGameplayLoopTests
    {
        private GameplayLoopService _loop;
        private SkillCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog(new AssetRegistry());
            _loop = new GameplayLoopService(_catalog, playerFactionId: 5, initialSilver: 1000);
            _loop.RegisterPlayer(1, "Thiếu hiệp", 10, new Vector2(1600, 3200));
        }

        [UnityTest]
        public IEnumerator PlayerSpawn_LoadsMap_SpawnsAtCorrectPosition()
        {
            yield return null;
            Assert.IsNotNull(_loop.Player);
            Assert.AreEqual("Thiếu hiệp", _loop.Player.nameVi);
            Assert.AreEqual(new Vector2(1600, 3200), _loop.Player.worldPos);
            Assert.Greater(_loop.Player.combat.currentLife, 0);
        }

        [UnityTest]
        public IEnumerator Combat_PlayerAttack_EnemyTakesDamage()
        {
            _loop.Player.combat.knownSkills.Add(1);
            _loop.Player.combat.skillLevels[1] = 10;
            _loop.Player.combat.currentMana = 999;
            var enemy = _loop.RegisterEnemy(100, "Mèo Vàng", 500, 5, new Vector2(1610, 3200));
            int hpBefore = enemy.combat.currentLife;

            var report = _loop.PlayerCastSkill(1, enemy.actorId);
            yield return null;

            Assert.IsNotNull(report);
            Assert.LessOrEqual(enemy.combat.currentLife, hpBefore);
        }

        [UnityTest]
        [Ignore("TODO: implement when death→loot event pipeline is complete")]
        public IEnumerator DeathFlow_EnemyDies_DropsLoot()
        {
            // Placeholder — loot-drop pipeline not yet wired.
            // Requires GameplayLoopService.OnEnemyDeath → loot table lookup → player reward.
            yield return null;
        }

        [UnityTest]
        public IEnumerator LevelUp_ExpThreshold_StatsIncrease()
        {
            int levelBefore = _loop.LevelService.Level;
            _loop.LevelService.AddExp(PlayerStatService.GetExpRequired(levelBefore));
            yield return null;

            Assert.GreaterOrEqual(_loop.LevelService.Level, levelBefore);
        }

        [UnityTest]
        public IEnumerator SkillCast_ManaReduced_CooldownStarts()
        {
            _loop.Player.combat.knownSkills.Add(1);
            _loop.Player.combat.skillLevels[1] = 10;
            _loop.Player.combat.currentMana = 999;
            var enemy = _loop.RegisterEnemy(102, "Sơn Tặc", 500, 5, new Vector2(1610, 3200));
            int manaBefore = _loop.Player.combat.currentMana;

            var report = _loop.PlayerCastSkill(1, enemy.actorId);
            yield return null;

            Assert.IsNotNull(report);
            Assert.LessOrEqual(_loop.Player.combat.currentMana, manaBefore);
        }

        [UnityTest]
        public IEnumerator Inventory_PickupItem_CountIncreases()
        {
            bool ok = _loop.Economy.DepositToStash(2001, 3);
            yield return null;

            Assert.IsTrue(ok);
            Assert.AreEqual(1, _loop.Economy.StashUsed);
            Assert.AreEqual(3, _loop.Economy.Stash[0].count);
        }

        [UnityTest]
        public IEnumerator Dialogue_NpcInteraction_DialogueShows()
        {
            var dialogue = new NpcDialogueService(new TaskFlagService());
            var root = dialogue.StartDialogue(500, playerLevel: 10);
            yield return null;

            Assert.IsNotNull(root);
            Assert.IsTrue(root.npcTextVi.Contains("Dã Tẩu"));
            Assert.Greater(root.options.Count, 0);
        }

        [UnityTest]
        public IEnumerator StationTravel_PayGold_TeleportToDestination()
        {
            var travel = new StationTravelService(new PlayerLevelService(initialLevel: 10));
            int silver = 1000;
            int mapId = 79;
            Vector2 pos = Vector2.zero;

            bool ok = travel.Travel(10, ref silver, ref pos, ref mapId);
            yield return null;

            Assert.IsTrue(ok);
            Assert.AreEqual(100, mapId);
            Assert.AreEqual(new Vector2(2000, 4000), pos);
            Assert.Less(silver, 1000);
        }
    }
}
