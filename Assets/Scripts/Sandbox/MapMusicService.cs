// -----------------------------------------------------------------------------
// VLTK Mobile — MapMusic runtime service
// Wraps PcMapMusicRegistry. PC source: settings/mapmusic.txt.
// Vietnamese: "Nhạc Bản Đồ", "Nhạc Ban Ngày", "Nhạc Ban Đêm", "Nhạc Chiến Đấu".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MapMusicService
    {
        private readonly PcMapMusicRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapMusicService() { _reg = new PcMapMusicRegistry(); }
        public MapMusicService(PcMapMusicRegistry reg) { _reg = reg ?? new PcMapMusicRegistry(); }

        public static MapMusicService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MapMusicService(PcMapMusicParser.BuildRegistry(path));
        }

        public PcMapMusicEntry Get(int mapId) => _reg.Get(mapId);
        public PcMapMusicEntry GetMusicForMap(int mapId) => _reg.Get(mapId);
        public IReadOnlyList<PcMapMusicEntry> GetAll() => _reg.All;

        public int GetDayMusic(int mapId) { var e = _reg.Get(mapId); return e != null ? e.dayMusicId : 0; }
        public int GetNightMusic(int mapId) { var e = _reg.Get(mapId); return e != null ? e.nightMusicId : 0; }
        public int GetBattleMusic(int mapId) { var e = _reg.Get(mapId); return e != null ? e.battleMusicId : 0; }
        public int GetDefaultMusic(int mapId) { var e = _reg.Get(mapId); return e != null ? e.musicId : 0; }
    }
}
