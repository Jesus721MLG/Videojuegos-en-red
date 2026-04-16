using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    /// <summary>
    /// Handles the visual board setup for networked play.
    /// After BattleshipPlayer receives board data from the server,
    /// this script renders ship indicators on the defence board
    /// and manages tile-click permissions per turn.
    ///
    /// EDITOR SETUP:
    /// 1. Create an empty GameObject in the Battle scene named "NetworkBoardSetup".
    /// 2. Add this script to it.
    /// 3. Drag both Board GameObjects (Board 0 and Board 1) into the
    ///    _boards array in the Inspector.
    ///    • Board at index 0 = Player 0's board (left).
    ///    • Board at index 1 = Player 1's board (right).
    /// 4. Assign a Material to _shipIndicatorMaterial if you want a custom look;
    ///    otherwise ships will be shown as a dark-grey tint on the tile sprite.
    /// </summary>
    public class NetworkBoardSetup : MonoBehaviour
    {
        [Header("Board References (0 = left, 1 = right)")]
        [SerializeField] BoardGenerator[] _boards = new BoardGenerator[2];

        [Header("Ship Indicator (optional)")]
        [Tooltip("Material applied to tiles that contain a ship on your own board. " +
                 "Leave empty for a default grey tint.")]
        [SerializeField] Material _shipIndicatorMaterial;

        [Header("Tile layers")]
        [Tooltip("Layer name used for clickable attack tiles (must match your Physics Raycaster).")]
        [SerializeField] string _clickableLayer = "GameBoard";
        [Tooltip("Layer name used for non-clickable tiles.")]
        [SerializeField] string _ignoreLayer = "Ignore Raycast";

        int _myIndex = -1;

        void OnEnable()
        {
            BattleshipPlayer.OnGameStarted         += OnGameStarted;
            BattleshipPlayer.OnBoardDataReceived   += OnBoardDataReceived;
            BattleshipPlayer.OnTurnChanged         += OnTurnChanged;
            BattleshipPlayer.OnAttackResultReceived += OnAttackResult;
        }

        void OnDisable()
        {
            BattleshipPlayer.OnGameStarted         -= OnGameStarted;
            BattleshipPlayer.OnBoardDataReceived   -= OnBoardDataReceived;
            BattleshipPlayer.OnTurnChanged         -= OnTurnChanged;
            BattleshipPlayer.OnAttackResultReceived -= OnAttackResult;
        }

        // ────────────────────── Event handlers ──────────────────────

        void OnGameStarted(int myIndex)
        {
            _myIndex = myIndex;

            // Make sure both boards have generated their tiles
            foreach (var bg in _boards)
            {
                if (bg != null && bg.TileList.Count == 0)
                    bg.GenerateBoard();
            }

            Tile.IsNetworked = true;

            // Disable clicking on BOTH boards initially
            SetBoardClickable(0, false);
            SetBoardClickable(1, false);
        }

        void OnBoardDataReceived()
        {
            if (_myIndex < 0) return;

            int[] data = BattleshipPlayer.LocalInstance?.GetMyBoardData();
            if (data == null || data.Length < 100) return;

            // Render ship indicators on MY board (defence board)
            BoardGenerator myBoard = _boards[_myIndex];
            if (myBoard == null) return;

            List<GameObject> tiles = myBoard.TileList;
            for (int i = 0; i < 100 && i < tiles.Count; i++)
            {
                if (data[i] > 0) // cell has a ship
                {
                    Tile t = tiles[i].GetComponent<Tile>();
                    if (t != null)
                        t.NetworkMarkAsShip(_shipIndicatorMaterial);
                }
            }
        }

        void OnTurnChanged(int currentTurn)
        {
            if (_myIndex < 0) return;

            bool myTurn = currentTurn == _myIndex;
            int opponentBoardIndex = 1 - _myIndex;

            // Enable clicking on opponent's board only when it is my turn
            SetBoardClickable(opponentBoardIndex, myTurn);
        }

        void OnAttackResult(int x, int z, bool isHit, bool isSunk, bool isMyAttack)
        {
            if (_myIndex < 0) return;

            // Determine which visual board to update:
            //   My attack   → update the OPPONENT's board (so I see my shots)
            //   Enemy attack → update MY board           (so I see incoming shots)
            int boardIndex = isMyAttack ? (1 - _myIndex) : _myIndex;

            BoardGenerator board = _boards[boardIndex];
            if (board == null) return;

            int tileIndex = x * 10 + z;
            if (tileIndex < 0 || tileIndex >= board.TileList.Count) return;

            Tile tile = board.TileList[tileIndex].GetComponent<Tile>();
            if (tile == null) return;

            if (isHit)
                tile.NetworkApplyHit();
            else
                tile.NetworkApplyMiss();

            // Audio feedback
            if (AudioPlayer.Instance != null)
            {
                AudioPlayer.Instance.Play(isHit ? "Hit" : "Miss");
                if (isSunk) AudioPlayer.Instance.Play("Sunk");
            }
        }

        // ────────────────────── Helpers ──────────────────────

        void SetBoardClickable(int boardIndex, bool clickable)
        {
            if (boardIndex < 0 || boardIndex >= _boards.Length) return;
            BoardGenerator bg = _boards[boardIndex];
            if (bg == null) return;

            int layer = LayerMask.NameToLayer(clickable ? _clickableLayer : _ignoreLayer);
            if (layer < 0) return;

            foreach (GameObject tileGO in bg.TileList)
                tileGO.layer = layer;
        }
    }
}
