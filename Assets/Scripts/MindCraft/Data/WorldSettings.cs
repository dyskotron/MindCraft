using System;
using Framewerk.Managers;
using MindCraft.Common;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using UnityEngine;

namespace MindCraft.Data
{
    public interface IWorldSettings
    {
        float Gravity { get; }
        int Seed { get; }
        Material MineMaterial { get; }
        Material BuildMaterial { get; }
        Material WorldMaterial { get; }
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
        public float MiningInterval = 0.4f;
        
        public AnimationCurve PickMovementCurve;
    }
    
    public class WorldSettings : IWorldSettings
    {
        [Inject] public IAssetManager AssetManager { get; set; }

        public float Gravity { get; private set; }
        public int Seed { get; private set; }
        public Material MineMaterial { get; private set; }
        public Material BuildMaterial { get; private set; }
        public Material WorldMaterial => _settings.WorldMaterial;

        public PlayerSettings PlayerSettings => _settings.PlayerSettings;

        private WorldSettingsDef _settings;

        [PostConstruct]
        public void PostConstruct()
        {
            _settings = AssetManager.GetAsset<WorldSettingsDef>(ResourcePath.WORLD_SETTINGS);
            Seed = _settings.Seed;
            MineMaterial = _settings.MineMaterial;
            BuildMaterial = _settings.BuildMaterial;
            Gravity = _settings.Gravity;
        }

        public Material GetMaterial(ChunkCoord coords)
        {
            if(!_settings.DebugChunksMaterialEnabled)
                return _settings.WorldMaterial;

            return (coords.X + coords.Y) % 2 == 0 ? _settings.WorldMaterial : _settings.DebugMaterial;
        }
    }
}