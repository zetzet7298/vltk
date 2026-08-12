// -----------------------------------------------------------------------------
// VLTK Mobile — ST-00.1 PC Config Manifest & Parser Tooling
// Source: Assets/StreamingAssets/Reference/PcSkills.txt, PcNpcS.txt, PcMissles.txt
// Parses tab-separated PC config files into Unity models.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Model;

namespace VLTK.Core
{
    // ─── Missile data (cross-reference from PcMissles.txt) ──────────────────

    [Serializable]
    public class PcMissileEntry
    {
        public int missileId;
        public string nameRaw;
        public string nameNormalized;
        public int speed;            // Speed column
        public int lifetime;         // LifeTime column
        public int count;            // Count column
        public int minRadius;        // MinRadius
        public int maxRadius;        // MaxRadius
        public string sprFile;       // SprFile
        public int flyEventId;       // FlyEvent
        public int collideEventId;   // CollideEvent
        public int vanishEventId;    // VanishedEvent
        public List<string> warnings = new();
    }

    // ─── Config manifest (result of parsing all 3 files) ─────────────────────

    [Serializable]
    public class PcConfigManifest
    {
        public List<SkillDefinition> skills = new();
        public List<NpcTemplate> npcTemplates = new();
        public List<PcMissileEntry> missiles = new();

        /// <summary>Missile lookup by id.</summary>
        public Dictionary<int, PcMissileEntry> MissileById = new();

        /// <summary>NPC template lookup by id.</summary>
        public Dictionary<int, NpcTemplate> NpcById = new();

        /// <summary>Skill lookup by id.</summary>
        public Dictionary<int, SkillDefinition> SkillById = new();

        /// <summary>Build lookup dictionaries after parsing.</summary>
        public void BuildLookups()
        {
            MissileById.Clear();
            foreach (var m in missiles) MissileById[m.missileId] = m;

            NpcById.Clear();
            foreach (var n in npcTemplates) NpcById[n.templateId] = n;

            SkillById.Clear();
            foreach (var s in skills) SkillById[s.skillId] = s;
        }
    }

    // ─── Main parser ─────────────────────────────────────────────────────────

    public static class PcConfigParser
    {
        private const char SEP = '\t';

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Tải toàn bộ config PC: PcSkills.txt, PcNpcS.txt, PcMissles.txt.
        /// Trả về PcConfigManifest chứa skills, npcTemplates, missiles.
        /// </summary>
        public static PcConfigManifest LoadAll(string streamingAssetsPath)
        {
            var manifest = new PcConfigManifest();

            string refPath = Path.Combine(streamingAssetsPath, "Reference");

            // PcSkills.txt
            string skillFile = Path.Combine(refPath, "PcSkills.txt");
            if (File.Exists(skillFile))
            {
                manifest.skills = ParseSkills(skillFile);
                SubsystemLog.Info("PcConfig", $"Parsed {manifest.skills.Count} skills from PcSkills.txt");
            }
            else
            {
                SubsystemLog.Warn("PcConfig", $"PcSkills.txt not found at {skillFile}");
            }

            // PcNpcS.txt
            string npcFile = Path.Combine(refPath, "PcNpcS.txt");
            if (File.Exists(npcFile))
            {
                manifest.npcTemplates = ParseNpcTemplates(npcFile);
                SubsystemLog.Info("PcConfig", $"Parsed {manifest.npcTemplates.Count} NPC templates from PcNpcS.txt");
            }
            else
            {
                SubsystemLog.Warn("PcConfig", $"PcNpcS.txt not found at {npcFile}");
            }

            // PcMissles.txt
            string missileFile = Path.Combine(refPath, "PcMissles.txt");
            if (File.Exists(missileFile))
            {
                manifest.missiles = ParseMissiles(missileFile);
                SubsystemLog.Info("PcConfig", $"Parsed {manifest.missiles.Count} missiles from PcMissles.txt");
            }
            else
            {
                SubsystemLog.Warn("PcConfig", $"PcMissles.txt not found at {missileFile}");
            }

            // ModMissles.txt is a fallback/expansion source. Do not let it overwrite
            // already-localized PC missile rows that share the same ids.
            string modMissileFile = Path.Combine(refPath, "ModMissles.txt");
            if (File.Exists(modMissileFile))
            {
                var modMissiles = ParseMissiles(modMissileFile);
                int added = MergeMissilesWithoutOverwriting(manifest.missiles, modMissiles);
                SubsystemLog.Info("PcConfig", $"Parsed {modMissiles.Count} mod missiles from ModMissles.txt ({added} new ids merged)");
            }
            else
            {
                SubsystemLog.Warn("PcConfig", $"ModMissles.txt not found at {modMissileFile}");
            }

            manifest.BuildLookups();
            return manifest;
        }

