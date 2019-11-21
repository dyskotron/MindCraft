using System.Collections.Generic;
using MindCraft.GameObjects;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Lookup;
using MindCraft.Model;
using strange.framework.api;
using UnityEngine;

namespace MindCraft.View
{
    public class ChunksRenderer
    {
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        
        private Dictionary<ChunkCoord, Chunk> _chunks = new Dictionary<ChunkCoord, Chunk>();
        private ChunkCoord _lastPlayerCoords;
        
        public void GenerateWorld()
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
        
        public void UpdateChunkMesh(ChunkCoord coords, byte[,,] chunkMap)
        {
            _chunks[coords].UpdateChunkMesh(chunkMap);
        }

        public void UpdateChunks(Vector3 playerPosition)
        {
            //update chunks
            var newCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(playerPosition);

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