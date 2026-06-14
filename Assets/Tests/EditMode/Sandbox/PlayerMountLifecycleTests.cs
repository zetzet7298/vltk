// -----------------------------------------------------------------------------
// VLTK Mobile — PlayerMountService EditMode tests.
// Kiểm tra mount lifecycle: Mount, Dismount, Tick transitions, host dispatch
// (refresh/SFX/started/completed/log/save), sprite path + action suffix.
// PC source: NpcS.txt HorseType, npcres/horse, 男主角骑马关联表.txt.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PlayerMountLifecycleTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IPlayerMountHost
        {
            public int VisualCalls;
            public int SfxCalls;
            public int StartCalls;
            public int CompleteCalls;
            public int DismountStartCalls;
            public int DismountCompleteCalls;
            public int LogCalls;
            public int SaveCalls;
            public int LastHorseType;
            public MountState LastState;
            public float LastSpeedMult;
            public int LastPlayerId;
            public bool LastIsMounting;
            public bool LastSaveIsMounted;

            public void RefreshMountVisual(int horseType, MountState newState, float speedMultiplier)
            {
                VisualCalls++;
                LastHorseType = horseType;
                LastState = newState;
                LastSpeedMult = speedMultiplier;
            }
            public void PlayMountSFX(int horseType, bool isMounting)
            {
                SfxCalls++;
                LastHorseType = horseType;
                LastIsMounting = isMounting;
            }
            public void OnMountStarted(int horseType, float transitionTime) { StartCalls++; LastHorseType = horseType; }
            public void OnMountCompleted(int horseType, float speedMultiplier) { CompleteCalls++; }
            public void OnDismountStarted(int horseType, float transitionTime) { DismountStartCalls++; }
            public void OnDismountCompleted() { DismountCompleteCalls++; }
            public void LogMountEvent(int horseType, string message) { LogCalls++; }
            public void SaveMountState(int playerId, int horseType, MountState state, bool isMounted)
            {
                SaveCalls++;
                LastPlayerId = playerId;
                LastSaveIsMounted = isMounted;
            }
        }

        // ── Ctor + state ────────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new PlayerMountService();
            Assert.AreEqual(MountState.None, svc.State);
            Assert.IsFalse(svc.IsMounted);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var svc = new PlayerMountService(host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new PlayerMountService();
            svc.AttachHost(host);
            svc.Mount(1);
            Assert.AreEqual(1, host.StartCalls);
        }

        // ── Mount ───────────────────────────────────────────────────────────

        [Test]
        public void Mount_StateNoneToMounting()
        {
            var svc = new PlayerMountService();
            svc.Mount(1);
            Assert.AreEqual(MountState.Mounting, svc.State);
            Assert.AreEqual(1, svc.HorseType);
        }

        [Test]
        public void Mount_AlreadyMounted_NoChange()
        {
            var svc = new PlayerMountService();
            svc.MountTransitionTime = 0.05f;
            svc.Mount(1);
            svc.Tick(0.1f);
            // Now Mounted
            int fired = 0;
            svc.OnMountChanged += e => fired++;
            svc.Mount(2); // already mounted
            Assert.AreEqual(0, fired);
        }

        [Test]
        public void Mount_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PlayerMountService(host);
            svc.Mount(1);
            Assert.AreEqual(1, host.VisualCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.StartCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void Mount_HostArgs()
        {
            var host = new FakeHost();
            var svc = new PlayerMountService(host);
            svc.Mount(5);
            Assert.AreEqual(5, host.LastHorseType);
            Assert.AreEqual(MountState.Mounting, host.LastState);
            Assert.IsTrue(host.LastIsMounting);
        }

        [Test]
        public void Mount_FiresOnMountChangedEvent()
        {
            var svc = new PlayerMountService();
            int fired = 0;
            svc.OnMountChanged += e => fired++;
            svc.Mount(1);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Mount_WithoutHost_DoesNotThrow()
        {
            var svc = new PlayerMountService();
            Assert.DoesNotThrow(() => svc.Mount(1));
        }

        // ── Tick transitions ────────────────────────────────────────────────

        [Test]
        public void Tick_MountingToMounted()
        {
            var svc = new PlayerMountService();
            svc.MountTransitionTime = 0.5f;
            svc.Mount(1);
            Assert.AreEqual(MountState.Mounting, svc.State);
            svc.Tick(0.6f);
            Assert.AreEqual(MountState.Mounted, svc.State);
            Assert.IsTrue(svc.IsMounted);
        }

        [Test]
        public void Tick_MountComplete_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PlayerMountService(host);
            svc.MountTransitionTime = 0.1f;
            svc.Mount(1);
            host.VisualCalls = 0; // reset (Mount called Visual once)
            host.CompleteCalls = 0;
            svc.Tick(0.2f);
            Assert.AreEqual(1, host.CompleteCalls);
            Assert.AreEqual(1, host.VisualCalls); // tick mount→Mounted refresh
        }

        [Test]
        public void Tick_NotInTransition_NoOp()
        {
            var svc = new PlayerMountService();
            int fired = 0;
            svc.OnMountChanged += e => fired++;
            svc.Tick(0.1f);
            Assert.AreEqual(0, fired);
        }

        // ── Dismount ────────────────────────────────────────────────────────

        [Test]
        public void Dismount_NotMounted_NoChange()
        {
            var svc = new PlayerMountService();
            int fired = 0;
            svc.OnMountChanged += e => fired++;
            svc.Dismount();
            Assert.AreEqual(0, fired);
        }

        [Test]
        public void Dismount_MountedToDismounting()
        {
            var svc = new PlayerMountService();
            svc.MountTransitionTime = 0.1f;
            svc.Mount(1);
            svc.Tick(0.2f); // → Mounted
            svc.Dismount();
            Assert.AreEqual(MountState.Dismounting, svc.State);
        }

        [Test]
        public void Dismount_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PlayerMountService(host);
            svc.MountTransitionTime = 0.1f;
            svc.Mount(1);
            svc.Tick(0.2f);
            host.SfxCalls = 0; // reset
            host.StartCalls = 0;
            svc.Dismount();
            Assert.AreEqual(1, host.DismountStartCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.IsFalse(host.LastIsMounting);
        }

        [Test]
        public void Tick_DismountingToNone()
        {
            var host = new FakeHost();
            var svc = new PlayerMountService(host);
            svc.MountTransitionTime = 0.1f;
            svc.Mount(1);
            svc.Tick(0.2f);
            svc.Dismount();
            svc.Tick(0.2f);
            Assert.AreEqual(MountState.None, svc.State);
            Assert.AreEqual(0, svc.HorseType);
            Assert.AreEqual(1, host.DismountCompleteCalls);
        }

        // ── Speed multiplier ────────────────────────────────────────────────

        [Test]
        public void SpeedMultiplier_NotMounted_One()
        {
            var svc = new PlayerMountService();
            Assert.AreEqual(1.0f, svc.SpeedMultiplier);
        }

        [Test]
        public void SpeedMultiplier_Mounted_OnePoint8()
        {
            var svc = new PlayerMountService();
            svc.MountTransitionTime = 0.1f;
            svc.Mount(1);
            svc.Tick(0.2f);
            Assert.AreEqual(1.8f, svc.SpeedMultiplier, 0.01f);
        }

        // ── Static mappings ─────────────────────────────────────────────────

        [Test]
        public void GetHorseSpritePath_Valid()
        {
            Assert.AreEqual(@"spr\npcres\horse\horse_001_stand.spr",
                PlayerMountService.GetHorseSpritePath(1, "stand"));
        }

        [Test]
        public void GetHorseSpritePath_Invalid_Null()
        {
            Assert.IsNull(PlayerMountService.GetHorseSpritePath(0, "stand"));
        }

        [Test]
        public void GetMountedActionSuffix_All()
        {
            Assert.AreEqual("RS01", PlayerMountService.GetMountedActionSuffix(PlayerVisualAction.Idle, PcWeaponType.ShortWeapon));
            Assert.AreEqual("RG01", PlayerMountService.GetMountedActionSuffix(PlayerVisualAction.Move, PcWeaponType.ShortWeapon));
            Assert.AreEqual("RA01", PlayerMountService.GetMountedActionSuffix(PlayerVisualAction.Attack, PcWeaponType.ShortWeapon));
            Assert.AreEqual("RM01", PlayerMountService.GetMountedActionSuffix(PlayerVisualAction.Magic, PcWeaponType.ShortWeapon));
        }

        [Test]
        public void GetHorseSpeedMultiplier_AllTypes()
        {
            Assert.AreEqual(1.6f, PlayerMountService.GetHorseSpeedMultiplier(1));
            Assert.AreEqual(1.8f, PlayerMountService.GetHorseSpeedMultiplier(2));
            Assert.AreEqual(2.0f, PlayerMountService.GetHorseSpeedMultiplier(3));
            Assert.AreEqual(1.8f, PlayerMountService.GetHorseSpeedMultiplier(99));
        }

        // ── PlayerId property ───────────────────────────────────────────────

        [Test]
        public void PlayerId_Default_Zero()
        {
            var svc = new PlayerMountService();
            Assert.AreEqual(0, svc.PlayerId);
        }

        [Test]
        public void Mount_PassesPlayerIdToSave()
        {
            var host = new FakeHost();
            var svc = new PlayerMountService(host) { PlayerId = 42 };
            svc.Mount(1);
            Assert.AreEqual(42, host.LastPlayerId);
        }

        // ── MountTransitionTime ─────────────────────────────────────────────

        [Test]
        public void MountTransitionTime_Property()
        {
            var svc = new PlayerMountService();
            svc.MountTransitionTime = 1.5f;
            Assert.AreEqual(1.5f, svc.MountTransitionTime);
        }
    }
}
