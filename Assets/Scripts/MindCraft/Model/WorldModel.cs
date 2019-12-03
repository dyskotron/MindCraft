using System.Collections.Generic;
using Framewerk.Managers;
using Framewerk.StrangeCore;
using MindCraft.Common;
using MindCraft.Common.Serialization;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.View;
using MindCraft.View.Chunk;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Model
{
    public interface IWorldModel : IBinarySerializable
    {
        void CreateChunkMaps(List<ChunkCoord> coordsList);
        
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

    public class WorldModel : IWorldModel, IBinarySerializable, IDestroyable
    {
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public WorldRenderer WorldRenderer { get; set; }

        //map for each generated chunk - only generated data which are always recreated the same
        private Dictionary<ChunkCoord, NativeArray<byte>> _chunkMaps = new Dictionary<ChunkCoord, NativeArray<byte>>();

        //Only player modified voxels stored there
        private Dictionary<ChunkCoord, byte[,,]> _playerModifiedMaps = new Dictionary<ChunkCoord, byte[,,]>();

        private BiomeDefData _biomeDef;

        #region Getters / Helper methods

        [PostConstruct]
        public void PostConstruct()
        {
            _biomeDef = WorldSettings.DefaultBiome;
        }
        
        public void Destroy()
        {
            foreach (var mapData in _chunkMaps.Values)
            {
                mapData.Dispose();
            } 
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
            //so that data can persist when we clean distant chunks from memory and can be used to save / load game
            if (!_playerModifiedMaps.ContainsKey(coords))
                _playerModifiedMaps[coords] = new byte[VoxelLookups.CHUNK_SIZE, VoxelLookups.CHUNK_HEIGHT, VoxelLookups.CHUNK_SIZE];

            _playerModifiedMaps[coords][x, y, z] = VoxelType;

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
                var handle = CreateMapJob(coordsList[i].X, coordsList[i].Y, _biomeDef, results[i]);
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

        private JobHandle CreateMapJob(int chunkX, int chunkY, BiomeDefData biomeDef, NativeArray<byte> map)
        {
            var job = new GenerateMapJob()
                      {
                          ChunkX = chunkX,
                          ChunkY = chunkY,
                          BiomeDef = biomeDef,
                          Map =  map
                      };
            
            return job.Schedule(VoxelLookups.VOXELS_PER_CHUNK,64);    
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

            return GenerateVoxel(x, y, z, _biomeDef);
        }

        public int GetTerrainHeight(int x, int y)
        {
            return GetTerrainHeight(x, y, _biomeDef);
        }

        public static int GetTerrainHeight(int x, int y, BiomeDefData biomeDef)
        {
            var sampleNoise = Noise.Get2DPerlin(x, y, biomeDef.Octaves, biomeDef.Lunacrity, biomeDef.Persistance, biomeDef.TerrainScale, biomeDef.Offset);
            var heightFromNoise = Mathf.FloorToInt(VoxelLookups.CHUNK_HEIGHT * sampleNoise);
            return math.clamp(biomeDef.TerrainCurve[heightFromNoise], 0, VoxelLookups.CHUNK_HEIGHT - 1);
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
                voxelValue = BlockMaskByte.TOP;
            //3 voxels under grass are dirt
            else if (y >= terrainHeight - 4)
                voxelValue = BlockMaskByte.MIDDLE;
            //rest is rock
            else
                voxelValue = BlockMaskByte.BOTTOM;

            //LODES PASS
            bool lodesPassResolved = false;
            var lodesCount = biomeDef.Lodes.Length;
            for (var i = 0; i < lodesCount; i++)
            {
                var lode = biomeDef.Lodes[i];
                
                if((lode.BlockMask & voxelValue) == 0)
                    continue; //try next lode
                
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
                        voxelValue = biomeDef.TopBlock;
                        break;
                    
                    case BlockMaskByte.MIDDLE:
                        voxelValue = biomeDef.MiddleBlock;
                        break;
                    
                    case BlockMaskByte.BOTTOM:
                        voxelValue = biomeDef.BottomBlock;
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
                    map[x,y,z] = blockType;
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
                {
                    Debug.LogWarning($"<color=\"aqua\">WorldModel.To1DChangesOnlyDictionary() => blockId:{(BlockTypeId)blockId} x:{x} y:{y} z:{z} </color>");
                    dict[i] = blockId;
                }
            }
            
            return dict;
        } 
    }
}