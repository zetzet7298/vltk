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
            var lines = ReadLines(absolutePath, encoding);
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
