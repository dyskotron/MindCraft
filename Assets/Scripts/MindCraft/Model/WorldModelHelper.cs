using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using UnityEngine;

namespace MindCraft.Model
{
    public static class WorldModelHelper
    {
        public static ChunkCoord GetChunkCoordsFromWorldPosition(Vector3 position)
        {
            return new ChunkCoord(Mathf.FloorToInt(position.x / VoxelLookups.CHUNK_SIZE),
                                  Mathf.FloorToInt(position.z / VoxelLookups.CHUNK_SIZE));
        }

        public static ChunkCoord GetChunkCoordsFromWorldXy(float x, float y)
        {
            return new ChunkCoord(Mathf.FloorToInt(x / VoxelLookups.CHUNK_SIZE),
                                  Mathf.FloorToInt(y / VoxelLookups.CHUNK_SIZE));
        }

        public static ChunkCoord GetChunkCoordsFromWorldXy(int x, int y)
        {
            return new ChunkCoord(Mathf.FloorToInt(x / (float) VoxelLookups.CHUNK_SIZE),
                                  Mathf.FloorToInt(y / (float) VoxelLookups.CHUNK_SIZE));
        }

        public static void GetLocalXyzFromWorldPosition(Vector3 position, out int x, out int y, out int z)
        {
            //always positive modulo hacky solution
            x = (Mathf.FloorToInt(position.x) % VoxelLookups.CHUNK_SIZE + VoxelLookups.CHUNK_SIZE) % VoxelLookups.CHUNK_SIZE;
            y = Mathf.FloorToInt(position.y);
            z = (Mathf.FloorToInt(position.z) % VoxelLookups.CHUNK_SIZE + VoxelLookups.CHUNK_SIZE) % VoxelLookups.CHUNK_SIZE;
        }

        public static void GetLocalXyzFromWorldPosition(float xIn, float yIn, float zIn, out int x, out int y, out int z)
        {
            //always positive modulo hacky solution
            x = (Mathf.FloorToInt(xIn) % VoxelLookups.CHUNK_SIZE + VoxelLookups.CHUNK_SIZE) % VoxelLookups.CHUNK_SIZE;
            y = Mathf.FloorToInt(yIn);
            z = (Mathf.FloorToInt(zIn) % VoxelLookups.CHUNK_SIZE + VoxelLookups.CHUNK_SIZE) % VoxelLookups.CHUNK_SIZE;
        }

        public static Vector3Int FloorPositionToVector3Int(Vector3 position)
        {
            return new Vector3Int(Mathf.FloorToInt(position.x),
                                  Mathf.FloorToInt(position.y),
                                  Mathf.FloorToInt(position.z));
        }
    }
}