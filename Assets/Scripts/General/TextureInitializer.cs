using UnityEngine;

namespace Battleship
{
    public class TextureInitializer : MonoBehaviour
    {
        [SerializeField] private Material _shipMaterial;
        [SerializeField] private Material _waterMaterial;
        [SerializeField] private bool _generateTexturesOnStart = true;

        private static bool _texturesGenerated = false;

        private void Start()
        {
            if (_generateTexturesOnStart && !_texturesGenerated)
            {
                GenerateAndApplyTextures();
                _texturesGenerated = true;
            }
        }

        public void GenerateAndApplyTextures()
        {
            // Generate ship metal texture
            if (_shipMaterial != null)
            {
                Texture2D shipTexture = TextureGenerator.GenerateMetalTexture(512, 512);
                Texture2D shipNormal = TextureGenerator.GenerateNormalMap(512, 512, 0.5f);
                _shipMaterial.SetTexture("_MainTex", shipTexture);
                _shipMaterial.SetTexture("_BumpMap", shipNormal);
            }

            // Generate water texture
            if (_waterMaterial != null)
            {
                Texture2D waterTexture = TextureGenerator.GenerateWaterTexture(512, 512);
                Texture2D waterNormal = TextureGenerator.GenerateNormalMap(512, 512, 1f);
                _waterMaterial.SetTexture("_MainTex", waterTexture);
                _waterMaterial.SetTexture("_BumpMap", waterNormal);
            }
        }
    }
}
