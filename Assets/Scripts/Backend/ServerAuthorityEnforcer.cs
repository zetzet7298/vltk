// -----------------------------------------------------------------------------
// VLTK.Backend — ServerAuthorityEnforcer
// Enforcer cho combat server-authoritative. MỌI quyết định combat (damage,
// status tick, PK) PHẢI gọi server trước; client KHÔNG được tự tính.
//
// Quy tắc vàng (xem skill-combat-contract.md §5):
//   1. Mọi giá trị số (HP/mana/damage/dotResults) đều KHÔNG tin client tính —
//      chỉ tin target/currentLife/currentMana server trả.
//   2. Animation được phép predict (local), nhưng số thì đợi server.
//   3. Sau mỗi call, client THAY THẾ state local bằng state server trả
//      (không merge) — parity KNpc.cpp vốn mutate struct Npc[] toàn cục.
//
// Lớp này cung cấp:
//   - CalcDamageOrThrowAsync: gọi server, nếu fail thì raise + KHÔNG mutate
//   - StatusTickOrThrowAsync: gọi server, nếu fail thì raise + KHÔNG mutate
//   - CheckPkOrThrowAsync: gọi server, nếu fail thì raise + KHÔNG cho đánh
//   - ApplyServerState: replace state local bằng state server (parity KNpc)
//   - RejectClientDamage: phát hiện client tự tính damage (parity check)
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using VLTK.Backend.Dto;
using VLTK.Core;

namespace VLTK.Backend
{
    /// <summary>
    /// Enforcer đảm bảo mọi quyết định combat đều gọi server trước khi áp dụng
    /// state local. KHÔNG tự ý recompute damage/status/pk ở client.
    ///
    /// Pattern dùng:
    /// <code>
    /// var resp = await ServerAuthorityEnforcer.CalcDamageOrThrowAsync(
    ///     client, request, ct);
    /// ServerAuthorityEnforcer.ApplyServerState(localState, resp.data.target);
    /// </code>
    /// </summary>
    public static class ServerAuthorityEnforcer
    {
        private const string Subsystem = "Backend.Authority";

        // ----------------------------------------------------------------
        // CalcDamage — gọi server, raise nếu fail
        // ----------------------------------------------------------------

        /// <summary>
        /// Gọi POST /v1/combat/damage/calc. Nếu response không thành công
        /// (HTTP 4xx/5xx, parse error, transport error) → throw exception
        /// để caller KHÔNG apply state. Đây là cách ngăn chặn client dùng
        /// damage local khi server không confirm.
        /// </summary>
        /// <exception cref="ServerAuthorityException">
        /// Ném khi server không confirm (code != "200", data == null,
        /// transport error, parse error). Caller PHẢI catch và KHÔNG mutate
        /// state local.
        /// </exception>
        public static async Task<DamageCalcResponse> CalcDamageOrThrowAsync(
            IGameBackend backend,
            DamageCalcRequest request,
            CancellationToken ct = default)
        {
            if (backend == null)
            {
                throw new ServerAuthorityException(
                    "no_backend", "backend is null — cannot call server");
            }
            if (request == null)
            {
                throw new ServerAuthorityException(
                    "invalid_arg", "DamageCalcRequest is null");
            }
            var resp = await backend.CalcDamageAsync(request, ct);
            if (!resp.IsSuccess)
            {
                SubsystemLog.Warn(Subsystem,
                    $"CalcDamageAsync failed: code={resp.code} msg={resp.message}");
                throw new ServerAuthorityException(resp.code ?? "unknown", resp.message ?? "unknown");
            }
            if (resp.data == null)
            {
                throw new ServerAuthorityException(
                    "empty_data", "server returned success but data is null");
            }
            if (resp.data.target == null)
            {
                throw new ServerAuthorityException(
                    "empty_target", "server returned success but data.target is null");
            }
            return resp.data;
        }

        // ----------------------------------------------------------------
        // StatusTick — gọi server, raise nếu fail
        // ----------------------------------------------------------------

