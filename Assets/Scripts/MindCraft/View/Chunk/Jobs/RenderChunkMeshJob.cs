using MindCraft.Common;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.View.Chunk.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct RenderChunkMeshJob : IJob
    {
        [ReadOnly] public NativeArray<byte> MapData;
        [ReadOnly] public NativeArray<float> LightLevels;

        //Lookups
        [ReadOnly] public NativeArray<float2> UvLookup;
        [ReadOnly] public NativeArray<BlockDefData> BlockDataLookup;
        [ReadOnly] public NativeArray<int3> Neighbours;
        [ReadOnly] public NativeArray<int2x4> LightNeighbours;
        [ReadOnly] public NativeArray<int> IndexToVertex;
        [ReadOnly] public NativeArray<int3> VerticesLookup;
        [ReadOnly] public NativeArray<int> TrianglesLookup;

        [WriteOnly] public NativeList<float3> Vertices;
        [WriteOnly] public NativeList<float3> Normals;
        [WriteOnly] public NativeList<int> Triangles;
        [WriteOnly] public NativeList<float2> Uvs;
        [WriteOnly] public NativeList<float> Colors;

        private int _currentVertexIndex;

        public RenderChunkMeshJob(ComputeMeshData data,
                                  NativeArray<float2> uvLookup,
                                  NativeArray<BlockDefData> blockDataLookup, 
                                  NativeArray<int3> neighbours, 
                                  NativeArray<int2x4> lightNeighbours, 
                                  NativeArray<int> indexToVertex, 
                                  NativeArray<int3> verticesLookup, 
                                  NativeArray<int> trianglesLookup)
        {
            _currentVertexIndex = 0;
            
            MapData = data.MapWithNeighbours;
            LightLevels = data.LightMapWithNeighbours;

            UvLookup = uvLookup;
            BlockDataLookup = blockDataLookup;
            Neighbours = neighbours;
            LightNeighbours = lightNeighbours;
            IndexToVertex = indexToVertex;
            VerticesLookup = verticesLookup;
            TrianglesLookup = trianglesLookup;

            Vertices = data.Vertices;
            Normals = data.Normals;
            Triangles = data.Triangles;
            Uvs = data.Uvs;
            Colors = data.Colors;
        }

        public void Execute()
        {
            //TODO: DATA LOCALITY MESS
            Vertices.Clear();
            Normals.Clear();
            Triangles.Clear();
            Uvs.Clear();
            Colors.Clear();

            //for(var index = 0; index < MapData.Length; index++){
            for (var x = 0; x < GeometryConsts.CHUNK_SIZE; x++)
            {
                for (var z = 0; z < GeometryConsts.CHUNK_SIZE; z++)
                {
                    for (var y = GeometryConsts.CHUNK_HEIGHT - 1; y >= 0; y--)
                    {
                        //we need x, y, z at this point for light
                        //no need to use more expensive ArrayHelper.ToCluster1D as we are going only trough middle chunk
                        var index = ArrayHelper.To1DMap(x, y, z) + GeometryConsts.MULTIMAP_CENTER_OFFSET;

                        var voxelId = MapData[index];

                        if (voxelId == BlockTypeByte.AIR)
                            continue;

                        var position = new int3(x, y, z);

                        //iterate faces
                        for (int iF = 0; iF < GeometryConsts.FACES_PER_VOXEL; iF++)
                        {
                            //check neighbours
                            var neighbourPosRelative = Neighbours[iF];
                            var lightNeighbours = LightNeighbours[iF];

                            var neighbourPosAbsolute = position + neighbourPosRelative;

                            if (!ShouldRenderNeighbour(neighbourPosAbsolute.x, neighbourPosAbsolute.y, neighbourPosAbsolute.z))
                                continue;

                            var neighbourId = ArrayHelper.ToCluster1D(neighbourPosAbsolute.x, neighbourPosAbsolute.y, neighbourPosAbsolute.z);

                            //iterate triangles
                            for (int iV = 0; iV < GeometryConsts.TRIANGLE_INDICES_PER_FACE; iV++)
                            {
                                var vertexIndex = IndexToVertex[iV];

                                // each face needs just 4 vertices & UVs
                                if (iV < GeometryConsts.VERTICES_PER_FACE)
                                {
                                    var vertexLookupIndex = TrianglesLookup[iF * GeometryConsts.VERTICES_PER_FACE + iV];
                                    Vertices.Add(position + VerticesLookup[vertexLookupIndex]);

                                    Normals.Add(neighbourPosRelative);

                                    var uvId = ArrayHelper.To1D(voxelId, iF, iV, TextureLookup.MAX_BLOCKDEF_COUNT, TextureLookup.FACES_PER_VOXEL);
                                    Uvs.Add(UvLookup[uvId]);

                                    //basic light level based on face direct neighbour
                                    var lightLevel = LightLevels[neighbourId];

                                    // uncomment to disable smooth lighting:
                                    // Colors.Add(lightLevel);Triangles.Add(_currentVertexIndex + vertexIndex); continue;

                                    //compute light from vertex adjacent neighbours
                                    int3 diagonal = new int3();

                                    for (var iL = 0; iL < 2; iL++)
                                    {
                                        var lightNeighbour = Neighbours[lightNeighbours[iV][iL]];
                                        var lnAbs = neighbourPosAbsolute + lightNeighbour;

                                        lightLevel += LightLevels[ArrayHelper.ToCluster1D(lnAbs.x, lnAbs.y, lnAbs.z)];
                                        diagonal += lightNeighbour;
                                    }

                                    //+ ugly hardcoded diagonal brick
                                    var diagonalAbs = neighbourPosAbsolute + diagonal;
                                    lightLevel += LightLevels[ArrayHelper.ToCluster1D(diagonalAbs.x, diagonalAbs.y, diagonalAbs.z)];

                                    Colors.Add(math.max(lightLevel * 0.25f, GeometryConsts.MIN_LIGHT)); //multiply instead of divide by 4 as that's faster
                                }

                                //we still need 6 triangle vertices tho
                                Triangles.Add(_currentVertexIndex + vertexIndex);
                            }

                            _currentVertexIndex += GeometryConsts.VERTICES_PER_FACE;
                        }
                    }
                }
            }
        }

        private bool ShouldRenderNeighbour(int x, int y, int z)
        {
            if (y >= GeometryConsts.CHUNK_HEIGHT)
                return true;

            if (y < 0)
                return false;

            var xOffset = (x + GeometryConsts.CHUNK_SIZE) >> GeometryConsts.CHUNK_SIZE_LOG2; // -> / GeometryLookups.CHUNK_SIZE
            var zOffset = (z + GeometryConsts.CHUNK_SIZE) >> GeometryConsts.CHUNK_SIZE_LOG2; // -> / GeometryLookups.CHUNK_SIZE

            //adjust x,z to be always within chunk voxel range
            x = (x + GeometryConsts.CHUNK_SIZE) & GeometryConsts.MODULO_BY_CHUNK_SIZE; // -> % GeometryLookups.CHUNK_SIZE
            z = (z + GeometryConsts.CHUNK_SIZE) & GeometryConsts.MODULO_BY_CHUNK_SIZE; // -> % GeometryLookups.CHUNK_SIZE

            var chunkAddress = (xOffset + zOffset * 3) << GeometryConsts.VOXELS_PER_CHUNK_LOG2; //-> * GeometryLookups.VOXELS_PER_CHUNK

            var id = ArrayHelper.To1DMap(x, y, z);
            return !BlockDataLookup[MapData[id + chunkAddress]].IsSolid;
        }
    }
}