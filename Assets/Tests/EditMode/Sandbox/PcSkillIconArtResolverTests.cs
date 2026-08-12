using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sprites;
using VLTK.UI;

namespace VLTK.Tests.EditMode.Sandbox
{
    public class PcSkillIconArtResolverTests
    {
        [Test]
        public void SignedHash_ResolvesKnownPcSkillIconUid()
        {
            Assert.AreEqual("c4454165", SprRuntimeService.ComputePathUidHex(@"\spr\Ui\技能图标\icon_sk_ty_at.spr"));
            Assert.AreEqual("bedc5b69", SprRuntimeService.ComputePathUidHex(@"\spr\Ui\技能图标\icon_sk_ty_at.spr", "GB2312", signedBytes: false));
        }

        [Test]
        public void PcSkillIconResolver_LoadsKnownDecodedIcons()
        {
            AssertDecodedIcon(2);
            AssertDecodedIcon(714);
            AssertDecodedIcon(1073);
            AssertDecodedIcon(1074);
        }

        private static void AssertDecodedIcon(int skillId)
        {
            Assert.IsTrue(PcSkillIconArtResolver.TryResolveSkillIconPng(skillId, out var iconPath), $"skillId={skillId}");
            Assert.IsTrue(File.Exists(iconPath), iconPath);

            var data = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(texture.LoadImage(data));
            Assert.AreEqual(36, texture.width);
            Assert.AreEqual(36, texture.height);
        }
    }
}
