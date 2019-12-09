using System.Collections.Generic;
using Framewerk.StrangeCore;
using MindCraft.Common;
using MindCraft.Common.Serialization;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model.Jobs;
using MindCraft.View.Chunk;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace MindCraft.Model
{
    public interface IWorldModel : IBinarySerializable
    {
        void CreateChunkMaps(List<ChunkCoord> coordsList);

        NativeArray<byte> GetMapByChunkCoords(ChunkCoord coords);
        bool CheckVoxelOnGlobalXyz(float x, float y, float z);
        void EditVoxel(Vector3 position, byte voxelType);

        /// <summary>
        /// Returns voxel on world coordinates - decides if we need to generate the voxel or we can retrieve that from existing chunk
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        byte GetVoxel(int x, int y, int z);

        void RemoveData(List<ChunkCoord> removeDataCords);

        int GetTerrainHeight(Vector3 position);
    }

    public class WorldModel : IWorldModel, IDestroyable
    {
        [Inject] public IBiomeDefs BiomeDefs { get; set; }
        [Inject] public IWorldRenderer WorldRenderer { get; set; }

        //map for each generated chunk - only generated data which are always recreated the same
        private Dictionary<ChunkCoord, NativeArray<byte>> _chunkMaps = new Dictionary<ChunkCoord, NativeArray<byte>>();
        private Dictionary<ChunkCoord, NativeArray<int>> _heights = new Dictionary<ChunkCoord, NativeArray<int>>();

        //Only player modified voxels stored there
        private Dictionary<ChunkCoord, byte[,,]> _playerModifiedMaps = new Dictionary<ChunkCoord, byte[,,]>();

        private NativeArray<BiomeDefData> _biomeDefs;
        private NativeArray<int> _terrainCurvesSampled;
        private NativeArray<LodeDefData> _lodes;
        private NativeArray<float> _lodeThresholds;

        private NativeArray<float2> _offsets;

        [PostConstruct]
        public void PostConstruct()
        {
            _biomeDefs = BiomeDefs.BiomeDefData;
            _terrainCurvesSampled = BiomeDefs.TerrainCurvesSampled;
            _lodes = BiomeDefs.Lodes;
            _lodeThresholds = BiomeDefs.LodeThresholds;

            //generate octave offsets
            var random = new Random();
            random.InitState(928349238); // TODO fill with seed 
            _offsets = new NativeArray<float2>(VoxelLookups.MAX_OCTAVES, Allocator.Persistent);
            _offsets[0] = 0;
            for (var i = 1; i < VoxelLookups.MAX_OCTAVES; i++)
            {
                _offsets[i] = random.NextFloat2(-1f, 1f);
            }
        }

        #region Getters / Helper methods

        public void Destroy()
        {
            foreach (var mapData in _chunkMaps.Values)
            {
                mapData.Dispose();
            }
            
            foreach (var height in _heights.Values)
            {
                height.Dispose();
            }

            _offsets.Dispose();
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

        public void EditVoxel(Vector3 position, byte voxelType)
        {
            WorldModelHelper.GetLocalXyzFromWorldPosition(position, out int x, out int y, out int z);
            var coords = WorldModelHelper.GetChunkCoordsFromWorldPosition(position);

            var id = ArrayHelper.To1D(x, y, z);

            var map = _chunkMaps[coords];
            map[id] = voxelType;

            //update voxel in chunk map
            _chunkMaps[coords] = map;

            //update voxel in user player modifications map
            //so that data can persist when we clean distant chunks from memory and can be used to save / load game
            if (!_playerModifiedMaps.ContainsKey(coords))
                _playerModifiedMaps[coords] = new byte[VoxelLookups.CHUNK_SIZE, VoxelLookups.CHUNK_HEIGHT, VoxelLookups.CHUNK_SIZE];

            _playerModifiedMaps[coords][x, y, z] = voxelType;

            //TODO: chunks update should not be called directly from model!
            //Also get chunks list in less ugly way than this hardcoded shiat
            var updateCoords = new List<ChunkCoord>();
            updateCoords.Add(coords);

            if (x <= 0)
                updateCoords.Add(coords + ChunkCoord.Left);
            else if (x >= VoxelLookups.CHUNK_SIZE - 1)
                updateCoords.Add(coords + ChunkCoord.Right);

            if (z <= 0)
            {
                updateCoords.Add(coords + ChunkCoord.Back);

                if (x <= 0)
                    updateCoords.Add(coords + ChunkCoord.LeftBack);
                else if (x >= VoxelLookups.CHUNK_SIZE - 1)
                    updateCoords.Add(coords + ChunkCoord.RightBack);
            }
            else if (z >= VoxelLookups.CHUNK_SIZE - 1)
            {
                updateCoords.Add(coords + ChunkCoord.Front);

                if (x <= 0)
                    updateCoords.Add(coords + ChunkCoord.LeftFront);
                else if (x >= VoxelLookups.CHUNK_SIZE - 1)
                    updateCoords.Add(coords + ChunkCoord.RightFront);
            }

            WorldRenderer.RenderChunks(updateCoords, updateCoords);
        }

        #region Terrain Generation

        /// <summary>
        /// Generates Chunk Map based only on seed and generation algorithm
        /// </summary>
        /// <param name="coordsList"> List of Chunk coordinates></param>
        public void CreateChunkMaps(List<ChunkCoord> coordsList)
        {
            var generateJobArray = new NativeArray<JobHandle>(coordsList.Count, Allocator.Temp);
            var results = new NativeArray<byte>[coordsList.Count];

            var biomes = new NativeArray<byte>[coordsList.Count];
            var heights = new NativeArray<int>[coordsList.Count];

            for (var i = 0; i < coordsList.Count; i++)
            {
                results[i] = new NativeArray<byte>(VoxelLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
                biomes[i] = new NativeArray<byte>(VoxelLookups.CHUNK_SIZE_POW2, Allocator.Persistent);
                heights[i] = new NativeArray<int>(VoxelLookups.CHUNK_SIZE_POW2, Allocator.Persistent);

                var biomeAndHeightJob = new GetBiomeAndHeightJob()
                                        {
                                            ChunkX = coordsList[i].X,
                                            ChunkY = coordsList[i].Y,
                                            BiomeDefs = _biomeDefs,
                                            Offsets = _offsets,
                                            TerrainCurves = _terrainCurvesSampled,
                                            Biomes = biomes[i],
                                            Heights = heights[i]
                                        };

                var handle = biomeAndHeightJob.Schedule(VoxelLookups.CHUNK_SIZE_POW2, 64);

                var generateDataJob = new GenerateChunkDataJob()
                                      {
                                          ChunkX = coordsList[i].X,
                                          ChunkY = coordsList[i].Y,
                                          BiomeDefs = _biomeDefs,
                                          Lodes = _lodes,
                                          LodeTresholds = _lodeThresholds,
                                          Map = results[i],
                                          Biomes = biomes[i],
                                          Heights = heights[i]
                                      };

                generateJobArray[i] = generateDataJob.Schedule(VoxelLookups.VOXELS_PER_CHUNK, 64, handle);
            }

            JobHandle.CompleteAll(generateJobArray);
            generateJobArray.Dispose();

            for (var i = 0; i < results.Length; i++)
            {
                var coords = coordsList[i];

                if (_playerModifiedMaps.ContainsKey(coords))
                {
                    var map = _playerModifiedMaps[coords];
                    for (var iX = 0; iX < VoxelLookups.CHUNK_SIZE; iX++)
                    {
                        for (var iY = 0; iY < VoxelLookups.CHUNK_HEIGHT; iY++)
                        {
                            for (var iZ = 0; iZ < VoxelLookups.CHUNK_SIZE; iZ++)
                            {
                                var blockId = map[iX, iY, iZ];
                                if (blockId != BlockTypeByte.NONE)
                                {
                                    var id = ArrayHelper.To1D(iX, iY, iZ);
                                    results[i][id] = blockId;
                                }
                            }
                        }
                    }
                }

                _chunkMaps[coords] = results[i];
                _heights[coords] = heights[i];
            }

            //dispose biomes native array, maps and heights are stored so model will dispose those later
            for (var i = 0; i < biomes.Length; i++)
            {
                biomes[i].Dispose();
            }
        }

        public void RemoveData(List<ChunkCoord> removeDataCords)
        {
            foreach (var removeDataCord in removeDataCords)
            {
                _chunkMaps[removeDataCord].Dispose();
                _chunkMaps.Remove(removeDataCord);
                
                _heights[removeDataCord].Dispose();
                _heights.Remove(removeDataCord);
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

            return map[index];
        }


        public int GetTerrainHeight(Vector3 position)
        {
            WorldModelHelper.GetLocalXyzFromWorldPosition(position, out int x, out int y, out int z);
            var coords = WorldModelHelper.GetChunkCoordsFromWorldPosition(position);



            return _heights[coords][x * VoxelLookups.CHUNK_SIZE + z];
        }

        #endregion

        public void Serialize(BinaryWriter writer)
        {
            writer.Begin();
            writer.Write(_playerModifiedMaps.Count);

            foreach (var playerModifiedMap in _playerModifiedMaps)
            {
                var dict = To1DChangesOnlyDictionary(playerModifiedMap.Value);

                writer.Write(playerModifiedMap.Key);
                writer.Write(dict.Count);

                foreach (var keyValuePair in dict)
                {
                    writer.Write(keyValuePair.Key);
                    writer.Write(keyValuePair.Value);
                }
            }

            writer.End();
        }

        public void Deserialize(BinaryReader reader)
        {
            var mapCount = reader.ReadInt();

            for (var iMaps = 0; iMaps < mapCount; iMaps++)
            {
                var coords = reader.Read<ChunkCoord>();
                var dictLength = reader.ReadInt();

                var map = new byte[VoxelLookups.CHUNK_SIZE, VoxelLookups.CHUNK_HEIGHT, VoxelLookups.CHUNK_SIZE];

                for (var iDict = 0; iDict < dictLength; iDict++)
                {
                    var id = reader.ReadInt();
                    ArrayHelper.To3D(id, out int x, out int y, out int z);
                    var blockType = reader.ReadByte();
                    map[x, y, z] = blockType;
                }

                _playerModifiedMaps[coords] = map;
            }
        }

        private Dictionary<int, byte> To1DChangesOnlyDictionary(byte[,,] changes)
        {
            var dict = new Dictionary<int, byte>();

            var length = VoxelLookups.CHUNK_HEIGHT * VoxelLookups.CHUNK_SIZE * VoxelLookups.CHUNK_SIZE;

            for (int i = 0; i < length; i++)
            {
                ArrayHelper.To3D(i, out int x, out int y, out int z);

                var blockId = changes[x, y, z];

                if (blockId != BlockTypeByte.NONE)
                    dict[i] = blockId;
            }

            return dict;
        }
    }
}