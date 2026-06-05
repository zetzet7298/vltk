// -----------------------------------------------------------------------------
// VLTK Mobile — Network Client Contract
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Network
{
    public interface INetworkClient
    {
        bool IsConnected { get; }
        event Action<byte[]> OnMessageReceived;

        void Connect(string host, int port);
        void Disconnect();
        void Send<T>(T message) where T : struct;
    }
}
