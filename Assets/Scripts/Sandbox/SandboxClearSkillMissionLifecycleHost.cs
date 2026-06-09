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
            SubsystemLog.Info("ClearSkill", $"StartMissionTimer({missionId}, {timerId}, {ticks})");
            // Implement if there's a timer service
        }

        public int AddNpc(int templateId, int level, int subWorld, int mpsX, int mpsY, int direction, string name)
        {
            SubsystemLog.Info("ClearSkill", $"AddNpc({templateId}, {level}, {subWorld}, {mpsX}, {mpsY}, {direction}, {name})");
            // Map subWorld to actual map if needed, then spawn NPC using MapEnemySpawnRuntime or MapNpcRespawnService
            var worldPos = MapEnemyDatabase.MpsToWorld(mpsX, mpsY);
            _manager.EnemyRuntime?.SpawnEnemy(templateId, worldPos, direction);
            return 9000 + UnityEngine.Random.Range(1, 1000); // return dummy NpcId
        }

        public void SetMissionV(int slot, int value)
        {
            SubsystemLog.Info("ClearSkill", $"SetMissionV({slot}, {value})");
            _manager.SetPcMissionValue(slot, value);
        }

        public void SetNpcScript(int npcId, string scriptPath)
        {
            SubsystemLog.Info("ClearSkill", $"SetNpcScript({npcId}, {scriptPath})");
        }

        public void GameOver()
        {
            SubsystemLog.Info("ClearSkill", "GameOver()");
        }

        public int GetMissionV(int slot)
        {
            return _manager.GetPcMissionValue(slot);
        }

        public void DelNpc(int npcId)
        {
            SubsystemLog.Info("ClearSkill", $"DelNpc({npcId})");
        }

        public void SetPlayerIndex(int roleIndex)
        {
            SubsystemLog.Info("ClearSkill", $"SetPlayerIndex({roleIndex})");
        }

        public void SetLogoutRV(int value)
        {
            SubsystemLog.Info("ClearSkill", $"SetLogoutRV({value})");
            _manager.SetLogoutRv(value);
        }

        public void SetDeathScript(string scriptPath)
        {
            SubsystemLog.Info("ClearSkill", $"SetDeathScript({scriptPath})");
            _manager.SetDeathScript(scriptPath);
        }

        public void SetPKFlag(int value)
        {
            SubsystemLog.Info("ClearSkill", $"SetPKFlag({value})");
            _manager.SetPkFlag(value);
        }

        public void ForbidChangePK(int value)
        {
            SubsystemLog.Info("ClearSkill", $"ForbidChangePK({value})");
            _manager.ForbidChangePk(value);
        }

        public void SetTaskTemp(int taskId, int value)
        {
            SubsystemLog.Info("ClearSkill", $"SetTaskTemp({taskId}, {value})");
            _manager.SetTaskTemp(taskId, value);
        }

        public int GetMSPlayerCount(int missionId, int group)
        {
            return _manager.GetPcMissionPlayerGroup(missionId);
        }

        public void CloseMission(int missionId)
        {
            SubsystemLog.Info("ClearSkill", $"CloseMission({missionId})");
        }
    }
}
