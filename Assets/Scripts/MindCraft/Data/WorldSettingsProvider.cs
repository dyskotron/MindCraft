using DefaultNamespace;
using Framewerk.Managers;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using UnityEngine;

namespace MindCraft.Data
{
    public interface IWorldSettingsProvider
    {
        IAssetManager AssetManager { get; set; }
        Material MineMaterial { get; }
        int Seed { get; }
        Material GetMaterial(ChunkCoord coords);
    }
    
    public class WorldSettingsProvider : IWorldSettingsProvider
    {
        [Inject] public IAssetManager AssetManager { get; set; }

        public Material MineMaterial { get; private set; }
        public Material PlacingMaterial { get; private set; }
        public int Seed { get; private set; }

        private WorldSettingsDef _settings;

        [PostConstruct]
        public void PostConstruct()
        {
            _settings = AssetManager.GetAsset<WorldSettingsDef>(ResourcePath.WORLD_SETTINGS);
            Seed = _settings.Seed;
            MineMaterial = _settings.MineMaterial;
            PlacingMaterial = _settings.PlacingMaterial;
        }

        public Material GetMaterial(ChunkCoord coords)
        {
            if(!_settings.DebugChunksMaterialEnabled)
                return _settings.WorldMaterial;

            return (coords.X + coords.Y) % 2 == 0 ? _settings.WorldMaterial : _settings.DebugMaterial;
        }
    }
}