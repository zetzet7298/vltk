// -----------------------------------------------------------------------------
// VLTK Mobile — focused PC skill source/script link parser.
// Source: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/skills.txt
// Purpose: prove SkillId -> LvlSetScript links without widening PcSkillFullParser.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcSkillSourceLinkParser
    {
        public const int SkillIdCol = 2;
        public const int LvlSetScriptCol = 70;

        public static Dictionary<int, string> ParseSkillScripts(string skillsTxtPath)
        {
            var result = new Dictionary<int, string>();
            if (string.IsNullOrEmpty(skillsTxtPath) || !File.Exists(skillsTxtPath))
                return result;

            // skills.txt mixes TCVN3-Vietnamese name columns with GBK-Chinese script-path
            // columns in the same file, so PcText.DecodeBest (one encoding per file) wins
            // windows-1252 and turns col70 (\script\skill\special\长兵物理攻击.lua) into
            // garbage. This parser only needs the numeric SkillId (col2) and the GBK script
            // path (col70), so decode explicitly as GB2312 (lenient in .NET) to preserve the
            // Chinese path bytes.
            var lines = PcText.ReadLines(skillsTxtPath, Encoding.GetEncoding("GB2312"));
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }

                var cols = line.Split('\t');
                if (cols.Length <= LvlSetScriptCol) continue;
                int skillId = PcItemCommon.Int(cols, SkillIdCol);
                if (skillId <= 0) continue;
                var script = PcItemCommon.Str(cols, LvlSetScriptCol);
                // skills.txt has multiple rows per SkillId (one per skill level). Only the
                // first/base row carries the LvlSetScript path; later level rows leave col70
                // empty. Keep the first non-empty path instead of letting an empty later row
                // clobber it (last-write-wins would wipe the script link).
                if (string.IsNullOrEmpty(script) && result.ContainsKey(skillId)) continue;
                if (!result.ContainsKey(skillId) || !string.IsNullOrEmpty(script))
                    result[skillId] = script;
            }
            return result;
        }
    }
}
