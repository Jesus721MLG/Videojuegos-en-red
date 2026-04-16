using System;
using Unity.Netcode;
using UnityEngine;

namespace Battleship
{
    /// <summary>
    /// Networked player component. Each connected player gets one of these spawned.
    /// Handles turn-based attacks, board state synchronization, and win conditions
    /// in a server-authoritative manner.
    /// </summary>
    public class NetworkPlayer : NetworkBehaviour
    {
        public static event Action<int, int, bool, bool, int, int> OnAttackResultReceived;
        public static event Action<int> OnTurnChanged;
        public static event Action<int> OnGameWon;
        public static event Action<int> OnPlayerIndexAssigned;
        public static event Action OnOpponentDisconnected;

        /// <summary>
        /// The player index (0 or 1) assigned by the server.
        /// </summary>
        public NetworkVariable<int> PlayerIndex { get; } = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        static NetworkPlayer _localPlayer;
        public static NetworkPlayer LocalPlayer => _localPlayer;

        static NetworkPlayer[] _allPlayers = new NetworkPlayer[2];
        public static NetworkPlayer[] AllPlayers => _allPlayers;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _localPlayer = this;
            }

            PlayerIndex.OnValueChanged += OnPlayerIndexChanged;

            if (IsServer)
            {
                AssignPlayerIndex();
            }
        }

        public override void OnNetworkDespawn()
        {
            PlayerIndex.OnValueChanged -= OnPlayerIndexChanged;

            if (IsOwner)
            {
                _localPlayer = null;
            }

            if (PlayerIndex.Value >= 0 && PlayerIndex.Value < 2)
            {
                _allPlayers[PlayerIndex.Value] = null;
            }
        }

        void AssignPlayerIndex()
        {
            if (_allPlayers[0] == null)
            {
                PlayerIndex.Value = 0;
                _allPlayers[0] = this;
            }
            else if (_allPlayers[1] == null)
            {
                PlayerIndex.Value = 1;
                _allPlayers[1] = this;
            }
        }

        void OnPlayerIndexChanged(int oldValue, int newValue)
        {
            if (newValue >= 0 && newValue < 2)
            {
                _allPlayers[newValue] = this;
                OnPlayerIndexAssigned?.Invoke(newValue);
            }
        }

        /// <summary>
        /// Called by the local player to attack a tile on the opponent's board.
        /// Sends the tile coordinates to the server for validation.
        /// </summary>
        [ServerRpc]
        public void AttackTileServerRpc(int tileX, int tileZ)
        {
            if (OnlineGameManager.Instance == null) return;

            OnlineGameManager.Instance.ProcessAttack(PlayerIndex.Value, tileX, tileZ);
        }

        /// <summary>
        /// Server sends the attack result to all clients for visual updates.
        /// Includes the attackedBoardIndex so clients don't need to infer it from turn state.
        /// </summary>
        [ClientRpc]
        public void AttackResultClientRpc(int tileX, int tileZ, bool isHit, bool isShipDestroyed, int shipId, int attackedBoardIndex)
        {
            OnAttackResultReceived?.Invoke(tileX, tileZ, isHit, isShipDestroyed, shipId, attackedBoardIndex);
        }

        /// <summary>
        /// Server notifies all clients that the turn has changed.
        /// </summary>
        [ClientRpc]
        public void ChangeTurnClientRpc(int currentPlayerIndex)
        {
            OnTurnChanged?.Invoke(currentPlayerIndex);
        }

        /// <summary>
        /// Server notifies all clients that a player has won.
        /// </summary>
        [ClientRpc]
        public void GameOverClientRpc(int winnerIndex)
        {
            OnGameWon?.Invoke(winnerIndex);
        }

        /// <summary>
        /// Server notifies all clients about the opponent disconnecting.
        /// </summary>
        [ClientRpc]
        public void OpponentDisconnectedClientRpc()
        {
            OnOpponentDisconnected?.Invoke();
        }
    }
}
