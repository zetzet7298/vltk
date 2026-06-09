using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class ItemExchangeRoleValueServiceTests
    {
        private static string SourceDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItemExchange");

        private static ItemExchangeRoleValueService LoadFromSourceCatalog()
            => ItemExchangeRoleValueService.LoadFromSourceCatalogDirectory(SourceDir);

        [Test]
        public void LoadFromStreamingAssets_MatchesSourceCatalogRoleValueShape()
        {
            var fromStreamingAssets = ItemExchangeRoleValueService.LoadFromStreamingAssets();
            var fromSourceCatalog = LoadFromSourceCatalog();

            Assert.IsTrue(fromStreamingAssets.Exists);
            CollectionAssert.AreEqual(new[] { "Value", "Jxb", "Limit", "Evaluate" },
                fromStreamingAssets.Sections.ToArray());
            Assert.AreEqual(35, fromStreamingAssets.KeyCount);
            Assert.AreEqual(fromSourceCatalog.KeyCount, fromStreamingAssets.KeyCount);
            Assert.AreEqual(fromSourceCatalog.Summary.jxbServerCount,
                fromStreamingAssets.Summary.jxbServerCount);
        }

        [Test]
        public void RoleValue_ExposesExactPcValueAndJxbIntegers()
        {
            var service = LoadFromSourceCatalog();

            Assert.AreEqual(5000, service.SkillValue);
            Assert.AreEqual(27, service.JxbValues.Count);
            Assert.AreEqual(1400, service.GetJxbValueOrDefault(11, -1));
            Assert.AreEqual(4000, service.GetJxbValueOrDefault(281, -1));
            Assert.AreEqual(400, service.GetJxbValueOrDefault(991, -1));
            Assert.AreEqual(2000, service.GetJxbValueOrDefault(1081, -1));
            Assert.IsTrue(service.TryGetJxbValue(2011, out var serverValue));
            Assert.AreEqual(400, serverValue);
        }

        [Test]
        public void RoleValue_ExposesLimitAndEvaluateFacts()
        {
            var service = LoadFromSourceCatalog();
            var summary = service.Summary;

            Assert.AreEqual(0, service.LimitCreateDate);
            Assert.AreEqual(20160301, service.CreateDate);
            Assert.IsFalse(service.EvaluateLevelEnabled);
            Assert.IsFalse(service.EvaluateSkillEnabled);
            Assert.IsFalse(service.EvaluateMoneyEnabled);
            Assert.IsTrue(service.EvaluateItemEnabled);
            Assert.IsFalse(service.EvaluateTaskEnabled);

            Assert.AreEqual(35, summary.keyCount);
            Assert.AreEqual(4, summary.sectionCount);
            Assert.AreEqual(400, summary.minJxbValue);
            Assert.AreEqual(4000, summary.maxJxbValue);
            Assert.IsTrue(summary.evaluateItem);
        }

        [Test]
        public void MissingKeys_ReturnFalseAndCallerDefaults()
        {
            var service = LoadFromSourceCatalog();

            Assert.IsFalse(service.TryGetRawValue("Jxb", "9999", out var raw));
            Assert.IsNull(raw);
            Assert.IsFalse(service.TryGetInt("Limit", "missing", out var parsed));
            Assert.AreEqual(0, parsed);
            Assert.AreEqual(-7, service.GetIntOrDefault("Evaluate", "missing", -7));
            Assert.AreEqual(-9, service.GetJxbValueOrDefault(9999, -9));
        }
    }
}
