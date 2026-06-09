using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcPartnerPetSourceIndexParserTests
    {
        private static string IndexPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcPartnerPet/partner_pet_source_index.txt");

        private static string IndexDir => Path.GetDirectoryName(IndexPath);

        [Test]
        public void ParseFile_ReadsPartnerPetSourceCatalog_NotRuntimeClaim()
        {
            Assert.IsTrue(File.Exists(IndexPath));
            var rows = PcPartnerPetSourceIndexParser.ParseFile(IndexPath);

            Assert.AreEqual(266, rows.Count,
                "Catalog covers scoped PC partner/pet source files only; it does not claim 266 runtime features.");
            Assert.AreEqual(66, rows.FindAll(r => r.isConfig).Count);
            Assert.AreEqual(200, rows.FindAll(r => r.isLua).Count);
            Assert.AreEqual(63, rows.FindAll(r => r.category == "partner-config").Count);
            Assert.AreEqual(3, rows.FindAll(r => r.category == "pet-config").Count);
            Assert.AreEqual(200, rows.FindAll(r => r.category == "partner-script").Count);
        }

        [Test]
        public void Registry_PreservesRepresentativeConfigHashesAndSizes()
        {
            var registry = PcPartnerPetSourceIndexParser.BuildRegistry(IndexDir);
            var partnerEvent = registry.GetBySourceRootPath("Client 6.0/settings/partner", "partner_event.ini");
            var petSkill = registry.GetBySourceRootPath("Client 6.0/settings/petsys", "pet_skill_def.txt");

            Assert.IsNotNull(partnerEvent);
            Assert.AreEqual("partner-config", partnerEvent.category);
            Assert.IsTrue(partnerEvent.isConfig);
            Assert.IsFalse(partnerEvent.isLua);
            Assert.AreEqual(23526, partnerEvent.sizeBytes);
            Assert.AreEqual("da9ba7c8a2ffc349ad7972c12a4717ee3c6aa103f6aebc54362eac7df64009a4", partnerEvent.sha256);

            Assert.IsNotNull(petSkill);
            Assert.AreEqual("pet-config", petSkill.category);
            Assert.AreEqual(2955, petSkill.sizeBytes);
            Assert.AreEqual("5320f7d57386e9aeaff8d59d3daeb978ed4a6f8fe369ef3d70273f5d8842c057", petSkill.sha256);
        }

        [Test]
        public void Registry_PreservesRepresentativeScriptHashesAndDuplicateSourceRoots()
        {
            var registry = PcPartnerPetSourceIndexParser.BuildRegistry(IndexDir);
            var action = registry.GetBySourceRootPath("Server 6.0/server/home_jxser/server1/script/partner", "partner_action.lua");
            var task = registry.GetBySourceRootPath("Server 6.0/server/home_jxser/server1/script/npclevelscript", "partner_task.lua");

            Assert.IsNotNull(action);
            Assert.AreEqual("partner-script", action.category);
            Assert.IsTrue(action.isLua);
            Assert.AreEqual(202, action.sizeBytes);
            Assert.AreEqual("c6a96b58ae461865d110385b4fee19271f6c5f6c6dfc5f83427bbac9220b326c", action.sha256);

            Assert.IsNotNull(task);
            Assert.AreEqual(6331, task.sizeBytes);
            Assert.AreEqual("bfab6efc54b7d8fe47b8714fd6a0ad67937e363456885479cc9c8f05940bec93", task.sha256);
            Assert.AreEqual(2, registry.GetByFileName("partner_action.lua").Count);
            Assert.AreEqual(2, registry.GetByFileName("partner_task.lua").Count);
        }

        [Test]
        public void Service_LoadsDefaultStreamingAssetsIndexAndGroupsRoots()
        {
            var service = PartnerPetSourceIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(266, service.Count);
            Assert.AreEqual(66, service.ConfigFileCount);
            Assert.AreEqual(200, service.LuaFileCount);
            Assert.AreEqual(21, service.GetBySourceRoot("Client 6.0/settings/partner").Count);
            Assert.AreEqual(1, service.GetBySourceRoot("Client 6.0/settings/petsys").Count);
            Assert.AreEqual(88, service.GetBySourceRoot("Server 6.0/server/home_jxser/server1/script/task/partner").Count);
            Assert.Greater(service.TotalSizeBytes, 0L);
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcPartnerPetSourceIndexParser.ParseFile("/tmp/not-a-real-partner-pet-source-index.txt").Count);
            Assert.AreEqual(0, PcPartnerPetSourceIndexParser.BuildRegistry("/tmp/not-a-real-partner-pet-source-index-dir").Count);
            Assert.AreEqual(0, PartnerPetSourceIndexService.LoadFromFile("/tmp/not-a-real-partner-pet-source-index.txt").Count);
        }

    }
}
