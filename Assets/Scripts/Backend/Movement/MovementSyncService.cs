// -----------------------------------------------------------------------------
// VLTK.Backend.Movement — MovementSyncService
//
// Service đồng bộ toạ độ runtime giữa client (Unity) và server (FastAPI).
// Mục tiêu: giảm tải HTTP bằng cách gom nhiều bước di chuyển local thành 1
// call định kỳ, đồng thời phát hiện drift khi server điều chỉnh (vd do vật
// cản, warping, GM teleport) để client KHÔNG tự tin vào vị trí local sai.
//
// Quy tắc parity:
//   - Backend POST /v1/movement ↔ KNpc.cpp:2591 ServeMove() (xem KNpc::SetPos
//     tại KNpc.cpp:5496). Server cập nhật toạ độ runtime; client nhận lại
//     SceneResponse với posX/posY SAU update.
//   - Sync interval mặc định 2s: PC engine không sync theo interval cố định
//     (server push qua GameServer protocol), nhưng Unity mobile dùng HTTP REST
//     nên cần gom theo nhịp. 2s là con số phù hợp với mức pin mobile và độ
//     mượt cảm nhận của người chơi; có thể chỉnh theo role/npc qua constructor.
//   - Reconciliation threshold mặc định 50 ô: phù hợp với collision
//     tolerance của VLTK client (Region bán kính 32-64 ô).
//
// Service KHÔNG phụ thuộc MonoBehaviour. Driver (BackendClientRunner hoặc
// SandboxManager) cung cấp deltaTime mỗi frame, service quyết định có sync
// không, gọi backend, cập nhật ServerPosition. Đây là pattern "tích luỹ
// thời gian" kinh điển cho HTTP sync, parity với các game client khác dùng
// FastAPI + WebSocket (vd MMORPG mobile).
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using VLTK.Backend.Dto;
using VLTK.Core;

namespace VLTK.Backend.Movement
{
    /// <summary>
    /// Service đồng bộ toạ độ nhân vật client ↔ server. Pure C#, không phụ
    /// thuộc MonoBehaviour. Driver cung cấp deltaTime qua
    /// <see cref="ShouldSync"/> và gọi <see cref="SyncPositionAsync"/> khi
    /// service báo đủ interval.
    /// </summary>
    public sealed class MovementSyncService
    {
        private const string Subsystem = "Backend.Movement";

        private readonly BackendClient _backend;
        private readonly int _roleId;
        private readonly float _syncInterval;

        // Tích luỹ dt từ frame trước. Reset về 0 mỗi khi ShouldSync trả true
        // (caller sẽ gọi SyncPositionAsync ngay sau đó).
        private float _accumulator;

        // Server position cuối cùng. Mặc định (0, 0) và HasServerPosition=false
        // cho tới khi nhận response thành công đầu tiên.
        private int _serverPosX;
        private int _serverPosY;
        private bool _hasServerPosition;

        /// <summary>
        /// Khởi tạo service. Backend KHÔNG được null; roleId phải &gt; 0;
        /// syncInterval &lt;= 0 sẽ bị clamp về 2f.
        /// </summary>
        /// <param name="backend">BackendClient (Mock hoặc Rest).</param>
        /// <param name="roleId">ID nhân vật cần sync.</param>
        /// <param name="syncInterval">Chu kỳ sync (giây), mặc định 2f.</param>
        public MovementSyncService(
            BackendClient backend,
            int roleId,
            float syncInterval = 2f)
        {
            if (backend == null)
            {
                throw new ArgumentNullException(nameof(backend));
            }
            if (roleId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(roleId), roleId, "roleId phải > 0");
            }
            _backend = backend;
            _roleId = roleId;
            _syncInterval = syncInterval > 0f ? syncInterval : 2f;
        }

        /// <summary>
        /// ID nhân vật đang được sync (do constructor truyền vào).
        /// </summary>
        public int RoleId => _roleId;

        /// <summary>
        /// Chu kỳ sync hiệu lực (giây). Có thể đã bị clamp nếu constructor
        /// nhận syncInterval &lt;= 0.
        /// </summary>
        public float SyncInterval => _syncInterval;

        /// <summary>
        /// Server position trả về lần cuối (qua SyncPositionAsync thành công).
        /// Tuple (X, Y) theo grid PC. Mặc định (0, 0) cho tới khi sync lần
        /// đầu.
        /// </summary>
        public (int X, int Y) ServerPosition => (_serverPosX, _serverPosY);

        /// <summary>
        /// True khi service đã nhận ÍT NHẤT 1 response thành công từ server
        /// (tức là ServerPosition có ý nghĩa thật). False khi mới khởi tạo
        /// hoặc tất cả call trước đều fail.
        /// </summary>
        public bool HasServerPosition => _hasServerPosition;

        /// <summary>
        /// Tích luỹ thời gian hiện tại (giây) từ lần sync cuối (hoặc khởi
        /// tạo). Chủ yếu để driver/UI hiển thị thanh đếm ngược; gameplay
        /// logic nên gọi <see cref="ShouldSync"/> thay vì đọc trực tiếp.
        /// </summary>
        public float Accumulator => _accumulator;

