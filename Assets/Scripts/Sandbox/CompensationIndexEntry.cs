// -----------------------------------------------------------------------------
// VLTK Mobile — Compensation index entry model.
// Source: StreamingAssets/Reference/PcCompensation/CompensationIndex.json
// Each entry maps a Lua script filename to its source path on the PC server.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Single entry from CompensationIndex.json.
    /// Maps a PC server Lua script to its relative path for compensation logic lookup.
    /// </summary>
    [Serializable]
    public sealed class CompensationIndexEntry
    {
        public string path;
        public string filename;
        public string rel_path;

        public bool IsValid()
            => !string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(rel_path);
    }
}
