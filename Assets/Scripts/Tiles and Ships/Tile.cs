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

        // ── Network support ──
        /// <summary>When true, tile clicks are routed through the network layer.</summary>
        public static bool IsNetworked;
        int _gridX;
        int _gridZ;
        int _boardIndex;

        private void Start()
        {
            _mesh = GetComponent<MeshRenderer>();
        }

        void OnMouseOver() => ActivateHighlight(_tileData.TargetedSprite);

        void OnMouseExit() => ActivateHighlight(_tileData.DefaultSprite);

        void OnMouseUp()
        {
            if (_tileChecked) return;

            if (IsNetworked)
            {
                HandleNetworkClick();
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

        // ────────────────────── Network helpers ──────────────────────

        /// <summary>Store the grid coordinates (called by BoardGenerator).</summary>
        public void SetCoordinates(int x, int z, int boardIndex)
        {
            _gridX      = x;
            _gridZ      = z;
            _boardIndex = boardIndex;
        }

        /// <summary>Called in network mode instead of CheckForHit.</summary>
        void HandleNetworkClick()
        {
            var local = BattleshipPlayer.LocalInstance;
            if (local == null) return;

            // Only allow clicking on the OPPONENT's board
            int opponentBoard = 1 - local.GetMyPlayerIndex();
            if (_boardIndex != opponentBoard)
            {
                Debug.Log("[Tile] You can only attack the opponent's board.");
                return;
            }

            local.RequestAttack(_gridX, _gridZ);
        }

        /// <summary>Visually mark this tile as HIT (called by NetworkBoardSetup).</summary>
        public void NetworkApplyHit()
        {
            if (_mesh == null) _mesh = GetComponent<MeshRenderer>();
            _mesh.material = _tileData.HitMaterial;
            _sprite.sprite = _tileData.DefaultSprite;
            _tileChecked = true;
        }

        /// <summary>Visually mark this tile as MISS (called by NetworkBoardSetup).</summary>
        public void NetworkApplyMiss()
        {
            if (_mesh == null) _mesh = GetComponent<MeshRenderer>();
            _mesh.material = _tileData.MissedMaterial;
            _sprite.sprite = _tileData.DefaultSprite;
            _tileChecked = true;
        }

        /// <summary>Tint the tile to show that a ship occupies it (defence board).</summary>
        public void NetworkMarkAsShip(Material shipMat)
        {
            if (_mesh == null) _mesh = GetComponent<MeshRenderer>();
            if (shipMat != null)
                _mesh.material = shipMat;
            else
                _mesh.material.color = new Color(0.45f, 0.45f, 0.45f); // dark-grey tint
        }
    }
}
