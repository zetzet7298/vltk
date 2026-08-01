import io, sys
p = r"Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs"
s = io.open(p, encoding="utf-8").read()

needle = r'''var s = BaseSkill(55, "\u6ecc\u6bd2\u672f", "Th\u1ed1i \u0110\u1ed9c Thu\u1eadt", 30, 20, 400, SkillMissileForm.Surround);'''
print("needle(escaped-unicode test):", needle.encode('unicode_escape').decode()[:80])
print("count:", s.count(needle))
# find exact line
for i, line in enumerate(s.split('\n')):
    if 'BaseSkill(55,' in line:
        print("line", i+1, repr(line[:120]))
        break
