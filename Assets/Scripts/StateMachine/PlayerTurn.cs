using System;
using System.Collections;
using UnityEngine;

namespace Battleship
{
    internal class PlayerTurn : State
    {
        public static event Action OnWin;

        int _currentPlayer;
        GameObject _currentPlayerBoard;
        BoardManager _currentPlayerBoardManager;

        int _opponent;
        BoardManager _opponentBoardManager;

        public PlayerTurn(GameFlowSystem gameManager) : base(gameManager)
        {
            _currentPlayer = GameManager.CurrentPlayer;
            _currentPlayerBoard = GameManager.Players[_currentPlayer].PlayerBoard;
            _currentPlayerBoardManager = _currentPlayerBoard.GetComponent<BoardManager>();

            _opponent = GameManager.CurrentOpponent;
            _opponentBoardManager = GameManager.Players[_opponent].PlayerBoard.GetComponent<BoardManager>();
        }

        public override IEnumerator Start()
        {
            GameManager.UI.SetPlayersTextOpacity(_currentPlayer, 1, _opponent, 0.5f);

            _currentPlayerBoardManager.ToggleBoardLayer(_currentPlayerBoardManager.LayerIgnoreRaycast);
            _opponentBoardManager.ToggleBoardLayer(_opponentBoardManager.LayerGameBoard);

            DisplayCurrentPlayersShips();
            ZoomShips(true);
            ZoomBoard(true);
            yield return new WaitForSeconds(1f);

            GameManager.MoveToNextStage(TakeTurn());
        }

        public override IEnumerator TakeTurn()
        {
            yield return new WaitUntil(() => GameManager.TurnEnded || WinCheck());

            GameManager.MoveToNextStage(Exit());
        }

        public override IEnumerator Exit()
        {
            _opponentBoardManager.ToggleBoardLayer(_opponentBoardManager.LayerIgnoreRaycast);
            ZoomBoard(false);
            ZoomShips(false);
            
            yield return new WaitForSeconds(1f);

            if (WinCheck())
            {
                OnWin?.Invoke();

                yield return new WaitForSeconds(1f);
                GameManager.SetState(new Win(GameManager));
                yield break;
            }

            HideUndestroyedShips();
            GameManager.SetState(new Prepare(GameManager));
        }

        void HideUndestroyedShips()
        {
            foreach (GameObject ship in GameManager.Players[_currentPlayer].PlacedShipsList)
            {
                if (!ship.GetComponent<Ship>().ShipDestroyed)
                {
                    foreach (Transform child in ship.transform)
                        child.gameObject.SetActive(false);
                }
            }
        }

        void DisplayCurrentPlayersShips()
        {
            foreach (GameObject ship in GameManager.Players[_currentPlayer].PlacedShipsList)
            {
                foreach (Transform child in ship.transform)
                    child.gameObject.SetActive(true);
            }
        }

        bool WinCheck()
        {
            foreach (GameObject placedShip in GameManager.Players[_opponent].PlacedShipsList)
            {
                Ship shipInfo = placedShip.GetComponent<Ship>();

                if (!shipInfo.ShipDestroyed)
                    return false;
            }

            return true;
        }

        void ZoomBoard(bool zoomIn)
        {
            _currentPlayerBoard.GetComponent<ObjectZoomer>().ZoomOverTime(_currentPlayerBoard.transform, zoomIn);
        }

        void ZoomShips(bool zoomIn)
        {
            foreach (GameObject ship in GameManager.Players[_currentPlayer].PlacedShipsList)
                ship.GetComponent<ObjectZoomer>().ZoomOverTime(ship.transform, zoomIn); 
        }
    }
}