// -----------------------------------------------------------------------------
// VLTK.Survivor — Wave config schema (structure-parity dhcd, giá trị own-design)
// -----------------------------------------------------------------------------
// Parity: 3 lớp LevelWaveConfig → WavePoolConfig → WaveMonsterConfig
// (BattleCore.LevelWaveConfig.cs / WavePoolConfig.cs / WaveMonsterConfig.cs),
// trigger enum WaveEventFuncType 9 giá trị (diffable-cs .../WaveEventFuncType.cs).
// Own-design: TriggerTimeout fail-safe, EliteRatio, EndType 0/1, mọi con số.
// Config dạng text (C# authoring) nạp qua WaveManager.InitByDiyLevelWave
// (parity LevelMonsterMgr.InitByDiyLevelWave hook — KHÔNG binary cfg dhcd).
// ScriptableObject = upgrade path nếu cần author trong Editor (D17).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Parity dhcd WaveEventFuncType — 9 trigger type quyết định "khi nào wave bắt đầu".</summary>
    public enum WaveEventFuncType
    {
        WAVE_TIME_TRIGGER = 1,                 // TriggerTime đạt → start
        WAVE_MONSTER_COUNT_PERCENT = 2,        // kill% của wave đang chạy (TriggerParams[0] = percent)
        WAVE_MONSTER_HP_PERCENT = 3,           // boss HP% (TriggerParams[0] = percent)
        WAVE_MONSTER_PLAY_SKILL = 4,           // monster cast skill (TriggerParams[0] = số lần)
        WAVE_TYPE_KILL_ALL_MONSTER_THISID = 5, // kill-all theo MonsterID (TriggerParams[0] = id)
        WAVE_TYPE_KILLALL_AND_TIMEOVER_THISID = 6, // kill-all id + hết TriggerTime
        WAVE_TYPE_OCCUPY_START = 7,            // chiếm ≥1 điểm (8 điểm đầu)
        WAVE_TYPE_OCCUPY_END = 8,              // chiếm điểm cụ thể (TriggerParams[0] = điểm index)
        WAVE_TYPE_OCCUPY_ALLEND = 9,           // chiếm toàn bộ (TriggerParams[0] = bitmask, own)
    }

    /// <summary>1 entry trong wave pool — parity WaveMonsterConfig (PoolID/MonsterID + ratio).</summary>
    [Serializable]
    public class WaveMonsterConfig
    {
        public int MonsterId = 1000;
        public bool IsBoss;      // parity MonsterCfg.IsBoss (boss-flag wave)
        public bool IsElite;     // own — dhcd KHÔNG có elite (research gap 4.1), self-design flag
        public float HpRatio = 1f;
        public float AtkRatio = 1f;
    }

    /// <summary>Nhịp spawn của 1 wave — parity WavePoolConfig (subset + own fields).</summary>
    [Serializable]
    public class WavePoolConfig
    {
        public float Time = 20f;               // wave lifetime (giây); 0 = không giới hạn thời gian
        public float Interval = 2f;            // giây/batch (ramp nhân vào đây)
        public int SingleNum = 2;              // con/batch
        public int MonsterNum = 8;             // quota tổng (bỏ qua khi Isloop=true)
        public float DynamicMonsterTime;       // giây từ wave start → bật dynamic phase (0 = tắt)
        public float DynamicLoopNum = 1f;      // own: batch multiplier khi dynamic (parity DynamicLoopNum)
        public int DynamicMonsterMaxNum;       // alive cap dynamic (0 = dùng MonsterNum)
        public bool Isloop;                    // parity: wave tự loop sau khi hết quota
        public float EliteRatio;               // own: xác suất elite mỗi lần spawn (0..1)
        public float HpRatio = 1f;
        public float AtkRatio = 1f;
        public List<WaveMonsterConfig> Monsters = new List<WaveMonsterConfig>();
    }

    /// <summary>1 wave — parity LevelWaveConfig (subset + own TriggerTimeout/EndType).</summary>
    [Serializable]
    public class LevelWaveConfig
    {
        public int WaveId;
        public WaveEventFuncType WaveType = WaveEventFuncType.WAVE_TIME_TRIGGER;
        public float TriggerTime = 1f;         // type 1/6
        public int[] TriggerParams;            // type 2/3/4/5/6/8/9: [0] = percent|monsterId|castCnt|point|mask
        public int EndType;                    // own: 0 = timeover (Time hết), 1 = kill-all (quota chết hết)
        public bool IsDeleteAllMonster = true; // parity: dọn monster còn sống khi wave kết thúc
        public bool IsReposeWave;              // parity field — endless ticket 41 dùng
        public float TriggerTimeout = 25f;     // own fail-safe: trigger không bao giờ met → force start
        public WavePoolConfig Pool = new WavePoolConfig();
    }

    /// <summary>Thông tin 1 lần spawn thực tế — director dùng để dựng monster GO.</summary>
    public readonly struct MonsterSpawnInfo
    {
        public readonly Vector3 Pos;
        public readonly int MonsterId;
        public readonly bool IsBoss;
        public readonly bool IsElite;
        public readonly float HpMul;
        public readonly float AtkMul;
        public readonly float SpeedMul;

        public MonsterSpawnInfo(Vector3 pos, int monsterId, bool isBoss, bool isElite,
            float hpMul, float atkMul, float speedMul)
        {
            Pos = pos; MonsterId = monsterId; IsBoss = isBoss; IsElite = isElite;
            HpMul = hpMul; AtkMul = atkMul; SpeedMul = speedMul;
        }
    }

    /// <summary>Context trigger mỗi tick — director cung cấp phần world-state, manager tự tính phần wave.</summary>
    public struct WaveTriggerContext
    {
        public float Elapsed;        // giây từ StartSpawn (manager tự fill)
        public float BossHpPercent;  // 0..1 của boss còn sống; không có boss → 1 (type 3 không fire)
        public int SkillCastCount;   // tổng skill cast (P2 nối vào; P1 luôn 0)
        public int OccupiedMask;     // bitmask điểm chiếm đóng (type 7-9; capture-mode ticket sẽ set)
    }
}
