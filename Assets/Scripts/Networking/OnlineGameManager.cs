using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Battleship
{
    /// <summary>
    /// Server-authoritative game manager for online multiplayer mode.
    /// Runs on the server/host to manage the board state, process attacks,
    /// handle turns, and determine win conditions.
    /// 
    /// Works alongside the existing GameManager and GameFlowSystem,
    /// intercepting game logic when in online mode.
    /// </summary>
    public class OnlineGameManager : NetworkBehaviour
    {
        public static OnlineGameManager Instance { get; private set; }

        public static event Action<int> OnOnlineTurnChanged;
        public static event Action<int> OnOnlineGameWon;
        public static event Action<int, int, bool, bool, int> OnOnlineAttackResult;
        public static event Action OnOnlineGameReady;

        /// <summary>
        /// Represents the state of a single cell on the board.
        /// </summary>
        struct CellState
        {
            public int ShipId;   // 0 = no ship, > 0 = ship ID
            public bool Attacked;
        }

        /// <summary>
        /// Tracks the state of a ship for win condition checking.
        /// </summary>
        class ShipState
        {
            public int ShipId;
            public int Length;
            public int Hits;
            public bool IsDestroyed => Hits >= Length;

            public List<Vector2Int> OccupiedCells = new List<Vector2Int>();
        }

        // Board state: boardData[playerIndex][x, z]
        CellState[,] _board0 = new CellState[10, 10];
        CellState[,] _board1 = new CellState[10, 10];

        // Ship tracking per player
        Dictionary<int, ShipState> _player0Ships = new Dictionary<int, ShipState>();
        Dictionary<int, ShipState> _player1Ships = new Dictionary<int, ShipState>();

        NetworkVariable<int> _currentTurn = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        NetworkVariable<bool> _gameActive = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        int _readyPlayers;

        public int CurrentTurn => _currentTurn.Value;
        public bool GameActive => _gameActive.Value;

        void Awake()
        {
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            _currentTurn.OnValueChanged += HandleTurnChanged;

            if (IsServer)
            {
                _readyPlayers = 0;
            }
        }

        public override void OnNetworkDespawn()
        {
            _currentTurn.OnValueChanged -= HandleTurnChanged;
            Instance = null;
        }

        void HandleTurnChanged(int oldValue, int newValue)
        {
            OnOnlineTurnChanged?.Invoke(newValue);
        }

        /// <summary>
        /// Called by GameManager after boards and ships are set up.
        /// Server reads the physical board state and builds data structures.
        /// </summary>
        public void InitializeBoardData(GameObject board0, GameObject board1,
            List<GameObject> player0Ships, List<GameObject> player1Ships)
        {
            if (!IsServer) return;

            BuildBoardData(board0, _board0, _player0Ships, player0Ships);
            BuildBoardData(board1, _board1, _player1Ships, player1Ships);

            _gameActive.Value = true;
            _currentTurn.Value = 0;

            NotifyGameReadyClientRpc();
        }

        /// <summary>
        /// Reads the physical board tiles and ships to build the data model.
        /// </summary>
        void BuildBoardData(GameObject boardObj, CellState[,] boardData,
            Dictionary<int, ShipState> shipStates, List<GameObject> ships)
        {
            // Clear board
            for (int x = 0; x < 10; x++)
                for (int z = 0; z < 10; z++)
                    boardData[x, z] = new CellState { ShipId = 0, Attacked = false };

            // Register ships
            foreach (var shipObj in ships)
            {
                Ship ship = shipObj.GetComponent<Ship>();
                if (ship == null) continue;

                var shipState = new ShipState
                {
                    ShipId = ship.ShipID,
                    Length = ship.ShipData.ShipLength,
                    Hits = 0
                };

                // Find which tiles this ship occupies by raycasting
                foreach (Transform child in shipObj.transform)
                {
                    Ray ray = new Ray(child.position, Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, 10f))
                    {
                        Tile tile = hit.collider.GetComponent<Tile>();
                        if (tile != null)
                        {
                            Vector2Int coords = GetTileCoordinates(tile, boardObj);
                            if (coords.x >= 0)
                            {
                                boardData[coords.x, coords.y].ShipId = ship.ShipID;
                                shipState.OccupiedCells.Add(coords);
                            }
                        }
                    }
                }

                shipStates[ship.ShipID] = shipState;
            }
        }

        /// <summary>
        /// Gets the (x, z) grid coordinates of a tile relative to its board.
        /// </summary>
        Vector2Int GetTileCoordinates(Tile tile, GameObject boardObj)
        {
            Vector3 boardPos = boardObj.transform.position;
            Vector3 tilePos = tile.transform.position;

            int x = Mathf.RoundToInt(tilePos.x - boardPos.x);
            int z = Mathf.RoundToInt(tilePos.z - boardPos.z);

            if (x >= 0 && x < 10 && z >= 0 && z < 10)
                return new Vector2Int(x, z);

            return new Vector2Int(-1, -1);
        }

        /// <summary>
        /// Processes an attack from a player. Server-authoritative.
        /// </summary>
        public void ProcessAttack(int attackerIndex, int tileX, int tileZ)
        {
            if (!IsServer || !_gameActive.Value) return;
            if (attackerIndex != _currentTurn.Value) return;
            if (tileX < 0 || tileX >= 10 || tileZ < 0 || tileZ >= 10) return;

            int defenderIndex = attackerIndex == 0 ? 1 : 0;
            CellState[,] targetBoard = defenderIndex == 0 ? _board0 : _board1;
            Dictionary<int, ShipState> targetShips = defenderIndex == 0 ? _player0Ships : _player1Ships;

            // Check if already attacked
            if (targetBoard[tileX, tileZ].Attacked) return;

            targetBoard[tileX, tileZ].Attacked = true;

            int shipId = targetBoard[tileX, tileZ].ShipId;
            bool isHit = shipId > 0;
            bool isDestroyed = false;

            if (isHit && targetShips.ContainsKey(shipId))
            {
                targetShips[shipId].Hits++;
                isDestroyed = targetShips[shipId].IsDestroyed;
            }

            // Broadcast result to all clients
            BroadcastAttackResult(tileX, tileZ, isHit, isDestroyed, shipId);

            // Check win condition
            if (CheckWinCondition(targetShips))
            {
                _gameActive.Value = false;
                BroadcastGameOver(attackerIndex);
                return;
            }

            // If miss, switch turns
            if (!isHit)
            {
                _currentTurn.Value = defenderIndex;
            }
        }

        bool CheckWinCondition(Dictionary<int, ShipState> ships)
        {
            foreach (var ship in ships.Values)
            {
                if (!ship.IsDestroyed)
                    return false;
            }
            return true;
        }

        void BroadcastAttackResult(int tileX, int tileZ, bool isHit, bool isDestroyed, int shipId)
        {
            // Use the host player to broadcast
            foreach (var player in NetworkPlayer.AllPlayers)
            {
                if (player != null)
                {
                    player.AttackResultClientRpc(tileX, tileZ, isHit, isDestroyed, shipId);
                }
            }
        }

        void BroadcastGameOver(int winnerIndex)
        {
            foreach (var player in NetworkPlayer.AllPlayers)
            {
                if (player != null)
                {
                    player.GameOverClientRpc(winnerIndex);
                }
            }
        }

        [ClientRpc]
        void NotifyGameReadyClientRpc()
        {
            OnOnlineGameReady?.Invoke();
        }

        /// <summary>
        /// Called by the client to signal that it's ready to play.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void PlayerReadyServerRpc()
        {
            _readyPlayers++;
        }
    }
}
