// -----------------------------------------------------------------------------
// VLTK Mobile — Character Panel Service (Thông tin nhân vật)
// UI service: dựng bảng chỉ số nhân vật: sinh lực, nội lực, thể lực, công, thủ…
// PC reference: PlayerProgressionState + EquipmentService + skill/buff bonus.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>Một dòng chỉ số nhân vật.</summary>
    public readonly struct CharacterPanelRow
    {
        public readonly int statId;
        public readonly string statName;
        public readonly int baseValue;
        public readonly int equipBonus;
        public readonly int buffBonus;
        public readonly int totalValue;
        public readonly string description;

        public CharacterPanelRow(int statId, string statName, int baseValue, int equipBonus, int buffBonus, int totalValue, string description)
        {
            this.statId = statId;
            this.statName = statName ?? string.Empty;
            this.baseValue = baseValue;
            this.equipBonus = equipBonus;
            this.buffBonus = buffBonus;
            this.totalValue = totalValue;
            this.description = description ?? string.Empty;
        }
    }

    /// <summary>Snapshot toàn bộ panel nhân vật.</summary>
    public sealed class CharacterPanelSnapshot
    {
        public int playerId;
        public string playerName;
        public int level;
        public int exp;
        public int expMax;
        public int hp;
        public int hpMax;
        public int mp;
        public int mpMax;
        public int stamina;
        public int staminaMax;
        public int attack;
        public int defense;
        public int hit;
        public int dodge;
        public int crit;
        public int block;
        public IReadOnlyList<CharacterPanelRow> rows;
    }

    /// <summary>Dịch vụ UI: panel thông tin nhân vật.</summary>
    public static class CharacterPanelService
    {
        public const string Title = "Nhân Vật";
        public const string LabelHp = "Sinh Lực";
        public const string LabelMp = "Nội Lực";
        public const string LabelStamina = "Thể Lực";
        public const string LabelAttack = "Công Kích";
        public const string LabelDefense = "Phòng Thủ";
        public const string LabelHit = "Chính Xác";
        public const string LabelDodge = "Né Tránh";
        public const string LabelCrit = "Bạo Kích";
        public const string LabelBlock = "Đỡ Đòn";
        public const string LabelPower = "Sức Mạnh";



        public readonly struct PcCharacterControl
        {
            public readonly string pcFile;
            public readonly string pcSection;
            public readonly string labelVi;
            public readonly string actionVi;
            public readonly string spr;

            public PcCharacterControl(string pcFile, string pcSection, string labelVi, string actionVi, string spr)
            {
                this.pcFile = pcFile;
                this.pcSection = pcSection;
                this.labelVi = labelVi;
                this.actionVi = actionVi;
                this.spr = spr;
            }
        }

        public static readonly IReadOnlyList<PcCharacterControl> PcControls = new[]
        {
            new PcCharacterControl("2711122c", "BtnAttribPage", "Thuộc tính", "mở trang chỉ số thuộc tính", @"\Spr\UI3\状态与装备\分页.spr"),
            new PcCharacterControl("2711122c", "BtnEquipPage", "Trang bị", "mở trang trang bị nhân vật", @"\Spr\UI3\状态与装备\分页.spr"),
            new PcCharacterControl("2711122c", "BtnJudgePage", "Đánh giá", "mở trang đánh giá/uy danh", @"\Spr\UI3\状态与装备\分页.spr"),
            new PcCharacterControl("2711122c", "BtnMeridianPage", "Kinh mạch", "mở trang kinh mạch", @"\Spr\UI3\状态与装备\分页.spr"),
            new PcCharacterControl("2711122c", "Item", "Vật phẩm đang mặc", "mở dòng vật phẩm/trang bị đang chọn", @"\spr\Ui3\状态与装备\装备和属性－物品.spr"),
            new PcCharacterControl("2711122c", "Close", "Đóng", "đóng cửa sổ nhân vật", @"\spr\UI3\主界面\关闭_vn.spr"),
            new PcCharacterControl("11da85ea", "AddStrength", "Cộng sức mạnh", "cộng 1 điểm sức mạnh", @"\Spr\Ui3\状态与装备\状态加点按钮改.spr"),
            new PcCharacterControl("11da85ea", "AddVitality", "Cộng sinh khí", "cộng 1 điểm sinh khí", @"\Spr\Ui3\状态与装备\状态加点按钮改.spr"),
            new PcCharacterControl("11da85ea", "AddDexterity", "Cộng thân pháp", "cộng 1 điểm thân pháp", @"\Spr\Ui3\状态与装备\状态加点按钮改.spr"),
            new PcCharacterControl("11da85ea", "AddEnergy", "Cộng nội công", "cộng 1 điểm nội công", @"\Spr\Ui3\状态与装备\状态加点按钮改.spr"),
            new PcCharacterControl("11da85ea", "SummitSign", "Lưu mô tả", "lưu mô tả/phát biểu nhân vật", @"\Spr\UI3\状态与装备\2字按钮.spr"),
            new PcCharacterControl("11da85ea", "DelSignButton", "Xóa mô tả", "xóa dòng mô tả nhân vật", @"\Spr\UI3\状态与装备\描述关闭.spr"),
        };

        public static readonly IReadOnlyDictionary<string, string> PassivePcCharacterControls =
            new Dictionary<string, string>
            {
                ["SignInput"] = "11da85ea [SignInput] is a text input field for character signature; SummitSign/DelSignButton are commands.",
                ["InputBack"] = "11da85ea [InputBack] is the signature input background art.",
                ["AttribListScroll"] = "3d62c429 [AttribListScroll] is scroll track art for opponent/player attribute list.",
                ["AttribListScroll_Btn"] = "3d62c429 [AttribListScroll_Btn] is a drag thumb; mobile ScrollView supplies drag/momentum.",
            };

        // 15 chỉ số chính theo PC: hp/mp/stamina + 12 chỉ số chiến đấu
        public static readonly int[] PcMainStatOrder =
        {
            1, 2, 3,                  // HP / MP / Stamina (max)
            20, 21,                   // Attack / Defense
            22, 23,                   // AttackSpeed / MoveSpeed
            24, 25, 26, 27,           // Hit / Dodge / Crit / CritDamage
            28, 29, 30,               // Block / BlockDamage / Tenacity
        };

        public static IReadOnlyList<int> GetPcStatOrder() => PcMainStatOrder;

        /// <summary>Dựng snapshot nhân vật.</summary>
        public static CharacterPanelSnapshot BuildSnapshot(PlayerProgressionState prog, object equip, int playerId)
        {
            int level = prog != null ? prog.level : 1;
            int hpMax = 100 + level * 50;
            int mpMax = 50 + level * 20;
            int staminaMax = 100 + level * 10;
            int attack = 10 + level * 5;
            int defense = 5 + level * 3;
            var rows = PcMainStatOrder.Select((statId, idx) => new CharacterPanelRow(
                statId: statId,
                statName: StatNameVi(statId),
                baseValue: 0,
                equipBonus: 0,
                buffBonus: 0,
                totalValue: 0,
                description: $"Chỉ số {StatNameVi(statId)}")).ToList();
            return new CharacterPanelSnapshot
            {
                playerId = playerId,
                playerName = prog != null ? prog.faction.ToString() : "Player",
                level = level,
                exp = 0,
                expMax = level * 100,
                hp = hpMax,
                hpMax = hpMax,
                mp = mpMax,
                mpMax = mpMax,
                stamina = staminaMax,
                staminaMax = staminaMax,
                attack = attack,
                defense = defense,
                hit = 50 + level * 2,
                dodge = 20 + level,
                crit = 5 + level / 2,
                block = 5,
                rows = rows,
            };
        }

        /// <summary>Tính sức mạnh tổng hợp từ snapshot.</summary>
        public static int ComputePowerLevel(CharacterPanelSnapshot snapshot)
        {
            if (snapshot == null) return 0;
            return snapshot.attack * 2
                 + snapshot.defense
                 + snapshot.hpMax / 10
                 + snapshot.mpMax / 20
                 + snapshot.hit
                 + snapshot.dodge * 2
                 + snapshot.crit * 3
                 + snapshot.block * 2
                 + snapshot.level * 50;
        }

        /// <summary>Bonus từ trang bị cho stat (stub — cần parse từ EquipmentService).</summary>
        public static int GetBonusFromEquipment(int playerId, int statId)
        {
            if (playerId <= 0 || statId <= 0) return 0;
            return 0;
        }

        private static string StatNameVi(int statId)
        {
            switch (statId)
            {
                case 1: return LabelHp + " Max";
                case 2: return LabelMp + " Max";
                case 3: return LabelStamina + " Max";
                case 20: return LabelAttack;
                case 21: return LabelDefense;
                case 22: return "Tốc đánh";
                case 23: return "Tốc chạy";
                case 24: return LabelHit;
                case 25: return LabelDodge;
                case 26: return LabelCrit;
                case 27: return "Sát thương bạo kích";
                case 28: return LabelBlock;
                case 29: return "Giảm sát thương đỡ";
                case 30: return "Kháng hiệu ứng";
                default: return $"Stat {statId}";
            }
        }
    }
}
