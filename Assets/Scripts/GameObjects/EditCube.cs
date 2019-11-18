using System.Collections.Generic;
using MapGeneration.Lookup;
using UnityEngine;
using UnityEngine.Rendering;

namespace MapGeneration
{
    public class EditCube
    {
        public World World => Locator.World;
        public Vector2[,,] UvLookup => Locator.TextureLookup.UvLookup;
        public GameObject GameObject => _gameObject;

        private const int FACES_PER_VERTEX = 6;
        private const int TRIANGLE_VERTICES_PER_FACE = 6;
        private const int VERTICES_PER_FACE = 4;

        private readonly GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        //Chunk Generation
        private int currentVertexIndex;
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();
        private Mesh _mesh;

        public EditCube()
        {
            _gameObject = new GameObject();
            _gameObject.name = $"EditCube";
            _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
            _meshFilter = _gameObject.AddComponent<MeshFilter>();

            _meshRenderer.material = World.PlaceVoxelMaterial;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;

            UpdateChunkMesh();
        }

        public void UpdateChunkMesh()
        {
            currentVertexIndex = 0;
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();

            AddVoxel(3);

            _mesh = new Mesh();
            _mesh.vertices = vertices.ToArray();
            _mesh.triangles = triangles.ToArray();
            _mesh.uv = uvs.ToArray();
            _mesh.RecalculateNormals();

            _meshFilter.mesh = _mesh;
        }

        public void SetVoxelType(int voxelId)
        {
            currentVertexIndex = 0;
            uvs.Clear();
            
            //iterate faces
            for (int iF = 0; iF < FACES_PER_VERTEX; iF++)
            {
                //iterate triangles
                for (int iV = 0; iV < VERTICES_PER_FACE; iV++)
                {
                    uvs.Add(UvLookup[voxelId, iF, iV]);
                }
                
                currentVertexIndex += VERTICES_PER_FACE;
            }
            
            _mesh.uv = uvs.ToArray();
            _meshFilter.mesh = _mesh;
        }

        private void AddVoxel(byte voxelId)
        {
            //iterate faces
            for (int iF = 0; iF < FACES_PER_VERTEX; iF++)
            {
                //iterate triangles
                for (int iV = 0; iV < TRIANGLE_VERTICES_PER_FACE; iV++)
                {
                    var vertexIndex = VoxelLookups.indexToVertex[iV];

                    // each face needs just 4 vertices & UVs
                    if (iV < VERTICES_PER_FACE)
                    {
                        vertices.Add(VoxelLookups.Vertices[VoxelLookups.Triangles[iF, iV]]);
                        uvs.Add(UvLookup[voxelId, iF, iV]);
                    }

                    //we still need 6 triangle vertices tho
                    triangles.Add(currentVertexIndex + vertexIndex);
                }

                currentVertexIndex += VERTICES_PER_FACE;
            }
        }
    }
}