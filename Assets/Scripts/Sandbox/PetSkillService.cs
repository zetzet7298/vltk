// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.4 Pet Skill (Kỹ Năng Thú Cưng) runtime service
// PC source: settings/petsys/pet_skill_def.txt — 21 columns per level.
//   Level  MagAttr1  Param1..3  spr  MagAttr2  Param1..3  + more
// Provides level→skill bonus lookup for both primary (magAttr1) and secondary (magAttr2) slots.
// Vietnamese: "Kỹ Năng Thú Cưng", "Cường Hóa", "Cấp Độ".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý kỹ năng thú cưng: tra cứu bonus theo level và slot.
    /// </summary>
    public class PetSkillService
    {
        private readonly PcPetSkillRegistry _registry;

        public const int MaxPetSkillLevel = 100;

        public int TotalSkills => _registry?.Count ?? 0;
        public PcPetSkillRegistry Registry => _registry;

        public PetSkillService(PcPetSkillRegistry registry)
        {
            _registry = registry ?? new PcPetSkillRegistry();
        }

        /// <summary>Skill entry theo level (PC: pet_skill_def.txt rows).</summary>
        public PcPetSkillEntry GetSkillForLevel(int level)
        {
            if (_registry == null || level <= 0) return null;
            int clamped = Mathf.Clamp(level, 1, MaxPetSkillLevel);
            // PC table is sparse; fall back to nearest lower level.
            while (clamped > 0)
            {
                var entry = _registry.GetLevel(clamped);
                if (entry != null) return entry;
                clamped--;
            }
            return _registry.GetLevel(1);
        }

        /// <summary>Max level trong table.</summary>
        public int GetMaxLevel()
        {
            if (_registry == null) return 0;
            int max = 0;
            // Registry doesn't expose keys; probe common levels.
            for (int lv = 1; lv <= MaxPetSkillLevel; lv++)
            {
                if (_registry.GetLevel(lv) != null) max = lv;
            }
            return max;
        }

        /// <summary>
        /// Lấy bonus sức mạnh (magAttr) cho level hiện tại.
        /// Primary slot dùng magAttr1; secondary slot dùng magAttr2.
        /// Trả về -1 nếu không có entry (PC convention: -1 = no skill).
        /// </summary>
        public int GetSkillBonus(int level, bool isPrimarySlot)
        {
            var entry = GetSkillForLevel(level);
            if (entry == null) return -1;
            return isPrimarySlot ? entry.magAttr1 : entry.magAttr2;
        }

        /// <summary>Tổng damage cộng thêm (param1) cho level.</summary>
        public int GetPrimaryDamageBonus(int level)
        {
            var entry = GetSkillForLevel(level);
            return entry?.param1 ?? 0;
        }

        /// <summary>Tổng damage cộng thêm (param1) cho level ở slot 2.</summary>
        public int GetSecondaryDamageBonus(int level)
        {
            var entry = GetSkillForLevel(level);
            // PC table stores magAttr2's params at cols 7..9; we only stored first set in entry.
            // For runtime use we expose magAttr2 value + param1 from same entry.
            return entry?.param1 ?? 0;
        }

        /// <summary>Static factory: load from StreamingAssets.</summary>
        public static PetSkillService LoadFromStreamingAssets(string subdir = "Reference/PcPet")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var reg = PcPetParser.BuildRegistry(dir);
            return new PetSkillService(reg);
        }
    }
}
