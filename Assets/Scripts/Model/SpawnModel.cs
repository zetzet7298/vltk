using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>
    /// M1.6 — Trap/Trigger region data from the region .dat trap section.
    /// Preserves bounds, script references, and trigger type for sandbox visualization.
    /// </summary>
    [Serializable]
    public enum TrapTriggerType
    {
        Unknown = 0,
        Enter = 1,       // triggered when entity enters
        Exit = 2,        // triggered when entity exits
        Timed = 3,       // periodic trigger
        Click = 4,       // player-activated
        Passive = 5,     // always active
    }

    [Serializable]
    public class TrapDefinition
    {
        /// <summary>Unique id within the region (index in trap array).</summary>
        public int trapIndex;

        /// <summary>Bounding area of the trap in region-local cell coordinates.</summary>
        public RectDef boundsRect;

        /// <summary>Script id or name from PC source (may be int or string).</summary>
        public string scriptRef;

        /// <summary>Trigger type as understood from PC source.</summary>
        public TrapTriggerType triggerType;

        /// <summary>True if a corresponding script was found during conversion.</summary>
        public bool scriptFound;

        /// <summary>Raw trap bytes for diagnostics (optional, may be null).</summary>
        public byte[] rawData;

        /// <summary>Conversion warnings for this trap.</summary>
        public List<string> warnings = new();
    }

    /// <summary>
    /// Container for all trap definitions in a region.
    /// </summary>
    [Serializable]
    public class RegionTrapManifest
    {
        public int mapId;
        public int regionX;
        public int regionY;
        public string sourceRegionFile;
        public List<TrapDefinition> traps = new();
        public int totalTraps;
        public int missingScripts;
        public ConversionStatus status;
    }

    /// <summary>
    /// M1.7 — NPC/Object spawn table from the region .dat npc/obj sections.
    /// </summary>
    [Serializable]
    public enum NpcDirection
    {
        South = 0, SouthWest = 1, West = 2, NorthWest = 3,
        North = 4, NorthEast = 5, East = 6, SouthEast = 7,
    }

    [Serializable]
    public class NpcSpawn
    {
        public int spawnIndex;
        public int templateId;      // NPC template/type id from PC source
        public string scriptRef;    // associated Lua script reference
        public float posX;
        public float posY;
        public NpcDirection direction;
        public int regionX;
        public int regionY;
        public bool templateFound;
        public List<string> warnings = new();
    }

    [Serializable]
    public class ObjectPlacement
    {
        public int placementIndex;
        public int spriteId;
        public string spritePath;
        public float posX;
        public float posY;
        public int layer;
        public int zOrder;
        public int flags;
        public bool isForeground;   // M1.4 AC#2: foreground behavior flag
        public bool spriteMissing;  // M1.4 AC#3: for placeholder
        public List<string> warnings = new();
    }

    [Serializable]
    public class RegionSpawnManifest
    {
        public int mapId;
        public int regionX;
        public int regionY;
        public string sourceRegionFile;
        public List<NpcSpawn> npcSpawns = new();
        public List<ObjectPlacement> objects = new();
        public int totalNpcs;
        public int totalObjects;
        public int missingTemplates;
        public int missingSprites;
        public ConversionStatus status;
    }
}
