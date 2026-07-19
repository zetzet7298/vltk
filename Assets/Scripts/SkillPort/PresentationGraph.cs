using System;
using System.Collections.Generic;

namespace VLTK.SkillPort
{
    public enum PlayerVisualGender
    {
        Any = 0,
        Male = 1,
        Female = 2,
    }

    public enum MountSelector
    {
        Any = 0,
        Unmounted = 1,
        Mounted = 2,
    }

    public enum WeaponVisibility
    {
        Any = 0,
        Equipped = 1,
        Empty = 2,
        Hidden = 3,
    }

    [Serializable]
    public sealed class PlayerVisualTuple
    {
        public PlayerVisualGender gender;
        public bool mounted;
        public int mountVisualId;
        public WeaponVisibility weaponVisibility;
        public int weaponVisualId;

        public bool IsCanonical()
        {
            if (gender != PlayerVisualGender.Male && gender != PlayerVisualGender.Female)
                return false;
            if (mounted != (mountVisualId > 0))
                return false;
            if (weaponVisibility == WeaponVisibility.Any)
                return false;
            if (weaponVisibility == WeaponVisibility.Equipped && weaponVisualId <= 0)
                return false;
            if ((weaponVisibility == WeaponVisibility.Empty || weaponVisibility == WeaponVisibility.Hidden) &&
                weaponVisualId != 0)
                return false;
            return true;
        }
    }

