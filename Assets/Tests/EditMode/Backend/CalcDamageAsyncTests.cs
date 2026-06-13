// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — CalcDamageAsyncTests
// EditMode test cho slice FS-03C endpoint CalcDamageAsync:
//   - Rest:  POST /v1/combat/damage/calc body=DamageCalcRequest
//            → DataResponse[DamageCalcResponse]
//   - Mock:  trả về DamageCalcResponse với mock damage formula
//
// Phủ: URL build, body JSON serialize (camelCase, target nested, attacker
// optional), parse envelope, error path (HTTP 500, transport error, invalid_arg
// khi request/target=null), mismatched client-vs-server damage detection.
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
    public class CalcDamageAsyncTests
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

        // Tạo CombatantState mặc định (life=1000, full HP, không có buff).
        private static CombatantState NewTarget(int life = 1000, int lifeMax = 1000) => new CombatantState
        {
            life = life,
            lifeMax = lifeMax,
            mana = 0,
            manaMax = 0,
            physicsResist = 0,
            coldResist = 0,
            fireResist = 0,
            lightResist = 0,
            poisonResist = 0,
            physicsResistMax = 95,
            coldResistMax = 95,
            fireResistMax = 95,
            lightResistMax = 95,
            poisonResistMax = 95,
            physicsArmor = 0,
            coldArmor = 0,
            fireArmor = 0,
            lightArmor = 0,
            poisonArmor = 0,
            physicsArmorTime = 0,
            coldArmorTime = 0,
            fireArmorTime = 0,
            lightArmorTime = 0,
            poisonArmorTime = 0,
            manaShieldPercent = 0,
            manaShieldTime = 0,
            meleeDmgRet = 0,
            rangeDmgRet = 0,
            meleeDmgRetPercent = 0,
            rangeDmgRetPercent = 0,
            damage2ManaPercent = 0,
            isPlayer = false,
        };

        // Envelope thật từ backend FastAPI: parity (200-50)*(100-20)/100 = 120
        private const string EnvelopeParity = "{" +
            "\"code\":\"200\"," +
            "\"message\":\"Success\"," +
            "\"data\":{" +
              "\"damage\":120," +
              "\"manaAbsorbed\":0," +
              "\"armorAbsorbed\":50," +
              "\"manaShieldBroke\":false," +
              "\"targetDied\":false," +
              "\"reflectToAttacker\":0," +
              "\"reflectKind\":5," +
              "\"target\":{" +
                "\"life\":880,\"lifeMax\":1000,\"mana\":0,\"manaMax\":0," +
                "\"physicsResist\":20,\"coldResist\":0,\"fireResist\":0,\"lightResist\":0,\"poisonResist\":0," +
                "\"physicsResistMax\":95,\"coldResistMax\":95,\"fireResistMax\":95,\"lightResistMax\":95,\"poisonResistMax\":95," +
                "\"physicsArmor\":0,\"coldArmor\":0,\"fireArmor\":0,\"lightArmor\":0,\"poisonArmor\":0," +
                "\"physicsArmorTime\":0,\"coldArmorTime\":0,\"fireArmorTime\":0,\"lightArmorTime\":0,\"poisonArmorTime\":0," +
                "\"manaShieldPercent\":0,\"manaShieldTime\":0," +
                "\"meleeDmgRet\":0,\"rangeDmgRet\":0,\"meleeDmgRetPercent\":0,\"rangeDmgRetPercent\":0," +
                "\"damage2ManaPercent\":0,\"isPlayer\":false" +
              "}" +
            "}}";

        // -------- RestGameBackend --------

        [Test]
        public async Task Rest_BuildsPostUrlAndSendsJsonBody()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/damage/calc", 200, EnvelopeParity);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var req = new DamageCalcRequest
            {
                atkMin = 200,
                atkMax = 200,
                damageKind = 0,
                isMelee = true,
                isReturn = false,
                pkDamageRate = 100,
                target = NewTarget(life: 1000),
                attacker = null,
                seed = 1,
            };
            var resp = await backend.CalcDamageAsync(req);

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.AreEqual(120, resp.data.damage);
            Assert.AreEqual(50, resp.data.armorAbsorbed);
            Assert.IsFalse(resp.data.targetDied);
            Assert.IsNotNull(resp.data.target);
            Assert.AreEqual(880, resp.data.target.life);

            // URL đã gửi: POST tới base + /v1/combat/damage/calc
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("POST", fake.Sent[0].Method);
            Assert.AreEqual($"{BaseUrl}/v1/combat/damage/calc", fake.Sent[0].Url);

            // Body JSON: camelCase, đủ field quan trọng
            string body = fake.Sent[0].Body;
            StringAssert.Contains("\"atkMin\":200", body);
            StringAssert.Contains("\"atkMax\":200", body);
            StringAssert.Contains("\"damageKind\":0", body);
            StringAssert.Contains("\"isMelee\":true", body);
            StringAssert.Contains("\"pkDamageRate\":100", body);
            StringAssert.Contains("\"target\":", body);
            StringAssert.Contains("\"life\":1000", body);
        }

        [Test]
        public async Task Rest_NullRequest_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CalcDamageAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task Rest_NullTarget_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var req = new DamageCalcRequest
            {
                atkMin = 100, atkMax = 100, damageKind = 0,
                target = null, // intentionally null
            };
            var resp = await backend.CalcDamageAsync(req);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task Rest_HttpError500_ReturnsFailure()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/damage/calc", 500, "{\"detail\":\"internal\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CalcDamageAsync(new DamageCalcRequest
            {
                atkMin = 100, atkMax = 100, target = NewTarget(),
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task Rest_HttpError422_ReturnsFailure()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/damage/calc", 422, "{\"detail\":\"validation\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CalcDamageAsync(new DamageCalcRequest
            {
                atkMin = -1, atkMax = -1, target = NewTarget(),
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("422", resp.code);
        }

        [Test]
        public async Task Rest_TransportError_ReturnsTransportError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("POST", "/v1/combat/damage/calc", new Exception("dns fail"));
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CalcDamageAsync(new DamageCalcRequest
            {
                atkMin = 100, atkMax = 100, target = NewTarget(),
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }

        [Test]
        public async Task Rest_MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/damage/calc", 200, "not json at all");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.CalcDamageAsync(new DamageCalcRequest
            {
                atkMin = 100, atkMax = 100, target = NewTarget(),
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("parse_error", resp.code);
        }

        // -------- MockGameBackend --------

        [Test]
        public async Task Mock_ReturnsDamageAfterArmorAndResist()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var target = NewTarget();
            target.physicsArmor = 50;
            target.physicsResist = 20;

            var resp = await backend.CalcDamageAsync(new DamageCalcRequest
            {
                atkMin = 200, atkMax = 200, damageKind = 0,
                target = target,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("Mock", resp.message);
            Assert.IsNotNull(resp.data);
            // Mock: (200-50) * (100-20)/100 = 120
            Assert.AreEqual(120, resp.data.damage);
            Assert.AreEqual(50, resp.data.armorAbsorbed);
            // target bị mutate: life 1000 - 120 = 880, physicsArmor = 200-50 = 150? No
            // mock: armorLeft = max(0, 50-200) = 0
            Assert.AreEqual(880, resp.data.target.life);
            Assert.AreEqual(0, resp.data.target.physicsArmor);
        }

        [Test]
        public async Task Mock_NullRequest_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.CalcDamageAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_NullTarget_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.CalcDamageAsync(new DamageCalcRequest
            {
                atkMin = 100, atkMax = 100, target = null,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_KillsTarget_WhenLifeDropsToZero()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var target = NewTarget(life: 5, lifeMax: 1000);

            var resp = await backend.CalcDamageAsync(new DamageCalcRequest
            {
                atkMin = 200, atkMax = 200, damageKind = 0,
                target = target,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsTrue(resp.data.targetDied, "Target life <= 0 phải đánh dấu targetDied=true");
            Assert.AreEqual(0, resp.data.target.life);
        }

        // -------- Server-authoritative parity check --------

        [Test]
        public void ServerAuthorityEnforcer_DetectsMismatchedClientDamage()
        {
            // Client tính damage = 200 (sai); server thật trả 120.
            var serverResp = new DamageCalcResponse { damage = 120 };
            bool ok = ServerAuthorityEnforcer.DamageMatchesServer(200, serverResp, tolerance: 0);
            Assert.IsFalse(ok, "Mismatch 200 vs 120 phải fail tolerance=0");
        }

        [Test]
        public void ServerAuthorityEnforcer_AcceptsMatchingDamage()
        {
            var serverResp = new DamageCalcResponse { damage = 120 };
            bool ok = ServerAuthorityEnforcer.DamageMatchesServer(120, serverResp, tolerance: 0);
            Assert.IsTrue(ok);
        }

        [Test]
        public void ServerAuthorityEnforcer_AcceptsDamageWithinTolerance()
        {
            // Server dùng seed RNG khác → client damage lệch nhẹ. Tolerance=5 OK.
            var serverResp = new DamageCalcResponse { damage = 120 };
            bool ok = ServerAuthorityEnforcer.DamageMatchesServer(125, serverResp, tolerance: 5);
            Assert.IsTrue(ok);
        }
    }
}
