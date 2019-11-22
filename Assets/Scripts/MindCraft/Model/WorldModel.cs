using System.Collections.Generic;
using System.Diagnostics;
using Framewerk.Managers;
using MindCraft.Common;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Lookup;
using MindCraft.View;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace MindCraft.Model
{
    public interface IWorldModel
    {
        byte[,,] TryGetMapByChunkCoords(ChunkCoord coords);
        bool CheckVoxelOnGlobalXyz(float x, float y, float z);
        void EditVoxel(Vector3 position, byte VoxelType);

        /// <summary>
        /// Generates Chunk Map based only on seed and generation algorithm
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        byte[,,] CreateChunkMap(ChunkCoord coords);

        /// <summary>
        /// Returns voxel on world coordinates - decides if we need to generate the voxel or we can retrieve that from existing chunk
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        byte GetVoxel(int x, int y, int z);

        int GetTerrainHeight(int x, int y);
    }

    public class WorldModel : IWorldModel
    {
        [Inject] public IAssetManager AssetManager { get; set; }
        [Inject] public ChunksRenderer ChunksRenderer { get; set; }
        
        //map for each generated chunk - only generated data which are always recreated the same
        private Dictionary<ChunkCoord, byte[,,]> _chunkMaps = new Dictionary<ChunkCoord, byte[,,]>();

        //Only player modified voxels stored there
        private Dictionary<ChunkCoord, byte[,,]> _playerModifiedMaps = new Dictionary<ChunkCoord, byte[,,]>();

        private BiomeDef _biomeDef;

        #region Getters / Helper methods

        [PostConstruct]
        public void PostConstruct()
        {
            _biomeDef = AssetManager.GetAsset<BiomeDef>(ResourcePath.BIOME_DEF);
        }

        public byte[,,] TryGetMapByChunkCoords(ChunkCoord coords)
        {
            _chunkMaps.TryGetValue(coords, out byte[,,] chunkMap);
            return chunkMap;
        }

        public bool CheckVoxelOnGlobalXyz(float x, float y, float z)
        {
            var coords = WorldModelHelper.GetChunkCoordsFromWorldXy(x, z);
            var chunkMap = TryGetMapByChunkCoords(coords);

            //consider returning true at this point
            //method is used for collision checking so if we get to hypothetical situation where chunk map is still not generated,
            //we should not allow player to move out of generated world.
            if (chunkMap == null)
                return false;

            WorldModelHelper.GetLocalXyzFromWorldPosition(x, y, z, out int xOut, out int yOut, out int zOut);

            return chunkMap[xOut, yOut, zOut] != VoxelTypeByte.AIR;
        }

        #endregion

        public void EditVoxel(Vector3 position, byte VoxelType)
        {
            WorldModelHelper.GetLocalXyzFromWorldPosition(position, out int x, out int y, out int z);
            var coords = WorldModelHelper.GetChunkCoordsFromWorldPosition(position);

            //update voxel in chunk map
            _chunkMaps[coords][x, y, z] = VoxelType;

            //update voxel in user player modifications map
            //so that data can persist when we clean distant chunks from memory or can be used to save / load game
            if (!_playerModifiedMaps.ContainsKey(coords))
                _playerModifiedMaps[coords] = new byte[VoxelLookups.CHUNK_SIZE, VoxelLookups.CHUNK_HEIGHT, VoxelLookups.CHUNK_SIZE];

            _playerModifiedMaps[coords][x, y, z] = VoxelType;

            //TODO: chunks update should not be called directly from model!
            ChunksRenderer.UpdateChunkMesh(coords, _chunkMaps[coords]);

            ChunkCoord neighbourCoords;

            if (x <= 0)
            {
                // Update left neighbour
                neighbourCoords = coords + ChunkCoord.Left;
                ChunksRenderer.UpdateChunkMesh(neighbourCoords, _chunkMaps[neighbourCoords]);
            }
            else if (x >= VoxelLookups.CHUNK_SIZE - 1)
            {
                // Update right neighbour
                neighbourCoords = coords + ChunkCoord.Right;
                ChunksRenderer.UpdateChunkMesh(neighbourCoords, _chunkMaps[neighbourCoords]);
            }

            if (z <= 0)
            {
                // Update back neighbour
                neighbourCoords = coords + ChunkCoord.Back;
                ChunksRenderer.UpdateChunkMesh(neighbourCoords, _chunkMaps[neighbourCoords]);
            }
            else if (z >= VoxelLookups.CHUNK_SIZE - 1)
            {
                // Update forward neighbour
                neighbourCoords = coords + ChunkCoord.Forward;
                ChunksRenderer.UpdateChunkMesh(neighbourCoords, _chunkMaps[neighbourCoords]);
            }
        }

        #region Terrain Generation

        /// <summary>
        /// Generates Chunk Map based only on seed and generation algorithm
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        public byte[,,] CreateChunkMap(ChunkCoord coords)
        {
            var mapWatch = new Stopwatch();
            mapWatch.Start();
            
            
            var map = new byte[VoxelLookups.CHUNK_SIZE, VoxelLookups.CHUNK_HEIGHT, VoxelLookups.CHUNK_SIZE];
            var nativeArray = new NativeArray<byte>(VoxelLookups.VOXELS_PER_CHUNK, Allocator.TempJob);
            
            var mapJob = CreateMapJob(coords.X, coords.Y, _biomeDef, nativeArray);
            mapJob.Complete();
            
            for (var iX = 0; iX < VoxelLookups.CHUNK_SIZE; iX++)
            {
                for (var iZ = 0; iZ < VoxelLookups.CHUNK_SIZE; iZ++)
                {
                    for (var iY = 0; iY < VoxelLookups.CHUNK_HEIGHT; iY++)
                    {
                        var index = ArrayHelper.To1D(iX, iY, iZ);
                        map[iX, iY, iZ] = nativeArray[index];
                    }
                }
            }

            _chunkMaps[coords] = map;
            
            nativeArray.Dispose();

            mapWatch.Stop();
            Chunk.MAP_ELAPSED_TOTAL += mapWatch.Elapsed.TotalSeconds;

            return map;
        }

        private JobHandle CreateMapJob(int chunkX, int chunkY, BiomeDef biomeDef, NativeArray<byte> map)
        {
            var job = new GenerateMapJob()
                      {
                          ChunkX = chunkX,
                          ChunkY = chunkY,
                          BiomeDef = biomeDef.BiomeDefData,
                          Map =  map
                      };
            
            return job.Schedule(VoxelLookups.VOXELS_PER_CHUNK,64);    
        }
        
        [BurstCompile]
        public struct GenerateMapJob : IJobParallelFor
        {
            [ReadOnly]
            public int ChunkX;
            [ReadOnly]
            public int ChunkY;
            
            [ReadOnly] public BiomeDefData BiomeDef;

            [WriteOnly] public NativeArray<byte> Map;
            
            public void Execute(int index)
            {
                ArrayHelper.To3D(index, out int x, out int y, out int z);
                Map[index] = GenerateVoxel(x + ChunkX * VoxelLookups.CHUNK_SIZE, y, z + ChunkY * VoxelLookups.CHUNK_SIZE, BiomeDef);
            }
        }

        /// <summary>
        /// Returns voxel on world coordinates - decides if we need to generate the voxel or we can retrieve that from existing chunk
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public byte GetVoxel(int x, int y, int z)
        {
            // ======== STATIC RULES ========

            if (y < 0 || y >= VoxelLookups.CHUNK_HEIGHT)
                return VoxelTypeByte.AIR;

            if (y == 0)
                return VoxelTypeByte.GREY_STONE;

            // ======== RETURN CACHED VOXELS OR PLAYER MODIFIED VOXELS ========

            var coords = WorldModelHelper.GetChunkCoordsFromWorldXy(x, z);
            var map = TryGetMapByChunkCoords(coords);
            if (map != null)
                return map[x - coords.X * VoxelLookups.CHUNK_SIZE, y, z - coords.Y * VoxelLookups.CHUNK_SIZE];

            // ======== BASIC PASS ========

            return GenerateVoxel(x, y, z, _biomeDef.BiomeDefData);
        }

        public int GetTerrainHeight(int x, int y)
        {
            return GetTerrainHeight(x, y, _biomeDef.BiomeDefData);
        }

        public static int GetTerrainHeight(int x, int y, BiomeDefData biomeDef)
        {
            return Mathf.FloorToInt(biomeDef.TerrainMin + biomeDef.TerrainHeight * Noise.Get2DPerlin(x, y, 0, biomeDef.TerrainScale));
        }

        /// <summary>
        /// Generates single voxel on world coordinates - called only by WorldModel when we truly want to generate
        /// and know that coordinates are valid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="biomeDef"></param>
        /// <returns></returns>
        private static byte GenerateVoxel(int x, int y, int z, BiomeDefData biomeDef)
        {
            // ======== STATIC RULES ========

            if (y == 0)
                return VoxelTypeByte.GREY_STONE;

            // ======== BASIC PASS ========

            var terrainHeight = GetTerrainHeight(x, z, biomeDef);

            byte voxelValue = 0;
            //everything higher then terrainHeight is air
            if (y >= terrainHeight)
                return VoxelTypeByte.AIR;

            //top voxels are grass
            if (y == terrainHeight - 1)
                voxelValue = VoxelTypeByte.DIRT_WITH_GRASS;
            //3 voxels under grass are dirt
            else if (y >= terrainHeight - 4)
                voxelValue = VoxelTypeByte.DIRT;
            //rest is rock
            else
                voxelValue = VoxelTypeByte.STONE;

            //LODES PASS
            if (voxelValue == VoxelTypeByte.STONE)
            {
                var lodesCount = biomeDef.Lodes.Length;
                for (var i = 0; i < lodesCount; i++)
                {
                    var lode = biomeDef.Lodes[i];
                    
                    if (y > lode.MinHeight && y < lode.MaxHeight)
                    {
                        var treshold = lode.Treshold;
                        switch (lode.ScaleTresholdByHeight)
                        {
                            case ScaleTresholdByHeight.HighestTop:
                                treshold *= (y - lode.MinHeight) / (float) lode.HeightRange;
                                break;
                            case ScaleTresholdByHeight.HighestBottom:
                                treshold *= (lode.MaxHeight - y) / (float) lode.HeightRange;
                                break;
                        }

                        if (Noise.Get3DPerlin(x, y, z, lode.Offset, lode.Scale, treshold))
                            voxelValue = lode.BlockId;
                    }
                }
            }

            return voxelValue;
        }

        #endregion
    }
}