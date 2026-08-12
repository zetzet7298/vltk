using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Production.App;

namespace VLTK.Tests.Production.EditMode
{
    public sealed class ProductionBoundaryTests
    {
        private static readonly string[] ProductionDirs =
        {
            "Assets/Scripts/App",
            "Assets/Scripts/Networking",
            "Assets/Scripts/UI/Runtime",
            "Assets/Scripts/World/Unity",
            "Assets/Editor/Production"
        };

        [Test]
        public void ProductionAssemblies_DoNotReferenceSandboxRuntime()
        {
            foreach (string dir in ProductionDirs)
            foreach (string file in Directory.GetFiles(dir, "*.asmdef", SearchOption.TopDirectoryOnly))
            {
                string text = File.ReadAllText(file);
                Assert.That(text, Does.Not.Contain("VLTK.Sandbox.Runtime"), file);
                Assert.That(text, Does.Not.Contain("VLTK.UI\""), file);
            }
        }

        [Test]
        public void ProductionCode_DoesNotContainForbiddenAssemblyBoundaryTokens()
        {
            string[] forbidden =
            {
                "VLTK.Sandbox",
                "SandboxManager",
                "SandboxPlayerController",
                "MockGameBackend",
                "TestData",
                "Map_79",
                "Directory.GetFiles"
            };

            foreach (string dir in ProductionDirs)
            foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly))
            {
                string text = File.ReadAllText(file);
                foreach (string token in forbidden)
                    Assert.That(text, Does.Not.Contain(token), file + " contains " + token);
            }
        }

        [Test]
        public void ProductionMovement_UsesEncoderSeam_NotGeneratedWireBytes()
        {
            string sender = "Assets/Scripts/Networking/MovementIntentSender.cs";
            string text = File.ReadAllText(sender);
            Assert.That(text, Does.Contain("IPlayerInputMoveEncoder"));
            Assert.That(text, Does.Not.Contain("Game.V1.MoveInput"));
            Assert.That(text, Does.Not.Contain("MoveInputFieldNumber"));
        }

        [Test]
        public void ProductionBootstrapper_WiresPythonBackendToCanonicalMap53()
        {
            var root = new GameObject("production-bootstrapper-test");
            try
            {
                var bootstrapper = root.AddComponent<ProductionBootstrapper>();
                bootstrapper.Initialize();

                Assert.That(bootstrapper.BackendRunner, Is.Not.Null);
                Assert.That(bootstrapper.BackendRunner.enterMapId, Is.EqualTo(53));
                Assert.That(bootstrapper.BackendRunner.enterPosX, Is.EqualTo(48032));
                Assert.That(bootstrapper.BackendRunner.enterPosY, Is.EqualTo(117504));
                Assert.That(bootstrapper.BackendRunner.runCombatDemoOnComplete, Is.False);
                Assert.That(bootstrapper.BackendRunner.playerObject,
                    Is.EqualTo(bootstrapper.Composition.avatarController.gameObject));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
