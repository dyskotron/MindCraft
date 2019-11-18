using System.Collections.Generic;
using System.Diagnostics;
using MapGeneration;
using MapGeneration.Defs;
using MapGeneration.Lookup;
using UnityEngine;

namespace Model
{
    public class WorldModel
    {
        public World World = Locator.World;
        
        private Dictionary<ChunkCoord, byte[,,]> _chunkMaps = new Dictionary<ChunkCoord, byte[,,]>();

        #region Getters / Helper methods

        public static ChunkCoord GetChunkCoordsFromWorldPosition(Vector3 position)
        {
            return new ChunkCoord(Mathf.FloorToInt(position.x / VoxelLookups.CHUNK_SIZE),
                                  Mathf.FloorToInt(position.z / VoxelLookups.CHUNK_SIZE));
        }
        
        public static ChunkCoord GetChunkCoordsFromWorldXyz(int x, int y)
        {
            return new ChunkCoord(Mathf.FloorToInt(x / (float)VoxelLookups.CHUNK_SIZE),
                                  Mathf.FloorToInt(y / (float)VoxelLookups.CHUNK_SIZE));
        }

        //?? check negative modulo?
        public static void GetLocalXyzFromWorldPosition(Vector3 position, out int x, out int y, out int z)
        {
            x = Mathf.FloorToInt(position.x) % VoxelLookups.CHUNK_SIZE;
            y = Mathf.FloorToInt(position.y);
            z = Mathf.FloorToInt(position.z) % VoxelLookups.CHUNK_SIZE;
        } 
        
        public byte[,,] TryGetMapByChunkCoords(ChunkCoord coords)
        {
            _chunkMaps.TryGetValue(coords, out byte[,,] chunkMap);
            return chunkMap;
        }

        #endregion

        //TODO: x y z and use GetLocalXyzFromWorldPosition instead
        public void EditVoxel(Vector3 position, byte VoxelType)
        {
            var posX = Mathf.FloorToInt(position.x);
            var posY = Mathf.FloorToInt(position.y);
            var posZ = Mathf.FloorToInt(position.z);

            var coords = GetChunkCoordsFromWorldPosition(position);
            
            posX -= coords.X * VoxelLookups.CHUNK_SIZE;
            posZ -= coords.Y * VoxelLookups.CHUNK_SIZE;

            _chunkMaps[coords][posX, posY, posZ] = VoxelType;
            
            //TODO: chunks update should not be called from model!!!
            World.GetChunk(coords).UpdateChunkMesh(_chunkMaps[coords]);
            
            ChunkCoord neighbourCoords;
            
            if (posX <= 0) // Update left neighbour
            {
                neighbourCoords = coords + ChunkCoord.Left;
                World.GetChunk(neighbourCoords).UpdateChunkMesh(_chunkMaps[neighbourCoords]);
            }
            else if (posX >= VoxelLookups.CHUNK_SIZE - 1) // Update right neighbour
            {
                neighbourCoords = coords + ChunkCoord.Right;
                World.GetChunk(neighbourCoords).UpdateChunkMesh(_chunkMaps[neighbourCoords]);
                
            }  
            
            if (posZ <= 0) // Update back neighbour
            {
                neighbourCoords = coords + ChunkCoord.Back;
                World.GetChunk(neighbourCoords).UpdateChunkMesh(_chunkMaps[neighbourCoords]);
                
            }
            else if (posZ >= VoxelLookups.CHUNK_SIZE - 1) // Update forward neighbour
            {
                neighbourCoords = coords + ChunkCoord.Forward;
                World.GetChunk(neighbourCoords).UpdateChunkMesh(_chunkMaps[neighbourCoords]);
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

            for (var iX = 0; iX < VoxelLookups.CHUNK_SIZE; iX++)
            {
                for (var iZ = 0; iZ < VoxelLookups.CHUNK_SIZE; iZ++)
                {
                    for (var iY = 0; iY < VoxelLookups.CHUNK_HEIGHT; iY++)
                    {
                        map[iX, iY, iZ] = GenerateVoxel(iX + coords.X * VoxelLookups.CHUNK_SIZE, iY, iZ+ coords.Y * VoxelLookups.CHUNK_SIZE);
                    }
                }
            }

            _chunkMaps[coords] = map;
            
            mapWatch.Stop();
            Chunk.MAP_ELAPSED_TOTAL += mapWatch.Elapsed.TotalSeconds;

            return map;
        }

        /// <summary>
        /// Returns voxel on world coordinates - decides if we need to generate the voxel or we can retrieve that from already generated data
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
                return VoxelTypeByte.HARD_ROCK;

            // ======== RETURN CACHED VOXELS AND PLAYER MODIFIED VOXELS ========

            var coords = GetChunkCoordsFromWorldXyz(x, z);
            var map = TryGetMapByChunkCoords(coords);
            if (map != null)
                return map[x - coords.X * VoxelLookups.CHUNK_SIZE, y, z - coords.Y * VoxelLookups.CHUNK_SIZE];

            // ======== BASIC PASS ========

            return GenerateVoxel(x, y, z);
        }
        
        public int GetTerainHeight(int x, int y)
        {
            return Mathf.FloorToInt(World.BiomeDef.TerrainMin + World.BiomeDef.TerrainHeight * Noise.Get2DPerlin(x, y, 0, World.BiomeDef.TerrainScale));
        }
        
        /// <summary>
        /// Generates single voxel on world coordinates - called only by WorldModel when we truly want to generate
        /// and know that coordinates are valid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private byte GenerateVoxel(int x, int y, int z)
        {
            // ======== STATIC RULES ========

            if (y == 0)
                return VoxelTypeByte.HARD_ROCK;

            // ======== BASIC PASS ========

            var terrainHeight = GetTerainHeight(x, z);

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
                voxelValue = VoxelTypeByte.ROCK;

            //LODES PASS
            if (voxelValue == VoxelTypeByte.ROCK)
            {
                foreach (var lode in World.BiomeDef.Lodes)
                {
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