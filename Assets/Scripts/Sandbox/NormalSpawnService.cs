// -----------------------------------------------------------------------------
// VLTK Mobile — Quái vật thường spawn runtime service
// Wraps PcNormalSpawnRegistry. Exposes spawn lookup by id/map.
// Vietnamese: "Quái thường", "Điểm sinh quái".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn điểm spawn quái thường.
    /// PC source: settings/normal.txt.
    /// </summary>
    public class NormalSpawnService
    {
        public const string LogTag = "NormalSpawn";
        private PcNormalSpawnRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcNormalSpawnRegistry registry)
        {
            _registry = registry ?? new PcNormalSpawnRegistry();
            OnLoaded?.Invoke();
        }

        public static NormalSpawnService LoadFromStreamingAssets(string relativeDir = "Reference/PcNpc")
        {
            var svc = new NormalSpawnService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcNormalSpawnParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} điểm spawn quái thường từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public SpawnPoint GetSpawn(int id) => _registry.Get(id);
        public List<SpawnPoint> GetByMap(int mapId) => _registry.GetByMap(mapId);
        public IEnumerable<SpawnPoint> All => _registry.All;
    }
}
