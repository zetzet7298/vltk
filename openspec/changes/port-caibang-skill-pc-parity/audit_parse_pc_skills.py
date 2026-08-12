# Audit script: parse PcSkills.txt (PC canonical full table) into a compact JSON
# for comparison against the Unity runtime catalog.
import csv, json, sys

SRC = r"C:\Projects\vltk-mobile\Assets\StreamingAssets\Reference\PcSkills.txt"
OUT = r"C:\Projects\vltk-mobile\openspec\changes\port-caibang-skill-pc-parity\evidence\pc-skills-audit.json"

COLS = [
    "SkillId", "AttackRadius", "MisslesForm", "ChildSkillId", "ChildSkillNum",
    "WaitTime", "CostValue", "Param1", "Param2", "LvlSetScript", "PreCastSpr",
    "SkillStyle", "Attrib", "ByMissle", "CollidSkillId", "FlySkillId",
]

rows = []
with open(SRC, encoding="utf-8-sig", errors="replace") as f:
    reader = csv.DictReader(f, delimiter="\t")
    if not reader.fieldnames:
        print("NO HEADER", flush=True); sys.exit(1)
    header = list(reader.fieldnames)
    missing = [c for c in COLS if c not in header]
    print("header cols:", len(header), "missing:", missing, flush=True)
    for r in reader:
        try:
            sid = int(r.get("SkillId", "").strip() or 0)
        except ValueError:
            continue
        if sid <= 0:
            continue
        def iv(name):
            v = (r.get(name) or "").strip()
            try: return int(v)
            except ValueError: return None
        rows.append({
            "id": sid,
            "name": (r.get("SkillName") or "").strip(),
            "radius": iv("AttackRadius"),
            "form": iv("MisslesForm"),
            "childId": iv("ChildSkillId"),
            "childNum": iv("ChildSkillNum"),
            "waitTime": iv("WaitTime"),
            "cost": iv("CostValue"),
            "param1": iv("Param1"),
            "param2": iv("Param2"),
            "script": (r.get("LvlSetScript") or "").strip(),
            "preCast": (r.get("PreCastSpr") or "").strip(),
            "style": iv("SkillStyle"),
        })

rows.sort(key=lambda x: x["id"])
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(rows, f, ensure_ascii=False, indent=0)
print(f"wrote {len(rows)} skills -> {OUT}", flush=True)
