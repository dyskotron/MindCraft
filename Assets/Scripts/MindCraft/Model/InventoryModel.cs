using MindCraft.Data;

namespace MindCraft.Model
{
    public interface IInventoryModel
    {
        BlockTypeId SelectedBlockType { get; set; }    
    }
    
    public class InventoryModel : IInventoryModel
    {
        public BlockTypeId SelectedBlockType { get; set; } = BlockTypeId.Wood;
    }
}