using MindCraft.Common;
using MindCraft.MapGeneration.Utils;
using Unity.Mathematics;

namespace MindCraft.Tests.Iteration
{
    public class TestTo1dBitwise : IMindCraftTest
    {
        private int2[] _indexes;
        public const int RADIUS = 100;

        public void Init()
        {
           
            
        }

        public void Run()
        {
            for (var x = 0; x < GeometryConsts.CHUNK_SIZE; x++)
            {
                for (var y= 0; y < GeometryConsts.CHUNK_SIZE; y++)
                {
                    for (var z = 0; z < GeometryConsts.CHUNK_SIZE; z++)
                    {
                        ArrayHelper.To1DMap(x, y, z);
                    }
                }    
            }
        }
    }
}