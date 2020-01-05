using MindCraft.Common;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MindCraft.View.Chunk
{
    public class ComputeMeshData
    {
        public int2 Coords { get; private set; }

        public bool IsRendering { get; set; }

        //JOB INPUT
        public NativeArray<byte> MapWithNeighbours { get; private set; }
        public NativeArray<float> LightMapWithNeighbours { get; private set; }
        public NativeArray<float> LightLevelMap { get; private set; }

        public NativeQueue<int3> LitVoxels { get; private set; }

        //JOB OUTPUT
        public NativeList<float3> Vertices { get; private set; }
        public NativeList<float3> Normals { get; private set; }
        public NativeList<int> Triangles { get; private set; }
        public NativeList<float2> Uvs { get; private set; }
        public NativeList<float> Colors { get; private set; }
        public JobHandle JobHandle { get; set; }

        public ComputeMeshData()
        {
            MapWithNeighbours = new NativeArray<byte>(GeometryLookups.VOXELS_PER_CLUSTER, Allocator.Persistent);
            LightMapWithNeighbours = new NativeArray<float>(GeometryLookups.VOXELS_PER_CLUSTER, Allocator.Persistent);
            LightLevelMap = new NativeArray<float>(GeometryLookups.VOXELS_PER_CHUNK, Allocator.Persistent);
            LitVoxels = new NativeQueue<int3>(Allocator.Persistent);

            Vertices = new NativeList<float3>(Allocator.Persistent);
            Normals = new NativeList<float3>(Allocator.Persistent);
            Triangles = new NativeList<int>(Allocator.Persistent);
            Uvs = new NativeList<float2>(Allocator.Persistent);
            Colors = new NativeList<float>(Allocator.Persistent);
        }

        public void Dispose()
        {
            MapWithNeighbours.Dispose();
            LightMapWithNeighbours.Dispose();
            LightLevelMap.Dispose();
            LitVoxels.Dispose();

            Vertices.Dispose();
            Normals.Dispose();
            Triangles.Dispose();
            Uvs.Dispose();
            Colors.Dispose();
        }

        public void Complete()
        {
            JobHandle.Complete();
            IsRendering = false;
        }

        public void SetCoords(int2 coords)
        {
            Coords = coords;
        }
    }
}