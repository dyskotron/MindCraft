using MindCraft.Common;
using MindCraft.MapGeneration.Utils;
using Unity.Mathematics;

namespace MindCraft.Tests.Iteration
{
    public class TestTo1dBasic : IMindCraftTest
    {
        private int2[] _indexes;
        public const int RADIUS = 100;

        public void Init()
        {
           
            
        }

        public void Run()
        {
            for (var x = 0; x < VoxelLookups.CHUNK_SIZE; x++)
            {
                for (var y= 0; y < VoxelLookups.CHUNK_SIZE; y++)
                {
                    for (var z = 0; z < VoxelLookups.CHUNK_SIZE; z++)
                    {
                        var i = ArrayHelper.To1D(x, y, z);
                    }
                }    
            }
        }
    }
}