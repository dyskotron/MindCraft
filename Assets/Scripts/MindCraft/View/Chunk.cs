using System.Collections;
using Framewerk.Managers;
using MindCraft.Common;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
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
        [Inject] public ICoroutineManager CoroutineManager { get; set; }
        [Inject] public TextureLookup TextureLookup { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }

        public const int FACES_PER_VOXEL = 6;
        private const int TRIANGLE_INDICES_PER_FACE = 6;
        private const int VERTICES_PER_FACE = 4;

        public bool IsRendering { get; private set; }

        private GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        //Chunk Generation
        private int currentVertexIndex;

        private ChunkCoord _coords;

        private NativeList<float3> _vertices;
        private NativeList<int> _triangles;
        private NativeList<float2> _uvs;
        private NativeList<float> _colors;

        private NativeArray<byte> _map;

        private JobHandle _jobHandle;

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


        #region Mesh Generation

        public void UpdateChunkMesh(NativeArray<byte> map)
        {
            if (IsRendering)
            {
                Debug.LogError($"<color=\"aqua\">Chunk.UpdateChunkMesh() : CHUNK ALREADY RENDERING!!!!</color>");
                return;
            }

            IsRendering = true;

            _map = new NativeArray<byte>(map, Allocator.Persistent);
            _vertices = new NativeList<float3>(Allocator.Persistent);
            _triangles = new NativeList<int>(Allocator.Persistent);
            _uvs = new NativeList<float2>(Allocator.Persistent);
            _colors = new NativeList<float>(Allocator.Persistent);

            var job = new RenderChunkMeshJob()
                      {
                          MapData = _map,
                          Vertices = _vertices,
                          Triangles = _triangles,
                          Uvs = _uvs,
                          Colors = _colors,
                          UvLookup = TextureLookup.WorldUvLookupNative,
                          TransparencyLookup = BlockDefs.TransparencyLookup,
                      };

            _jobHandle = job.Schedule();

            CoroutineManager.RunCoroutine(CheckRenderJobCoroutine());
        }

        private IEnumerator CheckRenderJobCoroutine()
        {
            if (!_jobHandle.IsCompleted)
                yield return null;

            ProcessJobResult();
        }

        private void ProcessJobResult()
        {
            _jobHandle.Complete();

            //process job result
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = ToV3Array(_vertices);
            mesh.triangles = _triangles.ToArray();
            mesh.uv = ToV2Array(_uvs);
            mesh.colors = ToColorArray(_colors);
            mesh.RecalculateNormals();

            _meshFilter.mesh = mesh;

            //dispose
            _vertices.Dispose();
            _triangles.Dispose();
            _uvs.Dispose();
            _colors.Dispose();
            _map.Dispose();

            IsRendering = false;
        }

        private Vector3[] ToV3Array(NativeList<float3> nl)
        {
            var vectors = new Vector3[nl.Length];

            for (var i = 0; i < nl.Length; i++)
            {
                vectors[i] = nl[i];
            }

            return vectors;
        }

        private Color[] ToColorArray(NativeList<float> nl)
        {
            var colors = new Color[nl.Length];

            for (var i = 0; i < nl.Length; i++)
            {
                colors[i] = new Color(0, 0, 0, nl[i]);
            }

            return colors;
        }

        private Vector2[] ToV2Array(NativeList<float2> nl)
        {
            var vectors = new Vector2[nl.Length];

            for (var i = 0; i < nl.Length; i++)
            {
                vectors[i] = nl[i];
            }

            return vectors;
        }

        public struct RenderChunkMeshJob : IJob
        {
            [ReadOnly] public NativeArray<byte> MapData;
            [ReadOnly] public NativeArray<float2> UvLookup;
            [ReadOnly] public NativeArray<bool> TransparencyLookup;

            [WriteOnly] public NativeList<float3> Vertices;
            [WriteOnly] public NativeList<int> Triangles;
            [WriteOnly] public NativeList<float2> Uvs;
            [WriteOnly] public NativeList<float> Colors;
            
            //[WriteOnly] public NativeArray<float> LightLevels;

            private int _currentVertexIndex;

            public void Execute()
            {
                //LightLevels = new NativeArray<float>(MapData.Length, Allocator.TempJob);

                //CalculateLight();
                
                float lightLevel = 1f;

                //for(var index = 0; index < MapData.Length; index++){
                for (var x = 0; x < VoxelLookups.CHUNK_SIZE; x++)
                {
                    for (var z = 0; z < VoxelLookups.CHUNK_SIZE; z++)
                    {
                        for (var y = VoxelLookups.CHUNK_HEIGHT - 1; y >= 0; y--)
                        {
                            //we need x, y, z at this point for light
                            var index = ArrayHelper.To1D(x, y, z);

                            if (y == VoxelLookups.CHUNK_HEIGHT - 1)
                                lightLevel = 1f;

                            var voxelId = MapData[index];

                            if (voxelId == BlockTypeByte.AIR)
                                continue;

                            lightLevel -= (1 / 32f);

                            var position = new Vector3(x, y, z);

                            //iterate faces
                            for (int iF = 0; iF < FACES_PER_VOXEL; iF++)
                            {
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

                                        Colors.Add(lightLevel);
                                    }

                                    //we still need 6 triangle vertices tho
                                    Triangles.Add(_currentVertexIndex + vertexIndex);
                                }

                                _currentVertexIndex += VERTICES_PER_FACE;
                            }
                        }
                    }
                }
                
                //LightLevels.Dispose();
            }
/*
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

                            var voxelId = MapData[index];
                            if (voxelId != BlockTypeByte.AIR)
                                lightLevel = TransparencyLookup[MapData[index]] ? 0.7f : 0.25f;

                            //LightLevels[index] = lightLevel;
                            LightLevels[index] = lightLevel;
                        }
                    }
                }
            }*/

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

        #endregion
    }
}