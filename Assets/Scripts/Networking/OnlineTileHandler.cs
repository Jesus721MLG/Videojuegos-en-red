using UnityEngine;

namespace Battleship
{
    /// <summary>
    /// Handles tile interactions in online mode.
    /// Converts tile clicks to network coordinates and sends them to the server.
    /// Also processes incoming attack results from the server to update tile visuals.
    /// 
    /// Attach this to any persistent GameObject in the Battle scene when in online mode.
    /// </summary>
    public class OnlineTileHandler : MonoBehaviour
    {
        [SerializeField] SO_TileData _tileData;

        [Header("Board References")]
        [SerializeField] GameObject _board0;
        [SerializeField] GameObject _board1;

        /// <summary>
        /// Board tile arrays indexed by [x * 10 + z] for quick lookup.
        /// </summary>
        Tile[,] _board0Tiles = new Tile[10, 10];
        Tile[,] _board1Tiles = new Tile[10, 10];

        bool _initialized;
        int _localPlayerIndex = -1;

        void OnEnable()
        {
            NetworkPlayer.OnAttackResultReceived += HandleAttackResult;
            NetworkPlayer.OnTurnChanged += HandleTurnChanged;
            NetworkPlayer.OnGameWon += HandleGameWon;
            NetworkPlayer.OnPlayerIndexAssigned += HandlePlayerIndexAssigned;
            OnlineGameManager.OnOnlineGameReady += HandleGameReady;
        }

        void OnDisable()
        {
            NetworkPlayer.OnAttackResultReceived -= HandleAttackResult;
            NetworkPlayer.OnTurnChanged -= HandleTurnChanged;
            NetworkPlayer.OnGameWon -= HandleGameWon;
            NetworkPlayer.OnPlayerIndexAssigned -= HandlePlayerIndexAssigned;
            OnlineGameManager.OnOnlineGameReady -= HandleGameReady;
        }

        void HandlePlayerIndexAssigned(int index)
        {
            if (NetworkPlayer.LocalPlayer != null &&
                NetworkPlayer.LocalPlayer.PlayerIndex.Value == index)
            {
                _localPlayerIndex = index;
            }
        }

        void HandleGameReady()
        {
            InitializeBoards();
        }

        /// <summary>
        /// Initializes the tile lookup arrays from the physical board objects.
        /// </summary>
        public void InitializeBoards()
        {
            if (_board0 != null)
                BuildTileMap(_board0, _board0Tiles);

            if (_board1 != null)
                BuildTileMap(_board1, _board1Tiles);

            _initialized = true;

            // Set local player index
            if (NetworkPlayer.LocalPlayer != null)
            {
                _localPlayerIndex = NetworkPlayer.LocalPlayer.PlayerIndex.Value;
            }

            SetupBoardInteractivity();
        }

        void BuildTileMap(GameObject board, Tile[,] tileMap)
        {
            Vector3 boardPos = board.transform.position;

            foreach (Transform child in board.transform)
            {
                Tile tile = child.GetComponent<Tile>();
                if (tile == null) continue;

                int x = Mathf.RoundToInt(child.position.x - boardPos.x);
                int z = Mathf.RoundToInt(child.position.z - boardPos.z);

                if (x >= 0 && x < 10 && z >= 0 && z < 10)
                {
                    tileMap[x, z] = tile;
                }
            }
        }

        /// <summary>
        /// In online mode, each player can only click the opponent's board.
        /// Player 0 clicks board 1, Player 1 clicks board 0.
        /// </summary>
        void SetupBoardInteractivity()
        {
            if (_localPlayerIndex < 0) return;

            // Disable clicking on own board
            GameObject ownBoard = _localPlayerIndex == 0 ? _board0 : _board1;
            BoardManager ownBoardManager = ownBoard.GetComponent<BoardManager>();
            ownBoardManager.ToggleBoardLayer(ownBoardManager.LayerIgnoreRaycast);

            // Enable clicking on opponent's board (when it's our turn)
            UpdateBoardClickability(OnlineGameManager.Instance.CurrentTurn);
        }

        /// <summary>
        /// Sends an attack to the server when the local player clicks a tile.
        /// Called from Tile.OnMouseUp() in online mode.
        /// </summary>
        public void SendAttack(Tile clickedTile)
        {
            if (!_initialized || _localPlayerIndex < 0) return;
            if (NetworkPlayer.LocalPlayer == null) return;

            // Determine which board the tile belongs to and get coordinates
            Vector2Int coords = GetTileCoordinates(clickedTile);
            if (coords.x < 0) return;

            NetworkPlayer.LocalPlayer.AttackTileServerRpc(coords.x, coords.y);
        }

