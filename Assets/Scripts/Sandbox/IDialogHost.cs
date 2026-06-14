// -----------------------------------------------------------------------------
// VLTK Mobile — IDialogHost: giao diện host cho DialogSysRuntimeService.
// Cho phép runtime dispatch các side-effect khi mở NPC dialog
// (UI panel, chuỗi text, option, ask string/number, give item, log).
// PC source: script/dailogsys/g_dialog.lua, dailogsay.lua, dialogoption.lua,
// composeoption.lua. PC surfaces: CreateNewSayEx, g_AskClientStringEx,
// g_AskClientNumberEx, g_GiveItemUI, g_DailogBack.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho dialog runtime. Implement bởi UI/Inventory/Chat.
    /// </summary>
    public interface IDialogHost
    {
        /// <summary>Hiển thị dialog panel với nội dung text cho NPC.</summary>
        void ShowDialog(string npcName, string dialogClass, string titleMsg);

        /// <summary>Thêm option surface vào dialog hiện tại (PC dialogoption.lua:OnSelect).</summary>
        void AddOptionSurface(string surface);

        /// <summary>Thêm say surface vào dialog hiện tại (PC dailogsay.lua:CreateNewSayEx).</summary>
        void AddSaySurface(string surface);

        /// <summary>Hỏi player nhập chuỗi (PC g_AskClientStringEx).</summary>
        void AskClientString(string prompt, int minLen, int maxLen);

        /// <summary>Hỏi player nhập số (PC g_AskClientNumberEx).</summary>
        void AskClientNumber(string prompt, int minVal, int maxVal);

        /// <summary>Mở UI give-item cho player chọn vật phẩm (PC g_GiveItemUI).</summary>
        void OpenGiveItemUi(int npcTemplateId, int maxItemCount);

        /// <summary>Đóng dialog hiện tại (PC g_DailogBack).</summary>
        void CloseDialog();

        /// <summary>Log thông báo dialog lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogDialogNotice(string npcName, string message);
    }
}
