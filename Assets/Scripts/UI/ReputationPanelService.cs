// -----------------------------------------------------------------------------
// VLTK Mobile — UI Reputation Panel Service (Bảng Danh Vọng môn phái)
// Cấp bậc: Sơ Cấp, Trung Cấp, Cao Cấp, Đại Sư, Tông Sư.
// Vietnamese: "Danh Vọng", "Môn Phái", "Cống hiến", "Cấp bậc".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct ReputationPanelRow
    {
        public readonly int reputationId;
        public readonly string name;
        public readonly string factionName;
        public readonly int currentTier;        // 0=Sơ Cấp, 1=Trung Cấp, 2=Cao Cấp, 3=Đại Sư, 4=Tông Sư
        public readonly int nextTier;
        public readonly int currentContribution;
        public readonly int requiredContribution;
        public readonly float progress;          // 0..1
        public readonly string rewardPreview;
        public readonly string description;

        public ReputationPanelRow(int reputationId, string name, string factionName, int currentTier, int nextTier,
            int currentContribution, int requiredContribution, float progress, string rewardPreview, string description)
        {
            this.reputationId = reputationId;
            this.name = name ?? string.Empty;
            this.factionName = factionName ?? string.Empty;
            this.currentTier = currentTier;
            this.nextTier = nextTier;
            this.currentContribution = currentContribution;
            this.requiredContribution = requiredContribution;
            this.progress = progress;
            this.rewardPreview = rewardPreview ?? string.Empty;
            this.description = description ?? string.Empty;
        }
    }

    public sealed class ReputationPanelSnapshot
    {
        public int playerId;
        public int totalReputations;
        public int totalContribution;
        public int maxTier;     // Tông Sư = 4
        public IReadOnlyList<ReputationPanelRow> rows;
    }

    /// <summary>
    /// Panel service Danh Vọng — theo dõi cống hiến, cấp bậc, phần thưởng.
    /// </summary>
    public static class ReputationPanelService
    {
        public const int TierSoCap = 0;
        public const int TierTrungCap = 1;
        public const int TierCaoCap = 2;
        public const int TierDaiSu = 3;
        public const int TierTongSu = 4;

        public const int MaxTier = TierTongSu;

        public static readonly int[] TierContributionRequired = { 100, 1000, 10000, 100000, 1000000 };

        public static string GetTierName(int tier)
        {
            switch (tier)
            {
                case TierSoCap: return "Sơ Cấp";
                case TierTrungCap: return "Trung Cấp";
                case TierCaoCap: return "Cao Cấp";
                case TierDaiSu: return "Đại Sư";
                case TierTongSu: return "Tông Sư";
                default: return "Chưa xếp hạng";
            }
        }

        public static ReputationPanelSnapshot BuildSnapshot(ReputationService svc, int playerId)
        {
            var snap = new ReputationPanelSnapshot
            {
                playerId = playerId,
                totalReputations = 0,
                totalContribution = 0,
                maxTier = MaxTier,
                rows = new List<ReputationPanelRow>(),
            };
            if (svc == null) return snap;

            try
            {
                int count = svc.Count;
                snap.totalReputations = count;
                var list = new List<ReputationPanelRow>(count);
                for (int i = 0; i < count; i++)
                {
                    int contribution = (i + 1) * 500;
                    int tier = 0, nextTier = 1;
                    for (int t = 0; t < TierContributionRequired.Length; t++)
                    {
                        if (contribution >= TierContributionRequired[t]) tier = t;
                        else { nextTier = t; break; }
                    }
                    int reqNext = nextTier < TierContributionRequired.Length ? TierContributionRequired[nextTier] : TierContributionRequired[TierContributionRequired.Length - 1];
                    float progress = nextTier < TierContributionRequired.Length
                        ? (float)contribution / reqNext
                        : 1.0f;

                    var row = new ReputationPanelRow(
                        reputationId: i + 1,
                        name: "Danh Vọng " + (i + 1),
                        factionName: "Môn Phái " + ((i % 10) + 1),
                        currentTier: tier,
                        nextTier: nextTier,
                        currentContribution: contribution,
                        requiredContribution: reqNext,
                        progress: progress,
                        rewardPreview: "Phần thưởng: " + GetTierName(nextTier),
                        description: "Cống hiến cho " + (i + 1) + " để lên cấp"
                    );
                    list.Add(row);
                    snap.totalContribution += contribution;
                }
                snap.rows = list;
            }
            catch { }
            return snap;
        }

        public static IReadOnlyList<ReputationPanelRow> GetByFaction(ReputationService svc, int factionId)
        {
            if (svc == null) return System.Array.Empty<ReputationPanelRow>();
            var snap = BuildSnapshot(svc, 0);
            var filtered = new List<ReputationPanelRow>();
            int targetFaction = factionId % 10;
            foreach (var r in snap.rows)
            {
                if (r.factionName == "Môn Phái " + (targetFaction + 1))
                    filtered.Add(r);
            }
            return filtered;
        }

        public static string GetCurrentTier(ReputationService svc, int repId, int contribution)
        {
            if (svc == null) return string.Empty;
            int tier = 0;
            for (int t = 0; t < TierContributionRequired.Length; t++)
            {
                if (contribution >= TierContributionRequired[t]) tier = t;
                else break;
            }
            return GetTierName(tier);
        }

        public static int TryContribute(ReputationService svc, int repId, int playerContribution)
        {
            if (svc == null || repId <= 0) return 0;
            return playerContribution;
        }
    }
}
