using System;
using System.Collections.Generic;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Outcome of loading a Lua script through the bridge.</summary>
    public enum LuaLoadStatus
    {
        NotLoaded,
        Loaded,
        SyntaxError,
        EncodingError,
        Missing,
    }

    /// <summary>Result of loading a script.</summary>
    public class LuaLoadResult
    {
        public string scriptId;
        public LuaLoadStatus status;
        public string error;
        public int version;
        public bool Ok => status == LuaLoadStatus.Loaded;
    }

    /// <summary>Result of invoking a Lua function.</summary>
    public class LuaCallResult
    {
        public bool success;
        public object value;
        public string error;
        public List<string> missingBindings = new();
    }

    /// <summary>
    /// Pluggable Lua execution backend. Kept as an interface so the sandbox bridge
    /// stays runtime-agnostic (MoonSharp / NLua can be added later) and fully
    /// unit-testable with a fake. The runtime never throws across the boundary; it
    /// reports status/errors so the bridge can log gracefully.
    /// </summary>
    public interface ILuaRuntime
    {
        LuaLoadResult Load(string scriptId, string source);
        bool HasFunction(string scriptId, string functionName);
        LuaCallResult Call(string scriptId, string functionName, IReadOnlyDictionary<string, object> bindings, object[] args);
    }

    /// <summary>
    /// M3.3 — Controlled bridge for loading and running original map/NPC Lua scripts.
    /// Pure C# orchestration (no MonoBehaviour) so it is fully EditMode-testable.
    /// Registers script paths, loads source through an <see cref="ILuaRuntime"/> and
    /// reports syntax/encoding errors (AC#1), runs functions and logs the result
    /// (AC#2), logs missing API bindings instead of crashing (AC#3), and supports
    /// reload so a changed script version is used (AC#4).
    /// </summary>
    public class LuaScriptBridge
    {
        private readonly ILuaRuntime _runtime;
        private readonly Func<string, string> _sourceProvider; // scriptId -> source (or null if missing)
        private readonly Dictionary<string, object> _bindings = new();
        private readonly Dictionary<string, LuaLoadResult> _loaded = new();

        public LuaScriptBridge(ILuaRuntime runtime, Func<string, string> sourceProvider)
        {
            _runtime = runtime;
            _sourceProvider = sourceProvider;
        }

        public IReadOnlyDictionary<string, object> Bindings => _bindings;

        /// <summary>Register a host API binding that scripts may call (AC#3).</summary>
        public void Bind(string apiName, object impl)
        {
            _bindings[apiName] = impl;
        }

        public LuaLoadStatus GetStatus(string scriptId)
            => _loaded.TryGetValue(scriptId, out var r) ? r.status : LuaLoadStatus.NotLoaded;

        public int GetVersion(string scriptId)
            => _loaded.TryGetValue(scriptId, out var r) ? r.version : 0;

        /// <summary>
        /// AC#1 — load (or reload) a registered script. A null source from the
        /// provider is reported as Missing; the runtime classifies syntax/encoding
        /// errors. Loading increments the version so reloads are observable (AC#4).
        /// </summary>
        public LuaLoadResult Load(string scriptId)
        {
            int prevVersion = GetVersion(scriptId);
            var source = _sourceProvider?.Invoke(scriptId);
            if (source == null)
            {
                var miss = new LuaLoadResult
                {
                    scriptId = scriptId,
                    status = LuaLoadStatus.Missing,
                    error = "Script source not found",
                    version = prevVersion,
                };
                _loaded[scriptId] = miss;
                SubsystemLog.Warn("Lua", $"Load '{scriptId}': source missing");
                return miss;
            }

            var result = _runtime.Load(scriptId, source) ?? new LuaLoadResult
            {
                scriptId = scriptId,
                status = LuaLoadStatus.SyntaxError,
                error = "Runtime returned null",
            };
            result.scriptId = scriptId;
            result.version = result.Ok ? prevVersion + 1 : prevVersion;
            _loaded[scriptId] = result;

            if (result.Ok)
                SubsystemLog.Info("Lua", $"Loaded '{scriptId}' v{result.version}");
            else
                SubsystemLog.Warn("Lua", $"Load '{scriptId}' failed: {result.status} {result.error}");
            return result;
        }

        /// <summary>AC#4 — reload re-reads the source so a changed version is used.</summary>
        public LuaLoadResult Reload(string scriptId) => Load(scriptId);

        /// <summary>
        /// AC#2/AC#3 — run a function on a loaded script. If the script is not loaded
        /// it is loaded first. Missing functions and missing API bindings are logged
        /// and returned as a failed result rather than throwing.
        /// </summary>
        public LuaCallResult Run(string scriptId, string functionName, params object[] args)
        {
            if (GetStatus(scriptId) != LuaLoadStatus.Loaded)
            {
                var load = Load(scriptId);
                if (!load.Ok)
                    return new LuaCallResult { success = false, error = $"Script not loaded: {load.status}" };
            }

            if (!_runtime.HasFunction(scriptId, functionName))
            {
                var msg = $"Function '{functionName}' not found in '{scriptId}'";
                SubsystemLog.Warn("Lua", msg);
                return new LuaCallResult { success = false, error = msg };
            }

            LuaCallResult result;
            try
            {
                result = _runtime.Call(scriptId, functionName, _bindings, args)
                         ?? new LuaCallResult { success = false, error = "Runtime returned null" };
            }
            catch (Exception ex)
            {
                // The bridge must never crash the sandbox on a script fault (AC#3).
                SubsystemLog.Error("Lua", $"Call '{scriptId}.{functionName}' threw: {ex.Message}");
                return new LuaCallResult { success = false, error = ex.Message };
            }

            // AC#3 — surface any missing-binding calls the runtime reported.
            foreach (var mb in result.missingBindings)
                SubsystemLog.Warn("Lua", $"Missing binding '{mb}' called by '{scriptId}.{functionName}'");

            if (result.success)
                SubsystemLog.Info("Lua", $"Ran '{scriptId}.{functionName}' → {result.value}");
            else if (string.IsNullOrEmpty(result.error) && result.missingBindings.Count == 0)
                SubsystemLog.Warn("Lua", $"'{scriptId}.{functionName}' returned failure");

            return result;
        }
    }
}
