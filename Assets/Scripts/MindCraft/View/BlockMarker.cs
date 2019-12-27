using System;
using System.Collections.Generic;
using MindCraft.MapGeneration.Utils;
using MindCraft.View.Chunk;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MindCraft.View
{
    public class BlockMarker
    {
        [Inject] public TextureLookup TextureLookup { get; set; }
        [Inject] public GeometryLookups GeometryLookups { get; set; }
        
        public Transform Transform => _transform;
        public GameObject GameObject => _gameObject;

        private const int FACES_PER_VERTEX = 6;
        private const int TRIANGLE_VERTICES_PER_FACE = 6;
        private const int VERTICES_PER_FACE = 4;
        private const int MINE_ANIMATION_FRAMES = 6;

        private GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private float _scale;

        //mesh generation
        private int currentVertexIndex;
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();
        private Mesh _mesh;
        private Transform _transform;

        public void Init(string name, Material material, float scale = 1f)
        {
            _scale = scale;
            _gameObject = new GameObject();
            _gameObject.name = name;
            _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
            _meshFilter = _gameObject.AddComponent<MeshFilter>();

            _meshRenderer.material = material;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;

            _transform = _gameObject.transform;

            CreateMesh();    
        }
        
        public void SetActive(bool active)
        {
            _gameObject.SetActive(active);
        }

        public void SetBlockId(int blockId)
        {
            currentVertexIndex = 0;
            uvs.Clear();
            
            //iterate faces
            for (int iF = 0; iF < FACES_PER_VERTEX; iF++)
            {
                //iterate triangles
                for (int iV = 0; iV < VERTICES_PER_FACE; iV++)
                {
                    uvs.Add(TextureLookup.WorldUvLookup[blockId, iF, iV]);
                }
                
                currentVertexIndex += VERTICES_PER_FACE;
            }
            
            _mesh.uv = uvs.ToArray();
            _meshFilter.mesh = _mesh;
        }
        
        public void SetMiningProgress(float progress) //0 = select >1 = damage ratio
        {
            var textureId = TextureLookup.UtilsTextureIndexes[Mathf.FloorToInt(progress * MINE_ANIMATION_FRAMES)];
            
            currentVertexIndex = 0;
            uvs.Clear();
            
            //iterate faces
            for (int iF = 0; iF < FACES_PER_VERTEX; iF++)
            {
                //iterate triangles
                for (int iV = 0; iV < VERTICES_PER_FACE; iV++)
                {
                    uvs.Add(TextureLookup.UtilsUvLookup[textureId, iV]);
                }
                
                currentVertexIndex += VERTICES_PER_FACE;
            }
            
            _mesh.uv = uvs.ToArray();
            _meshFilter.mesh = _mesh;
        }
        
        private void CreateMesh()
        {
            currentVertexIndex = 0;
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();

            CreateCubeMesh(3);

            _mesh = new Mesh();
            _mesh.vertices = vertices.ToArray();
            _mesh.triangles = triangles.ToArray();
            _mesh.uv = uvs.ToArray();
            _mesh.RecalculateNormals();

            _meshFilter.mesh = _mesh;
        }

        private void CreateCubeMesh(byte voxelId)
        {
            var zeroOffset = -0.5f * (_scale - 1) * Vector3.one;
            
            //iterate faces
            for (int iF = 0; iF < FACES_PER_VERTEX; iF++)
            {
                //iterate triangles
                for (int iV = 0; iV < TRIANGLE_VERTICES_PER_FACE; iV++)
                {
                    var vertexIndex = GeometryLookups.IndexToVertex[iV];

                    // each face needs just 4 vertices & UVs
                    if (iV < VERTICES_PER_FACE)
                    {
                        var vertexLookupIndex = GeometryLookups.TrianglesLookup[iF * GeometryLookups.VERTICES_PER_FACE + iV];
                        vertices.Add((float3) zeroOffset + GeometryLookups.VerticesLookup[vertexLookupIndex]);
                        uvs.Add(TextureLookup.WorldUvLookup[voxelId, iF, iV]);
                    }

                    //we still need 6 triangle vertices tho
                    triangles.Add(currentVertexIndex + vertexIndex);
                }

                currentVertexIndex += VERTICES_PER_FACE;
            }
        }
    }
}