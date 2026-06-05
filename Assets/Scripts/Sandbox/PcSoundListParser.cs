// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings soundlist.txt parser
// Source: soundlist.txt (danh sách âm thanh).
// Columns: SoundId  Name  FilePath  Category  Volume
//   Category: 0=skill, 1=ui, 2=ambient, 3=combat, 4=npc
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSoundListParser
    {
        public const int SoundIdCol = 0;
        public const int NameCol = 1;
        public const int FilePathCol = 2;
        public const int CategoryCol = 3;
        public const int VolumeCol = 4;

        public static List<PcSoundListEntry> ParseFile(string path)
        {
            var rows = new List<PcSoundListEntry>();
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
                rows.Add(new PcSoundListEntry
                {
                    soundId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    filePath = PcItemCommon.Str(cols, FilePathCol),
                    category = PcItemCommon.Int(cols, CategoryCol),
                    volume = PcItemCommon.Int(cols, VolumeCol),
                });
            }
            return rows;
        }

        public static PcSoundListRegistry BuildRegistry(string dir)
        {
            var reg = new PcSoundListRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcSoundListEntry
    {
        public int soundId;
        public string nameRaw;
        public string filePath;
        public int category;
        public int volume;
    }

    public sealed class PcSoundListRegistry
    {
        private readonly Dictionary<int, PcSoundListEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcSoundListEntry e) { if (e == null || e.soundId <= 0) return; _byId[e.soundId] = e; }
        public PcSoundListEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcSoundListEntry> GetByCategory(int category)
        {
            var list = new List<PcSoundListEntry>();
            foreach (var e in _byId.Values)
                if (e.category == category) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcSoundListEntry> All => new List<PcSoundListEntry>(_byId.Values);
    }
}
