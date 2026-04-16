using UnityEngine;

namespace Battleship
{
    public class PlacingAssisstant : MonoBehaviour
    {
        RaycastHit _hit;
        Tile _tile;

        public bool IsOverTile()
        {
            _tile = GetTile();

            if (_tile != null) 
                return true;

            _tile = null;
            return false;
        }

        public Tile GetTile()
        {
            Ray ray = new Ray(transform.position, Vector3.down);

            if (Physics.Raycast(ray, out _hit, 1f))
            {
                if (_hit.collider.gameObject.GetComponent<Tile>())
                    return _hit.collider.GetComponent<Tile>();
                else
                    return null;
            }
            else
                return null;
        }
    }
}
