using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace VLTK.SkillPort
{
    public enum ServerEnvelopeAcceptance
    {
        Accepted = 0,
        Duplicate = 1,
        EpochMismatch = 2,
        SequenceGap = 3,
        InvalidAcknowledgement = 4,
        TickRegression = 5,
    }

    public sealed class RealtimeSessionCursor
    {
        public ulong sessionEpoch { get; private set; }
        public ulong lastAllocatedClientSequence { get; private set; }
        public ulong lastAcknowledgedClientSequence { get; private set; }
        public ulong lastAppliedServerSequence { get; private set; }
        public ulong lastAppliedServerTick { get; private set; }
        public bool initialized { get; private set; }

        public void Begin(ulong sessionEpoch, ulong initialServerSequence, ulong initialServerTick)
        {
            if (sessionEpoch == 0)
                throw new ArgumentOutOfRangeException(nameof(sessionEpoch));

            this.sessionEpoch = sessionEpoch;
            lastAllocatedClientSequence = 0;
            lastAcknowledgedClientSequence = 0;
            lastAppliedServerSequence = initialServerSequence;
            lastAppliedServerTick = initialServerTick;
            initialized = true;
        }

        public ulong AllocateClientSequence()
        {
            if (!initialized)
                throw new InvalidOperationException("session cursor has not been initialized");
            lastAllocatedClientSequence = checked(lastAllocatedClientSequence + 1);
            return lastAllocatedClientSequence;
        }

        public ServerEnvelopeAcceptance AcceptServerEnvelope(
            ulong envelopeEpoch,
            ulong serverSequence,
            ulong acknowledgedClientSequence,
            ulong serverTick)
        {
            if (!initialized || envelopeEpoch != sessionEpoch)
                return ServerEnvelopeAcceptance.EpochMismatch;
            if (serverSequence <= lastAppliedServerSequence)
                return ServerEnvelopeAcceptance.Duplicate;
            if (serverSequence != lastAppliedServerSequence + 1)
                return ServerEnvelopeAcceptance.SequenceGap;
            if (acknowledgedClientSequence < lastAcknowledgedClientSequence ||
                acknowledgedClientSequence > lastAllocatedClientSequence)
                return ServerEnvelopeAcceptance.InvalidAcknowledgement;
            if (serverTick < lastAppliedServerTick)
                return ServerEnvelopeAcceptance.TickRegression;

            lastAppliedServerSequence = serverSequence;
            lastAppliedServerTick = serverTick;
            lastAcknowledgedClientSequence = acknowledgedClientSequence;
            return ServerEnvelopeAcceptance.Accepted;
        }
    }

    public enum RealtimeReceiveKind
    {
        Binary = 0,
        Closed = 1,
    }

    public readonly struct RealtimeReceiveMessage
    {
        public readonly RealtimeReceiveKind kind;
        public readonly byte[] payload;
        public readonly WebSocketCloseStatus? closeStatus;
        public readonly string closeDescription;

        public RealtimeReceiveMessage(
            RealtimeReceiveKind kind,
            byte[] payload,
            WebSocketCloseStatus? closeStatus,
            string closeDescription)
        {
            this.kind = kind;
            this.payload = payload;
            this.closeStatus = closeStatus;
            this.closeDescription = closeDescription;
        }
    }

    public interface IRealtimeBinarySocket : IDisposable
    {
        WebSocketState state { get; }
        Task ConnectAsync(Uri endpoint, CancellationToken cancellationToken);
        Task SendBinaryAsync(byte[] payload, CancellationToken cancellationToken);
        Task<RealtimeReceiveMessage> ReceiveBinaryAsync(CancellationToken cancellationToken);
        Task CloseAsync(string reason, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Bounded binary WebSocket transport for one length-delimited game.v1
    /// envelope per message. Protobuf encoding remains in the generated adapter.
    /// </summary>
    public sealed class ClientWebSocketBinaryTransport : IRealtimeBinarySocket
    {
        public const int DefaultMaxFrameBytes = 1_048_576;
        private const int ReceiveChunkBytes = 8192;

        private readonly ClientWebSocket _socket;
        private readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);
        private readonly int _maxFrameBytes;
        private readonly bool _allowInsecureForTests;
        private bool _disposed;

        public WebSocketState state => _socket.State;

        public ClientWebSocketBinaryTransport(
            int maxFrameBytes = DefaultMaxFrameBytes,
            bool allowInsecureForTests = false)
        {
            if (maxFrameBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxFrameBytes));

            _maxFrameBytes = maxFrameBytes;
            _allowInsecureForTests = allowInsecureForTests;
            _socket = new ClientWebSocket();
            _socket.Options.AddSubProtocol("game.v1");
        }

        public Task ConnectAsync(Uri endpoint, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            bool secure = string.Equals(endpoint.Scheme, "wss", StringComparison.OrdinalIgnoreCase);
            bool insecureTest = _allowInsecureForTests &&
                                string.Equals(endpoint.Scheme, "ws", StringComparison.OrdinalIgnoreCase);
            if (!secure && !insecureTest)
                throw new ArgumentException("production realtime endpoint must use wss", nameof(endpoint));
            if (_socket.State != WebSocketState.None)
                throw new InvalidOperationException("socket has already been used");

            return _socket.ConnectAsync(endpoint, cancellationToken);
        }

        public async Task SendBinaryAsync(byte[] payload, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (payload == null || payload.Length == 0 || payload.Length > _maxFrameBytes)
                throw new ArgumentOutOfRangeException(nameof(payload));
            if (_socket.State != WebSocketState.Open)
                throw new InvalidOperationException("socket is not open");

            await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _socket.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Binary,
                    true,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sendGate.Release();
            }
        }

        public async Task<RealtimeReceiveMessage> ReceiveBinaryAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (_socket.State != WebSocketState.Open && _socket.State != WebSocketState.CloseSent)
                throw new InvalidOperationException("socket cannot receive in its current state");

            byte[] chunk = new byte[ReceiveChunkBytes];
            using (var stream = new MemoryStream())
            {
                while (true)
                {
                    WebSocketReceiveResult result = await _socket.ReceiveAsync(
                        new ArraySegment<byte>(chunk), cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return new RealtimeReceiveMessage(
                            RealtimeReceiveKind.Closed,
                            null,
                            result.CloseStatus,
                            result.CloseStatusDescription);
                    }
                    if (result.MessageType != WebSocketMessageType.Binary)
                        throw new InvalidDataException("game.v1 accepts binary WebSocket messages only");
                    if (stream.Length + result.Count > _maxFrameBytes)
                        throw new InvalidDataException("realtime frame exceeds negotiated maximum");

                    stream.Write(chunk, 0, result.Count);
                    if (result.EndOfMessage)
                    {
                        if (stream.Length == 0)
                            throw new InvalidDataException("empty realtime frame");
                        return new RealtimeReceiveMessage(
                            RealtimeReceiveKind.Binary,
                            stream.ToArray(),
                            null,
                            null);
                    }
                }
            }
        }

        public async Task CloseAsync(string reason, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (_socket.State != WebSocketState.Open && _socket.State != WebSocketState.CloseReceived)
                return;
            await _socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                string.IsNullOrEmpty(reason) ? "client closing" : reason,
                cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _socket.Dispose();
            _sendGate.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ClientWebSocketBinaryTransport));
        }
    }
}
