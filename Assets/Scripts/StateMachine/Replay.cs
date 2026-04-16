using System;
using System.Collections;
using UnityEngine;

namespace Battleship
{
    internal class Replay : State
    {
        public static event Action OnReplaySet;
        GameObject _board;

        public Replay(GameFlowSystem gameManager) : base(gameManager)
        {
        }

        public override IEnumerator Start()
        {
            GameManager.GetComponent<GameManager>().DisableClicks();
            GameManager.UI.ChangeLoadingText("PREPARING FOR REPLAY...");
            ResetBoards();
            
            yield return new WaitForSeconds(1f);
            
            GameManager.UI.FadeImage();
            OnReplaySet?.Invoke();

            yield return new WaitUntil(() => GameManager.ReplaySystem.CheckForReplayFinished());
            yield return new WaitForSeconds(2f);

            GameManager.UI.DisplayPostReplayPanel();
        }

        void ResetBoards()
        {
            foreach (Player player in GameManager.Players)
            {
                _board = player.PlayerBoard;
                BoardGenerator boardGenerator = _board.GetComponent<BoardGenerator>();
                BoardManager boardManager = _board.GetComponent<BoardManager>();

                boardManager.ToggleBoardLayer(boardManager.LayerGameBoard);

                foreach (var tile in boardGenerator.TileList)
                {
                    tile.SetActive(false);
                    tile.SetActive(true);
                }

                foreach (var ship in player.PlacedShipsList)
                {
                    Ship shipInfo = ship.GetComponent<Ship>();
                    GameObject zone = shipInfo.Zone;

                    ship.SetActive(false);
                    zone.SetActive(false);
                    ship.SetActive(true);
                    zone.SetActive(true);
                }
            }

            Debug.Log("Ready for replay");
        }
    }
}