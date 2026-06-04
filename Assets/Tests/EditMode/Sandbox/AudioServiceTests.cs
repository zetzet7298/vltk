using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class AudioServiceTests
    {
        [Test]
        public void TryResolveResourcePath_NormalizesResourcesPathAndExtension()
        {
            var service = new AudioService();

            Assert.IsTrue(service.TryResolveResourcePath("Assets/Resources/Audio/SFX/ui_click.wav", out var resourcesPath));
            Assert.AreEqual("Audio/SFX/ui_click", resourcesPath);

            Assert.IsTrue(service.TryResolveResourcePath("Audio\\SFX\\ui_click.wav", out resourcesPath));
            Assert.AreEqual("Audio/SFX/ui_click", resourcesPath);
        }

        [Test]
        public void ResolveStreamingAssetsUri_UsesUnityFileUriForRelativePaths()
        {
            var service = new AudioService();

            var uri = service.ResolveStreamingAssetsUri("Audio/SFX/ui_click.wav");

            Assert.IsTrue(uri.StartsWith("file://"), uri);
            StringAssert.EndsWith("Audio/SFX/ui_click.wav", uri.Replace('\\', '/'));
        }

        [UnityTest]
        public IEnumerator LoadClipAsync_CachesStreamingAssetsClipByResourcePath()
        {
            var service = new AudioService();

            var firstLoad = service.LoadClipAsync("Audio/SFX/ui_click.wav");
            yield return WaitFor(firstLoad);

            Assert.IsNotNull(firstLoad.Result);
            Assert.IsTrue(service.TryGetCachedClip("Audio/SFX/ui_click.wav", out var cached));
            Assert.AreSame(firstLoad.Result, cached);

            var secondLoad = service.LoadClipAsync("Audio/SFX/ui_click.wav");
            yield return WaitFor(secondLoad);

            Assert.AreSame(firstLoad.Result, secondLoad.Result);
        }

        [UnityTest]
        public IEnumerator LoadClipAsync_LogsClearFallbackWhenFileIsMissing()
        {
            var service = new AudioService();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Audio clip missing: 'Audio/SFX/does_not_exist.wav'.*not found in Resources/Audio/SFX/does_not_exist or StreamingAssets"));

            var load = service.LoadClipAsync("Audio/SFX/does_not_exist.wav");
            yield return WaitFor(load);

            Assert.IsNull(load.Result);
        }

        private static IEnumerator WaitFor(System.Threading.Tasks.Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }
    }
}
