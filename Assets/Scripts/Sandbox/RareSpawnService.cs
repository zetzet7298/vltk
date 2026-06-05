// -----------------------------------------------------------------------------
// VLTK Mobile — Quái vật hiếm spawn runtime service
// Wraps PcRareSpawnRegistry. Exposes spawn lookup by id/map.
// Vietnamese: "Quái hiếm", "Tỉ lệ spawn hiếm".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn điểm spawn quái hiếm.
    /// PC source: settings/rare.txt.
    /// </summary>
    public class RareSpawnService
    {
        public const string LogTag = "RareSpawn";
        private PcRareSpawnRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcRareSpawnRegistry registry)
        {
            _registry = registry ?? new PcRareSpawnRegistry();
            OnLoaded?.Invoke();
        }

        public static RareSpawnService LoadFromStreamingAssets(string relativeDir = "Reference/PcNpc")
        {
            var svc = new RareSpawnService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcRareSpawnParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} điểm spawn quái hiếm từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcRareSpawnEntry GetSpawn(int id) => _registry.Get(id);
        public List<PcRareSpawnEntry> GetByMap(int mapId) => _registry.GetByMap(mapId);
        public IEnumerable<PcRareSpawnEntry> All => _registry.All;
    }
}
