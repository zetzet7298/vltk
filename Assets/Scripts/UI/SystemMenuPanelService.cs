// -----------------------------------------------------------------------------
// VLTK Mobile — PC System Menu Panel Service (BtnOptions / e6641da3.ini)
// PC system menu rows: ExitGame, GameHelp, Options, OffLine, ContiumeGame.
// Vietnamese: "Thoát game", "Trợ giúp", "Tùy chọn", "Treo máy", "Tiếp tục".
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
    /// PC system menu opened by 工具控制条.ini [Options] (ClassType inferred by button slot).
    /// Source: e6641da3.ini [ExitGame]/[GameHelp]/[Options]/[OffLine]/[ContiumeGame].
    /// Commented PC sections [CloseGame] and [GameTask] are intentionally not exposed.
    /// </summary>
    public static class SystemMenuPanelService
    {
        public const int MenuExitGame = 0;
        public const int MenuGameHelp = 1;
        public const int MenuOptions = 2;
        public const int MenuOffLine = 3;
        public const int MenuContinueGame = 4;

        // Disabled/commented in PC e6641da3.ini; keep documented so we do not invent buttons.
        public static readonly IReadOnlyDictionary<string, string> DisabledPcSystemMenuButtons =
            new Dictionary<string, string>
            {
                ["CloseGame"] = "Commented out in e6641da3.ini; no active PC button.",
                ["GameTask"] = "Commented out in e6641da3.ini; no active PC button.",
            };

        public static SystemMenuPanelSnapshot BuildSnapshot()
        {
            return new SystemMenuPanelSnapshot
            {
                playerId = 0,
                rows = new List<SystemMenuRow>
                {
                    new SystemMenuRow(MenuExitGame, "Thoát game", "PC [ExitGame] — yêu cầu xác nhận trước khi rời game", @"\spr\Ui3\系统\系统－退出.spr", true, true),
                    new SystemMenuRow(MenuGameHelp, "Trợ giúp", "PC [GameHelp] — mở hướng dẫn trò chơi", @"\spr\Ui3\系统\系统－帮助.spr", true, false),
                    new SystemMenuRow(MenuOptions, "Tùy chọn", "PC [Options] — cài đặt trò chơi", @"\spr\Ui3\系统\系统－选项.spr", true, false),
                    new SystemMenuRow(MenuOffLine, "Treo máy offline", "PC [OffLine] — trạng thái treo máy/rời mạng", @"\spr\Ui3\系统\系统－离线挂机.spr", true, true),
                    new SystemMenuRow(MenuContinueGame, "Tiếp tục game", "PC [ContiumeGame] — đóng menu hệ thống và quay lại", @"\spr\Ui3\系统\系统－继续.spr", true, false),
                },
            };
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