        /// <summary>
        /// Gọi POST /v1/combat/status/tick. Tương tự CalcDamage — nếu server
        /// không confirm → throw, caller KHÔNG tự tick local.
        /// </summary>
        public static async Task<StatusTickResponse> StatusTickOrThrowAsync(
            IGameBackend backend,
            StatusTickRequest request,
            CancellationToken ct = default)
        {
            if (backend == null)
            {
                throw new ServerAuthorityException(
                    "no_backend", "backend is null — cannot call server");
            }
            if (request == null)
            {
                throw new ServerAuthorityException(
                    "invalid_arg", "StatusTickRequest is null");
            }
            var resp = await backend.StatusTickAsync(request, ct);
            if (!resp.IsSuccess)
            {
                SubsystemLog.Warn(Subsystem,
                    $"StatusTickAsync failed: code={resp.code} msg={resp.message}");
                throw new ServerAuthorityException(resp.code ?? "unknown", resp.message ?? "unknown");
            }
            if (resp.data == null)
            {
                throw new ServerAuthorityException(
                    "empty_data", "server returned success but data is null");
            }
            if (resp.data.target == null || resp.data.status == null)
            {
                throw new ServerAuthorityException(
                    "empty_state", "server returned success but data.target/status is null");
            }
            return resp.data;
        }

        // ----------------------------------------------------------------
        // CheckPk — gọi server trước khi cho phép đánh
        // ----------------------------------------------------------------

        /// <summary>
        /// Gọi POST /v1/combat/pk/check. Nếu server fail HOẶC trả
        /// canAttack=false → throw (caller KHÔNG được phép đánh).
        /// </summary>
        public static async Task<PkCheckResponse> CheckPkOrThrowAsync(
            IGameBackend backend,
            PkCheckRequest request,
            CancellationToken ct = default)
        {
            if (backend == null)
            {
                throw new ServerAuthorityException(
                    "no_backend", "backend is null — cannot call server");
            }
            if (request == null)
            {
                throw new ServerAuthorityException(
                    "invalid_arg", "PkCheckRequest is null");
            }
            var resp = await backend.CheckPkAsync(request, ct);
            if (!resp.IsSuccess)
            {
                SubsystemLog.Warn(Subsystem,
                    $"CheckPkAsync failed: code={resp.code} msg={resp.message}");
                throw new ServerAuthorityException(resp.code ?? "unknown", resp.message ?? "unknown");
            }
            if (resp.data == null)
            {
                throw new ServerAuthorityException(
                    "empty_data", "server returned success but data is null");
            }
            if (!resp.data.canAttack)
            {
                // Server cho biết KHÔNG được đánh. KHÔNG throw để caller biết
                // lý do cụ thể — caller xử lý UI (vd hiện thông báo "vùng an
                // toàn"). Trả về data để caller đọc reason.
                SubsystemLog.Info(Subsystem,
                    $"PK blocked: reason={resp.data.reason ?? "(no reason)"}");
            }
            return resp.data;
        }

        // ----------------------------------------------------------------
        // ApplyServerState — replace state local bằng state server
        // ----------------------------------------------------------------

        /// <summary>
        /// Thay thế toàn bộ state local bằng state server. KNpc.cpp vốn mutate
        /// struct Npc[] toàn cục — Unity cũng phải làm vậy. KHÔNG merge; chỉ
        /// replace từng field. Trả về cùng `local` (đã mutate) để chain.
        /// </summary>
        /// <remarks>
        /// QUAN TRỌNG: sau call này, `local` VÀ `server` CÙNG THAM CHIẾU
        /// (cùng CombatantState instance). Nếu caller muốn giữ local riêng,
        /// hãy tạo bản copy trước khi gọi server.
        /// </remarks>
        public static CombatantState ApplyServerState(
            CombatantState local,
            CombatantState server)
        {
            if (server == null)
            {
                throw new ServerAuthorityException(
                    "empty_target", "server state is null — refuse to apply");
            }
            if (local == null)
            {
                // Caller chưa có state local → trả về state server luôn.
                return server;
            }
            // Vitals
            local.life = server.life;
            local.lifeMax = server.lifeMax;
            local.mana = server.mana;
            local.manaMax = server.manaMax;
            // Resist
            local.physicsResist = server.physicsResist;
            local.coldResist = server.coldResist;
            local.fireResist = server.fireResist;
            local.lightResist = server.lightResist;
            local.poisonResist = server.poisonResist;
            local.physicsResistMax = server.physicsResistMax;
            local.coldResistMax = server.coldResistMax;
            local.fireResistMax = server.fireResistMax;
            local.lightResistMax = server.lightResistMax;
            local.poisonResistMax = server.poisonResistMax;
            // Armor
            local.physicsArmor = server.physicsArmor;
            local.coldArmor = server.coldArmor;
            local.fireArmor = server.fireArmor;
            local.lightArmor = server.lightArmor;
            local.poisonArmor = server.poisonArmor;
            local.physicsArmorTime = server.physicsArmorTime;
            local.coldArmorTime = server.coldArmorTime;
            local.fireArmorTime = server.fireArmorTime;
            local.lightArmorTime = server.lightArmorTime;
            local.poisonArmorTime = server.poisonArmorTime;
            // Mana shield
            local.manaShieldPercent = server.manaShieldPercent;
            local.manaShieldTime = server.manaShieldTime;
            // Phản đòn
            local.meleeDmgRet = server.meleeDmgRet;
            local.rangeDmgRet = server.rangeDmgRet;
            local.meleeDmgRetPercent = server.meleeDmgRetPercent;
            local.rangeDmgRetPercent = server.rangeDmgRetPercent;
            local.damage2ManaPercent = server.damage2ManaPercent;
            local.isPlayer = server.isPlayer;
            return local;
        }

