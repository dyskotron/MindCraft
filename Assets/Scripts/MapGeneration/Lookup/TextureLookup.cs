using MapGeneration.Defs;
using UnityEngine;

namespace MapGeneration.Lookup
{
    //Pregenerate set of 4 uvs per face per voxel type
    public class TextureLookup
    {
        public VoxelDef[] VoxelDefs => Locator.World.VoxelDefs;

        //World
        private const int BLOCKS_PER_SIDE_WORLD = 4;
        private const float NORMALIZED_BLOCK_SIZE_WORLD = 1f / BLOCKS_PER_SIDE_WORLD;

        //Utils
        private const int BLOCKS_PER_SIDE_UTILS = 3;
        private const int BLOCKS_PER_TEXTURE_UTILS = BLOCKS_PER_SIDE_UTILS * BLOCKS_PER_SIDE_UTILS;
        private const float NORMALIZED_BLOCK_SIZE_UTILS = 1f / BLOCKS_PER_SIDE_UTILS;

        public Vector2[,,] WorldUvLookup;
        
        public Vector2[,] UtilsUvLookup;
        public int[] UtilsTextureIndexes = new[] {0, 6, 7, 3, 4, 8, 5, 0, 0};

        public void Init()
        {
            WorldUvLookup = new Vector2[VoxelDefs.Length, 6, 4];

            //TODO fix voxel type determination so we rely on actual enum in the voxel def not array index
            //zero is only marker for no data so we dont need to generate uv lookup
            //(we actually also don't need to do that for Air)
            for (var iVoxelType = 1; iVoxelType < VoxelDefs.Length; iVoxelType++)
            {
                var voxelDef = VoxelDefs[iVoxelType];

                for (var iFace = 0; iFace < voxelDef.FaceTextures.Length; iFace++)
                {
                    var textureId = voxelDef.FaceTextures[iFace];

                    float x = textureId % BLOCKS_PER_SIDE_WORLD * NORMALIZED_BLOCK_SIZE_WORLD;
                    float y = (int) (textureId / BLOCKS_PER_SIDE_WORLD) * NORMALIZED_BLOCK_SIZE_WORLD;

                    WorldUvLookup[iVoxelType, iFace, 0] = new Vector2(x, y);
                    WorldUvLookup[iVoxelType, iFace, 1] = new Vector2(x, y + NORMALIZED_BLOCK_SIZE_WORLD);
                    WorldUvLookup[iVoxelType, iFace, 2] = new Vector2(x + NORMALIZED_BLOCK_SIZE_WORLD, y);
                    WorldUvLookup[iVoxelType, iFace, 3] = new Vector2(x + NORMALIZED_BLOCK_SIZE_WORLD, y + NORMALIZED_BLOCK_SIZE_WORLD);
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
    }
}