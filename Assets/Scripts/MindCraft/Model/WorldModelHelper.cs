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
            return new int2(Mathf.FloorToInt(position.x / GeometryConsts.CHUNK_SIZE),
                                  Mathf.FloorToInt(position.z / GeometryConsts.CHUNK_SIZE));
        }

        public static int2 GetChunkCoordsFromWorldXy(float x, float y)
        {
            return new int2(Mathf.FloorToInt(x / GeometryConsts.CHUNK_SIZE),
                                  Mathf.FloorToInt(y / GeometryConsts.CHUNK_SIZE));
        }

        public static int2 GetChunkCoordsFromWorldXy(int x, int y)
        {
            return new int2(Mathf.FloorToInt(x / (float) GeometryConsts.CHUNK_SIZE),
                                  Mathf.FloorToInt(y / (float) GeometryConsts.CHUNK_SIZE));
        }

        public static void GetLocalXyzFromWorldPosition(Vector3 position, out int x, out int y, out int z)
        {
            //always positive modulo hacky solution
            x = (Mathf.FloorToInt(position.x) % GeometryConsts.CHUNK_SIZE + GeometryConsts.CHUNK_SIZE) % GeometryConsts.CHUNK_SIZE;
            y = Mathf.FloorToInt(position.y);
            z = (Mathf.FloorToInt(position.z) % GeometryConsts.CHUNK_SIZE + GeometryConsts.CHUNK_SIZE) % GeometryConsts.CHUNK_SIZE;
        }

        public static void GetLocalXyzFromWorldPosition(float xIn, float yIn, float zIn, out int x, out int y, out int z)
        {
            //always positive modulo hacky solution
            x = (Mathf.FloorToInt(xIn) % GeometryConsts.CHUNK_SIZE + GeometryConsts.CHUNK_SIZE) % GeometryConsts.CHUNK_SIZE;
            y = Mathf.FloorToInt(yIn);
            z = (Mathf.FloorToInt(zIn) % GeometryConsts.CHUNK_SIZE + GeometryConsts.CHUNK_SIZE) % GeometryConsts.CHUNK_SIZE;
        }

        public static Vector3Int FloorPositionToVector3Int(Vector3 position)
        {
            return new Vector3Int(Mathf.FloorToInt(position.x),
                                  Mathf.FloorToInt(position.y),
                                  Mathf.FloorToInt(position.z));
        }
    }
}