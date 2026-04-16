using System.Collections;
using UnityEngine;

namespace Battleship
{
    internal class Prepare : State
    {
        public Prepare(GameFlowSystem gameManager) : base(gameManager)
        {
        }

        public override IEnumerator Start()
        {
            // In online mode, skip the prepare screen since players don't share a screen
            if (GameManager.IsOnlineMode)
            {
                GameManager.SwitchPlayer();
                GameManager.SetState(new PlayerTurn(GameManager));
                yield break;
            }

            GameManager.SwitchPlayer();
            GameManager.UI.SetPlayerText(GameManager.CurrentPlayer);
            GameManager.UI.TogglePanel();
            yield break;
        }


    }
}