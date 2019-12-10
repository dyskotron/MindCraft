using UnityEngine;

namespace MindCraft.Data.Defs
{
    [CreateAssetMenu(menuName = "Defs/Block definition")]
    public class BlockDef : DefinitionSO<BlockTypeId>
    {
        public string Name;
        public bool IsSolid;
        public float LightModification;
        public int Hardness; // 0 means undestructible

        [Header("Textures")] 
        [Tooltip("Back, Front, Top, Bottom, Left, Right")] 
        public byte[] FaceTextures = new byte[6];

        public BlockDefData GetBlockData()
        {
            return new BlockDefData(IsSolid, LightModification);
        }
    }

    public struct BlockDefData
    {
        public readonly bool IsSolid;
        public readonly float LightModification;

        public BlockDefData(bool isSolid, float lightModification)
        {
            IsSolid = isSolid;
            LightModification = lightModification;
        }
    }
}