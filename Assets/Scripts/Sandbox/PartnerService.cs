// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.4 Partner (Đồng Hành / Thú Cưng) runtime service
// PC source: settings/partner/* — characteristic + level_exp + partner_event.
//   Spawns/despawns pet instances, tracks level/exp/hunger, levels up on exp gain.
//   PcPartnerRegistry gives AI/vision params; we keep a Dictionary<petId, instance>.
// Vietnamese: "Đồng Hành", "Thú Cưng", "Đói", "Lên Cấp".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Trạng thái thú cưng.</summary>
    public enum PetState
    {
        Idle = 0,         // Đang đứng
        Following = 1,    // Đang theo chủ nhân
        Hungry = 2,       // Đang đói
        Starving = 3,     // Sắp chết đói
    }

    /// <summary>Thú cưng runtime instance (Đồng Hành).</summary>
    [Serializable]
    public class PetInstance
    {
        public int petId;          // Instance ID (unique)
        public int templateId;     // PcPartnerEntry.characteristic
        public int level = 1;
        public int exp;
        public int currentHp = 100;
        public int maxHp = 100;
        public int hunger = 100;   // 0 = đói
        public string nameVi = "Thú Cưng";
        public PetState state = PetState.Idle;

        public bool IsStarving => hunger <= 0;
    }

    /// <summary>
    /// Service quản lý thú cưng runtime: spawn/despawn/level-up/hunger decay.
    /// Wraps PcPartnerRegistry (characteristic/AI params) + simple level_exp.
    /// </summary>
    public class PartnerService
    {
        public const int MaxPetSlots = 5;
        public const int MaxLevel = 100;
        public const int HungerDecayIntervalSeconds = 60;
        public const int StarvingThreshold = 0;

        // petId → instance
        private readonly Dictionary<int, PetInstance> _activePets = new();
        private int _nextPetId = 1;

        private readonly PcPartnerRegistry _registry;
        // level → exp needed (level * 100 simple formula)
        private readonly Dictionary<int, int> _levelExpTable = new();

        public event Action<int> HungerDecayed;   // (petId)
        public event Action<int> PetLevelledUp;   // (petId)
        public event Action<int> PetSpawned;      // (petId)
        public event Action<int> PetDespawned;    // (petId)
        public event Action<int> PetStarving;     // (petId)

        public int ActivePetCount => _activePets.Count;
        public IEnumerable<PetInstance> AllActivePets => _activePets.Values;
        public PcPartnerRegistry Registry => _registry;

        public PartnerService(PcPartnerRegistry registry)
        {
            _registry = registry ?? new PcPartnerRegistry();
            BuildDefaultLevelExp();
        }

        private void BuildDefaultLevelExp()
        {
            // PC: level_exp.txt — line 1 = 100, 2 = 500, 3 = 1200, ...
            // Simple exponential: exp(level) = level * 100 (PC top tier is ~level 100).
            for (int lv = 1; lv <= MaxLevel; lv++)
                _levelExpTable[lv] = lv * 100;
        }

        /// <summary>Thú cưng theo ID.</summary>
        public PetInstance GetPet(int petId)
            => _activePets.TryGetValue(petId, out var p) ? p : null;

        /// <summary>Spawn thú cưng mới.</summary>
        public PetInstance SpawnPet(int templateId, int level = 1, string nameVi = null)
        {
            if (_activePets.Count >= MaxPetSlots) return null;
            int id = _nextPetId++;
            var inst = new PetInstance
            {
                petId = id,
                templateId = templateId,
                level = Mathf.Clamp(level, 1, MaxLevel),
                nameVi = string.IsNullOrEmpty(nameVi) ? "Đồng Hành" : nameVi,
            };
            inst.maxHp = 100 + (inst.level - 1) * 20;
            inst.currentHp = inst.maxHp;
            _activePets[id] = inst;
            PetSpawned?.Invoke(id);
            SubsystemLog.Info("Partner", $"Spawn thú cưng: id={id} template={templateId} level={inst.level}");
            return inst;
        }

        /// <summary>Despawn thú cưng.</summary>
        public bool DespawnPet(int petId)
        {
            if (!_activePets.Remove(petId)) return false;
            PetDespawned?.Invoke(petId);
            SubsystemLog.Info("Partner", $"Despawn thú cưng: id={petId}");
            return true;
        }

        /// <summary>Lấy characteristic entry cho template.</summary>
        public PcPartnerEntry GetPartnerCharacteristic(int templateId)
            => _registry?.Get(templateId);

        /// <summary>EXP cần để đạt level kế tiếp (level * 100).</summary>
        public int GetExpForLevel(int level)
            => _levelExpTable.TryGetValue(level, out var e) ? e : level * 100;

        /// <summary>Cộng EXP và tự động lên cấp.</summary>
        public void AwardExp(int petId, int exp)
        {
            if (exp <= 0) return;
            var pet = GetPet(petId);
            if (pet == null || pet.level >= MaxLevel) return;

            pet.exp += exp;
            // Try level up loop
            while (pet.level < MaxLevel && pet.exp >= GetExpForLevel(pet.level))
            {
                pet.exp -= GetExpForLevel(pet.level);
                pet.level++;
                pet.maxHp = 100 + (pet.level - 1) * 20;
                pet.currentHp = pet.maxHp; // heal on level up
                PetLevelledUp?.Invoke(pet.petId);
                SubsystemLog.Info("Partner", $"Pet {pet.petId} lên cấp {pet.level}");
            }
        }

        /// <summary>Giảm độ đói theo lượng amount.</summary>
        public void DecayHunger(int petId, int amount = 5)
        {
            var pet = GetPet(petId);
            if (pet == null) return;
            pet.hunger = Mathf.Max(0, pet.hunger - amount);
            HungerDecayed?.Invoke(petId);
            if (pet.hunger == 0 && pet.state != PetState.Starving)
            {
                pet.state = PetState.Starving;
                PetStarving?.Invoke(petId);
                SubsystemLog.Warn("Partner", $"Pet {petId} đang đói!");
            }
        }

        /// <summary>Cho thú cưng ăn (hồi hunger).</summary>
        public void FeedPet(int petId, int amount = 30)
        {
            var pet = GetPet(petId);
            if (pet == null) return;
            pet.hunger = Mathf.Min(100, pet.hunger + amount);
            if (pet.hunger > 0 && pet.state == PetState.Starving)
                pet.state = PetState.Following;
        }

        /// <summary>Clear tất cả pets (test/reset).</summary>
        public void ClearAll()
        {
            var ids = new List<int>(_activePets.Keys);
            foreach (var id in ids) DespawnPet(id);
        }

        /// <summary>Static factory: load from StreamingAssets.</summary>
        public static PartnerService LoadFromStreamingAssets(string subdir = "Reference/PcPartner")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var reg = PcPartnerParser.BuildRegistry(dir);
            return new PartnerService(reg);
        }
    }
}
