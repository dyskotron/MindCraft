using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace MindCraft.Data.Defs
{
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
        
        [FormerlySerializedAs("Lodes")] public LodeDef[] lodesDef;
        
        // BiomeDefData we can pass to terrain generating jobs
        public BiomeDefData BiomeDefData;
        private NativeArray<int> _terrainCurveSampled;
        private NativeArray<LodeDefData> _lodes;
        private NativeArray<float> _lodeTresholds;

        private void OnEnable()
        {
            //sample terrain curve
            _terrainCurveSampled = CurveHelper.SampleCurve(TerrainCurve, VoxelLookups.CHUNK_HEIGHT);
            
            //populate lodes array
            _lodes = new NativeArray<LodeDefData>(lodesDef.Length, Allocator.Persistent);
            _lodeTresholds = new NativeArray<float>(lodesDef.Length * VoxelLookups.CHUNK_HEIGHT, Allocator.Persistent);
            
            for (var i = 0; i < lodesDef.Length; i++)
            {
                _lodes[i] = new LodeDefData(lodesDef[i]);
                CurveHelper.SampleCurve(lodesDef[i].TresholdByY, _lodeTresholds, i * VoxelLookups.CHUNK_HEIGHT);
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
                               _lodes,
                               _lodeTresholds
                           );
        }

        private void OnDisable()
        {
            _lodes.Dispose();
            _lodeTresholds.Dispose();
            _terrainCurveSampled.Dispose();
        }
    }
    
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
        
        public readonly NativeArray<LodeDefData> Lodes;
        public readonly NativeArray<float> LodeTresholds;

        public BiomeDefData(float frequency,
                            NativeArray<int> terrainCurve,
                            int octaves,
                            float lacunarity,
                            float persistance,
                            float2 offset,
                            byte topBlock,
                            byte middleBlock,
                            byte bottomBlock,
                            NativeArray<LodeDefData> lodes, 
                            NativeArray<float> lodeTresholds)
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
            LodeTresholds = lodeTresholds;
        }

        public float GetLodeTreshold(int i, int y)
        {
            return LodeTresholds[i * VoxelLookups.CHUNK_HEIGHT + y];
        }
    }
}