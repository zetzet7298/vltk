using System;
using System.Collections.Generic;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Mobile build target (M6.3).</summary>
    public enum MobileBuildTarget
    {
        Android,
        iOS,
    }

    /// <summary>Scripting backend; mobile release requires IL2CPP per the PRD.</summary>
    public enum ScriptingBackendKind
    {
        Mono,
        IL2CPP,
    }

    /// <summary>Build configuration to validate before a smoke build.</summary>
    public class BuildConfig
    {
        public MobileBuildTarget target;
        public ScriptingBackendKind backend = ScriptingBackendKind.IL2CPP;
        public bool developmentBuild;
        public string bundleId;
        public string version = "0.1.0";

        // Platform-setup signals (filled by the editor before validation).
        public bool androidSdkPresent;
        public bool androidNdkPresent;     // required for IL2CPP on Android
        public bool iosPlatformModulePresent;
        public bool iosSigningConfigured;
    }

    /// <summary>Result of validating a build configuration.</summary>
    public class BuildValidationResult
    {
        public bool canBuild;
        public bool gmExposed;        // whether GM/debug controls would be exposed
        public List<string> errors = new();
        public List<string> warnings = new();
    }

    /// <summary>
    /// M6.3 — Validates a mobile build configuration before a smoke build. Pure C#
    /// (no MonoBehaviour / no editor build API) so it is fully EditMode-testable.
    /// Confirms the IL2CPP config for Android (AC#1), reports missing iOS platform
    /// setup (AC#2), and enforces the GM-exposure guard: GM is allowed in development
    /// builds (AC#3 dev) but must be disabled/protected in release builds (M6.4 AC#3).
    /// A MonoBehaviour/editor driver maps the validated config to the real build API.
    /// </summary>
    public class BuildConfigService
    {
        /// <summary>AC#1/AC#2 — validate the config and decide whether a build can proceed.</summary>
        public BuildValidationResult Validate(BuildConfig config)
        {
            var result = new BuildValidationResult();
            if (config == null)
            {
                result.canBuild = false;
                result.errors.Add("Null build config");
                return result;
            }

            if (string.IsNullOrEmpty(config.bundleId))
                result.warnings.Add("Bundle id is not set");

            // Mobile release requires IL2CPP (PRD target runtime).
            if (!config.developmentBuild && config.backend != ScriptingBackendKind.IL2CPP)
                result.errors.Add("Release mobile build requires IL2CPP backend");

            switch (config.target)
            {
                case MobileBuildTarget.Android:
                    ValidateAndroid(config, result);
                    break;
                case MobileBuildTarget.iOS:
                    ValidateIos(config, result);
                    break;
            }

            // AC#3 — GM/debug controls are only exposed in development builds.
            result.gmExposed = config.developmentBuild;
            if (!config.developmentBuild)
                result.warnings.Add("Release build: GM/debug controls disabled");

            result.canBuild = result.errors.Count == 0;
            if (!result.canBuild)
                SubsystemLog.Error("Build", $"Build config invalid: {string.Join("; ", result.errors)}");
            return result;
        }

        private void ValidateAndroid(BuildConfig config, BuildValidationResult result)
        {
            // AC#1 — Android build completes with IL2CPP config; needs SDK + NDK.
            if (!config.androidSdkPresent)
                result.errors.Add("Android SDK not configured");
            if (config.backend == ScriptingBackendKind.IL2CPP && !config.androidNdkPresent)
                result.errors.Add("Android NDK required for IL2CPP build");
        }

        private void ValidateIos(BuildConfig config, BuildValidationResult result)
        {
            // AC#2 — iOS export completes OR reports missing platform setup.
            if (!config.iosPlatformModulePresent)
                result.errors.Add("iOS platform module/Xcode not available on this machine");
            else if (!config.iosSigningConfigured)
                result.warnings.Add("iOS signing not configured (export may need manual signing)");
        }

        /// <summary>
        /// M6.4 AC#3 — guard decision: should GM/debug controls be available for this
        /// config? True only for development builds. Release builds must protect/hide GM.
        /// </summary>
        public bool ShouldExposeGm(BuildConfig config)
            => config != null && config.developmentBuild;
    }
}
