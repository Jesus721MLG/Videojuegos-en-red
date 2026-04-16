using UnityEngine;

namespace Battleship
{
    [CreateAssetMenu (fileName = "TileData", menuName = "Tile Data", order = 52)]
    public class SO_TileData : ScriptableObject
    {
        [SerializeField] Sprite _defaultSprite;
        [SerializeField] Sprite _targetedSprite;

        [SerializeField] Material _defaultMaterial;
        [SerializeField] Material _missedMaterial;
        [SerializeField] Material _hitMaterial;

        public Sprite DefaultSprite => _defaultSprite;
        public Sprite TargetedSprite => _targetedSprite;
        public Material DefaultMaterial => _defaultMaterial;
        public Material MissedMaterial => _missedMaterial;
        public Material HitMaterial => _hitMaterial;


    }
}
