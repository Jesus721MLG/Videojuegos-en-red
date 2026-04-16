using UnityEngine;

namespace Battleship
{
    public class NoGoZone : MonoBehaviour
    {
        int _zoneID;

        private void OnEnable()
        {
            Ship.OnShipDestroyed += MarkZone;
            Ship.OnShipUndone += UnmarkZone;
        }

        public void SetZoneID(int id)
        {
            _zoneID = id;
        }

        public void MarkZone(int id)
        {
            if (_zoneID != id)
                return;

            RaycastHit hit;

            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);

                Ray ray = new Ray(child.position, Vector3.down);

                if (Physics.Raycast(ray, out hit, 10f))
                {
                    if (hit.collider.gameObject.GetComponent<Tile>())
                        hit.collider.GetComponent<Tile>().MarkEmptyTile();
                }
            }

            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }

        void UnmarkZone(int id)
        {
            if (_zoneID != id)
                return;

            RaycastHit hit;

            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);

                Ray ray = new Ray(child.position, Vector3.down);

                if (Physics.Raycast(ray, out hit, 10f))
                {
                    if (hit.collider.gameObject.GetComponent<Tile>())
                        hit.collider.GetComponent<Tile>().UnmarkTile();
                }
            }

            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Ship.OnShipDestroyed -= MarkZone;
            Ship.OnShipUndone -= UnmarkZone;
        }
    }
}
