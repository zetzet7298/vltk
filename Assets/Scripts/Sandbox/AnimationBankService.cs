// -----------------------------------------------------------------------------
// VLTK Mobile — ST-12.x Animation Bank runtime service
// Wraps PcAnimationBankRegistry. PC source: settings/animation/animation.txt.
// Quản lý animation sprite: lookup theo ID, tên, hướng; frame count, delay.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Animation Bank: tra cứu animation theo ID/tên/hướng, thông tin frame.
    /// </summary>
    public class AnimationBankService
    {
        public const string LogTag = "AnimationBank";
        public const string DefaultStreamingDir = "Reference/PcAnimation";

        private PcAnimationBankRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public AnimationBankService() { }
        public AnimationBankService(PcAnimationBankRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcAnimationBankRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "AnimationBank registry rỗng");
        }

        public PcAnimationBankEntry GetAnimation(int id) => _reg != null ? _reg.Get(id) : null;

        public PcAnimationBankEntry GetByName(string name) => _reg != null ? _reg.GetByName(name) : null;

        public IReadOnlyList<PcAnimationBankEntry> GetByDirection(int direction)
            => _reg != null ? _reg.GetByDirection(direction) : Array.Empty<PcAnimationBankEntry>();

        public IReadOnlyList<PcAnimationBankEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcAnimationBankEntry>();

        public int GetFrameDelayMs(int animId)
        {
            var entry = GetAnimation(animId);
            return entry != null ? entry.frameDelayMs : 0;
        }

        public int GetFrameCount(int animId)
        {
            var entry = GetAnimation(animId);
            return entry != null ? entry.frameCount : 0;
        }

        public static AnimationBankService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcAnimationBankParser.BuildRegistry(dir);
            return new AnimationBankService(reg);
        }
    }
}
