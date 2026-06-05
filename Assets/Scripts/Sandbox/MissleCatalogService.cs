// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX MissleCatalog Service (Đạn phép catalog runtime)
// Wraps PcMissleRegistry. Exposes lookup, filter by moveKind/followKind.
// Vietnamese: "Phi Đao", "Đạn Phép", "Đường Bay", "Theo Mục Tiêu".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý catalog đạn phép (skill missile). PC source:
    /// settings/missles.txt, missles1.txt, missletemplate.txt (480 rows).
    /// </summary>
    public class MissleCatalogService
    {
        public const string LogTag = "Missle";

        private PcMissleRegistry _registry;
        private readonly Dictionary<int, List<PcMissleEntry>> _byMoveKind = new();
        private readonly Dictionary<int, List<PcMissleEntry>> _byFollowKind = new();
        private bool _indexed;

        /// <summary>Sự kiện khi catalog load xong.</summary>
        public event Action OnMissleLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public MissleCatalogService() { }

        public MissleCatalogService(PcMissleRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcMissleRegistry registry)
        {
            _registry = registry ?? new PcMissleRegistry();
            _indexed = false;
            BuildIndex();
        }

        private void BuildIndex()
        {
            _byMoveKind.Clear();
            _byFollowKind.Clear();
            if (_registry == null) { _indexed = true; return; }
            foreach (var e in _registry.All)
            {
                if (e == null) continue;
                if (!_byMoveKind.TryGetValue(e.moveKind, out var list))
                {
                    list = new List<PcMissleEntry>();
                    _byMoveKind[e.moveKind] = list;
                }
                list.Add(e);
                if (!_byFollowKind.TryGetValue(e.followKind, out var fl))
                {
                    fl = new List<PcMissleEntry>();
                    _byFollowKind[e.followKind] = fl;
                }
                fl.Add(e);
            }
            _indexed = true;
        }

        // ── Query APIs ────────────────────────────────────────────────

        public PcMissleEntry GetMissle(int missleId)
            => _registry != null ? _registry.Get(missleId) : null;

        public IEnumerable<PcMissleEntry> GetAllMissles()
            => _registry != null ? _registry.All : Array.Empty<PcMissleEntry>();

        public IReadOnlyList<PcMissleEntry> GetByMoveKind(int moveKind)
        {
            if (!_indexed) BuildIndex();
            return _byMoveKind.TryGetValue(moveKind, out var v)
                ? (IReadOnlyList<PcMissleEntry>)v
                : Array.Empty<PcMissleEntry>();
        }

        public IReadOnlyList<PcMissleEntry> GetByFollowKind(int followKind)
        {
            if (!_indexed) BuildIndex();
            return _byFollowKind.TryGetValue(followKind, out var v)
                ? (IReadOnlyList<PcMissleEntry>)v
                : Array.Empty<PcMissleEntry>();
        }

        public int GetMoveKindCount(int moveKind) => GetByMoveKind(moveKind).Count;
        public int GetFollowKindCount(int followKind) => GetByFollowKind(followKind).Count;

        // ── Loading ───────────────────────────────────────────────────

        public static MissleCatalogService LoadFromStreamingAssets()
        {
            var svc = new MissleCatalogService();
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcAttrib");
            if (Directory.Exists(dir))
            {
                var reg = PcMissleParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
                SubsystemLog.Info(LogTag, $"MissleCatalogService loaded {reg.Count} đạn phép từ {dir}");
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"MissleCatalogService: directory không tồn tại {dir}");
            }
            svc.OnMissleLoaded?.Invoke();
            return svc;
        }
    }
}
