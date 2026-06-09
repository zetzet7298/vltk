// -----------------------------------------------------------------------------
// VLTK Mobile — focused PC skill source/script link parser.
// Source: /var/www/vltksource_new/vl_update_27/Client 6.0/settings/skills.txt
// Purpose: prove SkillId -> LvlSetScript links without widening PcSkillFullParser.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

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

            var lines = PcItemCommon.ReadServerLines(skillsTxtPath);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }

                var cols = line.Split('\t');
                if (cols.Length <= LvlSetScriptCol) continue;
                int skillId = PcItemCommon.Int(cols, SkillIdCol);
                if (skillId <= 0) continue;
                result[skillId] = PcItemCommon.Str(cols, LvlSetScriptCol);
            }
            return result;
        }
    }
}
