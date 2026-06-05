// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX EventBonus Service (Phần thưởng sự kiện runtime)
// Wraps PcEventRegistry. Track claimed reward state. PC source: settings/event/*
// Vietnamese: "Sự Kiện", "Phần Thưởng", "Đã Nhận", "Quà Tặng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý phần thưởng sự kiện (Tết, Trung Thu, Rương Thần Bí, v.v.).
    /// PC source: settings/event/{chinesenewyear,juanzhouhecheng,shenmibaoxiang,
    /// wangwanglibao,riddle,zhongqiuhuodong,other}/*.txt
    /// </summary>
    public class EventBonusService
    {
        public const string LogTag = "Event";

        private PcEventRegistry _registry;
        private readonly HashSet<string> _claimed = new();
        private readonly Dictionary<string, List<PcEventEntry>> _byEvent = new();
        private bool _indexed;

        /// <summary>Sự kiện khi người chơi nhận thưởng một entry.</summary>
        public event Action<string, string, int> OnBonusClaimed; // (eventName, fileName, lineIndex)

        public int Count => _registry != null ? _registry.Count : 0;
        public int ClaimedCount => _claimed.Count;

        public EventBonusService() { }

        public EventBonusService(PcEventRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcEventRegistry registry)
        {
            _registry = registry ?? new PcEventRegistry();
            _indexed = false;
            BuildIndex();
        }

        private void BuildIndex()
        {
            _byEvent.Clear();
            _claimed.Clear();
            if (_registry == null) { _indexed = true; return; }
            foreach (var e in _registry.All)
            {
                if (e == null || string.IsNullOrEmpty(e.eventName)) continue;
                if (!_byEvent.TryGetValue(e.eventName, out var list))
                {
                    list = new List<PcEventEntry>();
                    _byEvent[e.eventName] = list;
                }
                list.Add(e);
            }
            _indexed = true;
        }

        public void ResetClaims()
        {
            _claimed.Clear();
            SubsystemLog.Info(LogTag, "Đã reset toàn bộ trạng thái nhận thưởng sự kiện");
        }

        // ── Query APIs ────────────────────────────────────────────────

        public IReadOnlyList<PcEventEntry> GetEventBonuses(string eventName)
        {
            if (!_indexed) BuildIndex();
            return _byEvent.TryGetValue(eventName ?? string.Empty, out var v)
                ? (IReadOnlyList<PcEventEntry>)v
                : Array.Empty<PcEventEntry>();
        }

        public IEnumerable<string> GetAllEvents()
        {
            if (_registry == null) yield break;
            foreach (var e in _registry.All)
                if (!string.IsNullOrEmpty(e.eventName)) yield return e.eventName;
        }

        public IReadOnlyList<PcEventEntry> GetEntriesForFile(string eventName, string fileName)
        {
            var all = GetEventBonuses(eventName);
            var result = new List<PcEventEntry>();
            foreach (var e in all)
                if (string.Equals(e.fileName, fileName, StringComparison.OrdinalIgnoreCase))
                    result.Add(e);
            return result;
        }

        public bool IsClaimed(string eventName, string fileName, int lineIndex)
            => _claimed.Contains(MakeKey(eventName, fileName, lineIndex));

        public bool MarkClaimed(string eventName, string fileName, int lineIndex)
        {
            string key = MakeKey(eventName, fileName, lineIndex);
            if (_claimed.Contains(key)) return false;
            _claimed.Add(key);
            SubsystemLog.Info(LogTag, $"Đã nhận thưởng {eventName}/{fileName}#{lineIndex}");
            OnBonusClaimed?.Invoke(eventName, fileName, lineIndex);
            return true;
        }

        private static string MakeKey(string eventName, string fileName, int lineIndex)
            => $"{(eventName ?? string.Empty)}|{(fileName ?? string.Empty)}|{lineIndex}";

        // ── Loading ───────────────────────────────────────────────────

        public static EventBonusService LoadFromStreamingAssets()
        {
            var svc = new EventBonusService();
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcEvent");
            if (Directory.Exists(dir))
            {
                var reg = PcEventBonusParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
                SubsystemLog.Info(LogTag, $"EventBonusService loaded {reg.Count} phần thưởng từ {dir}");
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"EventBonusService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
