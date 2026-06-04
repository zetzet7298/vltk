// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.1 Mobile Input Spec & HUD Layout
// Touch input zones, joystick zones, HUD layout specs, dialogue/minimap specs.
// PC source: Mobile adaptation rules, touch-safe zones.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    [Serializable]
    public struct TouchZone
    {
        public string name;
        public Rect normalizedRect; // 0-1 range relative to screen
        public bool blocksJoystick;
        public string descriptionVi;
    }

    [Serializable]
    public struct HudElementSpec
    {
        public string elementId;
        public string nameVi;
        public Rect normalizedRect;
        public int drawOrder;
        public bool visible;
    }

    /// <summary>
    /// Spec cho embedded mobile HUD, dialogue UI, minimap, và input zones.
    /// Đảm bảo không conflict giữa joystick và HUD touch areas.
    /// </summary>
    public static class MobileInputSpec
    {
        // ── Screen Layout Constants ────────────────────────────────────────

        public const float JoystickRadius = 0.12f;        // 12% screen width
        public const float JoystickCenterX = 0.15f;       // 15% from left
        public const float JoystickCenterY = 0.25f;       // 25% from bottom

        public const float MinimapSize = 0.15f;           // 15% screen width
        public const float MinimapTop = 0.02f;            // 2% from top
        public const float MinimapRight = 0.02f;          // 2% from right

        public const float HotbarHeight = 0.10f;          // 10% screen height
        public const float HotbarBottom = 0.02f;           // 2% from bottom
        public const float HotbarRight = 0.02f;

        // ── Touch Zones ────────────────────────────────────────────────────

        public static readonly List<TouchZone> DefaultTouchZones = new()
        {
            new TouchZone
            {
                name = "joystick",
                normalizedRect = new Rect(0.0f, 0.0f, 0.35f, 0.5f),
                blocksJoystick = true,
                descriptionVi = "Vùng Joystick bên trái"
            },
            new TouchZone
            {
                name = "minimap",
                normalizedRect = new Rect(0.83f, 0.0f, 0.17f, 0.17f),
                blocksJoystick = false,
                descriptionVi = "Bản đồ nhỏ góc phải trên"
            },
            new TouchZone
            {
                name = "hotbar",
                normalizedRect = new Rect(0.2f, 0.88f, 0.6f, 0.12f),
                blocksJoystick = false,
                descriptionVi = "Thanh kỹ năng nóng"
            },
            new TouchZone
            {
                name = "action_buttons",
                normalizedRect = new Rect(0.75f, 0.5f, 0.25f, 0.35f),
                blocksJoystick = false,
                descriptionVi = "Nút hành động (Chạy/Ngựa/Tổ đội)"
            },
            new TouchZone
            {
                name = "hp_mp_bars",
                normalizedRect = new Rect(0.02f, 0.85f, 0.18f, 0.13f),
                blocksJoystick = false,
                descriptionVi = "Thanh HP/MP góc trái dưới"
            },
            new TouchZone
            {
                name = "dialogue",
                normalizedRect = new Rect(0.1f, 0.1f, 0.8f, 0.6f),
                blocksJoystick = true,
                descriptionVi = "Hộp hội thoại NPC"
            },
        };

        // ── HUD Element Specs ──────────────────────────────────────────────

        public static readonly List<HudElementSpec> DefaultHudLayout = new()
        {
            new HudElementSpec { elementId = "minimap", nameVi = "Bản Đồ Nhỏ", normalizedRect = new Rect(0.83f, 0.81f, 0.15f, 0.15f), drawOrder = 100, visible = true },
            new HudElementSpec { elementId = "hp_bar", nameVi = "Thanh Sinh Lực", normalizedRect = new Rect(0.02f, 0.92f, 0.18f, 0.03f), drawOrder = 50, visible = true },
            new HudElementSpec { elementId = "mp_bar", nameVi = "Thanh Nội Lực", normalizedRect = new Rect(0.02f, 0.88f, 0.18f, 0.03f), drawOrder = 50, visible = true },
            new HudElementSpec { elementId = "exp_bar", nameVi = "Thanh Kinh Nghiệm", normalizedRect = new Rect(0.22f, 0.96f, 0.56f, 0.02f), drawOrder = 40, visible = true },
            new HudElementSpec { elementId = "level_badge", nameVi = "Cấp Độ", normalizedRect = new Rect(0.01f, 0.94f, 0.05f, 0.05f), drawOrder = 60, visible = true },
            new HudElementSpec { elementId = "hotbar_1", nameVi = "Ô Kỹ Năng 1", normalizedRect = new Rect(0.25f, 0.88f, 0.08f, 0.10f), drawOrder = 70, visible = true },
            new HudElementSpec { elementId = "hotbar_2", nameVi = "Ô Kỹ Năng 2", normalizedRect = new Rect(0.34f, 0.88f, 0.08f, 0.10f), drawOrder = 70, visible = true },
            new HudElementSpec { elementId = "hotbar_3", nameVi = "Ô Kỹ Năng 3", normalizedRect = new Rect(0.43f, 0.88f, 0.08f, 0.10f), drawOrder = 70, visible = true },
            new HudElementSpec { elementId = "hotbar_4", nameVi = "Ô Kỹ Năng 4", normalizedRect = new Rect(0.52f, 0.88f, 0.08f, 0.10f), drawOrder = 70, visible = true },
            new HudElementSpec { elementId = "run_btn", nameVi = "Nút Chạy", normalizedRect = new Rect(0.82f, 0.55f, 0.07f, 0.07f), drawOrder = 80, visible = true },
            new HudElementSpec { elementId = "horse_btn", nameVi = "Nút Cưỡi Ngựa", normalizedRect = new Rect(0.90f, 0.55f, 0.07f, 0.07f), drawOrder = 80, visible = true },
            new HudElementSpec { elementId = "chat_bar", nameVi = "Thanh Chat", normalizedRect = new Rect(0.02f, 0.78f, 0.4f, 0.04f), drawOrder = 30, visible = true },
            new HudElementSpec { elementId = "silver_display", nameVi = "Bạc", normalizedRect = new Rect(0.02f, 0.82f, 0.12f, 0.04f), drawOrder = 45, visible = true },
        };

        /// <summary>Kiểm tra một touch position có nằm trong joystick zone không.</summary>
        public static bool IsInJoystickZone(Vector2 normalizedPos)
        {
            float dx = normalizedPos.x - JoystickCenterX;
            float dy = normalizedPos.y - JoystickCenterY;
            return (dx * dx + dy * dy) <= (JoystickRadius * JoystickRadius);
        }

        /// <summary>Tìm touch zone chứa vị trí.</summary>
        public static TouchZone FindTouchZone(Vector2 normalizedPos)
        {
            foreach (var zone in DefaultTouchZones)
            {
                if (zone.normalizedRect.Contains(normalizedPos))
                    return zone;
            }
            return default;
        }
    }
}
