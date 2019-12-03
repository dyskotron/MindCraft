using System.Collections;
using System.Collections.Generic;
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
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

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
        [Inject] public WorldRenderer WorldRenderer { get; set; }

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
            var dataCords = GetDataCoords(playerPosition, MapBoundsLookup.DataGeneration);
            var renderCords = GetRenderCoords(playerPosition, MapBoundsLookup.RenderGeneration);
            
            WorldModel.CreateChunkMaps(dataCords);
            WorldRenderer.RenderChunks(renderCords, dataCords);
            
            _lastPlayerCoords = playerPosition;
        }

        private void UpdateView()
        {
            var newCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(_playerView.transform.position);
            if (newCoords == _lastPlayerCoords)
                return;
            
            var watch = new Stopwatch();
            watch.Start();
            
            var dataCords = GetDataCoords(newCoords, MapBoundsLookup.MapDataAdd);
            WorldModel.CreateChunkMaps(dataCords);
            
            watch.Stop();
            var elapsedMap = watch.ElapsedMilliseconds;
            watch.Restart();
            
            var renderCords = GetRenderCoords(newCoords, MapBoundsLookup.ChunkAdd);
            WorldRenderer.RenderChunks(renderCords, dataCords);
            
            watch.Stop();
            
            Debug.LogWarning($"<color=\"aqua\">GameAppState.UpdateView() : elapsedData:{elapsedMap} : elapsedRender:{watch.ElapsedMilliseconds}</color>");

            _lastPlayerCoords = newCoords;
        }
        
        /// <summary>
        /// Temp shiat, move somewehere
        /// </summary>
        private HashSet<ChunkCoord> _generatedData = new HashSet<ChunkCoord>();
        private HashSet<ChunkCoord> _renderedChunks = new HashSet<ChunkCoord>();

        private List<ChunkCoord> GetDataCoords(ChunkCoord cords, int2[] relativeCordsArray)
        {
            var coordsList = new List<ChunkCoord>();
            foreach (var position in relativeCordsArray)
            {
                var currentCoords = new ChunkCoord(cords.X + position.x, cords.Y + position.y);
                if (!_generatedData.Contains(currentCoords))
                {
                    _generatedData.Add(currentCoords);
                    coordsList.Add(currentCoords);
                }
            }
            
            return coordsList;
        }
        
        private List<ChunkCoord> GetRenderCoords(ChunkCoord cords, int2[] relativeCordsArray)
        {
            var coordsList = new List<ChunkCoord>();
            foreach (var position in relativeCordsArray)
            {
                var currentCoords = new ChunkCoord(cords.X + position.x, cords.Y + position.y);
                if (!_renderedChunks.Contains(currentCoords))
                {
                    _renderedChunks.Add(currentCoords);
                    coordsList.Add(currentCoords);
                }
            }
            
            return coordsList;
        }
    }
}