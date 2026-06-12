// -----------------------------------------------------------------------------
// VLTK Mobile — Tests cho Faction + Guild + Battle services (batch 10).
// Test factory: FactionSkillTreeService, FactionBonusService, FactionRelationService,
// GuildScriptService, BattleMapConfigService, BattleRewardConfigService,
// BattleHonorService, SjBattleService.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class FactionSkillTreeServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => FactionSkillTreeService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByFaction_FiltersCorrectly()
        {
            var svc = new FactionSkillTreeService(new PcFactionSkillTreeRegistry());
            var entries = svc.GetByFaction(0);
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void CanLearn_RejectsWrongFaction()
        {
            var svc = new FactionSkillTreeService(new PcFactionSkillTreeRegistry());
            var known = new System.Collections.Generic.HashSet<int>();
            Assert.That(svc.CanLearn(0, 9999, 50, known), Is.False);
        }

        [Test]
        public void TryLearn_ReturnsZero_WhenOk()
        {
            var reg = new PcFactionSkillTreeRegistry();
            reg.Register(new PcFactionSkillTreeEntry
            {
                factionId = 0, skillId = 100, tier = 1, requiredLevel = 10,
            });
            var svc = new FactionSkillTreeService(reg);
            var known = new System.Collections.Generic.HashSet<int>();
            int result = svc.TryLearn(0, 100, 50, known);
            Assert.That(result, Is.EqualTo(FactionLearnResult.Ok));
            Assert.That(known.Contains(100), Is.True);
        }
    }

    public class FactionBonusServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => FactionBonusService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByFaction_FiltersCorrectly()
        {
            var svc = new FactionBonusService(new PcFactionBonusRegistry());
            var entries = svc.GetByFaction(0);
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void ComputeHpBonus_ZeroForInvalid()
        {
            var svc = new FactionBonusService(new PcFactionBonusRegistry());
            Assert.That(svc.ComputeHpBonus(0, 0), Is.EqualTo(0));
            Assert.That(svc.ComputeHpBonus(-1, 50), Is.EqualTo(0));
        }
    }

    public class FactionRelationServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => FactionRelationService.LoadFromStreamingAssets());
        }

        [Test]
        public void IsAlly_ReturnsFalse_For_SameFaction()
        {
            var svc = new FactionRelationService(new PcFactionRelationRegistry());
            Assert.That(svc.IsAlly(0, 0), Is.False);
        }

        [Test]
        public void IsEnemy_ReturnsTrue_ForRival()
        {
            var reg = new PcFactionRelationRegistry();
            reg.Register(new PcFactionRelationEntry
            {
                factionId = 0, enemyFactionId = 1, alignment = 0,
            });
            var svc = new FactionRelationService(reg);
            Assert.That(svc.IsEnemy(0, 1), Is.True);
        }

        [Test]
        public void GetAlignment_ZeroForNeutral()
        {
            var svc = new FactionRelationService(new PcFactionRelationRegistry());
            Assert.That(svc.GetAlignment(99), Is.EqualTo(FactionAlignment.Neutral));
        }
    }

    public class GuildScriptServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => GuildScriptService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var svc = new GuildScriptService(new PcGuildScriptRegistry());
            var entries = svc.GetByType(GuildScriptType.Create);
            Assert.That(entries, Is.Not.Null);
        }

        [Test]
        public void CanExecute_RejectsLowLevel()
        {
            var reg = new PcGuildScriptRegistry();
            reg.Register(new PcGuildScriptEntry
            {
                scriptId = 1, name = "test", type = GuildScriptType.Create, requiredLevel = 50,
            });
            var svc = new GuildScriptService(reg);
            Assert.That(svc.CanExecute(1, 10), Is.False);
            Assert.That(svc.CanExecute(1, 60), Is.True);
        }

        [Test]
        public void ExecuteScript_ReturnsZero_WhenOk()
        {
            var reg = new PcGuildScriptRegistry();
            reg.Register(new PcGuildScriptEntry
            {
                scriptId = 2, name = "donate", type = GuildScriptType.Donate, requiredLevel = 30,
            });
            var svc = new GuildScriptService(reg);
            var ctx = new GuildContext { playerId = 1, playerLevel = 50 };
            int result = svc.ExecuteScript(2, ctx);
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void GetScriptTypeName_NonEmpty()
        {
            var svc = new GuildScriptService(new PcGuildScriptRegistry());
            Assert.That(svc.GetScriptTypeName(GuildScriptType.Create), Is.Not.Empty);
            Assert.That(svc.GetScriptTypeName(99), Is.Not.Empty);
        }
    }

    public class BattleMapConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BattleMapConfigService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByBattleType_FiltersCorrectly()
        {
            var svc = new BattleMapConfigService(new PcBattleMapConfigRegistry());
            var entries = svc.GetByBattleType((int)BattleType.TongKim);
            Assert.That(entries, Is.Not.Null);
        }

        [Test]
        public void CanJoin_RejectsInvalid()
        {
            var reg = new PcBattleMapConfigRegistry();
            reg.Register(new PcBattleMapConfigEntry
            {
                battleMapId = 1, battleType = (int)BattleType.TongKim,
                minLevel = 50, maxLevel = 100,
            });
            var svc = new BattleMapConfigService(reg);
            Assert.That(svc.CanJoin(1, 30), Is.False);
            Assert.That(svc.CanJoin(1, 60), Is.True);
        }

        [Test]
        public void GetBattleTypeName_NonEmpty()
        {
            var svc = new BattleMapConfigService(new PcBattleMapConfigRegistry());
            Assert.That(svc.GetBattleTypeName((int)BattleType.TongKim), Is.Not.Empty);
            Assert.That(svc.GetBattleTypeName(99), Is.Not.Empty);
        }
    }

    public class BattleRewardConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BattleRewardConfigService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetWinReward_RejectsInvalid()
        {
            var svc = new BattleRewardConfigService(new PcBattleRewardConfigRegistry());
            Assert.That(svc.GetWinReward(99, 1), Is.Null);
        }

        [Test]
        public void GetForRank_FiltersCorrectly()
        {
            var reg = new PcBattleRewardConfigRegistry();
            reg.Register(new PcBattleRewardConfigEntry
            {
                rewardId = 1, battleType = (int)BattleType.TongKim, requiredRank = 1, winGold = 1000,
            });
            reg.Register(new PcBattleRewardConfigEntry
            {
                rewardId = 2, battleType = (int)BattleType.TongKim, requiredRank = 2, winGold = 5000,
            });
            var svc = new BattleRewardConfigService(reg);
            var rank1 = svc.GetForRank(1);
            Assert.That(rank1.Count, Is.EqualTo(1));
        }
    }

    public class BattleHonorServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BattleHonorService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByBattleType_FiltersCorrectly()
        {
            var reg = new PcBattleHonorRegistry();
            reg.Register(new PcBattleHonorEntry
            {
                honorId = 1, battleType = (int)BattleType.TongKim, name = "Chiến Thần",
                requiredScore = 1000,
            });
            var svc = new BattleHonorService(reg);
            var entries = svc.GetByBattleType((int)BattleType.TongKim);
            Assert.That(entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetHonorForScore_NullForZero()
        {
            var svc = new BattleHonorService(new PcBattleHonorRegistry());
            Assert.That(svc.GetHonorForScore((int)BattleType.TongKim, 0), Is.Null);
        }
    }

    public class SjBattleServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => SjBattleService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByTier_FiltersCorrectly()
        {
            var reg = new PcSjBattleRegistry();
            reg.Register(new PcSjBattleEntry
            {
                tierId = 1, tier = SongJinTier.So, name = "Tống Kim Sơ Cấp",
                minLevel = 30, maxLevel = 60, maxPlayers = 50,
            });
            var svc = new SjBattleService(reg);
            var entries = svc.GetByTier(SongJinTier.So);
            Assert.That(entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void CanJoinTier_RejectsLowLevel()
        {
            var reg = new PcSjBattleRegistry();
            reg.Register(new PcSjBattleEntry
            {
                tierId = 1, tier = SongJinTier.Cao, minLevel = 90, maxLevel = 150,
            });
            var svc = new SjBattleService(reg);
            // Cao tier requires minLevel=90; player level 30 is below it and below the
            // fallback threshold (30 + Cao*30 = 90), so a low-level player is rejected.
            Assert.That(svc.CanJoinTier(SongJinTier.Cao, 30), Is.False, "Cấp 30 < ngưỡng Cao (90) → bị từ chối");
            // So tier has no registered entry; fallback threshold = 30 + So*30 = 30, so level 30 qualifies.
            Assert.That(svc.CanJoinTier(SongJinTier.So, 30), Is.True);
        }

        [Test]
        public void GetTierName_NonEmpty()
        {
            var svc = new SjBattleService(new PcSjBattleRegistry());
            Assert.That(svc.GetTierName(SongJinTier.So), Is.EqualTo("Sơ Cấp"));
            Assert.That(svc.GetTierName(SongJinTier.Trung), Is.EqualTo("Trung Cấp"));
            Assert.That(svc.GetTierName(SongJinTier.Cao), Is.EqualTo("Cao Cấp"));
        }

        [Test]
        public void GetTierForLevel_ReturnsValidTier()
        {
            var svc = new SjBattleService(new PcSjBattleRegistry());
            int t0 = svc.GetTierForLevel(20);
            int t1 = svc.GetTierForLevel(50);
            int t2 = svc.GetTierForLevel(70);
            int t3 = svc.GetTierForLevel(120);
            Assert.That(t0, Is.InRange(0, 2));
            Assert.That(t1, Is.InRange(0, 2));
            Assert.That(t2, Is.InRange(0, 2));
            Assert.That(t3, Is.InRange(0, 2));
        }
    }
}
