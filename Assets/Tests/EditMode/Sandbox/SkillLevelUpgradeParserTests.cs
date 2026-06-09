using NUnit.Framework;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class SkillLevelUpgradeParserTests
    {
        [Test]
        public void PcConfigParser_PreservesLevelUpAndLvlSetScripts()
        {
            string header = "SkillName	Property	SkillId	Attrib	SkillStyle	SkillIcon	PreCastSpr	ManCastSnd	FMCastSnd	StateSpecialId	StatePriority	IsAura	LRSkill	NeedShadow	AttackRadius	MaxShadowNum	MslsGenerate	MslsGenerateData	CharClass	MisslesForm	ChildSkillId	ChildSkillLevel	ChildSkillNum	BaseSkill	CharAnimId	EventSkillLevel	IsMelee	WaitTime	ClientSend	SkillCostType	CostValue	TimePerCast	TimePerCastOnHorse	IsPhysical	TargetOnly	TargetEnemy	TargetAlly	TargetSelf	TargetOther	TargetObj	TargetNoNpc	ByMissle	IsUseAR	StartEvent	StartSkillId	FlyEvent	FlySkillId	FlyEventTime	CollideEvent	CollidSkillId	VanishedEvent	VanishedSkillId	ReqLevel	MaxLevel	EqtLimit	HorseLimit	DoHurt	WeaponSkill	Param1	Param1Memo	Param2	Param2Memo	StopWhenMove	HeelAtParent	RelativePosType	PeaceCanUse	ShowEvent	IsExpSkill	Series	ShowAddition	LvlSetScript	LvlSetting1	LvlData1	LvlSetting2	LvlData2	LvlSetting3	LvlData3	LvlSetting4	LvlData4	LvlSetting5	LvlData5	LvlSetting6	LvlData6	LvlSetting7	LvlData7	LvlSetting8	LvlData8	LvlSetting9	LvlData9	LvlSetting10	LvlData10	LvlSetting11	LvlData11	LvlSetting12	LvlData12	LvlSetting13	LvlData13	LvlSetting14	LvlData14	LvlSetting15	LvlData15	LvlSetting16	LvlData16	LvlSetting17	LvlData17	LvlSetting18	LvlData18	LvlSetting19	LvlData19	LvlSetting20	LvlData20	LevelUpScript	SkillDesc";
            var cols = new string[113];
            cols[0] = "Phổ Độ Chúng Sinh";
            cols[2] = "332";
            cols[4] = "0";
            cols[5] = "icon.spr";
            cols[18] = "2";
            cols[52] = "80";
            cols[53] = "20";
            cols[70] = @"\script\skill\emei.lua";
            cols[111] = @"\script\skill\lvlup_pudu_zhongsheng.lua";
            cols[112] = "desc";
            string[] lines = { header, string.Join("	", cols) };

            var rows = PcConfigParser.ParseSkillsLines(lines);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(332, rows[0].skillId);
            Assert.AreEqual("\\script\\skill\\emei.lua", rows[0].lvlSetScript);
            Assert.AreEqual("\\script\\skill\\lvlup_pudu_zhongsheng.lua", rows[0].levelUpScript);
            Assert.AreEqual(80, rows[0].reqLevel);
            Assert.AreEqual(20, rows[0].maxLevel);
        }

        [Test]
        public void LevelUpScriptCatalog_ContainsRepresentativePcRules()
        {
            var catalog = SkillLevelUpScriptCatalog.CreateDefault();

            AssertRepresentative(catalog, 332, "\\script\\skill\\lvlup_pudu_zhongsheng.lua", 93, 89, 86, 92, 282);
            AssertRepresentative(catalog, 351, "\\script\\skill\\lvlup_luanhuan_ji.lua", 347, 303, 343, 345, 349);
            AssertRepresentative(catalog, 390, "\\script\\skill\\lvlup_duanjin_fugu.lua", 67, 70, 64, 356, 72);
            AssertRepresentative(catalog, 391, "\\script\\skill\\lvlup_shehun_luanxin.lua", 136, 137, 140, 364, 143);
            AssertRepresentative(catalog, 394, "\\script\\skill\\lvlup_zuixian_cuogu.lua", 392, 174, 393, 175, 90);
            AssertRepresentative(catalog, 1110, "\\script\\skill\\lvlup_pililuanhuan_ji.lua", 45, 351);
        }

        private static void AssertRepresentative(SkillLevelUpScriptCatalog catalog, int skillId, string script, params int[] prerequisiteIds)
        {
            var rule = catalog.Resolve(skillId);
            Assert.IsNotNull(rule, $"missing {skillId}");
            Assert.AreEqual(script, rule.levelUpScript);
            Assert.AreEqual(prerequisiteIds.Length, rule.prerequisites.Count);
            for (int i = 0; i < prerequisiteIds.Length; i++)
                Assert.AreEqual(prerequisiteIds[i], rule.prerequisites[i].skillId);
        }
    }
}
