using strange.extensions.context.impl;
using Temari.Common;
using UnityEngine;

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
        
        void OnDestroy()
        {
            #if UNITY_EDITOR
            OnApplicationQuit();
            #endif
        }

        private void OnApplicationQuit()
        {
            _context.OnRemove();
        }
    }
}