using System.Collections.Generic;
using Framewerk.StrangeCore;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using MindCraft.View.Chunk.Jobs;
using strange.framework.api;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.View.Chunk
{
    public interface IWorldRenderer
    {
        void RenderChunks(List<int2> renderChunks, List<int2> dataCords);
        void RemoveChunks(List<int2> renderChunks, List<int2> dataCords);
    }

    public class WorldRenderer : IWorldRenderer, IDestroyable
    {
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }

        private Dictionary<int2, ChunkView> _chunks = new Dictionary<int2, ChunkView>();
        private Dictionary<int2, NativeArray<float>> _lightLevelsMaps = new Dictionary<int2, NativeArray<float>>();

        private List<ChunkView> _chunkPool = new List<ChunkView>();

        public void Destroy()
        {
            foreach (var chunkLightLevels in _lightLevelsMaps.Values)
            {
                chunkLightLevels.Dispose();
            }
        }

        public void RenderChunks(List<int2> renderChunks, List<int2> dataCords)
        {
            // ============ Calculate Light Rays ============
            var jobArray = new NativeArray<JobHandle>(dataCords.Count, Allocator.Temp);

            var i = 0;
            foreach (var coords in dataCords)
            {
                if(!_lightLevelsMaps.ContainsKey(coords))
                    _lightLevelsMaps[coords] = new NativeArray<float>(GeometryLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
                else
                    Debug.LogWarning($"<color=\"aqua\">WorldRenderer.RenderChunks() : light levels at{dataCords} already exists!</color>");
                
                
                var job = new CalculateLightRaysJob()
                          {
                              MapData = WorldModel.GetMapByChunkCoords(coords),
                              BlockDataLookup = BlockDefs.BlockDataLookup,
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
                _chunks.TryGetValue(coords, out ChunkView chunkView);

                if (chunkView == null)
                {
                    //TODO: bring back pooling
                    chunkView = InstanceProvider.GetInstance<ChunkView>();
                    _chunks[coords] = chunkView;
                    chunkView.Init(coords);
                }
                
                //render all chunks within view distance
                chunkView.UpdateChunkMesh(GetDataForChunkWithNeighbours(coords), GetLightsForChunkWithNeighbours(coords));
            }
        }

        public void RemoveChunks(List<int2> renderChunks, List<int2> dataCords)
        {
            //TODO: pooling
            foreach (var coords in renderChunks)
            {
                _chunks[coords].IsActive = false;
                _chunks.Remove(coords);
            }

            foreach (var coords in dataCords)
            {
                _lightLevelsMaps[coords].Dispose();
                _lightLevelsMaps.Remove(coords);
            }
        }

        #region Data for jobs processing

        private NativeArray<byte> GetDataForChunkWithNeighbours(int2 coords)
        {
            var multimap = new NativeArray<byte>(GeometryLookups.VOXELS_PER_CLUSTER, Allocator.Persistent);
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var offset = (x + y * 3) * GeometryLookups.VOXELS_PER_CHUNK;
                    var map = WorldModel.GetMapByChunkCoords(coords + new int2(x - 1, y - 1));
                multimap.Slice(offset, GeometryLookups.VOXELS_PER_CHUNK).CopyFrom(map);
                }
            }
            
            return multimap;
        }

        private NativeArray<float> GetLightsForChunkWithNeighbours(int2 coords)
        {
            var multimap = new NativeArray<float>(GeometryLookups.VOXELS_PER_CLUSTER, Allocator.Persistent);
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var offset = (x + y * 3) * GeometryLookups.VOXELS_PER_CHUNK;
                    var map = _lightLevelsMaps[coords + new int2(x - 1, y - 1)];
                multimap.Slice(offset, GeometryLookups.VOXELS_PER_CHUNK).CopyFrom(map);
                }
            }

            return multimap;
        }

        #endregion
    }
}