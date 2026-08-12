// -----------------------------------------------------------------------------
// VLTK.Survivor — Impact: buff config theo stack level.
// Parity dhcd: BuffConfig {BuffID, TimeType, ReplaceType} (BattleCore/BuffConfig.cs),
// BuffAttrConfig {StackNum, DurTime, AttrData[], DotDamageData, DotTickConfig}
// (BattleCore/BuffAttrConfig.cs), BuffConfigClient.FindAttr(stackNum)
// (BattleCore/BuffConfigClient.cs) — 1 buff = danh sách config theo stack.
// ReplaceType = own đơn giản (dhcd byte): Refresh (giữ stack, refresh duration)
// / Stack (stack lên, level config đổi). Numeric = own (O1).
// -----------------------------------------------------------------------------
using System.Collections.Generic;

namespace VLTK.Survivor
{
    public enum BuffTimeType
    {
        During = 0,  // BUFF_TIME_DURING
        Infinit = 1, // BUFF_TIME_INFINIT
    }

    public enum BuffReplaceType
    {
        Refresh = 0, // re-apply: refresh duration, stack giữ nguyên (dhcd ReplaceAdd mặc định)
        Stack = 1,   // stack lên tới MaxStack, level config theo stack
    }

    /// <summary>Config 1 stack level của buff — parity BuffAttrConfig.</summary>
    public sealed class BuffAttrConfig
    {
        public int StackNum = 1;                              // stack level yêu cầu (1-based)
        public float DurTime = 1f;                            // giây; <= 0 = infinite
        public ActorAttrImpact[] AttrData = System.Array.Empty<ActorAttrImpact>();
        public BuffStateID States = BuffStateID.None;
        public SkillAttrDamageData DotDamageData;             // null = không DOT
        public BuffDotTickConfig DotTick;                     // null = không DOT

        public bool HasDot => DotDamageData != null && DotTick != null;
    }

    /// <summary>1 buff = danh sách level config theo stack — parity BuffConfigClient.</summary>
    public sealed class BuffDef
    {
        public int BuffId;
        public BuffTimeType TimeType = BuffTimeType.During;
        public BuffReplaceType ReplaceType = BuffReplaceType.Refresh;
        public readonly List<BuffAttrConfig> Levels = new List<BuffAttrConfig>();

        /// <summary>Chọn config theo stackNum — parity FindAttr(stack): level cao nhất có StackNum &lt;= stack.</summary>
        public BuffAttrConfig FindAttr(int stackNum)
        {
            if (Levels.Count == 0) return null;
            BuffAttrConfig best = Levels[0];
            foreach (var l in Levels)
            {
                if (l.StackNum <= stackNum) best = l;
                else break;
            }
            return best;
        }

        public int MaxStack => Levels.Count > 0 ? Levels[Levels.Count - 1].StackNum : 0;
    }
}
