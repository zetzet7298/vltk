// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/playertitle.txt player title parser
// Source: playertitle.txt (444 entries, GB2312, 18 tab columns).
//   TitleName  TitleId  SpeicalGraphic  FaceId  AuraSkill  AuraSkillLevel  ...
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcPlayerTitleParser
    {
        public const int NameCol = 0;
        public const int TitleIdCol = 1;
        public const int SpecialGraphicCol = 2;
        public const int FaceIdCol = 3;
        public const int AuraSkillCol = 4;
        public const int AuraLevelCol = 5;

        public static List<PcPlayerTitleEntry> ParseFile(string path)
        {
            var rows = new List<PcPlayerTitleEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, TitleIdCol);
                if (id <= 0) continue;
                rows.Add(new PcPlayerTitleEntry
                {
                    titleId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    specialGraphic = PcItemCommon.Str(cols, SpecialGraphicCol),
                    faceId = PcItemCommon.Int(cols, FaceIdCol),
                    auraSkill = PcItemCommon.Int(cols, AuraSkillCol),
                    auraLevel = PcItemCommon.Int(cols, AuraLevelCol),
                });
            }
            return rows;
        }

        public static PcPlayerTitleRegistry BuildRegistry(string dir)
        {
            var reg = new PcPlayerTitleRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcPlayerTitleEntry
    {
        public int titleId;
        public string nameRaw;
        public string specialGraphic;
        public int faceId;
        public int auraSkill;
        public int auraLevel;
    }

    public sealed class PcPlayerTitleRegistry
    {
        private readonly Dictionary<int, PcPlayerTitleEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcPlayerTitleEntry e) { if (e == null || e.titleId <= 0) return; _byId[e.titleId] = e; }
        public PcPlayerTitleEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
    }
}
