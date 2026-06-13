// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — CombatDto
// Mirror cho payload của 3 endpoint combat FS-03A (server-authoritative):
//   - POST /v1/combat/damage/calc  → CalcDamageRequest / DamageCalcResponse
//   - POST /v1/combat/status/tick  → StatusTickRequest  / StatusTickResponse
//   - POST /v1/combat/pk/check     → PkCheckRequest     / PkCheckResponse
//
// Backend dùng CamelCaseModel + extra="forbid" → mọi field C# phải camelCase
// và khớp 1-1 alias. Các nested state (CombatantState, StatusBundle) cũng
// dùng camelCase để JsonUtility + Newtonsoft map đúng.
//
// Parity source: Assets/StreamingAssets/Reference/KNpc.cpp (MÃ NGUỒN C++ THẬT)
//   - CalcDamage  KNpc.cpp:2125-2362
//   - ProcessState KNpc.cpp:612-863
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Backend.Dto
{
    // -----------------------------------------------------------------------
    // CombatantState — trạng thái runtime của một thực thể trong combat
    // -----------------------------------------------------------------------

    /// <summary>
    /// State runtime của attacker HOẶC target. Field C# camelCase khớp alias
    /// backend (combat_resolve.py / status_effect.py). Trùng với
    /// `CombatantStateSchema` trong skill-combat-contract.md §2.3.
    ///
    /// LƯU Ý QUAN TRỌNG (server-authoritative): client KHÔNG ĐƯỢC tự ý
    /// thay đổi các field này. Sau mỗi call damage/calc hoặc status/tick,
    /// client THAY THẾ state local bằng state server trả về (xem
    /// ServerAuthorityEnforcer.ApplyServerState).
    /// </summary>
    [Serializable]
    public sealed class CombatantState
    {
        // Vitals
        public int life;
        public int lifeMax;
        public int mana;
        public int manaMax;

        // Resist (cap bởi resistMax = 95)
        public int physicsResist;
        public int coldResist;
        public int fireResist;
        public int lightResist;
        public int poisonResist;
        public int physicsResistMax;
        public int coldResistMax;
        public int fireResistMax;
        public int lightResistMax;
        public int poisonResistMax;

        // Armor (giáp tạm — có timer)
        public int physicsArmor;
        public int coldArmor;
        public int fireArmor;
        public int lightArmor;
        public int poisonArmor;
        public int physicsArmorTime;
        public int coldArmorTime;
        public int fireArmorTime;
        public int lightArmorTime;
        public int poisonArmorTime;

        // Mana shield (khiên nội lực)
        public int manaShieldPercent;
        public int manaShieldTime;

        // Phản đòn
        public int meleeDmgRet;
        public int rangeDmgRet;
        public int meleeDmgRetPercent;
        public int rangeDmgRetPercent;

        // Hấp thụ (% dmg -> mana)
        public int damage2ManaPercent;

        // Cờ phân biệt player vs NPC (ảnh hưởng PK rate)
        public bool isPlayer;
    }

    // -----------------------------------------------------------------------
    // StatusBundle — bundle các state hiệu ứng (poison/freeze/burn/...)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Một hiệu ứng trạng thái đơn lẻ. Cấu trúc 4-int (value0/value1/value2/time)
    /// giữ parity với `KStateNode` của engine PC (KNpc.cpp). time là "lượt
    /// còn lại"; khi time=0 → hiệu ứng hết hạn.
    /// </summary>
    [Serializable]
    public sealed class StateNode
    {
        public int value0;
        public int value1;
        public int value2;
        public int time;
    }

    /// <summary>
    /// Tập hợp 8 loại state. Field camelCase khớp alias backend
    /// (status_effect.py:StatusBundleSchema). Mặc định time=0 cho mọi state.
    /// </summary>
    [Serializable]
    public sealed class StatusBundle
    {
        public StateNode poisonState;
        public StateNode freezeState;
        public StateNode burnState;
        public StateNode confuseState;
        public StateNode stunState;
        public StateNode lifeState;   // HoT (heal over time)
        public StateNode manaState;   // MoT (mana over time)
        public StateNode drunkState;
    }

    // -----------------------------------------------------------------------
    // DotResult — kết quả một lần damage từ status tick (poison DoT, burn DoT,...)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Kết quả damage từ một lần DoT trong status/tick. Cùng shape với
    /// DamageCalcResponse.damage nhưng gói gọn cho list (poison DoT, burn DoT,...)
    /// </summary>
    [Serializable]
    public sealed class DotResult
    {
        public int damage;
        public int manaAbsorbed;
        public int armorAbsorbed;
        public bool manaShieldBroke;
        public bool targetDied;
        public int reflectToAttacker;
        public int reflectKind;
    }

    // -----------------------------------------------------------------------
    // DamageCalcRequest/Response — POST /v1/combat/damage/calc
    // -----------------------------------------------------------------------

    /// <summary>
    /// Yêu cầu tính sát thương 1 đòn (parity KNpc::CalcDamage 2125-2362).
    /// Server là NGUỒN CHÂN LÝ DUY NHẤT cho damage — client gửi context
    /// (atkMin/atkMax/kind/melee/return/pkRate), server mutate `target` tại
    /// chỗ và trả về damage + state sau. KHÔNG tự tính damage ở client.
    /// </summary>
    [Serializable]
    public sealed class DamageCalcRequest
    {
        public int atkMin;
        public int atkMax;

        /// <summary>0=physics, 1=cold, 2=fire, 3=light, 4=poison, 5=magic.</summary>
        public int damageKind;

        public bool isMelee;

        /// <summary>Đòn phản đòn (chặn đệ quy) — KNpc.cpp:2318.</summary>
        public bool isReturn;

        /// <summary>% dmg khi cả hai là player. Default 100.</summary>
        public int pkDamageRate;

        public CombatantState target;
        public CombatantState attacker;   // optional — null khi không có

        /// <summary>Seed RNG (null = random). Tương ứng `g_Random` của C++.</summary>
        public int seed;
    }

    /// <summary>
    /// Kết quả tính sát thương. Server MUTATE `target` tại chỗ — response
    /// trả về target đã mutate để client thay thế state local (không cần
    /// merge). Lưu ý: KHÔNG dùng damage ở client để hiển thị HP — chỉ dùng
    /// target.life server trả.
    /// </summary>
    [Serializable]
    public sealed class DamageCalcResponse
    {
        public int damage;
        public int manaAbsorbed;
        public int armorAbsorbed;
        public bool manaShieldBroke;
        public bool targetDied;
        public int reflectToAttacker;
        public int reflectKind;
        public CombatantState target;
    }

    // -----------------------------------------------------------------------
    // StatusTickRequest/Response — POST /v1/combat/status/tick
    // -----------------------------------------------------------------------

    /// <summary>
    /// Yêu cầu tiến 1 frame ProcessState (parity KNpc.cpp:612-863). Server
    /// mutate `target` + `status` tại chỗ, trả về control flags + dotResults
    /// + state sau. Client KHÔNG tự tick status local.
    /// </summary>
    [Serializable]
    public sealed class StatusTickRequest
    {
        public CombatantState target;
        public StatusBundle status;

        /// <summary>loopFrames hiện tại (regen mỗi %10==0).</summary>
        public int loopFrames;

        public bool isSitting;
        public int lifeReplenish;
        public int manaReplenish;

        /// <summary>Kẻ gây độc cuối (m_nLastPoisonDamageIdx).</summary>
        public CombatantState poisonSource;

        public int activeAuraId;
        public int activeAuraLevel;
    }

    /// <summary>
    /// Kết quả 1 frame ProcessState. controlled=true khi bị freeze (odd-tick)
    /// hoặc stun. confuseEnded=true khi confuseState.time vừa về 0 frame này.
    /// </summary>
    [Serializable]
    public sealed class StatusTickResponse
    {
        public bool controlled;
        public bool confuseEnded;
        public List<DotResult> dotResults;
        public int auraCastSkillId;
        public int auraCastLevel;
        public CombatantState target;
        public StatusBundle status;
    }

    // -----------------------------------------------------------------------
    // PkCheckRequest/Response — POST /v1/combat/pk/check
    // -----------------------------------------------------------------------

    /// <summary>
    /// Kiểm tra server-side PK hợp lệ (vùng an toàn / khác phe / battle).
    /// Server là NGUỒN CHÂN LÝ — client KHÔNG ĐƯỢC tự quyết định có được
    /// đánh player khác hay không, LUÔN gọi pk/check trước.
    /// </summary>
    [Serializable]
    public sealed class PkCheckRequest
    {
        /// <summary>0=chưa phân phe.</summary>
        public int attackerCamp;
        public int targetCamp;

        /// <summary>City/Capital/Field/Battlefield/...</summary>
        public string mapType;

        public bool inBattle;
    }

    /// <summary>
    /// Kết quả kiểm tra PK. canAttack=true chỉ khi mapPkAllowed AND
    /// !isSafeZone. reason=null khi OK; chuỗi tiếng Việt giải thích khi fail.
    /// </summary>
    [Serializable]
    public sealed class PkCheckResponse
    {
        public bool canAttack;
        public bool mapPkAllowed;
        public bool isSafeZone;
        public string reason;
    }
}
