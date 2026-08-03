// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorSkillCatalogService (ticket 43 wiring)
// Runtime loader cho skill catalog — route (b): parse trực tiếp từ
// StreamingAssets/Reference (PcSkills.txt + PcAllFactionLearnedDisplaySkills.txt
// + PcAttrib/missles.txt) qua SurvivorSkillParser, KHÔNG cần editor generator.
//
// Vì sao route này (không phải Generated/ .asset):
//  - StreamingAssets có sẵn trong player build (Generator OutputFolder nằm
//    ngoài Resources/Addressables → .asset không load được runtime).
//  - Spec D17: "SkillDef/drop table/wave table/level curve = ScriptableObject
//    hoặc text config (StreamingAssets) tự author" — text config hợp lệ.
//  - Parser thuần đã verified 258/258 EditMode; generator chỉ là editor sugar.
//
// Fail-closed: file thiếu / parse lỗi → catalog rỗng (SkillService tồn tại với
// pool rỗng → levelup rơi về legacy P1; boss pool rỗng → boss chase-only;
// supply defs rỗng → heal/bomb disabled). KHÔNG crash, KHÔNG bịa path.
// isStaged = listing /SpritesRuntime/*.spr (cùng convention SprRuntimeService
// DefaultSpritesRoot); thiếu root → toàn bộ visual fail-closed proxy (cast vẫn
// chạy — parity AGENTS.md).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Sprites;

namespace VLTK.Survivor
{
    public static class SurvivorSkillCatalogService
    {
        public const string ReferenceDir = "Reference";
        public const string PcSkillsFile = "PcSkills.txt";
        public const string DisplayFile = "PcAllFactionLearnedDisplaySkills.txt";
        public const string MissilesSubDir = "PcAttrib";
        public const string MissilesFile = "missles.txt";

        /// <summary>
        /// Đọc + parse catalog từ StreamingAssets lúc runtime. isStaged = listing
        /// SpritesRuntime (signed/unsigned hash do parser tự probe qua delegate).
        /// Lỗi bất kỳ → catalog rỗng (fail-closed), log 1 warning.
        /// </summary>
        public static SurvivorSkillCatalog LoadFromStreamingAssets()
        {
            try
            {
                string refDir = Path.Combine(Application.streamingAssetsPath, ReferenceDir);
                var pc = File.ReadAllBytes(Path.Combine(refDir, PcSkillsFile));
                var display = File.ReadAllBytes(Path.Combine(refDir, DisplayFile));
                var missiles = File.ReadAllBytes(Path.Combine(refDir, MissilesSubDir, MissilesFile));
                var staged = LoadStagedUids();
                return SurvivorSkillParser.Parse(pc, display, missiles, uid => staged.Contains(uid));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SurvivorSkillCatalog] load fail-closed (catalog rỗng): " + e.Message);
                return new SurvivorSkillCatalog();
            }
        }

        /// <summary>
        /// Lọc catalog → List SkillDef của 1 pool (Player / BossNpc), thứ tự giữ
        /// theo file. catalog null/rỗng → list rỗng (fail-closed).
        /// </summary>
        public static List<SkillDef> Defs(SurvivorSkillCatalog catalog, SurvivorSkillPool pool)
        {
            var res = new List<SkillDef>();
            if (catalog == null) return res;
            for (int i = 0; i < catalog.Skills.Count; i++)
            {
                var row = catalog.Skills[i];
                if (row.Pool != pool) continue;
                res.Add(SkillDef.FromRow(row));
            }
            return res;
        }

        /// <summary>SkillDef có SupplyTag Heal/Bomb (setup supply slots); Aura = passive, skip.</summary>
        public static List<SkillDef> SupplyDefs(SurvivorSkillCatalog catalog)
        {
            var res = new List<SkillDef>();
            if (catalog == null) return res;
            for (int i = 0; i < catalog.Skills.Count; i++)
            {
                var row = catalog.Skills[i];
                if (row.SupplyTag == SurvivorSupplyTag.Heal || row.SupplyTag == SurvivorSupplyTag.Bomb)
                    res.Add(SkillDef.FromRow(row));
            }
            return res;
        }

        private static HashSet<string> LoadStagedUids()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                // cùng convention SprRuntimeService: root = <project>/SpritesRuntime
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SprRuntimeService.DefaultSpritesRoot));
                if (!Directory.Exists(root)) return set;
                foreach (var f in Directory.GetFiles(root, "*.spr"))
                    set.Add(Path.GetFileNameWithoutExtension(f));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SurvivorSkillCatalog] staged listing fail-closed (visual proxy): " + e.Message);
            }
            return set;
        }
    }
}
