// -----------------------------------------------------------------------------
// VLTK Mobile — ST-2.x Faction Relation Service
// Quản lý quan hệ giữa các môn phái (Chính/Tà/Trung Lập).
// Reference: faction_relation.txt.
// Vietnamese: "Quan Hệ Phái", "Đồng Minh", "Thù Địch", "Trung Lập".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Hệ phái.
    /// </summary>
    public static class FactionAlignment
    {
        public const int Justice = 0;       // Chính phái
        public const int Evil = 1;          // Tà phái
        public const int Neutral = 2;       // Trung Lập

        public static string GetName(int alignment)
        {
            switch (alignment)
            {
                case Justice: return "Chính Phái";
                case Evil: return "Tà Phái";
                case Neutral: return "Trung Lập";
                default: return "Không Rõ";
            }
        }
    }

    /// <summary>
    /// Service quản lý quan hệ giữa các môn phái.
    /// </summary>
    public class FactionRelationService
    {
        public const string LogTag = "FactionRelation";
        public const string DefaultStreamingDir = "Reference/PcFaction";

        private PcFactionRelationRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public FactionRelationService() { }
        public FactionRelationService(PcFactionRelationRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcFactionRelationRegistry reg)
        {
            _registry = reg ?? new PcFactionRelationRegistry();
            if (_registry.Count == 0) SubsystemLog.Warn(LogTag, "Quan hệ phái rỗng");
        }

        public static FactionRelationService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new FactionRelationService();
            var reg = PcFactionRelationParser.BuildRegistry(dir);
            svc.RegisterRegistry(reg);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} quan hệ phái");
            return svc;
        }

        public PcFactionRelationEntry GetRelation(int factionId)
            => _registry != null ? _registry.Get(factionId) : null;

        public IReadOnlyList<int> GetAllies(int factionId)
        {
            var r = GetRelation(factionId);
            if (r == null) return Array.Empty<int>();
            return r.alliedFactionId > 0
                ? new List<int> { r.alliedFactionId }
                : (IReadOnlyList<int>)Array.Empty<int>();
        }

        public IReadOnlyList<int> GetEnemies(int factionId)
        {
            var r = GetRelation(factionId);
            if (r == null) return Array.Empty<int>();
            return r.enemyFactionId > 0
                ? new List<int> { r.enemyFactionId }
                : (IReadOnlyList<int>)Array.Empty<int>();
        }

        /// <summary>Phái này có phải đồng minh với phái kia không.</summary>
        public bool IsAlly(int f1, int f2)
        {
            if (f1 == f2) return false;
            var allies1 = GetAllies(f1);
            foreach (var ally in allies1)
                if (ally == f2) return true;
            return false;
        }

        /// <summary>Phái này có phải thù địch với phái kia không.</summary>
        public bool IsEnemy(int f1, int f2)
        {
            if (f1 == f2) return false;
            var enemies1 = GetEnemies(f1);
            foreach (var enemy in enemies1)
                if (enemy == f2) return true;
            return false;
        }

        /// <summary>Lấy hệ phái (0=chính, 1=tà, 2=trung lập).</summary>
        public int GetAlignment(int factionId)
        {
            var r = GetRelation(factionId);
            return r?.alignment ?? FactionAlignment.Neutral;
        }

        public string GetAlignmentName(int alignment)
            => FactionAlignment.GetName(alignment);
    }
}
