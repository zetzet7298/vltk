using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>PC 1024 player trade window manifest from d84aceb8.dat.</summary>
    public static class ExchangePanelService
    {
        public readonly struct PcExchangeControl
        {
            public readonly string pcSection;
            public readonly string labelVi;
            public readonly string actionVi;
            public readonly string spr;

            public PcExchangeControl(string pcSection, string labelVi, string actionVi, string spr)
            {
                this.pcSection = pcSection;
                this.labelVi = labelVi;
                this.actionVi = actionVi;
                this.spr = spr;
            }
        }

        public static readonly IReadOnlyList<PcExchangeControl> PcControls = new[]
        {
            new PcExchangeControl("OkBtn", "Khóa giao dịch", "khóa vật phẩm/bạc đã đặt ở lượt xác nhận đầu", @"\Spr\Ui3\交易\玩家交易－锁定交易.spr"),
            new PcExchangeControl("TradeBtn", "Xác nhận cuối", "hoàn tất giao dịch khi hai bên đã khóa", @"\Spr\Ui3\交易\玩家交易－最终确认.spr"),
            new PcExchangeControl("CancelBtn", "Hủy giao dịch", "hủy phiên và đóng cửa sổ giao dịch", @"\Spr\Ui3\交易\玩家交易－取消交易.spr"),
            new PcExchangeControl("AddMoney", "Tăng bạc", "tăng lượng bạc bản thân đặt vào giao dịch", @"\Spr\Ui3\交易\玩家交易－加钱.spr"),
            new PcExchangeControl("ReduceMoney", "Giảm bạc", "giảm lượng bạc bản thân đặt vào giao dịch", @"\Spr\Ui3\交易\玩家交易－减钱.spr"),
        };

        public static readonly IReadOnlyDictionary<string, string> PassivePcExchangeFields =
            new Dictionary<string, string>
            {
                ["SelfMoney"] = "d84aceb8 [SelfMoney] is a 138x14 value field for local offered silver, driven by AddMoney/ReduceMoney.",
                ["OtherMoney"] = "d84aceb8 [OtherMoney] is a 138x14 value field for the other player's offered silver.",
                ["TakewithMoney"] = "d84aceb8 [TakewithMoney] is a 138x14 value field near the money adjust buttons, not a separate command.",
            };

        public static IReadOnlyList<string> BuildRows(TradeSession session, PartyMember target, EconomyService economy)
        {
            var rows = new List<string>
            {
                "PC d84aceb8 [Main] giao dịch: OkBtn/TradeBtn/CancelBtn/AddMoney/ReduceMoney.",
                target == null ? "Đối tượng: chưa chọn người chơi." : $"Đối tượng: {target.nameVi} Lv{target.level} [{PartyService.FactionNameVi(target.factionId)}]",
                economy == null ? "Ví bạc: --" : $"Ví bạc: {economy.Wallet.silver}",
            };

            if (session == null)
            {
                rows.Add("Phiên: chưa tạo — PC cần chọn người chơi trước khi giao dịch.");
                return rows;
            }

            rows.Add($"Phiên: #{session.initiatorId}->{session.targetId}");
            rows.Add($"Bạc bản thân đặt: {session.initiatorSilver}; đối phương đặt: {session.targetSilver}");
            rows.Add($"Khóa: bản thân={(session.initiatorLocked ? "đã khóa" : "chưa")}, đối phương={(session.targetLocked ? "đã khóa" : "chưa")}");
            rows.Add(session.IsReady ? "Trạng thái: đủ điều kiện xác nhận cuối." : "Trạng thái: đang chờ hai bên khóa giao dịch.");
            return rows;
        }
    }
}
