using System;
using UnityEngine;

namespace Battleship
{
    public class Tile : MonoBehaviour
    {
        public static event Action<int> OnTileHit;
        public static event Action<int> OnTileUndone;

        [SerializeField] SO_TileData _tileData;
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] Ship _ship;
        int _tileID; 
        MeshRenderer _mesh;
        bool _tileChecked;

        private void Start()
        {
            _mesh = GetComponent<MeshRenderer>();
        }

        void OnMouseOver() => ActivateHighlight(_tileData.TargetedSprite);

        void OnMouseExit() => ActivateHighlight(_tileData.DefaultSprite);

        void OnMouseUp()
        {
            if (_tileChecked)
                return;

            // In online mode, send the click through the network instead of processing locally
            if (NetworkManagerBattleship.IsOnlineMode)
            {
                OnlineTileHandler handler = FindObjectOfType<OnlineTileHandler>();
                if (handler != null)
                    handler.SendAttack(this);
                return;
            }

            CheckForHit();
        }

        void ActivateHighlight(Sprite sprite) 
        {
            if (!_tileChecked)
                _sprite.sprite = sprite;
            else
                _sprite.sprite = _tileData.DefaultSprite;
        }

        public void GetShipOnTile(Ship ship)
        {
            _ship = ship;
            _tileID = ship.ShipID;
        }

        void CheckForHit()
        {
            if (_ship != null)
            {
                AudioPlayer.Instance.Play("Hit");
                _mesh.material = _tileData.HitMaterial;
                OnTileHit?.Invoke(_tileID);
            }
            else
            {
                AudioPlayer.Instance.Play("Miss");
                _mesh.material = _tileData.MissedMaterial;
                GameFlowSystem.Instance.TurnEnded = true;
            }

            RecordPlayedTile();
            _tileChecked = true;
        }

        /// <summary>
        /// Applies an attack result received from the server in online mode.
        /// Updates the tile visual without triggering local game logic.
        /// </summary>
        public void ApplyOnlineResult(bool isHit)
        {
            if (_tileChecked) return;

            if (isHit)
            {
                _mesh.material = _tileData.HitMaterial;
            }
            else
            {
                _mesh.material = _tileData.MissedMaterial;
            }

            _tileChecked = true;
        }

        public void MarkEmptyTile()
        {
            _mesh.material = _tileData.MissedMaterial;
            _tileChecked = true;
        }

        #region REPLAY RELATED
        void RecordPlayedTile()
        {
            FindObjectOfType<ReplaySystem>().AddToTileList(this);
        }

        public void ReplayTile()
        {
            CheckForHit();
        }

        public void UndoTile()
        {
            _tileChecked = false;
            _mesh.material = _tileData.DefaultMaterial;
            OnTileUndone?.Invoke(_tileID);
        }

        public void UnmarkTile()
        {
            _mesh.material = _tileData.DefaultMaterial;
            _tileChecked = false;
        }
        #endregion

        private void OnDisable()
        {
            _tileChecked = false;
            _mesh.material = _tileData.DefaultMaterial;
        }
    }
}
