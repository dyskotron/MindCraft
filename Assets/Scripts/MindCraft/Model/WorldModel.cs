using System;
using System.Collections.Generic;
using Framewerk.StrangeCore;
using MindCraft.Common;
using MindCraft.Common.Serialization;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.View.Chunk;
using Unity.Burst;
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
    }

    public class WorldModel : IWorldModel, IDestroyable
    {
        [Inject] public IBiomeDefs BiomeDefs { get; set; }
        [Inject] public IWorldRenderer WorldRenderer { get; set; }

        //map for each generated chunk - only generated data which are always recreated the same
        private Dictionary<ChunkCoord, NativeArray<byte>> _chunkMaps = new Dictionary<ChunkCoord, NativeArray<byte>>();

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
            var jobArray = new NativeArray<JobHandle>(coordsList.Count, Allocator.Temp);
            var results = new NativeArray<byte>[coordsList.Count];

            for (var i = 0; i < coordsList.Count; i++)
            {
                results[i] = new NativeArray<byte>(VoxelLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
                var handle = CreateMapJob(coordsList[i].X, coordsList[i].Y, _biomeDefs, _offsets, results[i], _terrainCurvesSampled, _lodes, _lodeThresholds);
                jobArray[i] = handle;
            }

            JobHandle.CompleteAll(jobArray);
            jobArray.Dispose();

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
            }
        }

        public void RemoveData(List<ChunkCoord> removeDataCords)
        {
            foreach (var removeDataCord in removeDataCords)
            {
                _chunkMaps[removeDataCord].Dispose();
                _chunkMaps.Remove(removeDataCord);
            }
        }

        private JobHandle CreateMapJob(int chunkX, int chunkY, NativeArray<BiomeDefData> biomeDef, NativeArray<float2> offsets, NativeArray<byte> map, NativeArray<int> terrainCurves, NativeArray<LodeDefData> lodes, NativeArray<float> lodeTresholds)
        {
            var job = new GenerateMapJob()
                      {
                          ChunkX = chunkX,
                          ChunkY = chunkY,
                          BiomeDefs = biomeDef,
                          Offsets = offsets,
                          TerrainCurves = terrainCurves,
                          Lodes = lodes,
                          LodeTresholds = lodeTresholds,
                          Map = map
                      };

            return job.Schedule(VoxelLookups.VOXELS_PER_CHUNK, 64);
        }

        [BurstCompile(CompileSynchronously = true)]
        public struct GenerateMapJob : IJobParallelFor
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
                Map[index] = GenerateVoxel(x + ChunkX * VoxelLookups.CHUNK_SIZE, y, z + ChunkY * VoxelLookups.CHUNK_SIZE, BiomeDefs, Offsets, TerrainCurves, Lodes, LodeTresholds);
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

        /*
        public int GetTerrainHeight(int x, int y)
        {
            return GetTerrainHeight(x, y, _biomeDefs[0], _offsets, _terrainCurvesSampled);
        }*/

        public static int GetTerrainHeight(int x, int y, BiomeDefData biomeDef, NativeArray<float2> offsets, NativeArray<int> terrainCurve)
        {
            var sampleNoise = Noise.GetHeight(x, y, biomeDef.Octaves, biomeDef.Lacunarity, biomeDef.Persistance, biomeDef.Frequency, offsets, biomeDef.Offset);
            var heightFromNoise = Mathf.FloorToInt(VoxelLookups.CHUNK_HEIGHT * sampleNoise);
            return math.clamp(terrainCurve[biomeDef.TerrainCurveStartPos + heightFromNoise], 0, VoxelLookups.CHUNK_HEIGHT - 1);
        }

        private static int GetBiomeIdFromHeightMap(float x, int range)
        {
            var biomeId = Mathf.FloorToInt(x * range);
            biomeId = math.clamp(biomeId, 0, range - 1);
            return biomeId;
        }

        /// <summary>
        /// Generates single voxel on world coordinates - called only by WorldModel when we truly want to generate
        /// and know that coordinates are valid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="biomeDefs"></param>
        /// <param name="offsets"></param>
        /// <param name="terrainCurves"></param>
        /// <param name="lodes"></param>
        /// <returns></returns>
        private static byte GenerateVoxel(int x, int y, int z, NativeArray<BiomeDefData> biomeDefs, NativeArray<float2> offsets, NativeArray<int> terrainCurves, NativeArray<LodeDefData> lodes, NativeArray<float> tresholds)
        {
            //TODO: generate whole column, so height biome and determination etc is not called every voxel!


            // ======== STATIC RULES ========

            if (y == 0)
                return BlockTypeByte.GREY_STONE;

            // ======== BIOME PASS ========

            BiomeDefData biome = biomeDefs[0];

            float closest = 300;

            float totalHeight = 0;
            float totalWeight = 0;

            //hardcoded noise function to get biome temperature
            var temperature = 0.5f + noise.snoise(new float2(x * 0.0012523f, z * 0.000932f)) / 2f;
            temperature += 0.1f * (0.5f + noise.snoise(new float2(x * 0.042523f, z * 0.03932f)) / 2f);
            temperature /= 1.1f;
            
            //var cellular = noise.cellular2x2(new float2(x * 0.022523f, z * 0.0232f));
            //var temperature = cellular.x;
            //var temperature = 0.5f + noise.cnoise(noise.cellular(new float2(x * 0.0062523f , z  * 0.00432f ))) / 2f;

            //var id = (byte) math.max((cellular.y * (BlockTypeByte.TypeCount - 1)), 1);
            //id = (byte)math.clamp(id, 2, BlockTypeByte.TypeCount - 1);

            //return y > temperature * VoxelLookups.CHUNK_HEIGHT ? BlockTypeByte.AIR : BlockTypeByte.STONE;

            float difference = 0;

            for (var i = 0; i < biomeDefs.Length; i++)
            {
                var currentDef = biomeDefs[i];
                //var temperatureMap = (noise.cellular(new float2(x * currentDef.Frequency, z * currentDef.Frequency)));
                //var temperature = 0.5f + temperatureMap.x / 2;

                // var biomeTemperature = i / (float) biomeDefs.Length;

                difference = math.abs(currentDef.Temperature - temperature);

                var weight = (float) Math.Pow(biomeDefs.Length - math.min(difference, biomeDefs.Length), 50);

                totalHeight += weight * GetTerrainHeight(x, z, currentDef, offsets, terrainCurves);
                totalWeight += weight;

                if (difference < closest)
                {
                    closest = difference;
                    biome = currentDef;
                }
            }

            var terrainHeight = Mathf.FloorToInt(totalHeight / totalWeight);
            //var terrainHeight =GetTerrainHeight(x, z, biome, offsets, terrainCurves);

            // ======== BASIC PASS ========

            byte voxelValue = 0;
            //everything higher then terrainHeight is air
            if (y >= terrainHeight)
                return BlockTypeByte.AIR;

            //top voxels are grass
            if (y == terrainHeight - 1)
                voxelValue = BlockMaskByte.TOP;
            //3 voxels under grass are dirt
            else if (y >= terrainHeight - 4)
                voxelValue = BlockMaskByte.MIDDLE;
            //rest is rock
            else
                voxelValue = BlockMaskByte.BOTTOM;

            //LODES PASS
            bool lodesPassResolved = false;

            for (var i = biome.LodesStartPos; i < biome.LodesCount; i++)
            {
                var lode = lodes[i];

                if ((lode.BlockMask & voxelValue) == 0)
                    continue; //try next lode

                if (y > lode.MinHeight && y < lode.MaxHeight)
                {
                    var treshold = tresholds[biome.LodesStartPos + i * VoxelLookups.CHUNK_HEIGHT + y];

                    //tresholds  biomeDef.GetLodeTreshold(i, y);

                    //if (Noise.Get3DPerlin(x, y, z, lode.Offset, lode.Frequency, treshold))
                    //TODO make noise 2d/3d lode option
                    if (Noise.GetLodePresence(lode.Algorithm, x, y, z, lode.Offset, lode.Frequency, treshold))
                    {
                        voxelValue = lode.BlockId;
                        lodesPassResolved = true;
                        break; //We found our block - continue to next pass!
                    }
                }
            }


            //if no lode was applied, show basic biome block for given placeholder
            if (!lodesPassResolved)
            {
                switch (voxelValue)
                {
                    case BlockMaskByte.TOP:
                        voxelValue = biome.TopBlock;
                        break;

                    case BlockMaskByte.MIDDLE:
                        voxelValue = biome.MiddleBlock;
                        break;

                    case BlockMaskByte.BOTTOM:
                        voxelValue = biome.BottomBlock;
                        break;
                }
            }

            return voxelValue;
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