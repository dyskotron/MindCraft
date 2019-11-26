using UnityEngine;
using UnityEngine.Serialization;

namespace MindCraft.Data.Defs
{
    [CreateAssetMenu(menuName = "Defs/World Settings")]
    public class WorldSettingsDef : ScriptableObject
    {
        public float Gravity = -9.8f;
        public int Seed;
        public Material WorldMaterial;
        public Material MineMaterial;
        public Material BuildMaterial;
        
        public BiomeDef DefaultBiome;

        public PlayerSettings PlayerSettings;

        [Header("Debug Params")] 
        public bool DebugChunksMaterialEnabled;
        public Material DebugMaterial;
        
        
    }
}