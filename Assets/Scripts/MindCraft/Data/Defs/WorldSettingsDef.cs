using UnityEngine;

namespace MindCraft.Data.Defs
{
    [CreateAssetMenu(menuName = "Defs/World Settings")]
    public class WorldSettingsDef : ScriptableObject
    {
        public int Seed;
        public Material WorldMaterial;
        public Material MineMaterial;
        public Material PlacingMaterial;

        [Header("Player")] 
        public float PlayerRadius = 0.3f;
        public float PlayerHeight = 1f;

        [Header("Debug Params")] 
        public bool DebugChunksMaterialEnabled;
        public Material DebugMaterial;
    }
}