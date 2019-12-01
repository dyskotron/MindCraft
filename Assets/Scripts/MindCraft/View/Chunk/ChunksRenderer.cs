using System.Collections.Generic;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using strange.framework.api;
using Unity.Collections;

namespace MindCraft.View.Chunk
{
    public class ChunksRenderer
    {
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        
        private Dictionary<ChunkCoord, ChunkView> _chunks = new Dictionary<ChunkCoord, ChunkView>();
        private List<ChunkView> _chunkPool = new List<ChunkView>();
        
        public void UpdateChunkMesh(ChunkCoord coords, NativeArray<byte> chunkMap)
        {
            _chunks[coords].UpdateChunkMesh(GetDataForChunkWithNeighbours(coords));
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
            chunkView.UpdateChunkMesh( GetDataForChunkWithNeighbours(coords));

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

        private NativeArray<byte> GetDataForChunkWithNeighbours(ChunkCoord coords)
        {
            var multimap = new NativeArray<byte>(9 * VoxelLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var offset = (x + y * 3) * VoxelLookups.VOXELS_PER_CHUNK;
                    var map = WorldModel.GetMapByChunkCoords(coords + new ChunkCoord(x - 1, y - 1));
                    multimap.Slice(offset, VoxelLookups.VOXELS_PER_CHUNK).CopyFrom(map);
                }
            }

            return multimap;
        }
    }
}