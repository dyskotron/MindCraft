using System.Collections.Generic;
using System.Diagnostics;
using DefaultNamespace;
using Framewerk;
using Framewerk.AppStateMachine;
using Framewerk.Managers;
using MindCraft.Data;
using MindCraft.GameObjects;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Lookup;
using MindCraft.Model;
using MindCraft.Physics;
using MindCraft.View.Screen;
using strange.framework.api;
using UnityEngine;

namespace MindCraft.Controller.Fsm
{
    public class GameAppState : AppState<GameAppScreen>
    {
        public static float GENERATION_TIME_TOTAL = 0;
        
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IUpdater Updater { get; set; }
        [Inject] public IAssetManager AssetManager { get; set; }
        
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IVoxelPhysicsWorld Physics { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public IPlayerController PlayerController { get; set; }

        private Dictionary<ChunkCoord, Chunk> _chunks = new Dictionary<ChunkCoord, Chunk>();
        private Player _player;
        private ChunkCoord _lastPlayerCoords;

        protected override void Enter()
        {
            base.Enter();

            Random.InitState(WorldSettings.Seed);
            Cursor.lockState = CursorLockMode.Locked;
            
            var watch = new Stopwatch();
            watch.Start();
            GenerateWorld();
            watch.Stop();

            GENERATION_TIME_TOTAL = (float)watch.Elapsed.TotalSeconds;
            
            //create player
            _player = AssetManager.GetGameObject<Player>(ResourcePath.PLAYER_PREFAB);
            _player.transform.position = new Vector3(0f, WorldModel.GetTerrainHeight(0,0) + 5, 0f);

            var playerBody = new VoxelRigidBody(WorldSettings.PlayerSettings.Radius, 
                                                WorldSettings.PlayerSettings.Height, 
                                                _player.transform);
            
            //Start Physics
            Physics.AddRigidBody(playerBody);
            
            //Start PlayerController
            PlayerController.Init(playerBody, _player.PlayerCamera.transform);
            
            Updater.EveryFrame(UpdateView);
            
            _player.PlayerCamera.nearClipPlane = 0.01f;
            _player.PlayerCamera.farClipPlane = VoxelLookups.VIEW_DISTANCE;

        }
        
        //MOVE TO MODEL
        private void GenerateWorld()
        {
            //create chunks
            for (var x = -VoxelLookups.VIEW_DISTANCE_IN_CHUNKS; x < VoxelLookups.VIEW_DISTANCE_IN_CHUNKS; x++)
            {
                for (var y = -VoxelLookups.VIEW_DISTANCE_IN_CHUNKS; y < VoxelLookups.VIEW_DISTANCE_IN_CHUNKS; y++)
                {
                    CreateChunk(new ChunkCoord(x, y));
                }
            }
        }

        private void UpdateView()
        {
            //update chunks
            var newCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(_player.transform.position);

            if (newCoords == _lastPlayerCoords)
                return;

            var lastMinX = _lastPlayerCoords.X - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var lastMinY = _lastPlayerCoords.Y - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var lastMaxX = _lastPlayerCoords.X + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var lastMaxY = _lastPlayerCoords.Y + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;

            var newMinX = newCoords.X - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var newMinY = newCoords.Y - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var newMaxX = newCoords.X + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var newMaxY = newCoords.Y + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;

            //TODO: merged loops so everything is checked in one iteration

            //show new chunks
            for (var x = newMinX; x < newMaxX; x++)
            {
                for (var y = newMinY; y < newMaxY; y++)
                {
                    //except old cords
                    if (x >= lastMinX && x < lastMaxX && y >= lastMinY && y < lastMaxY)
                        continue;

                    ShowChunk(x, y);
                }
            }

            //hide all old chunks
            for (var x = lastMinX; x < lastMaxX; x++)
            {
                for (var y = lastMinY; y < lastMaxY; y++)
                {
                    //except new ones
                    if (x >= newMinX && x < newMaxX && y >= newMinY && y < newMaxY)
                        continue;

                    HideChunk(x, y);
                }
            }

            _lastPlayerCoords = newCoords;
        }
        
        private void CreateChunk(ChunkCoord coords)
        {
            var map = WorldModel.CreateChunkMap(coords);
            var chunk = InstanceProvider.GetInstance<Chunk>();
            chunk.Init(coords);
            chunk.UpdateChunkMesh(map);

            _chunks[coords] = chunk;
        }
        
        private void ShowChunk(int x, int y)
        {
            var coords = new ChunkCoord(x, y);
            if (!_chunks.ContainsKey(coords))
                CreateChunk(coords);
            else if (!_chunks[coords].IsActive)
            {
                _chunks[coords].IsActive = true;
            }
        }
        
        private void HideChunk(int x, int y)
        {
            _chunks.TryGetValue(new ChunkCoord(x, y), out Chunk chunk);

            if (chunk != null)
                chunk.IsActive = false;
        }
    }
}