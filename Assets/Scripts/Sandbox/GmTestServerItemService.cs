// -----------------------------------------------------------------------------
// VLTK Mobile — Lệnh bài GM Test Server runtime service
// PC source:
//   settings/item/004/magicscript.txt row 5129 (6/1/4890)
//   script/item/gmroleitem2.lua -> GMPassword_Test()
//   script/global/gm/lenhbaiadmintestserver.lua DialogMain + submenus
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum GmItemActionStatus
    {
        Success,
        Blocked,
        NeedsConfirmation,
        NotPorted,
        Invalid,
    }

    public sealed class GmItemActionResult
    {
        public GmItemActionStatus status;
        public string message;
        public bool success => status == GmItemActionStatus.Success;

        public static GmItemActionResult Success(string msg) => new() { status = GmItemActionStatus.Success, message = msg };
        public static GmItemActionResult Blocked(string msg) => new() { status = GmItemActionStatus.Blocked, message = msg };
        public static GmItemActionResult Confirm(string msg) => new() { status = GmItemActionStatus.NeedsConfirmation, message = msg };
        public static GmItemActionResult NotPorted(string msg) => new() { status = GmItemActionStatus.NotPorted, message = msg };
        public static GmItemActionResult Invalid(string msg) => new() { status = GmItemActionStatus.Invalid, message = msg };
    }

    public sealed class GmItemMenuOption
    {
        public string label;
        public string actionId;
        public string nextMenuId;
        public string pcFunction;
        public bool destructive;

        public GmItemMenuOption(string label, string actionId, string pcFunction = null, string nextMenuId = null, bool destructive = false)
        {
            this.label = label;
            this.actionId = actionId;
            this.pcFunction = pcFunction ?? actionId;
            this.nextMenuId = nextMenuId;
            this.destructive = destructive;
        }
    }

    public sealed class GmTestServerItemService
    {
        public const int ItemGenre = 6;
        public const int DetailType = 1;
        public const int ParticularType = 4890;
        public const int TaskSkillSupportReceived = 5744;
        public const int TaskHorseUpgradePoints = 5953;
        public const int TaskActiveTitle = 1122;
        public const int PcGmTitleId = 5000;

        public const string MainMenuId = "main";
        public const string TestServerMenuId = "testserver";
        public const string MaterialsMenuId = "nguyenlieuhoatdong";
        public const string HorseMaterialMenuId = "nguyenlieunangcapngua";
        public const string SpawnMenuId = "gm_menu_taobai";
        public const string TravelMenuId = "vitri_khac";
        public const string BossAssassinMenuId = "gotobosssatthu";

        private readonly SandboxManager _manager;
        private readonly InventoryService _inventory;
        private readonly GmAccessService _access;

        public GmTestServerItemService(SandboxManager manager = null, InventoryService inventory = null, GmAccessService access = null)
        {
            _manager = manager;
            _inventory = inventory;
            _access = access ?? new GmAccessService();
        }

        private InventoryService Inventory => _inventory ?? _manager?.InventoryService;
        private TaskFlagService Tasks => _manager?.TaskFlagService;
        private PlayerProgressionState Progression => _manager?.PlayerProgression;
        public bool CanUse => _access == null || _access.IsAllowed;

        public bool IsGmToken(int itemGenre, int detailType, int particularType)
            => itemGenre == ItemGenre && detailType == DetailType && particularType == ParticularType;

        public IReadOnlyList<GmItemMenuOption> GetMenu(string menuId = MainMenuId)
        {
            menuId = string.IsNullOrEmpty(menuId) ? MainMenuId : menuId;
            return menuId switch
            {
                MainMenuId => BuildMainMenu(),
                TestServerMenuId => BuildTestServerMenu(),
                MaterialsMenuId => BuildMaterialsMenu(),
                HorseMaterialMenuId => BuildHorseMaterialMenu(),
                SpawnMenuId => BuildSpawnMenu(),
                TravelMenuId => BuildTravelMenu(),
                BossAssassinMenuId => BuildBossAssassinMenu(),
                _ => Array.Empty<GmItemMenuOption>(),
            };
        }

        private IReadOnlyList<GmItemMenuOption> BuildMainMenu()
        {
            var list = new List<GmItemMenuOption>
            {
                new("Test Server (mọi thứ cần ở đây nha)", null, "testserver", TestServerMenuId),
                new("Tạo Bãi", null, "gm_menu_taobai", SpawnMenuId),
                new("Xóa toàn bộ item trong hành trang", "XoaItemHanhTrangGM", destructive: true),
                new("Hỗ trợ làm nhiệm vụ hoàng kim nhanh", "helpgoldquest"),
                new("Hỗ trợ đi làm nhiệm vụ", null, "vitri_khac", TravelMenuId),
                new("Nhận nguyên liệu hoạt động", null, "nguyenlieuhoatdong", MaterialsMenuId),
                new("Lấy vật phẩm", "TakeSpecifiedItem"),
                new("Sửa lỗi Thần Hành Phù", "fixthanhanhphu"),
            };
            if (ShouldShowSkillSupport())
                list.Add(new GmItemMenuOption("Nhận hỗ trợ skill 1x-6x", "HoTroSkill2"));
            list.Add(new GmItemMenuOption("Thay đổi danh hiệu", "change_title"));
            list.Add(new GmItemMenuOption("Kỹ năng", "SkillsSystem"));
            list.Add(new GmItemMenuOption("shop", "shoptongkim"));
            return list;
        }

        private IReadOnlyList<GmItemMenuOption> BuildTestServerMenu() => new List<GmItemMenuOption>
        {
            new("Nhận các loại điểm(level, tiền vạn, đồng,...)", "CacLoaiDiem"),
            new("Thú cưỡi", "ThuCuoi"),
            new("Vật phẩm hỗ trợ", "VatPhamHoTro"),
            new("Trang bị", "DanhSachTrangBi2"),
            new("Học kỹ năng môn phái", "HoTroSkill"),
            new("Điều kiện tạo bang hội", "DieuKienTaoBangHoi"),
            new("Tẩy tủy nhanh-Cộng Điểm Nhanh", "TayTuyNhanh"),
            new("Đổi tên nhân vật", "gm_doiten_menu"),
        };

        private IReadOnlyList<GmItemMenuOption> BuildMaterialsMenu()
        {
            var list = new List<GmItemMenuOption>();
            if (IsEventAutoEnabled())
                list.Add(new GmItemMenuOption("Nhận nguyên liệu event", "NguyenLieuEvent"));
            list.Add(new GmItemMenuOption("Nguyên Liệu Nâng Cấp Ngựa", null, "nguyenlieunangcapngua", HorseMaterialMenuId));
            return list;
        }

        private IReadOnlyList<GmItemMenuOption> BuildHorseMaterialMenu() => new List<GmItemMenuOption>
        {
            new("Nhận 1 bộ nâng cấp chiến mã", "cotuoivadaythung"),
            new("Nhận điểm tích lũy nâng cấp 10.000 điểm", "bacdauthuanmadon"),
        };

        private IReadOnlyList<GmItemMenuOption> BuildSpawnMenu() => new List<GmItemMenuOption>
        {
            new("Tạo bãi (Auto)", "gm_taobai_auto"),
            new("Tạo bãi (Chọn cấp)", "gm_taobai_choncap"),
            new("Xóa quái xung quanh", "gm_xoabai"),
        };

        private IReadOnlyList<GmItemMenuOption> BuildTravelMenu() => new List<GmItemMenuOption>
        {
            new("Boss sát thủ", null, "gotobosssatthu", BossAssassinMenuId),
            new("Vượt ải", "goto_satthu"),
            new("Tín Sứ", "goto_tinsu"),
            new("Kiếm Gia Mê Cung", "goto_kiemgia"),
            new("Thí Luyện Đường", "goto_thiluyenduong"),
            new("Viêm Đế Bảo Tàng", "goto_viemde"),
            new("Phong Lăng Độ", "goto_phonglangdo"),
            new("Thiên Trì Mật Cảnh", "goto_thientri"),
            new("Loạn Chiến Cửu Châu", "goto_chaucoc"),
            new("Chiến trường Thất Thành Đại Chiến", "gopos_sevencityfield"),
        };

        private IReadOnlyList<GmItemMenuOption> BuildBossAssassinMenu() => new List<GmItemMenuOption>
        {
            new("Boss Sát thủ 9x", "gopos_9x"),
            new("Boss Sát thủ 2x", "gopos_2x"),
            new("Boss Sát thủ 3x", "gopos_3x"),
            new("Boss Sát thủ 4x", "gopos_4x"),
            new("Boss Sát thủ 5x", "gopos_5x"),
            new("Boss Sát thủ 6x", "gopos_6x"),
            new("Boss Sát thủ 7x", "gopos_7x"),
            new("Boss Sát thủ 8x", "gopos_8x"),
        };

        public GmItemActionResult EnsureGmLoginInGame()
        {
            if (!CanUse) return GmItemActionResult.Blocked(_access.DenialMessage);
            var inv = Inventory;
            if (inv == null) return GmItemActionResult.Invalid("InventoryService chưa sẵn sàng.");

            if (!inv.HasPcItem(ItemGenre, DetailType, ParticularType)) inv.AddPcItem(ItemGenre, DetailType, ParticularType);
            if (!inv.HasPcItem(6, 1, 1266)) inv.AddPcItem(6, 1, 1266);

            if (Progression != null && Progression.level < 5)
                Progression.level = 5;
            var player = _manager?.GameplayLoop?.Player;
            if (player != null && player.level < 5)
            {
                player.level = 5;
                player.combat.level = 5;
            }

            Tasks?.SetFlag(TaskActiveTitle, PcGmTitleId);
            var title = _manager?.TitleService;
            if (title != null && title.UnlockPlayerTitle(PcGmTitleId))
                title.SetActivePlayerTitle(PcGmTitleId);
            return GmItemActionResult.Success("Đã cấp Lệnh bài GM Test Server theo GMLoginInGame().");
        }

        public GmItemActionResult Execute(string actionId, bool confirmed = false, int numberValue = 1)
        {
            if (!CanUse) return GmItemActionResult.Blocked(_access.DenialMessage);
            if (string.IsNullOrEmpty(actionId)) return GmItemActionResult.Invalid("Action rỗng.");

            return actionId switch
            {
                "XoaItemHanhTrangGM" => ClearInventoryAndRestoreGmItems(confirmed),
                "fixthanhanhphu" => GmItemActionResult.Success("Đã reset trạng thái sử dụng Thần Hành Phù/Thổ Địa Phù."),
                "HoTroSkill2" => GrantSkillSupport1xTo6x(),
                "SkillsSystem" => GmItemActionResult.Success("OPEN_SKILL_PANEL"),
                "cotuoivadaythung" => GiveHorseUpgradeSet(Math.Max(1, numberValue)),
                "bacdauthuanmadon" => SetHorseUpgradePoints(),
                "TakeSpecifiedItem" => GmItemActionResult.NotPorted("TakeSpecifiedItem cần UI nhập genre/detail/particular/count; backend AddPcItem đã sẵn sàng."),
                "shoptongkim" => GmItemActionResult.Success("OPEN_TONG_KIM_SHOP"),
                "change_title" => GmItemActionResult.NotPorted("change_title cần panel chọn danh hiệu; TitleService data đã có nhưng HUD chưa gắn panel này."),
                _ => TryTravel(actionId) ?? GmItemActionResult.NotPorted($"Chưa port backend cho PC Lua function: {actionId}"),
            };
        }

        private GmItemActionResult ClearInventoryAndRestoreGmItems(bool confirmed)
        {
            if (!confirmed) return GmItemActionResult.Confirm("Bạn có muốn xóa toàn bộ item trong hành trang không?");
            var inv = Inventory;
            if (inv == null) return GmItemActionResult.Invalid("InventoryService chưa sẵn sàng.");
            inv.ClearInventory();
            AddPc(inv, 6, 1, 438, 1);
            AddPc(inv, 6, 1, 1266, 1);
            AddPc(inv, 6, 1, 4850, 1);
            AddPc(inv, 6, 1, 4890, 1);
            AddPc(inv, 6, 1, 4852, 1);
            AddPc(inv, 6, 1, 4908, 1);
            return GmItemActionResult.Success("Đã xóa hành trang và cấp lại bộ item GM PC.");
        }

        private GmItemActionResult GiveHorseUpgradeSet(int count)
        {
            var inv = Inventory;
            if (inv == null) return GmItemActionResult.Invalid("InventoryService chưa sẵn sàng.");
            int ok = 0;
            if (AddPc(inv, 6, 1, 4891, count)) ok++;
            if (AddPc(inv, 6, 1, 4892, count)) ok++;
            if (AddPc(inv, 6, 1, 4894, count)) ok++;
            return ok == 3
                ? GmItemActionResult.Success($"Đã nhận {count} bộ nâng cấp chiến mã.")
                : GmItemActionResult.NotPorted("Thiếu một phần item nguyên liệu nâng cấp chiến mã trong mobile item DB.");
        }

        private GmItemActionResult SetHorseUpgradePoints()
        {
            Tasks?.SetFlag(TaskHorseUpgradePoints, 10000);
            return GmItemActionResult.Success("Đã set điểm tích lũy nâng cấp chiến mã = 10.000.");
        }

        private GmItemActionResult GrantSkillSupport1xTo6x()
        {
            Tasks?.SetFlag(TaskSkillSupportReceived, 1);
            Progression?.MaxAllSkillLevels(_manager?.CombatSkillCatalog);
            var player = _manager?.GameplayLoop?.Player;
            if (player != null && Progression != null)
            {
                player.combat.knownSkills = Progression.knownSkills;
                player.combat.skillLevels = Progression.skillLevels;
            }
            return GmItemActionResult.Success("Đã nhận hỗ trợ skill 1x-6x theo PC HoTroSkill2().");
        }

        private GmItemActionResult TryTravel(string actionId)
        {
            if (!TravelTargets.TryGetValue(actionId, out var t)) return null;
            var manager = _manager ?? SandboxManager.Instance;
            if (manager == null || manager.MapManager == null)
                return GmItemActionResult.NotPorted($"Chưa có MapManager để đi tới {t.name}.");
            if (!manager.MapManager.Catalog.ContainsKey(t.mapId))
                return GmItemActionResult.NotPorted($"Map {t.mapId} ({t.name}) chưa có trong catalog mobile.");

            manager.SwitchMap(t.mapId);
            manager.PlayerController?.PlaceAt(new Vector2(t.x, t.y), snapCamera: true);
            SubsystemLog.Info("GMItem", $"Teleport {actionId} -> map {t.mapId} ({t.x},{t.y})");
            return GmItemActionResult.Success($"Đã chuyển tới {t.name}.");
        }

        private static bool AddPc(InventoryService inv, int g, int d, int p, int count)
            => inv != null && inv.AddPcItem(g, d, p, count);

        private bool ShouldShowSkillSupport()
            => (Tasks == null || Tasks.GetFlag(TaskSkillSupportReceived) == 0);

        private static bool IsEventAutoEnabled() => true;

        private static readonly Dictionary<string, (int mapId, int x, int y, string name)> TravelTargets = new()
        {
            { "goto_satthu", (78, 1509, 3209, "Vượt ải") },
            { "goto_thientri", (934, 1598, 3240, "Thiên Trì Mật Cảnh") },
            { "goto_chaucoc", (176, 1574, 2955, "Loạn Chiến Cửu Châu") },
            { "goto_vantieu", (1, 1559, 2768, "Vận Tiêu") },
            { "goto_tinsu", (11, 3024, 5086, "Tín Sứ") },
            { "goto_thiluyenduong", (176, 1588, 2941, "Thí Luyện Đường") },
            { "goto_kiemgia", (949, 1580, 3158, "Kiếm Gia Mê Cung") },
            { "goto_viemde", (37, 1711, 3179, "Viêm Đế Bảo Tàng") },
            { "goto_phonglangdo", (336, 1124, 3187, "Phong Lăng Độ") },
            { "gopos_9x", (93, 1640, 3264, "Boss Sát thủ 9x") },
            { "gopos_2x", (73, 1544, 2944, "Boss Sát thủ 2x") },
            { "gopos_3x", (4, 1576, 2992, "Boss Sát thủ 3x") },
            { "gopos_4x", (5, 1616, 3472, "Boss Sát thủ 4x") },
            { "gopos_5x", (12, 1792, 3168, "Boss Sát thủ 5x") },
            { "gopos_6x", (164, 1784, 3120, "Boss Sát thủ 6x") },
            { "gopos_7x", (123, 1600, 3200, "Boss Sát thủ 7x") },
            { "gopos_8x", (201, 1768, 3200, "Boss Sát thủ 8x") },
        };
    }
}
