// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/hudart.txt HUD Art Catalog parser
// Source: settings/hudart.txt (1,851 SPR/PNG art, tab-separated).
//   Cols: ArtId  Name  Path  Type  Width  Height
// Type: 0 = button, 1 = icon, 2 = bg, 3 = progress, 4 = label
// Tolerant of missing file (trả về registry rỗng).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcHudArtCatalogParser
    {
        public const int ArtIdCol = 0;
        public const int NameCol = 1;
        public const int PathCol = 2;
        public const int TypeCol = 3;
        public const int WidthCol = 4;
        public const int HeightCol = 5;

        public const int TypeButton = 0;
        public const int TypeIcon = 1;
        public const int TypeBg = 2;
        public const int TypeProgress = 3;
        public const int TypeLabel = 4;

        public static List<HudArtEntry> ParseFile(string path)
        {
            var rows = new List<HudArtEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            string[] lines;
            try { lines = PcItemCommon.ReadServerLines(path).ToArray(); }
            catch { try { lines = File.ReadAllLines(path); } catch { return rows; } }
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, ArtIdCol);
                if (id <= 0) continue;
                rows.Add(new HudArtEntry
                {
                    artId = id,
                    name = PcItemCommon.Str(cols, NameCol),
                    path = PcItemCommon.Str(cols, PathCol),
                    type = PcItemCommon.Int(cols, TypeCol),
                    width = PcItemCommon.Int(cols, WidthCol),
                    height = PcItemCommon.Int(cols, HeightCol),
                });
            }
            return rows;
        }

        public static HudArtRegistry BuildRegistry(string dir)
        {
            var reg = new HudArtRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string[] candidates = { "hudart.txt", "hud_art.txt", "ui_art.txt", "art.txt" };
            foreach (var fn in candidates)
            {
                string main = Path.Combine(dir, fn);
                if (File.Exists(main))
                {
                    foreach (var s in ParseFile(main)) reg.Register(s);
                    return reg;
                }
            }
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
            {
                foreach (var s in ParseFile(f)) reg.Register(s);
            }
            return reg;
        }
    }

    [System.Serializable]
    public class HudArtEntry
    {
        public int artId;
        public string name;
        public string path;       // Tương đối trong Resources/ hoặc StreamingAssets
        public int type;          // 0=button, 1=icon, 2=bg, 3=progress, 4=label
        public int width;
        public int height;
    }

    public sealed class HudArtRegistry
    {
        private readonly Dictionary<int, HudArtEntry> _byId = new();
        private readonly Dictionary<int, List<HudArtEntry>> _byType = new();
        public int Count => _byId.Count;
        public IEnumerable<HudArtEntry> All => _byId.Values;
        public void Register(HudArtEntry e)
        {
            if (e == null || e.artId <= 0) return;
            _byId[e.artId] = e;
            if (!_byType.TryGetValue(e.type, out var list))
            {
                list = new List<HudArtEntry>();
                _byType[e.type] = list;
            }
            list.Add(e);
        }
        public HudArtEntry Get(int artId)
            => _byId.TryGetValue(artId, out var v) ? v : null;
        public IReadOnlyList<HudArtEntry> GetByType(int type)
            => _byType.TryGetValue(type, out var v)
                ? (IReadOnlyList<HudArtEntry>)v
                : (IReadOnlyList<HudArtEntry>)System.Array.Empty<HudArtEntry>();
    }
}
