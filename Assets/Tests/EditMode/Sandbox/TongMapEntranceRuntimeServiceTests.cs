using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class TongMapEntranceRuntimeServiceTests
    {
        [Test]
        public void GetMapEnterPos_UsesPcDefaultAndBorderOverride()
        {
            Assert.That(TongMapEntranceRuntimeService.TaskLpCountId, Is.EqualTo(1745));
            Assert.That(TongMapEntranceRuntimeService.GetMapEnterPos(587), Is.EqualTo(new TongMapEntranceCell(1718, 3313)));
            Assert.That(TongMapEntranceRuntimeService.GetMapEnterPos(591), Is.EqualTo(new TongMapEntranceCell(1712, 3330)));
        }

        [Test]
        public void DefaultRegion_BannedNonOwner_ReturnsToCurrentMapCopyAndMentionsTask()
        {
            var plan = Evaluate(new TongMapEntranceRequest
            {
                ProductRegion = "vn",
                MapType = 1,
                MapTongId = 10,
                PlayerTongId = 20,
                MapBan = 1,
                CurrentMapCopyId = 591,
                TaskLpCountValue = 3
            });

            Assert.That(plan.Decision, Is.EqualTo("default-banned-non-owner"));
            Assert.That(plan.Actions.Count, Is.EqualTo(2));
            AssertAction(plan.Actions[0], "SetPos", new TongMapEntranceCell(1712, 3330));
            AssertAction(plan.Actions[1], "PostMessage", TongMapEntranceRuntimeService.BanWithTaskMessage);
            Assert.That(ContainsAction(plan, "SetFightState"), Is.False);
        }

        [Test]
        public void DefaultRegion_OwnerOrOpenMap_DoesNothing()
        {
            var service = new TongMapEntranceRuntimeService();
            var owner = service.Evaluate(new TongMapEntranceRequest
            {
                ProductRegion = "vn",
                MapType = 1,
                MapTongId = 10,
                PlayerTongId = 10,
                MapBan = 1,
                CurrentMapCopyId = 591,
                TaskLpCountValue = 3
            });
            var openMap = service.Evaluate(new TongMapEntranceRequest
            {
                ProductRegion = "vn",
                MapType = 1,
                MapTongId = 10,
                PlayerTongId = 20,
                MapBan = 0,
                CurrentMapCopyId = 591
            });

            Assert.That(owner.Decision, Is.EqualTo("default-allowed"));
            Assert.That(owner.Actions, Is.Empty);
            Assert.That(openMap.Decision, Is.EqualTo("default-allowed"));
            Assert.That(openMap.Actions, Is.Empty);
        }

        [Test]
        public void CnIb_MainNoExpireWarning_SuppressesOwnerNearExpiryWarning()
        {
            var plan = Evaluate(new TongMapEntranceRequest
            {
                ProductRegion = "cn_ib",
                MapType = 1,
                MapTongId = 10,
                PlayerTongId = 10,
                MapBan = 1,
                ExpireState = 1,
                NoExpireWarning = 1,
                ExpireDateText = "2026-06-10",
                TemplateMapId = 591
            });

            Assert.That(plan.Decision, Is.EqualTo("cn_ib-allowed"));
            Assert.That(plan.Actions, Is.Empty);
        }

        [Test]
        public void CnIb_Expired_EjectsViaTemplateWithFightStateReset()
        {
            var plan = Evaluate(new TongMapEntranceRequest
            {
                ProductRegion = "cn_ib",
                MapType = 1,
                MapTongId = 10,
                PlayerTongId = 10,
                MapBan = 1,
                ExpireState = 2,
                NoExpireWarning = 1,
                TemplateMapId = 591
            });

            Assert.That(plan.Decision, Is.EqualTo("cn_ib-expired"));
            Assert.That(plan.Actions.Count, Is.EqualTo(3));
            AssertAction(plan.Actions[0], "PostMessage", TongMapEntranceRuntimeService.ExpiredMessage);
            AssertAction(plan.Actions[1], "SetFightState", 0);
            AssertAction(plan.Actions[2], "SetPos", new TongMapEntranceCell(1712, 3330));
        }

        [Test]
        public void CnIb_OwnerNearExpiryWarnsOnlyWhenWarningIsNotSuppressed()
        {
            var plan = Evaluate(new TongMapEntranceRequest
            {
                ProductRegion = "cn_ib",
                MapType = 1,
                MapTongId = 10,
                PlayerTongId = 10,
                MapBan = 1,
                ExpireState = 1,
                NoExpireWarning = 0,
                ExpireDateText = "2026-06-10",
                TemplateMapId = 591
            });

            Assert.That(plan.Decision, Is.EqualTo("cn_ib-near-expiry-owner-warning"));
            Assert.That(plan.Actions.Count, Is.EqualTo(1));
            Assert.That(plan.Actions[0].Kind, Is.EqualTo("PostMessage"));
            StringAssert.Contains("sắp đến kỳ hạn 2026-06-10", plan.Actions[0].Message);
            Assert.That(ContainsAction(plan, "SetPos"), Is.False);
            Assert.That(ContainsAction(plan, "SetFightState"), Is.False);
        }

        [Test]
        public void CnIb_BannedNonOwner_EjectsViaTemplateAndUsesTaskSensitiveMessage()
        {
            var plan = Evaluate(new TongMapEntranceRequest
            {
                ProductRegion = "cn_ib",
                MapType = 1,
                MapTongId = 10,
                PlayerTongId = 20,
                MapBan = 1,
                ExpireState = 0,
                NoExpireWarning = 1,
                TemplateMapId = 591,
                TaskLpCountValue = 0
            });

            Assert.That(plan.Decision, Is.EqualTo("cn_ib-banned-non-owner"));
            Assert.That(plan.Actions.Count, Is.EqualTo(3));
            AssertAction(plan.Actions[0], "PostMessage", TongMapEntranceRuntimeService.BanMessage);
            AssertAction(plan.Actions[1], "SetFightState", 0);
            AssertAction(plan.Actions[2], "SetPos", new TongMapEntranceCell(1712, 3330));
        }

        [Test]
        public void NonTongMapOrZeroTongId_ReturnsDeterministicNoOpPlanWithGaps()
        {
            var service = new TongMapEntranceRuntimeService();
            var nonTongMap = service.Evaluate(new TongMapEntranceRequest
            {
                ProductRegion = "vn",
                MapType = 0,
                MapTongId = 10,
                PlayerTongId = 20,
                MapBan = 1
            });
            var noOwner = service.Evaluate(new TongMapEntranceRequest
            {
                ProductRegion = "vn",
                MapType = 1,
                MapTongId = 0,
                PlayerTongId = 20,
                MapBan = 1
            });

            Assert.That(nonTongMap.Decision, Is.EqualTo("not-tong-map"));
            Assert.That(noOwner.Decision, Is.EqualTo("map-has-no-tong-owner"));
            Assert.That(nonTongMap.Actions, Is.Empty);
            Assert.That(noOwner.Actions, Is.Empty);
            Assert.That(nonTongMap.RemainingHostApiGaps, Does.Contain("TONG_GetTongMapTemplate"));
            Assert.That(noOwner.TaskLpCountId, Is.EqualTo(1745));
        }

        private static TongMapEntrancePlan Evaluate(TongMapEntranceRequest request)
        {
            return new TongMapEntranceRuntimeService().Evaluate(request);
        }

        private static bool ContainsAction(TongMapEntrancePlan plan, string kind)
        {
            foreach (var action in plan.Actions)
                if (action.Kind == kind)
                    return true;
            return false;
        }

        private static void AssertAction(TongMapEntranceAction action, string kind, TongMapEntranceCell position)
        {
            Assert.That(action.Kind, Is.EqualTo(kind));
            Assert.That(action.Position, Is.EqualTo(position));
        }

        private static void AssertAction(TongMapEntranceAction action, string kind, int fightState)
        {
            Assert.That(action.Kind, Is.EqualTo(kind));
            Assert.That(action.FightState, Is.EqualTo(fightState));
        }

        private static void AssertAction(TongMapEntranceAction action, string kind, string message)
        {
            Assert.That(action.Kind, Is.EqualTo(kind));
            Assert.That(action.Message, Is.EqualTo(message));
        }
    }
}
