using Unity.Collections;
using Unity.Mathematics;

namespace MindCraft.MapGeneration
{
    public static class Noise
    {
        public static float Get2DPerlin(float x, float y, int octaves, float lacunarity, float persistance, float startFrequency, NativeArray<float2> octaveOffsets, float2 offset)
        {
            float value = 0;
            float amplitude = 1f;
            float frequency = startFrequency;
            float maxAmplitude = 0;

            for (var i = 0; i < octaves; i++)
            {
                value += amplitude * noise.cnoise( octaveOffsets[i] +  offset + new float2(x * frequency, y * frequency));

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