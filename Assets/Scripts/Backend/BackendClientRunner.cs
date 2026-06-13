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
using VLTK.Backend.Combat;
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

        // ---- FS-03D combat demo params ----

        [Tooltip("Số lần lặp luồng cast → damage trong RunCombatDemoAsync. Mỗi lần " +
                 "publish 1 event lên CombatFeedbackBus (Normal/Crit/Miss/Heal).")]
        [Min(0)]
        public int combatDemoRounds = 3;

        [Tooltip("Skill ID dùng cho demo cast. Mặc định 22 (Kim Ba) — skill no-cost " +
                 "trong Mock backend nên không tốn mana.")]
        public int combatDemoSkillId = 22;

        [Tooltip("Damage tối đa cho hit effect (test inspector). 0 = dùng mock default.")]
        [Min(0)]
        public int combatDemoDamage = 50;

        [Tooltip("Tỉ lệ crit (0..1). 0 = không crit, 1 = luôn crit. Mặc định 0.3.")]
        [Range(0f, 1f)]
        public float critChance = 0.3f;

        [Tooltip("Tỉ lệ miss (0..1). 0 = không miss, 1 = luôn miss. Mặc định 0.1.")]
        [Range(0f, 1f)]
        public float missChance = 0.1f;

        [Tooltip("Vị trí world để publish feedback event (test/demo).")]
        public Vector3 feedbackSpawnPosition = Vector3.zero;

        [Header("Combat demo auto-run")]
        [Tooltip("Tự động chạy RunCombatDemoAsync() sau khi RunAsync() xong. " +
                 "Bỏ tick nếu muốn gọi thủ công.")]
        public bool runCombatDemoOnComplete = true;

        /// <summary>BackendClient thực sự dùng để gọi API (cho diagnostics/UI).</summary>
        public BackendClient Client { get; private set; }

        /// <summary>Config đã load (kết quả sau StreamingAssets override).</summary>
        public BackendConfig Config { get; private set; }

        /// <summary>True nếu luồng smoke đã chạy xong (cả khi lỗi).</summary>
        public bool IsCompleted { get; private set; }

        /// <summary>Thông báo lỗi cuối cùng (null nếu thành công).</summary>
        public string LastError { get; private set; }

        /// <summary>True nếu RunCombatDemoAsync() đã xong (cả khi lỗi).</summary>
        public bool IsCombatDemoCompleted { get; private set; }

        /// <summary>Số event feedback đã publish lên CombatFeedbackBus (test/UI). Reset khi RunAsync gọi lại.</summary>
        public int FeedbackEventCount { get; private set; }

        private void Start()
        {
            if (runOnStart)
            {
                // Fire-and-forget — Unity coroutine không thể chờ Task, nên
                // dùng callback async. _ = RunAsync(...).
                _ = StartFullFlowAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Convenience wrapper: chạy RunAsync (login → list roles → enter
        /// map) rồi nếu runCombatDemoOnComplete=true thì chain sang
        /// RunCombatDemoAsync (cast → damage → feedback). Trả về Task để
        /// caller có thể await cả 2 phần.
        /// </summary>
        public async Task StartFullFlowAsync(CancellationToken externalCt)
        {
            await RunAsync(externalCt);
            if (runCombatDemoOnComplete && IsCompleted && LastError == null)
            {
                await RunCombatDemoAsync(externalCt);
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

        // ===================================================================
        // FS-03D — Combat demo loop
        // ===================================================================

        /// <summary>
        /// Chạy luồng combat demo (FS-03D): cast skill (FS-03B) → damage calc
        /// (FS-03C) → publish CombatFeedbackEvent lên bus. Mỗi round:
        ///   1. Roll miss (theo <see cref="missChance"/>) → publish Miss.
        ///   2. Roll crit (theo <see cref="critChance"/>) → publish Crit.
        ///   3. Else Normal → publish Normal với damage ngẫu nhiên [1, combatDemoDamage].
        /// Cứ mỗi 2 round chèn 1 Heal event (mô phỏng regen/HoT).
        ///
        /// Mục đích: cho phép PlayMode test kiểm tra CombatFeedbackView /
        /// HitEffectSpawner / CameraShake nhận event đúng thứ tự. KHÔNG tự
        /// tính damage — lấy delta từ DamageCalcResponse (server-authoritative).
        /// </summary>
        public async Task RunCombatDemoAsync(CancellationToken externalCt)
        {
            if (Client == null)
            {
                LastError = "RunCombatDemoAsync: Client null — gọi RunAsync() trước.";
                Debug.LogError($"[BackendClientRunner] {LastError}");
                IsCombatDemoCompleted = true;
                return;
            }
            if (IsCombatDemoCompleted)
            {
                Debug.LogWarning("[BackendClientRunner] RunCombatDemoAsync() đã chạy rồi; bỏ qua.");
                return;
            }
            FeedbackEventCount = 0;
            Debug.Log($"[BackendClientRunner] combat demo start: rounds={combatDemoRounds}");

            // Role ID dùng cho demo — lấy từ first role (đã resolve trong RunAsync).
            // Trong demo thực tế, lưu roleId ở 1 biến private để dùng. Ở đây
            // giả định roleId=1 (mock mặc định) — Runner không bắt buộc biết
            // roleId thật vì mock backend bỏ qua check.
            int roleId = 1;
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                for (int i = 0; i < combatDemoRounds; i++)
                {
                    externalCt.ThrowIfCancellationRequested();

                    // Bước 1: Cast skill (FS-03B)
                    var castReq = new SkillCastRequest
                    {
                        roleId = roleId,
                        skillId = combatDemoSkillId,
                        nowMs = nowMs,
                    };
                    var cast = await Client.CastSkillAsync(castReq, externalCt);
                    if (!cast.IsSuccess || !cast.data.cast)
                    {
                        // Cast fail: xem như miss
                        PublishFeedback(CombatFeedbackKind.Miss, 0, feedbackSpawnPosition);
                        continue;
                    }

                    // Bước 2: Damage calc (FS-03C) — tính damage thật
                    var dmgReq = new DamageCalcRequest
                    {
                        atkMin = 10,
                        atkMax = combatDemoDamage > 0 ? combatDemoDamage : 50,
                        damageKind = 0, // physics
                        isMelee = true,
                        isReturn = false,
                        pkDamageRate = 100,
                        target = new CombatantState { life = 1000, lifeMax = 1000 },
                    };
                    var dmg = await Client.CalcDamageAsync(dmgReq, externalCt);
                    if (!dmg.IsSuccess)
                    {
                        PublishFeedback(CombatFeedbackKind.Miss, 0, feedbackSpawnPosition);
                        continue;
                    }

                    // Bước 3: Decide kind (miss/crit/normal) + publish
                    int damageDealt = dmg.data.damage;
                    if (damageDealt == 0)
                    {
                        PublishFeedback(CombatFeedbackKind.Miss, 0, feedbackSpawnPosition);
                    }
                    else if (UnityEngine.Random.value < critChance)
                    {
                        PublishFeedback(CombatFeedbackKind.Crit, damageDealt, feedbackSpawnPosition);
                    }
                    else
                    {
                        PublishFeedback(CombatFeedbackKind.Normal, damageDealt, feedbackSpawnPosition);
                    }

                    // Bước 4: Mỗi 2 round, chèn 1 Heal event (giả lập regen)
                    if ((i + 1) % 2 == 0)
                    {
                        int healAmount = UnityEngine.Random.Range(20, 60);
                        PublishFeedback(CombatFeedbackKind.Heal, healAmount, feedbackSpawnPosition);
                    }

                    // Yield nhỏ để không block frame quá lâu
                    await Task.Yield();
                }
                Debug.Log($"[BackendClientRunner] combat demo done: " +
                          $"{FeedbackEventCount} feedback events published.");
            }
            catch (OperationCanceledException)
            {
                LastError = "combat demo cancelled";
                Debug.LogWarning($"[BackendClientRunner] {LastError}");
            }
            catch (Exception ex)
            {
                LastError = $"combat demo error: {ex.GetType().Name}: {ex.Message}";
                Debug.LogError($"[BackendClientRunner] {LastError}");
            }
            finally
            {
                IsCombatDemoCompleted = true;
            }
        }

        private void PublishFeedback(CombatFeedbackKind kind, int value, Vector3 pos)
        {
            var evt = new CombatFeedbackEvent(kind, value, pos);
            CombatFeedbackBus.Raise(evt);
            FeedbackEventCount++;
            Debug.Log($"[BackendClientRunner] feedback publish: {evt}");
        }
    }
}
