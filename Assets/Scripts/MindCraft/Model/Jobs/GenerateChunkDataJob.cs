using MindCraft.Common;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace MindCraft.Model.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct GenerateChunkDataJob : IJobParallelFor
    {
        [ReadOnly] public int ChunkX;
        [ReadOnly] public int ChunkY;
        [ReadOnly] public NativeArray<BiomeDefData> BiomeDefs;
        [ReadOnly] public NativeArray<LodeDefData> Lodes;
        [ReadOnly] public NativeArray<float> LodeTresholds;
        [ReadOnly] public NativeArray<byte> Biomes;
        [ReadOnly] public NativeArray<int> Heights;
        
        [WriteOnly] public NativeArray<byte> Map;

        public void Execute(int index)
        {
            ArrayHelper.To3DMap(index, out int x, out int y, out int z);

            int index2d = x * GeometryConsts.CHUNK_SIZE + z;
            Map[index] = GenerationHelper.GenerateVoxel(x + ChunkX * GeometryConsts.CHUNK_SIZE, y, z + ChunkY * GeometryConsts.CHUNK_SIZE, Heights[index2d], BiomeDefs[Biomes[index2d]], Lodes, LodeTresholds);
        }
    }
}