using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Tests.TestBounds
{
    public class TestBoundsRadial : IMindCraftTest
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
                    if (IsQuadWithinCircle(iX, iY, radiusSqr))
                    {
                        indexes.Add(new int2(iX, iY));
                    }
                }
            }

            _indexes = indexes.ToArray();

            Debug.LogWarning($"<color=\"aqua\">TestBoundsRadial.Init() : {_indexes.Length}</color>");
        }

        public void Run()
        {
            for (var i = 0; i < _indexes.Length; i++)
            {
                TestMethods.Float2TestingMethod(_indexes[i]);
            }
        }

        /// <summary>
        /// check if any of quad's corners is within circle.
        /// Quads considered 1 x 1 size with its position defined by 0,0 coords
        /// </summary>
        /// <param name="x">x position of quad</param>
        /// <param name="y">y position of quad</param>
        /// <param name="radiusSqr">pow2 radius</param>
        /// <returns>True of any of corners lies within circle</returns>
        private bool IsQuadWithinCircle(int x , int y, int radiusSqr)
        {
            if( (x * x + y * y) <= radiusSqr)
                return true;
            
            if( ((x + 1) * (x + 1) + y * y) <= radiusSqr)
                return true;
            
            if( ((x + 1) * (x + 1) + (y + 1) * (y + 1)) <= radiusSqr)
                return true;
            
            if( (x * x + (y + 1) * (y + 1)) <= radiusSqr)
                return true;

            return false;
        }
    }
}