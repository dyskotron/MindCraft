using System;
using MindCraft.Common.Serialization;
using MindCraft.MapGeneration.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.MapGeneration
{
    [Serializable]
    public struct ChunkCoord : IEquatable<ChunkCoord>, IBinarySerializable
    {
        public static ChunkCoord Left => _leftCoord;
        public static ChunkCoord Right => _rightCoord;
        public static ChunkCoord Forward => _forwardCoord;
        public static ChunkCoord Back => _backCoord;
        
        private static readonly ChunkCoord _leftCoord    = new ChunkCoord(-1, 0);
        private static readonly ChunkCoord _rightCoord   = new ChunkCoord(1, 0);
        private static readonly ChunkCoord _forwardCoord = new ChunkCoord(0, 1);
        private static readonly ChunkCoord _backCoord    = new ChunkCoord(0, -1);
        
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

        public ChunkCoord(int2 position)
        {
            X = position.x;
            Y = position.y;
        }
        
        public override string ToString()
        {
            return $"ChunkChoords({X}.{Y})";
        }

        public static bool operator == (ChunkCoord lhs, ChunkCoord rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }

        public static Vector3 operator * (ChunkCoord a, float d)
        {
            return new Vector3(a.X * d, 0, a.Y * d);
        }

        public static ChunkCoord operator + (ChunkCoord a, ChunkCoord b)
        {
            return new ChunkCoord(a.X + b.X, a.Y + b.Y);
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

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
        }

        public void Deserialize(BinaryReader reader)
        {
            X = reader.ReadInt();
            Y = reader.ReadInt();
        }
    }
}