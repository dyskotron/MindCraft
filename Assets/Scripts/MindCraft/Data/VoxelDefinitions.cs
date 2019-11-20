using DefaultNamespace;
using MindCraft.Data.Defs;

namespace MindCraft.Data
{
    public enum BlockTypeId
    {   
        None = 0, //Used only for user modified voxel map -> we'll display block from original generated map
        Air,
        HardRock,
        Rock,
        Dirt,
        DirtWithGrass,
    }

    public static class VoxelTypeByte
    {
        public const byte NONE = (byte)BlockTypeId.None;
        public const byte AIR = (byte)BlockTypeId.Air;
        public const byte HARD_ROCK = (byte)BlockTypeId.HardRock;
        public const byte ROCK = (byte)BlockTypeId.Rock;
        public const byte DIRT = (byte)BlockTypeId.Dirt;
        public const byte DIRT_WITH_GRASS = (byte)BlockTypeId.DirtWithGrass;
    }
    
    public interface IBlockDefs
    {
        BlockDef GetDefinitionById(BlockTypeId id);
        BlockDef[] GetAllDefinitions();
    }

    public class BlockDefs : ScriptableObjectDefintions<BlockDef, BlockTypeId> , IBlockDefs
    {
        protected override string Path => ResourcePath.BLOCK_DEFS;
    }
}