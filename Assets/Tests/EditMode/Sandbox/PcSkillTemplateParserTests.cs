using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcSkillTemplateParserTests
    {
        private static string SourceFile => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcSkill/skilltemplate.txt");

        [Test]
        public void ImportedSource_IsPcSkilltemplateSchema_NotOld219RowTableClaim()
        {
            Assert.IsTrue(File.Exists(SourceFile));
            var fields = PcSkillTemplateParser.ParseTemplateFile(SourceFile);

            Assert.AreEqual(67, fields.Count,
                "PC settings/skilltemplate.txt contains 67 [field] sections; the old 219-row template claim is not PC-confirmed.");
            Assert.AreNotEqual(219, fields.Count);
        }

        [Test]
        public void Parser_ProvesLineCountsFromPcSource()
        {
            var catalog = PcSkillTemplateParser.BuildCatalog(Path.GetDirectoryName(SourceFile));

            Assert.AreEqual(318, catalog.totalLineCount);
            Assert.AreEqual(220, catalog.nonEmptyLineCount);
            Assert.AreEqual(67, catalog.Count);
            Assert.AreEqual(153, catalog.NonEmptyPropertyLineCount());
        }

        [Test]
        public void Parser_PreservesRepresentativePcFields()
        {
            var catalog = PcSkillTemplateParser.BuildCatalog(Path.GetDirectoryName(SourceFile));

            var skillName = catalog.GetField("SkillName");
            Assert.IsNotNull(skillName);
            Assert.AreEqual(1, skillName.sourceLine);
            Assert.AreEqual("String", skillName.typeRaw);
            Assert.AreEqual("Tªn chiªu thøc", skillName.staticNameRaw);

            var skillId = catalog.GetField("SkillId");
            Assert.IsNotNull(skillId);
            Assert.AreEqual(5, skillId.sourceLine);
            Assert.AreEqual("Number", skillId.typeRaw);
            Assert.AreEqual("0", skillId.valueRaw);
        }

        [Test]
        public void Parser_PreservesIndexListsAndEventFields()
        {
            var catalog = PcSkillTemplateParser.BuildCatalog(Path.GetDirectoryName(SourceFile));

            var skillStyle = catalog.GetField("SkillStyle");
            Assert.IsNotNull(skillStyle);
            Assert.AreEqual("IndexList", skillStyle.typeRaw);
            Assert.IsTrue(skillStyle.valueRaw.Contains("Lo¹i bÉy"));
            Assert.AreEqual("0", skillStyle.defaultValueRaw);

            Assert.AreEqual(168, catalog.GetField("ByMissle").sourceLine);
            Assert.AreEqual(234, catalog.GetField("Param1").sourceLine);
            Assert.AreEqual(317, catalog.GetField("LvlData10").sourceLine);
        }

        [Test]
        public void CatalogService_LoadsDefaultPcSkilltemplateCopy()
        {
            var service = SkillTemplateCatalogService.LoadFromStreamingAssets();

            Assert.AreEqual(67, service.Count);
            Assert.AreEqual(318, service.TotalLineCount);
            Assert.AreEqual(220, service.NonEmptyLineCount);
            Assert.IsNotNull(service.GetField("LvlSetScript"));
        }
    }

    internal static class PcSkillTemplateCatalogTestExtensions
    {
        public static int NonEmptyPropertyLineCount(this PcSkillTemplateCatalog catalog)
        {
            return catalog.Fields.Sum(field => field.properties.Count);
        }
    }
}
