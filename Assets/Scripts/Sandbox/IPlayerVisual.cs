// -----------------------------------------------------------------------------
// VLTK Mobile — IPlayerVisual Interface
// Common API for MalePlayerVisual and FemalePlayerVisual.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Shared API cho cả male/female player visual.
    /// SandboxPlayerController (và combat services) chỉ reference interface này.
    /// </summary>
    public interface IPlayerVisual
    {
        PlayerVisualAction currentAction { get; set; }
        PcWeaponType currentWeapon { get; set; }
        bool isMounted { get; set; }
        int direction { get; set; }
        bool playAutomatically { get; set; }

        int LoadedPartCount { get; }
        int CurrentFrameInDirection { get; }
        bool HasAllRequiredParts { get; }
        int MissingRequiredPartCount { get; }
        IReadOnlyList<string> LastMissingRequiredParts { get; }
        Vector2 LastMoveInput { get; }
        bool IsMounted { get; }

        int GetCurrentDirection();
        int GetRiderSortingOrder();

        void SetMoveInput(Vector2 input);
        void SetAction(PlayerVisualAction action);
        void SetMounted(bool mounted);
        void SetWeapon(PcWeaponType weapon);
        void SetDirection(int nextDirection);
        void SetEquipVariant(PlayerEquipSlot slot, int variant);
        void Tick(float deltaTime);
    }
}
