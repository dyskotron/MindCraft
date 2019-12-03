using Unity.Collections;

namespace MindCraft.Common
{
    public static class NativeArrayUtil
    {
        public static void Copy<T>(NativeArray<T> dest, int destIdx, NativeArray<T> src, int srcIdx, int count)
            where T : struct
        {
            dest.Slice(destIdx, count).CopyFrom(src.Slice(srcIdx, count));
        }
    }
}