// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC Region_S spawn marker for NPC/object entries whose exact visual asset is not staged yet.
    /// Keeps authoritative coordinates available without showing placeholders.
    /// </summary>
    public sealed class BaLangPcSpawnMarker : MonoBehaviour
    {
        public int templateId;
        public string rawName;
        public string vietnameseName;
        public int mpsX;
        public int mpsY;
        public string script;
        public string missingVisual;
    }
}
