// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorSkillCatalogGenerator (editor-only)
// Menu: VLTK/Survivor/Generate Skill Catalog
// Reads the three JX sources from StreamingAssets/Reference, parses +
// resolves staging fail-closed, writes one SkillDef asset per skill under
// Assets/Scripts/Survivor/Skill/Generated/, plus _manifest.txt with counts
// (total / player pool / staged % / fail-closed lists).
// Re-running deletes + regenerates the folder (idempotent).
// #if UNITY_EDITOR: file lives in an Editor/ folder inside the runtime asmdef;
// the guard keeps it out of player builds.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VLTK.Survivor
{
    public static class SurvivorSkillCatalogGenerator
    {
        public const string OutputFolder = "Assets/Scripts/Survivor/Skill/Generated";
        public const string ManifestName = "_manifest.txt";

        [MenuItem("VLTK/Survivor/Generate Skill Catalog")]
        public static void GenerateSkillCatalog()
        {
            try
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var refDir = Path.Combine(projectRoot, "Assets", "StreamingAssets", "Reference");

                var pcSkills = File.ReadAllBytes(Path.Combine(refDir, "PcSkills.txt"));
                var display = File.ReadAllBytes(Path.Combine(refDir, "PcAllFactionLearnedDisplaySkills.txt"));
                var missiles = File.ReadAllBytes(Path.Combine(refDir, "PcAttrib", "missles.txt"));

                var staged = LoadStagedUids(Path.Combine(projectRoot, "SpritesRuntime"));
                var catalog = SurvivorSkillParser.Parse(pcSkills, display, missiles, uid => staged.Contains(uid));

                WriteAssets(catalog);
                LogSummary(catalog);
                Debug.Log($"[SurvivorSkillCatalog] generated {catalog.Skills.Count} SkillDefs → {OutputFolder}/");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SurvivorSkillCatalog] generate failed: {e}");
            }
        }

        private static HashSet<string> LoadStagedUids(string spritesRoot)
        {
            var set = new HashSet<string>();
            if (Directory.Exists(spritesRoot))
            {
                foreach (var f in Directory.GetFiles(spritesRoot, "*.spr"))
                    set.Add(Path.GetFileNameWithoutExtension(f).ToLowerInvariant());
            }
            else
            {
                Debug.LogWarning($"[SurvivorSkillCatalog] SpritesRuntime root missing: {spritesRoot} — all sprites fail-closed");
            }
            return set;
        }

        private static void WriteAssets(SurvivorSkillCatalog catalog)
        {
            if (AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.DeleteAsset(OutputFolder);
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scripts/Survivor/Skill/Generated"));
            AssetDatabase.Refresh();

            foreach (var row in catalog.Skills)
            {
                var def = SkillDef.FromRow(row);
                def.name = $"Skill_{row.Id:D5}";
                AssetDatabase.CreateAsset(def, $"{OutputFolder}/Skill_{row.Id:D5}.asset");
            }

            File.WriteAllText(
                Path.Combine(Application.dataPath, "Scripts/Survivor/Skill/Generated", ManifestName),
                BuildManifest(catalog));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string BuildManifest(SurvivorSkillCatalog c)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Survivor SkillDef catalog — generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z");
            sb.AppendLine($"pc_skills_rows={c.Skills.Count}");
            sb.AppendLine($"player_pool={c.PlayerPoolCount}");
            sb.AppendLine($"boss_npc_pool={c.Skills.Count - c.PlayerPoolCount}");
            sb.AppendLine($"display_file_rows={c.DisplayFileRows}");
            sb.AppendLine($"missile_rows={c.MissileRows}");
            sb.AppendLine($"precast_nonempty={c.PreCastNonEmpty}");
            sb.AppendLine($"precast_staged={c.PreCastStaged}");
            sb.AppendLine($"precast_staged_pct={(c.PreCastNonEmpty > 0 ? (100f * c.PreCastStaged / c.PreCastNonEmpty).ToString("F1") : "0")}");
            sb.AppendLine($"child_visual_resolved={c.ChildVisualResolved}");
            AppendList(sb, "fail_closed_no_precast_staged", c.FailClosedNoPreCastStaged);
            AppendList(sb, "fail_closed_no_child_missile_row", c.FailClosedNoChildMissileRow);
            AppendList(sb, "fail_closed_no_child_anim_file", c.FailClosedNoChildAnimFile);
            AppendList(sb, "fail_closed_no_child_anim_staged", c.FailClosedNoChildAnimStaged);
            return sb.ToString();
        }

        private static void AppendList(StringBuilder sb, string section, List<SkillFailEntry> list)
        {
            sb.AppendLine();
            sb.AppendLine($"[{section}] count={list.Count}");
            foreach (var e in list)
                sb.AppendLine($"{e.SkillId}|{e.Detail}|{e.Path}");
        }

        private static void LogSummary(SurvivorSkillCatalog c)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[SurvivorSkillCatalog] total skills={c.Skills.Count} player={c.PlayerPoolCount} boss/npc={c.Skills.Count - c.PlayerPoolCount}");
            sb.AppendLine($"[SurvivorSkillCatalog] display rows={c.DisplayFileRows} missile rows={c.MissileRows}");
            sb.AppendLine($"[SurvivorSkillCatalog] precast {c.PreCastStaged}/{c.PreCastNonEmpty} staged ({(c.PreCastNonEmpty > 0 ? 100f * c.PreCastStaged / c.PreCastNonEmpty : 0):F1}%), child visual resolved={c.ChildVisualResolved}");
            sb.AppendLine($"[SurvivorSkillCatalog] fail-closed: noPreCastStaged={c.FailClosedNoPreCastStaged.Count} noChildMissileRow={c.FailClosedNoChildMissileRow.Count} noChildAnimFile={c.FailClosedNoChildAnimFile.Count} noChildAnimStaged={c.FailClosedNoChildAnimStaged.Count}");
            foreach (var e in c.FailClosedNoChildMissileRow)
                sb.AppendLine($"[SurvivorSkillCatalog]   child-missing: skill {e.SkillId} child={e.Path}");
            Debug.Log(sb.ToString());
        }
    }
}
#endif
