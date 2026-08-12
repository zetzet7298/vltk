// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.1 Player Mount Service
// Quản lý cưỡi ngựa / horse mount cho player.
// Source: PC NpcS.txt HorseType, npcres/horse SPR set.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Trạng thái cưỡi ngựa.
    /// </summary>
    public enum MountState
    {
        None,       // Không cưỡi
        Mounting,   // Đang lên ngựa (transition)
        Mounted,    // Đang cưỡi
        Dismounting,// Đang xuống ngựa (transition)
    }

    /// <summary>
    /// Event data khi mount state thay đổi.
    /// </summary>
    public struct MountChangeEvent
    {
        public MountState oldState;
        public MountState newState;
        public int horseType;       // PC HorseType from NpcS.txt
        public float speedMultiplier;
    }

    /// <summary>
    /// Service quản lý cưỡi ngựa / horse mount cho player.
    /// Khi mounted:
    /// - Switch player visual sang riding SPR
    /// - Tăng movement speed (ngựa chạy nhanh hơn)
    /// - Thay đổi animation: stand→ride, run→gallop, attack→mounted_attack
    /// Source: PC NpcS.txt HorseType column + npcres/horse SPR files.
    /// </summary>
    public class PlayerMountService
    {
        private MountState _state = MountState.None;
        private int _horseType;
        private float _mountTransitionTime = 0.5f;
        private float _transitionTimer;
        private IPlayerMountHost _host;
        private int _playerId = 0;

        /// <summary>Current mount state.</summary>
        public MountState State => _state;

        /// <summary>Current horse type (PC HorseType from NpcS.txt).</summary>
        public int HorseType => _horseType;

        /// <summary>Speed multiplier khi mounted. Source: PC horse speed stats.</summary>
        public float SpeedMultiplier => _state == MountState.Mounted ? 1.8f : 1.0f;

        /// <summary>Có đang mounted không.</summary>
        public bool IsMounted => _state == MountState.Mounted;

        /// <summary>Event fired khi mount state thay đổi.</summary>
        public event Action<MountChangeEvent> OnMountChanged;

        public int PlayerId { get => _playerId; set => _playerId = value; }
        public float MountTransitionTime { get => _mountTransitionTime; set => _mountTransitionTime = value; }

        public PlayerMountService() : this(null) { }
        public PlayerMountService(IPlayerMountHost host) { _host = host; }

        public void AttachHost(IPlayerMountHost host) { _host = host; }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Bắt đầu cưỡi ngựa. horseType từ PC NpcS.txt HorseType column.
        /// Source: NpcS.txt → HorseType field, npcres/horse/horse_{type}_*.spr
        /// </summary>
        public void Mount(int horseType)
        {
            if (_state == MountState.Mounted || _state == MountState.Mounting) return;

            var oldState = _state;
            _horseType = horseType;
            _state = MountState.Mounting;
            _transitionTimer = _mountTransitionTime;

            OnMountChanged?.Invoke(new MountChangeEvent
            {
                oldState = oldState,
                newState = _state,
                horseType = horseType,
                speedMultiplier = SpeedMultiplier,
            });

            if (_host != null)
            {
                _host.RefreshMountVisual(horseType, _state, SpeedMultiplier);
                _host.PlayMountSFX(horseType, true);
                _host.OnMountStarted(horseType, _mountTransitionTime);
                _host.LogMountEvent(horseType, $"Bắt đầu cưỡi ngựa loại {horseType}");
                _host.SaveMountState(_playerId, horseType, _state, false);
            }

            SubsystemLog.Info("Mount", $"Mounting horse type {horseType}");
        }

        /// <summary>
        /// Xuống ngựa.
        /// </summary>
        public void Dismount()
        {
            if (_state != MountState.Mounted) return;

            var oldState = _state;
            int dismountHorseType = _horseType;
            _state = MountState.Dismounting;
            _transitionTimer = _mountTransitionTime;

            OnMountChanged?.Invoke(new MountChangeEvent
            {
                oldState = oldState,
                newState = _state,
                horseType = _horseType,
                speedMultiplier = 1.0f,
            });

            if (_host != null)
            {
                _host.RefreshMountVisual(_horseType, _state, 1.0f);
                _host.PlayMountSFX(_horseType, false);
                _host.OnDismountStarted(dismountHorseType, _mountTransitionTime);
                _host.LogMountEvent(dismountHorseType, $"Bắt đầu xuống ngựa loại {dismountHorseType}");
                _host.SaveMountState(_playerId, dismountHorseType, _state, true);
            }

            SubsystemLog.Info("Mount", "Dismounting");
        }

        /// <summary>
        /// Update mỗi frame. Xử lý mount/dismount transitions.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_state == MountState.Mounting)
            {
                _transitionTimer -= deltaTime;
                if (_transitionTimer <= 0f)
                {
                    var old = _state;
                    _state = MountState.Mounted;
                    OnMountChanged?.Invoke(new MountChangeEvent
                    {
                        oldState = old,
                        newState = _state,
                        horseType = _horseType,
                        speedMultiplier = SpeedMultiplier,
                    });
                    if (_host != null)
                    {
                        _host.RefreshMountVisual(_horseType, _state, SpeedMultiplier);
                        _host.OnMountCompleted(_horseType, SpeedMultiplier);
                        _host.SaveMountState(_playerId, _horseType, _state, true);
                    }
                }
            }
            else if (_state == MountState.Dismounting)
            {
                _transitionTimer -= deltaTime;
                if (_transitionTimer <= 0f)
                {
                    var old = _state;
                    int dismountHorseType = _horseType;
                    _state = MountState.None;
                    _horseType = 0;
                    OnMountChanged?.Invoke(new MountChangeEvent
                    {
                        oldState = old,
                        newState = _state,
                        horseType = 0,
                        speedMultiplier = 1.0f,
                    });
                    if (_host != null)
                    {
                        _host.OnDismountCompleted();
                        _host.LogMountEvent(dismountHorseType, "Hoàn tất xuống ngựa");
                        _host.SaveMountState(_playerId, 0, _state, false);
                    }
                }
            }
        }

        // ── Visual helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Get SPR path cho horse body parts khi mounted.
        /// Source: npcres/horse/horse_{type}_{action}.spr
        /// </summary>
        public static string GetHorseSpritePath(int horseType, string action)
        {
            if (horseType <= 0) return null;
            return $@"spr\npcres\horse\horse_{horseType:D3}_{action}.spr";
        }

        /// <summary>
        /// Get riding action suffix. Khi mounted, player animations switch to riding variants.
        /// Source: PC 男主角骑马关联表.txt
        /// </summary>
        public static string GetMountedActionSuffix(PlayerVisualAction action, PcWeaponType weapon)
        {
            // PC Settings/NpcRes/{男,女}主角骑马关联表.txt.
            // Never substitute non-canonical legacy banks.
            if (weapon == PcWeaponType.HiddenWeapon && action is PlayerVisualAction.RideAttack or PlayerVisualAction.Attack or
                PlayerVisualAction.RideAttack1 or PlayerVisualAction.Attack1 or PlayerVisualAction.RideMagic or PlayerVisualAction.Magic)
                return "HM01";

            return action switch
            {
                PlayerVisualAction.Ride or PlayerVisualAction.Idle => MalePlayerSpriteCatalog.MountIdleSuffix,
                PlayerVisualAction.RideWalk or PlayerVisualAction.Walk => MalePlayerSpriteCatalog.MountWalkSuffix,
                PlayerVisualAction.RideMove or PlayerVisualAction.Move => MalePlayerSpriteCatalog.MountMoveSuffix,
                PlayerVisualAction.RideAttack or PlayerVisualAction.Attack => "HA01",
                PlayerVisualAction.RideAttack1 or PlayerVisualAction.Attack1 => "HA02",
                PlayerVisualAction.RideMagic or PlayerVisualAction.Magic => "HM01",
                _ => MalePlayerSpriteCatalog.MountIdleSuffix,
            };
        }

        /// <summary>
        /// PC horse speed from NpcS.txt. Horses have RunSpeed in NPC template.
        /// </summary>
        public static float GetHorseSpeedMultiplier(int horseType)
        {
            // Default PC horse speed multiplier: 1.8x
            // Different horse types may have different speeds
            return horseType switch
            {
                1 => 1.6f,   // Basic horse
                2 => 1.8f,   // War horse
                3 => 2.0f,   // Premium horse
                _ => 1.8f,
            };
        }
    }
}
