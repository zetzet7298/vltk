using System;
using System.Collections.Generic;

namespace VLTK.SkillPort
{
    public enum ContentSelectionFailure
    {
        None = 0,
        InvalidActiveRelease = 1,
        ActiveReleaseNotInstalled = 2,
        ManifestHashMismatch = 3,
        ProjectionHashMismatch = 4,
    }

    [Serializable]
    public sealed class ContentReleaseDigest
    {
        public string releaseId;
        public string manifestSha256;
        public string projectionSha256;
        public int schemaVersion;

        public ContentReleaseDigest() { }

        public ContentReleaseDigest(
            string releaseId,
            string manifestSha256,
            string projectionSha256,
            int schemaVersion = 1)
        {
            this.releaseId = releaseId;
            this.manifestSha256 = manifestSha256;
            this.projectionSha256 = projectionSha256;
            this.schemaVersion = schemaVersion;
        }

        public bool IsCanonical()
        {
            return schemaVersion > 0 &&
                   IsCanonicalUuid(releaseId) &&
                   IsLowerHexSha256(manifestSha256) &&
                   IsLowerHexSha256(projectionSha256);
        }

        public bool SameRelease(ContentReleaseDigest other)
        {
            return other != null &&
                   string.Equals(releaseId, other.releaseId, StringComparison.Ordinal) &&
                   schemaVersion == other.schemaVersion;
        }

        public bool ExactMatch(ContentReleaseDigest other)
        {
            return SameRelease(other) &&
                   string.Equals(manifestSha256, other.manifestSha256, StringComparison.Ordinal) &&
                   string.Equals(projectionSha256, other.projectionSha256, StringComparison.Ordinal);
        }

        public static bool IsCanonicalUuid(string value)
        {
            if (string.IsNullOrEmpty(value) || !Guid.TryParseExact(value, "D", out Guid parsed))
                return false;
            return string.Equals(value, parsed.ToString("D"), StringComparison.Ordinal);
        }

