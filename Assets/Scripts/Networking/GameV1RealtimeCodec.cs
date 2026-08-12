using System;
using Game.V1;
using Google.Protobuf;
using VLTK.SkillPort;

namespace VLTK.Production.Networking
{
    public sealed class GameV1RealtimeCodec : IRealtimeHelloEncoder, IRealtimeAdmissionAckDecoder, IRealtimeInitialSnapshotDecoder, IPlayerInputMoveEncoder
    {
        private readonly RealtimeSessionCursor _cursor = new RealtimeSessionCursor();
        private VerifiedContentResponse _acceptedContent;

        public ulong SessionEpoch => _cursor.sessionEpoch;
        public ulong LastServerSeq => _cursor.lastAppliedServerSequence;
        public ulong LastServerTick => _cursor.lastAppliedServerTick;
        public WorldSnapshot LastSnapshot { get; private set; }

        public void BeginSession(ulong sessionEpoch, ulong serverSequence, ulong serverTick)
        {
            _cursor.Begin(sessionEpoch, serverSequence, serverTick);
        }

        public byte[] EncodeClientHello(RealtimeAdmission admission)
        {
            if (admission == null || admission.content == null)
                return null;
            _acceptedContent = admission.content;
            var hello = new ClientEnvelope
            {
                RequestId = "hello-1",
                Hello = new ClientHello
                {
                    Protocol = "game.v1",
                    Ticket = admission.admissionTicket ?? string.Empty,
                    ClientVersion = admission.clientVersion ?? string.Empty,
                    ContentReleaseId = admission.content.contentReleaseId ?? string.Empty,
                    SupportedReconnectGraceSeconds = 15,
                    AcceptedContent = ToProto(admission.content.contentDigest)
                }
            };
            return EncodeDelimited(hello);
        }

        public RealtimeAdmissionResult DecodeAdmissionAck(byte[] payload)
        {
            var server = DecodeDelimited<ServerEnvelope>(payload);
            if (server == null || server.Hello == null)
                return new RealtimeAdmissionResult(false, "server_hello_missing");
            ServerHello hello = server.Hello;
            if (hello.Protocol != "game.v1" || hello.TickRateHz != 18 || hello.ReconnectGraceSeconds != 15 || hello.SessionEpoch == 0)
                return new RealtimeAdmissionResult(false, "server_hello_invalid");
            if (_acceptedContent != null && !ContentMatches(hello.ActiveContent, _acceptedContent.contentDigest))
                return new RealtimeAdmissionResult(false, "content_digest_mismatch");
            _cursor.Begin(hello.SessionEpoch, server.ServerSeq, server.ServerTick);
            return new RealtimeAdmissionResult(true, null);
        }

        public void DecodeInitialSnapshot(byte[] payload)
        {
            var server = DecodeDelimited<ServerEnvelope>(payload);
            if (server == null || server.Snapshot == null || !server.Snapshot.Full)
                throw new InvalidOperationException("initial snapshot missing");
            ServerEnvelopeAcceptance accepted = _cursor.AcceptServerEnvelope(server.SessionEpoch, server.ServerSeq, server.LastProcessedClientSeq, server.ServerTick);
            if (accepted != ServerEnvelopeAcceptance.Accepted)
                throw new InvalidOperationException("initial snapshot cursor rejected: " + accepted);
            if (!SnapshotIsMap53(server.Snapshot))
                throw new InvalidOperationException("initial snapshot map 53 missing");
            LastSnapshot = server.Snapshot;
        }

        public byte[] EncodeMoveInput(MoveIntent intent)
        {
            if (!_cursor.initialized)
                return null;
            ulong seq = _cursor.AllocateClientSequence();
            string commandId = string.IsNullOrWhiteSpace(intent.commandId) ? "move-" + seq : intent.commandId;
            ulong targetTick = intent.targetTick == 0 ? intent.clientTick : intent.targetTick;
            var envelope = new ClientEnvelope
            {
                RequestId = string.IsNullOrWhiteSpace(intent.requestId) ? commandId : intent.requestId,
                SessionEpoch = _cursor.sessionEpoch,
                ClientSeq = seq,
                AckServerSeq = _cursor.lastAppliedServerSequence,
                InputBatch = new InputBatch
                {
                    Inputs = { new PlayerInput
                    {
                        InputSeq = seq,
                        TargetTick = targetTick,
                        CommandId = commandId,
                        Move = new MoveInput
                        {
                            AxisXMilli = ClampMilli(intent.quantizedX),
                            AxisYMilli = ClampMilli(intent.quantizedY),
                            FacingMillirad = intent.facingMillirad ?? FacingFromMilli(intent.quantizedX, intent.quantizedY),
                            Stop = !intent.HasMovement
                        }
                    }}
                }
            };
            return EncodeDelimited(envelope);
        }

