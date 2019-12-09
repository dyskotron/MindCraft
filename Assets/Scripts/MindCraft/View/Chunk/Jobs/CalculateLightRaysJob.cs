using MindCraft.Common;
using MindCraft.Data;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace MindCraft.View.Chunk.Jobs
{
    public struct CalculateLightRaysJob : IJob
    {
        [ReadOnly] public NativeArray<byte> MapData;
        [ReadOnly] public NativeArray<bool> TransparencyLookup;

        public NativeArray<float> LightLevels;

        public void Execute()
        {
            float lightLevel = 1f;

            for (var x = 0; x < VoxelLookups.CHUNK_SIZE; x++)
            {
                for (var z = 0; z < VoxelLookups.CHUNK_SIZE; z++)
                {
                    lightLevel = 1f;

                    for (var y = VoxelLookups.CHUNK_HEIGHT - 1; y >= 0; y--)
                    {
                        var index = ArrayHelper.To1D(x, y, z);
                        LightLevels[index] = lightLevel;

                        var voxelId = MapData[index];

                        //basically air has transparency 1 so we're keeping last value
                        if (voxelId != BlockTypeByte.AIR)
                            lightLevel = Mathf.Min(TransparencyLookup[voxelId] ? 0.7f : 0.25f, lightLevel);

                        LightLevels[index] = lightLevel;
                    }
                }
            }
        }
    }
}