        Vector2Int GetTileCoordinates(Tile tile)
        {
            // Check opponent's board
            GameObject opponentBoard = _localPlayerIndex == 0 ? _board1 : _board0;
            Vector3 boardPos = opponentBoard.transform.position;
            Vector3 tilePos = tile.transform.position;

            int x = Mathf.RoundToInt(tilePos.x - boardPos.x);
            int z = Mathf.RoundToInt(tilePos.z - boardPos.z);

            if (x >= 0 && x < 10 && z >= 0 && z < 10)
                return new Vector2Int(x, z);

            return new Vector2Int(-1, -1);
        }

        /// <summary>
        /// Processes the attack result from the server and updates tile visuals.
        /// </summary>
        void HandleAttackResult(int tileX, int tileZ, bool isHit, bool isShipDestroyed, int shipId)
        {
            if (!_initialized) return;

            // Determine which board was attacked
            // The current turn player attacked the opponent's board
            int currentTurn = OnlineGameManager.Instance != null ? OnlineGameManager.Instance.CurrentTurn : 0;

            // If hit didn't change turn, current turn is still the attacker
            // The attacked board is the opponent of whoever's turn it is
            int attackedBoardIndex = currentTurn == 0 ? 1 : 0;

            // After a miss, the turn has already changed, so we need to look at
            // who was attacking before the change
            if (!isHit)
            {
                // Turn already changed, so current turn is now the defender
                // The attacked board is the current turn player's board
                attackedBoardIndex = currentTurn;
            }

            Tile[,] targetTiles = attackedBoardIndex == 0 ? _board0Tiles : _board1Tiles;

            if (tileX >= 0 && tileX < 10 && tileZ >= 0 && tileZ < 10)
            {
                Tile tile = targetTiles[tileX, tileZ];
                if (tile != null)
                {
                    tile.ApplyOnlineResult(isHit);

                    if (isHit)
                    {
                        AudioPlayer.Instance.Play("Hit");
                    }
                    else
                    {
                        AudioPlayer.Instance.Play("Miss");
                    }
                }
            }

            // If a ship was destroyed, show it
            if (isShipDestroyed && shipId > 0)
            {
                ShowDestroyedShip(attackedBoardIndex, shipId);
            }
        }

        void ShowDestroyedShip(int boardIndex, int shipId)
        {
            if (GameFlowSystem.Instance == null) return;

            var playerShips = GameFlowSystem.Instance.Players[boardIndex].PlacedShipsList;
            foreach (var shipObj in playerShips)
            {
                Ship ship = shipObj.GetComponent<Ship>();
                if (ship != null && ship.ShipID == shipId)
                {
                    foreach (Transform child in shipObj.transform)
                        child.gameObject.SetActive(true);
                    break;
                }
            }
        }

        void HandleTurnChanged(int currentPlayerIndex)
        {
            UpdateBoardClickability(currentPlayerIndex);
        }

        void UpdateBoardClickability(int currentPlayerIndex)
        {
            if (_localPlayerIndex < 0) return;

            GameObject opponentBoard = _localPlayerIndex == 0 ? _board1 : _board0;
            BoardManager opponentBoardManager = opponentBoard.GetComponent<BoardManager>();

            if (currentPlayerIndex == _localPlayerIndex)
            {
                // Our turn - enable clicking on opponent's board
                opponentBoardManager.ToggleBoardLayer(opponentBoardManager.LayerGameBoard);
            }
            else
            {
                // Not our turn - disable clicking
                opponentBoardManager.ToggleBoardLayer(opponentBoardManager.LayerIgnoreRaycast);
            }
        }

        void HandleGameWon(int winnerIndex)
        {
            // Disable all board clicking
            if (_board0 != null)
            {
                BoardManager bm = _board0.GetComponent<BoardManager>();
                bm.ToggleBoardLayer(bm.LayerIgnoreRaycast);
            }
            if (_board1 != null)
            {
                BoardManager bm = _board1.GetComponent<BoardManager>();
                bm.ToggleBoardLayer(bm.LayerIgnoreRaycast);
            }

            // Show win UI
            if (GameFlowSystem.Instance != null && GameFlowSystem.Instance.UI != null)
            {
                string winnerText = winnerIndex == _localPlayerIndex ? "YOU WIN!" : "YOU LOSE!";
                GameFlowSystem.Instance.UI.SetDisplayOnlineWinPanel(winnerText);
            }
        }
    }
}
