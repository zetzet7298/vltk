// -----------------------------------------------------------------------------
// VLTK Mobile — PC itemexchange_setting rolevalue.ini typed facts
// Source of truth: /var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/itemexchange_setting/rolevalue.ini
// Purpose: expose typed config values only; this does not execute item exchange.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    [Serializable]
    public sealed class ItemExchangeRoleValueSummary
    {
        public bool exists;
        public int sectionCount;
        public int keyCount;
        public int skillValue;
        public int jxbServerCount;
        public int minJxbValue;
        public int maxJxbValue;
        public int limitCreateDate;
        public int createDate;
        public bool evaluateLevel;
        public bool evaluateSkill;
        public bool evaluateMoney;
        public bool evaluateItem;
        public bool evaluateTask;
    }

    public sealed class ItemExchangeRoleValueService
    {
        public const string DefaultStreamingAssetsPath = "Reference/PcItemExchange/rolevalue.ini";

        private readonly PcItemExchangeRoleValueIni _ini;
        private readonly Dictionary<string, PcItemExchangeIniKey> _keys =
            new Dictionary<string, PcItemExchangeIniKey>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, int> _jxbValues = new Dictionary<int, int>();

        public IReadOnlyList<string> Sections => _ini.sections;
        public IReadOnlyList<PcItemExchangeIniKey> Keys => _ini.keys;
        public IReadOnlyDictionary<int, int> JxbValues => _jxbValues;
        public ItemExchangeRoleValueSummary Summary { get; private set; }
        public bool Exists => _ini.exists;
        public int SectionCount => _ini.sections.Count;
        public int KeyCount => _ini.keys.Count;
        public int SkillValue => GetIntOrDefault("Value", "skill", 0);
        public int LimitCreateDate => GetIntOrDefault("Limit", "limitcreatedate", 0);
        public int CreateDate => GetIntOrDefault("Limit", "createdate", 0);
        public bool EvaluateLevelEnabled => IsEvaluateEnabled("level");
        public bool EvaluateSkillEnabled => IsEvaluateEnabled("skill");
        public bool EvaluateMoneyEnabled => IsEvaluateEnabled("money");
        public bool EvaluateItemEnabled => IsEvaluateEnabled("item");
        public bool EvaluateTaskEnabled => IsEvaluateEnabled("task");

        public ItemExchangeRoleValueService(PcItemExchangeRoleValueIni ini)
        {
            _ini = ini ?? new PcItemExchangeRoleValueIni();
            IndexIni();
            Summary = BuildSummary();
        }

        public static ItemExchangeRoleValueService LoadFromStreamingAssets(
            string relativePath = DefaultStreamingAssetsPath)
        {
            return LoadFromFile(Path.Combine(Application.streamingAssetsPath, relativePath));
        }

        public static ItemExchangeRoleValueService LoadFromSourceCatalogDirectory(string dir)
        {
            var catalog = PcItemExchangeSourceParser.ParseDirectory(dir);
            return new ItemExchangeRoleValueService(catalog.roleValue);
        }

        public static ItemExchangeRoleValueService LoadFromFile(string path)
        {
            return new ItemExchangeRoleValueService(PcItemExchangeSourceParser.ParseIni(path));
        }

        public bool TryGetRawValue(string section, string key, out string value)
        {
            PcItemExchangeIniKey entry;
            if (_keys.TryGetValue(MakeFullKey(section, key), out entry))
            {
                value = entry.value;
                return true;
            }
            value = null;
            return false;
        }

        public bool TryGetInt(string section, string key, out int value)
        {
            string raw;
            if (!TryGetRawValue(section, key, out raw))
            {
                value = 0;
                return false;
            }
            return TryParseInt(raw, out value);
        }

        public int GetIntOrDefault(string section, string key, int defaultValue)
        {
            int value;
            return TryGetInt(section, key, out value) ? value : defaultValue;
        }

        public bool TryGetJxbValue(int serverId, out int value) => _jxbValues.TryGetValue(serverId, out value);
        public int GetJxbValueOrDefault(int serverId, int defaultValue = 0)
        {
            int value;
            return TryGetJxbValue(serverId, out value) ? value : defaultValue;
        }

        public bool IsEvaluateEnabled(string key) => GetIntOrDefault("Evaluate", key, 0) != 0;

        private void IndexIni()
        {
            foreach (var item in _ini.keys)
            {
                if (item == null) continue;
                _keys[MakeFullKey(item.section, item.key)] = item;
                int serverId, jxbValue;
                if (IsSection(item.section, "Jxb") && TryParseInt(item.key, out serverId) &&
                    TryParseInt(item.value, out jxbValue))
                    _jxbValues[serverId] = jxbValue;
            }
        }

        private ItemExchangeRoleValueSummary BuildSummary()
        {
            int min = 0, max = 0;
            foreach (var value in _jxbValues.Values)
            {
                if (min == 0 || value < min) min = value;
                if (value > max) max = value;
            }
            return new ItemExchangeRoleValueSummary
            {
                exists = Exists,
                sectionCount = SectionCount,
                keyCount = KeyCount,
                skillValue = SkillValue,
                jxbServerCount = _jxbValues.Count,
                minJxbValue = min,
                maxJxbValue = max,
                limitCreateDate = LimitCreateDate,
                createDate = CreateDate,
                evaluateLevel = EvaluateLevelEnabled,
                evaluateSkill = EvaluateSkillEnabled,
                evaluateMoney = EvaluateMoneyEnabled,
                evaluateItem = EvaluateItemEnabled,
                evaluateTask = EvaluateTaskEnabled,
            };
        }

        private static string MakeFullKey(string section, string key)
        {
            section = (section ?? string.Empty).Trim();
            key = (key ?? string.Empty).Trim();
            return section.Length == 0 ? key : section + "." + key;
        }

        private static bool IsSection(string actual, string expected)
        {
            return string.Equals((actual ?? string.Empty).Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseInt(string raw, out int value)
        {
            return int.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value);
        }
    }
}
