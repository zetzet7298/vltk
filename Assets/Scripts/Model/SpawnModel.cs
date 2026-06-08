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

        /// <summary>Raw PC KSPTrap.uTrapId from Region_S Trap.dat.</summary>
        public uint trapId;

        /// <summary>Hex display of <see cref="trapId"/> for diagnostics.</summary>
        public string trapIdHex;

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

    /// <summary>
    /// Rare spawn entry (PC rare.txt). The PC source uses this file as a magic
    /// enhancement rate table; the data fields below are a best-effort mapping
    /// to the spawn-table model so a registry can index it by template id.
    /// When the source lacks coordinate/drop-rate columns, those fields stay 0/null.
    /// </summary>
    [Serializable]
    public class RareSpawnEntry
    {
        public int entryId;
        public string nameRaw;
        public string nameNormalized;
        public int mapId;
        public int npcTemplateId;
        public int positionX;
        public int positionY;
        public int respawnSec;
        public string dropRateFile;
        public int magicId;
        public int levelMin;
        public int levelMax;
        public List<string> warnings = new();
    }

    /// <summary>
    /// Gold boss entry (PC goldboss.txt). The PC source uses this file to describe
    /// boss damage bases, aura skills, and passive skills; coordinates/drop files
    /// are not part of the source and remain default values.
    /// </summary>
    [Serializable]
    public class GoldBossEntry
    {
        public int bossTemplateId;
        public string nameRaw;
        public string nameNormalized;
        public int level;
        public int mapId;
        public int positionX;
        public int positionY;
        public string dropRateFile;
        public int respawnHours;
        public int physicalDamageBase;
        public int poisonDamageBase;
        public int coldDamageBase;
        public int fireDamageBase;
        public int lightingDamageBase;
        public string auraSkillName;
        public int auraSkillLevel;
        public string passiveSkillName;
        public int passiveSkillLevel;
        public List<string> warnings = new();
    }

    /// <summary>
    /// Generalized PC spawn-point record. The model carries the monster-spawn
    /// fields (mapId, x/y, direction, count, respawn, ai, group) plus optional
    /// template identity and level. When the PC source file does not provide
    /// some of these columns (e.g. settings/normal.txt is an item equipment
    /// table that has no map/x/y fields), the parser leaves the spawn fields
    /// at their default 0 values and records the gap in `warnings`.
    /// </summary>
    [Serializable]
    public class SpawnPoint
    {
        public int mapId;
        public int npcTemplateId;
        public int x;
        public int y;
        public int direction;
        public int count;
        public int level;
        public int respawnSec;
        public int aiMode;
        public int groupId;
        public string nameRaw;
        public string sourceFile;
        public int rowIndex;
        public List<string> warnings = new();
    }
}
