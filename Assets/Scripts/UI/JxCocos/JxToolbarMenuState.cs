// -----------------------------------------------------------------------------
// VLTK Mobile — JX top toolbar menu state (E0)
// Port source: /home/zet/Projects/jx-cocos/client/Classes/vn/gamescence/KgameWorldVN.cpp
//
// Source lines around toolbar creation:
//  - 9 CCMenuItemSprite buttons in order:
//    Nhân Vật, Hành Trang, Võ Công, Bảo Danh, Bằng Hữu, Tổ Đội,
//    Bang Hội, Cài Đặt, Kỳ Trân Các.
//  - normal/selected/disabled sprites live under ui_vn/toolbar/*.png.
//  - all normal buttons scale 0.85f; shop override scale 0.9f and position (215,0).
//  - CCMenu alignItemsHorizontallyWithPadding(5), position y = visibleHeight - 30 + offset.
//  - Bảo Danh button is replaced with a lambda that shows the VN notice instead
//    of opening the task panel: "Đại hiệp có thể xem tại Tiếu Ngạo Giang Hồ Lục".
//  - Source callback behavior is not uniform: some panels toggle when open, while
//    skill/team are one-shot no-op when already open, options recreates.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.UI.JxCocos
{
    public enum JxToolbarButtonPolicy
    {
        Toggle = 0,
        OpenIfClosed = 1,
        AlwaysReopen = 2,
        NoticeOnly = 3,
    }

    public sealed class JxToolbarMenuButton
    {
        public int Index;
        public JxHudPanel Panel;
        public string Label = string.Empty;
        public string NormalSprite = string.Empty;
        public string SelectedSprite = string.Empty;
        public string DisabledSprite = string.Empty;
        public float Scale = 0.85f;
        public Vector2 LocalPosition;
        public bool HasExplicitLocalPosition;
        public JxToolbarButtonPolicy Policy;
        public string Callback = string.Empty;
        public string Notice = string.Empty;
    }

    public sealed class JxToolbarMenuCommand
    {
        public JxHudPanel Panel;
        public bool Open;
        public bool Close;
        public bool Reopen;
        public string Callback = string.Empty;
        public string Notice = string.Empty;
    }

    public sealed class JxToolbarMenuState
    {
        public const int ButtonCount = 9;
        public const float DefaultScale = 0.85f;
        public const float ShopScale = 0.9f;
        public const float HorizontalPadding = 5f;
        public const float TopMenuOffsetY = -30f;
        public const string SpriteRoot = "ui_vn/toolbar/";
        public const string TaskNotice = "Đại hiệp có thể xem tại Tiếu Ngạo Giang Hồ Lục";

        public static readonly JxToolbarMenuButton[] Buttons =
        {
            Button(0, JxHudPanel.Character, "Nhân Vật", "nhanvat.png", "nhanvat2.png", "nhanvat2.png", JxToolbarButtonPolicy.AlwaysReopen, "mRoleStatusCallback"),
            Button(1, JxHudPanel.Inventory, "Hành Trang", "hanhtrang.png", "hanhtrang2.png", "hanhtrang2.png", JxToolbarButtonPolicy.Toggle, "mItemsCallback"),
            Button(2, JxHudPanel.Skill, "Võ Công", "vocong.png", "vocong2.png", "vocong2.png", JxToolbarButtonPolicy.OpenIfClosed, "mSkillsCallback"),
            Button(3, JxHudPanel.Quest, "Bảo Danh", "baodanh.png", "baodanh2.png", "baodanh2.png", JxToolbarButtonPolicy.NoticeOnly, "lambdaTaskNotice", TaskNotice),
            Button(4, JxHudPanel.Friend, "Bằng Hữu", "banghuu.png", "banghuu2.png", "banghuu2.png", JxToolbarButtonPolicy.Toggle, "mFriendCallback"),
            Button(5, JxHudPanel.Team, "Tổ Đội", "todoi.png", "todoi2.png", "todoi2.png", JxToolbarButtonPolicy.OpenIfClosed, "mTeamCallback"),
            Button(6, JxHudPanel.Guild, "Bang Hội", "banghoi.png", "banghoi2.png", "banghoi2.png", JxToolbarButtonPolicy.Toggle, "mFactionCallback"),
            Button(7, JxHudPanel.Settings, "Cài Đặt", "caidat.png", "caidat2.png", "caidat2.png", JxToolbarButtonPolicy.AlwaysReopen, "mOptionsCallback"),
            Button(8, JxHudPanel.Shop, "Kỳ Trân Các", "kytrancac1.png", "kytrancac2.png", "kytrancac3.png", JxToolbarButtonPolicy.AlwaysReopen, "mQizCallback", scale: ShopScale, position: new Vector2(215f, 0f), hasPosition: true),
        };

        private readonly HashSet<JxHudPanel> _openPanels = new();
        public IReadOnlyCollection<JxHudPanel> OpenPanels => _openPanels;

        public bool IsOpen(JxHudPanel panel) => _openPanels.Contains(panel);

        public void SetOpen(JxHudPanel panel, bool open)
        {
            if (panel == JxHudPanel.None) return;
            if (open) _openPanels.Add(panel);
            else _openPanels.Remove(panel);
        }

        public JxToolbarMenuCommand Press(JxHudPanel panel)
        {
            var cfg = Get(panel);
            if (cfg == null) return new JxToolbarMenuCommand { Panel = panel };

            var cmd = new JxToolbarMenuCommand { Panel = panel, Callback = cfg.Callback, Notice = cfg.Notice };
            switch (cfg.Policy)
            {
                case JxToolbarButtonPolicy.NoticeOnly:
                    // Source task button is overwritten by lambda notice, does not open OBJ_NODE_TASK.
                    return cmd;
                case JxToolbarButtonPolicy.Toggle:
                    if (_openPanels.Contains(panel))
                    {
                        _openPanels.Remove(panel);
                        cmd.Close = true;
                    }
                    else
                    {
                        _openPanels.Add(panel);
                        cmd.Open = true;
                    }
                    return cmd;
                case JxToolbarButtonPolicy.OpenIfClosed:
                    if (!_openPanels.Contains(panel))
                    {
                        _openPanels.Add(panel);
                        cmd.Open = true;
                    }
                    return cmd;
                case JxToolbarButtonPolicy.AlwaysReopen:
                    if (_openPanels.Contains(panel))
                    {
                        cmd.Close = true;
                        cmd.Reopen = true;
                    }
                    _openPanels.Add(panel);
                    cmd.Open = true;
                    return cmd;
                default:
                    return cmd;
            }
        }

        public Vector2 MenuPosition(float visibleWidth, float visibleHeight, Vector2 origin, float wideDeviceOffsetY = 0f) =>
            new(visibleWidth / 2f + origin.x, visibleHeight + origin.y + TopMenuOffsetY + wideDeviceOffsetY);

        public static JxToolbarMenuButton Get(JxHudPanel panel)
        {
            for (int i = 0; i < Buttons.Length; i++)
                if (Buttons[i].Panel == panel) return Buttons[i];
            return null;
        }

        public static int IndexOf(JxHudPanel panel)
        {
            var button = Get(panel);
            return button?.Index ?? -1;
        }

        private static JxToolbarMenuButton Button(
            int index,
            JxHudPanel panel,
            string label,
            string normal,
            string selected,
            string disabled,
            JxToolbarButtonPolicy policy,
            string callback,
            string notice = "",
            float scale = DefaultScale,
            Vector2 position = default,
            bool hasPosition = false)
        {
            return new JxToolbarMenuButton
            {
                Index = index,
                Panel = panel,
                Label = label,
                NormalSprite = SpriteRoot + normal,
                SelectedSprite = SpriteRoot + selected,
                DisabledSprite = SpriteRoot + disabled,
                Scale = scale,
                LocalPosition = position,
                HasExplicitLocalPosition = hasPosition,
                Policy = policy,
                Callback = callback,
                Notice = notice ?? string.Empty,
            };
        }
    }
}