        /// <summary>
        /// Merge fallback missile rows without replacing already parsed PC rows.
        /// Existing rows keep their localized name/path data; fallback rows only fill an
        /// empty sprite path or add truly new ids.
        /// </summary>
        public static int MergeMissilesWithoutOverwriting(List<PcMissileEntry> target, IEnumerable<PcMissileEntry> fallback)
        {
            if (target == null || fallback == null) return 0;

            var byId = new Dictionary<int, PcMissileEntry>();
            foreach (var missile in target)
            {
                if (missile != null) byId[missile.missileId] = missile;
            }

            int added = 0;
            foreach (var missile in fallback)
            {
                if (missile == null) continue;

                if (byId.TryGetValue(missile.missileId, out var existing))
                {
                    if (string.IsNullOrEmpty(existing.sprFile) && !string.IsNullOrEmpty(missile.sprFile))
                    {
                        existing.sprFile = missile.sprFile;
                    }
                    continue;
                }

                target.Add(missile);
                byId[missile.missileId] = missile;
                added++;
            }

            return added;
        }

        // ── PcSkills.txt parser ──────────────────────────────────────────────
        //
        // Canonical 114-col header (0-indexed):
        //   0  SkillName      1  Property         2  SkillId        3  Attrib
        //   4  SkillStyle     5  SkillIcon         6  PreCastSpr     7  ManCastSnd
        //   8  FMCastSnd      9  StateSpecialId   10  StatePriority  11 IsAura
        //  12  LRSkill        13 NeedShadow       14  AttackRadius   15 MaxShadowNum
        //  16  MslsGenerate   17 MslsGenerateData 18  CharClass     19 MisslesForm
        //  20  ChildSkillId   21 ChildSkillLevel  22  ChildSkillNum  23 BaseSkill
        //  24  CharAnimId     25 EventSkillLevel  26  IsMelee        27 WaitTime
        //  28  IsSaveCd      29 ClientSend        30  SkillCostType  31 CostValue
        //  32  TimePerCast    33 TimePerCastOnHorse 34 IsPhysical    35 TargetOnly
        //  36  TargetEnemy    37 TargetAlly       38  TargetSelf     39 TargetOther
        //  40  TargetObj      41 TargetNoNpc      42  ByMissle       43 IsUseAR
        //  44  StartEvent     45 StartSkillId     46  FlyEvent       47 FlySkillId
        //  48  FlyEventTime   49 CollideEvent      50  CollidSkillId  51 VanishedEvent
        //  52  VanishedSkillId 53 ReqLevel        54  MaxLevel       55 EqtLimit
        //  56  HorseLimit     57 DoHurt           58  WeaponSkill    59 Param1
        //  60  Param1Memo     61 Param2Memo       62  StopWhenMove   63 HeelAtParent
        //  64  RelativePosType 65 PeaceCanUse     66  ShowEvent      67 IsExpSkill
        // Canonical tail: 69 Series  70 ShowAddition  71 LvlSetScript  72 LvlSetting1
        //  72-111 LvlSetting1..20 / LvlData1..20  112 LevelUpScript  113 SkillDesc
        // Canonical 114-column sources insert IsSaveCd at 28; legacy 113-column rows omit it.
        // Header-name mapping keeps both layouts aligned without positional guessing.

        public static List<SkillDefinition> ParseSkills(string path)
        {
            return ParseSkillsLines(File.ReadAllLines(path));
        }

