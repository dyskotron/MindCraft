using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Utils.NoiseVisualizer
{
    public class NoiseVisualizer : MonoBehaviour
    {
        public const int WIDTH = 200;
        public const int HEIGHT = 200;

        public const float Multiplier = 1 / 20.687f;

        public const int RESO = 100;
        public const int COLOR_SIZE = RESO * RESO * RESO;

        public Renderer TextureRenderer;
        public Renderer TextureRenderer2;

        private void Start()
        {
            var texture = new Texture2D(WIDTH, HEIGHT);
            var pixels = new Color[WIDTH * HEIGHT];
            
            var texture2 = new Texture2D(WIDTH, HEIGHT);
            var pixels2 = new Color[WIDTH * HEIGHT];

            for (var x = 0; x < WIDTH; x++)
            {
                for (var y = 0; y < HEIGHT; y++)
                {
                    var coords = new float2(x * Multiplier, y * Multiplier);

                    //var cell = math.floor(coords);
                    //var rand = math.unlerp(-1, 1, noise.snoise(coords));
                    //var rand =  noise.cellular(new float2(x / 10 * Multiplier, y / 10 * Multiplier));

                    var voronoi = CustomVoronoi.voronoi(coords);
                    
                    var randClosest = math.unlerp(-1, 1, noise.snoise(voronoi.ClosestCell * 0.223f));
                    var rand2Nd = math.unlerp(-1, 1, noise.snoise(voronoi.SecondClosestCell * 0.223f));
                    
                    
                    //greyscale cells
                    //pixels[x + y * WIDTH] = Color.Lerp(Color.black, Color.white, randClosest);
                    
                    //color cells
                    //pixels[x + y * WIDTH] = CustomVoronoi.rand1dToColor(randClosest);
                    
                    //greyscale border
                    pixels[x + y * WIDTH] = Color.Lerp(Color.black, Color.white, voronoi.MinEdgeDistance * 1.5f);
                    
                    
                    /*
                    pixels[x + y * WIDTH] = Color.Lerp( CustomVoronoi.rand1dToColor(rand2Nd), 
                                                        CustomVoronoi.rand1dToColor(randClosest),
                                                        math.min( 0.5f + voronoi.MinEdgeDistance, 1f));*/
                    
                    
                    var color2 = Color.Lerp(Color.black, 
                                                        CustomVoronoi.rand1dToColor(randClosest),
                                                        math.min( 0.5f + voronoi.MinEdgeDistance, 1f));

                    color2.a = 0.5f;

                    pixels2[x + y * WIDTH] = color2;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            TextureRenderer.material.mainTexture = texture;
            
            texture2.SetPixels(pixels2);
            texture2.Apply();
            TextureRenderer2.material.mainTexture = texture2;
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