using System.Collections.Generic;
using System.Diagnostics;
using Framewerk.AppStateMachine;
using MindCraft.GameObjects;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Lookup;
using MindCraft.Model;
using MindCraft.View.Screen;
using strange.framework.api;
using UnityEngine;

namespace MindCraft.Controller.Fsm
{
    public class GameAppState : AppState<GameAppScreen>
    {
        public static float GENERATION_TIME_TOTAL = 0;
        
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public IInstanceProvider InstanceProvider { get; set; }


        private Dictionary<ChunkCoord, Chunk> _chunks = new Dictionary<ChunkCoord, Chunk>();

        protected override void Enter()
        {
            base.Enter();

            Cursor.lockState = CursorLockMode.Locked;

//            Random.InitState(Seed);
//            
//            Camera.main.nearClipPlane = 0.01f;
//            Camera.main.farClipPlane = VoxelLookups.VIEW_DISTANCE;

            var watch = new Stopwatch();
            watch.Start();
            GenerateWorld();
            watch.Stop();

            GENERATION_TIME_TOTAL = (float)watch.Elapsed.TotalSeconds;
        }

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