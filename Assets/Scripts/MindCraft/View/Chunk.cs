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
using UnityEngine.Timeline;

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

            _map = new NativeArray<byte>(map, Allocator.Persistent);
            _lightLevels = new NativeArray<float>(map.Length, Allocator.Persistent);
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
                          LitVoxels = _litVoxels,
                          UvLookup = TextureLookup.WorldUvLookupNative,
                          TransparencyLookup = BlockDefs.TransparencyLookup,
                          Debug = _debug,
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

            Debug.LogWarning($"<color=\"aqua\">Chunk.ProcessJobResult() : LightVertexesCounted:{_debug[0]}</color>");
            
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
                            for (int iF = 0; iF < FACES_PER_VOXEL; iF++)
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
                                for (int iV = 0; iV < TRIANGLE_INDICES_PER_FACE; iV++)
                                {
                                    var vertexIndex = VoxelLookups.indexToVertex[iV];

                                    // each face needs just 4 vertices & UVs
                                    if (iV < VERTICES_PER_FACE)
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

                                _currentVertexIndex += VERTICES_PER_FACE;
                            }
                        }
                    }
                }
            }

            private float GetVertexNeighbourLightLevel(int x , int y, int z)
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

        #endregion
    }
}