        public static List<SkillDefinition> ParseSkillsLines(IReadOnlyList<string> lines)
        {
            var result = new List<SkillDefinition>();
            if (lines == null || lines.Count < 2) return result;

            string[] header = lines[0].Split(SEP);
            var headerIndex = BuildHeaderIndex(header);

            int waitTimeIdx = HeaderCol(headerIndex, "WaitTime");
            int clientSendIdx = HeaderCol(headerIndex, "ClientSend");
            int skillCostTypeIdx = HeaderCol(headerIndex, "SkillCostType");
            int costIdx = HeaderCol(headerIndex, "CostValue");
            int timePerCastIdx = HeaderCol(headerIndex, "TimePerCast");
            int timePerCastOnHorseIdx = HeaderCol(headerIndex, "TimePerCastOnHorse");
            int isPhysicalIdx = HeaderCol(headerIndex, "IsPhysical");
            int targetOnlyIdx = HeaderCol(headerIndex, "TargetOnly");
            int targetEnemyIdx = HeaderCol(headerIndex, "TargetEnemy");
            int targetAllyIdx = HeaderCol(headerIndex, "TargetAlly");
            int targetSelfIdx = HeaderCol(headerIndex, "TargetSelf");
            int targetObjIdx = HeaderCol(headerIndex, "TargetObj");
            int byMissileIdx = HeaderCol(headerIndex, "ByMissle");
            int isUseAttackRatingIdx = HeaderCol(headerIndex, "IsUseAR");
            int reqLevelIdx = HeaderCol(headerIndex, "ReqLevel");
            int maxLevelIdx = HeaderCol(headerIndex, "MaxLevel");
            int eqtLimitIdx = HeaderCol(headerIndex, "EqtLimit");
            int horseLimitIdx = HeaderCol(headerIndex, "HorseLimit");
            int doHurtIdx = HeaderCol(headerIndex, "DoHurt");
            int weaponSkillIdx = HeaderCol(headerIndex, "WeaponSkill");
            int lvlSetScriptIdx = HeaderCol(headerIndex, "LvlSetScript");
            int lvlSetting1Idx = HeaderCol(headerIndex, "LvlSetting1");
            int levelUpScriptIdx = HeaderCol(headerIndex, "LevelUpScript");

            if (waitTimeIdx < 0 || clientSendIdx < 0 || skillCostTypeIdx < 0 || costIdx < 0 || timePerCastIdx < 0 ||
                timePerCastOnHorseIdx < 0 || isPhysicalIdx < 0 || targetOnlyIdx < 0 || targetEnemyIdx < 0 ||
                targetAllyIdx < 0 || targetSelfIdx < 0 || targetObjIdx < 0 || byMissileIdx < 0 ||
                isUseAttackRatingIdx < 0 || reqLevelIdx < 0 || maxLevelIdx < 0 || eqtLimitIdx < 0 ||
                horseLimitIdx < 0 || doHurtIdx < 0 || weaponSkillIdx < 0 || lvlSetScriptIdx < 0 ||
                lvlSetting1Idx < 0 || levelUpScriptIdx < 0)
            {
                return result;
            }

            for (int i = 1; i < lines.Count; i++)
            {
                // Preserve trailing tab-delimited empty fields. Some canonical rows
                // (for example SkillId 720) intentionally have an empty final
                // SkillDesc column; trimming the whole line collapses that column
                // and makes an otherwise complete row look truncated.
                string line = lines[i]?.TrimEnd('\r') ?? string.Empty;
                if (string.IsNullOrEmpty(line)) continue;

                string[] cols = line.Split(SEP);
                if (cols.Length < 54) continue; // Need at least up to MaxLevel.
                if (cols.Length <= levelUpScriptIdx || cols.Length <= lvlSetting1Idx) continue;

                var skill = new SkillDefinition();
                int ci = 0;

                skill.nameRaw = Col(cols, ref ci);                    // 0
                ci++; // 1 Property (skip)
                skill.skillId = IntCol(cols, ref ci);                 // 2
                ci++; // 3 Attrib (skip)
                skill.skillStyle = (PcSkillStyle)IntCol(cols, ref ci);// 4
                ci++; // 5 SkillIcon (skip, referenced as string)
                // [CaiBang-SoundParity 2026-06-18] cols 6/7/8 used to be skipped,
                // which silently dropped every PC skill cast sound (ManCastSnd/
                // FMCastSnd) and skill-level PreCastSpr. PC source: skills.txt cols
                // 6 PreCastSpr / 7 ManCastSnd / 8 FMCastSnd — consumed by KSkill::Cast
                // to fire the cast-frame sound + body precast SPR before missile spawn.
                string preCastSprPath = ColSafe(cols, ci);            // 6 PreCastSpr
                ci++;
                skill.manCastSndPath = ColSafe(cols, ci);             // 7 ManCastSnd
                ci++;
                skill.fmCastSndPath = ColSafe(cols, ci);              // 8 FMCastSnd
                ci++;
                if (!string.IsNullOrEmpty(preCastSprPath) && skill.effectSourceId == null)
                {
                    skill.effectSourceId = new SourceAssetId
                    {
                        sourcePath = preCastSprPath,
                        resourceKind = ResourceKind.Sprite,
                        uid = preCastSprPath.GetHashCode(),
                    };
                }
                skill.stateSpecialId = IntCol(cols, ref ci);          // 9
                ci++; // 10 StatePriority (skip)
                skill.isAura = IntCol(cols, ref ci) != 0;            // 11
                ci += 3; // 12-14 LRSkill, NeedShadow, AttackRadius
                skill.attackRadius = IntColSafe(cols, 14);
                ci += 2; // 15-16 MaxShadowNum, MslsGenerate
                skill.missilesGenerate = IntColSafe(cols, 16);
                skill.missilesGenerateData = IntColSafe(cols, 17);
                ci = 18;
                int charClass = IntCol(cols, ref ci);                 // 18 CharClass
                skill.missileForm = (SkillMissileForm)IntCol(cols, ref ci); // 19
                skill.childSkillId = IntCol(cols, ref ci);            // 20
                skill.childSkillLevel = IntCol(cols, ref ci);         // 21
                skill.childSkillNum = IntCol(cols, ref ci);           // 22
                skill.baseSkill = IntCol(cols, ref ci) != 0;          // 23
                skill.charAnimId = IntCol(cols, ref ci);              // 24
                ci++; // 25 EventSkillLevel (skip)
                skill.isMelee = IntCol(cols, ref ci) != 0;           // 26
                skill.waitTime = IntColSafe(cols, waitTimeIdx);      // 27 WaitTime
                skill.skillCostType = IntColSafe(cols, skillCostTypeIdx);
                skill.cost = IntColSafe(cols, costIdx);              // CostValue
                skill.timePerCast = IntColSafe(cols, timePerCastIdx);
                skill.timePerCastOnHorse = IntColSafe(cols, timePerCastOnHorseIdx);
                skill.isPhysical = IntColSafe(cols, isPhysicalIdx) != 0;
                skill.targetOnly = IntColSafe(cols, targetOnlyIdx) != 0;
                skill.targetEnemy = IntColSafe(cols, targetEnemyIdx) != 0;
                skill.targetAlly = IntColSafe(cols, targetAllyIdx) != 0;
                skill.targetSelf = IntColSafe(cols, targetSelfIdx) != 0;
                skill.targetObj = IntColSafe(cols, targetObjIdx) != 0;
                skill.byMissile = IntColSafe(cols, byMissileIdx) != 0;
                skill.isUseAttackRating = IntColSafe(cols, isUseAttackRatingIdx) != 0;
                skill.reqLevel = IntColSafe(cols, reqLevelIdx);
                skill.maxLevel = IntColSafe(cols, maxLevelIdx);
                int eqtLimit = IntColSafe(cols, eqtLimitIdx);
                skill.equipLimit = eqtLimit;
                skill.horseLimit = IntColSafe(cols, horseLimitIdx);
                skill.doHurt = IntColSafe(cols, doHurtIdx) != 0;
                skill.weaponSkill = IntColSafe(cols, weaponSkillIdx) != 0;

                string scriptPath = ColSafe(cols, lvlSetScriptIdx);
                int factionId = CombatFactionExt.FactionFromLuaScript(scriptPath);
                if (factionId == CombatFactionExt.NoneId)
                {
                    factionId = charClass switch
                    {
                        1 => CombatFactionExt.ShaolinId,
                        2 => CombatFactionExt.EMeiId,
                        3 => CombatFactionExt.TangMenId,
                        4 => CombatFactionExt.CaiBangId,
                        5 => CombatFactionExt.WuDangId,
                        _ => CombatFactionExt.NoneId
                    };
                }
                skill.lvlSetScript = scriptPath;
                skill.faction = (CombatFaction)factionId;

                skill.levelUpScript = ColSafe(cols, levelUpScriptIdx);

                // Vietnamese name: PcSkills.txt names are already Vietnamese-ized
                skill.nameNormalized = skill.nameRaw?.Trim();

                ParseLevelData(cols, skill, lvlSetting1Idx);

                // Icon reference
                string iconPath = ColSafe(cols, 5);
                if (!string.IsNullOrEmpty(iconPath) && iconPath.StartsWith("\\"))
                    iconPath = iconPath.TrimStart('\\');
                if (!string.IsNullOrEmpty(iconPath))
                    skill.iconSourceId = new SourceAssetId { sourcePath = iconPath };

                result.Add(skill);
            }

            return result;
        }

