# -*- coding: utf-8 -*-
import io, sys

sys.stdout.reconfigure(encoding="utf-8")

# TCVN3 table copied from Assets/Scripts/PortData/PcText.cs (canonical repo decoder)
TCVN3 = [
    0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
    16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
    32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,
    48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
    64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
    80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
    96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
    112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
    128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,
    144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,
    160,258,194,202,212,416,431,272,259,226,234,244,417,432,273,175,
    176,177,178,179,180,224,7843,227,225,7841,186,7857,7859,7861,7855,191,
    192,193,194,195,196,197,7863,7847,7849,7851,7845,7853,232,205,7867,7869,
    233,7865,7873,7875,7877,7871,7879,236,7881,217,218,219,297,237,
    7883,242,224,7887,245,243,7885,7891,7893,7895,7889,7897,7901,7903,
    7905,7899,7907,249,240,7911,361,250,7909,7915,7917,7919,7913,7921,
    7923,7927,7929,253,7925,255,
]

def tcvn3_to_unicode(text):
    out = []
    for ch in text:
        code = ord(ch)
        if 0 <= code < len(TCVN3):
            out.append(chr(TCVN3[code]))
        else:
            out.append(ch)
    return "".join(out)

raw = open(r"Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt", "rb").read()
text = raw.decode("latin-1")  # western bytes as-is
lines = text.split("\r\n") if "\r\n" in text else text.split("\n")
hdr = lines[0].split("\t")
want = [271,273,318,319,321,322,323,324,325,708,709,711,1055,1056,1058,1059,1060,252,269,282,328,336,380,385,712,713,1061,1062,1063,1065,1114,353,355,356,384,1066,1067,390,391,715,267,365,368,716,1078,1079,1057]
rows = {}
for ln in lines[1:]:
    c = ln.split("\t")
    if len(c) < 5: continue
    try: sid = int(c[2])
    except ValueError: continue
    if sid in want:
        d = dict(zip(hdr, c))
        rows[sid] = (tcvn3_to_unicode(d['SkillName'].strip()), d['LvlSetScript'].strip())

FAC = {
    "shaolin": "CombatFaction.Shaolin", "tianwang": "CombatFaction.TianWang",
    "emei": "CombatFaction.EMei", "cuiyan": "CombatFaction.CuiYan",
    "wudu": "CombatFaction.WuDu", "tianren": "CombatFaction.TianRen",
    "wudang": "CombatFaction.WuDang", "tangmen": "CombatFaction.TangMen",
    "kunlun": "CombatFaction.KunLun", "gaibang": "CombatFaction.CaiBang",
}
def faction_of(script):
    s = script.lower()
    for k, v in FAC.items():
        if k in s: return v
    return "CombatFaction.Shaolin"

entries = []
for sid in want:
    name, script = rows[sid]
    entries.append("            (%d, \"%s\", %s)," % (sid, name.replace('"', '\\"'), faction_of(script)))
table = "\n".join(entries)

factory = r"Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs"
s = io.open(factory, encoding="utf-8").read()
start = s.find("MissingLearnedSkillStubs = new[]")
end = s.find("        };", start)
assert start > 0 and end > start
stub_head = s[s.find("// SKL-ALLFAC-001"):start]
s = s[:s.find("// SKL-ALLFAC-001")] + stub_head + table + "\n" + s[end:]
io.open(factory, "w", encoding="utf-8", newline="\n").write(s)

# sanity: print decoded names
for sid in want:
    print(sid, rows[sid][0])
