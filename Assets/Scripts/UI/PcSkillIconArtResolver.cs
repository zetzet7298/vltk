// -----------------------------------------------------------------------------
// VLTK Mobile — PC skill icon art resolver
// Resolves skillId -> PC SkillIcon UID decoded from Skills.txt (GB2312) and staged
// from canonical pak_unpacked data/spr/unknown via signed-byte FileNameHash.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.UI
{
    public static class PcSkillIconArtResolver
    {
        private const string ManifestRelativePath = "Sprites/SkillIconsPc/skill_icon_pc_manifest.json";
        private const string IconArtRelativeRoot = "UI/HUD/Art/PcSkillIcons";

        [Serializable]
        private sealed class Manifest
        {
            public ManifestRow[] rows;
        }

        [Serializable]
        private sealed class ManifestRow
        {
            public int skillId;
            public string uid;
            public bool found;
        }

        private static Dictionary<int, string> s_skillToUid;

        public static bool TryResolveSkillIconPng(int skillId, out string path)
        {
            path = null;
            if (skillId <= 0)
                return false;

            EnsureLoaded();
            if (s_skillToUid == null || !s_skillToUid.TryGetValue(skillId, out var uid) || string.IsNullOrEmpty(uid))
                return false;

            var root = Application.streamingAssetsPath ?? string.Empty;
            var artRoot = CombineStreamingPath(root, IconArtRelativeRoot);
            var animatedFrame = CombineStreamingPath(CombineStreamingPath(artRoot, uid), "frame_000.png");
            if (File.Exists(animatedFrame))
            {
                path = animatedFrame;
                return true;
            }

            var singleFrame = CombineStreamingPath(artRoot, uid + ".png");
            if (File.Exists(singleFrame))
            {
                path = singleFrame;
                return true;
            }

            return false;
        }

        public static bool IsPcSkillIconName(string name, out int skillId)
        {
            skillId = 0;
            const string prefix = "cai_bang_skill_";
            if (string.IsNullOrEmpty(name) || !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
            return int.TryParse(name.Substring(prefix.Length), out skillId) && skillId > 0;
        }

        private static void EnsureLoaded()
        {
            if (s_skillToUid != null)
                return;

            s_skillToUid = new Dictionary<int, string>();
            var manifestPath = CombineStreamingPath(Application.streamingAssetsPath ?? string.Empty, ManifestRelativePath);
            if (!File.Exists(manifestPath))
                return;

            try
            {
                var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
                if (manifest?.rows == null)
                    return;

                foreach (var row in manifest.rows)
                {
                    if (row == null || row.skillId <= 0 || !row.found || string.IsNullOrEmpty(row.uid))
                        continue;
                    if (!s_skillToUid.ContainsKey(row.skillId))
                        s_skillToUid.Add(row.skillId, row.uid);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PC Skill Icon] Failed to load manifest {manifestPath}: {ex.Message}");
            }
        }

        private static string CombineStreamingPath(string left, string right)
        {
            if (string.IsNullOrEmpty(left)) return right ?? string.Empty;
            if (string.IsNullOrEmpty(right)) return left;
            return left.TrimEnd('/', '\\') + "/" + right.TrimStart('/', '\\');
        }
    }
}
