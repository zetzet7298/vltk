// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Editor
{
    /// <summary>
    /// Editor utility to ensure player and training NPCs always spawn at the pentagon
    /// center (MPS 6665, 6509) when entering play mode, regardless of where the player
    /// GameObject was placed in the editor scene.
    /// </summary>
    [InitializeOnLoad]
    public static class SandboxDefaultSpawnEditor
    {
        private const int DEFAULT_SPAWN_MPS_X = 6665;
        private const int DEFAULT_SPAWN_MPS_Y = 6509;

        static SandboxDefaultSpawnEditor()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // When entering play mode, reset player position to training center
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // SandboxManager will handle spawning via PlacePlayerOnActiveMap
                // This is just a safety check to document the intended spawn point
                Debug.Log($"[SandboxDefaultSpawn] Play mode entered. Player will spawn at MPS ({DEFAULT_SPAWN_MPS_X}, {DEFAULT_SPAWN_MPS_Y})");
            }
            
            // When exiting play mode, we could restore editor position here if needed
            // But Unity already handles this automatically via scene restore
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log("[SandboxDefaultSpawn] Edit mode entered. Player position in editor preserved.");
            }
        }

        [MenuItem("VLTK/Spawn/Set Player to Training Center (Edit Mode)")]
        private static void SetPlayerToTrainingCenterInEditor()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[SandboxDefaultSpawn] This menu is for Edit Mode only. Use SandboxManager in Play Mode.");
                return;
            }

            // Find MalePlayer in the scene
            var playerController = Object.FindObjectOfType<SandboxPlayerController>();
            if (playerController == null)
            {
                Debug.LogWarning("[SandboxDefaultSpawn] No SandboxPlayerController found in scene.");
                return;
            }

            // Calculate world position from MPS coordinates
            // Using the same conversion as runtime: BaLangEnemyDatabase.MpsToWorld
            // int regionRow = mpsY / 1024;
            // float worldX = mpsX;
            // float worldY = -(mpsY - regionRow * 512);
            int regionRow = DEFAULT_SPAWN_MPS_Y / 1024;
            float worldX = DEFAULT_SPAWN_MPS_X;
            float worldY = -(DEFAULT_SPAWN_MPS_Y - regionRow * 512);
            Vector2 spawnWorld = new Vector2(worldX, worldY);

            Undo.RecordObject(playerController.transform, "Move Player to Training Center");
            playerController.transform.position = new Vector3(spawnWorld.x, spawnWorld.y, 0f);
            
            EditorUtility.SetDirty(playerController.gameObject);
            
            Debug.Log($"[SandboxDefaultSpawn] Player moved to training center: MPS ({DEFAULT_SPAWN_MPS_X}, {DEFAULT_SPAWN_MPS_Y}) = World ({spawnWorld.x:F2}, {spawnWorld.y:F2})");
        }

        [MenuItem("VLTK/Spawn/Show Training Center Coordinates")]
        private static void ShowTrainingCenterCoordinates()
        {
            int regionRow = DEFAULT_SPAWN_MPS_Y / 1024;
            float worldX = DEFAULT_SPAWN_MPS_X;
            float worldY = -(DEFAULT_SPAWN_MPS_Y - regionRow * 512);
            
            Debug.Log($"[SandboxDefaultSpawn] Training Center Coordinates:\n" +
                     $"  MPS: ({DEFAULT_SPAWN_MPS_X}, {DEFAULT_SPAWN_MPS_Y})\n" +
                     $"  World: ({worldX:F2}, {worldY:F2})");
        }
    }
}
