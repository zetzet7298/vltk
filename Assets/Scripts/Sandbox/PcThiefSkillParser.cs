// -----------------------------------------------------------------------------
// VLTK Mobile — PC thiefskill.txt focused parser/service data.
// Source: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/thiefskill.txt
// Columns: SkillId SkillName ThiefStyle AttackRadius MaxLevel TimePerCast ...
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcThiefSkillParser
    {
        public const int SkillIdCol = 0;
        public const int SkillNameCol = 1;
        public const int ThiefStyleCol = 2;
        public const int AttackRadiusCol = 3;
        public const int MaxLevelCol = 4;
        public const int TimePerCastCol = 5;
        public const int ThiefPercentCol = 6;
        public const int SkillCostTypeCol = 7;
        public const int Param1Col = 8;
        public const int Param2Col = 9;
        public const int MovieCol = 10;
        public const int TargetMovieInfoCol = 11;
        public const int SkillSoundCol = 12;
        public const int TargetMovieCol = 13;
        public const int SkillIconCol = 14;
        public const int CostCol = 15;
        public const int DescCol = 16;

        public static List<PcThiefSkillEntry> ParseFile(string path)
        {
            var rows = new List<PcThiefSkillEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            // thiefskill.txt is mixed-encoding: name cols are TCVN3, SPR/sound/icon cols are GBK.
            // Read raw bytes and decode each column with the correct encoding.
            RegisterCodePages();
            var gbk = System.Text.Encoding.GetEncoding("GB18030");
            var lines = PcText.ReadLinesTcvn3(path);
            var rawLines = File.ReadAllBytes(path);
            bool headerSkipped = false;
            int rawOffset = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; rawOffset = SkipRawLine(rawLines, rawOffset); continue; }

                // Decode SPR columns from raw bytes (GB18030)
                var cols = line.Split('\t');
                var rawCols = SplitRawLine(rawLines, ref rawOffset);
                if (cols.Length <= SkillIdCol) { rawOffset = SkipRawLine(rawLines, rawOffset); continue; }
                int skillId = PcItemCommon.Int(cols, SkillIdCol);
                if (skillId <= 0) continue;

                rows.Add(new PcThiefSkillEntry
                {
                    skillId = skillId,
                    skillName = PcItemCommon.Str(cols, SkillNameCol),
                    thiefStyle = PcItemCommon.Int(cols, ThiefStyleCol),
                    attackRadius = PcItemCommon.Int(cols, AttackRadiusCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    timePerCast = PcItemCommon.Int(cols, TimePerCastCol),
                    thiefPercent = PcItemCommon.Int(cols, ThiefPercentCol),
                    skillCostType = PcItemCommon.Int(cols, SkillCostTypeCol),
                    param1 = PcItemCommon.Int(cols, Param1Col),
                    param2 = PcItemCommon.Int(cols, Param2Col),
                    movie = DecodeGbkCol(rawCols, MovieCol, gbk) ?? PcItemCommon.Str(cols, MovieCol),
                    targetMovieInfo = PcItemCommon.Str(cols, TargetMovieInfoCol),
                    skillSound = DecodeGbkCol(rawCols, SkillSoundCol, gbk) ?? PcItemCommon.Str(cols, SkillSoundCol),
                    targetMovie = DecodeGbkCol(rawCols, TargetMovieCol, gbk) ?? PcItemCommon.Str(cols, TargetMovieCol),
                    skillIcon = DecodeGbkCol(rawCols, SkillIconCol, gbk) ?? PcItemCommon.Str(cols, SkillIconCol),
                    cost = PcItemCommon.Int(cols, CostCol),
                    desc = PcItemCommon.Str(cols, DescCol),
                });
            }
            return rows;
        }

        private static void RegisterCodePages()
        {
            try
            {
                var pt = System.Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                var prov = pt?.GetProperty("Instance")?.GetValue(null, null) as System.Text.EncodingProvider;
                if (prov != null) System.Text.Encoding.RegisterProvider(prov);
            }
            catch { }
        }

        private static int SkipRawLine(byte[] data, int offset)
        {
            while (offset < data.Length && data[offset] != (byte)'\n') offset++;
            return offset < data.Length ? offset + 1 : data.Length;
        }

        private static byte[][] SplitRawLine(byte[] data, ref int offset)
        {
            var result = new List<byte[]>();
            int start = offset;
            while (offset < data.Length)
            {
                if (data[offset] == (byte)'\t')
                {
                    result.Add(data[start..offset]);
                    start = offset + 1;
                    offset++;
                }
                else if (data[offset] == (byte)'\r' || data[offset] == (byte)'\n')
                {
                    break;
                }
                else
                {
                    offset++;
                }
            }
            result.Add(data[start..(offset < data.Length ? offset : data.Length)]);
            // advance past line ending
            while (offset < data.Length && (data[offset] == (byte)'\r' || data[offset] == (byte)'\n')) offset++;
            return result.ToArray();
        }

        private static string DecodeGbkCol(byte[][] rawCols, int col, System.Text.Encoding gbk)
        {
            if (col >= rawCols.Length) return null;
            var bytes = rawCols[col];
            if (bytes == null || bytes.Length == 0) return null;
            try
            {
                var decoded = gbk.GetString(bytes);
                return string.IsNullOrEmpty(decoded) ? null : decoded;
            }
            catch { return null; }
        }

        public static PcThiefSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcThiefSkillRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string path = Path.Combine(dir, "thiefskill.txt");
            foreach (var entry in ParseFile(path)) reg.Register(entry);
            reg.LinkSkillScripts(Path.Combine(dir, "skills.txt"));
            return reg;
        }
    }

    [System.Serializable]
    public class PcThiefSkillEntry
    {
        public int skillId;
        public string skillName;
        public int thiefStyle;
        public int attackRadius;
        public int maxLevel;
        public int timePerCast;
        public int thiefPercent;
        public int skillCostType;
        public int param1;
        public int param2;
        public string movie;
        public string targetMovieInfo;
        public string skillSound;
        public string targetMovie;
        public string skillIcon;
        public int cost;
        public string desc;
        public string lvlSetScript;
    }

    public sealed class PcThiefSkillRegistry
    {
        private readonly Dictionary<int, PcThiefSkillEntry> _bySkillId = new();
        private readonly Dictionary<int, PcThiefSkillEntry> _byThiefStyle = new();
        public int Count => _bySkillId.Count;
        public IReadOnlyList<PcThiefSkillEntry> All => new List<PcThiefSkillEntry>(_bySkillId.Values);
        public void Register(PcThiefSkillEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            _bySkillId[e.skillId] = e;
            _byThiefStyle[e.thiefStyle] = e;
        }
        public PcThiefSkillEntry Get(int skillId) => _bySkillId.TryGetValue(skillId, out var v) ? v : null;
        public PcThiefSkillEntry GetByThiefStyle(int thiefStyle) => _byThiefStyle.TryGetValue(thiefStyle, out var v) ? v : null;
        public void LinkSkillScripts(string skillsTxtPath)
        {
            var scripts = PcSkillSourceLinkParser.ParseSkillScripts(skillsTxtPath);
            foreach (var entry in _bySkillId.Values)
                if (scripts.TryGetValue(entry.skillId, out var script)) entry.lvlSetScript = script;
        }
    }
}
