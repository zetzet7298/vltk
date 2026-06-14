// -----------------------------------------------------------------------------
// VLTK Mobile — Pet Service (Pet runtime - distinguishes from PartnerService)
// Quản lý pet chi tiết: đói, thân mật, kỹ năng pet.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Thông tin chi tiết pet.
    /// </summary>
    public class PetDetailEntry
    {
        public int petId;
        public int playerId;
        public string name;
        public string spritePath;
        public int level;
        public int exp;
        public int hunger;        // 0-100, 0 = đói
        public int intimacy;      // 0-1000
        public string skills;     // danh sách id skill cách nhau dấu ","
    }

    /// <summary>
    /// Service quản lý pet (nuôi, cho ăn, huấn luyện).
    /// </summary>
    public class PetService
    {
        public const string LogTag = "Pet";
        public const int MaxHunger = 100;
        public const int MaxIntimacy = 1000;
        public const int HungryThreshold = 30;

        private readonly Dictionary<int, PetDetailEntry> _pets = new();
        private IPetHost _host;

        public int Count => _pets.Count;

        public event Action<int, int> OnPetCreated; // (playerId, petId)
        public event Action<int> OnPetFed;
        public event Action<int, int> OnPetTrained; // (playerId, skillId)

        public PetService() : this(null) { }
        public PetService(IPetHost host) { _host = host; }

        public void AttachHost(IPetHost host) { _host = host; }

        /// <summary>Khởi tạo pet cho player (helper cho tests).</summary>
        public PetDetailEntry CreatePet(int playerId, int petId, string name, string spritePath)
        {
            var p = new PetDetailEntry
            {
                petId = petId,
                playerId = playerId,
                name = name ?? string.Empty,
                spritePath = spritePath ?? string.Empty,
                level = 1,
                exp = 0,
                hunger = MaxHunger,
                intimacy = 0,
                skills = string.Empty,
            };
            _pets[playerId] = p;
            OnPetCreated?.Invoke(playerId, petId);
            if (_host != null)
            {
                _host.OnPetCreated(playerId, petId, p.name, p.hunger, p.intimacy);
                _host.PlayPetSFX(playerId, petId, "spawn");
                _host.LogPetEvent(playerId, petId, $"Tạo pet {p.name} cho player {playerId}");
                _host.SavePetState(playerId, petId, p.level, p.exp, p.hunger, p.intimacy);
            }
            return p;
        }

        /// <summary>Lấy pet của player.</summary>
        public PetDetailEntry GetPet(int playerId)
        {
            return _pets.TryGetValue(playerId, out var p) ? p : null;
        }

        /// <summary>Cho pet ăn (foodId ảnh hưởng lượng no).</summary>
        public bool TryFeed(int playerId, int foodId)
        {
            if (foodId <= 0) return false;
            if (!_pets.TryGetValue(playerId, out var p)) return false;
            p.hunger = System.Math.Min(MaxHunger, p.hunger + 25);
            OnPetFed?.Invoke(playerId);
            if (_host != null)
            {
                _host.OnPetFed(playerId, p.petId, foodId, p.hunger);
                _host.PlayPetSFX(playerId, p.petId, "feed");
                _host.LogPetEvent(playerId, p.petId, $"Cho pet ăn food {foodId}, hunger={p.hunger}");
                _host.SavePetState(playerId, p.petId, p.level, p.exp, p.hunger, p.intimacy);
            }
            return true;
        }

        /// <summary>Huấn luyện skill cho pet.</summary>
        public bool TryTrain(int playerId, int skillId)
        {
            if (skillId <= 0) return false;
            if (!_pets.TryGetValue(playerId, out var p)) return false;
            if (p.hunger < HungryThreshold)
            {
                _host?.OnPetHungry(playerId, p.petId, p.hunger, HungryThreshold);
                _host?.LogPetEvent(playerId, p.petId, $"Pet đói (hunger={p.hunger}), không huấn luyện được");
                return false; // pet đói không học được
            }
            var list = new List<string>();
            if (!string.IsNullOrEmpty(p.skills)) list.AddRange(p.skills.Split(','));
            string sid = skillId.ToString();
            if (!list.Contains(sid)) list.Add(sid);
            p.skills = string.Join(",", list);
            p.exp += 10;
            if (p.exp >= p.level * 100)
            {
                p.exp = 0;
                p.level++;
            }
            OnPetTrained?.Invoke(playerId, skillId);
            if (_host != null)
            {
                _host.OnPetTrained(playerId, p.petId, skillId, p.level, p.exp);
                _host.PlayPetSFX(playerId, p.petId, "train");
                _host.LogPetEvent(playerId, p.petId, $"Huấn luyện skill {skillId} cho pet, level={p.level}");
                _host.SavePetState(playerId, p.petId, p.level, p.exp, p.hunger, p.intimacy);
            }
            return true;
        }

        public int GetHunger(int playerId)
            => _pets.TryGetValue(playerId, out var p) ? p.hunger : 0;

        public int GetIntimacy(int playerId)
            => _pets.TryGetValue(playerId, out var p) ? p.intimacy : 0;

        public bool IsHungry(int playerId)
        {
            if (!_pets.TryGetValue(playerId, out var p)) return false;
            return p.hunger < HungryThreshold;
        }

        /// <summary>Tăng thân mật khi tương tác.</summary>
        public void AddIntimacy(int playerId, int amount)
        {
            if (!_pets.TryGetValue(playerId, out var p)) return;
            int newIntimacy = System.Math.Min(MaxIntimacy, System.Math.Max(0, p.intimacy + amount));
            p.intimacy = newIntimacy;
            _host?.OnPetIntimacyChanged(playerId, p.petId, newIntimacy);
            _host?.SavePetState(playerId, p.petId, p.level, p.exp, p.hunger, p.intimacy);
        }

        public static PetService LoadFromStreamingAssets() => new PetService();
    }
}
