using Unity.Collections;
using Unity.Mathematics;

namespace MindCraft.MapGeneration
{
    public static class Noise
    {
        private static readonly NativeArray<float2> _offsets;
        private const int MAX_OCTAVES = 10;

        static Noise()
        {
            var random = new Random();
            random.InitState(928349238); // TODO fill with seed 
            _offsets = new NativeArray<float2>(MAX_OCTAVES, Allocator.Persistent);
            _offsets[0] = 0;
            for (var i = 1; i < MAX_OCTAVES; i++)
            {
                _offsets[i] = random.NextFloat2(-1f, 1f);
            }
        }

        public static float Get2DPerlin(float x, float y, int octaves, float lacunarity, float persistance, float scale, float offset)
        {
            float value = 0;
            float amplitude = 1f;
            float frequency = 1f;
            float maxAmplitude = 0;

            for (var i = 0; i < octaves; i++)
            {
                value += amplitude * noise.cnoise( 
                                                   _offsets[i] + 
                                                  new float2(+x * scale * frequency + offset,
                                                             -y * scale * frequency + offset));

                //keep track of max amplitude
                maxAmplitude += amplitude;

                //adjust params for next octave
                amplitude *= persistance;
                frequency *= lacunarity;
            }

            return 0.5f + 0.5f * value / maxAmplitude;
        }

        public static bool Get3DPerlin(float x, float y, float z, float offset, float scale, float threshold)
        {
            return threshold < (0.5f + 0.5f * noise.cnoise(new float3(x * scale + offset,
                                                                      y * scale + offset,
                                                                      z * scale + offset)));
        }
    }
}