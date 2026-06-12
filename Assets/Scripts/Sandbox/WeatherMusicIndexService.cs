// -----------------------------------------------------------------------------
// VLTK Mobile — PC weather/music source index service.
// Data-only catalog for settings/weather and settings/music; no audio/weather runtime.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class WeatherMusicIndexService
    {
        public const string DefaultStreamingDir = "Reference/PcWeatherMusic";
        public const string NoRuntimeClaim = "Index only: PC weather/music files, counts, hashes; no runtime playback/weather semantics.";

        private readonly List<PcWeatherMusicEntry> _all = new List<PcWeatherMusicEntry>();
        private readonly Dictionary<string, PcWeatherMusicEntry> _byKey = new Dictionary<string, PcWeatherMusicEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PcWeatherMusicEntry> _byFile = new Dictionary<string, PcWeatherMusicEntry>(StringComparer.OrdinalIgnoreCase);

public WeatherMusicIndexService() : this(null) { }
                public WeatherMusicIndexService(IEnumerable<PcWeatherMusicEntry> rows)
        {
            if (rows == null) return;
            foreach (var row in rows)
            {
                if (row == null || string.IsNullOrEmpty(row.key)) continue;
                _all.Add(row);
                _byKey[row.key] = row;
                if (!string.IsNullOrEmpty(row.fileName)) _byFile[row.fileName] = row;
                if (row.clientServerByteIdentical) ClientServerIdenticalCount++;
                TotalBytes += row.bytes;
                TotalDataRows += row.dataRowCount;
            }
        }

        public int Count => _all.Count;
        public int ClientServerIdenticalCount { get; private set; }
        public int TotalBytes { get; private set; }
        public int TotalDataRows { get; private set; }
        public IReadOnlyList<PcWeatherMusicEntry> All => _all;

        public PcWeatherMusicEntry GetByKey(string key)
            => key != null && _byKey.TryGetValue(key, out var row) ? row : null;

        public PcWeatherMusicEntry GetByFileName(string fileName)
            => fileName != null && _byFile.TryGetValue(fileName, out var row) ? row : null;

        public static WeatherMusicIndexService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            return LoadFromDirectory(dir);
        }

        public static WeatherMusicIndexService LoadFromDirectory(string dir)
        {
            var rows = PcWeatherMusicParser.ParseFile(Path.Combine(dir ?? string.Empty, PcWeatherMusicParser.SourceFileName));
            return new WeatherMusicIndexService(rows);
        }
    }
}
