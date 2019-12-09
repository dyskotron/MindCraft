using System;
using Plugins.Framewerk;
using strange.extensions.context.impl;
using UnityEngine;

namespace MindCraft
{
    public class Bootstrap : ContextView
    {
        public ViewConfig ViewConfig;

        [Range(1f,0)]
        public float GlobalLightLevel = 1f;
        
        private MindCraftContext _context;
        
        private static readonly int LightLevel = Shader.PropertyToID("GlobalLightLevel");
        private static readonly int MinGlobalLightLevel = Shader.PropertyToID("minGlobalLightLevel");
        private static readonly int MaxGlobalLightLevel = Shader.PropertyToID("maxGlobalLightLevel");

        private void Start()
        {
            _context = new MindCraftContext(this, ViewConfig);
            _context.Start();
            
            Shader.SetGlobalFloat(MinGlobalLightLevel, 0);
            Shader.SetGlobalFloat(MaxGlobalLightLevel, 1);
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

        private void Update()
        {
            Shader.SetGlobalFloat(LightLevel, GlobalLightLevel);
        }
    }
}