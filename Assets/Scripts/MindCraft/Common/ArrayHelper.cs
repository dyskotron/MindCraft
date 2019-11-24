using MindCraft.MapGeneration.Utils;

namespace MindCraft.Common
{
    public static class ArrayHelper
    {
        public static int To1D(int x, int y, int z, int xMax = VoxelLookups.CHUNK_SIZE, int yMax = VoxelLookups.CHUNK_HEIGHT)
        {
            return z * xMax * yMax + y * xMax + x;
        }

        public static void To3D(int idx, out int x, out int y, out int z, int xMax = VoxelLookups.CHUNK_SIZE, int yMax = VoxelLookups.CHUNK_HEIGHT)
        {
            z = idx / (xMax * yMax);
            idx -= (z * xMax * yMax);
            y = idx / xMax;
            x = idx % xMax;
        }
    }
}