using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VLTK.Survivor
{
    /// <summary>
    /// Boot + match brain cho Survivor mode. parity dhcd BattleLevelLogic lifecycle
    /// (Init/Start/GameStart/Update/GameEnd/Destroy) + match registries.
    /// P1: proxy visuals + flat-stat skill cards. P1.5+: bridge JX visual/skill.
    /// </summary>
    public class SurvivorGameDirector : MonoBehaviour
    {
        public static SurvivorGameDirector Instance { get; private set; }

        [Header("Arena")]
        public Vector2 ArenaHalf = new Vector2(3.3f, 5.8f);

        public readonly SurvivorJoystick Input = new SurvivorJoystick();
        public SurvivorPlayer Player { get; private set; }
        public List<SurvivorMonster> Monsters { get; } = new List<SurvivorMonster>();

        /// <summary>
        /// Pause chung ref-count per-scope (ticket 43, spec D13): CardChoice/
        /// Settings/AppLifecycle/GameOver/LevelUp chung 1 counter — resume chỉ
        /// khi TẤT CẢ scope release (không resume nhầm khi card mở + app background).
        /// Boot tạo trong OnInit; null trước boot (fail-safe).
        /// </summary>
        public SurvivorPause Pause { get; private set; }

        /// <summary>Overlay modal (levelup/gameover) — boot trong OnInit.</summary>
        public OverlayPanel Overlay => _overlay;

        /// <summary>Settings panel (ticket 40/43) — boot trong OnInit; null nếu boot chưa chạy.</summary>
        public SurvivorAudioSettingsPanel SettingsPanel => _settingsPanel;

        /// <summary>Supply mgr + bar (ticket 33/43) — boot trong OnInit; null nếu boot chưa chạy.</summary>
        public SurvivorSupplyMgr Supply => _supply;
        public SupplyBar SupplyBar => _supplyBar;

        /// <summary>
        /// Spawn-gate (ticket 42, đòn bẩy 60fps): non-null trả false → bỏ qua spawn
        /// monster thường (boss luôn spawn — parity, exempt trim). SurvivorMonsterCap
        /// đăng ký khi tồn tại trong scene. Fail-closed: gate null → spawn tự do
        /// (sandbox/không cấu hình → hành vi cũ, không đổi).
        /// </summary>
        public System.Func<int, bool> MonsterSpawnGate;
        public Transform PlayerTransform => Player != null ? Player.transform : null;

        private readonly List<Projectile> _projectiles = new List<Projectile>();
        private readonly List<XpGem> _gems = new List<XpGem>();
        private WaveSpawner _spawner;
        private OverlayPanel _overlay;
        private SurvivorSupplyMgr _supply;
        private SupplyBar _supplyBar;
        private SkillChoiceService _skillChoice;
        private SurvivorAudioSettingsPanel _settingsPanel;

        // --- wave trigger context (ticket 30) ---
        public int SkillCastCount;  // P2 skill system sẽ ++ (trigger type 4); P1 luôn 0
        public int OccupiedMask;    // capture-mode ticket sẽ set (trigger type 7-9)
        private readonly Dictionary<SurvivorMonster, int> _monsterIds = new Dictionary<SurvivorMonster, int>();
        private readonly Dictionary<SurvivorMonster, int> _monsterWave = new Dictionary<SurvivorMonster, int>();
        private SurvivorMonster _boss;

        // --- boss (ticket 31) ---
        [Header("Boss (ticket 31)")]
        public List<BossPhaseDef> BossPhases;                  // null → SurvivorBoss.DefaultPhases()
        public List<SkillDef> BossSkillPool = new List<SkillDef>(); // BossNpc pool (ticket 26); rỗng → boss chỉ chase/kit (fail-closed)
        public DropTableSO DropTable;                          // booty drop table; null → chỉ gem burst (fail-closed)
        public System.Random DropRng = new System.Random();

        /// <summary>Boss wrapper active (phase/booty); inner = SurvivorMonster trong Monsters.</summary>
        public SurvivorBoss ActiveBoss { get; private set; }

        /// <summary>Boss HP% cho trigger type 3; không có boss → 1 (trigger không fire).</summary>
        public float BossHpPercent => _boss != null && _boss.MaxHp > 0f ? _boss.Hp / _boss.MaxHp : 1f;

        private void Awake()
        {
            Instance = this;
            if (!OnInit()) { enabled = false; return; }
        }

        private void Start()
        {
            if (!OnStart()) return;
            OnGameStart();
        }

        private void Update()
        {
            // ticket 44: Tick TRƯỚC early-return — waiting window (ticket 29/O6)
            // phải chạy cả khi paused (modal mở ⇔ CardChoice scope ⇔ IsPaused).
            // Tick dùng Time.unscaledTime (KHÔNG phải Time.time — time bị timeScale
            // đóng băng khi paused → auto-close chết, premise correction ticket 44).
            _skillChoice?.Tick(Time.unscaledTime);
            if (Pause != null && Pause.IsPaused) return;
            OnUpdate();
            Input.Update();
            _spawner.Tick(Time.deltaTime, SpawnMonsterAt);
            if (_spawner.ConsumeWaveFinished()) CleanupWaveMonsters();
        }

        /// <summary>
        /// App lifecycle (spec D13): ra ngoài app → pause; vào lại → resume.
        /// Scope AppLifecycle ref-count chung — card/settings đang mở vẫn giữ pause.
        /// Unity gọi trước/trong Awake trên vài platform → null-check fail-safe.
        /// </summary>
        private void OnApplicationPause(bool paused)
        {
            if (Pause == null) return;
            if (paused) Pause.Acquire(SurvivorPause.AppLifecycleScope);
            else Pause.Release(SurvivorPause.AppLifecycleScope);
        }

        private void OnDestroy() => OnDestroyInternal();

        // --- lifecycle hooks (parity BattleLevelLogic). MVP: boot match. ---
        protected virtual bool OnInit()
        {
            // reset timescale về 1 mỗi run mới — scene reload sau gameover/pause
            // không mang theo timescale cũ (Pause apply delegate là per-instance).
            Time.timeScale = 1f;
            Pause = new SurvivorPause(paused => Time.timeScale = paused ? 0f : 1f);
            SpawnArenaVisual();
            SpawnPlayer();
            _overlay = OverlayPanel.Build(); // cũng dựng SurvivorHud (EnsureInstance)
            _spawner = new WaveSpawner();
            SurvivorAudioMgr.EnsureInstance();
            SurvivorAudioMgr.Instance?.SetContext(SurvivorAudioContext.Battle);
            BootSkillSystem();   // ticket 43: roster + skill choice + boss pool + supply
            BootSupplyBar();
            BootSettingsAndHud();
            Debug.Log("[Survivor] OnInit");
            return true;
        }
        protected virtual bool OnStart() { Debug.Log("[Survivor] OnStart"); return true; }
        protected virtual void OnGameStart() { Debug.Log("[Survivor] OnGameStart"); }
        protected virtual void OnUpdate() { }
        protected virtual void OnDestroyInternal() { if (Instance == this) Instance = null; }
        protected virtual void OnAfterBattleEnd() { }

        public void GameEnd()
        {
            OnAfterBattleEnd();
            Debug.Log("[Survivor] GameEnd");
        }

        // --- registries / spawning ---
        public SurvivorMonster NearestMonster(Vector3 from)
        {
            SurvivorMonster best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < Monsters.Count; i++)
            {
                var m = Monsters[i];
                if (m == null) continue;
                float d = (m.transform.position - from).sqrMagnitude;
                if (d < bd) { bd = d; best = m; }
            }
            return best;
        }

        public void SpawnProjectile(Vector3 pos, Vector2 dir, float dmg)
        {
            SpawnProjectile(pos, dir, dmg, 10f, 2f, "", SkillImpactSource.None, null);
        }

        /// <summary>Ticket 27: đạn skill — speed/life từ missles.txt, visual child staged, attribution.</summary>
        public void SpawnProjectile(Vector3 pos, Vector2 dir, float dmg, float speed, float life,
            string spriteUid, SkillImpactSource source, object caster)
        {
            var go = new GameObject(spriteUid.Length > 0 ? "skill_proj" : "proj");
            var p = go.AddComponent<Projectile>();
            p.Init(pos, dir, dmg, source, caster, spriteUid);
            p.speed = speed;
            p.life = life;
            _projectiles.Add(p);
        }

        private void SpawnMonsterAt(MonsterSpawnInfo info)
        {
            // own: boss wave lặp (loop table) khi boss cũ còn sống → không spawn boss thứ 2
            // (boss sống xuyên wave tới khi chết — parity; chết rồi thì wave boss kế spawn mới)
            if (info.IsBoss && ActiveBoss != null) return;
            // ticket 42: cap gate — chặn spawn thường khi đạt cap (boss exempt)
            if (!info.IsBoss && MonsterSpawnGate != null && !MonsterSpawnGate(Monsters.Count)) return;
            var go = new GameObject(info.IsBoss ? "boss" : info.IsElite ? "elite" : "monster");
            var vis = go.AddComponent<ProxyActorVisual>();
            // own tier visual: boss to đỏ sẫm, elite tím, thường đỏ
            if (info.IsBoss) { vis.color = new Color(0.55f, 0.1f, 0.1f); vis.worldSize = new Vector2(1.7f, 2.1f); }
            else if (info.IsElite) { vis.color = new Color(0.7f, 0.35f, 0.95f); vis.worldSize = new Vector2(1.0f, 1.3f); }
            else { vis.color = new Color(0.9f, 0.3f, 0.3f); vis.worldSize = new Vector2(0.7f, 0.9f); }
            var m = go.AddComponent<SurvivorMonster>();
            m.MaxHp = 3f * info.HpMul;                       // base 3 HP (P1) × tier/pool ratio
            m.Speed = 1.6f * info.SpeedMul;                  // base 1.6 (P1) × tier
            m.ContactDamage = Mathf.Max(1, Mathf.RoundToInt(info.AtkMul));
            m.XpDrop = info.IsBoss ? 10 : info.IsElite ? 3 : 1; // own reward tier
            if (info.IsBoss) m.VisualRes = "boss012";        // ticket 35 staged; SPR thiếu → proxy (fail-closed)
            m.Init(vis, info.Pos);
            _monsterIds[m] = info.MonsterId;                 // kill attribution cho trigger kill%/kill-all
            _monsterWave[m] = _spawner.CurrentWaveIndex;     // stamp wave cho cleanup đúng đối tượng
            if (info.IsBoss) _boss = m;
            if (info.IsBoss)
            {
                var boss = go.AddComponent<SurvivorBoss>();
                boss.Init(m, BossPhases ?? SurvivorBoss.DefaultPhases(), BossSkillPool);
                ActiveBoss = boss;
                SurvivorAudioMgr.Instance?.SetContext(SurvivorAudioContext.Boss); // boss spawn → nhạc boss
            }
            Monsters.Add(m);
        }

        private void SpawnPlayer()
        {
            var go = new GameObject("player");
            // P1.5: JxPlayerVisual tự probe SPR → MalePlayerVisual hoặc fallback ProxyActorVisual.
            var vis = go.AddComponent<JxPlayerVisual>();
            Player = go.AddComponent<SurvivorPlayer>();
            Player.Init(vis, Vector3.zero);
            Player.LevelUp += p => OnLevelUp(p);
            Player.Died += _ => OnPlayerDied();
        }

        private void SpawnArenaVisual()
        {
            var go = new GameObject("arena");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProxyVisuals.White();
            sr.color = new Color(0.08f, 0.08f, 0.12f);
            sr.drawMode = SpriteDrawMode.Simple;
            var sz = ArenaHalf * 2f;
            go.transform.localScale = new Vector3(sz.x, sz.y, 1f);
            sr.sortingOrder = -10;
            go.transform.position = new Vector3(0, 0, 5);
        }

        // ------------------------------------------------------------------
        // ticket 43: boot wiring P2 (council FAIL fix — dead-wired feature)
        // ------------------------------------------------------------------

        /// <summary>
        /// Skill boot (ticket 43): catalog runtime từ StreamingAssets → roster
        /// (Player.Cast) + skill choice (overlay.SkillService) + boss pool.
        /// Fail-closed: catalog rỗng → roster trống (auto-attack P1), pool rỗng
        /// → levelup rơi về legacy P1 (TryShowSkillChoice false), boss pool rỗng
        /// → boss chase-only (SurvivorBoss fail-closed). KHÔNG crash.
        /// </summary>
        private void BootSkillSystem()
        {
            var catalog = SurvivorSkillCatalogService.LoadFromStreamingAssets();

            Player.Cast = new SkillCastRuntime();

            var pool = new SkillChoicePool();
            var playerDefs = SurvivorSkillCatalogService.Defs(catalog, SurvivorSkillPool.Player);
            for (int i = 0; i < playerDefs.Count; i++) pool.Add(playerDefs[i]);

            _skillChoice = new SkillChoiceService(Player.Cast, pool, new System.Random(), null, Pause);
            _overlay.SkillService = _skillChoice; // levelup → modal skill thật (thay P1 flat-card)

            BossSkillPool = SurvivorSkillCatalogService.Defs(catalog, SurvivorSkillPool.BossNpc);

            _supply = new SurvivorSupplyMgr();
            _supply.Setup(SurvivorSkillCatalogService.SupplyDefs(catalog)); // heal/bomb slot enabled (fail-closed)
            _supply.Caster = Player;
            _supply.HealTarget = new SurvivorSupplyMgr.SurvivorPlayerDamageable(Player);
        }

        /// <summary>Supply bar (ticket 33/43): Build + OnUse → effect thật (heal/bomb/magnet/full-clear).</summary>
        private void BootSupplyBar()
        {
            if (_supply == null) return;
            _supplyBar = SupplyBar.Build(_supply);
            _supplyBar.OnUse = OnSupplyUsed;
        }

        private void OnSupplyUsed(SupplyKind kind)
        {
            switch (kind)
            {
                case SupplyKind.Heal: _supply.UseHeal(); break;
                case SupplyKind.Bomb: _supply.UseBomb(Player != null ? Player.transform.position : Vector3.zero, Monsters); break;
                case SupplyKind.Magnet: _supply.UseMagnet(_gems); break;
                default: _supply.UseFullClear(Monsters); break;
            }
        }

        /// <summary>
        /// Settings panel + HUD wire (ticket 43): wave banner nguồn thật + i18n
        /// CHUNG 1 SurvivorText instance (panel SetLanguage → HUD/Overlay refresh).
        /// HUD resolve: Instance (play mode Awake set) hoặc FindAnyObjectByType
        /// (EditMode boot — Awake không chạy, test seam).
        /// </summary>
        private void BootSettingsAndHud()
        {
            var hud = SurvivorHud.Instance;
            if (hud == null) hud = UnityEngine.Object.FindAnyObjectByType<SurvivorHud>();
            if (hud != null)
                hud.WaveIndexSource = () => WaveIndex; // 1 dòng wire (ticket 37) — banner wave số thật

            // i18n chung: overlay + settings panel dùng CHUNG instance của HUD
            var text = hud != null ? hud.Texts : SurvivorText.LoadFromStreamingAssets();
            if (_overlay != null) _overlay.Language = text;
            _settingsPanel = SurvivorAudioSettingsPanel.Build(text, Pause);
        }

        // --- events ---
        public void OnProjectileGone(Projectile p) => _projectiles.Remove(p);

        /// <summary>Wave index cho HUD (ticket 37) — 1 dòng wire WaveIndexSource.</summary>
        public int WaveIndex => _spawner.CurrentWaveIndex;

        /// <summary>Tổng monster đã kill (gameover stats 37).</summary>
        public int Kills { get; private set; }

        public void OnMonsterKilled(SurvivorMonster m)
        {
            Monsters.Remove(m);
            Kills++;
            if (_monsterIds.TryGetValue(m, out int id)) _spawner.OnMonsterKilled(id);
            _monsterIds.Remove(m);
            _monsterWave.Remove(m);
            if (_boss == m) _boss = null;
            SpawnGem(m.transform.position, m.XpDrop);
        }

        /// <summary>Booty boss (ticket 31): gem burst + DropTable roll theo BootyID phase active.
        /// Gọi từ SurvivorBoss.SpawnBooty (Update poll hoặc OnDestroy fallback) — luôn sau
        /// OnMonsterKilled(inner) nên gem base đã spawn trước.</summary>
        public void OnBossKilled(SurvivorBoss boss)
        {
            if (boss == null) return;
            if (ActiveBoss == boss) ActiveBoss = null;
            SurvivorAudioMgr.Instance?.SetContext(SurvivorAudioContext.Battle); // boss chết → nhạc battle
            SpawnGemBurst(boss.transform.position, boss.BootyGems, boss.BootyGemAmount);
            if (DropTable == null || boss.BootyId <= 0) return; // fail-closed: chỉ gem burst
            var rolls = new SurvivorCollectItemMgr(DropTable).RollActorDrop(boss.BootyId, DropRng);
            for (int i = 0; i < rolls.Count; i++)
            {
                // Gold/Heal/Magnet/Bomb chưa có runtime spawn (ticket 13/32/33 supply) → bỏ qua fail-closed
                if (rolls[i].OutputType != DropOutputType.Xp) continue;
                SpawnGem(boss.transform.position, rolls[i].Amount > 0 ? rolls[i].Amount : 1);
            }
        }

        public void SpawnGem(Vector3 pos, int amount)
        {
            var go = new GameObject("gem");
            var g = go.AddComponent<XpGem>();
            g.Init(pos, amount);
            _gems.Add(g);
        }

        public void SpawnGemBurst(Vector3 pos, int count, int amount)
        {
            for (int i = 0; i < count; i++)
            {
                var off = Random.insideUnitCircle * 0.8f;
                SpawnGem(pos + new Vector3(off.x, off.y, 0f), amount);
            }
        }

        /// <summary>
        /// Dọn monster còn sống sau wave finish (parity TimeOverCheckDestoryMonster khi
        /// IsDeleteAllMonster). Chỉ destroy monster stamp wave cũ — không đụng wave mới
        /// (dhcd cho wave chồng nhau; own: 1 wave active). Boss wave (deleteAll=false)
        /// giữ boss sống cho wave kế trigger HP%.
        /// </summary>
        private void CleanupWaveMonsters()
        {
            if (!_spawner.WaveCleanupMonsters) return;
            int cur = _spawner.CurrentWaveIndex;
            for (int i = Monsters.Count - 1; i >= 0; i--)
            {
                var m = Monsters[i];
                if (m == _boss) continue; // parity: boss entity sống xuyên wave tới khi chết (chết → booty ticket 31)
                _monsterWave.TryGetValue(m, out int stamp);
                if (stamp >= cur) continue; // thuộc wave đang chạy
                _monsterIds.Remove(m);
                _monsterWave.Remove(m);
                Destroy(m.gameObject);
                Monsters.RemoveAt(i);
            }
        }

        public void OnGemCollected(XpGem g) => _gems.Remove(g);

        public void OnLevelUp(SurvivorPlayer p)
        {
            // parity r-dhcd-003: timescale pause while card open. ticket 43: dùng
            // SurvivorPause scope LevelUp — onClosed (cả 2 path modal) release;
            // service path không gọi onPick nên director không tự resume.
            Pause.Acquire(SurvivorPause.LevelUpScope);
            var cards = PickCards(3);
            _overlay.ShowLevelUp(cards,
                card => p.ApplyCard(card),
                () => Pause.Release(SurvivorPause.LevelUpScope));
        }

        public void OnPlayerDied()
        {
            // ticket 45 (44a): player chết khi modal levelup mở → monster damage
            // không check Pause → LevelUpScope chưa release (onClosed không bao
            // giờ fire — ShowGameOver tắt poll). Release TRƯỚC ShowGameOver;
            // no-op khi scope vắng → an toàn path không modal.
            Pause.Release(SurvivorPause.LevelUpScope);
            Pause.Acquire(SurvivorPause.GameOverScope);
            _overlay.ShowGameOver(() =>
            {
                Pause.Release(SurvivorPause.GameOverScope);
                SceneManager.LoadScene("Survivor");
            });
        }

        private static List<SkillCard> PickCards(int n)
        {
            var pool = new List<SkillCard>(SkillCard.Pool);
            var res = new List<SkillCard>();
            for (int i = 0; i < n && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                res.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return res;
        }
    }
}
