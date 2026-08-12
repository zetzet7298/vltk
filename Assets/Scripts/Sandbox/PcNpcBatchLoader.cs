// -----------------------------------------------------------------------------
// VLTK Mobile — PC NPC batch loader (2,000 NPCs + rare + goldboss)
// Purpose: load npcs.txt, rare.txt, goldboss.txt in one call and register into
// the NpcTemplateRegistry. Pure C# (no MonoBehaviour) for EditMode testability.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public sealed class PcNpcBatchResult
    {
        public Dictionary<string, int> perFileCounts = new();
        public int totalTemplates;
        public List<string> warnings = new();
    }

    public static class PcNpcBatchLoader
    {
        public static PcNpcBatchResult LoadAll(string npcDir, NpcTemplateRegistry registry)
        {
            var result = new PcNpcBatchResult();
            if (string.IsNullOrEmpty(npcDir) || !Directory.Exists(npcDir))
            {
                result.warnings.Add("NPC directory not found");
                return result;
            }
            if (registry == null)
            {
                result.warnings.Add("Registry is null");
                return result;
            }

            // Load main NPC templates (2,000)
            string npcsPath = Path.Combine(npcDir, "npcs.txt");
            if (File.Exists(npcsPath))
            {
                int count = PcNpcSFullParser.ImportIntoRegistry(npcsPath, registry);
                result.perFileCounts["npcs"] = count;
                result.totalTemplates += count;
            }
            else
            {
                result.warnings.Add("npcs.txt not found");
                result.perFileCounts["npcs"] = 0;
            }

            // NOTE (fix #14, 2026-06-12): PC rare.txt is NOT an NPC spawn/template
            // file — it is a RARE MAGIC-ATTRIBUTE / WEAPON-ENCHANT ROLL TABLE
            // (NAME, MAGIC_ID, MAG_P1_MIN/MAX, per-weapon/-slot/-element weights).
            // It is loaded by RareEnchantService, not the NPC batch loader, so it
            // is intentionally not read here and not counted as NPC templates.

            // Load gold boss templates (32)
            string bossPath = Path.Combine(npcDir, "goldboss.txt");
            if (File.Exists(bossPath))
            {
                var bossEntries = PcGoldBossParser.ParseFile(bossPath);
                result.perFileCounts["goldboss"] = bossEntries.Count;
                result.totalTemplates += bossEntries.Count;
            }
            else
            {
                result.perFileCounts["goldboss"] = 0;
            }

            return result;
        }
    }
}
