using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    public abstract class State 
    {
      
        protected GameFlowSystem GameManager;

        public State(GameFlowSystem gameManager)
        {
            GameManager = gameManager;
        }

        public virtual IEnumerator Start()
        {
            yield break;
        }

        public virtual IEnumerator TakeTurn()
        {
            yield break;
        }

        public virtual IEnumerator Exit()
        {
            yield break;
        }


    }
}