        // ── PcNpcS.txt parser ────────────────────────────────────────────────
        //
        // Header: Name Kind Camp Series Treasure HeadImage ClientOnly CorpseIdx
        //   RedLum GreenLum BlueLum NpcResType ArmorType HelmType WeaponType HorseType
        //   RideHorse StandFrame StandFrame1 DeathFrame WalkFrame RunFrame HurtFrame
        //   Skill1 Level1 Skill2 Level2 Skill3 Level3 Skill4 Level4
        //   ActionScript LevelScript ExpParam ExpParam1-3 LifeParam LifeParam1-3
        //   LifeReplenish ARParam ARParam1-3 DefenseParam DefenseParam1-3
        //   MinDamageParam MinDamageParam1-3 MaxDamageParam MaxDamageParam1-3
        //   WalkSpeed RunSpeed AttackSpeed CastSpeed VisionRadius HitRecover
        //   ActiveRadius AIMode AIParam1-9
        //   FireResist ColdResist LightResist PoisonResist PhysicsResist
        //   FireResistMax ColdResistMax LightResistMax PoisonResistMax PhysicsResistMax
        //   ReviveFrame Stature DropRateFile

        public static List<NpcTemplate> ParseNpcTemplates(string path)
        {
            var result = new List<NpcTemplate>();
            string[] lines = File.ReadAllLines(path);
            if (lines.Length < 2) return result;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] cols = line.Split(SEP);
                if (cols.Length < 10) continue;

