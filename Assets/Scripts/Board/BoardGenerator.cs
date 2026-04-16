using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    public class BoardGenerator : MonoBehaviour
    {
        [SerializeField] bool _generateInEditor;
        [SerializeField] GameObject _tilePrefab;
        [SerializeField] int _boardSizeX = 10, _boardSizeZ = 10;

        [Header("Network")]
        [Tooltip("0 = left board (Player 0), 1 = right board (Player 1). " +
                 "Set this in the Inspector for each board.")]
        [SerializeField] int _boardIndex;

        List<GameObject> _tileList = new List<GameObject>();
        public List<GameObject> TileList => _tileList;

        /// <summary>Board index used by the network layer to identify boards.</summary>
        public int BoardIndex => _boardIndex;

        #region GENERATE_IN_EDITOR
        private void OnDrawGizmos()
        {
            if (_tilePrefab != null && _generateInEditor)
            {
                GenerateBoard();
            }
        }
        #endregion

        public void GenerateBoard()
        {
            for (int i = 0; i < _boardSizeX; i++)
            {
                for (int j = 0; j < _boardSizeZ; j++)
                {
                    Vector3 tilePos = new Vector3(transform.position.x + i, 0, transform.position.z + j);
                    GameObject tile = Instantiate(_tilePrefab, tilePos, Quaternion.identity, transform);
                    tile.name = $"Tile: x {i}, z {j}";
                    _tileList.Add(tile);

                    // Pass grid coordinates to the tile (used by the network layer)
                    Tile tileScript = tile.GetComponent<Tile>();
                    if (tileScript != null)
                        tileScript.SetCoordinates(i, j, _boardIndex);
                }
            }
        }
    }
}
