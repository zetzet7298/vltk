// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorI18nTests (ticket 38)
// EditMode self-check pure logic: lookup + fallback chain 3 tầng (en→vi→raw key),
// hot-switch event, real bundle parse từ StreamingAssets.
// ponytail: không PlayMode, không scene — logic thuần qua RegisterBundle inject.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorI18nTests
    {
        private static SurvivorTextEntry E(string key, string text) => new SurvivorTextEntry { key = key, text = text };

        private static SurvivorText MakeBundled()
        {
            var t = new SurvivorText();
            t.RegisterBundle("vi", new List<SurvivorTextEntry>
            {
                E("survivor.menu.start", "Bắt đầu"),
                E("survivor.skill.4.name", "Thiếu Lâm Côn pháp"),
                E("survivor.only.vi", "chỉ có vi"),
                E("survivor.empty.vi", ""),
            });
            t.RegisterBundle("en", new List<SurvivorTextEntry>
            {
                E("survivor.menu.start", "Start"),
                E("survivor.skill.4.name", "Shaolin Staff Art"),
                E("survivor.only.en", "en only"),
                E("survivor.only.vi", ""), // có key nhưng text rỗng = missing → fallback vi
            });
            return t;
        }

        // --- tầng 1: lang hiện tại có key ---
        [Test]
        public void Get_EnExisting_ReturnsEnText()
        {
            var t = MakeBundled();
            t.SetLanguage("en");
            Assert.AreEqual("Start", t.Get("survivor.menu.start"));
            Assert.AreEqual("Shaolin Staff Art", t.Get("survivor.skill.4.name"));
        }

        [Test]
        public void Get_ViExisting_ReturnsViText()
        {
            var t = MakeBundled();
            Assert.AreEqual("Bắt đầu", t.Get("survivor.menu.start"), "default lang = vi");
        }

        // --- tầng 2: thiếu key ở lang hiện tại → fallback vi ---
        [Test]
        public void Fallback_EnMissing_ReturnsVi()
        {
            var t = MakeBundled();
            t.SetLanguage("en");
            Assert.AreEqual("chỉ có vi", t.Get("survivor.only.vi"));
        }

        [Test]
        public void Fallback_EmptyText_TreatedMissing_ReturnsVi()
        {
            var t = MakeBundled();
            t.SetLanguage("en");
            Assert.AreEqual("chỉ có vi", t.Get("survivor.only.vi"), "en key rỗng → coi missing → vi");
        }

        // --- tầng 3: thiếu cả → raw key ---
        [Test]
        public void Fallback_BothMissing_ReturnsRawKey()
        {
            var t = MakeBundled();
            t.SetLanguage("en");
            Assert.AreEqual("survivor.nonexistent", t.Get("survivor.nonexistent"));
        }

        [Test]
        public void Fallback_DescEmpty_ReturnsRawKey()
        {
            var t = MakeBundled();
            t.SetLanguage("en");
            Assert.AreEqual("survivor.skill.4.desc", t.Get("survivor.skill.4.desc"),
                "desc trống mọi tầng → raw key (ticket 26/27 sẽ cấp nội dung)");
        }

        // --- chain đầy đủ trên 1 key: en → bỏ en → vi → bỏ vi → raw ---
        [Test]
        public void FallbackChain_EnToViToRaw_OnSameKey()
        {
            var t = MakeBundled();
            t.SetLanguage("en");
            Assert.AreEqual("en only", t.Get("survivor.only.en"), "tầng 1: en");

            t.RegisterBundle("en", new List<SurvivorTextEntry>()); // bỏ hẳn bundle en
            Assert.AreEqual("chỉ có vi", t.Get("survivor.only.vi"), "tầng 2: vi");
            Assert.AreEqual("survivor.nonexistent", t.Get("survivor.nonexistent"), "tầng 3: raw key");
        }

        [Test]
        public void Fallback_UnknownLang_GoesViThenRaw()
        {
            var t = MakeBundled();
            t.SetLanguage("fr"); // bundle chưa đăng ký
            Assert.AreEqual("Bắt đầu", t.Get("survivor.menu.start"), "fr → vi");
            Assert.AreEqual("survivor.nonexistent", t.Get("survivor.nonexistent"), "fr → vi miss → raw");
        }

        // --- runtime switch không restart: event notify ---
        [Test]
        public void SetLanguage_FiresChangedEvent_WithNewLang()
        {
            var t = MakeBundled();
            var fired = new List<string>();
            t.Changed += lang => fired.Add(lang);

            t.SetLanguage("en");
            t.SetLanguage("vi");
            Assert.AreEqual(new[] { "en", "vi" }, fired);
        }

        [Test]
        public void SetLanguage_SameLang_NoEvent()
        {
            var t = MakeBundled();
            int fired = 0;
            t.Changed += _ => fired++;
            t.SetLanguage("vi");
            Assert.AreEqual(0, fired);
        }

        [Test]
        public void SetLanguage_Empty_FallsBackToVi()
        {
            var t = MakeBundled();
            t.SetLanguage("");
            Assert.AreEqual("vi", t.Language);
        }

        // --- bundle thật từ StreamingAssets: parse + key set nhất quán ---
        [Test]
        public void LoadFromStreamingAssets_ParsesRealBundles()
        {
            var t = SurvivorText.LoadFromStreamingAssets();
            Assert.AreEqual(2, t.Languages.Count, "vi + en đều load được");
            Assert.Greater(t.Count, 0, "bundle vi có entry");
            Assert.AreEqual("Bắt đầu", t.Get("survivor.menu.start"), "vi thật");
            Assert.AreNotEqual("survivor.menu.start", t.Get("survivor.menu.start"), "không rơi ra raw key");

            t.SetLanguage("en");
            Assert.AreEqual("Start", t.Get("survivor.menu.start"), "en thật");
            Assert.AreEqual("Shaolin Staff Art", t.Get("survivor.skill.4.name"), "skill name seed từ PcSkills.txt");
            Assert.AreEqual("survivor.skill.4.desc", t.Get("survivor.skill.4.desc"), "desc trống → raw key");
        }

        [Test]
        public void Bundles_HaveIdenticalKeySets()
        {
            var t = SurvivorText.LoadFromStreamingAssets();
            var vi = new HashSet<string>();
            foreach (var e in SurvivorTextLoader.Load("vi")) vi.Add(e.key);
            var en = new HashSet<string>();
            foreach (var e in SurvivorTextLoader.Load("en")) en.Add(e.key);

            Assert.IsTrue(vi.SetEquals(en),
                "vi/en key set phải khớp — lệch: vi-only=" + string.Join(",", vi.Except(en)) +
                " en-only=" + string.Join(",", en.Except(vi)));
        }
    }
}
