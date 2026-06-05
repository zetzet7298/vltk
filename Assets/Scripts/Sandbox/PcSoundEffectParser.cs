// -----------------------------------------------------------------------------
// VLTK Mobile — PC soundeffect.txt sound effect registry parser
// Source: server settings/soundeffect.txt (Reference/PcSound).
// Cols: SoundId, Name, FilePath, Category, Volume
// Categories: 0=ui_click, 1=ui_open, 2=ui_close, 3=combat_hit, 4=combat_skill,
//             5=combat_death, 6=npc_greet, 7=ambient, 8=music
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSoundEffectParser
    {
        public const int SoundIdCol = 0;
        public const int NameCol = 1;
        public const int FilePathCol = 2;
        public const int CategoryCol = 3;
        public const int VolumeCol = 4;

        public static List<PcSoundEffectEntry> ParseFile(string path)
        {
            var rows = new List<PcSoundEffectEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, SoundIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSoundEffectEntry
                {
                    soundId = id,
                    name = PcItemCommon.Str(cols, NameCol),
                    filePath = PcItemCommon.Str(cols, FilePathCol),
                    category = PcItemCommon.Int(cols, CategoryCol),
                    volume = PcItemCommon.Int(cols, VolumeCol),
                });
            }
            return rows;
        }

        public static PcSoundEffectRegistry BuildRegistry(string dir)
        {
            var reg = new PcSoundEffectRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }

    [System.Serializable]
    public class PcSoundEffectEntry
    {
        public int soundId;
        public string name;
        public string filePath;
        public int category; // 0=ui_click ... 8=music
        public int volume;   // 0..100
    }

    public sealed class PcSoundEffectRegistry
    {
        private readonly Dictionary<int, PcSoundEffectEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcSoundEffectEntry e) { if (e == null || e.soundId <= 0) return; _byId[e.soundId] = e; }
        public PcSoundEffectEntry Get(int soundId) => _byId.TryGetValue(soundId, out var v) ? v : null;
        public IReadOnlyList<PcSoundEffectEntry> GetByCategory(int category)
        {
            var list = new List<PcSoundEffectEntry>();
            foreach (var e in _byId.Values)
                if (e.category == category) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcSoundEffectEntry> All => new List<PcSoundEffectEntry>(_byId.Values);
    }
}
