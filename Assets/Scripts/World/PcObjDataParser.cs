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
        public int obstacleKind;
        public int loopAnimation;
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
            var lines = PcText.ReadLinesTcvn3(path);
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
                    obstacleKind = Int(cols, 52),
                    loopAnimation = Int(cols, 53),
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
