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
        public const int MemoCol = 16;       // PC playertitle.txt col 16 = Memo
        public const int TitlePriorityCol = 17; // PC playertitle.txt col 17 = TitlePriority

        public static List<PcPlayerTitleEntry> ParseFile(string path)
        {
            var rows = new List<PcPlayerTitleEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            // playertitle.txt is a Vietnamese-localized TCVN3 file (Western ANSI bytes
            // whose high chars are TCVN3 glyph codes). It must NOT go through the
            // auto-detecting DecodeBest() path (PcItemCommon.ReadServerLines -> PcText
            // .ReadLines(path, null)): in the REAL Editor that scorer is biased toward
            // GB2312/hanzi and mis-decodes ~28 rows whose Vietnamese high byte is
            // immediately followed by a 0x09 TAB. The .NET GB2312 decoder treats
            // <leadByte><0x09> as a single 2-byte unit and SWALLOWS the tab -> column
            // shift -> TitleId reads garbage -> rows dropped (363 collapses to 335) and
            // the surviving names render as mojibake. ReadLinesTcvn3 forces windows-1252
            // + TCVN3 with no auto-detect, so every tab survives and all 363 titles load
            // with clean Vietnamese names. (Same GBK<->TCVN3 mojibake family as the
            // accepted meridian fix, commit ecd0b2294 / PcMeridianParser.)
            var lines = PcText.ReadLinesTcvn3(path);
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
                    memo = PcItemCommon.Str(cols, MemoCol),
                    titlePriority = PcItemCommon.Int(cols, TitlePriorityCol),
                });
            }
            return rows;
        }

        public static PcPlayerTitleRegistry BuildRegistry(string dir)
        {
            var reg = new PcPlayerTitleRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // PC player titles live only in playertitle.txt (363 rows). Do NOT scan
            // every *.txt — factiontitle.txt shares the directory and must stay
            // owned by PcFactionTitleParser.
            string main = Path.Combine(dir, "playertitle.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
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
        public string memo;
        public int titlePriority;
    }

    public sealed class PcPlayerTitleRegistry
    {
        private readonly Dictionary<int, PcPlayerTitleEntry> _byId = new();
        private readonly List<PcPlayerTitleEntry> _ordered = new();
        public int Count => _byId.Count;
        public void Register(PcPlayerTitleEntry e)
        {
            if (e == null || e.titleId <= 0) return;
            if (!_byId.ContainsKey(e.titleId)) _ordered.Add(e);
            _byId[e.titleId] = e;
        }
        public PcPlayerTitleEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        /// <summary>Toàn bộ danh hiệu theo thứ tự PC (thứ tự dòng trong playertitle.txt).</summary>
        public IReadOnlyList<PcPlayerTitleEntry> All => _ordered;
    }
}
