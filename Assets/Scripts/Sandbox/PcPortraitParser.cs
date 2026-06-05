// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings portrait.ini parser
// Source: portrait.ini (chân dung/avatar).
// Columns: PortraitId  Name  FactionId  Sex  SprPath  RequiredLevel
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcPortraitParser
    {
        public const int PortraitIdCol = 0;
        public const int NameCol = 1;
        public const int FactionIdCol = 2;
        public const int SexCol = 3;
        public const int SprPathCol = 4;
        public const int RequiredLevelCol = 5;

        public static List<PcPortraitEntry> ParseFile(string path)
        {
            var rows = new List<PcPortraitEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, PortraitIdCol);
                if (id <= 0) continue;
                rows.Add(new PcPortraitEntry
                {
                    portraitId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    factionId = PcItemCommon.Int(cols, FactionIdCol),
                    sex = PcItemCommon.Int(cols, SexCol),
                    sprPath = PcItemCommon.Str(cols, SprPathCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                });
            }
            return rows;
        }

        public static PcPortraitRegistry BuildRegistry(string dir)
        {
            var reg = new PcPortraitRegistry();
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
    public class PcPortraitEntry
    {
        public int portraitId;
        public string nameRaw;
        public int factionId;
        public int sex;
        public string sprPath;
        public int requiredLevel;
    }

    public sealed class PcPortraitRegistry
    {
        private readonly Dictionary<int, PcPortraitEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcPortraitEntry e) { if (e == null || e.portraitId <= 0) return; _byId[e.portraitId] = e; }
        public PcPortraitEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcPortraitEntry> GetByFaction(int factionId)
        {
            var list = new List<PcPortraitEntry>();
            foreach (var e in _byId.Values)
                if (e.factionId == factionId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcPortraitEntry> All => new List<PcPortraitEntry>(_byId.Values);
    }
}
