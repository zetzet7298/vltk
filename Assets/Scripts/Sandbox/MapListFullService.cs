// -----------------------------------------------------------------------------
// VLTK Mobile — MapListFull runtime service
// Wraps PcMapListFullRegistry. PC source: settings/maplist.ini (1,005 maps).
// Vietnamese: "Thành phố", "Thủ đô", "Vùng", "Đồng", "Hang động", "Bang hội", "Chiến trường", "Phụ bản".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MapListFullService
    {
        // StreamingAssets-relative directory holding the committed maplist.ini.
        // Reflected on by ServiceStreamingAssetTestUtil to confirm the load
        // matches committed data.
        public const string DefaultStreamingDir = "Reference/PcMap";

        private readonly PcMapListFullRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapListFullService() { _reg = new PcMapListFullRegistry(); }
        public MapListFullService(PcMapListFullRegistry reg) { _reg = reg ?? new PcMapListFullRegistry(); }

        public static MapListFullService LoadFromStreamingAssets(string subDir = DefaultStreamingDir)
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MapListFullService(PcMapListFullParser.BuildRegistry(path));
        }

        public PcMapListFullEntry GetMap(int id) => _reg.Get(id);
        public IReadOnlyList<PcMapListFullEntry> GetByType(int type) => _reg.GetByType(type);
        public IReadOnlyList<PcMapListFullEntry> GetByLevel(int level) => _reg.GetByLevel(level);
        public IReadOnlyList<PcMapListFullEntry> GetCities() => _reg.GetCityMaps();
        public IReadOnlyList<PcMapListFullEntry> GetCapitals() => _reg.GetCapitalMaps();
        public IReadOnlyList<PcMapListFullEntry> GetCountries() => _reg.GetCountryMaps();
        public IReadOnlyList<PcMapListFullEntry> GetFields() => _reg.GetFieldMaps();
        public IReadOnlyList<PcMapListFullEntry> GetCaves() => _reg.GetCaveMaps();
        public IReadOnlyList<PcMapListFullEntry> GetTongMaps() => _reg.GetTongMaps();
        public IReadOnlyList<PcMapListFullEntry> GetBattlefields() => _reg.GetBattlefieldMaps();
        public IReadOnlyList<PcMapListFullEntry> GetInstances() => _reg.GetInstanceMaps();
        public IReadOnlyList<PcMapListFullEntry> GetOthers() => _reg.GetOtherMaps();
        public IReadOnlyList<PcMapListFullEntry> GetAllMaps() => _reg.All;

        public bool IsBattlefield(int mapId) { var e = _reg.Get(mapId); return e != null && e.isBattlefield; }
        public bool IsCity(int mapId) { var e = _reg.Get(mapId); return e != null && e.type == PcMapListFullParser.TypeCity; }
        public bool IsInstance(int mapId) { var e = _reg.Get(mapId); return e != null && e.isInstance; }

        public string GetMapTypeName(int type) => type switch
        {
            PcMapListFullParser.TypeCity => "Thành phố",
            PcMapListFullParser.TypeCapital => "Thủ đô",
            PcMapListFullParser.TypeCountry => "Vùng",
            PcMapListFullParser.TypeField => "Đồng",
            PcMapListFullParser.TypeCave => "Hang động",
            PcMapListFullParser.TypeTong => "Bang hội",
            PcMapListFullParser.TypeBattlefield => "Chiến trường",
            PcMapListFullParser.TypeInstance => "Phụ bản",
            _ => "Khác",
        };

        public IReadOnlyList<PcMapListFullEntry> SearchByName(string prefix)
        {
            var list = new List<PcMapListFullEntry>();
            if (string.IsNullOrEmpty(prefix)) return list;
            foreach (var e in _reg.All)
                if (e.nameRaw != null && e.nameRaw.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    list.Add(e);
            return list;
        }
    }
}
