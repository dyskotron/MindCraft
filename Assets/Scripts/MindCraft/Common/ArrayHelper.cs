using MindCraft.MapGeneration.Utils;

namespace MindCraft.Common
{
    //Bitshift Values for common chunk sizes
    // << 1   ->    2
    // << 2   ->    4
    // << 3   ->    8
    // << 4   ->   16
    // << 5   ->   32
    // << 6   ->   64
    // << 7   ->  128
    // << 8   ->  256
    // << 9   ->  512
    // << 10  -> 1024


    public static class ArrayHelper
    {
        public static int To1D(int x, int y, int z, int xMax = GeometryConsts.CHUNK_SIZE, int yMax = GeometryConsts.CHUNK_HEIGHT)
        {
            return z * xMax * yMax + y * xMax + x;
        }

        // returns voxel position in 3 x 3 cluster array
        // central chunk is 0 -> GeometryLookups.CHUNK_SIZE;
        // cluster range is -GeometryLookups.CHUNK_SIZE -> 2 * GeometryLookups.CHUNK_SIZE
        public static int ToCluster1D(int x, int y, int z)
        {
            //determine chunk  offset in cluster
            var clusterX = (x + GeometryConsts.CHUNK_SIZE) >> 3; // -> / GeometryLookups.CHUNK_SIZE
            x = (x + GeometryConsts.CHUNK_SIZE) & 7; // -> % GeometryLookups.CHUNK_SIZE

            var clusterZ = (z + GeometryConsts.CHUNK_SIZE) >> 3; // -> / GeometryLookups.CHUNK_SIZE
            z = (z + GeometryConsts.CHUNK_SIZE) & 7; // -> % GeometryLookups.CHUNK_SIZE

            var offset = (clusterX + 3 * clusterZ) << 13; //-> * GeometryLookups.VOXELS_PER_CHUNK

            return offset + (z << 10) + (y << 3) + x; 
        }

        public static void To3D(int id, out int x, out int y, out int z, int xMax = GeometryConsts.CHUNK_SIZE, int yMax = GeometryConsts.CHUNK_HEIGHT)
        {
            z = id / (xMax * yMax); // a = b / c
            id -= (z * xMax * yMax); // x = b - a * c
            y = id / xMax;
            x = id % xMax;
        }

        //specific for map, dimensions hardcoded as bitwise oprations
        public static int To1DMap(int x, int y, int z)
        {
            return (z << 10) + (y << 3) + x; //z * 8 * 128 + y * 8 + x
        }

        //specific for map, dimensions hardcoded as bitwise oprations
        public static void To3DMap(int id, out int x, out int y, out int z)
        {
            z = id >> 10; //10 = bitwise equivalent to division by 1024 (8 * 128)
            id &= 1023; // bitwise % 1024 (1024 - 1)
            y = id >> 3; // shift => to divide by
            x = id & 7; // bitwise % 8 (8 - 1)
        }
    }
}