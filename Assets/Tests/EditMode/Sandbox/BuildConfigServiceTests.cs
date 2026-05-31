using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M6.3 — Android/iOS Build Smoke tests. Android IL2CPP config validity (AC#1),
    /// iOS missing-platform-setup reporting (AC#2), and the GM-exposure guard for
    /// dev vs release (AC#3 / M6.4 AC#3).
    /// </summary>
    public class BuildConfigServiceTests
    {
        private BuildConfig Android(bool sdk = true, bool ndk = true, bool dev = false,
            ScriptingBackendKind backend = ScriptingBackendKind.IL2CPP)
            => new BuildConfig
            {
                target = MobileBuildTarget.Android,
                backend = backend,
                developmentBuild = dev,
                bundleId = "com.vltk.mobile",
                androidSdkPresent = sdk,
                androidNdkPresent = ndk,
            };

        private BuildConfig Ios(bool module = true, bool signing = true, bool dev = false)
            => new BuildConfig
            {
                target = MobileBuildTarget.iOS,
                backend = ScriptingBackendKind.IL2CPP,
                developmentBuild = dev,
                bundleId = "com.vltk.mobile",
                iosPlatformModulePresent = module,
                iosSigningConfigured = signing,
            };

        // --- AC#1: Android IL2CPP config ---

        [Test]
        public void Validate_Android_IL2CPP_WithSdkNdk_CanBuild()
        {
            var svc = new BuildConfigService();
            var result = svc.Validate(Android());
            Assert.IsTrue(result.canBuild);
            Assert.IsEmpty(result.errors);
        }

        [Test]
        public void Validate_Android_IL2CPP_MissingNdk_Fails()
        {
            var svc = new BuildConfigService();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Build config invalid"));
            var result = svc.Validate(Android(ndk: false));
            Assert.IsFalse(result.canBuild);
            CollectionAssert.Contains(result.errors, "Android NDK required for IL2CPP build");
        }

        [Test]
        public void Validate_Android_MissingSdk_Fails()
        {
            var svc = new BuildConfigService();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Build config invalid"));
            var result = svc.Validate(Android(sdk: false));
            Assert.IsFalse(result.canBuild);
        }

        [Test]
        public void Validate_ReleaseMono_Fails_RequiresIL2CPP()
        {
            var svc = new BuildConfigService();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Build config invalid"));
            var result = svc.Validate(Android(backend: ScriptingBackendKind.Mono, dev: false));
            Assert.IsFalse(result.canBuild);
            CollectionAssert.Contains(result.errors, "Release mobile build requires IL2CPP backend");
        }

        [Test]
        public void Validate_DevMono_Allowed()
        {
            var svc = new BuildConfigService();
            var result = svc.Validate(Android(backend: ScriptingBackendKind.Mono, dev: true));
            Assert.IsTrue(result.canBuild); // dev build may use Mono
        }

        // --- AC#2: iOS missing platform setup ---

        [Test]
        public void Validate_Ios_MissingPlatformModule_Reports()
        {
            var svc = new BuildConfigService();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Build config invalid"));
            var result = svc.Validate(Ios(module: false));
            Assert.IsFalse(result.canBuild);
            CollectionAssert.Contains(result.errors, "iOS platform module/Xcode not available on this machine");
        }

        [Test]
        public void Validate_Ios_NoSigning_WarnsButCanBuild()
        {
            var svc = new BuildConfigService();
            var result = svc.Validate(Ios(module: true, signing: false));
            Assert.IsTrue(result.canBuild);
            Assert.IsNotEmpty(result.warnings);
        }

        // --- AC#3 / M6.4 AC#3: GM exposure guard ---

        [Test]
        public void DevelopmentBuild_ExposesGm()
        {
            var svc = new BuildConfigService();
            var result = svc.Validate(Android(dev: true));
            Assert.IsTrue(result.gmExposed);
            Assert.IsTrue(svc.ShouldExposeGm(Android(dev: true)));
        }

        [Test]
        public void ReleaseBuild_HidesGm()
        {
            var svc = new BuildConfigService();
            var result = svc.Validate(Android(dev: false));
            Assert.IsFalse(result.gmExposed);
            Assert.IsFalse(svc.ShouldExposeGm(Android(dev: false)));
            CollectionAssert.Contains(result.warnings, "Release build: GM/debug controls disabled");
        }

        [Test]
        public void Validate_NullConfig_Fails()
        {
            var svc = new BuildConfigService();
            var result = svc.Validate(null);
            Assert.IsFalse(result.canBuild);
        }
    }
}
