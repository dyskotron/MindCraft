using Framewerk.StrangeCore;
using MindCraft.Common;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;

namespace MindCraft.Data
{
    public interface IBiomeDefs
    {
        NativeArray<BiomeDefData> BiomeDefData { get; }
        NativeArray<int> TerrainCurvesSampled { get; }
        NativeArray<LodeDefData> Lodes { get; }
        NativeArray<float> LodeThresholds { get; }
        BiomeDef GetDefinitionById(BiomeDefId id);
        BiomeDef[] GetAllDefinitions();
    }

    public enum BiomeDefId
    {
        SnowyMountains,
        HillySavanna,
        Canyons
    }

    public class BiomeDefs : ScriptableObjectDefintions<BiomeDef, BiomeDefId>, IBiomeDefs, IDestroyable
    {
        protected override string Path => ResourcePath.BIOME_DEFS;

        private NativeArray<bool> _transparencyLookup;

        public NativeArray<BiomeDefData> BiomeDefData => _biomeDefData;
        public NativeArray<int> TerrainCurvesSampled => _terrainCurvesSampled;
        public NativeArray<LodeDefData> Lodes => _lodes;
        public NativeArray<float> LodeThresholds => _lodeThresholds;

        private NativeArray<BiomeDefData> _biomeDefData;
        private NativeArray<int> _terrainCurvesSampled;
        private NativeArray<LodeDefData> _lodes;
        private NativeArray<float> _lodeThresholds;


        public override void PostConstruct()
        {
            base.PostConstruct();

            var defs = GetAllDefinitions();

            //TODO: count lode array size properly!
            const int MAX_LODES_PER_DEF = 20;
            const int MAX_THRESHOLD_PER_DEF = MAX_LODES_PER_DEF * VoxelLookups.CHUNK_HEIGHT;

            _biomeDefData = new NativeArray<BiomeDefData>(defs.Length, Allocator.Persistent);
            _terrainCurvesSampled = new NativeArray<int>(defs.Length * VoxelLookups.CHUNK_HEIGHT, Allocator.Persistent);
            _lodes = new NativeArray<LodeDefData>(defs.Length * MAX_LODES_PER_DEF, Allocator.Persistent);
            _lodeThresholds = new NativeArray<float>(defs.Length * MAX_THRESHOLD_PER_DEF, Allocator.Persistent);

            var lodeIndex = 0;

            for (var i = 0; i < defs.Length; i++)
            {
                var def = defs[i];

                //////////// BIOME DEF /////////////
                _biomeDefData[i] = new BiomeDefData(def, lodeIndex);

                ////////// TERRAIN CURVES //////////
                CurveHelper.SampleCurve(def.TerrainCurve, _terrainCurvesSampled, i * VoxelLookups.CHUNK_HEIGHT);

                ////////////// LODES //////////////
                foreach (var lodeDef in def.lodeDefs)
                {
                    _lodes[lodeIndex] = new LodeDefData(lodeDef);
                    
                    CurveHelper.SampleCurve(lodeDef.ThresholdByY, _lodeThresholds,lodeIndex * VoxelLookups.CHUNK_HEIGHT);
                    
                    lodeIndex++;
                }
            }
        }

        public void Destroy()
        {
            TerrainCurvesSampled.Dispose();
            BiomeDefData.Dispose();
            _lodes.Dispose();
            _lodeThresholds.Dispose();
        }
    }
}