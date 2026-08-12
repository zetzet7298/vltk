// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — Skill DTOs (FS-03B contract)
//
// Pin từ backend contract (FS-03A, commit 2b92a39, branch main ở vltk-server)
// + skill-combat-contract.md (502 dòng, parity source KNpc.cpp CalcDamage
// 2125-2362 + ProcessState 612-863). Các endpoint skill trong slice này:
//
//   GET  /v1/skill/by-role/{roleId}                     → PlayerSkillListResponse
//   POST /v1/skill/learn                                → PlayerSkillResponse
//   POST /v1/skill/by-role/{roleId}/level-up/{skillId}  → PlayerSkillResponse
//   POST /v1/skill/cast/check                           → SkillCastCheckResponse
//   POST /v1/skill/cast                                 → SkillCastResponse
//
// Quy tắc server-authoritative (chống spoof H-SK2/H-SK3):
//   - /cast KHÔNG nhận currentMana/Life/Stamina/lastCastMs từ client — server
//     đọc thẳng từ player_states + player_skills.last_cast_ms rồi mutate.
//   - Client chỉ gửi gate fields (onHorse/relation/distance/weaponType/
//     equipState/nowMs) — đây là context runtime từ client (engine PC cũng
//     đọc từ Npc state runtime), không lưu DB.
//   - /cast/check là STATELESS pre-flight dùng current* từ client để validate
//     gate + resource + cooldown. Dùng trước khi gọi /cast để UI gate mượt.
//
// Field camelCase khớp alias generator (to_camel) của backend CamelCaseModel.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VLTK.Backend.Dto
{
    // ====================================================================
    // Read: ListSkillsAsync
    // ====================================================================

    /// <summary>
    /// Một bản ghi skill đã học trong danh sách. Dùng cho cả list/level-up/ learn
    /// (cùng shape). skillName + maxLevel đính kèm từ template (nếu có); null/0
    /// nếu skill id không resolve được template.
    /// </summary>
    [Serializable]
    public sealed class PlayerSkillResponse
    {
        public int id;
        public int roleId;
        public int skillId;
        public int level;
        public bool isActive;

        /// <summary>Tên skill (template-side, có thể null nếu skillId lạ).</summary>
        public string skillName;

        /// <summary>Level tối đa (template-side, 0 nếu không biết).</summary>
        public int maxLevel;
    }

    /// <summary>
    /// Phản hồi data từ GET /v1/skill/by-role/{roleId}. Trả về danh sách skill
    /// của nhân vật (mảng rỗng nếu roleId chưa học skill nào).
    /// </summary>
    [Serializable]
    public sealed class PlayerSkillListResponse
    {
        public int roleId;

        /// <summary>List skill (không null; rỗng khi chưa học).</summary>
        public List<PlayerSkillResponse> skills;
    }

    // ====================================================================
    // Write: LearnSkillAsync
    // ====================================================================

    /// <summary>
    /// Request body cho POST /v1/skill/learn.
    ///   roleId     : int>=1, role cần học skill
    ///   skillId    : int>=1, id từ skills.txt
    ///   charLevel  : 1..200, cấp hiện tại của role
    ///   faction    : 0..9; -1 = chưa nhập phái
    /// </summary>
    [Serializable]
    public sealed class SkillLearnRequest
    {
        public int roleId;
        public int skillId;
        public int charLevel;
        public int faction;

        public SkillLearnRequest() { }

        public SkillLearnRequest(int roleId, int skillId, int charLevel, int faction = -1)
        {
            this.roleId = roleId;
            this.skillId = skillId;
            this.charLevel = charLevel;
            this.faction = faction;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    // ====================================================================
    // Cast gate: CastSkillCheckAsync (stateless) vs CastSkillAsync (server-auth)
    // ====================================================================

    /// <summary>
    /// Request body cho POST /v1/skill/cast/check. STATELESS — server không đụng
    /// DB, chỉ dùng current* + gate fields do client cung cấp để trả về OK/block.
    ///
    /// Lý do dùng: client predict xem có thể cast không trước khi gửi /cast (UI
    /// gate mượt, hiển thị cooldown bar). Nhưng PHẢI reconcile với /cast vì
    /// server-authoritative sẽ lấy resource/cooldown thật từ DB.
    ///
    ///   roleId           : int>=1
    ///   skillId          : int>=1
    ///   currentMana      : 0..∞, mana hiện có
    ///   currentLife      : 0..∞, sinh lực hiện có
    ///   currentStamina   : 0..∞, thể lực hiện có
    ///   onHorse          : gate context runtime
    ///   relation         : 0=self, 1=ally, 2=enemy, 3=other
    ///   distance         : 0..∞ (0 = bỏ qua tầm, skill không dùng IsUseAR)
    ///   weaponType       : 0=không yêu cầu; 1=kiếm; 2=đao; ...
    ///   equipState       : -2 = không giới hạn
    ///   nowMs            : mốc hiện tại (ms)
    ///   lastCastMs       : mốc thi triển gần nhất (0 = chưa cast)
    /// </summary>
    [Serializable]
    public sealed class SkillCastCheckRequest
    {
        public int roleId;
        public int skillId;
        public int currentMana;
        public int currentLife;
        public int currentStamina;
        public bool onHorse;
        public int relation;
        public int distance;
        public int weaponType;
        public int equipState;
        public long nowMs;
        public long lastCastMs;

        public SkillCastCheckRequest() { }

        public SkillCastCheckRequest(
            int roleId,
            int skillId,
            int currentMana,
            int currentLife,
            int currentStamina,
            bool onHorse = false,
            int relation = 0,
            int distance = 0,
            int weaponType = 0,
            int equipState = -2,
            long nowMs = 0,
            long lastCastMs = 0)
        {
            this.roleId = roleId;
            this.skillId = skillId;
            this.currentMana = currentMana;
            this.currentLife = currentLife;
            this.currentStamina = currentStamina;
            this.onHorse = onHorse;
            this.relation = relation;
            this.distance = distance;
            this.weaponType = weaponType;
            this.equipState = equipState;
            this.nowMs = nowMs;
            this.lastCastMs = lastCastMs;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    /// <summary>
    /// Phản hồi từ /v1/skill/cast/check. STATELESS, chỉ để predict.
    ///   canCast       : true nếu pass tất cả gate (3-7 + 8 cooldown + 9 resource)
    ///   reason        : null khi OK; "Vũ khí đang trang bị không phù hợp" v.v.
    ///   costType      : 0=none, 1=mana, 2=life, 3=stamina
    ///   costValue     : lượng tài nguyên phải trả
    ///   delayPerCast  : ms (TimePerCast / TimePerCastOnHorse / WaitTime)
    ///   nextCastTime  : mốc sẵn sàng (lastCastMs nếu có, else 0)
    /// </summary>
    [Serializable]
    public sealed class SkillCastCheckResponse
    {
        public int skillId;
        public bool canCast;
        public string reason;
        public int costType;
        public int costValue;
        public int delayPerCast;
        public long nextCastTime;
    }

    // ====================================================================
    // Cast: CastSkillAsync (server-authoritative)
    // ====================================================================

    /// <summary>
    /// Request body cho POST /v1/skill/cast. SERVER-AUTHORITATIVE — server đọc
    /// currentMana/Life/Stamina + last_cast_ms từ DB, KHÔNG nhận từ client
    /// (chống spoof H-SK2 cost + H-SK3 cooldown).
    ///
    ///   roleId      : int>=1
    ///   skillId     : int>=1
    ///   onHorse     : gate context runtime (không lưu DB)
    ///   relation    : 0=self, 1=ally, 2=enemy, 3=other
    ///   distance    : 0..∞ (0 = bỏ qua tầm)
    ///   weaponType  : 0=không yêu cầu
    ///   equipState  : -2 = không giới hạn
    ///   nowMs       : mốc hiện tại (>=1), server dùng để tính next + ghi last_cast_ms
    /// </summary>
    [Serializable]
    public sealed class SkillCastRequest
    {
        public int roleId;
        public int skillId;
        public bool onHorse;
        public int relation;
        public int distance;
        public int weaponType;
        public int equipState;
        public long nowMs;

        public SkillCastRequest() { }

        public SkillCastRequest(
            int roleId,
            int skillId,
            long nowMs,
            bool onHorse = false,
            int relation = 0,
            int distance = 0,
            int weaponType = 0,
            int equipState = -2)
        {
            this.roleId = roleId;
            this.skillId = skillId;
            this.nowMs = nowMs;
            this.onHorse = onHorse;
            this.relation = relation;
            this.distance = distance;
            this.weaponType = weaponType;
            this.equipState = equipState;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    /// <summary>
    /// Một effect (attrib + effectKey + 3 tham số) sinh ra sau khi cast.
    /// Đây là danh sách effect nội suy từ template skill, parity KSkill::Cast
    /// effect list trong KNpc.cpp. Client dùng để áp buff/debuff/effectKey
    /// animation — KHÔNG tự tính damage ở đây.
    /// </summary>
    [Serializable]
    public sealed class SkillCastEffect
    {
        public string attrib;       // "physicsArmor" / "life" / "fireResist" / ...
        public string effectKey;    // "armor" / "dot" / "hot" / "buff" / ...
        public int p1;
        public int p2;
        public int p3;
    }

    /// <summary>
    /// Phản hồi từ /v1/skill/cast. PHẢI dùng currentLife/Mana/Stamina + effects
    /// server trả — KHÔNG dùng số client tự tính (parity Predict-reconcile FS-03A §5).
    ///
    ///   cast          : true nếu server OK (gate + cooldown + resource đều pass)
    ///   costType      : 0=none, 1=mana, 2=life, 3=stamina
    ///   costPaid      : lượng thực tế bị trừ (costValue khi costType>0, 0 nếu NONE)
    ///   currentLife   : sinh lực SAU cast (server-authoritative)
    ///   currentMana   : nội lực SAU cast (server-authoritative)
    ///   currentStamina: thể lực SAU cast
    ///   nextCastTime  : nowMs + max(delay, waitTime) — mốc sẵn sàng cho cast kế
    ///   effects       : danh sách effect nội suy (rỗng nếu skill no-effect)
    /// </summary>
    [Serializable]
    public sealed class SkillCastResponse
    {
        public int skillId;
        public bool cast;
        public int costType;
        public int costPaid;
        public int currentLife;
        public int currentMana;
        public int currentStamina;
        public long nextCastTime;
        public List<SkillCastEffect> effects;
    }
}
