# Audit: per-faction skill inventory from PcAllFactionLearnedDisplaySkills.txt vs runtime catalog.
import csv, json

EVID = r"C:\Projects\vltk-mobile\openspec\changes\port-caibang-skill-pc-parity\evidence"
SRC = r"C:\Projects\vltk-mobile\Assets\StreamingAssets\Reference\PcAllFactionLearnedDisplaySkills.txt"

FAC = {1:["Shaolin","TianWang"],2:["EMei","CuiYan"],3:["TangMen","WuDu"],4:["CaiBang","TianRen"],5:["WuDang","KunLun"]}

inv = {}  # charclass -> {sid: name}
with open(SRC, encoding="utf-8-sig", errors="replace") as f:
    rd = csv.DictReader(f, delimiter="\t")
    for r in rd:
        try: sid = int((r.get("SkillId") or "").strip())
        except: continue
        try: cc = int((r.get("CharClass") or "").strip())
        except: continue
        inv.setdefault(cc, {})[sid] = (r.get("SkillName") or "").strip()

# catalog: faction name -> set of skill ids (full catalog, incl. passive)
cat_fac = {}
with open(EVID + r"\catalog-all.tsv", encoding="utf-8-sig") as f:
    rd = csv.DictReader(f, delimiter="\t")
    for r in rd:
        fac = (r["faction"] or "").strip()
        cat_fac.setdefault(fac, set()).add(int(r["skillId"]))

all_cat = set()
for v in cat_fac.values(): all_cat |= v

print("=== PC learned skills per faction-pair vs FULL catalog ===")
for cc in sorted(inv):
    facs = FAC.get(cc, ["class%d" % cc])
    have = set()
    for fac in facs:
        have |= cat_fac.get(fac, set())
    pcs = set(inv[cc])
    missing = sorted(pcs - have)
    # present in catalog but under a wrong faction (faction=None or other)
    wrongfac = sorted((pcs & (all_cat - have)))
    print("%s (%s): PC %d, catalog has %d, MISSING %d, WRONG-FACTION %d" % (",".join(facs), cc, len(pcs), len(pcs & have), len(missing), len(wrongfac)))
    for sid in wrongfac:
        print("   WRONGFAC %d\t%s" % (sid, inv[cc][sid]))
    for sid in missing:
        print("   MISSING %d\t%s" % (sid, inv[cc][sid]))
