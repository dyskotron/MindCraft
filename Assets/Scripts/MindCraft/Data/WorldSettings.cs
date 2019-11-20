using System;
using DefaultNamespace;
using Framewerk.Managers;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using UnityEngine;
using UnityEngine.Serialization;

namespace MindCraft.Data
{
    public interface IWorldSettings
    {
        int Seed { get; }
        IAssetManager AssetManager { get; set; }
        Material MineMaterial { get; }
        Material GetMaterial(ChunkCoord coords);
        
        PlayerSettings PlayerSettings { get; }
    }

    [Serializable]
    public class PlayerSettings
    {
        public float Radius = 0.3f;
        public float Height = 1.8f;
        
        public float LookSpeed = 3;
        public float WalkSpeed = 3;
        public float RunSpeed = 10;
        public float JumpForce = 5;
    }
    
    public class WorldSettings : IWorldSettings
    {
        [Inject] public IAssetManager AssetManager { get; set; }

        public int Seed { get; private set; }
        public Material MineMaterial { get; private set; }
        public Material PlacingMaterial { get; private set; }

        public PlayerSettings PlayerSettings => _settings.PlayerSettings;

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