using MindCraft.Common;
using MindCraft.Data;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.View.Chunk.Jobs
{
    public struct OldRenderChunkMeshJob : IJob
    {
        [ReadOnly] public NativeArray<byte> MapData;
        [ReadOnly] public NativeArray<float2> UvLookup;
        [ReadOnly] public NativeArray<bool> TransparencyLookup;

        [WriteOnly] public NativeList<float3> Vertices;
        [WriteOnly] public NativeList<int> Triangles;
        [WriteOnly] public NativeList<float2> Uvs;
        [WriteOnly] public NativeList<float> Colors;
        
        public NativeQueue<int3> LitVoxels;

        public NativeArray<float> LightLevels;

        public NativeArray<int> Debug;

        public int _currentVertexIndex;

        public void Execute()
        {
            CalculateLight();

            //for(var index = 0; index < MapData.Length; index++){
            for (var x = 0; x < VoxelLookups.CHUNK_SIZE; x++)
            {
                for (var z = 0; z < VoxelLookups.CHUNK_SIZE; z++)
                {
                    for (var y = VoxelLookups.CHUNK_HEIGHT - 1; y >= 0; y--)
                    {
                        //we need x, y, z at this point for light
                        var index = ArrayHelper.To1D(x, y, z);

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

                            var neighbourId = ArrayHelper.To1D(nX, nY, nZ);

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

                                    //TODO: get neighbours to job properly
                                    if (IsVoxelInChunk(nX, nY, nZ))
                                    {
                                        //basic light level based on face direct neighbour
                                        var lightLevel = LightLevels[neighbourId];


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
                                    }
                                    else
                                        Colors.Add(1);
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

        private float GetVertexNeighbourLightLevel(int x, int y, int z)
        {
            if (IsVoxelInChunk(x, y, z))
            {
                //consider adding neighbour light only as 0.5 weight compared to main source
                var lightNeighbourId = ArrayHelper.To1D(x, y, z);
                return LightLevels[lightNeighbourId];
            }

            return 1;
        }

        private void CalculateLight()
        {
            float lightLevel = 1f;

            for (var x = 0; x < VoxelLookups.CHUNK_SIZE; x++)
            {
                for (var z = 0; z < VoxelLookups.CHUNK_SIZE; z++)
                {
                    lightLevel = 1f;

                    for (var y = VoxelLookups.CHUNK_HEIGHT - 1; y >= 0; y--)
                    {
                        var index = ArrayHelper.To1D(x, y, z);
                        LightLevels[index] = lightLevel;

                        var voxelId = MapData[index];

                        //basically air has transparency 1 so we're keeping last value
                        if (voxelId != BlockTypeByte.AIR)
                            lightLevel = Mathf.Min(TransparencyLookup[voxelId] ? 0.7f : 0.25f, lightLevel);

                        if (lightLevel > VoxelLookups.LIGHT_FALL_OFF)
                            LitVoxels.Enqueue(new int3(x, y, z));

                        LightLevels[index] = lightLevel;
                    }
                }
            }

            //iterate trough lit voxels and project light to neighbours
            /*
            while (LitVoxels.Count > 0)
            {
                Debug[0] = Debug[0] + 1;
                
                var litVoxel = LitVoxels.Dequeue();
                var litVoxelId = ArrayHelper.To1D(litVoxel.x, litVoxel.y, litVoxel.z);
                var litVoxelFalloff = LightLevels[litVoxelId] - VoxelLookups.LIGHT_FALL_OFF;
                
                //iterate trough neighbours
                for (int iF = 0; iF < FACES_PER_VOXEL; iF++)
                {
                    var neighbour = litVoxel + VoxelLookups.NeighboursInt3[iF];

                    if (IsVoxelInChunk(neighbour.x, neighbour.y, neighbour.z))
                    {
                        var neighbourId = ArrayHelper.To1D(neighbour.x, neighbour.y, neighbour.z);
                        if(LightLevels[neighbourId] < litVoxelFalloff)
                        {
                            LightLevels[neighbourId] = litVoxelFalloff;
                            if(litVoxelFalloff > VoxelLookups.LIGHT_FALL_OFF)
                                LitVoxels.Enqueue(neighbour);
                        }
                    }
                }      
            }
            */
        }

        private bool GetTransparency(int x, int y, int z)
        {
            //TODO: specific checks for each direction when using by face checks, don't test in rest of cases at all
            if (IsVoxelInChunk(x, y, z))
            {
                var id = ArrayHelper.To1D(x, y, z);
                return TransparencyLookup[MapData[id]];
            }

            return true;
        }

        private static bool IsVoxelInChunk(int x, int y, int z)
        {
            return !(x < 0 || y < 0 || z < 0 || x >= VoxelLookups.CHUNK_SIZE || y >= VoxelLookups.CHUNK_HEIGHT || z >= VoxelLookups.CHUNK_SIZE);
        }
    }
}