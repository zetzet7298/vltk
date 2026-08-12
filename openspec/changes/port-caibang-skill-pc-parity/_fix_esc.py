# -*- coding: utf-8 -*-
import io

p = r"Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs"
s = io.open(p, encoding="utf-8").read()
BS = chr(92)  # single backslash

table = """        private static readonly (int id, string key)[] MissingSkillPreCastSpr = new[]
        {
""" + "\n".join(
    '            ({0}, "{1}"),'.format(sid, '"' + BS + BS + 'spr' + BS + BS + 'skill' + BS + BS + folder + BS + BS + fname + '"'.replace('""', '"').split('"')[0].join(['""']) if False else key)
    for sid, key in []
) + """

        };

        private static void RegisterMissingLearnedSkillStubs(SkillCatalog catalog)"""

# simpler: rebuild the whole block explicitly
entries = [
    (328, "spr/skill/峨嵋/mag_em_13_施魔法.spr"),
    (336, "spr/skill/峨嵋/mag_em_13_施魔法.spr"),
    (380, "spr/skill/峨嵋/mag_em_13_施魔法.spr"),
    (385, "spr/skill/峨嵋/mag_em_13_施魔法.spr"),
    (1061, "spr/skill/峨嵋/mag_em_13_施魔法.spr"),
    (1062, "spr/skill/峨嵋/mag_em_13_施魔法.spr"),
    (1063, "spr/skill/峨嵋/mag_em_13_施魔法.spr"),
    (1065, "spr/skill/峨嵋/mag_em_13_施魔法.spr"),
    (391, "spr/skill/天忍/mag_tr_16_施魔法.spr"),
    (365, "spr/skill/昆仑/kl_16_魔法施放.spr"),
    (1078, "spr/skill/昆仑/kl_16_魔法施放.spr"),
    (1079, "spr/skill/昆仑/kl_16_魔法施放.spr"),
    (1056, "spr/skill/1502/sl/sl_150_gunshao_dl.spr"),
]
lines = []
for sid, key in entries:
    csharp_key = BS + BS + key.replace("/", BS + BS)
    lines.append('            ({0}, "{1}"),'.format(sid, csharp_key))
block = """        // PC PreCastSpr for the stub rows, pinned only where the SPR is verified staged
        // (fail-closed: never reference an unstaged sprite). Decoded from the TCVN3 file's
        // GBK PreCastSpr column via the staged-hash SprRuntimeService probe (2026-07-18).
        private static readonly (int id, string key)[] MissingSkillPreCastSpr = new[]
        {
""" + "\n".join(lines) + """
        };

        private static void RegisterMissingLearnedSkillStubs(SkillCatalog catalog)"""

start = s.find("// PC PreCastSpr for the stub rows")
end = s.find("private static void RegisterMissingLearnedSkillStubs", start)
assert start > 0 and end > start, "anchors"
s = s[:start] + block + s[end:]
io.open(p, "w", encoding="utf-8", newline="\n").write(s)
i = s.find("(328,", start)
print(repr(s[i:i + 80]))
