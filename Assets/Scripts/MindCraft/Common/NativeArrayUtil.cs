using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Common
{
    /// memcpy solution for coping native to managed collections based on Lotte's work:
    /// https://gist.github.com/LotteMakesStuff/c2f9b764b15f74d14c00ceb4214356b4
    public static class NativeArrayUtil
    {
        public static void Copy<T>(NativeArray<T> dest, int destIdx, NativeArray<T> src, int srcIdx, int count)
            where T : struct
        {
            dest.Slice(destIdx, count).CopyFrom(src.Slice(srcIdx, count));
        }
        
        public static unsafe Vector3[] NativeFloat3ToManagedVector3(NativeArray<float3> nativeBuffer)
        {
            Vector3[] vertexArray = new Vector3[nativeBuffer.Length];
            
            // pin the target vertex array and get a pointer to it
            fixed (void* vertexArrayPointer = vertexArray)
            {
                // memcopy the native array over the top
                UnsafeUtility.MemCpy(vertexArrayPointer, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(nativeBuffer), vertexArray.Length * (long) UnsafeUtility.SizeOf<float3>());
            }
            
            return vertexArray;
        }
        
        public static unsafe Vector2[] NativeFloat2ToManagedVector2(NativeArray<float2> nativeBuffer)
        {
            Vector2[] vertexArray = new Vector2[nativeBuffer.Length];
            
            // pin the target vertex array and get a pointer to it
            fixed (void* vertexArrayPointer = vertexArray)
            {
                // memcopy the native array over the top
                UnsafeUtility.MemCpy(vertexArrayPointer, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(nativeBuffer), vertexArray.Length * (long) UnsafeUtility.SizeOf<float2>());
            }
            
            return vertexArray;
        }

        public static  Color[] NativeFloatToManagedColor(NativeList<float> nl)
        {
            var colors = new Color[nl.Length];

            for (var i = 0; i < nl.Length; i++)
            {
                colors[i] = new Color(0, 0, 0, nl[i]);
            }

            return colors;
        }
    }
}