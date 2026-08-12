// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Character Info runtime adapter
// Wires PcCharacterPanelState from live runtime services:
//   - HudDataBridge         → HP/MP/stamina/EXP/level/name (snapshot)
//   - GameplayLoopService.LevelService → potential points + DistributePotential
//   - combat actor          → attack/defense/speed/resists (when available)
//   - InventoryService/PlayerEquipmentService → trang-bị zone fill state + isFemale
//   - TitleService          → active title text
//   - MeridianService       → 8 huyệt progression
//
// Null-safe: any missing service collapses to empty fields (the panel renders
// with "—" and disabled backend-missing controls). Constructed by
// GameHudController.OnStatusClick; tests build PcCharacterPanelState directly.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using VLTK.Sandbox;

namespace VLTK.UI.CharacterInfo
{
    /// <summary>
    /// Builds a <see cref="PcCharacterPanelState"/> from the live sandbox. Keep
    /// this adapter in VLTK.UI (it only reads services; it does not mutate game
    /// state except via the documented LevelService.DistributePotential callback).
    /// </summary>
    public static class CharacterInfoRuntimeAdapter
    {
        /// <summary>
        /// Build the panel state. All args are nullable; the returned state is
        /// always non-null and renders an empty (no-data) panel when nothing is
        /// wired, which is the same behavior as the prior null-statsProvider path.
        /// </summary>
        public static PcCharacterPanelState Build(
            HudDataBridge bridge,
            GameplayLoopService loop,
            PlayerEquipmentService equipment,
            InventoryService inventory,
            TitleService titles,
            MeridianService meridians,
            Func<bool> isFemaleProvider,
            Action openInventory)
        {
            var state = new PcCharacterPanelState(() => BuildStats(bridge, loop, equipment, titles))
            {
                IsFemaleProvider = isFemaleProvider,
                EquipmentStateProvider = () => BuildEquipmentState(equipment, inventory),
                DistributePotential = kind => TryDistribute(loop, kind),
                OpenInventory = openInventory,
            };
            return state;
        }

        // ---- stats ----

        private static PcStatsSnapshot BuildStats(
            HudDataBridge bridge,
            GameplayLoopService loop,
            PlayerEquipmentService equipment,
            TitleService titles)
        {
            var snap = bridge != null ? bridge.BuildSnapshot() : default;
            var level = loop != null ? loop.LevelService : null;

            string nameVi = "Vô Danh";
            string titleVi = titles != null && titles.ActivePlayerTitle != null
                ? titles.ActivePlayerTitle.nameRaw
                : string.Empty;

            int strength = level != null ? level.Strength : 0;
            int vitality = level != null ? level.Vitality : 0;
            int dexterity = level != null ? level.Dexterity : 0;
            int inner = level != null ? level.InnerStrength : 0;
            int remain = level != null ? level.PotentialPoints : 0;
            int lvl = level != null ? level.Level : (snap.valid ? snap.level : 1);
            long curExp = level != null ? level.CurrentExp : snap.currentExp;
            long maxExp = level != null
                ? PlayerStatService.GetExpRequired(level.Level)
                : snap.maxExp;

            return new PcStatsSnapshot(
                nameVi, titleVi, lvl, transLife: 0, prestige: 0, luck: 0, worldRank: 0,
                snap.valid ? snap.currentLife : 0,
                snap.valid ? snap.maxLife : 1,
                snap.valid ? snap.currentMana : 0,
                snap.valid ? snap.maxMana : 1,
                snap.valid ? snap.currentStamina : 0,
                snap.valid ? snap.maxStamina : 1,
                curExp, maxExp,
                strength, vitality, dexterity, inner, remain,
                leftDamage: string.Empty, rightDamage: string.Empty,
                attack: 0, defense: 0, moveSpeed: 0, attackSpeed: 0,
                resistPhy: 0, resistCold: 0, resistLightning: 0, resistFire: 0, resistPoison: 0);
        }

        private static bool TryDistribute(GameplayLoopService loop, PcPotentialKind kind)
        {
            if (loop == null) return false;
            int str = kind == PcPotentialKind.Strength ? 1 : 0;
            int dex = kind == PcPotentialKind.Dexterity ? 1 : 0;
            int vit = kind == PcPotentialKind.Vitality ? 1 : 0;
            int inner = kind == PcPotentialKind.InnerEnergy ? 1 : 0;
            return loop.LevelService.DistributePotential(str, dex, vit, inner);
        }

        // ---- equipment ----

        private static Dictionary<EquipSlot, bool> BuildEquipmentState(
            PlayerEquipmentService equipment,
            InventoryService inventory)
        {
            var result = new Dictionary<EquipSlot, bool>();
            if (inventory != null && inventory.Equipped != null)
            {
                foreach (var pair in inventory.Equipped)
                    result[pair.Key] = pair.Value != null;
            }
            // Visual SPR layer (head/body/weapon/mount) — counts as equipped when variant set.
            if (equipment != null)
            {
                if (equipment.IsEquipped(PlayerEquipSlot.Head))   result[EquipSlot.Helmet] = true;
                if (equipment.IsEquipped(PlayerEquipSlot.Body))   result[EquipSlot.Armor]  = true;
                if (equipment.IsEquipped(PlayerEquipSlot.Weapon)) result[EquipSlot.Weapon] = true;
                if (equipment.IsEquipped(PlayerEquipSlot.Mount))  result[EquipSlot.Mount]  = true;
            }
            return result;
        }

        // ---- meridian ----

        // ---- meridian (unused by the combined 2711122c panel; kept for future tabs) ----

        private static PcMeridianSnapshot BuildMeridian(MeridianService meridians, GameplayLoopService loop)
        {
            if (meridians == null)
                return new PcMeridianSnapshot(null, "0/0");
            int playerLevel = loop != null ? loop.LevelService.Level : 1;
            var points = new List<PcMeridianPoint>();
            int i = 0;
            int totalLevels = 0;
            foreach (var id in meridians.GetMeridianIds())
            {
                if (i >= 8) break;
                int tier1 = meridians.GetPlayerAcupointLevel(id, 1);
                totalLevels += tier1;
                var entry = meridians.GetAcupoint(id, 1);
                string nameVi = entry != null ? entry.nameRaw : string.Empty;
                points.Add(new PcMeridianPoint(i, id, nameVi, tier1, 149 + i * 40, 76));
                i++;
            }
            string countText = string.Format(CultureInfo.InvariantCulture,
                "Tổng cộng: {0} huyệt đã đột phá (cấp {1})",
                points.Count, playerLevel);
            return new PcMeridianSnapshot(points, countText);
        }
    }
}
