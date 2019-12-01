using System.Collections;
using Framewerk.Managers;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MindCraft.View.Chunk
{
    public class ChunkView
    {
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public ICoroutineManager CoroutineManager { get; set; }
        [Inject] public TextureLookup TextureLookup { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }

        public const int FACES_PER_VOXEL = 6;
        public const int TRIANGLE_INDICES_PER_FACE = 6;
        public const int VERTICES_PER_FACE = 4;

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
        private NativeArray<float> _lightLevels;
        private NativeQueue<int3> _litVoxels;
        private NativeArray<int> _debug;

        private JobHandle _jobHandle;
        private RenderChunkMeshJob _job;

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

            _map = map;//new NativeHashMap<int2, NativeArray<byte>>(9, Allocator.Persistent);
            
            //copy here?
            _lightLevels = new NativeArray<float>(VoxelLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
            
            //populate with all voxels lit for noew
            for (var i = 0; i < _lightLevels.Length; i++)
            {
                _lightLevels[i] = 1f;
            }
            _litVoxels = new NativeQueue<int3>(Allocator.Persistent);
            
            _vertices = new NativeList<float3>(Allocator.Persistent);
            _triangles = new NativeList<int>(Allocator.Persistent);
            _uvs = new NativeList<float2>(Allocator.Persistent);
            _colors = new NativeList<float>(Allocator.Persistent);
            _debug = new NativeArray<int>(1, Allocator.Persistent);

            _job = new RenderChunkMeshJob()
                      {
                          MapData = _map,
                          Vertices = _vertices,
                          Triangles = _triangles,
                          Uvs = _uvs,
                          Colors = _colors,
                          LightLevels = _lightLevels,
                          UvLookup = TextureLookup.WorldUvLookupNative,
                          TransparencyLookup = BlockDefs.TransparencyLookup,
                      };

            _jobHandle = _job.Schedule();

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
            _lightLevels.Dispose();
            _litVoxels.Dispose();
            _debug.Dispose();

            IsRendering = false;
        }
        
        #endregion


        #region Native collection convesion helpers

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

        

        #endregion
    }
}