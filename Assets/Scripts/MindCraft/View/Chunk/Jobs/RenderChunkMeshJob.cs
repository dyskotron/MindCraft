using MindCraft.Common;
using MindCraft.Data;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.View.Chunk
{
    public struct RenderChunkMeshJob : IJob
    {
        [ReadOnly] public NativeArray<byte> MapData;
        [ReadOnly] public NativeArray<float2> UvLookup;
        [ReadOnly] public NativeArray<bool> TransparencyLookup;

        [WriteOnly] public NativeList<float3> Vertices;
        [WriteOnly] public NativeList<int> Triangles;
        [WriteOnly] public NativeList<float2> Uvs;
        [WriteOnly] public NativeList<float> Colors;

        public NativeArray<float> LightLevels;

        public int _currentVertexIndex;

        public void Execute()
        {
            //for(var index = 0; index < MapData.Length; index++){
            for (var x = 0; x < VoxelLookups.CHUNK_SIZE; x++)
            {
                for (var z = 0; z < VoxelLookups.CHUNK_SIZE; z++)
                {
                    for (var y = VoxelLookups.CHUNK_HEIGHT - 1; y >= 0; y--)
                    {
                        //we need x, y, z at this point for light
                        //no need to use more expensive ArrayHelper.ToCluster1D as we are going only trough middle chunk
                        var index = ArrayHelper.To1D(x, y, z) + VoxelLookups.MULTIMAP_CENTER_OFFSET;
                        
                        var voxelId = MapData[index];

                        if (voxelId == BlockTypeByte.AIR)
                            continue;

                        var position = new Vector3(x, y, z);

                        //iterate faces
                        for (int iF = 0; iF < ChunkView.FACES_PER_VOXEL; iF++)
                        {
                            //check neighbours
                            var neighbourPos = VoxelLookups.Neighbours[iF];
                            var lightNeighbours = VoxelLookups.LightNeighbours[iF];

                            var nX = x + neighbourPos.x;
                            var nY = y + neighbourPos.y;
                            var nZ = z + neighbourPos.z;

                            if (!GetTransparency(nX, nY, nZ))
                                continue;

                            var neighbourId = ArrayHelper.ToCluster1D(nX, nY, nZ);
                            //var neighbourId = ArrayHelper.To1D(nX, nY, nZ) + VoxelLookups.MULTIMAP_CENTER_OFFSET;

                            //iterate triangles
                            for (int iV = 0; iV < ChunkView.TRIANGLE_INDICES_PER_FACE; iV++)
                            {
                                var vertexIndex = VoxelLookups.indexToVertex[iV];

                                // each face needs just 4 vertices & UVs
                                if (iV < ChunkView.VERTICES_PER_FACE)
                                {
                                    Vertices.Add(position + VoxelLookups.Vertices[VoxelLookups.Triangles[iF, iV]]);

                                    var uvId = ArrayHelper.To1D(voxelId, iF, iV, TextureLookup.MAX_BLOCKDEF_COUNT, TextureLookup.FACES_PER_VOXEL);
                                    Uvs.Add(UvLookup[uvId]);
                                    
                                    //basic light level based on face direct neighbour
                                    var lightLevel = LightLevels[neighbourId];
                                    /*

                                    //compute light from vertex adjacent neighbours

                                    //so we're getting two neighbours /of vertex /specific for face 

                                    Vector3Int diagonal = new Vector3Int();

                                    for (var iL = 0; iL < 2; iL++)
                                    {
//                                                if (iL == 0)
//                                                {
//                                                    lightLevel += 1;
//                                                    continue;
//                                                } 

                                        var lightNeighbour = VoxelLookups.Neighbours[lightNeighbours[iV][iL]];
                                        var lnX = nX + lightNeighbour.x;
                                        var lnY = nY + lightNeighbour.y;
                                        var lnZ = nZ + lightNeighbour.z;

                                        lightLevel += GetVertexNeighbourLightLevel(lnX, lnY, lnZ);

                                        diagonal += lightNeighbour;
                                    }

                                    //+ ugly hardcoded diagonal brick

                                    var lnXDiagonal = nX + diagonal.x;
                                    var lnYDiagonal = nY + diagonal.y;
                                    var lnZDiagonal = nZ + diagonal.z;
                                    lightLevel += GetVertexNeighbourLightLevel(lnXDiagonal, lnYDiagonal, lnZDiagonal);


                                    Colors.Add(lightLevel * 0.25f); //multiply instead of divide by 3 as that's faster - but we can use >> 2 in the end
                                    */
                                    Colors.Add(lightLevel);
                                    
                                }

                                //we still need 6 triangle vertices tho
                                Triangles.Add(_currentVertexIndex + vertexIndex);
                            }

                            _currentVertexIndex += ChunkView.VERTICES_PER_FACE;
                        }
                    }
                }
            }
        }
        
        private bool GetTransparency(int x, int y, int z)
        {
            if (y >= VoxelLookups.CHUNK_HEIGHT)
                return true;

            if (y < 0)
                return false;
            
            
            var xOffset = (x + VoxelLookups.CHUNK_SIZE) / VoxelLookups.CHUNK_SIZE;
            var zOffset = (z + VoxelLookups.CHUNK_SIZE) / VoxelLookups.CHUNK_SIZE;
            
            //adjust x,z to be always within chunk voxel range
            x = (x + VoxelLookups.CHUNK_SIZE) % VoxelLookups.CHUNK_SIZE;
            z = (z + VoxelLookups.CHUNK_SIZE) % VoxelLookups.CHUNK_SIZE;

            var chunkAddress = (xOffset + zOffset * 3) * VoxelLookups.VOXELS_PER_CHUNK;
            
            //TODO: specific checks for each direction when using by face checks, don't test in rest of cases at all
//            if (IsVoxelInChunk(x, y, z))
//            {
                var id = ArrayHelper.To1D(x, y, z);
                return TransparencyLookup[MapData[id + chunkAddress]];
//            }

            return true;
        }

        private static bool IsVoxelInChunk(int x, int y, int z)
        {
            return !(x < 0 || y < 0 || z < 0 || x >= VoxelLookups.CHUNK_SIZE || y >= VoxelLookups.CHUNK_HEIGHT || z >= VoxelLookups.CHUNK_SIZE);
        }
    }
}