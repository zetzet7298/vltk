// -----------------------------------------------------------------------------
// VLTK Mobile — ST-2.5/ST-10.5 Player + Faction Title runtime service
// Source: PC settings/playertitle.txt (363 entries) + factiontitle.txt (81).
// Quản lý danh hiệu nhân vật (Danh Hiệu) + danh hiệu môn phái (Môn Phái).
// Wraps PcPlayerTitleRegistry + PcFactionTitleRegistry.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Danh Hiệu (player title) + Danh Hiệu Môn Phái (faction title).
    /// PC source: settings/playertitle.txt (363) + factiontitle.txt (81).
    /// </summary>
    public class TitleService
    {
        // ── Data registries (parsed from PC) ────────────────────────────────
        private PcPlayerTitleRegistry _playerReg;
        private PcFactionTitleRegistry _factionReg;

        // ── Player state (Mở Khóa / Kích Hoạt) ─────────────────────────────
        private readonly HashSet<int> _unlockedPlayerTitles = new();
        private readonly HashSet<int> _unlockedFactionTitles = new();
        private int _activePlayerTitleId;
        private int _activeFactionTitleId;
        private int _currentFaction;

        // ── Events (UI hook) ───────────────────────────────────────────────
        /// <summary>Event khi mở khóa danh hiệu mới. (titleId, isFaction=true nếu là danh hiệu môn phái).</summary>
        public event Action<int, bool> OnTitleUnlocked;
        /// <summary>Event khi danh hiệu hiển thị thay đổi. (titleId, isFaction).</summary>
        public event Action<int, bool> OnActiveTitleChanged;

        // ── Getters ────────────────────────────────────────────────────────
        public int ActivePlayerTitleId => _activePlayerTitleId;
        public int ActiveFactionTitleId => _activeFactionTitleId;
        public int CurrentFaction => _currentFaction;
        public int UnlockedPlayerTitleCount => _unlockedPlayerTitles.Count;
        public int UnlockedFactionTitleCount => _unlockedFactionTitles.Count;
        public int PlayerTitleCount => _playerReg?.Count ?? 0;
        public int FactionTitleCount => _factionReg?.Count ?? 0;

        public PcPlayerTitleEntry ActivePlayerTitle =>
            _activePlayerTitleId > 0 ? GetPlayerTitle(_activePlayerTitleId) : null;
        public PcFactionTitleEntry ActiveFactionTitle =>
            _activeFactionTitleId > 0 ? GetFactionTitle(_activeFactionTitleId) : null;

        public TitleService() { }

        public TitleService(PcPlayerTitleRegistry playerReg, PcFactionTitleRegistry factionReg)
        {
            RegisterRegistries(playerReg, factionReg);
        }

        /// <summary>Inject registry instances (cho tests / DI).</summary>
        public void RegisterRegistries(PcPlayerTitleRegistry player, PcFactionTitleRegistry faction)
        {
            _playerReg = player;
            _factionReg = faction;
            if (_playerReg == null || _playerReg.Count == 0)
                SubsystemLog.Warn("Title", "Player title registry rỗng");
            if (_factionReg == null || _factionReg.Count == 0)
                SubsystemLog.Warn("Title", "Faction title registry rỗng");
        }

        /// <summary>Load cả 2 registry từ StreamingAssets/Reference/PcTitle.</summary>
        public static TitleService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcTitle");
            var player = PcPlayerTitleParser.BuildRegistry(root);
            var faction = PcFactionTitleParser.BuildRegistry(root);
            return new TitleService(player, faction);
        }

        // ── Faction set/get ────────────────────────────────────────────────
        /// <summary>Cập nhật môn phái hiện tại của nhân vật.</summary>
        public void SetFaction(int factionId)
        {
            if (_currentFaction == factionId) return;
            _currentFaction = factionId;
            SubsystemLog.Info("Title", $"Môn Phái đổi → {factionId}");
            // Nếu đang kích hoạt danh hiệu môn phái cũ mà không thuộc phái mới → reset
            if (_activeFactionTitleId > 0)
            {
                var f = GetFactionTitle(_activeFactionTitleId);
                if (f != null && f.factionId != factionId)
                {
                    _activeFactionTitleId = 0;
                    OnActiveTitleChanged?.Invoke(0, true);
                }
            }
        }

        // ── Mở Khóa (unlock) ──────────────────────────────────────────────
        /// <summary>Mở khóa danh hiệu nhân vật. No-op nếu đã có.</summary>
        public bool UnlockPlayerTitle(int titleId)
        {
            if (titleId <= 0 || _playerReg == null) return false;
            if (GetPlayerTitle(titleId) == null) return false; // không tồn tại trong catalog
            if (_unlockedPlayerTitles.Add(titleId))
            {
                SubsystemLog.Info("Title", $"Mở Khóa danh hiệu nhân vật #{titleId}");
                OnTitleUnlocked?.Invoke(titleId, false);
                return true;
            }
            return false;
        }

        /// <summary>Mở khóa danh hiệu môn phái. No-op nếu đã có.</summary>
        public bool UnlockFactionTitle(int titleId)
        {
            if (titleId <= 0 || _factionReg == null) return false;
            if (GetFactionTitle(titleId) == null) return false;
            if (_unlockedFactionTitles.Add(titleId))
            {
                SubsystemLog.Info("Title", $"Mở Khóa danh hiệu môn phái #{titleId}");
                OnTitleUnlocked?.Invoke(titleId, true);
                return true;
            }
            return false;
        }

        // ── Kích Hoạt (active) ─────────────────────────────────────────────
        /// <summary>Đặt danh hiệu nhân vật đang hiển thị. Trả về true nếu đổi được.</summary>
        public bool SetActivePlayerTitle(int titleId)
        {
            if (titleId < 0) return false;
            if (titleId > 0 && !_unlockedPlayerTitles.Contains(titleId)) return false; // chưa Mở Khóa
            if (titleId > 0 && GetPlayerTitle(titleId) == null) return false;
            if (_activePlayerTitleId == titleId) return true;
            _activePlayerTitleId = titleId;
            SubsystemLog.Info("Title", $"Kích Hoạt danh hiệu nhân vật #{titleId}");
            OnActiveTitleChanged?.Invoke(titleId, false);
            return true;
        }

        /// <summary>Đặt danh hiệu môn phái. Yêu cầu đã mở khóa VÀ khớp môn phái hiện tại.</summary>
        public bool SetActiveFactionTitle(int titleId)
        {
            if (titleId < 0) return false;
            if (titleId > 0)
            {
                if (!_unlockedFactionTitles.Contains(titleId)) return false; // chưa Mở Khóa
                var entry = GetFactionTitle(titleId);
                if (entry == null) return false;
                if (entry.factionId != _currentFaction) return false; // không khớp phái
            }
            if (_activeFactionTitleId == titleId) return true;
            _activeFactionTitleId = titleId;
            SubsystemLog.Info("Title", $"Kích Hoạt danh hiệu môn phái #{titleId}");
            OnActiveTitleChanged?.Invoke(titleId, true);
            return true;
        }

        // ── Lookup ─────────────────────────────────────────────────────────
        public PcPlayerTitleEntry GetPlayerTitle(int titleId)
            => _playerReg != null ? _playerReg.Get(titleId) : null;

        public PcFactionTitleEntry GetFactionTitle(int titleId)
            => _factionReg != null ? _factionReg.Get(titleId) : null;

        public IReadOnlyList<PcFactionTitleEntry> GetFactionTitlesForFaction(int factionId)
            => _factionReg != null
                ? _factionReg.GetFactionTitles(factionId)
                : (IReadOnlyList<PcFactionTitleEntry>)Array.Empty<PcFactionTitleEntry>();

        /// <summary>Toàn bộ danh hiệu nhân vật (theo thứ tự PC playertitle.txt).</summary>
        public IReadOnlyList<PcPlayerTitleEntry> AllPlayerTitles
            => _playerReg != null ? _playerReg.All : (IReadOnlyList<PcPlayerTitleEntry>)Array.Empty<PcPlayerTitleEntry>();

        /// <summary>Toàn bộ danh hiệu môn phái (theo thứ tự PC factiontitle.txt).</summary>
        public IReadOnlyList<PcFactionTitleEntry> AllFactionTitles
            => _factionReg != null ? _factionReg.All : (IReadOnlyList<PcFactionTitleEntry>)Array.Empty<PcFactionTitleEntry>();

        public bool IsPlayerTitleUnlocked(int titleId) => _unlockedPlayerTitles.Contains(titleId);
        public bool IsFactionTitleUnlocked(int titleId) => _unlockedFactionTitles.Contains(titleId);
    }
}
