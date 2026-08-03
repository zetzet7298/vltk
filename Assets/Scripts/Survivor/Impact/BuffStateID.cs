// -----------------------------------------------------------------------------
// VLTK.Survivor — Impact: control-state bitmap.
// Parity dhcd BuffStateID (DiffableCs/BattleCore/BattleCore/BuffStateID.cs):
// 20 state 0..19 — mobile map sang bitmask (bit i = state i+1; Max = sentinel bit 19).
// KHÔNG enum status tên riêng cho poison/burn/freeze/slow/silence: element flavor
// nằm ở config (SkillAttrDamageData.MagicType / attr impact MoveSpeed / NO_SKILL…).
// -----------------------------------------------------------------------------
using System;

namespace VLTK.Survivor
{
    [Flags]
    public enum BuffStateID : int
    {
        None = 0,
        Stun = 1 << 0,            // 1 STUN
        Undead = 1 << 1,          // 2 UNDEAD
        Invisible = 1 << 2,       // 3 INVISIBLE
        Bigger = 1 << 3,          // 4 BIGGER
        NoMove = 1 << 4,          // 5 NO_MOVE
        NoSkill = 1 << 5,         // 6 NO_SKILL (silence)
        Sleep = 1 << 6,           // 7 SLEEP (bị damage → remove, dhcd RemoveSleepTypeBuff)
        WalkWater = 1 << 7,       // 8 WALK_WATER
        WalkBox = 1 << 8,         // 9 WALK_BOX
        ForceCollider = 1 << 9,   // 10 FORCE_COLLIDER
        NoTrapDamage = 1 << 10,   // 11 NO_TRAP_DAMAGE
        TrapMonster = 1 << 11,    // 12 TRAP_MONSTER
        FullInvisible = 1 << 12,  // 13 FULL_INVISIBLE
        BianShens = 1 << 13,      // 14 BIANSHENS
        PlayerShadow = 1 << 14,   // 15 PLAYER_SHADOW
        GetUpSpeed = 1 << 15,     // 16 GET_UPSPEED
        MonsterNoMove = 1 << 16,  // 17 MONSTER_NO_MOVE
        MonsterAoe = 1 << 17,     // 18 MONSTER_AOE
        Confusion = 1 << 18,      // 19 CONFUSION
        Max = 1 << 19,            // 20 MAX (sentinel — không phải state dùng được)
    }

    /// <summary>20 bit để iterate — bỏ None + Max sentinel.</summary>
    public static class BuffStates
    {
        public static readonly BuffStateID[] All =
        {
            BuffStateID.Stun, BuffStateID.Undead, BuffStateID.Invisible, BuffStateID.Bigger,
            BuffStateID.NoMove, BuffStateID.NoSkill, BuffStateID.Sleep,
            BuffStateID.WalkWater, BuffStateID.WalkBox, BuffStateID.ForceCollider,
            BuffStateID.NoTrapDamage, BuffStateID.TrapMonster, BuffStateID.FullInvisible,
            BuffStateID.BianShens, BuffStateID.PlayerShadow, BuffStateID.GetUpSpeed,
            BuffStateID.MonsterNoMove, BuffStateID.MonsterAoe, BuffStateID.Confusion,
        };
    }
}
