// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.1 Spawn Batch Manager
// Batch enemy updates (position, AI, visual) để giảm per-frame overhead.
// Spatial partitioning: chỉ update enemies gần camera.
// Source: PcNpcS.txt AI parameters, Region_S.dat spawn positions.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Quản lý batch update cho enemy spawns. Chỉ update enemies gần camera,
    /// deactivate enemies ở xa (beyond vision radius).
    /// Giảm per-frame overhead khi map có nhiều enemies (500+).
    /// </summary>
    public class SpawnBatchManager
    {
        public const int DefaultBatchSize = 50;

        private readonly List<BatchEnemyEntry> _enemies = new();
        private readonly Queue<int> _activationQueue = new();
        private readonly HashSet<int> _activeSet = new();
        // Tracks indices already enqueued in the current flush so the same
        // enemy is not pushed onto _activationQueue every frame it stays in
        // activation range. Mirrors _activeSet but covers "pending" state.
        private readonly HashSet<int> _pendingSet = new();

        private int _batchSize;
        private float _deactivateDistance;
        private float _activateDistance;
        private int _currentBatchIndex;

        /// <summary>Số enemy đang active.</summary>
        public int ActiveCount => _activeSet.Count;

        /// <summary>Tổng số enemy đã đăng ký.</summary>
        public int TotalCount => _enemies.Count;

        public SpawnBatchManager(int batchSize = DefaultBatchSize,
            float activateDistance = 800f, float deactivateDistance = 1200f)
        {
            _batchSize = batchSize;
            _activateDistance = activateDistance;
            _deactivateDistance = deactivateDistance;
        }

        // ── Registration ───────────────────────────────────────────────────

        /// <summary>Đăng ký một enemy vào batch system.</summary>
        public void Register(int enemyId, Vector2 position, NpcSpawnHandle handle)
        {
            _enemies.Add(new BatchEnemyEntry
            {
                enemyId = enemyId,
                position = position,
                handle = handle,
                isActive = false,
                lastUpdateFrame = -1,
            });
        }

        /// <summary>Xóa toàn bộ enemies.</summary>
        public void Clear()
        {
            _enemies.Clear();
            _activationQueue.Clear();
            _activeSet.Clear();
            _pendingSet.Clear();
            _currentBatchIndex = 0;
        }

        // ── Frame update ───────────────────────────────────────────────────

        /// <summary>
        /// Update mỗi frame. Thực hiện:
        /// 1. Deactivate enemies quá xa camera
        /// 2. Activate enemies gần camera (batch)
        /// 3. Update active enemies theo batch (AI, visual)
        /// </summary>
        public void UpdateBatch(Vector2 cameraPosition, int frameCount)
        {
            // Phase 1: Distance check → deactivate far enemies
            float deactDistSq = _deactivateDistance * _deactivateDistance;
            float actDistSq = _activateDistance * _activateDistance;

            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                float distSq = (e.position - cameraPosition).sqrMagnitude;

                if (e.isActive && distSq > deactDistSq)
                {
                    // Deactivate
                    e.isActive = false;
                    e.handle?.SetActive(false);
                    _activeSet.Remove(i);
                    _enemies[i] = e;
                }
                else if (!e.isActive && distSq < actDistSq)
                {
                    // Queue for activation (dedupe: don't push the same index
                    // twice while it is still pending activation).
                    if (_pendingSet.Add(i))
                        _activationQueue.Enqueue(i);
                }
            }

            // Phase 2: Activate queued enemies (limited batch per frame)
            int activated = 0;
            while (_activationQueue.Count > 0 && activated < _batchSize)
            {
                int idx = _activationQueue.Dequeue();
                _pendingSet.Remove(idx);
                var e = _enemies[idx];
                if (!e.isActive)
                {
                    e.isActive = true;
                    e.handle?.SetActive(true);
                    _activeSet.Add(idx);
                    _enemies[idx] = e;
                    activated++;
                }
            }

            // Phase 3: Update active enemies in batch
            int batchCount = 0;
            for (int offset = 0; offset < _enemies.Count && batchCount < _batchSize; offset++)
            {
                int idx = (_currentBatchIndex + offset) % _enemies.Count;
                var e = _enemies[idx];

                if (e.isActive && frameCount - e.lastUpdateFrame >= 2) // Update every 2 frames
                {
                    e.handle?.UpdateTick();
                    e.lastUpdateFrame = frameCount;
                    _enemies[idx] = e;
                    batchCount++;
                }
            }

            _currentBatchIndex = (_currentBatchIndex + _batchSize) % Mathf.Max(1, _enemies.Count);
        }

        // ── Data structures ────────────────────────────────────────────────

        private struct BatchEnemyEntry
        {
            public int enemyId;
            public Vector2 position;
            public NpcSpawnHandle handle;
            public bool isActive;
            public int lastUpdateFrame;
        }
    }

    /// <summary>
    /// Handle cho một enemy spawn. Được gọi bởi SpawnBatchManager
    /// để activate/deactivate/update enemy.
    /// </summary>
    public abstract class NpcSpawnHandle
    {
        public abstract void SetActive(bool active);
        public abstract void UpdateTick();
    }
}
