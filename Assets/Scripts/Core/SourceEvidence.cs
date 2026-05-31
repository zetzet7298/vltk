using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Core
{
    [Serializable]
    public class SourceEvidenceRecord
    {
        public string claim;
        public string pcSourceAnchor;
        public string symbolOrFile;
        public DiscoveryTool tool;
        public string queryUsed;
        public string resolvedValue;
        public long timestamp;
        public string notes;

        public override string ToString()
            => $"[{tool}] {claim} => {pcSourceAnchor} ({symbolOrFile})";
    }

    public static class SourceEvidence
    {
        private static readonly List<SourceEvidenceRecord> _records = new();
        private static bool _dirty;

        public static IReadOnlyList<SourceEvidenceRecord> Records => _records.AsReadOnly();
        public static bool IsDirty => _dirty;

        public static void Record(
            string claim,
            string pcSourceAnchor,
            string symbolOrFile = "",
            DiscoveryTool tool = DiscoveryTool.Manual,
            string queryUsed = "",
            string resolvedValue = "",
            string notes = "")
        {
            _records.Add(new SourceEvidenceRecord
            {
                claim = claim,
                pcSourceAnchor = pcSourceAnchor,
                symbolOrFile = symbolOrFile,
                tool = tool,
                queryUsed = queryUsed,
                resolvedValue = resolvedValue,
                timestamp = DateTime.Now.Ticks,
                notes = notes,
            });
            _dirty = true;
        }

        public static int RecordCount => _records.Count;

        public static List<SourceEvidenceRecord> FindByTool(DiscoveryTool tool)
        {
            var result = new List<SourceEvidenceRecord>();
            foreach (var r in _records)
                if (r.tool == tool)
                    result.Add(r);
            return result;
        }

        public static List<SourceEvidenceRecord> FindByClaim(string partial)
        {
            var result = new List<SourceEvidenceRecord>();
            foreach (var r in _records)
                if (r.claim != null && r.claim.Contains(partial))
                    result.Add(r);
            return result;
        }

        public static void SaveToJSON(string path)
        {
            try
            {
                var json = JsonUtility.ToJson(new Wrapper { items = _records.ToArray() }, true);
                File.WriteAllText(path, json);
                _dirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SourceEvidence] Save failed: {ex.Message}");
            }
        }

        public static void LoadFromJSON(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<Wrapper>(json);
                if (wrapper?.items != null)
                {
                    _records.Clear();
                    _records.AddRange(wrapper.items);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SourceEvidence] Load failed: {ex.Message}");
            }
        }

        [Serializable]
        private class Wrapper { public SourceEvidenceRecord[] items; }
    }
}
