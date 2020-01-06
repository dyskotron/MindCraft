using Framewerk.StrangeCore;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.MapGeneration.Utils
{
    public partial class GeometryLookups : IDestroyable
    {
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