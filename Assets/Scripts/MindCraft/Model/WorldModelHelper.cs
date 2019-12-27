using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Model
{
    public static class WorldModelHelper
    {
        public static int2 GetChunkCoordsFromWorldPosition(Vector3 position)
        {
            return new int2(Mathf.FloorToInt(position.x / GeometryLookups.CHUNK_SIZE),
                                  Mathf.FloorToInt(position.z / GeometryLookups.CHUNK_SIZE));
        }

        public static int2 GetChunkCoordsFromWorldXy(float x, float y)
        {
            return new int2(Mathf.FloorToInt(x / GeometryLookups.CHUNK_SIZE),
                                  Mathf.FloorToInt(y / GeometryLookups.CHUNK_SIZE));
        }

        public static int2 GetChunkCoordsFromWorldXy(int x, int y)
        {
            return new int2(Mathf.FloorToInt(x / (float) GeometryLookups.CHUNK_SIZE),
                                  Mathf.FloorToInt(y / (float) GeometryLookups.CHUNK_SIZE));
        }

        public static void GetLocalXyzFromWorldPosition(Vector3 position, out int x, out int y, out int z)
        {
            //always positive modulo hacky solution
            x = (Mathf.FloorToInt(position.x) % GeometryLookups.CHUNK_SIZE + GeometryLookups.CHUNK_SIZE) % GeometryLookups.CHUNK_SIZE;
            y = Mathf.FloorToInt(position.y);
            z = (Mathf.FloorToInt(position.z) % GeometryLookups.CHUNK_SIZE + GeometryLookups.CHUNK_SIZE) % GeometryLookups.CHUNK_SIZE;
        }

        public static void GetLocalXyzFromWorldPosition(float xIn, float yIn, float zIn, out int x, out int y, out int z)
        {
            //always positive modulo hacky solution
            x = (Mathf.FloorToInt(xIn) % GeometryLookups.CHUNK_SIZE + GeometryLookups.CHUNK_SIZE) % GeometryLookups.CHUNK_SIZE;
            y = Mathf.FloorToInt(yIn);
            z = (Mathf.FloorToInt(zIn) % GeometryLookups.CHUNK_SIZE + GeometryLookups.CHUNK_SIZE) % GeometryLookups.CHUNK_SIZE;
        }

        public static Vector3Int FloorPositionToVector3Int(Vector3 position)
        {
            return new Vector3Int(Mathf.FloorToInt(position.x),
                                  Mathf.FloorToInt(position.y),
                                  Mathf.FloorToInt(position.z));
        }
    }
}