// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.23 Music Service (Nhạc nền runtime)
// Wraps PcMusicRegistry. PC source: settings/music/musicset.txt.
// Vietnamese: "Nhạc Nền", "Thành Thị", "Đồng Hoang", "Chiến Đấu", "Hang Động", "Boss".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class MusicService
    {
        public const string LogTag = "Music";
        public const string DefaultStreamingDir = "Reference/PcMusic";

        private PcMusicRegistry _registry;

        public event Action OnMusicLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public MusicService() { }
        public MusicService(PcMusicRegistry registry) { AttachRegistry(registry); }

        public void AttachRegistry(PcMusicRegistry registry)
        {
            _registry = registry ?? new PcMusicRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} bản nhạc nền");
            OnMusicLoaded?.Invoke();
        }

        public PcMusicEntry GetTrack(int trackId)
            => _registry != null ? _registry.Get(trackId) : null;

        public IReadOnlyList<PcMusicEntry> GetByScene(int sceneType)
            => _registry != null
                ? _registry.GetByScene(sceneType)
                : (IReadOnlyList<PcMusicEntry>)Array.Empty<PcMusicEntry>();

        public IEnumerable<PcMusicEntry> GetAllTracks()
            => _registry != null ? _registry.All : (IEnumerable<PcMusicEntry>)Array.Empty<PcMusicEntry>();

        public static MusicService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new MusicService();
            if (Directory.Exists(dir))
            {
                var reg = PcMusicParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Music: directory không tồn tại {dir}");
                svc.OnMusicLoaded?.Invoke();
            }
            return svc;
        }
    }
}
