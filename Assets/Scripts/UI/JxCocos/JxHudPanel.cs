// -----------------------------------------------------------------------------
// VLTK Mobile — JX HUD panel types + command bus (jx-cocos KgameWorldVN toolbar)
//
// Nguồn: KgameWorldVN.cpp toolbar callbacks (mRoleStatusCallback, mItemsCallback,
// mSkillsCallback, mTaskCallback, mFriendCallback, mTeamCallback, mFactionCallback,
// mOptionsCallback, mQizCallback) + ancillary (mianExc/mianPk/mianRun/mianSit/
// mianSelectNpc). Mỗi callback mở/tắt panel tương ứng.
//
// Enum index theo thứ tự gốc 9 nút menu (INDEX.md "Menu Button → File Map").
// -----------------------------------------------------------------------------

using System;

namespace VLTK.UI.JxCocos
{
    /// <summary>9 panel chính của menu toolbar (+ ancillary actions).</summary>
    public enum JxHudPanel
    {
        None = -1,
        Character = 0,   // Nhân Vật    -> KuiRoleStateVN
        Inventory = 1,   // Hành Trang  -> KuiItemVN
        Skill = 2,       // Võ Công     -> KuiSkillVN
        Quest = 3,       // Bảo Danh    -> KuiTaskInfoVN
        Friend = 4,      // Bằng Hữu    -> KuiFriendListVN
        Team = 5,        // Tổ Đội      -> KuiTeamVN
        Guild = 6,       // Bang Hội    -> KuiTongInfoVN
        Settings = 7,    // Cài Đặt     -> (options)
        Shop = 8,        // Kỳ Trân Các -> KuiShopVN

        // Overlay (not a toolbar menu button): opened by clicking the minimap.
        WorldMap = 100,  // KuiMaxMapVN overlay (big map)
    }

    /// <summary>Ancillary toolbar actions (not modal panels).</summary>
    public enum JxHudAction
    {
        None = 0,
        Exchange,  // giaodich -> giao dịch/đổi
        PkState,   // trangthai -> chọn trạng thái PK
        Run,       // dichay -> chạy/bộ
        Sit,       // ngoi -> ngồi
        Interact,  // giaotiep -> tương tác NPC gần nhất
    }

    /// <summary>
    /// Command bus the toolbar publishes open/close panel intents through, so the
    /// controller wires the actual panel show/hide without the toolbar knowing
    /// panel internals. Pure interface — EditMode-testable via fakes.
    /// </summary>
    public interface IJxHudCommandBus
    {
        /// <summary>Toggle a modal panel open/closed (toolbar press semantics).</summary>
        void PublishPanelRequested(JxHudPanel panel);

        /// <summary>Fire an ancillary toolbar action.</summary>
        void PublishActionRequested(JxHudAction action);
    }
}
