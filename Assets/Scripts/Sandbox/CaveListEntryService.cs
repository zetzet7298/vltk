// -----------------------------------------------------------------------------
// VLTK Mobile — Hang động (Cave list) runtime service
// Wraps PcCaveListEntryRegistry. Exposes cave lookup + enter validation.
// Vietnamese: "Hang động", "Cấp yêu cầu", "Tổ đội".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn hang động (Cave).
    /// PC source: settings/cavelist.ini.
    /// </summary>
    public class CaveListEntryService
    {
        public const string LogTag = "CaveList";
        private PcCaveListEntryRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcCaveListEntryRegistry registry)
        {
            _registry = registry ?? new PcCaveListEntryRegistry();
            OnLoaded?.Invoke();
        }

        public static CaveListEntryService LoadFromStreamingAssets(string relativeDir = "Reference/PcMap")
        {
            var svc = new CaveListEntryService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcCaveListEntryParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} hang động từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcCaveListEntryRow GetCave(int id) => _registry.Get(id);
        public List<PcCaveListEntryRow> GetByMap(int mapId) => _registry.GetByMap(mapId);
        public List<PcCaveListEntryRow> GetByLevel(int level) => _registry.GetByLevel(level);
        public bool CanEnter(int id, int level, int party) => _registry.CanEnter(id, level, party);
        public IEnumerable<PcCaveListEntryRow> All => _registry.All;
    }
}
