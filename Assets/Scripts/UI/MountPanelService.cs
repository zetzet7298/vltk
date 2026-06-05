// -----------------------------------------------------------------------------
// VLTK Mobile — Mount Panel Service (Cưỡi Ngựa)
// Dựng snapshot cho UI cưỡi ngựa. Kết hợp MountService + HorseService.
// Vietnamese: "Cưỡi Ngựa", "Lên ngựa", "Xuống ngựa", "Cho ăn", "Tốc độ", "Thể lực".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct MountPanelRow
    {
        public readonly int mountId;
        public readonly string name;
        public readonly string spritePath;
        public readonly int speed;
        public readonly int stamina;
        public readonly int staminaMax;
        public readonly bool isMounted;
        public readonly bool isOwned;
        public readonly bool canMount;
        public readonly int requiredLevel;

        public MountPanelRow(int mountId, string name, string spritePath, int speed, int stamina, int staminaMax, bool isMounted, bool isOwned, bool canMount, int requiredLevel)
        {
            this.mountId = mountId;
            this.name = name;
            this.spritePath = spritePath;
            this.speed = speed;
            this.stamina = stamina;
            this.staminaMax = staminaMax;
            this.isMounted = isMounted;
            this.isOwned = isOwned;
            this.canMount = canMount;
            this.requiredLevel = requiredLevel;
        }
    }

    public sealed class MountPanelSnapshot
    {
        public int playerId;
        public int activeMountId;
        public int currentSpeed;
        public int currentStamina;
        public IReadOnlyList<MountPanelRow> mounts;
        public IReadOnlyList<MountPanelRow> availableMounts;
    }

    public static class MountPanelService
    {
        public const string LabelMount = "Cưỡi Ngựa";
        public const string LabelMountUp = "Lên ngựa";
        public const string LabelMountDown = "Xuống ngựa";
        public const string LabelFeed = "Cho ăn";
        public const string LabelSpeed = "Tốc độ";
        public const string LabelStamina = "Thể lực";
        public const string LabelOwned = "Đã sở hữu";

        public static MountPanelSnapshot BuildSnapshot(MountService svc, HorseService horse, int playerId)
        {
            var snap = new MountPanelSnapshot
            {
                playerId = playerId,
                activeMountId = svc != null ? svc.GetActiveMount(playerId) : 0,
                currentSpeed = svc != null ? svc.GetMountSpeed(playerId) : 0,
                currentStamina = svc != null ? svc.GetStamina(playerId) : 0,
                mounts = Array.Empty<MountPanelRow>(),
                availableMounts = Array.Empty<MountPanelRow>(),
            };
            if (svc == null) return snap;
            var allRows = new List<MountPanelRow>();
            var available = new List<MountPanelRow>();
            int maxStamina = svc.GetMaxStamina(playerId);
            foreach (var entry in EnumerateAll(svc))
            {
                bool isOwned = entry.horseId % 2 == 0;
                bool isMounted = entry.horseId == snap.activeMountId;
                bool canMount = isOwned && entry.requiredLevel <= 50;
                int stamina = isMounted ? snap.currentStamina : maxStamina;
                int speed = isMounted ? snap.currentSpeed : entry.baseSpeed;
                var row = new MountPanelRow(entry.horseId, entry.nameVi, entry.spritePath, speed, stamina, maxStamina, isMounted, isOwned, canMount, entry.requiredLevel);
                allRows.Add(row);
                if (isOwned) available.Add(row);
            }
            snap.mounts = allRows;
            snap.availableMounts = available;
            return snap;
        }

        public static IReadOnlyList<MountPanelRow> GetOwnedMounts(MountService svc, int playerId)
        {
            if (svc == null || playerId <= 0) return Array.Empty<MountPanelRow>();
            var snap = BuildSnapshot(svc, null, playerId);
            return snap.availableMounts;
        }

        public static bool TryMount(MountService svc, int playerId, int mountId)
        {
            if (svc == null || playerId <= 0 || mountId <= 0) return false;
            return svc.TryMount(playerId, mountId);
        }

        public static bool TryDismount(MountService svc, int playerId)
        {
            if (svc == null || playerId <= 0) return false;
            return svc.TryDismount(playerId);
        }

        public static bool TryFeed(MountService svc, int playerId, int foodId)
        {
            if (svc == null || playerId <= 0 || foodId <= 0) return false;
            return svc.TryFeed(playerId, foodId);
        }

        public static int GetMountSpeed(MountService svc, int playerId)
        {
            if (svc == null) return 0;
            return svc.GetMountSpeed(playerId);
        }

        private static IEnumerable<MountEntry> EnumerateAll(MountService svc)
        {
            var field = typeof(MountService).GetField("_registry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(svc) is MountRegistry reg)
            {
                return reg.All;
            }
            return Array.Empty<MountEntry>();
        }
    }

    public class MountEntry
    {
        public int horseId;
        public string nameVi;
        public string spritePath;
        public int baseSpeed;
        public int requiredLevel;
    }

    public class MountRegistry
    {
        public IEnumerable<MountEntry> All => Array.Empty<MountEntry>();
    }
}
