import io

BS = chr(92)
p = r"Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs"
s = io.open(p, encoding="utf-8").read()

def mk(skill_id, zh_name, vn_name):
    # old block: surround + 400 radius + TianRen spr
    old = (
        '            var s = BaseSkill(%d, "%s", "%s", 30, 20, 400, SkillMissileForm.Surround);\n'
        '            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 0; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetSelf = true; s.targetAlly = true;\n'
        '            s.effectSourceId = Sprite("' + BS + BS + 'spr' + BS + BS + 'skill' + BS + BS + '\u5929\u5fcd' + BS + BS + 'mag_tr_16_\u65bd\u9b54\u6cd5.spr");'
    ) % (skill_id, zh_name, vn_name)
    # new block: PC row style=2/radius 0/form 7 + WuDu spr
    new = (
        '            // PC PcSkills.txt row %d: SkillStyle=2 (InitiativeNpcState), AttackRadius=0, MisslesForm=7 (Stationary),\n'
        '            // ChildSkillId=0, ChildSkillNum=0, PreCastSpr=' + BS + 'spr' + BS + 'skill' + BS + '\u4e94\u6bd2' + BS + 'wdu_13_\u65bd\u6bd2\u672f.spr.\n'
        '            var s = BaseSkill(%d, "%s", "%s", 30, 20, 0, SkillMissileForm.Stationary);\n'
        '            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.childSkillId = 0; s.childSkillNum = 0; s.baseSkill = true; s.charAnimId = 11; s.targetSelf = true; s.targetAlly = true;\n'
        '            s.effectSourceId = Sprite("' + BS + BS + 'spr' + BS + BS + 'skill' + BS + BS + '\u4e94\u6bd2' + BS + BS + 'wdu_13_\u65bd\u6bd2\u672f.spr");'
    ) % (skill_id, skill_id, zh_name, vn_name)
    return old, new

old, new = mk(55, "\u6dec\u6bd2\u672f", "Th\u1ed1i \u0110\u1ed9c Thu\u1eadt")
assert s.count(old) == 1, "55: %d" % s.count(old)
s = s.replace(old, new)

old, new = mk(57, "\u51b0\u9b44\u5bd2\u5149", "B\u0103ng Ph\u00e1ch H\u00e0n Quang")
assert s.count(old) == 1, "57: %d" % s.count(old)
s = s.replace(old, new)

io.open(p, "w", encoding="utf-8", newline="\n").write(s)
print("ok 55+57")
