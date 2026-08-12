using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class SandboxClearSkillMissionLifecycleRuntimeTests
    {
        [Test]
        public void SandboxClearSkillMissionLifecycleHost_MapsCallsToSandboxManager()
        {
            var managerGo = new UnityEngine.GameObject("SandboxManager");
            var manager = managerGo.AddComponent<SandboxManager>();
            var host = new SandboxClearSkillMissionLifecycleHost(manager);

            host.StartMissionTimer(10, 1, 600);
            host.SetMissionV(1, 100);
            Assert.That(host.GetMissionV(1), Is.EqualTo(100));

            host.SetTaskTemp(10, 20);
            host.SetPKFlag(2);
            host.ForbidChangePK(1);
            host.SetLogoutRV(1);
            host.SetDeathScript(@"\script\test.lua");
            host.CloseMission(10);
            host.GameOver();

            Assert.Pass("Host methods executed without exception against SandboxManager.");
        }
    }
}
