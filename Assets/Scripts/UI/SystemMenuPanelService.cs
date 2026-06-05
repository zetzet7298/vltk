// -----------------------------------------------------------------------------
// VLTK Mobile — UI System Menu Panel Service (Menu Hệ Thống)
// Menu chính của game: Nhân Vật, Kỹ Năng, Túi Đồ, Bản Đồ, Nhiệm Vụ, Thư, Bang.
// Vietnamese: "Menu", "Nhân Vật", "Kỹ Năng", "Túi Đồ", "Bản Đồ", "Nhiệm Vụ".
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.UI
{
    public readonly struct SystemMenuRow
    {
        public readonly int menuId;
        public readonly string name;
        public readonly string description;
        public readonly string iconPath;
        public readonly bool isEnabled;
        public readonly bool requiresConfirm;

        public SystemMenuRow(int menuId, string name, string description, string iconPath, bool isEnabled, bool requiresConfirm)
        {
            this.menuId = menuId;
            this.name = name ?? string.Empty;
            this.description = description ?? string.Empty;
            this.iconPath = iconPath ?? string.Empty;
            this.isEnabled = isEnabled;
            this.requiresConfirm = requiresConfirm;
        }
    }

    public sealed class SystemMenuPanelSnapshot
    {
        public int playerId;
        public IReadOnlyList<SystemMenuRow> rows;
    }

    /// <summary>
    /// Panel service Menu Hệ Thống — danh sách menu chính + filter enabled.
    /// </summary>
    public static class SystemMenuPanelService
    {
        public const int MenuCharacter = 0;
        public const int MenuSkill = 1;
        public const int MenuInventory = 2;
        public const int MenuMap = 3;
        public const int MenuQuest = 4;
        public const int MenuMail = 5;
        public const int MenuGuild = 6;
        public const int MenuTitle = 7;
        public const int MenuAchievement = 8;
        public const int MenuSettings = 9;
        public const int MenuHelp = 10;
        public const int MenuLogout = 11;
        public const int MenuQuit = 12;

        public static SystemMenuPanelSnapshot BuildSnapshot()
        {
            var snap = new SystemMenuPanelSnapshot
            {
                playerId = 0,
                rows = new List<SystemMenuRow>(),
            };
            try
            {
                var list = new List<SystemMenuRow>
                {
                    new SystemMenuRow(MenuCharacter, "Nhân Vật", "Thông tin nhân vật", "icons/character.png", true, false),
                    new SystemMenuRow(MenuSkill, "Kỹ Năng", "Cây kỹ năng và chiêu thức", "icons/skill.png", true, false),
                    new SystemMenuRow(MenuInventory, "Túi Đồ", "Túi đồ và trang bị", "icons/inventory.png", true, false),
                    new SystemMenuRow(MenuMap, "Bản Đồ", "Bản đồ thế giới", "icons/map.png", true, false),
                    new SystemMenuRow(MenuQuest, "Nhiệm Vụ", "Nhiệm vụ hiện tại", "icons/quest.png", true, false),
                    new SystemMenuRow(MenuMail, "Thư", "Hòm thư", "icons/mail.png", true, false),
                    new SystemMenuRow(MenuGuild, "Bang Hội", "Bang hội và thành viên", "icons/guild.png", true, false),
                    new SystemMenuRow(MenuTitle, "Danh Hiệu", "Danh hiệu và thành tựu", "icons/title.png", true, false),
                    new SystemMenuRow(MenuAchievement, "Thành Tựu", "Hệ thống thành tựu", "icons/achievement.png", true, false),
                    new SystemMenuRow(MenuSettings, "Cài Đặt", "Cài đặt game", "icons/settings.png", true, false),
                    new SystemMenuRow(MenuHelp, "Trợ Giúp", "Hướng dẫn và FAQ", "icons/help.png", true, false),
                    new SystemMenuRow(MenuLogout, "Đăng Xuất", "Đăng xuất khỏi game", "icons/logout.png", true, true),
                    new SystemMenuRow(MenuQuit, "Thoát", "Thoát khỏi game", "icons/quit.png", true, true),
                };
                snap.rows = list;
            }
            catch { }
            return snap;
        }

        public static SystemMenuRow? GetByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var snap = BuildSnapshot();
            foreach (var r in snap.rows)
                if (string.Equals(r.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return r;
            return null;
        }

        public static IReadOnlyList<SystemMenuRow> GetEnabled()
        {
            var snap = BuildSnapshot();
            var list = new List<SystemMenuRow>();
            foreach (var r in snap.rows)
                if (r.isEnabled) list.Add(r);
            return list;
        }
    }
}
