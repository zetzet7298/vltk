// -----------------------------------------------------------------------------
// VLTK Mobile — AttribConstService EditMode tests.
// Kiểm tra thuộc tính hằng số runtime: section list, key/value, magic code, host.
// PC source: settings/attribconstdata.ini + magicdesc.ini + rolevalue.ini + gamesetting.ini.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class AttribConstServiceHostServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IAttribConstHost
        {
            public int LoadedCalls;
            public int RegistryAttachedCalls;
            public int SectionQueriedCalls;
            public int SectionMissingCalls;
            public int KeyQueriedCalls;
            public int KeyMissingCalls;
            public int MagicCodeResolvedCalls;
            public int ShowCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public int LastSectionCount;
            public int LastTotalEntries;
            public long LastDurationMs;
            public string LastSection;
            public int LastEntryCount;
            public string LastKey;
            public string LastValue;
            public int LastMagicCode;
            public string LastSfxAction;

            public void OnAttribLoaded(int sectionCount, int totalEntries, long durationMs)
            {
                LoadedCalls++;
                LastSectionCount = sectionCount;
                LastTotalEntries = totalEntries;
                LastDurationMs = durationMs;
            }
            public void OnAttribRegistryAttached(int sectionCount, int totalEntries)
            {
                RegistryAttachedCalls++;
            }
            public void OnAttribSectionQueried(string section, int entryCount)
            {
                SectionQueriedCalls++;
                LastSection = section;
                LastEntryCount = entryCount;
            }
            public void OnAttribSectionMissing(string section) { SectionMissingCalls++; }
            public void OnAttribKeyQueried(string section, string key, string value)
            {
                KeyQueriedCalls++;
                LastKey = key;
                LastValue = value;
            }
            public void OnAttribKeyMissing(string section, string key) { KeyMissingCalls++; }
            public void OnMagicCodeResolved(string section, string key, int magicCode)
            {
                MagicCodeResolvedCalls++;
                LastMagicCode = magicCode;
            }
            public void ShowAttribUI(string section, int entryCount) { ShowCalls++; }
            public void LogAttribEvent(string section, string key, string message) { LogCalls++; }
            public void PlayAttribSFX(string action) { SfxCalls++; LastSfxAction = action; }
            public void SaveAttribCache(int sectionCount, int totalEntries) { SaveCalls++; }
        }

        private static PcAttribConstRegistry MakeRegistry()
        {
            var reg = new PcAttribConstRegistry();
            var magic = new PcAttribConstSection { name = "MagicDesc" };
            magic.data["Fire"] = "100";
            magic.data["Ice"] = "200";
            reg.Register(magic);
            var role = new PcAttribConstSection { name = "RoleValue" };
            role.data["InitGold"] = "1000";
            reg.Register(role);
            var game = new PcAttribConstSection { name = "GameSetting" };
            game.data["MaxLevel"] = "120";
            reg.Register(game);
            return reg;
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new AttribConstService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void Constructor_WithRegistry()
        {
            var reg = MakeRegistry();
            var svc = new AttribConstService(reg);
            Assert.IsTrue(svc.Count > 0);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var reg = MakeRegistry();
            var svc = new AttribConstService(reg, host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new AttribConstService();
            svc.AttachHost(host);
            svc.AttachRegistry(MakeRegistry());
            Assert.AreEqual(1, host.RegistryAttachedCalls);
        }

        [Test]
        public void AttachRegistry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AttribConstService(null, host);
            svc.AttachRegistry(MakeRegistry());
            Assert.AreEqual(1, host.RegistryAttachedCalls);
            Assert.AreEqual(1, host.LoadedCalls);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.ShowCalls);
            Assert.AreEqual("load", host.LastSfxAction);
            Assert.AreEqual(3, host.LastSectionCount);
        }

        [Test]
        public void AttachRegistry_Null_FallsBackToEmpty()
        {
            var svc = new AttribConstService(MakeRegistry());
            svc.AttachRegistry(null);
            Assert.AreEqual(0, svc.Count);
        }

        // ── GetAllSections / GetSection ─────────────────────────────────────

        [Test]
        public void GetAllSections_Empty()
        {
            var svc = new AttribConstService();
            int n = 0;
            foreach (var _ in svc.GetAllSections()) n++;
            Assert.AreEqual(0, n);
        }

        [Test]
        public void GetAllSections_Populated()
        {
            var svc = new AttribConstService(MakeRegistry());
            int n = 0;
            foreach (var _ in svc.GetAllSections()) n++;
            Assert.AreEqual(3, n);
        }

        [Test]
        public void GetSection_Empty()
        {
            var svc = new AttribConstService();
            Assert.AreEqual(0, svc.GetSection("").Count);
        }

        [Test]
        public void GetSection_Null_Empty()
        {
            var svc = new AttribConstService();
            Assert.AreEqual(0, svc.GetSection(null).Count);
        }

        [Test]
        public void GetSection_NotFound_Empty()
        {
            var svc = new AttribConstService(MakeRegistry());
            Assert.AreEqual(0, svc.GetSection("Unknown").Count);
        }

        [Test]
        public void GetSection_Exists()
        {
            var svc = new AttribConstService(MakeRegistry());
            var list = svc.GetSection("MagicDesc");
            Assert.AreEqual(2, list.Count);
        }

        [Test]
        public void GetSection_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AttribConstService(MakeRegistry(), host);
            svc.GetSection("MagicDesc");
            Assert.AreEqual(1, host.SectionQueriedCalls);
            Assert.AreEqual("MagicDesc", host.LastSection);
            Assert.AreEqual(2, host.LastEntryCount);
        }

        [Test]
        public void GetSection_Missing_Dispatches()
        {
            var host = new FakeHost();
            var svc = new AttribConstService(MakeRegistry(), host);
            svc.GetSection("Unknown");
            Assert.AreEqual(1, host.SectionMissingCalls);
            Assert.AreEqual(0, host.SectionQueriedCalls);
        }

        // ── GetValue / GetInt ───────────────────────────────────────────────

        [Test]
        public void GetValue_Exists()
        {
            var svc = new AttribConstService(MakeRegistry());
            Assert.AreEqual("100", svc.GetValue("MagicDesc", "Fire"));
        }

        [Test]
        public void GetValue_NotFound_Null()
        {
            var svc = new AttribConstService(MakeRegistry());
            Assert.IsNull(svc.GetValue("MagicDesc", "Unknown"));
        }

        [Test]
        public void GetValue_NullRegistry_Null()
        {
            var svc = new AttribConstService();
            Assert.IsNull(svc.GetValue("MagicDesc", "Fire"));
        }

        [Test]
        public void GetValue_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AttribConstService(MakeRegistry(), host);
            svc.GetValue("MagicDesc", "Fire");
            Assert.AreEqual(1, host.KeyQueriedCalls);
            Assert.AreEqual("Fire", host.LastKey);
            Assert.AreEqual("100", host.LastValue);
        }

        [Test]
        public void GetValue_NotFound_Dispatches()
        {
            var host = new FakeHost();
            var svc = new AttribConstService(MakeRegistry(), host);
            svc.GetValue("MagicDesc", "Unknown");
            Assert.AreEqual(1, host.KeyMissingCalls);
        }

        [Test]
        public void GetInt_Exists()
        {
            var svc = new AttribConstService(MakeRegistry());
            Assert.AreEqual(100, svc.GetInt("MagicDesc", "Fire"));
        }

        [Test]
        public void GetInt_NotFound_Fallback()
        {
            var svc = new AttribConstService(MakeRegistry());
            Assert.AreEqual(-1, svc.GetInt("MagicDesc", "Unknown", -1));
        }

        [Test]
        public void GetInt_NonNumeric_Fallback()
        {
            var reg = new PcAttribConstRegistry();
            var sec = new PcAttribConstSection { name = "Test" };
            sec.data["Bad"] = "NotANumber";
            reg.Register(sec);
            var svc = new AttribConstService(reg);
            Assert.AreEqual(99, svc.GetInt("Test", "Bad", 99));
        }

        // ── ResolveMagicCode ────────────────────────────────────────────────

        [Test]
        public void ResolveMagicCode_Exists()
        {
            var svc = new AttribConstService(MakeRegistry());
            Assert.AreEqual(100, svc.ResolveMagicCode("MagicDesc", "Fire"));
        }

        [Test]
        public void ResolveMagicCode_NotFound_NegativeOne()
        {
            var svc = new AttribConstService(MakeRegistry());
            Assert.AreEqual(-1, svc.ResolveMagicCode("MagicDesc", "Unknown"));
        }

        [Test]
        public void ResolveMagicCode_NullRegistry_NegativeOne()
        {
            var svc = new AttribConstService();
            Assert.AreEqual(-1, svc.ResolveMagicCode("MagicDesc", "Fire"));
        }

        [Test]
        public void ResolveMagicCode_NullSection_NegativeOne()
        {
            var svc = new AttribConstService(MakeRegistry());
            Assert.AreEqual(-1, svc.ResolveMagicCode(null, "Fire"));
        }

        [Test]
        public void ResolveMagicCode_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AttribConstService(MakeRegistry(), host);
            svc.ResolveMagicCode("MagicDesc", "Fire");
            Assert.AreEqual(1, host.MagicCodeResolvedCalls);
            Assert.AreEqual(100, host.LastMagicCode);
        }

        // ── OnAttribLoaded event ────────────────────────────────────────────

        [Test]
        public void AttachRegistry_FiresOnAttribLoadedEvent()
        {
            var svc = new AttribConstService();
            int fired = 0;
            svc.OnAttribLoaded += () => fired++;
            svc.AttachRegistry(MakeRegistry());
            Assert.AreEqual(1, fired);
        }

        // ── LoadFromStreamingAssets (smoke test) ───────────────────────────

        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => AttribConstService.LoadFromStreamingAssets());
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void AttribConstService_WithoutHost_DoesNotThrow()
        {
            var svc = new AttribConstService(MakeRegistry());
            Assert.DoesNotThrow(() => svc.GetSection("MagicDesc"));
            Assert.DoesNotThrow(() => svc.GetValue("MagicDesc", "Fire"));
            Assert.DoesNotThrow(() => svc.GetInt("MagicDesc", "Fire"));
            Assert.DoesNotThrow(() => svc.ResolveMagicCode("MagicDesc", "Fire"));
        }
    }
}
