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
            return new MountPanelSnapshot { mounts = System.Array.Empty<MountPanelRow>(), availableMounts = System.Array.Empty<MountPanelRow>() };
        }

        public static IReadOnlyList<MountPanelRow> GetOwnedMounts(MountService svc, int playerId)
        {
            return System.Array.Empty<MountPanelRow>();
        }

        public static bool TryMount(MountService svc, int playerId, int mountId)
        {
            return false;
        }

        public static bool TryDismount(MountService svc, int playerId)
        {
            return false;
        }

        public static bool TryFeed(MountService svc, int playerId, int foodId)
        {
            return false;
        }

        public static int GetMountSpeed(MountService svc, int playerId)
        {
            return 0;
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
