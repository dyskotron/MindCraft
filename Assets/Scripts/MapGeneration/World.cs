using UnityEngine;
using UnityEngine.Serialization;

namespace MapGeneration
{
    public class World : MonoBehaviour
    {
        public Material Material;
        [FormerlySerializedAs("VoxelTypes")] public VoxelDef[] voxelDefs;
        public GameObject ChunkPrefab;
        public int HalfWorldSize = 7;

        private void Start()
        {
            Locator.World = this;
            
            //create chunks
            for (var x = -HalfWorldSize; x < HalfWorldSize; x++)
            {
                for (var y = -HalfWorldSize; y < HalfWorldSize; y++)
                {
                    Instantiate(ChunkPrefab, new Vector3(x * VoxelLookups.CHUNK_SIZE, 0, y * VoxelLookups.CHUNK_SIZE), Quaternion.identity);
                }   
            }
        }
    }
}