        public static bool IsLowerHexSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    return false;
            }
            return true;
        }
    }

    public readonly struct ContentSelectionResult
    {
        public readonly ContentReleaseDigest selected;
        public readonly ContentSelectionFailure failure;
        public readonly string detail;

        public bool success => failure == ContentSelectionFailure.None && selected != null;

        public ContentSelectionResult(
            ContentReleaseDigest selected,
            ContentSelectionFailure failure,
            string detail)
        {
            this.selected = selected;
            this.failure = failure;
            this.detail = detail;
        }
    }

    public static class ContentReleaseSelector
    {
        /// <summary>
        /// Selects only a byte-for-byte compatible installed projection. Protocol
        /// N/N-1 compatibility never relaxes this content hash gate.
        /// </summary>
        public static ContentSelectionResult SelectExact(
            ContentReleaseDigest active,
            IEnumerable<ContentReleaseDigest> installed)
        {
            if (active == null || !active.IsCanonical())
            {
                return new ContentSelectionResult(
                    null,
                    ContentSelectionFailure.InvalidActiveRelease,
                    "active release digest is missing or non-canonical");
            }

            bool sawRelease = false;
            bool sawManifestMismatch = false;
            if (installed != null)
            {
                foreach (ContentReleaseDigest candidate in installed)
                {
                    if (candidate == null || !candidate.IsCanonical() || !candidate.SameRelease(active))
                        continue;

                    sawRelease = true;
                    if (!string.Equals(candidate.manifestSha256, active.manifestSha256, StringComparison.Ordinal))
                    {
                        sawManifestMismatch = true;
                        continue;
                    }

                    if (!string.Equals(candidate.projectionSha256, active.projectionSha256, StringComparison.Ordinal))
                    {
                        return new ContentSelectionResult(
                            null,
                            ContentSelectionFailure.ProjectionHashMismatch,
                            "client projection hash does not match the active release");
                    }

                    return new ContentSelectionResult(candidate, ContentSelectionFailure.None, null);
                }
            }

            if (sawManifestMismatch)
            {
                return new ContentSelectionResult(
                    null,
                    ContentSelectionFailure.ManifestHashMismatch,
                    "manifest hash does not match the active release");
            }

            return new ContentSelectionResult(
                null,
                ContentSelectionFailure.ActiveReleaseNotInstalled,
                sawRelease
                    ? "active release is installed but no compatible projection exists"
                    : "active release is not installed");
        }
    }

    public enum SkillAuthorityMode
    {
        Disabled = 0,
        LegacyActiveGoShadow = 1,
        GoActiveLegacyShadow = 2,
        GoOnly = 3,
    }

    public enum SkillPresentationMode
    {
        Disabled = 0,
        Legacy = 1,
        GraphV2Shadow = 2,
        GraphV2 = 3,
    }

    [Serializable]
    public sealed class SkillRuntimeMode
    {
        public int skillId;
        public string factionKey;
        public bool exposed;
        public SkillAuthorityMode authorityMode;
        public SkillPresentationMode presentationMode;

        public SkillRuntimeMode Clone()
        {
            return new SkillRuntimeMode
            {
                skillId = skillId,
                factionKey = factionKey,
                exposed = exposed,
                authorityMode = authorityMode,
                presentationMode = presentationMode,
            };
        }

        public static SkillRuntimeMode DisabledFor(int skillId, string factionKey)
        {
            return new SkillRuntimeMode
            {
                skillId = skillId,
                factionKey = factionKey,
                exposed = false,
                authorityMode = SkillAuthorityMode.Disabled,
                presentationMode = SkillPresentationMode.Disabled,
            };
        }
    }

    public sealed class RuntimePolicySnapshot
    {
        private readonly Dictionary<int, SkillRuntimeMode> _skills =
            new Dictionary<int, SkillRuntimeMode>();
        private readonly HashSet<string> _disabledFactions =
            new HashSet<string>(StringComparer.Ordinal);

        public ulong policyRevision { get; private set; }
        public bool globalKillSwitch { get; private set; }

        public RuntimePolicySnapshot(ulong policyRevision, bool globalKillSwitch = false)
        {
            this.policyRevision = policyRevision;
            this.globalKillSwitch = globalKillSwitch;
        }

        public void SetGlobalKillSwitch(bool disabled)
        {
            globalKillSwitch = disabled;
        }

        public void SetFactionDisabled(string factionKey, bool disabled)
        {
            if (string.IsNullOrEmpty(factionKey))
                throw new ArgumentException("faction key is required", nameof(factionKey));

            if (disabled)
                _disabledFactions.Add(factionKey);
            else
                _disabledFactions.Remove(factionKey);
        }

        public void SetSkill(SkillRuntimeMode mode)
        {
            if (mode == null)
                throw new ArgumentNullException(nameof(mode));
            if (mode.skillId <= 0)
                throw new ArgumentOutOfRangeException(nameof(mode), "skill id must be positive");
            if (string.IsNullOrEmpty(mode.factionKey))
                throw new ArgumentException("faction key is required", nameof(mode));

            _skills[mode.skillId] = mode.Clone();
        }

        /// <summary>
        /// Most restrictive policy wins. Missing policies fail closed.
        /// </summary>
        public SkillRuntimeMode Resolve(int skillId, string factionKey)
        {
            if (globalKillSwitch ||
                string.IsNullOrEmpty(factionKey) ||
                _disabledFactions.Contains(factionKey) ||
                !_skills.TryGetValue(skillId, out SkillRuntimeMode mode) ||
                !mode.exposed ||
                !string.Equals(mode.factionKey, factionKey, StringComparison.Ordinal) ||
                mode.authorityMode == SkillAuthorityMode.Disabled ||
                mode.presentationMode == SkillPresentationMode.Disabled)
            {
                return SkillRuntimeMode.DisabledFor(skillId, factionKey);
            }

            return mode.Clone();
        }
    }
}
