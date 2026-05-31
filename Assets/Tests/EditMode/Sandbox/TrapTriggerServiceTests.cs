using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M3.4 — Trap Script Hook tests. Stub log when the Lua bridge is disabled
    /// (AC#1), Lua function attempted when enabled (AC#2), and failure surfaced with
    /// the trap id (AC#3).
    /// </summary>
    public class TrapTriggerServiceTests
    {
        private class FakeLuaRuntime : ILuaRuntime
        {
            public HashSet<string> functions = new();
            public LuaCallResult nextCall;

            public LuaLoadResult Load(string scriptId, string source)
                => new LuaLoadResult { scriptId = scriptId, status = LuaLoadStatus.Loaded };
            public bool HasFunction(string scriptId, string functionName) => functions.Contains(functionName);
            public LuaCallResult Call(string scriptId, string functionName,
                IReadOnlyDictionary<string, object> bindings, object[] args)
                => nextCall ?? new LuaCallResult { success = true, value = "ok" };
        }

        private TrapDefinition MakeTrap(int index, string scriptRef)
            => new TrapDefinition { trapIndex = index, scriptRef = scriptRef, triggerType = TrapTriggerType.Enter };

        private LuaScriptBridge MakeBridge(FakeLuaRuntime rt)
            => new LuaScriptBridge(rt, _ => "function OnEnter() end");

        // --- AC#1: bridge disabled → stub log ---

        [Test]
        public void OnPlayerEnter_BridgeDisabled_StubLogs()
        {
            var svc = new TrapTriggerService(bridge: null, luaEnabled: false);
            LogAssert.Expect(LogType.Log, "[Trap] [STUB] Trap 3 triggered (script=scripts/trap.lua)");
            var rec = svc.OnPlayerEnter(MakeTrap(3, "scripts/trap.lua"));
            Assert.AreEqual(TrapFireOutcome.Stubbed, rec.outcome);
            Assert.AreEqual(1, svc.Log.Count);
        }

        [Test]
        public void OnPlayerEnter_BridgeEnabledButNull_StubLogs()
        {
            // Enabled flag true but no bridge instance → still safe stub.
            var svc = new TrapTriggerService(bridge: null, luaEnabled: true);
            LogAssert.Expect(LogType.Log, "[Trap] [STUB] Trap 1 triggered (script=<none>)");
            var rec = svc.OnPlayerEnter(MakeTrap(1, null));
            Assert.AreEqual(TrapFireOutcome.Stubbed, rec.outcome);
        }

        // --- AC#2: bridge enabled → Lua function attempted ---

        [Test]
        public void OnPlayerEnter_BridgeEnabled_InvokesLua()
        {
            var rt = new FakeLuaRuntime();
            rt.functions.Add("OnEnter");
            rt.nextCall = new LuaCallResult { success = true, value = "done" };
            var svc = new TrapTriggerService(MakeBridge(rt), luaEnabled: true);

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'scripts/trap.lua' v1");
            LogAssert.Expect(LogType.Log, "[Lua] Ran 'scripts/trap.lua.OnEnter' → done");
            var rec = svc.OnPlayerEnter(MakeTrap(5, "scripts/trap.lua"));
            Assert.AreEqual(TrapFireOutcome.LuaInvoked, rec.outcome);
            StringAssert.Contains("done", rec.detail);
        }

        [Test]
        public void OnPlayerEnter_BridgeEnabled_NoScript_ReportsNoScript()
        {
            var rt = new FakeLuaRuntime();
            var svc = new TrapTriggerService(MakeBridge(rt), luaEnabled: true);
            LogAssert.Expect(LogType.Warning, "[Trap] Trap 2 has no script reference");
            var rec = svc.OnPlayerEnter(MakeTrap(2, null));
            Assert.AreEqual(TrapFireOutcome.NoScript, rec.outcome);
        }

        // --- AC#3: Lua failure surfaced with trap id ---

        [Test]
        public void OnPlayerEnter_LuaFunctionMissing_FailsWithTrapId()
        {
            var rt = new FakeLuaRuntime(); // no OnEnter registered
            var svc = new TrapTriggerService(MakeBridge(rt), luaEnabled: true);

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'scripts/trap.lua' v1");
            LogAssert.Expect(LogType.Warning, "[Lua] Function 'OnEnter' not found in 'scripts/trap.lua'");
            LogAssert.Expect(LogType.Error, "[Trap] Trap 9 script 'scripts/trap.lua.OnEnter' failed: Function 'OnEnter' not found in 'scripts/trap.lua'");
            var rec = svc.OnPlayerEnter(MakeTrap(9, "scripts/trap.lua"));
            Assert.AreEqual(TrapFireOutcome.LuaFailed, rec.outcome);
        }

        [Test]
        public void OnPlayerEnter_LuaReturnsFailure_SurfacedWithTrapId()
        {
            var rt = new FakeLuaRuntime();
            rt.functions.Add("OnEnter");
            rt.nextCall = new LuaCallResult { success = false, error = "nil index" };
            var svc = new TrapTriggerService(MakeBridge(rt), luaEnabled: true);

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'scripts/trap.lua' v1");
            LogAssert.Expect(LogType.Error, "[Trap] Trap 4 script 'scripts/trap.lua.OnEnter' failed: nil index");
            var rec = svc.OnPlayerEnter(MakeTrap(4, "scripts/trap.lua"));
            Assert.AreEqual(TrapFireOutcome.LuaFailed, rec.outcome);
            StringAssert.Contains("nil index", rec.detail);
        }

        // --- log management ---

        [Test]
        public void Log_AccumulatesAndClears()
        {
            var svc = new TrapTriggerService(bridge: null, luaEnabled: false);
            LogAssert.Expect(LogType.Log, "[Trap] [STUB] Trap 1 triggered (script=<none>)");
            LogAssert.Expect(LogType.Log, "[Trap] [STUB] Trap 2 triggered (script=<none>)");
            svc.OnPlayerEnter(MakeTrap(1, null));
            svc.OnPlayerEnter(MakeTrap(2, null));
            Assert.AreEqual(2, svc.Log.Count);
            svc.ClearLog();
            Assert.AreEqual(0, svc.Log.Count);
        }

        [Test]
        public void EnableDisable_TogglesBehavior()
        {
            var rt = new FakeLuaRuntime();
            rt.functions.Add("OnEnter");
            var svc = new TrapTriggerService(MakeBridge(rt), luaEnabled: false);
            LogAssert.Expect(LogType.Log, "[Trap] [STUB] Trap 1 triggered (script=scripts/trap.lua)");
            Assert.AreEqual(TrapFireOutcome.Stubbed, svc.OnPlayerEnter(MakeTrap(1, "scripts/trap.lua")).outcome);

            svc.LuaBridgeEnabled = true;
            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'scripts/trap.lua' v1");
            LogAssert.Expect(LogType.Log, "[Lua] Ran 'scripts/trap.lua.OnEnter' → ok");
            Assert.AreEqual(TrapFireOutcome.LuaInvoked, svc.OnPlayerEnter(MakeTrap(1, "scripts/trap.lua")).outcome);
        }
    }
}
