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
        private int2 _lastPlayerCoords;

        //TODO: chunk size 16 x 16 x 256
        //TODO: trees
        //TODO: water
        //TODO: fix building block colliding with player
        //TODO: Fix physics bug
        //TODO: Custom render pipeline
        //TODO: separate mesh for solid and transparent voxels
        //TODO: split all meshes to one long array per each side so it can be mesh instanced?
        //TODO: make alpha setting for shader per vertex so it can be only one shader?
        
        protected override void Enter()
        {
            base.Enter();

            Random.InitState(WorldSettings.Seed);
            
            WorldRenderer.Init();
            
            //Create player
            _playerView = AssetManager.GetGameObject<PlayerView>(ResourcePath.PLAYER_PREFAB);

            Vector3 initPosition;
            if (SaveLoadManager.LoadedGame.IsLoaded)
                initPosition = SaveLoadManager.LoadedGame.InitPosition;
            else
                initPosition = new Vector3(0.5f, WorldModel.GetTerrainHeight(new Vector3(0, 0, 0)) + 1, 0.5f);
            
            initPosition.y = GeometryConsts.CHUNK_HEIGHT - 5;
            _playerView.transform.position = initPosition;
            
            var playerCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(initPosition);
            
            GenerateWorld(playerCoords);
            
            var camera = ViewConfig.Camera3d;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = GeometryConsts.VIEW_DISTANCE;

            Updater.EveryFrame(UpdateView);
        }

        private void GenerateWorld(int2 playerPosition)
        {
            var dataCords = GetDataCoords(playerPosition, MapBoundsLookup.DataGeneration);
            var renderCords = GetRenderCoords(playerPosition, MapBoundsLookup.RenderGeneration);

            Debug.LogWarning($"<color=\"aqua\">GameAppState.GenerateWorld() : RenderGeneration.Length: {MapBoundsLookup.RenderGeneration.Length}</color>");

            var dataWatch = new Stopwatch();
            dataWatch.Start();
            WorldModel.CreateChunkMaps(dataCords);
            dataWatch.Stop();
            Debug.LogWarning($"<color=\"aqua\">GameAppState.GenerateWorld() : dataWatch.ElapsedMilliseconds: {dataWatch.ElapsedMilliseconds}</color>");
            
            var renderChunksWatch = new Stopwatch();
            renderChunksWatch.Start();
            WorldRenderer.RenderChunks(renderCords, dataCords);
            renderChunksWatch.Stop();
            Debug.LogWarning($"<color=\"aqua\">GameAppState.GenerateWorld() : renderChunksWatch.ElapsedMilliseconds: {renderChunksWatch.ElapsedMilliseconds}</color>");

            _lastPlayerCoords = playerPosition;
        }

        private void UpdateView()
        {
            var newCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(_playerView.transform.position);
            if (newCoords.x == _lastPlayerCoords.x && newCoords.y == _lastPlayerCoords.y)
                return;

            //Remove unused data + view
            var watch = new Stopwatch();
            watch.Start();
            
            var removeDataCords = GetDataCoords(newCoords, MapBoundsLookup.MapDataRemove, true);
            var removeRenderCords = GetRenderCoords(newCoords, MapBoundsLookup.ChunkRemove, true);
            WorldModel.RemoveData(removeDataCords);
            WorldRenderer.RemoveChunks(removeRenderCords, removeDataCords);

            //Create map data + queue chunk render for newly discovered chunks
            
            var dataCords = GetDataCoords(newCoords, MapBoundsLookup.MapDataAdd);
            var renderCords = GetRenderCoords(newCoords, MapBoundsLookup.ChunkAdd);
            WorldModel.CreateChunkMaps(dataCords);
            WorldRenderer.RenderChunks(renderCords, dataCords);
            
            watch.Stop();
            
            Debug.LogWarning($"<color=\"aqua\">GameAppState.UpdateView() : watch.ElapsedMilliseconds: {watch.ElapsedMilliseconds}</color>");


            _lastPlayerCoords = newCoords;
        }

        /// <summary>
        /// Temp shiat, move out from gamestate?
        /// </summary>
        private HashSet<int2> _generatedData = new HashSet<int2>();
        private HashSet<int2> _renderedChunks = new HashSet<int2>();

        private List<int2> GetDataCoords(int2 cords, int2[] relativeCordsArray, bool remove = false)
        {
            var coordsList = new List<int2>();
            foreach (var position in relativeCordsArray)
            {
                var currentCoords = new int2(cords.x + position.x, cords.y + position.y);
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

        private List<int2> GetRenderCoords(int2 cords, int2[] relativeCordsArray, bool remove = false)
        {
            var coordsList = new List<int2>();
            foreach (var position in relativeCordsArray)
            {
                var currentCoords = new int2(cords.x + position.x, cords.y + position.y);
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