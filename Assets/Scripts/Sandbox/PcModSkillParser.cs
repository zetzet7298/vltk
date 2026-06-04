// -----------------------------------------------------------------------------
// VLTK Mobile — PC ModSkills.txt data-driven port
// Source: Assets/StreamingAssets/Reference/ModSkills.txt
// Purpose: keep expansion/event/title/boss skills visible to mobile runtime
// instead of silently dropping ids 1216+.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Raw ModSkills.txt row. Field names match PC header so parity audit stays easy.
    /// </summary>
    public sealed class PcModSkillRow
    {
        public string skillName;
        public string property;
        public int skillId;
        public int attrib;
        public int skillStyle;
        public string skillIcon;
        public string preCastSpr;
        public string manCastSnd;
        public string fmCastSnd;
        public int stateSpecialId;
        public int statePriority;
        public bool isAura;
        public int lrSkill;
        public bool needShadow;
        public int attackRadius;
        public int maxShadowNum;
        public int missilesGenerate;
        public int missilesGenerateData;
        public int charClass;
        public int missilesForm;
        public int childSkillId;
        public int childSkillLevel;
        public int childSkillNum;
        public bool baseSkill;
        public int charAnimId;
        public int eventSkillLevel;
        public bool isMelee;
        public int waitTime;
        public bool isSaveCd;
        public bool clientSend;
        public int skillCostType;
        public int costValue;
        public int timePerCast;
        public int timePerCastOnHorse;
        public bool isPhysical;
        public bool targetOnly;
        public bool targetEnemy;
        public bool targetAlly;
        public bool targetSelf;
        public bool targetOther;
        public bool targetObj;
        public bool targetNoNpc;
        public bool byMissile;
        public bool isUseAttackRating;
        public int startEvent;
        public int startSkillId;
        public int flyEvent;
        public int flySkillId;
        public int flyEventTime;
        public int collideEvent;
        public int collideSkillId;
        public int vanishedEvent;
        public int vanishedSkillId;
        public int reqLevel;
        public int maxLevel;
        public int equipLimit;
        public int horseLimit;
        public bool doHurt;
        public bool weaponSkill;
        public int series;
        public string levelSetScript;
        public readonly List<(string setting, string data)> levelSettings = new();
        public string levelUpScript;
        public string skillDesc;
    }

    /// <summary>
    /// Parses PC ModSkills.txt into SkillDefinition entries. This ports full id
    /// coverage first; exact Lua formula evaluation can be layered later per skill.
    /// </summary>
    public static class PcModSkillParser
    {
        public const int ExpansionMinSkillId = 1216;

        public static List<PcModSkillRow> ParseFile(string absolutePath, int minSkillId = ExpansionMinSkillId)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<PcModSkillRow>();
            return ParseLines(File.ReadAllLines(absolutePath), minSkillId);
        }

        public static List<PcModSkillRow> ParseLines(IEnumerable<string> lines, int minSkillId = ExpansionMinSkillId)
        {
            var rows = new List<PcModSkillRow>();
            if (lines == null) return rows;
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var c = line.Split('\t');
                if (c.Length < 55) continue;
                int id = Int(c, 2);
                if (id < minSkillId) continue;
                var row = new PcModSkillRow
                {
                    skillName = Str(c, 0),
                    property = Str(c, 1),
                    skillId = id,
                    attrib = Int(c, 3),
                    skillStyle = Int(c, 4),
                    skillIcon = Str(c, 5),
                    preCastSpr = Str(c, 6),
                    manCastSnd = Str(c, 7),
                    fmCastSnd = Str(c, 8),
                    stateSpecialId = Int(c, 9),
                    statePriority = Int(c, 10),
                    isAura = Bool(c, 11),
                    lrSkill = Int(c, 12),
                    needShadow = Bool(c, 13),
                    attackRadius = Int(c, 14),
                    maxShadowNum = Int(c, 15),
                    missilesGenerate = Int(c, 16),
                    missilesGenerateData = Int(c, 17),
                    charClass = Int(c, 18),
                    missilesForm = Int(c, 19),
                    childSkillId = Int(c, 20),
                    childSkillLevel = Int(c, 21),
                    childSkillNum = Int(c, 22),
                    baseSkill = Bool(c, 23),
                    charAnimId = Int(c, 24),
                    eventSkillLevel = Int(c, 25),
                    isMelee = Bool(c, 26),
                    waitTime = Int(c, 27),
                    isSaveCd = Bool(c, 28),
                    clientSend = Bool(c, 29),
                    skillCostType = Int(c, 30),
                    costValue = Int(c, 31),
                    timePerCast = Int(c, 32),
                    timePerCastOnHorse = Int(c, 33),
                    isPhysical = Bool(c, 34),
                    targetOnly = Bool(c, 35),
                    targetEnemy = Bool(c, 36),
                    targetAlly = Bool(c, 37),
                    targetSelf = Bool(c, 38),
                    targetOther = Bool(c, 39),
                    targetObj = Bool(c, 40),
                    targetNoNpc = Bool(c, 41),
                    byMissile = Bool(c, 42),
                    isUseAttackRating = Bool(c, 43),
                    startEvent = Int(c, 44),
                    startSkillId = Int(c, 45),
                    flyEvent = Int(c, 46),
                    flySkillId = Int(c, 47),
                    flyEventTime = Int(c, 48),
                    collideEvent = Int(c, 49),
                    collideSkillId = Int(c, 50),
                    vanishedEvent = Int(c, 51),
                    vanishedSkillId = Int(c, 52),
                    reqLevel = Int(c, 53),
                    maxLevel = Int(c, 54),
                    equipLimit = Int(c, 55),
                    horseLimit = Int(c, 56),
                    doHurt = Bool(c, 57),
                    weaponSkill = Bool(c, 58),
                    series = Int(c, 69),
                    levelSetScript = Str(c, 71),
                    levelUpScript = Str(c, 112),
                    skillDesc = Str(c, 113),
                };
                for (int i = 72; i + 1 < Math.Min(c.Length, 112); i += 2)
                {
                    var setting = Str(c, i);
                    var data = Str(c, i + 1);
                    if (!string.IsNullOrEmpty(setting) || !string.IsNullOrEmpty(data))
                        row.levelSettings.Add((setting, data));
                }
                rows.Add(row);
            }
            return rows;
        }

        public static SkillCatalog CreateCatalogFromFile(string absolutePath, IAssetRegistry assets = null, int minSkillId = ExpansionMinSkillId)
        {
            var catalog = new SkillCatalog(assets);
            foreach (var row in ParseFile(absolutePath, minSkillId))
                catalog.Register(ToSkillDefinition(row));
            return catalog;
        }

        public static SkillDefinition ToSkillDefinition(PcModSkillRow row)
        {
            if (row == null) return null;
            var maxLevel = row.maxLevel > 0 ? row.maxLevel : 1;
            var skill = new SkillDefinition
            {
                skillId = row.skillId,
                nameRaw = row.skillName,
                nameNormalized = NormalizeVietnamese(row.skillName),
                reqLevel = Math.Max(0, row.reqLevel),
                maxLevel = maxLevel,
                cost = row.costValue,
                skillCostType = row.skillCostType,
                timePerCast = row.timePerCast,
                waitTime = row.waitTime,
                attackRadius = row.attackRadius,
                isPhysical = row.isPhysical,
                isMelee = row.isMelee,
                isAura = row.isAura || row.skillStyle == 14,
                stateSpecialId = row.stateSpecialId,
                skillStyle = ToSkillStyle(row.skillStyle),
                faction = ToFaction(row.charClass, row.levelSetScript),
                missileForm = ToMissileForm(row.missilesForm),
                childSkillId = row.childSkillId,
                childSkillLevel = row.childSkillLevel,
                childSkillNum = row.childSkillNum,
                baseSkill = row.baseSkill,
                charAnimId = row.charAnimId,
                targetOnly = row.targetOnly,
                targetEnemy = row.targetEnemy,
                targetAlly = row.targetAlly,
                targetSelf = row.targetSelf,
                targetObj = row.targetObj,
                byMissile = row.byMissile,
                isUseAttackRating = row.isUseAttackRating,
                doHurt = row.doHurt,
                weaponSkill = row.weaponSkill,
                equipLimit = row.equipLimit,
                horseLimit = row.horseLimit,
                missilesGenerate = row.missilesGenerate,
                missilesGenerateData = row.missilesGenerateData,
                iconSourceId = Sprite(row.skillIcon),
                effectSourceId = Sprite(row.preCastSpr),
            };

            AttachMissileSpriteFromPcRegistry(skill);

            var level = new SkillLevelData { level = 1 };
            if (row.costValue != 0)
                level.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, row.costValue, 0, 0));
            foreach (var entry in row.levelSettings)
            {
                if (TryMapMagicKind(entry.setting, out var kind))
                    AddMappedAttribute(level, kind, row.skillStyle, entry.data);
                else if (!string.IsNullOrEmpty(entry.setting))
                    skill.warnings.Add($"ModSkill {row.skillId} has unimplemented Lua setting {entry.setting} ({entry.data})");
            }
            skill.pcLevelData.Add(level);
            if (level.First(MagicAttributeKind.PhysicsDamageV) is SkillMagicAttribute dmg)
                skill.damageLevels.Add(new SkillDamageLevel { level = 1, baseDamage = dmg.value3, attackRatio = 1f, isPhysical = row.isPhysical });
            return skill;
        }

        private static void AttachMissileSpriteFromPcRegistry(SkillDefinition skill)
        {
            if (skill == null || skill.missileForm == SkillMissileForm.None || skill.childSkillId <= 0)
                return;

            if (PcMissileRegistry.TryGet(skill.childSkillId, out var missile) && !string.IsNullOrWhiteSpace(missile.sprFile))
                skill.missileSpriteId = Sprite(missile.sprFile);
        }

        private static void AddMappedAttribute(SkillLevelData level, MagicAttributeKind kind, int style, string data)
        {
            var attr = new SkillMagicAttribute(kind, 0, 0, 0);
            // ModSkills mostly reference Lua functions; preserving kind is already useful
            // for runtime/category checks. Numeric constants are copied when present.
            if (int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                attr.value1 = v;
            if (kind == MagicAttributeKind.PhysicsDamageV || kind == MagicAttributeKind.FireDamageV || kind == MagicAttributeKind.ColdDamageV || kind == MagicAttributeKind.LightingDamageV || kind == MagicAttributeKind.PoisonDamageV)
                level.damage.Add(attr);
            else if (kind == MagicAttributeKind.SkillCostV)
                level.skill.Add(attr);
            else if (style == 2 || style == 3 || style == 14)
                level.state.Add(attr);
            else
                level.immediate.Add(attr);
        }

        private static CombatFaction ToFaction(int charClass, string levelSetScript)
        {
            int factionId = CombatFactionExt.FactionFromLuaScript(levelSetScript);
            if (factionId != CombatFactionExt.NoneId)
                return (CombatFaction)factionId;

            return charClass switch
            {
                1 => CombatFaction.Shaolin,
                2 => CombatFaction.EMei,
                3 => CombatFaction.TangMen,
                4 => CombatFaction.CaiBang,
                5 => CombatFaction.WuDang,
                _ => CombatFaction.None,
            };
        }

        private static PcSkillStyle ToSkillStyle(int style) => style switch
        {
            1 => PcSkillStyle.Melee,
            2 => PcSkillStyle.InitiativeNpcState,
            3 => PcSkillStyle.PassivityNpcState,
            4 => PcSkillStyle.Summon,
            14 => PcSkillStyle.InitiativeNpcState,
            _ => PcSkillStyle.Missiles,
        };

        private static SkillMissileForm ToMissileForm(int form) => form switch
        {
            1 => SkillMissileForm.Single,
            2 => SkillMissileForm.Fan,
            3 => SkillMissileForm.Surround,
            4 => SkillMissileForm.Chain,
            _ => SkillMissileForm.None,
        };

        private static bool TryMapMagicKind(string raw, out MagicAttributeKind kind)
        {
            kind = default;
            switch ((raw ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "physicsdamage_v": kind = MagicAttributeKind.PhysicsDamageV; return true;
                case "firedamage_v": kind = MagicAttributeKind.FireDamageV; return true;
                case "colddamage_v": kind = MagicAttributeKind.ColdDamageV; return true;
                case "lightingdamage_v": kind = MagicAttributeKind.LightingDamageV; return true;
                case "poisondamage_v": kind = MagicAttributeKind.PoisonDamageV; return true;
                case "physicsenhance_p": kind = MagicAttributeKind.PhysicsEnhanceP; return true;
                case "attackrating_p": kind = MagicAttributeKind.AttackRatingP; return true;
                case "addphysicsdamage_p": kind = MagicAttributeKind.AddPhysicsDamageP; return true;
                case "attackratingenhance_p": kind = MagicAttributeKind.AttackRatingEnhanceP; return true;
                case "deadlystrikeenhance_p": kind = MagicAttributeKind.DeadlyStrikeEnhanceP; return true;
                case "deadlystrike_p": kind = MagicAttributeKind.DeadlyStrikeP; return true;
                case "stun_p": kind = MagicAttributeKind.StunP; return true;
                case "lifemax_p": kind = MagicAttributeKind.LifeMaxP; return true;
                case "manamax_p": kind = MagicAttributeKind.ManaMaxP; return true;
                case "skill_cost_v": kind = MagicAttributeKind.SkillCostV; return true;
                case "fastwalkrun_p": kind = MagicAttributeKind.FastWalkRunP; return true;
                case "allres_p": kind = MagicAttributeKind.AllResP; return true;
                case "adddefense_v": kind = MagicAttributeKind.AddDefenseV; return true;
                default: return false;
            }
        }

        private static SourceAssetId Sprite(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            return new SourceAssetId { sourcePath = path, resourceKind = ResourceKind.Sprite, uid = path.GetHashCode(), discoveryTool = DiscoveryTool.Runtime, evidenceNote = "ModSkills.txt" };
        }

        private static string NormalizeVietnamese(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            // Keep already-Vietnamese strings. Mojibake/CJK rows remain raw for audit.
            return raw.Replace("C�n Kh�n", "Càn Khôn")
                      .Replace("Kim Quy�n", "Kim Quyền")
                      .Replace("Thi�n", "Thiên")
                      .Replace("Sinh l�c", "Sinh lực")
                      .Replace("N�i l�c", "Nội lực");
        }

        private static string Str(string[] c, int i) => i >= 0 && i < c.Length ? (c[i] ?? string.Empty).Trim() : string.Empty;
        private static int Int(string[] c, int i) => int.TryParse(Str(c, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        private static bool Bool(string[] c, int i) => Int(c, i) != 0;
    }
}
