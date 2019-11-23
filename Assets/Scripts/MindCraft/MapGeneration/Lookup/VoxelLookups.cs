using UnityEngine;

namespace MindCraft.MapGeneration.Lookup
{
    public static class VoxelLookups
    {
        // biggest possible chunk size of 128 height is 6 as worst case is every other cube rendered in all directions
        // that means (24 vertices * 6(width) * 6(depth) * 128(height)) / 2 = 55296 Vertices out of 65534 Vertex per mesh Limit
        public const int CHUNK_SIZE = 6; 
        public const int CHUNK_HEIGHT = 128;
        public const int VIEW_DISTANCE = 100;
        public static readonly int VIEW_DISTANCE_IN_CHUNKS = Mathf.CeilToInt(VIEW_DISTANCE / (float)CHUNK_SIZE);

        public static readonly Vector3Int[] Vertices =
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 1),
            new Vector3Int(1, 1, 1),
            new Vector3Int(0, 1, 1),
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

        public static readonly Vector3Int[] Neighbours =
        {
            new Vector3Int(0,0, -1), // Back
            new Vector3Int(0,0, 1), // Front
            new Vector3Int(0,1,0), // Top
            new Vector3Int(0,-1,0), // Bottom
            new Vector3Int(-1,0,0), // Left
            new Vector3Int(1,0,0), // Right
        };

        public static readonly int[] indexToVertex = {0, 1, 2, 2, 1, 3};
    }
}