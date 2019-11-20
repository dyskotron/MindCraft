using System.Collections.Generic;
using System.Diagnostics;
using DefaultNamespace;
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
        [Inject] public IVoxelPhysicsWorld Physics { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IAssetManager AssetManager { get; set; }


        private Dictionary<ChunkCoord, Chunk> _chunks = new Dictionary<ChunkCoord, Chunk>();
        private Player _player;

        protected override void Enter()
        {
            base.Enter();

            Cursor.lockState = CursorLockMode.Locked;
            
            Random.InitState(WorldSettings.Seed);

//            Random.InitState(Seed);
//            
//            Camera.main.nearClipPlane = 0.01f;
//            Camera.main.farClipPlane = VoxelLookups.VIEW_DISTANCE;

            var watch = new Stopwatch();
            watch.Start();
            GenerateWorld();
            watch.Stop();

            GENERATION_TIME_TOTAL = (float)watch.Elapsed.TotalSeconds;
            
            
            //create player
            _player = AssetManager.GetGameObject<Player>(ResourcePath.PLAYER_PREFAB);
            _player.transform.position = new Vector3(0f, WorldModel.GetTerrainHeight(0,0) + 5, 0f);
            
            //Start Physics
            Physics.AddRigidBody(new VoxelRigidBody(WorldSettings.PlayerRadius, WorldSettings.PlayerHeight, _player.transform));
            Physics.Start();
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

        private void CreateChunk(ChunkCoord coords)
        {
            var map = WorldModel.CreateChunkMap(coords);
            var chunk = InstanceProvider.GetInstance<Chunk>();
            chunk.Init(coords);
            chunk.UpdateChunkMesh(map);

            _chunks[coords] = chunk;
        }
    }
}