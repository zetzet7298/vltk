// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/mailtemplate.txt (Mail templates) parser
// Source: mailtemplate.txt (templates cho thư trong game).
// Cols: TemplateId, TitleTemplate, BodyTemplate, SenderName, DefaultItemId, Type
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMailParser
    {
        public const int TemplateIdCol = 0;
        public const int TitleTemplateCol = 1;
        public const int BodyTemplateCol = 2;
        public const int SenderNameCol = 3;
        public const int DefaultItemIdCol = 4;
        public const int TypeCol = 5;

        public static List<PcMailEntry> ParseFile(string path)
        {
            var rows = new List<PcMailEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, TemplateIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMailEntry
                {
                    templateId = id,
                    titleTemplate = PcItemCommon.Str(cols, TitleTemplateCol),
                    bodyTemplate = PcItemCommon.Str(cols, BodyTemplateCol),
                    senderName = PcItemCommon.Str(cols, SenderNameCol),
                    defaultItemId = PcItemCommon.Int(cols, DefaultItemIdCol),
                    type = PcItemCommon.Int(cols, TypeCol),
                });
            }
            return rows;
        }

        public static PcMailRegistry BuildRegistry(string dir)
        {
            var reg = new PcMailRegistry();
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
    public class PcMailEntry
    {
        public int templateId;
        public string titleTemplate;
        public string bodyTemplate;
        public string senderName;
        public int defaultItemId;
        public int type;
    }

    public sealed class PcMailRegistry
    {
        private readonly Dictionary<int, PcMailEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcMailEntry e) { if (e == null || e.templateId <= 0) return; _byId[e.templateId] = e; }
        public PcMailEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcMailEntry> GetAll()
        {
            var list = new List<PcMailEntry>(_byId.Values);
            return list;
        }
        public IReadOnlyList<PcMailEntry> All => new List<PcMailEntry>(_byId.Values);
    }
}
