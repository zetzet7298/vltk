// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — ServerAuthorityEnforcerTests
// EditMode test cho ServerAuthorityEnforcer — enforcer bắt buộc mọi quyết
// định combat phải gọi server trước; KHÔNG tin damage/client tính local.
//
// Phủ:
//   - CalcDamageOrThrowAsync: success / fail / null request / null backend
//   - StatusTickOrThrowAsync: success / fail / null request
//   - CheckPkOrThrowAsync: success / canAttack=false / null request
//   - ApplyServerState: replace toàn bộ field; raise nếu server null
//   - ApplyServerStatus: replace toàn bộ state node
//   - DamageMatchesServer: parity check (0 tolerance)
//   - Client-vs-server damage detection: mismatch → log warn
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Mock;
using VLTK.Backend.Rest;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class ServerAuthorityEnforcerTests
    {
        private const string BaseUrl = "http://127.0.0.1:8020";

        private static BackendConfig NewRestConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = false,
            defaultTimeoutSeconds = 5,
        };

        private static CombatantState NewTarget(int life = 1000) => new CombatantState
        {
            life = life,
            lifeMax = 1000,
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

        private const string EnvelopeOk = "{" +
            "\"code\":\"200\"," +
            "\"message\":\"Success\"," +
            "\"data\":{" +
              "\"damage\":120,\"manaAbsorbed\":0,\"armorAbsorbed\":0," +
              "\"manaShieldBroke\":false,\"targetDied\":false," +
              "\"reflectToAttacker\":0,\"reflectKind\":5," +
              "\"target\":{\"life\":880,\"lifeMax\":1000,\"mana\":0,\"manaMax\":0," +
                "\"physicsResist\":0,\"coldResist\":0,\"fireResist\":0,\"lightResist\":0,\"poisonResist\":0," +
                "\"physicsResistMax\":95,\"coldResistMax\":95,\"fireResistMax\":95,\"lightResistMax\":95,\"poisonResistMax\":95," +
                "\"physicsArmor\":0,\"coldArmor\":0,\"fireArmor\":0,\"lightArmor\":0,\"poisonArmor\":0," +
                "\"physicsArmorTime\":0,\"coldArmorTime\":0,\"fireArmorTime\":0,\"lightArmorTime\":0,\"poisonArmorTime\":0," +
                "\"manaShieldPercent\":0,\"manaShieldTime\":0," +
                "\"meleeDmgRet\":0,\"rangeDmgRet\":0,\"meleeDmgRetPercent\":0,\"rangeDmgRetPercent\":0," +
                "\"damage2ManaPercent\":0,\"isPlayer\":false}" +
            "}}";

        // -------- CalcDamageOrThrowAsync --------

        [Test]
        public async Task CalcDamage_Success_ReturnsServerResponse()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/damage/calc", 200, EnvelopeOk);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await ServerAuthorityEnforcer.CalcDamageOrThrowAsync(backend,
                new DamageCalcRequest
                {
                    atkMin = 200, atkMax = 200,
                    target = NewTarget(),
                });

            Assert.AreEqual(120, resp.damage);
            Assert.AreEqual(880, resp.target.life);
        }

        [Test]
        public void CalcDamage_ServerHttpError_ThrowsServerAuthorityException()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/damage/calc", 500, "{\"detail\":\"db down\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var ex = Assert.ThrowsAsync<ServerAuthorityException>(async () =>
                await ServerAuthorityEnforcer.CalcDamageOrThrowAsync(backend,
                    new DamageCalcRequest { atkMin = 100, atkMax = 100, target = NewTarget() }));

            Assert.AreEqual("500", ex.AuthorityCode);
        }

        [Test]
        public void CalcDamage_NullRequest_ThrowsInvalidArg()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var ex = Assert.ThrowsAsync<ServerAuthorityException>(async () =>
                await ServerAuthorityEnforcer.CalcDamageOrThrowAsync(backend, null));

            Assert.AreEqual("invalid_arg", ex.AuthorityCode);
        }

        [Test]
        public void CalcDamage_NullBackend_ThrowsNoBackend()
        {
            var ex = Assert.ThrowsAsync<ServerAuthorityException>(async () =>
                await ServerAuthorityEnforcer.CalcDamageOrThrowAsync(null,
                    new DamageCalcRequest { atkMin = 100, atkMax = 100, target = NewTarget() }));

            Assert.AreEqual("no_backend", ex.AuthorityCode);
        }

        // -------- StatusTickOrThrowAsync --------

        [Test]
        public async Task StatusTick_Success_ReturnsServerResponse()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/status/tick", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{" +
                "\"controlled\":false,\"confuseEnded\":false," +
                "\"dotResults\":[],\"auraCastSkillId\":0,\"auraCastLevel\":0," +
                "\"target\":{\"life\":1000,\"lifeMax\":1000,\"mana\":0,\"manaMax\":0," +
                  "\"physicsResist\":0,\"coldResist\":0,\"fireResist\":0,\"lightResist\":0,\"poisonResist\":0," +
                  "\"physicsResistMax\":95,\"coldResistMax\":95,\"fireResistMax\":95,\"lightResistMax\":95,\"poisonResistMax\":95," +
                  "\"physicsArmor\":0,\"coldArmor\":0,\"fireArmor\":0,\"lightArmor\":0,\"poisonArmor\":0," +
                  "\"physicsArmorTime\":0,\"coldArmorTime\":0,\"fireArmorTime\":0,\"lightArmorTime\":0,\"poisonArmorTime\":0," +
                  "\"manaShieldPercent\":0,\"manaShieldTime\":0," +
                  "\"meleeDmgRet\":0,\"rangeDmgRet\":0,\"meleeDmgRetPercent\":0,\"rangeDmgRetPercent\":0," +
                  "\"damage2ManaPercent\":0,\"isPlayer\":false}," +
                "\"status\":{\"poisonState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                  "\"freezeState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                  "\"burnState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                  "\"confuseState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                  "\"stunState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                  "\"lifeState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                  "\"manaState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}," +
                  "\"drunkState\":{\"value0\":0,\"value1\":0,\"value2\":0,\"time\":0}}}}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var req = new StatusTickRequest
            {
                target = NewTarget(),
                status = new StatusBundle(),
            };
            var resp = await ServerAuthorityEnforcer.StatusTickOrThrowAsync(backend, req);

            Assert.IsFalse(resp.controlled);
            Assert.IsNotNull(resp.target);
        }

        [Test]
        public void StatusTick_ServerError_ThrowsServerAuthorityException()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/status/tick", 500, "{\"detail\":\"db down\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var ex = Assert.ThrowsAsync<ServerAuthorityException>(async () =>
                await ServerAuthorityEnforcer.StatusTickOrThrowAsync(backend,
                    new StatusTickRequest { target = NewTarget(), status = new StatusBundle() }));

            Assert.AreEqual("500", ex.AuthorityCode);
        }

        // -------- CheckPkOrThrowAsync --------

        [Test]
        public async Task CheckPk_Success_ReturnsServerResponse()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/pk/check", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{" +
                "\"canAttack\":true,\"mapPkAllowed\":true,\"isSafeZone\":false,\"reason\":null}}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await ServerAuthorityEnforcer.CheckPkOrThrowAsync(backend,
                new PkCheckRequest
                {
                    attackerCamp = 1, targetCamp = 2,
                    mapType = "Battlefield", inBattle = true,
                });

            Assert.IsTrue(resp.canAttack);
        }

        [Test]
        public async Task CheckPk_CanAttackFalse_DoesNotThrow_ButReturnsResponse()
        {
            // Server cho biết KHÔNG được đánh → enforcer KHÔNG throw, trả về
            // response để caller hiện thông báo.
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/pk/check", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{" +
                "\"canAttack\":false,\"mapPkAllowed\":false,\"isSafeZone\":true," +
                "\"reason\":\"Vùng an toàn — cấm PK\"}}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await ServerAuthorityEnforcer.CheckPkOrThrowAsync(backend,
                new PkCheckRequest
                {
                    attackerCamp = 1, targetCamp = 2,
                    mapType = "City", inBattle = false,
                });

            Assert.IsFalse(resp.canAttack);
            Assert.IsTrue(resp.isSafeZone);
        }

        [Test]
        public void CheckPk_ServerError_ThrowsServerAuthorityException()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/pk/check", 500, "{\"detail\":\"db down\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var ex = Assert.ThrowsAsync<ServerAuthorityException>(async () =>
                await ServerAuthorityEnforcer.CheckPkOrThrowAsync(backend,
                    new PkCheckRequest
                    {
                        attackerCamp = 1, targetCamp = 2,
                        mapType = "City", inBattle = false,
                    }));

            Assert.AreEqual("500", ex.AuthorityCode);
        }

        // -------- ApplyServerState --------

        [Test]
        public void ApplyServerState_ReplacesAllFields()
        {
            var local = NewTarget();
            local.life = 500;
            local.physicsResist = 50;
            local.isPlayer = true;

            var server = NewTarget();
            server.life = 880;
            server.physicsResist = 20;
            server.isPlayer = false;

            ServerAuthorityEnforcer.ApplyServerState(local, server);

            Assert.AreEqual(880, local.life);
            Assert.AreEqual(20, local.physicsResist);
            Assert.IsFalse(local.isPlayer);
        }

        [Test]
        public void ApplyServerState_NullServer_Throws()
        {
            var local = NewTarget();
            var ex = Assert.Throws<ServerAuthorityException>(() =>
                ServerAuthorityEnforcer.ApplyServerState(local, null));
            Assert.AreEqual("empty_target", ex.AuthorityCode);
        }

        [Test]
        public void ApplyServerState_NullLocal_ReturnsServer()
        {
            var server = NewTarget();
            var result = ServerAuthorityEnforcer.ApplyServerState(null, server);
            Assert.AreSame(server, result);
        }

        // -------- ApplyServerStatus --------

        [Test]
        public void ApplyServerStatus_ReplacesAllStateNodes()
        {
            var local = new StatusBundle
            {
                poisonState = new StateNode { time = 5 },
            };
            var server = new StatusBundle
            {
                poisonState = new StateNode { value0 = 15, value1 = 2, time = 3 },
                freezeState = new StateNode { time = 1 },
            };

            var result = ServerAuthorityEnforcer.ApplyServerStatus(local, server);

            Assert.AreSame(server, result);
            Assert.AreEqual(3, local.poisonState.time);
            Assert.AreEqual(15, local.poisonState.value0);
            Assert.AreEqual(1, local.freezeState.time);
        }

        [Test]
        public void ApplyServerStatus_NullServer_Throws()
        {
            var local = new StatusBundle();
            var ex = Assert.Throws<ServerAuthorityException>(() =>
                ServerAuthorityEnforcer.ApplyServerStatus(local, null));
            Assert.AreEqual("empty_status", ex.AuthorityCode);
        }

        // -------- DamageMatchesServer --------

        [Test]
        public void DamageMatchesServer_Tolerance0_ExactMatch()
        {
            var resp = new DamageCalcResponse { damage = 120 };
            Assert.IsTrue(ServerAuthorityEnforcer.DamageMatchesServer(120, resp, 0));
            Assert.IsFalse(ServerAuthorityEnforcer.DamageMatchesServer(125, resp, 0));
        }

        [Test]
        public void DamageMatchesServer_Tolerance5_AcceptsOffByOne()
        {
            var resp = new DamageCalcResponse { damage = 120 };
            Assert.IsTrue(ServerAuthorityEnforcer.DamageMatchesServer(125, resp, 5));
            Assert.IsFalse(ServerAuthorityEnforcer.DamageMatchesServer(126, resp, 5));
        }

        [Test]
        public void DamageMatchesServer_NullServer_AlwaysFalse()
        {
            // Server null → không có gì để so sánh → false
            Assert.IsFalse(ServerAuthorityEnforcer.DamageMatchesServer(120, null, 0));
        }

        // -------- Mismatched client-vs-server detection (FS-03C acceptance) --------

        [Test]
        public async Task ClientDamageThatDiffersFromServer_IsRejected()
        {
            // Server trả damage = 120. Client tự tính = 999 (sai).
            // Enforcer KHÔNG trả client damage — nó trả server damage.
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/combat/damage/calc", 200, EnvelopeOk);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            int clientComputedDamage = 999; // client tính sai
            var resp = await ServerAuthorityEnforcer.CalcDamageOrThrowAsync(backend,
                new DamageCalcRequest { atkMin = 200, atkMax = 200, target = NewTarget() });

            // Authority: dùng server damage, KHÔNG dùng client damage
            int damageToApply = resp.damage;
            Assert.AreEqual(120, damageToApply, "Phải dùng server damage (120), KHÔNG dùng client (999)");
            Assert.IsFalse(ServerAuthorityEnforcer.DamageMatchesServer(clientComputedDamage, resp, 0),
                "Mismatch phải fail parity check");
        }
    }
}
