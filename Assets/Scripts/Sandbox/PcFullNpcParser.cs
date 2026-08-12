// -----------------------------------------------------------------------------
// VLTK Mobile — PC full npcs.txt port
// Source: server settings/npcs.txt (2000 rows, tab-separated, GB2312 server /
// UTF-8 client). Header is ASCII column names, body rows hold Chinese
// (server) or TCVN3/Vietnamese (client) names that do not round-trip through
// GB2312 cleanly; the parser preserves raw bytes and only normalises whitespace.
// Fills the full ~100 column schema into VLTK.Model.NpcTemplate.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Tab-separated npcs.txt row. Field names mirror the PC header so audit
    /// and parity scripts can compare against the source without translation.
    /// </summary>
    public sealed class PcFullNpcRow
    {
        public int rowIndex;

        public string nameRaw;
        public string nameNormalized;
        public int kind;
        public int camp;
        public int series;
        public int treasure;
        public string headImage;
        public int clientOnly;
        public int corpseIdx;
        public int redLum;
        public int greenLum;
        public int blueLum;
        public string npcResType;
        public int armorType;
        public int helmType;
        public int weaponType;
        public int horseType;
        public int rideHorse;
        public int standFrame;
        public int standFrame1;
        public int deathFrame;
        public int walkFrame;
        public int runFrame;
        public int hurtFrame;

        public int skill1;
        public int skill1Level;
        public int skill2;
        public int skill2Level;
        public int skill3;
        public int skill3Level;
        public int skill4;
        public int skill4Level;

        public string actionScript;
        public string levelScript;

        public int expParam;
        public int expParam1;
        public int expParam2;
        public int expParam3;
        public int lifeParam;
        public int lifeParam1;
        public int lifeParam2;
        public int lifeParam3;
        public int lifeReplenish;

        public int arParam;
        public int arParam1;
        public int arParam2;
        public int arParam3;
        public int defenseParam;
        public int defenseParam1;
        public int defenseParam2;
        public int defenseParam3;

        public int minDamageParam;
        public int minDamageParam1;
        public int minDamageParam2;
        public int minDamageParam3;
        public int maxDamageParam;
        public int maxDamageParam1;
        public int maxDamageParam2;
        public int maxDamageParam3;

        public int walkSpeed;
        public int runSpeed;
        public int attackSpeed;
        public int castSpeed;
        public int visionRadius;
        public int hitRecover;
        public int activeRadius;
        public int aiMode;
        public readonly int[] aiParams = new int[9];

        public int fireResist;
        public int coldResist;
        public int lightResist;
        public int poisonResist;
        public int physicsResist;
        public int fireResistMax;
        public int coldResistMax;
        public int lightResistMax;
        public int poisonResistMax;
        public int physicsResistMax;

        public int reviveFrame;
        public int stature;
        public string dropRateFile;
        public int aiMaxTime;

        public int physicalDamageBase;
        public int physicalMagicBase;
        public int poisonDamageBase;
        public int poisonMagicBase;
        public int coldDamageBase;
        public int coldMagicBase;
        public int fireDamageBase;
        public int fireMagicBase;
        public int lightingDamageBase;
        public int lightingMagicBase;

        public int auraSkillId;
        public int auraSkillLevel;
        public int passiveSkillId;
        public int passiveSkillLevel;

        public readonly List<string> warnings = new();
    }

    /// <summary>
    /// Parses PC npcs.txt into NpcTemplate entries. The PC format is dense
    /// (100+ columns) and the body rows are usually GB2312/Chinese on server
    /// builds and TCVN3/Vietnamese on client builds. Both encodings are
    /// accepted via the encoding parameter; auto-detect falls back from
    /// GB2312 to UTF-8 when too many replacement characters appear.
    /// </summary>
    public static class PcFullNpcParser
    {
        public const int MinColumns = 80;
        public const int MaxAiParams = 9;

        private static readonly Dictionary<int, string> VietnameseNameOverrides = new()
        {
            [31] = "Mèo vàng",
            [42] = "Hươu đốm",
            [43] = "Heo trắng",
        };

        public static List<NpcTemplate> ParseFile(string absolutePath, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<NpcTemplate>();
            var lines = encoding == null
                ? ReadNpcLinesWithLegacyVietnameseNames(absolutePath)
                : ReadLines(absolutePath, encoding);
            return ParseLines(lines);
        }

        public static List<NpcTemplate> ParseLines(IEnumerable<string> lines)
        {
            var result = new List<NpcTemplate>();
            if (lines == null) return result;
            bool headerSkipped = false;
            int rowIndex = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                var row = ParseRow(cols);
                row.rowIndex = rowIndex;
                if (cols.Length < MinColumns)
                    row.warnings.Add($"Row has {cols.Length} columns, expected >= {MinColumns}");
                result.Add(BuildTemplate(rowIndex, row));
                rowIndex++;
            }
            return result;
        }

        public static PcFullNpcRow ParseRow(string[] cols)
        {
            var row = new PcFullNpcRow();
            int ci = 0;
            row.nameRaw = Str(cols, ci++);
            row.kind = Int(cols, ci++);
            row.camp = Int(cols, ci++);
            row.series = Int(cols, ci++);
            row.treasure = Int(cols, ci++);
            row.headImage = Str(cols, ci++);
            row.clientOnly = Int(cols, ci++);
            row.corpseIdx = Int(cols, ci++);
            row.redLum = Int(cols, ci++);
            row.greenLum = Int(cols, ci++);
            row.blueLum = Int(cols, ci++);
            row.npcResType = Str(cols, ci++);
            row.armorType = Int(cols, ci++);
            row.helmType = Int(cols, ci++);
            row.weaponType = Int(cols, ci++);
            row.horseType = Int(cols, ci++);
            row.rideHorse = Int(cols, ci++);
            row.standFrame = Int(cols, ci++);
            row.standFrame1 = Int(cols, ci++);
            row.deathFrame = Int(cols, ci++);
            row.walkFrame = Int(cols, ci++);
            row.runFrame = Int(cols, ci++);
            row.hurtFrame = Int(cols, ci++);
            row.skill1 = Int(cols, ci++);
            row.skill1Level = Int(cols, ci++);
            row.skill2 = Int(cols, ci++);
            row.skill2Level = Int(cols, ci++);
            row.skill3 = Int(cols, ci++);
            row.skill3Level = Int(cols, ci++);
            row.skill4 = Int(cols, ci++);
            row.skill4Level = Int(cols, ci++);
            row.actionScript = Str(cols, ci++);
            row.levelScript = Str(cols, ci++);
            row.expParam = Int(cols, ci++);
            row.expParam1 = Int(cols, ci++);
            row.expParam2 = Int(cols, ci++);
            row.expParam3 = Int(cols, ci++);
            row.lifeParam = Int(cols, ci++);
            row.lifeParam1 = Int(cols, ci++);
            row.lifeParam2 = Int(cols, ci++);
            row.lifeParam3 = Int(cols, ci++);
            row.lifeReplenish = Int(cols, ci++);
            row.arParam = Int(cols, ci++);
            row.arParam1 = Int(cols, ci++);
            row.arParam2 = Int(cols, ci++);
            row.arParam3 = Int(cols, ci++);
            row.defenseParam = Int(cols, ci++);
            row.defenseParam1 = Int(cols, ci++);
            row.defenseParam2 = Int(cols, ci++);
            row.defenseParam3 = Int(cols, ci++);
            row.minDamageParam = Int(cols, ci++);
            row.minDamageParam1 = Int(cols, ci++);
            row.minDamageParam2 = Int(cols, ci++);
            row.minDamageParam3 = Int(cols, ci++);
            row.maxDamageParam = Int(cols, ci++);
            row.maxDamageParam1 = Int(cols, ci++);
            row.maxDamageParam2 = Int(cols, ci++);
            row.maxDamageParam3 = Int(cols, ci++);
            row.walkSpeed = Int(cols, ci++);
            row.runSpeed = Int(cols, ci++);
            row.attackSpeed = Int(cols, ci++);
            row.castSpeed = Int(cols, ci++);
            row.visionRadius = Int(cols, ci++);
            row.hitRecover = Int(cols, ci++);
            row.activeRadius = Int(cols, ci++);
            row.aiMode = Int(cols, ci++);
            for (int j = 0; j < MaxAiParams; j++)
                row.aiParams[j] = Int(cols, ci++);
            row.fireResist = Int(cols, ci++);
            row.coldResist = Int(cols, ci++);
            row.lightResist = Int(cols, ci++);
            row.poisonResist = Int(cols, ci++);
            row.physicsResist = Int(cols, ci++);
            row.fireResistMax = Int(cols, ci++);
            row.coldResistMax = Int(cols, ci++);
            row.lightResistMax = Int(cols, ci++);
            row.poisonResistMax = Int(cols, ci++);
            row.physicsResistMax = Int(cols, ci++);
            row.reviveFrame = Int(cols, ci++);
            row.stature = Int(cols, ci++);
            row.dropRateFile = Str(cols, ci++);
            row.aiMaxTime = Int(cols, ci++);
            row.physicalDamageBase = Int(cols, ci++);
            row.physicalMagicBase = Int(cols, ci++);
            row.poisonDamageBase = Int(cols, ci++);
            row.poisonMagicBase = Int(cols, ci++);
            row.coldDamageBase = Int(cols, ci++);
            row.coldMagicBase = Int(cols, ci++);
            row.fireDamageBase = Int(cols, ci++);
            row.fireMagicBase = Int(cols, ci++);
            row.lightingDamageBase = Int(cols, ci++);
            row.lightingMagicBase = Int(cols, ci++);
            row.auraSkillId = Int(cols, ci++);
            row.auraSkillLevel = Int(cols, ci++);
            row.passiveSkillId = Int(cols, ci++);
            row.passiveSkillLevel = Int(cols, ci++);

            if (row.actionScript == "-1") row.actionScript = string.Empty;
            if (row.levelScript == "-1") row.levelScript = string.Empty;
            if (row.dropRateFile == "-1") row.dropRateFile = string.Empty;
            return row;
        }

        public static int BuildRegistry(NpcTemplateRegistry registry, string absolutePath, Encoding encoding = null)
        {
            if (registry == null) return 0;
            var templates = ParseFile(absolutePath, encoding);
            int count = 0;
            foreach (var t in templates)
            {
                registry.Register(t);
                count++;
            }
            return count;
        }

        private static NpcTemplate BuildTemplate(int templateId, PcFullNpcRow row)
        {
            return new NpcTemplate
            {
                templateId = templateId,
                nameRaw = row.nameRaw?.Trim() ?? string.Empty,
                nameNormalized = NormalizeName(templateId, row.nameRaw),
                maxLife = row.lifeParam > 0 ? row.lifeParam : row.lifeParam1,
                attack = row.arParam > 0 ? row.arParam : row.arParam1,
                defense = row.defenseParam > 0 ? row.defenseParam : row.defenseParam1,
                kind = row.kind,
                series = row.series,
                walkSpeed = row.walkSpeed,
                runSpeed = row.runSpeed,
                visionRadius = row.visionRadius,
                activeRadius = row.activeRadius,
                aiMode = row.aiMode,
                aiParams = (int[])row.aiParams.Clone(),
                spriteClipRef = row.npcResType,
                // Preserve both PC scripts: action/AI goes in scriptRef, the
                // level-up event script lives in levelScriptRef so neither is
                // silently dropped when both are populated.
                scriptRef = string.IsNullOrEmpty(row.actionScript) ? null : row.actionScript,
                levelScriptRef = string.IsNullOrEmpty(row.levelScript) ? null : row.levelScript,
                warnings = new List<string>(row.warnings),
            };
        }

        private static string NormalizeName(int templateId, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var trimmed = raw.Trim();
            if (VietnameseNameOverrides.TryGetValue(templateId, out var vname))
                return vname;
            return trimmed;
        }


        // PC server settings/npcs.txt in 00.src-tinh-kiem is a mixed legacy file:
        // tab/numeric columns are ASCII-compatible, most NPC names are TCVN3
        // Vietnamese bytes, and a small tail still uses GB2312 Chinese names.
        // Decode per name field instead of applying one whole-file codec.
        private static string[] ReadNpcLinesWithLegacyVietnameseNames(string path)
        {
            var raw = File.ReadAllBytes(path);
            var strictUtf8 = new UTF8Encoding(false, true);
            try { return SplitLines(strictUtf8.GetString(raw)); }
            catch { /* legacy file, continue below */ }

            var rawLines = SplitRawLines(raw);
            var lines = new string[rawLines.Count];
            for (int i = 0; i < rawLines.Count; i++)
            {
                var fields = SplitRawTabs(rawLines[i]);
                if (fields.Count == 0) { lines[i] = string.Empty; continue; }
                if (i == 0)
                {
                    lines[i] = DecodeAsciiCompatible(rawLines[i]);
                    continue;
                }

                var decoded = new string[fields.Count];
                decoded[0] = DecodeLegacyNpcName(fields[0]);
                for (int f = 1; f < fields.Count; f++)
                    decoded[f] = DecodeAsciiCompatible(fields[f]);
                lines[i] = string.Join("\t", decoded);
            }
            return lines;
        }

        private static List<byte[]> SplitRawLines(byte[] raw)
        {
            var result = new List<byte[]>();
            int start = 0;
            for (int i = 0; i <= raw.Length; i++)
            {
                if (i != raw.Length && raw[i] != (byte)'\n') continue;
                int len = i - start;
                if (len > 0 && raw[start + len - 1] == (byte)'\r') len--;
                var line = new byte[len];
                Buffer.BlockCopy(raw, start, line, 0, len);
                result.Add(line);
                start = i + 1;
            }
            return result;
        }

        private static List<byte[]> SplitRawTabs(byte[] rawLine)
        {
            var result = new List<byte[]>();
            int start = 0;
            for (int i = 0; i <= rawLine.Length; i++)
            {
                if (i != rawLine.Length && rawLine[i] != (byte)'\t') continue;
                int len = i - start;
                var field = new byte[len];
                Buffer.BlockCopy(rawLine, start, field, 0, len);
                result.Add(field);
                start = i + 1;
            }
            return result;
        }

        private static string DecodeLegacyNpcName(byte[] rawName)
        {
            if (rawName == null || rawName.Length == 0) return string.Empty;
            string vietnamese = Tcvn3ToUnicode(rawName).Trim();
            string gbStrict = DecodeGb2312(rawName, strict: true).Trim();
            bool preferGb = !string.IsNullOrEmpty(gbStrict)
                && CountCjk(gbStrict) >= 2
                && (ContainsLegacyArtifacts(vietnamese) || CountAsciiLetters(vietnamese) == 0);
            return preferGb ? gbStrict : vietnamese;
        }

        private static string DecodeAsciiCompatible(byte[] raw)
        {
            if (raw == null || raw.Length == 0) return string.Empty;
            return Encoding.ASCII.GetString(raw).Trim();
        }

        private static string DecodeGb2312(byte[] raw, bool strict)
        {
            try
            {
                var enc = strict
                    ? Encoding.GetEncoding("GB2312", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)
                    : Encoding.GetEncoding("GB2312");
                return enc.GetString(raw);
            }
            catch { return string.Empty; }
        }

        private static string Tcvn3ToUnicode(byte[] raw)
        {
            var sb = new StringBuilder(raw.Length);
            foreach (byte b in raw)
                sb.Append(char.ConvertFromUtf32(Tcvn3Table[b]));
            return sb.ToString();
        }

        private static int CountCjk(string text)
        {
            int count = 0;
            if (string.IsNullOrEmpty(text)) return 0;
            foreach (char ch in text)
                if (ch >= '\u4e00' && ch <= '\u9fff') count++;
            return count;
        }

        private static int CountAsciiLetters(string text)
        {
            int count = 0;
            if (string.IsNullOrEmpty(text)) return 0;
            foreach (char ch in text)
                if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')) count++;
            return count;
        }

        private static bool ContainsLegacyArtifacts(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            const string bad = "ÅÆÇÐÑÒÓÕÖ×ØÙÚÛÜÝÞß¶·¸¹º»¼½¾¿±²³¤¥¦§¨©ª«¬®¯°µ";
            foreach (char ch in text)
                if (ch == '\ufffd' || ch == '?' || bad.IndexOf(ch) >= 0) return true;
            return false;
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
            if (text.Length > 0 && text[0] == '\ufeff') text = text.Substring(1);
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private static readonly int[] Tcvn3Table = new[]
        {
            0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
            32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
            64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
            96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
            128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,
            160,258,194,202,212,416,431,272,259,226,234,244,417,432,273,175,176,177,178,179,180,224,7843,227,225,7841,186,7857,7859,7861,7855,191,
            192,193,194,195,196,197,7863,7847,7849,7851,7845,7853,232,205,7867,7869,233,7865,7873,7875,7877,7871,7879,236,7881,217,218,219,
            297,237,7883,242,224,7887,245,243,7885,7891,7893,7895,7889,7897,7901,7903,7905,7899,7907,249,240,7911,361,250,7909,7915,7917,7919,7913,7921,7923,7927,7929,253,7925,255
        };

        private static string[] ReadLines(string path, Encoding encoding)
        {
            return PcText.ReadLines(path, encoding);
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
