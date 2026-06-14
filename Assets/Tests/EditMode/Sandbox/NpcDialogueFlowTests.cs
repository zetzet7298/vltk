// -----------------------------------------------------------------------------
// VLTK Mobile — NpcDialogueService EditMode tests.
// Kiểm tra hội thoại NPC lifecycle: 4 NPC types (Dã Tẩu 500, Võ Sư 311,
// Xa Phu 501, default), option filtering theo condition, host dispatch chain
// (open/options/SFX/greeting/log/close/quest).
// PC source: NPC dialogue flows + Vietnamese localization.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class NpcDialogueFlowTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : INpcDialogueHost
        {
            public int OpenCalls;
            public int CloseCalls;
            public int OptionsCalls;
            public int SfxCalls;
            public int LogCalls;
            public int GreetingCalls;
            public int QuestCalls;
            public int LastNpcTemplateId;
            public int LastPlayerLevel;
            public int LastOptionCount;
            public int LastOptionIndex;
            public int LastQuestId;
            public string LastText;

            public void OnDialogueOpened(int npcTemplateId, int playerLevel, string npcTextVi)
            {
                OpenCalls++;
                LastNpcTemplateId = npcTemplateId;
                LastPlayerLevel = playerLevel;
                LastText = npcTextVi;
            }
            public void OnDialogueClosed(int npcTemplateId) { CloseCalls++; }
            public void OnDialogueOptions(int npcTemplateId, int playerLevel, int optionCount, string npcTextVi)
            {
                OptionsCalls++;
                LastOptionCount = optionCount;
            }
            public void PlayDialogueSFX(int npcTemplateId, int playerLevel) { SfxCalls++; }
            public void LogDialogueEvent(int npcTemplateId, int playerLevel, string message) { LogCalls++; }
            public void PlayNpcGreeting(int npcTemplateId, int playerLevel) { GreetingCalls++; }
            public void DispatchQuestOption(int npcTemplateId, int playerLevel, int optionIndex, int questId)
            {
                QuestCalls++;
                LastOptionIndex = optionIndex;
                LastQuestId = questId;
            }
        }

        private static NpcDialogueService BuildService(INpcDialogueHost host = null)
        {
            var task = new TaskFlagService();
            return new NpcDialogueService(task, host);
        }

        // ── Constructor / properties ───────────────────────────────────────

        [Test]
        public void Constructor_Default_NoHost()
        {
            var svc = new NpcDialogueService(new TaskFlagService());
            Assert.AreEqual(0, svc.CurrentNpcTemplateId);
        }

        [Test]
        public void Constructor_NullTaskService_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new NpcDialogueService(null));
        }

        [Test]
        public void Constructor_DefaultCtor_NullTaskService()
        {
            var svc = new NpcDialogueService();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new NpcDialogueService(new TaskFlagService());
            svc.AttachHost(host);
            svc.StartDialogue(500, 1);
            Assert.AreEqual(1, host.OpenCalls);
        }

        // ── StartDialogue: 4 NPC types ─────────────────────────────────────

        [Test]
        public void StartDialogue_DaTau_ReturnsRootNode()
        {
            var svc = BuildService();
            var node = svc.StartDialogue(500, 1);
            Assert.IsNotNull(node);
            Assert.AreEqual(1, node.nodeId);
            Assert.That(node.npcTextVi, Does.Contain("Dã Tẩu"));
        }

        [Test]
        public void StartDialogue_VoSu_ReturnsRootNode()
        {
            var svc = BuildService();
            var node = svc.StartDialogue(311, 1);
            Assert.IsNotNull(node);
            Assert.That(node.npcTextVi, Does.Contain("Võ Sư"));
        }

        [Test]
        public void StartDialogue_XaPhu_ReturnsRootNode()
        {
            var svc = BuildService();
            var node = svc.StartDialogue(501, 10);
            Assert.IsNotNull(node);
            Assert.That(node.npcTextVi, Does.Contain("Xa Phu"));
        }

        [Test]
        public void StartDialogue_Default_ReturnsGenericNode()
        {
            var svc = BuildService();
            var node = svc.StartDialogue(9999, 1);
            Assert.IsNotNull(node);
            Assert.That(node.npcTextVi, Does.Contain("NPC"));
        }

        [Test]
        public void StartDialogue_FiresOnDialogueStartedEvent()
        {
            var svc = BuildService();
            int fired = 0;
            svc.OnDialogueStarted += n => fired++;
            svc.StartDialogue(500, 1);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void StartDialogue_FiresOnNpcTemplateUsedEvent()
        {
            var svc = BuildService();
            int fired = 0;
            int lastTpl = 0;
            svc.OnNpcTemplateUsed += (t, l) => { fired++; lastTpl = t; };
            svc.StartDialogue(500, 5);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(500, lastTpl);
        }

        [Test]
        public void StartDialogue_SetsCurrentNpc()
        {
            var svc = BuildService();
            svc.StartDialogue(500, 5);
            Assert.AreEqual(500, svc.CurrentNpcTemplateId);
            Assert.AreEqual(5, svc.CurrentPlayerLevel);
        }

        [Test]
        public void StartDialogue_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.StartDialogue(500, 1);
            Assert.AreEqual(1, host.OpenCalls);
            Assert.AreEqual(1, host.OptionsCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.GreetingCalls);
            Assert.AreEqual(1, host.LogCalls);
        }

        [Test]
        public void StartDialogue_WithoutHost_NoThrow()
        {
            var svc = BuildService();
            Assert.DoesNotThrow(() => svc.StartDialogue(500, 1));
        }

        // ── Option filtering ────────────────────────────────────────────────

        [Test]
        public void StartDialogue_OptionsFilteredByCondition()
        {
            var task = new TaskFlagService();
            // Set quest 1000 to active state (1 = đã nhận)
            task.SetFlag(1000, 1, 0, 5, "Test quest");
            var svc = new NpcDialogueService(task);
            var node = svc.StartDialogue(500, 1);
            // Option 1 (nhận quest) bị filter ra vì GetFlag(1000) != 0
            // Option 2 (trả quest) cũng filter vì chưa complete
            // Còn option 3 (chỉ ghé ngang)
            bool hasNhan = false;
            foreach (var opt in node.options)
            {
                if (opt.textVi != null && opt.textVi.Contains("nhận")) hasNhan = true;
            }
            Assert.IsFalse(hasNhan);
        }

        [Test]
        public void StartDialogue_AllOptionsShownWhenConditionsMet()
        {
            var task = new TaskFlagService();
            // Set quest 1000 to complete (3 = đã trả)
            task.SetFlag(1000, 3, 5, 5, "Test quest");
            var svc = new NpcDialogueService(task);
            var node = svc.StartDialogue(500, 1);
            // Option 1 (nhận) bị filter (flag=3 != 0)
            // Option 2 (trả) xuất hiện vì IsTaskComplete true
            bool hasTra = false;
            foreach (var opt in node.options)
            {
                if (opt.textVi != null && opt.textVi.Contains("trả")) hasTra = true;
            }
            Assert.IsTrue(hasTra);
        }

        // ── SelectOption ────────────────────────────────────────────────────

        [Test]
        public void SelectOption_CloseOption_ReturnsNull()
        {
            var svc = BuildService();
            svc.StartDialogue(500, 1);
            var closeOpt = new DialogueOption { textVi = "Tạm biệt", targetNodeId = 0 };
            var result = svc.SelectOption(500, 1, closeOpt);
            Assert.IsNull(result);
        }

        [Test]
        public void SelectOption_Close_FiresOnDialogueEnded()
        {
            var svc = BuildService();
            svc.StartDialogue(500, 1);
            int ended = 0;
            svc.OnDialogueEnded += () => ended++;
            var closeOpt = new DialogueOption { textVi = "Tạm biệt", targetNodeId = 0 };
            svc.SelectOption(500, 1, closeOpt);
            Assert.AreEqual(1, ended);
        }

        [Test]
        public void SelectOption_Close_DispatchesOnDialogueClosed()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.StartDialogue(500, 1);
            var closeOpt = new DialogueOption { textVi = "Tạm biệt", targetNodeId = 0 };
            svc.SelectOption(500, 1, closeOpt);
            Assert.AreEqual(1, host.CloseCalls);
        }

        [Test]
        public void SelectOption_InvokesSelectAction()
        {
            var svc = BuildService();
            svc.StartDialogue(500, 1);
            int actionCalled = 0;
            var opt = new DialogueOption
            {
                textVi = "Test",
                targetNodeId = 0, // close
                selectAction = () => actionCalled++
            };
            svc.SelectOption(500, 1, opt);
            Assert.AreEqual(1, actionCalled);
        }

        [Test]
        public void SelectOption_TargetNode_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.StartDialogue(500, 1); // Dã Tẩu has node 2
            // Build option pointing to node 2
            var opt = new DialogueOption
            {
                textVi = "Xem tiếp",
                targetNodeId = 2
            };
            var result = svc.SelectOption(500, 1, opt);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.nodeId);
            // SFX called once for option
            Assert.AreEqual(1, host.SfxCalls);
            // OnDialogueOpened called again for the new node
            Assert.AreEqual(2, host.OpenCalls); // 1 initial + 1 from select
        }

        [Test]
        public void SelectOption_NpcTextVi_Vietnamese()
        {
            var svc = BuildService();
            svc.StartDialogue(500, 1);
            var opt = new DialogueOption { textVi = "Xem tiếp", targetNodeId = 2 };
            var result = svc.SelectOption(500, 1, opt);
            Assert.That(result.npcTextVi, Does.Contain("Dã Tẩu"));
        }

        [Test]
        public void SelectOption_QuestOption_DispatchesQuest()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.StartDialogue(500, 1);
            var opt = new DialogueOption
            {
                textVi = "Nhận nhiệm vụ",
                targetNodeId = 2
            };
            svc.SelectOption(500, 1, opt);
            Assert.AreEqual(1, host.QuestCalls);
            Assert.AreEqual(1000, host.LastQuestId);
        }

        [Test]
        public void SelectOption_NonQuestOption_NoQuestDispatch()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.StartDialogue(500, 1);
            var opt = new DialogueOption
            {
                textVi = "Xem tiếp",
                targetNodeId = 2
            };
            svc.SelectOption(500, 1, opt);
            Assert.AreEqual(0, host.QuestCalls);
        }

        [Test]
        public void SelectOption_NoMatchingNode_Closes()
        {
            var svc = BuildService();
            svc.StartDialogue(500, 1);
            var opt = new DialogueOption { textVi = "Test", targetNodeId = 999 };
            int ended = 0;
            svc.OnDialogueEnded += () => ended++;
            svc.SelectOption(500, 1, opt);
            Assert.AreEqual(1, ended);
        }

        // ── CloseDialogue ───────────────────────────────────────────────────

        [Test]
        public void CloseDialogue_FiresOnDialogueEnded()
        {
            var svc = BuildService();
            svc.StartDialogue(500, 1);
            int fired = 0;
            svc.OnDialogueEnded += () => fired++;
            svc.CloseDialogue();
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void CloseDialogue_ResetsCurrentNpc()
        {
            var svc = BuildService();
            svc.StartDialogue(500, 1);
            svc.CloseDialogue();
            Assert.AreEqual(0, svc.CurrentNpcTemplateId);
            Assert.AreEqual(0, svc.CurrentPlayerLevel);
        }

        [Test]
        public void CloseDialogue_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.StartDialogue(500, 1);
            svc.CloseDialogue();
            Assert.AreEqual(1, host.CloseCalls);
        }

        [Test]
        public void CloseDialogue_WithoutHost_NoThrow()
        {
            var svc = BuildService();
            svc.StartDialogue(500, 1);
            Assert.DoesNotThrow(() => svc.CloseDialogue());
        }

        [Test]
        public void CloseDialogue_WithoutStart_StillFires()
        {
            var svc = BuildService();
            int ended = 0;
            svc.OnDialogueEnded += () => ended++;
            svc.CloseDialogue();
            Assert.AreEqual(1, ended);
        }
    }
}
