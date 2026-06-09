// -----------------------------------------------------------------------------
// VLTK Mobile — PC HuoYueDu / activity-points source index parser.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/{script,settings}/huoyuedu
// Catalog only: source/config evidence plus huoyuedu.txt rows. No Lua execution
// and no gameplay/runtime reward claim.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcHuoYueDuParser
    {
        public const string SourceIndexFileName = "huoyuedu_source_index.txt";
        public const string ConfigIndexFileName = "huoyuedu_config_index.txt";
        public const string ActivityConfigFileName = "huoyuedu.txt";

        public const int SourceIndexCol = 0;
        public const int SourceRootCol = 1;
        public const int RelativePathCol = 2;
        public const int DirectoryCol = 3;
        public const int FileNameCol = 4;
        public const int ExtensionCol = 5;
        public const int IsLuaCol = 6;
        public const int SourceSizeBytesCol = 7;
        public const int SourceLineCountCol = 8;
        public const int SourceSha256Col = 9;

        public const int ConfigSizeBytesCol = 6;
        public const int ConfigLineCountCol = 7;
        public const int ConfigDataRowsCol = 8;
        public const int ConfigSha256Col = 9;
        public const int ConfigHeaderColumnsCol = 10;

        public static List<PcHuoYueDuFileIndexEntry> ParseSourceIndexFile(string path)
            => ParseFileIndex(path, false);

        public static List<PcHuoYueDuFileIndexEntry> ParseConfigIndexFile(string path)
            => ParseFileIndex(path, true);

        public const string MainFile = ActivityConfigFileName;

        public static List<PcHuoYueDuEntry> ParseFile(string path)
        {
            var rows = new List<PcHuoYueDuEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            bool headerSkipped = false;
            foreach (var raw in PcItemCommon.ReadServerLines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = raw.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcHuoYueDuEntry
                {
                    activityId = PcItemCommon.Int(cols, 0),
                    nameRaw = PcItemCommon.Str(cols, 1),
                    type = InferType(cols),
                    dailyLimit = cols.Length > 3 ? PcItemCommon.Int(cols, 3) : 0,
                    scoreReward = cols.Length > 4 ? PcItemCommon.Int(cols, 4) : 0,
                    expReward = cols.Length > 5 ? PcItemCommon.Int(cols, 5) : 0,
                    weekReset = cols.Length > 14 ? PcItemCommon.Int(cols, 14) : 0,
                });
            }
            return rows;
        }

        public static PcHuoYueDuRegistry BuildRegistry(string dir)
        {
            var reg = new PcHuoYueDuRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, MainFile);
            if (File.Exists(main)) foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }

        public static List<PcHuoYueDuActivityEntry> ParseActivityConfigFile(string path)
        {
            var rows = new List<PcHuoYueDuActivityEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            bool headerSkipped = false;
            foreach (var raw in PcItemCommon.ReadServerLines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = raw.Split('\t');
                if (cols.Length < 15) continue;
                var entry = new PcHuoYueDuActivityEntry
                {
                    activityId = Int(cols, 0),
                    activityName = Str(cols, 1),
                    countTask = Int(cols, 2),
                    maxCount = Int(cols, 3),
                    weekResetFlag = Int(cols, 14),
                    parameters = new int[10],
                };
                for (int i = 0; i < entry.parameters.Length; i++) entry.parameters[i] = Int(cols, 4 + i);
                if (entry.activityId > 0) rows.Add(entry);
            }
            return rows;
        }

        public static PcHuoYueDuIndexRegistry BuildIndexRegistry(string dir)
        {
            var registry = new PcHuoYueDuIndexRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return registry;
            foreach (var entry in ParseSourceIndexFile(Path.Combine(dir, SourceIndexFileName))) registry.RegisterFile(entry);
            foreach (var entry in ParseConfigIndexFile(Path.Combine(dir, ConfigIndexFileName))) registry.RegisterFile(entry);
            foreach (var entry in ParseActivityConfigFile(Path.Combine(dir, ActivityConfigFileName))) registry.RegisterActivity(entry);
            return registry;
        }

        private static List<PcHuoYueDuFileIndexEntry> ParseFileIndex(string path, bool isConfig)
        {
            var rows = new List<PcHuoYueDuFileIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= (isConfig ? ConfigSha256Col : SourceSha256Col)) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcHuoYueDuFileIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    sourceRoot = Str(cols, SourceRootCol),
                    relativePath = Str(cols, RelativePathCol),
                    directory = Str(cols, DirectoryCol),
                    fileName = Str(cols, FileNameCol),
                    extension = Str(cols, ExtensionCol),
                    isLua = !isConfig && Bool(cols, IsLuaCol),
                    isConfig = isConfig,
                    sizeBytes = Long(cols, isConfig ? ConfigSizeBytesCol : SourceSizeBytesCol),
                    lineCount = Int(cols, isConfig ? ConfigLineCountCol : SourceLineCountCol),
                    dataRows = isConfig ? Int(cols, ConfigDataRowsCol) : 0,
                    sha256 = Str(cols, isConfig ? ConfigSha256Col : SourceSha256Col),
                    headerColumns = isConfig ? Str(cols, ConfigHeaderColumnsCol) : string.Empty,
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        private static int InferType(string[] cols)
        {
            int id = PcItemCommon.Int(cols, 0);
            if (id >= 1 && id <= 41) return id - 1;
            return id;
        }

        private static List<string> ReadUtf8Lines(string path)
        {
            var text = File.ReadAllText(path, Encoding.UTF8).TrimStart('\ufeff');
            return new List<string>(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
        }

        private static string Str(string[] cols, int i)
            => cols != null && i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;

        private static int Int(string[] cols, int i)
            => int.TryParse(Str(cols, i), out var value) ? value : 0;

        private static long Long(string[] cols, int i)
            => long.TryParse(Str(cols, i), out var value) ? value : 0L;

        private static bool Bool(string[] cols, int i)
        {
            var value = Str(cols, i);
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }

    [System.Serializable]
    public class PcHuoYueDuEntry
    {
        public int activityId;
        public string nameRaw;
        public int type;
        public int dailyLimit;
        public int scoreReward;
        public int expReward;
        public int weekReset;
    }

    public sealed class PcHuoYueDuRegistry
    {
        private readonly Dictionary<int, PcHuoYueDuEntry> _byId = new Dictionary<int, PcHuoYueDuEntry>();
        private readonly Dictionary<int, List<PcHuoYueDuEntry>> _byType = new Dictionary<int, List<PcHuoYueDuEntry>>();
        public int Count => _byId.Count;
        public IEnumerable<PcHuoYueDuEntry> All => _byId.Values;

        public void Register(PcHuoYueDuEntry e)
        {
            if (e == null || e.activityId <= 0) return;
            _byId[e.activityId] = e;
            if (!_byType.TryGetValue(e.type, out var list))
            {
                list = new List<PcHuoYueDuEntry>();
                _byType[e.type] = list;
            }
            list.Add(e);
        }

        public PcHuoYueDuEntry Get(int activityId)
            => _byId.TryGetValue(activityId, out var v) ? v : null;

        public IReadOnlyList<PcHuoYueDuEntry> GetByType(int type)
            => _byType.TryGetValue(type, out var v)
                ? (IReadOnlyList<PcHuoYueDuEntry>)v
                : Array.Empty<PcHuoYueDuEntry>();
    }

}
