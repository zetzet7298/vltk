using System.IO;
using NUnit.Framework;
using VLTK.Sandbox.ItemData;

namespace VLTK.Tests.Sandbox.ItemData
{
    public class PcMeleeWeaponParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItem/meleeweapon_sample.txt");

        [Test]
        public void ParseFile_LoadsAtLeastFiveMeleeWeaponRows()
        {
            var rows = PcMeleeWeaponParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
        }

        [Test]
        public void ParseFile_ProducesNonEmptyNames()
        {
            var rows = PcMeleeWeaponParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
            foreach (var r in rows)
            {
                Assert.Greater(r.itemId, 0);
                Assert.IsFalse(string.IsNullOrEmpty(r.nameRaw));
                Assert.IsFalse(string.IsNullOrEmpty(r.nameNormalized));
                Assert.IsFalse(r.nameRaw.Contains("�"));
            }
        }
    }
}
