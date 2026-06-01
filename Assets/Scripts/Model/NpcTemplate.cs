using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>
    /// M3.1 — NPC template derived from PC source config (KNpcType / NpcRes).
    /// A spawn point references a template by id; the template carries the display
    /// name, basic stats, and resource/script references where known. Missing
    /// references are tracked so validation can report them.
    /// </summary>
    [Serializable]
    public class NpcTemplate
    {
        public int templateId;
        public string nameRaw;
        public string nameNormalized;

        // Basic stats (defaults when source omits them).
        public int level;
        public int maxLife;
        public int attack;
        public int defense;

        // PC combat/AI identity from Settings/NpcS.txt.
        public int kind;
        public int series;
        public int walkSpeed;
        public int runSpeed;
        public int visionRadius;
        public int activeRadius;
        public int aiMode;
        public int[] aiParams;

        // Resource references (resolved through the asset registry).
        public SourceAssetId spriteSourceId;   // body sprite (.spr)
        public string spriteClipRef;           // atlas/clip key once decoded
        public string scriptRef;               // Lua script id/name

        // Resolution flags stamped during validation.
        public bool spriteResolved;
        public bool scriptResolved;

        public List<string> warnings = new();

        public string DisplayName =>
            !string.IsNullOrEmpty(nameNormalized) ? nameNormalized :
            !string.IsNullOrEmpty(nameRaw) ? nameRaw : $"NPC_{templateId}";
    }

    /// <summary>Missing-resource report entry for a template (M3.1 AC#3).</summary>
    [Serializable]
    public class NpcResourceIssue
    {
        public int templateId;
        public string kind;   // "sprite" | "script"
        public string sourceKey;
        public string message;
    }
}
