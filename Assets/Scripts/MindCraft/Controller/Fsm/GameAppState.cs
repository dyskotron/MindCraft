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
using MindCraft.View.Chunk;
using MindCraft.View.Screen;
using Plugins.Framewerk;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace MindCraft.Controller.Fsm
{
    public class GameAppState : AppState<GameAppScreen>
    {
        [Inject] public IUpdater Updater { get; set; }
        [Inject] public IAssetManager AssetManager { get; set; }
        [Inject] public ViewConfig ViewConfig { get; set; }

        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public IWorldRenderer WorldRenderer { get; set; }
        [Inject] public ISaveLoadManager SaveLoadManager { get; set; }

        private PlayerView _playerView;
        private ChunkCoord _lastPlayerCoords;

        protected override void Enter()
        {
            base.Enter();

            Random.InitState(WorldSettings.Seed);
            
            //Create player
            _playerView = AssetManager.GetGameObject<PlayerView>(ResourcePath.PLAYER_PREFAB);

            Vector3 initPosition; 
            if (SaveLoadManager.LoadedGame.IsLoaded)
                initPosition = SaveLoadManager.LoadedGame.InitPosition;
            else
                initPosition = new Vector3(0.5f, WorldModel.GetTerrainHeight(new Vector3(0, 0, 0)) + 1, 0.5f);
            
            _playerView.transform.position = initPosition;
            
            var playerCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(initPosition);
            
            GenerateWorld(playerCoords);
            
            var camera = ViewConfig.Camera3d;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = VoxelLookups.VIEW_DISTANCE;

            Updater.EveryFrame(UpdateView);
        }

        private void GenerateWorld(ChunkCoord playerPosition)
        {
            var dataCords = GetDataCoords(playerPosition, MapBoundsLookup.DataGeneration);
            var renderCords = GetRenderCoords(playerPosition, MapBoundsLookup.RenderGeneration);

            var dataWatch = new Stopwatch();
            dataWatch.Start();
            WorldModel.CreateChunkMaps(dataCords);
            dataWatch.Stop();
            Debug.LogWarning($"<color=\"aqua\">GameAppState.GenerateWorld() : dataWatch.ElapsedMilliseconds: {dataWatch.ElapsedMilliseconds}</color>");
            
            var renderChunksWatch = new Stopwatch();
            renderChunksWatch.Start();
            WorldRenderer.RenderChunks(renderCords, dataCords);
            renderChunksWatch.Stop();
            Debug.LogWarning($"<color=\"aqua\">GameAppState.GenerateWorld() : renderChunksWatch.ElapsedMilliseconds: {renderChunksWatch.Elapsed.TotalSeconds}</color>");

            _lastPlayerCoords = playerPosition;
        }

        private void UpdateView()
        {
            var newCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(_playerView.transform.position);
            if (newCoords == _lastPlayerCoords)
                return;

            //Remove unused data + view
            var removeDataCords = GetDataCoords(newCoords, MapBoundsLookup.MapDataRemove, true);
            var removeRenderCords = GetRenderCoords(newCoords, MapBoundsLookup.ChunkRemove, true);
            WorldModel.RemoveData(removeDataCords);
            WorldRenderer.RemoveChunks(removeRenderCords, removeDataCords);

            //Create map data + queue chunk render for newly discovered chunks
            var dataCords = GetDataCoords(newCoords, MapBoundsLookup.MapDataAdd);
            var renderCords = GetRenderCoords(newCoords, MapBoundsLookup.ChunkAdd);
            WorldModel.CreateChunkMaps(dataCords);
            WorldRenderer.RenderChunks(renderCords, dataCords);

            _lastPlayerCoords = newCoords;
        }

        /// <summary>
        /// Temp shiat, move out from gamestate?
        /// </summary>
        private HashSet<ChunkCoord> _generatedData = new HashSet<ChunkCoord>();
        private HashSet<ChunkCoord> _renderedChunks = new HashSet<ChunkCoord>();

        private List<ChunkCoord> GetDataCoords(ChunkCoord cords, int2[] relativeCordsArray, bool remove = false)
        {
            var coordsList = new List<ChunkCoord>();
            foreach (var position in relativeCordsArray)
            {
                var currentCoords = new ChunkCoord(cords.X + position.x, cords.Y + position.y);
                if (_generatedData.Contains(currentCoords) == remove)
                {
                    if (remove)
                        _generatedData.Remove(currentCoords);
                    else
                        _generatedData.Add(currentCoords);

                    coordsList.Add(currentCoords);
                }
            }

            return coordsList;
        }

        private List<ChunkCoord> GetRenderCoords(ChunkCoord cords, int2[] relativeCordsArray, bool remove = false)
        {
            var coordsList = new List<ChunkCoord>();
            foreach (var position in relativeCordsArray)
            {
                var currentCoords = new ChunkCoord(cords.X + position.x, cords.Y + position.y);
                if (_renderedChunks.Contains(currentCoords) == remove)
                {
                    if (remove)
                        _renderedChunks.Remove(currentCoords);
                    else
                        _renderedChunks.Add(currentCoords);

                    coordsList.Add(currentCoords);
                }
            }

            return coordsList;
        }
    }
}