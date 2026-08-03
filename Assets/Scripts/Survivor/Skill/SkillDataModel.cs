// -----------------------------------------------------------------------------
// VLTK.Survivor — SkillDataModel
// Pure data model for the JX skill catalog pipeline (ticket 26). No Unity
// dependencies — the parse/resolve logic lives in SurvivorSkillParser and is
// covered by EditMode pure-logic tests. SkillDef (ScriptableObject) mirrors
// SkillRow for asset generation.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Survivor
{
    /// <summary>Player-learnable pool (10 factions) vs boss/npc/event pool.</summary>
    public enum SurvivorSkillPool { Player, BossNpc }

    /// <summary>
    /// Supply-skill subset tag (spec D2 / ticket 26). heal = LvlSetting1
    /// lifereplenish_v/lifemax_v; bomb = physicsdamage_v or path *bomb.lua;
    /// aura = IsAura=1. Magnet has no JX skill source (own collect-mgr feature)
    /// → never tagged here.
    /// </summary>
    public enum SurvivorSupplyTag { None, Heal, Bomb, Aura }

    /// <summary>Resolved child-missile visual/behavior (missles.txt row).</summary>
    [Serializable]
    public class MissileVisualInfo
    {
        public int Id;
        /// <summary>Staged SPR UID of AnimFile2 (primary missile visual), or "" fail-closed.</summary>
        public string AnimFileUid = "";
        public int MoveKind;
        public float Speed;
        public float LifeTime;
        public int ResponseSkill;
    }

    /// <summary>One parsed PcSkills.txt row (schema map per spec D2).</summary>
    [Serializable]
    public class SkillRow
    {
        public int Id;
        public string Name = "";
        public string Desc = "";

        // col 70 LvlSetScript → first path segment after \script\skill\
        public string Faction = "";
        public SurvivorSkillPool Pool;
        public bool InDisplayFile;

        public int Form;            // col 19; 7 = dominant ranged, 12 = melee (NOT in SkillMissileForm enum)
        public bool IsMelee;        // col 26
        public bool SpawnsMissile;  // col 41 ByMissle
        public bool IsAura;         // col 11

        public int ChildMissileId;  // col 20 → missles.txt lookup
        public MissileVisualInfo ChildMissile; // null = fail-closed (no visual)

        /// <summary>col 22 ChildSkillNum (magic misslenum → PC m_nChildSkillNum): số đạn fan spread (ticket 27).</summary>
        public int ChildSkillNum;

        public string PreCastPath = "";   // col 6 raw (provenance)
        public string PreCastSprUid = ""; // staged UID, "" fail-closed

        public int FanParam1;       // col 58, fan spread angle step, 1/64 vòng
        public int FanParam2;       // col 60, fan spread offset px

        public int ReqLevel;        // col 52
        public int MaxLevel;        // col 53

        public int AttackRadius;    // col 14
        public float TimePerCast;   // col 31 (cooldown)
        public bool IsPhysical;     // col 33

        public string[] LvlScripts = new string[20]; // LvlSetting1-20, cols 71,73..109
        public int[] LvlData = new int[20];          // LvlData1-20, cols 72,74..110

        public SurvivorSupplyTag SupplyTag;
    }

    /// <summary>Fail-closed entry: skill kept, visual not assigned (never crash).</summary>
    public struct SkillFailEntry
    {
        public int SkillId;
        public string Detail;
        public string Path;
    }

    /// <summary>Parse result container + counts for the generate log.</summary>
    public class SurvivorSkillCatalog
    {
        public List<SkillRow> Skills = new List<SkillRow>();
        public List<SkillFailEntry> FailClosedNoPreCastStaged = new List<SkillFailEntry>();
        public List<SkillFailEntry> FailClosedNoChildMissileRow = new List<SkillFailEntry>();
        public List<SkillFailEntry> FailClosedNoChildAnimFile = new List<SkillFailEntry>();
        public List<SkillFailEntry> FailClosedNoChildAnimStaged = new List<SkillFailEntry>();
        public List<int> DuplicateIds = new List<int>(); // data PC lặp id (vd 521) — dedupe giữ row đầu

        public int DisplayFileRows;
        public int MissileRows;
        public int PreCastNonEmpty;
        public int PreCastStaged;
        public int ChildVisualResolved;

        public int PlayerPoolCount
        {
            get { int n = 0; foreach (var s in Skills) if (s.Pool == SurvivorSkillPool.Player) n++; return n; }
        }
    }
}
