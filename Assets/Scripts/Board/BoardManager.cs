using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    public class BoardManager : MonoBehaviour
    {
        [SerializeField] ShipPlacer _shipPlacer;

        int _layerIgnoreRaycast;
        int _layerGameBoard;

        public int LayerIgnoreRaycast => _layerIgnoreRaycast;
        public int LayerGameBoard => _layerGameBoard;

        private void OnEnable()
        {
            ShipPlacer.OnAllShipsPlaced += HideShipsAndZones;
            Replay.OnReplaySet += HideShipsAndZones;
        }

        private void Start()
        {
            _layerIgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            _layerGameBoard = LayerMask.NameToLayer("GameBoard");
        }

        void HideShipsAndZones()
        {
            List<GameObject> spawnedShips = _shipPlacer.SpawnedShips;
            Dictionary<GameObject, GameObject> shipsAndZones = _shipPlacer.ShipDictionary;

            foreach (var ship in spawnedShips)
            {
                foreach (Transform child in ship.transform)
                    child.gameObject.SetActive(false);

                GameObject zone;
                shipsAndZones.TryGetValue(ship, out zone);

                foreach (Transform child in zone.transform)
                    child.gameObject.SetActive(false);
            }
        }

        public void ToggleBoardLayer(int layer)
        {
            Transform board = gameObject.transform;

            foreach (Transform child in board)
                child.gameObject.layer = layer;
        }

        public void DisableClickingTiles()
        {
            Transform board = gameObject.transform;

            foreach (Transform child in board)
                child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        private void OnDisable()
        {
            ShipPlacer.OnAllShipsPlaced -= HideShipsAndZones;
            Replay.OnReplaySet -= HideShipsAndZones;
        }

    }
}
