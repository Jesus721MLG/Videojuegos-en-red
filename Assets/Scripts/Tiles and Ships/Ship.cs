using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    public class Ship : MonoBehaviour
    {
        #region FIELDS AND PROPERTIES
        public static event Action<int> OnShipDestroyed;
        public static event Action<int> OnShipUndone;

        [SerializeField] SO_ShipData _shipData;
        GameObject _noGoZone;
        int _shipID;
        int _hits;
        bool _isDestroyed;

        public SO_ShipData ShipData => _shipData;
        public int ShipID => _shipID;
        public GameObject Zone => _noGoZone;
        public bool ShipDestroyed => _isDestroyed;
        #endregion

        private void OnEnable()
        {
            Tile.OnTileHit += HandleHit;
            Tile.OnTileUndone += UndoHit;
        }

        private void Start()
        {
            DefineZone();
        }

        void DefineZone()
        {
            ShipPlacer shipPlacer = FindObjectOfType<ShipPlacer>();
            Dictionary<GameObject, GameObject> shipsAndZones = shipPlacer.ShipDictionary;

            shipsAndZones.TryGetValue(gameObject, out _noGoZone);
            _noGoZone.GetComponent<NoGoZone>().SetZoneID(_shipID);
        }

        public void AssignShipID(int id)
        {
            _shipID = id;
        }

        public void HandleHit(int id)
        {
            if (_shipID != id || _isDestroyed)
                return;

            _hits++;

            if (_hits == _shipData.ShipLength)
            {
                OnShipDestroyed?.Invoke(_shipID);
                _isDestroyed = true;

                foreach (Transform child in transform)
                    child.gameObject.SetActive(true);
            }
        }

        void UndoHit(int id)
        {
            if (_shipID != id)
                return;

            _hits--;

            if (_isDestroyed)
            {
                OnShipUndone?.Invoke(_shipID);

                foreach (Transform child in transform)
                    child.gameObject.SetActive(false);

                _isDestroyed = false;
            }
        }

        private void OnDisable()
        {
            _hits = 0;
            _isDestroyed = false;
            Tile.OnTileHit -= HandleHit;
            Tile.OnTileUndone -= UndoHit;
        }

    }
}