                var npc = new NpcTemplate();
                int ci = 0;

                npc.nameRaw = Col(cols, ref ci);                       // 0 Name
                npc.kind = IntCol(cols, ref ci);                        // 1 Kind
                ci++; // 2 Camp (skip)
                npc.series = IntCol(cols, ref ci);                      // 3 Series
                ci += 7; // 4-10 Treasure..NpcResType (skip detailed)
                string resType = ColSafe(cols, 11);                     // 11 NpcResType
                ci += 14; // 12-25 ArmorType..HurtFrame
                ci += 8; // 26-33 Skill1-Level4 (skip)
                ci += 2; // 34-35 ActionScript, LevelScript
                ci += 4; // 36-39 ExpParam, ExpParam1-3

                // LifeParam: level formula params
                ci += 4; // 40-43 LifeParam, LifeParam1-3
                ci++; // 44 LifeReplenish

                // ARParam, DefenseParam, DamageParam
                ci += 12; // 45-56 AR/Defense/Damage params

                npc.walkSpeed = IntColSafe(cols, 57);                   // 57 WalkSpeed
                npc.runSpeed = IntColSafe(cols, 58);                    // 58 RunSpeed
                ci += 2; // 59-60 AttackSpeed, CastSpeed
                npc.visionRadius = IntColSafe(cols, 61);                // 61 VisionRadius
                ci++; // 62 HitRecover
                npc.activeRadius = IntColSafe(cols, 63);                // 63 ActiveRadius
                npc.aiMode = IntColSafe(cols, 64);                      // 64 AIMode

                // AIParam1-9
                npc.aiParams = new int[9];
                for (int j = 0; j < 9 && (65 + j) < cols.Length; j++)
                    npc.aiParams[j] = IntColSafe(cols, 65 + j);

                // Template id = row index (PC uses 0-based data row ids)
                npc.templateId = i - 1;

                // NpcResType as sprite reference
                npc.spriteClipRef = resType;

                // Vietnamese name will be set later by localization lookup
                npc.nameNormalized = npc.nameRaw;

                result.Add(npc);
            }

