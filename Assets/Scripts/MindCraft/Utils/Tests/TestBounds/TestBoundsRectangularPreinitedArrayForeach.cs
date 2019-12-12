using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Tests.TestBounds
{
    public class TestBoundsRectangularPreinitedArrayForeach : IMindCraftTest
    {
        private int2[] _indexes;
        public const int RADIUS = 100;
        
        public void Init()
        {
            var indexes = new List<int2>();

            var min = -RADIUS;
            var max = RADIUS;

            var radiusSqr = RADIUS * RADIUS;

            for (var iX = min; iX < max; iX++)
            {
                for (var iY = min; iY < max; iY++)
                {   
                    indexes.Add(new int2(iX, iY));  
                }
            }

            _indexes = indexes.ToArray();

            Debug.LogWarning($"<color=\"aqua\">TestBoundsRectangularPreinitedArrayForeach.Init() : {_indexes.Length}</color>");
        }

        public void Run()
        {
            foreach (var t in _indexes)
            {
                TestMethods.Float2TestingMethod(t);
            }
        }
    }
}