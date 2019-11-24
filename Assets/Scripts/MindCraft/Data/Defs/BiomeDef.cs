using System;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace MindCraft.Data.Defs
{
    public struct BiomeDefData
    {
        public int TerrainMin;
        public int TerrainMax;
        public int TerrainHeight;
        public float TerrainScale;
        
        public NativeArray<Lode> Lodes;
    }
    
    [CreateAssetMenu(menuName = "Defs/Biome definition")]
    public class BiomeDef : ScriptableObject
    {
        public string Name;
        [Range(0,1f)]
        public float TerrainScale;
        [FormerlySerializedAs("TerrainGround")] public int TerrainMin;
        public int TerrainMax;
        public int TerrainHeight => TerrainMax - TerrainMin;

        public Lode[] Lodes;

        public BiomeDefData BiomeDefData;
        
        private NativeArray<Lode> _lodes;

        private void OnEnable()
        {
            BiomeDefData = new BiomeDefData();
            BiomeDefData.TerrainMin = TerrainMin;
            BiomeDefData.TerrainMax = TerrainMax;
            BiomeDefData.TerrainHeight = TerrainHeight;
            BiomeDefData.TerrainScale = TerrainScale;

            _lodes = new NativeArray<Lode>(Lodes.Length, Allocator.Persistent);

            for (var i = 0; i < Lodes.Length; i++)
            {
                _lodes[i] = Lodes[i];
            }

            BiomeDefData.Lodes = _lodes;
        }

        private void OnDisable()
        {
            _lodes.Dispose();
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
        
        [Range(0,VoxelLookups.CHUNK_HEIGHT)]
        public int MinHeight;
        
        [Range(0,VoxelLookups.CHUNK_HEIGHT)]
        public int MaxHeight;
        
        [Range(0,1f)]
        public float Scale;
        
        [Range(0,1f)]
        public float Treshold;

        public ScaleTresholdByHeight ScaleTresholdByHeight;
        
        [Range(0,1f)]
        public float Offset;
        
        [SerializeField]
        private BlockTypeId _blockId;

        public byte BlockId => (byte)_blockId;
    }
}