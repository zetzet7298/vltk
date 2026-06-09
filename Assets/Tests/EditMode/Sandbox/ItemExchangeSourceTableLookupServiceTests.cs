using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class ItemExchangeSourceTableLookupServiceTests
    {
        private static string SourceDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItemExchange");

        private static ItemExchangeSourceTableLookupService Load()
            => ItemExchangeSourceTableLookupService.LoadFromDirectory(SourceDir);

        [Test]
        public void LoadFromDirectory_ExposesNormalAndRareShapeWithoutRuntimeLogs()
        {
            var service = Load();
            var summary = service.Summary;

            Assert.AreEqual(78, service.NormalHeaderCount);
            Assert.AreEqual(7334, service.NormalRowCount);
            Assert.AreEqual(29, service.RareHeaderCount);
            Assert.AreEqual(480, service.RareRowCount);
            Assert.AreEqual(78, summary.normalHeaderCount);
            Assert.AreEqual(7334, summary.normalRowCount);
            Assert.AreEqual(29, summary.rareHeaderCount);
            Assert.AreEqual(480, summary.rareRowCount);
            Assert.IsFalse(service.HasRoleValueLog);
            Assert.IsFalse(summary.hasRoleValueLog);
            Assert.IsFalse(Directory.Exists(Path.Combine(SourceDir, "rolevalue_log")));
        }

        [Test]
        public void NormalTable_PreservesRepresentativePcHeaderAndFirstRowValues()
        {
            var service = Load();
            var first = service.Normal.Rows.First();

            Assert.AreEqual("normal.txt", service.Normal.SourceName);
            Assert.AreEqual("道具名称", service.Normal.Headers[0]);
            Assert.AreEqual("道具品质", service.Normal.Headers[1]);
            Assert.AreEqual("道具ID", service.Normal.Headers[2]);
            Assert.AreEqual("M閚g Long Ch輓h H錸g T╪g M穙", first.GetRawOrDefault("道具名称"));
            Assert.AreEqual("1", first.GetRawOrDefault("道具品质"));
            Assert.AreEqual("1", first.GetRawOrDefault("道具ID"));
            Assert.AreEqual("10", first.GetRawOrDefault("等级"));
            Assert.AreEqual("50", first.GetRawOrDefault("保底"));
            Assert.IsTrue(first.TryGetInt("道具ID", out var itemId));
            Assert.AreEqual(1, itemId);
        }

        [Test]
        public void RareTable_FindsRowsByNameAndMagicIdWhenColumnsExist()
        {
            var service = Load();

            CollectionAssert.AreEqual(new[] { "NAME", "MAGIC_ID", "MAG_P1_MIN", "MAG_P1_MAX" },
                service.Rare.Headers.Take(4).ToArray());
            Assert.IsTrue(service.TryFindByName("rare", "加伤害强化", out var byName));
            Assert.IsTrue(service.TryFindByMagicId("rare.txt", 126, out var byMagicId));
            Assert.AreSame(byName, byMagicId);
            Assert.AreEqual("126", byName.GetRawOrDefault("MAGIC_ID"));
            Assert.AreEqual("5", byName.GetRawOrDefault("MAG_P1_MIN"));
            Assert.AreEqual("10", byName.GetRawOrDefault("MAG_P1_MAX"));
            Assert.AreEqual("2500", byName.GetRawOrDefault("SWORD"));
            Assert.IsTrue(byName.TryGetInt("MAGIC_ID", out var magicId));
            Assert.AreEqual(126, magicId);
        }

        [Test]
        public void MissingTablesColumnsAndRows_ReturnSafeFalseOrFallbacks()
        {
            var service = Load();

            Assert.IsFalse(service.TryGetTable("missing", out var missingTable));
            Assert.IsNull(missingTable);
            Assert.IsFalse(service.TryFindByName("normal", "加伤害强化", out var normalByName));
            Assert.IsNull(normalByName);
            Assert.IsFalse(service.TryFindByMagicId("normal", 126, out var normalByMagic));
            Assert.IsNull(normalByMagic);
            Assert.IsFalse(service.Rare.Rows[0].TryGetRaw("missing", out var missingValue));
            Assert.IsNull(missingValue);
            Assert.AreEqual("fallback", service.Rare.Rows[0].GetRawOrDefault("missing", "fallback"));
        }
    }
}
