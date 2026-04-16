using UnityEngine;

namespace Battleship
{
    public class TextureGenerator : MonoBehaviour
    {
        /// <summary>
        /// Generates a procedural metal texture for ships
        /// </summary>
        public static Texture2D GenerateMetalTexture(int width = 512, int height = 512)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                float x = (i % width) / (float)width;
                float y = (i / width) / (float)height;

                // Create metallic appearance with Perlin noise
                float noise1 = Mathf.PerlinNoise(x * 4f, y * 4f);
                float noise2 = Mathf.PerlinNoise(x * 8f, y * 8f) * 0.5f;
                float metallic = (noise1 + noise2) * 0.7f;

                // Base dark gray metal
                Color baseColor = new Color(0.3f, 0.3f, 0.35f);
                Color highlightColor = new Color(0.6f, 0.6f, 0.65f);

                pixels[i] = Color.Lerp(baseColor, highlightColor, metallic);
                pixels[i].a = 1f;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;

            return texture;
        }

        /// <summary>
        /// Generates a procedural water texture with wave patterns
        /// </summary>
        public static Texture2D GenerateWaterTexture(int width = 512, int height = 512)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                float x = (i % width) / (float)width;
                float y = (i / width) / (float)height;

                // Create wave patterns
                float wave1 = Mathf.Sin(x * 10f) * 0.3f;
                float wave2 = Mathf.Sin(y * 8f) * 0.3f;
                float noise = Mathf.PerlinNoise(x * 3f, y * 3f) * 0.4f;

                float waterValue = (wave1 + wave2 + noise) * 0.5f + 0.5f;

                // Ocean colors - deep blue to light cyan
                Color deepWater = new Color(0.1f, 0.2f, 0.4f);
                Color lightWater = new Color(0.2f, 0.5f, 0.8f);
                Color foam = new Color(0.9f, 0.95f, 1f);

                Color finalColor;
                if (waterValue > 0.7f)
                {
                    finalColor = Color.Lerp(lightWater, foam, (waterValue - 0.7f) / 0.3f);
                }
                else if (waterValue > 0.4f)
                {
                    finalColor = Color.Lerp(deepWater, lightWater, (waterValue - 0.4f) / 0.3f);
                }
                else
                {
                    finalColor = deepWater;
                }

                pixels[i] = finalColor;
                pixels[i].a = 1f;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;

            return texture;
        }

        /// <summary>
        /// Generates a procedural normal map for enhanced lighting
        /// </summary>
        public static Texture2D GenerateNormalMap(int width = 512, int height = 512, float strength = 1f)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % width;
                int y = i / width;

                // Calculate height using Perlin noise
                float height1 = Mathf.PerlinNoise(x / (float)width * 4f, y / (float)height * 4f);
                float height2 = Mathf.PerlinNoise(x / (float)width * 8f, y / (float)height * 8f) * 0.5f;
                float heightValue = (height1 + height2) * strength;

                // Get neighboring heights for normal calculation
                float heightLeft = GetNormalHeight(x - 1, y, width, height);
                float heightRight = GetNormalHeight(x + 1, y, width, height);
                float heightUp = GetNormalHeight(x, y - 1, width, height);
                float heightDown = GetNormalHeight(x, y + 1, width, height);

                // Calculate normal
                Vector3 normal = new Vector3(heightLeft - heightRight, 2f, heightUp - heightDown);
                normal.Normalize();

                // Convert normal to color (normal map format)
                Color normalColor = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                pixels[i] = normalColor;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;

            return texture;
        }

        private static float GetNormalHeight(int x, int y, int width, int height)
        {
            x = ((x % width) + width) % width;
            y = ((y % height) + height) % height;
            return Mathf.PerlinNoise(x / (float)width * 4f, y / (float)height * 4f);
        }
    }
}
