using DefaultNamespace;
using Framewerk.Managers;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using UnityEngine;

namespace MindCraft.Data
{
    public interface IWorldSettings
    {
        int Seed { get; }
        IAssetManager AssetManager { get; set; }
        Material MineMaterial { get; }
        Material GetMaterial(ChunkCoord coords);
        
        float PlayerRadius { get; }
        float PlayerHeight { get; }
    }
    
    public class WorldSettings : IWorldSettings
    {
        [Inject] public IAssetManager AssetManager { get; set; }

        public int Seed { get; private set; }
        public Material MineMaterial { get; private set; }
        public Material PlacingMaterial { get; private set; }
        
        public float PlayerRadius { get; private set; }
        public float PlayerHeight { get; private set; }

        private WorldSettingsDef _settings;

        [PostConstruct]
        public void PostConstruct()
        {
            _settings = AssetManager.GetAsset<WorldSettingsDef>(ResourcePath.WORLD_SETTINGS);
            Seed = _settings.Seed;
            MineMaterial = _settings.MineMaterial;
            PlacingMaterial = _settings.PlacingMaterial;
            PlayerRadius = _settings.PlayerRadius;
            PlayerHeight = _settings.PlayerHeight;
        }

        public Material GetMaterial(ChunkCoord coords)
        {
            if(!_settings.DebugChunksMaterialEnabled)
                return _settings.WorldMaterial;

            return (coords.X + coords.Y) % 2 == 0 ? _settings.WorldMaterial : _settings.DebugMaterial;
        }
    }
}