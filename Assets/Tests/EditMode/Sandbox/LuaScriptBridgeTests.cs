using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M3.3 — Lua Script Loader tests. Uses a fake ILuaRuntime so the bridge logic
    /// (load/error, run function, missing-binding logging, reload) is verified
    /// without a real Lua VM (AC#1–AC#4).
    /// </summary>
    public class LuaScriptBridgeTests
    {
        /// <summary>Configurable fake Lua runtime.</summary>
        private class FakeLuaRuntime : ILuaRuntime
        {
            public LuaLoadStatus loadStatus = LuaLoadStatus.Loaded;
            public string loadError;
            public HashSet<string> functions = new();
            public LuaCallResult nextCall;
            public bool throwOnCall;

            public LuaLoadResult Load(string scriptId, string source)
                => new LuaLoadResult { scriptId = scriptId, status = loadStatus, error = loadError };

            public bool HasFunction(string scriptId, string functionName) => functions.Contains(functionName);

            public LuaCallResult Call(string scriptId, string functionName,
                IReadOnlyDictionary<string, object> bindings, object[] args)
            {
                if (throwOnCall) throw new System.InvalidOperationException("lua boom");
                return nextCall ?? new LuaCallResult { success = true, value = "ok" };
            }
        }

        // --- AC#1: load reports success / error / missing ---

        [Test]
        public void Load_ValidScript_Loaded()
        {
            var rt = new FakeLuaRuntime { loadStatus = LuaLoadStatus.Loaded };
            var bridge = new LuaScriptBridge(rt, _ => "function f() end");

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'maps/a.lua' v1");
            var r = bridge.Load("maps/a.lua");
            Assert.IsTrue(r.Ok);
            Assert.AreEqual(1, r.version);
            Assert.AreEqual(LuaLoadStatus.Loaded, bridge.GetStatus("maps/a.lua"));
        }

        [Test]
        public void Load_SyntaxError_Reported()
        {
            var rt = new FakeLuaRuntime { loadStatus = LuaLoadStatus.SyntaxError, loadError = "unexpected symbol" };
            var bridge = new LuaScriptBridge(rt, _ => "function f(");

            LogAssert.Expect(LogType.Warning, "[Lua] Load 'bad.lua' failed: SyntaxError unexpected symbol");
            var r = bridge.Load("bad.lua");
            Assert.IsFalse(r.Ok);
            Assert.AreEqual(LuaLoadStatus.SyntaxError, r.status);
        }

        [Test]
        public void Load_MissingSource_ReportedMissing()
        {
            var rt = new FakeLuaRuntime();
            var bridge = new LuaScriptBridge(rt, _ => null); // provider returns null

            LogAssert.Expect(LogType.Warning, "[Lua] Load 'gone.lua': source missing");
            var r = bridge.Load("gone.lua");
            Assert.AreEqual(LuaLoadStatus.Missing, r.status);
        }

        [Test]
        public void Load_EncodingError_Reported()
        {
            var rt = new FakeLuaRuntime { loadStatus = LuaLoadStatus.EncodingError, loadError = "invalid GBK" };
            var bridge = new LuaScriptBridge(rt, _ => "\xff\xfe");
            LogAssert.Expect(LogType.Warning, "[Lua] Load 'enc.lua' failed: EncodingError invalid GBK");
            var r = bridge.Load("enc.lua");
            Assert.AreEqual(LuaLoadStatus.EncodingError, r.status);
        }

        // --- AC#2: run a function, logs result ---

        [Test]
        public void Run_ExistingFunction_ExecutesAndLogs()
        {
            var rt = new FakeLuaRuntime();
            rt.functions.Add("OnEnter");
            rt.nextCall = new LuaCallResult { success = true, value = 42 };
            var bridge = new LuaScriptBridge(rt, _ => "function OnEnter() return 42 end");

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'm.lua' v1");
            LogAssert.Expect(LogType.Log, "[Lua] Ran 'm.lua.OnEnter' → 42");
            var r = bridge.Run("m.lua", "OnEnter");
            Assert.IsTrue(r.success);
            Assert.AreEqual(42, r.value);
        }

        [Test]
        public void Run_MissingFunction_FailsGracefully()
        {
            var rt = new FakeLuaRuntime();
            var bridge = new LuaScriptBridge(rt, _ => "-- empty");

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'm.lua' v1");
            LogAssert.Expect(LogType.Warning, "[Lua] Function 'Nope' not found in 'm.lua'");
            var r = bridge.Run("m.lua", "Nope");
            Assert.IsFalse(r.success);
        }

        // --- AC#3: missing binding logged, no crash ---

        [Test]
        public void Run_MissingBinding_LoggedNotCrashed()
        {
            var rt = new FakeLuaRuntime();
            rt.functions.Add("DoThing");
            rt.nextCall = new LuaCallResult
            {
                success = true,
                value = null,
                missingBindings = new List<string> { "GameApi.Teleport" },
            };
            var bridge = new LuaScriptBridge(rt, _ => "function DoThing() GameApi.Teleport() end");

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'm.lua' v1");
            LogAssert.Expect(LogType.Warning, "[Lua] Missing binding 'GameApi.Teleport' called by 'm.lua.DoThing'");
            LogAssert.Expect(LogType.Log, "[Lua] Ran 'm.lua.DoThing' → ");
            var r = bridge.Run("m.lua", "DoThing");
            Assert.IsTrue(r.success);
            Assert.Contains("GameApi.Teleport", r.missingBindings);
        }

        [Test]
        public void Run_RuntimeThrows_CaughtAndReported()
        {
            var rt = new FakeLuaRuntime { throwOnCall = true };
            rt.functions.Add("Boom");
            var bridge = new LuaScriptBridge(rt, _ => "function Boom() error() end");

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'm.lua' v1");
            LogAssert.Expect(LogType.Error, "[Lua] Call 'm.lua.Boom' threw: lua boom");
            var r = bridge.Run("m.lua", "Boom");
            Assert.IsFalse(r.success);
            Assert.AreEqual("lua boom", r.error);
        }

        [Test]
        public void Bind_RegistersHostApi()
        {
            var rt = new FakeLuaRuntime();
            var bridge = new LuaScriptBridge(rt, _ => "-- x");
            bridge.Bind("GameApi.Log", new object());
            Assert.IsTrue(bridge.Bindings.ContainsKey("GameApi.Log"));
        }

        // --- AC#4: reload uses new version ---

        [Test]
        public void Reload_IncrementsVersion()
        {
            var rt = new FakeLuaRuntime { loadStatus = LuaLoadStatus.Loaded };
            var bridge = new LuaScriptBridge(rt, _ => "function f() end");

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'm.lua' v1");
            bridge.Load("m.lua");
            Assert.AreEqual(1, bridge.GetVersion("m.lua"));

            LogAssert.Expect(LogType.Log, "[Lua] Loaded 'm.lua' v2");
            var r = bridge.Reload("m.lua");
            Assert.AreEqual(2, r.version);
        }
    }
}
