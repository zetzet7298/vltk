// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/obj/objsetting.txt parser
// Source: settings/obj/objsetting.txt (GBK). 53+ cols tab-separated.
// Lưu thông tin setting chi tiết cho mỗi object (cùng schema objdata.txt).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace VLTK.Sandbox
{
    [Serializable]
    public class PcObjSettingEntry
    {
        public int dataId;
        public string name;
        public string kind;
        public string imageName;
        public int lifeTime;
        public int height;
        public string rawLine;
    }

    public sealed class PcObjSettingRegistry
    {
        private readonly Dictionary<int, PcObjSettingEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcObjSettingEntry e)
        {
            if (e == null || e.dataId <= 0) return;
            _byId[e.dataId] = e;
        }
        public PcObjSettingEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcObjSettingEntry> All => new List<PcObjSettingEntry>(_byId.Values);
    }

    public static class PcObjSettingParser
    {
        public static PcObjSettingRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcObjSettingRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "objsetting.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                if (!int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0) continue;
                reg.Register(new PcObjSettingEntry
                {
                    dataId = id,
                    name = cols[0].Trim(),
                    kind = cols.Length > 2 ? cols[2].Trim() : string.Empty,
                    imageName = cols.Length > 4 ? cols[4].Trim() : string.Empty,
                    lifeTime = cols.Length > 6 && int.TryParse(cols[6].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lt) ? lt : 0,
                    height = cols.Length > 8 && int.TryParse(cols[8].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) ? h : 0,
                    rawLine = line
                });
            }
            return reg;
        }
    }
}
