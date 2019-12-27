using System.Collections;
using Framewerk.Managers;
using MindCraft.Common;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.View.Chunk.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace MindCraft.View.Chunk
{
    public class ChunkView
    {
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public ICoroutineManager CoroutineManager { get; set; }
        [Inject] public TextureLookup TextureLookup { get; set; }
        [Inject] public GeometryLookups GeometryLookups { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }

        public bool IsRendering { get; private set; }

        //GameObject / components
        private GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        
        //Jobs input 
        private NativeArray<byte> _map;
        private NativeArray<float> _lights;
        private NativeQueue<int3> _litVoxels;
        
        //Jobs output (mesh data)
        private NativeList<float3> _vertices;
        private NativeList<float3> _normals;
        private NativeList<int> _triangles;
        private NativeList<float2> _uvs;
        private NativeList<float> _colors;

        private RenderChunkMeshJob _meshJob;
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

        public void Init(int2 coords)
        {
            _gameObject.name = $"Chunk({coords.x},{coords.y})";
            _gameObject.transform.position = new Vector3(coords.x * GeometryLookups.CHUNK_SIZE, 0, coords.y * GeometryLookups.CHUNK_SIZE);

            //Debug _meshRenderer.material = WorldSettings.GetMaterial(coords);
        }

        public bool IsActive
        {
            get { return _gameObject.activeSelf; }
            set { _gameObject.SetActive(value); }
        }

        #region Mesh Generation

        public void UpdateChunkMesh(NativeArray<byte> map, NativeArray<float> lights)
        {
            if (IsRendering)
            {
                Debug.LogError($"<color=\"aqua\">Chunk.UpdateChunkMesh() : CHUNK ALREADY RENDERING!!!!</color>");
                return;
            }

            IsRendering = true;

            _map = map;
            _lights = lights;
            
            _litVoxels = new NativeQueue<int3>(Allocator.Persistent);
            
            _vertices = new NativeList<float3>(Allocator.Persistent);
            _normals = new NativeList<float3>(Allocator.Persistent);
            _triangles = new NativeList<int>(Allocator.Persistent);
            _uvs = new NativeList<float2>(Allocator.Persistent);
            _colors = new NativeList<float>(Allocator.Persistent);

            _meshJob = new RenderChunkMeshJob
                      {
                          MapData = _map,
                          LightLevels = _lights,
                          UvLookup = TextureLookup.WorldUvLookupNative,
                          BlockDataLookup = BlockDefs.BlockDataLookup,
                          
                          Neighbours = GeometryLookups.Neighbours,
                          LightNeighbours = GeometryLookups.LightNeighbours,
                          IndexToVertex = GeometryLookups.IndexToVertex,
                          VerticesLookup = GeometryLookups.VerticesLookup,
                          TrianglesLookup = GeometryLookups.TrianglesLookup,
                          
                          Vertices = _vertices,
                          Normals = _normals,
                          Triangles = _triangles,
                          Uvs = _uvs,
                          Colors = _colors,
                      };

            _jobHandle = _meshJob.Schedule();
            
            //Uncomment to finish job in same callstack
            //ProcessJobResult(); return;

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
            mesh.vertices = NativeArrayUtil.NativeFloat3ToManagedVector3(_vertices);
            mesh.triangles = _triangles.ToArray();
            mesh.uv = NativeArrayUtil.NativeFloat2ToManagedVector2(_uvs);
            mesh.colors = NativeArrayUtil.NativeFloatToManagedColor(_colors);
            mesh.normals = NativeArrayUtil.NativeFloat3ToManagedVector3(_normals);
            _meshFilter.mesh = mesh;

            //dispose
            _map.Dispose();
            _lights.Dispose();
            _litVoxels.Dispose();
            _vertices.Dispose();
            _normals.Dispose();
            _triangles.Dispose();
            _uvs.Dispose();
            _colors.Dispose();

            IsRendering = false;
        }
        
        #endregion
    }
}