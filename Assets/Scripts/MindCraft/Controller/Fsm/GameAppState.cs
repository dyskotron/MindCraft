using System.Diagnostics;
using Framewerk;
using Framewerk.AppStateMachine;
using Framewerk.Managers;
using MindCraft.Common;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using MindCraft.View;
using MindCraft.View.Chunk;
using MindCraft.View.Screen;
using Temari.Common;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

            //create player
            _playerView = AssetManager.GetGameObject<PlayerView>(ResourcePath.PLAYER_PREFAB);
            _playerView.transform.position = new Vector3(0f, WorldModel.GetTerrainHeight(0, 0) + 5, 0f);

            //Generate World
            var watch = new Stopwatch();
            watch.Start();
            GenerateWorld(new ChunkCoord());
            watch.Stop();

            GENERATION_TIME_TOTAL = (float) watch.Elapsed.TotalSeconds;

            var camera = ViewConfig.Camera3d;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = VoxelLookups.VIEW_DISTANCE;

            Updater.EveryFrame(UpdateView);
        }

        private void GenerateWorld(ChunkCoord playerPosition)
        {
            WorldModel.GenerateWorldAroundPlayer(playerPosition);
            ChunksRenderer.GenerateChunksAroundPlayer(playerPosition);
            
            _lastPlayerCoords = playerPosition;
        }

        private void UpdateView()
        {
            var newCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(_playerView.transform.position);
            if (newCoords == _lastPlayerCoords)
                return;

            //TODO: check new / old coords distance and generate full map around player when difference is bigger than 1 chunk
            //Useful later on for teleporting or any other transport that is fast enough that player can move several chunks within frame
            
            var watch = new Stopwatch();
            watch.Start();
            
            WorldModel.UpdateWorldAroundPlayer(newCoords);
            
            watch.Stop();

            var elapsedMap = watch.ElapsedMilliseconds;
            
            watch.Restart();
            ChunksRenderer.UpdateChunksAroundPlayer(newCoords);
            
            watch.Stop();
            
            Debug.LogWarning($"<color=\"aqua\">GameAppState.UpdateView() : elapsedData:{elapsedMap} : elapsedRender:{watch.ElapsedMilliseconds}</color>");


            _lastPlayerCoords = newCoords;
        }
    }
}