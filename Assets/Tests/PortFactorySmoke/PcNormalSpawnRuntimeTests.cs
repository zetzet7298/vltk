// -----------------------------------------------------------------------------
// VLTK Mobile — PortFactorySmoke: PcNormalSpawnRuntimeService evidence tests.
// Proves: 5,384 data rows loaded, exact column anchors, template ID lookup,
//         aggregate unique positive count, and empty/null resilience.
// Pattern: follows PortFactoryBatch4EvidenceTests reflection-based assertions.
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace VLTK.Tests.PortFactorySmoke
{
    public sealed class PcNormalSpawnRuntimeTests
    {
        private const string DefaultRelativeDir = "Reference/PcNormalSpawn";
        private const string DefaultFileName = "normal.json";
        private const int ExpectedRowCount = 5384;
        private const int ExpectedUniquePositiveTemplates = 3255;
        private const int ExpectedColumnCount = 78;

        [Test]
        public void NormalSpawnRuntime_LoadsExactly5384Rows()
        {
            object service = LoadServiceFromDefaultDir();
            Assert.That(IntMember(service, "Count"), Is.EqualTo(ExpectedRowCount),
                "normal.json must yield exactly 5,384 data rows");
        }

        [Test]
        public void NormalSpawnRuntime_UniquePositiveTemplateCountIs3255()
        {
            object service = LoadServiceFromDefaultDir();
            Assert.That(IntMember(service, "UniquePositiveTemplateCount"),
                Is.EqualTo(ExpectedUniquePositiveTemplates),
                "unique positive template IDs must be 3,255");
        }

        [Test]
        public void NormalSpawnRuntime_FirstRowAnchorsMatch()
        {
            object service = LoadServiceFromDefaultDir();

            // First data row: template ID = 1, level = 10
            Assert.That(IntMember(service, "FirstTemplateId"), Is.EqualTo(1));
            Assert.That(IntMember(service, "FirstLevel"), Is.EqualTo(10));
            Assert.That(IntMember(service, "SourceColumnCount"), Is.EqualTo(ExpectedColumnCount));
        }

        [Test]
        public void NormalSpawnRuntime_TemplateIdLookupWorks()
        {
            object service = LoadServiceFromDefaultDir();

            // Template ID 1 should exist
            object sp1 = InvokeInstance(service, "GetByTemplateId", 1);
            Assert.That(sp1, Is.Not.Null, "template ID 1 should be found");
            Assert.That(StringMember(sp1, "nameRaw"), Is.Not.Empty);

            // Template ID 100 should exist
            object sp100 = InvokeInstance(service, "GetByTemplateId", 100);
            Assert.That(sp100, Is.Not.Null, "template ID 100 should be found");
            Assert.That(IntMember(sp100, "npcTemplateId"), Is.EqualTo(100));

            // Non-existent template should return null
            object spNone = InvokeInstance(service, "GetByTemplateId", 999999);
            Assert.That(spNone, Is.Null, "non-existent template should return null");
        }

        [Test]
        public void NormalSpawnRuntime_AllRowsHaveSourceFileNormalJson()
        {
            object service = LoadServiceFromDefaultDir();
            int count = 0;
            object all = Member(service, "All");
            foreach (var sp in (System.Collections.IEnumerable)all)
            {
                Assert.That(StringMember(sp, "sourceFile"), Is.EqualTo("normal.json"),
                    "every row must carry sourceFile=normal.json");
                count++;
            }
            Assert.That(count, Is.EqualTo(ExpectedRowCount));
        }

        [Test]
        public void NormalSpawnRuntime_EmptyReturnsZeroCount()
        {
            object empty = InvokeStatic(SandboxType("PcNormalSpawnRuntimeService"), "Empty");
            Assert.That(IntMember(empty, "Count"), Is.EqualTo(0));
            Assert.That(IntMember(empty, "UniquePositiveTemplateCount"), Is.EqualTo(0));
        }

        // ---- Helper methods (same pattern as PortFactoryBatch4EvidenceTests) ----

        private static string ProjectRoot => Directory.GetCurrentDirectory();

        private static object LoadServiceFromDefaultDir()
        {
            string dir = Path.Combine(ProjectRoot, "Assets/StreamingAssets", DefaultRelativeDir);
            return InvokeStatic(SandboxType("PcNormalSpawnRuntimeService"), "LoadFromDirectory", dir);
        }

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null,
                type.FullName + "." + methodName + " missing");
            return method.Invoke(null, BuildInvokeArgs(method, args));
        }

        private static object InvokeInstance(object target, string methodName,
            params object[] args)
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
            Type type = TypeInAssembly("Assembly-CSharp", fullName)
                      ?? TypeInAssembly("VLTK.Sandbox.Runtime", fullName);
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

        private static string StringMember(object target, string name)
            => Convert.ToString(Member(target, name));

        private static object Member(object target, string name)
        {
            Type type = target as Type ?? target.GetType();
            BindingFlags flags = BindingFlags.Public
                | (target is Type ? BindingFlags.Static : BindingFlags.Instance);
            PropertyInfo prop = type.GetProperty(name, flags);
            if (prop != null)
                return prop.GetValue(target is Type ? null : target, null);
            FieldInfo field = type.GetField(name, flags);
            Assert.That(field, Is.Not.Null,
                type.FullName + "." + name + " missing");
            return field.GetValue(target is Type ? null : target);
        }
    }
}
