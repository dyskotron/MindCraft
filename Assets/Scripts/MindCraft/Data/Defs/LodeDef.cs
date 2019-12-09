using System;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace MindCraft.Data.Defs
{
    public enum LodeAlgorithm
    {
        Perlin2d,
        Perlin3d,
        Simplex2d,
        Simplex3d,
    }
    
    [CreateAssetMenu(menuName = "Defs/Lode definition")]
    public class LodeDef : ScriptableObject
    {
        public BlockTypeId BlockId;

        public BlockMaskId BlockMask;
        
        [Range(0.1f,50f)]
        public float Frequency;
        
        [Range(0,VoxelLookups.CHUNK_HEIGHT)]
        public int MinHeight;
        
        [Range(0,VoxelLookups.CHUNK_HEIGHT)]
        public int MaxHeight;

        public AnimationCurve ThresholdByY;
        
        public LodeAlgorithm Algorithm;
        
        public float3 Offset;
    }
    
    public struct LodeDefData
    {
        public readonly byte BlockId;
        
        public readonly byte BlockMask;
        
        public readonly float Frequency;
        
        public readonly int MinHeight;
        
        public readonly int MaxHeight;
        
        public readonly LodeAlgorithm Algorithm;
        
        public readonly float3 Offset;
        
        public LodeDefData(LodeDef lodeDef)
        {
            BlockId = (byte)lodeDef.BlockId;
            BlockMask = (byte)lodeDef.BlockMask;
            Frequency = lodeDef.Frequency * 0.01f;
            MinHeight = lodeDef.MinHeight;
            MaxHeight = lodeDef.MaxHeight;
            Offset = lodeDef.Offset;
            Algorithm = lodeDef.Algorithm;
        }
    }
}