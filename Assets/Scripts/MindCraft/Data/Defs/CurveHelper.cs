using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using UnityEngine;

namespace MindCraft.Data.Defs
{
    public static class CurveHelper
    {
        public static NativeArray<int> SampleCurve(AnimationCurve curve, int range)
        {
            var terrainCurveSampled = new NativeArray<int>(range, Allocator.Persistent);
            
            for (var i = 0; i < VoxelLookups.CHUNK_HEIGHT; i++)
            {
                terrainCurveSampled[i] = (int)(curve.Evaluate(i / (float)range) * range);
            }
            
            return terrainCurveSampled; 
        }
        
        public static void SampleCurve(AnimationCurve curve, NativeArray<float> destination, int startPosition)
        {
            for (var i = 0; i < VoxelLookups.CHUNK_HEIGHT; i++)
            {
                destination[startPosition + i] = (curve.Evaluate(i / (float)VoxelLookups.CHUNK_HEIGHT));
            }
        }
    }
}