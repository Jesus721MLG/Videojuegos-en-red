using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Battleship
{
    /// <summary>
    /// Custom NetworkManager for Battleship multiplayer.
    /// Handles player connections, server-side board data, ship placement,
    /// turn management, and attack validation.
    ///
    /// EDITOR SETUP:
    /// 1. Create an empty GameObject named "NetworkManager" in your Battle scene.
    /// 2. Add this script (BattleshipNetManager) to it.
    /// 3. Also add a "Kcp Transport" component (from Mirror) to the same GameObject.
    /// 4. Drag the Kcp Transport into the "Transport" field of BattleshipNetManager.
    /// 5. Create a Player Prefab (empty GameObject with NetworkIdentity + BattleshipPlayer),
    ///    save it in Assets/Prefabs, and drag it into the "Player Prefab" field.
    /// </summary>
    public class BattleshipNetManager : NetworkManager
    {
        [Header("Battleship Settings")]
        [Tooltip("Ship lengths to place on each board. Default: Carrier(5), Battleship(4), Cruiser(3), Submarine(3), Destroyer(2)")]
        [SerializeField] int[] _shipLengths = { 5, 4, 3, 3, 2 };

        // ── Server-side board data: 0 = water, >0 = shipId ──
        int[,] _board0 = new int[10, 10];
        int[,] _board1 = new int[10, 10];

        // Ship health tracking: shipId → remaining hit-points
        Dictionary<int, int> _shipHealth0 = new Dictionary<int, int>();
        Dictionary<int, int> _shipHealth1 = new Dictionary<int, int>();

        // Turn management
        int _currentTurn; // 0 = player 0, 1 = player 1
        bool _gameStarted;
        int _connectedPlayers;

        // Connected player references
        readonly List<BattleshipPlayer> _players = new List<BattleshipPlayer>();

        /// <summary>Convenience accessor (casts the base singleton).</summary>
        public static new BattleshipNetManager singleton =>
            (BattleshipNetManager)NetworkManager.singleton;

        public int CurrentTurn => _currentTurn;
        public bool GameStarted => _gameStarted;

        // ────────────────────── Connection callbacks ──────────────────────

        public override void OnStartServer()
        {
            base.OnStartServer();
            _connectedPlayers = 0;
            _gameStarted = false;
            _players.Clear();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            // Reset the static networked flag so local mode works correctly
            // if the player returns to the main menu and starts a local game.
            Tile.IsNetworked = false;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            Tile.IsNetworked = false;
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Create the player manually so we can set SyncVars BEFORE
            // spawning.  base.OnServerAddPlayer would spawn first, causing
            // a race where the client sees PlayerIndex == -1.
            GameObject playerObj = Instantiate(playerPrefab);
            var player = playerObj.GetComponent<BattleshipPlayer>();
            player.PlayerIndex = _connectedPlayers;
            _players.Add(player);
            _connectedPlayers++;

            NetworkServer.AddPlayerForConnection(conn, playerObj);

            Debug.Log($"[Server] Player {player.PlayerIndex} connected. Total: {_connectedPlayers}");

            // Notify all players about lobby count
            foreach (var p in _players)
                p.TargetLobbyStatus(p.connectionToClient,
                    _connectedPlayers >= 2
                        ? "Both players connected! Starting game..."
                        : $"Waiting for opponent... ({_connectedPlayers}/2)");

            if (_connectedPlayers == 2)
                ServerSetupAndStartGame();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            var player = conn.identity?.GetComponent<BattleshipPlayer>();
            if (player != null) _players.Remove(player);
            _connectedPlayers--;
            _gameStarted = false;

            foreach (var p in _players)
                p.RpcOpponentDisconnected();

            base.OnServerDisconnect(conn);
        }

        // ────────────────────── Game setup (server) ──────────────────────

        void ServerSetupAndStartGame()
        {
            PlaceShipsOnBoard(_board0, _shipHealth0);
            PlaceShipsOnBoard(_board1, _shipHealth1);

            // Send each player ONLY their own ship positions (TargetRpc = secure)
            _players[0].TargetReceiveOwnBoard(
                _players[0].connectionToClient, SerializeBoard(_board0));
            _players[1].TargetReceiveOwnBoard(
                _players[1].connectionToClient, SerializeBoard(_board1));

            _currentTurn = 0;
            _gameStarted = true;

            // Notify both players
            foreach (var p in _players)
            {
                p.RpcGameStarted(p.PlayerIndex, _currentTurn);
            }

            Debug.Log("[Server] Game started!");
        }

        // ────────────────────── Attack processing (server) ──────────────────────

        /// <summary>Called by BattleshipPlayer.CmdAttack on the server.</summary>
        public void ServerProcessAttack(int attackerIndex, int x, int z)
        {
            if (!_gameStarted) return;
            if (attackerIndex != _currentTurn) return;
            if (x < 0 || x >= 10 || z < 0 || z >= 10) return;

            int defenderIndex = 1 - attackerIndex;
            int[,] defBoard = defenderIndex == 0 ? _board0 : _board1;
            Dictionary<int, int> defHealth =
                defenderIndex == 0 ? _shipHealth0 : _shipHealth1;

            int cell = defBoard[x, z];

            // Already attacked
            if (cell < 0) return;

            bool isHit = cell > 0;
            bool isSunk = false;
            bool isWin = false;
            int shipId = cell;

            if (isHit)
            {
                defHealth[shipId]--;
                defBoard[x, z] = -1; // mark hit

                if (defHealth[shipId] <= 0)
                {
                    isSunk = true;
                    isWin = true;
                    foreach (int hp in defHealth.Values)
                    {
                        if (hp > 0) { isWin = false; break; }
                    }
                }
            }
            else
            {
                defBoard[x, z] = -2; // mark miss
            }

            // Broadcast result to both players
            foreach (var p in _players)
                p.RpcAttackResult(attackerIndex, defenderIndex, x, z,
                                  isHit, isSunk, shipId, isWin);

            if (isWin)
            {
                _gameStarted = false;
                foreach (var p in _players)
                    p.RpcGameOver(attackerIndex);
            }
            else if (!isHit)
            {
                // Miss → switch turn
                _currentTurn = defenderIndex;
                foreach (var p in _players)
                    p.RpcTurnChanged(_currentTurn);
            }
            // Hit → same player continues (no RpcTurnChanged needed)
        }

        // ────────────────────── Ship placement (server, data-only) ──────────────────────

        void PlaceShipsOnBoard(int[,] board, Dictionary<int, int> health)
        {
            System.Array.Clear(board, 0, board.Length);
            health.Clear();

            int shipId = 1;
            foreach (int length in _shipLengths)
            {
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < 1000)
                {
                    attempts++;
                    bool horizontal = Random.Range(0, 2) == 0;

                    int x = horizontal
                        ? Random.Range(0, 10 - length + 1)
                        : Random.Range(0, 10);
                    int z = horizontal
                        ? Random.Range(0, 10)
                        : Random.Range(0, 10 - length + 1);

                    if (CanPlaceShip(board, x, z, length, horizontal))
                    {
                        for (int i = 0; i < length; i++)
                        {
                            int px = horizontal ? x + i : x;
                            int pz = horizontal ? z : z + i;
                            board[px, pz] = shipId;
                        }
                        health[shipId] = length;
                        shipId++;
                        placed = true;
                    }
                }

                if (!placed)
                    Debug.LogError($"[Server] Failed to place ship of length {length}!");
            }
        }

        bool CanPlaceShip(int[,] board, int startX, int startZ, int length, bool horizontal)
        {
            for (int i = 0; i < length; i++)
            {
                int x = horizontal ? startX + i : startX;
                int z = horizontal ? startZ : startZ + i;

                if (x < 0 || x >= 10 || z < 0 || z >= 10) return false;
                if (board[x, z] != 0) return false;

                // No adjacent ships (including diagonals)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nx = x + dx, nz = z + dz;
                        if (nx >= 0 && nx < 10 && nz >= 0 && nz < 10
                            && board[nx, nz] != 0)
                            return false;
                    }
            }
            return true;
        }

        /// <summary>Flatten a 10×10 board into a 100-element array for RPC.</summary>
        int[] SerializeBoard(int[,] board)
        {
            int[] flat = new int[100];
            for (int x = 0; x < 10; x++)
                for (int z = 0; z < 10; z++)
                    flat[x * 10 + z] = board[x, z];
            return flat;
        }
    }
}
