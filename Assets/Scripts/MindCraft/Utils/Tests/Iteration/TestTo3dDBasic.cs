using MindCraft.Common;
using MindCraft.MapGeneration.Utils;
using Unity.Mathematics;

namespace MindCraft.Tests.Iteration
{
    public class TestTo3dDBasic : IMindCraftTest
    {
        private int2[] _indexes;
        public const int RADIUS = 100;

        public void Init()
        {
           
            
        }

        public void Run()
        {
            for (var i = 0; i < VoxelLookups.CHUNK_SIZE * VoxelLookups.CHUNK_SIZE * VoxelLookups.CHUNK_HEIGHT; i++)
            {
                ArrayHelper.To3D(i, out int x,out int y,out int z);   
            }
        }
    }
}