using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Model.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct GetBiomeAndHeightJob : IJobParallelFor
    {
        [ReadOnly] public int ChunkX;
        [ReadOnly] public int ChunkY;
        [ReadOnly] public NativeArray<BiomeDefData> BiomeDefs;
        [ReadOnly] public NativeArray<float2> Offsets;
        [ReadOnly] public NativeArray<int> TerrainCurves;
        
        [WriteOnly] public NativeArray<byte> Biomes;
        [WriteOnly] public NativeArray<int> Heights;

        public void Execute(int index)
        {
            //ArrayHelper.To3D(index, out int x, out int y, out int z);

            //int index2d = x * GeometryLookups.CHUNK_SIZE + z;
            int x = Mathf.FloorToInt( index / (float)GeometryLookups.CHUNK_SIZE);
            int z = index % GeometryLookups.CHUNK_SIZE;
            
            Biomes[index] = GenerationHelper.GetBiomeForColumn(x + ChunkX * GeometryLookups.CHUNK_SIZE, z + ChunkY * GeometryLookups.CHUNK_SIZE, BiomeDefs, Offsets, TerrainCurves, out int terrainHeight);
            Heights[index] = terrainHeight;
        }
    }
}