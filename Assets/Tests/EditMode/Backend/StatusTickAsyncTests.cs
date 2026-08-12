// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — StatusTickAsyncTests
// EditMode test cho slice FS-03C endpoint StatusTickAsync:
//   - Rest:  POST /v1/combat/status/tick body=StatusTickRequest
//            → DataResponse[StatusTickResponse]
//   - Mock:  trả về StatusTickResponse với poison time-- + DoT
//
// Phủ: URL build, body JSON serialize (camelCase, target/status nested),
// parse envelope, error path, controlled=true khi freeze/stun còn time,
// poison DoT làm trừ life.
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
    public class StatusTickAsyncTests
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

        private static CombatantState NewTarget() => new CombatantState
        {
            life = 1000,
            lifeMax = 1000,
            mana = 0,
            manaMax = 0,
        };

        private static StatusBundle NewEmptyStatus() => new StatusBundle
        {
            poisonState = new StateNode { value0 = 0, value1 = 0, value2 = 0, time = 0 },
            freezeState = new StateNode { value0 = 0, value1 = 0, value2 = 0, time = 0 },
            burnState = new StateNode { value0 = 0, value1 = 0, value2 = 0, time = 0 },
            confuseState = new StateNode { value0 = 0, value1 = 0, value2 = 0, time = 0 },
            stunState = new StateNode { value0 = 0, value1 = 0, value2 = 0, time = 0 },
            lifeState = new StateNode { value0 = 0, value1 = 0, value2 = 0, time = 0 },
            manaState = new StateNode { value0 = 0, value1 = 0, value2 = 0, time = 0 },
            drunkState = new StateNode { value0 = 0, value1 = 0, value2 = 0, time = 0 },
        };

        // Envelope thật từ backend FastAPI — poison DoT 15 dmg
        private const string EnvelopePoison = "{" +
            "\"code\":\"200\"," +
            "\"message\":\"Success\"," +
            "\"data\":{" +
              "\"controlled\":false,\"confuseEnded\":false," +
              "\"dotResults\":[{\"damage\":15,\"manaAbsorbed\":0,\"armorAbsorbed\":0," +
                "\"manaShieldBroke\":false,\"targetDied\":false,\"reflectToAttacker\":0,\"reflectKind\":4}]," +
              "\"auraCastSkillId\":0,\"auraCastLevel\":0," +
              "\"target\":{\"life\":985,\"lifeMax\":1000,\"mana\":0,\"manaMax\":0," +
                "\"physicsResist\":0,\"coldResist\":0,\"fireResist\":0,\"lightResist\":0,\"poisonResist\":0," +
                "\"physicsResistMax\":95,\"coldResistMax\":95,\"fireResistMax\":95,\"lightResistMax\":95,\"poisonResistMax\":95," +
                "\"physicsArmor\":0,\"coldArmor\":0,\"fireArmor\":0,\"lightArmor\":0,\"poisonArmor\":0," +
                "\"physicsArmorTime\":0,\"coldArmorTime\":0,\"fireArmorTime\":0,\"lightArmorTime\":0,\"poisonArmorTime\":0," +
                "\"manaShieldPercent\":0,\"manaShieldTime\":0," +
                "\"meleeDmgRet\":0,\"rangeDmgRet\":0,\"meleeDmgRetPercent\":0,\"rangeDmgRetPercent\":0," +
                "\"damage2ManaPercent\":0,\"isPlayer\":false}," +
              "\"status\":{" +
                "\"poisonState\":{\"value0\":15,\"value1\":2,\"value2\":0,\"time\":2}," +
                "\"freezeState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                "\"burnState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                "\"confuseState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                "\"stunState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                "\"lifeState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                "\"manaState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                "\"drunkState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}}" +
            "}}";

        // -------- RestGameBackend --------

        [Test]
        public async Task Rest_BuildsPostUrlAndSendsJsonBody()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/status/tick", 200, EnvelopePoison);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var req = new StatusTickRequest
            {
                target = NewTarget(),
                status = NewEmptyStatus(),
                loopFrames = 1,
                isSitting = false,
                lifeReplenish = 30,
                manaReplenish = 20,
            };
            // Active poison: server sẽ tick
            req.status.poisonState = new StateNode { value0 = 15, value1 = 2, value2 = 0, time = 3 };
            var resp = await backend.StatusTickAsync(req);

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.IsNotNull(resp.data.dotResults);
            Assert.AreEqual(1, resp.data.dotResults.Count);
            Assert.AreEqual(15, resp.data.dotResults[0].damage);
            Assert.AreEqual(985, resp.data.target.life);
            Assert.AreEqual(2, resp.data.status.poisonState.time);

            // URL
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("POST", fake.Sent[0].Method);
            Assert.AreEqual($"{BaseUrl}/v1/combat/status/tick", fake.Sent[0].Url);

            // Body
            string body = fake.Sent[0].Body;
            StringAssert.Contains("\"target\":", body);
            StringAssert.Contains("\"status\":", body);
            StringAssert.Contains("\"lifeReplenish\":30", body);
            StringAssert.Contains("\"manaReplenish\":20", body);
        }

        [Test]
        public async Task Rest_NullRequest_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.StatusTickAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task Rest_NullTarget_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var req = new StatusTickRequest
            {
                target = null,
                status = NewEmptyStatus(),
            };
            var resp = await backend.StatusTickAsync(req);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Rest_NullStatus_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var req = new StatusTickRequest
            {
                target = NewTarget(),
                status = null,
            };
            var resp = await backend.StatusTickAsync(req);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Rest_HttpError500_ReturnsFailure()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/status/tick", 500, "{\"detail\":\"db down\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.StatusTickAsync(new StatusTickRequest
            {
                target = NewTarget(), status = NewEmptyStatus(),
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task Rest_TransportError_ReturnsTransportError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("POST", "/v1/combat/status/tick", new Exception("conn reset"));
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.StatusTickAsync(new StatusTickRequest
            {
                target = NewTarget(), status = NewEmptyStatus(),
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }

        [Test]
        public async Task Rest_MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/status/tick", 200, "not json at all");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.StatusTickAsync(new StatusTickRequest
            {
                target = NewTarget(), status = NewEmptyStatus(),
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("parse_error", resp.code);
        }

        // -------- MockGameBackend --------

        [Test]
        public async Task Mock_DecrementsPoisonTimeAndAppliesDot()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var target = NewTarget();
            var status = NewEmptyStatus();
            status.poisonState = new StateNode { value0 = 15, value1 = 2, value2 = 0, time = 3 };

            var resp = await backend.StatusTickAsync(new StatusTickRequest
            {
                target = target, status = status,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data.dotResults);
            Assert.AreEqual(1, resp.data.dotResults.Count);
            Assert.AreEqual(15, resp.data.dotResults[0].damage);
            Assert.AreEqual(4, resp.data.dotResults[0].reflectKind); // poison
            // time 3 -> 2
            Assert.AreEqual(2, resp.data.status.poisonState.time);
            // life 1000 - 15 = 985
            Assert.AreEqual(985, resp.data.target.life);
        }

        [Test]
        public async Task Mock_ControlledTrue_WhenStunActive()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var status = NewEmptyStatus();
            status.stunState = new StateNode { time = 2 };

            var resp = await backend.StatusTickAsync(new StatusTickRequest
            {
                target = NewTarget(), status = status,
            });

            Assert.IsTrue(resp.data.controlled, "Stun còn time>0 → controlled=true");
            Assert.AreEqual(1, resp.data.status.stunState.time);
        }

        [Test]
        public async Task Mock_ControlledTrue_WhenFreezeActive()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var status = NewEmptyStatus();
            status.freezeState = new StateNode { time = 1 };

            var resp = await backend.StatusTickAsync(new StatusTickRequest
            {
                target = NewTarget(), status = status,
            });

            Assert.IsTrue(resp.data.controlled);
            Assert.AreEqual(0, resp.data.status.freezeState.time);
        }

        [Test]
        public async Task Mock_ConfuseEnded_WhenTimeReachesZero()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var status = NewEmptyStatus();
            status.confuseState = new StateNode { time = 1 };

            var resp = await backend.StatusTickAsync(new StatusTickRequest
            {
                target = NewTarget(), status = status,
            });

            Assert.IsTrue(resp.data.confuseEnded, "Confuse vừa hết → confuseEnded=true");
            Assert.AreEqual(0, resp.data.status.confuseState.time);
        }

        [Test]
        public async Task Mock_NullRequest_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.StatusTickAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_EmptyStatus_NoOp()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.StatusTickAsync(new StatusTickRequest
            {
                target = NewTarget(), status = NewEmptyStatus(),
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data.dotResults);
            Assert.AreEqual(0, resp.data.dotResults.Count);
            Assert.IsFalse(resp.data.controlled);
            Assert.IsFalse(resp.data.confuseEnded);
            // target life không đổi
            Assert.AreEqual(1000, resp.data.target.life);
        }
    }
}
