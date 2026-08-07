import sys, collections, os
base = "C:/Projects/vltk-mobile/Assets/StreamingAssets/Reference"
TCVN3 = [0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,160,258,194,202,212,416,431,272,259,226,234,244,417,432,273,175,176,177,178,179,180,224,7843,227,225,7841,186,7857,7859,7861,7855,191,192,193,194,195,196,197,7863,7847,7849,7851,7845,7853,232,205,7867,7869,233,7865,7873,7875,7877,7871,7879,236,7881,217,218,219,297,237,7883,242,224,7887,245,243,7885,7891,7893,7895,7889,7897,7901,7903,7905,7899,7907,249,240,7911,361,250,7909,7915,7917,7919,7913,7921,7923,7927,7929,253,7925,255]
def tcvn3(raw):
    western = raw.decode("windows-1252", errors="replace")
    return "".join(chr(TCVN3[ord(c)]) if 0 <= ord(c) < len(TCVN) and ord(c)>=160 else c for c in western) if False else "".join(chr(TCVN3[min(ord(c),255)]) if ord(c)<len(TCVN3) else c for c in western)

def load(path):
    raw=open(path,"rb").read()
    txt=tcvn3(raw)
    lines=txt.replace("\r\n","\n").replace("\r","\n").split("\n")
    return lines

# PcSkills.txt
p = base+"/PcSkills.txt"
lines = load(p)
print("=== PcSkills.txt ===")
print("total lines:", len(lines))
header = lines[0].split("\t") if lines else []
print("header cols:", len(header))
for i,c in enumerate(header):
    print(f"  [{i}] {c}")
data = [l for l in lines[1:] if l.strip()]
print("data rows:", len(data))
# faction breakdown col 71
from collections import Counter
fac = Counter()
rows=[]
for l in data:
    cols=l.split("\t")
    if len(cols)<72: 
        continue
    f=cols[71].strip()
    fac[f]+=1
    rows.append(cols)
print("=== faction (LvlSetScript col 71) breakdown ===")
for f,c in fac.most_common():
    print(f"  {c:4d}  {f!r}")
print("=== sample rows (SkillId|name|faction|SkillStyle|Attrib|IsAura|ReqLv|MaxLv) ===")
for r in rows[:8]:
    def g(i): return r[i] if i<len(r) else ""
    print(f"  id={g(2):>6} name={g(0)[:18]!r:22} faction={g(71)!r:18} style={g(4)} attr={g(3)} isAura={g(11)} reqLv={g(53)} maxLv={g(54)}")
