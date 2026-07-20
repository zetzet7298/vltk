using System;
using System.Threading;
using System.Threading.Tasks;
using VLTK.SkillPort;

namespace VLTK.Production.Networking
{
    public sealed class RealtimeAdmission
    {
        public string endpoint;
        public string admissionTicket;
        public string clientVersion;
        public VerifiedContentResponse content;

        public bool IsValid()
        {
            return RealtimeEndpointPolicy.IsProductionWss(endpoint)
                && !string.IsNullOrWhiteSpace(admissionTicket)
                && content != null
                && content.IsValid();
        }
    }

    public readonly struct RealtimeAdmissionResult
    {
        public readonly bool admitted;
        public readonly string failureCode;

        public RealtimeAdmissionResult(bool admitted, string failureCode)
        {
            this.admitted = admitted;
            this.failureCode = failureCode;
        }
    }

    public interface IRealtimeHelloEncoder
    {
        byte[] EncodeClientHello(RealtimeAdmission admission);
    }

    public interface IRealtimeAdmissionAckDecoder
    {
        RealtimeAdmissionResult DecodeAdmissionAck(byte[] payload);
    }

    public interface IRealtimeInitialSnapshotDecoder
    {
        void DecodeInitialSnapshot(byte[] payload);
    }

    public interface IRealtimeAdmissionClient
    {
        Task<RealtimeAdmissionResult> AdmitAsync(RealtimeAdmission admission, CancellationToken cancellationToken);
    }

    public sealed class RealtimeAdmissionClient : IRealtimeAdmissionClient, IDisposable
    {
        private readonly Func<IRealtimeBinarySocket> _socketFactory;
        private readonly IRealtimeHelloEncoder _helloEncoder;
        private readonly IRealtimeAdmissionAckDecoder _ackDecoder;
        private IRealtimeBinarySocket _socket;

        public IRealtimeBinarySocket ActiveSocket => _socket;

        public RealtimeAdmissionClient(
            Func<IRealtimeBinarySocket> socketFactory,
            IRealtimeHelloEncoder helloEncoder,
            IRealtimeAdmissionAckDecoder ackDecoder)
        {
            if (socketFactory == null) throw new ArgumentNullException(nameof(socketFactory));
            _socketFactory = socketFactory;
            _helloEncoder = helloEncoder;
            _ackDecoder = ackDecoder;
        }

        public async Task<RealtimeAdmissionResult> AdmitAsync(RealtimeAdmission admission, CancellationToken cancellationToken)
        {
            if (admission == null || !admission.IsValid())
                return new RealtimeAdmissionResult(false, "invalid_admission");
            if (_helloEncoder == null)
                return new RealtimeAdmissionResult(false, "client_hello_encoder_missing");
            if (_ackDecoder == null)
                return new RealtimeAdmissionResult(false, "admission_ack_decoder_missing");

            byte[] hello = _helloEncoder.EncodeClientHello(admission);
            if (hello == null || hello.Length == 0)
                return new RealtimeAdmissionResult(false, "client_hello_encoder_missing");

            if (_socket != null)
                return new RealtimeAdmissionResult(false, "socket_already_admitted");

            IRealtimeBinarySocket socket = _socketFactory();
            try
            {
                await socket.ConnectAsync(new Uri(admission.endpoint), cancellationToken).ConfigureAwait(false);
                await socket.SendBinaryAsync(hello, cancellationToken).ConfigureAwait(false);
                RealtimeReceiveMessage ack = await socket.ReceiveBinaryAsync(cancellationToken).ConfigureAwait(false);
                if (ack.kind != RealtimeReceiveKind.Binary || ack.payload == null || ack.payload.Length == 0)
                    return new RealtimeAdmissionResult(false, "admission_ack_missing");
                RealtimeAdmissionResult result = _ackDecoder.DecodeAdmissionAck(ack.payload);
                if (!result.admitted)
                    return result;
                if (_ackDecoder is IRealtimeInitialSnapshotDecoder snapshotDecoder)
                {
                    RealtimeReceiveMessage snapshot = await socket.ReceiveBinaryAsync(cancellationToken).ConfigureAwait(false);
                    if (snapshot.kind != RealtimeReceiveKind.Binary || snapshot.payload == null || snapshot.payload.Length == 0)
                        return new RealtimeAdmissionResult(false, "initial_snapshot_missing");
                    snapshotDecoder.DecodeInitialSnapshot(snapshot.payload);
                }
                _socket = socket;
                socket = null;
                return result;
            }
            finally
            {
                socket?.Dispose();
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();
            _socket = null;
        }

        public static RealtimeAdmissionClient CreateDefault(IRealtimeHelloEncoder helloEncoder, IRealtimeAdmissionAckDecoder ackDecoder)
        {
            return new RealtimeAdmissionClient(() => new ClientWebSocketBinaryTransport(), helloEncoder, ackDecoder);
        }
    }
}
