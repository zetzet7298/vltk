using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Outcome of a trap trigger firing.</summary>
    public enum TrapFireOutcome
    {
        Stubbed,        // AC#1: bridge disabled, stub logged
        LuaInvoked,     // AC#2: Lua function ran successfully
        LuaFailed,      // AC#3: Lua function failed/errored
        NoScript,       // trap has no script ref
    }

    /// <summary>Record of a single trap fire (for the GM Logs tab).</summary>
    public class TrapFireRecord
    {
        public int trapIndex;
        public string scriptRef;
        public TrapFireOutcome outcome;
        public string detail;

        public override string ToString()
            => $"trap#{trapIndex} [{outcome}] script={scriptRef ?? "<none>"} {detail}";
    }

    /// <summary>
    /// M3.4 — Routes trap/trigger region entry to either a stub log (Lua bridge
    /// disabled, AC#1) or the configured Lua hook (bridge enabled, AC#2), surfacing
    /// failures with the trap id in the log (AC#3). Pure C# (no MonoBehaviour) so it
    /// is fully EditMode-testable. A MonoBehaviour driver calls
    /// <see cref="OnPlayerEnter"/> when the player enters a trap's bounds.
    /// </summary>
    public class TrapTriggerService
    {
        private readonly LuaScriptBridge _bridge;
        private readonly List<TrapFireRecord> _log = new();

        /// <summary>AC#1/AC#2 — when false, traps only stub-log; when true, Lua hooks fire.</summary>
        public bool LuaBridgeEnabled { get; set; }

        /// <summary>Lua function name attempted on trap entry (configurable).</summary>
        public string EnterFunction { get; set; } = "OnEnter";

        public IReadOnlyList<TrapFireRecord> Log => _log;

        public TrapTriggerService(LuaScriptBridge bridge, bool luaEnabled = false)
        {
            _bridge = bridge;
            LuaBridgeEnabled = luaEnabled;
        }

        /// <summary>Fire a trap as the player enters its region.</summary>
        public TrapFireRecord OnPlayerEnter(TrapDefinition trap)
        {
            if (trap == null) throw new ArgumentNullException(nameof(trap));

            var record = new TrapFireRecord { trapIndex = trap.trapIndex, scriptRef = trap.scriptRef };

            // AC#1 — bridge disabled: stub log only.
            if (!LuaBridgeEnabled || _bridge == null)
            {
                record.outcome = TrapFireOutcome.Stubbed;
                record.detail = "stub trigger (Lua bridge disabled)";
                SubsystemLog.Info("Trap", $"[STUB] Trap {trap.trapIndex} triggered (script={trap.scriptRef ?? "<none>"})");
                _log.Add(record);
                return record;
            }

            // Bridge enabled but trap has no script.
            if (string.IsNullOrEmpty(trap.scriptRef))
            {
                record.outcome = TrapFireOutcome.NoScript;
                record.detail = "no script reference on trap";
                SubsystemLog.Warn("Trap", $"Trap {trap.trapIndex} has no script reference");
                _log.Add(record);
                return record;
            }

            // AC#2 — attempt the configured Lua function.
            var call = _bridge.Run(trap.scriptRef, EnterFunction, trap.trapIndex);
            if (call.success)
            {
                record.outcome = TrapFireOutcome.LuaInvoked;
                record.detail = $"{EnterFunction} → {call.value}";
            }
            else
            {
                // AC#3 — failure surfaced with the trap id.
                record.outcome = TrapFireOutcome.LuaFailed;
                record.detail = call.error ?? "lua call failed";
                SubsystemLog.Error("Trap",
                    $"Trap {trap.trapIndex} script '{trap.scriptRef}.{EnterFunction}' failed: {record.detail}");
            }
            _log.Add(record);
            return record;
        }

        public void ClearLog() => _log.Clear();
    }
}
