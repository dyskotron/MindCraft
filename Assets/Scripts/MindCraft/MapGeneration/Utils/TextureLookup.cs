using Framewerk.StrangeCore;
using MindCraft.Common;
using MindCraft.Data;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.MapGeneration.Utils
{
    //Pregenerates set of 4 uvs per face per voxel type
    public class TextureLookup : IDestroyable
    {
        [Inject] public IBlockDefs BlockDefs { get; set; }

        //Utils
        public const int FACES_PER_VOXEL = 6;
        public const int MAX_BLOCKDEF_COUNT = 128;
        
        private const int BLOCKS_PER_SIDE_UTILS = 3;
        private const int BLOCKS_PER_TEXTURE_UTILS = BLOCKS_PER_SIDE_UTILS * BLOCKS_PER_SIDE_UTILS;
        private const float NORMALIZED_BLOCK_SIZE_UTILS = 1f / BLOCKS_PER_SIDE_UTILS;
        
        //World
        private const int BLOCKS_PER_SIDE_WORLD = 8;
        private const float NORMALIZED_BLOCK_SIZE_WORLD = 1f / BLOCKS_PER_SIDE_WORLD;

        public Vector2[,,] WorldUvLookup;
        public NativeArray<float2> WorldUvLookupNative;
        
        public Vector2[,] UtilsUvLookup;
        public int[] UtilsTextureIndexes = {0, 6, 7, 3, 4, 8, 5, 0, 0};

        [PostConstruct]
        public void PostConstruct()
        {
            //TODO: use actual block def count count - BlockDefs.GetAllDefinitions().Length
            //not that important for lookup tho, and introduces the need to pass block def count to chunk render job, so fuck-it pile for now
            WorldUvLookup = new Vector2[MAX_BLOCKDEF_COUNT, FACES_PER_VOXEL, 4];
            WorldUvLookupNative = new NativeArray<float2>(WorldUvLookup.Length, Allocator.Persistent);

            //TODO fix voxel type determination so we rely on actual enum in the voxel def not array index
            //zero is only marker for no data so we dont need to generate uv lookup
            //(we actually also don't need to do that for Air)

            var blockDefs = BlockDefs.GetAllDefinitions();
            int uvId;
                
            for (var iVoxelType = 1; iVoxelType < blockDefs.Length; iVoxelType++)
            {
                var voxelDef = blockDefs[iVoxelType];

                for (var iFace = 0; iFace < voxelDef.FaceTextures.Length; iFace++)
                {
                    var textureId = voxelDef.FaceTextures[iFace];

                    float x = textureId % BLOCKS_PER_SIDE_WORLD * NORMALIZED_BLOCK_SIZE_WORLD;
                    float y = (int) (textureId / BLOCKS_PER_SIDE_WORLD) * NORMALIZED_BLOCK_SIZE_WORLD;

                    WorldUvLookup[iVoxelType, iFace, 0] = new Vector2(x, y);
                    WorldUvLookup[iVoxelType, iFace, 1] = new Vector2(x, y + NORMALIZED_BLOCK_SIZE_WORLD);
                    WorldUvLookup[iVoxelType, iFace, 2] = new Vector2(x + NORMALIZED_BLOCK_SIZE_WORLD, y);
                    WorldUvLookup[iVoxelType, iFace, 3] = new Vector2(x + NORMALIZED_BLOCK_SIZE_WORLD, y + NORMALIZED_BLOCK_SIZE_WORLD);

                    
                    uvId = ArrayHelper.To1D(iVoxelType, iFace, 0, MAX_BLOCKDEF_COUNT, FACES_PER_VOXEL);
                    WorldUvLookupNative[uvId] = new Vector2(x, y);
                    
                    uvId = ArrayHelper.To1D(iVoxelType, iFace, 1, MAX_BLOCKDEF_COUNT, FACES_PER_VOXEL);
                    WorldUvLookupNative[uvId] = new Vector2(x, y + NORMALIZED_BLOCK_SIZE_WORLD);
                    
                    uvId = ArrayHelper.To1D(iVoxelType, iFace, 2, MAX_BLOCKDEF_COUNT, FACES_PER_VOXEL);
                    WorldUvLookupNative[uvId] = new Vector2(x + NORMALIZED_BLOCK_SIZE_WORLD, y);
                    
                    uvId = ArrayHelper.To1D(iVoxelType, iFace, 3, MAX_BLOCKDEF_COUNT, FACES_PER_VOXEL);
                    WorldUvLookupNative[uvId] = new Vector2(x + NORMALIZED_BLOCK_SIZE_WORLD, y + NORMALIZED_BLOCK_SIZE_WORLD);
                }
            }

            UtilsUvLookup = new Vector2[BLOCKS_PER_TEXTURE_UTILS, 4];

            for (var i = 0; i < BLOCKS_PER_TEXTURE_UTILS; i++)
            {
                float x = i % BLOCKS_PER_SIDE_UTILS * NORMALIZED_BLOCK_SIZE_UTILS;
                float y = (int) (i / BLOCKS_PER_SIDE_UTILS) * NORMALIZED_BLOCK_SIZE_UTILS;

                UtilsUvLookup[i, 0] = new Vector2(x, y);
                UtilsUvLookup[i, 1] = new Vector2(x, y + NORMALIZED_BLOCK_SIZE_UTILS);
                UtilsUvLookup[i, 2] = new Vector2(x + NORMALIZED_BLOCK_SIZE_UTILS, y);
                UtilsUvLookup[i, 3] = new Vector2(x + NORMALIZED_BLOCK_SIZE_UTILS, y + NORMALIZED_BLOCK_SIZE_UTILS);
            }
        }

        public void Destroy()
        {
            WorldUvLookupNative.Dispose();    
        }
    }
}