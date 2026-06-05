// -----------------------------------------------------------------------------
// VLTK Mobile — PC goldboss.txt port
// Source: server settings/goldboss.txt (GB2312). Header is ASCII (Name,
// PhysicalDamageBase..LightingMagic, AuraSkillName, AuraSkillLevel,
// PasstSkillName, PasstSkillLevel). Body rows hold Vietnamese TCVN3 boss
// names and damage-base formulas in the "rate|value" notation. The PC
// schema has no map/coord/drop-rate columns, so those fields default to
// 0/null on every row.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcGoldBossParser
    {
        public const int MinColumns = 4;

        public static List<GoldBossEntry> ParseFile(string absolutePath, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<GoldBossEntry>();
            return ParseLines(PcText.ReadLines(absolutePath, encoding));
        }

        public static List<GoldBossEntry> ParseLines(IEnumerable<string> lines)
        {
            var result = new List<GoldBossEntry>();
            if (lines == null) return result;
            bool headerSkipped = false;
            int rowIndex = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < MinColumns) { rowIndex++; continue; }
                result.Add(BuildEntry(rowIndex, cols));
                rowIndex++;
            }
            return result;
        }

        public static GoldBossEntry ParseRow(string[] cols)
        {
            return BuildEntry(0, cols);
        }

        private static GoldBossEntry BuildEntry(int bossTemplateId, string[] cols)
        {
            var name = Str(cols, 0);
            return new GoldBossEntry
            {
                bossTemplateId = bossTemplateId,
                nameRaw = name,
                nameNormalized = name.Trim(),
                level = 0,
                physicalDamageBase = ParseRate(Str(cols, 1)),
                poisonDamageBase = ParseRate(Str(cols, 3)),
                coldDamageBase = ParseRate(Str(cols, 5)),
                fireDamageBase = ParseRate(Str(cols, 7)),
                lightingDamageBase = ParseRate(Str(cols, 9)),
                auraSkillName = Str(cols, 11),
                auraSkillLevel = Int(cols, 12),
                passiveSkillName = Str(cols, 13),
                passiveSkillLevel = cols.Length > 14 ? Int(cols, 14) : 0,
            };
        }

        private static int ParseRate(string token)
        {
            if (string.IsNullOrEmpty(token)) return 0;
            var pipe = token.IndexOf('|');
            if (pipe < 0)
            {
                if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                    return v;
                return 0;
            }
            var rateStr = token.Substring(0, pipe);
            if (int.TryParse(rateStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rate))
                return rate;
            return 0;
        }

        private static string Str(string[] c, int i) => i >= 0 && i < c.Length ? (c[i] ?? string.Empty).Trim() : string.Empty;
        private static int Int(string[] c, int i)
        {
            var s = Str(c, i);
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            return 0;
        }
    }
}
