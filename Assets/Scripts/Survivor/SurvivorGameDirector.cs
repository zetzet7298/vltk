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
        public Transform PlayerTransform => Player != null ? Player.transform : null;

        private readonly List<Projectile> _projectiles = new List<Projectile>();
        private readonly List<XpGem> _gems = new List<XpGem>();
        private WaveSpawner _spawner;
        private OverlayPanel _overlay;
        private bool _paused;

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
            if (_paused) return;
            OnUpdate();
            Input.Update();
            _spawner.Tick(Time.deltaTime, SpawnMonsterAt);
        }

        private void OnDestroy() => OnDestroyInternal();

        // --- lifecycle hooks (parity BattleLevelLogic). MVP: boot match. ---
        protected virtual bool OnInit()
        {
            SpawnArenaVisual();
            SpawnPlayer();
            _overlay = OverlayPanel.Build();
            _spawner = new WaveSpawner();
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
            var go = new GameObject("proj");
            var p = go.AddComponent<Projectile>();
            p.Init(pos, dir, dmg);
            _projectiles.Add(p);
        }

        private void SpawnMonsterAt(Vector3 pos)
        {
            var go = new GameObject("monster");
            var vis = go.AddComponent<ProxyActorVisual>();
            vis.color = new Color(0.9f, 0.3f, 0.3f);
            vis.worldSize = new Vector2(0.7f, 0.9f);
            var m = go.AddComponent<SurvivorMonster>();
            m.Init(vis, pos);
            Monsters.Add(m);
        }

        private void SpawnPlayer()
        {
            var go = new GameObject("player");
            var vis = go.AddComponent<ProxyActorVisual>();
            vis.color = new Color(0.3f, 0.8f, 1f);
            vis.worldSize = new Vector2(0.7f, 1.1f);
            Player = go.AddComponent<SurvivorPlayer>();
            Player.Init(vis, Vector3.zero);
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

        // --- events ---
        public void OnProjectileGone(Projectile p) => _projectiles.Remove(p);

        public void OnMonsterKilled(SurvivorMonster m)
        {
            Monsters.Remove(m);
            var go = new GameObject("gem");
            var g = go.AddComponent<XpGem>();
            g.Init(m.transform.position, m.XpDrop);
            _gems.Add(g);
        }

        public void OnGemCollected(XpGem g) => _gems.Remove(g);

        public void OnLevelUp(SurvivorPlayer p)
        {
            // parity r-dhcd-003: timescale pause while card open
            _paused = true;
            Time.timeScale = 0f;
            var cards = PickCards(3);
            _overlay.ShowLevelUp(cards, card =>
            {
                p.ApplyCard(card);
                _overlay.Hide();
                Time.timeScale = 1f;
                _paused = false;
            });
        }

        public void OnPlayerDied()
        {
            _paused = true;
            Time.timeScale = 0f;
            _overlay.ShowGameOver(() =>
            {
                Time.timeScale = 1f;
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
