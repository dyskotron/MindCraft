using UnityEngine;

namespace MapGeneration
{
    public static class VoxelLookups
    {
        public const int CHUNK_SIZE = 16;
        public const int CHUNK_HEIGHT = 128;

        public static readonly Vector3[] Vertices =
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1),
        };

        public static readonly int[,] Triangles =
        {
            {0, 3, 1, 2}, // Back
            {5, 6, 4, 7}, // Front
            {3, 7, 2, 6}, // Top
            {1, 5, 0, 4}, // Bottom
            {4, 7, 0, 3}, // Left
            {1, 2, 5, 6}, // Right
        };

        public static readonly Vector3[] Neighbours =
        {
            Vector3.back,
            Vector3.forward,
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
        };

        public static readonly int[] indexToVertex = {0, 1, 2, 2, 1, 3};
    }
}