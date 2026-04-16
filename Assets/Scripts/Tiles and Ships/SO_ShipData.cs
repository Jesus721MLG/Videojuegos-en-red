using UnityEngine;

namespace Battleship
{
    [CreateAssetMenu(fileName = "ShipData", menuName = "Ship Data", order = 51)]
    public class SO_ShipData : ScriptableObject
    {
        [SerializeField] GameObject _shipPrefab;
        [SerializeField] int _shipLength;
        [SerializeField] GameObject _placingAssisstant;
        [SerializeField] GameObject _noGoZone;
        [SerializeField] int _amountToPlace = 1;
        [SerializeField] Vector3[] _allowedRotations =
            { Vector3.zero,
              new Vector3 (0, 90, 0),
              new Vector3 (0, 180, 0),
              new Vector3 (0, 270, 0)
            };


        public GameObject ShipPrefab => _shipPrefab;
        public int ShipLength => _shipLength;
        public GameObject PlacingAssisstant => _placingAssisstant;
        public GameObject NoGoZone => _noGoZone;
        public int AmountToPlace => _amountToPlace;
        public Vector3[] AllowedRotations => _allowedRotations;
    }
}
