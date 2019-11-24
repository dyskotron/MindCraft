using MindCraft.MapGeneration.Utils;

namespace MindCraft.Common
{
    public static class ArrayHelper
    {
        public static int To1D(int x, int y, int z)
        {
            return z * VoxelLookups.CHUNK_SIZE * VoxelLookups.CHUNK_HEIGHT + y * VoxelLookups.CHUNK_SIZE + x;
        }

        public static void To3D(int idx, out int x, out int y, out int z)
        {
            z = idx / (VoxelLookups.CHUNK_SIZE * VoxelLookups.CHUNK_HEIGHT);
            idx -= (z * VoxelLookups.CHUNK_SIZE * VoxelLookups.CHUNK_HEIGHT);
            y = idx / VoxelLookups.CHUNK_SIZE;
            x = idx % VoxelLookups.CHUNK_SIZE;
        }
    }
}