using System;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Data.Defs
{
    public struct BiomeDefData
    {
        public readonly float Frequency;

        public readonly NativeArray<int> TerrainCurve;
        public readonly int Octaves;
        public readonly float Lacunarity;
        public readonly float Persistance;
        public readonly float2 Offset;

        public readonly byte TopBlock;
        public readonly byte MiddleBlock;
        public readonly byte BottomBlock;
        
        public readonly NativeArray<Lode> Lodes;

        public BiomeDefData(float frequency,
                            NativeArray<int> terrainCurve, 
                            int octaves,
                            float lacunarity,
                            float persistance,
                            float2 offset,
                            byte topBlock,
                            byte middleBlock,
                            byte bottomBlock,
                            NativeArray<Lode> lodes)
        {
            Frequency = frequency;
            TerrainCurve = terrainCurve;
            Octaves = octaves;
            Lacunarity = lacunarity;
            Persistance = persistance;
            Offset = offset;
            TopBlock = topBlock;
            MiddleBlock = middleBlock;
            BottomBlock = bottomBlock;
            Lodes = lodes;
        }
    }
    
    [CreateAssetMenu(menuName = "Defs/Biome definition")]
    public class BiomeDef : ScriptableObject
    {
        public string Name;

        public float Frequency => _frequency * 0.01f;
        [SerializeField] [Range(0, 5f)]
        private float _frequency = 1;
        
        public AnimationCurve TerrainCurve;

        [Range(0,10)]
        public int Octaves = 3;
        [Range(1f, 10f)]
        public float Lacunarity = 2.1f;
        [Range(0.1f, 1f)]
        public float Persistance = 0.5f;
        
        public float2 Offset;
        
        public BlockTypeId TopBlock;
        public BlockTypeId MiddleBlock;
        public BlockTypeId BottomBlock;
        
        public Lode[] Lodes;
        
        // BiomeDefData we can pass to terrain generating jobs
        public BiomeDefData BiomeDefData;
        private NativeArray<Lode> _lodes;
        private NativeArray<int> _terrainCurveSampled;

        private void OnEnable()
        {
            //sample terrain curve
            _terrainCurveSampled = new NativeArray<int>(VoxelLookups.CHUNK_HEIGHT, Allocator.Persistent);
            for (var i = 0; i < VoxelLookups.CHUNK_HEIGHT; i++)
            {
                _terrainCurveSampled[i] = (int)(TerrainCurve.Evaluate(i / (float)VoxelLookups.CHUNK_HEIGHT) * VoxelLookups.CHUNK_HEIGHT);
            }
            
            //populate lodes array
            _lodes = new NativeArray<Lode>(Lodes.Length, Allocator.Persistent);
            for (var i = 0; i < Lodes.Length; i++)
            {
                _lodes[i] = Lodes[i];
            }
            
            BiomeDefData = new BiomeDefData(
                               Frequency,
                               _terrainCurveSampled,
                               Octaves,
                               Lacunarity,
                               Persistance,
                               Offset,
                               (byte)TopBlock,
                               (byte)MiddleBlock,
                               (byte)BottomBlock,
                               _lodes
                           );
        }

        private void OnDisable()
        {
            _lodes.Dispose();
            _terrainCurveSampled.Dispose();
        }
    }

    public enum ScaleTresholdByHeight
    {
        None,
        HighestTop, //highest scale is at Lode max height 
        HighestBottom //highest scale is at top of Lode min height
    }

    [Serializable]
    public struct Lode
    {
        public int HeightRange => MaxHeight - MinHeight;

        public byte BlockMask => (byte) _blockMask;
        
        [Range(0,VoxelLookups.CHUNK_HEIGHT)]
        public int MinHeight;
        
        [Range(0,VoxelLookups.CHUNK_HEIGHT)]
        public int MaxHeight;

        public float Scale => 0.01f / _scale; //invert + multiply scale
        
        [Range(0,1f)]
        public float Treshold;

        public ScaleTresholdByHeight ScaleTresholdByHeight;
        
        [Range(0,1f)]
        public float Offset;
        
        public byte BlockId => (byte)_blockId;
        
        
        [SerializeField]
        private BlockTypeId _blockId;

        [SerializeField]
        private BlockMaskId _blockMask;
        
        [SerializeField][Range(0,2f)]
        private float _scale;
    }
}