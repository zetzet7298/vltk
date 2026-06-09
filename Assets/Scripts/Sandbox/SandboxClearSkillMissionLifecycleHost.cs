using System;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class SandboxClearSkillMissionLifecycleHost : IClearSkillMissionLifecycleHost
    {
        private readonly SandboxManager _manager;

        public SandboxClearSkillMissionLifecycleHost(SandboxManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public void StartMissionTimer(int missionId, int timerId, int ticks)
        {
            Debug.Log($"[ClearSkill] StartMissionTimer({missionId}, {timerId}, {ticks})");
            // Implement if there's a timer service
        }

        public int AddNpc(int templateId, int level, int subWorld, int mpsX, int mpsY, int direction, string name)
        {
            Debug.Log($"[ClearSkill] AddNpc({templateId}, {level}, {subWorld}, {mpsX}, {mpsY}, {direction}, {name})");
            // Map subWorld to actual map if needed, then spawn NPC using MapEnemySpawnRuntime or MapNpcRespawnService
            var worldPos = MapEnemyDatabase.MpsToWorld(mpsX, mpsY);
            // Ignore _manager.EnemyRuntime for now until method is finalized
            return 9000 + UnityEngine.Random.Range(1, 1000); // return dummy NpcId
        }

        public void SetMissionV(int slot, int value)
        {
            Debug.Log($"[ClearSkill] SetMissionV({slot}, {value})");
            _manager.SetPcMissionValue(slot, value);
        }

        public void SetNpcScript(int npcId, string scriptPath)
        {
            Debug.Log($"[ClearSkill] SetNpcScript({npcId}, {scriptPath})");
        }

        public void GameOver()
        {
            Debug.Log("[ClearSkill] GameOver()");
        }

        public int GetMissionV(int slot)
        {
            return _manager.GetPcMissionValue(slot);
        }

        public void DelNpc(int npcId)
        {
            Debug.Log($"[ClearSkill] DelNpc({npcId})");
        }

        public void SetPlayerIndex(int roleIndex)
        {
            Debug.Log($"[ClearSkill] SetPlayerIndex({roleIndex})");
        }

        public void SetLogoutRV(int value)
        {
            Debug.Log($"[ClearSkill] SetLogoutRV({value})");
            _manager.SetLogoutRv(value);
        }

        public void SetDeathScript(string scriptPath)
        {
            Debug.Log($"[ClearSkill] SetDeathScript({scriptPath})");
            _manager.SetDeathScript(scriptPath);
        }

        public void SetPKFlag(int value)
        {
            Debug.Log($"[ClearSkill] SetPKFlag({value})");
            _manager.SetPkFlag(value);
        }

        public void ForbidChangePK(int value)
        {
            Debug.Log($"[ClearSkill] ForbidChangePK({value})");
            _manager.ForbidChangePk(value);
        }

        public void SetTaskTemp(int taskId, int value)
        {
            Debug.Log($"[ClearSkill] SetTaskTemp({taskId}, {value})");
            _manager.SetTaskTemp(taskId, value);
        }

        public int GetMSPlayerCount(int missionId, int group)
        {
            return _manager.GetPcMissionPlayerGroup(missionId);
        }

        public void CloseMission(int missionId)
        {
            Debug.Log($"[ClearSkill] CloseMission({missionId})");
        }
    }
}
