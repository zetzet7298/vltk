// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho Network protocol (NetworkMessageTypes +
// MessageRouter + OpCodes). Vietnamese test descriptions.
// -----------------------------------------------------------------------------

using System;
using NUnit.Framework;
using UnityEngine;
using VLTK.Network;

namespace VLTK.Tests.Sandbox
{
    public class NetworkProtocolTests
    {
        [SetUp]
        public void Reset()
        {
            // Test isolation: clear registry trước mỗi test
            MessageRouter.Clear();
        }

        [Test]
        public void OpCodes_PlayerPosition_Equals_1001()
        {
            Assert.AreEqual((ushort)1001, OpCodes.PlayerPosition,
                "OpCode PlayerPosition phải là 1001 theo đặc tả");
        }

        [Test]
        public void OpCodes_SkillCast_Equals_3001()
        {
            Assert.AreEqual((ushort)3001, OpCodes.SkillCast,
                "OpCode SkillCast phải là 3001 theo đặc tả");
        }

        [Test]
        public void OpCodes_AllHaveUniqueValues()
        {
            // Lấy tất cả const ushort qua reflection để đảm bảo không trùng
            var fields = typeof(OpCodes).GetFields(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static);
            var seen = new System.Collections.Generic.HashSet<ushort>();
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(ushort)) continue;
                ushort v = (ushort)f.GetValue(null);
                Assert.IsTrue(seen.Add(v),
                    $"OpCodes.{f.Name}={v} bị trùng với opCode khác");
            }
            Assert.Greater(seen.Count, 40, "Phải có ít nhất 40 OpCode duy nhất");
        }

        [Test]
        public void MessageRouter_Register_StoresType()
        {
            MessageRouter.Register(OpCodes.SkillCast, typeof(SkillCastMsg));
            Assert.AreEqual(1, MessageRouter.RegisteredOpCodes,
                "Sau khi Register 1 opCode, RegisteredOpCodes phải bằng 1");
        }

        [Test]
        public void MessageRouter_GetMessageType_ReturnsRegistered()
        {
            MessageRouter.Register(OpCodes.Damage, typeof(DamageMsg));
            var t = MessageRouter.GetMessageType(OpCodes.Damage);
            Assert.AreEqual(typeof(DamageMsg), t,
                "GetMessageType phải trả về Type đã đăng ký");
        }

        [Test]
        public void MessageRouter_GetUnregistered_ReturnsNull()
        {
            var t = MessageRouter.GetMessageType(OpCodes.BossSpawn);
            Assert.IsNull(t, "opCode chưa đăng ký phải trả về null");
        }

        [Test]
        public void MessageRouter_Unregister_RemovesType()
        {
            MessageRouter.Register(OpCodes.Heal, typeof(HealMsg));
            Assert.AreEqual(1, MessageRouter.RegisteredOpCodes);
            bool removed = MessageRouter.Unregister(OpCodes.Heal);
            Assert.IsTrue(removed, "Unregister phải trả về true khi opCode tồn tại");
            Assert.AreEqual(0, MessageRouter.RegisteredOpCodes);
            Assert.IsNull(MessageRouter.GetMessageType(OpCodes.Heal));
        }

        [Test]
        public void MessageRouter_Unregister_Unknown_ReturnsFalse()
        {
            bool removed = MessageRouter.Unregister((ushort)60000);
            Assert.IsFalse(removed, "Unregister opCode chưa đăng ký phải trả về false");
        }

        [Test]
        public void MessageRouter_Count_MatchesRegistered()
        {
            Assert.AreEqual(0, MessageRouter.RegisteredOpCodes);
            MessageRouter.Register(OpCodes.Chat, typeof(ChatChannelMsg));
            MessageRouter.Register(OpCodes.ChatEmote, typeof(ChatEmoteMsg));
            MessageRouter.Register(OpCodes.MapChange, typeof(MapChangeMsg));
            Assert.AreEqual(3, MessageRouter.RegisteredOpCodes,
                "Count phải khớp với số opCode đã register");
        }

        [Test]
        public void MessageRouter_RegisterDefaults_PopulatesAll()
        {
            MessageRouter.RegisterDefaults();
            // 40+ messages
            Assert.GreaterOrEqual(MessageRouter.RegisteredOpCodes, 40,
                "RegisterDefaults phải đăng ký ít nhất 40 opCode");
            // Verify một số spot-check
            Assert.AreEqual(typeof(SkillCastMsg), MessageRouter.GetMessageType(OpCodes.SkillCast));
            Assert.AreEqual(typeof(BossSpawnMsg), MessageRouter.GetMessageType(OpCodes.BossSpawn));
            Assert.AreEqual(typeof(PlayerStateMsg), MessageRouter.GetMessageType(OpCodes.PlayerState));
        }

        [Test]
        public void MessageRouter_Register_OverridesExisting()
        {
            MessageRouter.Register(OpCodes.SkillCast, typeof(SkillCastMsg));
            MessageRouter.Register(OpCodes.SkillCast, typeof(DamageMsg)); // override
            Assert.AreEqual(typeof(DamageMsg),
                MessageRouter.GetMessageType(OpCodes.SkillCast),
                "Register cùng opCode phải override Type cũ");
        }

        [Test]
        public void MessageRouter_RegisterNull_DoesNothing()
        {
            MessageRouter.Register(OpCodes.Damage, null);
            Assert.AreEqual(0, MessageRouter.RegisteredOpCodes,
                "Register với null Type không được tính");
        }

        [Test]
        public void PlayerStateMsg_SerializesToJson_NonEmpty()
        {
            var msg = new PlayerStateMsg
            {
                playerId = 42,
                hp = 1000,
                mp = 500,
                stamina = 100,
                level = 50,
                exp = 99999,
                state = 1,
                x = 12.5f,
                y = 0f,
                z = -7.25f,
                direction = 4,
            };
            string json = JsonUtility.ToJson(msg);
            Assert.IsNotNull(json, "JsonUtility.ToJson phải trả về chuỗi");
            Assert.IsNotEmpty(json, "JSON phải non-empty");
            StringAssert.Contains("\"playerId\":42", json);
            StringAssert.Contains("\"hp\":1000", json);
        }

        [Test]
        public void SkillCastMsg_DeserializesFromJson_MatchesInput()
        {
            var src = new SkillCastMsg
            {
                casterId = 7,
                skillId = 1539,
                targetId = 99,
                targetX = 100.5f,
                targetY = -200.25f,
                level = 12,
            };
            string json = JsonUtility.ToJson(src);
            var dst = JsonUtility.FromJson<SkillCastMsg>(json);
            Assert.AreEqual(src.casterId, dst.casterId);
            Assert.AreEqual(src.skillId, dst.skillId);
            Assert.AreEqual(src.targetId, dst.targetId);
            Assert.AreEqual(src.level, dst.level);
            Assert.AreEqual(src.targetX, dst.targetX, 0.001f);
            Assert.AreEqual(src.targetY, dst.targetY, 0.001f);
        }

        [Test]
        public void BuffApplyMsg_DeserializesFromJson_MatchesInput()
        {
            var src = new BuffApplyMsg
            {
                targetId = 1234,
                buffId = 56,
                durationMs = 30000,
                sourceId = 42,
            };
            string json = JsonUtility.ToJson(src);
            var dst = JsonUtility.FromJson<BuffApplyMsg>(json);
            Assert.AreEqual(src.targetId, dst.targetId);
            Assert.AreEqual(src.buffId, dst.buffId);
            Assert.AreEqual(src.durationMs, dst.durationMs);
            Assert.AreEqual(src.sourceId, dst.sourceId);
        }

        [Test]
        public void TaskCompleteMsg_DeserializesFromJson_MatchesInput()
        {
            var src = new TaskCompleteMsg
            {
                playerId = 8888,
                taskId = 200,
                rewardItemId = 1,
                rewardCount = 100,
            };
            string json = JsonUtility.ToJson(src);
            var dst = JsonUtility.FromJson<TaskCompleteMsg>(json);
            Assert.AreEqual(src.playerId, dst.playerId);
            Assert.AreEqual(src.taskId, dst.taskId);
            Assert.AreEqual(src.rewardItemId, dst.rewardItemId);
            Assert.AreEqual(src.rewardCount, dst.rewardCount);
        }

        [Test]
        public void ChatChannelMsg_StringFieldSurvivesRoundtrip()
        {
            var src = new ChatChannelMsg
            {
                playerId = 42,
                channel = 0, // world
                message = "Xin chào thế giới!",
                targetId = 0,
            };
            string json = JsonUtility.ToJson(src);
            var dst = JsonUtility.FromJson<ChatChannelMsg>(json);
            Assert.AreEqual(src.message, dst.message, "String field phải sống sót qua roundtrip JSON");
            Assert.AreEqual(src.channel, dst.channel);
        }

        [Test]
        public void GuildCreateMsg_StringFieldSurvivesRoundtrip()
        {
            var src = new GuildCreateMsg
            {
                playerId = 1,
                guildName = "Cái Bang Hội Bá Đạo",
                guildId = 9999,
            };
            string json = JsonUtility.ToJson(src);
            var dst = JsonUtility.FromJson<GuildCreateMsg>(json);
            Assert.AreEqual(src.guildName, dst.guildName,
                "Guild name (tiếng Việt có dấu) phải survive roundtrip");
            Assert.AreEqual(src.guildId, dst.guildId);
        }

        [Test]
        public void TongJinScoreMsg_Roundtrips()
        {
            var src = new TongJinScoreMsg
            {
                songScore = 1500,
                jinScore = 1200,
                timeLeftSec = 300,
            };
            string json = JsonUtility.ToJson(src);
            var dst = JsonUtility.FromJson<TongJinScoreMsg>(json);
            Assert.AreEqual(src.songScore, dst.songScore);
            Assert.AreEqual(src.jinScore, dst.jinScore);
            Assert.AreEqual(src.timeLeftSec, dst.timeLeftSec);
        }

        [Test]
        public void BossSpawnMsg_Roundtrips()
        {
            var src = new BossSpawnMsg
            {
                bossId = 32,
                mapId = 100,
                x = 500f,
                y = 0f,
                despawnTime = 1800,
            };
            string json = JsonUtility.ToJson(src);
            var dst = JsonUtility.FromJson<BossSpawnMsg>(json);
            Assert.AreEqual(src.bossId, dst.bossId);
            Assert.AreEqual(src.mapId, dst.mapId);
            Assert.AreEqual(src.despawnTime, dst.despawnTime);
        }
    }
}
