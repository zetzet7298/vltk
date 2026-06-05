// -----------------------------------------------------------------------------
// VLTK Mobile — Full PC NpcS.txt parser (2,000 NPC templates)
// Source: server/settings/npcs.txt (GB2312, 103 tab-separated columns)
// Purpose: parse the complete NPC template catalog into NpcTemplate objects.
// Columns: Name, Kind, Camp, Series, Treasure, HeadImage, ClientOnly, CorpseIdx,
// R/G/B Lum, NpcResType, ArmorType, HelmType, WeaponType, HorseType, RideHorse,
// Stand/Death/Walk/Run/Hurt frames, 4 Skill+Level pairs, ActionScript,
// LevelScript, 4 × (Exp/Life/AR/Defense/MinDmg/MaxDmg) param blocks,
// WalkSpeed, RunSpeed, AttackSpeed, CastSpeed, VisionRadius, HitRecover,
// ActiveRadius, AIMode, AIParam1-9, 5 resistances (+ max), ReviveFrame,
// Stature, DropRateFile, AIMaxTime, 8 damage bases, AuraSkill, PasstSkill.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcNpcSFullParser
    {
        public const int TotalColumns = 103;
        public const int MinColumns = 62;

        // Column indices (0-based)
        public const int NameCol = 0;
        public const int KindCol = 1;
        public const int CampCol = 2;
        public const int SeriesCol = 3;
        public const int NpcResTypeCol = 11;
        public const int ActionScriptCol = 31;
        public const int LevelScriptCol = 32;
        public const int WalkSpeedCol = 58;
        public const int RunSpeedCol = 59;
        public const int VisionRadiusCol = 62;
        public const int ActiveRadiusCol = 64;
        public const int AIModeCol = 65;
        public const int AIParam1Col = 66;
        public const int DropRateFileCol = 87;

        public static List<NpcTemplate> ParseFile(string path)
        {
            var rows = new List<NpcTemplate>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int rowIndex = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < MinColumns) continue;
                rowIndex++;
                var npc = ParseRow(cols, rowIndex);
                if (npc != null) rows.Add(npc);
            }
            return rows;
        }

        public static NpcTemplate ParseRow(string[] cols, int templateIdHint = 0)
        {
            if (cols == null || cols.Length < MinColumns) return null;

            string nameRaw = PcItemCommon.Str(cols, NameCol);
            int templateId = templateIdHint > 0 ? templateIdHint : 0;
            if (string.IsNullOrEmpty(nameRaw) && templateId <= 0) return null;

            int kind = PcItemCommon.Int(cols, KindCol);
            int series = PcItemCommon.Int(cols, SeriesCol);
            string resType = PcItemCommon.Str(cols, NpcResTypeCol);
            int walkSpeed = PcItemCommon.Int(cols, WalkSpeedCol);
            int runSpeed = PcItemCommon.Int(cols, RunSpeedCol);
            int vision = PcItemCommon.Int(cols, VisionRadiusCol);
            int active = PcItemCommon.Int(cols, ActiveRadiusCol);
            int aiMode = PcItemCommon.Int(cols, AIModeCol);
            string actionScript = PcItemCommon.Str(cols, ActionScriptCol);

            // Parse AI params (cols 66-74)
            var aiParams = new int[9];
            for (int i = 0; i < 9; i++)
                aiParams[i] = PcItemCommon.Int(cols, AIParam1Col + i);

            // Calculate approximate level from ExpParam (cols 33-36)
            int level = EstimateLevel(cols);

            // Calculate approximate maxLife from LifeParam (cols 37-40)
            int maxLife = EstimateLife(cols);

            // Calculate attack/defense from DamageParam/DefenseParam
            int attack = EstimateAttack(cols);
            int defense = EstimateDefense(cols);

            var npc = new NpcTemplate
            {
                templateId = templateId,
                nameRaw = nameRaw,
                nameNormalized = nameRaw.Trim(),
                kind = kind,
                series = series,
                level = level,
                maxLife = maxLife,
                attack = attack,
                defense = defense,
                walkSpeed = walkSpeed > 0 ? walkSpeed : 4,
                runSpeed = runSpeed > 0 ? runSpeed : 6,
                visionRadius = vision > 0 ? vision : 400,
                activeRadius = active > 0 ? active : 700,
                aiMode = aiMode,
                aiParams = aiParams,
                spriteClipRef = resType,
                spriteResolved = !string.IsNullOrEmpty(resType),
                scriptRef = actionScript,
            };

            // Build icon source from NpcResType
            if (!string.IsNullOrEmpty(resType))
            {
                string folder = resType.StartsWith("ani") ? "animal" :
                    resType.StartsWith("npc") ? "npc" : "enemy";
                npc.spriteSourceId = new SourceAssetId
                {
                    sourcePath = $@"spr\npcres\{folder}\{resType}\{resType}_stand.spr",
                    resourceKind = ResourceKind.Sprite,
                    uid = resType.GetHashCode(),
                    discoveryTool = DiscoveryTool.Vltktool,
                    evidenceNote = "pc_npcs_full",
                };
            }

            if (PcItemCommon.ContainsReplacementChar(nameRaw))
                npc.warnings.Add($"NPC template id={templateId} name contains replacement char");

            return npc;
        }

        /// <summary>
        /// Import all 2,000 NPC templates into a NpcTemplateRegistry.
        /// Returns count of templates registered.
        /// </summary>
        public static int ImportIntoRegistry(string path, NpcTemplateRegistry registry)
        {
            if (registry == null) return 0;
            var templates = ParseFile(path);
            foreach (var t in templates)
                registry.Register(t);
            return templates.Count;
        }

        private static int EstimateLevel(string[] cols)
        {
            if (cols.Length <= 33) return 1;
            int exp0 = PcItemCommon.Int(cols, 33);
            int exp1 = PcItemCommon.Int(cols, 34);
            if (exp0 <= 0 && exp1 <= 0) return 1;
            int baseVal = exp0 > 0 ? exp0 : exp1;
            // Level approximation: exp grows exponentially
            if (baseVal < 10) return 1;
            if (baseVal < 50) return 10;
            if (baseVal < 100) return 30;
            if (baseVal < 200) return 50;
            if (baseVal < 500) return 70;
            if (baseVal < 1000) return 90;
            return 100;
        }

        private static int EstimateLife(string[] cols)
        {
            if (cols.Length <= 37) return 100;
            int life0 = PcItemCommon.Int(cols, 37);
            int life1 = PcItemCommon.Int(cols, 38);
            int life2 = PcItemCommon.Int(cols, 39);
            int life3 = PcItemCommon.Int(cols, 40);
            // Take the max parameter value as base life estimate
            int maxParam = System.Math.Max(System.Math.Max(life0, life1), System.Math.Max(life2, life3));
            return maxParam > 0 ? maxParam : 100;
        }

        private static int EstimateAttack(string[] cols)
        {
            // MinDamageParam (cols 50-53), MaxDamageParam (cols 54-57)
            if (cols.Length <= 54) return 10;
            int minDmg = PcItemCommon.Int(cols, 50);
            int maxDmg = PcItemCommon.Int(cols, 54);
            return System.Math.Max(minDmg, maxDmg > 0 ? maxDmg : 10);
        }

        private static int EstimateDefense(string[] cols)
        {
            // DefenseParam (cols 46-49)
            if (cols.Length <= 46) return 5;
            int def0 = PcItemCommon.Int(cols, 46);
            int def1 = PcItemCommon.Int(cols, 47);
            return System.Math.Max(def0, def1 > 0 ? def1 : 5);
        }
    }
}
