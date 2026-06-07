// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC-derived Ba Lăng huyện enemy templates from Region_S server data.
    /// Real PC spawns: 514 enemies (kind=0), 25 town NPCs (kind=3).
    /// Enemy types: 金猫(31), 梅花鹿(42), 白猪(43) + training dummies (413,414,415).
    /// Source: StreamingAssets/Reference/PcNpcS.txt + Region_S.dat spawn positions.
    /// </summary>
    public static class BaLangEnemyDatabase
    {
        public const int MapId = SandboxManager.BaLangHuyenMapId;

        /// <summary>Enemy template IDs (kind=0 animals that wander outside town).</summary>
        public static readonly int[] EnemyTemplateIds = { 31, 42, 43 };

        /// <summary>All supported visual template IDs. Training dummies stay excluded until real SPRs are staged.</summary>
        public static readonly int[] AllTemplateIds = { 31, 42, 43 };

        public static IEnumerable<NpcTemplate> CreateTemplates()
        {
            // PC template ids in Region_S are 0-based data ids; row lookup is id + 1 because NpcS.txt has a header row.
            yield return Template(31, "金猫", "Mèo vàng", "ani049",
                kind: 0, series: 1, maxLife: 100,
                walk: 6, run: 6, vision: 400, active: 700, ai: 1,
                60, 10, 10, 0, 0, 0, 20, 0, 0);

            yield return Template(42, "梅花鹿", "Hươu đốm", "ani061",
                kind: 0, series: 2, maxLife: 100,
                walk: 4, run: 4, vision: 400, active: 700, ai: 4,
                80, 40, 10, 0, 0, 0, 10, 0, 0);

            yield return Template(43, "白猪", "Heo trắng", "ani063",
                kind: 0, series: 3, maxLife: 100,
                walk: 3, run: 3, vision: 400, active: 700, ai: 4,
                60, 20, 10, 0, 0, 0, 10, 0, 0);
        }

        public static void RegisterAll(NpcTemplateRegistry registry)
        {
            if (registry == null) return;
            foreach (var template in CreateTemplates())
                registry.Register(template);
        }

        public static string VietnameseSeriesName(int series)
        {
            return series switch
            {
                0 => "Kim hệ",
                1 => "Mộc hệ",
                2 => "Thủy hệ",
                3 => "Hỏa hệ",
                4 => "Thổ hệ",
                _ => "Vô hệ",
            };
        }

        public static string DisplayNameWithSeries(NpcTemplate template)
        {
            if (template == null) return "Vô hệ Kẻ địch";
            return $"{VietnameseSeriesName(template.series)} {template.DisplayName}";
        }

        /// <summary>
        /// Convert PC MPS coordinates to Unity world coordinates.
        /// MPS: regionCol = mpsX / 512, regionRow = mpsY / 1024.
        /// Unity: worldX = mpsX, worldY = -(mpsY - regionRow * 512).
        /// </summary>
        public static readonly int[] TrainerTemplateIds = { 311, 413, 414, 415 };

        public static string VietnameseTrainerName(int templateId, string rawName)
        {
            return templateId switch
            {
                311 => "Võ sư",
                413 => "Cọc gỗ",
                414 => "Mộc nhân",
                415 => "Bao cát",
                _ => string.IsNullOrEmpty(rawName) ? $"NPC_{templateId}" : rawName,
            };
        }

        public static bool IsTrainerSpawn(int templateId)
        {
            foreach (var id in TrainerTemplateIds)
                if (id == templateId) return true;
            return false;
        }

        /// <summary>
        /// Convert PC MPS (global pixel) coordinates to Unity world coordinates.
        /// PC region math: regionRow = mpsY / 1024 (integer), regionCol = mpsX / 1024.
        /// Unity: worldX = mpsX, worldY = -(mpsY - regionRow * 512).
        /// Matches MapRenderer region placement (col*512, row*512 in Unity space).
        /// </summary>
public static UnityEngine.Vector2 MpsToWorld(int mpsX, int mpsY)
        {
            int regionRow = mpsY / 1024;
            float worldX = mpsX;
            float worldY = -(mpsY - regionRow * 512f);
            return new UnityEngine.Vector2(worldX, worldY);
        }

        /// <summary>
        /// Inverse of MpsToWorld. Converts Unity world position back to PC MPS coords.
        /// worldY = -(mpsY - regionRow*512) => mpsY = regionRow*512 - worldY.
        /// regionRow is estimated from worldY: regionRow = floor((mpsY)/1024).
        /// Approximation: mpsY ~ -worldY / 0.5 for initial estimate.
        /// </summary>
public static void WorldToMps(float worldX, float worldY, out int mpsX, out int mpsY)
        {
            mpsX = Mathf.RoundToInt(worldX);
            int approx = Mathf.RoundToInt(-worldY * 2f);
            int regionRow = approx / 1024;
            mpsY = Mathf.RoundToInt(regionRow * 512f - worldY);
        }

        public static string BuildNpcSprPath(string resType, string action)
        {
            if (string.IsNullOrWhiteSpace(resType)) return null;
            string folder = resType.StartsWith("ani", System.StringComparison.OrdinalIgnoreCase)
                ? "animal"
                : resType.StartsWith("boss", System.StringComparison.OrdinalIgnoreCase) ? "boss" : "enemy";
            return $@"spr\npcres\{folder}\{resType}\{resType}_{action}.spr";
        }

        private static NpcTemplate Template(int id, string raw, string vi, string resType,
            int kind, int series, int maxLife,
            int walk, int run, int vision, int active, int ai,
            params int[] aiParams)
        {
            return new NpcTemplate
            {
                templateId = id,
                nameRaw = raw,
                nameNormalized = vi,
                level = 1,
                maxLife = maxLife,
                kind = kind,
                series = series,
                walkSpeed = walk,
                runSpeed = run,
                visionRadius = vision,
                activeRadius = active,
                aiMode = ai,
                aiParams = aiParams,
                spriteClipRef = resType,
                spriteResolved = true,
            };
        }
    }
}
