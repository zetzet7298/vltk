# Diff Unity runtime catalog vs PC canonical PcSkills.txt.
import json, csv, sys

EVID = r"C:\Projects\vltk-mobile\openspec\changes\port-caibang-skill-pc-parity\evidence"
PC = json.load(open(EVID + r"\pc-skills-audit.json", encoding="utf-8"))
pc = {r["id"]: r for r in PC}

# PC MisslesForm int -> SkillMissileForm enum
FORM = {0:"None",1:"Single",2:"Fan",3:"Surround",4:"Chain",5:"Zone",6:"Stance",7:"Stationary"}
def form_name(v):
    if v is None: return "?"
    return FORM.get(v, "f%d" % v)

rows = []
with open(EVID + r"\catalog-dump.tsv", encoding="utf-8-sig") as f:
    rd = csv.DictReader(f, delimiter="\t")
    for r in rd:
        rows.append(r)

def iv(s):
    try: return int(s.strip())
    except: return None

diffs = []
missing = []
for r in rows:
    sid = iv(r["skillId"])
    if sid is None: continue
    p = pc.get(sid)
    if p is None:
        missing.append(sid)
        continue
    issues = []
    radius = iv(r["radius"]); childId = iv(r["childId"]); childNum = iv(r["childNum"])
    cost = iv(r["cost"]); wait = iv(r["wait"])
    if radius is not None and p["radius"] is not None and radius != p["radius"]:
        issues.append("radius %s->PC %s" % (radius, p["radius"]))
    if childId is not None and p["childId"] is not None and childId != p["childId"]:
        issues.append("child %s->PC %s" % (childId, p["childId"]))
    if childNum is not None and p["childNum"] is not None and childNum != p["childNum"]:
        issues.append("childNum %s->PC %s" % (childNum, p["childNum"]))
    if cost is not None and p["cost"] is not None and cost != p["cost"]:
        issues.append("cost %s->PC %s" % (cost, p["cost"]))
    if wait is not None and p["waitTime"] is not None and wait != p["waitTime"]:
        issues.append("wait %s->PC %s" % (wait, p["waitTime"]))
    if issues:
        diffs.append((sid, r["name"], r["faction"], r["style"], "; ".join(issues)))

print("catalog rows: %d, pc rows: %d, pc-missing: %d, diffs: %d" % (len(rows), len(pc), len(missing), len(diffs)))
print("=== DIFFS ===")
for sid, name, fac, style, iss in sorted(diffs):
    print("%d\t%s\t%s\t%s\t%s" % (sid, name, fac, style, iss))
print("=== NOT IN PC (catalog only) ===")
print(sorted(missing))