        /// <summary>
        /// Apply StatusBundle server về bundle local. Tương tự
        /// <see cref="ApplyServerState"/> cho StatusBundle.
        /// </summary>
        public static StatusBundle ApplyServerStatus(
            StatusBundle local,
            StatusBundle server)
        {
            if (server == null)
            {
                throw new ServerAuthorityException(
                    "empty_status", "server status is null — refuse to apply");
            }
            if (local == null) return server;
            local.poisonState = server.poisonState;
            local.freezeState = server.freezeState;
            local.burnState = server.burnState;
            local.confuseState = server.confuseState;
            local.stunState = server.stunState;
            local.lifeState = server.lifeState;
            local.manaState = server.manaState;
            local.drunkState = server.drunkState;
            return local;
        }

        // ----------------------------------------------------------------
        // RejectClientDamage — phát hiện client tự tính sai
        // ----------------------------------------------------------------

        /// <summary>
        /// Kiểm tra client-computed damage có khớp server-computed damage
        /// không. Nếu khác quá tolerance → log warn + return false. Caller
        /// PHẢI dùng server damage (KHÔNG dùng client damage). Hàm này chỉ
        /// dùng để test/audit; production code không cần gọi (đã dùng
        /// server damage trong response).
        ///
        /// Tolerance mặc định = 0 (parity tuyệt đối — damage phải khớp 1:1
        /// theo combat_resolve.calc_damage). Có thể nới lỏng khi server dùng
        /// seed RNG khác (khi đó damage chỉ "approximately equal").
        /// </summary>
        public static bool DamageMatchesServer(
            int clientDamage,
            DamageCalcResponse serverResponse,
            int tolerance = 0)
        {
            if (serverResponse == null) return false;
            int diff = System.Math.Abs(clientDamage - serverResponse.damage);
            bool ok = diff <= tolerance;
            if (!ok)
            {
                SubsystemLog.Warn(Subsystem,
                    $"Client damage {clientDamage} != server {serverResponse.damage} " +
                    $"(diff={diff} > tolerance={tolerance})");
            }
            return ok;
        }

        // ----------------------------------------------------------------
        // AuditHook — cho Editor / Debug overlay theo dõi
        // ----------------------------------------------------------------

        /// <summary>
        /// Event fired mỗi khi enforcer phát hiện mismatch giữa client và
        /// server. UI/Editor có thể sub để hiện cảnh báo. KHÔNG dùng cho
        /// gameplay logic.
        /// </summary>
        public static event Action<string> OnAuthorityViolation;

        internal static void RaiseViolation(string message)
        {
            if (OnAuthorityViolation != null)
            {
                try { OnAuthorityViolation.Invoke(message); }
                catch { /* swallow để không làm vỡ combat flow */ }
            }
        }
    }

    /// <summary>
    /// Exception ném khi server không confirm combat decision. Caller PHẢI
    /// catch và KHÔNG áp dụng state local.
    /// </summary>
    public sealed class ServerAuthorityException : Exception
    {
        public string AuthorityCode { get; }

        public ServerAuthorityException(string code, string message)
            : base(message ?? "server authority violation")
        {
            AuthorityCode = code ?? "unknown";
        }
    }
}
