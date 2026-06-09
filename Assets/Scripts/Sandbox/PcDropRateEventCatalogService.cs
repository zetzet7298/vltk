// -----------------------------------------------------------------------------
// VLTK Mobile — PC event drop-rate source catalog service.
// Data-only wrapper for settings/droprate/event provenance and row metadata.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class PcDropRateEventCatalogService
    {
        public const string LogTag = "PcDropRateEventCatalog";
        public const string DefaultStreamingDir = "Reference/PcDropRateEvent";
        public const string PcSourceRelativeRoot = PcDropRateEventSourceParser.PcSourceRelativeRoot;

        private readonly PcDropRateEventCatalog _catalog;
        public int FileCount => _catalog != null ? _catalog.FileCount : 0;
        public int DropRowCount => _catalog != null ? _catalog.DropRowCount : 0;
        public int DirectoryCount => _catalog != null ? _catalog.DirectoryCount : 0;
        public long TotalSizeBytes => _catalog != null ? _catalog.TotalSizeBytes : 0L;
        public IReadOnlyList<PcDropRateEventFileEntry> Files => _catalog != null ? _catalog.Files : Array.Empty<PcDropRateEventFileEntry>();

        public PcDropRateEventCatalogService(PcDropRateEventCatalog catalog)
        {
            _catalog = catalog ?? new PcDropRateEventCatalog();
        }

        public PcDropRateEventFileEntry GetFile(string relativePath) => _catalog.GetFile(relativePath);
        public IReadOnlyList<PcDropRateEventDropRow> GetRows(string relativePath) => _catalog.GetRows(relativePath);

        public static PcDropRateEventCatalogService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var catalog = PcDropRateEventSourceParser.BuildCatalog(dir);
            if (catalog.FileCount > 0)
                SubsystemLog.Info(LogTag, $"Loaded PC event droprate index: {catalog.FileCount} files, {catalog.DropRowCount} rows");
            else
                SubsystemLog.Warn(LogTag, $"PC event droprate index missing at {dir}");
            return new PcDropRateEventCatalogService(catalog);
        }
    }

    public sealed class PcDropRateEventCatalog
    {
        private readonly List<PcDropRateEventFileEntry> _files = new List<PcDropRateEventFileEntry>();
        private readonly List<PcDropRateEventDropRow> _rows = new List<PcDropRateEventDropRow>();
        private readonly Dictionary<string, PcDropRateEventFileEntry> _byPath = new Dictionary<string, PcDropRateEventFileEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcDropRateEventDropRow>> _rowsByPath = new Dictionary<string, List<PcDropRateEventDropRow>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public int FileCount => _files.Count;
        public int DropRowCount => _rows.Count;
        public int DirectoryCount => _directories.Count;
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcDropRateEventFileEntry> Files => _files;

        public void RegisterFile(PcDropRateEventFileEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.relativePath) || _byPath.ContainsKey(entry.relativePath)) return;
            _files.Add(entry);
            _byPath[entry.relativePath] = entry;
            _directories.Add(string.IsNullOrEmpty(entry.directory) ? "." : entry.directory);
            TotalSizeBytes += entry.sizeBytes;
        }

        public void RegisterDropRow(PcDropRateEventDropRow row)
        {
            if (row == null || string.IsNullOrEmpty(row.relativePath)) return;
            _rows.Add(row);
            if (!_rowsByPath.TryGetValue(row.relativePath, out var list))
            {
                list = new List<PcDropRateEventDropRow>();
                _rowsByPath[row.relativePath] = list;
            }
            list.Add(row);
        }

        public PcDropRateEventFileEntry GetFile(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _byPath.TryGetValue(relativePath, out var entry) ? entry : null;
        public IReadOnlyList<PcDropRateEventDropRow> GetRows(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _rowsByPath.TryGetValue(relativePath, out var list) ? (IReadOnlyList<PcDropRateEventDropRow>)list : Array.Empty<PcDropRateEventDropRow>();
    }

    [Serializable]
    public sealed class PcDropRateEventFileEntry
    {
        public int sourceIndex;
        public string relativePath;
        public string directory;
        public string fileName;
        public long sizeBytes;
        public string sha256;
        public int sectionCount;
        public int dropRowCount;
        public int mainCount;
        public int randRange;
        public int magicRate;
        public int moneyRate;
        public int moneyScale;
    }

    [Serializable]
    public sealed class PcDropRateEventDropRow
    {
        public int sourceIndex;
        public string relativePath;
        public int sectionIndex;
        public int genre;
        public int detail;
        public int particular;
        public int randRate;
        public int minItemLevel;
        public int maxItemLevel;
    }
}
