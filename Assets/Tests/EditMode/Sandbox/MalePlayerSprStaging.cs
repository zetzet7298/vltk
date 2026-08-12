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
    ///   1. Explicit <c>sourceRoot</c> argument.
    ///   2. <c>STREAMING_SPRITES_OVERRIDE</c> env var (CI / hermetic builds).
    ///   3. The repo-local <c>SpritesRuntime</c> canonical UID slice used by Editor.
    ///   4. <c>StreamingAssets/Sprites</c> when the external runtime slice is absent.
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
        /// Defaults to <c>SpritesRuntime</c>, then
        /// <c>Path.Combine(Application.streamingAssetsPath, "Sprites")</c>, or the
        /// <see cref="DefaultSourceRootEnvVar"/> env var when set.
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
        ///   3. Project-root <c>SpritesRuntime</c>, matching <see cref="SprRuntimeService"/>.
        ///   4. <c>Application.streamingAssetsPath + "Sprites"</c> as fallback.
        /// </summary>
        public static string ResolveSourceRoot(string overridePath)
        {
            if (!string.IsNullOrEmpty(overridePath))
                return overridePath;
            string env = Environment.GetEnvironmentVariable(DefaultSourceRootEnvVar);
            if (!string.IsNullOrEmpty(env))
                return env;

            string localRuntime = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SprRuntimeService.DefaultSpritesRoot));
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
            PcWeaponType[] weapons =
            {
                PcWeaponType.EmptyHand,
                PcWeaponType.ShortWeapon,
                PcWeaponType.LongWeapon,
                PcWeaponType.DualWeapon,
                PcWeaponType.HiddenWeapon,
            };
            PlayerVisualAction[] actions =
            {
                PlayerVisualAction.Idle,
                PlayerVisualAction.Move,
                PlayerVisualAction.Magic,
                PlayerVisualAction.Attack,
                PlayerVisualAction.Attack1,
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
            PcWeaponType[] weapons =
            {
                PcWeaponType.EmptyHand,
                PcWeaponType.ShortWeapon,
                PcWeaponType.LongWeapon,
                PcWeaponType.DualWeapon,
                PcWeaponType.HiddenWeapon,
            };
            PlayerVisualAction[] actions =
            {
                PlayerVisualAction.Ride,
                PlayerVisualAction.RideWalk,
                PlayerVisualAction.RideMove,
                PlayerVisualAction.RideAttack,
                PlayerVisualAction.RideAttack1,
                PlayerVisualAction.RideMagic,
            };
            foreach (var weapon in weapons)
            {
                foreach (var action in actions)
                {
                    var parts = MalePlayerSpriteCatalog.BuildParts(action, weapon,
                        MalePlayerSpriteCatalog.ArmorVariant,
                        MalePlayerSpriteCatalog.ArmorVariant,
                        MalePlayerSpriteCatalog.GetWeaponSprVariant(weapon),
                        MalePlayerSpriteCatalog.ArmorVariant,
                        MalePlayerSpriteCatalog.MountHorseVariant);
                    foreach (var spec in parts)
                        StageOne(sourceRoot, destRoot, spec.sourcePath, staged);
                }
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
                PcWeaponType.HiddenWeapon,
            };
            PlayerVisualAction[] actions =
            {
                PlayerVisualAction.Idle,
                PlayerVisualAction.Move,
                PlayerVisualAction.Magic,
                PlayerVisualAction.Attack,
                PlayerVisualAction.Attack1,
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

            PlayerVisualAction[] mountedActions =
            {
                PlayerVisualAction.Ride,
                PlayerVisualAction.RideWalk,
                PlayerVisualAction.RideMove,
                PlayerVisualAction.RideAttack,
                PlayerVisualAction.RideAttack1,
                PlayerVisualAction.RideMagic,
            };
            foreach (var weapon in weapons)
            {
                foreach (var action in mountedActions)
                {
                    var parts = FemalePlayerSpriteCatalog.BuildParts(action, weapon,
                        FemalePlayerSpriteCatalog.ArmorVariant,
                        FemalePlayerSpriteCatalog.ArmorVariant,
                        FemalePlayerSpriteCatalog.GetWeaponSprVariant(weapon),
                        FemalePlayerSpriteCatalog.ArmorVariant,
                        FemalePlayerSpriteCatalog.MountHorseVariant);
                    foreach (var spec in parts)
                        StageOne(resolvedSource, dest, spec.sourcePath, staged);
                }
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
