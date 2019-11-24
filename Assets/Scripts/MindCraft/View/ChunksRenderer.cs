using System.Collections.Generic;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
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
        private List<Chunk> _chunkPool = new List<Chunk>();
        
        public void UpdateChunkMesh(ChunkCoord coords, byte[,,] chunkMap)
        {
            _chunks[coords].UpdateChunkMesh(chunkMap);
        }
        
        public void GenerateChunksAroundPlayer(ChunkCoord coords)
        {
            //render all chunks within view distance
            foreach (var pos in MapBoundsLookup.ChunkGenaration)
            {
                CreateChunk(new ChunkCoord(pos)); 
            } 
        }

        public void UpdateChunksAroundPlayer(ChunkCoord newCoords)
        {
            if (newCoords == _lastPlayerCoords)
                return;

            //hide chunks out of sight
            foreach (var position in MapBoundsLookup.ChunkRemove)
            {
                HideChunk(newCoords.X + position.x, newCoords.Y + position.y);    
            }

            var iShownChunks = 0;
            //show chunks cmming into view distance
            foreach (var position in MapBoundsLookup.ChunkAdd)
            {
                if (ShowChunk(newCoords.X + position.x, newCoords.Y + position.y))
                    iShownChunks++;
            }

            Debug.LogWarning($"<color=\"aqua\">ChunksRenderer.UpdateChunksAroundPlayer() : Chunks Shown {iShownChunks}</color>");
            
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
        
        private bool ShowChunk(int x, int y)
        {
            var coords = new ChunkCoord(x, y);
            if (!_chunks.ContainsKey(coords)){
                CreateChunk(coords);
                return true;
            }
            else if (!_chunks[coords].IsActive)
            {
                _chunks[coords].IsActive = true;
            }
            
            return false;
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