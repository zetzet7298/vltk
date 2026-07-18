// -----------------------------------------------------------------------------
// VLTK Mobile — Compensation Index Runtime Service evidence tests.
// Validates: CompensationIndex.json (9 files) loads correctly at runtime,
// lookup by filename/rel_path works, directory enumeration correct,
// and CompensationService pipeline integration wired.
//
// PC source of truth:
//   - vng_event/denbu_baotri_5server/main.lua
//   - vng_event/denbutrongkhaihoan/main.lua
//   - vng_event/denbu_congthanh/{congthanh,head}.lua
//   - activitysys/config/37/{registe,head,variables,extend,config}.lua
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace VLTK.Tests.PortFactorySmoke
{
    public sealed class CompensationIndexRuntimeTests
    {
        // --- Evidence: Index file exists and loads ---

        [Test]
        public void CompensationIndexJson_ExistsInStreamingAssets()
        {
            string indexPath = Path.Combine(
                ProjectRoot,
                "Assets/StreamingAssets/Reference/PcCompensation/CompensationIndex.json");

            Assert.That(File.Exists(indexPath), Is.True,
                "CompensationIndex.json must exist in StreamingAssets/Reference/PcCompensation/");
        }

        [Test]
        public void CompensationIndexRuntimeService_LoadsNineIndexedFiles()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            Assert.That(IntMember(service, "Count"), Is.EqualTo(9),
                "CompensationIndex must contain exactly 9 indexed files");
            Assert.That(BoolMember(service, "IsLoaded"), Is.True);
        }

        // --- Evidence: Filename lookups ---

        [Test]
        public void GetByFilename_ResolvesMainLua_ToDenbuBaotriEntry()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object entry = InvokeInstance(service, "GetByFilename", "main.lua");
            Assert.That(entry, Is.Not.Null, "main.lua must resolve to an entry");

            Assert.That(StringMember(entry, "rel_path"),
                Is.EqualTo("vng_event/denbu_baotri_5server/main.lua"));
        }

        [Test]
        public void GetByFilename_ResolvesConfigLua_ToActivity37Config()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object entry = InvokeInstance(service, "GetByFilename", "config.lua");
            Assert.That(entry, Is.Not.Null, "config.lua must resolve to an entry");

            Assert.That(StringMember(entry, "rel_path"),
                Is.EqualTo("activitysys/config/37/config.lua"));
        }

        [Test]
        public void GetByFilename_ReturnsNullForUnknownFile()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object entry = InvokeInstance(service, "GetByFilename", "nonexistent.lua");
            Assert.That(entry, Is.Null);
        }

        // --- Evidence: Rel_path lookups ---

        [Test]
        public void GetByRelPath_ResolvesDenbuCongthanhCongthanh()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object entry = InvokeInstance(service, "GetByRelPath",
                "vng_event/denbu_congthanh/congthanh.lua");
            Assert.That(entry, Is.Not.Null);
            Assert.That(StringMember(entry, "filename"), Is.EqualTo("congthanh.lua"));
        }

        [Test]
        public void GetByRelPath_ResolvesActivity37Variables()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object entry = InvokeInstance(service, "GetByRelPath",
                "activitysys/config/37/variables.lua");
            Assert.That(entry, Is.Not.Null);
            Assert.That(StringMember(entry, "filename"), Is.EqualTo("variables.lua"));
        }

        // --- Evidence: Directory enumeration ---

        [Test]
        public void GetUniqueDirectories_ReturnsFourDirectories()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object dirs = InvokeInstance(service, "GetUniqueDirectories");
            int dirCount = IntMember(dirs, "Count");
            Assert.That(dirCount, Is.EqualTo(4),
                "Must have 4 unique directories: denbu_baotri_5server, " +
                "denbutrongkhaihoan, denbu_congthanh, activitysys/config/37");
        }

        [Test]
        public void CountByDirectoryPrefix_VngEvent_ReturnsFour()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            int count = Convert.ToInt32(
                InvokeInstance(service, "CountByDirectoryPrefix", "vng_event/"));
            Assert.That(count, Is.EqualTo(4),
                "vng_event/ should contain 4 files (2 main.lua + congthanh.lua + head.lua)");
        }

        [Test]
        public void CountByDirectoryPrefix_ActivitySys_ReturnsFive()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            int count = Convert.ToInt32(
                InvokeInstance(service, "CountByDirectoryPrefix", "activitysys/"));
            Assert.That(count, Is.EqualTo(5),
                "activitysys/ should contain 5 files (registe,head,variables,extend,config)");
        }

        // --- Evidence: GetAllByFilename returns multiple for "main.lua" ---

        [Test]
        public void GetAllByFilename_MainLua_ReturnsTwoEntries()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object allMain = InvokeInstance(service, "GetAllByFilename", "main.lua");
            Assert.That(IntMember(allMain, "Count"), Is.EqualTo(2),
                "main.lua appears in 2 directories (denbu_baotri_5server + denbutrongkhaihoan)");
        }

        // --- Evidence: CompensationService pipeline integration ---

        [Test]
        public void BuildCompensationService_WiresToExistingCompensationService()
        {
            object indexService = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object compService = InvokeInstance(indexService, "BuildCompensationService");
            Assert.That(compService, Is.Not.Null,
                "BuildCompensationService must return a non-null CompensationService");
        }

        // --- Evidence: All 9 entries are valid ---

        [Test]
        public void AllEntries_AreValidWithNonEmptyFields()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object all = Member(service, "AllEntries");
            int count = IntMember(all, "Count");
            Assert.That(count, Is.EqualTo(9));

            for (int i = 0; i < count; i++)
            {
                object entry = InvokeInstance(all, "get_Item", i);
                Assert.That(entry, Is.Not.Null, $"Entry[{i}] must not be null");
                Assert.That(StringMember(entry, "filename"), Is.Not.Empty,
                    $"Entry[{i}].filename must not be empty");
                Assert.That(StringMember(entry, "rel_path"), Is.Not.Empty,
                    $"Entry[{i}].rel_path must not be empty");
            }
        }

        // --- Evidence: Entry validity check ---

        [Test]
        public void CompensationIndexEntry_IsValid_ReturnsTrueForLoadedEntries()
        {
            object service = InvokeStatic(
                SandboxType("CompensationIndexRuntimeService"),
                "LoadFromStreamingAssets");

            object all = Member(service, "AllEntries");
            for (int i = 0; i < IntMember(all, "Count"); i++)
            {
                object entry = InvokeInstance(all, "get_Item", i);
                Assert.That(BoolMember(entry, "IsValid"), Is.True,
                    $"Entry[{i}] must report IsValid=true");
            }
        }

        // --- Evidence: LoadFromJson works for test injection ---

        [Test]
        public void LoadFromJson_ParsesInjectedJsonCorrectly()
        {
            object service = Activator.CreateInstance(SandboxType("CompensationIndexRuntimeService"));

            string testJson = "[{\"path\":\"/test/a.lua\",\"filename\":\"a.lua\",\"rel_path\":\"test/a.lua\"}," +
                              "{\"path\":\"/test/b.lua\",\"filename\":\"b.lua\",\"rel_path\":\"test/b.lua\"}]";

            InvokeInstance(service, "LoadFromJson", testJson);
            Assert.That(IntMember(service, "Count"), Is.EqualTo(2));
            Assert.That(BoolMember(service, "IsLoaded"), Is.True);

            object entry = InvokeInstance(service, "GetByFilename", "a.lua");
            Assert.That(entry, Is.Not.Null);
            Assert.That(StringMember(entry, "rel_path"), Is.EqualTo("test/a.lua"));
        }

        // --- Evidence: LoadFromJson handles empty/null gracefully ---

        [Test]
        public void LoadFromJson_EmptyJson_DoesNotCrash()
        {
            object service = Activator.CreateInstance(SandboxType("CompensationIndexRuntimeService"));

            InvokeInstance(service, "LoadFromJson", "");
            Assert.That(IntMember(service, "Count"), Is.EqualTo(0));
            Assert.That(BoolMember(service, "IsLoaded"), Is.False);
        }

        [Test]
        public void LoadFromJson_EmptyArray_DoesNotCrash()
        {
            object service = Activator.CreateInstance(SandboxType("CompensationIndexRuntimeService"));

            InvokeInstance(service, "LoadFromJson", "[]");
            Assert.That(IntMember(service, "Count"), Is.EqualTo(0));
        }

        // =====================================================================
        // Helpers — mirrors PortFactoryBatch4EvidenceTests pattern
        // =====================================================================

        private static string ProjectRoot => Directory.GetCurrentDirectory();

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null,
                type.FullName + "." + methodName + " missing");
            return method.Invoke(null, BuildInvokeArgs(method, args));
        }

        private static object InvokeInstance(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null,
                target.GetType().FullName + "." + methodName + " missing");
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
                        method.DeclaringType.FullName + "." + method.Name +
                        " requires parameter " + parameters[i].Name);
                    args[i] = parameters[i].DefaultValue;
                }
            }
            return args;
        }

        private static Type SandboxType(string shortName)
        {
            string fullName = "VLTK.Sandbox." + shortName;
            Type type = TypeInAssembly("VLTK.Sandbox.Runtime", fullName)
                      ?? TypeInAssembly("Assembly-CSharp", fullName);
            if (type == null)
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(fullName);
                    if (type != null) break;
                }
            }
            Assert.That(type, Is.Not.Null,
                fullName + " missing from loaded Unity project assemblies");
            return type;
        }

        private static Type TypeInAssembly(string assemblyName, string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == assemblyName)
                    return asm.GetType(fullName);
            }
            try { return Assembly.Load(assemblyName).GetType(fullName); }
            catch { return null; }
        }

        private static int IntMember(object target, string name)
            => Convert.ToInt32(Member(target, name));

        private static bool BoolMember(object target, string name)
            => Convert.ToBoolean(Member(target, name));

        private static string StringMember(object target, string name)
            => Convert.ToString(Member(target, name));

        private static object Member(object targetOrType, string name)
        {
            Type type = targetOrType as Type ?? targetOrType.GetType();
            BindingFlags flags = BindingFlags.Public |
                (targetOrType is Type ? BindingFlags.Static : BindingFlags.Instance);
            PropertyInfo prop = type.GetProperty(name, flags);
            if (prop != null)
                return prop.GetValue(targetOrType is Type ? null : targetOrType, null);
            FieldInfo field = type.GetField(name, flags);
            Assert.That(field, Is.Not.Null,
                type.FullName + "." + name + " missing");
            return field.GetValue(targetOrType is Type ? null : targetOrType);
        }
    }
}
