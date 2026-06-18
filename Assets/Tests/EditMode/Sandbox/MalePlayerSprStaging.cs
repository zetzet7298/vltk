// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved. Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Runtime staging helper for male player weapon/attack/staff SPR fixtures.
    /// CTS-06: makes <see cref="MalePlayerVisualTests"/> self-contained so the
    /// 14 weapon/staff cases do not depend on which SPRs happen to be pre-staged
    /// in <c>StreamingAssets/Sprites</c>. Each test fixture gets its own temp
    /// directory and the runtime is pointed at it via
    /// <see cref="MalePlayerVisual.spritesRootOverride"/>.
    ///
    /// Source priority for fixture data (in order):
    ///   1. <c>sourceRoot</c> argument (defaults to <c>StreamingAssets/Sprites</c>
    ///      where the canonical extracted PC SPRs are already hashed by UID).
    ///   2. <c>STREAMING_SPRITES_OVERRIDE</c> env var (CI / hermetic builds).
    ///   3. The temp dir already populated by a prior <see cref="StageForTests"/>
    ///      call (idempotent).
    ///
    /// Each part spec emitted by <see cref="MalePlayerSpriteCatalog.BuildParts"/>
    /// for a (weapon × action) pair is hashed via
    /// <see cref="SprRuntimeService.ComputePathUidHex(string, string, bool)"/>
    /// (the same UID scheme the runtime uses to look up files on disk) and
    /// copied to the temp dir under its UID filename. The runtime then resolves
    /// the staged file by the same UID, so the test no longer cares whether
    /// global <c>StreamingAssets/Sprites</c> is populated.
    ///
    /// The helper is intentionally a static class so multiple test fixtures
    /// can share or replace each other without leaking state between them, and
    /// so it has no MonoBehaviour lifecycle (fully EditMode-testable).
    /// </summary>
    public static class MalePlayerSprStaging
    {
        /// <summary>
        /// Default canonical source for staged male player SPRs.
        /// Mirrors <c>Application.streamingAssetsPath + "Sprites"</c>, which is
        /// where the project's UID-hashed SPRs already live after the canonical
        /// <c>pak_unpacked</c> tree has been mirrored.
        /// </summary>
        public const string DefaultSourceRootEnvVar = "STREAMING_SPRITES_OVERRIDE";

        /// <summary>
        /// Stage every male player weapon/action combination into a fresh temp
        /// directory. The returned path is suitable for assigning to
        /// <see cref="MalePlayerVisual.spritesRootOverride"/>.
        /// </summary>
        /// <param name="sourceRoot">
        /// Optional override for where to read the canonical SPR bytes from.
        /// Defaults to <c>Path.Combine(Application.streamingAssetsPath, "Sprites")</c>
        /// (the existing UID-hashed fixture root) or the
        /// <see cref="DefaultSourceRootEnvVar"/> env var if set.
        /// </param>
        /// <param name="tempRoot">
        /// Optional explicit temp dir. When null, a unique dir under
        /// <see cref="Path.GetTempPath"/> is allocated.
        /// </param>
        /// <param name="staged">
        /// Output list of (source path, hashed uid) that were actually copied —
        /// useful for tests that want to assert coverage or to log missing
        /// fixtures when a test fails.
        /// </param>
        /// <returns>The temp dir containing the staged fixtures.</returns>
        public static string StageForTests(
            string sourceRoot = null,
            string tempRoot = null,
            List<StagedFixture> staged = null)
        {
            string resolvedSource = ResolveSourceRoot(sourceRoot);
            string dest = EnsureTempDir(tempRoot);

            StageMountedFixtures(resolvedSource, dest, staged);
            StageOnFootFixtures(resolvedSource, dest, staged);

            return dest;
        }

        /// <summary>
        /// Stage a single source path by computing its UID and copying the file
        /// from <paramref name="sourceRoot"/> to <paramref name="destRoot"/>.
        /// Returns the UID hash (lowercase hex) on success, or null if the
        /// source file is missing.
        /// </summary>
        public static string StageOne(string sourceRoot, string destRoot, string sourcePath, List<StagedFixture> staged = null)
        {
            if (string.IsNullOrEmpty(sourcePath))
                return null;
            string uid = SprRuntimeService.ComputePathUidHex(sourcePath);
            if (string.IsNullOrEmpty(uid))
                return null;

            string src = Path.Combine(sourceRoot, uid + ".spr");
            if (!File.Exists(src))
                return null;

            string dst = Path.Combine(destRoot, uid + ".spr");
            if (!File.Exists(dst))
            {
                File.Copy(src, dst);
            }
            staged?.Add(new StagedFixture(sourcePath, uid));
            return uid;
        }

        /// <summary>
        /// Convenience overload that resolves the source root via
        /// <see cref="ResolveSourceRoot"/> and uses the given dest root verbatim.
        /// </summary>
        public static string StageOne(string destRoot, string sourcePath, List<StagedFixture> staged = null)
            => StageOne(ResolveSourceRoot(null), destRoot, sourcePath, staged);

        /// <summary>
        /// Resolve the source root in priority order:
        ///   1. Caller-supplied non-empty string.
        ///   2. <see cref="DefaultSourceRootEnvVar"/> env var.
        ///   3. <c>Application.streamingAssetsPath + "Sprites"</c> (the
        ///      project's UID-hashed fixture root).
        /// </summary>
        public static string ResolveSourceRoot(string overridePath)
        {
            if (!string.IsNullOrEmpty(overridePath))
                return overridePath;
            string env = Environment.GetEnvironmentVariable(DefaultSourceRootEnvVar);
            if (!string.IsNullOrEmpty(env))
                return env;

            string localRuntime = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SpritesRuntime"));
            if (Directory.Exists(localRuntime))
                return localRuntime;

            return Path.Combine(Application.streamingAssetsPath, "Sprites");
        }

        /// <summary>
        /// Create the temp dir if it doesn't exist. When <paramref name="tempRoot"/>
        /// is null, a unique dir under <see cref="Path.GetTempPath"/> is allocated.
        /// </summary>
        public static string EnsureTempDir(string tempRoot)
        {
            string dest = string.IsNullOrEmpty(tempRoot)
                ? Path.Combine(Path.GetTempPath(), "MalePlayerTest_" + Guid.NewGuid().ToString("N"))
                : tempRoot;
            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);
            return dest;
        }

        /// <summary>
        /// Recursively delete the temp dir. Safe to call on a null or non-existent
        /// path. Errors are swallowed because the temp dir is best-effort cleanup.
        /// </summary>
        public static void CleanupTempDir(string tempRoot)
        {
            if (string.IsNullOrEmpty(tempRoot))
                return;
            try
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
                // Best-effort: temp dir cleanup is not on the test's critical path.
            }
        }

        // ----- internal: catalog-driven staging -----

        private static void StageOnFootFixtures(string sourceRoot, string destRoot, List<StagedFixture> staged)
        {
            // Cover every on-foot weapon × action combination the test suite
            // exercises. Mounted (Ride / RideMove) is staged separately because
            // its catalog is built via BuildMountedParts and uses different
            // variant numbers (mount outfit 050, horse 016).
            PcWeaponType[] weapons =
            {
                PcWeaponType.EmptyHand,
                PcWeaponType.ShortWeapon,
                PcWeaponType.LongWeapon,
                PcWeaponType.DualWeapon,
            };
            PlayerVisualAction[] actions =
            {
                PlayerVisualAction.Idle,
                PlayerVisualAction.Move,
                PlayerVisualAction.Magic,
                PlayerVisualAction.Attack,
            };
            foreach (var weapon in weapons)
            {
                foreach (var action in actions)
                {
                    var parts = MalePlayerSpriteCatalog.BuildParts(action, weapon);
                    foreach (var spec in parts)
                        StageOne(sourceRoot, destRoot, spec.sourcePath, staged);
                }
            }
        }

        private static void StageMountedFixtures(string sourceRoot, string destRoot, List<StagedFixture> staged)
        {
            // Mounted idle (RD01) and gallop (HR01) share the same BuildMountedParts
            // helper but the catalog selects the suffix at the call site. Stage
            // both, with the mount outfit 050 rider and horse 016 body.
            string[] suffixes = { "RD01", "HR01" };
            foreach (var suffix in suffixes)
            {
                var parts = MalePlayerSpriteCatalog.BuildMountedParts(
                    bodyVariant: 50,
                    headVariant: 50,
                    hairVariant: 50,
                    horseVariant: 16,
                    suffix: suffix);
                foreach (var spec in parts)
                    StageOne(sourceRoot, destRoot, spec.sourcePath, staged);
            }
        }

        /// <summary>
        /// Stage every female player weapon/action combination into a fresh temp directory.
        /// </summary>
        public static string StageFemaleForTests(
            string sourceRoot = null,
            string tempRoot = null,
            List<StagedFixture> staged = null)
        {
            string resolvedSource = ResolveSourceRoot(sourceRoot);
            string dest = EnsureTempDir(tempRoot);

            PcWeaponType[] weapons =
            {
                PcWeaponType.EmptyHand,
                PcWeaponType.ShortWeapon,
                PcWeaponType.LongWeapon,
                PcWeaponType.DualWeapon,
            };
            PlayerVisualAction[] actions =
            {
                PlayerVisualAction.Idle,
                PlayerVisualAction.Move,
                PlayerVisualAction.Magic,
                PlayerVisualAction.Attack,
            };
            foreach (var weapon in weapons)
            {
                foreach (var action in actions)
                {
                    var parts = FemalePlayerSpriteCatalog.BuildParts(action, weapon);
                    foreach (var spec in parts)
                        StageOne(resolvedSource, dest, spec.sourcePath, staged);
                }
            }

            // Also stage mounted actions for female
            string[] suffixes = { "RD01", "HR01" };
            foreach (var suffix in suffixes)
            {
                var parts = FemalePlayerSpriteCatalog.BuildMountedParts(
                    bodyVariant: 50,
                    headVariant: 50,
                    hairVariant: 50,
                    horseVariant: 16,
                    suffix: suffix);
                foreach (var spec in parts)
                    StageOne(resolvedSource, dest, spec.sourcePath, staged);
            }

            return dest;
        }

        /// <summary>
        /// One staged fixture record, exposed so tests can assert coverage or
        /// log the exact set of source paths the runtime was pointed at.
        /// </summary>
        public readonly struct StagedFixture
        {
            public readonly string SourcePath;
            public readonly string Uid;

            public StagedFixture(string sourcePath, string uid)
            {
                SourcePath = sourcePath;
                Uid = uid;
            }
        }
    }
}
