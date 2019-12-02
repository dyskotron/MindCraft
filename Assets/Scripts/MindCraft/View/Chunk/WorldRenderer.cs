using System.Collections.Generic;
using Framewerk.StrangeCore;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using strange.framework.api;
using Unity.Collections;
using Unity.Jobs;

namespace MindCraft.View.Chunk
{
    public class WorldRenderer : IDestroyable
    {
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }
        
        private Dictionary<ChunkCoord, ChunkView> _chunks = new Dictionary<ChunkCoord, ChunkView>();
        private Dictionary<ChunkCoord, NativeArray<float>> _lightLevelsMaps = new Dictionary<ChunkCoord, NativeArray<float>>();
        
        private List<ChunkView> _chunkPool = new List<ChunkView>();
        
        public void Destroy()
        {
            foreach (var chunkLightLevels in _lightLevelsMaps.Values)
            {
                chunkLightLevels.Dispose();    
            }
        }
        
        public void UpdateChunkMesh(ChunkCoord coords, NativeArray<byte> chunkMap)
        {
            var job = new CalculateLightRaysJob()
                      {
                          MapData = WorldModel.GetMapByChunkCoords(coords),
                          TransparencyLookup = BlockDefs.TransparencyLookup,
                          LightLevels = _lightLevelsMaps[coords]
                      };

            var handle = job.Schedule();
            handle.Complete();
            
            _chunks[coords].UpdateChunkMesh(GetDataForChunkWithNeighbours(coords), GetLightsForChunkWithNeighbours(coords));
        }

        public void RenderChunks(List<ChunkCoord> renderChunks, List<ChunkCoord> dataCords)
        {   
            // ============ Calculate Light Rays ============
            var jobArray = new NativeArray<JobHandle>(dataCords.Count, Allocator.Temp);
            
            
            var i = 0;
            foreach (var coords in dataCords)
            {
                _lightLevelsMaps[coords] = new NativeArray<float>(VoxelLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
                var job = new CalculateLightRaysJob()
                          {
                              MapData = WorldModel.GetMapByChunkCoords(coords),
                              TransparencyLookup = BlockDefs.TransparencyLookup,
                              LightLevels = _lightLevelsMaps[coords]
                          };

                jobArray[i] = job.Schedule();
                i++;
            }
            
            JobHandle.CompleteAll(jobArray);
            
            jobArray.Dispose();
            
            // ============ Diffuse Lights + Render ============
            foreach (var coords in renderChunks)
            {
                //init chunks
                var chunkView = InstanceProvider.GetInstance<ChunkView>();    
                chunkView.Init(coords);
                _chunks[coords] = chunkView; 
                
                // process diffuse lights
                // TODO
                
                //render all chunks within view distance
                chunkView.UpdateChunkMesh( GetDataForChunkWithNeighbours(coords), GetLightsForChunkWithNeighbours(coords));
            } 
        }
        
        #region Data for jobs processing

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

        private NativeArray<float> GetLightsForChunkWithNeighbours(ChunkCoord coords)
        {
            var multimap = new NativeArray<float>(9 * VoxelLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var offset = (x + y * 3) * VoxelLookups.VOXELS_PER_CHUNK;
                    var map = _lightLevelsMaps[coords + new ChunkCoord(x - 1, y - 1)];
                    multimap.Slice(offset, VoxelLookups.VOXELS_PER_CHUNK).CopyFrom(map);
                }
            }

            return multimap;
        }

        #endregion

        
    }
}