using System.Collections.Generic;
using System.Diagnostics;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace MindCraft.View
{
    public class Chunk
    {
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public TextureLookup TextureLookup { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }

        public static double MAP_ELAPSED_TOTAL = 0;
        public static double MESH_ELAPSED_TOTAL = 0;
        public static double CHUNKS_TOTAL = 0;

        private const int VERTEX_LIMIT = 65534;
        
        private const int FACES_PER_VOXEL = 6;
        private const int TRIANGLE_INDICES_PER_FACE = 6;
        private const int VERTICES_PER_FACE = 4;

        private GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        //Chunk Generation
        private int currentVertexIndex;
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();

        private ChunkCoord _coords;
        private byte[,,] _map;

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
                return _map[x, y, z];

            return WorldModel.GetVoxel(x + _coords.X * VoxelLookups.CHUNK_SIZE, y, z + _coords.Y * VoxelLookups.CHUNK_SIZE);
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
        }

        private bool IsVoxelInChunk(int x, int y, int z)
        {
            return !(x < 0 || y < 0 || z < 0 || x >= VoxelLookups.CHUNK_SIZE || y >= VoxelLookups.CHUNK_HEIGHT || z >= VoxelLookups.CHUNK_SIZE);
        }

        #region Mesh Generation

        public void UpdateChunkMesh(byte[,,] map)
        {
            _map = map;

            var meshWatch = new Stopwatch();
            meshWatch.Start();

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

            meshWatch.Stop();
            MESH_ELAPSED_TOTAL += meshWatch.Elapsed.TotalSeconds;
            CHUNKS_TOTAL++;
        }

        #endregion
    }
}