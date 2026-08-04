// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorYSortTests
// Ticket 46: Y-sort refine — side-view XY (camera +Z): Y cao = xa = render TRƯỚC
// (sortingOrder thấp). Player + monster + proxy CÙNG công thức ActorDepth.BaseOrder.
// Seam: EditMode pure-logic + runtime component (không scene, không PlayMode).
// Sandbox hook: sortingBaseOverride (-1 = mặc định PC, KHÔNG đổi behavior).
// -----------------------------------------------------------------------------

using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;
using VLTK.Survivor;
using VLTK.Tests.Sandbox;

namespace VLTK.Tests.Survivor
{
    public class SurvivorYSortTests
    {
        // Public flag cần thiết: PartRuntime.renderer là field public (giống MalePlayerVisualTests)
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // --- gate 1: công thức chung — Y cao → order thấp (monotonic trong band arena ±6) ---
        [Test]
        public void ActorDepth_HigherY_LowerOrder()
        {
            int yLow = ActorDepth.BaseOrder(-5.8f);   // gần camera (trước)
            int yMid = ActorDepth.BaseOrder(0f);      // mặc định = PlayerSortingOrder
            int yHigh = ActorDepth.BaseOrder(5.8f);   // xa camera (sau)
            Assert.AreEqual(MapRenderer.PlayerSortingOrder, yMid, "Y=0 phải = base player");
            Assert.Greater(yLow, yMid, "Y thấp (gần) → order cao (đè)");
            Assert.Greater(yMid, yHigh, "Y cao (xa) → order thấp (render trước)");
            // monotonic: từng bước nhỏ cũng đổi thứ tự (k = 40 bậc/unit, không tie)
            Assert.Greater(ActorDepth.BaseOrder(0.1f), ActorDepth.BaseOrder(0.5f));
            Assert.Greater(ActorDepth.BaseOrder(0.5f), ActorDepth.BaseOrder(1f));
            Assert.Greater(ActorDepth.BaseOrder(-1f), ActorDepth.BaseOrder(1f));
        }

        // --- gate 2: clamp int16 (-32768..32767) — Y cực trị không overflow ---
        [Test]
        public void ActorDepth_ClampsToInt16Band()
        {
            Assert.AreEqual(-32768, ActorDepth.BaseOrder(10000f), "Y rất cao → clamp min");
            Assert.AreEqual(32767, ActorDepth.BaseOrder(-10000f), "Y rất thấp → clamp max");
            Assert.LessOrEqual(ActorDepth.BaseOrder(-1e6f), short.MaxValue);
            Assert.GreaterOrEqual(ActorDepth.BaseOrder(1e6f), short.MinValue);
            Assert.LessOrEqual(ActorDepth.BaseOrder(123.456f), short.MaxValue);
            Assert.GreaterOrEqual(ActorDepth.BaseOrder(123.456f), short.MinValue);
        }

