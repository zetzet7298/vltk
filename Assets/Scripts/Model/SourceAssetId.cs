using System;
using UnityEngine;

namespace VLTK.Model
{
    [Serializable]
    public enum ResourceKind
    {
        Unknown,
        Map,
        Region,
        Terrain,
        Sprite,
        Object,
        Npc,
        Trap,
        Config,
        Item,
        Skill,
        Lua,
        Audio,
    }

    [Serializable]
    public enum DiscoveryTool
    {
        Unknown,
        Semble,
        GitNexus,
        Vltktool,
        Manual,
        Runtime,
    }

    [Serializable]
    public class SourceAssetId
    {
        public string sourcePath;
        public string packageName;
        public int uid;
        public ResourceKind resourceKind;
        public string encoding;
        public DiscoveryTool discoveryTool;
        public string evidenceNote;

        public string ToKey()
        {
            if (!string.IsNullOrEmpty(sourcePath)) return sourcePath;
            return $"{packageName}:{uid}";
        }
    }
}
