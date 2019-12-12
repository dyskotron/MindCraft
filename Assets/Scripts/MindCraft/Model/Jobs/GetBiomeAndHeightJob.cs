using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Utils;
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

            //int index2d = x * VoxelLookups.CHUNK_SIZE + z;
            int x = Mathf.FloorToInt( index / (float)VoxelLookups.CHUNK_SIZE);
            int z = index % VoxelLookups.CHUNK_SIZE;
            
            //Biomes[index] = GenerationHelper.GetBiomeForColumn(x + ChunkX * VoxelLookups.CHUNK_SIZE, z + ChunkY * VoxelLookups.CHUNK_SIZE, BiomeDefs, Offsets, TerrainCurves, out int terrainHeight);
            //Heights[index] = terrainHeight;

            var voronoi = CustomVoronoi.voronoi(0.005f * new float2((x + ChunkX * VoxelLookups.CHUNK_SIZE) * 1.673453456345f, (z + ChunkY * VoxelLookups.CHUNK_SIZE) * 1.0123134134234f));
            var voronoiRand = math.unlerp(-1, 1, noise.snoise(voronoi.ClosestCell * 0.223f));

            var biomeId = (int) math.floor(voronoiRand * BiomeDefs.Length);
            var biome = BiomeDefs[biomeId];

            Biomes[index] = (byte)biomeId;
            
            var biomeHeight = GenerationHelper.GetTerrainHeight(x + ChunkX * VoxelLookups.CHUNK_SIZE, z + ChunkY * VoxelLookups.CHUNK_SIZE, biome, Offsets, TerrainCurves);
            var edgeHeight = 70;

            var edgeDistanceNormalised = math.clamp(voronoi.MinEdgeDistance * 3f, 0f, 1f);

            Heights[index] = (int) (edgeHeight * (1 - edgeDistanceNormalised) + biomeHeight * edgeDistanceNormalised);

        }
    }
}