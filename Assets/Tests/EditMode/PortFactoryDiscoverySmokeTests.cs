using NUnit.Framework;

namespace VLTK.Tests.EditMode
{
    /// <summary>
    /// Minimal discovery sentinel for the EditMode test assembly.
    /// If Unity Test Runner still reports 0 tests while this file exists,
    /// the failure is assembly inclusion/discovery rather than test naming or fixtures.
    /// </summary>
    public class PortFactoryDiscoverySmokeTests
    {
        [Test]
        public void EditModeAssembly_IsDiscoverableByUnityTestRunner()
        {
            Assert.Pass("VLTK.Tests.EditMode discovery smoke test is present.");
        }
    }
}
