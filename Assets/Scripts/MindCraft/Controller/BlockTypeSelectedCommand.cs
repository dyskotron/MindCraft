using MindCraft.Data;
using MindCraft.Model;
using strange.extensions.command.impl;
using strange.extensions.signal.impl;

namespace MindCraft.Controller
{
    public class BlockTypeSelectedSignal : Signal<BlockTypeId>
    {
        
    }
    
    public class BlockTypeSelectedCommand : Command
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public BlockTypeId BlockTypeId { get; set; }
        
        public override void Execute()
        {
            InventoryModel.SelectedBlockType = BlockTypeId;
        }
    }
}