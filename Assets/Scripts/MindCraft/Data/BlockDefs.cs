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

        NativeArray<bool> TransparencyLookup { get; }
    }

    public class BlockDefs : ScriptableObjectDefintions<BlockDef, BlockTypeId> , IBlockDefs, IDestroyable
    {
        public NativeArray<bool> TransparencyLookup => _transparencyLookup;
        
        protected override string Path => ResourcePath.BLOCK_DEFS;
        
        private NativeArray<bool> _transparencyLookup;

        public override void PostConstruct()
        {
            base.PostConstruct();
            
            var defs = GetAllDefinitions();
            
            _transparencyLookup = new NativeArray<bool>(defs.Length, Allocator.Persistent);
            foreach (var blockDef in defs)
            {
                _transparencyLookup[(int) blockDef.Id] = blockDef.IsTransparent;
            }
        }

        public void Destroy()
        {
            _transparencyLookup.Dispose();    
        }
    }
}