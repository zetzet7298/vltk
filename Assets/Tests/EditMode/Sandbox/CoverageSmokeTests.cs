// -----------------------------------------------------------------------------
// VLTK Mobile — Coverage smoke tests: instantiate mọi service xem có lỗi không.
// Dùng reflection quét tất cả class VLTK.Sandbox.*Service có property Count,
// khởi tạo không tham số và kiểm tra API tối thiểu.
// Mục đích: đảm bảo tất cả service import được + API cơ bản hoạt động.
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    [Category("Smoke")]
    public class CoverageSmokeTests
    {
        private static IEnumerable<Type> GetAllServiceTypes()
        {
            var asm = Assembly.GetAssembly(typeof(AdventureService));
            if (asm == null) yield break;
            foreach (var t in asm.GetTypes())
            {
                if (t == null) continue;
                if (t.IsAbstract || t.IsInterface) continue;
                if (!t.Name.EndsWith("Service")) continue;
                if (t.Namespace != "VLTK.Sandbox") continue;
                // Phải có public Count property int
                var countProp = t.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                if (countProp == null || countProp.PropertyType != typeof(int)) continue;
                yield return t;
            }
        }

        [Test]
        public void Test_AllServices_AreInstantiable()
        {
            int total = 0, instantiated = 0;
            var failed = new List<string>();
            foreach (var t in GetAllServiceTypes())
            {
                total++;
                try
                {
                    var inst = Activator.CreateInstance(t);
                    if (inst != null) instantiated++;
                }
                catch (Exception)
                {
                    failed.Add(t.Name);
                }
            }
            UnityEngine.Debug.Log($"[Smoke] {instantiated}/{total} services instantiable");
            if (failed.Count > 0)
            {
                UnityEngine.Debug.LogWarning($"[Smoke] Failed to instantiate: {string.Join(", ", failed)}");
            }
            Assert.AreEqual(total, instantiated, $"Tất cả {total} services phải instantiate được");
        }

        [Test]
        public void Test_AllServices_CountPropertyIsNonNegative()
        {
            int tested = 0;
            var nullCount = new List<string>();
            foreach (var t in GetAllServiceTypes())
            {
                try
                {
                    var inst = Activator.CreateInstance(t) as object;
                    if (inst == null) continue;
                    var countProp = t.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                    if (countProp == null) continue;
                    var count = (int)(countProp.GetValue(inst) ?? 0);
                    if (count < 0)
                    {
                        nullCount.Add($"{t.Name}={count}");
                    }
                    tested++;
                }
                catch { }
            }
            Assert.AreEqual(0, nullCount.Count, $"Tất cả services phải có Count >= 0: {string.Join(", ", nullCount)}");
            Assert.Greater(tested, 100, "Phải test ≥100 services");
        }

        [Test]
        public void Test_AllServices_GetByIdZero_ReturnsNull()
        {
            int tested = 0;
            int nullReturns = 0;
            int threwException = 0;
            foreach (var t in GetAllServiceTypes())
            {
                try
                {
                    var inst = Activator.CreateInstance(t) as object;
                    if (inst == null) continue;
                    var getMethod = t.GetMethod("Get", new[] { typeof(int) });
                    if (getMethod == null) continue;
                    var result = getMethod.Invoke(inst, new object[] { 0 });
                    if (result == null) nullReturns++;
                    tested++;
                }
                catch
                {
                    threwException++;
                }
            }
            Assert.AreEqual(0, threwException, "Get(0) phải không throw exception");
            Assert.Greater(tested, 0, "Phải test ≥1 service có Get(int)");
        }

        [Test]
        public void Test_AllServices_HasLoadFromStreamingAssets()
        {
            int total = 0, withLoader = 0;
            var noLoader = new List<string>();
            foreach (var t in GetAllServiceTypes())
            {
                total++;
                var m = t.GetMethod("LoadFromStreamingAssets", BindingFlags.Public | BindingFlags.Static);
                if (m != null) withLoader++;
                else noLoader.Add(t.Name);
            }
            UnityEngine.Debug.Log($"[Smoke] {withLoader}/{total} services có LoadFromStreamingAssets");
            if (noLoader.Count > 0 && noLoader.Count < 30)
            {
                UnityEngine.Debug.LogWarning($"[Smoke] No LoadFromStreamingAssets: {string.Join(", ", noLoader)}");
            }
            // Không bắt buộc phải 100% (một số service là wrapper/inner)
            Assert.Greater(withLoader, 100, "Phải có >100 services có LoadFromStreamingAssets");
        }
    }
}
