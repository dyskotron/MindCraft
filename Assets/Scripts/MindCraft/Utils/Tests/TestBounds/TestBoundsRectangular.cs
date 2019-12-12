using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Tests.TestBounds
{
    public class TestBoundsRectangular : IMindCraftTest
    {
        private int2[] _indexes;
        public const int RADIUS = 100;
        
        public void Init()
        {
            var min = -RADIUS;
            var max = RADIUS;
            
            var i = 0;
            for (var iX = min; iX <= max; iX++)
            {
                for (var iY = min; iY <= max; iY++)
                {
                    TestMethods.Float2TestingMethod(new int2(iX, iY));
                    i++;
                }
            }
            Debug.LogWarning($"<color=\"aqua\">TestBoundsRectangular.Init() : {i}</color>");    
        }

        public void Run()
        {
            var min = -RADIUS;
            var max = RADIUS;
            
            for (var iX = min; iX <= max; iX++)
            {
                for (var iY = min; iY <= max; iY++)
                {
                    TestMethods.Float2TestingMethod(new int2(iX, iY));
                }
            }   
        }
    }
}