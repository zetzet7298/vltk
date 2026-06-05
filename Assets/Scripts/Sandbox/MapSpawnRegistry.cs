// -----------------------------------------------------------------------------
// VLTK Mobile — Runtime registry for PcNormalSpawnParser output.
// The PC source (settings/normal.txt) is keyed by template id, not map id.
// The registry therefore indexes by template id (the natural key) and exposes
// GetSpawnsForMap / CountForMap for API compatibility with the spawn-shaped
// model. Since the source has no map column, those map lookups always return
// empty. Use GetByTemplateId / AllSpawns for the real access pattern.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class MapSpawnRegistry
    {
        private readonly List<SpawnPoint> _all = new List<SpawnPoint>();
        private readonly Dictionary<int, List<SpawnPoint>> _byTemplate =
            new Dictionary<int, List<SpawnPoint>>();

        public IReadOnlyList<SpawnPoint> AllSpawns => _all;

        public int TotalCount => _all.Count;

        public int TemplateCount => _byTemplate.Count;

        public void Load(IEnumerable<SpawnPoint> points)
        {
            if (points == null) return;
            foreach (var p in points)
                Register(p);
        }

        public void Register(SpawnPoint point)
        {
            if (point == null) return;
            _all.Add(point);
            if (!_byTemplate.TryGetValue(point.npcTemplateId, out var list))
            {
                list = new List<SpawnPoint>();
                _byTemplate[point.npcTemplateId] = list;
            }
            list.Add(point);
        }

        public IEnumerable<SpawnPoint> GetSpawnsForMap(int mapId)
        {
            return Array.Empty<SpawnPoint>();
        }

        public int CountForMap(int mapId)
        {
            return 0;
        }

        public bool TryGetByTemplateId(int templateId, out SpawnPoint point)
        {
            if (_byTemplate.TryGetValue(templateId, out var list) && list != null && list.Count > 0)
            {
                point = list[0];
                return true;
            }
            point = null;
            return false;
        }

        public IReadOnlyList<SpawnPoint> GetAllByTemplateId(int templateId)
        {
            if (_byTemplate.TryGetValue(templateId, out var list))
                return list;
            return Array.Empty<SpawnPoint>();
        }

        public int CountForTemplate(int templateId)
        {
            return _byTemplate.TryGetValue(templateId, out var list) ? list.Count : 0;
        }

        public void Clear()
        {
            _all.Clear();
            _byTemplate.Clear();
        }
    }
}
