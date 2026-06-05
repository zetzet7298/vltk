using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.PlayMode
{
    public class AudioServicePlayModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
        }



        [UnityTest]
        public IEnumerator PlayBGMAsync_RetriesMissingRequestedTrackEvenWhenPreviousClipRemainsAssigned()
        {
            _root = new GameObject("AudioService_PlayMode_Root");
            var service = new AudioService();
            service.Initialize(_root.transform);

            var defs = GetAudioDefs(service);
            defs["test_bgm_a"] = new AudioDef
            {
                id = "test_bgm_a",
                category = AudioCategory.BGM,
                resourcePath = "Audio/SFX/ui_click.wav",
                volume = 1f,
                loop = true,
            };
            defs["test_bgm_b"] = new AudioDef
            {
                id = "test_bgm_b",
                category = AudioCategory.BGM,
                resourcePath = "Audio/SFX/test_bgm_b_missing.wav",
                volume = 1f,
                loop = true,
            };

            var playA = service.PlayBGMAsync("test_bgm_a");
            while (!playA.IsCompleted) yield return null;
            if (playA.IsFaulted) throw playA.Exception;

            var bgmSource = GetBgmSource(service);
            Assert.IsNotNull(bgmSource.clip, "BGM A should load successfully before testing the retry path.");
            Assert.AreEqual("test_bgm_a", bgmSource.clip.name);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Audio clip missing: 'Audio/SFX/test_bgm_b_missing.wav'.*not found in Resources/Audio/SFX/test_bgm_b_missing or StreamingAssets"));
            var missingB = service.PlayBGMAsync("test_bgm_b");
            while (!missingB.IsCompleted) yield return null;
            if (missingB.IsFaulted) throw missingB.Exception;

            Assert.IsNotNull(bgmSource.clip, "The previous BGM clip should remain assigned after BGM B fails to load.");
            Assert.AreEqual("test_bgm_a", bgmSource.clip.name);

            defs["test_bgm_b"].resourcePath = "Audio/SFX/ui_click.wav";

            var retryB = service.PlayBGMAsync("test_bgm_b");
            while (!retryB.IsCompleted) yield return null;
            if (retryB.IsFaulted) throw retryB.Exception;

            Assert.IsNotNull(bgmSource.clip, "Retrying BGM B after making it loadable should not be skipped because BGM A is still assigned.");
            Assert.AreEqual("test_bgm_b", bgmSource.clip.name);
        }

        [UnityTest]
        public IEnumerator PlaySFX_UiClick_CachesClipWhenStreamingAssetExists()
        {
            _root = new GameObject("AudioService_PlayMode_Root");
            var service = new AudioService();
            service.Initialize(_root.transform);

            var play = service.PlaySFXAsync("ui_click");
            while (!play.IsCompleted) yield return null;
            if (play.IsFaulted) throw play.Exception;

            Assert.IsTrue(service.TryGetCachedClip("Audio/SFX/ui_click.wav", out var clip));
            Assert.IsNotNull(clip, "PlaySFX(\"ui_click\") should resolve the real StreamingAssets clip when it exists.");
        }


        // NOTE: These helpers use reflection to access private fields (_defs, _bgmSource).
        // If the field names change, these tests will fail at runtime instead of compile time.
        // Consider [assembly: InternalsVisibleTo("Tests")] + internal accessors in a future refactor.
        private static Dictionary<string, AudioDef> GetAudioDefs(AudioService service)
        {
            var field = typeof(AudioService).GetField("_defs", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "AudioService should keep audio definitions in _defs.");
            return (Dictionary<string, AudioDef>)field.GetValue(service);
        }

        private static AudioSource GetBgmSource(AudioService service)
        {
            var field = typeof(AudioService).GetField("_bgmSource", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "AudioService should keep its BGM source in _bgmSource.");
            return (AudioSource)field.GetValue(service);
        }
    }
}
