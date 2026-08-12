using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Production.Networking;

namespace VLTK.Tests.Production.EditMode
{
    public sealed class ProductionOpenApiRestGatewayTests
    {
        [Test]
        public async Task TypedBootstrap_SendsRealmAndIdempotencyHeaders_AndParsesNestedDtos()
        {
            var handler = new CaptureHandler(ValidBootstrapJson());
            var gateway = new ProductionOpenApiRestGateway("https://api.example/v1", "00000000-0000-0000-0000-000000000053", () => "access-token", new HttpClient(handler));

            BootstrapResponse response = await gateway.BootstrapAsync("20000000-0000-0000-0000-000000000001", "editor-p1", "idem-key-12345678", CancellationToken.None);

            Assert.That(handler.lastPath, Does.StartWith("/v1/bootstrap"));
            Assert.That(handler.realmHeader, Is.EqualTo("00000000-0000-0000-0000-000000000053"));
            Assert.That(handler.idempotencyHeader, Is.EqualTo("idem-key-12345678"));
            Assert.That(response.IsValid("00000000-0000-0000-0000-000000000053"), Is.True);
            Assert.That(response.contentDigest.clientProjectionSha256, Is.EqualTo(new string('c', 64)));
            Assert.That(response.admission.url, Is.EqualTo("wss://realm.example/game"));
        }

        [Test]
        public void TypedBootstrap_RejectsUnexpectedResponseField()
        {
            var handler = new CaptureHandler(ValidBootstrapJson().TrimEnd('}') + ",\"unexpected\":true}");
            var gateway = new ProductionOpenApiRestGateway("https://api.example/v1", "00000000-0000-0000-0000-000000000053", () => "access-token", new HttpClient(handler));

            Assert.ThrowsAsync<InvalidOperationException>(() => gateway.BootstrapAsync("20000000-0000-0000-0000-000000000001", "editor-p1", "idem-key-12345678", CancellationToken.None));
        }

        private static string ValidBootstrapJson()
        {
            string a = new string('a', 64);
            string b = new string('b', 64);
            string c = new string('c', 64);
            return "{\"realmId\":\"00000000-0000-0000-0000-000000000053\",\"contentReleaseId\":\"30000000-0000-0000-0000-000000000053\",\"sourceSnapshotId\":\"map-runtime\",\"userFacingLocale\":\"vi\",\"manifestUrl\":\"https://cdn.example/map-runtime.v1.json\",\"manifestSha256\":\"" + a + "\",\"minClientVersion\":\"editor-p1\",\"recommendedClientVersion\":\"editor-p1\",\"uiPanelFlags\":{\"schemaVersion\":1,\"revision\":1,\"contentReleaseId\":\"30000000-0000-0000-0000-000000000053\",\"issuedAt\":\"2030-01-01T00:00:00Z\",\"signingKeyId\":\"ui-key\",\"signatureAlgorithm\":\"Ed25519\",\"signature\":\"signed-ui-flags\",\"flags\":[]},\"contentDigest\":{\"contentReleaseId\":\"30000000-0000-0000-0000-000000000053\",\"manifestSha256\":\"" + a + "\",\"sourceSnapshotId\":\"map-runtime\",\"catalogUnionSize\":242,\"catalogUnionSha256\":\"" + b + "\",\"runtimeSkillPolicyId\":\"p1\",\"clientProjectionSha256\":\"" + c + "\"},\"runtimeSkillPolicy\":{\"policyId\":\"p1\",\"catalogUnionSize\":242,\"catalogUnionSha256\":\"" + b + "\",\"sourceTool\":\"vltktool\",\"filesystemFallbackAllowed\":false,\"runtimeParityClaimed\":false,\"pcRuntimeEvidenceStatus\":\"pending\",\"androidPhysicalEvidenceStatus\":\"pending\"},\"admission\":{\"url\":\"wss://realm.example/game\",\"subprotocol\":\"game.v1\",\"ticket\":\"ticket-00000000000000000000000000\",\"expiresAt\":\"2030-01-01T00:00:00Z\",\"tickRateHz\":18,\"sessionEpoch\":7,\"reconnectGraceSeconds\":15}}";
        }

        private sealed class CaptureHandler : HttpMessageHandler
        {
            private readonly string _payload;
            public string lastPath;
            public string realmHeader;
            public string idempotencyHeader;

            public CaptureHandler(string payload) { _payload = payload; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                lastPath = request.RequestUri.AbsolutePath + request.RequestUri.Query;
                realmHeader = request.Headers.Contains("X-Realm-ID") ? string.Join(",", request.Headers.GetValues("X-Realm-ID")) : null;
                idempotencyHeader = request.Headers.Contains("Idempotency-Key") ? string.Join(",", request.Headers.GetValues("Idempotency-Key")) : null;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_payload) });
            }
        }
    }
}
