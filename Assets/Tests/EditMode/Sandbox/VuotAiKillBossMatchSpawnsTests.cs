using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class VuotAiKillBossMatchSpawnsTests
    {
        [Test]
        public void KillBossMatch_CreatesTenPcMissionBossEntries()
        {
            var registry = new NpcTemplateRegistry();
            MapEnemyDatabase.RegisterAllForMap(907, registry);
            var entries = new List<BaLangNpcEntry>();

            Random.InitState(69);
            int count = VuotAiKillBossMatchSpawns.AddMissionBossEntries(907, registry, entries);

            Assert.AreEqual(10, count);
            Assert.AreEqual(10, entries.Count);
            CollectionAssert.AreEquivalent(VuotAiKillBossMatchSpawns.BossTemplateIds,
                entries.Select(e => e.template.templateId));
            CollectionAssert.AreEquivalent(new[]
            {
                "Nhất quỷ", "Nhị quỷ", "Tam quỷ", "Tứ quỷ", "Ngũ quỷ",
                "Lục quỷ", "Thất quỷ", "Bát quỷ", "Cửu quỷ", "Thập quỷ",
            }, entries.Select(e => e.template.DisplayName));
            Assert.IsTrue(entries.All(e => e.level == 100));
            Assert.IsTrue(entries.All(e => IsPcKillBossPosition(e.worldPosition)));
        }

        [Test]
        public void SpawnForMap_VuotAiSkipsStaticRegionSAndSpawnsMissionBosses()
        {
            var root = new GameObject("vuot-ai-runtime-test");
            try
            {
                var runtime = root.AddComponent<MapEnemySpawnRuntime>();
                Random.InitState(69);

                runtime.SpawnForMap(907, "__static_region_s_should_be_ignored_for_killbossmatch__");

                Assert.AreEqual(10, runtime.liveEnemyCount);
                Assert.AreEqual(10, runtime.Entries.Count);
                CollectionAssert.AreEquivalent(VuotAiKillBossMatchSpawns.BossTemplateIds,
                    runtime.Entries.Select(e => e.template.templateId));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static bool IsPcKillBossPosition(Vector2 world)
        {
            foreach (var cell in PcPositions)
            {
                if (world == MapEnemyDatabase.MpsToWorld(cell.x * 32, cell.y * 32)) return true;
                if (world == MapEnemyDatabase.MpsToWorld(cell.x * 32 + 32, cell.y * 32 + 32)) return true;
            }
            return false;
        }

        private static readonly Vector2Int[] PcPositions =
        {
            new Vector2Int(1266, 3241), new Vector2Int(1330, 3313), new Vector2Int(1422, 3467),
            new Vector2Int(1607, 3347), new Vector2Int(1647, 3301), new Vector2Int(1545, 3282),
            new Vector2Int(1445, 3368), new Vector2Int(1402, 3277), new Vector2Int(1444, 3210),
            new Vector2Int(1485, 3175), new Vector2Int(1421, 3120), new Vector2Int(1505, 3431),
        };
    }
}
