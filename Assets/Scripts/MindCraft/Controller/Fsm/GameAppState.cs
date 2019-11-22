using System.Diagnostics;
using Framewerk;
using Framewerk.AppStateMachine;
using Framewerk.Managers;
using MindCraft.Common;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Lookup;
using MindCraft.Model;
using MindCraft.View;
using MindCraft.View.Screen;
using Temari.Common;
using UnityEngine;

namespace MindCraft.Controller.Fsm
{
    public class GameAppState : AppState<GameAppScreen>
    {
        public static float GENERATION_TIME_TOTAL = 0;
        
        [Inject] public IUpdater Updater { get; set; }
        [Inject] public IAssetManager AssetManager { get; set; }
        [Inject] public ViewConfig ViewConfig { get; set; }
        
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public ChunksRenderer ChunksRenderer { get; set; }

        private PlayerView _playerView;
        private ChunkCoord _lastPlayerCoords;
        
        protected override void Enter()
        {
            base.Enter();
            
            Random.InitState(WorldSettings.Seed);
            Cursor.lockState = CursorLockMode.Locked;
            
            //create player
            _playerView = AssetManager.GetGameObject<PlayerView>(ResourcePath.PLAYER_PREFAB);
            _playerView.transform.position = new Vector3(0f, WorldModel.GetTerrainHeight(0,0) + 5, 0f);
            
            //Generate World
            var watch = new Stopwatch();
            watch.Start();
            GenerateWorld(new ChunkCoord());
            watch.Stop();

            GENERATION_TIME_TOTAL = (float)watch.Elapsed.TotalSeconds;
            
            var camera = ViewConfig.Camera3d;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = VoxelLookups.VIEW_DISTANCE;
            
            Updater.EveryFrame(UpdateView);
        }

        private void UpdateView()
        {
            var newCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(_playerView.transform.position);
            if (newCoords == _lastPlayerCoords)
                return;

            WorldModel.GenerateWorldAroundPlayer(newCoords);
            ChunksRenderer.UpdateChunksAroundPlayer(newCoords);

            _lastPlayerCoords = newCoords;
        }
        
        private void GenerateWorld(ChunkCoord playerPosition)
        {
            WorldModel.GenerateWorldAroundPlayer(playerPosition);
            ChunksRenderer.RenderChunksAroundPlayer(playerPosition);
            _lastPlayerCoords = playerPosition;
        }
    }
}