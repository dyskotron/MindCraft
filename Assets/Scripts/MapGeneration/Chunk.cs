using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    public class Chunk
    {
        public const int EMPTY_VOXEL = 0;
        private const int FACES_PER_VERTEX = 6;
        private const int TRIANGLE_VERTICES_PER_FACE = 6;
        private const int VERTICES_PER_FACE = 4;

        //texture consts
        private const int BLOCKS_PER_TEXTURE = 4;
        private const float NORMALIZED_BLOCK_SIZE = 1f / BLOCKS_PER_TEXTURE;

        private const float TEXTURE_CORDS_FIX = 0.001f;
        private static readonly Vector2 CORDS_FIX_OFFSET = new Vector2(TEXTURE_CORDS_FIX, TEXTURE_CORDS_FIX);
        private const float FIXED_BLOCK_SIZE = NORMALIZED_BLOCK_SIZE - 2 * TEXTURE_CORDS_FIX;

        private readonly GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        //Chunk Generation
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector2> uvs;
        private byte[,,] voxelMap;

        private ChunkCoord _coords;
        private Vector3 _position;

        private int currentVertexIndex;

        public Chunk(ChunkCoord coords)
        {
            _coords = coords;
            _position = new Vector3(coords.X * VoxelLookups.CHUNK_SIZE, 0, coords.Y * VoxelLookups.CHUNK_SIZE);

            _gameObject = new GameObject();
            _gameObject.name = $"Chunk({coords.X},{coords.Y})";
            _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
            _meshFilter = _gameObject.AddComponent<MeshFilter>();
            _gameObject.transform.position = new Vector3(coords.X * VoxelLookups.CHUNK_SIZE, 0, coords.Y * VoxelLookups.CHUNK_SIZE);

            _meshRenderer.material = Locator.World.Material;

            CreateMap();
            _meshFilter.mesh = CreateMesh();
        }

        public bool IsActive
        {
            get { return _gameObject.activeSelf; }
            set { _gameObject.SetActive(value); }
        }

        private void CreateMap()
        {
            voxelMap = new byte[VoxelLookups.CHUNK_SIZE, VoxelLookups.CHUNK_HEIGHT, VoxelLookups.CHUNK_SIZE];

            for (var iX = 0; iX < VoxelLookups.CHUNK_SIZE; iX++)
            {
                for (var iZ = 0; iZ < VoxelLookups.CHUNK_SIZE; iZ++)
                {
                    for (var iY = 0; iY < VoxelLookups.CHUNK_HEIGHT; iY++)
                    {
                        voxelMap[iX, iY, iZ] = Locator.World.GetVoxel(new Vector3(iX, iY, iZ) + _position);
                    }
                }
            }
        }

        private Mesh CreateMesh()
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();
            uvs = new List<Vector2>();

            currentVertexIndex = 0;

            for (var iX = 0; iX < VoxelLookups.CHUNK_SIZE; iX++)
            {
                for (var iZ = 0; iZ < VoxelLookups.CHUNK_SIZE; iZ++)
                {
                    for (var iY = 0; iY < VoxelLookups.CHUNK_HEIGHT; iY++)
                    {
                        if (GetVoxelData(new Vector3(iX, iY, iZ)) != EMPTY_VOXEL)
                            AddVoxel(new Vector3(iX, iY, iZ));
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            return mesh;
        }

        private void AddVoxel(Vector3 position)
        {
            var voxelId = GetVoxelData(position);
            var voxelType = Locator.World.voxelDefs[voxelId];

            //iterate faces
            for (int iF = 0; iF < FACES_PER_VERTEX; iF++)
            {
                //check neighbours
                if (GetVoxelData(position + VoxelLookups.Neighbours[iF]) != EMPTY_VOXEL)
                    continue;

                //iterate triangles
                for (int iV = 0; iV < TRIANGLE_VERTICES_PER_FACE; iV++)
                {
                    var vertexIndex = VoxelLookups.indexToVertex[iV];

                    // each face needs just 4 vertices & UVs
                    if (iV < VERTICES_PER_FACE)
                    {
                        vertices.Add(position + VoxelLookups.Vertices[VoxelLookups.Triangles[iF, iV]]);
                    }

                    //we still need 6 triangle vertices tho
                    triangles.Add(currentVertexIndex + vertexIndex);
                }

                AddTexture(voxelType.FaceTextures[iF]);
                currentVertexIndex += VERTICES_PER_FACE;
            }
        }

        private void AddTexture(int textureId)
        {
            float x = (textureId % BLOCKS_PER_TEXTURE) * NORMALIZED_BLOCK_SIZE;
            float y = (int) (textureId / BLOCKS_PER_TEXTURE) * NORMALIZED_BLOCK_SIZE;

            uvs.Add(CORDS_FIX_OFFSET + new Vector2(x, y));
            uvs.Add(CORDS_FIX_OFFSET + new Vector2(x, y + FIXED_BLOCK_SIZE));
            uvs.Add(CORDS_FIX_OFFSET + new Vector2(x + FIXED_BLOCK_SIZE, y));
            uvs.Add(CORDS_FIX_OFFSET + new Vector2(x + FIXED_BLOCK_SIZE, y + FIXED_BLOCK_SIZE));
        }

        private byte GetVoxelData(Vector3 position)
        {
            var x = Mathf.FloorToInt(position.x);
            var y = Mathf.FloorToInt(position.y);
            var z = Mathf.FloorToInt(position.z);

            //TODO: specific checks for each direction, so we save useless checks
            if (!IsVoxelInChunk(x, y, z))
                return Locator.World.GetVoxel(position + _position);

            return voxelMap[x, y, z];
        }

        private bool IsVoxelInChunk(int x, int y, int z)
        {
            return !(x < 0 || y < 0 || z < 0 || x >= VoxelLookups.CHUNK_SIZE || y >= VoxelLookups.CHUNK_HEIGHT || z >= VoxelLookups.CHUNK_SIZE);
        }
    }
}