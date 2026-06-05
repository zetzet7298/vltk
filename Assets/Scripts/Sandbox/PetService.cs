// -----------------------------------------------------------------------------
// VLTK Mobile — Pet Service (Pet runtime - distinguishes from PartnerService)
// Quản lý pet chi tiết: đói, thân mật, kỹ năng pet.
// -----------------------------------------------------------------------------

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

        public int Count => _pets.Count;

        public PetService() { }

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
            return true;
        }

        /// <summary>Huấn luyện skill cho pet.</summary>
        public bool TryTrain(int playerId, int skillId)
        {
            if (skillId <= 0) return false;
            if (!_pets.TryGetValue(playerId, out var p)) return false;
            if (p.hunger < HungryThreshold) return false; // pet đói không học được
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
            p.intimacy = System.Math.Min(MaxIntimacy, System.Math.Max(0, p.intimacy + amount));
        }

        public static PetService LoadFromStreamingAssets() => new PetService();
    }
}
