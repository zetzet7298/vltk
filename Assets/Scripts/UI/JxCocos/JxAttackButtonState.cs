// -----------------------------------------------------------------------------
// VLTK Mobile — JX attack button state (port of jx-cocos KgameWorldVN.cpp)
//
// Nguồn truth: /home/zet/Projects/jx-cocos/client/Classes/vn/gamescence/KgameWorldVN.cpp
//
// Trong KgameWorldVN.cpp (dòng ~800-830):
//  - _Maniattack_State: Sprite hiển thị animation đòn đánh (main skill swing).
//    animation_attack = CCAnimation::create():
//      for j=0..5: addSpriteFrame("KgameWorld/attack_%d.png")   ← 6 frame
//      setDelayPerUnit(0.05f)
//      setLoops(1)
//      setRestoreOriginalFrame(TRUE)
//  - Vị trí: tại pMainSkillItem position (+origin, -KONIS_OFFSET).
//  - Nút skill chính: mr-1_new.png (idle), mr-2_new.png (pressed) — UI resource.
//
// Logic port-critical (verify được):
//  - Frame count = 6 (attack_0..attack_5).
//  - DelayPerUnit = 0.05s, Loops = 1, RestoreOriginalFrame = true.
//  - Press state: đang hold nút → IsPressed. Trigger 1 swing animation cycle.
//  - Swing duration = FrameCount * DelayPerUnit = 0.30s.
//
// Lớp này là PURE STATE (không MonoBehaviour), EditMode-testable.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.UI.JxCocos
{
    /// <summary>Nút tấn công / main skill — port _Maniattack_State (KgameWorldVN.cpp).</summary>
    public sealed class JxAttackButtonState
    {
        // --- Animation config (KgameWorldVN.cpp, _Maniattack_State setup) ---
        /// <summary>Số frame swing animation (attack_0..attack_5).</summary>
        public const int FrameCount = 6;
        /// <summary>DelayPerUnit (giây). Nguồn: setDelayPerUnit(0.05f).</summary>
        public const float DelayPerUnit = 0.05f;
        /// <summary>Số lần loop. Nguồn: setLoops(1).</summary>
        public const int Loops = 1;
        /// <summary>Restore original frame sau khi chạy xong. Nguồn: TRUE.</summary>
        public const bool RestoreOriginalFrame = true;

        /// <summary>Tổng thời gian 1 swing = FrameCount * DelayPerUnit.</summary>
        public const float SwingDuration = FrameCount * DelayPerUnit; // 0.30s

        // --- Main skill button (mr-1/mr-2_new.png) ---
        public const string TextureIdle = "KgameWorld/mr-1_new.png";
        public const string TexturePressed = "KgameWorld/mr-2_new.png";
        public const string AttackFramePattern = "KgameWorld/attack_{0}.png";

        /// <summary>Đang hold nút?</summary>
        public bool IsPressed { get; private set; }
        /// <summary>Đang chạy swing animation?</summary>
        public bool IsSwinging { get; private set; }
        /// <summary>Thời gian đã trôi qua trong swing hiện tại (giây).</summary>
        public float SwingElapsed { get; private set; }

        /// <summary>Frame animation hiện tại [0..FrameCount-1] khi đang swing, -1 nếu idle.</summary>
        public int CurrentFrame
        {
            get
            {
                if (!IsSwinging) return -1;
                int f = Mathf.FloorToInt(SwingElapsed / DelayPerUnit);
                return Mathf.Clamp(f, 0, FrameCount - 1);
            }
        }

        /// <summary>Đường dẫn frame attack_%d.png cho index [0..FrameCount-1].</summary>
        public static string GetAttackFramePath(int index)
        {
            return string.Format(AttackFramePattern, Mathf.Clamp(index, 0, FrameCount - 1));
        }

        /// <summary>Press nút → trigger 1 swing cycle.</summary>
        public void Press()
        {
            IsPressed = true;
            IsSwinging = true;
            SwingElapsed = 0f;
        }

        /// <summary>Release nút.</summary>
        public void Release()
        {
            IsPressed = false;
        }

        /// <summary>
        /// Tick swing animation. Trả true khi swing vừa kết thúc (frame cuối).
        /// Auto restore: IsSwinging=false khi SwingElapsed &gt;= SwingDuration.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (!IsSwinging) return false;
            SwingElapsed += deltaTime;
            if (SwingElapsed >= SwingDuration)
            {
                // Loops=1, RestoreOriginalFrame=true → kết thúc + về frame gốc.
                IsSwinging = false;
                SwingElapsed = 0f;
                return true;
            }
            return false;
        }

        /// <summary>Texture nút hiện tại (idle/pressed).</summary>
        public string CurrentTexture => IsPressed ? TexturePressed : TextureIdle;
    }
}