        /// <summary>
        /// Cộng dồn dt vào accumulator; trả về true khi đạt syncInterval (tức
        /// caller nên gọi <see cref="SyncPositionAsync"/> ở frame này hoặc
        /// frame kế tiếp). Khi trả true, accumulator được reset về 0 để bắt
        /// đầu chu kỳ mới.
        ///
        /// Lưu ý: đây là "fire and reset" — caller CẦN gọi
        /// <see cref="SyncPositionAsync"/> ngay sau khi ShouldSync trả true,
        /// nếu không sẽ bị mất nhịp.
        /// </summary>
        /// <param name="dt">Delta time (giây) của frame hiện tại.</param>
        public bool ShouldSync(float dt)
        {
            if (dt < 0f)
            {
                // dt âm → bỏ qua (xảy ra khi pause game / debug step back).
                return false;
            }
            _accumulator += dt;
            if (_accumulator >= _syncInterval)
            {
                _accumulator = 0f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gọi backend POST /v1/movement để cập nhật vị trí. Trả về
        /// SceneResponse từ server (KHÔNG trả BackendResponse&lt;&gt; — service
        /// đã unwrap, caller chỉ cần data). Trả null khi fail (server 4xx/5xx,
        /// transport error, validation error, role chưa có scene, …). Caller
        /// kiểm tra null để biết có nên dùng kết quả hay giữ vị trí local.
        /// </summary>
        /// <param name="posX">Toạ độ X theo grid PC (KNpc.cpp nMpsX).</param>
        /// <param name="posY">Toạ độ Y theo grid PC (KNpc.cpp nMpsY).</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<SceneResponse> SyncPositionAsync(
            int posX,
            int posY,
            CancellationToken ct = default)
        {
            var request = new UpdatePositionRequest(_roleId, posX, posY);
            BackendResponse<SceneResponse> resp;
            try
            {
                resp = await _backend.UpdatePositionAsync(request, ct);
            }
            catch (Exception ex)
            {
                // Transport exception (dns fail, ssl error, ...) — caller
                // không nhận được SceneResponse. KHÔNG mutate _serverPos* để
                // giữ giá trị cũ cho lần reconcile sau.
                SubsystemLog.Warn(Subsystem,
                    $"SyncPositionAsync threw {ex.GetType().Name}: {ex.Message}");
                return null;
            }

            if (!resp.IsSuccess || resp.data == null)
            {
                SubsystemLog.Warn(Subsystem,
                    $"SyncPositionAsync failed: code={resp.code} msg={resp.message}");
                return null;
            }

            // Thành công — cập nhật ServerPosition về toạ độ server trả.
            // Đây là "predict-then-reconcile": client đoán vị trí sẽ là
            // (posX, posY), server xác nhận lại bằng resp.data. Hai giá trị
            // có thể KHÁC nhau (vd server thấy vật cản, warping, GM teleport)
            // → caller DÙNG resp.data làm vị trí cuối cùng.
            _serverPosX = resp.data.posX;
            _serverPosY = resp.data.posY;
            _hasServerPosition = true;
            // Reset accumulator về 0 vì vừa sync thành công — chu kỳ mới bắt
            // đầu từ đây, dù caller có gọi ShouldSync(dt) ngay frame sau hay
            // không.
            _accumulator = 0f;
            return resp.data;
        }

        /// <summary>
        /// Kiểm tra client position có bị drift quá xa so với server position
        /// hay không. True khi EITHER |dx| &gt; threshold HOẶC |dy| &gt;
        /// threshold (Manhattan-like OR, parity với VLTK client collision
        /// check dùng max(|dx|, |dy|) — chọn max-style thay vì Euclidean để
        /// khớp với grid-based collision).
        ///
        /// Khi <see cref="HasServerPosition"/> = false (chưa từng sync thành
        /// công), trả false — không có dữ liệu server để so sánh. Caller
        /// KHÔNG nên coi đây là "đang khớp", mà coi là "chưa biết".
        /// </summary>
        /// <param name="clientX">Toạ độ X theo grid PC mà client đang hiển thị.</param>
        /// <param name="clientY">Toạ độ Y theo grid PC mà client đang hiển thị.</param>
        /// <param name="threshold">Ngưỡng drift (ô lưới), mặc định 50.</param>
        public bool NeedsReconciliation(int clientX, int clientY, int threshold = 50)
        {
            if (!_hasServerPosition) return false;
            if (threshold < 0) threshold = 0;
            int dx = Math.Abs(clientX - _serverPosX);
            int dy = Math.Abs(clientY - _serverPosY);
            // Max-style: chỉ cần 1 trục vượt threshold → drift. Đây là cách
            // VLTK client check vị trí vật cản (vd entity ở (5, 100) so với
            // (3, 4) → |dy|=96 vượt 50 → cần reconcile). Euclidean sqrt
            // không cần thiết vì grid là Manhattan.
            return dx > threshold || dy > threshold;
        }
    }
}
