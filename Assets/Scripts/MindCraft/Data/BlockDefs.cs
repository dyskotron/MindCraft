using MindCraft.Common;
using MindCraft.Data.Defs;

namespace MindCraft.Data
{
    public enum BlockTypeId
    {   
        None = 0, //Used only for user modified voxel map -> we'll display block from original generated map
        Air,
        GreyStone,
        Dirt,
        DirtWithGrass,
        DirtWithSand,
        DirtWithSnow,
        Stone,
        StoneWithGrass,
        StoneWithSand,
        StoneWithSnow,
        Redsand,
        Redstone,
        RedstoneWithSand,
        Wood,
        Glass,
        Sand,
        Snow,
        Ice,
        Water,
        Lava,
        Trunk,
        TrunkWhite,
        Leaves,
    }

    public static class BlockTypeByte
    {
        public const byte NONE = (byte)BlockTypeId.None;
        public const byte AIR = (byte)BlockTypeId.Air;
        public const byte GREY_STONE = (byte)BlockTypeId.GreyStone;
        public const byte STONE = (byte)BlockTypeId.Stone;
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