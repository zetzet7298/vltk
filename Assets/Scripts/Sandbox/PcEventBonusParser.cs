// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/event/* event bonus/award catalog parser
// Source: event/chinesenewyear/{bonuslist,crystal,giftslvlup}.txt
//         event/juanzhouhecheng/{amuletlist,pendantlist,ringlist,crystal,giftslvlup}.txt
//         event/wangwanglibao/gift_pack.txt
//         event/shenmibaoxiang/shenmibaoiangaward.txt
//         event/riddle/huadeng.txt  (XPOS, YPOS — lantern spawns)
//         event/zhongqiuhuodong/*lantern.txt  (XPOS, YPOS)
//         event/other/shensuanzi/{vn,zh}.txt
// All in GB2312, mixed column counts. We treat each non-empty non-header line
// as a generic reward/spawn row keyed by (eventName, fileName, lineIndex).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcEventBonusParser
    {
        public static List<PcEventEntry> ParseFile(string path, string eventName, string fileName)
        {
            var rows = new List<PcEventEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int idx = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                idx++;
                var cols = line.Split('\t');
                var entry = new PcEventEntry
                {
                    eventName = eventName,
                    fileName = fileName,
                    lineIndex = idx,
                    columns = new List<string>(cols),
                };
                if (cols.Length > 0) entry.nameRaw = cols[0];
                rows.Add(entry);
            }
            return rows;
        }

        public static PcEventRegistry BuildRegistry(string rootDir)
        {
            var reg = new PcEventRegistry();
            if (string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir)) return reg;
            foreach (var eventDir in Directory.GetDirectories(rootDir))
            {
                string eventName = Path.GetFileName(eventDir);
                foreach (var f in Directory.GetFiles(eventDir, "*.txt", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(f);
                    foreach (var s in ParseFile(f, eventName, fileName)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcEventEntry
    {
        public string eventName;
        public string fileName;
        public int lineIndex;
        public string nameRaw;
        public List<string> columns = new();
    }

    public sealed class PcEventRegistry
    {
        private readonly List<PcEventEntry> _all = new();
        private readonly Dictionary<string, List<PcEventEntry>> _byEvent = new();
        public int Count => _all.Count;
        public void Register(PcEventEntry e)
        {
            if (e == null) return;
            _all.Add(e);
            if (!_byEvent.TryGetValue(e.eventName, out var list))
            {
                list = new List<PcEventEntry>();
                _byEvent[e.eventName] = list;
            }
            list.Add(e);
        }
        public IReadOnlyList<PcEventEntry> GetEvent(string eventName)
            => _byEvent.TryGetValue(eventName ?? string.Empty, out var v) ? v : (IReadOnlyList<PcEventEntry>)System.Array.Empty<PcEventEntry>();
        public IEnumerable<PcEventEntry> All => _all;
    }
}
