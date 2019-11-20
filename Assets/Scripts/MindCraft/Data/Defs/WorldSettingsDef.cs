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

        public PlayerSettings PlayerSettings;

        [Header("Debug Params")] 
        public bool DebugChunksMaterialEnabled;
        public Material DebugMaterial;
    }
}