    [Serializable]
    public sealed class PresentationCue
    {
        public string cueId;
        public CombatLifecycleKind lifecycleKind;
        public SkillTriggerPhase triggerPhase;
        public int frameOffset;
        public int durationFrames;
        public int animationId;
        public int visualEffectId;
        public int missileContentId;
        public string audioCueId;
        public List<string> requiredAssetHashes = new List<string>();

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(cueId) || lifecycleKind == CombatLifecycleKind.Unspecified ||
                frameOffset < 0 || durationFrames < 0)
                return false;
            if (animationId <= 0 && visualEffectId <= 0 && missileContentId <= 0 &&
                string.IsNullOrEmpty(audioCueId))
                return false;
            if (requiredAssetHashes == null)
                return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string hash in requiredAssetHashes)
            {
                if (!ContentReleaseDigest.IsLowerHexSha256(hash) || !seen.Add(hash))
                    return false;
            }
            return true;
        }
    }

    [Serializable]
    public sealed class PresentationVariant
    {
        public string variantId;
        public PlayerVisualGender gender = PlayerVisualGender.Any;
        public MountSelector mount = MountSelector.Any;
        public int mountVisualId = -1;
        public WeaponVisibility weaponVisibility = WeaponVisibility.Any;
        public int weaponVisualId = -1;
        public List<PresentationCue> cues = new List<PresentationCue>();

        public bool Matches(PlayerVisualTuple tuple)
        {
            if (tuple == null || !tuple.IsCanonical())
                return false;
            if (gender != PlayerVisualGender.Any && gender != tuple.gender)
                return false;
            if (mount == MountSelector.Mounted && !tuple.mounted)
                return false;
            if (mount == MountSelector.Unmounted && tuple.mounted)
                return false;
            if (mountVisualId >= 0 && mountVisualId != tuple.mountVisualId)
                return false;
            if (weaponVisibility != WeaponVisibility.Any && weaponVisibility != tuple.weaponVisibility)
                return false;
            if (weaponVisualId >= 0 && weaponVisualId != tuple.weaponVisualId)
                return false;
            return true;
        }

        public int Specificity()
        {
            int score = 0;
            if (gender != PlayerVisualGender.Any) score++;
            if (mount != MountSelector.Any) score++;
            if (mountVisualId >= 0) score++;
            if (weaponVisibility != WeaponVisibility.Any) score++;
            if (weaponVisualId >= 0) score++;
            return score;
        }
    }

    [Serializable]
    public sealed class SkillPresentationGraph
    {
        public int skillId;
        public int canonicalFrameRate;
        public List<PresentationVariant> variants = new List<PresentationVariant>();

        public bool Validate(out string error)
        {
            if (skillId <= 0 || canonicalFrameRate <= 0 || variants == null || variants.Count == 0)
            {
                error = "graph header is invalid";
                return false;
            }

            var variantIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (PresentationVariant variant in variants)
            {
                if (variant == null || string.IsNullOrEmpty(variant.variantId) ||
                    !variantIds.Add(variant.variantId) || variant.cues == null || variant.cues.Count == 0)
                {
                    error = "variant is missing, duplicated, or empty";
                    return false;
                }

                var cueIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (PresentationCue cue in variant.cues)
                {
                    if (cue == null || !cue.IsValid() || !cueIds.Add(cue.cueId))
                    {
                        error = "cue is invalid or duplicated";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
    }

    public enum PresentationResolveFailure
    {
        None = 0,
        InvalidGraph = 1,
        InvalidTuple = 2,
        MissingVariant = 3,
        AmbiguousVariant = 4,
        MissingLifecycleCue = 5,
    }

    public readonly struct PresentationResolveResult
    {
        public readonly PresentationVariant variant;
        public readonly IReadOnlyList<PresentationCue> cues;
        public readonly PresentationResolveFailure failure;
        public readonly string detail;

        public bool success => failure == PresentationResolveFailure.None && variant != null;

        public PresentationResolveResult(
            PresentationVariant variant,
            IReadOnlyList<PresentationCue> cues,
            PresentationResolveFailure failure,
            string detail)
        {
            this.variant = variant;
            this.cues = cues;
            this.failure = failure;
            this.detail = detail;
        }
    }

    public static class PresentationGraphResolver
    {
        /// <summary>
        /// Selects the single most-specific canonical tuple. Equal-specificity
        /// matches fail closed instead of relying on list order.
        /// </summary>
        public static PresentationResolveResult Resolve(
            SkillPresentationGraph graph,
            PlayerVisualTuple tuple,
            CombatLifecycleKind lifecycleKind,
            SkillTriggerPhase triggerPhase)
        {
            if (graph == null)
            {
                return new PresentationResolveResult(
                    null, null, PresentationResolveFailure.InvalidGraph, "graph is null");
            }
            if (!graph.Validate(out string graphError))
            {
                return new PresentationResolveResult(
                    null, null, PresentationResolveFailure.InvalidGraph, graphError);
            }
            if (tuple == null || !tuple.IsCanonical())
            {
                return new PresentationResolveResult(
                    null, null, PresentationResolveFailure.InvalidTuple, "visual tuple is not canonical");
            }

            PresentationVariant selected = null;
            int selectedScore = -1;
            foreach (PresentationVariant variant in graph.variants)
            {
                if (!variant.Matches(tuple))
                    continue;
                int score = variant.Specificity();
                if (score > selectedScore)
                {
                    selected = variant;
                    selectedScore = score;
                }
                else if (score == selectedScore)
                {
                    return new PresentationResolveResult(
                        null,
                        null,
                        PresentationResolveFailure.AmbiguousVariant,
                        "multiple equally specific variants match the visual tuple");
                }
            }

            if (selected == null)
            {
                return new PresentationResolveResult(
                    null, null, PresentationResolveFailure.MissingVariant,
                    "no canonical presentation variant matches the visual tuple");
            }

            var cues = new List<PresentationCue>();
            foreach (PresentationCue cue in selected.cues)
            {
                if (cue.lifecycleKind != lifecycleKind)
                    continue;
                if (cue.triggerPhase != SkillTriggerPhase.Unspecified && cue.triggerPhase != triggerPhase)
                    continue;
                cues.Add(cue);
            }
            cues.Sort((a, b) =>
            {
                int byFrame = a.frameOffset.CompareTo(b.frameOffset);
                return byFrame != 0 ? byFrame : string.CompareOrdinal(a.cueId, b.cueId);
            });

            if (cues.Count == 0)
            {
                return new PresentationResolveResult(
                    selected, cues, PresentationResolveFailure.MissingLifecycleCue,
                    "variant has no cue for the authoritative lifecycle event");
            }

            return new PresentationResolveResult(selected, cues, PresentationResolveFailure.None, null);
        }
    }
}
