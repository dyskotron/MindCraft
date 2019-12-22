using Framewerk.StrangeCore;
using MindCraft.Common;
using MindCraft.Data.Defs;
using Unity.Collections;

namespace MindCraft.Data
{
    public interface IBlockDefs
    {
        BlockDef GetDefinitionById(BlockTypeId id);
        BlockDef[] GetAllDefinitions();

        NativeArray<BlockDefData> BlockDataLookup { get; }
    }

    public class BlockDefs : ScriptableObjectDefintions<BlockDef, BlockTypeId> , IBlockDefs, IDestroyable
    {
        public NativeArray<BlockDefData> BlockDataLookup => _blockDataLookup;
        
        protected override string Path => ResourcePath.BLOCK_DEFS;
        
        private NativeArray<BlockDefData> _blockDataLookup;

        public override void PostConstruct()
        {
            base.PostConstruct();
            
            var defs = GetAllDefinitions();
            
            _blockDataLookup = new NativeArray<BlockDefData>(defs.Length, Allocator.Persistent);
            foreach (var blockDef in defs)
            {
                _blockDataLookup[(int) blockDef.Id] = blockDef.GetBlockData();
            }
        }

        public void Destroy()
        {
            _blockDataLookup.Dispose();    
        }
    }
}