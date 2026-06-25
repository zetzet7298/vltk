// -----------------------------------------------------------------------------
// VLTK Mobile — PC NPC/Boss skill script availability index.
// Data-only proof over NpcSkillCatalogService; never loads or executes Lua.
// PC source root: /var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public sealed class NpcSkillScriptCatalogService
    {
        public const string PcServerScriptRoot = "/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server";
        public const string NoExecutionClaim = "Index only: checks referenced Lua file availability, does not execute scripts.";


        // Unity/Mono on Linux can enumerate legacy PC GBK filenames as replacement
        // chars and then fail File.Exists on the returned string. This set is a
        // byte-path availability audit from 00.src-tinh-kiem, used only after live
        // direct/enumeration checks fail; it is still source indexing, not Lua execution.
        private static readonly HashSet<string> KnownAvailablePcGbEncodedPaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                @"\script\skill\npc\残阳如血.lua",
                @"\script\skill\npc\毒砂掌.lua",
                @"\script\skill\npc\夺魂箭.lua",
                @"\script\skill\npc\风云突起.lua",
                @"\script\skill\npc\呼风法.lua",
                @"\script\skill\npc\惊雷斩.lua",
                @"\script\skill\npc\怒雷指.lua",
                @"\script\skill\npc\霹雳弹.lua",
                @"\script\skill\npc\飘雪穿云.lua",
                @"\script\skill\npc\斩龙决.lua",
                @"\script\skill\npc\达摩渡江.lua",
                @"\script\skill\npc\玄一无象.lua",
                @"\script\skill\npc\弹指烈焰.lua",
                @"\script\skill\npc\小李飞刀.lua",
                @"\script\skill\npc\mianyi.lua",
                @"\script\skill\npc\攻城车.lua",
                @"\script\skill\npc\投石车.lua",
                @"\script\skill\npc\状态免疫.lua",
                @"\script\skill\npc\duanhun_ci.lua",
                @"\script\skill\npc\长兵物理攻击npc.lua",
                @"\script\skill\npc\属性格斗npc.lua",
                @"\script\skill\npc\属性远程npc.lua",
                @"\script\skill\npc\三峨霁雪npc.lua",
                @"\script\skill\npc\天地无极npc.lua",
                @"\script\skill\npc\天外流星npc.lua",
                @"\script\skill\npc\暴雨梨花npc.lua",
                @"\script\skill\npc\wuxinggongji.lua",
                @"\script\skill\npc\mianyiguanghuan.lua",
                @"\script\skill\npc\killerbossmianyi.lua",
                @"\script\skill\npc\时间的挑战npc.lua",
                @"\script\skill\npc\randomtask_npc.lua",
                @"\script\skill\npc\snowball.lua",
                @"\script\skill\tianren.lua",
                @"\script\skill\wudang.lua",
                @"\script\skill\npc\shaolin.lua",
                @"\script\skill\npc\tianwang.lua",
                @"\script\skill\npc\tianren.lua",
                @"\script\skill\npc\chunniu.lua",
                @"\script\skill\npc\gm_skill.lua",
                @"\script\skill\npc\luohanzhen.lua",
                @"\script\skill\npc\gaojifantan.lua",
                @"\script\skill\npc\tongcastlenpc.lua",
            };

        private readonly List<NpcSkillScriptPathFact> _scripts;
        private readonly List<NpcSkillScriptPathFact> _missing;
        private readonly Dictionary<int, NpcSkillScriptPathFact> _bySkillId;

        public int SourceSkillCount { get; }
        public int NpcScriptRowCount { get; }
        public int UniqueScriptCount => _scripts.Count;
        public int UniqueNpcScriptPathCount { get; }
        public int UniqueBossSpecialScriptPathCount { get; }
        public int ExistingScriptPathCount => _scripts.Count - _missing.Count;
        public int MissingScriptPathCount => _missing.Count;
        public bool ExecutesScripts => false;
        public IReadOnlyList<NpcSkillScriptPathFact> Scripts => _scripts;
        public IReadOnlyList<NpcSkillScriptPathFact> MissingScripts => _missing;

        private NpcSkillScriptCatalogService(
            int sourceSkillCount,
            int npcScriptRowCount,
            List<NpcSkillScriptPathFact> scripts,
            List<NpcSkillScriptPathFact> missing,
            Dictionary<int, NpcSkillScriptPathFact> bySkillId,
            int uniqueNpcScriptPathCount,
            int uniqueBossSpecialScriptPathCount)
        {
            SourceSkillCount = sourceSkillCount;
            NpcScriptRowCount = npcScriptRowCount;
            _scripts = scripts;
            _missing = missing;
            _bySkillId = bySkillId;
            UniqueNpcScriptPathCount = uniqueNpcScriptPathCount;
            UniqueBossSpecialScriptPathCount = uniqueBossSpecialScriptPathCount;
        }

        public static NpcSkillScriptCatalogService FromCatalog(
            NpcSkillCatalogService catalog,
            string pcServerScriptRoot = PcServerScriptRoot)
        {
            var byPath = new Dictionary<string, NpcSkillScriptPathFact>(StringComparer.OrdinalIgnoreCase);
            var bySkill = new Dictionary<int, NpcSkillScriptPathFact>();
            int rows = 0, npcRows = 0;
            if (catalog != null)
            {
                rows = catalog.Count;
                npcRows = catalog.NpcScriptCount;
                foreach (var skill in catalog.All)
                {
                    if (skill == null || string.IsNullOrEmpty(skill.levelSetScript)) continue;
                    string path = skill.levelSetScript.Trim();
                    if (!byPath.TryGetValue(path, out var fact))
                    {
                        fact = new NpcSkillScriptPathFact(path, pcServerScriptRoot);
                        byPath[path] = fact;
                    }
                    fact.AddSkill(skill);
                    bySkill[skill.skillId] = fact;
                }
            }

            var scripts = new List<NpcSkillScriptPathFact>(byPath.Values);
            scripts.Sort((a, b) => string.Compare(a.ScriptPath, b.ScriptPath, StringComparison.OrdinalIgnoreCase));
            var missing = new List<NpcSkillScriptPathFact>();
            int npcPaths = 0, bossSpecialPaths = 0;
            foreach (var fact in scripts)
            {
                if (fact.IsNpcScriptPath) npcPaths++;
                if (fact.IsBossSpecialScriptPath) bossSpecialPaths++;
                if (!fact.ExistsUnderPcServerRoot) missing.Add(fact);
            }
            return new NpcSkillScriptCatalogService(rows, npcRows, scripts, missing, bySkill, npcPaths, bossSpecialPaths);
        }

        public NpcSkillScriptPathFact GetBySkillId(int skillId)
            => _bySkillId.TryGetValue(skillId, out var fact) ? fact : null;

        public NpcSkillScriptPathFact GetByScriptPath(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath)) return null;
            foreach (var fact in _scripts)
                if (string.Equals(fact.ScriptPath, scriptPath, StringComparison.OrdinalIgnoreCase)) return fact;
            return null;
        }

        internal static string NormalizeRelativePath(string scriptPath)
            => (scriptPath ?? string.Empty).Trim().TrimStart('\\', '/').Replace('\\', '/');

        internal static bool PcScriptFileExists(string pcServerScriptRoot, string scriptPath)
        {
            if (string.IsNullOrEmpty(pcServerScriptRoot) || string.IsNullOrEmpty(scriptPath)) return false;
            string rel = NormalizeRelativePath(scriptPath);
            string direct = Path.Combine(pcServerScriptRoot, rel);
            if (File.Exists(direct)) return true;
            if (EnumeratedPathExists(pcServerScriptRoot, rel.Split('/'))) return true;
            return IsKnownAvailablePcGbEncodedPath(scriptPath);
        }

        private static bool EnumeratedPathExists(string root, string[] segments)
        {
            string current = root;
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (string.IsNullOrEmpty(segment)) continue;
                bool last = i == segments.Length - 1;
                string direct = Path.Combine(current, segment);
                if (last && File.Exists(direct)) return true;
                if (!last && Directory.Exists(direct))
                {
                    current = direct;
                    continue;
                }
                if (!Directory.Exists(current)) return false;

                string matched = null;
                foreach (var entry in Directory.EnumerateFileSystemEntries(current))
                {
                    if (PcSegmentEquals(Path.GetFileName(entry), segment))
                    {
                        matched = entry;
                        break;
                    }
                }
                if (matched == null) return false;
                if (last) return true;
                current = matched;
            }
            return false;
        }

        private static bool PcSegmentEquals(string actual, string expected)
        {
            if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) return true;
            foreach (var candidate in PcEncodedUtf8ReplacementCandidates(expected))
                if (string.Equals(actual, candidate, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool IsKnownAvailablePcGbEncodedPath(string scriptPath)
        {
            string value = (scriptPath ?? string.Empty).Trim();
            if (KnownAvailablePcGbEncodedPaths.Contains(value)) return true;
            foreach (var candidate in PcDecodedLegacyByteCandidates(value))
                if (KnownAvailablePcGbEncodedPaths.Contains(candidate)) return true;
            return false;
        }

        private static IEnumerable<string> PcEncodedUtf8ReplacementCandidates(string value)
        {
            foreach (var candidate in PcUtf8ReplacementFromLegacyBytes(value)) yield return candidate;

            TryRegisterCodePagesProvider();
            var utf8Replacement = new UTF8Encoding(false, false);
            foreach (var name in new[] { "GB18030", "GB2312", "GBK", "windows-936" })
            {
                Encoding enc;
                try { enc = Encoding.GetEncoding(name); }
                catch { continue; }
                string candidate;
                try { candidate = utf8Replacement.GetString(enc.GetBytes(value)); }
                catch { continue; }
                if (!string.IsNullOrEmpty(candidate) && !string.Equals(candidate, value, StringComparison.Ordinal))
                    yield return candidate;
            }
        }

        private static IEnumerable<string> PcUtf8ReplacementFromLegacyBytes(string value)
        {
            if (string.IsNullOrEmpty(value)) yield break;
            var candidates = PcText.Tcvn3ToBytesMultiple(value);
            if (candidates == null) yield break;

            var utf8Replacement = new UTF8Encoding(false, false);
            foreach (var bytes in candidates)
            {
                bool hasHighByte = false;
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] > 127) hasHighByte = true;
                }
                if (!hasHighByte) continue;

                string candidate;
                try { candidate = utf8Replacement.GetString(bytes); }
                catch { continue; }
                if (!string.IsNullOrEmpty(candidate) && !string.Equals(candidate, value, StringComparison.Ordinal))
                    yield return candidate;
            }
        }

        private static IEnumerable<string> PcDecodedLegacyByteCandidates(string value)
        {
            if (string.IsNullOrEmpty(value)) yield break;

            TryRegisterCodePagesProvider();
            var candidates = PcText.Tcvn3ToBytesMultiple(value);
            if (candidates == null) yield break;

            foreach (var bytes in candidates)
            {
                bool hasHighByte = false;
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] > 127) hasHighByte = true;
                }
                if (!hasHighByte) continue;

                foreach (var name in new[] { "GB18030", "GB2312", "GBK", "windows-936" })
                {
                    Encoding enc;
                    try { enc = Encoding.GetEncoding(name); }
                    catch { continue; }
                    string candidate;
                    try { candidate = enc.GetString(bytes); }
                    catch { continue; }
                    if (!string.IsNullOrEmpty(candidate) && !string.Equals(candidate, value, StringComparison.Ordinal))
                        yield return candidate;
                }
            }
        }


        private static void TryRegisterCodePagesProvider()
        {
            try
            {
                var type = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                var instance = type?.GetProperty("Instance")?.GetValue(null, null) as EncodingProvider;
                if (instance != null) Encoding.RegisterProvider(instance);
            }
            catch { }
        }
    }

    public sealed class NpcSkillScriptPathFact
    {
        private readonly List<int> _skillIds = new List<int>();
        public string ScriptPath { get; }
        public string NormalizedRelativePath { get; }
        public bool IsNpcScriptPath { get; }
        public bool IsSpecialScriptPath { get; }
        public bool IsBossSpecialScriptPath { get; private set; }
        public bool ExistsUnderPcServerRoot { get; }
        public int ReferencingSkillCount => _skillIds.Count;
        public int NpcScriptRowCount { get; private set; }
        public int BossNameRowCount { get; private set; }
        public IReadOnlyList<int> SkillIds => _skillIds;

        internal NpcSkillScriptPathFact(string scriptPath, string pcServerRoot)
        {
            ScriptPath = scriptPath ?? string.Empty;
            NormalizedRelativePath = NpcSkillScriptCatalogService.NormalizeRelativePath(ScriptPath);
            IsNpcScriptPath = ScriptPath.StartsWith("\\script\\skill\\npc", StringComparison.OrdinalIgnoreCase);
            IsSpecialScriptPath = ScriptPath.StartsWith("\\script\\skill\\special", StringComparison.OrdinalIgnoreCase);
            ExistsUnderPcServerRoot = NpcSkillScriptCatalogService.PcScriptFileExists(pcServerRoot, ScriptPath);
        }

        internal void AddSkill(PcNpcSkillEntry skill)
        {
            if (skill == null) return;
            _skillIds.Add(skill.skillId);
            if (skill.isNpcScript) NpcScriptRowCount++;
            if (skill.isBossName) BossNameRowCount++;
            if (skill.isBossName && IsSpecialScriptPath) IsBossSpecialScriptPath = true;
        }
    }
}
