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
            string name = "NPC";
            if (svc != null)
            {
                var npc = svc.GetNpc(npcId);
                if (npc != null) name = npc.nameRaw ?? name;
            }
            int dialogId = startDialogId > 0 ? startDialogId : 1;
            var rows = new List<NpcDialogRow>
            {
                new NpcDialogRow(dialogId, npcId, "Chào mừng hiệp khách.", dialogId + 1, false, 0),
            };
            return new NpcDialogSnapshot
            {
                npcId = npcId,
                npcName = name,
                currentDialogId = dialogId,
                currentText = "Chào mừng hiệp khách.",
                options = System.Array.Empty<NpcDialogOption>(),
                rows = rows,
            };
        }

        /// <summary>Lấy dòng thoại kế tiếp.</summary>
        public static NpcDialogRow? GetNext(int currentDialogId)
        {
            if (currentDialogId <= 0) return null;
            return new NpcDialogRow(
                dialogId: currentDialogId + 1,
                npcId: 0,
                textVi: $"Tiếp tục câu chuyện…",
                nextDialogId: currentDialogId + 2,
                hasOptions: false,
                optionCount: 0);
        }

        /// <summary>Kiểm tra dialog có dẫn tới nhận nhiệm vụ không (heuristic: dialogId % 7 == 0).</summary>
        public static bool HasQuest(int dialogId)
        {
            if (dialogId <= 0) return false;
            return (dialogId % 7) == 0;
        }

        /// <summary>Kiểm tra dialog có mở shop không (heuristic: dialogId % 11 == 0).</summary>
        public static bool HasShop(int dialogId)
        {
            if (dialogId <= 0) return false;
            return (dialogId % 11) == 0;
        }
    }
}
