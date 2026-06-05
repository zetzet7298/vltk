// -----------------------------------------------------------------------------
// VLTK Mobile — ST-8.14 Battle Script Runtime Service (Kịch Bản Chiến Đấu runtime)
// Runtime layer bao bọc BattleScriptService — thêm evaluate condition + execute.
// Vietnamese: "Kịch Bản", "Chiến Đấu", "Bắt Đầu", "Kết Thúc", "Giết Boss", "Chết".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Context truyền vào khi evaluate / execute battle script.
    /// </summary>
    public class BattleContext
    {
        public int playerId;
        public int playerFaction;
        public int playerLevel;
        public int playerKills;
        public int playerDeaths;
        public int currentMapId;
        public int currentNpcId;
        public int teamScore;
        public int enemyScore;
        public long elapsedSec;
        public int randomSeed;
    }

    /// <summary>
    /// Service runtime kịch bản chiến đấu: evaluate + execute (mock).
    /// </summary>
    public class BattleScriptRuntimeService
    {
        public const string LogTag = "BattleScriptRuntime";

        private BattleScriptService _inner;

        public int Count => _inner != null ? _inner.Count : 0;

        public BattleScriptRuntimeService() : this(null) { }
        public BattleScriptRuntimeService(BattleScriptService inner) { _inner = inner; }

        public void AttachInner(BattleScriptService inner)
        {
            _inner = inner ?? new BattleScriptService();
            SubsystemLog.Info(LogTag, $"BattleScriptRuntime attached: {Count} script");
        }

        public PcBattleScriptEntry GetScript(int scriptId)
            => _inner != null ? _inner.GetScript(scriptId) : null;

        public IReadOnlyList<PcBattleScriptEntry> GetByType(int type)
        {
            // type ở đây = scriptName hash hoặc 0 = tất cả; ánh xạ từ trigger type
            return GetByTrigger(type);
        }

        public IReadOnlyList<PcBattleScriptEntry> GetByTrigger(int trigger)
        {
            if (_inner == null) return System.Array.Empty<PcBattleScriptEntry>();
            var src = _inner.GetScriptsByTrigger(trigger);
            var list = new List<PcBattleScriptEntry>(src);
            return list;
        }

        public IReadOnlyList<PcBattleScriptEntry> All
            => _inner != null
                ? (IReadOnlyList<PcBattleScriptEntry>)new List<PcBattleScriptEntry>(_inner.GetAllScripts())
                : (IReadOnlyList<PcBattleScriptEntry>)System.Array.Empty<PcBattleScriptEntry>();

        /// <summary>
        /// Đánh giá điều kiện kích hoạt script dựa trên context.
        /// Trả về false nếu script không tồn tại, true nếu các check cơ bản vượt qua.
        /// </summary>
        public bool EvaluateCondition(int scriptId, BattleContext ctx)
        {
            var s = GetScript(scriptId);
            if (s == null) return false;
            if (ctx == null) return false;
            // Điều kiện cơ bản: mapId trùng (hoặc 0 = toàn cục)
            if (s.mapId > 0 && ctx.currentMapId > 0 && s.mapId != ctx.currentMapId) return false;
            // Trigger type matching thời gian
            switch (s.triggerType)
            {
                case 0: // start
                    return ctx.elapsedSec >= 0;
                case 1: // end
                    return ctx.elapsedSec >= 60; // mặc định 1 phút
                case 2: // kill_boss
                    return ctx.currentNpcId == s.npcId && ctx.playerKills > 0;
                case 3: // death
                    return ctx.playerDeaths > 0;
            }
            return true;
        }

        /// <summary>
        /// Thực thi kịch bản (mock — không có side-effect thật ngoài log).
        /// Trả về 0 = ok, 1 = fail (điều kiện sai), 2 = interrupt.
        /// </summary>
        public int ExecuteScript(int scriptId, BattleContext ctx)
        {
            var s = GetScript(scriptId);
            if (s == null) return 1;
            if (ctx == null) return 1;
            if (!EvaluateCondition(scriptId, ctx)) return 1;
            try
            {
                SubsystemLog.Info(LogTag, $"Execute script {s.scriptId} ({s.scriptName}) — map={s.mapId} npc={s.npcId} reward={s.rewardId}x{s.rewardCount} score+{s.scoreReward}");
                return 0;
            }
            catch (System.Exception ex)
            {
                SubsystemLog.Warn(LogTag, $"Execute script {scriptId} lỗi: {ex.Message}");
                return 2;
            }
        }

        public static BattleScriptRuntimeService LoadFromStreamingAssets()
        {
            var inner = BattleScriptService.LoadFromStreamingAssets();
            return new BattleScriptRuntimeService(inner);
        }
    }
}