            return result;
        }

        // ── PcMissles.txt parser ─────────────────────────────────────────────

        public static List<PcMissileEntry> ParseMissiles(string path)
        {
            var result = new List<PcMissileEntry>();
            string[] lines = File.ReadAllLines(path);
            if (lines.Length < 2) return result;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] cols = line.Split(SEP);
                if (cols.Length < 12) continue;

                var missile = new PcMissileEntry();

                missile.missileId = IntColSafe(cols, 0);
                missile.nameRaw = ColSafe(cols, 1);
                missile.nameNormalized = missile.nameRaw?.Trim();
                missile.lifetime = IntColSafe(cols, 10);
                missile.speed = IntColSafe(cols, 11);
                missile.count = IntColSafe(cols, 14); // LoopPlay

                missile.minRadius = IntColSafe(cols, 6);
                missile.maxRadius = IntColSafe(cols, 8);

                string spr = ColSafe(cols, 29);
                if (string.IsNullOrEmpty(spr)) spr = ColSafe(cols, 32);
                if (string.IsNullOrEmpty(spr)) spr = ColSafe(cols, 35);
                if (string.IsNullOrEmpty(spr)) spr = ColSafe(cols, 38);
                missile.sprFile = spr;

                if (cols.Length > 18) missile.flyEventId = IntColSafe(cols, 18);
                if (cols.Length > 20) missile.collideEventId = IntColSafe(cols, 20);
                if (cols.Length > 21) missile.vanishEventId = IntColSafe(cols, 21);

                result.Add(missile);
            }

            return result;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string Col(string[] cols, ref int index)
        {
            if (index >= cols.Length) return "";
            return cols[index++].Trim();
        }

        private static string ColSafe(string[] cols, int index)
        {
            return index < cols.Length ? cols[index].Trim() : "";
        }

        private static int IntCol(string[] cols, ref int index)
        {
            if (index >= cols.Length) { index++; return 0; }
            string raw = cols[index++].Trim();
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            return 0;
        }

        private static int IntColSafe(string[] cols, int index)
        {
            if (index >= cols.Length) return 0;
            string raw = cols[index].Trim();
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            return 0;
        }

        private static Dictionary<string, int> BuildHeaderIndex(string[] header)
        {
            var map = new Dictionary<string, int>(header.Length, StringComparer.Ordinal);
            for (int i = 0; i < header.Length; i++)
            {
                if (!map.ContainsKey(header[i]))
                    map[header[i]] = i;
            }
            return map;
        }

        private static int HeaderCol(IReadOnlyDictionary<string, int> headerIndex, string name)
        {
            return headerIndex.TryGetValue(name, out int index) ? index : -1;
        }

        /// <summary>
        /// Parse LvlSetting1..20 / LvlData1..20 columns into SkillLevelData.
        /// Canonical PcSkills.txt slice shifts LvlSetting1 to 72; legacy 113-col
        /// fixtures keep it at 71. Column index comes from header map.
        /// </summary>
        private static void ParseLevelData(string[] cols, SkillDefinition skill, int lvlStartCol)
        {
            if (lvlStartCol < 0 || lvlStartCol >= cols.Length) return;

            int maxPairs = Math.Min(20, (cols.Length - lvlStartCol) / 2);

            for (int p = 0; p < maxPairs; p++)
            {
                int settingIdx = lvlStartCol + p * 2;
                int dataIdx = settingIdx + 1;

                string attrName = ColSafe(cols, settingIdx).Trim();
                string attrValue = ColSafe(cols, dataIdx).Trim();

                if (string.IsNullOrEmpty(attrName) || string.IsNullOrEmpty(attrValue))
                    continue;

                // Store as a generic level-data entry for now.
                // Full parsing of Lua curve data happens at runtime via PcSkillTuningRegistry.
                var ld = new SkillLevelData { level = p + 1 };
                var attr = ParseMagicAttribute(attrName, attrValue);
                if (attr != null)
                    ld.damage.Add(attr);

                if (ld.damage.Count > 0 || ld.immediate.Count > 0 || ld.state.Count > 0 || ld.skill.Count > 0)
                    skill.pcLevelData.Add(ld);
            }
        }

        private static SkillMagicAttribute ParseMagicAttribute(string attrName, string attrValue)
        {
            var kind = attrName.ToLowerInvariant() switch
            {
                "physicsenhance_p" => MagicAttributeKind.PhysicsEnhanceP,
                "attackrating_p" => MagicAttributeKind.AttackRatingP,
                "addphysicsdamage_p" => MagicAttributeKind.AddPhysicsDamageP,
                "attackratingenhance_p" => MagicAttributeKind.AttackRatingEnhanceP,
                "deadlystrikeenhance_p" => MagicAttributeKind.DeadlyStrikeEnhanceP,
                "lightingres_p" => MagicAttributeKind.LightingResP,
                "fireres_p" => MagicAttributeKind.FireResP,
                "poisonres_p" => MagicAttributeKind.PoisonResP,
                "coldres_p" => MagicAttributeKind.ColdResP,
                "physicsres_p" => MagicAttributeKind.PhysicsResP,
                "allres_p" => MagicAttributeKind.AllResP,
                "adddefense_v" => MagicAttributeKind.AddDefenseV,
                "confuse_p" => MagicAttributeKind.ConfuseP,
                "skill_cost_v" => MagicAttributeKind.SkillCostV,
                "physicsdamage_v" => MagicAttributeKind.PhysicsDamageV,
                "firedamage_v" => MagicAttributeKind.FireDamageV,
                "poisondamage_v" => MagicAttributeKind.PoisonDamageV,
                "lightingdamage_v" => MagicAttributeKind.LightingDamageV,
                "seriesdamage_p" => MagicAttributeKind.SeriesDamageP,
                "manashield_p" => MagicAttributeKind.ManaShieldP,
                "manamax_p" => MagicAttributeKind.ManaMaxP,
                "manareplenish_v" => MagicAttributeKind.ManaReplenishV,
                "lightingenhance_p" => MagicAttributeKind.LightingEnhanceP,
                "attackspeed_v" => MagicAttributeKind.AttackSpeedV,
                "castspeed_v" => MagicAttributeKind.CastSpeedV,
                "stealmana_p" => MagicAttributeKind.StealManaP,
                "deadlystrike_p" => MagicAttributeKind.DeadlyStrikeP,
                "stun_p" => MagicAttributeKind.StunP,
                "staminamax_p" => MagicAttributeKind.StaminaMaxP,
                "colddamage_v" => MagicAttributeKind.ColdDamageV,
                "ignoredefense_p" => MagicAttributeKind.IgnoreDefenseP,
                "badstatustimereduce_v" => MagicAttributeKind.BadStatusTimeReduceV,
                "addpoisondamage_v" => MagicAttributeKind.AddPoisonDamageV,
                "addcolddamage_v" => MagicAttributeKind.AddColdDamageV,
                // [CaiBang-FirePool 2026-07-17] PC has two distinct fire-add kinds (jx-pc
                //   KNpcAttribModify::AddFireMagicV vs AddFireDamageV). Do not conflate.
                "addfiremagic_v" => MagicAttributeKind.AddFireMagicV,
                "addfiredamage_v" => MagicAttributeKind.AddFireDamageV,
                "addlightingmagic_v" or "addlightingdamage_v" => MagicAttributeKind.AddLightingDamageV,
                "steallife_p" => MagicAttributeKind.StealLifeP,
                "lifereplenish_v" => MagicAttributeKind.LifeReplenishV,
                "stealstamina_p" => MagicAttributeKind.StealStaminaP,
                "firearmor_v" => MagicAttributeKind.FireResP,
                "poisonarmor_v" => MagicAttributeKind.PoisonResP,
                "physicsarmor_v" => MagicAttributeKind.PhysicsResP,
                "attackspeed_p" => MagicAttributeKind.AttackSpeedV,
                "castspeed_p" => MagicAttributeKind.CastSpeedV,
                "lifemax_p" or "lifemax_yan_p" => MagicAttributeKind.LifeMaxP,
                "fireenhance_p" => MagicAttributeKind.FireEnhanceP,
                "fastwalkrun_p" => MagicAttributeKind.FastWalkRunP,
                // [CaiBang-slistcache 2026-07-15] PC slistcache gaibang.lua new attribute kinds.
                "physicsres_yan_p" => MagicAttributeKind.PhysicsResYanP,
                "fireres_yan_p" => MagicAttributeKind.FireResYanP,
                "allres_yan_p" => MagicAttributeKind.AllResYanP,
                "returnres_p" => MagicAttributeKind.ReturnResP,
                "anti_do_hurt_p" => MagicAttributeKind.AntiDoHurtP,
                "fatallystrike_p" => MagicAttributeKind.FatallyStrikeP,
                "me2metaldamage_p" => MagicAttributeKind.Me2MetalDamageP,
                "metal2medamage_p" => MagicAttributeKind.Metal2MeDamageP,
                "anti_block_rate" => MagicAttributeKind.AntiBlockRate,
                _ => (MagicAttributeKind?)null,
            };

            if (kind == null) return null;
            return new SkillMagicAttribute(kind.Value, 0, 0, 0);
        }
    }
}
