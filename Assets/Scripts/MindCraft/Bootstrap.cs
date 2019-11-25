using strange.extensions.context.impl;
using Temari.Common;

namespace MindCraft
{
    public class Bootstrap : ContextView
    {
        public ViewConfig ViewConfig;
        
        private MindCraftContext _context;

        private void Start()
        {
            _context = new MindCraftContext(this, ViewConfig);
            _context.Start();
        }

        private void OnApplicationQuit()
        {
            _context.OnRemove();
        }
    }
}