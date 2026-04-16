using System;
using UnityEngine;
using Mirror;

namespace Battleship
{
    /// <summary>
    /// Per-player NetworkBehaviour.  One instance is spawned for each connected
    /// player.  Handles sending attacks (Commands) and receiving results (RPCs).
    ///
    /// EDITOR SETUP:
    /// 1. Create an empty GameObject, name it "PlayerPrefab".
    /// 2. Add a NetworkIdentity component to it.
    /// 3. Add this BattleshipPlayer script to it.
    /// 4. Drag it into the Project window (Assets/Prefabs) to make it a prefab.
    /// 5. Delete the instance from the scene.
    /// 6. Drag the prefab into BattleshipNetManager → Player Prefab field.
    /// </summary>
    public class BattleshipPlayer : NetworkBehaviour
    {
        // ── Synced field ──
        [SyncVar] public int PlayerIndex = -1;

        // ── Local state ──
        int  _myPlayerIndex = -1;
        bool _isMyTurn;
        int[] _myBoardData;

        /// <summary>Static shortcut to the local player instance.</summary>
        public static BattleshipPlayer LocalInstance { get; private set; }

        // ── Events (subscribed to by LobbyUI / NetworkBoardSetup) ──
        public static event Action<string> OnLobbyStatus;           // status message
        public static event Action<int>    OnGameStarted;            // my player index
        public static event Action<int>    OnTurnChanged;            // whose turn (0/1)
        public static event Action<int, int, bool, bool, bool>
                                           OnAttackResultReceived;   // x, z, isHit, isSunk, isMyAttack
        public static event Action<int>    OnGameOver;               // winner index
        public static event Action         OnOpponentDisconnected;
        public static event Action         OnBoardDataReceived;

        // ────────────────────── Lifecycle ──────────────────────

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            LocalInstance = this;
            _myPlayerIndex = PlayerIndex;
            Debug.Log($"[Client] I am Player {_myPlayerIndex}");
        }

        void OnDestroy()
        {
            if (LocalInstance == this)
                LocalInstance = null;
        }

        // ────────────────────── Public helpers ──────────────────────

        public int  GetMyPlayerIndex() => _myPlayerIndex;
        public bool IsMyTurn()         => _isMyTurn;
        public int[] GetMyBoardData()  => _myBoardData;

        /// <summary>
        /// Called by Tile when the local player clicks on an opponent tile.
        /// </summary>
        public void RequestAttack(int x, int z)
        {
            if (!isLocalPlayer) return;
            if (!_isMyTurn)
            {
                Debug.Log("[Client] Not your turn!");
                return;
            }
            CmdAttack(x, z);
        }

        // ────────────────────── Command  (Client → Server) ──────────────────────

        [Command]
        void CmdAttack(int x, int z)
        {
            BattleshipNetManager.singleton.ServerProcessAttack(PlayerIndex, x, z);
        }

        // ────────────────────── TargetRpcs  (Server → one client) ──────────────────────

        /// <summary>Lobby status message (only to this client).</summary>
        [TargetRpc]
        public void TargetLobbyStatus(NetworkConnectionToClient target, string msg)
        {
            OnLobbyStatus?.Invoke(msg);
        }

        /// <summary>Receive own board ship layout (secure – only this client).</summary>
        [TargetRpc]
        public void TargetReceiveOwnBoard(NetworkConnectionToClient target, int[] boardData)
        {
            _myBoardData = boardData;
            Debug.Log($"[Client] Received board data ({boardData.Length} cells)");
            OnBoardDataReceived?.Invoke();
        }

        // ────────────────────── ClientRpcs  (Server → all clients) ──────────────────────

        [ClientRpc]
        public void RpcGameStarted(int myIndex, int startingTurn)
        {
            if (!isLocalPlayer) return;

            _myPlayerIndex = myIndex;
            _isMyTurn = (startingTurn == _myPlayerIndex);

            Debug.Log($"[Client] Game started! I am Player {_myPlayerIndex}. " +
                      $"Starting turn: Player {startingTurn}");
            OnGameStarted?.Invoke(_myPlayerIndex);
            OnTurnChanged?.Invoke(startingTurn);
        }

        [ClientRpc]
        public void RpcAttackResult(int attackerIndex, int defenderIndex,
                                     int x, int z,
                                     bool isHit, bool isSunk,
                                     int shipId, bool isWin)
        {
            if (!isLocalPlayer) return;

            bool isMyAttack = attackerIndex == _myPlayerIndex;

            Debug.Log($"[Client] Attack ({x},{z}): " +
                      $"{(isHit ? "HIT" : "MISS")} {(isSunk ? "(SUNK!)" : "")} " +
                      $"by Player {attackerIndex}");

            OnAttackResultReceived?.Invoke(x, z, isHit, isSunk, isMyAttack);
        }

        [ClientRpc]
        public void RpcTurnChanged(int newTurn)
        {
            if (!isLocalPlayer) return;

            _isMyTurn = newTurn == _myPlayerIndex;
            Debug.Log($"[Client] Turn → Player {newTurn}. " +
                      $"{(_isMyTurn ? "MY TURN!" : "Waiting...")}");
            OnTurnChanged?.Invoke(newTurn);
        }

        [ClientRpc]
        public void RpcGameOver(int winnerIndex)
        {
            if (!isLocalPlayer) return;

            Debug.Log($"[Client] Game Over! Winner: Player {winnerIndex} " +
                      $"{(winnerIndex == _myPlayerIndex ? "(ME!)" : "(opponent)")}");
            OnGameOver?.Invoke(winnerIndex);
        }

        [ClientRpc]
        public void RpcOpponentDisconnected()
        {
            if (!isLocalPlayer) return;

            Debug.Log("[Client] Opponent disconnected!");
            OnOpponentDisconnected?.Invoke();
        }
    }
}
