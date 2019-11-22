using Framewerk.UI.List;
using MindCraft.Data;

namespace MindCraft.View.Inventory
{
    public class InventoryItemData : IListItemDataProvider
    {
        public BlockTypeId BlockTypeId { get; }
        
        public InventoryItemData(BlockTypeId blockTypeId)
        {
            BlockTypeId = blockTypeId;
        }
    }
}