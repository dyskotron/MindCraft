using System;
using MindCraft.Data.Defs;
using Unity.Collections;
using Unity.Mathematics;

namespace MindCraft.MapGeneration
{
    public static class Noise
    {
        public static float GetHeight(float x, float y, int octaves, float lacunarity, float persistance, float startFrequency, NativeArray<float2> octaveOffsets, float2 offset)
        {
            float value = 0;
            float amplitude = 1f;
            float frequency = startFrequency;
            float maxAmplitude = 0;

            for (var i = 0; i < octaves; i++)
            {
                value += amplitude * noise.snoise( octaveOffsets[i] +  offset + new float2(x * frequency, y * frequency));

                //keep track of max amplitude
                maxAmplitude += amplitude;

                //adjust params for next octave
                amplitude *= persistance;
                frequency *= lacunarity;
            }

            return 0.5f + 0.5f * value / maxAmplitude;
        }

        public static bool GetLodePresence(LodeAlgorithm algorithm, float x, float y, float z, float offset, float scale, float threshold)
        {

            switch (algorithm)
            {
                case LodeAlgorithm.Perlin2d:
                    return threshold < (0.5f + 0.5f * noise.cnoise(new float2(x * scale + offset, z * scale + offset)));
                    break;
                case LodeAlgorithm.Perlin3d:
                    return threshold < (0.5f + 0.5f * noise.cnoise(new float3(x * scale + offset,
                                                                              y * scale + offset,
                                                                              z * scale + offset)));
                    break;
                case LodeAlgorithm.Simplex2d:
                    return threshold < (0.5f + 0.5f * noise.snoise(new float2(x * scale + offset, z * scale + offset)));
                    break;
                case LodeAlgorithm.Simplex3d:
                    return threshold < (0.5f + 0.5f * noise.snoise(new float3(x * scale + offset,
                                                                              y * scale + offset,
                                                                              z * scale + offset)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }
            
        }
        
        public static bool Get3DPerlin(float x, float y, float z, float offset, float scale, float threshold)
        {
            return threshold < (0.5f + 0.5f * noise.cnoise(new float3(x * scale + offset,
                                                                      y * scale + offset,
                                                                      z * scale + offset)));
        }
    }
}