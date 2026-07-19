using System;
using Google.Protobuf;

namespace VLTK.SkillPort
{
    public static class GameV1SkillPortAdapters
    {
        public static global::Game.V1.ContentDigest ToProtoContentDigest(
            ContentReleaseDigest digest,
            string sourceSnapshotId,
            uint catalogUnionSize,
            string runtimeSkillPolicyId)
        {
            return ToProtoContentDigest(
                digest,
                sourceSnapshotId,
                catalogUnionSize,
                runtimeSkillPolicyId,
                string.Empty);
        }

        public static global::Game.V1.ContentDigest ToProtoContentDigest(
            ContentReleaseDigest digest,
            string sourceSnapshotId,
            uint catalogUnionSize,
            string runtimeSkillPolicyId,
            string clientProjectionSha256)
        {
            if (digest == null)
                throw new ArgumentNullException(nameof(digest));
            if (!digest.IsCanonical())
                throw new ArgumentException("content digest is non-canonical", nameof(digest));
            if (!string.IsNullOrEmpty(clientProjectionSha256) &&
                !ContentReleaseDigest.IsLowerHexSha256(clientProjectionSha256))
                throw new ArgumentException("client projection digest is non-canonical", nameof(clientProjectionSha256));

            return new global::Game.V1.ContentDigest
            {
                ContentReleaseId = digest.releaseId,
                ManifestSha256 = digest.manifestSha256,
                SourceSnapshotId = sourceSnapshotId ?? string.Empty,
                CatalogUnionSize = catalogUnionSize,
                CatalogUnionSha256 = digest.projectionSha256,
                RuntimeSkillPolicyId = runtimeSkillPolicyId ?? string.Empty,
                ClientProjectionSha256 = clientProjectionSha256 ?? string.Empty,
            };
        }

