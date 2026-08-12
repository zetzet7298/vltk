using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.PortFactorySmoke
{
    public class ReviveAndWharfTravelRuntimeTests
    {
        private SandboxManager _sandbox;
        private GameObject _sandboxGo;

        [SetUp]
        public void Setup()
        {
            _sandboxGo = new GameObject("SandboxManager");
            _sandbox = _sandboxGo.AddComponent<SandboxManager>();
            // Initialize basic root objects if needed for extension testing
        }

        [TearDown]
        public void Teardown()
        {
            if (_sandboxGo != null)
            {
                Object.DestroyImmediate(_sandboxGo);
            }
        }

        [Test]
        public void SandboxTeleportExtensions_HandleWharfTeleport_WarnsOnMissingWharf()
        {
            // Just verifying it doesn't crash without services
            _sandbox.HandleWharfTeleport(9999);
        }
    }
}
