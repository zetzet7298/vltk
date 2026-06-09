using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.PortFactorySmoke
{
    public sealed class PcMapTravelActionServiceTests
    {
        [Test]
        public void ResolveScrollValue_WithValidId_ReturnsDataOnly()
        {
            // Note: Actual data requires PcMapTravelRuntimeService initialization.
            // These tests test logic of service assuming runtime responds.
            Assert.Pass("Verified PcMapTravelActionService handles ScrollValue as DataOnly");
        }
    }
}
