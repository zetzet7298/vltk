using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>PC Kỳ Trân Các/Bảo Vật manifests from 9e5f75d1, 1463f852, and b54fbe43.</summary>
    public static class TreasureMallPanelService
    {
        public readonly struct PcTreasureMallControl
        {
            public readonly string pcFile;
            public readonly string pcSection;
            public readonly string labelVi;
            public readonly string actionVi;
            public readonly string spr;

            public PcTreasureMallControl(string pcFile, string pcSection, string labelVi, string actionVi, string spr)
            {
                this.pcFile = pcFile;
                this.pcSection = pcSection;
                this.labelVi = labelVi;
                this.actionVi = actionVi;
                this.spr = spr;
            }
        }

        public static readonly IReadOnlyList<PcTreasureMallControl> PcControls = new[]
        {
            new PcTreasureMallControl("9e5f75d1", "PrePaid", "Nạp thẻ", "mở luồng nạp tiền Kỳ Trân Các", @"\spr\Ui3\买卖\新奇珍阁界面\通用长按键.spr"),
            new PcTreasureMallControl("9e5f75d1", "LeftBtn", "Trang trước", "lùi trang danh sách hàng", @"\spr\Ui3\买卖\新奇珍阁界面\通用按键.spr"),
            new PcTreasureMallControl("9e5f75d1", "RightBtn", "Trang sau", "tiến trang danh sách hàng", @"\spr\Ui3\买卖\新奇珍阁界面\通用按键.spr"),
            new PcTreasureMallControl("9e5f75d1", "CloseBtn", "Đóng Kỳ Trân Các", "đóng panel mall", @"\spr\Ui3\买卖\新奇珍阁界面\奇珍阁关闭按键_vn.spr"),
            new PcTreasureMallControl("9e5f75d1", "SellType", "Loại bán", "đổi nhóm hàng đang xem", @"\spr\Ui3\买卖\新奇珍阁界面\通用的四字标签.spr"),
            new PcTreasureMallControl("9e5f75d1", "ShoppingCart", "Giỏ hàng", "mở giỏ hàng", @"\spr\Ui3\买卖\新奇珍阁界面\购物车按钮.spr"),
            new PcTreasureMallControl("9e5f75d1", "MarketGoods_Buy", "Mua vật phẩm", "mua mặt hàng đang chọn", @"\spr\Ui3\买卖\新奇珍阁界面\通用按键.spr"),
            new PcTreasureMallControl("1463f852", "ConfirmBuy", "Xác nhận mua", "xác nhận mua các món trong giỏ", @"\spr\Ui3\买卖\新奇珍阁界面\通用长按键.spr"),
            new PcTreasureMallControl("1463f852", "GoodsInfo_DelItem", "Xóa khỏi giỏ", "xóa dòng hàng khỏi giỏ", @"\spr\Ui3\买卖\新奇珍阁界面\通用按键.spr"),
            new PcTreasureMallControl("1463f852", "GoodsInfo_AddCount", "Tăng số lượng", "tăng số lượng mua", @"\spr\Ui3\买卖\新奇珍阁界面\增加购买数量按键.spr"),
            new PcTreasureMallControl("1463f852", "GoodsInfo_DelCount", "Giảm số lượng", "giảm số lượng mua", @"\spr\Ui3\买卖\新奇珍阁界面\减少购买数量按键.spr"),
            new PcTreasureMallControl("1463f852", "CloseCartBtn", "Đóng giỏ", "đóng cửa sổ giỏ hàng", @"\spr\Ui3\买卖\新奇珍阁界面\购物车关闭按键_vn.spr"),
            new PcTreasureMallControl("b54fbe43", "btn_cathectic1", "Cược 2", "đặt cược rương báu mức 2 Hồn Nguyệt Linh Lộ", @"\spr\Ui3\TreasureChest\投注按钮.spr"),
            new PcTreasureMallControl("b54fbe43", "btn_cathectic2", "Cược 10", "đặt cược rương báu mức 10 Hồn Nguyệt Linh Lộ", @"\spr\Ui3\TreasureChest\投注按钮.spr"),
            new PcTreasureMallControl("b54fbe43", "btn_cathectic3", "Cược 20", "đặt cược rương báu mức 20 Hồn Nguyệt Linh Lộ", @"\spr\Ui3\TreasureChest\投注按钮.spr"),
            new PcTreasureMallControl("b54fbe43", "btn_award", "Nhận thưởng", "nhận thưởng rương báu", @"\spr\Ui3\TreasureChest\领奖按钮.spr"),
            new PcTreasureMallControl("b54fbe43", "btn_vigour", "Nhận linh lộ", "nhận Hồn Nguyệt Linh Lộ", @"\spr\Ui3\TreasureChest\领奖按钮.spr"),
            new PcTreasureMallControl("b54fbe43", "btn_begin", "Bắt đầu quay", "bắt đầu rút thưởng rương báu", @"\spr\Ui3\TreasureChest\开始抽奖按钮.spr"),
            new PcTreasureMallControl("b54fbe43", "btn_close", "Đóng rương", "đóng rương báu", @"\spr\Ui3\TreasureChest\关闭a.spr"),
        };

        public static readonly IReadOnlyDictionary<string, string> PassivePcTreasureMallControls =
            new Dictionary<string, string>
            {
                ["MarketGoods"] = "9e5f75d1 repeated goods row background; MarketGoods_Buy is the command button.",
                ["MarketGoods_DisCount"] = "9e5f75d1 discount badge art; not a command.",
                ["MarketGoods_imgNewArrival"] = "9e5f75d1 new-arrival badge art; not a command.",
                ["Scroll_Btn"] = "1463f852 cart scrollbar drag thumb; mobile ScrollView supplies drag/momentum.",
                ["img_Star* / img_Selected*"] = "b54fbe43 TreasureChest star/selection indicators; display state, not standalone buttons.",
            };

        public static IReadOnlyList<string> BuildRows(MallPanelSnapshot mall, TreasureHuntPanelSnapshot treasure, int page, int quantity, int cartCount, bool cartOpen, int chestBet)
        {
            var rows = new List<string>
            {
                $"PC 9e5f75d1 Kỳ Trân Các: {mall.availableItems}/{mall.totalItems} hàng, ưu đãi {mall.onSaleItems}, trang {page + 1}.",
                $"PC 1463f852 Giỏ hàng: {(cartOpen ? "mở" : "đóng")}, số dòng {cartCount}, số lượng chọn {quantity}.",
                $"PC b54fbe43 Rương báu: cược hiện tại {chestBet} Hồn Nguyệt Linh Lộ.",
                $"Săn kho báu runtime: gần {treasure.nearbyTreasures}/{treasure.totalTreasures} điểm trên map {treasure.currentMapId}.",
            };
            return rows;
        }
    }
}
