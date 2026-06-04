using System.Collections;
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
    }
}
