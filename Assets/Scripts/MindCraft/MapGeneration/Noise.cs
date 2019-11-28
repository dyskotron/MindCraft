using Unity.Mathematics;

namespace MindCraft.MapGeneration
{
    public static class Noise
    {
        public static float Get2DPerlin(float x, float y, int octaves, float lacunarity, float persistance, float scale, float offset)
        {
            float value = 0;
            float currentPersistance = 1f;
            float totalPersistance = 0;
            for (var i = 0; i < octaves; i++)
            {
                totalPersistance += currentPersistance;
                value += currentPersistance * noise.cnoise(new float2(+x * scale + offset, -y * scale + offset)); 
                scale *= lacunarity;
                currentPersistance *= persistance;
            }

            return 0.5f + 0.5f * value / totalPersistance; 
        }

        public static bool Get3DPerlin(float x, float y, float z, float offset, float scale, float threshold)
        {
            return threshold < (0.5f + 0.5f * noise.cnoise( new float3(x   * scale + offset, 
                                                           y  * scale + offset, 
                                                           z  * scale + offset)));
        }
    }
}