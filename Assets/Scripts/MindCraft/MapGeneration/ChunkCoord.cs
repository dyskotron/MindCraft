using System;
using MindCraft.Common.Serialization;
using MindCraft.MapGeneration.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.MapGeneration
{
    public static class ChunkCoord
    {
        public static int2 Left => LeftCoord;
        public static int2 Right => RightCoord;
        public static int2 Front => FrontCoord;
        public static int2 Back => BackCoord;
        public static int2 LeftFront => LeftFrontCoord;
        public static int2 RightFront => RightFrontCoord;
        public static int2 LeftBack => LeftBackCoord;
        public static int2 RightBack => RightBackCoord;
        
        private static readonly int2 LeftCoord  = new int2(-1, 0);
        private static readonly int2 RightCoord = new int2(1, 0);
        private static readonly int2 FrontCoord = new int2(0, 1);
        private static readonly int2 BackCoord  = new int2(0, -1);
        
        private static readonly int2 LeftFrontCoord  = new int2(-1, 1);
        private static readonly int2 RightFrontCoord = new int2(1, 1);
        private static readonly int2 LeftBackCoord   = new int2(-1, -1);
        private static readonly int2 RightBackCoord  = new int2(1, -1);
    }
}