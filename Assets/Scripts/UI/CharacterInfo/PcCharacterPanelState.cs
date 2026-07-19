// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Character Info PC panel state
// Replaces the former direct PlayerStateResponse dependency with a testable
// snapshot/callback contract. The runtime adapter (CharacterInfoRuntimeAdapter)
// builds this from HudDataBridge + GameplayLoopService + combat actor +
// inventory/equipment + TitleService + MeridianService; EditMode constructs it
// directly with known values.
//
// PC source (coordinates are read by CharacterInfoContent from the sub-page
// INIs; only the *data* lives here):
//   - 11da85ea  frame_thuoc_tinh   (thuộc tính sub-page)
//   - 3f5d0331  frame_trang_bi_*   (trang bị sub-page, male/female)
//   - 4cf41f88  frame_danh_gia     (đánh giá sub-page)
//   - df252c4e  kinh_mạch sub-page
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI.CharacterInfo
{
    /// <summary>One potential attribute the player can spend a point on.</summary>
    public enum PcPotentialKind
    {
        Strength,    // Sức mạnh
        Vitality,    // Thể lực
        Dexterity,   // Thân pháp
        InnerEnergy, // Nội lực
    }

    /// <summary>
    /// Snapshot of the Thuộc tính tab. All values are formatted by the content
    /// layer; the snapshot carries raw numbers only.
    /// </summary>
    public readonly struct PcStatsSnapshot
    {
        public readonly string nameVi;
        public readonly string titleVi;
        public readonly int level;
        public readonly int transLife;       // Trùng sinh
        public readonly int prestige;        // Uy danh
        public readonly int luck;            // Cơ duyên
        public readonly int worldRank;       // Bảng thiên hạ
        public readonly int currentLife;
        public readonly int maxLife;
        public readonly int currentMana;
        public readonly int maxMana;
        public readonly int currentStamina;
        public readonly int maxStamina;
        public readonly long currentExp;
        public readonly long maxExp;
        public readonly int strength;
        public readonly int vitality;
        public readonly int dexterity;
        public readonly int innerEnergy;
        public readonly int remainPoint;     // PC RemainPoint — driving the +/- buttons
        public readonly string leftDamage;   // often a range string already formatted
        public readonly string rightDamage;
        public readonly int attack;
        public readonly int defense;
        public readonly int moveSpeed;
        public readonly int attackSpeed;
        public readonly int resistPhy;
        public readonly int resistCold;
        public readonly int resistLightning;
        public readonly int resistFire;
        public readonly int resistPoison;

        public PcStatsSnapshot(
            string nameVi, string titleVi, int level, int transLife, int prestige, int luck, int worldRank,
            int currentLife, int maxLife, int currentMana, int maxMana, int currentStamina, int maxStamina,
            long currentExp, long maxExp,
            int strength, int vitality, int dexterity, int innerEnergy, int remainPoint,
            string leftDamage, string rightDamage,
            int attack, int defense, int moveSpeed, int attackSpeed,
            int resistPhy, int resistCold, int resistLightning, int resistFire, int resistPoison)
        {
            this.nameVi = nameVi ?? string.Empty;
            this.titleVi = titleVi ?? string.Empty;
            this.level = level;
            this.transLife = transLife;
            this.prestige = prestige;
            this.luck = luck;
            this.worldRank = worldRank;
            this.currentLife = currentLife;
            this.maxLife = maxLife;
            this.currentMana = currentMana;
            this.maxMana = maxMana;
            this.currentStamina = currentStamina;
            this.maxStamina = maxStamina;
            this.currentExp = currentExp;
            this.maxExp = maxExp;
            this.strength = strength;
            this.vitality = vitality;
            this.dexterity = dexterity;
            this.innerEnergy = innerEnergy;
            this.remainPoint = remainPoint;
            this.leftDamage = leftDamage ?? string.Empty;
            this.rightDamage = rightDamage ?? string.Empty;
            this.attack = attack;
            this.defense = defense;
            this.moveSpeed = moveSpeed;
            this.attackSpeed = attackSpeed;
            this.resistPhy = resistPhy;
            this.resistCold = resistCold;
            this.resistLightning = resistLightning;
            this.resistFire = resistFire;
            this.resistPoison = resistPoison;
        }
    }

    /// <summary>One equipment hit-zone (sub-page 3f5d0331, 16 zones total).</summary>
    public readonly struct PcEquipZone
    {
        /// <summary>INI section name (Cap/Weapon/Ring1/...). Stable identifier.</summary>
        public readonly string key;
        /// <summary>Gameplay slot bound to inventory/equipment, or null for unbound framework zones.</summary>
        public readonly EquipSlot? gameplaySlot;
        public readonly int left;
        public readonly int top;
        public readonly int width;
        public readonly int height;

        public PcEquipZone(string key, EquipSlot? gameplaySlot, int left, int top, int width, int height)
        {
            this.key = key;
            this.gameplaySlot = gameplaySlot;
            this.left = left;
            this.top = top;
            this.width = width;
            this.height = height;
        }
    }

    /// <summary>One kinh-mạch huyệt (sub-page df252c4e, 8 points).</summary>
    public readonly struct PcMeridianPoint
    {
        public readonly int index;          // 0..7 — PC imgBreathPoint_N
        public readonly int meridianId;     // canonical id (1..8) from MeridianService order
        public readonly string nameVi;      // Đốc Mạch / Nhâm Mạch / ...
        public readonly int level;          // current progression level for this meridian
        public readonly int left;
        public readonly int top;

        public PcMeridianPoint(int index, int meridianId, string nameVi, int level, int left, int top)
        {
            this.index = index;
            this.meridianId = meridianId;
            this.nameVi = nameVi ?? string.Empty;
            this.level = level;
            this.left = left;
            this.top = top;
        }
    }

    /// <summary>Snapshot of the Kinh mạch tab.</summary>
    public readonly struct PcMeridianSnapshot
    {
        public readonly IReadOnlyList<PcMeridianPoint> points;
        /// <summary>Tổng số châm cứu đã thực hiện, formatted text.</summary>
        public readonly string currentCountText;

        public PcMeridianSnapshot(IReadOnlyList<PcMeridianPoint> points, string currentCountText)
        {
            this.points = points ?? Array.Empty<PcMeridianPoint>();
            this.currentCountText = currentCountText ?? string.Empty;
        }
    }

    /// <summary>
    /// Panel state consumed by CharacterInfoContent. Built by the runtime adapter
    /// or directly by tests. The +/- and Item actions are exposed as callbacks so
    /// the content stays UI-only and the runtime owns mutation.
    /// </summary>
    public sealed class PcCharacterPanelState
    {
        /// <summary>Tab keys in PC sheet order (left to right at the top of the sheet).</summary>
        public const string TabThuocTinh = "thuoctinh";
        public const string TabTrangBi = "trangbi";
        public const string TabDanhGia = "danhgia";
        public const string TabKinhMach = "kinhmach";

        public PcCharacterPanelState(Func<PcStatsSnapshot> statsProvider)
        {
            StatsProvider = statsProvider;
        }

        // ---- Data providers (null-safe; content renders empty/disabled when null) ----
        public Func<PcStatsSnapshot> StatsProvider { get; }
        public Func<bool> IsFemaleProvider { get; set; }
        public Func<IReadOnlyDictionary<EquipSlot, bool>> EquipmentStateProvider { get; set; }
        public Func<PcMeridianSnapshot> MeridianProvider { get; set; }

        // ---- Callbacks (content invokes; runtime mutates) ----
        /// <summary>Spend one point on the given potential. Returns true on success.</summary>
        public Func<PcPotentialKind, bool> DistributePotential { get; set; }
        /// <summary>Open the inventory popup (Hành trang) from the trang-bị tab Item button.</summary>
        public Action OpenInventory { get; set; }

        /// <summary>Read the stats snapshot; null-safe.</summary>
        public PcStatsSnapshot ReadStats() => StatsProvider?.Invoke() ?? default;
    }
}
