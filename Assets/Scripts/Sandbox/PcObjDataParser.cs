// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/obj/objdata.txt parser
// Source: settings/obj/objdata.txt (GBK/GB2312). 53+ cols tab-separated.
// Quản lý metadata object trong thế giới (rương, biển báo, vật phẩm rơi).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Metadata 1 object trong thế giới game (rương, biển báo, đạn rơi, vật phẩm đặt xuống đất).
    /// </summary>
    [Serializable]
    public class PcObjDataEntry
    {
        public int dataId;
        public string name;
        public string kind;
        public string imageName;
        public int lifeTime;
        public int height;
    }

    public sealed class PcObjDataRegistry
    {
        private readonly Dictionary<int, PcObjDataEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcObjDataEntry e)
        {
            if (e == null || e.dataId <= 0) return;
            _byId[e.dataId] = e;
        }
        public PcObjDataEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcObjDataEntry> All => new List<PcObjDataEntry>(_byId.Values);
        public IReadOnlyList<PcObjDataEntry> GetByKind(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return System.Array.Empty<PcObjDataEntry>();
            var result = new List<PcObjDataEntry>();
            foreach (var e in _byId.Values) if (string.Equals(e.kind, kind, StringComparison.OrdinalIgnoreCase)) result.Add(e);
            return result;
        }
    }

    public static class PcObjDataParser
    {
        public static PcObjDataRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcObjDataRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "objdata.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            bool headerSkipped = false;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                if (!int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0) continue;
                reg.Register(new PcObjDataEntry
                {
                    dataId = id,
                    name = cols[0].Trim(),
                    kind = cols.Length > 2 ? cols[2].Trim() : string.Empty,
                    imageName = cols.Length > 4 ? cols[4].Trim() : string.Empty,
                    lifeTime = cols.Length > 6 && int.TryParse(cols[6].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lt) ? lt : 0,
                    height = cols.Length > 8 && int.TryParse(cols[8].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) ? h : 0
                });
            }
            return reg;
        }
    }
}
