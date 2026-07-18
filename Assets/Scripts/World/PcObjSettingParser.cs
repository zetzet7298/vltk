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
        public int layer;
        public int height;
        public int imageTotalFrame;
        public int imageCurFrame;
        public int imageTotalDir;
        public int imageCurDir;
        public int imageInterval;
        public int imageCgXpos;
        public int imageCgYpos;
        public int isUnseen;
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
            var lines = PcText.ReadLines(path, null);
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
                    layer = Int(cols, 7),
                    height = Int(cols, 8),
                    imageTotalFrame = Int(cols, 21),
                    imageCurFrame = Int(cols, 22),
                    imageTotalDir = Int(cols, 23),
                    imageCurDir = Int(cols, 24),
                    imageInterval = Int(cols, 25),
                    imageCgXpos = Int(cols, 26),
                    imageCgYpos = Int(cols, 27),
                    isUnseen = Int(cols, 51),
                    rawLine = line
                });
            }
            return reg;
        }

        private static int Int(string[] cols, int index)
        {
            if (cols == null || index < 0 || index >= cols.Length) return 0;
            return int.TryParse(cols[index].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }
    }
}
