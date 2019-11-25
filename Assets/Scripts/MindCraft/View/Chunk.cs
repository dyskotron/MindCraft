using System.Collections.Generic;
using MindCraft.Common;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MindCraft.View
{
    public class Chunk
    {
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public TextureLookup TextureLookup { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }

        public const int FACES_PER_VOXEL = 6;
        private const int TRIANGLE_INDICES_PER_FACE = 6;
        private const int VERTICES_PER_FACE = 4;

        private GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        //Chunk Generation
        private int currentVertexIndex;

        private ChunkCoord _coords;
        //private byte[,,] _map;

        [PostConstruct]
        public void PostConstruct()
        {
            _gameObject = new GameObject();
            _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
            _meshFilter = _gameObject.AddComponent<MeshFilter>();

            _meshRenderer.material = WorldSettings.WorldMaterial;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
        }

        public void Init(ChunkCoord coords)
        {
            _coords = coords;

            _gameObject.name = $"Chunk({coords.X},{coords.Y})";
            _gameObject.transform.position = new Vector3(coords.X * VoxelLookups.CHUNK_SIZE, 0, coords.Y * VoxelLookups.CHUNK_SIZE);

            //Debug _meshRenderer.material = WorldSettings.GetMaterial(coords);
        }

        public bool IsActive
        {
            get { return _gameObject.activeSelf; }
            set { _gameObject.SetActive(value); }
        }

        private byte GetVoxelData(int x, int y, int z)
        {
            //TODO: specific checks for each direction when using by face checks, dont test in rest of cases at all
            if (IsVoxelInChunk(x, y, z))
                return 0;//_map[x, y, z]; //TODO fix this

            return WorldModel.GetVoxel(x + _coords.X * VoxelLookups.CHUNK_SIZE, y, z + _coords.Y * VoxelLookups.CHUNK_SIZE);
        }

        private bool IsVoxelInChunk(int x, int y, int z)
        {
            return !(x < 0 || y < 0 || z < 0 || x >= VoxelLookups.CHUNK_SIZE || y >= VoxelLookups.CHUNK_HEIGHT || z >= VoxelLookups.CHUNK_SIZE);
        }

        #region Mesh Generation
/*
        public void UpdateChunkMesh(NativeArray<byte> map)
        {   
            _map = map;

            currentVertexIndex = 0;
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();

            for (var iX = 0; iX < VoxelLookups.CHUNK_SIZE; iX++)
            {
                for (var iZ = 0; iZ < VoxelLookups.CHUNK_SIZE; iZ++)
                {
                    for (var iY = 0; iY < VoxelLookups.CHUNK_HEIGHT; iY++)
                    {
                        var type = _map[iX, iY, iZ];
                        if (type != BlockTypeByte.AIR)
                            AddVoxel(type, iX, iY, iZ);
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            _meshFilter.mesh = mesh;
        }
        
        private void AddVoxel(byte voxelId, int x, int y, int z)
        {
            var position = new Vector3(x, y, z);
            //iterate faces
            for (int iF = 0; iF < FACES_PER_VOXEL; iF++)
            {
                var neighbour = VoxelLookups.Neighbours[iF];

                //check neighbours
                var blockDef = BlockDefs.GetDefinitionById((BlockTypeId) GetVoxelData(x + neighbour.x, y + neighbour.y, z + neighbour.z));
                if (!blockDef.IsTransparent)
                    continue;

                //iterate triangles
                for (int iV = 0; iV < TRIANGLE_INDICES_PER_FACE; iV++)
                {
                    var vertexIndex = VoxelLookups.indexToVertex[iV];

                    // each face needs just 4 vertices & UVs
                    if (iV < VERTICES_PER_FACE)
                    {
                        vertices.Add(position + VoxelLookups.Vertices[VoxelLookups.Triangles[iF, iV]]);
                        uvs.Add(TextureLookup.WorldUvLookup[voxelId, iF, iV]);
                    }

                    //we still need 6 triangle vertices tho
                    triangles.Add(currentVertexIndex + vertexIndex);
                }

                currentVertexIndex += VERTICES_PER_FACE;
            }
        }*/

        public void UpdateChunkMesh(NativeArray<byte> map)
        { 
            var vertices = new NativeList<float3>(Allocator.Persistent);
            var trinagles = new NativeList<int>(Allocator.Persistent);
            var uvs = new NativeList<float2>(Allocator.Persistent);
            
            var mapJob = CreateRenderChunkJob(map, vertices, trinagles, uvs, TextureLookup.WorldUvLookupNative, BlockDefs.TransparencyLookup);
            mapJob.Complete();
            
            //process job result
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = ToV3Array(vertices);
            mesh.triangles = trinagles.ToArray();
            mesh.uv = ToV2Array(uvs);
            mesh.RecalculateNormals();

            _meshFilter.mesh = mesh;
            
            //dispose
            vertices.Dispose();
            trinagles.Dispose();
            uvs.Dispose();
        }

        private Vector3[] ToV3Array(NativeList<float3> nl)
        {
            var vec = new Vector3[nl.Length]; 
            
            for (var i = 0; i < nl.Length; i++)
            {
                vec[i] = nl[i];
            }

            return vec;
        }

        private Vector2[] ToV2Array(NativeList<float2> nl)
        {
            var vec = new Vector2[nl.Length]; 
            
            for (var i = 0; i < nl.Length; i++)
            {
                vec[i] = nl[i];
            }

            return vec;
        }

        private JobHandle CreateRenderChunkJob(NativeArray<byte> mapData, NativeList<float3> vertices, NativeList<int> triangles, NativeList<float2> uvs, NativeArray<float2> uvLookup, NativeArray<bool> transparencyLookup)
        {
            var job = new RenderChunkMeshJob()
                      {
                          MapData = mapData,
                          Vertices = vertices,
                          Triangles = triangles,
                          Uvs =  uvs,
                          UvLookup = uvLookup,
                          TransparencyLookup = transparencyLookup,
                      };
            
            return job.Schedule();    
        }
        
        [BurstCompile]
        public struct RenderChunkMeshJob : IJob
        {
            [ReadOnly] public NativeArray<byte> MapData;
            [ReadOnly] public NativeArray<float2> UvLookup;
            [ReadOnly] public NativeArray<bool> TransparencyLookup;
            
            [WriteOnly] public NativeList<float3> Vertices;
            [WriteOnly] public NativeList<int> Triangles;
            [WriteOnly] public NativeList<float2> Uvs;

            private int _currentVertexIndex;

            public void Execute()
            {
                for(var index = 0; index < MapData.Length; index++){
                    
                    var voxelId = MapData[index];
                    
                    if(voxelId == BlockTypeByte.AIR)
                        continue;
                    
                    ArrayHelper.To3D(index, out int x, out int y, out int z);


                    //TODO: render shit 
                    var position = new Vector3(x, y, z);
                    //iterate faces
                    for (int iF = 0; iF < FACES_PER_VOXEL; iF++)
                    {
                        //var neighbour = VoxelLookups.Neighbours[iF];

                        //check neighbours
                        var neighbour = VoxelLookups.Neighbours[iF];
                        if (!GetTransparency(x + neighbour.x, y + neighbour.y, z + neighbour.z))
                            continue;

                        //iterate triangles
                        for (int iV = 0; iV < TRIANGLE_INDICES_PER_FACE; iV++)
                        {
                            var vertexIndex = VoxelLookups.indexToVertex[iV];

                            // each face needs just 4 vertices & UVs
                            if (iV < VERTICES_PER_FACE)
                            {
                                Vertices.Add(position + VoxelLookups.Vertices[VoxelLookups.Triangles[iF, iV]]);

                                var uvId = ArrayHelper.To1D(voxelId, iF, iV, TextureLookup.MAX_BLOCKDEF_COUNT, TextureLookup.FACES_PER_VOXEL);
                                Uvs.Add(UvLookup[uvId]);
                            }

                            //we still need 6 triangle vertices tho
                            Triangles.Add(_currentVertexIndex + vertexIndex);
                        }

                        _currentVertexIndex += VERTICES_PER_FACE;
                    }
                }
            }
            
            private bool GetTransparency(int x, int y, int z)
            {
                //TODO: specific checks for each direction when using by face checks, dont test in rest of cases at all
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

        #endregion
    }
}