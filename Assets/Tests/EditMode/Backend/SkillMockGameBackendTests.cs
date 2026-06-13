// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — SkillMockGameBackendTests
// Test cho 5 endpoint FS-03B trên MockGameBackend. Mỗi endpoint cover happy
// + validation (input fail trước khi trả response). Mock KHÔNG gọi network,
// KHÔNG cần FakeHttpTransport.
// -----------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Mock;

namespace VLTK.Tests.Backend
{
    public class SkillMockGameBackendTests
    {
        private const string BaseUrl = "http://127.0.0.1:8020";

        private static BackendConfig NewConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = true,
            defaultTimeoutSeconds = 5,
        };

        // ============================================================
        // ListSkillsAsync
        // ============================================================

        [Test]
        public async Task ListSkillsAsync_ReturnsMockSeedSkill()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.ListSkillsAsync(1);

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code}");
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(1, resp.data.roleId);
            Assert.AreEqual(1, resp.data.skills.Count);
            // Mock seeded Kim Ba (skillId=22) parity FS-03A evidence 06_skill_by_role.json.
            Assert.AreEqual(22, resp.data.skills[0].skillId);
            Assert.AreEqual("Kim Ba", resp.data.skills[0].skillName);
            Assert.AreEqual(20, resp.data.skills[0].maxLevel);
        }

        [Test]
        public async Task ListSkillsAsync_NonPositiveRoleId_ValidationError()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.ListSkillsAsync(0);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        [Test]
        public async Task ListSkillsAsync_Code200AndMessageMock()
        {
            // Mock: code="200", message="Mock" để phân biệt với response thật.
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.ListSkillsAsync(1);

            Assert.AreEqual("200", resp.code);
            Assert.AreEqual("Mock", resp.message);
        }

        // ============================================================
        // LearnSkillAsync
        // ============================================================

        [Test]
        public async Task LearnSkillAsync_ReturnsLevelOnePlayerSkill()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, 0));

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(1, resp.data.roleId);
            Assert.AreEqual(22, resp.data.skillId);
            Assert.AreEqual(1, resp.data.level, "Mock: level=1");
            Assert.IsTrue(resp.data.isActive);
        }

        [Test]
        public async Task LearnSkillAsync_NullRequest_ValidationError()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.LearnSkillAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        [Test]
        public async Task LearnSkillAsync_InvalidInputs_ValidationError()
        {
            var mock = new MockGameBackend(NewConfig());
            Assert.AreEqual("validation_error",
                (await mock.LearnSkillAsync(new SkillLearnRequest(0, 22, 50, 0))).code);
            Assert.AreEqual("validation_error",
                (await mock.LearnSkillAsync(new SkillLearnRequest(1, 0, 50, 0))).code);
            Assert.AreEqual("validation_error",
                (await mock.LearnSkillAsync(new SkillLearnRequest(1, 22, 0, 0))).code);
            Assert.AreEqual("validation_error",
                (await mock.LearnSkillAsync(new SkillLearnRequest(1, 22, 201, 0))).code);
            Assert.AreEqual("validation_error",
                (await mock.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, -2))).code);
            Assert.AreEqual("validation_error",
                (await mock.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, 10))).code);
        }

        [Test]
        public async Task LearnSkillAsync_FactionMinusOne_Accepted()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, -1));
            Assert.IsTrue(resp.IsSuccess);
        }

        // ============================================================
        // LevelUpSkillAsync
        // ============================================================

        [Test]
        public async Task LevelUpSkillAsync_ReturnsLevelTwoPlayerSkill()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.LevelUpSkillAsync(1, 22);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(1, resp.data.roleId);
            Assert.AreEqual(22, resp.data.skillId);
            Assert.AreEqual(2, resp.data.level, "Mock: level=2 (parity FS-03A evidence 07)");
        }

        [Test]
        public async Task LevelUpSkillAsync_InvalidInputs_ValidationError()
        {
            var mock = new MockGameBackend(NewConfig());
            Assert.AreEqual("validation_error",
                (await mock.LevelUpSkillAsync(0, 22)).code);
            Assert.AreEqual("validation_error",
                (await mock.LevelUpSkillAsync(1, 0)).code);
        }

        // ============================================================
        // CastSkillCheckAsync
        // ============================================================

        [Test]
        public async Task CastSkillCheckAsync_Skill210_Mana50CanCast()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.CastSkillCheckAsync(
                new SkillCastCheckRequest(1, 210, 300, 1000, 100, nowMs: 1000));

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.IsTrue(resp.data.canCast);
            Assert.AreEqual(210, resp.data.skillId);
            // Parity FS-03A evidence 09_skill_cast_check.json.
            Assert.AreEqual(1, resp.data.costType, "skill 210: costType=1 (mana)");
            Assert.AreEqual(50, resp.data.costValue);
            Assert.AreEqual(0, resp.data.delayPerCast);
        }

        [Test]
        public async Task CastSkillCheckAsync_Skill22_NoCost()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.CastSkillCheckAsync(
                new SkillCastCheckRequest(1, 22, 300, 1000, 100, nowMs: 1000));

            Assert.IsTrue(resp.IsSuccess);
            // Kim Ba: costType=0, costValue=0 (parity FS-03A §3 "Skill test data").
            Assert.AreEqual(0, resp.data.costType);
            Assert.AreEqual(0, resp.data.costValue);
        }

        [Test]
        public async Task CastSkillCheckAsync_NullRequest_ValidationError()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.CastSkillCheckAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        [Test]
        public async Task CastSkillCheckAsync_InvalidInputs_ValidationError()
        {
            var mock = new MockGameBackend(NewConfig());
            Assert.AreEqual("validation_error",
                (await mock.CastSkillCheckAsync(
                    new SkillCastCheckRequest(0, 210, 300, 1000, 100, nowMs: 1000))).code);
            Assert.AreEqual("validation_error",
                (await mock.CastSkillCheckAsync(
                    new SkillCastCheckRequest(1, 0, 300, 1000, 100, nowMs: 1000))).code);
            Assert.AreEqual("validation_error",
                (await mock.CastSkillCheckAsync(
                    new SkillCastCheckRequest(1, 210, 300, 1000, 100, nowMs: 0))).code);
        }

        // ============================================================
        // CastSkillAsync
        // ============================================================

        [Test]
        public async Task CastSkillAsync_ReturnsServerAuthoritativeResourceDelta()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.CastSkillAsync(new SkillCastRequest(1, 210, 1000));

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.IsTrue(resp.data.cast);
            Assert.AreEqual(210, resp.data.skillId);
            // Parity FS-03A evidence 10_skill_cast.json: server trừ mana 300→250.
            Assert.AreEqual(1, resp.data.costType);
            Assert.AreEqual(50, resp.data.costPaid);
            Assert.AreEqual(1000, resp.data.currentLife);
            Assert.AreEqual(250, resp.data.currentMana,
                "server-authoritative currentMana SAU cast");
            Assert.AreEqual(0, resp.data.currentStamina);
            Assert.AreEqual(1000L, resp.data.nextCastTime);
            Assert.IsNotNull(resp.data.effects);
        }

        [Test]
        public async Task CastSkillAsync_NullRequest_ValidationError()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.CastSkillAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        [Test]
        public async Task CastSkillAsync_InvalidInputs_ValidationError()
        {
            var mock = new MockGameBackend(NewConfig());
            Assert.AreEqual("validation_error",
                (await mock.CastSkillAsync(new SkillCastRequest(0, 210, 1000))).code);
            Assert.AreEqual("validation_error",
                (await mock.CastSkillAsync(new SkillCastRequest(1, 0, 1000))).code);
            Assert.AreEqual("validation_error",
                (await mock.CastSkillAsync(new SkillCastRequest(1, 210, 0))).code);
        }

        [Test]
        public async Task CastSkillAsync_Code200AndMessageMock()
        {
            var mock = new MockGameBackend(NewConfig());
            var resp = await mock.CastSkillAsync(new SkillCastRequest(1, 210, 1000));

            Assert.AreEqual("200", resp.code);
            Assert.AreEqual("Mock", resp.message);
        }
    }
}
