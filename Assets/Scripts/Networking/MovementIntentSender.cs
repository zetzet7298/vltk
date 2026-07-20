using System;
using System.Threading;
using System.Threading.Tasks;
using VLTK.SkillPort;

namespace VLTK.Production.Networking
{
    public readonly struct MoveIntent
    {
        public readonly float x;
        public readonly float y;
        public readonly int quantizedX;
        public readonly int quantizedY;
        public readonly ulong clientTick;
        public readonly ulong targetTick;
        public readonly string commandId;
        public readonly string requestId;
        public readonly uint? facingMillirad;

        public MoveIntent(float x, float y, int quantizedX, int quantizedY, ulong clientTick, ulong targetTick = 0, string commandId = null, string requestId = null, uint? facingMillirad = null)
        {
            this.x = x;
            this.y = y;
            this.quantizedX = quantizedX;
            this.quantizedY = quantizedY;
            this.clientTick = clientTick;
            this.targetTick = targetTick;
            this.commandId = commandId;
            this.requestId = requestId;
            this.facingMillirad = facingMillirad;
        }

        public bool HasMovement => Math.Abs(x) > 0.0001f || Math.Abs(y) > 0.0001f;
    }

    public readonly struct MovementSessionGate
    {
        public readonly bool contentVerified;
        public readonly bool realtimeAdmitted;

        public MovementSessionGate(bool contentVerified, bool realtimeAdmitted)
        {
            this.contentVerified = contentVerified;
            this.realtimeAdmitted = realtimeAdmitted;
        }
    }

    public readonly struct MovementSendResult
    {
        public readonly bool sent;
        public readonly string failureCode;

        public MovementSendResult(bool sent, string failureCode)
        {
            this.sent = sent;
            this.failureCode = failureCode;
        }
    }

    public interface IPlayerInputMoveEncoder
    {
        byte[] EncodeMoveInput(MoveIntent intent);
    }

    public sealed class MovementIntentSender
    {
        private readonly IRealtimeBinarySocket _socket;
        private readonly IPlayerInputMoveEncoder _encoder;

        public MovementIntentSender(IRealtimeBinarySocket socket, IPlayerInputMoveEncoder encoder)
        {
            _socket = socket;
            _encoder = encoder;
        }

        public async Task<MovementSendResult> TrySendAsync(
            MoveIntent intent,
            MovementSessionGate gate,
            CancellationToken cancellationToken)
        {
            if (!gate.contentVerified)
                return new MovementSendResult(false, "content_not_verified");
            if (!gate.realtimeAdmitted)
                return new MovementSendResult(false, "realtime_not_admitted");
            if (!intent.HasMovement)
                return new MovementSendResult(false, "empty_move");
            if (_socket == null)
                return new MovementSendResult(false, "socket_missing");
            if (_encoder == null)
                return new MovementSendResult(false, "move_input_encoder_missing");

            byte[] payload = _encoder.EncodeMoveInput(intent);
            if (payload == null || payload.Length == 0)
                return new MovementSendResult(false, "move_input_encoder_missing");

            await _socket.SendBinaryAsync(payload, cancellationToken).ConfigureAwait(false);
            return new MovementSendResult(true, null);
        }
    }
}
