// -----------------------------------------------------------------------------
// VLTK Mobile — ST-7.x Sign-In runtime service
// Wraps PcSignInRegistry. PC source: settings/event/signin.txt.
// Quản lý điểm danh 30 ngày: phần thưởng, nhân đôi, chuỗi ngày.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Điểm Danh: lookup theo ngày, kiểm tra đã điểm danh hôm nay, nhân đôi.
    /// </summary>
    public class SignInService
    {
        public const string LogTag = "SignIn";
        public const string DefaultStreamingDir = "Reference/PcEvent";

        private PcSignInRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public SignInService() { }
        public SignInService(PcSignInRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcSignInRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "SignIn registry rỗng");
        }

        public PcSignInEntry GetReward(int day) => _reg != null ? _reg.Get(day) : null;

        public IReadOnlyList<PcSignInEntry> GetByTotalDays(int totalDays)
            => _reg != null ? _reg.GetByTotalDays(totalDays) : Array.Empty<PcSignInEntry>();

        public IReadOnlyList<PcSignInEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcSignInEntry>();

        public bool CanSignIn(int day, int lastSignInDay, int totalDays)
        {
            if (day <= 0) return false;
            if (day <= lastSignInDay) return false;
            var entry = GetReward(day);
            return entry != null;
        }

        public PcSignInEntry GetRewardForTotalDays(int totalDays)
        {
            if (_reg == null) return null;
            PcSignInEntry match = null;
            foreach (var e in _reg.All)
            {
                if (e.totalDaysSoFar > totalDays) continue;
                if (match == null || e.totalDaysSoFar > match.totalDaysSoFar) match = e;
            }
            return match;
        }

        public bool IsDouble(int day)
        {
            var entry = GetReward(day);
            return entry != null && entry.isDouble;
        }

        public static SignInService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcSignInParser.BuildRegistry(dir);
            return new SignInService(reg);
        }
    }
}
