using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    public class ShipPlacer : MonoBehaviour
    {
        public static event Action OnAllShipsPlaced;

        #region FIELDS AND PROPERTIES

        [SerializeField] GameObject[] _boards;
        [SerializeField] List<Ship> _shipsToPlace = new List<Ship>();

        [Header("Parent objects")][SerializeField] Transform _shipsParent;
        [SerializeField] Transform _noGoZonesParent;
        [SerializeField] Transform _placingAssisstantsParent;

        [SerializeField] int _xBoardRange, _zBoardRange;

        int _currentBoard;
        SO_ShipData _currentShipData;
        int _currentShip;
        int _sameShipPlaced;
        bool _availablePositionFound;

        List<GameObject> _spawnedShips = new List<GameObject>();
        Dictionary<GameObject, GameObject> _shipDictionary = new Dictionary<GameObject, GameObject>();
        public List<GameObject> SpawnedShips => _spawnedShips;
        public Dictionary<GameObject, GameObject> ShipDictionary => _shipDictionary;

        #endregion

        public void SetUpBoards()
        {
            for (int i = 0; i < _boards.Length; i++)
            {
                _currentBoard = i;
                PlaceShips();
            }

            OnAllShipsPlaced?.Invoke();
        }

        void PlaceShips() 
        {
            for (int i = 0; i < _shipsToPlace.Count; i++) 
            {
                _currentShip = i;
                _currentShipData = _shipsToPlace[_currentShip].ShipData;
                _sameShipPlaced = 0;

                for (int j = 0; j < _currentShipData.AmountToPlace; j++)
                {
                    if (_sameShipPlaced == _currentShipData.AmountToPlace) return;
                    _availablePositionFound = false;

                    while (!_availablePositionFound)
                        SearchForAvailableSpace();
                }
            }

            CheckForRemainingAssisstants();
        }
        
        GameObject InstantiatePlacingAssisstant()
        {
            int xPos = UnityEngine.Random.Range(0, _xBoardRange);
            int zPos = UnityEngine.Random.Range(0, _zBoardRange);

            GameObject pa = Instantiate(_currentShipData.PlacingAssisstant);
            pa.transform.parent = _placingAssisstantsParent;
            pa.transform.position = new Vector3(_boards[_currentBoard].transform.position.x + xPos, 1f, _boards[_currentBoard].transform.position.z + zPos);
            return pa;
        }
        
        bool CanPlaceShip(Transform t)
        {
            foreach (Transform child in t)
            {
                PlacingAssisstant pa = child.GetComponent<PlacingAssisstant>();

                if (!pa.IsOverTile())
                    return false;
            }

            return true;
        }

        void SearchForAvailableSpace()
        {
            GameObject pa = InstantiatePlacingAssisstant();

            for (int i = 0; i < _currentShipData.AllowedRotations.Length; i++)
            {
                List<int> allowedRotationsList = new List<int> { 0, 1, 2, 3 };
                int randomRotation = allowedRotationsList[UnityEngine.Random.Range(0, allowedRotationsList.Count)];

                pa.transform.rotation = Quaternion.Euler(_currentShipData.AllowedRotations[randomRotation]);

                if (CanPlaceShip(pa.transform))
                {
                    ProcessShipPlacement(pa);
                    break;
                }
                else if (allowedRotationsList.Count == 0)
                    Destroy(pa);  
                else
                {
                    Destroy(pa);
                    allowedRotationsList.Remove(randomRotation);
                }
            }
        }
        
        void InstantiateShipSet(GameObject assisstant)
        {
            Vector3 pos = new Vector3(assisstant.transform.position.x, 0.6f, assisstant.transform.position.z);

            GameObject newShip = Instantiate(_currentShipData.ShipPrefab, pos, assisstant.transform.rotation, _shipsParent);
            GameObject noGoZone = Instantiate(_currentShipData.NoGoZone, pos, assisstant.transform.rotation, _noGoZonesParent);
            noGoZone.SetActive(true); 

            _spawnedShips.Add(newShip);
            _shipDictionary.Add(newShip, noGoZone);

            GameFlowSystem.Instance.AddShipToPlayerList(_currentBoard, newShip);
            newShip.GetComponent<Ship>().AssignShipID(_spawnedShips.IndexOf(newShip) + 1);
            AssignShipToTile(newShip);

            Destroy(assisstant);
        }
        
        void ProcessShipPlacement(GameObject assisstant)
        {
            InstantiateShipSet(assisstant);
            _sameShipPlaced++;
            _availablePositionFound = true;
        }
        
        void AssignShipToTile(GameObject spawnedShip)
        {
            Ship ship = spawnedShip.GetComponent<Ship>();
            RaycastHit hit;

            foreach (Transform child in spawnedShip.transform)
            {
                Ray ray = new Ray(child.position, Vector3.down);

                if (Physics.Raycast(ray, out hit, 10f))
                {
                    if (hit.collider.gameObject.GetComponent<Tile>())
                        hit.collider.GetComponent<Tile>().GetShipOnTile(ship);
                    else
                        Debug.Log("Tile not found.");
                }
            }
        }

        void CheckForRemainingAssisstants()
        {
            if (_placingAssisstantsParent.childCount == 0)
                return;

            foreach (Transform child in _placingAssisstantsParent)
                Destroy(child.gameObject);
        }
    }
}
