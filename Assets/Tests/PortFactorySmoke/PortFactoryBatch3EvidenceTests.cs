using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace VLTK.Tests.PortFactorySmoke
{
    public sealed class PortFactoryBatch3EvidenceTests
    {
        [Test]
        public void Batch3SandboxServices_AreDiscoverableInAssemblyCSharp()
        {
            AssertLoadMethod("TranslifeLevelService");
            AssertLoadMethod("HongbaoService");
            AssertLoadMethod("ItemExchangeSourceCatalogService");
            AssertLoadMethod("MissionBattleConfigService");
            Assert.That(SandboxType("CityWarTransferRouteService"), Is.Not.Null);
            Assert.That(SandboxType("CityWarPcConstants"), Is.Not.Null);
        }

        [Test]
        public void TranslifeAndHongbao_LoadPcRowCounts()
        {
            object translife = LoadService("TranslifeLevelService", "Reference/PcTask");
            object hongbao = LoadService("HongbaoService", "Reference/PcItemFull");

            Assert.That(IntMember(translife, "Count"), Is.EqualTo(41));
            Assert.That(IntMember(hongbao, "Count"), Is.EqualTo(69));
        }

        [Test]
        public void ItemExchangeAndMissionBattle_LoadPcMatrixCounts()
        {
            object itemExchange = LoadService("ItemExchangeSourceCatalogService", "Reference/PcItemExchange");
            object missionBattle = LoadService("MissionBattleConfigService", "Reference/PcBattlefield/MissionBattle");

            Assert.That(IntMember(itemExchange, "NormalRowCount"), Is.EqualTo(7334));
            Assert.That(IntMember(itemExchange, "RareRowCount"), Is.EqualTo(480));
            Assert.That(IntMember(itemExchange, "LevelExpRowCount"), Is.EqualTo(200));
            Assert.That(IntMember(itemExchange, "LevelLeadExpRowCount"), Is.EqualTo(100));
            Assert.That(IntMember(itemExchange, "RoleValueKeyCount"), Is.EqualTo(35));
            Assert.That(IntMember(missionBattle, "Count"), Is.EqualTo(5));
            Assert.That(IntMember(missionBattle, "ComboCellCount"), Is.EqualTo(25));
            Assert.That(IntMember(missionBattle, "ScoreCellCount"), Is.EqualTo(25));
        }

        [Test]
        public void CityWarTransferRoute_ReflectsPcMapAndCampSplit()
        {
            Type serviceType = SandboxType("CityWarTransferRouteService");
            Type sideType = SandboxType("CityWarCardSide");

            Assert.That(IntMember(serviceType, "DefenderTransferMapId"), Is.EqualTo(222));
            Assert.That(IntMember(serviceType, "AttackerTransferMapId"), Is.EqualTo(223));
            Assert.That(IntMember(serviceType, "DefenderCamp"), Is.EqualTo(1));
            Assert.That(IntMember(serviceType, "AttackerCamp"), Is.EqualTo(2));

            object defender = Enum.Parse(sideType, "Defender");
            object attacker = Enum.Parse(sideType, "Attacker");
            AssertAcceptedRoute(defender, 222, 1);
            AssertAcceptedRoute(attacker, 223, 2);
        }

        private static void AssertLoadMethod(string typeName)
        {
            Assert.That(SandboxType(typeName).GetMethod("LoadFromStreamingAssets", BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
        }

        private static object LoadService(string typeName, string subdir)
        {
            Type type = SandboxType(typeName);
            MethodInfo method = type.GetMethod("LoadFromStreamingAssets", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, typeName + ".LoadFromStreamingAssets missing");
            return method.Invoke(null, new object[] { subdir });
        }

        private static void AssertAcceptedRoute(object side, int expectedMap, int expectedCamp)
        {
            Type serviceType = SandboxType("CityWarTransferRouteService");
            Type inputType = SandboxType("CityWarTransferRouteInput");
            Type constantsType = SandboxType("CityWarPcConstants");

            object input = Activator.CreateInstance(inputType);
            inputType.GetField("CityId").SetValue(input, 1);

            MethodInfo cardMethod = constantsType.GetMethod("GetCardItemIdForCity", BindingFlags.Public | BindingFlags.Static);
            int cardId = (int)cardMethod.Invoke(null, new object[] { 1, side });
            object itemCounts = inputType.GetField("ItemCounts").GetValue(input);
            itemCounts.GetType().GetMethod("Add", new[] { typeof(int), typeof(int) }).Invoke(itemCounts, new object[] { cardId, 1 });

            object service = Activator.CreateInstance(serviceType);
            MethodInfo routeMethod = serviceType.GetMethod("BuildNpcRoute", BindingFlags.Public | BindingFlags.Instance);
            object route = routeMethod.Invoke(service, new object[] { input, side });

            Assert.That(BoolMember(route, "Accepted"), Is.True);
            Assert.That(IntMember(route, "TransferMapId"), Is.EqualTo(expectedMap));
            Assert.That(IntMember(route, "RouteCamp"), Is.EqualTo(expectedCamp));
            Assert.That(BoolMember(route, "MatchedCard"), Is.True);
            IList cells = (IList)Member(route, "PossibleNewWorlds");
            Assert.That(cells.Count, Is.EqualTo(2));
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
    }
}
