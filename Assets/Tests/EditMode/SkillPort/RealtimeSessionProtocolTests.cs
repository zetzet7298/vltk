using System;
using NUnit.Framework;
using VLTK.SkillPort;

namespace VLTK.Tests.SkillPort
{
    [Category("SkillPort")]
    public class RealtimeSessionProtocolTests
    {
        [Test]
        public void SessionCursor_AllocatesAndAcceptsOrderedSequences()
        {
            var cursor = new RealtimeSessionCursor();
            cursor.Begin(sessionEpoch: 7, initialServerSequence: 10, initialServerTick: 100);
            Assert.AreEqual(1UL, cursor.AllocateClientSequence());
            Assert.AreEqual(2UL, cursor.AllocateClientSequence());

            Assert.AreEqual(
                ServerEnvelopeAcceptance.Accepted,
                cursor.AcceptServerEnvelope(7, 11, acknowledgedClientSequence: 1, serverTick: 101));
            Assert.AreEqual(11UL, cursor.lastAppliedServerSequence);
            Assert.AreEqual(1UL, cursor.lastAcknowledgedClientSequence);
        }

        [Test]
        public void SessionCursor_RejectsGapBadAckAndWrongEpochWithoutMutation()
        {
            var cursor = new RealtimeSessionCursor();
            cursor.Begin(7, 10, 100);
            cursor.AllocateClientSequence();

            Assert.AreEqual(
                ServerEnvelopeAcceptance.SequenceGap,
                cursor.AcceptServerEnvelope(7, 12, 0, 101));
            Assert.AreEqual(
                ServerEnvelopeAcceptance.InvalidAcknowledgement,
                cursor.AcceptServerEnvelope(7, 11, 2, 101));
            Assert.AreEqual(
                ServerEnvelopeAcceptance.EpochMismatch,
                cursor.AcceptServerEnvelope(8, 11, 1, 101));
            Assert.AreEqual(10UL, cursor.lastAppliedServerSequence);
        }

        [Test]
        public void SessionCursor_DuplicateServerEnvelopeIsIdempotent()
        {
            var cursor = new RealtimeSessionCursor();
            cursor.Begin(7, 10, 100);

            Assert.AreEqual(
                ServerEnvelopeAcceptance.Duplicate,
                cursor.AcceptServerEnvelope(7, 10, 0, 100));
            Assert.AreEqual(10UL, cursor.lastAppliedServerSequence);
        }

        [Test]
        public void WebSocketTransport_RequiresSecureEndpointByDefault()
        {
            using (var transport = new ClientWebSocketBinaryTransport())
            {
                Assert.Throws<ArgumentException>(() =>
                    transport.ConnectAsync(new Uri("ws://localhost/game"), default));
            }
        }

        [Test]
        public void WebSocketTransport_RejectsInvalidFrameLimit()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ClientWebSocketBinaryTransport(0));
        }
    }
}
