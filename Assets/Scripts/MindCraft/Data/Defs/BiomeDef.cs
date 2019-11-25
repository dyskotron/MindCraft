using System;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace MindCraft.Data.Defs
{
    public struct BiomeDefData
    {
        public float TerrainScale;
        
        public NativeArray<int> TerrainCurve { get; set; }
        
        public byte TopBlock;
        public byte MiddleBlock;
        public byte BottomBlock;
        
        public NativeArray<Lode> Lodes;
    }
    
    [CreateAssetMenu(menuName = "Defs/Biome definition")]
    public class BiomeDef : ScriptableObject
    {
        public string Name;

        //invert scale as smaller number for bigger terrain seems counterintuitive + scale to human-easy numbers
        public float TerrainScale => 0.01f / _terrainScale; 
        [FormerlySerializedAs("TerrainScale")] [SerializeField][Range(0,5f)]
        private float _terrainScale;

        public AnimationCurve TerrainCurve;
        
        public Lode[] Lodes;

        public BlockTypeId TopBlock;
        public BlockTypeId MiddleBlock;
        public BlockTypeId BottomBlock;
        
        // BiomeDefData we can pass to terrain generating jobs
        public BiomeDefData BiomeDefData;
        private NativeArray<Lode> _lodes;
        private NativeArray<int> _terrainCurveSampled;

        private void OnEnable()
        {
            BiomeDefData = new BiomeDefData();
            BiomeDefData.TerrainScale = TerrainScale; 

            _lodes = new NativeArray<Lode>(Lodes.Length, Allocator.Persistent);

            //popuplate native array
            for (var i = 0; i < Lodes.Length; i++)
            {
                _lodes[i] = Lodes[i];
            }
            
            //sample terrain curve
            _terrainCurveSampled = new NativeArray<int>(VoxelLookups.CHUNK_HEIGHT, Allocator.Persistent);
            for (var i = 0; i < VoxelLookups.CHUNK_HEIGHT; i++)
            {
                _terrainCurveSampled[i] = (int)(TerrainCurve.Evaluate(i / (float)VoxelLookups.CHUNK_HEIGHT) * VoxelLookups.CHUNK_HEIGHT);
            }

            BiomeDefData.TerrainCurve = _terrainCurveSampled;

            BiomeDefData.TopBlock = (byte)TopBlock;            
            BiomeDefData.MiddleBlock = (byte)MiddleBlock;            
            BiomeDefData.BottomBlock = (byte)BottomBlock;            
            
            BiomeDefData.Lodes = _lodes;
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