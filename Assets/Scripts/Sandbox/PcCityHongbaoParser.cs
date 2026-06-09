// -----------------------------------------------------------------------------
// VLTK Mobile — PC chengshidahongbao.txt parser (hồng bao thành thị đại)
// Source of truth: /var/www/vltksource_new/vl_update_27/*/settings/item/chengshidahongbao.txt
// Cols: Name, Type, Genre, Detail, Particular, Serise, Level, Param1-6, Proba, Costly, Msg, Log
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public class PcCityHongbaoEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Genre { get; set; }
        public int Detail { get; set; }
        public int Particular { get; set; }
        public int Serise { get; set; }
        public int Level { get; set; }
        public int[] Param { get; } = new int[PcCityHongbaoParser.ParamCount];
        public int Proba { get; set; }
        public int Costly { get; set; }
        public string Msg { get; set; } = string.Empty;
        public int Log { get; set; }
    }

    public sealed class PcCityHongbaoRegistry
    {
        private readonly Dictionary<int, PcCityHongbaoEntry> _byId = new Dictionary<int, PcCityHongbaoEntry>();
        public int Count => _byId.Count;
        public int TotalProba { get; private set; }
        public PcCityHongbaoEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcCityHongbaoEntry> All => _byId.Values;

        public void Add(PcCityHongbaoEntry e)
        {
            if (e == null || e.Id <= 0) return;
            if (_byId.TryGetValue(e.Id, out var old)) TotalProba -= old.Proba;
            _byId[e.Id] = e;
            TotalProba += e.Proba;
        }
    }

    public static class PcCityHongbaoParser
    {
        public const string SourceFileName = "chengshidahongbao.txt";
        public const int NameCol = 0;
        public const int TypeCol = 1;
        public const int GenreCol = 2;
        public const int DetailCol = 3;
        public const int ParticularCol = 4;
        public const int SeriseCol = 5;
        public const int LevelCol = 6;
        public const int Param1Col = 7;
        public const int ParamCount = 6;
        public const int ProbaCol = 13;
        public const int CostlyCol = 14;
        public const int MsgCol = 15;
        public const int LogCol = 16;
        public const int ExpectedColumnCount = 17;

        public static PcCityHongbaoRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcCityHongbaoRegistry();
            if (string.IsNullOrEmpty(absoluteDir)) return reg;
            var path = Directory.Exists(absoluteDir) ? Path.Combine(absoluteDir, SourceFileName) : absoluteDir;
            if (!File.Exists(path)) return reg;
            foreach (var entry in ParseFile(path)) reg.Add(entry);
            return reg;
        }

        public static List<PcCityHongbaoEntry> ParseFile(string absolutePath)
        {
            var rows = new List<PcCityHongbaoEntry>();
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath)) return rows;

            var lines = PcMapListParser.ReadLines(absolutePath);
            bool headerSkipped = false;
            int seqId = 0;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var cols = raw.Split('\t');
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    if (PcItemCommon.Str(cols, NameCol).Equals("Name", StringComparison.OrdinalIgnoreCase)) continue;
                }
                var entry = ParseRow(cols, ++seqId);
                if (entry != null) rows.Add(entry);
            }
            return rows;
        }

        private static PcCityHongbaoEntry ParseRow(string[] cols, int seqId)
        {
            if (cols == null || cols.Length < ExpectedColumnCount) return null;
            var name = PcItemCommon.Str(cols, NameCol);
            if (string.IsNullOrEmpty(name)) return null;
            var entry = new PcCityHongbaoEntry
            {
                Id = seqId,
                Name = name,
                Type = PcItemCommon.Int(cols, TypeCol),
                Genre = PcItemCommon.Int(cols, GenreCol),
                Detail = PcItemCommon.Int(cols, DetailCol),
                Particular = PcItemCommon.Int(cols, ParticularCol),
                Serise = PcItemCommon.Int(cols, SeriseCol),
                Level = PcItemCommon.Int(cols, LevelCol),
                Proba = PcItemCommon.Int(cols, ProbaCol),
                Costly = PcItemCommon.Int(cols, CostlyCol),
                Msg = PcItemCommon.Str(cols, MsgCol),
                Log = PcItemCommon.Int(cols, LogCol),
            };
            for (int i = 0; i < ParamCount; i++) entry.Param[i] = PcItemCommon.Int(cols, Param1Col + i);
            return entry;
        }
    }
}
