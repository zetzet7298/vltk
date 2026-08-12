// -----------------------------------------------------------------------------
// VLTK Mobile — ClearSkill mission lifecycle plan recorder.
// PC source of truth: 00.src-tinh-kiem/server1/script/missions/clearskill/{mission,timer}.lua
// Pure model-only adapter: records planned host calls; it does not execute Unity gameplay.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public static class ClearSkillMissionLifecyclePlanExecutor
    {
        public static ClearSkillMissionLifecycleExecutionResult Replay(
            IReadOnlyList<LifecycleOperation> plan,
            IClearSkillMissionLifecycleHost host)
        {
            var failures = new List<ClearSkillMissionLifecycleExecutionFailure>();
            if (plan == null)
            {
                failures.Add(new ClearSkillMissionLifecycleExecutionFailure(-1, string.Empty, "plan is null"));
                return new ClearSkillMissionLifecycleExecutionResult(failures);
            }
            if (host == null)
            {
                failures.Add(new ClearSkillMissionLifecycleExecutionFailure(-1, string.Empty, "host is null"));
                return new ClearSkillMissionLifecycleExecutionResult(failures);
            }

            int? lastResult = null;
            for (int i = 0; i < plan.Count; i++)
            {
                var op = plan[i];
                if (op == null)
                {
                    failures.Add(new ClearSkillMissionLifecycleExecutionFailure(i, string.Empty, "operation is null"));
                    continue;
                }

                var args = ResolveArgs(op, lastResult, i, failures);
                if (args == null) continue;

                int? result;
                if (!Dispatch(op.Name, args, op.TextArg, host, i, failures, out result)) continue;
                if (result.HasValue) lastResult = result.Value;
            }

            return new ClearSkillMissionLifecycleExecutionResult(failures);
        }

        private static int[] ResolveArgs(LifecycleOperation op, int? lastResult, int index, List<ClearSkillMissionLifecycleExecutionFailure> failures)
        {
            var args = (int[])op.IntArgs.Clone();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != LifecycleOperation.ResultOfPreviousOperation) continue;
                if (!lastResult.HasValue)
                {
                    failures.Add(new ClearSkillMissionLifecycleExecutionFailure(index, op.Name, "missing previous result"));
                    return null;
                }
                args[i] = lastResult.Value;
            }
            return args;
        }

        private static bool Dispatch(string name, int[] a, string text, IClearSkillMissionLifecycleHost host, int index,
            List<ClearSkillMissionLifecycleExecutionFailure> failures, out int? result)
        {
            result = null;
            switch (name)
            {
                case "StartMissionTimer": if (!Require(name, a, 3, index, failures)) return false; host.StartMissionTimer(a[0], a[1], a[2]); return true;
                case "AddNpc": if (!Require(name, a, 6, index, failures)) return false; result = host.AddNpc(a[0], a[1], a[2], a[3], a[4], a[5], text); return true;
                case "SetMissionV": if (!Require(name, a, 2, index, failures)) return false; host.SetMissionV(a[0], a[1]); return true;
                case "SetNpcScript": if (!Require(name, a, 1, index, failures)) return false; host.SetNpcScript(a[0], text); return true;
                case "GameOver": if (!Require(name, a, 0, index, failures)) return false; host.GameOver(); return true;
                case "GetMissionV": if (!Require(name, a, 1, index, failures)) return false; result = host.GetMissionV(a[0]); return true;
                case "DelNpc": if (!Require(name, a, 1, index, failures)) return false; host.DelNpc(a[0]); return true;
                case "SetPlayerIndex": if (!Require(name, a, 1, index, failures)) return false; host.SetPlayerIndex(a[0]); return true;
                case "SetLogoutRV": if (!Require(name, a, 1, index, failures)) return false; host.SetLogoutRV(a[0]); return true;
                case "SetDeathScript": if (!Require(name, a, 0, index, failures)) return false; host.SetDeathScript(text); return true;
                case "SetPKFlag": if (!Require(name, a, 1, index, failures)) return false; host.SetPKFlag(a[0]); return true;
                case "ForbidChangePK": if (!Require(name, a, 1, index, failures)) return false; host.ForbidChangePK(a[0]); return true;
                case "SetTaskTemp": if (!Require(name, a, 2, index, failures)) return false; host.SetTaskTemp(a[0], a[1]); return true;
                case "GetMSPlayerCount": if (!Require(name, a, 2, index, failures)) return false; result = host.GetMSPlayerCount(a[0], a[1]); return true;
                case "CloseMission": if (!Require(name, a, 1, index, failures)) return false; host.CloseMission(a[0]); return true;
                default:
                    failures.Add(new ClearSkillMissionLifecycleExecutionFailure(index, name, "unsupported operation"));
                    return false;
            }
        }

        private static bool Require(string name, int[] args, int count, int index, List<ClearSkillMissionLifecycleExecutionFailure> failures)
        {
            if (args.Length == count) return true;
            failures.Add(new ClearSkillMissionLifecycleExecutionFailure(index, name, "expected " + count + " int args, got " + args.Length));
            return false;
        }
    }

    public interface IClearSkillMissionLifecycleHost
    {
        void StartMissionTimer(int missionId, int timerId, int ticks);
        int AddNpc(int templateId, int level, int subWorld, int mpsX, int mpsY, int direction, string name);
        void SetMissionV(int slot, int value);
        void SetNpcScript(int npcId, string scriptPath);
        void GameOver();
        int GetMissionV(int slot);
        void DelNpc(int npcId);
        void SetPlayerIndex(int roleIndex);
        void SetLogoutRV(int value);
        void SetDeathScript(string scriptPath);
        void SetPKFlag(int value);
        void ForbidChangePK(int value);
        void SetTaskTemp(int taskId, int value);
        int GetMSPlayerCount(int missionId, int group);
        void CloseMission(int missionId);
    }

    public sealed class RecordingClearSkillMissionLifecycleHost : IClearSkillMissionLifecycleHost
    {
        private readonly List<ClearSkillMissionLifecycleCall> _calls = new List<ClearSkillMissionLifecycleCall>();
        private readonly Dictionary<int, int> _missionValues = new Dictionary<int, int>();

        public IReadOnlyList<ClearSkillMissionLifecycleCall> Calls { get { return _calls; } }
        public int NextNpcId { get; set; } = 9001;
        public int MissionPlayerCount { get; set; }

        public void SetMissionValueSnapshot(int slot, int value) => _missionValues[slot] = value;

        public void StartMissionTimer(int missionId, int timerId, int ticks) => Add("StartMissionTimer", new[] { missionId, timerId, ticks });
        public int AddNpc(int templateId, int level, int subWorld, int mpsX, int mpsY, int direction, string name) { Add("AddNpc", new[] { templateId, level, subWorld, mpsX, mpsY, direction }, name, NextNpcId); return NextNpcId; }
        public void SetMissionV(int slot, int value) { _missionValues[slot] = value; Add("SetMissionV", new[] { slot, value }); }
        public void SetNpcScript(int npcId, string scriptPath) => Add("SetNpcScript", new[] { npcId }, scriptPath);
        public void GameOver() => Add("GameOver", Array.Empty<int>());
        public int GetMissionV(int slot) { int value; _missionValues.TryGetValue(slot, out value); Add("GetMissionV", new[] { slot }, null, value); return value; }
        public void DelNpc(int npcId) => Add("DelNpc", new[] { npcId });
        public void SetPlayerIndex(int roleIndex) => Add("SetPlayerIndex", new[] { roleIndex });
        public void SetLogoutRV(int value) => Add("SetLogoutRV", new[] { value });
        public void SetDeathScript(string scriptPath) => Add("SetDeathScript", Array.Empty<int>(), scriptPath);
        public void SetPKFlag(int value) => Add("SetPKFlag", new[] { value });
        public void ForbidChangePK(int value) => Add("ForbidChangePK", new[] { value });
        public void SetTaskTemp(int taskId, int value) => Add("SetTaskTemp", new[] { taskId, value });
        public int GetMSPlayerCount(int missionId, int group) { Add("GetMSPlayerCount", new[] { missionId, group }, null, MissionPlayerCount); return MissionPlayerCount; }
        public void CloseMission(int missionId) => Add("CloseMission", new[] { missionId });

        private void Add(string name, int[] args, string text = null, int? returnValue = null)
        {
            _calls.Add(new ClearSkillMissionLifecycleCall(name, args, text, returnValue));
        }
    }

    public sealed class ClearSkillMissionLifecycleCall
    {
        public ClearSkillMissionLifecycleCall(string name, int[] intArgs, string textArg, int? returnValue)
        {
            Name = name;
            IntArgs = intArgs ?? Array.Empty<int>();
            TextArg = textArg;
            ReturnValue = returnValue;
        }

        public string Name { get; }
        public int[] IntArgs { get; }
        public string TextArg { get; }
        public int? ReturnValue { get; }
    }

    public sealed class ClearSkillMissionLifecycleExecutionResult
    {
        public ClearSkillMissionLifecycleExecutionResult(IReadOnlyList<ClearSkillMissionLifecycleExecutionFailure> failures)
        {
            Failures = failures ?? Array.Empty<ClearSkillMissionLifecycleExecutionFailure>();
        }

        public bool Succeeded { get { return Failures.Count == 0; } }
        public IReadOnlyList<ClearSkillMissionLifecycleExecutionFailure> Failures { get; }
    }

    public sealed class ClearSkillMissionLifecycleExecutionFailure
    {
        public ClearSkillMissionLifecycleExecutionFailure(int operationIndex, string operationName, string reason)
        {
            OperationIndex = operationIndex;
            OperationName = operationName;
            Reason = reason;
        }

        public int OperationIndex { get; }
        public string OperationName { get; }
        public string Reason { get; }
    }
}
