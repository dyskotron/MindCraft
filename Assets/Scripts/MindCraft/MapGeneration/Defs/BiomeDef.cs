using System;
using MindCraft.MapGeneration.Lookup;
using UnityEngine;
using UnityEngine.Serialization;

namespace MindCraft.MapGeneration.Defs
{
    [CreateAssetMenu(menuName = "MapGeneration/Biome definition")]
    public class BiomeDef : ScriptableObject
    {
        public string Name;
        [Range(0,1f)]
        public float TerrainScale;
        [FormerlySerializedAs("TerrainGround")] public int TerrainMin;
        public int TerrainMax;
        public int TerrainHeight => TerrainMax - TerrainMin;

        public Lode[] Lodes;
    }

    public enum ScaleTresholdByHeight
    {
        None,
        HighestTop, //highest scale is at Lode max height 
        HighestBottom //highest scale is at top of Lode min height
    }

    [Serializable]
    public class Lode
    {
        public string Name;

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
        private VoxelTypeId _blockId;

        public byte BlockId => (byte)_blockId;
    }
}