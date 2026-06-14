// -----------------------------------------------------------------------------
// VLTK Mobile — GuildService EditMode tests.
// Kiểm tra guild lifecycle khớp PC: tong_apply.lua (create), tong_disband.lua
// (disband), tong_apply_member.lua (add member), tong_leave.lua/tong_kick.lua
// (remove member). IGuildHost dispatch cho UI/chat/global news.
// PC source: script/tong/tong_mix.lua, tong_apply.lua, tong_disband.lua,
// tong_apply_member.lua, tong_leave.lua, tong_kick.lua, settings/tong/tong_level_data.txt.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class GuildServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IGuildHost
        {
            public int CreateCalls;
            public int DisbandCalls;
            public int JoinCalls;
            public int LeftCalls;
            public int UpgradedCalls;
            public int DonateCalls;
            public int BroadcastCalls;
            public string LastGuild;
            public string LastPlayer;
            public bool DeductOk = true;
            public int LastDeductAmount;

            public void OnGuildCreated(string guildName, string founderName)
            {
                CreateCalls++;
                LastGuild = guildName;
                LastPlayer = founderName;
            }
            public void OnGuildDisbanded(string guildName, string leaderName) { DisbandCalls++; }
            public void OnMemberJoined(string guildName, string playerName, GuildMemberRole role)
            {
                JoinCalls++;
                LastPlayer = playerName;
            }
            public void OnMemberLeft(string guildName, string playerName, GuildMemberRole role) { LeftCalls++; }
            public void OnGuildLevelUpgraded(string guildName, int oldLevel, int newLevel) { UpgradedCalls++; }
            public void OnFundsDonated(string guildName, string playerName, int amount) { DonateCalls++; }
            public void BroadcastToTong(string guildName, string message) { BroadcastCalls++; }
            public bool TryDeductPlayerMoney(string playerName, int amount)
            {
                LastDeductAmount = amount;
                return DeductOk;
            }
        }

        private static GuildService BuildService(IGuildHost host = null)
            => new GuildService(null, host);

        // ── Constants & defaults ─────────────────────────────────────────────

        [Test]
        public void InitialState_NotCreated_EmptyName()
        {
            var svc = BuildService();
            Assert.IsFalse(svc.IsCreated);
            Assert.AreEqual(string.Empty, svc.GuildName);
            Assert.AreEqual(0, svc.MemberCount);
        }

        [Test]
        public void CreateCost_IsThousand()
        {
            Assert.AreEqual(1000, GuildService.CreateCost);
        }

        [Test]
        public void MinMaxNameLength_Bounds()
        {
            Assert.AreEqual(3, GuildService.MinGuildNameLength);
            Assert.AreEqual(12, GuildService.MaxGuildNameLength);
        }

        // ── CreateGuild ──────────────────────────────────────────────────────

        [Test]
        public void CreateGuild_Valid_Succeeds()
        {
            var svc = BuildService();
            var r = svc.CreateGuild("Thiên Hạ", "Lý Tiểu Long", 1, 5000);
            Assert.AreEqual(GuildService.GuildCreationResult.Success, r);
            Assert.IsTrue(svc.IsCreated);
            Assert.AreEqual("Thiên Hạ", svc.GuildName);
            Assert.AreEqual("Lý Tiểu Long", svc.FounderName);
            Assert.AreEqual(1, svc.MemberCount);
        }

        [Test]
        public void CreateGuild_AlreadyCreated_Fails()
        {
            var svc = BuildService();
            svc.CreateGuild("A", "X", 1, 5000);
            var r = svc.CreateGuild("B", "Y", 2, 5000);
            Assert.AreEqual(GuildService.GuildCreationResult.AlreadyCreated, r);
        }

        [Test]
        public void CreateGuild_NullName_FailsInvalidName()
        {
            var svc = BuildService();
            var r = svc.CreateGuild(null, "X", 1, 5000);
            Assert.AreEqual(GuildService.GuildCreationResult.InvalidName, r);
        }

        [Test]
        public void CreateGuild_TooShortName_FailsInvalidName()
        {
            var svc = BuildService();
            var r = svc.CreateGuild("AB", "X", 1, 5000);
            Assert.AreEqual(GuildService.GuildCreationResult.InvalidName, r);
        }

        [Test]
        public void CreateGuild_TooLongName_FailsInvalidName()
        {
            var svc = BuildService();
            var r = svc.CreateGuild("AAAAAAAAAAAAAAAAA", "X", 1, 5000);
            Assert.AreEqual(GuildService.GuildCreationResult.InvalidName, r);
        }

        [Test]
        public void CreateGuild_InsufficientFunds_Fails()
        {
            var svc = BuildService();
            var r = svc.CreateGuild("Bang A", "X", 1, 500);
            Assert.AreEqual(GuildService.GuildCreationResult.InsufficientFunds, r);
        }

        [Test]
        public void CreateGuild_DispatchesToHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.CreateGuild("Bang B", "Alice", 1, 5000);
            Assert.AreEqual(1, host.CreateCalls);
            Assert.AreEqual("Bang B", host.LastGuild);
            Assert.AreEqual("Alice", host.LastPlayer);
            Assert.AreEqual(1, host.JoinCalls); // founder joined as Leader
            Assert.AreEqual(1000, host.LastDeductAmount);
        }

        [Test]
        public void CreateGuild_HostDeductFails_Fails()
        {
            var host = new FakeHost { DeductOk = false };
            var svc = BuildService(host);
            var r = svc.CreateGuild("Bang C", "Bob", 1, 5000);
            Assert.AreEqual(GuildService.GuildCreationResult.InsufficientFunds, r);
            Assert.IsFalse(svc.IsCreated);
        }

        // ── DisbandGuild ─────────────────────────────────────────────────────

        [Test]
        public void DisbandGuild_ByLeader_Succeeds()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang D", "Leader1", 100, 5000);
            Assert.IsTrue(svc.DisbandGuild(100));
            Assert.IsFalse(svc.IsCreated);
        }

        [Test]
        public void DisbandGuild_ByNonMember_Fails()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang E", "Leader1", 100, 5000);
            Assert.IsFalse(svc.DisbandGuild(200));
            Assert.IsTrue(svc.IsCreated);
        }

        [Test]
        public void DisbandGuild_WithoutCreating_Fails()
        {
            var svc = BuildService();
            Assert.IsFalse(svc.DisbandGuild(100));
        }

        [Test]
        public void DisbandGuild_DispatchesToHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.CreateGuild("Bang F", "Leader1", 100, 5000);
            Assert.IsTrue(svc.DisbandGuild(100));
            Assert.AreEqual(1, host.DisbandCalls);
        }

        // ── AddMember ────────────────────────────────────────────────────────

        [Test]
        public void AddMember_ByLeader_Succeeds()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang G", "Leader1", 1, 5000);
            Assert.IsTrue(svc.AddMember(2, "Bob", GuildMemberRole.Member, 1));
            Assert.AreEqual(2, svc.MemberCount);
        }

        [Test]
        public void AddMember_Duplicate_Fails()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang H", "Leader1", 1, 5000);
            svc.AddMember(2, "Bob", GuildMemberRole.Member, 1);
            Assert.IsFalse(svc.AddMember(2, "Bob", GuildMemberRole.Member, 1));
        }

        [Test]
        public void AddMember_ByMemberRole_Fails()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang I", "Leader1", 1, 5000);
            svc.AddMember(2, "Bob", GuildMemberRole.Member, 1);
            // Bob (Member) cannot invite
            Assert.IsFalse(svc.AddMember(3, "Carol", GuildMemberRole.Member, 2));
        }

        [Test]
        public void AddMember_ByElder_Succeeds()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang J", "Leader1", 1, 5000);
            svc.AddMember(2, "Bob", GuildMemberRole.Elder, 1);
            Assert.IsTrue(svc.AddMember(3, "Carol", GuildMemberRole.Member, 2));
            Assert.AreEqual(3, svc.MemberCount);
        }

        [Test]
        public void AddMember_WithoutGuild_Fails()
        {
            var svc = BuildService();
            Assert.IsFalse(svc.AddMember(2, "Bob", GuildMemberRole.Member, 1));
        }

        [Test]
        public void AddMember_DispatchesToHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.CreateGuild("Bang K", "Leader1", 1, 5000);
            host.JoinCalls = 0; // reset
            svc.AddMember(2, "Bob", GuildMemberRole.Member, 1);
            Assert.AreEqual(1, host.JoinCalls);
            Assert.AreEqual("Bob", host.LastPlayer);
        }

        // ── RemoveMember ─────────────────────────────────────────────────────

        [Test]
        public void RemoveMember_Regular_Succeeds()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang L", "Leader1", 1, 5000);
            svc.AddMember(2, "Bob", GuildMemberRole.Member, 1);
            Assert.IsTrue(svc.RemoveMember(2, "Bob"));
            Assert.AreEqual(1, svc.MemberCount);
        }

        [Test]
        public void RemoveMember_Leader_Fails()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang M", "Leader1", 1, 5000);
            Assert.IsFalse(svc.RemoveMember(1, "Leader1"));
        }

        [Test]
        public void RemoveMember_NonMember_Fails()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang N", "Leader1", 1, 5000);
            Assert.IsFalse(svc.RemoveMember(999, "X"));
        }

        [Test]
        public void RemoveMember_DispatchesToHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.CreateGuild("Bang O", "Leader1", 1, 5000);
            svc.AddMember(2, "Bob", GuildMemberRole.Member, 1);
            host.LeftCalls = 0;
            svc.RemoveMember(2, "Bob");
            Assert.AreEqual(1, host.LeftCalls);
        }

        // ── Existing flow (level/funds) preserved ────────────────────────────

        [Test]
        public void TryUpgrade_NotEnoughFunds_Fails()
        {
            var svc = BuildService();
            svc.CreateGuild("Bang P", "Leader1", 1, 5000);
            var r = svc.TryUpgrade(2, 0);
            Assert.AreEqual(GuildUpgradeResult.NotEnoughFunds, r);
        }

        [Test]
        public void GetUpgradeCost_NoRegistry_ReturnsZero()
        {
            var svc = BuildService();
            Assert.AreEqual(0, svc.GetUpgradeCost(2));
        }

        [Test]
        public void Donate_PositiveAmount_AddsToFunds()
        {
            var svc = BuildService();
            svc.Donate(500);
            Assert.AreEqual(500, svc.GuildFunds);
        }
    }
}
