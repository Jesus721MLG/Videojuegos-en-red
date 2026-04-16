using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] GameObject[] _boards;
        BoardGenerator _boardGenerator;
        BoardManager _boardManager;

        [SerializeField] ShipPlacer _shipPlacer;
        [SerializeField] GameUI _gameUI;
        [SerializeField] GameObject _clickBlocker;

        public GameObject[] Boards => _boards;

        private void Awake()
        {
            if (!_shipPlacer)
                _shipPlacer = FindObjectOfType<ShipPlacer>();

            if (_clickBlocker.activeSelf)
                _clickBlocker.SetActive(false);
        }

        private void Start()
        {
            SetupGame();
        }

        void SetupGame()
        {
            foreach (GameObject board in _boards)
            {
                _boardGenerator = board.GetComponent<BoardGenerator>();
                _boardGenerator.GenerateBoard();
            }

            _shipPlacer.SetUpBoards();

            foreach (GameObject board in _boards)
            {
                _boardManager = board.GetComponent<BoardManager>();
                _boardManager.DisableClickingTiles();
            }

            // In online mode, initialize the networked game state
            if (NetworkManagerBattleship.IsOnlineMode)
            {
                InitializeOnlineGame();
            }

            StartCoroutine(RevealScreen());
        }

        /// <summary>
        /// Initializes the OnlineGameManager with board and ship data from the
        /// physical scene. Only runs on the server/host.
        /// </summary>
        void InitializeOnlineGame()
        {
            OnlineGameManager onlineManager = FindObjectOfType<OnlineGameManager>();
            if (onlineManager == null || !onlineManager.IsServer) return;

            GameFlowSystem gfs = GameFlowSystem.Instance;
            if (gfs == null) return;

            List<GameObject> player0Ships = gfs.Players[0].PlacedShipsList;
            List<GameObject> player1Ships = gfs.Players[1].PlacedShipsList;

            onlineManager.InitializeBoardData(_boards[0], _boards[1], player0Ships, player1Ships);
        }

        IEnumerator RevealScreen()
        {
            yield return new WaitForSeconds(2);
            _gameUI.FadeImage();

            // In online mode, skip the Prepare state and let OnlineTileHandler manage turns
            if (NetworkManagerBattleship.IsOnlineMode)
            {
                _gameUI.SetOnlinePlayerInfo();
            }
        }

        public void DisableClicks()
        {
            _clickBlocker.SetActive(true);
        }

    }
}
