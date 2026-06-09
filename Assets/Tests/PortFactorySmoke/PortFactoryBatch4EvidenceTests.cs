using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace VLTK.Tests.PortFactorySmoke
{
    public sealed class PortFactoryBatch4EvidenceTests
    {
        [Test]
        public void HongbaoOpenAndCityHongbao_ExposePcWeightedCatalogFacts()
        {
            object hongbao = LoadService("HongbaoService", "Reference/PcItemFull");
            object entries = InvokeInstance(hongbao, "GetAllHongbaos");
            object open = Activator.CreateInstance(SandboxType("HongbaoOpenResultService"), entries);

            Assert.That(IntMember(open, "Count"), Is.EqualTo(69));
            Assert.That(IntMember(open, "TotalProba"), Is.EqualTo(1000000));
            Assert.That(IntMember(open, "Type1Count"), Is.EqualTo(42));
            Assert.That(IntMember(open, "Type2Count"), Is.EqualTo(27));
            Assert.That(IntMember(open, "CostlyCount"), Is.EqualTo(15));
            Assert.That(IntMember(open, "LogCount"), Is.EqualTo(69));

            string itemFullDir = Path.Combine(ProjectRoot, "Assets/StreamingAssets/Reference/PcItemFull");
            object cityRegistry = InvokeStatic(SandboxType("PcCityHongbaoParser"), "BuildRegistry", itemFullDir);
            Assert.That(IntMember(cityRegistry, "Count"), Is.EqualTo(67));
            Assert.That(IntMember(cityRegistry, "TotalProba"), Is.EqualTo(1010000));
            Assert.That(IntMember(LoadService("CityHongbaoService", "Reference/PcItemFull"), "Count"), Is.EqualTo(67));
        }

        [Test]
        public void RoleValueAndMissionBattleScoring_ExposeBatch4Facts()
        {
            object roleValue = InvokeStatic(SandboxType("ItemExchangeRoleValueService"), "LoadFromStreamingAssets");
            Assert.That(IntMember(roleValue, "SectionCount"), Is.EqualTo(4));
            Assert.That(IntMember(roleValue, "KeyCount"), Is.EqualTo(35));
            Assert.That(IntMember(roleValue, "SkillValue"), Is.EqualTo(5000));
            Assert.That(Convert.ToInt32(InvokeInstance(roleValue, "GetJxbValueOrDefault", 281, -1)), Is.EqualTo(4000));

            object scoring = InvokeStatic(SandboxType("MissionBattleScoringService"), "LoadFromStreamingAssets");
            object fact = InvokeInstance(scoring, "Lookup", "Soldier", "General");
            Assert.That(IntMember(scoring, "RankCount"), Is.EqualTo(5));
            Assert.That(IntMember(scoring, "ComboCellCount"), Is.EqualTo(25));
            Assert.That(IntMember(scoring, "ScoreCellCount"), Is.EqualTo(25));
            Assert.That(IntMember(fact, "ComboValue"), Is.EqualTo(1));
            Assert.That(IntMember(fact, "ScoreValue"), Is.EqualTo(150));
        }

        [Test]
        public void TranslifeBonusAndClearSkillExecutor_ExposeBatch4Facts()
        {
            string pcTaskDir = Path.Combine(ProjectRoot, "Assets/StreamingAssets/Reference/PcTask");
            object translife = InvokeStatic(SandboxType("TranslifeLevelBonusService"), "FromDirectory", pcTaskDir);
            Assert.That(IntMember(translife, "SourceRowCount"), Is.EqualTo(41));
            Assert.That(IntMember(translife, "SourceHeaderColumnCount"), Is.EqualTo(29));
            Assert.That(IntMember(translife, "SourceBonusGroupCount"), Is.EqualTo(7));

            Type constantsType = SandboxType("ClearSkillMissionLifecycleConstants");
            object plan = InvokeStatic(constantsType, "PlanInitMission");
            object host = Activator.CreateInstance(SandboxType("RecordingClearSkillMissionLifecycleHost"));
            SetMember(host, "NextNpcId", 4321);
            object result = InvokeStatic(SandboxType("ClearSkillMissionLifecyclePlanExecutor"), "Replay", plan, host);

            Assert.That(BoolMember(result, "Succeeded"), Is.True);
            Assert.That(IntMember(Member(host, "Calls"), "Count"), Is.EqualTo(5));
        }

        [Test]
        public void CityWarTokenNpcSkillAndFactionMap_ExposeBatch4Facts()
        {
            object input = Activator.CreateInstance(SandboxType("CityWarChallengeTokenInput"));
            SetMember(input, "TodayTaskDate", 2669);
            SetMember(input, "StoredTaskDate", 2669);
            SetMember(input, "StoredDailyCount", 298);
            SetMember(input, "CurrentTongTotal", 0);

            object tokenTuple = Member(SandboxType("CityWarPcConstants"), "ChallengeTokenItem");
            object tokenUnit = Activator.CreateInstance(SandboxType("CityWarChallengeTokenUnit"), 501, tokenTuple, 2);
            object givenItems = Member(input, "GivenItems");
            givenItems.GetType().GetMethod("Add").Invoke(givenItems, new[] { tokenUnit });

            object tokenPlan = InvokeInstance(
                Activator.CreateInstance(SandboxType("CityWarChallengeTokenService")),
                "BuildTurnInPlan",
                input);
            Assert.That(BoolMember(tokenPlan, "Accepted"), Is.True);
            Assert.That(IntMember(tokenPlan, "DailyCountAfter"), Is.EqualTo(300));
            Assert.That(IntMember(tokenPlan, "DailyRemaining"), Is.EqualTo(0));

            object npcSkills = InvokeStatic(SandboxType("NpcSkillCatalogService"), "LoadFromStreamingAssets");
            Assert.That(IntMember(npcSkills, "Count"), Is.EqualTo(158));
            Assert.That(IntMember(npcSkills, "NpcScriptCount"), Is.EqualTo(145));
            Assert.That(IntMember(npcSkills, "BossNameCount"), Is.EqualTo(21));
            Assert.That(IntMember(npcSkills, "BossNameOnlyCount"), Is.EqualTo(13));

            object factionMaps = InvokeStatic(SandboxType("FactionMapService"), "LoadFromStreamingAssets");
            Assert.That(IntMember(factionMaps, "Count"), Is.EqualTo(33));
            Assert.That(IntMember(InvokeInstance(factionMaps, "GetBySourceTable", "citymap"), "Count"), Is.EqualTo(11));
            Assert.That(IntMember(InvokeInstance(factionMaps, "GetBySourceTable", "aDynMapCopyName"), "Count"), Is.EqualTo(7));
        }

        private static string ProjectRoot => Directory.GetCurrentDirectory();

        private static object LoadService(string typeName, string subdir)
        {
            return InvokeStatic(SandboxType(typeName), "LoadFromStreamingAssets", subdir);
        }

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, type.FullName + "." + methodName + " missing");
            return method.Invoke(null, BuildInvokeArgs(method, args));
        }

        private static object InvokeInstance(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, target.GetType().FullName + "." + methodName + " missing");
            return method.Invoke(target, BuildInvokeArgs(method, args));
        }

        private static object[] BuildInvokeArgs(MethodInfo method, object[] provided)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] args = new object[parameters.Length];
            provided = provided ?? Array.Empty<object>();
            for (int i = 0; i < args.Length; i++)
            {
                if (i < provided.Length)
                {
                    args[i] = provided[i];
                }
                else
                {
                    Assert.That(parameters[i].IsOptional, Is.True,
                        method.DeclaringType.FullName + "." + method.Name + " requires parameter " + parameters[i].Name);
                    args[i] = parameters[i].DefaultValue;
                }
            }
            return args;
        }

        private static Type SandboxType(string shortName)
        {
            string fullName = "VLTK.Sandbox." + shortName;
            Type type = TypeInAssembly("Assembly-CSharp", fullName) ?? TypeInAssembly("VLTK.Sandbox", fullName);
            if (type == null)
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(fullName);
                    if (type != null)
                        break;
                }
            }
            Assert.That(type, Is.Not.Null, fullName + " missing from loaded Unity project assemblies");
            return type;
        }

        private static Type TypeInAssembly(string assemblyName, string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == assemblyName)
                    return asm.GetType(fullName);
            }
            try
            {
                return Assembly.Load(assemblyName).GetType(fullName);
            }
            catch
            {
                return null;
            }
        }

        private static int IntMember(object targetOrType, string name)
        {
            return Convert.ToInt32(Member(targetOrType, name));
        }

        private static bool BoolMember(object targetOrType, string name)
        {
            return Convert.ToBoolean(Member(targetOrType, name));
        }

        private static object Member(object targetOrType, string name)
        {
            Type type = targetOrType as Type ?? targetOrType.GetType();
            BindingFlags flags = BindingFlags.Public | (targetOrType is Type ? BindingFlags.Static : BindingFlags.Instance);
            PropertyInfo prop = type.GetProperty(name, flags);
            if (prop != null)
                return prop.GetValue(targetOrType is Type ? null : targetOrType, null);
            FieldInfo field = type.GetField(name, flags);
            Assert.That(field, Is.Not.Null, type.FullName + "." + name + " missing");
            return field.GetValue(targetOrType is Type ? null : targetOrType);
        }

        private static void SetMember(object target, string name, object value)
        {
            Type type = target.GetType();
            PropertyInfo prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                prop.SetValue(target, value, null);
                return;
            }
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, type.FullName + "." + name + " missing");
            field.SetValue(target, value);
        }
    }
}
