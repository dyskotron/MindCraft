using System;
using UnityEngine;

namespace MapGeneration
{
    public struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public int X;
        public int Y;

        public ChunkCoord(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public ChunkCoord(Vector3 position)
        {
            X = Mathf.FloorToInt(position.x / VoxelLookups.CHUNK_SIZE);
            Y = Mathf.FloorToInt(position.z / VoxelLookups.CHUNK_SIZE);
        }
        
        public static bool operator == (ChunkCoord lhs, ChunkCoord rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }

        public static bool operator != (ChunkCoord lhs, ChunkCoord rhs)
        {
            return lhs.X != rhs.X || lhs.Y != rhs.Y;
        }
        
        public override bool Equals(object other)
        {
            return other is ChunkCoord && Equals((ChunkCoord) other);
        }

        public bool Equals(ChunkCoord other)
        {
            return X == other.X && Y == other.Y;
        }
    }
}