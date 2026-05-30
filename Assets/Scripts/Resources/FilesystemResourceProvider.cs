using System;
using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Resources
{
    public class FilesystemResourceProvider : IResourceProvider
    {
        private readonly string _rootPath;
        private readonly HashSet<string> _knownFiles = new();
        private bool _indexed;

        public string ProviderId => $"FS:{Path.GetFileName(_rootPath)}";

        public FilesystemResourceProvider(string rootPath)
        {
            _rootPath = rootPath;
        }

        public bool Contains(string resourcePath)
        {
            EnsureIndexed();
            return _knownFiles.Contains(Normalize(resourcePath));
        }

        public ResourceLookupResult Load(string resourcePath)
        {
            var fullPath = Path.Combine(_rootPath, resourcePath);
            if (File.Exists(fullPath))
                return ResourceLookupResult.Found(File.ReadAllBytes(fullPath), ProviderId);

            // Try case-insensitive match
            EnsureIndexed();
            foreach (var known in _knownFiles)
            {
                if (string.Equals(known, Normalize(resourcePath), StringComparison.OrdinalIgnoreCase))
                {
                    return ResourceLookupResult.Found(
                        File.ReadAllBytes(Path.Combine(_rootPath, known)),
                        ProviderId);
                }
            }

            return ResourceLookupResult.Missing(resourcePath);
        }

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

                SubsystemLog.Info("ResourceLookup", $"Indexed {files.Length} files from {_rootPath}");
            }
            catch (Exception ex)
            {
                SubsystemLog.Error("ResourceLookup", $"Index error: {ex.Message}");
            }
        }

        private static string Normalize(string path)
            => path.Replace('\\', '/').ToLowerInvariant();
    }
}
