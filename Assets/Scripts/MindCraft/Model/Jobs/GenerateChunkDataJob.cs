using MindCraft.Common;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MindCraft.Model.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct GenerateChunkDataJob : IJobParallelFor
    {
        [ReadOnly] public int ChunkX;
        [ReadOnly] public int ChunkY;
        [ReadOnly] public NativeArray<BiomeDefData> BiomeDefs;
        [ReadOnly] public NativeArray<float2> Offsets;
        [ReadOnly] public NativeArray<int> TerrainCurves;
        [ReadOnly] public NativeArray<LodeDefData> Lodes;
        [ReadOnly] public NativeArray<float> LodeTresholds;
        [WriteOnly] public NativeArray<byte> Map;
        [ReadOnly] public NativeArray<byte> Biomes;
        [ReadOnly] public NativeArray<int> Heights;

        public void Execute(int index)
        {
            ArrayHelper.To3D(index, out int x, out int y, out int z);

            int index2d = x * VoxelLookups.CHUNK_SIZE + z;
            //Map[index] = GenerationHelper.GenerateVoxel(x + ChunkX * VoxelLookups.CHUNK_SIZE, y, z + ChunkY * VoxelLookups.CHUNK_SIZE, BiomeDefs, Offsets, TerrainCurves, Lodes, LodeTresholds);
            Map[index] = GenerationHelper.GenerateVoxel(x + ChunkX * VoxelLookups.CHUNK_SIZE, y, z + ChunkY * VoxelLookups.CHUNK_SIZE, Heights[index2d], BiomeDefs[Biomes[index2d]], Lodes, LodeTresholds);
        }
    }
    /*
    
    [BurstCompile(CompileSynchronously = true)]
    public struct GenerateChunkDataJob : IJobParallelFor
    {
        [ReadOnly] public int ChunkX;
        [ReadOnly] public int ChunkY;
        [ReadOnly] public NativeArray<BiomeDefData> BiomeDefs;
        [ReadOnly] public NativeArray<float2> Offsets;
        [ReadOnly] public NativeArray<int> TerrainCurves;
        [ReadOnly] public NativeArray<LodeDefData> Lodes;
        [ReadOnly] public NativeArray<float> LodeTresholds;
        [WriteOnly] public NativeArray<byte> Map;

        public void Execute(int index)
        {
            ArrayHelper.To3D(index, out int x, out int y, out int z);
            Map[index] = GenerationHelper.GenerateVoxel(x + ChunkX * VoxelLookups.CHUNK_SIZE, y, z + ChunkY * VoxelLookups.CHUNK_SIZE, BiomeDefs, Offsets, TerrainCurves, Lodes, LodeTresholds);
        }
    }*/
}