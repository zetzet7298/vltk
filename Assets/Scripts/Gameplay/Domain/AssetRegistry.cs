using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public interface IAssetRegistry
    {
        AssetRegistryEntry Resolve(string sourcePath);
        AssetRegistryEntry Resolve(int uid);
        AssetRegistryEntry Resolve(SourceAssetId sourceId);
        void Register(AssetRegistryEntry entry);
        IReadOnlyList<AssetRegistryEntry> GetAll();
        IReadOnlyList<AssetRegistryEntry> GetByStatus(AssetStatus status);
        IReadOnlyList<AssetRegistryEntry> GetByMapId(int mapId);
        ValidationResult Validate();
    }

    public class ValidationResult
    {
        public readonly List<string> Errors = new();
        public readonly List<string> Warnings = new();
        public bool IsOk => Errors.Count == 0;
    }

    public class AssetRegistry : IAssetRegistry
    {
        private readonly Dictionary<string, AssetRegistryEntry> _byPath = new();
        private readonly Dictionary<int, AssetRegistryEntry> _byUid = new();
        private readonly List<AssetRegistryEntry> _all = new();

        public void Register(AssetRegistryEntry entry)
        {
            if (entry?.sourceId == null)
            {
                SubsystemLog.Error("Registry", "Cannot register entry with null sourceId");
                return;
            }

            _all.Add(entry);

            var key = entry.sourceId.ToKey();
            if (!string.IsNullOrEmpty(key))
            {
                if (_byPath.ContainsKey(key))
                    SubsystemLog.Warn("Registry", $"Duplicate source path: {key}");
                else
                    _byPath[key] = entry;
            }

            if (entry.sourceId.uid != 0)
            {
                if (_byUid.ContainsKey(entry.sourceId.uid))
                    SubsystemLog.Warn("Registry", $"Duplicate uid: {entry.sourceId.uid}");
                else
                    _byUid[entry.sourceId.uid] = entry;
            }
        }

        public AssetRegistryEntry Resolve(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath)) return null;
            _byPath.TryGetValue(sourcePath, out var entry);
            return entry;
        }

        public AssetRegistryEntry Resolve(int uid)
        {
            _byUid.TryGetValue(uid, out var entry);
            return entry;
        }

        public AssetRegistryEntry Resolve(SourceAssetId sourceId)
        {
            if (sourceId == null) return null;
            var entry = Resolve(sourceId.sourcePath);
            if (entry != null) return entry;
            return Resolve(sourceId.uid);
        }

        public IReadOnlyList<AssetRegistryEntry> GetAll() => _all.AsReadOnly();

        public IReadOnlyList<AssetRegistryEntry> GetByStatus(AssetStatus status)
        {
            var result = new List<AssetRegistryEntry>();
            foreach (var e in _all)
                if (e.status == status)
                    result.Add(e);
            return result.AsReadOnly();
        }

        public IReadOnlyList<AssetRegistryEntry> GetByMapId(int mapId)
        {
            var result = new List<AssetRegistryEntry>();
            foreach (var e in _all)
                if (e.sourceId != null && e.sourceId.uid == mapId)
                    result.Add(e);
            return result.AsReadOnly();
        }

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            var seen = new HashSet<string>();

            foreach (var entry in _all)
            {
                if (entry.sourceId == null)
                {
                    result.Errors.Add("Entry with null sourceId");
                    continue;
                }

                var key = entry.sourceId.ToKey();
                if (seen.Contains(key))
                    result.Warnings.Add($"Duplicate key: {key}");
                else
                    seen.Add(key);

                if (entry.status == AssetStatus.Missing)
                    result.Warnings.Add($"Missing asset: {key}");
                else if (entry.status == AssetStatus.Invalid)
                    result.Errors.Add($"Invalid asset: {key}");
            }

            return result;
        }
    }
}
