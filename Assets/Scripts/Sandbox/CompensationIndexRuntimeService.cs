// -----------------------------------------------------------------------------
// VLTK Mobile — Compensation index runtime service.
// Loads CompensationIndex.json from StreamingAssets/Reference/PcCompensation/,
// builds lookup tables by filename and rel_path, and serves queries for the
// compensation pipeline.
//
// PC source of truth:
//   - vng_event/denbu_baotri_5server/main.lua
//   - vng_event/denbutrongkhaihoan/main.lua
//   - vng_event/denbu_congthanh/congthanh.lua + head.lua
//   - activitysys/config/37/{registe,head,variables,extend,config}.lua
//   Total: 9 indexed files
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Runtime service that loads and serves the CompensationIndex.json
    /// which catalogs all PC compensation Lua scripts available for porting.
    /// Provides lookup by filename, rel_path, and enumeration of all entries.
    /// </summary>
    public sealed class CompensationIndexRuntimeService
    {
        public const string LogTag = "CompensationIndex";
        public const string DefaultIndexPath = "Reference/PcCompensation/CompensationIndex.json";

        private readonly List<CompensationIndexEntry> _entries = new();
        private readonly Dictionary<string, CompensationIndexEntry> _byFilename = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CompensationIndexEntry> _byRelPath = new(StringComparer.OrdinalIgnoreCase);
        private ICompensationHost _host;

        /// <summary>Total number of indexed compensation files.</summary>
        public int Count => _entries.Count;

        /// <summary>True after a successful LoadFromStreamingAssets or LoadFromJson call.</summary>
        public bool IsLoaded { get; private set; }

        /// <summary>All indexed entries in insertion order.</summary>
        public IReadOnlyList<CompensationIndexEntry> AllEntries => _entries;

        public CompensationIndexRuntimeService() : this(null) { }
        public CompensationIndexRuntimeService(ICompensationHost host) { _host = host; }

        public void AttachHost(ICompensationHost host) { _host = host; }

        /// <summary>
        /// Load from StreamingAssets using Unity's streaming path.
        /// </summary>
        public static CompensationIndexRuntimeService LoadFromStreamingAssets(string indexPath = null)
        {
            string fullPath = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(indexPath) ? DefaultIndexPath : indexPath);

            var service = new CompensationIndexRuntimeService();
            service.LoadFromPath(fullPath);
            return service;
        }

        /// <summary>
        /// Load from a raw JSON string (for testing or embedded data).
        /// </summary>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                SubsystemLog.Warn(LogTag, "Cannot load from empty JSON");
                _host?.OnLoadFailed("<json>", "empty json");
                return;
            }

            _host?.OnLoadStart("<json>");
            CompensationIndexList entries = null;
            try
            {
                entries = JsonUtility.FromJson<CompensationIndexList>(json);
            }
            catch (System.ArgumentException)
            {
                // JsonUtility throws on raw arrays — fall through to wrapped parse
            }
            if (entries?.items == null)
            {
                // JsonUtility wraps arrays poorly — try array parse via wrapper
                // The file is a raw JSON array, so we wrap it
                var wrapped = "{\"items\":" + json + "}";
                entries = JsonUtility.FromJson<CompensationIndexList>(wrapped);
            }

            if (entries?.items == null || entries.items.Length == 0)
            {
                SubsystemLog.Warn(LogTag, "Parsed 0 entries from CompensationIndex JSON");
                _host?.OnLoadFailed("<json>", "0 entries");
                return;
            }

            BuildIndex(entries.items);
            IsLoaded = true;
            _host?.OnLoadComplete(_entries.Count, _byFilename.Count, _byRelPath.Count);
            _host?.ShowCompensationList(_entries.Count, _entries.Count);
            _host?.PlayCompensationSFX("load");
            SubsystemLog.Info(LogTag, $"Loaded {_entries.Count} compensation index entries");
        }

        /// <summary>
        /// Load from a filesystem path (editor/test use).
        /// </summary>
        public void LoadFromPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                SubsystemLog.Warn(LogTag, $"Index file not found: {fullPath}");
                _host?.OnLoadFailed(fullPath ?? "<null>", "file not found");
                return;
            }

            _host?.OnLoadStart(fullPath);
            string json = File.ReadAllText(fullPath);
            LoadFromJson(json);
        }

        /// <summary>
        /// Lookup by exact filename (e.g. "main.lua", "config.lua").
        /// Returns null if not found.
        /// </summary>
        public CompensationIndexEntry GetByFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return null;
            _byFilename.TryGetValue(filename, out var entry);
            _host?.OnQuery("filename", filename, entry != null, entry != null ? 1 : 0);
            _host?.SaveCompensationLog("filename", filename, entry != null ? 1 : 0);
            return entry;
        }

        /// <summary>
        /// Lookup by relative path (e.g. "vng_event/denbu_baotri_5server/main.lua").
        /// Returns null if not found.
        /// </summary>
        public CompensationIndexEntry GetByRelPath(string relPath)
        {
            if (string.IsNullOrEmpty(relPath)) return null;
            _byRelPath.TryGetValue(relPath, out var entry);
            _host?.OnQuery("relpath", relPath, entry != null, entry != null ? 1 : 0);
            _host?.SaveCompensationLog("relpath", relPath, entry != null ? 1 : 0);
            return entry;
        }

        /// <summary>
        /// Get all entries matching a given filename (may return multiple if same filename
        /// appears in different directories).
        /// </summary>
        public IReadOnlyList<CompensationIndexEntry> GetAllByFilename(string filename)
        {
            var result = new List<CompensationIndexEntry>();
            if (string.IsNullOrEmpty(filename)) return result;
            foreach (var entry in _entries)
            {
                if (string.Equals(entry.filename, filename, StringComparison.OrdinalIgnoreCase))
                    result.Add(entry);
            }
            _host?.OnQuery("filenameAll", filename, result.Count > 0, result.Count);
            return result;
        }

        /// <summary>
        /// Count entries under a given directory prefix (e.g. "vng_event/").
        /// </summary>
        public int CountByDirectoryPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return 0;
            int count = 0;
            foreach (var entry in _entries)
            {
                if (entry.rel_path != null &&
                    entry.rel_path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Get all unique directories represented in the index.
        /// </summary>
        public HashSet<string> GetUniqueDirectories()
        {
            var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.rel_path)) continue;
                int lastSlash = entry.rel_path.LastIndexOf('/');
                if (lastSlash > 0)
                    dirs.Add(entry.rel_path.Substring(0, lastSlash));
            }
            return dirs;
        }

        /// <summary>
        /// Resolve a CompensationService for the full pipeline:
        /// loads the .txt data files from the same directory as the index.
        /// </summary>
        public CompensationService BuildCompensationService()
        {
            var compService = CompensationService.LoadFromStreamingAssets(
                CompensationService.DefaultStreamingDir);
            return compService;
        }

        private void BuildIndex(CompensationIndexEntry[] items)
        {
            _entries.Clear();
            _byFilename.Clear();
            _byRelPath.Clear();

            for (int i = 0; i < items.Length; i++)
            {
                var entry = items[i];
                if (entry == null || !entry.IsValid()) continue;

                _entries.Add(entry);

                // First-seen wins for single-lookup dicts
                if (!_byFilename.ContainsKey(entry.filename))
                    _byFilename[entry.filename] = entry;

                if (!_byRelPath.ContainsKey(entry.rel_path))
                    _byRelPath[entry.rel_path] = entry;
            }
        }

        /// <summary>
        /// JSON wrapper for deserializing an array of CompensationIndexEntry.
        /// JsonUtility cannot deserialize top-level arrays, so we wrap them.
        /// </summary>
        [Serializable]
        private sealed class CompensationIndexList
        {
            public CompensationIndexEntry[] items;
        }
    }
}
