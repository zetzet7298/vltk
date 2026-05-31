using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Resources
{
    /// <summary>
    /// Serves pre-converted files from <see cref="Application.streamingAssetsPath"/>.
    /// <para>
    /// This is the <b>runtime gameplay provider</b> (AC4). It reads only pre-extracted
    /// and pre-converted files; it never parses raw PAK archives.
    /// New PAK providers can be registered separately for the converter/tool pipeline
    /// without touching this class or any runtime code.
    /// </para>
    /// </summary>
    public class StreamingAssetsResourceProvider : IResourceProvider
    {
        private readonly string _subDir;
        private readonly string _rootPath;
        private readonly HashSet<string> _knownFiles = new();
        private bool _indexed;

        /// <param name="subDir">
        /// Optional subdirectory relative to <see cref="Application.streamingAssetsPath"/>.
        /// Pass an empty string (default) to use the root of StreamingAssets.
        /// </param>
        public StreamingAssetsResourceProvider(string subDir = "")
        {
            _subDir = subDir ?? string.Empty;
            _rootPath = string.IsNullOrEmpty(_subDir)
                ? Application.streamingAssetsPath
                : Path.Combine(Application.streamingAssetsPath, _subDir);
        }

        /// <inheritdoc/>
        public string ProviderId => $"StreamingAssets:{_subDir}";

        /// <inheritdoc/>
        public bool Contains(string resourcePath)
        {
            EnsureIndexed();
            return _knownFiles.Contains(Normalize(resourcePath));
        }

        /// <inheritdoc/>
        public ResourceLookupResult Load(string resourcePath)
        {
            var fullPath = Path.Combine(_rootPath, resourcePath);
            if (File.Exists(fullPath))
                return ResourceLookupResult.Found(File.ReadAllBytes(fullPath), ProviderId);

            // Case-insensitive fallback (important on case-sensitive filesystems)
            EnsureIndexed();
            var normalized = Normalize(resourcePath);
            foreach (var known in _knownFiles)
            {
                if (string.Equals(known, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return ResourceLookupResult.Found(
                        File.ReadAllBytes(Path.Combine(_rootPath, known)),
                        ProviderId);
                }
            }

            return ResourceLookupResult.Missing(resourcePath);
        }

        // ── Indexing ──────────────────────────────────────────────────────────

        private void EnsureIndexed()
        {
            if (_indexed) return;
            _indexed = true;

            try
            {
                if (!Directory.Exists(_rootPath)) return;

                var files = Directory.GetFiles(_rootPath, "*", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    var rel = f.Substring(_rootPath.Length).TrimStart(Path.DirectorySeparatorChar, '/');
                    _knownFiles.Add(Normalize(rel));
                }

                SubsystemLog.Info("ResourceLookup",
                    $"StreamingAssetsResourceProvider indexed {files.Length} files from '{_rootPath}'");
            }
            catch (Exception ex)
            {
                SubsystemLog.Error("ResourceLookup",
                    $"StreamingAssetsResourceProvider index error: {ex.Message}");
            }
        }

        private static string Normalize(string path)
            => path.Replace('\\', '/').ToLowerInvariant();
    }
}
