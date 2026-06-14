// -----------------------------------------------------------------------------
// VLTK Mobile — INpcDialogueHost: giao diện host cho NpcDialogueService.
// Cho phép runtime dispatch các side-effect khi mở / chọn option / đóng
// hội thoại NPC (UI panel, audio, quest dispatch, log, close timer).
// PC source: NPC dialogue flows + Vietnamese localization.
// PC surfaces: OpenDialogue, CloseDialogue, PlayDialogueSFX, Msg2Player.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho NpcDialogueService. Implement bởi UI/Audio/Quest/Chat.
    /// </summary>
    public interface INpcDialogueHost
    {
        /// <summary>Mở panel hội thoại NPC trên UI (PC OpenDialogue / ShowDialoguePanel).</summary>
        void OnDialogueOpened(int npcTemplateId, int playerLevel, string npcTextVi);

        /// <summary>Đóng panel hội thoại NPC (PC CloseDialogue).</summary>
        void OnDialogueClosed(int npcTemplateId);

        /// <summary>Hiển thị danh sách option cho player chọn (PC ShowDialogueOptions).</summary>
        void OnDialogueOptions(int npcTemplateId, int playerLevel, int optionCount, string npcTextVi);

        /// <summary>Phát âm thanh khi NPC nói / option được chọn (PC PlayDialogueSFX).</summary>
        void PlayDialogueSFX(int npcTemplateId, int playerLevel);

        /// <summary>Log thông báo dialogue lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogDialogueEvent(int npcTemplateId, int playerLevel, string message);

        /// <summary>Phát NPC chào / wave khi bắt đầu hội thoại (PC NPCGreeting).</summary>
        void PlayNpcGreeting(int npcTemplateId, int playerLevel);

        /// <summary>Dispatch quest-related option (nhận / trả quest) lên quest service (PC quest_event).</summary>
        void DispatchQuestOption(int npcTemplateId, int playerLevel, int optionIndex, int questId);
    }
}
