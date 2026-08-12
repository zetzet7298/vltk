// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/music/musicset.txt music (nhạc nền) parser
// Source: musicset.txt (GB2312, tab-separated, 1 row per map).
//   Map ID \t MusicFile1 \t Volume1 \t StartTime1 \t EndTime1 \t CycleRandom1
//   + repeats for slot 2..4 (battle, cave, boss variants).
// We flatten all 4 slots into PcMusicEntry rows with a SceneType derived from slot.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMusicParser
    {
        public static List<PcMusicEntry> ParseFile(string path)
        {
            var rows = new List<PcMusicEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                autoId++;
                int mapId = PcItemCommon.Int(cols, 0);
                // Slots 1..4 (cols 1..4 are file paths, 5..8 volume-ish, etc.)
                string[] sceneNames = { "City", "Field", "Battle", "Cave", "Boss" };
                for (int slot = 0; slot < 4; slot++)
                {
                    int fileCol = 1 + slot * 5;
                    int volCol = fileCol + 1;
                    if (cols.Length <= fileCol) continue;
                    string filePath = PcItemCommon.Str(cols, fileCol);
                    if (string.IsNullOrEmpty(filePath) || filePath == "-1") continue;
                    rows.Add(new PcMusicEntry
                    {
                        trackId = autoId * 10 + slot,
                        trackName = $"{sceneNames[System.Math.Min(slot, sceneNames.Length - 1)]}_{mapId}",
                        filePath = filePath,
                        sceneType = slot, // 0=city, 1=field, 2=battle, 3=cave
                        volume = cols.Length > volCol ? PcItemCommon.Int(cols, volCol) : 100,
                        loop = 1,
                        mapId = mapId,
                    });
                }
            }
            return rows;
        }

        public static PcMusicRegistry BuildRegistry(string dir)
        {
            var reg = new PcMusicRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "musicset.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcMusicEntry
    {
        public int trackId;
        public string trackName;
        public string filePath;
        public int sceneType;   // 0=city, 1=field, 2=battle, 3=cave, 4=boss
        public int volume;      // 0-100
        public int loop;        // 0=once, 1=loop
        public int mapId;       // PC: per-map slot
    }

    public sealed class PcMusicRegistry
    {
        private readonly Dictionary<int, PcMusicEntry> _byId = new();
        private readonly Dictionary<int, List<PcMusicEntry>> _byScene = new();
        public int Count => _byId.Count;
        public IEnumerable<PcMusicEntry> All => _byId.Values;

        public void Register(PcMusicEntry e)
        {
            if (e == null || e.trackId <= 0) return;
            _byId[e.trackId] = e;
            if (!_byScene.TryGetValue(e.sceneType, out var list))
            {
                list = new List<PcMusicEntry>();
                _byScene[e.sceneType] = list;
            }
            list.Add(e);
        }

        public PcMusicEntry Get(int trackId)
            => _byId.TryGetValue(trackId, out var v) ? v : null;

        public IReadOnlyList<PcMusicEntry> GetByScene(int sceneType)
            => _byScene.TryGetValue(sceneType, out var v)
                ? (IReadOnlyList<PcMusicEntry>)v
                : System.Array.Empty<PcMusicEntry>();
    }
}