        public static global::Game.V1.ContentDigest ToProtoContentDigest(
            SkillPortClientProjection projection,
            string contentReleaseId,
            string sourceSnapshotId,
            string runtimeSkillPolicyId)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));

            return ToProtoContentDigest(
                new ContentReleaseDigest(
                    contentReleaseId,
                    projection.protocolManifestSha256,
                    projection.catalogUnionSha256),
                sourceSnapshotId,
                SkillPortClientProjection.ExpectedCatalogUnionSize,
                runtimeSkillPolicyId,
                projection.projectionSha256);
        }

        public static ContentReleaseDigest ToContentReleaseDigest(global::Game.V1.ContentDigest digest)
        {
            if (digest == null)
                return null;
            return new ContentReleaseDigest(
                digest.ContentReleaseId,
                digest.ManifestSha256,
                digest.CatalogUnionSha256);
        }

        public static global::Game.V1.RuntimeSkillPolicy ToProtoRuntimeSkillPolicy(
            SkillPortClientProjection projection,
            string policyId)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));

            return new global::Game.V1.RuntimeSkillPolicy
            {
                PolicyId = policyId ?? string.Empty,
                CatalogUnionSize = SkillPortClientProjection.ExpectedCatalogUnionSize,
                CatalogUnionSha256 = projection.catalogUnionSha256,
                FilesystemFallbackAllowed = false,
                RuntimeParityClaimed = false,
                PcRuntimeEvidenceStatus = "BLOCKED",
                AndroidPhysicalEvidenceStatus = "BLOCKED",
            };
        }

        public static RuntimePolicySnapshot ToRuntimePolicySnapshot(
            global::Game.V1.RuntimeSkillPolicy policy,
            SkillPortClientProjection projection,
            ulong policyRevision)
        {
            if (policy == null || projection == null ||
                policy.CatalogUnionSize != SkillPortClientProjection.ExpectedCatalogUnionSize ||
                policy.FilesystemFallbackAllowed || policy.RuntimeParityClaimed ||
                !string.Equals(policy.CatalogUnionSha256, projection.catalogUnionSha256, StringComparison.Ordinal))
            {
                var closed = new RuntimePolicySnapshot(policyRevision, true);
                return closed;
            }
            return SkillPortClientProjectionLoader.BuildRuntimePolicy(projection, policyRevision);
        }

        public static global::Game.V1.ClientHello ToProtoClientHello(
            string ticket,
            string clientVersion,
            SkillPortClientProjection acceptedProjection,
            ulong resumeSessionEpoch,
            ulong lastAppliedServerSeq,
            ulong lastAppliedServerTick)
        {
            if (acceptedProjection == null)
                throw new ArgumentNullException(nameof(acceptedProjection));

            return new global::Game.V1.ClientHello
            {
                Protocol = "game.v1",
                Ticket = ticket ?? string.Empty,
                ClientVersion = clientVersion ?? string.Empty,
                ContentReleaseId = acceptedProjection.contentReleaseId,
                ResumeSessionEpoch = resumeSessionEpoch,
                LastAppliedServerSeq = lastAppliedServerSeq,
                LastAppliedServerTick = lastAppliedServerTick,
                AcceptedContent = ToProtoContentDigest(
                    acceptedProjection,
                    acceptedProjection.contentReleaseId,
                    acceptedProjection.sourceSnapshotId,
                    acceptedProjection.runtimeSkillPolicyId),
                SupportedReconnectGraceSeconds = 15,
            };
        }

        public static ContentReleaseDigest ActiveContentDigest(global::Game.V1.ServerHello hello)
        {
            if (hello == null)
                return null;
            return ToContentReleaseDigest(hello.ActiveContent);
        }

        public static ServerEnvelopeAcceptance AcceptServerEnvelope(
            RealtimeSessionCursor cursor,
            global::Game.V1.ServerEnvelope envelope)
        {
            if (cursor == null)
                throw new ArgumentNullException(nameof(cursor));
            if (envelope == null)
                return ServerEnvelopeAcceptance.EpochMismatch;
            return cursor.AcceptServerEnvelope(
                envelope.SessionEpoch,
                envelope.ServerSeq,
                envelope.LastProcessedClientSeq,
                envelope.ServerTick);
        }

        public static CombatPresentationSnapshot ToPresentationSnapshot(
            global::Game.V1.ActiveCombatResyncState state,
            ulong serverSequence)
        {
            if (state == null)
                return null;

            var snapshot = new CombatPresentationSnapshot
            {
                serverSequence = serverSequence,
                baselineTick = state.BaselineTick,
            };

            foreach (global::Game.V1.ActiveCastState cast in state.ActiveCasts)
            {
                snapshot.casts.Add(new ActiveCastPresentation
                {
                    castId = cast.CastId,
                    skillId = CheckedInt(cast.SkillId),
                    sourceEntityId = cast.SourceEntityId,
                    startedAtTick = cast.StartedTick,
                    recoveryStartedAtTick = cast.RecoveryUntilTick,
                    recovering = cast.RecoveryUntilTick > 0,
                });
            }

            foreach (global::Game.V1.ActiveMissileState missile in state.ActiveMissiles)
            {
                snapshot.missiles.Add(new ActiveMissilePresentation
                {
                    missileInstanceId = missile.MissileInstanceId,
                    missileContentId = CheckedInt(missile.MissileId),
                    spawnedAtTick = missile.SpawnedTick,
                    lastLifecycleTick = missile.SpawnedTick,
                    phase = CombatLifecycleKind.MissileSpawned,
                    lastX = missile.X,
                    lastY = missile.Y,
                });
            }

            foreach (global::Game.V1.ActiveStatusState activeStatus in state.ActiveStatuses)
            {
                global::Game.V1.StatusEffectDelta status = activeStatus.Status;
                if (status == null)
                    continue;
                snapshot.statuses.Add(new ActiveStatusPresentation
                {
                    statusInstanceId = StatusInstanceId(activeStatus.TargetEntityId, status.EffectId),
                    targetEntityId = activeStatus.TargetEntityId,
                    statusEffectId = CheckedInt(status.EffectId),
                    revision = status.AppliedAtTick == 0 ? 1UL : status.AppliedAtTick,
                    expiresAtTick = status.ExpiresAtTick,
                });
            }

            return snapshot;
        }

        public static CombatLifecycleEvent ToLifecycleEvent(
            global::Game.V1.CombatEvent evt,
            ulong serverSequence)
        {
            if (evt == null)
                return null;

            global::Game.V1.StatusEffectDelta status = evt.StatusEffects.Count > 0 ? evt.StatusEffects[0] : null;
            return new CombatLifecycleEvent
            {
                eventId = evt.EventId,
                serverSequence = serverSequence,
                serverTick = evt.ServerTick,
                kind = ToLifecycleKind(evt.Kind),
                triggerPhase = ToTriggerPhase(evt.Kind),
                sourceEntityId = evt.SourceEntityId,
                targetEntityId = evt.TargetEntityId,
                skillId = CheckedInt(evt.SkillId),
                castId = evt.CastId,
                missileInstanceId = MissileInstanceId(evt),
                missileContentId = CheckedInt(evt.MissileId),
                statusInstanceId = status != null ? StatusInstanceId(evt.TargetEntityId, status.EffectId) : string.Empty,
                statusEffectId = status != null ? CheckedInt(status.EffectId) : 0,
                statusRevision = evt.LifecycleRevision,
                  expiresAtTick = status != null ? status.ExpiresAtTick : evt.LifecycleEndsTick,
                  impactX = evt.ImpactX,
                  impactY = evt.ImpactY,
                  skillLevel = CheckedInt(evt.SkillLevel),
                  animationId = CheckedInt(evt.AnimationId),
                  visualEffectId = CheckedInt(evt.VisualEffectId),
                  audioCueId = evt.AudioCueId,
                  stateCode = evt.StateCode,
                  value = evt.Value,
                  resultFlags = evt.ResultFlags,
              };
        }

        public static byte[] Serialize(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            return message.ToByteArray();
        }

        public static global::Game.V1.ClientEnvelope ParseClientEnvelope(byte[] bytes)
        {
            return global::Game.V1.ClientEnvelope.Parser.ParseFrom(bytes);
        }

        public static global::Game.V1.ServerEnvelope ParseServerEnvelope(byte[] bytes)
        {
            return global::Game.V1.ServerEnvelope.Parser.ParseFrom(bytes);
        }

        private static CombatLifecycleKind ToLifecycleKind(global::Game.V1.CombatEventKind kind)
        {
            switch (kind)
            {
                case global::Game.V1.CombatEventKind.CastStarted: return CombatLifecycleKind.CastStarted;
                case global::Game.V1.CombatEventKind.CastCancelled: return CombatLifecycleKind.CastCancelled;
                case global::Game.V1.CombatEventKind.MissileSpawned: return CombatLifecycleKind.MissileSpawned;
                case global::Game.V1.CombatEventKind.Hit: return CombatLifecycleKind.Hit;
                case global::Game.V1.CombatEventKind.Heal: return CombatLifecycleKind.Heal;
                case global::Game.V1.CombatEventKind.ResourceChanged: return CombatLifecycleKind.ResourceChanged;
                case global::Game.V1.CombatEventKind.StatusApplied: return CombatLifecycleKind.StatusApplied;
                case global::Game.V1.CombatEventKind.StatusRemoved: return CombatLifecycleKind.StatusRemoved;
                case global::Game.V1.CombatEventKind.Death: return CombatLifecycleKind.Death;
                case global::Game.V1.CombatEventKind.Revive: return CombatLifecycleKind.Revive;
                case global::Game.V1.CombatEventKind.CastRecoveryStarted: return CombatLifecycleKind.CastRecoveryStarted;
                case global::Game.V1.CombatEventKind.CastRecoveryEnded: return CombatLifecycleKind.CastRecoveryEnded;
                case global::Game.V1.CombatEventKind.MissileFlyStarted: return CombatLifecycleKind.MissileFlyTriggered;
                case global::Game.V1.CombatEventKind.MissileCollided: return CombatLifecycleKind.MissileCollided;
                case global::Game.V1.CombatEventKind.MissileVanished: return CombatLifecycleKind.MissileVanished;
                case global::Game.V1.CombatEventKind.StatusRefreshed: return CombatLifecycleKind.StatusRefreshed;
                case global::Game.V1.CombatEventKind.StatusExpired: return CombatLifecycleKind.StatusExpired;
                default: return CombatLifecycleKind.Unspecified;
            }
        }

        private static SkillTriggerPhase ToTriggerPhase(global::Game.V1.CombatEventKind kind)
        {
            switch (kind)
            {
                case global::Game.V1.CombatEventKind.CastStarted: return SkillTriggerPhase.CastStart;
                case global::Game.V1.CombatEventKind.CastRecoveryEnded: return SkillTriggerPhase.CastEnd;
                case global::Game.V1.CombatEventKind.MissileFlyStarted: return SkillTriggerPhase.MissileFly;
                case global::Game.V1.CombatEventKind.MissileCollided: return SkillTriggerPhase.MissileCollide;
                case global::Game.V1.CombatEventKind.MissileVanished: return SkillTriggerPhase.MissileVanish;
                default: return SkillTriggerPhase.Unspecified;
            }
        }

        private static string MissileInstanceId(global::Game.V1.CombatEvent evt)
        {
            if (evt.MissileId == 0)
                return string.Empty;
            // ponytail: proto has no missile_instance_id; presentation key only, replace if contract adds one.
            return evt.CastId + ":" + evt.MissileId + ":" + evt.HitIndex;
        }

        private static string StatusInstanceId(string targetEntityId, uint effectId)
        {
            return (targetEntityId ?? string.Empty) + ":" + effectId;
        }

        private static int CheckedInt(uint value)
        {
            if (value > int.MaxValue)
                throw new OverflowException("uint value exceeds Int32.MaxValue");
            return (int)value;
        }
    }
}
