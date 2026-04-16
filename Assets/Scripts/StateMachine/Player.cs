using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    [System.Serializable]
    public class Player
    {
        public List<GameObject> PlacedShipsList = new List<GameObject>();
        [SerializeField] GameObject _myBoard;

        public GameObject PlayerBoard => _myBoard;
    }
}
