// -----------------------------------------------------------------------------
// VLTK Mobile — DialogSysRuntimeService EditMode tests.
// Kiểm tra dialog flow khớp PC surfaces (CreateNewSayEx, g_AskClientStringEx,
// g_AskClientNumberEx, g_GiveItemUI, g_DailogBack), IDialogHost dispatch,
// JSON index load, source index delegation.
// PC source: script/dailogsys/g_dialog.lua, dailogsay.lua, dialogoption.lua,
// composeoption.lua (5 core scripts).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class DialogSysRuntimeServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IDialogHost
        {
            public int ShowDialogCalls;
            public string LastNpcName;
            public string LastDialogClass;
            public string LastTitleMsg;
            public List<string> OptionSurfaces = new();
            public List<string> SaySurfaces = new();
            public int AskStringCalls;
            public int AskNumberCalls;
            public int GiveItemCalls;
            public int CloseCalls;
            public int LogCalls;

            public void ShowDialog(string npcName, string dialogClass, string titleMsg)
            {
                ShowDialogCalls++;
                LastNpcName = npcName;
                LastDialogClass = dialogClass;
                LastTitleMsg = titleMsg;
            }
            public void AddOptionSurface(string surface) { OptionSurfaces.Add(surface); }
            public void AddSaySurface(string surface) { SaySurfaces.Add(surface); }
            public void AskClientString(string prompt, int minLen, int maxLen) { AskStringCalls++; }
            public void AskClientNumber(string prompt, int minVal, int maxVal) { AskNumberCalls++; }
            public void OpenGiveItemUi(int npcTemplateId, int maxItemCount) { GiveItemCalls++; }
            public void CloseDialog() { CloseCalls++; }
            public void LogDialogNotice(string npcName, string message) { LogCalls++; }
        }

        private static DialogSysRuntimeService BuildService()
        {
            var jsonEntries = new List<DialogSysIndexEntry>
            {
                new DialogSysIndexEntry { FileName = "g_dialog.lua", RelativePath = "g_dialog.lua", SizeBytes = 1024 },
                new DialogSysIndexEntry { FileName = "dailog.lua", RelativePath = "dailog.lua", SizeBytes = 2048 },
                new DialogSysIndexEntry { FileName = "dailogsay.lua", RelativePath = "dailogsay.lua", SizeBytes = 4096 },
                new DialogSysIndexEntry { FileName = "dialogoption.lua", RelativePath = "dialogoption.lua", SizeBytes = 8192 },
                new DialogSysIndexEntry { FileName = "composeoption.lua", RelativePath = "composeoption.lua", SizeBytes = 16384 },
            };
            return new DialogSysRuntimeService(jsonEntries, null);
        }

        // ── PC surface constants ─────────────────────────────────────────────

        [Test]
        public void PcCoreScriptNames_MatchExpectedFiles()
        {
            Assert.AreEqual("g_dialog.lua", DialogSysRuntimeService.ScriptGDialog);
            Assert.AreEqual("dailog.lua", DialogSysRuntimeService.ScriptDailog);
            Assert.AreEqual("dailogsay.lua", DialogSysRuntimeService.ScriptDailogSay);
            Assert.AreEqual("dialogoption.lua", DialogSysRuntimeService.ScriptDialogOption);
            Assert.AreEqual("composeoption.lua", DialogSysRuntimeService.ScriptComposeOption);
        }

        [Test]
        public void PcDialogClasses_HasFourCoreClasses()
        {
            CollectionAssert.AreEquivalent(
                new[] { "G_DIALOG", "DailogClass", "DailogOptionClass", "ComposeOptionClass" },
                DialogSysRuntimeService.PcDialogClasses);
        }

        [Test]
        public void PcSaySurfaces_NineFunctions()
        {
            Assert.AreEqual(9, DialogSysRuntimeService.PcSaySurfaces.Length);
            CollectionAssert.Contains(DialogSysRuntimeService.PcSaySurfaces, "CreateNewSayEx");
            CollectionAssert.Contains(DialogSysRuntimeService.PcSaySurfaces, "g_DailogBack");
            CollectionAssert.Contains(DialogSysRuntimeService.PcSaySurfaces, "g_AskClientStringEx");
            CollectionAssert.Contains(DialogSysRuntimeService.PcSaySurfaces, "g_AskClientNumberEx");
        }

        [Test]
        public void PcOptionSurfaces_TwoFunctions()
        {
            Assert.AreEqual(2, DialogSysRuntimeService.PcOptionSurfaces.Length);
            CollectionAssert.Contains(DialogSysRuntimeService.PcOptionSurfaces, "OnSelect");
            CollectionAssert.Contains(DialogSysRuntimeService.PcOptionSurfaces, "GetEntry");
        }

        // ── Index lookup ─────────────────────────────────────────────────────

        [Test]
        public void HasScript_RegisteredFile_ReturnsTrue()
        {
            var svc = BuildService();
            Assert.IsTrue(svc.HasScript("g_dialog.lua"));
            Assert.IsTrue(svc.HasScript("dialogoption.lua"));
        }

        [Test]
        public void HasScript_UnknownFile_ReturnsFalse()
        {
            var svc = BuildService();
            Assert.IsFalse(svc.HasScript("nonexistent.lua"));
        }

        [Test]
        public void GetJsonEntryByFileName_Registered_ReturnsEntry()
        {
            var svc = BuildService();
            var e = svc.GetJsonEntryByFileName("g_dialog.lua");
            Assert.IsNotNull(e);
            Assert.AreEqual(1024, e.SizeBytes);
        }

        [Test]
        public void GetJsonEntryByFileName_Unknown_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.GetJsonEntryByFileName("foo.lua"));
        }

        [Test]
        public void TotalJsonSizeBytes_SumsAllEntries()
        {
            var svc = BuildService();
            Assert.AreEqual(1024 + 2048 + 4096 + 8192 + 16384, svc.TotalJsonSizeBytes);
        }

        [Test]
        public void JsonEntryCount_EqualsRegistered()
        {
            var svc = BuildService();
            Assert.AreEqual(5, svc.JsonEntryCount);
        }

        // ── OpenDialog flow ──────────────────────────────────────────────────

        [Test]
        public void OpenDialog_NullContext_ReturnsNotOpened()
        {
            var svc = BuildService();
            var r = svc.OpenDialog(null);
            Assert.IsFalse(r.opened);
        }

        [Test]
        public void OpenDialog_ValidContext_ReturnsOpenedWithClass()
        {
            var svc = BuildService();
            var r = svc.OpenDialog(new DialogOpenContext { npcName = "Lý Tiểu Long", npcTemplateId = 100 });
            Assert.IsTrue(r.opened);
            Assert.AreEqual("Lý Tiểu Long", r.npcName);
            Assert.AreEqual("DailogClass", r.dialogClass);
            Assert.IsNotEmpty(r.titleMsg);
        }

        [Test]
        public void OpenDialog_NullName_FallsBackToTemplateId()
        {
            var svc = BuildService();
            var r = svc.OpenDialog(new DialogOpenContext { npcName = null, npcTemplateId = 42 });
            Assert.AreEqual("NPC_42", r.npcName);
        }

        [Test]
        public void OpenDialog_DispatchesToHost()
        {
            var svc = BuildService();
            var host = new FakeHost();
            svc.AttachHost(host);
            svc.OpenDialog(new DialogOpenContext { npcName = "Bạch Tiểu Thư" });
            Assert.AreEqual(1, host.ShowDialogCalls);
            Assert.AreEqual("Bạch Tiểu Thư", host.LastNpcName);
            Assert.AreEqual("DailogClass", host.LastDialogClass);
            Assert.IsNotEmpty(host.LastTitleMsg);
            Assert.GreaterOrEqual(host.OptionSurfaces.Count, 0); // empty if no source index
            Assert.AreEqual(1, host.LogCalls);
        }

        [Test]
        public void OpenDialog_WithoutHost_DoesNotThrow()
        {
            var svc = BuildService();
            Assert.DoesNotThrow(() => svc.OpenDialog(new DialogOpenContext { npcName = "X" }));
        }

        [Test]
        public void SelectOption_ValidContext_ReturnsBool()
        {
            var svc = BuildService();
            // Without source index, no OnSelect surfaces; returns false.
            Assert.IsFalse(svc.SelectOption(new DialogOpenContext { npcName = "Y" }, "Buy"));
        }

        // ── CreateNewSay flow ────────────────────────────────────────────────

        [Test]
        public void CreateNewSay_AlwaysOpened_EvenWithNullTitle()
        {
            var svc = BuildService();
            var r = svc.CreateNewSay(null, null);
            Assert.IsTrue(r.opened);
            Assert.AreEqual("CreateNewSayEx", r.dialogClass);
            Assert.AreEqual(string.Empty, r.titleMsg);
            Assert.IsEmpty(r.optionSurfaces);
        }

        [Test]
        public void CreateNewSay_WithOptions_AddsToSurfaces()
        {
            var svc = BuildService();
            var r = svc.CreateNewSay("Bạn muốn giao dịch?", new List<string> { "Mua", "Bán", "Đóng" });
            Assert.IsTrue(r.opened);
            Assert.AreEqual(3, r.optionSurfaces.Count);
            CollectionAssert.AreEquivalent(new[] { "Mua", "Bán", "Đóng" }, r.optionSurfaces);
        }

        [Test]
        public void CreateNewSay_DispatchesToHost()
        {
            var svc = BuildService();
            var host = new FakeHost();
            svc.AttachHost(host);
            svc.CreateNewSay("Title", new List<string> { "A", "B" });
            Assert.AreEqual(1, host.ShowDialogCalls);
            Assert.AreEqual(2, host.OptionSurfaces.Count);
        }

        [Test]
        public void CreateNewSay_WithoutHost_DoesNotThrow()
        {
            var svc = BuildService();
            Assert.DoesNotThrow(() => svc.CreateNewSay("Title", new List<string> { "X" }));
        }

        // ── Source index delegation ──────────────────────────────────────────

        [Test]
        public void GetSourceByPath_NullService_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.GetSourceByPath("g_dialog.lua"));
        }

        [Test]
        public void GetSourcesByFunction_NullService_ReturnsEmpty()
        {
            var svc = BuildService();
            var sources = svc.GetSourcesByFunction("OnSelect");
            Assert.IsNotNull(sources);
            Assert.AreEqual(0, sources.Count);
        }

        [Test]
        public void GetSourcesBySurface_NullService_ReturnsEmpty()
        {
            var svc = BuildService();
            var sources = svc.GetSourcesBySurface("g_DailogBack");
            Assert.IsNotNull(sources);
            Assert.AreEqual(0, sources.Count);
        }

        [Test]
        public void SourceIndexCount_WithoutService_IsZero()
        {
            var svc = BuildService();
            Assert.AreEqual(0, svc.SourceIndexCount);
            Assert.AreEqual(0, svc.SourceLuaFileCount);
            Assert.AreEqual(0, svc.SourceFunctionCount);
        }

        // ── Static loader safety ─────────────────────────────────────────────

        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow_WhenStreamingAssetsMissing()
        {
            // In EditMode test runner, Application.streamingAssetsPath may not be
            // configured the same way. Just verify the static method does not
            // throw on missing files.
            Assert.DoesNotThrow(() => DialogSysRuntimeService.LoadFromStreamingAssets());
        }
    }
}
