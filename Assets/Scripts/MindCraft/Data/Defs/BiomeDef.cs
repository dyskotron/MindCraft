using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Data.Defs
{
    [CreateAssetMenu(menuName = "Defs/Biome definition")]
    public class BiomeDef : DefinitionSO<BiomeDefId>
    {
        [Range(0f, 1f)] 
        public float Temperature;
        
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
        
        public LodeDef[] lodeDefs;
        
        private NativeArray<LodeDefData> _lodes;
        private NativeArray<float> _lodeTresholds;

        private void OnEnable()
        {
            //populate lodes array
            _lodes = new NativeArray<LodeDefData>(lodeDefs.Length, Allocator.Persistent);
            _lodeTresholds = new NativeArray<float>(lodeDefs.Length * GeometryConsts.CHUNK_HEIGHT, Allocator.Persistent);
            
            for (var i = 0; i < lodeDefs.Length; i++)
            {
                _lodes[i] = new LodeDefData(lodeDefs[i]);
                CurveHelper.SampleCurve(lodeDefs[i].ThresholdByY, _lodeTresholds, i * GeometryConsts.CHUNK_HEIGHT);
            }
        }

        private void OnDisable()
        {
            _lodes.Dispose();
            _lodeTresholds.Dispose();
        }
    }
    
    public struct BiomeDefData
    {
        public readonly float Temperature;
        public readonly float Frequency;
        //public readonly NativeArray<int> TerrainCurve;
        public readonly int TerrainCurveStartPos;
        public readonly int LodesStartPos;
        public readonly int LodesCount;
        
        public readonly int Octaves;
        public readonly float Lacunarity;
        public readonly float Persistance;
        public readonly float2 Offset;

        public readonly byte TopBlock;
        public readonly byte MiddleBlock;
        public readonly byte BottomBlock;
        
        //public readonly NativeArray<LodeDefData> Lodes;
        //public readonly NativeArray<float> LodeTresholds;

        public BiomeDefData(BiomeDef def, int lodeStartPos)
        {
            Temperature = def.Temperature;
            Frequency = def.Frequency;
            Octaves = def.Octaves;
            Lacunarity = def.Lacunarity;
            Persistance = def.Persistance;
            Offset = def.Offset;
            TopBlock = (byte)def.TopBlock;
            MiddleBlock = (byte)def.MiddleBlock;
            BottomBlock = (byte)def.BottomBlock;
            TerrainCurveStartPos = (int) def.Id * GeometryConsts.CHUNK_HEIGHT;
            
            LodesStartPos = lodeStartPos;
            LodesCount = def.lodeDefs.Length;
        }
    }
}