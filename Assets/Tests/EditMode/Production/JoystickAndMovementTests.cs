using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using VLTK.Production.Networking;
using VLTK.Production.UI.Runtime;
using VLTK.SkillPort;

namespace VLTK.Tests.Production.EditMode
{
    public sealed class JoystickAndMovementTests
    {
        [Test]
        public void Joystick_DeadZoneSuppressesMovement()
        {
            var intent = ProductionJoystickInput.Quantize(new Vector2(0.05f, 0f));
            Assert.That(intent.active, Is.False);
            Assert.That(intent.move, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Joystick_QuantizesAndClampsMagnitude()
        {
            var intent = ProductionJoystickInput.Quantize(new Vector2(2f, 2f), 0.15f, 127);
            Assert.That(intent.active, Is.True);
            Assert.That(intent.move.magnitude, Is.LessThanOrEqualTo(1.0001f));
            Assert.That(intent.quantizedX, Is.GreaterThan(0));
            Assert.That(intent.quantizedY, Is.GreaterThan(0));
        }

        [Test]
        public async Task MovementSend_GatesOnVerifiedContentAndRealtimeAdmission()
        {
            var socket = new FakeSocket();
            var sender = new MovementIntentSender(socket, new FakeMoveEncoder());
            var intent = new MoveIntent(1f, 0f, 1000, 0, 9);

            var noContent = await sender.TrySendAsync(intent, new MovementSessionGate(false, true), CancellationToken.None);
            var noRealtime = await sender.TrySendAsync(intent, new MovementSessionGate(true, false), CancellationToken.None);
            var sent = await sender.TrySendAsync(intent, new MovementSessionGate(true, true), CancellationToken.None);

            Assert.That(noContent.failureCode, Is.EqualTo("content_not_verified"));
            Assert.That(noRealtime.failureCode, Is.EqualTo("realtime_not_admitted"));
            Assert.That(sent.sent, Is.True);
            Assert.That(socket.lastPayload, Is.EqualTo(new byte[] { 53, 1, 0, 9 }));
        }

        [Test]
        public async Task MovementSend_ReturnsEncoderSeam_WhenGeneratedMoveInputIsUnavailable()
        {
            var sender = new MovementIntentSender(new FakeSocket(), null);
            var result = await sender.TrySendAsync(new MoveIntent(1f, 0f, 1000, 0, 1), new MovementSessionGate(true, true), CancellationToken.None);
            Assert.That(result.sent, Is.False);
            Assert.That(result.failureCode, Is.EqualTo("move_input_encoder_missing"));
        }

        private sealed class FakeMoveEncoder : IPlayerInputMoveEncoder
        {
            public byte[] EncodeMoveInput(MoveIntent intent)
            {
                return new byte[] { 53, (byte)intent.x, (byte)intent.y, (byte)intent.clientTick };
            }
        }

        private sealed class FakeSocket : IRealtimeBinarySocket
        {
            public byte[] lastPayload;
            public WebSocketState state => WebSocketState.Open;
            public void Dispose() { }
            public Task ConnectAsync(System.Uri endpoint, CancellationToken cancellationToken) { return Task.CompletedTask; }
            public Task SendBinaryAsync(byte[] payload, CancellationToken cancellationToken) { lastPayload = payload; return Task.CompletedTask; }
            public Task<RealtimeReceiveMessage> ReceiveBinaryAsync(CancellationToken cancellationToken) { return Task.FromResult(default(RealtimeReceiveMessage)); }
            public Task CloseAsync(string reason, CancellationToken cancellationToken) { return Task.CompletedTask; }
        }
    }
}
