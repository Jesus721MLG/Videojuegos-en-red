using System.Collections;
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

        private void Awake()
        {
            if (!_shipPlacer)
                _shipPlacer = FindObjectOfType<ShipPlacer>();

            if (_clickBlocker.activeSelf)
                _clickBlocker.SetActive(false);
        }

        private void Start()
        {
            // In network mode the boards are still generated (tiles are needed)
            // but ship placement is handled by the server, not locally.
            if (BattleshipNetManager.singleton != null)
            {
                SetupNetworkGame();
                return;
            }

            SetupGame();
        }

        /// <summary>Local-only game setup (original flow).</summary>
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

            StartCoroutine(RevealScreen());
        }

        IEnumerator RevealScreen()
        {
            yield return new WaitForSeconds(2);
            _gameUI.FadeImage();
        }

        public void DisableClicks()
        {
            _clickBlocker.SetActive(true);
        }

        /// <summary>
        /// Network game setup: generate tile grids but skip ship placement
        /// (ships are placed server-side as data and sent to each client).
        /// </summary>
        void SetupNetworkGame()
        {
            foreach (GameObject board in _boards)
            {
                _boardGenerator = board.GetComponent<BoardGenerator>();
                _boardGenerator.GenerateBoard();

                _boardManager = board.GetComponent<BoardManager>();
                _boardManager.DisableClickingTiles();
            }

            // The LobbyUI panel is already visible; once both players
            // connect the NetworkBoardSetup will handle the rest.
            StartCoroutine(RevealScreen());
        }

    }
}