        // --- gate 3: proxy runtime — SyncDepth theo Y (cùng công thức) ---
        [Test]
        public void Proxy_SyncDepth_OrdersByY()
        {
            var go = new GameObject("proxy_sort_test");
            try
            {
                var proxy = go.AddComponent<ProxyActorVisual>();
                InvokeStart(proxy); // EditMode: Start không tự chạy
                var sr = go.GetComponent<SpriteRenderer>();
                Assert.NotNull(sr, "Start phải tạo SpriteRenderer");

                proxy.SyncDepth(5f);
                int atHigh = sr.sortingOrder;
                proxy.SyncDepth(-5f);
                int atLow = sr.sortingOrder;
                proxy.SyncDepth(0f);
                int atZero = sr.sortingOrder;

                Assert.AreEqual(ActorDepth.BaseOrder(5f), atHigh);
                Assert.AreEqual(ActorDepth.BaseOrder(-5f), atLow);
                Assert.AreEqual(ActorDepth.BaseOrder(0f), atZero);
                Assert.Greater(atLow, atZero, "proxy Y thấp → order cao");
                Assert.Greater(atZero, atHigh, "proxy Y cao → order thấp");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // --- gate 4: proxy fail-closed — SyncDepth trước Start (chưa có renderer) không crash ---
        [Test]
        public void Proxy_SyncDepth_BeforeStart_FailClosed()
        {
            var go = new GameObject("proxy_failclosed_test");
            try
            {
                var proxy = go.AddComponent<ProxyActorVisual>();
                Assert.DoesNotThrow(() => proxy.SyncDepth(3f), "chưa có renderer phải no-op");
                Assert.IsNull(go.GetComponent<SpriteRenderer>(), "SyncDepth không được tạo renderer");
                proxy.SyncDepth(3f); // lặp lại vẫn an toàn
            }
            finally { Object.DestroyImmediate(go); }
        }

        // --- gate 5: monster (PcNpcVisual) — override theo Y, shadow luôn dưới actor chủ, clamp int16 ---
        [Test]
        public void NpcVisual_SyncDepth_OrdersByY_ShadowBelowActor()
        {
            var go = new GameObject("npc_sort_test");
            try
            {
                var npc = go.AddComponent<PcNpcVisual>();
                InvokeAwake(npc); // EditMode: AddComponent không tự chạy Awake (EnsureRenderer/Shadow)
                var sr = go.transform.Find("NpcSprite").GetComponent<SpriteRenderer>();
                var shadow = go.transform.Find("NpcShadow").GetComponent<SpriteRenderer>();
                Assert.NotNull(sr);
                Assert.NotNull(shadow);

                // default: không override → hành vi PC cũ (player - 10), shadow - 20
                Assert.AreEqual(MapRenderer.PlayerSortingOrder - 10, sr.sortingOrder);
                Assert.AreEqual(MapRenderer.PlayerSortingOrder - 20, shadow.sortingOrder);

                // Y cao (sau): order thấp; Y thấp (trước): order cao — cùng ActorDepth
                npc.sortingBaseOverride = ActorDepth.BaseOrder(5f);
                npc.ApplySortingBase();
                int atHigh = sr.sortingOrder;
                Assert.AreEqual(ActorDepth.BaseOrder(5f), atHigh);
                Assert.AreEqual(atHigh - 10, shadow.sortingOrder, "shadow = base - 10");

                npc.sortingBaseOverride = ActorDepth.BaseOrder(-5f);
                npc.ApplySortingBase();
                int atLow = sr.sortingOrder;
                Assert.AreEqual(ActorDepth.BaseOrder(-5f), atLow);
                Assert.AreEqual(atLow - 10, shadow.sortingOrder);
                Assert.Greater(atLow, atHigh, "monster Y thấp → đè monster Y cao");

                // clamp: base ở sát int16 min → shadow không tràn xuống dưới -32768
                npc.sortingBaseOverride = short.MinValue;
                npc.ApplySortingBase();
                Assert.AreEqual(-32768, sr.sortingOrder);
                Assert.AreEqual(-32768, shadow.sortingOrder, "shadow clamp int16, không overflow");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // --- gate 6: player (MalePlayerVisual) + monster chung công thức, thứ tự đúng 2 chiều ---
        [Test]
        public void PlayerAndMonster_SharedFormula_OrderFollowsWorldY()
        {
            string root = MalePlayerSprStaging.StageForTests();
            GameObject pgo = null, mgo = null;
            try
            {
                // player visual (JX bridge target): part renderer = BaseOrder(Y) + SortingOffset(kind, dir)
                pgo = new GameObject("player_sort_test");
                var player = pgo.AddComponent<MalePlayerVisual>();
                player.spritesRootOverride = root;
                player.RefreshActionParts(force: true);

                player.sortingBaseOverride = ActorDepth.BaseOrder(2f); // player đứng SAU (Y cao)
                player.RefreshActionParts(force: true);                // kết thúc bằng ApplySorting
                int playerBodyBehind = BodyOrder(player);
                Assert.Greater(0, playerBodyBehind - (ActorDepth.BaseOrder(2f) + 40), "body order = base + part offset");

                player.sortingBaseOverride = ActorDepth.BaseOrder(-1f); // player đứng TRƯỚC (Y thấp)
                player.RefreshActionParts(force: true);
                int playerBodyFront = BodyOrder(player);
                Assert.Greater(playerBodyFront, playerBodyBehind, "player Y thấp → order cao hơn");

                // monster (PcNpcVisual base) — cùng công thức
                mgo = new GameObject("monster_sort_test");
                var npc = mgo.AddComponent<PcNpcVisual>();
                InvokeAwake(npc); // EditMode: AddComponent không tự chạy Awake (EnsureRenderer/Shadow)
                npc.sortingBaseOverride = ActorDepth.BaseOrder(-1f);
                npc.ApplySortingBase();
                int monsterFront = mgo.transform.Find("NpcSprite").GetComponent<SpriteRenderer>().sortingOrder;
                npc.sortingBaseOverride = ActorDepth.BaseOrder(2f);
                npc.ApplySortingBase();
                int monsterBehind = mgo.transform.Find("NpcSprite").GetComponent<SpriteRenderer>().sortingOrder;

                // monster đứng TRƯỚC player (Y thấp hơn) → monster > player (đè)
                Assert.Greater(monsterFront, playerBodyBehind,
                    "monster trước (Y=-1) phải đè player sau (Y=2)");
                // monster đứng SAU player (Y cao hơn) → monster < player
                Assert.Less(monsterBehind, playerBodyFront,
                    "monster sau (Y=2) phải dưới player trước (Y=-1)");
            }
            finally
            {
                if (pgo != null) Object.DestroyImmediate(pgo);
                if (mgo != null) Object.DestroyImmediate(mgo);
                MalePlayerSprStaging.CleanupTempDir(root);
            }
        }

        // --- helpers ---

        private static void InvokeStart(MonoBehaviour mb)
        {
            typeof(ProxyActorVisual).GetMethod("Start", PrivateInstance).Invoke(mb, null);
        }

        private static void InvokeAwake(MonoBehaviour mb)
        {
            mb.GetType().GetMethod("Awake", PrivateInstance).Invoke(mb, null);
        }

        private static int BodyOrder(MalePlayerVisual v)
        {
            var parts = (IDictionary)typeof(MalePlayerVisual).GetField("_parts", PrivateInstance).GetValue(v);
            var body = parts[PlayerSpritePartKind.Body];
            // dùng runtime.GetType() (giống MalePlayerVisualTests) — GetNestedType khó match private class
            var renderer = (SpriteRenderer)body.GetType().GetField("renderer", PrivateInstance).GetValue(body);
            return renderer.sortingOrder;
        }
    }
}
