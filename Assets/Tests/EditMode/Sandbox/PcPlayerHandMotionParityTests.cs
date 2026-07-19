// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved. Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Cast-only parity: catalog path/UID provenance and visible body-hand frame motion.
    /// Missing canonical bytes must remain missing; this fixture never supplies a fallback.
    /// </summary>
    [TestFixture, Category("Slow")]
    public sealed class PcPlayerHandMotionParityTests
    {
        private const string ManifestFile = "PcPlayerCastSprites.provenance.json";
        private const int CastTickCount = 20;
        private const int CanonicalSubclassCastCellCount = 360;
        private string _maleRoot;
        private string _femaleRoot;
        private GameObject _go;
        private Manifest _manifest;

        private static readonly PcWeaponType[] Weapons =
        {
            PcWeaponType.EmptyHand,
            PcWeaponType.ShortWeapon,
            PcWeaponType.LongWeapon,
            PcWeaponType.DualWeapon,
            PcWeaponType.HiddenWeapon,
        };

        private static readonly PlayerVisualAction[] FootActions =
        {
            PlayerVisualAction.Magic,
            PlayerVisualAction.Attack,
            PlayerVisualAction.Attack1,
        };

        private static readonly PlayerVisualAction[] MountedActions =
        {
            PlayerVisualAction.RideMagic,
            PlayerVisualAction.RideAttack,
            PlayerVisualAction.RideAttack1,
        };

        public static IEnumerable<TestCaseData> CastCells()
        {
            foreach (bool female in new[] { false, true })
            foreach (bool mounted in new[] { false, true })
            foreach (var weapon in Weapons)
            foreach (var action in mounted ? MountedActions : FootActions)
                yield return new TestCaseData(female, mounted, weapon, action)
                    .SetName($"{(female ? "Female" : "Male")}_{(mounted ? "Mounted" : "Foot")}_{weapon}_{action}_SynchronizesHands");
        }

        public static IEnumerable<TestCaseData> WeaponSubclassCastCells()
        {
            foreach (int variant in Enumerable.Range(1, 30))
            foreach (bool female in new[] { false, true })
            foreach (bool mounted in new[] { false, true })
            foreach (var action in mounted ? MountedActions : FootActions)
                yield return new TestCaseData(female, mounted, WeaponFamilyForVariant(variant), variant, action)
                    .SetName($"{(female ? "Female" : "Male")}_{(mounted ? "Mounted" : "Foot")}_Weapon{variant:D3}_{action}_SynchronizesBodyHands");
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            string sourceRoot = Path.Combine(Application.streamingAssetsPath, "Sprites");
            _maleRoot = MalePlayerSprStaging.StageForTests(sourceRoot);
            _femaleRoot = MalePlayerSprStaging.StageFemaleForTests(sourceRoot);

            string manifestPath = Path.Combine(Application.streamingAssetsPath, "Reference", ManifestFile);
            Assert.IsTrue(File.Exists(manifestPath), $"Missing cast provenance: {manifestPath}");
            _manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
              Assert.IsNotNull(_manifest);
              Assert.IsNotNull(_manifest.items);
              Assert.AreEqual("vltk.player-cast-sprite-provenance/v2", _manifest.schema);
              Assert.AreEqual(_manifest.items.Count(item => item.status == "staged"), _manifest.summary.staged);
              Assert.AreEqual(_manifest.items.Count(item => item.status == "missing"), _manifest.summary.missing);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_go != null)
                UnityEngine.Object.DestroyImmediate(_go);
            MalePlayerSprStaging.CleanupTempDir(_maleRoot);
            MalePlayerSprStaging.CleanupTempDir(_femaleRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                UnityEngine.Object.DestroyImmediate(_go);
            _go = null;
        }

        [Test]
          public void CastProvenance_RequestedByMatchesRuntimeHandMotionMatrix()
        {
            var runtimeCells = RuntimeCells().ToArray();
            CollectionAssert.AreEquivalent(runtimeCells, _manifest.matrix.requested_cells);
            Assert.AreEqual(60, runtimeCells.Length);
            Assert.AreEqual(runtimeCells.Length, runtimeCells.Distinct().Count());
            Assert.AreEqual(273, _manifest.summary.items_total);
            Assert.AreEqual(237, _manifest.summary.staged);
            Assert.AreEqual(36, _manifest.summary.missing);
            Assert.AreEqual(_manifest.summary.items_total, _manifest.items.Length);
            Assert.AreEqual(_manifest.summary.staged, _manifest.items.Count(item => item.status == "staged"));
            Assert.AreEqual(_manifest.summary.missing, _manifest.items.Count(item => item.status == "missing"));

            var itemsByPath = _manifest.items.ToDictionary(item => item.logical_path);
              var expectedByPath = _manifest.items.ToDictionary(item => item.logical_path,
                  item => new List<string>());
              var expectedRequiredByPath = _manifest.items.ToDictionary(item => item.logical_path,
                  item => new List<string>());
              var expectedOptionalByPath = _manifest.items.ToDictionary(item => item.logical_path,
                  item => new List<string>());
              foreach (string cell in runtimeCells)
              {
                ParseCell(cell, out bool female, out bool mounted, out PcWeaponType weapon, out PlayerVisualAction action);
                string suffix = ExpectedSuffix(weapon, mounted, action);
                foreach (var part in BuildParts(female, action, weapon))
                {
                    if (string.IsNullOrEmpty(part.sourcePath)) continue;
                    Assert.IsTrue(part.sourcePath.EndsWith("_" + suffix + ".spr"),
                        $"Runtime cell {cell} uses wrong suffix: {part.sourcePath}");

                    string logicalPath = "\\" + part.sourcePath;
                      if (itemsByPath.ContainsKey(logicalPath))
                      {
                          expectedByPath[logicalPath].Add(cell);
                          (part.required ? expectedRequiredByPath : expectedOptionalByPath)[logicalPath].Add(cell);
                      }
                  }
              }

            foreach (string cell in runtimeCells)
                Assert.IsTrue(expectedByPath.Values.Any(cells => cells.Contains(cell)),
                    $"Runtime cell lacks any available provenance item: {cell}");

              int mixedRequirementItems = 0;
              foreach (var item in _manifest.items)
              {
                Assert.IsNotNull(item.requested_by, item.logical_path);
                Assert.AreEqual(item.requested_by.Length, item.requested_by.Distinct().Count(),
                    $"Duplicate requested_by cell: {item.logical_path}");
                  CollectionAssert.AreEquivalent(expectedByPath[item.logical_path], item.requested_by,
                      $"Stale, extra, missing, or shared requested_by cell: {item.logical_path}");
                  bool requiredByAnyCell = expectedRequiredByPath[item.logical_path].Count > 0;
                  bool optionalByAnyCell = expectedOptionalByPath[item.logical_path].Count > 0;
                  Assert.AreEqual(requiredByAnyCell, item.required_by_catalog,
                      $"required_by_catalog must mean required by at least one requested cell: {item.logical_path}");
                  if (requiredByAnyCell && optionalByAnyCell)
                  {
                      mixedRequirementItems++;
                      Assert.IsNotNull(item.required_by_cells, item.logical_path);
                      Assert.IsNotNull(item.optional_by_cells, item.logical_path);
                      CollectionAssert.AreEquivalent(expectedRequiredByPath[item.logical_path], item.required_by_cells,
                          $"Mixed required attribution changed: {item.logical_path}");
                      CollectionAssert.AreEquivalent(expectedOptionalByPath[item.logical_path], item.optional_by_cells,
                          $"Mixed optional attribution changed: {item.logical_path}");
                  }
                  else
                  {
                      Assert.IsTrue(item.required_by_cells == null || item.required_by_cells.Length == 0,
                          $"Non-mixed item must not carry required_by_cells: {item.logical_path}");
                      Assert.IsTrue(item.optional_by_cells == null || item.optional_by_cells.Length == 0,
                          $"Non-mixed item must not carry optional_by_cells: {item.logical_path}");
                  }
                  Assert.AreEqual(SprRuntimeService.ComputePathUidHex(item.logical_path.Substring(1)), item.uid,
                      $"UID/hash path mismatch: {item.logical_path}");
                Assert.AreEqual("vltktool.jx_hash.hash_resource_path", item.hash.tool);
                Assert.AreEqual("gbk", item.hash.encoding);
                Assert.AreEqual(BitConverter.ToString(Encoding.GetEncoding(936).GetBytes(item.logical_path)).Replace("-", "").ToLowerInvariant(),
                    item.hash.path_bytes_hex, $"GBK hash path bytes changed: {item.logical_path}");
                  Assert.IsTrue(item.status == "staged" || item.status == "missing", item.logical_path);
              }
              Assert.AreEqual(_manifest.summary.mixed_requirement_items, mixedRequirementItems);
              Assert.AreEqual(3, mixedRequirementItems);
          }

          [Test]
          public void CastProvenance_RequirementAttributionMatchesPinnedPcLayerRule()
          {
              // Independent PC rule: male mounted LW_000 is a real empty layer;
              // female mounted LongWeapon is the only optional mounted left layer.
              AssertPinnedRequirement("\\spr\\npcres\\man\\MA_LW_000_HA01.spr", true);
              AssertPinnedRequirement("\\spr\\npcres\\man\\MA_LW_000_HA02.spr", true);
              AssertPinnedRequirement("\\spr\\npcres\\man\\MA_LW_000_HM01.spr", true);
              AssertPinnedRequirement("\\spr\\npcres\\woman\\FM_LW_000_HA01.spr", true,
                  new[] { "female/mounted/EmptyHand/RideAttack", "female/mounted/ShortWeapon/RideAttack" },
                  new[] { "female/mounted/LongWeapon/RideAttack" });
              AssertPinnedRequirement("\\spr\\npcres\\woman\\FM_LW_000_HA02.spr", true,
                  new[] { "female/mounted/EmptyHand/RideAttack1", "female/mounted/ShortWeapon/RideAttack1" },
                  new[] { "female/mounted/LongWeapon/RideAttack1" });
              AssertPinnedRequirement("\\spr\\npcres\\woman\\FM_LW_000_HM01.spr", true,
                  new[]
                  {
                      "female/mounted/EmptyHand/RideMagic",
                      "female/mounted/HiddenWeapon/RideAttack",
                      "female/mounted/HiddenWeapon/RideAttack1",
                      "female/mounted/HiddenWeapon/RideMagic",
                      "female/mounted/ShortWeapon/RideMagic",
                  },
                  new[] { "female/mounted/LongWeapon/RideMagic" });

              foreach (bool female in new[] { false, true })
              foreach (bool mounted in new[] { false, true })
              foreach (var weapon in Weapons)
              foreach (var action in mounted ? MountedActions : FootActions)
              {
                  var left = BuildParts(female, action, weapon).Single(part => part.kind == PlayerSpritePartKind.LeftWeapon);
                  bool expectedRequired = weapon != PcWeaponType.LongWeapon || mounted && !female;
                  Assert.AreEqual(expectedRequired, left.required,
                      $"PC left-layer requirement changed for {(female ? "female" : "male")}/{(mounted ? "mounted" : "foot")}/{weapon}/{action}.");
              }
          }

          private void AssertPinnedRequirement(string logicalPath, bool required, string[] requiredBy = null,
              string[] optionalBy = null)
          {
              var item = _manifest.items.Single(entry => entry.logical_path == logicalPath);
              Assert.AreEqual(required, item.required_by_catalog, logicalPath);
              if (requiredBy == null && optionalBy == null)
              {
                  Assert.IsTrue(item.required_by_cells == null || item.required_by_cells.Length == 0, logicalPath);
                  Assert.IsTrue(item.optional_by_cells == null || item.optional_by_cells.Length == 0, logicalPath);
                  return;
              }

              CollectionAssert.AreEquivalent(requiredBy, item.required_by_cells, logicalPath);
              CollectionAssert.AreEquivalent(optionalBy, item.optional_by_cells, logicalPath);
          }

        [TestCaseSource(nameof(CastCells))]
        public void CastCell_UsesCanonicalSprites_AndSynchronizesVisibleHands(bool female, bool mounted, PcWeaponType weapon, PlayerVisualAction action)
        {
            object visual = CreateVisual(female);
            Configure(visual, weapon, mounted, action);
            var specs = BuildParts(female, action, weapon);

            Assert.AreEqual(action, CurrentAction(visual));
            Assert.AreEqual(weapon, CurrentWeapon(visual));
            Assert.AreEqual(mounted, IsMounted(visual));
            AssertExactWeaponAndAction(specs, weapon, mounted, action);

            var requiredMissing = new List<PlayerSpritePartSpec>();
            foreach (var spec in specs.Where(part => part.required))
            {
                var record = RecordFor(spec.sourcePath);
                Assert.IsNotNull(record, $"Required catalog path absent from provenance: {spec.sourcePath}");
                Assert.AreEqual(SprRuntimeService.ComputePathUidHex(spec.sourcePath), record.uid, spec.sourcePath);
                if (record.status == "missing")
                {
                    requiredMissing.Add(spec);
                    Assert.IsNotEmpty(record.uid);
                    Assert.IsNotEmpty(record.reason);
                    Assert.IsFalse(File.Exists(Path.Combine(female ? _femaleRoot : _maleRoot, record.uid + ".spr")),
                        $"Missing canonical UID must not be substituted: {spec.sourcePath}");
                }
                else
                {
                    Assert.AreEqual("staged", record.status, spec.sourcePath);
                }
            }

            Assert.AreEqual(requiredMissing.Count == 0, HasAllRequiredParts(visual),
                $"Fail-closed state mismatch. Runtime missing: {string.Join(", ", MissingRequiredParts(visual))}");
            foreach (var missing in requiredMissing)
                CollectionAssert.Contains(MissingRequiredParts(visual), missing.sourcePath);

            AssertOptionalHolesStayOptional(specs, visual);
            AssertKnpcResAbsoluteFramesForStagedLayers(visual, specs);
        }

        [Test]
        public void WeaponSubclassCastCells_CoverAllCanonicalMeleeVariants()
        {
            var cells = WeaponSubclassCastCells().ToArray();
            Assert.AreEqual(CanonicalSubclassCastCellCount, cells.Length);
            foreach (int variant in Enumerable.Range(1, 30))
                Assert.IsTrue(WeaponFamilyForVariant(variant) is PcWeaponType.ShortWeapon or PcWeaponType.LongWeapon or PcWeaponType.DualWeapon);
        }

        [TestCaseSource(nameof(WeaponSubclassCastCells))]
        public void WeaponSubclassCastCell_UsesExactBanksAndSynchronizesStagedBodyHands(bool female, bool mounted,
            PcWeaponType weapon, int weaponVariant, PlayerVisualAction action)
        {
            object visual = CreateVisual(female);
            Configure(visual, weapon, mounted, action);
            SetWeaponVariant(visual, weaponVariant);
            var specs = female
                ? FemalePlayerSpriteCatalog.BuildParts(action, weapon, weaponVariant: weaponVariant)
                : MalePlayerSpriteCatalog.BuildParts(action, weapon, weaponVariant: weaponVariant);

            Assert.AreEqual(weapon, CurrentWeapon(visual));
            Assert.AreEqual(action, CurrentAction(visual));
            Assert.AreEqual(mounted, IsMounted(visual));
            AssertExactSubclassWeaponAndAction(specs, weapon, weaponVariant, mounted, action);
            AssertUnprovenWeaponOverlaysFailClosed(specs, visual);
            AssertKnpcResAbsoluteFramesForStagedLayers(visual, specs,
                PlayerSpritePartKind.Body, PlayerSpritePartKind.LeftHand, PlayerSpritePartKind.RightHand);
        }

        private object CreateVisual(bool female)
        {
            _go = new GameObject(female ? "FemaleHandMotionParity" : "MaleHandMotionParity");
            if (female)
            {
                var visual = _go.AddComponent<FemalePlayerVisual>();
                visual.spritesRootOverride = _femaleRoot;
                visual.playAutomatically = false;
                return visual;
            }

            var male = _go.AddComponent<MalePlayerVisual>();
            male.spritesRootOverride = _maleRoot;
            male.playAutomatically = false;
            return male;
        }

        private static PlayerSpritePartSpec[] BuildParts(bool female, PlayerVisualAction action, PcWeaponType weapon)
            => female
                ? FemalePlayerSpriteCatalog.BuildParts(action, weapon)
                : MalePlayerSpriteCatalog.BuildParts(action, weapon);

        private static void Configure(object visual, PcWeaponType weapon, bool mounted, PlayerVisualAction action)
        {
            if (visual is FemalePlayerVisual female)
            {
                female.SetWeapon(weapon);
                female.SetMounted(mounted);
                female.SetAction(action);
                return;
            }

            var male = (MalePlayerVisual)visual;
            male.SetWeapon(weapon);
            male.SetMounted(mounted);
            male.SetAction(action);
        }

        private static void SetWeaponVariant(object visual, int weaponVariant)
        {
            if (visual is FemalePlayerVisual female)
                female.SetEquipVariant(PlayerEquipSlot.Weapon, weaponVariant);
            else
                ((MalePlayerVisual)visual).SetEquipVariant(PlayerEquipSlot.Weapon, weaponVariant);
        }

        private static PlayerVisualAction CurrentAction(object visual)
            => visual is FemalePlayerVisual female ? female.currentAction : ((MalePlayerVisual)visual).currentAction;

        private static PcWeaponType CurrentWeapon(object visual)
            => visual is FemalePlayerVisual female ? female.currentWeapon : ((MalePlayerVisual)visual).currentWeapon;

        private static bool IsMounted(object visual)
            => visual is FemalePlayerVisual female ? female.IsMounted : ((MalePlayerVisual)visual).IsMounted;

        private static bool HasAllRequiredParts(object visual)
            => visual is FemalePlayerVisual female ? female.HasAllRequiredParts : ((MalePlayerVisual)visual).HasAllRequiredParts;

        private static IReadOnlyList<string> MissingRequiredParts(object visual)
            => visual is FemalePlayerVisual female ? female.LastMissingRequiredParts : ((MalePlayerVisual)visual).LastMissingRequiredParts;

        private static int CurrentFrame(object visual)
            => visual is FemalePlayerVisual female ? female.CurrentFrameInDirection : ((MalePlayerVisual)visual).CurrentFrameInDirection;

        private static float PlaybackRate(object visual)
            => visual is FemalePlayerVisual female ? female.CurrentPlaybackRate : ((MalePlayerVisual)visual).CurrentPlaybackRate;

        private static void Tick(object visual, float deltaTime)
        {
            if (visual is FemalePlayerVisual female)
                female.Tick(deltaTime);
            else
                ((MalePlayerVisual)visual).Tick(deltaTime);
        }

        private void AssertExactWeaponAndAction(IEnumerable<PlayerSpritePartSpec> specs, PcWeaponType weapon, bool mounted, PlayerVisualAction action)
        {
            string suffix = ExpectedSuffix(weapon, mounted, action);
            int weaponVariant = MalePlayerSpriteCatalog.GetWeaponSprVariant(weapon);
            var right = specs.Single(part => part.kind == PlayerSpritePartKind.RightWeapon);
            var left = specs.Single(part => part.kind == PlayerSpritePartKind.LeftWeapon);

            Assert.IsTrue(specs.Where(part => !string.IsNullOrEmpty(part.sourcePath)).All(part => part.sourcePath.EndsWith("_" + suffix + ".spr")),
                $"Catalog must use exact cast suffix {suffix}.");
            StringAssert.Contains($"RW_{weaponVariant:D3}_", right.sourcePath);
            StringAssert.Contains($"LW_{(weapon == PcWeaponType.DualWeapon ? 13 : 0):D3}_", left.sourcePath);
        }

        private void AssertExactSubclassWeaponAndAction(IEnumerable<PlayerSpritePartSpec> specs, PcWeaponType weapon,
            int weaponVariant, bool mounted, PlayerVisualAction action)
        {
            string suffix = ExpectedSubclassSuffix(weapon, weaponVariant, mounted, action);
            bool alternate = MalePlayerSpriteCatalog.ResolveMotionProfile(weapon, weaponVariant)
                == PcWeaponMotionProfile.AlternatePhysicalOrder;
            Assert.AreEqual(IsAlternateSubclassVariant(weapon, weaponVariant), alternate,
                $"Unexpected physical-order profile for weapon variant {weaponVariant}.");
            Assert.IsTrue(specs.Where(part => !string.IsNullOrEmpty(part.sourcePath)).All(part => part.sourcePath.EndsWith("_" + suffix + ".spr")),
                $"Weapon variant {weaponVariant} must use exact cast suffix {suffix}.");

            var right = specs.Single(part => part.kind == PlayerSpritePartKind.RightWeapon);
            var left = specs.Single(part => part.kind == PlayerSpritePartKind.LeftWeapon);
            StringAssert.Contains($"RW_{weaponVariant:D3}_", right.sourcePath);
            StringAssert.Contains($"LW_{(weapon == PcWeaponType.DualWeapon ? weaponVariant : 0):D3}_", left.sourcePath);
        }

        private void AssertUnprovenWeaponOverlaysFailClosed(IEnumerable<PlayerSpritePartSpec> specs, object visual)
        {
            foreach (var overlay in specs.Where(part => part.kind is PlayerSpritePartKind.LeftWeapon or PlayerSpritePartKind.RightWeapon))
            {
                var record = RecordFor(overlay.sourcePath);
                if (record != null && record.status == "staged") continue;
                Assert.IsFalse(overlay.required && HasAllRequiredParts(visual),
                    $"Missing or unproven required weapon overlay must fail closed: {overlay.sourcePath}");
                if (overlay.required)
                    CollectionAssert.Contains(MissingRequiredParts(visual), overlay.sourcePath);
            }
        }

        private static IEnumerable<string> RuntimeCells()
        {
            foreach (bool female in new[] { false, true })
            foreach (bool mounted in new[] { false, true })
            foreach (var weapon in Weapons)
            foreach (var action in mounted ? MountedActions : FootActions)
                yield return $"{(female ? "female" : "male")}/{(mounted ? "mounted" : "foot")}/{weapon}/{action}";
        }

        private static void ParseCell(string cell, out bool female, out bool mounted, out PcWeaponType weapon,
            out PlayerVisualAction action)
        {
            string[] segments = cell.Split('/');
            Assert.AreEqual(4, segments.Length, $"Invalid requested cell: {cell}");
            female = segments[0] == "female";
            Assert.IsTrue(female || segments[0] == "male", $"Invalid gender: {cell}");
            mounted = segments[1] == "mounted";
            Assert.IsTrue(mounted || segments[1] == "foot", $"Invalid posture: {cell}");
            Assert.IsTrue(Enum.TryParse(segments[2], out weapon), $"Invalid weapon: {cell}");
            Assert.IsTrue(Enum.TryParse(segments[3], out action), $"Invalid action: {cell}");
        }

        private static PcWeaponType WeaponFamilyForVariant(int weaponVariant)
        {
            if (weaponVariant is >= 1 and <= 6 or >= 19 and <= 22) return PcWeaponType.ShortWeapon;
            if (weaponVariant is >= 7 and <= 12 or >= 23 and <= 26) return PcWeaponType.LongWeapon;
            if (weaponVariant is >= 13 and <= 18 or >= 27 and <= 30) return PcWeaponType.DualWeapon;
            throw new ArgumentOutOfRangeException(nameof(weaponVariant));
        }

        private static bool IsAlternateSubclassVariant(PcWeaponType weapon, int weaponVariant)
            => weapon switch
            {
                PcWeaponType.ShortWeapon => weaponVariant is >= 4 and <= 6 or >= 20 and <= 22,
                PcWeaponType.LongWeapon => weaponVariant is >= 10 and <= 12 or >= 25 and <= 26,
                PcWeaponType.DualWeapon => weaponVariant is >= 16 and <= 18 or >= 29 and <= 30,
                _ => false,
            };

        private static string ExpectedSubclassSuffix(PcWeaponType weapon, int weaponVariant, bool mounted,
            PlayerVisualAction action)
        {
            if (mounted) return ExpectedSuffix(weapon, true, action);

            string magic = weapon switch
            {
                PcWeaponType.ShortWeapon => "MG03",
                PcWeaponType.LongWeapon => "MG04",
                PcWeaponType.DualWeapon => "MG05",
                _ => throw new ArgumentOutOfRangeException(nameof(weapon)),
            };
            if (action == PlayerVisualAction.Magic) return magic;

            string primary = weapon switch
            {
                PcWeaponType.ShortWeapon => "AT02",
                PcWeaponType.LongWeapon => "AT04",
                PcWeaponType.DualWeapon => "AT06",
                _ => throw new ArgumentOutOfRangeException(nameof(weapon)),
            };
            string alternate = weapon switch
            {
                PcWeaponType.ShortWeapon => "AT03",
                PcWeaponType.LongWeapon => "AT05",
                PcWeaponType.DualWeapon => "AT07",
                _ => throw new ArgumentOutOfRangeException(nameof(weapon)),
            };
            bool reverse = IsAlternateSubclassVariant(weapon, weaponVariant);
            return action == PlayerVisualAction.Attack
                ? (reverse ? alternate : primary)
                : (reverse ? primary : alternate);
        }

        private static string ExpectedSuffix(PcWeaponType weapon, bool mounted, PlayerVisualAction action)
        {
            if (mounted)
            {
                if (weapon == PcWeaponType.HiddenWeapon) return "HM01";
                return action == PlayerVisualAction.RideAttack ? "HA01"
                    : action == PlayerVisualAction.RideAttack1 ? "HA02"
                    : "HM01";
            }

            return MalePlayerSpriteCatalog.ResolveFootActionSuffix(action, weapon);
        }

        private void AssertOptionalHolesStayOptional(IEnumerable<PlayerSpritePartSpec> specs, object visual)
        {
            foreach (var spec in specs.Where(part => !part.required &&
                         (part.kind == PlayerSpritePartKind.Shoulder || part.kind == PlayerSpritePartKind.LeftWeapon)))
            {
                Assert.IsFalse(MissingRequiredParts(visual).Contains(spec.sourcePath),
                    $"Optional {spec.kind} must not make the visual fail closed.");
                if (string.IsNullOrEmpty(spec.sourcePath)) continue;

                var record = RecordFor(spec.sourcePath);
                Assert.IsNotNull(record, $"Optional catalog path absent from provenance: {spec.sourcePath}");
                Assert.AreEqual(SprRuntimeService.ComputePathUidHex(spec.sourcePath), record.uid, spec.sourcePath);
                Assert.IsTrue(record.status == "staged" || record.status == "missing", spec.sourcePath);
                if (record.status == "missing")
                    Assert.IsNotEmpty(record.reason, $"Missing optional hole needs provenance: {spec.sourcePath}");
            }
        }

        private void AssertKnpcResAbsoluteFramesForStagedLayers(object visual, IEnumerable<PlayerSpritePartSpec> specs,
            params PlayerSpritePartKind[] assertedKinds)
        {
            var loaded = specs
                .Where(spec => spec.kind != PlayerSpritePartKind.Shadow && !string.IsNullOrEmpty(spec.sourcePath))
                .Select(spec => new { spec, record = RecordFor(spec.sourcePath) })
                .Where(part => part.record != null && part.record.status == "staged")
                .OrderBy(part => (int)part.spec.kind)
                .ToArray();
            Assert.IsNotEmpty(loaded, "Cast cell needs one staged non-shadow KNpcRes driver layer.");
            var asserted = assertedKinds.Length == 0
                ? loaded
                : loaded.Where(part => assertedKinds.Contains(part.spec.kind)).ToArray();
            Assert.AreEqual(assertedKinds.Length == 0 ? loaded.Length : assertedKinds.Length, asserted.Length,
                "Every asserted body/hand layer must be staged and loaded from canonical provenance.");

            var driver = loaded[0];
            int driverFramesPerDirection = driver.record.spr.frames_total / MalePlayerSpriteCatalog.DirectionCount;
            Assert.AreEqual(0, driver.record.spr.frames_total % MalePlayerSpriteCatalog.DirectionCount,
                $"KNpcRes driver {driver.spec.kind} must retain its directional frame layout.");

            var playerVisual = (IPlayerVisual)visual;
            foreach (int direction in Enumerable.Range(0, MalePlayerSpriteCatalog.DirectionCount))
            foreach (int tick in Enumerable.Range(0, CastTickCount))
            {
                playerVisual.SetDirection(direction);
                playerVisual.SetLogicalActionProgress(tick / (float)CastTickCount);
                playerVisual.Tick(0f);

                int frameInDirection = Mathf.FloorToInt(tick / (float)CastTickCount * driverFramesPerDirection);
                int expectedAbsoluteFrame = direction * driverFramesPerDirection + frameInDirection;
                Assert.AreEqual(frameInDirection, CurrentFrame(visual),
                    $"KNpcRes driver diagnostic mismatch at direction {direction}, tick {tick}.");

                foreach (var part in asserted)
                {
                    var renderer = RendererFor(_go, part.spec.kind);
                    Assert.IsNotNull(renderer, $"No renderer for staged {part.spec.kind}.");
                    Assert.IsTrue(renderer.enabled, $"Staged {part.spec.kind} disabled at direction {direction}, tick {tick}.");
                    Assert.IsNotNull(renderer.sprite, $"Staged {part.spec.kind} sprite missing at direction {direction}, tick {tick}.");
                    Assert.AreEqual(expectedAbsoluteFrame, SpriteFrameIndex(renderer.sprite),
                        $"KNpcRes absolute driver frame mismatch for {part.spec.kind} at direction {direction}, tick {tick}.");
                }
            }
            playerVisual.SetLogicalActionProgress(-1f);
        }

        private Item RecordFor(string sourcePath)
            => _manifest.items.SingleOrDefault(item => item.logical_path == "\\" + sourcePath);

        private static SpriteRenderer RendererFor(GameObject root, PlayerSpritePartKind kind)
        {
            string prefix = $"Part_{(int)kind}_";
            return root.GetComponentsInChildren<SpriteRenderer>(true)
                .SingleOrDefault(renderer => renderer.gameObject.name.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static int SpriteFrameIndex(Sprite sprite)
        {
            int separator = sprite.name.LastIndexOf('_');
            Assert.GreaterOrEqual(separator, 0, $"Unexpected runtime sprite name: {sprite.name}");
            Assert.IsTrue(int.TryParse(sprite.name.Substring(separator + 1), out int index), $"Unexpected runtime sprite name: {sprite.name}");
            return index;
        }

        [Serializable]
          private sealed class Manifest
          {
              public string schema;
              public Matrix matrix;
              public Summary summary;
            public Item[] items;
        }

        [Serializable]
        private sealed class Matrix
        {
            public string[] requested_cells;
        }

        [Serializable]
        private sealed class Summary
        {
            public int items_total;
              public int staged;
              public int missing;
              public int mixed_requirement_items;
        }

        [Serializable]
        private sealed class Item
        {
            public string logical_path;
            public string uid;
            public Hash hash;
              public string[] requested_by;
              public bool required_by_catalog;
              public string[] required_by_cells;
              public string[] optional_by_cells;
              public string status;
            public string reason;
            public Spr spr;
        }

        [Serializable]
        private sealed class Hash
        {
            public string tool;
            public string encoding;
            public string path_bytes_hex;
        }

        [Serializable]
        private sealed class Spr
        {
            public int frames_total;
        }
    }
}
