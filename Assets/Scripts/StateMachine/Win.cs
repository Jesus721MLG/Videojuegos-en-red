using System.Collections;

namespace Battleship
{
    internal class Win : State
    {
        public Win(GameFlowSystem gameManager) : base(gameManager)
        {
        }

        public override IEnumerator Start()
        {
            GameManager.UI.SetPlayersTextOpacity(GameManager.CurrentPlayer, 1, GameManager.CurrentOpponent, 1);
            GameManager.UI.SetDisplayWinPanel(GameManager.CurrentPlayer);

            yield break;
        }
    }
}