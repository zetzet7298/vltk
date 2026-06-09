// -----------------------------------------------------------------------------
// VLTK Mobile — DialogSys Runtime Smoke Tests (PortFactorySmoke)
// Verifies DialogSysRuntimeService loads index, counts, and lookup.
// PC source: script/dailogsys (5 core Lua scripts).
// Evidence standard: exact PC counts + representative behavior assertions.
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace VLTK.Tests.PortFactorySmoke
{
    public sealed class DialogSysRuntimeTests
    {
        // ── JSON Index: 5 core Lua scripts ───────────────────────────────────

        [Test]
        public void DialogSysRuntime_Loads5CoreLuaScriptsFromJsonIndex()
        {
            object svc = LoadRuntime();
            Assert.That(IntMember(svc, "JsonEntryCount"), Is.EqualTo(5));
        }

        [Test]
        public void DialogSysRuntime_JsonIndexTotalSizeMatchesPcSource()
        {
            object svc = LoadRuntime();
            // 951 + 2549 + 1764 + 2783 + 3776 = 11823 bytes
            Assert.That(IntMember(svc, "TotalJsonSizeBytes"), Is.EqualTo(11823));
        }

        [Test]
        public void DialogSysRuntime_HasAll5CoreScriptNames()
        {
            object svc = LoadRuntime();
            Assert.That(BoolMember(svc, "HasScript"), Is.False); // null arg = false
            Assert.IsTrue(BoolInvoke(svc, "HasScript", "g_dialog.lua"));
            Assert.IsTrue(BoolInvoke(svc, "HasScript", "dailog.lua"));
            Assert.IsTrue(BoolInvoke(svc, "HasScript", "dailogsay.lua"));
            Assert.IsTrue(BoolInvoke(svc, "HasScript", "dialogoption.lua"));
            Assert.IsTrue(BoolInvoke(svc, "HasScript", "composeoption.lua"));
        }

        [Test]
        public void DialogSysRuntime_JsonEntryLookup_ReturnsCorrectSize()
        {
            object svc = LoadRuntime();
            object gDialog = InvokeInstance(svc, "GetJsonEntryByFileName", "g_dialog.lua");
            Assert.That(gDialog, Is.Not.Null);
            Assert.That(IntMember(gDialog, "SizeBytes"), Is.EqualTo(951));

            object dailog = InvokeInstance(svc, "GetJsonEntryByFileName", "dailog.lua");
            Assert.That(dailog, Is.Not.Null);
            Assert.That(IntMember(dailog, "SizeBytes"), Is.EqualTo(2549));

            object dailogSay = InvokeInstance(svc, "GetJsonEntryByFileName", "dailogsay.lua");
            Assert.That(dailogSay, Is.Not.Null);
            Assert.That(IntMember(dailogSay, "SizeBytes"), Is.EqualTo(3776));

            object dialogOption = InvokeInstance(svc, "GetJsonEntryByFileName", "dialogoption.lua");
            Assert.That(dialogOption, Is.Not.Null);
            Assert.That(IntMember(dialogOption, "SizeBytes"), Is.EqualTo(1764));

            object composeOption = InvokeInstance(svc, "GetJsonEntryByFileName", "composeoption.lua");
            Assert.That(composeOption, Is.Not.Null);
            Assert.That(IntMember(composeOption, "SizeBytes"), Is.EqualTo(2783));
        }

        // ── Source Index: 5 Lua files with functions/symbols/surfaces ────────

        [Test]
        public void DialogSysRuntime_SourceIndexMatches5Files()
        {
            object svc = LoadRuntime();
            Assert.That(IntMember(svc, "SourceIndexCount"), Is.EqualTo(5));
            Assert.That(IntMember(svc, "SourceLuaFileCount"), Is.EqualTo(5));
        }

        [Test]
        public void DialogSysRuntime_SourceIndexFunctionCounts_MatchPcSource()
        {
            object svc = LoadRuntime();
            // Total: composeoption(4) + dailog(9) + dailogsay(9) + dialogoption(3) + g_dialog(5) = 30
            Assert.That(IntMember(svc, "SourceFunctionCount"), Is.EqualTo(30));
            // Total globals: 10 + 8 + 5 + 1 + 3 = 27
            Assert.That(IntMember(svc, "SourceGlobalSymbolCount"), Is.EqualTo(27));
        }

        [Test]
        public void DialogSysRuntime_SourceOptionAndSaySurfaceCounts_MatchPcSource()
        {
            object svc = LoadRuntime();
            // Option surfaces: composeoption(2) + dialogoption(4) + g_dialog(1) = 7
            Assert.That(IntMember(svc, "SourceOptionSurfaceCount"), Is.EqualTo(7));
            // Say surfaces: dailogsay(3) + dialogoption(1) + g_dialog(1) = 5
            Assert.That(IntMember(svc, "SourceSaySurfaceCount"), Is.EqualTo(5));
        }

        [Test]
        public void DialogSysRuntime_SourceTotalSizeBytes_MatchesPcSource()
        {
            object svc = LoadRuntime();
            Assert.That(IntMember(svc, "SourceTotalSizeBytes"), Is.EqualTo(11823));
        }

        // ── Source Lookup by function/surface ─────────────────────────────────

        [Test]
        public void DialogSysRuntime_SourceLookup_GDialogOnSelectFound()
        {
            object svc = LoadRuntime();
            object sources = InvokeInstance(svc, "GetSourcesByFunction", "G_DIALOG:OnSelect");
            int count = (int)sources.GetType().GetProperty("Count").GetValue(sources);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void DialogSysRuntime_SourceLookup_OnSelectSurfaceFound()
        {
            object svc = LoadRuntime();
            object sources = InvokeInstance(svc, "GetSourcesBySurface", "OnSelect");
            int count = (int)sources.GetType().GetProperty("Count").GetValue(sources);
            Assert.That(count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void DialogSysRuntime_SourceLookup_CreateNewSayExFound()
        {
            object svc = LoadRuntime();
            object sources = InvokeInstance(svc, "GetSourcesByFunction", "CreateNewSayEx");
            int count = (int)sources.GetType().GetProperty("Count").GetValue(sources);
            Assert.That(count, Is.EqualTo(1));
        }

        // ── Runtime dialog flow (mock) ───────────────────────────────────────

        [Test]
        public void DialogSysRuntime_OpenDialog_ReturnsPcDialogClass()
        {
            object svc = LoadRuntime();
            Type ctxType = SandboxType("DialogOpenContext");
            object ctx = Activator.CreateInstance(ctxType);
            ctxType.GetField("npcTemplateId").SetValue(ctx, 500);
            ctxType.GetField("npcName").SetValue(ctx, "Dã Tẩu");
            ctxType.GetField("playerLevel").SetValue(ctx, 10);

            object result = InvokeInstance(svc, "OpenDialog", ctx);
            Assert.That(BoolMember(result, "opened"), Is.True);
            Assert.That(StrMember(result, "dialogClass"), Is.EqualTo("DailogClass"));
            Assert.That(StrMember(result, "npcName"), Is.EqualTo("Dã Tẩu"));
        }

        [Test]
        public void DialogSysRuntime_SelectOption_VerifiesSurfaceExists()
        {
            object svc = LoadRuntime();
            Type ctxType = SandboxType("DialogOpenContext");
            object ctx = Activator.CreateInstance(ctxType);
            ctxType.GetField("npcTemplateId").SetValue(ctx, 500);

            bool selected = (bool)InvokeInstance(svc, "SelectOption", ctx, "Nhận nhiệm vụ");
            Assert.That(selected, Is.True);
        }

        [Test]
        public void DialogSysRuntime_CreateNewSay_ReturnsMockSayResult()
        {
            object svc = LoadRuntime();
            var options = new System.Collections.Generic.List<string> { "Option A", "Option B" };
            object result = InvokeInstance(svc, "CreateNewSay", "Title test", options);
            Assert.That(BoolMember(result, "opened"), Is.True);
            Assert.That(StrMember(result, "dialogClass"), Is.EqualTo("CreateNewSayEx"));
            Assert.That(StrMember(result, "titleMsg"), Is.EqualTo("Title test"));
        }

        // ── Compile-time constants ───────────────────────────────────────────

        [Test]
        public void DialogSysRuntime_PcConstants_AreAccessible()
        {
            Type svcType = SandboxType("DialogSysRuntimeService");
            Assert.That(svcType, Is.Not.Null);

            // Verify constant fields exist
            Assert.That(StrMember(svcType, "LogTag"), Is.EqualTo("DialogSysRuntime"));
            Assert.That(StrMember(svcType, "JsonIndexPath"), Is.EqualTo("Reference/PcDialogSys/DialogSysIndex.json"));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string ProjectRoot => Directory.GetCurrentDirectory();

        private static object LoadRuntime()
        {
            return InvokeStatic(SandboxType("DialogSysRuntimeService"), "LoadFromStreamingAssets");
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

        private static bool BoolInvoke(object target, string methodName, params object[] args)
        {
            return (bool)InvokeInstance(target, methodName, args);
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

        private static string StrMember(object targetOrType, string name)
        {
            return Convert.ToString(Member(targetOrType, name));
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
