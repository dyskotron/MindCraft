using System.Collections.Generic;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using MindCraft.View.Chunk;
using strange.framework.api;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MindCraft.View
{
    public class ChunksRenderer
    {
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        
        private Dictionary<ChunkCoord, ChunkView> _chunks = new Dictionary<ChunkCoord, ChunkView>();
        private List<ChunkView> _chunkPool = new List<ChunkView>();
        
        public void UpdateChunkMesh(ChunkCoord coords, NativeArray<byte> chunkMap)
        {
            _chunks[coords].UpdateChunkMesh(chunkMap);
        }
        
        public void GenerateChunksAroundPlayer(ChunkCoord coords)
        {
            //render all chunks within view distance
            foreach (var pos in MapBoundsLookup.ChunkGeneration)
            {
                CreateChunk(new ChunkCoord(pos)); 
            } 
        }

        public void UpdateChunksAroundPlayer(ChunkCoord newCoords)
        {
            //TODO ONLY SetActive(false); for chunks within MapBoundsLookup.REMOVE_RING_OFFSET
            //current HideChunk method moving chunk to the pool should be calls only for those out of remove ring.
            
            //hide chunks out of sight
            foreach (var position in MapBoundsLookup.ChunkRemove)
            {
                HideChunk(newCoords.X + position.x, newCoords.Y + position.y);    
            }

            //show chunks coming into view distance
            foreach (var position in MapBoundsLookup.ChunkAdd)
            {
                ShowChunk(newCoords.X + position.x, newCoords.Y + position.y);
            }
        }
        
        public void CreateChunk(ChunkCoord coords)
        {
            var map = WorldModel.GetMapByChunkCoords(coords);

            ChunkView chunkView;
            if (_chunkPool.Count > 0)
            {
                chunkView = _chunkPool[0];
                _chunkPool.RemoveAt(0);
            }
            else
            {
                chunkView = InstanceProvider.GetInstance<ChunkView>();    
            }
            
            chunkView.Init(coords);
            chunkView.UpdateChunkMesh(map);

            _chunks[coords] = chunkView;
        }
        
        private void ShowChunk(int x, int y)
        {
            var coords = new ChunkCoord(x, y);
            
            if (!_chunks.ContainsKey(coords))
                CreateChunk(coords);
            else if (!_chunks[coords].IsActive)
                _chunks[coords].IsActive = true;
        }
        
        private void HideChunk(int x, int y)
        {
            var coords = new ChunkCoord(x, y);
            _chunks.TryGetValue(coords, out ChunkView chunk);

            if (chunk != null)
            {
                chunk.IsActive = false;
                _chunks.Remove(coords);
                
                if (!chunk.IsRendering)
                    _chunkPool.Add(chunk);
                //else
                    //TODO: schedule for pooling
            }
        }
    }
}