        public static byte[] EncodeDelimited(IMessage message)
        {
            byte[] payload = message.ToByteArray();
            byte[] length = EncodeVarint((ulong)payload.Length);
            byte[] frame = new byte[length.Length + payload.Length];
            Buffer.BlockCopy(length, 0, frame, 0, length.Length);
            Buffer.BlockCopy(payload, 0, frame, length.Length, payload.Length);
            return frame;
        }

        public static T DecodeDelimited<T>(byte[] frame) where T : class, IMessage<T>, new()
        {
            if (frame == null || frame.Length == 0)
                return null;
            ulong length = 0;
            int shift = 0;
            int index = 0;
            while (index < frame.Length)
            {
                byte b = frame[index++];
                length |= (ulong)(b & 0x7f) << shift;
                if ((b & 0x80) == 0)
                    break;
                shift += 7;
                if (shift > 63)
                    return null;
            }
            if (length != (ulong)(frame.Length - index))
                return null;
            var parser = new MessageParser<T>(() => new T());
            return parser.ParseFrom(frame, index, frame.Length - index);
        }

        private static ContentDigest ToProto(ContentDigestDto digest)
        {
            if (digest == null) return null;
            return new ContentDigest
            {
                ContentReleaseId = digest.contentReleaseId ?? string.Empty,
                ManifestSha256 = digest.manifestSha256 ?? string.Empty,
                SourceSnapshotId = digest.sourceSnapshotId ?? string.Empty,
                CatalogUnionSize = digest.catalogUnionSize,
                CatalogUnionSha256 = digest.catalogUnionSha256 ?? string.Empty,
                RuntimeSkillPolicyId = digest.runtimeSkillPolicyId ?? string.Empty,
                ClientProjectionSha256 = digest.clientProjectionSha256 ?? string.Empty
            };
        }

        private static bool ContentMatches(ContentDigest actual, ContentDigestDto expected)
        {
            return actual != null && expected != null
                && actual.ContentReleaseId == expected.contentReleaseId
                && actual.ManifestSha256 == expected.manifestSha256
                && actual.SourceSnapshotId == expected.sourceSnapshotId
                && actual.CatalogUnionSize == expected.catalogUnionSize
                && actual.CatalogUnionSha256 == expected.catalogUnionSha256
                && actual.RuntimeSkillPolicyId == expected.runtimeSkillPolicyId
                && actual.ClientProjectionSha256 == expected.clientProjectionSha256;
        }

        private static bool SnapshotIsMap53(WorldSnapshot snapshot)
        {
            if (snapshot.Upserts == null || snapshot.Upserts.Count == 0) return false;
            for (int i = 0; i < snapshot.Upserts.Count; i++)
                if (snapshot.Upserts[i] != null && snapshot.Upserts[i].MapId == ProductionMapIds.CanonicalBootMapId) return true;
            return false;
        }

        private static byte[] EncodeVarint(ulong value)
        {
            byte[] tmp = new byte[10];
            int i = 0;
            while (value >= 0x80)
            {
                tmp[i++] = (byte)(value | 0x80);
                value >>= 7;
            }
            tmp[i++] = (byte)value;
            byte[] result = new byte[i];
            Buffer.BlockCopy(tmp, 0, result, 0, i);
            return result;
        }

        private static int ClampMilli(int value)
        {
            return Math.Max(-1000, Math.Min(1000, value));
        }

        private static uint FacingFromMilli(int x, int y)
        {
            if (x == 0 && y == 0)
                return 0;
            double angle = Math.Atan2(y, x);
            if (angle < 0) angle += Math.PI * 2;
            return (uint)Math.Round(angle * 1000.0);
        }
    }
}
