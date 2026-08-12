import collections, os
base = "C:/Projects/vltk-mobile/Assets/StreamingAssets/Reference"
TCVN3=[0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,160,258,194,202,212,416,431,272,259,226,234,244,417,432,273,175,176,177,178,179,180,224,7843,227,225,7841,186,7857,7859,7861,7855,191,192,193,194,195,196,197,7863,7847,7849,7851,7845,7853,232,205,7867,7869,233,7865,7873,7875,7877,7871,7879,236,7881,217,218,219,297,237,7883,242,224,7887,245,243,7885,7891,7893,7895,7889,7897,7901,7903,7905,7899,7907,249,240,7911,361,250,7909,7915,7917,7919,7913,7921,7923,7927,7929,253,7925,255]
def tcvn3(raw):
    w=raw.decode("windows-1252",errors="replace")
    return "".join(chr(TCVN3[ord(c)]) if ord(c)<len(TCVN3) else c for c in w)
def load(p):
    t=tcvn3(open(p,"rb").read())
    return t.replace("\r\n","\n").replace("\r","\n").split("\n")

p=base+"/PcSkills.txt"
lines=load(p)
hdr=lines[0].split("\t")
data=[l for l in lines[1:] if l.strip()]
rows=[l.split("\t") for l in data if len(l.split("\t"))>=72]
def col(r,i): return r[i].strip() if i<len(r) else ""
print("=== data rows:",len(rows),"| total data lines:",len(data))

# col 70 = LvlSetScript (FACTION)
fac=collections.Counter(col(r,70) for r in rows)
print("\n=== FACTION (col70 LvlSetScript) ===")
for f,c in fac.most_common():
    print(f"  {c:4d}  {f!r}")

# cast-form: MisslesForm(19), IsMelee(26), ByMissle(41), ChildSkillId(20)
print("\n=== CAST FORM ===")
mf=collections.Counter(col(r,19) for r in rows)
print("MisslesForm(19):",dict(mf))
melee=collections.Counter(col(r,26) for r in rows)
print("IsMelee(26):",dict(melee))
bym=collections.Counter(col(r,41) for r in rows)
print("ByMissle(41):",dict(bym))
haschild=sum(1 for r in rows if col(r,20))
print("rows with ChildSkillId(20) nonempty:",haschild)
hasprecast=sum(1 for r in rows if col(r,6))
print("rows with PreCastSpr(6) nonempty:",hasprecast)

# how many have NO precast AND NO child AND melee=0  (no-visual candidates)
novis=sum(1 for r in rows if not col(r,6) and not col(r,20) and col(r,26)!="1")
print("no-precast + no-child + not-melee:",novis)

# sample actual skill rows: faction skills (col70 nonempty)
print("\n=== SAMPLE faction skills (col70 nonempty) ===")
n=0
for r in rows:
    if col(r,70):
        print(f"  id={col(r,2):>6} name={col(r,0)[:20]!r:24} faction={col(r,70)!r:20} mf={col(r,19)} melee={col(r,26)} child={col(r,20)} precast={'Y' if col(r,6) else '-'} p1={col(r,58)} p2={col(r,60)}")
        n+=1
        if n>=12: break

# supply/buff skills: IsAura(11)=1 or effect scripts
print("\n=== IsAura(11) breakdown ===")
aura=collections.Counter(col(r,11) for r in rows)
print(dict(aura))
