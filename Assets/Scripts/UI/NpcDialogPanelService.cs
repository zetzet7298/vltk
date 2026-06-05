// -----------------------------------------------------------------------------
// VLTK Mobile — NPC Dialog Panel Service (Đối thoại NPC)
// UI service: dựng cây thoại NPC, lựa chọn, liên kết nhiệm vụ/cửa hàng.
// PC reference: settings/npc/<npc>/dialog.txt và quest/shop links.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>Tuỳ chọn trong hộp thoại NPC.</summary>
    public readonly struct NpcDialogOption
    {
        public readonly string text;
        public readonly int nextDialogId;

        public NpcDialogOption(string text, int nextDialogId)
        {
            this.text = text ?? string.Empty;
            this.nextDialogId = nextDialogId;
        }
    }

    /// <summary>Một dòng trong panel đối thoại.</summary>
    public readonly struct NpcDialogRow
    {
        public readonly int dialogId;
        public readonly int npcId;
        public readonly string textVi;
        public readonly int nextDialogId;
        public readonly bool hasOptions;
        public readonly int optionCount;

        public NpcDialogRow(int dialogId, int npcId, string textVi, int nextDialogId, bool hasOptions, int optionCount)
        {
            this.dialogId = dialogId;
            this.npcId = npcId;
            this.textVi = textVi ?? string.Empty;
            this.nextDialogId = nextDialogId;
            this.hasOptions = hasOptions;
            this.optionCount = optionCount;
        }
    }

    /// <summary>Snapshot toàn bộ panel đối thoại.</summary>
    public sealed class NpcDialogSnapshot
    {
        public int npcId;
        public string npcName;
        public int currentDialogId;
        public string currentText;
        public IReadOnlyList<NpcDialogOption> options;
        public IReadOnlyList<NpcDialogRow> rows;
    }

    /// <summary>Dịch vụ UI: panel đối thoại NPC.</summary>
    public static class NpcDialogPanelService
    {
        public const string Title = "Đối Thoại";
        public const string LabelContinue = "Tiếp tục";
        public const string LabelClose = "Đóng";
        public const string LabelQuest = "Nhiệm vụ";
        public const string LabelShop = "Cửa hàng";
        public const string LabelAccept = "Đồng ý";
        public const string LabelDecline = "Từ chối";

        /// <summary>Dựng snapshot hộp thoại bắt đầu với NPC.</summary>
        public static NpcDialogSnapshot BuildSnapshot(NpcSpawnService svc, int npcId, int startDialogId = 0)
        {
            return new NpcDialogSnapshot { options = System.Array.Empty<NpcDialogOption>(), rows = System.Array.Empty<NpcDialogRow>() };
        }

        public static NpcDialogRow? GetNext(int currentDialogId)
        {
            return null;
        }

        public static bool HasQuest(int dialogId)
        {
            return false;
        }

        public static bool HasShop(int dialogId)
        {
            return false;
        }

    }
}
