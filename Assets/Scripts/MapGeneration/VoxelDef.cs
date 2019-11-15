using UnityEngine;
using UnityEngine.Serialization;

namespace MapGeneration
{
    [CreateAssetMenu(menuName = "MapGeneration/Voxel definition")]
    public class VoxelDef : ScriptableObject
    {
        public string Name;
        public bool IsSolid;

        [Header("Textures")] 
        [Tooltip("Back, Front, Top, Bottom, Left, Right")] 
        public byte[] FaceTextures = new byte[6];
    }
}