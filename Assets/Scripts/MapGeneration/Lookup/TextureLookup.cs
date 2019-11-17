using MapGeneration.Defs;
using UnityEngine;

namespace MapGeneration.Lookup
{
    public class TextureLookup
    {
        public VoxelDef[] VoxelDefs => Locator.World.VoxelDefs;
        
        //texture consts
        private const int BLOCKS_PER_TEXTURE_DIMENSION = 4;
        private const float NORMALIZED_BLOCK_SIZE = 1f / BLOCKS_PER_TEXTURE_DIMENSION;

        public Vector2[,,] UvLookup;

        public void Init()
        {
            UvLookup = new Vector2[VoxelDefs.Length, 6, 4];
            
            for(var iVoxelType = 0; iVoxelType < VoxelDefs.Length; iVoxelType++)
            {
                var voxelDef = VoxelDefs[iVoxelType];
                
                for(var iFace = 0; iFace < voxelDef.FaceTextures.Length; iFace++)
                {
                    var textureId = voxelDef.FaceTextures[iFace];
                    
                    float x = (textureId % BLOCKS_PER_TEXTURE_DIMENSION) * NORMALIZED_BLOCK_SIZE;
                    float y = (int) (textureId / BLOCKS_PER_TEXTURE_DIMENSION) * NORMALIZED_BLOCK_SIZE;

                    UvLookup[iVoxelType, iFace, 0] = new Vector2(x, y);
                    UvLookup[iVoxelType, iFace, 1] = new Vector2(x, y + NORMALIZED_BLOCK_SIZE);
                    UvLookup[iVoxelType, iFace, 2] = new Vector2(x + NORMALIZED_BLOCK_SIZE, y);
                    UvLookup[iVoxelType, iFace, 3] = new Vector2(x + NORMALIZED_BLOCK_SIZE, y + NORMALIZED_BLOCK_SIZE);
                    
                }
            } 
        }
    }
}