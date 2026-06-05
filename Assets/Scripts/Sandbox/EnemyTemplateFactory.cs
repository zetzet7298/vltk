// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.1 Enemy Template Factory
// Generalizes BaLangEnemyDatabase hardcoded templates into a factory that
// reads from PcNpcS.txt via PcConfigParser or falls back to existing data.
// Source: PcNpcS.txt (kind=0 rows), Region_S.dat spawn positions.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Factory tạo NPC templates từ PC config. Hỗ trợ toàn bộ enemy types
    /// từ PcNpcS.txt (kind=0 = quái vật, kind=3 = NPC thị trấn).
    /// Thay thế hardcoded data trong BaLangEnemyDatabase.
    /// </summary>
    public static class EnemyTemplateFactory
    {
        private static PcConfigManifest _manifest;

        /// <summary>Set the PC config manifest for dynamic template loading.</summary>
        public static void SetManifest(PcConfigManifest manifest)
        {
            _manifest = manifest;
        }

        /// <summary>
        /// Tạo toàn bộ enemy templates. Nếu có manifest → đọc từ PcNpcS.txt.
        /// Nếu không → fallback sang BaLangEnemyDatabase hardcoded data.
        /// </summary>
        public static IEnumerable<NpcTemplate> CreateAllTemplates()
        {
            if (_manifest != null && _manifest.npcTemplates.Count > 0)
            {
                foreach (var npc in _manifest.npcTemplates)
                {
                    if (npc.kind == 0) // kind=0 = enemy/quái
                        yield return npc;
                }
            }
            else
            {
                // Fallback: Ba Lang hardcoded enemies
                foreach (var t in BaLangEnemyDatabase.CreateTemplates())
                    yield return t;
            }
        }

        /// <summary>Tạo templates cho một map cụ thể.</summary>
        public static IEnumerable<NpcTemplate> CreateTemplatesForMap(int mapId)
        {
            // Map-specific template sets
            if (mapId == BaLangEnemyDatabase.MapId)
            {
                foreach (var t in BaLangEnemyDatabase.CreateTemplates())
                    yield return t;
                yield break;
            }

            // Generic: return all enemy templates
            foreach (var t in CreateAllTemplates())
                yield return t;
        }

        /// <summary>
        /// Đăng ký toàn bộ templates vào NpcTemplateRegistry.
        /// Ưu tiên manifest data, fallback sang hardcoded.
        /// </summary>
        public static void RegisterAll(NpcTemplateRegistry registry, PcConfigManifest manifest = null)
        {
            if (manifest != null) SetManifest(manifest);

            foreach (var template in CreateAllTemplates())
                registry.Register(template);
        }

        /// <summary>Tên hiển thị tiếng Việt kèm ngũ hành.</summary>
        public static string DisplayNameWithSeries(NpcTemplate template)
        {
            return BaLangEnemyDatabase.DisplayNameWithSeries(template);
        }

        /// <summary>Tên ngũ hành tiếng Việt.</summary>
        public static string VietnameseSeriesName(int series)
        {
            return BaLangEnemyDatabase.VietnameseSeriesName(series);
        }

        /// <summary>Kiểm tra template có phải enemy (kind=0).</summary>
        public static bool IsEnemy(NpcTemplate template) => template != null && template.kind == 0;

        /// <summary>Kiểm tra template có phải town NPC (kind=3).</summary>
        public static bool IsTownNpc(NpcTemplate template) => template != null && template.kind == 3;

        /// <summary>Kiểm tra template có phải trainer.</summary>
        public static bool IsTrainer(int templateId) => BaLangEnemyDatabase.IsTrainerSpawn(templateId);
    }
}
