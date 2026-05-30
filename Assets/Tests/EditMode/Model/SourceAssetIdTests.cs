using NUnit.Framework;
using VLTK.Model;

namespace VLTK.Tests.Model
{
    public class SourceAssetIdTests
    {
        [Test]
        public void ToKey_PrefersSourcPath_WhenSet()
        {
            var id = new SourceAssetId
            {
                sourcePath = "maps/0001.dat",
                packageName = "maps_pak",
                uid = 42,
            };
            Assert.AreEqual("maps/0001.dat", id.ToKey());
        }

        [Test]
        public void ToKey_FallsBackToPackageAndUid_WhenSourcePathEmpty()
        {
            var id = new SourceAssetId
            {
                sourcePath = "",
                packageName = "spr_pak",
                uid = 99,
            };
            Assert.AreEqual("spr_pak:99", id.ToKey());
        }

        [Test]
        public void ToKey_FallsBackToPackageAndUid_WhenSourcePathNull()
        {
            var id = new SourceAssetId
            {
                sourcePath = null,
                packageName = "data_01",
                uid = 7,
            };
            Assert.AreEqual("data_01:7", id.ToKey());
        }

        [Test]
        public void ToKey_IsStable_ForSameValues()
        {
            var id1 = new SourceAssetId { sourcePath = "maps/abc.dat" };
            var id2 = new SourceAssetId { sourcePath = "maps/abc.dat" };
            Assert.AreEqual(id1.ToKey(), id2.ToKey());
        }

        [Test]
        public void AllResourceKinds_AreDefined()
        {
            // Ensure all spec-required kinds exist in the enum
            Assert.IsTrue(System.Enum.IsDefined(typeof(ResourceKind), ResourceKind.Map));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ResourceKind), ResourceKind.Region));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ResourceKind), ResourceKind.Terrain));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ResourceKind), ResourceKind.Sprite));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ResourceKind), ResourceKind.Npc));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ResourceKind), ResourceKind.Lua));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ResourceKind), ResourceKind.Item));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ResourceKind), ResourceKind.Audio));
        }
    }
}
