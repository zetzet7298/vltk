// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorPause (ticket 43 wiring; spec D13)
// Ref-count pause per-scope (r-dhcd-003 m_pauseCount parity-shape; own:
// timeScale ∈ {0,1}, KHÔNG claim input lock). Scope = string key, mỗi scope
// acquire/release độc lập; apply delegate chỉ chạy ở transition tổng 0→1/1→0
// — nên nhiều scope chồng nhau (card + settings + app-lifecycle) không bao
// giờ resume nhầm khi một scope còn giữ pause.
//
// Scopes (spec D13): CardChoice (modal skill), Settings (panel cài đặt),
// AppLifecycle (OnApplicationPause), GameOver, LevelUp (director giữ tới khi
// modal đóng — legacy path + service path chung 1 counter).
// Core thuần (delegate inject) — EditMode test được (spec Testing Decisions).
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.Survivor
{
    /// <summary>
    /// Ref-count pause chung toàn game: apply(true) khi count 0→1, apply(false)
    /// khi count 1→0. Mỗi scope (CardChoice/Settings/AppLifecycle/GameOver/
    /// LevelUp) đếm riêng — Release đúng scope không làm mất pause của scope khác.
    /// </summary>
    public sealed class SurvivorPause
    {
        public const string CardChoiceScope = "CardChoice";
        public const string SettingsScope = "Settings";
        public const string AppLifecycleScope = "AppLifecycle";
        public const string GameOverScope = "GameOver";
        public const string LevelUpScope = "LevelUp";

        private readonly System.Action<bool> _apply;
        private readonly Dictionary<string, int> _scopes = new Dictionary<string, int>();
        private int _count;

        public SurvivorPause(System.Action<bool> apply) { _apply = apply; }

        public bool IsPaused => _count > 0;
        public int Count => _count;

        public void Acquire(string scope)
        {
            if (string.IsNullOrEmpty(scope)) return;
            _scopes.TryGetValue(scope, out int n);
            _scopes[scope] = n + 1;
            if (_count == 0) _apply?.Invoke(true);
            _count++;
        }

        public void Release(string scope)
        {
            if (string.IsNullOrEmpty(scope)) return;
            if (!_scopes.TryGetValue(scope, out int n) || n <= 0) return;
            if (n == 1) _scopes.Remove(scope); else _scopes[scope] = n - 1;
            _count--;
            if (_count == 0) _apply?.Invoke(false);
        }

        public int ScopeCount(string scope)
            => scope != null && _scopes.TryGetValue(scope, out int n) ? n : 0;
    }
}
