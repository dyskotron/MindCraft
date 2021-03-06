using MindCraft.Common;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MindCraft.View.Chunk.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct CalculateLightRaysJob : IJob
    {
        [ReadOnly] public NativeArray<byte> MapData;
        [ReadOnly] public NativeArray<BlockDefData> BlockDataLookup;

        [WriteOnly] public NativeArray<float> LightLevels;

        public void Execute()
        {
            float lightLevel = 1f;

            for (var x = 0; x < GeometryConsts.CHUNK_SIZE; x++)
            {
                for (var z = 0; z < GeometryConsts.CHUNK_SIZE; z++)
                {
                    lightLevel = 1f;

                    for (var y = GeometryConsts.CHUNK_HEIGHT - 1; y >= 0; y--)
                    {
                        var index = ArrayHelper.To1DMap(x, y, z);
                        LightLevels[index] = lightLevel;

                        var voxelId = MapData[index];

                        var voxelData = BlockDataLookup[voxelId];

                        if (voxelData.IsSolid)
                            lightLevel = voxelData.LightModification;
                        else
                            lightLevel = math.max(lightLevel + voxelData.LightModification,  0);

                        LightLevels[index] = lightLevel;
                    }
                }
            }
        }
    }
}