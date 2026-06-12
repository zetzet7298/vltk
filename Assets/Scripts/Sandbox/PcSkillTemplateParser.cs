// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/skilltemplate.txt schema parser.
// Source: vl_update_27/Server 6.0/server/home_jxser/server1/settings/skilltemplate.txt
// This PC file is an INI-like skill table field template, not a 219-row data table.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkillTemplateParser
    {
        public const string PcSourceRelativePath = "Server 6.0/server/home_jxser/server1/settings/skilltemplate.txt";
        public const int PcFieldTemplateCount = 67;

        public const int TemplateIdCol = 0;
        public const int TemplateNameCol = 1;
        public const int MissleIdCol = 2;
        public const int EffectTypeCol = 3;
        public const int DurationCol = 4;
        public const int PeriodMsCol = 5;
        public const int MaxStacksCol = 6;

        public static List<PcSkillTemplateField> ParseTemplateFile(string path)
        {
            return ParseTemplateLines(ReadRawLines(path));
        }

        public static List<PcSkillTemplateField> ParseTemplateLines(IReadOnlyList<string> lines)
        {
            var result = new List<PcSkillTemplateField>();
            if (lines == null) return result;

            PcSkillTemplateField current = null;
            for (int i = 0; i < lines.Count; i++)
            {
                var raw = lines[i] ?? string.Empty;
                var text = raw.Trim();
                if (text.Length == 0) continue;

                if (text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal))
                {
                    current = new PcSkillTemplateField
                    {
                        fieldName = text.Substring(1, text.Length - 2).Trim(),
                        sourceLine = i + 1,
                    };
                    if (current.fieldName.Length > 0) result.Add(current);
                    continue;
                }

                if (current == null) continue;
                int eq = raw.IndexOf('=');
                if (eq < 0) continue;

                var key = raw.Substring(0, eq).Trim();
                var value = raw.Substring(eq + 1).Trim();
                var prop = new PcSkillTemplateProperty
                {
                    key = key,
                    valueRaw = value,
                    rawLine = raw,
                    sourceLine = i + 1,
                };
                current.properties.Add(prop);
                current.SetKnownProperty(prop);
            }
            return result;
        }

        public static PcSkillTemplateCatalog BuildCatalog(string dir)
        {
            var catalog = new PcSkillTemplateCatalog();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return catalog;
            var path = Path.Combine(dir, "skilltemplate.txt");
            if (!File.Exists(path)) return catalog;
            foreach (var field in ParseTemplateFile(path)) catalog.Register(field);
            catalog.totalLineCount = CountLines(path);
            catalog.nonEmptyLineCount = CountNonEmptyLines(path);
            return catalog;
        }

        public static List<string> ReadRawLines(string path)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;
            var bytes = File.ReadAllBytes(path);
            var chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++) chars[i] = (char)bytes[i];
            var normalized = new string(chars).Replace("\r\n", "\n").Replace('\r', '\n');
            result.AddRange(normalized.Split('\n'));
            if (result.Count > 0 && result[result.Count - 1].Length == 0) result.RemoveAt(result.Count - 1);
            return result;
        }

        private static int CountLines(string path) => ReadRawLines(path).Count;

        private static int CountNonEmptyLines(string path)
        {
            int count = 0;
            foreach (var line in ReadRawLines(path)) if (!string.IsNullOrWhiteSpace(line)) count++;
            return count;
        }

        // Legacy tab-table API kept so old SkillTemplateService/tests still compile.
        public static List<PcSkillTemplateEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillTemplateEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcText.ReadLinesTcvn3(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int id = PcItemCommon.Int(cols, 0);
                if (id <= 0) continue;
                rows.Add(new PcSkillTemplateEntry
                {
                    templateId = id,
                    nameRaw = PcItemCommon.Str(cols, 1),
                    missleId = PcItemCommon.Int(cols, 2),
                    effectType = PcItemCommon.Int(cols, 3),
                    duration = cols.Length > 4 ? PcItemCommon.Int(cols, 4) : 0,
                    periodMs = cols.Length > 5 ? PcItemCommon.Int(cols, 5) : 0,
                    maxStacks = cols.Length > 6 ? PcItemCommon.Int(cols, 6) : 0,
                });
            }
            return rows;
        }

        public static PcSkillTemplateRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillTemplateRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [Serializable]
    public class PcSkillTemplateField
    {
        public string fieldName;
        public int sourceLine;
        public string typeRaw;
        public string staticNameRaw;
        public string valueRaw;
        public string defaultValueRaw;
        public readonly List<PcSkillTemplateProperty> properties = new List<PcSkillTemplateProperty>();

        public void SetKnownProperty(PcSkillTemplateProperty prop)
        {
            if (prop == null || string.IsNullOrEmpty(prop.key)) return;
            if (string.Equals(prop.key, "Type", StringComparison.OrdinalIgnoreCase)) typeRaw = prop.valueRaw;
            else if (string.Equals(prop.key, "StaticName", StringComparison.OrdinalIgnoreCase)) staticNameRaw = prop.valueRaw;
            else if (string.Equals(prop.key, "Value", StringComparison.OrdinalIgnoreCase)) valueRaw = prop.valueRaw;
            else if (string.Equals(prop.key, "DefaultValue", StringComparison.OrdinalIgnoreCase)) defaultValueRaw = prop.valueRaw;
        }

        public string GetProperty(string key)
        {
            foreach (var prop in properties)
                if (string.Equals(prop.key, key, StringComparison.OrdinalIgnoreCase)) return prop.valueRaw;
            return null;
        }
    }

    [Serializable]
    public class PcSkillTemplateProperty
    {
        public string key;
        public string valueRaw;
        public string rawLine;
        public int sourceLine;
    }

    public sealed class PcSkillTemplateCatalog
    {
        private readonly List<PcSkillTemplateField> _fields = new List<PcSkillTemplateField>();
        private readonly Dictionary<string, PcSkillTemplateField> _byName =
            new Dictionary<string, PcSkillTemplateField>(StringComparer.OrdinalIgnoreCase);

        public int totalLineCount;
        public int nonEmptyLineCount;
        public int Count => _fields.Count;
        public IReadOnlyList<PcSkillTemplateField> Fields => _fields;

        public void Register(PcSkillTemplateField field)
        {
            if (field == null || string.IsNullOrEmpty(field.fieldName)) return;
            _fields.Add(field);
            _byName[field.fieldName] = field;
        }

        public PcSkillTemplateField GetField(string fieldName)
            => !string.IsNullOrEmpty(fieldName) && _byName.TryGetValue(fieldName, out var value) ? value : null;
    }

    [Serializable]
    public class PcSkillTemplateEntry
    {
        public int templateId;
        public string nameRaw;
        public int missleId;
        public int effectType;
        public int duration;
        public int periodMs;
        public int maxStacks;
    }

    public sealed class PcSkillTemplateRegistry
    {
        private readonly Dictionary<int, PcSkillTemplateEntry> _byId = new Dictionary<int, PcSkillTemplateEntry>();
        public int Count => _byId.Count;
        public void Register(PcSkillTemplateEntry e)
        {
            if (e == null || e.templateId <= 0) return;
            _byId[e.templateId] = e;
        }
        public PcSkillTemplateEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcSkillTemplateEntry> GetAll() => _byId.Values;
    }
}
