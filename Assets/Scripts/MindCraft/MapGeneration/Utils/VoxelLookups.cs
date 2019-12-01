using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.MapGeneration.Utils
{
    public static class VoxelLookups
    {
        public const int CHUNK_SIZE = 8; 
        public const int CHUNK_HEIGHT = 128;
        public const int VOXELS_PER_CHUNK = CHUNK_SIZE * CHUNK_SIZE * CHUNK_HEIGHT;
        public const int VIEW_DISTANCE = 100;
        public const float LIGHT_FALL_OFF = 0.3f;
        public static readonly int VIEW_DISTANCE_IN_CHUNKS = Mathf.CeilToInt(VIEW_DISTANCE / (float)CHUNK_SIZE);
        
        //index of chunk in the center of concenated arrays we send to jobs that needs to know about neighbours
        //center chunk is what we care about, rest is just to get surrounding data
        public static readonly int MULTIMAP_CENTER_OFFSET = 4 * VoxelLookups.VOXELS_PER_CHUNK;

        //array of all local vertex coordinates for cube
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

        //array of vertices that makes two triangles per face    
        public static readonly int[,] Triangles =
        {
            {0, 3, 1, 2}, // Front
            {5, 6, 4, 7}, // Back
            {3, 7, 2, 6}, // Top
            {1, 5, 0, 4}, // Bottom
            {4, 7, 0, 3}, // Left
            {1, 2, 5, 6}, // Right
        };

        //array of relative neighbour coordinates per face
        public static readonly Vector3Int[] Neighbours =
        {
            new Vector3Int(0,0, -1), // Front
            new Vector3Int(0,0, 1), // Back
            new Vector3Int(0,1,0), // Top
            new Vector3Int(0,-1,0), // Bottom
            new Vector3Int(-1,0,0), // Left
            new Vector3Int(1,0,0), // Right
        };
        
        public static readonly int3[] NeighboursInt3 =
        {
            new int3(0,0, -1), // Front
            new int3(0,0, 1), // Back
            new int3(0,1,0), // Top
            new int3(0,-1,0), // Bottom
            new int3(-1,0,0), // Left
            new int3(1,0,0), // Right
        };

        public static readonly int[] indexToVertex = {0, 1, 2, 2, 1, 3};

        
        public static readonly int2x4[] LightNeighbours =
        {
            new int2x4(4,4,5,5,3,2,3,2), // Front     
            new int2x4(5,5,4,4,3,2,3,2), // Back   
            new int2x4(4,4,5,5,0,1,0,1), // Top
            new int2x4(5,5,4,4,0,1,0,1), // Bottom 
            new int2x4(1,1,0,0,3,2,3,2), // Left  
            new int2x4(0,0,1,1,3,2,3,2), // Right  
        };

    }
}