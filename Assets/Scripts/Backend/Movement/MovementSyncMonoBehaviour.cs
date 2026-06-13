// -----------------------------------------------------------------------------
// VLTK.Backend.Movement — MovementSyncMonoBehaviour
//
// Runtime driver cho MovementSyncService (FS-04C). Gắn lên GameObject player
// (hoặc GameObject chứa SandboxPlayerController) để tự động:
//   1. Mỗi FixedUpdate: cộng dồn dt, nếu đạt syncInterval thì gọi
//      SyncPositionAsync với toạ độ grid (WorldGridMapper.WorldToGrid) của
//      transform.position hiện tại.
//   2. Mỗi Update: sau khi service nhận response thành công, kiểm tra
//      NeedsReconciliation. Nếu drift quá threshold, log warning + snap
//      transform.position về toạ độ world tương ứng với ServerPosition
//      (GridToWorld).
//
// Pattern "predict-then-reconcile":
//   - Client dự đoán vị trí sẽ là vị trí hiện tại; gửi lên server.
//   - Server xác nhận lại (SceneResponse.posX/posY).
//   - Nếu khác (server thấy vật cản, warping, GM teleport), client snap
//     về vị trí server trả — KHÔNG tự ý bỏ qua.
//
// Mục đích parity với PC engine: PC client gửi position mỗi frame qua UDP;
// Unity mobile dùng REST polling 2s/lần (đủ cho single-player demo).
//
// Dependency: phải được BindBackend() sau khi BackendClientRunner.RunAsync()
// hoàn tất. roleId phải > 0; nếu chưa BindBackend() thì FixedUpdate/Update
// sẽ no-op (KHÔNG throw).
// -----------------------------------------------------------------------------

using System.Threading;
using UnityEngine;
using VLTK.Backend.Dto;

