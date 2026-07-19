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
        // PC walk/run mode toggle: when true and moving, the visual plays Walk (WK01) instead of Move/Run (RN01).
        bool walkMode { get; set; }
        // PC 打坐 (meditate): when true, the visual is forced to the Sit (ZZ01) action and ignores move input.
        bool isMeditating { get; set; }
        int direction { get; set; }
        bool playAutomatically { get; set; }

        int LoadedPartCount { get; }
        int ActionPartsRefreshCount { get; }
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
        void SetWeapon(PcWeaponType weapon, int exactVariant);
        void SetDirection(int nextDirection);
        void SetEquipVariant(PlayerEquipSlot slot, int variant);
        /// <summary>Set controller-owned normalized cast progress. Pass a negative value to resume Tick cadence.</summary>
        void SetLogicalActionProgress(float normalizedProgress);
        void Tick(float deltaTime);
    }
}
