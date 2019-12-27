using Framewerk.StrangeCore;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.MapGeneration.Utils
{
    public partial class GeometryLookups : IDestroyable
    {
        public const int FACES_PER_VOXEL = 6;
        public const int TRIANGLE_INDICES_PER_FACE = 6;
        public const int VERTICES_PER_FACE = 4;
        
        public const int MAX_OCTAVES = 10;
        
        public const int CHUNK_SIZE = 8; 
        public const int CHUNK_SIZE_POW2 = CHUNK_SIZE * CHUNK_SIZE; 
        public const int CHUNK_HEIGHT = 128;
        public const int VOXELS_PER_CHUNK = CHUNK_SIZE * CHUNK_SIZE * CHUNK_HEIGHT;
        public const int VOXELS_PER_CLUSTER = VOXELS_PER_CHUNK * 9;
        public const int VIEW_DISTANCE = 100;
        public static readonly int VIEW_DISTANCE_IN_CHUNKS = Mathf.CeilToInt(VIEW_DISTANCE / (float)CHUNK_SIZE);
        
        public const float LIGHT_FALL_OFF = 0.2f;
        public const float MIN_LIGHT = 0.15f;
        
        //
        public const int DIFFUSE_LIGHTS_MARGIN = 5; //CAN'T BE BIGGER THAN CHUNK_SIZE!
        public const int LIGHTS_CLUSTER_MIN = - DIFFUSE_LIGHTS_MARGIN;
        public const int LIGHTS_CLUSTER_MAX = CHUNK_SIZE + DIFFUSE_LIGHTS_MARGIN - 1;
        
        //index of chunk in the center of concenated arrays we send to jobs that needs to know about neighbours
        //center chunk is what we care about, rest is just to get surrounding data
        public const int MULTIMAP_CENTER_OFFSET = 4 * VOXELS_PER_CHUNK;
        
        //relative coordinates of all voxel's neighbours.
        public NativeArray<int3> Neighbours { get; private set; }

        //all 4 neighbouring voxels for given face corner
        public NativeArray<int2x4> LightNeighbours{ get; private set; }
        
        public NativeArray<int> IndexToVertex{ get; private set; }
        
        //vertices for all 8 cube corners
        public NativeArray<int3> VerticesLookup{ get; private set; }
        
        //vertex indexes needed to construct two triangles per face    
        public NativeArray<int> TrianglesLookup{ get; private set; }

        [PostConstruct]
        public void PostConstruct()
        {
            Neighbours = new NativeArray<int3>(6, Allocator.Persistent)
                         {
                             [0] = new int3(0, 0, -1), // Front
                             [1] = new int3(0, 0, 1),  // Back
                             [2] = new int3(0, 1, 0),  // Top
                             [3] = new int3(0, -1, 0), // Bottom
                             [4] = new int3(-1, 0, 0), // Left
                             [5] = new int3(1, 0, 0),  // Right
                         };

            LightNeighbours = new NativeArray<int2x4>(6, Allocator.Persistent)
                              {
                                  [0] = new int2x4(4, 4, 5, 5, 3, 2, 3, 2), // Front     
                                  [1] = new int2x4(5, 5, 4, 4, 3, 2, 3, 2), // Back   
                                  [2] = new int2x4(4, 4, 5, 5, 0, 1, 0, 1), // Top
                                  [3] = new int2x4(5, 5, 4, 4, 0, 1, 0, 1), // Bottom 
                                  [4] = new int2x4(1, 1, 0, 0, 3, 2, 3, 2), // Left  
                                  [5] = new int2x4(0, 0, 1, 1, 3, 2, 3, 2), // Right  
                              };

            IndexToVertex = new NativeArray<int>(6, Allocator.Persistent)
                            {
                                [0] = 0,
                                [1] = 1,
                                [2] = 2,
                                [3] = 2,
                                [4] = 1,
                                [5] = 3
                            };

            VerticesLookup = new NativeArray<int3>(8, Allocator.Persistent)
                             {
                                 [0] = new int3(0, 0, 0),
                                 [1] = new int3(1, 0, 0),
                                 [2] = new int3(1, 1, 0),
                                 [3] = new int3(0, 1, 0),
                                 [4] = new int3(0, 0, 1),
                                 [5] = new int3(1, 0, 1),
                                 [6] = new int3(1, 1, 1),
                                 [7] = new int3(0, 1, 1)
                             };
            
            int[] triangles = 
            {
                0, 3, 1, 2, // Front
                5, 6, 4, 7, // Back
                3, 7, 2, 6, // Top
                1, 5, 0, 4, // Bottom
                4, 7, 0, 3, // Left
                1, 2, 5, 6, // Right
            };
            
            TrianglesLookup = new NativeArray<int>(triangles, Allocator.Persistent);
        }

        public void Destroy()
        {
            Neighbours.Dispose();
            LightNeighbours.Dispose();
            IndexToVertex.Dispose();
            VerticesLookup.Dispose();
            TrianglesLookup.Dispose();
        }
    }
}