// -----------------------------------------------------------------------------
// VLTK Mobile — PC mapmusic.txt parser
// Source: settings/mapmusic.txt (nhạc riêng cho từng map: ngày/đêm/chiến đấu).
// Cols: MapId  MusicId  DayMusicId  NightMusicId  BattleMusicId
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapMusicParser
    {
        public const int MapIdCol = 0;
        public const int MusicIdCol = 1;
        public const int DayMusicIdCol = 2;
        public const int NightMusicIdCol = 3;
        public const int BattleMusicIdCol = 4;

        public static List<PcMapMusicEntry> ParseFile(string path)
        {
            var rows = new List<PcMapMusicEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, MapIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMapMusicEntry
                {
                    mapId = id,
                    musicId = PcItemCommon.Int(cols, MusicIdCol),
                    dayMusicId = PcItemCommon.Int(cols, DayMusicIdCol),
                    nightMusicId = PcItemCommon.Int(cols, NightMusicIdCol),
                    battleMusicId = PcItemCommon.Int(cols, BattleMusicIdCol),
                });
            }
            return rows;
        }

        public static PcMapMusicRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapMusicRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcMapMusicEntry
    {
        public int mapId;
        public int musicId;
        public int dayMusicId;
        public int nightMusicId;
        public int battleMusicId;
    }

    public sealed class PcMapMusicRegistry
    {
        private readonly Dictionary<int, PcMapMusicEntry> _byMapId = new();
        public int Count => _byMapId.Count;
        public void Register(PcMapMusicEntry e)
        {
            if (e == null || e.mapId <= 0) return;
            _byMapId[e.mapId] = e;
        }
        public PcMapMusicEntry Get(int mapId) => _byMapId.TryGetValue(mapId, out var v) ? v : null;
        public IReadOnlyList<PcMapMusicEntry> All => new List<PcMapMusicEntry>(_byMapId.Values);
    }
}
