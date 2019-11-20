using Framewerk.Managers;
using MindCraft.MapGeneration;
using UnityEngine;

namespace MindCraft.Data.Defs
{
    [CreateAssetMenu(menuName = "Defs/World Settings")]
    public class WorldSettingsDef : ScriptableObject
    {
        public Material WorldMaterial;
        public Material MineMaterial;
        public Material PlacingMaterial;
        public int Seed;

        [Header("Debug Params")] public bool DebugChunksMaterialEnabled;
        public Material DebugMaterial;
    }
}