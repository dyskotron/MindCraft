using UnityEngine;

namespace MindCraft.Data.Defs
{
    [CreateAssetMenu(menuName = "Defs/Block definition")]
    public class BlockDef : DefinitionSO<BlockTypeId>
    {
        public string Name;
        public bool IsSolid;
        public int Hardness; // 0 means undestructible

        [Header("Textures")] 
        [Tooltip("Back, Front, Top, Bottom, Left, Right")] 
        public byte[] FaceTextures = new byte[6];
    }
}