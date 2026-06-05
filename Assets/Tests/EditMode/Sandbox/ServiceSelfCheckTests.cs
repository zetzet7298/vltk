// -----------------------------------------------------------------------------
// VLTK Mobile — Service self-check: kiểm tra API tối thiểu của mỗi service.
// Dùng reflection verify: Count property (int), Get(int) method, GetAll()/All,
// static LoadFromStreamingAssets(). Diagnostic test, luôn pass.
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class ServiceSelfCheckTests
    {
        private static IEnumerable<Type> EnumerateServices()
        {
            var asm = Assembly.GetAssembly(typeof(AdventureService));
            if (asm == null) yield break;
            foreach (var t in asm.GetTypes())
            {
                if (t == null) continue;
                if (t.IsAbstract || t.IsInterface) continue;
                if (!t.Name.EndsWith("Service")) continue;
                if (t.Namespace != "VLTK.Sandbox") continue;
                yield return t;
            }
        }

        [Test]
        public void Test_Diagnostic_ServiceApiCheck()
        {
            int total = 0;
            int hasCount = 0, hasGet = 0, hasAll = 0, hasLoad = 0;
            var report = new List<string>();
            foreach (var t in EnumerateServices())
            {
                total++;
                var hasCountProp = t.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance) != null;
                var hasGetMethod = t.GetMethod("Get", new[] { typeof(int) }) != null;
                var hasAllMethod = t.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null
                                   || t.GetProperty("All", BindingFlags.Public | BindingFlags.Instance) != null;
                var hasLoader = t.GetMethod("LoadFromStreamingAssets", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null) != null
                                || t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                     .Any(m => m.Name == "LoadFromStreamingAssets");

                if (hasCountProp) hasCount++;
                if (hasGetMethod) hasGet++;
                if (hasAllMethod) hasAll++;
                if (hasLoader) hasLoad++;

                if (!hasCountProp || !hasGetMethod || !hasAllMethod || !hasLoader)
                {
                    report.Add($"{t.Name}: Count={hasCountProp} Get={hasGetMethod} All={hasAllMethod} Load={hasLoader}");
                }
            }
            UnityEngine.Debug.Log($"[SelfCheck] {total} services: Count={hasCount} Get={hasGet} All={hasAll} Load={hasLoad}");

            // In báo cáo (max 50 dòng)
            foreach (var line in report.Take(50))
            {
                UnityEngine.Debug.LogWarning($"[SelfCheck] {line}");
            }
            Assert.Greater(total, 100, "Phải có >100 services");
        }

        [Test]
        public void Test_Diagnostic_TotalServiceCount()
        {
            int count = EnumerateServices().Count();
            UnityEngine.Debug.Log($"[SelfCheck] Total VLTK.Sandbox.*Service types: {count}");
            Assert.Greater(count, 140, "Phải có ≥140 services theo goal");
        }

        [Test]
        public void Test_Diagnostic_FactionAndTitleCatalogsExist()
        {
            var fac = typeof(FactionVietnameseCatalog);
            var tit = typeof(TitleVietnameseCatalog);
            Assert.IsNotNull(fac, "FactionVietnameseCatalog phải tồn tại");
            Assert.IsNotNull(tit, "TitleVietnameseCatalog phải tồn tại");
            var facMethod = fac.GetMethod("GetVietnameseName");
            var titMethod = tit.GetMethod("GetVietnameseName");
            Assert.IsNotNull(facMethod, "FactionVietnameseCatalog.GetVietnameseName phải tồn tại");
            Assert.IsNotNull(titMethod, "TitleVietnameseCatalog.GetVietnameseName phải tồn tại");
        }
    }
}
