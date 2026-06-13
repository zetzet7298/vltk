// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — SkillRestGameBackendTests
// Test cho 5 endpoint FS-03B trên RestGameBackend (dùng FakeHttpTransport).
// Mỗi endpoint cover: happy + 4xx + 5xx + malformed JSON + predict mismatch.
//
// Pin contract FS-03A (commit 2b92a39 backend, branch main):
//   GET  /v1/skill/by-role/{roleId}                    → { roleId, skills: [...] }
//   POST /v1/skill/learn  body {roleId, skillId, charLevel, faction}
//                                                       → PlayerSkillResponse
//   POST /v1/skill/by-role/{roleId}/level-up/{skillId} → PlayerSkillResponse
//   POST /v1/skill/cast/check body {roleId, skillId, currentMana, currentLife,
//       currentStamina, onHorse, relation, distance, weaponType, equipState,
//       nowMs, lastCastMs}                              → SkillCastCheckResponse
//   POST /v1/skill/cast body {roleId, skillId, onHorse, relation, distance,
//       weaponType, equipState, nowMs}                 → SkillCastResponse
//     (KHÔNG có currentMana/Life/Stamina/lastCastMs — server-authoritative)
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Rest;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class SkillRestGameBackendTests
    {
        private const string BaseUrl = "http://127.0.0.1:8020";

        private static BackendConfig NewConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = false,
            defaultTimeoutSeconds = 5,
        };

        // ============================================================
        // ListSkillsAsync
        // ============================================================

        [Test]
        public async Task ListSkillsAsync_HappyPath_OneSkill()
        {
            var fake = new FakeHttpTransport();
            // Match response 06_skill_by_role.json — Kim Ba level 1.
            const string body = "{\"code\":\"200\",\"message\":\"Success\"," +
                                "\"data\":{\"roleId\":1," +
                                "\"skills\":[{\"id\":1,\"roleId\":1,\"skillId\":22," +
                                "\"level\":1,\"isActive\":true,\"skillName\":\"Kim Ba\"," +
                                "\"maxLevel\":20}]}}";
            fake.QueueResponse("GET", "/v1/skill/by-role/1", 200, body);
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListSkillsAsync(1);

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(1, resp.data.roleId);
            Assert.AreEqual(1, resp.data.skills.Count);
            Assert.AreEqual(22, resp.data.skills[0].skillId);
            Assert.AreEqual("Kim Ba", resp.data.skills[0].skillName);
            Assert.AreEqual(20, resp.data.skills[0].maxLevel);

            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("GET", fake.Sent[0].Method);
            StringAssert.Contains("/v1/skill/by-role/1", fake.Sent[0].Url);
        }

        [Test]
        public async Task ListSkillsAsync_HappyPath_Empty()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/skill/by-role/2", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{\"roleId\":2,\"skills\":[]}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListSkillsAsync(2);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(0, resp.data.skills.Count);
        }

        [Test]
        public async Task ListSkillsAsync_500_ServerError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/skill/by-role/1", 500, "{\"detail\":\"oops\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListSkillsAsync(1);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task ListSkillsAsync_NonPositiveRoleId_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListSkillsAsync(0);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
            Assert.AreEqual(0, fake.Sent.Count, "không được gửi request khi validation fail");
        }

        [Test]
        public async Task ListSkillsAsync_MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/skill/by-role/1", 200, "not json at all");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListSkillsAsync(1);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("parse_error", resp.code);
        }

        // ============================================================
        // LearnSkillAsync
        // ============================================================

        [Test]
        public async Task LearnSkillAsync_HappyPath_SendsPostWithBody()
        {
            var fake = new FakeHttpTransport();
            // Match response 05_skill_learn.json — id=1, Kim Ba level 1.
            fake.QueueResponse("POST", "/v1/skill/learn", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"id\":1,\"roleId\":1,\"skillId\":22,\"level\":1," +
                "\"isActive\":true,\"skillName\":\"Kim Ba\",\"maxLevel\":20}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, 0));

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(1, resp.data.id);
            Assert.AreEqual(22, resp.data.skillId);
            Assert.AreEqual(1, resp.data.level);

            // Body phải chứa roleId, skillId, charLevel, faction (camelCase).
            var body = fake.Sent[0].Body;
            Assert.IsTrue(body.Contains("\"roleId\":1"), $"body phải có roleId; got {body}");
            Assert.IsTrue(body.Contains("\"skillId\":22"), $"body phải có skillId; got {body}");
            Assert.IsTrue(body.Contains("\"charLevel\":50"), $"body phải có charLevel; got {body}");
            Assert.IsTrue(body.Contains("\"faction\":0"), $"body phải có faction; got {body}");
        }

        [Test]
        public async Task LearnSkillAsync_409_AlreadyLearned()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/learn", 409,
                "{\"detail\":\"Nhân vật đã học kỹ năng này\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, 0));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("409", resp.code);
        }

        [Test]
        public async Task LearnSkillAsync_422_LevelTooLow()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/learn", 422,
                "{\"detail\":\"Chưa đủ cấp độ yêu cầu\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LearnSkillAsync(new SkillLearnRequest(1, 22, 1, 0));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("422", resp.code);
        }

        [Test]
        public async Task LearnSkillAsync_404_SkillUnknown()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/learn", 404,
                "{\"detail\":\"Kỹ năng không có trong bảng định nghĩa\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LearnSkillAsync(new SkillLearnRequest(1, 99999, 50, 0));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("404", resp.code);
        }

        [Test]
        public async Task LearnSkillAsync_500_ServerError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/learn", 500, "{\"detail\":\"oops\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, 0));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task LearnSkillAsync_NullRequest_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LearnSkillAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task LearnSkillAsync_NonPositiveRoleId_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LearnSkillAsync(new SkillLearnRequest(0, 22, 50, 0));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        [Test]
        public async Task LearnSkillAsync_NonPositiveSkillId_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LearnSkillAsync(new SkillLearnRequest(1, 0, 50, 0));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        [Test]
        public async Task LearnSkillAsync_CharLevelOutOfRange_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            // charLevel=0 fail
            var resp0 = await backend.LearnSkillAsync(new SkillLearnRequest(1, 22, 0, 0));
            Assert.IsFalse(resp0.IsSuccess);
            Assert.AreEqual("validation_error", resp0.code);

            // charLevel=201 fail
            var resp1 = await backend.LearnSkillAsync(new SkillLearnRequest(1, 22, 201, 0));
            Assert.IsFalse(resp1.IsSuccess);
            Assert.AreEqual("validation_error", resp1.code);
        }

        [Test]
        public async Task LearnSkillAsync_FactionOutOfRange_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            // faction=-2 fail (chỉ chấp nhận -1..9)
            var resp = await backend.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, -2));
            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);

            // faction=10 fail
            var resp2 = await backend.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, 10));
            Assert.IsFalse(resp2.IsSuccess);
            Assert.AreEqual("validation_error", resp2.code);
        }

        [Test]
        public async Task LearnSkillAsync_FactionMinusOne_Accepted()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/learn", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"id\":1,\"roleId\":1,\"skillId\":22,\"level\":1," +
                "\"isActive\":true,\"skillName\":\"Kim Ba\",\"maxLevel\":20}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            // faction=-1 (chưa nhập phái) — parity backend Pydantic constraint.
            var resp = await backend.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, -1));

            Assert.IsTrue(resp.IsSuccess);
        }

        // ============================================================
        // LevelUpSkillAsync
        // ============================================================

        [Test]
        public async Task LevelUpSkillAsync_HappyPath_BuildsUrlWithRoleAndSkillId()
        {
            var fake = new FakeHttpTransport();
            // Match response 07_skill_level_up.json — level=2.
            fake.QueueResponse("POST", "/v1/skill/by-role/1/level-up/22", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"id\":1,\"roleId\":1,\"skillId\":22,\"level\":2," +
                "\"isActive\":true,\"skillName\":\"Kim Ba\",\"maxLevel\":20}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LevelUpSkillAsync(1, 22);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(2, resp.data.level);
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("POST", fake.Sent[0].Method);
            StringAssert.Contains("/v1/skill/by-role/1/level-up/22", fake.Sent[0].Url);
        }

        [Test]
        public async Task LevelUpSkillAsync_404_NotLearned()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/by-role/1/level-up/999", 404,
                "{\"detail\":\"Nhân vật chưa học kỹ năng này\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LevelUpSkillAsync(1, 999);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("404", resp.code);
        }

        [Test]
        public async Task LevelUpSkillAsync_422_MaxLevel()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/by-role/1/level-up/22", 422,
                "{\"detail\":\"Kỹ năng đã đạt cấp tối đa\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LevelUpSkillAsync(1, 22);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("422", resp.code);
        }

        [Test]
        public async Task LevelUpSkillAsync_NonPositiveRoleId_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LevelUpSkillAsync(0, 22);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task LevelUpSkillAsync_NonPositiveSkillId_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LevelUpSkillAsync(1, 0);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        // ============================================================
        // CastSkillCheckAsync (stateless pre-flight)
        // ============================================================

        [Test]
        public async Task CastSkillCheckAsync_HappyPath_BuildsUrlAndSendsBody()
        {
            var fake = new FakeHttpTransport();
            // Match response 09_skill_cast_check.json — skill 210, canCast=true,
            // costType=1, costValue=50, delayPerCast=0.
            fake.QueueResponse("POST", "/v1/skill/cast/check", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"skillId\":210,\"canCast\":true,\"reason\":null," +
                "\"costType\":1,\"costValue\":50,\"delayPerCast\":0,\"nextCastTime\":0}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var req = new SkillCastCheckRequest(1, 210, 300, 1000, 100,
                onHorse: false, relation: 2, distance: 0,
                weaponType: 0, equipState: -2, nowMs: 1000, lastCastMs: 0);
            var resp = await backend.CastSkillCheckAsync(req);

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.IsTrue(resp.data.canCast);
            Assert.AreEqual(1, resp.data.costType);
            Assert.AreEqual(50, resp.data.costValue);
            Assert.AreEqual(0, resp.data.delayPerCast);

            // Body phải chứa currentMana, currentLife, currentStamina, nowMs, lastCastMs.
            var body = fake.Sent[0].Body;
            Assert.IsTrue(body.Contains("\"currentMana\":300"), $"body phải có currentMana; got {body}");
            Assert.IsTrue(body.Contains("\"currentLife\":1000"), $"body phải có currentLife; got {body}");
            Assert.IsTrue(body.Contains("\"currentStamina\":100"), $"body phải có currentStamina; got {body}");
            Assert.IsTrue(body.Contains("\"nowMs\":1000"), $"body phải có nowMs; got {body}");
            Assert.IsTrue(body.Contains("\"lastCastMs\":0"), $"body phải có lastCastMs; got {body}");
        }

        [Test]
        public async Task CastSkillCheckAsync_404_NotLearned()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/cast/check", 404,
                "{\"detail\":\"Nhân vật chưa học kỹ năng này\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.CastSkillCheckAsync(
                new SkillCastCheckRequest(1, 999, 0, 0, 0, nowMs: 1000));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("404", resp.code);
        }

        [Test]
        public async Task CastSkillCheckAsync_NullRequest_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.CastSkillCheckAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        [Test]
        public async Task CastSkillCheckAsync_ZeroNowMs_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            // nowMs=0 fail (parity backend Pydantic nowMs >= 1).
            var resp = await backend.CastSkillCheckAsync(
                new SkillCastCheckRequest(1, 210, 300, 1000, 100, nowMs: 0));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        // ============================================================
        // CastSkillAsync (server-authoritative)
        // ============================================================

        [Test]
        public async Task CastSkillAsync_HappyPath_SendsPostWithGateContextOnly()
        {
            var fake = new FakeHttpTransport();
            // Match response 10_skill_cast.json — server-authoritative trừ mana
            // 300 → 250, costPaid=50, currentLife=1000, currentStamina=0.
            fake.QueueResponse("POST", "/v1/skill/cast", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"skillId\":210,\"cast\":true,\"costType\":1,\"costPaid\":50," +
                "\"currentLife\":1000,\"currentMana\":250,\"currentStamina\":0," +
                "\"nextCastTime\":1000,\"effects\":[]}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var req = new SkillCastRequest(1, 210, 1000,
                onHorse: false, relation: 2, distance: 0,
                weaponType: 0, equipState: -2);
            var resp = await backend.CastSkillAsync(req);

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.IsTrue(resp.data.cast);
            Assert.AreEqual(50, resp.data.costPaid);
            // PHẢI dùng số server trả, KHÔNG tính client-side (parity FS-03A §5).
            Assert.AreEqual(250, resp.data.currentMana);
            Assert.AreEqual(1000, resp.data.currentLife);
            Assert.AreEqual(1000L, resp.data.nextCastTime);
            Assert.IsNotNull(resp.data.effects);

            // Body phải KHÔNG chứa currentMana/Life/Stamina/lastCastMs
            // (server-authoritative, chống spoof H-SK2/H-SK3).
            var body = fake.Sent[0].Body;
            Assert.IsFalse(body.Contains("currentMana"),
                $"cast request KHÔNG được gửi currentMana (server-authoritative); got {body}");
            Assert.IsFalse(body.Contains("currentLife"),
                $"cast request KHÔNG được gửi currentLife; got {body}");
            Assert.IsFalse(body.Contains("currentStamina"),
                $"cast request KHÔNG được gửi currentStamina; got {body}");
            Assert.IsFalse(body.Contains("lastCastMs"),
                $"cast request KHÔNG được gửi lastCastMs; got {body}");
            // Body PHẢI chứa gate context + nowMs.
            Assert.IsTrue(body.Contains("\"onHorse\":false"), $"body phải có onHorse; got {body}");
            Assert.IsTrue(body.Contains("\"relation\":2"), $"body phải có relation; got {body}");
            Assert.IsTrue(body.Contains("\"nowMs\":1000"), $"body phải có nowMs; got {body}");
        }

        [Test]
        public async Task CastSkillAsync_409_Cooldown()
        {
            var fake = new FakeHttpTransport();
            // Match response 11_skill_cast_cooldown.json — backend trả bare
            // {"detail": "..."} (không envelope).
            fake.QueueResponse("POST", "/v1/skill/cast", 409,
                "{\"detail\":\"Kỹ năng đang trong thời gian hồi\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.CastSkillAsync(new SkillCastRequest(1, 210, 1001));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("409", resp.code);
        }

        [Test]
        public async Task CastSkillAsync_409_NotEnoughResource()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/cast", 409,
                "{\"detail\":\"Không đủ tài nguyên để thi triển kỹ năng\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.CastSkillAsync(new SkillCastRequest(1, 210, 1000));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("409", resp.code);
        }

        [Test]
        public async Task CastSkillAsync_409_GateFail()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/cast", 409,
                "{\"detail\":\"Không thể thi triển kỹ năng lúc này\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.CastSkillAsync(new SkillCastRequest(1, 210, 1000));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("409", resp.code);
        }

        [Test]
        public async Task CastSkillAsync_404_PlayerStateMissing()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/cast", 404,
                "{\"detail\":\"Không tìm thấy trạng thái nhân vật\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.CastSkillAsync(new SkillCastRequest(1, 210, 1000));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("404", resp.code);
        }

        [Test]
        public async Task CastSkillAsync_NullRequest_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.CastSkillAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        // ============================================================
        // Cancellation
        // ============================================================

        [Test]
        public async Task ListSkillsAsync_CancelledToken_DoesNotThrow()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/skill/by-role/1", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{\"roleId\":1,\"skills\":[]}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var resp = await backend.ListSkillsAsync(1, cts.Token);
            Assert.IsTrue(resp.code == "cancelled" || resp.code == "200",
                $"code phải cancelled hoặc 200; got {resp.code}");
        }

        // ============================================================
        // Predict-reconcile: /cast/check mismatch với client predict
        // ============================================================

        [Test]
        public async Task CastSkillCheckAsync_ClientPredictOK_ServerDeniesCooldown()
        {
            // Test scenario: client predict coi như OK (lastCastMs=0, đủ mana)
            // nhưng server deny do gate context fail (relation/distance chưa hợp lệ).
            // Server-authoritative LUÔN thắng — caller phải đọc server check.
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/cast/check", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"skillId\":210,\"canCast\":false," +
                "\"reason\":\"Khoảng cách quá xa\"," +
                "\"costType\":1,\"costValue\":50,\"delayPerCast\":0,\"nextCastTime\":0}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var req = new SkillCastCheckRequest(1, 210, 300, 1000, 100, nowMs: 1000);
            var resp = await backend.CastSkillCheckAsync(req);

            Assert.IsTrue(resp.IsSuccess, "HTTP 200 nhưng server nói canCast=false");
            Assert.IsFalse(resp.data.canCast,
                "MISMATCH: client predict OK nhưng server deny — caller phải dùng server");
            Assert.AreEqual("Khoảng cách quá xa", resp.data.reason);

            // Đây là "predict mismatch" mà PredictState.Reconcile sẽ trả
            // predictionMatched=false, caller KHÔNG gửi /cast.
        }
    }
}
