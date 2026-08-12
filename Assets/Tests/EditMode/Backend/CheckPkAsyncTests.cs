// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — CheckPkAsyncTests
// EditMode test cho slice FS-03C endpoint CheckPkAsync:
//   - Rest:  POST /v1/combat/pk/check body=PkCheckRequest
//            → DataResponse[PkCheckResponse]
//   - Mock:  City/Capital = safe zone, Battlefield = OK
//
// Phủ: URL build, body JSON serialize, parse envelope, error path, safe
// zone logic, same-fence block, different-fence allow.
// -----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Mock;
using VLTK.Backend.Rest;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class CheckPkAsyncTests
    {
        private const string BaseUrl = "http://127.0.0.1:8020";

        private static BackendConfig NewRestConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = false,
            defaultTimeoutSeconds = 5,
        };

        private static BackendConfig NewMockConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = true,
        };

        // Envelope thật từ backend FastAPI — vùng an toàn City
        private const string EnvelopeSafeZone = "{" +
            "\"code\":\"200\"," +
            "\"message\":\"Success\"," +
            "\"data\":{" +
              "\"canAttack\":false," +
              "\"mapPkAllowed\":false," +
              "\"isSafeZone\":true," +
              "\"reason\":\"Vùng an toàn — cấm PK\"" +
            "}}";

        // Envelope thật từ backend FastAPI — Battlefield OK
        private const string EnvelopeBattlefield = "{" +
            "\"code\":\"200\"," +
            "\"message\":\"Success\"," +
            "\"data\":{" +
              "\"canAttack\":true," +
              "\"mapPkAllowed\":true," +
              "\"isSafeZone\":false," +
              "\"reason\":null" +
            "}}";

        // -------- RestGameBackend --------

        [Test]
        public async Task Rest_BuildsPostUrlAndSendsJsonBody()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/pk/check", 200, EnvelopeSafeZone);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var req = new PkCheckRequest
            {
                attackerCamp = 1,
                targetCamp = 2,
                mapType = "City",
                inBattle = false,
            };
            var resp = await backend.CheckPkAsync(req);

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.IsFalse(resp.data.canAttack);
            Assert.IsTrue(resp.data.isSafeZone);
            Assert.AreEqual("Vùng an toàn — cấm PK", resp.data.reason);

            // URL
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("POST", fake.Sent[0].Method);
            Assert.AreEqual($"{BaseUrl}/v1/combat/pk/check", fake.Sent[0].Url);

            // Body
            string body = fake.Sent[0].Body;
            StringAssert.Contains("\"attackerCamp\":1", body);
            StringAssert.Contains("\"targetCamp\":2", body);
            StringAssert.Contains("\"mapType\":\"City\"", body);
            StringAssert.Contains("\"inBattle\":false", body);
        }

        [Test]
        public async Task Rest_ParsesBattlefieldAllow()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/pk/check", 200, EnvelopeBattlefield);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2,
                mapType = "Battlefield", inBattle = true,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsTrue(resp.data.canAttack);
            Assert.IsTrue(resp.data.mapPkAllowed);
            Assert.IsFalse(resp.data.isSafeZone);
            Assert.IsNull(resp.data.reason);
        }

        [Test]
        public async Task Rest_NullRequest_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CheckPkAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task Rest_NullMapType_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2,
                mapType = null, inBattle = false,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task Rest_EmptyMapType_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2,
                mapType = "", inBattle = false,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Rest_HttpError500_ReturnsFailure()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/pk/check", 500, "{\"detail\":\"db error\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2, mapType = "City", inBattle = false,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task Rest_TransportError_ReturnsTransportError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("POST", "/v1/combat/pk/check", new Exception("conn reset"));
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2, mapType = "City", inBattle = false,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }

        [Test]
        public async Task Rest_MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/pk/check", 200, "not json at all");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2, mapType = "City", inBattle = false,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("parse_error", resp.code);
        }

        // -------- MockGameBackend --------

        [Test]
        public async Task Mock_City_IsSafeZone_NoAttack()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2,
                mapType = "City", inBattle = false,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsFalse(resp.data.canAttack);
            Assert.IsTrue(resp.data.isSafeZone);
            Assert.IsFalse(resp.data.mapPkAllowed);
            StringAssert.Contains("Vùng an toàn", resp.data.reason);
        }

        [Test]
        public async Task Mock_Capital_IsSafeZone_NoAttack()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2,
                mapType = "Capital", inBattle = false,
            });

            Assert.IsTrue(resp.data.isSafeZone);
            Assert.IsFalse(resp.data.canAttack);
        }

        [Test]
        public async Task Mock_Battlefield_AllowsDifferentCamp()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2,
                mapType = "Battlefield", inBattle = true,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsTrue(resp.data.canAttack);
            Assert.IsTrue(resp.data.mapPkAllowed);
            Assert.IsFalse(resp.data.isSafeZone);
        }

        [Test]
        public async Task Mock_SameCamp_NonSafeZone_BlocksAttack()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 1, // cùng phe
                mapType = "Field", inBattle = false,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsFalse(resp.data.canAttack);
            Assert.IsTrue(resp.data.mapPkAllowed, "Field không phải safe zone");
            Assert.IsFalse(resp.data.isSafeZone);
            StringAssert.Contains("Cùng phe", resp.data.reason);
        }

        [Test]
        public async Task Mock_NullRequest_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.CheckPkAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_NullMapType_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.CheckPkAsync(new PkCheckRequest
            {
                attackerCamp = 1, targetCamp = 2,
                mapType = null, inBattle = false,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }
    }
}
