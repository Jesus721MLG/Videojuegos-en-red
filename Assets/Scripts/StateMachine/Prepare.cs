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
            GameManager.SwitchPlayer();
            GameManager.UI.SetPlayerText(GameManager.CurrentPlayer);
            GameManager.UI.TogglePanel();
            yield break;
        }


    }
}