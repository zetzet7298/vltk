// -----------------------------------------------------------------------------
// VLTK Mobile — JX joystick state (port of jx-cocos HRocker / KHRocker.cpp)
//
// Nguồn truth: /home/zet/Projects/jx-cocos/client/Classes/gamescene/KHRocker.cpp
// Setup:       /home/zet/Projects/jx-cocos/client/Classes/vn/gamescence/KgameWorldVN.cpp
//
// HRocker là joystick di chuyển (yaoganx.png = thumb, life_bg_null.png = bg).
// Setup trong KgameWorldVN.cpp (dòng ~845):
//   joystick = HRocker::HRockerWithCenter(ccp(150, 150), 85, controlSprite, ygSprite, true);
//                              ^center        ^radius ^isFollowRole=true
//
// Logic port-critical (verify được):
//  - getRad(p1,p2): góc [0,2π) rad, math convention (CCW từ +x, y-up):
//      yy >= 0 ? acos(xx/xie) : 2π - acos(xx/xie)
//  - getAngleSigned(): = getRad * (180/π)  → [0,360)
//  - Dead zone: nDistance <= 5 → return (không move)   [onTouchMoved]
//  - 8-direction bucketing: nVer thresholds (22.5°) → nDir code (xem AngleToDir).
//    Bảng mã nDir (sprite frame start index = direction_index * 8, range 0..63):
//      0=Down, 8=DownLeft, 16=Left, 24=UpLeft,
//      32=Up, 40=UpRight, 48=Right, 56=DownRight
//    (CCW từ Down — khớp layout frame sprite JX).
//  - updateMovement: clamp nDir 0..63, nM 0..2 → Goto(nDir, nM).
//    Block khi m_Doing == do_attack || do_magic.
//  - getDirection(): ccpNormalize(centerPoint - currentPoint) — port y nguyên.
//
// Touch area (isFollowRole): CCRectMake(55, 40, 300, 250) — toạ độ node-space.
// Toạ độ đầu vào PHẢI y-up (GL/cocos convention). Adapter chịu trách nhiệm convert.
//
// Lớp này là PURE STATE (không MonoBehaviour), EditMode-testable.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.UI.JxCocos
{
    /// <summary>Joystick di chuyển — port HRocker (KHRocker.cpp).</summary>
    public sealed class JxJoystickState
    {
        // --- Setup constants (KgameWorldVN.cpp) ---
        public const float DefaultCenterX = 150f;
        public const float DefaultCenterY = 150f;
        public const float DefaultRadius = 85f;

        /// <summary>Dead radius: nDistance &lt;= 5 → bỏ qua (onTouchMoved).</summary>
        public const float DeadRadius = 5f;

        // --- Touch area (onTouchBegan, isFollowRole=true) ---
        // CCRectMake(55, 40, 300, 250) — node-space, y-up.
        public const float TouchAreaX = 55f;
        public const float TouchAreaY = 40f;
        public const float TouchAreaW = 300f;
        public const float TouchAreaH = 250f;

        // --- JX 8-direction codes (KHRocker.cpp onTouchMoved bucketing) ---
        // nDir = direction_index * 8. Direction index order (CCW từ Down):
        //   0=Down, 1=DownLeft, 2=Left, 3=UpLeft, 4=Up, 5=UpRight, 6=Right, 7=DownRight
        public const int DirDown = 0;
        public const int DirDownLeft = 8;
        public const int DirLeft = 16;
        public const int DirUpLeft = 24;
        public const int DirUp = 32;
        public const int DirUpRight = 40;
        public const int DirRight = 48;
        public const int DirDownRight = 56;
        /// <summary>Chưa có hướng (chưa active / trong dead zone).</summary>
        public const int DirNone = -1;

        // --- Clamp ranges (updateMovement) ---
        public const int NDirMin = 0;
        public const int NDirMax = 63;
        public const int NMMin = 0;
        public const int NMMax = 2;

        /// <summary>Center hiện tại (theo touch begin — isFollowRole).</summary>
        public Vector2 Center { get; private set; }
        /// <summary>Bán kính joystick.</summary>
        public float Radius { get; private set; }
        /// <summary>Vị trí thumb hiện tại (đã clamp trong radius).</summary>
        public Vector2 Current { get; private set; }
        /// <summary>Đang active (đang chạm).</summary>
        public bool IsActive { get; private set; }
        /// <summary>isRun — đang trong trạng thái di chuyển.</summary>
        public bool IsRunning { get; private set; }
        /// <summary>nDir hiện tại (0..63) hoặc <see cref="DirNone"/>.</summary>
        public int Dir { get; private set; }
        /// <summary>nM (move mode) — clamp 0..2.</summary>
        public int MoveMode { get; private set; }
        /// <summary>Block movement (đang attack/magic)? Set bởi gameplay.</summary>
        public bool IsActionLocked { get; set; }

        public JxJoystickState(float radius = DefaultRadius)
        {
            Radius = radius;
            Center = new Vector2(DefaultCenterX, DefaultCenterY);
            Current = Center;
            Dir = DirNone;
            MoveMode = 0;
        }

        /// <summary>
        /// getRad(p1, p2) — góc [0, 2π) radian, math convention (CCW từ +x, y-up).
        /// Nguồn: KHRocker.cpp getRad.
        /// </summary>
        public static float GetRad(Vector2 center, Vector2 point)
        {
            float xx = point.x - center.x;
            float yy = point.y - center.y;
            float xie = Mathf.Sqrt(xx * xx + yy * yy);
            if (xie == 0f) return 0f;
            return yy >= 0f
                ? Mathf.Acos(xx / xie)
                : (2f * Mathf.PI - Mathf.Acos(xx / xie));
        }

        /// <summary>getAngleSigned() — degrees [0, 360). Nguồn: KHRocker.cpp.</summary>
        public static float GetAngleSigned(Vector2 center, Vector2 point)
        {
            return GetRad(center, point) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 8-direction bucketing: nVer (deg [0,360)) → nDir code.
        /// Nguồn: KHRocker.cpp onTouchMoved (boundaries 22.5°, lower-inclusive).
        /// </summary>
        public static int AngleToDir(float nVerDeg)
        {
            if (nVerDeg >= 337.5f || nVerDeg < 22.5f) return DirRight;        // phải
            if (nVerDeg < 67.5f) return DirUpRight;                            // phải lên
            if (nVerDeg < 112.5f) return DirUp;                                // lên
            if (nVerDeg < 157.5f) return DirUpLeft;                            // trái lên
            if (nVerDeg < 202.5f) return DirLeft;                              // trái
            if (nVerDeg < 247.5f) return DirDownLeft;                          // trái xuống
            if (nVerDeg < 292.5f) return DirDown;                              // xuống
            return DirDownRight;                                               // phải xuống (292.5..337.5)
        }

        /// <summary>
        /// getDirection() — ccpNormalize(centerPoint - currentPoint).
        /// Port y nguyên từ source (vector từ current → center). Vector zero nếu degenerate.
        /// </summary>
        public static Vector2 GetDirection(Vector2 center, Vector2 current)
        {
            Vector2 d = center - current;
            float mag = d.magnitude;
            return mag > 0f ? d / mag : Vector2.zero;
        }

        /// <summary>Vòng quay thumb (độ) — jsSprite->setRotation(Rotation). CCW từ +x.</summary>
        public static float GetThumbRotation(Vector2 center, Vector2 current)
        {
            Vector2 v = current - center;
            return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        }

        /// <summary>Touch có nằm trong vùng nhận (isFollowRole rect) không?</summary>
        public bool IsInTouchArea(Vector2 p)
        {
            return p.x >= TouchAreaX && p.x <= TouchAreaX + TouchAreaW
                && p.y >= TouchAreaY && p.y <= TouchAreaY + TouchAreaH;
        }

        /// <summary>
        /// onTouchBegan (isFollowRole): active nếu touch trong vùng nhận.
        /// Center = touchPoint, currentPoint = touchPoint.
        /// </summary>
        public bool TryBegin(Vector2 touchPoint)
        {
            if (!IsInTouchArea(touchPoint)) return false;
            Center = touchPoint;
            Current = touchPoint;
            IsActive = true;
            IsRunning = true;
            Dir = DirNone;
            MoveMode = 0;
            return true;
        }

        /// <summary>
        /// onTouchMoved: clamp current trong radius, dead-zone check, bucket nDir.
        /// Trả false nếu trong dead zone (nDistance &lt;= 5).
        /// </summary>
        public bool Move(Vector2 touchPoint)
        {
            if (!IsActive) return false;

            float dist = Vector2.Distance(touchPoint, Center);
            if (dist > Radius)
                Current = Center + (touchPoint - Center).normalized * Radius;
            else
            {
                if (dist <= DeadRadius) return false; // 原地不动 — skip
                Current = touchPoint;
            }

            float nVer = GetAngleSigned(Center, Current);
            Dir = AngleToDir(nVer);
            MoveMode = 0;
            return true;
        }

        /// <summary>
        /// updateMovement: kiểm tra sẵn sàng Goto.
        /// Trả true + dir/mode đã clamp nếu: đang run, distance &gt; dead, không action-locked.
        /// Block khi IsActionLocked (m_Doing == do_attack/do_magic).
        /// </summary>
        public bool ShouldGoto(out int dir, out int mode)
        {
            dir = Dir;
            mode = MoveMode;
            if (!IsRunning) return false;
            if (IsActionLocked) return false;
            float distance = Vector2.Distance(Current, Center);
            if (distance <= DeadRadius) return false;
            if (Dir < 0) return false;
            dir = Mathf.Clamp(Dir, NDirMin, NDirMax);
            mode = Mathf.Clamp(MoveMode, NMMin, NMMax);
            return true;
        }

        /// <summary>onTouchEnded: ngừng run, reset dir.</summary>
        public void End()
        {
            IsRunning = false;
            IsActive = false;
            Dir = DirNone;
        }

        /// <summary>Inactive() — ẩn + ngừng schedule (HRocker::Inactive).</summary>
        public void Deactivate()
        {
            IsActive = false;
            IsRunning = false;
            Dir = DirNone;
        }
    }
}
