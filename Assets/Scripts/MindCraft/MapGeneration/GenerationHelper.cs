using System;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.MapGeneration.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.MapGeneration
{
    public static class GenerationHelper
    {
        /// <summary>
        /// Generates single voxel on world coordinates - called only by WorldModel when we truly want to generate
        /// and know that coordinates are valid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="biomeDefs"></param>
        /// <param name="offsets"></param>
        /// <param name="terrainCurves"></param>
        /// <param name="lodes"></param>
        /// <returns></returns>
        public static byte GenerateVoxel(int x, int y, int z, NativeArray<BiomeDefData> biomeDefs, NativeArray<float2> offsets, NativeArray<int> terrainCurves, NativeArray<LodeDefData> lodes, NativeArray<float> tresholds)
        {
            //TODO: generate whole column, so height biome and determination etc is not called every voxel!


            // ======== STATIC RULES ========

            if (y == 0)
                return BlockTypeByte.GREY_STONE;

            // ======== BIOME PASS ========

            BiomeDefData biome = biomeDefs[0];

            float closest = 300;

            float totalHeight = 0;
            float totalWeight = 0;

            //hardcoded noise function to get biome temperature
            var temperature = 0.5f + noise.snoise(new float2(x * 0.0012523f, z * 0.000932f)) / 2f;
            temperature += 0.1f * (0.5f + noise.snoise(new float2(x * 0.042523f, z * 0.03932f)) / 2f);
            temperature /= 1.1f;
            
            //var cellular = noise.cellular2x2(new float2(x * 0.022523f, z * 0.0232f));
            //var temperature = cellular.x;
            //var temperature = 0.5f + noise.cnoise(noise.cellular(new float2(x * 0.0062523f , z  * 0.00432f ))) / 2f;

            //var id = (byte) math.max((cellular.y * (BlockTypeByte.TypeCount - 1)), 1);
            //id = (byte)math.clamp(id, 2, BlockTypeByte.TypeCount - 1);

            //return y > temperature * VoxelLookups.CHUNK_HEIGHT ? BlockTypeByte.AIR : BlockTypeByte.STONE;

            float difference = 0;

            for (var i = 0; i < biomeDefs.Length; i++)
            {
                var currentDef = biomeDefs[i];
                //var temperatureMap = (noise.cellular(new float2(x * currentDef.Frequency, z * currentDef.Frequency)));
                //var temperature = 0.5f + temperatureMap.x / 2;

                // var biomeTemperature = i / (float) biomeDefs.Length;

                difference = math.abs(currentDef.Temperature - temperature);

                var weight = (float) Math.Pow(biomeDefs.Length - math.min(difference, biomeDefs.Length), 50);

                totalHeight += weight * GetTerrainHeight(x, z, currentDef, offsets, terrainCurves);
                totalWeight += weight;

                if (difference < closest)
                {
                    closest = difference;
                    biome = currentDef;
                }
            }

            var terrainHeight = Mathf.FloorToInt(totalHeight / totalWeight);
            //var terrainHeight =GetTerrainHeight(x, z, biome, offsets, terrainCurves);

            // ======== BASIC PASS ========

            byte voxelValue = 0;
            //everything higher then terrainHeight is air
            if (y >= terrainHeight)
                return BlockTypeByte.AIR;

            //top voxels are grass
            if (y == terrainHeight - 1)
                voxelValue = BlockMaskByte.TOP;
            //3 voxels under grass are dirt
            else if (y >= terrainHeight - 4)
                voxelValue = BlockMaskByte.MIDDLE;
            //rest is rock
            else
                voxelValue = BlockMaskByte.BOTTOM;

            //LODES PASS
            bool lodesPassResolved = false;

            for (var i = biome.LodesStartPos; i < biome.LodesCount; i++)
            {
                var lode = lodes[i];

                if ((lode.BlockMask & voxelValue) == 0)
                    continue; //try next lode

                if (y > lode.MinHeight && y < lode.MaxHeight)
                {
                    var treshold = tresholds[biome.LodesStartPos + i * VoxelLookups.CHUNK_HEIGHT + y];

                    //tresholds  biomeDef.GetLodeTreshold(i, y);

                    //if (Noise.Get3DPerlin(x, y, z, lode.Offset, lode.Frequency, treshold))
                    //TODO make noise 2d/3d lode option
                    if (Noise.GetLodePresence(lode.Algorithm, x, y, z, lode.Offset, lode.Frequency, treshold))
                    {
                        voxelValue = lode.BlockId;
                        lodesPassResolved = true;
                        break; //We found our block - continue to next pass!
                    }
                }
            }


            //if no lode was applied, show basic biome block for given placeholder
            if (!lodesPassResolved)
            {
                switch (voxelValue)
                {
                    case BlockMaskByte.TOP:
                        voxelValue = biome.TopBlock;
                        break;

                    case BlockMaskByte.MIDDLE:
                        voxelValue = biome.MiddleBlock;
                        break;

                    case BlockMaskByte.BOTTOM:
                        voxelValue = biome.BottomBlock;
                        break;
                }
            }

            return voxelValue;
        }

        /// <summary>
        /// Generates single voxel on world coordinates - called only by WorldModel when we truly want to generate
        /// and know that coordinates are valid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="terrainHeight"></param>
        /// <param name="biome"></param>
        /// <param name="lodes"></param>
        /// <param name="lodeTresholds"></param>
        /// <returns></returns>
        public static byte GenerateVoxel(int x, int y, int z, int terrainHeight, BiomeDefData biome, NativeArray<LodeDefData> lodes, NativeArray<float> lodeTresholds)
        {
            //TODO: generate whole column, so height biome and determination etc is not called every voxel!


            // ======== STATIC RULES ========

            if (y == 0)
                return BlockTypeByte.GREY_STONE;

            
            // ======== BASIC PASS ========

            byte voxelValue = 0;
            //everything higher then terrainHeight is air
            if (y >= terrainHeight)
                return BlockTypeByte.AIR;

            //top voxels are grass
            if (y == terrainHeight - 1)
                voxelValue = BlockMaskByte.TOP;
            //3 voxels under grass are dirt
            else if (y >= terrainHeight - 4)
                voxelValue = BlockMaskByte.MIDDLE;
            //rest is rock
            else
                voxelValue = BlockMaskByte.BOTTOM;

            //LODES PASS
            bool lodesPassResolved = false;

            for (var i = biome.LodesStartPos; i < biome.LodesCount; i++)
            {
                var lode = lodes[i];

                if ((lode.BlockMask & voxelValue) == 0)
                    continue; //try next lode

                if (y > lode.MinHeight && y < lode.MaxHeight)
                {
                    var treshold = lodeTresholds[biome.LodesStartPos + i * VoxelLookups.CHUNK_HEIGHT + y];

                    //tresholds  biomeDef.GetLodeTreshold(i, y);

                    //if (Noise.Get3DPerlin(x, y, z, lode.Offset, lode.Frequency, treshold))
                    //TODO make noise 2d/3d lode option
                    if (Noise.GetLodePresence(lode.Algorithm, x, y, z, lode.Offset, lode.Frequency, treshold))
                    {
                        voxelValue = lode.BlockId;
                        lodesPassResolved = true;
                        break; //We found our block - continue to next pass!
                    }
                }
            }


            //if no lode was applied, show basic biome block for given placeholder
            if (!lodesPassResolved)
            {
                switch (voxelValue)
                {
                    case BlockMaskByte.TOP:
                        voxelValue = biome.TopBlock;
                        break;

                    case BlockMaskByte.MIDDLE:
                        voxelValue = biome.MiddleBlock;
                        break;

                    case BlockMaskByte.BOTTOM:
                        voxelValue = biome.BottomBlock;
                        break;
                }
            }

            return voxelValue;
        }
        
        /// <summary>
        /// Generates single voxel on world coordinates - called only by WorldModel when we truly want to generate
        /// and know that coordinates are valid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="biomeDefs"></param>
        /// <param name="offsets"></param>
        /// <param name="terrainCurves"></param>
        /// <param name="lodes"></param>
        /// <returns></returns>
        public static byte GetBiomeForColumn(int x, int z, NativeArray<BiomeDefData> biomeDefs, NativeArray<float2> offsets, NativeArray<int> terrainCurves, out int terrainHeight)
        {
            // ======== BIOME PASS ========

            BiomeDefData biome = biomeDefs[0];
            int biomeId = 0;

            float closest = 300;

            float totalHeight = 0;
            float totalWeight = 0;

            //hardcoded noise function to get biome temperature
            var temperature = 0.5f + noise.snoise(new float2(x * 0.0012523f, z * 0.000932f)) / 2f;
            temperature += 0.1f * (0.5f + noise.snoise(new float2(x * 0.042523f, z * 0.03932f)) / 2f);
            temperature /= 1.1f;
            
            //var cellular = noise.cellular2x2(new float2(x * 0.022523f, z * 0.0232f));
            //var temperature = cellular.x;
            //var temperature = 0.5f + noise.cnoise(noise.cellular(new float2(x * 0.0062523f , z  * 0.00432f ))) / 2f;

            //var id = (byte) math.max((cellular.y * (BlockTypeByte.TypeCount - 1)), 1);
            //id = (byte)math.clamp(id, 2, BlockTypeByte.TypeCount - 1);

            //return y > temperature * VoxelLookups.CHUNK_HEIGHT ? BlockTypeByte.AIR : BlockTypeByte.STONE;

            float difference = 0;

            for (var i = 0; i < biomeDefs.Length; i++)
            {
                var currentDef = biomeDefs[i];
                //var temperatureMap = (noise.cellular(new float2(x * currentDef.Frequency, z * currentDef.Frequency)));
                //var temperature = 0.5f + temperatureMap.x / 2;

                // var biomeTemperature = i / (float) biomeDefs.Length;

                difference = math.abs(currentDef.Temperature - temperature);

                var weight = (float) Math.Pow(biomeDefs.Length - math.min(difference, biomeDefs.Length), 50);

                totalHeight += weight * GetTerrainHeight(x, z, currentDef, offsets, terrainCurves);
                totalWeight += weight;

                if (difference < closest)
                {
                    closest = difference;
                    biome = currentDef;
                    biomeId = i;
                }
            }

            terrainHeight = Mathf.FloorToInt(totalHeight / totalWeight);
            //var terrainHeight = GetTerrainHeight(x, z, biome, offsets, terrainCurves);

            return (byte)biomeId;
        }
        
        
        public static int GetTerrainHeight(int x, int y, BiomeDefData biomeDef, NativeArray<float2> offsets, NativeArray<int> terrainCurve)
        {
            var sampleNoise = Noise.GetHeight(x, y, biomeDef.Octaves, biomeDef.Lacunarity, biomeDef.Persistance, biomeDef.Frequency, offsets, biomeDef.Offset);
            var heightFromNoise = Mathf.FloorToInt(VoxelLookups.CHUNK_HEIGHT * sampleNoise);
            return math.clamp(terrainCurve[biomeDef.TerrainCurveStartPos + heightFromNoise], 0, VoxelLookups.CHUNK_HEIGHT - 1);
        }
    }
}