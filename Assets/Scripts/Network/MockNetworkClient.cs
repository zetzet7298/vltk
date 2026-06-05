// -----------------------------------------------------------------------------
// VLTK Mobile — Mock Network Client
// Simulates server echo with mobile latency for offline multiplayer tests.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VLTK.Network
{
    public sealed class MockNetworkClient : MonoBehaviour, INetworkClient
    {
        public int minLatencyMs = 50;
        public int maxLatencyMs = 200;
        public bool autoConnectOnStart;

        private readonly Queue<PendingMessage> _pending = new();
        private float _time;

        public bool IsConnected { get; private set; }
        public event Action<byte[]> OnMessageReceived;

        private struct PendingMessage
        {
            public float deliverAt;
            public byte[] payload;
        }

        private void Start()
        {
            if (autoConnectOnStart) Connect("mock", 0);
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            while (_pending.Count > 0 && _pending.Peek().deliverAt <= _time)
                OnMessageReceived?.Invoke(_pending.Dequeue().payload);
        }

        public void Connect(string host, int port)
        {
            IsConnected = true;
        }

        public void Disconnect()
        {
            IsConnected = false;
            _pending.Clear();
        }

        public void Send<T>(T message) where T : struct
        {
            if (!IsConnected) return;
            string json = JsonUtility.ToJson(message);
            int delay = UnityEngine.Random.Range(minLatencyMs, maxLatencyMs + 1);
            _pending.Enqueue(new PendingMessage
            {
                deliverAt = _time + delay / 1000f,
                payload = Encoding.UTF8.GetBytes(json),
            });
        }
    }
}
