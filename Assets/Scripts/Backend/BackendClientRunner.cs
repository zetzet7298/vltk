// -----------------------------------------------------------------------------
// VLTK.Backend — BackendClientRunner
// MonoBehaviour runtime "wirer": load BackendConfig (Resources/BackendConfig
// asset → StreamingAssets/BackendConfig.json override) → tạo BackendClient
// → thực thi luồng login → list roles → enter map.
//
// Mục đích: cung cấp một runtime entry point cho Bootstrap scene để verify
// rằng end-to-end (StreamingAssets config → REST/Mock → server) hoạt động,
// thay vì chỉ smoke qua EditMode test. Khi `useMock=true` (mặc định) thì
// không cần server thật; khi `useMock=false` và StreamingAssets có JSON
// override thì sẽ gọi server thật.
//
// Bootstrap scene tối thiểu (Assets/Scenes/Bootstrap.unity) chỉ cần
// GameObject này — KHÔNG cần Camera/UI/EventSystem. Runner chạy trong
// Awake/Start và log kết quả ra Console.
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VLTK.Backend.Dto;

namespace VLTK.Backend
{
    /// <summary>
    /// Runtime entry point: khởi tạo <see cref="BackendClient"/> từ
    /// <see cref="BackendConfig"/> (Resources + StreamingAssets override) rồi
    /// chạy luồng login → list roles → enter map.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BackendClientRunner : MonoBehaviour
    {
        /// <summary>Account dùng cho login smoke. Có thể chỉnh trong Inspector.</summary>
        [Tooltip("Tên account dùng cho login smoke (POST /v1/account/login).")]
        public string accName = "runner_smoke";

        [Tooltip("Password PLAINTEXT cho login smoke.")]
        public string password = "runner_pw";

        [Tooltip("Client IP override (optional) — gửi kèm login body.")]
        public string clientIp;

        [Tooltip("map_id để enter sau khi có role đầu tiên. Mặc định 1 = Phượng Tường.")]
        [Min(1)]
        public int enterMapId = 1;

        [Tooltip("Vị trí spawn khi enter map (posX, posY).")]
        public int enterPosX = 1500;

        [Tooltip("Vị trí spawn khi enter map (posX, posY).")]
        public int enterPosY = 1500;

        [Tooltip("Chạy luồng smoke trong Start(). Bỏ tick nếu muốn gọi RunAsync() thủ công.")]
        public bool runOnStart = true;

        [Tooltip("Timeout toàn bộ luồng (giây). Runner dừng và log lỗi nếu quá hạn.")]
        [Min(1)]
        public int runTimeoutSeconds = 15;

        /// <summary>BackendClient thực sự dùng để gọi API (cho diagnostics/UI).</summary>
        public BackendClient Client { get; private set; }

        /// <summary>Config đã load (kết quả sau StreamingAssets override).</summary>
        public BackendConfig Config { get; private set; }

        /// <summary>True nếu luồng smoke đã chạy xong (cả khi lỗi).</summary>
        public bool IsCompleted { get; private set; }

        /// <summary>Thông báo lỗi cuối cùng (null nếu thành công).</summary>
        public string LastError { get; private set; }

        private void Start()
        {
            if (runOnStart)
            {
                // Fire-and-forget — Unity coroutine không thể chờ Task, nên
                // dùng callback async. _ = RunAsync(...).
                _ = RunAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Khởi tạo BackendClient (Resources + StreamingAssets override) rồi
        /// chạy luồng login → list roles → enter map. Hàm này có thể gọi
        /// từ code khác (UI, test runner, Editor menu) ngoài Start().
        /// </summary>
        public async Task RunAsync(CancellationToken externalCt)
        {
            if (IsCompleted)
            {
                Debug.LogWarning("[BackendClientRunner] RunAsync() đã chạy rồi; bỏ qua.");
                return;
            }

            // 1. Load config — Resources/BackendConfig.asset → default.
            Config = BackendConfig.LoadOrDefault();
            // 2. Áp dụng override từ StreamingAssets/BackendConfig.json (nếu có).
            Config.ApplyStreamingAssetsOverrideIfPresent();
            // 3. Tạo BackendClient (mock vs rest theo config.useMock).
            Client = new BackendClient(Config);

            Debug.Log($"[BackendClientRunner] config: baseUrl={Config.baseUrl} " +
                      $"apiPrefix={Config.apiPrefix} useMock={Config.useMock} " +
                      $"timeout={Config.defaultTimeoutSeconds}s");

            using var timeoutCts = new CancellationTokenSource(
                TimeSpan.FromSeconds(runTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                externalCt, timeoutCts.Token);
            var ct = linkedCts.Token;

            try
            {
                // Luồng 1: POST /v1/account/login
                var login = await Client.LoginAsync(accName, password, null, clientIp, ct);
                if (!login.IsSuccess)
                {
                    LastError = $"login failed: {login.code} {login.message}";
                    Debug.LogError($"[BackendClientRunner] {LastError}");
                    return;
                }
                Debug.Log($"[BackendClientRunner] login OK: accName={login.data.accName} " +
                          $"serviceFlag={login.data.serviceFlag} extPoint={login.data.extPoint}");

                // Luồng 2: GET /v1/role/by-account/{accName}
                var roles = await Client.ListRolesAsync(login.data.accName, ct);
                if (!roles.IsSuccess)
                {
                    LastError = $"list roles failed: {roles.code} {roles.message}";
                    Debug.LogError($"[BackendClientRunner] {LastError}");
                    return;
                }
                if (roles.data == null || roles.data.roles == null || roles.data.roles.Count == 0)
                {
                    LastError = "list roles returned 0 roles; không thể enter map.";
                    Debug.LogError($"[BackendClientRunner] {LastError}");
                    return;
                }
                var firstRole = roles.data.roles[0];
                Debug.Log($"[BackendClientRunner] list roles OK: account={roles.data.account} " +
                          $"count={roles.data.roles.Count} firstRoleId={firstRole.id} " +
                          $"name={firstRole.roleName} faction={firstRole.factionName}");

                // Luồng 3: POST /v1/map/enter {roleId, mapId, posX, posY}
                var enterReq = new EnterMapRequest
                {
                    roleId = firstRole.id,
                    mapId = enterMapId,
                    posX = enterPosX,
                    posY = enterPosY,
                };
                var enter = await Client.EnterMapAsync(enterReq, ct);
                if (!enter.IsSuccess)
                {
                    LastError = $"enter map failed: {enter.code} {enter.message}";
                    Debug.LogError($"[BackendClientRunner] {LastError}");
                    return;
                }
                Debug.Log($"[BackendClientRunner] enter map OK: roleId={enter.data.roleId} " +
                          $"mapId={enter.data.mapId} pos=({enter.data.posX},{enter.data.posY})");

                LastError = null;
            }
            catch (OperationCanceledException)
            {
                LastError = $"timeout after {runTimeoutSeconds}s";
                Debug.LogError($"[BackendClientRunner] {LastError}");
            }
            catch (Exception ex)
            {
                LastError = $"{ex.GetType().Name}: {ex.Message}";
                Debug.LogError($"[BackendClientRunner] unexpected error: {LastError}");
            }
            finally
            {
                IsCompleted = true;
            }
        }
    }
}
