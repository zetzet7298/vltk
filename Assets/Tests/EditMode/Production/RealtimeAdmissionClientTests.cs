using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Production.Networking;
using VLTK.SkillPort;

namespace VLTK.Tests.Production.EditMode
{
    public sealed class RealtimeAdmissionClientTests
    {
        [Test]
        public async Task AdmitAsync_KeepsSocketOpen_ForMovementReuse_AfterHelloAndSnapshot()
        {
            var socket = new QueuedSocket(new byte[] { 1 }, new byte[] { 2 });
            var codec = new FakeCodec();
            var client = new RealtimeAdmissionClient(() => socket, codec, codec);

            var result = await client.AdmitAsync(new RealtimeAdmission { endpoint = "wss://realm.example/game", admissionTicket = "ticket", clientVersion = "editor-p1", content = ValidContent() }, CancellationToken.None);

            Assert.That(result.admitted, Is.True);
            Assert.That(client.ActiveSocket, Is.SameAs(socket));
            Assert.That(socket.disposed, Is.False);
            Assert.That(codec.snapshotDecoded, Is.True);
        }

        private static VerifiedContentResponse ValidContent()
        {
            return new VerifiedContentResponse
            {
                verified = true,
                mapId = 53,
                contentDigest = new ContentDigestDto
                {
                    contentReleaseId = "30000000-0000-0000-0000-000000000053",
                    manifestSha256 = new string('a', 64),
                    sourceSnapshotId = "map-runtime",
                    catalogUnionSize = 242,
                    catalogUnionSha256 = new string('b', 64),
                    runtimeSkillPolicyId = "p1",
                    clientProjectionSha256 = new string('c', 64)
                },
                trust = new ContentTrustResult(ContentTrustMode.EditorPinnedDigest, true, false, null),
                provenanceSha256 = new string('d', 64)
            };
        }

        private sealed class FakeCodec : IRealtimeHelloEncoder, IRealtimeAdmissionAckDecoder, IRealtimeInitialSnapshotDecoder
        {
            public bool snapshotDecoded;
            public byte[] EncodeClientHello(RealtimeAdmission admission) => new byte[] { 53 };
            public RealtimeAdmissionResult DecodeAdmissionAck(byte[] payload) => new RealtimeAdmissionResult(payload != null && payload.Length == 1 && payload[0] == 1, null);
            public void DecodeInitialSnapshot(byte[] payload) { snapshotDecoded = payload != null && payload.Length == 1 && payload[0] == 2; }
        }

        private sealed class QueuedSocket : IRealtimeBinarySocket
        {
            private readonly Queue<byte[]> _frames = new Queue<byte[]>();
            public bool disposed;
            public WebSocketState state => WebSocketState.Open;
            public QueuedSocket(params byte[][] frames) { foreach (byte[] frame in frames) _frames.Enqueue(frame); }
            public void Dispose() { disposed = true; }
            public Task ConnectAsync(Uri endpoint, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task SendBinaryAsync(byte[] payload, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task<RealtimeReceiveMessage> ReceiveBinaryAsync(CancellationToken cancellationToken) => Task.FromResult(new RealtimeReceiveMessage(RealtimeReceiveKind.Binary, _frames.Dequeue(), null, null));
            public Task CloseAsync(string reason, CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
