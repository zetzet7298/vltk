// -----------------------------------------------------------------------------
// VLTK Mobile — Generalized multi-map enemy/NPC spawn database.
// Replaces BaLangEnemyDatabase for all maps.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Model;
using VLTK.Sprites;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Per-map enemy/NPC database. Each map has its own set of enemy templates
    /// derived from PC NpcS.txt and Region_S.dat spawn data.
    /// Centralized registry for all maps' NPC templates.
    /// </summary>
    public static class MapEnemyDatabase
    {
        private static bool _pcNpcsLoaded;

        /// <summary>Load full PC NPC catalog from StreamingAssets if not already loaded.</summary>
        public static void EnsurePcNpcsLoaded()
        {
            if (_pcNpcsLoaded) return;
            _pcNpcsLoaded = true;
            var npcDir = System.IO.Path.Combine(
                UnityEngine.Application.streamingAssetsPath, "Reference/PcNpc");
            if (!System.IO.Directory.Exists(npcDir)) return;
            var templates = PcNpcSFullParser.ParseFile(
                System.IO.Path.Combine(npcDir, "npcs.txt"));
            foreach (var t in templates)
            {
                if (t == null || t.templateId <= 0) continue;
                if (_templateLookup.TryGetValue(t.templateId, out var existing) && IsCuratedTemplate(t.templateId))
                    MergePcTemplateIntoCurated(existing, t);
                else
                    _templateLookup[t.templateId] = t;
            }
        }
        // Common enemy templates shared across outdoor maps.
        // Templates derived from PC NpcS.txt + KNpc.cpp.
        private static readonly NpcTemplate[] SharedTemplates = new[]
        {
            // Ba Lăng Huyện (Map 79) enemies
            MakeTemplate(31, "金猫", "Mèo vàng", "ani049", 0, 1, 100, 6, 6, 400, 700, 1, 60, 10, 10, 0, 0, 0, 20, 0, 0),
            MakeTemplate(42, "梅花鹿", "Hươu đốm", "ani061", 0, 2, 100, 4, 4, 400, 700, 4, 80, 40, 10, 0, 0, 0, 10, 0, 0),
            MakeTemplate(43, "白猪", "Heo trắng", "ani063", 0, 3, 100, 3, 3, 400, 700, 4, 60, 20, 10, 0, 0, 0, 10, 0, 0),

            // Giang Tân Thôn / Đào Hoa Đảo enemies (Map 2, 3)
            MakeTemplate(35, "野兔", "Thỏ hoang", "ani053", 0, 4, 80, 5, 5, 350, 600, 1, 50, 10, 10, 0, 0, 0, 15, 0, 0),
            MakeTemplate(37, "灰狼", "Sói xám", "ani055", 0, 0, 150, 5, 7, 450, 800, 3, 70, 30, 15, 0, 0, 0, 25, 0, 0),
            MakeTemplate(38, "野猪", "Lợn rừng", "ani056", 0, 3, 180, 3, 4, 400, 700, 4, 60, 20, 10, 0, 0, 0, 12, 0, 0),
            MakeTemplate(39, "毒蛇", "Rắn độc", "ani057", 0, 2, 90, 6, 8, 300, 500, 2, 40, 10, 20, 0, 0, 0, 30, 0, 0),

            // Tương Dương (Map 11) enemies
            MakeTemplate(50, "老虎", "Hổ", "ani070", 0, 0, 300, 5, 7, 500, 900, 3, 80, 40, 15, 0, 0, 0, 35, 0, 0),
            MakeTemplate(51, "黑熊", "Gấu đen", "ani071", 0, 3, 400, 3, 4, 450, 800, 4, 70, 30, 10, 0, 0, 0, 40, 0, 0),
            MakeTemplate(52, "鳄鱼", "Cá sấu", "ani072", 0, 2, 350, 4, 5, 400, 700, 2, 60, 20, 20, 0, 0, 0, 35, 0, 0),

            // Thành Đô outskirts (Map 37) enemies
            MakeTemplate(55, "山贼", "Cướp núi", "enemy178", 0, 0, 500, 4, 6, 500, 1000, 5, 100, 50, 20, 0, 0, 0, 50, 0, 0),
            MakeTemplate(56, "毒蛛", "Nhện độc", "ani075", 0, 2, 200, 5, 6, 350, 600, 2, 50, 20, 15, 0, 0, 0, 30, 0, 0),
            MakeTemplate(57, "蝎子", "Bọ cạp", "ani076", 0, 0, 220, 4, 5, 300, 550, 3, 45, 15, 10, 0, 0, 0, 25, 0, 0),

            // Đại Lý outskirts (Map 80) enemies
            MakeTemplate(60, "大象", "Voi", "ani080", 0, 3, 600, 3, 4, 500, 1000, 4, 90, 40, 15, 0, 0, 0, 55, 0, 0),
            MakeTemplate(61, "孔雀", "Công", "ani081", 0, 4, 120, 5, 5, 350, 600, 1, 50, 10, 10, 0, 0, 0, 15, 0, 0),
            MakeTemplate(62, "蟒蛇", "Trăn", "ani082", 0, 2, 350, 4, 6, 400, 700, 3, 70, 30, 15, 0, 0, 0, 40, 0, 0),

            // Biện Kinh (Map 78) / Lâm An (Map 103) enemies
            MakeTemplate(65, "响马", "Cướp đường", "enemy200", 0, 0, 450, 5, 7, 500, 1000, 5, 90, 40, 20, 0, 0, 0, 45, 0, 0),
            MakeTemplate(66, "恶犬", "Chó dữ", "ani085", 0, 0, 150, 7, 9, 400, 700, 2, 60, 20, 10, 0, 0, 0, 20, 0, 0),
            MakeTemplate(67, "蝙蝠", "Dơi", "ani086", 0, 2, 100, 6, 8, 300, 500, 1, 40, 10, 15, 0, 0, 0, 10, 0, 0),

            // Quảng Châu (Map 176) / Phượng Tường (Map 121)
            MakeTemplate(70, "水牛", "Trâu nước", "ani090", 0, 3, 250, 3, 3, 350, 600, 4, 60, 20, 10, 0, 0, 0, 20, 0, 0),
            MakeTemplate(71, "毒蛙", "Ếch độc", "ani091", 0, 2, 130, 5, 5, 300, 500, 2, 45, 15, 10, 0, 0, 0, 15, 0, 0),
            MakeTemplate(72, "猛虎", "Hổ dữ", "ani092", 0, 0, 500, 6, 8, 550, 1000, 3, 90, 40, 20, 0, 0, 0, 50, 0, 0),

            // Vượt ải Nhiếp Thí Trần / killbossmatch (PC script tbNpc ids 1480..1489).
            MakeTemplate(1481, "gubo_Christmas", "Nhất quỷ", "boss018", 0, 0, 4200000, 18, 18, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
            MakeTemplate(1485, "tangburan_Christmas", "Nhị quỷ", "boss019", 0, 1, 4500000, 6, 6, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
            MakeTemplate(1488, "lanyiyi_Christmas", "Tam quỷ", "boss008", 0, 1, 3600000, 12, 12, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
            MakeTemplate(1483, "helianpiao_Christmas", "Tứ quỷ", "boss002", 0, 2, 3100000, 12, 12, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
            MakeTemplate(1482, "zhonglingxiu_Christmas", "Ngũ quỷ", "boss005", 0, 2, 3100000, 12, 12, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
            MakeTemplate(1480, "duanmurui_Christmas", "Lục quỷ", "boss015", 0, 3, 3600000, 12, 12, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
            MakeTemplate(1489, "mengcanglang_Christmas", "Thất quỷ", "boss012", 0, 3, 4200000, 12, 12, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
            MakeTemplate(1486, "shansinan_Christmas", "Bát quỷ", "boss022", 0, 4, 4800000, 12, 12, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
            MakeTemplate(1487, "xuanjizi_Christmas", "Cửu quỷ", "boss017", 0, 4, 4800000, 12, 12, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
            MakeTemplate(1484, "wangzuo_Christmas", "Thập quỷ", "boss024", 0, 0, 8000000, 15, 15, 1800, 2000, 1, 50, 25, 25, 25, 25, 30),
        };

        /// <summary>
        /// Map-id to default enemy template IDs. Derived from PC Region_S spawn distribution.
        /// </summary>
        private static readonly Dictionary<int, int[]> MapEnemyTemplates = new()
        {
            [MapPortManifest.BaLangHuyenId] = new[] { 31, 42, 43 },
            [MapPortManifest.GiangTanThonId] = new[] { 35, 37, 38 },
            [MapPortManifest.DaoHoaDaoId] = new[] { 37, 39, 43 },
            [MapPortManifest.TuongDuongId] = new[] { 37, 50, 51 },
            [MapPortManifest.ThanhDoId] = new[] { 55, 56, 57 },
            [MapPortManifest.DaiLyId] = new[] { 60, 61, 62 },
            [MapPortManifest.BienKinhId] = new[] { 65, 66, 67 },
            [MapPortManifest.LamAnId] = new[] { 55, 66, 67 },
            [MapPortManifest.PhuongTuongId] = new[] { 50, 70, 72 },
            // PC source: global/autoexec.lua spawns Bạch Dực (822) and Dịch quan (377) on map 389.
            [MapPortManifest.TinSuVuotAiPhongKy120Id] = new[] { 822, 377 },
            // PC source: missions/killbossmatch/class.lua tbMapId={907..916}, tbNpc={1480..1489}.
            [MapPortManifest.VuotAiNhiepThiTranId] = new[] { 1481, 1485, 1488, 1483, 1482, 1480, 1489, 1486, 1487, 1484 },
        };

        /// <summary>Default spawn points per map (training area or town center from PC data).</summary>
        private static readonly Dictionary<int, Vector2> DefaultSpawnPoints = new()
        {
            // Ba Lăng training pentagon center (verified PC coordinates)
            [MapPortManifest.BaLangHuyenId] = new Vector2(53246f, -52041f),
            // Giang Tân Thôn village center
            [MapPortManifest.GiangTanThonId] = new Vector2(48000f, -46000f),
            // Đào Hoa Đảo pier
            [MapPortManifest.DaoHoaDaoId] = new Vector2(52000f, -50000f),
            // Tương Dương town square
            [MapPortManifest.TuongDuongId] = new Vector2(55000f, -54000f),
            // Thành Đô market area
            [MapPortManifest.ThanhDoId] = new Vector2(50000f, -48000f),
            // Đại Lý city center
            [MapPortManifest.DaiLyId] = new Vector2(47000f, -45000f),
            // Biện King imperial city
            [MapPortManifest.BienKinhId] = new Vector2(53000f, -51000f),
            // Lâm An lakeside
            [MapPortManifest.LamAnId] = new Vector2(49000f, -47000f),
            // Phượng Tường town
            [MapPortManifest.PhuongTuongId] = new Vector2(50000f, -48000f),
            // Tín sứ vượt ải / Phong Kỳ 120+: wagoner.lua NewWorld(389,1582,3137).
            [MapPortManifest.TinSuVuotAiPhongKy120Id] = new Vector2(50624f, -50208f),
            // Vượt ải Nhiếp Thí Trần: killbossmatch/class.lua NewWorld(907,1476,3274).
            [MapPortManifest.VuotAiNhiepThiTranId] = new Vector2(47232f, -52544f),
            // Đấu trường liên đấu Kiệt xuất: Center of the map geometry
            [MapPortManifest.DauTruongLienDauId] = new Vector2(53248f, -55296f),
            // Lâm Du Quan default spawn point
            [MapPortManifest.LamDuQuanId] = new Vector2(50500f, -12400f),
        };

        private static readonly Dictionary<int, NpcTemplate> _templateLookup;

        static MapEnemyDatabase()
        {
            _templateLookup = new Dictionary<int, NpcTemplate>();
            foreach (var t in SharedTemplates)
                _templateLookup[t.templateId] = t;
        }

        private static bool IsCuratedTemplate(int templateId)
        {
            foreach (var template in SharedTemplates)
                if (template.templateId == templateId)
                    return true;
            return false;
        }

        private static void MergePcTemplateIntoCurated(NpcTemplate curated, NpcTemplate pc)
        {
            if (curated == null || pc == null) return;
            curated.kind = pc.kind;
            curated.series = pc.series;
            curated.walkSpeed = pc.walkSpeed > 0 ? pc.walkSpeed : curated.walkSpeed;
            curated.runSpeed = pc.runSpeed > 0 ? pc.runSpeed : curated.runSpeed;
            curated.visionRadius = pc.visionRadius > 0 ? pc.visionRadius : curated.visionRadius;
            curated.activeRadius = pc.activeRadius > 0 ? pc.activeRadius : curated.activeRadius;
            curated.aiMode = pc.aiMode > 0 ? pc.aiMode : curated.aiMode;
            curated.aiParams = pc.aiParams != null && pc.aiParams.Length > 0 ? pc.aiParams : curated.aiParams;
            curated.scriptRef = string.IsNullOrEmpty(pc.scriptRef) ? curated.scriptRef : pc.scriptRef;
            curated.levelScriptRef = string.IsNullOrEmpty(pc.levelScriptRef) ? curated.levelScriptRef : pc.levelScriptRef;
            curated.attack = pc.attack > 0 ? pc.attack : curated.attack;
            curated.defense = pc.defense > 0 ? pc.defense : curated.defense;
            curated.maxLife = Mathf.Max(curated.maxLife, pc.maxLife);
            if (string.IsNullOrEmpty(curated.spriteClipRef))
                curated.spriteClipRef = pc.spriteClipRef;
        }

        public static NpcTemplate Resolve(int templateId)
        {
            EnsurePcNpcsLoaded();
            _templateLookup.TryGetValue(templateId, out var t);
            return t;
        }

        public static void RegisterAllForMap(int mapId, NpcTemplateRegistry registry)
        {
            if (registry == null) return;
            EnsurePcNpcsLoaded();
            // Always register Ba Lăng templates as base
            BaLangEnemyDatabase.RegisterAll(registry);
            // Register map-specific templates
            if (MapEnemyTemplates.TryGetValue(mapId, out var ids))
            {
                foreach (var id in ids)
                {
                    var t = Resolve(id);
                    if (t != null) registry.Register(t);
                }
            }
            // Register all PC NPCs that match this map's templates
            foreach (var kvp in _templateLookup)
            {
                if (!registry.Contains(kvp.Key))
                    registry.Register(kvp.Value);
            }
        }

        public static void RegisterAll(NpcTemplateRegistry registry)
        {
            if (registry == null) return;
            foreach (var t in SharedTemplates)
                registry.Register(t);
        }

        public static int[] GetEnemyTemplateIdsForMap(int mapId)
        {
            return MapEnemyTemplates.TryGetValue(mapId, out var ids) ? ids : new[] { 31, 42, 43 };
        }

        public static Vector2 GetDefaultSpawnPoint(int mapId)
        {
            return DefaultSpawnPoints.TryGetValue(mapId, out var sp) ? sp : new Vector2(50000f, -48000f);
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

        /// <summary>
        /// Convert PC MPS (global pixel) coordinates to Unity world coordinates.
        /// PC: regionRow = mpsY / 1024, regionCol = mpsX / 1024.
        /// Unity: worldX = mpsX, worldY = -(mpsY - regionRow * 512).
        /// </summary>
        public static Vector2 MpsToWorld(int mpsX, int mpsY)
        {
            int regionRow = mpsY / 1024;
            float worldX = mpsX;
            float worldY = -(mpsY - regionRow * 512f);
            return new Vector2(worldX, worldY);
        }

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
            foreach (var candidate in CandidateNpcSprPaths(resType, action))
            {
                if (IsNpcSprAvailable(candidate))
                    return candidate;
            }
            return BuildNpcSprPathExact(resType, action);
        }

        private static string BuildNpcSprPathExact(string resType, string action)
        {
            string folder = NpcResFolder(resType);
            if (string.Equals(action, "base", StringComparison.OrdinalIgnoreCase))
                return $@"spr\npcres\{folder}\{resType}\{resType}.spr";
            return $@"spr\npcres\{folder}\{resType}\{resType}_{action}.spr";
        }

        private static string NpcResFolder(string resType)
        {
            if (resType.StartsWith("ani", StringComparison.OrdinalIgnoreCase)) return "animal";
            if (resType.StartsWith("boss", StringComparison.OrdinalIgnoreCase)) return "boss";
            if (resType.StartsWith("passerby", StringComparison.OrdinalIgnoreCase)) return "passerby";
            if (resType.StartsWith("critter", StringComparison.OrdinalIgnoreCase)) return "critter";
            return "enemy";
        }

        private static IEnumerable<string> CandidateNpcSprPaths(string resType, string requestedAction)
        {
            if (!string.IsNullOrWhiteSpace(requestedAction))
                yield return BuildNpcSprPathExact(resType, requestedAction);
            if (resType.StartsWith("passerby", StringComparison.OrdinalIgnoreCase))
            {
                string folder = NpcResFolder(resType);
                yield return $@"spr\npcres\{folder}\{resType}\{resType}z.spr";
                yield return $@"spr\npcres\{folder}\{resType}\{resType}s.spr";
                foreach (var action in new[] { "wlk", "st", "st01", "pst", "base", "die", "st02" })
                {
                    if (!string.Equals(action, requestedAction, StringComparison.OrdinalIgnoreCase))
                        yield return BuildNpcSprPathExact(resType, action);
                }
            }
            else
            {
                foreach (var action in new[] { "wlk", "st", "die" })
                {
                    if (!string.Equals(action, requestedAction, StringComparison.OrdinalIgnoreCase))
                        yield return BuildNpcSprPathExact(resType, action);
                }
            }
        }

        private static bool IsNpcSprAvailable(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) return false;
            string uid = SprRuntimeService.ComputePathUidHex(sourcePath);
            foreach (var root in EnumerateNpcSpriteRoots())
            {
                if (File.Exists(Path.Combine(root, uid + ".spr")))
                    return true;
            }
            return false;
        }

        private static IEnumerable<string> EnumerateNpcSpriteRoots()
        {
            var root = Application.streamingAssetsPath;
            yield return Path.Combine(root, "Sprites");
            yield return Path.Combine(root, "Generated", "NpcSprites");
        }

        public static bool IsTrainerSpawn(int templateId)
        {
            return BaLangEnemyDatabase.IsTrainerSpawn(templateId);
        }

        public static string VietnameseTrainerName(int templateId, string rawName)
        {
            return BaLangEnemyDatabase.VietnameseTrainerName(templateId, rawName);
        }

        private static NpcTemplate MakeTemplate(int id, string raw, string vi, string resType,
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
