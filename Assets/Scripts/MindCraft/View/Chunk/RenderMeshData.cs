using MindCraft.Data;
using MindCraft.MapGeneration.Utils;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MindCraft.View.Chunk
{
    public class RenderMeshData
    {
        public int2 Coords { get; private set; }

        public Mesh Mesh { get; }
        //public Matrix4x4 Transform { get; private set; }

        private GameObject _gameObject;
        public RenderMeshData(Material material)
        {
            Mesh = new Mesh {indexFormat = IndexFormat.UInt32};
            
            //mesh
            _gameObject = new GameObject();
            var meshRenderer = _gameObject.AddComponent<MeshRenderer>();

            meshRenderer.material = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
                
            var meshFilter = _gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = Mesh;
        }

        public void SetCoords(int2 coords)
        {
            Coords = coords;
            
            var position = new Vector3(coords.x * GeometryLookups.CHUNK_SIZE, 0, coords.y * GeometryLookups.CHUNK_SIZE);
            _gameObject.transform.position = position;
        }
    }
}