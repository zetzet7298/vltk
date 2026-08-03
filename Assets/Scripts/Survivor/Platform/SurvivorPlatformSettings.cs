// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorPlatformSettings (ticket 42, mobile ship)
// Runtime platform config cho mobile: portrait lock, 60fps, sleep, safe-area,
// joystick radius. Tự nạp thêm component vào SurvivorDirector GO (scene wiring
// = orchestrator/build; KHÔNG sửa SurvivorGameDirector).
//
//  - Portrait: Android + Editor set Screen.orientation ngay (ProjectSettings
//    defaultScreenOrientation=3 AutoRotation — runtime lock phòng hờ). iOS:
//    Screen.orientation bị ignore → portrait = PlayerSettings (build config,
//    orchestrator lo). Fail-closed: chỉ set khi lock bật.
//  - 60fps: Application.targetFrameRate = 60 (vsync mobile mặc định off).
//  - Safe-area: notch/cutout → CurrentSafePadding (normalized inset 4 cạnh);
//    consumer UI (HUD/Overlay ticket 37) đọc static này — additive, không sửa 37.
//  - Joystick: SurvivorJoystick đã dùng Input.touch + WASD fallback (đã verify);
//    Radius const→field (edit ADDITIVE, báo ticket) để thiết bị to/nhỏ chỉnh được.
// Fail-closed: director null → bỏ qua joystick; safe-area invalid → padding 0.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Safe-area inset 4 cạnh (normalized 0..1) — rõ ràng hơn Rect (xMax = x+w bẫy).</summary>
    public struct SafePadding
    {
        public float Left, Bottom, Right, Top;

        public bool IsZero => Left == 0f && Bottom == 0f && Right == 0f && Top == 0f;
    }

    /// <summary>Padding math thuần (test EditMode, không scene).</summary>
    public static class SafeAreaUtil
    {
        /// <summary>
        /// Normalized inset từ 4 cạnh screen. Fail-closed: screen ≤ 0 → zero;
        /// giá trị ngoài [0,1] → clamp.
        /// </summary>
        public static SafePadding ComputePadding(Rect safeArea, Vector2 screen)
        {
            if (screen.x <= 0f || screen.y <= 0f) return default;
            return new SafePadding
            {
                Left = Clamp01(safeArea.xMin / screen.x),
                Bottom = Clamp01(safeArea.yMin / screen.y),
                Right = Clamp01((screen.x - safeArea.xMax) / screen.x),
                Top = Clamp01((screen.y - safeArea.yMax) / screen.y),
            };
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    /// <summary>
    /// Platform settings runtime. Awake: áp ngay; Update: refresh safe-area khi
    /// Screen.safeArea đổi (status bar/rotation edge case — portrait cố định nên
    /// hiếm, so Rect mỗi frame rẻ).
    /// </summary>
    public sealed class SurvivorPlatformSettings : MonoBehaviour
    {
        [Tooltip("Khóa portrait (Android + Editor). iOS = PlayerSettings (build).")]
        public bool LockPortrait = true;

        [Tooltip("60fps budget — Application.targetFrameRate; ≤0 → không set.")]
        public int TargetFrameRate = 60;

        [Tooltip("Giữ màn hình sáng khi chơi.")]
        public bool KeepScreenAwake = true;

        [Tooltip("Bán kính joystick px (SurvivorJoystick.Radius — const→field additive).")]
        public float JoystickRadius = 140f;

        /// <summary>Safe-area inset normalized mới nhất — UI đọc static này.</summary>
        public static SafePadding CurrentSafePadding { get; private set; }

        private Rect _lastSafeArea;

        private void Awake()
        {
            Apply();
            RefreshSafeArea(force: true);
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea) RefreshSafeArea(force: false);
        }

        public void Apply()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            if (LockPortrait) Screen.orientation = ScreenOrientation.Portrait; // iOS ignore — PlayerSettings
#endif
            if (TargetFrameRate > 0) Application.targetFrameRate = TargetFrameRate;
            if (KeepScreenAwake) Screen.sleepTimeout = SleepTimeout.NeverSleep;
            var d = SurvivorGameDirector.Instance;
            if (d != null && JoystickRadius > 0f) d.Input.Radius = JoystickRadius; // additive — không sửa joystick logic
        }

        private void RefreshSafeArea(bool force)
        {
            _lastSafeArea = Screen.safeArea;
            CurrentSafePadding = SafeAreaUtil.ComputePadding(_lastSafeArea,
                new Vector2(Screen.width, Screen.height));
            if (force)
                Debug.Log($"[SurvivorPlatform] portrait={LockPortrait} fps={Application.targetFrameRate} " +
                          $"safe={CurrentSafePadding}");
        }
    }
}
