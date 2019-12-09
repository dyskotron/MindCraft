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

        public static bool GetLodePresence(LodeAlgorithm algorithm, float x, float y, float z, float3 offset, float scale, float threshold)
        {
            //for min / max threshold we dont need to run noise algorithm to determine lode presence
            if (threshold >= 1)
                return false;

            if (threshold <= 0)
                return true;
            
            switch (algorithm)
            {
                case LodeAlgorithm.Perlin2d:
                    return threshold < (0.5f + 0.5f * noise.cnoise(offset.xy + new float2(x, z) * scale));
                case LodeAlgorithm.Perlin3d:
                    return threshold < (0.5f + 0.5f * noise.cnoise(offset + new float3(x, y, z) * scale));
                case LodeAlgorithm.Simplex2d:
                    return threshold < (0.5f + 0.5f * noise.snoise(offset.xy + new float2(x, z) * scale));
                case LodeAlgorithm.Simplex3d:
                    return threshold < (0.5f + 0.5f * noise.snoise(offset + new float3(x, y, z) * scale));
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }
            
        }
    }
}