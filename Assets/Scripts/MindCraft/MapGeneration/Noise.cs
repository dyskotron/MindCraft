using Unity.Mathematics;

namespace MindCraft.MapGeneration
{
    public static class Noise
    {
        public static float Get2DPerlin(float x, float y, float offset, float scale)
        {
            return 0.5f + 0.5f * noise.cnoise(new float2( + x * scale + offset, -y * scale + offset));
        }

        public static bool Get3DPerlin(float x, float y, float z, float offset, float scale, float threshold)
        {
            return threshold < (0.5f + 0.5f * noise.cnoise( new float3(x   * scale + offset, 
                                                           y  * scale + offset, 
                                                           z  * scale + offset)));
        }
    }
}