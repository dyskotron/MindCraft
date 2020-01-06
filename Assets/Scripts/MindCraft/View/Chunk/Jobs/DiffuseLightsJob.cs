using MindCraft.Common;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.View.Chunk.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct DiffuseLightsJob : IJob
    {
        [ReadOnly] public NativeArray<byte> MapData;

        //Lookups
        [ReadOnly] public NativeArray<BlockDefData> BlockDataLookup;
        [ReadOnly] public NativeArray<int3> Neighbours;

        public NativeArray<float> LightLevels;
        public NativeQueue<int3> LitVoxels;

        private int _currentVertexIndex;

        public DiffuseLightsJob(ComputeMeshData data, NativeArray<BlockDefData> blockDataLookup, NativeArray<int3> neighbours)
        {
            _currentVertexIndex = 0;
            
            BlockDataLookup = blockDataLookup;
            MapData = data.MapWithNeighbours;

            Neighbours = neighbours;
            LightLevels = data.LightMapWithNeighbours;
            LitVoxels = data.LitVoxels;
        }

        public void Execute()
        {
            LitVoxels.Clear();
            
            //Enqueue lit voxels for processing
            //TODO: parallel job filter or store litvoxels already in calculate light ray job
            for (var x = GeometryConsts.LIGHTS_CLUSTER_MIN; x < GeometryConsts.LIGHTS_CLUSTER_MAX; x++)
            {
                for (var z = GeometryConsts.LIGHTS_CLUSTER_MIN; z < GeometryConsts.LIGHTS_CLUSTER_MAX; z++)
                {
                    for (var y = GeometryConsts.CHUNK_HEIGHT - 1; y >= 0; y--)
                    {
                        var index = ArrayHelper.ToCluster1D(x, y, z);
                        if (LightLevels[index] > GeometryConsts.LIGHT_FALL_OFF)
                            LitVoxels.Enqueue(new int3(x, y, z));
                    }
                }
            }

            //Iterate trough lit voxels and project light to neighbours
            while (LitVoxels.Count > 0)
            {
                var litVoxel = LitVoxels.Dequeue();
                var litVoxelId = ArrayHelper.ToCluster1D(litVoxel.x, litVoxel.y, litVoxel.z);
                var neighbourLightValue = LightLevels[litVoxelId] - GeometryConsts.LIGHT_FALL_OFF;

                //iterate trough neighbours
                for (int iF = 0; iF < GeometryConsts.FACES_PER_VOXEL; iF++)
                {
                    var neighbour = litVoxel + Neighbours[iF];

                    if (CheckVoxelBounds(neighbour.x, neighbour.y, neighbour.z))
                    {
                        var neighbourId = ArrayHelper.ToCluster1D(neighbour.x, neighbour.y, neighbour.z);
                        var neighbourType = MapData[neighbourId];

                        if (!BlockDataLookup[neighbourType].IsSolid && LightLevels[neighbourId] < neighbourLightValue)
                        {
                            LightLevels[neighbourId] = neighbourLightValue;
                            if (neighbourLightValue > GeometryConsts.LIGHT_FALL_OFF)
                                LitVoxels.Enqueue(neighbour);
                        }
                    }
                }
            }
        }

        private bool CheckVoxelBounds(int neighbourX, int neighbourY, int neighbourZ)
        {
            if (neighbourX < GeometryConsts.LIGHTS_CLUSTER_MIN || neighbourZ < GeometryConsts.LIGHTS_CLUSTER_MIN || neighbourY < 0)
                return false;

            if (neighbourX >= GeometryConsts.LIGHTS_CLUSTER_MAX || neighbourZ >= GeometryConsts.LIGHTS_CLUSTER_MAX || neighbourY >= GeometryConsts.CHUNK_HEIGHT)
                return false;

            return true;
        }
    }
}