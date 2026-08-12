import io

p = r"Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs"
s = io.open(p, encoding="utf-8").read()


def spr(path):
    return 'Sprite("' + path.replace("\\", "\\\\") + '")'


edits = [
    # 102 CuiYan Phong Quyen Tan Tuyet
    ('            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 7; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true; s.startSkillId = 398; s.startSkillLevel = 1; // G6 anchor (Phase 4 wire)',
     '            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 7; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true; s.startSkillId = 398; s.startSkillLevel = 1; // G6 anchor (Phase 4 wire)\n            // PC PreCastSpr: \\spr\\skill\\峨嵋\\mag_em_13_施魔法.spr (PcSkills.txt row 102).\n            s.effectSourceId = ' + spr(r"\spr\skill\峨嵋\mag_em_13_施魔法.spr") + ';'),
    # 148 TianRen Ma Diem That Sat
    ('            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 58; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;\n            s.startSkillId = 192; s.startSkillLevel = 1; // G6: anchor cho Ngự Phong Thuật (Phase 4 wire)',
     '            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 58; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;\n            s.startSkillId = 192; s.startSkillLevel = 1; // G6: anchor cho Ngự Phong Thuật (Phase 4 wire)\n            // PC PreCastSpr: \\spr\\skill\\天忍\\mag_tr_16_施魔法.spr (PcSkills.txt row 148).\n            s.effectSourceId = ' + spr(r"\spr\skill\天忍\mag_tr_16_施魔法.spr") + ';'),
    # 174 KunLun Ki Ban Phu — anchor on the BaseSkill line
    ('            var s = BaseSkill(174, "\u7f81\u7eca\u7b26", "Ki B\u00e1n Ph\u00f9", 20, 20, 400, SkillMissileForm.Surround);\n            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;',
     '            var s = BaseSkill(174, "\u7f81\u7eca\u7b26", "Ki B\u00e1n Ph\u00f9", 20, 20, 400, SkillMissileForm.Surround);\n            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;\n            // PC PreCastSpr: \\spr\\skill\\昆仑\\kl_16_魔法施放.spr (PcSkills.txt row 174).\n            s.effectSourceId = ' + spr(r"\spr\skill\昆仑\kl_16_魔法施放.spr") + ';'),
]
for old, new in edits:
    assert s.count(old) == 1, "block not unique: %r" % old[:70]
    s = s.replace(old, new)

# 136 Hoa Lien Phan Hoa
old = '            var s = BaseSkill(136, "\u706b\u83b2\u711a\u534e", "H\u1ecfa Li\u00ean Ph\u1ea7n Hoa", 10, 20, 400, SkillMissileForm.Surround);'
assert s.count(old) == 1
new = old + '\n            // PC PreCastSpr: \\spr\\skill\\天忍\\mag_tr_16_施魔法.spr (PcSkills.txt row 136).\n            s.effectSourceId = ' + spr(r"\spr\skill\天忍\mag_tr_16_施魔法.spr") + ';'
s = s.replace(old, new)

# 392/393: KunLunStaticOnly builder gets PC preCast art per id
old = '''        private static SkillDefinition KunLunStaticOnly(int id)
        {
            var s = BaseSkill(id, "KL" + id, "KunLun " + id, 1, 1, 0, SkillMissileForm.None);
            s.faction = CombatFaction.KunLun;
            return s;
        }'''
assert s.count(old) == 1
new = '''        private static SkillDefinition KunLunStaticOnly(int id)
        {
            var s = BaseSkill(id, "KL" + id, "KunLun " + id, 1, 1, 0, SkillMissileForm.None);
            s.faction = CombatFaction.KunLun;
            // PC PreCastSpr (PcSkills.txt): 392/393 = \\spr\\skill\\昆仑\\kl_16_魔法施放.spr (393 = kl_17, chua staged -> fallback kl_16).
            if (id == 392 || id == 393)
                s.effectSourceId = Sprite("\\\\spr\\\\skill\\\\昆仑\\\\kl_16_魔法施放.spr");
            return s;
        }'''
s = s.replace(old, new)

io.open(p, "w", encoding="utf-8", newline="\n").write(s)
print("ok 6 skills")
