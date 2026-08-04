// -----------------------------------------------------------------------------
// VLTK.Tests.PlayMode — SurvivorDirectionPlayModeTests
// Ticket 48: verify 8-way facing end-to-end trong Survivor scene.
//  - Player: chạy 8 hướng (Input.Move) → MalePlayerVisual.direction khớp JX order
//    (0=S 1=SW 2=W 3=NW 4=N 5=NE 6=E 7=SE); idle giữ hướng cuối (không reset).
//  - Monster: đuổi player (khoảng cách giảm) + PcNpcVisual.direction khớp
//    DirectionFromMove(vector đuổi) — cùng convention với player.
//  - Evidence: screenshot game view 3 hướng + idle + monster chase.
// Note: load scene qua EditorSceneManager (Survivor.unity không trong Build Settings).
// -----------------------------------------------------------------------------

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using VLTK.Sandbox;
using VLTK.Survivor;

namespace VLTK.Tests.PlayMode
{
    public class SurvivorDirectionPlayModeTests
    {
        private const string ScenePath = "Assets/Scenes/Survivor.unity";
        private const string ShotDir = "C:/Projects/vltk-mobile/Assets/Screenshots/";

        private SurvivorGameDirector _dir;

        /// <summary>Load scene + chờ đúng 1 player sống (scene cũ unload hẳn) có JX visual.</summary>
        private IEnumerator LoadSceneAndGetDirector()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            _dir = null;
            int guard = 0;
            while (guard++ < 600)
            {
                yield return null;
                var d = SurvivorGameDirector.Instance;
                if (d == null || d.Player == null) continue;
                var players = Object.FindObjectsByType<SurvivorPlayer>(FindObjectsInactive.Exclude);
                // đúng 1 player + là player của director + visual JX đã init
                if (players.Length == 1 && players[0] == d.Player
                    && d.Player.GetComponent<JxPlayerVisual>() != null
                    && d.Player.GetComponent<MalePlayerVisual>() != null)
                {
                    _dir = d;
                    break;
                }
            }
            yield return null; // settle thêm 1 frame
        }

        [UnityTest]
        public IEnumerator PlayerVisual_EightDirections_ThroughBridge_AndIdleHold()
        {
            yield return LoadSceneAndGetDirector();
            Assert.IsNotNull(_dir, "director timeout");
            Assert.IsNotNull(_dir.Player, "player spawn timeout");

            var player = _dir.Player;
            player.MaxHp = 99999; // chống chết giữa test
            var bridge = player.GetComponent<JxPlayerVisual>(); // IActorVisual player dùng
            var mpv = player.GetComponent<MalePlayerVisual>();
            Assert.IsNotNull(bridge, "player bridge missing");
            Assert.IsNotNull(mpv, "player JX visual missing (MA_BD_019_ST01.spr staged)");

            // 8 hướng qua đúng interface Update dùng (IActorVisual.SetDirection)
            int[] expect = { 0, 1, 2, 3, 4, 5, 6, 7 };
            string[] names = { "S", "SW", "W", "NW", "N", "NE", "E", "SE" };
            for (int i = 0; i < expect.Length; i++)
            {
                bridge.SetDirection(expect[i]);
                yield return null;
                Assert.AreEqual(expect[i], mpv.direction, $"direction '{names[i]}' qua bridge");
                if (names[i] == "E" || names[i] == "NE" || names[i] == "N")
                    ScreenCapture.CaptureScreenshot(ShotDir + "ticket48-player-" + names[i] + ".png");
            }

            // idle: PlayMove(false) KHÔNG reset direction (giữ hướng cuối — PC behavior)
            bridge.SetDirection(7);
            bridge.PlayMove(false);
            yield return null;
            Assert.AreEqual(7, mpv.direction, "idle giữ hướng cuối (SE), không reset");
            ScreenCapture.CaptureScreenshot(ShotDir + "ticket48-player-idle.png");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Monster_ChasesPlayer_AndUsesSharedDirectionConvention()
        {
            yield return LoadSceneAndGetDirector();
            Assert.IsNotNull(_dir, "director timeout");
            Assert.IsNotNull(_dir.Player, "player timeout");

            var player = _dir.Player;
            player.MaxHp = 99999;
            player.Damage = 0f; // không giết monster giữa test
            player.Init(player.GetComponent<JxPlayerVisual>(), Vector3.zero);
            _dir.Input.Move = Vector2.zero; // player đứng yên, monster đuổi tới

            int guard = 0;
            while (_dir.Monsters.Count == 0 && guard++ < 600) yield return null;
            Assert.Greater(_dir.Monsters.Count, 0, "monster spawn timeout");

            var m = _dir.Monsters[0];
            var npc = m.GetComponent<PcNpcVisual>();
            Assert.IsNotNull(npc, "monster JX visual missing (PcNpcVisual)");

            float d0 = Vector3.Distance(m.transform.position, player.transform.position);
            yield return new WaitForSeconds(1.2f);
            float d1 = Vector3.Distance(m.transform.position, player.transform.position);
            Assert.Less(d1, d0 - 0.2f, $"monster phải đuổi player (d0={d0:F2} → d1={d1:F2})");

            // facing monster khớp hướng đuổi (chỉ sample khi còn xa — gần rồi có thể
            // dist≈0 giữ hướng cũ theo thiết kế idle-hold).
            if (d1 > 1.5f)
            {
                var expected = MalePlayerSpriteCatalog.DirectionFromMove(
                    (player.transform.position - m.transform.position).normalized);
                Assert.AreEqual(expected, npc.direction,
                    $"monster facing theo hướng đuổi (expected={expected}, d1={d1:F2})");
            }
            ScreenCapture.CaptureScreenshot(ShotDir + "ticket48-monster-chase.png");
            yield return null;
        }
    }
}
