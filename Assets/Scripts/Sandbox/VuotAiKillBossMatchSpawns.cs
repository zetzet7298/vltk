// -----------------------------------------------------------------------------
// VLTK Mobile — PC killbossmatch mission NPC spawns for Vượt ải Nhiếp Thí Trần.
// Source: Server 6.0/server1/script/missions/killbossmatch/class.lua
// OnInit(): ClearMapNpc/ClearMapObj/ClearMapTrap, then _RefreshNpc(nMapId).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class VuotAiKillBossMatchSpawns
    {
        public static readonly int[] MissionMapIds = { 907, 908, 909, 910, 911, 912, 913, 914, 915, 916 };
        public static readonly int[] BossTemplateIds = { 1481, 1485, 1488, 1483, 1482, 1480, 1489, 1486, 1487, 1484 };

        private static readonly BossDef[] Bosses =
        {
            new BossDef(1481, "Nhất quỷ", 0),
            new BossDef(1485, "Nhị quỷ", 1),
            new BossDef(1488, "Tam quỷ", 1),
            new BossDef(1483, "Tứ quỷ", 2),
            new BossDef(1482, "Ngũ quỷ", 2),
            new BossDef(1480, "Lục quỷ", 3),
            new BossDef(1489, "Thất quỷ", 3),
            new BossDef(1486, "Bát quỷ", 4),
            new BossDef(1487, "Cửu quỷ", 4),
            new BossDef(1484, "Thập quỷ", 0),
        };

        // PC tbNpcPos uses cell coords; basemission_CallNpc multiplies by 32 MPS pixels.
        private static readonly Vector2Int[] BossPositions =
        {
            new Vector2Int(1266, 3241), new Vector2Int(1330, 3313), new Vector2Int(1422, 3467),
            new Vector2Int(1607, 3347), new Vector2Int(1647, 3301), new Vector2Int(1545, 3282),
            new Vector2Int(1445, 3368), new Vector2Int(1402, 3277), new Vector2Int(1444, 3210),
            new Vector2Int(1485, 3175), new Vector2Int(1421, 3120), new Vector2Int(1505, 3431),
        };

        public static bool IsMissionMap(int mapId) => Array.IndexOf(MissionMapIds, mapId) >= 0;

        public static int AddMissionBossEntries(int mapId, NpcTemplateRegistry registry, IList<BaLangNpcEntry> target)
        {
            if (!IsMissionMap(mapId) || registry == null || target == null)
                return 0;

            int[] bossOrder = PcChaosOrder(Bosses.Length);
            int[] posOrder = PcChaosOrder(BossPositions.Length);
            int firstInstanceId = target.Count + 1;
            int added = 0;

            for (int i = 0; i < 5; i++)
            {
                AddBossEntry(Bosses[bossOrder[i]], BossPositions[posOrder[i]], 0, registry, target, firstInstanceId + added++);
                AddBossEntry(Bosses[bossOrder[i + 5]], BossPositions[posOrder[i]], 32, registry, target, firstInstanceId + added++);
            }
            return added;
        }

        private static void AddBossEntry(BossDef def, Vector2Int cell, int mpsOffset, NpcTemplateRegistry registry,
            IList<BaLangNpcEntry> target, int instanceId)
        {
            var source = registry.Resolve(def.TemplateId) ?? MapEnemyDatabase.Resolve(def.TemplateId);
            if (source == null)
                return;
            if (!registry.Contains(def.TemplateId))
                registry.Register(source);

            int mpsX = cell.x * 32 + mpsOffset;
            int mpsY = cell.y * 32 + mpsOffset;
            target.Add(new BaLangNpcEntry
            {
                template = CloneMissionTemplate(source, def),
                worldPosition = MapEnemyDatabase.MpsToWorld(mpsX, mpsY),
                series = def.Series,
                level = 100,
                facing = 0,
                instanceId = instanceId,
            });
        }

        private static int[] PcChaosOrder(int count)
        {
            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            for (int i = 0; i < count; i++)
            {
                int j = UnityEngine.Random.Range(0, count);
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order;
        }

        private static NpcTemplate CloneMissionTemplate(NpcTemplate source, BossDef def)
        {
            return new NpcTemplate
            {
                templateId = source.templateId,
                nameRaw = def.NameVi,
                nameNormalized = def.NameVi,
                level = 100,
                maxLife = source.maxLife,
                attack = source.attack,
                defense = source.defense,
                kind = source.kind,
                series = def.Series,
                walkSpeed = source.walkSpeed,
                runSpeed = source.runSpeed,
                visionRadius = source.visionRadius,
                activeRadius = source.activeRadius,
                aiMode = source.aiMode,
                aiParams = source.aiParams == null ? null : (int[])source.aiParams.Clone(),
                spriteSourceId = source.spriteSourceId,
                spriteClipRef = source.spriteClipRef,
                scriptRef = source.scriptRef,
                levelScriptRef = source.levelScriptRef,
                spriteResolved = source.spriteResolved,
                scriptResolved = source.scriptResolved,
                warnings = new List<string>(source.warnings),
            };
        }

        private readonly struct BossDef
        {
            public readonly int TemplateId;
            public readonly string NameVi;
            public readonly int Series;
            public BossDef(int templateId, string nameVi, int series)
            {
                TemplateId = templateId;
                NameVi = nameVi;
                Series = series;
            }
        }
    }
}
