// -----------------------------------------------------------------------------
// VLTK Mobile — MusicConfigService: runtime service cho PC music/musicset.txt
// Bổ sung cho MusicService: lookup bài nhạc + volume/start/end/cycleRandom theo map.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MusicConfigService
    {
        private readonly PcMusicConfigRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MusicConfigService() { _reg = new PcMusicConfigRegistry(); }
        public MusicConfigService(PcMusicConfigRegistry reg) { _reg = reg ?? new PcMusicConfigRegistry(); }

        public static MusicConfigService LoadFromStreamingAssets(string subDir = "Reference/PcMusic")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MusicConfigService(PcMusicConfigParser.BuildRegistry(path));
        }

        public PcMusicConfigEntry GetForMap(int mapId) => _reg.Get(mapId);
        public IReadOnlyList<PcMusicConfigEntry> All => _reg.All;
        public IReadOnlyList<PcMusicTrack> GetTracksForMap(int mapId)
        {
            var e = _reg.Get(mapId);
            return e != null ? (IReadOnlyList<PcMusicTrack>)e.tracks : System.Array.Empty<PcMusicTrack>();
        }
    }
}