namespace VLTK.Backend.Movement
{
    /// <summary>
    /// MonoBehaviour driver đồng bộ toạ độ runtime client ↔ server. Pure
    /// driver layer: không phụ thuộc SandboxPlayerController trực tiếp — chỉ
    /// đọc <see cref="Transform.position"/> của GameObject chứa component
    /// này. Tách bạch rõ ràng với input/visual.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MovementSyncMonoBehaviour : MonoBehaviour
    {
        private const string Subsystem = "Backend.Movement.Mono";

        [Header("Sync config")]
        [Tooltip("Chu kỳ sync (giây). Mặc định 2f — đủ cho single-player demo. " +
                 "PC engine sync mỗi frame qua UDP; Unity mobile dùng REST nên " +
                 "gom theo nhịp 2s để giảm tải pin và bandwidth.")]
        [Min(0.1f)]
        public float syncInterval = 2f;

        [Tooltip("Ngưỡng drift (ô lưới) để trigger reconciliation. Mặc định 50 " +
                 "ô — phù hợp với Region bán kính 32-64 ô của VLTK client.")]
        [Min(0)]
        public int reconciliationThreshold = 50;

        [Header("Debug")]
        [Tooltip("In log mỗi lần sync (info). Bỏ tick khi production để giảm " +
                 "spam Console.")]
        public bool verbose = false;

        // Backend + roleId được inject từ BackendClientRunner sau khi
        // RunAsync() hoàn tất. Khi null/<=0 thì driver no-op.
        private BackendClient _backend;
        private int _roleId;

        // Service pure C# bọc logic sync (đã test ở MovementSyncServiceTests).
        // Tạo mới khi BindBackend() được gọi; reset về null khi OnDisable.
        private MovementSyncService _syncService;

        // Cancellation token cho lần sync đang bay. Khi component bị disable
        // hoặc bind backend mới, cancel sync cũ để tránh race.
        private CancellationTokenSource _syncCts;

        /// <summary>
        /// True khi driver đã có backend + roleId hợp lệ (sau BindBackend).
        /// False khi mới spawn hoặc sau khi UnbindBackend.
        /// </summary>
        public bool IsBound => _backend != null && _roleId > 0;

        /// <summary>
        /// Reference tới service (null khi chưa BindBackend). Cho diagnostics/UI.
        /// </summary>
        public MovementSyncService Service => _syncService;

        /// <summary>
        /// Inject BackendClient + roleId. An toàn gọi nhiều lần (re-bind sẽ
        /// cancel sync cũ và tạo service mới). Phải gọi SAU khi
        /// BackendClientRunner.RunAsync() xong vì cần roleId thật từ
        /// ListRolesAsync.
        /// </summary>
        /// <param name="backend">BackendClient (Mock hoặc Rest).</param>
        /// <param name="roleId">roleId &gt; 0; nếu &lt;= 0 sẽ coi như unbind.</param>
        public void BindBackend(BackendClient backend, int roleId)
        {
            if (backend == null)
            {
                Debug.LogWarning($"[{Subsystem}] BindBackend(null, {roleId}) — " +
                                 "coi như unbind.");
                UnbindBackend();
                return;
            }
            if (roleId <= 0)
            {
                Debug.LogWarning($"[{Subsystem}] BindBackend(backend, {roleId}) — " +
                                 "roleId phải > 0; coi như unbind.");
                UnbindBackend();
                return;
            }

            // Hủy sync đang bay (nếu có) để tránh race khi re-bind.
            CancelInFlight();

            _backend = backend;
            _roleId = roleId;
            _syncService = new MovementSyncService(backend, roleId, syncInterval);
            if (verbose)
            {
                Debug.Log($"[{Subsystem}] bound: roleId={roleId} " +
                          $"syncInterval={syncInterval}s " +
                          $"threshold={reconciliationThreshold}");
            }
        }

        /// <summary>
        /// Hủy binding. Driver trở về no-op cho tới khi BindBackend() lại.
        /// </summary>
        public void UnbindBackend()
        {
            CancelInFlight();
            _backend = null;
            _roleId = 0;
            _syncService = null;
        }

        private void OnDisable()
        {
            // Khi GameObject bị disable, hủy sync đang bay để tránh callback
            // vào component đã chết.
            CancelInFlight();
        }

        private void OnDestroy()
        {
            CancelInFlight();
        }

        private void CancelInFlight()
        {
            if (_syncCts == null) return;
            try
            {
                if (!_syncCts.IsCancellationRequested)
                {
                    _syncCts.Cancel();
                }
            }
            catch (System.ObjectDisposedException)
            {
                // CTS đã dispose do caller khác — bỏ qua.
            }
            finally
            {
                _syncCts.Dispose();
                _syncCts = null;
            }
        }

        private void FixedUpdate()
        {
            if (_syncService == null) return; // chưa BindBackend

            // Tích luỹ dt; nếu đạt interval thì service trả true và reset
            // accumulator. Sau đó gọi SyncPositionAsync ngay frame này.
            if (!_syncService.ShouldSync(Time.fixedDeltaTime)) return;

            // Convert world position hiện tại → grid position theo PC engine.
            Vector3 worldPos = transform.position;
            int gridX = WorldGridMapper.WorldToGridX(worldPos.x);
            int gridY = WorldGridMapper.WorldToGridY(worldPos.y);

            // Cancel sync cũ (nếu còn bay) rồi tạo CTS mới cho lần này.
            CancelInFlight();
            _syncCts = new CancellationTokenSource();
            var ct = _syncCts.Token;

            if (verbose)
            {
                Debug.Log($"[{Subsystem}] sync: world=({worldPos.x:F1},{worldPos.y:F1}) " +
                          $"→ grid=({gridX},{gridY})");
            }

            // Fire-and-forget; lỗi đã được service log bên trong. Driver
            // KHÔNG block FixedUpdate — Task chạy background, callback
            // xử lý ở Update() qua NeedsReconciliation.
            _ = DoSyncAsync(gridX, gridY, ct);
        }

        private async System.Threading.Tasks.Task DoSyncAsync(
            int gridX, int gridY, CancellationToken ct)
        {
            if (_syncService == null) return; // unbind giữa chừng
            SceneResponse resp = null;
            try
            {
                resp = await _syncService.SyncPositionAsync(gridX, gridY, ct);
            }
            catch (System.OperationCanceledException)
            {
                // BindBackend mới / OnDisable / OnDestroy — bỏ qua.
                return;
            }
            catch (System.Exception ex)
            {
                // Service đã log rồi; chỉ thêm context subsystem.
                Debug.LogWarning($"[{Subsystem}] sync threw {ex.GetType().Name}: " +
                                 $"{ex.Message}");
                return;
            }
            if (resp == null) return; // fail đã log ở service

            if (verbose)
            {
                Debug.Log($"[{Subsystem}] sync OK: server=({resp.posX},{resp.posY})");
            }
            // Reconciliation sẽ xử lý ở Update() — gọi NeedsReconciliation
            // với toạ độ grid hiện tại (KHÔNG dùng server posX/posY làm
            // "clientX" vì chính client vừa gửi posX/posY đó).
        }

        private void Update()
        {
            if (_syncService == null) return;
            if (!_syncService.HasServerPosition) return; // chưa sync lần nào

            // Convert world position hiện tại → grid để so sánh với server.
            Vector3 worldPos = transform.position;
            int clientGridX = WorldGridMapper.WorldToGridX(worldPos.x);
            int clientGridY = WorldGridMapper.WorldToGridY(worldPos.y);

            if (!_syncService.NeedsReconciliation(
                    clientGridX, clientGridY, reconciliationThreshold))
            {
                return;
            }

            // Drift quá threshold → snap về vị trí server trả.
            var (serverX, serverY) = _syncService.ServerPosition;
            Vector2 serverWorld = WorldGridMapper.GridToWorld(serverX, serverY);
            Vector3 newWorld = new Vector3(serverWorld.x, serverWorld.y, worldPos.z);

            // Một interpolation duy nhất thay vì nhiều concat — Update() hot
            // path, cần tránh alloc mỗi frame khi drift xảy ra liên tục.
            Debug.LogWarning(string.Format(
                "[{0}] reconcile snap: clientGrid=({1},{2}) " +
                "serverGrid=({3},{4}) threshold={5} " +
                "oldWorld=({6:F1},{7:F1}) newWorld=({8:F1},{9:F1})",
                Subsystem, clientGridX, clientGridY,
                serverX, serverY, reconciliationThreshold,
                worldPos.x, worldPos.y,
                newWorld.x, newWorld.y));

            transform.position = newWorld;
        }
    }
}
