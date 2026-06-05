// -----------------------------------------------------------------------------
// VLTK Mobile — Network Entity Manager
// Remote-player spawn/sync foundation. No third-party netcode dependency.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Network
{
    public sealed class NetworkEntityManager : MonoBehaviour
    {
        public GameObject remotePlayerPrefab;
        public float positionLerpSpeed = 12f;

        private readonly Dictionary<int, GameObject> _networkPlayers = new();
        private readonly Dictionary<int, Vector3> _targetPositions = new();

        public IReadOnlyDictionary<int, GameObject> NetworkPlayers => _networkPlayers;

        private void Update()
        {
            float t = 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime);
            foreach (var pair in _networkPlayers)
            {
                if (!_targetPositions.TryGetValue(pair.Key, out var target) || pair.Value == null) continue;
                pair.Value.transform.position = Vector3.Lerp(pair.Value.transform.position, target, t);
            }
        }

        // Marker trên GameObject để biết entry này là placeholder sinh ra từ SyncPosition
        // (chưa có PlayerJoinMsg). SpawnRemotePlayer với tên thật sẽ destroy placeholder
        // và tạo lại với name chính xác.
        private const string PlaceholderPrefix = "RemotePlayer_";

        public GameObject SpawnRemotePlayer(PlayerJoinMsg msg)
        {
            if (_networkPlayers.TryGetValue(msg.playerId, out var existing) && existing != null)
            {
                // Nếu entry hiện tại là placeholder (name=?) và msg mang tên thật, replace.
                if (existing.name.StartsWith(PlaceholderPrefix + msg.playerId + "_", StringComparison.Ordinal)
                    && existing.name.EndsWith("?", StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(msg.playerName))
                {
                    Destroy(existing);
                    _networkPlayers.Remove(msg.playerId);
                    _targetPositions.Remove(msg.playerId);
                }
                else
                {
                    return existing;
                }
            }

            var go = remotePlayerPrefab != null
                ? Instantiate(remotePlayerPrefab, transform)
                : new GameObject($"RemotePlayer_{msg.playerId}_{msg.playerName}");

            go.name = $"RemotePlayer_{msg.playerId}_{msg.playerName}";
            _networkPlayers[msg.playerId] = go;
            _targetPositions[msg.playerId] = go.transform.position;
            return go;
        }

        public void RemoveRemotePlayer(int playerId)
        {
            if (_networkPlayers.TryGetValue(playerId, out var go) && go != null)
                Destroy(go);
            _networkPlayers.Remove(playerId);
            _targetPositions.Remove(playerId);
        }

        public void SyncPosition(PlayerPositionMsg msg)
        {
            // Tránh permanent wrong display name: nếu player chưa từng Join (chỉ gửi Position),
            // tạo placeholder mang tên "?" để PlayerJoinMsg sau đó replace bằng tên thật
            // trong SpawnRemotePlayer(). Không bao giờ commit tên "Người chơi" vào scene.
            if (!_networkPlayers.ContainsKey(msg.playerId))
            {
                SpawnRemotePlayer(new PlayerJoinMsg
                {
                    playerId = msg.playerId,
                    playerName = "?",
                    sectId = 0,
                });
            }
            _targetPositions[msg.playerId] = new Vector3(msg.x, msg.y, msg.z);
        }
    }
}
