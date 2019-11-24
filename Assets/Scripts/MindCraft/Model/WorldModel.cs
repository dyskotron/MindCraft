using System.Collections.Generic;
using System.Diagnostics;
using Framewerk.Managers;
using MindCraft.Common;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.View;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MindCraft.Model
{
    public interface IWorldModel
    {
        void GenerateWorldAroundPlayer(ChunkCoord coords);
        void UpdateWorldAroundPlayer(ChunkCoord newCoords);
        
        NativeArray<byte> GetMapByChunkCoords(ChunkCoord coords);
        bool CheckVoxelOnGlobalXyz(float x, float y, float z);
        void EditVoxel(Vector3 position, byte VoxelType);

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
        private Dictionary<ChunkCoord, NativeArray<byte>> _chunkMaps = new Dictionary<ChunkCoord, NativeArray<byte>>();

        //Only player modified voxels stored there
        private Dictionary<ChunkCoord, byte[,,]> _playerModifiedMaps = new Dictionary<ChunkCoord, byte[,,]>();

        private BiomeDef _biomeDef;

        #region Getters / Helper methods

        [PostConstruct]
        public void PostConstruct()
        {
            _biomeDef = AssetManager.GetAsset<BiomeDef>(ResourcePath.BIOME_DEF);
        }

        public NativeArray<byte> GetMapByChunkCoords(ChunkCoord coords)
        {
            return _chunkMaps[coords];
        }

        public bool CheckVoxelOnGlobalXyz(float x, float y, float z)
        {
            var coords = WorldModelHelper.GetChunkCoordsFromWorldXy(x, z);
            var chunkMap = GetMapByChunkCoords(coords);

            WorldModelHelper.GetLocalXyzFromWorldPosition(x, y, z, out int xOut, out int yOut, out int zOut);

            
            return chunkMap[ArrayHelper.To1D(xOut, yOut, zOut)] != BlockTypeByte.AIR;
        }

        #endregion

        public void EditVoxel(Vector3 position, byte VoxelType)
        {
            WorldModelHelper.GetLocalXyzFromWorldPosition(position, out int x, out int y, out int z);
            var coords = WorldModelHelper.GetChunkCoordsFromWorldPosition(position);

            var id = ArrayHelper.To1D(x, y, z);

            var map = _chunkMaps[coords];
            map[id] = VoxelType;
            
            //update voxel in chunk map
            _chunkMaps[coords] = map;

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

        public void GenerateWorldAroundPlayer(ChunkCoord playerCords)
        {
            //create all map data within view distance
            var coordsList = new List<ChunkCoord>();
            foreach (var position in MapBoundsLookup.MapDataGeneration)
            {
                var coords = new ChunkCoord(playerCords.X + position.x, playerCords.Y + position.y);
                if(!_chunkMaps.ContainsKey(coords))
                    coordsList.Add(coords);
            }

            CreateChunkMaps(coordsList);
        }
        
        public void UpdateWorldAroundPlayer(ChunkCoord newCoords)
        {
            //remove chunk data out of sight
            /* TODO: dispose array properly
            foreach (var position in MapBoundsLookup.MapDataRemove)
            {
                _chunkMaps.Remove(new ChunkCoord(newCoords.X + position.x, newCoords.Y + position.y));
            }*/
            
            //create data for chunks coming into view distance
            var coordsList = new List<ChunkCoord>();
            foreach (var position in MapBoundsLookup.MapDataAdd)
            {
                var coords = new ChunkCoord(newCoords.X + position.x, newCoords.Y + position.y);
                if(!_chunkMaps.ContainsKey(coords))
                    coordsList.Add(coords);
            }

            CreateChunkMaps(coordsList);
        }

        /// <summary>
        /// Generates Chunk Map based only on seed and generation algorithm
        /// </summary>
        /// <param name="coordsList"> List of Chunk coordinates></param>
        private void CreateChunkMaps(List<ChunkCoord> coordsList)
        {
            var jobArray = new NativeArray<JobHandle>(coordsList.Count, Allocator.Temp);
            var results = new NativeArray<byte>[coordsList.Count];
            
            var watch = new Stopwatch();
            watch.Start();
            
            for (var i = 0; i < coordsList.Count; i++)
            {
                results[i] = new NativeArray<byte>(VoxelLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
                var handle = CreateMapJob(coordsList[i].X, coordsList[i].Y, _biomeDef, results[i]);
                jobArray[i] = handle;
            }
            
            JobHandle.ScheduleBatchedJobs();
            
            JobHandle.CompleteAll(jobArray);
            jobArray.Dispose();

            var jobTime = watch.ElapsedMilliseconds;
            
            watch.Restart();
            
            for (var i = 0; i < results.Length; i++)
            {
                var coords = coordsList[i];
                _chunkMaps[coords] = results[i];
            }
            
            var processTime = watch.ElapsedMilliseconds;


            Debug.LogWarning($"<color=\"aqua\">WorldModel.CreateChunkMaps() : Jobified map generation ended. jobTime: {jobTime} processTime: {processTime}</color>");
        }

        /// <summary>
        /// Generates Chunk Map based only on seed and generation algorithm
        /// </summary>
        /// <param name="coords"Chunk coordinates></param>
        public void CreateChunkMap(ChunkCoord coords)
        {
            var mapWatch = new Stopwatch();
            mapWatch.Start();
            
            
            var voxelArray = new NativeArray<byte>(VoxelLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
            
            var mapJob = CreateMapJob(coords.X, coords.Y, _biomeDef, voxelArray);
            mapJob.Complete();


            _chunkMaps[coords] = voxelArray;
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
            
            return job.Schedule(VoxelLookups.VOXELS_PER_CHUNK,512);    
        }
        
        [BurstCompile(CompileSynchronously = true)]
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
                return BlockTypeByte.AIR;

            if (y == 0)
                return BlockTypeByte.GREY_STONE;

            // ======== RETURN CACHED VOXELS OR PLAYER MODIFIED VOXELS ========

            var coords = WorldModelHelper.GetChunkCoordsFromWorldXy(x, z);
            var map = GetMapByChunkCoords(coords);

            var index = ArrayHelper.To1D(x - coords.X * VoxelLookups.CHUNK_SIZE, y, z - coords.Y * VoxelLookups.CHUNK_SIZE);
            
            if (map != null)
                return map[index];

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
                return BlockTypeByte.GREY_STONE;

            // ======== BASIC PASS ========

            var terrainHeight = GetTerrainHeight(x, z, biomeDef);

            byte voxelValue = 0;
            //everything higher then terrainHeight is air
            if (y >= terrainHeight)
                return BlockTypeByte.AIR;

            //top voxels are grass
            if (y == terrainHeight - 1)
                voxelValue = BlockTypeByte.DIRT_WITH_GRASS;
            //3 voxels under grass are dirt
            else if (y >= terrainHeight - 4)
                voxelValue = BlockTypeByte.DIRT;
            //rest is rock
            else
                voxelValue = BlockTypeByte.STONE;

            //LODES PASS
            if (voxelValue == BlockTypeByte.STONE)
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