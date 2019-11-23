using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Tests.TestBounds
{
    public class TestBoundsRectangularPreinitedArray : IMindCraftTest
    {
        private int2[] _indexes;
        public const int RADIUS = 100;
        
        public void Init()
        {
            var indexes = new List<int2>();

            var min = -RADIUS;
            var max = RADIUS;

            var radiusSqr = RADIUS * RADIUS;

            for (var iX = min; iX <= max; iX++)
            {
                for (var iY = min; iY <= max; iY++)
                {   
                    indexes.Add(new int2(iX, iY));  
                }
            }

            _indexes = indexes.ToArray();

            Debug.LogWarning($"<color=\"aqua\">TestBoundsRectangularPreinitedArray.Init() : {_indexes.Length}</color>");
        }

        public void Run()
        {
            for (var index = 0; index < _indexes.Length; index++)
            {
                var t = _indexes[index];
                TestMethods.Float2TestingMethod(t);
            }
        }
    }
}