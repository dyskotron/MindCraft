using System.Collections.Generic;
using System.Diagnostics;
using Framewerk;
using Framewerk.StrangeCore;
using MindCraft.Common;
using MindCraft.Data;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using MindCraft.View.Chunk.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MindCraft.View.Chunk
{
    public interface IWorldRenderer
    {
        void Init();
        void RenderChunks(List<int2> renderCoords, List<int2> dataCoords);
        void RemoveChunks(List<int2> renderCoords, List<int2> dataCoords);
    }

    public class WorldRenderer : IWorldRenderer, IDestroyable
    {
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IUpdater Updater { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public TextureLookup TextureLookup { get; set; }
        [Inject] public GeometryLookups GeometryLookups { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }

        // ===== JOB DATA ====
        
        // rendered
        // active
        // data
        // pool
        
        //all used data can by found by coords (rendered, active & lightmap only)
        private Dictionary<int2, int> _dataIdByCoords = new Dictionary<int2, int>();

        private ComputeMeshData[] _computeData;

        private int _renderedDataCount = 0; // all mesh data generated and ready   (B1)
        private int _activeDataCount = 0; // rendered + in progress                (B2)
        private int _dataCount = 0; // all used data(active) + lightmap only      (B3)

        // ==== RENDER DATA =====
        
        // active
        // pool

        private Dictionary<int2, int> _meshIdByCoords = new Dictionary<int2, int>();
        private RenderMeshData[] _meshData;
        private int _activeMeshCount = 0;
        
        
        //TODO: batch meshes - one mesh per (lets say) 10 chunks,
        //use dirty flag pattern, whenever chunks are changed via remove chunks or when updating mesh after jobs are done,
        //just set the flag for uberchunk and join array just before first render

        public void Init()
        {
            _computeData = new ComputeMeshData[MapBoundsLookup.DataChunksCount * 2];

            //TODO: determine safe data margin for data in process
            
            for (var i = 0; i < MapBoundsLookup.DataChunksCount * 2; i++)
            {
                _computeData[i] = new ComputeMeshData();
            }

            _meshData = new RenderMeshData[MapBoundsLookup.RenderChunksCount * 2];

            for (var i = 0; i < MapBoundsLookup.RenderChunksCount * 2; i++)
            {
                _meshData[i] = new RenderMeshData(WorldSettings.WorldMaterial);
            }

            Updater.EveryFrame(Update);
        }

        public void Destroy()
        {
            foreach (var chunkMeshData in _computeData)
            {
                chunkMeshData.Dispose();
            }

            _computeData = null;

            Updater.RemoveFrameAction(Update);
        }

        public void RenderChunks(List<int2> renderCoords, List<int2> dataCoords)
        {
            // ============ Calculate Light Rays ============
            var lightRaysJobArray = new NativeArray<JobHandle>(dataCoords.Count, Allocator.Temp);

            var i = 0;
            foreach (var coords in dataCoords)
            {
                int dataId;

                if (!_dataIdByCoords.ContainsKey(coords))
                {
                    dataId = GetDataFromPool(coords);
                }
                else
                {
                    dataId = ReuseData(coords);
                    Debug.LogWarning($"<color=\"aqua\">WorldRenderer.RenderChunks() : Chunk Mesh Data at{dataCoords} already exists!</color>");
                }

                var job = new CalculateLightRaysJob()
                          {
                              MapData = WorldModel.GetMapByChunkCoords(coords),
                              BlockDataLookup = BlockDefs.BlockDataLookup,
                              LightLevels = _computeData[dataId].LightLevelMap
                          };

                lightRaysJobArray[i] = job.Schedule();
                i++;
            }

            JobHandle.CompleteAll(lightRaysJobArray);
            lightRaysJobArray.Dispose();

            // ============ Diffuse Lights + Render ============
            foreach (var coords in renderCoords)
            {
                var dataId = _dataIdByCoords[coords];
                if (dataId == 0)
                    Debug.LogWarning($"<color=\"aqua\">WorldRenderer.RenderChunks() : GOT ZERO EH</color>");

                var data = _computeData[dataId];
                //TODO: check that data on this Coords are not currently rendered

                GetDataForChunkWithNeighbours(coords, data);
                GetLightsForChunkWithNeighbours(coords, data);

                var diffuseLightsJob = new DiffuseLightsJob(data, BlockDefs.BlockDataLookup, GeometryLookups.Neighbours);
                var diffuseJobHandle = diffuseLightsJob.Schedule();

                var meshJob = new RenderChunkMeshJob(data,
                                                     TextureLookup.WorldUvLookupNative,
                                                     BlockDefs.BlockDataLookup,
                                                     GeometryLookups.Neighbours,
                                                     GeometryLookups.LightNeighbours,
                                                     GeometryLookups.IndexToVertex,
                                                     GeometryLookups.VerticesLookup,
                                                     GeometryLookups.TrianglesLookup
                                                    );

                data.JobHandle = meshJob.Schedule(diffuseJobHandle);
                data.IsRendering = true;

                //move data from current position in  "data" part to (newly)last "active" position
                SwapData(dataId, _activeDataCount, true);

                _activeDataCount++;
            }
        }

        private void Update()
        {
            //=== CHECK ALL CURRENTLY RENDERING CHUNKS

            for (var i = _renderedDataCount; i < _activeDataCount; i++)
            {
                var data = _computeData[i];
                if (!data.JobHandle.IsCompleted)
                    continue;

                if (!data.IsRendering)
                {
                    Debug.LogError($"<color=\"aqua\">WorldRenderer.Update() : Data with jobhandle complete is not actually rendering. id:{i}</color>");
                    continue;
                }

                //Reuse mesh data for given cords or take new from pool 
                var meshId = _meshIdByCoords.ContainsKey(data.Coords) ? _meshIdByCoords[data.Coords] : _activeMeshCount++;
                data.Complete();
                
                var mesh = _meshData[meshId].Mesh;
                mesh.Clear();
                mesh.vertices = NativeArrayUtil.NativeFloat3ToManagedVector3(data.Vertices);
                mesh.triangles = data.Triangles.ToArray();
                mesh.uv = NativeArrayUtil.NativeFloat2ToManagedVector2(data.Uvs);
                mesh.colors = NativeArrayUtil.NativeFloatToManagedColor(data.Colors);
                mesh.normals = NativeArrayUtil.NativeFloat3ToManagedVector3(data.Normals);
                
                _meshData[meshId].SetCoords(data.Coords);
                _meshIdByCoords[data.Coords] = meshId;

                //swap first uncompleted data with current data
                //so we have contiguous part of completed data on the beginning of array
                SwapData(i, _renderedDataCount, true);
                _renderedDataCount++;

                //TODO: limit chunks generation by time / number of chunks processed ?
            }
        }

        public void RemoveChunks(List<int2> renderCoords, List<int2> dataCoords)
        {
            //move all rendered chunks to "data" part
            //TODO: solve removing chunks during rendering
            foreach (var coords in renderCoords)
            {
                //=== REMOVE DATA ===

                var id = _dataIdByCoords[coords];

                if (id >= _renderedDataCount)
                {
                    Debug.LogError($"<color=\"aqua\">WorldRenderer.RemoveChunks() Render cords to remove not rendered yet, " +
                                   $"current data state is: {GetDataState(id)} " +
                                   $"coords: {coords}" +
                                   $"id: {id}</color>");
                    continue;
                }

                var temp = _computeData[id];

                _renderedDataCount--;
                _activeDataCount--;

                if (id != _renderedDataCount)
                {
                    //move last rendered to removed pos
                    _computeData[id] = _computeData[_renderedDataCount];
                    _dataIdByCoords[_computeData[id].Coords] = id;
                }

                if (_renderedDataCount != _activeDataCount)
                {
                    //move last active to removed last rendered pos
                    _computeData[_renderedDataCount] = _computeData[_activeDataCount];
                    _dataIdByCoords[_computeData[_renderedDataCount].Coords] = _renderedDataCount;
                }

                //move our removed data to removed last active pos
                _computeData[_activeDataCount] = temp;
                _dataIdByCoords[temp.Coords] = _activeDataCount;

                //CheckDataIntegrity(id);

                //=== REMOVE MESHES ===
                if (_meshIdByCoords.ContainsKey(coords))
                {
                    var meshId = _meshIdByCoords[coords];
                    
                    //update active meshes counter and mesh coords
                    _activeMeshCount--;
                    _meshIdByCoords.Remove(coords);
                    
                    //swap removed mesh with last active mesh if not the same
                    if (meshId != _activeMeshCount)
                    {
                        var meshData = _meshData[meshId];

                        //move last active mesh data to removed position
                        _meshData[meshId] = _meshData[_activeMeshCount];
                        _meshIdByCoords[_meshData[meshId].Coords] = meshId;

                        //move mesh data to (newly) first inactive position and remove coords
                        _meshData[_activeMeshCount] = meshData;
                    }
                }
                else
                {
                    Debug.LogError($"<color=\"aqua\">WorldRenderer.RemoveChunks() : NO MESH FOR COORDS {coords}</color>");
                }
            }

            foreach (var coords in dataCoords)
            {
                var removeId = _dataIdByCoords[coords];

                if (removeId < _activeDataCount || removeId >= _dataCount)
                {
                    Debug.LogError($"<color=\"aqua\">WorldRenderer.RemoveChunks() Data coords to remove not on valid position, " +
                                   $"current data state is: {GetDataState(removeId)} " +
                                   $"coords: {coords}" +
                                   $"id: {removeId}</color>");
                    continue;
                }

                _dataCount--;
                SwapData(_dataCount, removeId, false);

                _dataIdByCoords.Remove(coords);
            }
        }

        #region Data for jobs processing

        private void GetDataForChunkWithNeighbours(int2 coords, ComputeMeshData data)
        {
            var multimap = data.MapWithNeighbours;

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var offset = (x + y * 3) * GeometryLookups.VOXELS_PER_CHUNK;
                    var map = WorldModel.GetMapByChunkCoords(coords + new int2(x - 1, y - 1));
                    multimap.Slice(offset, GeometryLookups.VOXELS_PER_CHUNK).CopyFrom(map);
                }
            }
        }

        private void GetLightsForChunkWithNeighbours(int2 coords, ComputeMeshData data)
        {
            var multimap = data.LightMapWithNeighbours;
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var offset = (x + y * 3) * GeometryLookups.VOXELS_PER_CHUNK;
                    var id = _dataIdByCoords[coords + new int2(x - 1, y - 1)];
                    var map = _computeData[id].LightLevelMap;
                    multimap.Slice(offset, GeometryLookups.VOXELS_PER_CHUNK).CopyFrom(map);
                }
            }
        }

        #endregion

        #region Data manipulation helpers

        private int GetDataFromPool(int2 coords)
        {
            //id is datacount -> put data on the end of all used data
            var dataId = _dataCount;

            ComputeMeshData computeMeshData = _computeData[dataId];
            computeMeshData.SetCoords(coords);
            
            _dataIdByCoords[coords] = dataId;

            //move border of data so it contains newly added data
            _dataCount++;

            return dataId;
        }

        private int ReuseData(int2 coords)
        {
            var id = _dataIdByCoords[coords];

            if (id < _renderedDataCount)
            {
                //move data to inactive data part of array (same way as in RemoveChunks method)

                var temp = _computeData[id];

                _renderedDataCount--;
                _activeDataCount--;

                if (id != _renderedDataCount)
                {
                    //move last rendered to removed pos
                    //if (id != _renderedDataCount)
                    _computeData[id] = _computeData[_renderedDataCount];
                    _dataIdByCoords[_computeData[id].Coords] = id;
                }
//                else
//                    Debug.LogWarning($"<color=\"aqua\">WorldRenderer.RemoveChunks() : SWAPPING SAME IDS({_renderedDataCount}) _renderedDataCount</color>");

                if (_renderedDataCount != _activeDataCount)
                {
                    //move last active to removed last rendered pos
                    _computeData[_renderedDataCount] = _computeData[_activeDataCount];
                    _dataIdByCoords[_computeData[_renderedDataCount].Coords] = _renderedDataCount;
                }
//                else
//                    Debug.LogWarning($"<color=\"aqua\">WorldRenderer.RemoveChunks() : SWAPPING SAME IDS({_activeDataCount}) _activeDataCount</color>");

                //move our removed data to removed last active pos
                _computeData[_activeDataCount] = temp;
                _dataIdByCoords[temp.Coords] = _activeDataCount;

                return _activeDataCount;
            }

            if (id < _activeDataCount)
            {
                //data are in rendering phase, mark data and use data from pool instead   
                Debug.LogError($"<color=\"aqua\">WorldRenderer.ReuseData() : trying to reuse mesh data during rendering, this case is not handled properly yet</color>");
                _computeData[id].IsRendering = false;
                return GetDataFromPool(coords);
            }

            if (id < _dataCount)
            {
                //data are already in inactive data part of array
                return id;
            }

            //we shouldn't get there
            Debug.LogWarning($"<color=\"aqua\">WorldRenderer.ReuseData() : Getting data from pool via ReuseData method. " +
                             $"Should be used directly for better performance</color>");
            return GetDataFromPool(coords);
        }

        private void SwapData(int id1, int id2, bool updateId2Coords)
        {
            //store data we want to move
            var temp = _computeData[id1];

            //move data on target position to original data pos
            _computeData[id1] = _computeData[id2];

            if (updateId2Coords)
                _dataIdByCoords[_computeData[id1].Coords] = id1;

            //move our data to target position and update Coords
            _computeData[id2] = temp;
            _dataIdByCoords[temp.Coords] = id2;
        }

        private string GetDataState(int id)
        {
            if (id < _renderedDataCount)
                return "Rendered";

            if (id < _activeDataCount)
                return "Rendering";

            if (id < _dataCount)
                return "Pasive data";

            return "Pooled";
        }

        private void CheckDataIntegrity(int t)
        {
            for (int i = 0; i < _dataCount; i++)
            {
                var data = _computeData[i];
                if (_dataIdByCoords[data.Coords] != i)
                {
                    Debug.LogError($"<color=\"aqua\">WorldRenderer.CheckDataIntegrity() : DATA AT {i} target{t}: COORDS MISMATCH</color>");
                }
            }
        }

        #endregion
    }
}