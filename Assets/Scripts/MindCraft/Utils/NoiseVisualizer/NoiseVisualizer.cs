using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Utils.NoiseVisualizer
{
    public class NoiseVisualizer : MonoBehaviour
    {
        public const int WIDTH = 100;
        public const int HEIGHT = 100;

        public const float Multiplier = 1 / 15f;

        public const int RESO = 100;
        public const int COLOR_SIZE = RESO * RESO * RESO;

        public Renderer TextureRenderer;

        private void Start()
        {
            var texture = new Texture2D(WIDTH, HEIGHT);
            var pixels = new Color[WIDTH * HEIGHT];

            for (var x = 0; x < WIDTH; x++)
            {
                for (var y = 0; y < HEIGHT; y++)
                {
                    //var rand = math.unlerp(-1, 1, noise.cnoise(new float2(x * Multiplier, y * Multiplier)));
                    var rand =  noise.cellular(new float2(x * Multiplier, y * Multiplier));
                    pixels[x + y * WIDTH] = Color.Lerp(Color.black, Color.white, rand.x);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            TextureRenderer.sharedMaterial.mainTexture = texture;
            TextureRenderer.transform.localScale = new Vector3(WIDTH, 1, HEIGHT);
        }

        public static Color ToColor(float idFloat, int xMax = RESO, int yMax = RESO)
        {
            var id = (int) (idFloat * COLOR_SIZE);
            var r = 0;
            var g = 0;
            var b = 0;

            b = id / (xMax * yMax); // a = b / c
            id -= (b * xMax * yMax); // x = b - a * c
            g = id / xMax;
            r = id % xMax;

            return new Color(r, g, b);
        }
    }
}