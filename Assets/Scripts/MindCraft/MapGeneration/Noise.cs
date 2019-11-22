using MindCraft.MapGeneration.Lookup;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.MapGeneration
{
    public static class Noise
    {
        public static float Get2DPerlin(float x, float y, float offset, float scale)
        {
            return noise.cnoise(new float2((x + 0.1f) / VoxelLookups.CHUNK_SIZE * scale + offset,
                                        (y + 0.1f) / VoxelLookups.CHUNK_SIZE * scale + offset));
        }

        public static bool Get3DPerlin(float x, float y, float z, float offset, float scale, float threshold)
        {
            return noise.cnoise(
                         new float3((x + offset) * scale,
                                    (y + offset) * scale,
                                    (z + offset) * scale)
                        ) > threshold;
        }
    }
}