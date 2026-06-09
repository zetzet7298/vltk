using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcFlipCardProtocolParserTests
    {
        private static string FlipCardDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcFlipCard");

        [Test]
        public void BuildRegistry_LoadsPcFlipCardProtocolFacts()
        {
            var reg = PcFlipCardProtocolParser.BuildRegistry(FlipCardDir);

            Assert.AreEqual(6, reg.Count);
            Assert.IsTrue(reg.TryGetInt("OPT_OPEN_UI", out var open));
            Assert.AreEqual(1, open);
            Assert.IsTrue(reg.TryGetInt("OPT_SET_CELL", out var setCell));
            Assert.AreEqual(2, setCell);
            Assert.AreEqual("OpenCardsUi", reg.Get("CLIENT_OPEN_UI_FUNCTION").valueRaw);
            Assert.AreEqual("OpenCardBack", reg.Get("CLIENT_SET_CELL_FUNCTION").valueRaw);
            Assert.AreEqual("emSCRIPT_PROTOCOL_FLIP_CARD", reg.Get("CLIENT_CLICK_PROTOCOL").valueRaw);
            Assert.AreEqual("RemoveOpenCardUiItem", reg.Get("CLIENT_CLOSE_FUNCTION").valueRaw);
        }

        [Test]
        public void LoadFromStreamingAssets_ExposesProtocolConstantsOnly()
        {
            var svc = FlipCardProtocolService.LoadFromStreamingAssets();

            Assert.AreEqual(6, svc.Count);
            Assert.IsTrue(svc.TryGetInt("OPT_OPEN_UI", out var open));
            Assert.AreEqual(1, open);
            Assert.IsTrue(svc.Get("CLIENT_SET_CELL_FUNCTION").evidence.Contains("OpenCardBack"));
        }
    }
}
