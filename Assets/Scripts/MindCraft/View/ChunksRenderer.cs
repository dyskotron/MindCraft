using System.Collections.Generic;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Lookup;
using MindCraft.Model;
using strange.framework.api;

namespace MindCraft.View
{
    public class ChunksRenderer
    {
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        
        private Dictionary<ChunkCoord, Chunk> _chunks = new Dictionary<ChunkCoord, Chunk>();
        private ChunkCoord _lastPlayerCoords;
        private List<Chunk> _chunkPool = new List<Chunk>();
        
        public void UpdateChunkMesh(ChunkCoord coords, byte[,,] chunkMap)
        {
            _chunks[coords].UpdateChunkMesh(chunkMap);
        }
        
        public void RenderChunksAroundPlayer(ChunkCoord coords)
        {
            var xMin = coords.X - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var xMax = coords.X + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var yMin = coords.Y - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var yMax = coords.Y + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            
            //create map data
            for (var x = xMin; x < xMax; x++)
            {
                for (var y = yMin; y < yMax; y++)
                {
                    CreateChunk(new ChunkCoord(x, y));
                }
            }
        }

        public void UpdateChunksAroundPlayer(ChunkCoord newCoords)
        {
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

            //TODO: merge loops so everything is checked in one iteration

            //show new chunks
            for (var x = newMinX; x <= newMaxX; x++)
            {
                for (var y = newMinY; y <= newMaxY; y++)
                {
                    //except old cords
                    if (x >= lastMinX && x < lastMaxX && y >= lastMinY && y <= lastMaxY)
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
        
        public void CreateChunk(ChunkCoord coords)
        {
            var map = WorldModel.GetMapByChunkCoords(coords);

            Chunk chunk;
            if (_chunkPool.Count > 0)
            {
                chunk = _chunkPool[0];
                _chunkPool.RemoveAt(0);
            }
            else
            {
                chunk = InstanceProvider.GetInstance<Chunk>();    
            }
            
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
            var coords = new ChunkCoord(x, y);
            _chunks.TryGetValue(coords, out Chunk chunk);

            if (chunk != null)
            {
                _chunkPool.Add(_chunks[coords]);
                _chunks.Remove(coords);
            }
        }
    }
}